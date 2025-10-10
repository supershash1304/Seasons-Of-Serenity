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
}
