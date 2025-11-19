using UnityEngine;
using UnityEngine.AI; // Needed for NavMeshAgent disable

public class MonsterHealth : MonoBehaviour
{
    // The monster's max health, set to 50 as requested
    public int maxHealth = 50; 
    private int currentHealth;
    
    // Optional: Reference to the AI script for disabling
    private FinalBossController aiController;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        // Get references to components that need to be disabled on death
        aiController = GetComponent<FinalBossController>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Public method called by the player's attack script when a hit is detected.
    /// This is where the monster takes damage.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to subtract.</param>
    public void TakeDamage(int damageAmount)
    {
        // Safety check to prevent damage after death
        if (currentHealth <= 0) return; 

        currentHealth -= damageAmount;
        
        Debug.Log(gameObject.name + " took " + damageAmount + " damage. Remaining health: " + currentHealth);

        // Optional: Trigger a 'Hurt' animation if health is still positive
        if (currentHealth > 0 && animator != null)
        {
            // Assuming you have a "Hurt" trigger in your Animator
            // animator.SetTrigger("Hurt"); 
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " has been defeated!");

        // 1. Play the death animation (assuming "Die" is a Trigger)
        if (animator != null)
        {
            animator.SetTrigger("Die"); 
        }

        // 2. Stop movement (if using NavMeshAgent)
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 3. Prevent AI logic from running
        if (aiController != null)
        {
            aiController.enabled = false; 
        }

        // 4. Disable this health script to prevent further damage calls
        this.enabled = false;

        // 5. Destroy the GameObject after the death animation plays out
        Destroy(gameObject, 5f); 
    }
}