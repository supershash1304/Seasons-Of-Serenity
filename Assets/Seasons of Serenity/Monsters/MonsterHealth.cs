using UnityEngine;
using UnityEngine.AI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHealth = 50;
    private int currentHealth;

    private MonsterAI ai;
    private Animator animator;
    private NavMeshAgent agent;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        ai = GetComponent<MonsterAI>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (currentHealth > 0)
        {
            // Optional Hurt animation
            animator.SetTrigger("Hit");
        }
        else
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        // Play death animation
        animator.SetTrigger("Die");

        // Disable movement
        if (agent != null) agent.enabled = false;
        if (ai != null) ai.enabled = false;

        // Destroy the monster after animation
        Destroy(gameObject, 4f);
    }

    public bool IsDead()
    {
        return isDead;
    }
}
