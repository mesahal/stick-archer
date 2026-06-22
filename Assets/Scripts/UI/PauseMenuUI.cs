using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menu modal overlay.
/// Shows Resume, Settings, and Quit To Menu buttons over a dimmed background.
///
/// SETUP: Build the pause panel in the Unity Editor (GameArena scene):
///   1. Create a full-screen dim overlay Image (black, alpha 0.7)
///   2. Add a center-aligned modal panel (680×720 per design, rx=36)
///   3. Add gold top accent bar, pause icon, title, three buttons
///   4. Add status text at bottom ("Round 3 / 5 · Score 3 — 2")
///   5. Drag-assign the references in the Inspector below
///   6. Wire the Pause button (in-game HUD) to call PauseMenuUI.Instance.TogglePause()
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance;

    [Header("Root Panels")]
    [Tooltip("The full-screen overlay (dim + modal). Toggled on/off.")]
    public GameObject pauseOverlay;

    [Tooltip("Semi-transparent dim background (Image with black, alpha=0.7)")]
    public Image dimBackground;

    [Header("Modal Content")]
    [Tooltip("The modal panel containing all pause UI")]
    public CanvasGroup modalCanvasGroup;

    [Header("Buttons")]
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Status")]
    [Tooltip("Status line at bottom, e.g. 'Round 3 / 5 · Score 3 — 2 · Tap Resume to continue'")]
    public TextMeshProUGUI statusText;

    [Header("Animation")]
    [Tooltip("Animate modal scale on open")]
    public bool animateOpen = true;
    public float openDuration = 0.25f;

    // ── State ──────────────────────────────────────────
    private bool isPaused = false;
    private RectTransform modalRect;
    private bool buttonsWired;

    // ── Lifecycle ──────────────────────────────────────

    void Awake()
    {
        Instance = this;
        ResolveReferences();

        if (modalCanvasGroup != null)
            modalRect = modalCanvasGroup.GetComponent<RectTransform>();

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
    }

    void Start()
    {
        WireButtons();
    }

    void OnDestroy()
    {
        UnwireButtons();
    }

    void Update()
    {
        // ESC key or Android back button
        if (isPaused && Input.GetKeyDown(KeyCode.Escape))
            OnResume();
    }

    // ── Public API ─────────────────────────────────────

    /// <summary>Toggle pause on/off.</summary>
    public void TogglePause()
    {
        if (isPaused)
            OnResume();
        else
            Show();
    }

    /// <summary>Open the pause menu.</summary>
    public void Show()
    {
        ResolveReferences();
        WireButtons();

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseOverlay != null)
        {
            pauseOverlay.transform.SetAsLastSibling();
            pauseOverlay.SetActive(true);
        }

        UpdateStatusText();

        if (animateOpen)
            StartCoroutine(AnimateOpen());
    }

    // ── Button Callbacks ───────────────────────────────

    void OnResume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
    }

    void OnSettings()
    {
        var settings = FindBestSettingsPanel();
        if (settings != null)
            settings.Toggle(true);
        else
            Debug.LogWarning("[PauseMenuUI] Settings button clicked, but no SettingsPanel exists in the scene.");
    }

    void OnQuit()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        // Return to main menu
        if (GameMode.IsPractice)
            PracticeGameManager.Instance?.ReturnToMenu();
        else
            NetworkManager.Instance?.ReturnToMenu();
    }

    // ── Status Text ────────────────────────────────────

    void UpdateStatusText()
    {
        if (statusText == null) return;

        // Scores are mirrored on the HUD; derive the round number from them.
        var uim = UIManager.Instance;
        string p1 = uim?.player1ScoreText?.text ?? "0";
        string p2 = uim?.player2ScoreText?.text ?? "0";
        int.TryParse(p1, out int s1);
        int.TryParse(p2, out int s2);
        int scoreToWin = GameMode.IsPractice
            ? (PracticeGameManager.Instance != null ? PracticeGameManager.Instance.scoreToWin : 5)
            : (GameManager.Instance != null ? GameManager.Instance.scoreToWin : 5);
        int round = s1 + s2 + 1;
        statusText.text = $"Round {round} / {scoreToWin} · Score {p1} — {p2} · Tap Resume to continue";
    }

    // ── Animation ──────────────────────────────────────

    System.Collections.IEnumerator AnimateOpen()
    {
        if (modalCanvasGroup == null || modalRect == null) yield break;

        modalCanvasGroup.alpha = 0f;
        modalRect.localScale = Vector3.one * 0.85f;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            // Ease-out cubic
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            modalCanvasGroup.alpha = ease;
            modalRect.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, ease);
            yield return null;
        }

        modalCanvasGroup.alpha = 1f;
        modalRect.localScale = Vector3.one;
    }

    void ResolveReferences()
    {
        if (pauseOverlay == null)
        {
            Transform found = FindDeep(transform, "PauseOverlay");
            if (found != null)
                pauseOverlay = found.gameObject;
        }

        if (pauseOverlay != null)
        {
            if (dimBackground == null)
                dimBackground = FindDeep(pauseOverlay.transform, "Dim")?.GetComponent<Image>();

            if (modalCanvasGroup == null)
                modalCanvasGroup = FindDeep(pauseOverlay.transform, "Modal")?.GetComponent<CanvasGroup>();

            if (resumeButton == null)
                resumeButton = FindDeep(pauseOverlay.transform, "ResumeBtn")?.GetComponent<Button>()
                    ?? FindDeep(pauseOverlay.transform, "ResumeButton")?.GetComponent<Button>();
            if (settingsButton == null)
                settingsButton = FindDeep(pauseOverlay.transform, "SettingsBtn")?.GetComponent<Button>()
                    ?? FindDeep(pauseOverlay.transform, "SettingsButton")?.GetComponent<Button>();
            if (quitButton == null)
                quitButton = FindDeep(pauseOverlay.transform, "QuitBtn")?.GetComponent<Button>()
                    ?? FindDeep(pauseOverlay.transform, "QuitButton")?.GetComponent<Button>();

            if (statusText == null)
                statusText = FindDeep(pauseOverlay.transform, "StatusText")?.GetComponent<TextMeshProUGUI>();
        }

        if (modalCanvasGroup != null && modalRect == null)
            modalRect = modalCanvasGroup.GetComponent<RectTransform>();
    }

    void WireButtons()
    {
        ResolveReferences();
        UnwireButtons();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResume);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettings);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);

        buttonsWired = true;
    }

    void UnwireButtons()
    {
        if (!buttonsWired) return;

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnResume);
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettings);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuit);

        buttonsWired = false;
    }

    SettingsPanel FindBestSettingsPanel()
    {
        SettingsPanel[] panels = FindObjectsOfType<SettingsPanel>(true);
        if (panels == null || panels.Length == 0)
            return null;

        Canvas ownCanvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>(true);
        SettingsPanel fallback = null;

        foreach (SettingsPanel settings in panels)
        {
            if (settings == null)
                continue;

            if (fallback == null)
                fallback = settings;

            Canvas settingsCanvas = settings.GetComponent<Canvas>() ?? settings.GetComponentInParent<Canvas>(true);
            if (ownCanvas != null && settingsCanvas == ownCanvas)
                return settings;
        }

        return fallback ?? panels[0];
    }

    Transform FindDeep(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
