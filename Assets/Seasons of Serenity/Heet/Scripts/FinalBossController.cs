using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for lists/dictionaries in the matrix

public class FinalBossController : MonoBehaviour
{
    // --- Configuration Variables ---
    public Transform player; // Assign the Player's Transform in the Inspector
    public float detectionRadius = 10f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float attackRayDistance = 3f; 
    
    [Header("Health & Damage")]
    // Monster Health: 100 points (based on earlier request)
    public int bossHealth = 100; 
    // Monster Damage: 5 points (based on earlier request)
    public int attackDamage = 5; 

    [Header("Physics & Layers")]
    public LayerMask groundLayer; 
    public LayerMask playerLayer; 
    public float groundCheckDistance = 0.3f;

    // --- Private Components & State ---
    private Rigidbody rb;
    private Animator animator;
    private bool isActive = false;
    private bool isGrounded;
    private bool isAttacking = false;

    // --- Decision Matrix Variables ---
    private DecisionNode[,] matrix;
    private DecisionNode currentNode;
    private DecisionEdge lastAction;

    // --- Life Cycle Methods ---

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        // Ensure Player is found
        if (player == null) 
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        InitializeMatrix();
        currentNode = matrix[0, 0]; // Starting node
        StartCoroutine(AIDecisionLoop());
    }

    private void Update()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundLayer);

        // Activation check
        if (!isActive && player != null && Vector3.Distance(transform.position, player.position) <= detectionRadius)
        {
            isActive = true;
        }

        // Boss death check
        if (bossHealth <= 0)
        {
            if (this.enabled) 
            {
                animator.SetTrigger("Die");
                StopAllCoroutines();
                this.enabled = false;
                // You might also want to disable the Rigidbody and Collider here
            }
            return;
        }

        // Follow player if active AND not attacking
        if (isActive && !isAttacking && player != null)
        {
            FollowPlayer();
        }
    }

    // --- AI Decision Loop ---

    private IEnumerator AIDecisionLoop()
    {
        // Add a delay before starting the loop to prevent immediate first action
        yield return new WaitForSeconds(0.5f); 
        
        while (true)
        {
            // Only make a decision if active, have a node, and not currently attacking
            if (isActive && currentNode != null && !isAttacking)
            {
                // 1. Select action based on weighted probability
                lastAction = currentNode.GetRandomEdge();
                
                if (lastAction != null)
                {
                    // 2. Begin attack phase
                    isAttacking = true;
                    // Trigger animation (and implicitly call PerformRaycastAttack via event)
                    animator.SetTrigger(lastAction.ActionName); 
                    currentNode = lastAction.TargetNode; // Move to the next decision state

                    // 3. Wait until the animation event (OnAttackComplete) is called 
                    // This blocks the loop until the attack animation finishes
                    while (isAttacking)
                        yield return null;
                }
            }
            // Add a small breather/delay before checking again
            yield return new WaitForSeconds(0.2f); 
        }
    }

    // --- Animation Events (Called from Animator Controller) ---

    // Called via Animation Event at the end of the attack animation
    public void OnAttackComplete()
    {
        isAttacking = false;
        // Optional: Reset movement animations here (e.g., animator.SetBool("IsRunning", true))
    }

    // Called via Animation Event at the point of impact in the attack animation
    public void PerformRaycastAttack()
    {
        if (player == null) return;

        RaycastHit hit;
        
        // Raycast from the boss towards the player within attack range
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, attackRayDistance, playerLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // Monster deals its damage (5 points)
                hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage); 
                ApplyFeedback(true);
            }
        }
        else
        {
            ApplyFeedback(false);
        }
    }

    // --- Damage & Reinforcement ---

    // Public method called by the Player's attack script (PlayerAttackController)
    // Takes 25 points of damage from player's attack (as per earlier request)
    public void ReceiveDamage(int damage)
    {
        bossHealth -= damage;
        // Optional: Play 'Hurt' animation trigger if bossHealth > 0
    }

    public void ApplyFeedback(bool successfulHit)
    {
        if (lastAction == null) return;

        float reward = successfulHit ? 0.5f : -0.3f;
        lastAction.AdjustWeight(reward);
        
        // You can remove this Debug.Log once the system is working
        Debug.Log($"Reinforcement applied to {lastAction.ActionName}. New Weight = {lastAction.Weight}");
    }

    // --- Movement & Matrix Initialization ---

    private void FollowPlayer()
    {
        if (!isGrounded || player == null) return;

        // Rotation
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        // Movement 
        Vector3 move = direction * moveSpeed * Time.deltaTime;
        rb.MovePosition(transform.position + move);
        
        // Optional: Trigger Run/Walk animation here
        // animator.SetBool("IsRunning", true); 
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

        // Connecting the nodes with actions
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                DecisionNode node = matrix[x, y];

                // Note: The DecisionNode/Edge logic will select which attack to perform
                if (x < size - 1) node.AddEdge(Direction.Right, new DecisionEdge("Attack01", 1.0f, matrix[x + 1, y]));
                if (y < size - 1) node.AddEdge(Direction.Down, new DecisionEdge("Attack02Maintain", 1.2f, matrix[x, y + 1]));
                if (x > 0) node.AddEdge(Direction.Left, new DecisionEdge("Attack03Maintain", 0.8f, matrix[x - 1, y]));
                if (y > 0) node.AddEdge(Direction.Up, new DecisionEdge("Attack04", 1.5f, matrix[x, y - 1]));
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * attackRayDistance);
    }
}