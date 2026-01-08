using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int CurrentHealth { get; private set; }

    [Header("Optional")]
    public bool destroyOnDeath = true;
    public float destroyDelay = 4f;

    [Header("Boss Settings")]
    [SerializeField] private bool isBoss = false;

    private bool isDead;
    private bool bossDeathReported = false;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        CurrentHealth -= dmg;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        Debug.Log($"[EnemyHealth] {name} took {dmg}. HP {CurrentHealth}/{maxHealth}");

        // Let AI / Animator react (optional)
        SendMessage("OnHit", SendMessageOptions.DontRequireReceiver);

        if (CurrentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[EnemyHealth] {name} died.");

        // Let AI / Animator handle death
        SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);

        // ✅ Notify WinManager ONLY if this enemy is a boss
        if (isBoss && !bossDeathReported)
        {
            bossDeathReported = true;

            if (WinManager.Instance != null)
            {
                WinManager.Instance.RegisterBossDeath();
            }
            else
            {
                Debug.LogWarning("[EnemyHealth] WinManager not found in scene!");
            }
        }

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }

    public bool IsDead() => isDead;
}
