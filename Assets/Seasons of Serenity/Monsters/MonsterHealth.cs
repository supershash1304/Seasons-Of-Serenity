using UnityEngine;
using UnityEngine.AI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHealth = 50;
    public int CurrentHealth { get; private set; }

    private MonsterAI ai;
    private Animator animator;
    private NavMeshAgent agent;

    private bool isDead = false;

    private void Awake()
    {
        // Use Awake so references exist before other scripts hit us
        ai = GetComponent<MonsterAI>();
        agent = GetComponent<NavMeshAgent>();

        // Animator is often on a child for rigged monsters
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        Debug.Log($"[MonsterHealth] {name} took {damageAmount} damage. HP: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth > 0)
        {
            if (animator != null)
                animator.SetTrigger("Hit");
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        Debug.Log($"[MonsterHealth] {name} died.");

        if (animator != null)
            animator.SetTrigger("Die");

        if (agent != null) agent.enabled = false;
        if (ai != null) ai.enabled = false;

        Destroy(gameObject, 4f);
    }

    public bool IsDead() => isDead;
}
