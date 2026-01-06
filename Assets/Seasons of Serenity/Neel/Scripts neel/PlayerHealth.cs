using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    public int CurrentHealth { get; private set; }

    // 🔔 Event for UI / other systems
    public event Action<float> OnHealthNormalizedChanged;
    public event Action OnPlayerDied;

    private void Start()
    {
        CurrentHealth = maxHealth;
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        Debug.Log($"Player took {amount} damage. HP: {CurrentHealth}/{maxHealth}");

        NotifyHealthChanged();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void NotifyHealthChanged()
    {
        float normalized = (float)CurrentHealth / maxHealth;
        OnHealthNormalizedChanged?.Invoke(normalized);
    }

    private void Die()
    {
        Debug.Log("Player has died.");

        OnPlayerDied?.Invoke();

        // TODO:
        // - disable movement
        // - play death animation
        // - show game over UI
    }
}
