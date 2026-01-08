using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject introPanel;

    [Header("Intro UI")]
    [SerializeField] private TextMeshProUGUI introText;
    [SerializeField] private GameObject nextButtonObject; // the button gameObject (optional)

    [Header("Scene To Load")]
    [SerializeField] private string gameSceneName = "Game Scene";

    // You can edit these in code or expose them in inspector by making them [TextArea] fields.
    private string[] pages;
    private int currentPageIndex = 0;

    private void Awake()
    {
        // Intro/instructions pages
    {
        pages = new string[]
        {
        "WELCOME!\n\nThis is a third-person action game.\n\nControls:\nW = Move forward\nA/D = Rotate\nShift+W = Run\nSpace = Jump",

        "COMBAT:\n\nPress 1-4 to select a beam attack.\nLeft Click to fire.\nYour beams use raycasting (with spread) to hit enemies.",

        "ENEMIES & BOSSES:\n\n- Basic monsters use rule-based AI\n- Water Monster uses a Behavior Tree\n- Final Boss adapts using 2d matrix randomizer and weighted attack choices.",

        "HINT:\n\nThe final boss is located on the EXACT OPPOSITE side of the map from where you start.\n\nUse landmarks to navigate — LOOK OUT FOR THE LAVA FALL."
        };
    }

}

private void Start()
    {
        // Initial UI state
        mainMenuPanel.SetActive(true);
        introPanel.SetActive(false);
    }

    // Hook this to Start button OnClick()
    public void OnStartPressed()
    {
        mainMenuPanel.SetActive(false);
        introPanel.SetActive(true);

        currentPageIndex = 0;
        UpdateIntroPage();
    }

    // Hook this to Next button OnClick()
    public void OnNextPressed()
    {
        currentPageIndex++;

        if (currentPageIndex >= pages.Length)
        {
            // Done reading -> start game
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        UpdateIntroPage();
    }

    // Hook this to Quit button OnClick()
    public void OnQuitPressed()
    {
        // Works in builds; in editor it won't close play mode.
        Application.Quit();
    }

    private void UpdateIntroPage()
    {
        introText.text = pages[currentPageIndex];

        // Optional: change Next button text to "Play" on last page (if using TMP on button)
        // If you want this, put a TMP text on the button and reference it.
        // Or simply leave it as "Next".
    }
}
