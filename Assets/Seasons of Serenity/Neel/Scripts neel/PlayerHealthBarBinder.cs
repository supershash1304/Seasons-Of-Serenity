using UnityEngine;
using MagicPigGames;

public class PlayerHealthBarBinder : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public ProgressBar progressBar;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (progressBar == null)
            progressBar = GetComponent<ProgressBar>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthNormalizedChanged += UpdateBar;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthNormalizedChanged -= UpdateBar;
    }

    private void Start()
    {
        if (playerHealth != null)
            UpdateBar(playerHealth.CurrentHealth / (float)playerHealth.maxHealth);
    }

    private void UpdateBar(float normalized)
    {
        progressBar.SetProgress(normalized);
    }
}
