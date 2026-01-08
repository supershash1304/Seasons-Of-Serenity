using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;          // keep public if other scripts access it directly
    private int currentHealth;
    private bool isDead = false;

    // ✅ Binder expects this event (subscribe/unsubscribe)
    public event Action<float> OnHealthNormalizedChanged;

    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 5f;
    [SerializeField] private string startMenuSceneName = "StartMenu";

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI deathText;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;

        if (deathPanel != null)
            deathPanel.SetActive(false);

        // ✅ Push initial value to health bar
        NotifyHealthNormalizedChanged();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // ✅ Update health bar
        NotifyHealthNormalizedChanged();

        if (currentHealth <= 0)
            Die();
    }

    // Optional if you ever add healing
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        NotifyHealthNormalizedChanged();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Disable player control scripts so they can't move/attack while dead
        var movement = GetComponent<NeelMovement>();
        if (movement) movement.enabled = false;

        var attack = GetComponent<NeelAttack>();
        if (attack) attack.enabled = false;

        var controller = GetComponent<CharacterController>();
        if (controller) controller.enabled = false;

        // Show death UI
        if (deathPanel != null)
            deathPanel.SetActive(true);

        if (deathText != null)
            deathText.text = "You Died\nLoading Start Menu...";

        StartCoroutine(LoadStartMenuAfterDelay());
    }

    private IEnumerator LoadStartMenuAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        SceneManager.LoadScene(startMenuSceneName);
    }

    private void NotifyHealthNormalizedChanged()
    {
        float normalized = (maxHealth <= 0) ? 0f : (float)currentHealth / maxHealth;
        OnHealthNormalizedChanged?.Invoke(normalized);
    }
}
