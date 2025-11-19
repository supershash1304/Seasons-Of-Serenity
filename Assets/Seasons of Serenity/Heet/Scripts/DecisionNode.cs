using System.Collections.Generic;
using UnityEngine;

public class DecisionNode
{
    public Vector2Int Position { get; private set; }
    public Dictionary<Direction, DecisionEdge> Edges;

    public DecisionNode(int x, int y)
    {
        Position = new Vector2Int(x, y);
        Edges = new Dictionary<Direction, DecisionEdge>();
    }

    public void AddEdge(Direction direction, DecisionEdge edge)
    {
        if (!Edges.ContainsKey(direction))
        {
            Edges[direction] = edge;
        }
    }

    public List<DecisionEdge> GetAvailableEdges()
    {
        return new List<DecisionEdge>(Edges.Values);
    }

    public DecisionEdge GetRandomEdge()
    {
        List<DecisionEdge> options = GetAvailableEdges();
        if (options.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var edge in options)
        {
            totalWeight += edge.Weight;
        }

        float rand = Random.Range(0, totalWeight);
        float cumulative = 0f;
        foreach (var edge in options)
        {
            cumulative += edge.Weight;
            if (rand <= cumulative)
                return edge;
        }

        return options[options.Count - 1]; // fallback
    }
}
