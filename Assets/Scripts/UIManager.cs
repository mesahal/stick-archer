using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all in-game UI — compact centered scoreboard matching the
/// Stick Archers Battle reference, thin health bars, wind indicator.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject lobbyPanel;
    public GameObject gameHUDPanel;
    public GameObject resultPanel;
    public GameObject opponentLeftPanel;

    [Header("HUD")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public Slider chargeMeter;

    [Header("HP Indicators (set by editor script)")]
    public Image[] player1Hearts;
    public Image[] player2Hearts;

    [Header("Lobby")]
    public TextMeshProUGUI lobbyStatusText;

    [Header("Result")]
    public TextMeshProUGUI resultTitleText;

    // Runtime health bars
    private Image _p1HealthBar;
    private Image _p2HealthBar;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        WireButtons();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameArena")
        {
            CleanOldUI();
            ShowGameHUD();
            BuildScoreboard();
            BuildHealthBars();
            BuildWindIndicator();
        }
    }

    /// <summary>
    /// Destroy any pre-existing UI panels placed in the scene editor that would
    /// overlap with our runtime-built scoreboard and health bars.
    /// </summary>
    void CleanOldUI()
    {
        // If gameHUDPanel has old children (P1Panel, P2Panel, etc.), destroy them
        if (gameHUDPanel != null)
        {
            for (int i = gameHUDPanel.transform.childCount - 1; i >= 0; i--)
            {
                var child = gameHUDPanel.transform.GetChild(i);
                string n = child.name;
                if (n.Contains("P1") || n.Contains("P2") || n.Contains("Score") ||
                    n.Contains("Health") || n.Contains("Wind") || n.Contains("Avatar") ||
                    n.Contains("VS") || n.Contains("Panel"))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // ── Compact Centered Scoreboard ──────────────────────────────
    void BuildScoreboard()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
        if (canvas == null) return;
        var root = canvas.GetComponent<RectTransform>();

        // Already built?
        if (GameObject.Find("Scoreboard") != null) return;

        // Dark pill background — top center
        var pill = new GameObject("Scoreboard");
        pill.transform.SetParent(root, false);
        var pillRt = pill.AddComponent<RectTransform>();
        pillRt.anchorMin = new Vector2(0.30f, 0.95f);
        pillRt.anchorMax = new Vector2(0.70f, 0.995f);
        pillRt.offsetMin = pillRt.offsetMax = Vector2.zero;
        var pillImg = pill.AddComponent<Image>();
        pillImg.color = new Color(0.08f, 0.10f, 0.18f, 0.80f);

        // P1 color block (left)
        var p1Block = CreateColorBlock(pillRt, "P1Block",
            new Vector2(0.02f, 0.10f), new Vector2(0.18f, 0.90f),
            new Color(0.20f, 0.60f, 0.95f));

        // P1 score
        player1ScoreText = CreateScoreLabel(pillRt, "P1Score",
            new Vector2(0.18f, 0f), new Vector2(0.42f, 1f));

        // VS / separator
        CreateSeparatorLabel(pillRt, "VS",
            new Vector2(0.42f, 0f), new Vector2(0.58f, 1f));

        // P2 score
        player2ScoreText = CreateScoreLabel(pillRt, "P2Score",
            new Vector2(0.58f, 0f), new Vector2(0.82f, 1f));

        // P2 color block (right)
        var p2Block = CreateColorBlock(pillRt, "P2Block",
            new Vector2(0.82f, 0.10f), new Vector2(0.98f, 0.90f),
            new Color(0.85f, 0.20f, 0.18f));
    }

    Image CreateColorBlock(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = col;
        return img;
    }

    TextMeshProUGUI CreateScoreLabel(RectTransform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "0";
        tmp.fontSize = 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    void CreateSeparatorLabel(RectTransform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "—";
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1, 1, 1, 0.6f);
    }

    // ── Thin Health Bars ─────────────────────────────────────────
    void BuildHealthBars()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
        if (canvas == null) return;
        var root = canvas.GetComponent<RectTransform>();

        _p1HealthBar = BuildHealthBar(root, 1);
        _p2HealthBar = BuildHealthBar(root, 2);
    }

    Image BuildHealthBar(RectTransform root, int playerIndex)
    {
        // Background
        var bg = new GameObject($"P{playerIndex}HealthBG");
        bg.transform.SetParent(root, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = playerIndex == 1 ? new Vector2(0.02f, 0.87f) : new Vector2(0.52f, 0.87f);
        bgRt.anchorMax = playerIndex == 1 ? new Vector2(0.48f, 0.90f) : new Vector2(0.98f, 0.90f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 0.6f);

        // Fill
        var fill = new GameObject($"P{playerIndex}HealthFill");
        fill.transform.SetParent(bgRt, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(1, 1);
        fillRt.offsetMax = new Vector2(-1, -1);
        fillRt.pivot = new Vector2(0, 0.5f);

        var fillImg = fill.AddComponent<Image>();
        fillImg.color = playerIndex == 1
            ? new Color(0.20f, 0.60f, 0.95f)
            : new Color(0.85f, 0.20f, 0.18f);

        // Label
        var label = new GameObject($"P{playerIndex}Label");
        label.transform.SetParent(bgRt, false);
        var labRt = label.AddComponent<RectTransform>();
        labRt.anchorMin = Vector2.zero; labRt.anchorMax = Vector2.one;
        labRt.offsetMin = labRt.offsetMax = Vector2.zero;
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = $"P{playerIndex}";
        tmp.fontSize = 12;
        tmp.alignment = playerIndex == 1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        tmp.color = new Color(1, 1, 1, 0.5f);
        tmp.margin = new Vector4(4, 0, 4, 0);

        return fillImg;
    }

    // ── Wind Indicator ───────────────────────────────────────────
    void BuildWindIndicator()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
        if (canvas == null) return;
        var root = canvas.GetComponent<RectTransform>();

        if (GameObject.Find("WindIndicator") != null) return;

        // Small pill — top center, just below scoreboard
        var pill = new GameObject("WindIndicator");
        pill.transform.SetParent(root, false);
        var pillRt = pill.AddComponent<RectTransform>();
        pillRt.anchorMin = new Vector2(0.35f, 0.81f);
        pillRt.anchorMax = new Vector2(0.65f, 0.85f);
        pillRt.offsetMin = pillRt.offsetMax = Vector2.zero;
        pill.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.15f, 0.50f);

        var textGO = new GameObject("WindText");
        textGO.transform.SetParent(pillRt, false);
        var textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 0);
        textRt.offsetMax = new Vector2(-4, 0);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Wind: 0";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.8f, 0.9f, 1f);

        var wind = FindObjectOfType<WindSystem>();
        if (wind != null)
            wind.windText = tmp;
    }

    // ── Button wiring ────────────────────────────────────────────
    void WireButtons()
    {
        if (resultPanel != null)
        {
            var rb = resultPanel.transform.Find("RematchButton")?.GetComponent<Button>();
            if (rb != null) { rb.onClick.RemoveAllListeners(); rb.onClick.AddListener(OnRematchPressed); }

            var mb = resultPanel.transform.Find("MenuButton")?.GetComponent<Button>();
            if (mb != null) { mb.onClick.RemoveAllListeners(); mb.onClick.AddListener(OnMenuPressed); }
        }
        if (opponentLeftPanel != null)
        {
            var bk = opponentLeftPanel.transform.Find("BackBtn")?.GetComponent<Button>();
            if (bk != null) { bk.onClick.RemoveAllListeners(); bk.onClick.AddListener(OnMenuPressed); }
        }
    }

    // ── Panel management ─────────────────────────────────────────
    public void ShowMainMenu()    => SetPanel(mainMenuPanel);
    public void ShowLobby(string statusMessage)
    {
        SetPanel(lobbyPanel);
        if (lobbyStatusText != null) lobbyStatusText.text = statusMessage;
    }
    public void ShowGameHUD()     => SetPanel(gameHUDPanel);

    public void ShowResult(bool localPlayerWon)
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);
        if (gameHUDPanel != null) gameHUDPanel.SetActive(false);
        if (resultTitleText != null)
            resultTitleText.text = localPlayerWon ? "You Win!" : "You Lose!";
    }

    public void ShowOpponentLeft()
    {
        if (opponentLeftPanel != null) opponentLeftPanel.SetActive(true);
    }

    public void UpdateScore(int p1Score, int p2Score)
    {
        if (player1ScoreText != null) player1ScoreText.text = p1Score.ToString();
        if (player2ScoreText != null) player2ScoreText.text = p2Score.ToString();
    }

    public void UpdateChargeMeter(float value)
    {
        if (chargeMeter != null) chargeMeter.value = value;
    }

    /// <summary>Set the HP heart indicators for a player (1 or 2). Legacy — kept for compatibility.</summary>
    public void SetPlayerHP(int playerIndex, int hp, int maxHp)
    {
        Image[] hearts = playerIndex == 1 ? player1Hearts : player2Hearts;
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            if (i < hp)
                hearts[i].color = new Color(1f, 0.25f, 0.30f, 1f);
            else
                hearts[i].color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
    }

    /// <summary>Update percentage health bar (0-maxHealth). Uses pre-built bars.</summary>
    public void SetPlayerHealth(int playerIndex, float health, float maxHealth)
    {
        Image bar = playerIndex == 1 ? _p1HealthBar : _p2HealthBar;
        if (bar != null)
            UpdateBar(bar, health, maxHealth);

        // Also keep legacy hearts in sync
        if (maxHealth > 0)
            SetPlayerHP(playerIndex, Mathf.RoundToInt(health / (maxHealth / 3f)), 3);
    }

    void UpdateBar(Image bar, float health, float maxHealth)
    {
        if (bar == null) return;
        float pct = maxHealth > 0 ? health / maxHealth : 0f;
        bar.rectTransform.anchorMax = new Vector2(pct, 1f);
        // Keep team color — no color change by health
    }

    // ── Button callbacks ─────────────────────────────────────────
    public void OnPlayButtonPressed()   => NetworkManager.Instance?.ConnectAndPlay();
    public void OnRematchPressed()
    {
        if (GameMode.IsPractice) PracticeGameManager.Instance?.ReturnToMenu();
        else                     NetworkManager.Instance?.ReturnToMenu();
    }
    public void OnMenuPressed()
    {
        if (GameMode.IsPractice) PracticeGameManager.Instance?.ReturnToMenu();
        else                     NetworkManager.Instance?.ReturnToMenu();
    }

    void SetPanel(GameObject target)
    {
        mainMenuPanel?.SetActive(mainMenuPanel == target);
        lobbyPanel?.SetActive(lobbyPanel == target);
        gameHUDPanel?.SetActive(gameHUDPanel == target);
        resultPanel?.SetActive(false);
        opponentLeftPanel?.SetActive(false);
    }
}
