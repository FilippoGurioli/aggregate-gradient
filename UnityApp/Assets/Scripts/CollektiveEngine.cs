using System.Collections.Generic;
using UnityEngine;

public class CollektiveEngine : MonoBehaviour
{
    [SerializeField] private int nodeCount = 10;
    [SerializeField] private double maxDistance = 3f;
    [SerializeField] private List<int> sources = new List<int> { 0 };
    [SerializeField] private float timeScale = 0.1f;
    [SerializeField] private int rounds = 10;
    [SerializeField] private NodeBehaviour nodePrefab;
    [SerializeField] private float distance = 3f;
    [SerializeField] private bool noStop;

    private int _handle;
    private int _currentRound;
    private Dictionary<int, NodeBehaviour> _nodes = new();

    private void Awake()
    {
        _handle = CollektiveApiWithDistance.Create(nodeCount, maxDistance);
        foreach (var source in sources)
            CollektiveApiWithDistance.SetSource(_handle, source, true);
        Time.timeScale = timeScale;
        CreateNodeTree();
    }

    private void FixedUpdate() //internal simulation loop -> currently bound to unity | to be uncorrelated
    {
        if (!noStop && _currentRound >= rounds) return;
        _currentRound++;
        foreach (var (_, node) in _nodes)
            CollektiveApiWithDistance.UpdatePosition(_handle, node.Id, node.transform.position);
        CollektiveApiWithDistance.Step(_handle, 1);
    }

    private void CreateNodeTree()
    {
        // 1. Compute positions using a BFS-tree layout rooted at node 0
        var positions = ComputeTreeLayout(_handle, nodeCount, distance, distance * 3);

        // 2. Instantiate nodes using the computed positions
        for (var i = 0; i < nodeCount; i++)
        {
            if (!positions.TryGetValue(i, out var position))
            {
                // Fallback in case something went wrong; shouldn't normally happen
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
    private static Dictionary<int, Vector3> ComputeTreeLayout(
        int handle,
        int nodeCount,
        float horizontalSpacing,
        float verticalSpacing)
    {
        var positions = new Dictionary<int, Vector3>(nodeCount);

        var visited = new HashSet<int>();
        var depth = new Dictionary<int, int>();         // nodeId -> layer index
        var layers = new Dictionary<int, List<int>>();  // layer index -> nodes in that layer

        var queue = new Queue<int>();

        // Root is node 0
        const int rootId = 0;
        visited.Add(rootId);
        depth[rootId] = 0;
        layers[0] = new List<int> { rootId };
        queue.Enqueue(rootId);

        // BFS to build a spanning tree from node 0
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDepth = depth[current];

            // Get neighbors from the native engine
            var neighbors = CollektiveApiWithDistance.GetNeighborhood(handle, current);
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

        // Handle unreachable nodes (if the graph is not fully connected)
        var unreachable = new List<int>();
        for (var i = 0; i < nodeCount; i++)
        {
            if (!visited.Contains(i))
            {
                unreachable.Add(i);
            }
        }

        if (unreachable.Count > 0)
        {
            // Put them in a separate layer after the last BFS layer
            var extraLayerIndex = layers.Count;
            layers[extraLayerIndex] = unreachable;
            foreach (var n in unreachable)
            {
                depth[n] = extraLayerIndex;
            }
        }

        // Now assign actual world positions per layer
        foreach (var kvp in layers)
        {
            var layerIndex = kvp.Key;
            var nodesInLayer = kvp.Value;

            var count = nodesInLayer.Count;
            if (count == 0) continue;

            // Center the layer around x = 0
            var totalWidth = (count - 1) * horizontalSpacing;
            var startX = -totalWidth / 2f;
            var y = -layerIndex * verticalSpacing; // downwards per level

            for (var i = 0; i < count; i++)
            {
                var nodeId = nodesInLayer[i];
                var x = startX + i * horizontalSpacing;
                positions[nodeId] = new Vector3(x, y, 0f);
            }
        }

        return positions;
    }

    public int GetValue(int id) => CollektiveApiWithDistance.GetValue(_handle, id);

    public List<(NodeBehaviour, NodeBehaviour)> GetAllLinks()
    {
        var result = new List<(NodeBehaviour, NodeBehaviour)>();
        foreach (var (id, node) in _nodes)
        {
            var neighborhood = CollektiveApiWithDistance.GetNeighborhood(_handle, node.Id);
            foreach (var neighborId in neighborhood)
            {
                if (!_nodes.TryGetValue(neighborId, out var neighborNode))
                    continue;
                if (node.Id >= neighborNode.Id)
                    continue;
                result.Add((node, neighborNode));
            }
        }
        return result;
    }
}
