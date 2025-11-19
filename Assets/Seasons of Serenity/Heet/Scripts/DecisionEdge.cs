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

    public void AdjustWeight(float reward)
    {
        Weight += reward;
        Weight = Mathf.Clamp(Weight, 0.1f, 10f);  // Keeps the weight usable
    }

    public void ExecuteAction()
    {
        Debug.Log("Executing attack: " + ActionName);

        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss != null)
        {
            Animator animator = boss.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(ActionName);
            }
        }
    }
}
