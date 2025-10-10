using UnityEngine;

public class DecisionEdge
{
    public string ActionName;
    public float Weight;
    public DecisionNode TargetNode;

    public DecisionEdge(string actionName, float weight, DecisionNode targetNode)
    {
        ActionName = actionName;
        Weight = weight;
        TargetNode = targetNode;
    }

    public void ExecuteAction()
    {
        Debug.Log("Final Boss performs: " + ActionName);

        // Find the boss GameObject (must be tagged "Boss")
        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss != null)
        {
            Animator animator = boss.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(ActionName); // Triggers animation
            }
        }
    }

    public void AdjustWeight(float reward)
    {
        Weight += reward;
        Weight = Mathf.Clamp(Weight, 0.1f, 10f);
    }
}
