using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FinalBossController : MonoBehaviour
{
    [Header("Player & Vision")]
    public Transform player;
    public float detectionRadius = 10f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.4f;

    [Header("Combat")]
    public float attackRayDistance = 3f;
    public int attackDamage = 5;
    public LayerMask playerLayer;

    [Header("Health")]
    public int bossHealth = 100;

    private Rigidbody rb;
    private Animator animator;

    private bool isActive = false;
    private bool isAttacking = false;
    private bool isGrounded = false;

    // Decision matrix
    private DecisionNode[,] matrix;
    private DecisionNode currentNode;
    private DecisionEdge lastAction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        InitializeMatrix();
        currentNode = matrix[0, 0];

        StartCoroutine(AIDecisionLoop());
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.3f,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );

        // Activate
        if (!isActive && player != null &&
            Vector3.Distance(transform.position, player.position) <= detectionRadius)
        {
            isActive = true;
        }

        // Death
        if (bossHealth <= 0)
        {
            animator.SetTrigger("Die");
            StopAllCoroutines();
            enabled = false;
            return;
        }

        // Follow player
        if (isActive && !isAttacking)
        {
            FollowPlayer();
        }
    }

    // ------------------------------------------
    // AI LOOP
    // ------------------------------------------
    private IEnumerator AIDecisionLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            if (isActive && !isAttacking && currentNode != null)
            {
                lastAction = currentNode.GetRandomEdge();

                if (lastAction != null)
                {
                    isAttacking = true;

                    // Trigger the attack animation via matrix
                    animator.SetTrigger(lastAction.ActionName);

                    // Move to next state node
                    currentNode = lastAction.TargetNode;

                    // Wait for OnAttackComplete()
                    while (isAttacking)
                        yield return null;
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    // ------------------------------------------
    // ANIMATION EVENTS
    // ------------------------------------------

    // Called on LAST FRAME of each attack animation
    public void OnAttackComplete()
{
    isAttacking = false;

    // After attack, go back to running animation
    animator.SetFloat("Speed", 1f);
}


    // Called at IMPACT frame during animations
    public void PerformRaycastAttack()
    {
        if (player == null) return;

        Vector3 origin = transform.position + Vector3.up * 1.6f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, attackRayDistance, playerLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
                ApplyFeedback(true);
            }
        }
        else
        {
            ApplyFeedback(false);
        }
    }

    // ------------------------------------------
    // MOVEMENT
    // ------------------------------------------
    private void FollowPlayer()
{
    if (!isGrounded || player == null) return;

    Vector3 direction = (player.position - transform.position).normalized;
    direction.y = 0;

    Quaternion lookRotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);

    Vector3 move = direction * moveSpeed * Time.deltaTime;
    rb.MovePosition(transform.position + move);

    // IMPORTANT: tell animator to play BattleRunForward
    animator.SetFloat("Speed", 1f);
}


    // ------------------------------------------
    // DAMAGE SYSTEM
    // ------------------------------------------
    public void ReceiveDamage(int dmg)
    {
        bossHealth -= dmg;
    }

    public void ApplyFeedback(bool hitSuccess)
    {
        if (lastAction == null) return;

        float reward = hitSuccess ? 0.5f : -0.3f;
        lastAction.AdjustWeight(reward);

        Debug.Log($"RL Updated: {lastAction.ActionName} weight = {lastAction.Weight}");
    }

    // ------------------------------------------
    // MATRIX LOGIC
    // ------------------------------------------
  private void InitializeMatrix()
{
    int size = 4;
    matrix = new DecisionNode[size, size];

    for (int x = 0; x < size; x++)
    {
        for (int y = 0; y < size; y++)
        {
            matrix[x, y] = new DecisionNode(x, y);
        }
    }

    for (int x = 0; x < size; x++)
    {
        for (int y = 0; y < size; y++)
        {
            DecisionNode node = matrix[x, y];

            // Balanced weights:
            float w1 = 1.0f; // Attack01
            float w2 = 0.9f; // Attack02
            float w3 = 1.3f; // Attack03
            float w4 = 1.4f; // Attack04

            // Edge assignments (smarter variety)
            if (x < size - 1) node.AddEdge(Direction.Right, new DecisionEdge("Attack01", w1, matrix[x + 1, y]));
            if (y < size - 1) node.AddEdge(Direction.Down, new DecisionEdge("Attack02Maintain", w2, matrix[x, y + 1]));
            if (x > 0) node.AddEdge(Direction.Left, new DecisionEdge("Attack03Maintain", w3, matrix[x - 1, y]));
            if (y > 0) node.AddEdge(Direction.Up, new DecisionEdge("Attack04", w4, matrix[x, y - 1]));
        }
    }
}

}
