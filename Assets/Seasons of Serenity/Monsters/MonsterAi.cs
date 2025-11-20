using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    public float detectionRadius = 25f;
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 10;

    private Transform player;
    private Animator animator;
    private NavMeshAgent agent;
    private MonsterHealth health;

    private float nextAttackTime = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<MonsterHealth>();

        agent.stoppingDistance = attackRange - 0.5f;
    }

    void Update()
    {
        if (health.IsDead()) return;  // Stop logic when dead

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= detectionRadius)
        {
            if (dist > attackRange)
            {
                MoveToPlayer();
            }
            else
            {
                AttackPlayer();
            }
        }
        else
        {
            Idle();
        }
    }

  void MoveToPlayer()
{
    agent.isStopped = false;
    agent.SetDestination(player.position);

    animator.SetFloat("Speed", 1f);
    Debug.Log("Speed set to 1 (moving)");

    
}


    void Idle()
    {
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f);
    }

    void AttackPlayer()
    {
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f);

        transform.LookAt(player);

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;

            animator.SetTrigger("Attack");

            // Raycast damage check
            if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, attackRange))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    PlayerHealth ph = hit.collider.GetComponent<PlayerHealth>();
                    if (ph != null)
                        ph.TakeDamage(attackDamage);
                }
            }
        }
    }
}
