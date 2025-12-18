using System.Collections.Generic;
using System.Linq;
using Data;
using Engine;
using UnityEngine;

public class CollektiveEngineSocket : MonoBehaviour, IEngine, IEngineWithLinks
{
    [Header("Socket")]
    [SerializeField] private string host;
    [SerializeField] private int port;

    [Header("Engine")]
    [SerializeField] private int nodeCount = 10;
    [SerializeField] private double maxDistance = 3f;
    [SerializeField] private List<int> sources = new List<int> { 0 };
    [SerializeField] private float timeScale = 0.1f;
    [SerializeField] private int rounds = 10;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private float distance = 3f;
    [SerializeField] private bool noStop;

    private int _currentRound;
    private readonly Dictionary<int, NodeBehaviour> _nodes = new();
    private SocketEngine _engine;
    private Dictionary<int, double> _state = new();
    private HashSet<Link> _links = new();

    private void Awake()
    {
        _engine = new SocketEngine(host, port);
        _engine.Create(nodeCount, maxDistance);
        foreach (var source in sources)
        {
            _engine.SetSource(source);
            _state[source] = 0;
        }
        for (var i = 0; i < nodeCount; i++)
        {
            if (sources.Contains(i)) continue;
            _state[i] = double.PositiveInfinity;
        }
        _engine.OnNewState += UpdateState;
        Time.timeScale = timeScale;
        CreateNodeTree();
    }

    private void UpdateState(List<(double value, List<int> neighbors)> state)
    {
        for (int i = 0; i < state.Count; i++)
            _state[i] = state[i].value;
        var newLinks = new HashSet<Link>();
        for (int i = 0; i < state.Count; i++)
            foreach (var neighbor in state[i].neighbors)
                newLinks.Add(new Link(i, neighbor));
        _links.RemoveWhere(link => !newLinks.Contains(link));
        foreach (var link in newLinks)
            _links.Add(link);
    }

    private void FixedUpdate()
    {
        if (!noStop)
        {
            Debug.Log($"{_currentRound}/{rounds}");
            _currentRound++;
            if (_currentRound >= rounds)
                Application.Quit();
        }
        foreach (var (_, node) in _nodes)
            _engine.NewPosition(node.Id, node.transform.position);
        _engine.Step();
        _engine.Poll();
    }

    private void OnDestroy() => _engine.Dispose();

    private void CreateNodeTree()
    {
        var positions = ComputeTreeLayout(nodeCount, distance, distance * 2);
        for (var i = 0; i < nodeCount; i++)
        {
            if (!positions.TryGetValue(i, out var position))
            {
                position = Vector3.zero;
            }
            var go = Instantiate(nodePrefab, position, Quaternion.identity);
            var node = go.GetComponent<NodeBehaviour>();
            node.Initialize(i, this);
            _nodes.Add(i, node);
        }
    }

    /// <summary>
    /// Computes a tree-like layout:
    /// - Root is node 0 at the top.
    /// - Each BFS level is one row lower.
    /// - Nodes in the same level are spaced horizontally.
    /// - Unreachable nodes are placed in a separate row at the bottom.
    /// </summary>
    private Dictionary<int, Vector3> ComputeTreeLayout(
        int nodeCount,
        float horizontalSpacing,
        float verticalSpacing)
    {
        var positions = new Dictionary<int, Vector3>(nodeCount);
        var visited = new HashSet<int>();
        var depth = new Dictionary<int, int>();
        var layers = new Dictionary<int, List<int>>();
        var queue = new Queue<int>();
        const int rootId = 0;
        visited.Add(rootId);
        depth[rootId] = 0;
        layers[0] = new List<int> { rootId };
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDepth = depth[current];
            var neighbors = GetNeighborsOf(current);
            if (neighbors == null) continue;
            foreach (var neighbor in neighbors)
            {
                if (!visited.Add(neighbor)) continue;
                var d = currentDepth + 1;
                depth[neighbor] = d;
                if (!layers.TryGetValue(d, out var list))
                {
                    list = new List<int>();
                    layers[d] = list;
                }
                list.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }
        var unreachable = new List<int>();
        for (var i = 0; i < nodeCount; i++)
        {
            if (!visited.Contains(i))
                unreachable.Add(i);
        }

        if (unreachable.Count > 0)
        {
            var extraLayerIndex = layers.Count;
            layers[extraLayerIndex] = unreachable;
            foreach (var n in unreachable)
                depth[n] = extraLayerIndex;
        }
        foreach (var kvp in layers)
        {
            var layerIndex = kvp.Key;
            var nodesInLayer = kvp.Value;
            var count = nodesInLayer.Count;
            if (count == 0) continue;
            var totalWidth = (count - 1) * horizontalSpacing;
            var startX = -totalWidth / 2f;
            var y = -layerIndex * verticalSpacing;
            for (var i = 0; i < count; i++)
            {
                var nodeId = nodesInLayer[i];
                var x = startX + i * horizontalSpacing;
                positions[nodeId] = new Vector3(x, y, 0f);
            }
        }
        return positions;
    }

    private List<int> GetNeighborsOf(int id) => _links
      .Where(l => l.Node1 == id || l.Node2 == id)
      .Select(l => l.Node1 == id ? l.Node2 : l.Node1)
      .ToList();

    public double GetValue(int id) => _state[id];

    public List<(NodeBehaviour, NodeBehaviour)> GetAllLinks() => _links.Select(l => (_nodes[l.Node1], _nodes[l.Node2])).ToList();

    public bool IsSource(int id) => sources.Contains(id);
}
