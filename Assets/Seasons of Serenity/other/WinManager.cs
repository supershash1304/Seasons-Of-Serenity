using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : MonoBehaviour
{
    public static WinManager Instance { get; private set; }

    [Header("Boss Win Condition")]
    [SerializeField] private int totalBossesToWin = 3;
    [SerializeField] private string endMenuSceneName = "EndMenu";
    [SerializeField] private float winDelay = 2f; // allow final death animation

    private int bossesDead = 0;
    private bool gameWon = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Call this when a boss dies
    public void RegisterBossDeath()
    {
        if (gameWon) return;

        bossesDead++;

        if (bossesDead >= totalBossesToWin)
        {
            gameWon = true;
            StartCoroutine(LoadEndMenuAfterDelay());
        }
    }

    private IEnumerator LoadEndMenuAfterDelay()
    {
        yield return new WaitForSeconds(winDelay);
        SceneManager.LoadScene(endMenuSceneName);
    }
}
