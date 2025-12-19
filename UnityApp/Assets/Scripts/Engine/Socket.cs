using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Engine
{
    internal sealed class NodeStateDto
    {
        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("neighbors")]
        public List<int> Neighbors { get; set; } = new();
    }

    public sealed class SocketEngine : IDisposable
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;
        private Thread _rxThread;
        private volatile bool _running;
        private readonly ConcurrentQueue<string> _inbox = new();
        private readonly object _txLock = new();

        public event Action<List<(double value, List<int> neighbors)>> OnNewState;
        public event Action<string> OnProtocolError;
        public Func<long> NowTicks;
        public Action<string, long, long> WriteTimings;
        private readonly ConcurrentDictionary<int, long> _pendingSteps = new();

        public SocketEngine(string host, int port) => Connect(host, port);

        public void Create(int nodeCount, double maxDistance)
            => SendOp("createSim", new { nodeCount, maxDistance });

        public void SetSource(int nodeId)
            => SendOp("setSource", new { nodeId });

        public void Step(int rid, long startTicks, int stepCount = 1)
        {
            _pendingSteps[rid] = startTicks;
            SendOp("step", new { stepCount, rid });
        }

        public void NewPosition(int nodeId, Vector3 position)
            => SendOp("newPosition", new { nodeId, x = position.x, y = position.y, z = position.z });

        public void Poll(int maxMessagesPerPoll = 256)
        {
            for (int i = 0; i < maxMessagesPerPoll; i++)
            {
                if (!_inbox.TryDequeue(out var line)) break;
                HandleInboundLine(line);
            }
        }

        public void Dispose() => Disconnect();

        private void HandleInboundLine(string line)
        {
            bool doTiming = (NowTicks != null && WriteTimings != null);
            long tParse0 = 0;
            if (doTiming) tParse0 = NowTicks.Invoke();

            JObject obj;
            try { obj = JObject.Parse(line); }
            catch (Exception ex)
            {
                OnProtocolError?.Invoke($"Invalid JSON from server: {ex.Message}. Line: {line}");
                return;
            }

            var valuesToken = obj["values"];
            if (valuesToken != null && valuesToken.Type == JTokenType.Array)
            {
                try
                {
                    var state = valuesToken.ToObject<List<NodeStateDto>>();
                    var parsedState = state.Select(ns => (ns.Value, ns.Neighbors)).ToList();
                    int rid = obj["rid"]?.Value<int>() ?? -1;

                    if (doTiming)
                    {
                        long tParse1 = NowTicks.Invoke();
                        WriteTimings.Invoke("step.socket.parse", tParse0, tParse1);

                        if (rid != -1 && _pendingSteps.TryRemove(rid, out var startTicks))
                            WriteTimings.Invoke("step.socket.wait", startTicks, tParse1);
                    }
                    else
                    {
                        // still remove pending step to avoid growth
                        if (rid != -1) _pendingSteps.TryRemove(rid, out _);
                    }

                    OnNewState?.Invoke(parsedState);
                }
                catch (Exception ex)
                {
                    OnProtocolError?.Invoke($"Failed to parse State.values: {ex.Message}. Line: {line}");
                }
                return;
            }

            if (obj["error"] != null)
            {
                OnProtocolError?.Invoke(obj["error"]!.ToString());
                return;
            }

            OnProtocolError?.Invoke($"Unknown message from server: {line}");
        }

        private void Connect(string host, int port)
        {
            if (_running) return;
            _client = new TcpClient();
            _client.Connect(host, port);
            var ns = _client.GetStream();
            _writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true };
            _reader = new StreamReader(ns, new UTF8Encoding(false));
            _running = true;
            _rxThread = new Thread(ReceiveLoop) { IsBackground = true };
            _rxThread.Start();
            Debug.Log($"[Unity] Connected to {host}:{port}");
        }

        private void Disconnect()
        {
            _running = false;
            try { _client?.Close(); } catch { /* ignored */ }
            _client = null;
            _writer = null;
            _reader = null;
            if (_rxThread != null && _rxThread.IsAlive)
                _rxThread.Join(200);
            _rxThread = null;
        }

        private void ReceiveLoop()
        {
            try
            {
                while (_running && _client != null && _client.Connected)
                {
                    var line = _reader.ReadLine();
                    if (line == null) break;
                    _inbox.Enqueue(line);
                }
            }
            catch (Exception ex)
            {
                if (_running)
                    _inbox.Enqueue($"{{\"error\":\"{ex.GetType().Name}: {ex.Message}\"}}");
            }
        }

        private void SendOp(string op, object data)
        {
            var payload = new JObject
            {
                ["op"] = op,
                ["data"] = data != null ? JToken.FromObject(data) : new JObject()
            };
            var line = payload.ToString(Formatting.None);

            bool doTiming = (NowTicks != null && WriteTimings != null);
            long t0 = 0;
            if (doTiming && op == "step") t0 = NowTicks.Invoke();

            lock (_txLock)
            {
                if (_writer == null) return;
                _writer.WriteLine(line);
            }

            if (doTiming && op == "step")
            {
                long t1 = NowTicks.Invoke();
                WriteTimings.Invoke("step.socket.send", t0, t1);
            }
        }
    }
}
