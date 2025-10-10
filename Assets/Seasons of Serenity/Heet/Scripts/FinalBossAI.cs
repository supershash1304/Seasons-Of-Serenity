using System.Linq;
using UnityEngine;

usiusing UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FinalBossAI : MonoBehaviour
{
    public int gridSize = 4;
    private DecisionNode[,] matrix;
    private DecisionNode currentNode;
    private DecisionEdge lastAction;

    public float actionCooldown = 2f;
    private float actionTimer;

    void Start()
    {
        GenerateMatrix();
        currentNode = matrix[0, 0]; // Boss starts at top-left
        actionTimer = actionCooldown;
    }

    void Update()
    {
        actionTimer -= Time.deltaTime;
        if (actionTimer <= 0f)
        {
            Traverse();
            actionTimer = actionCooldown;
        }
    }

    void GenerateMatrix()
    {
        matrix = new DecisionNode[gridSize, gridSize];

        // Create nodes
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                matrix[x, y] = new DecisionNode(x, y);
            }
        }

        // Connect edges with attack states
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                DecisionNode node = matrix[x, y];

                if (x < gridSize - 1)
                    node.AddEdge(Direction.Right, new DecisionEdge("Attack1", 1f, matrix[x + 1, y]));
                if (x > 0)
                    node.AddEdge(Direction.Left, new DecisionEdge("Attack2", 1f, matrix[x - 1, y]));
                if (y < gridSize - 1)
                    node.AddEdge(Direction.Up, new DecisionEdge("Attack3", 1f, matrix[x, y + 1]));
                if (y > 0)
                    node.AddEdge(Direction.Down, new DecisionEdge("Attack4", 1f, matrix[x, y - 1]));
            }
        }
    }

    void Traverse()
    {
        if (currentNode.Edges.Count == 0) return;

        float totalWeight = currentNode.Edges.Values.Sum(e => e.Weight);
        float choice = Random.Range(0, totalWeight);
        float cumulative = 0f;

        foreach (var kvp in currentNode.Edges)
        {
            cumulative += kvp.Value.Weight;
            if (choice <= cumulative)
            {
                lastAction = kvp.Value;
                kvp.Value.ExecuteAction();
                currentNode = kvp.Value.TargetNode;
                break;
            }
        }
    }

    // Call this from external event (e.g., boss hits player)
    public void ApplyFeedback(bool successfulHit)
    {
        if (lastAction == null) return;
        float reward = successfulHit ? 0.5f : -0.3f;
        lastAction.AdjustWeight(reward);
    }
}

