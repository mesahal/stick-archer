using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Reusable error/warning modal dialog.
/// Can be used in any scene for connection errors, disconnects, etc.
///
/// SETUP: Build the error modal in the Unity Editor:
///   1. Create a full-screen dim overlay (black, alpha 0.7)
///   2. Add a center modal panel (600×400, rx=36)
///   3. Add warning icon, title text, body text, and an OK button
///   4. Drag-assign the references below
///   5. Call ErrorModalUI.Instance.Show("Title", "Message") from any script
/// </summary>
public class ErrorModalUI : MonoBehaviour
{
    public static ErrorModalUI Instance;

    [Header("Root")]
    [Tooltip("Full-screen overlay (dim + modal)")]
    public GameObject errorOverlay;

    [Header("Content")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Image warningIcon;

    [Header("Action")]
    public Button okButton;
    public Button retryButton;

    [Header("Animation")]
    public CanvasGroup modalCanvasGroup;
    public float openDuration = 0.2f;

    // ── Callbacks ──────────────────────────────────────
    private System.Action onOkCallback;
    private System.Action onRetryCallback;

    void Awake()
    {
        Instance = this;

        if (errorOverlay != null)
            errorOverlay.SetActive(false);
    }

    void Start()
    {
        if (okButton != null)
            okButton.onClick.AddListener(OnOk);
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);
    }

    // ── Public API ─────────────────────────────────────

    /// <summary>Show error modal with title and message.</summary>
    public void Show(string title, string body, System.Action onOk = null)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;

        onOkCallback = onOk;

        // Hide retry button for simple error messages
        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (errorOverlay != null)
            errorOverlay.SetActive(true);

        if (modalCanvasGroup != null)
            StartCoroutine(AnimateOpen());
    }

    /// <summary>Show error modal with retry option.</summary>
    public void ShowWithRetry(string title, string body,
                               System.Action onOk = null,
                               System.Action onRetry = null)
    {
        Show(title, body, onOk);
        onRetryCallback = onRetry;

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);
    }

    /// <summary>Show a connection error with retry.</summary>
    public void ShowConnectionError(string details = null)
    {
        string body = "Unable to connect to the server.";
        if (!string.IsNullOrEmpty(details))
            body += $"\n\n{details}";
        body += "\n\nPlease check your internet connection and try again.";

        ShowWithRetry("CONNECTION LOST", body,
            onOk: () =>
            {
                if (NetworkManager.Instance != null)
                    NetworkManager.Instance.ReturnToMenu();
                else
                    SceneManager.LoadScene("MainMenu");
            },
            onRetry: () => NetworkManager.Instance?.ConnectAndPlay()
        );
    }

    // ── Button Callbacks ───────────────────────────────

    void OnOk()
    {
        Hide();
        onOkCallback?.Invoke();
    }

    void OnRetry()
    {
        Hide();
        onRetryCallback?.Invoke();
    }

    void Hide()
    {
        if (errorOverlay != null)
            errorOverlay.SetActive(false);
    }

    // ── Animation ──────────────────────────────────────

    System.Collections.IEnumerator AnimateOpen()
    {
        if (modalCanvasGroup == null) yield break;

        var rect = modalCanvasGroup.GetComponent<RectTransform>();
        modalCanvasGroup.alpha = 0f;
        if (rect != null) rect.localScale = Vector3.one * 0.9f;

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            modalCanvasGroup.alpha = ease;
            if (rect != null)
                rect.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, ease);
            yield return null;
        }

        modalCanvasGroup.alpha = 1f;
        if (rect != null) rect.localScale = Vector3.one;
    }
}
