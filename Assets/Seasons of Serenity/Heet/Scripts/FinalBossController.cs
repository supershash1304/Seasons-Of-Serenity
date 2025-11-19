using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalBossController : MonoBehaviour
{
    public Transform player;
    public float detectionRadius = 10f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float decisionDelay = 1f;

    public int bossHealth = 200;
    public int attackDamage = 10;

    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float groundCheckDistance = 0.3f;
    public float attackRayDistance = 3f;

    private bool isGrounded;
    private Rigidbody rb;
    private Animator animator;

    private DecisionNode[,] matrix;
    private DecisionNode currentNode;
    private DecisionEdge lastAction;
    private bool isActive = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        InitializeMatrix();
        currentNode = matrix[0, 0]; // Starting node
        StartCoroutine(AIDecisionLoop());
    }

    private void Update()
    {
        // Ground check using Raycast
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundLayer);

        // Activate boss if player enters radius
        if (!isActive && Vector3.Distance(transform.position, player.position) <= detectionRadius)
        {
            isActive = true;
        }

        // Boss death
        if (bossHealth <= 0)
        {
            animator.SetTrigger("Die");
            StopAllCoroutines();
            this.enabled = false;
            return;
        }

        // Follow player if active
        if (isActive)
        {
            FollowPlayer();
        }
    }
    private bool isAttacking = false;

public void OnAttackComplete()
{
    isAttacking = false;
}

    private IEnumerator AIDecisionLoop()
{
    while (true)
    {
        if (isActive && currentNode != null && !isAttacking)
        {
            lastAction = currentNode.GetRandomEdge();
            if (lastAction != null)
            {
                isAttacking = true;
                GetComponent<Animator>().SetTrigger(lastAction.ActionName);
                currentNode = lastAction.TargetNode;

                // Now WAIT until animation finishes via event
                while (isAttacking)
                    yield return null;
            }
        }

        yield return null;
    }
}


    public void PerformRaycastAttack()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, attackRayDistance, playerLayer))
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

    public void ApplyFeedback(bool successfulHit)
    {
        if (lastAction == null) return;

        float reward = successfulHit ? 0.5f : -0.3f;
        lastAction.AdjustWeight(reward);
        Debug.Log($"Reinforcement applied to {lastAction.ActionName}. New Weight = {lastAction.Weight}");
    }

    public void ReceiveDamage(int damage)
    {
        bossHealth -= damage;
    }

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

                if (x < size - 1) node.AddEdge(Direction.Right, new DecisionEdge("Attack01", 1.0f, matrix[x + 1, y]));
                if (y < size - 1) node.AddEdge(Direction.Down, new DecisionEdge("Attack02Maintain", 1.2f, matrix[x, y + 1]));
                if (x > 0) node.AddEdge(Direction.Left, new DecisionEdge("Attack03Maintain", 0.8f, matrix[x - 1, y]));
                if (y > 0) node.AddEdge(Direction.Up, new DecisionEdge("Attack04", 1.5f, matrix[x, y - 1]));
            }
        }
    }

    private void FollowPlayer()
    {
        if (!isGrounded) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        Vector3 move = direction * moveSpeed * Time.deltaTime;
        rb.MovePosition(transform.position + move);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
