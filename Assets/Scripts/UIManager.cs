using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StickArcher.Progression;
using System.Collections.Generic;

/// <summary>
/// Manages all in-game UI state — score updates, health bars, charge meter,
/// panel visibility, and button callbacks.
///
/// SETUP: In the Unity Editor, build the GameArena HUD visually:
///   1. Scoreboard panel (top center) with P1Score and P2Score TextMeshPro texts
///   2. Two health bar Images (P1 left, P2 right) — fill bars whose anchorMax.x is adjusted
///   3. Wind indicator text
///   4. Charge meter Slider
///   5. GameHUDPanel, ResultPanel, OpponentLeftPanel containers
///   6. Drag-assign all references in the Inspector below
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    private static Button wiredPauseButton;
    private static Sprite defeatResultBackgroundSprite;
    private static Sprite victoryResultBackgroundSprite;
    private static Sprite victorySpotlightSprite;
    private static Sprite defeatCardGradientSprite;
    private static Sprite resultBtnPrimaryGradientSprite;
    private static Sprite resultBtnSuccessGradientSprite;
    private static readonly Dictionary<int, Sprite> capsuleSpriteCache = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, Sprite> roundedRectCache = new Dictionary<int, Sprite>();

    [Header("Panels (assign in Inspector)")]
    public GameObject mainMenuPanel;
    public GameObject lobbyPanel;
    public GameObject gameHUDPanel;
    public GameObject resultPanel;
    public GameObject opponentLeftPanel;

    [Header("HUD - Scores (assign in Inspector)")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;

    [Header("HUD - Score Badges (design 05, optional)")]
    [Tooltip("Gold badge text showing P1 score on the player HUD panel")]
    public TextMeshProUGUI player1ScoreBadge;
    [Tooltip("Gold badge text showing P2 score on the player HUD panel")]
    public TextMeshProUGUI player2ScoreBadge;

    [Header("HUD - Player Names (design 05, optional)")]
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player2NameText;

    [Header("HUD - Round Indicator (design 05, optional)")]
    [Tooltip("Text showing current round, e.g. '3 / 5'")]
    public TextMeshProUGUI roundNumberText;

    [Header("HUD - Charge Meter (assign in Inspector)")]
    public Slider chargeMeter;

    [Header("HUD - Health Bars (assign in Inspector)")]
    [Tooltip("The fill Image for Player 1's health bar.")]
    public Image player1HealthBar;
    [Tooltip("The fill Image for Player 2's health bar.")]
    public Image player2HealthBar;
    [Tooltip("Optional: Text overlay on P1 health bar, e.g. '73 / 100'")]
    public TextMeshProUGUI player1HealthText;
    [Tooltip("Optional: Text overlay on P2 health bar, e.g. '73 / 100'")]
    public TextMeshProUGUI player2HealthText;

    [Header("HUD - Wind (assign in Inspector)")]
    [Tooltip("Text that displays wind direction and strength")]
    public TextMeshProUGUI windText;

    [Header("HUD - Pause (design 06)")]
    [Tooltip("Button that opens the pause menu")]
    public Button pauseButton;

    [Header("HP Heart Indicators (legacy, optional)")]
    public Image[] player1Hearts;
    public Image[] player2Hearts;

    [Header("Lobby")]
    public TextMeshProUGUI lobbyStatusText;

    [Header("Result (design 08/09)")]
    public TextMeshProUGUI resultTitleText;
    [Tooltip("Final score text on results screen, e.g. '5 — 2'")]
    public TextMeshProUGUI resultScoreText;
    [Tooltip("Optional: confetti/particle parent to enable on victory")]
    public GameObject victoryEffects;
    [Tooltip("Background Image on the result panel — tinted differently for win/lose")]
    public Image resultBackground;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        WireButtons();
        WirePauseButton();

        // If we're in the GameArena scene, show the HUD
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameArena")
        {
            ShowGameHUD();
        }
    }

    void OnDestroy()
    {
        if (wiredPauseButton != null && wiredPauseButton == pauseButton)
        {
            wiredPauseButton.onClick.RemoveListener(OnPauseButtonPressed);
            wiredPauseButton = null;
        }
    }

    // ── Score ────────────────────────────────────────────────────

    public void UpdateScore(int p1Score, int p2Score)
    {
        if (player1ScoreText != null) player1ScoreText.text = p1Score.ToString();
        if (player2ScoreText != null) player2ScoreText.text = p2Score.ToString();

        // Also update HUD score badges (design 05)
        if (player1ScoreBadge != null) player1ScoreBadge.text = p1Score.ToString();
        if (player2ScoreBadge != null) player2ScoreBadge.text = p2Score.ToString();
    }

    /// <summary>Update the round indicator text (design 05).</summary>
    public void UpdateRound(int currentRound, int totalRounds)
    {
        if (roundNumberText != null)
            roundNumberText.text = $"{currentRound} / {totalRounds}";
    }

    /// <summary>Update the wind indicator text. Pushed by WindSystem each round.</summary>
    public void UpdateWind(float windForce)
    {
        if (windText == null) return;
        string direction = windForce > 0 ? "→" : "←";
        Transform arrow = windText.transform.parent != null
            ? windText.transform.parent.Find("WindArrow")
            : null;
        TextMeshProUGUI arrowText = arrow != null ? arrow.GetComponent<TextMeshProUGUI>() : null;

        if (arrowText != null)
        {
            arrowText.text = direction;
            windText.text = $"{Mathf.Abs(windForce):F1}";
        }
        else
        {
            windText.text = $"{direction} {Mathf.Abs(windForce):F1}";
        }
    }

    // ── Charge Meter ────────────────────────────────────────────

    public void UpdateChargeMeter(float value)
    {
        if (chargeMeter != null)
        {
            chargeMeter.value = value;

            // Tint the slider fill using design system: green → yellow → red
            var fill = chargeMeter.fillRect != null ? chargeMeter.fillRect.GetComponent<Image>() : null;
            if (fill != null)
            {
                ChargeMeterUI chargeUI = fill.GetComponent<ChargeMeterUI>();
                if (chargeUI != null)
                    chargeUI.SetCharge(value);
                else
                    fill.color = UIDesignSystem.GetChargeColor(value);
            }
        }
    }

    // ── Health ───────────────────────────────────────────────────

    /// <summary>Update percentage health bar (0-maxHealth).</summary>
    public void SetPlayerHealth(int playerIndex, float health, float maxHealth)
    {
        Image bar = playerIndex == 1 ? player1HealthBar : player2HealthBar;
        if (bar != null)
            UpdateBar(bar, health, maxHealth);

        // Update health text overlay (design 05)
        TextMeshProUGUI healthText = playerIndex == 1 ? player1HealthText : player2HealthText;
        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(health)} / 100";

        // Also keep legacy hearts in sync
        if (maxHealth > 0)
            SetPlayerHP(playerIndex, Mathf.RoundToInt(health / (maxHealth / 3f)), 3);
    }

    void UpdateBar(Image bar, float health, float maxHealth)
    {
        if (bar == null) return;
        float pct = maxHealth > 0 ? health / maxHealth : 0f;
        bar.rectTransform.anchorMax = new Vector2(pct, 1f);

        // Gradient fill (design 05): green gradient when healthy, red gradient when low,
        // with a warm amber tint through the mid band. Falls back to a flat colour if the
        // gradient sprites aren't present.
        Sprite full = UIArtProvider.HpFull;
        Sprite low  = UIArtProvider.HpLow;
        if (full != null && low != null)
        {
            bar.type = Image.Type.Simple;
            if (pct <= 0.33f)
            {
                bar.sprite = low;
                bar.color = Color.white;
            }
            else
            {
                bar.sprite = full;
                bar.color = pct > 0.6f
                    ? Color.white
                    : Color.Lerp(new Color(1f, 0.72f, 0.38f, 1f), Color.white, (pct - 0.33f) / 0.27f);
            }
        }
        else
        {
            bar.color = UIDesignSystem.GetHealthColor(pct);
        }
    }

    /// <summary>Set the HP heart indicators for a player (legacy).</summary>
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

    // ── Panel Management ────────────────────────────────────────

    public void ShowMainMenu()    => SetPanel(mainMenuPanel);

    /// <summary>Call right before starting online matchmaking so the runtime lobby is
    /// allowed to show again after a previous cancel.</summary>
    public void BeginLobby() => _lobbyDismissed = false;

    public void ShowLobby(string statusMessage)
    {
        // If the scene has no authored lobby panel, build one at runtime (design 04)
        // on its own dedicated canvas so it sits above (and independent of) the menu.
        if (lobbyPanel == null)
        {
            // Once cancelled, stay dismissed until an explicit new Play Online
            // (BeginLobby) — so stray late matchmaking callbacks can't resurrect it.
            if (_lobbyDismissed) return;

            BuildRuntimeLobbyPanel();
            if (_runtimeLobbyPanel != null) _runtimeLobbyPanel.SetActive(true);
            if (_runtimeLobbyStatus != null) _runtimeLobbyStatus.text = statusMessage;
            return;
        }

        SetPanel(lobbyPanel);
        if (lobbyStatusText != null) lobbyStatusText.text = statusMessage;
    }
    public void ShowGameHUD()     => SetPanel(gameHUDPanel);

    public void ShowResult(bool localPlayerWon)
    {
        string p1 = player1ScoreText != null ? player1ScoreText.text : "0";
        string p2 = player2ScoreText != null ? player2ScoreText.text : "0";

        BuildRuntimeResultPanel(localPlayerWon);
        if (resultPanel == null) return;

        resultPanel.SetActive(true);
        if (gameHUDPanel != null) gameHUDPanel.SetActive(false);

        if (resultTitleText != null)
        {
            resultTitleText.text = localPlayerWon ? "VICTORY!" : "DEFEAT";
            ApplyResultTitleStyle(resultTitleText, localPlayerWon);
        }

        // Score text (design 08/09)
        if (resultScoreText != null)
        {
            resultScoreText.text = localPlayerWon
                ? $"{p1} <color=#FFFFFF66>—</color> {p2}"
                : $"{p1} <color=#FFFFFF4D>—</color> <color=#F23F3F>{p2}</color>";
        }

        // Victory effects (confetti, spotlight)
        if (victoryEffects != null)
            victoryEffects.SetActive(localPlayerWon);
    }

    // ── Runtime Lobby (design 04 — built when no authored lobby panel exists) ──
    GameObject _runtimeLobbyPanel;
    TextMeshProUGUI _runtimeLobbyStatus;
    // Static so a cancel on one UIManager instance is honored by any other instance
    // that the network layer might call ShowLobby on (avoids the lobby resurrecting).
    static bool _lobbyDismissed;

    static readonly string[] LobbyCharNames = { "ADVENTURER", "SOLDIER" };
    static readonly string[] LobbyCharArt   = { "Characters/Player1/archer_idle", "Characters/Player2/archer_idle" };

    void BuildRuntimeLobbyPanel()
    {
        if (_runtimeLobbyPanel != null) return;

        // Dedicated overlay canvas so the lobby is independent of the menu canvas
        // (which other code may disable) and always renders on top.
        var canvasGO = new GameObject("RuntimeLobbyCanvas",
            typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("RuntimeLobbyPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        Stretch(panel.GetComponent<RectTransform>());

        // Background gradient
        var bg = panel.AddComponent<Image>();
        bg.sprite = UIArtProvider.BgSkyMenu;
        bg.color = bg.sprite != null ? Color.white : Hex("#0F1A38");

        // Watermark
        var wm = CreateRuntimeText(panel.transform, "Watermark", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1700, 260));
        wm.text = "STICK ARCHER";
        wm.fontSize = 150f; wm.alignment = TextAlignmentOptions.Center;
        wm.color = new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, 0.06f);
        UIFontProvider.Apply(wm, UIFontProvider.Black);

        // Title
        var title = CreateRuntimeText(panel.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 380), new Vector2(1400, 90));
        title.text = "FINDING OPPONENT";
        title.fontSize = 60f; title.alignment = TextAlignmentOptions.Center; title.characterSpacing = 6f;
        title.color = Color.white;
        UIFontProvider.Apply(title, UIFontProvider.ExtraBold);

        // Status text
        _runtimeLobbyStatus = CreateRuntimeText(panel.transform, "Status", new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1000, 50));
        _runtimeLobbyStatus.text = "Connecting...";
        _runtimeLobbyStatus.fontSize = 30f; _runtimeLobbyStatus.alignment = TextAlignmentOptions.Center;
        _runtimeLobbyStatus.color = new Color(1, 1, 1, 0.7f);
        UIFontProvider.Apply(_runtimeLobbyStatus, UIFontProvider.Medium);

        // Player cards
        int you = Mathf.Clamp(CharacterSelectUI.SelectedCharacter, 0, LobbyCharNames.Length - 1);
        BuildLobbyCard(panel.transform, new Vector2(-380, -40), true, you);
        BuildLobbyCard(panel.transform, new Vector2(380, -40), false, (you + 1) % LobbyCharNames.Length);

        // VS circle
        var vs = CreateRuntimeImage(panel.transform, "VsCircle", new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(150, 150),
            Hex("#141A29", 0.96f), false);
        UIArtProvider.ApplySliced(vs, UIArtProvider.Circle128);
        var vsBorder = CreateRuntimeImage(vs.transform, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150),
            new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, 0.5f), false);
        Stretch(vsBorder.rectTransform);
        UIArtProvider.ApplySliced(vsBorder, UIArtProvider.Circle128);
        var vsTxt = CreateRuntimeText(vs.transform, "VS", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 90));
        vsTxt.text = "VS"; vsTxt.fontSize = 56f; vsTxt.alignment = TextAlignmentOptions.Center;
        vsTxt.color = UIDesignSystem.Gold;
        UIFontProvider.Apply(vsTxt, UIFontProvider.Black);

        // Cancel Search button (red pill)
        var cancelGO = new GameObject("CancelButton", typeof(RectTransform));
        cancelGO.transform.SetParent(panel.transform, false);
        SetRect(cancelGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -430), new Vector2(520, 120));
        var cancelImg = cancelGO.AddComponent<Image>();
        cancelImg.sprite = UIArtProvider.BtnDanger;
        cancelImg.color = cancelImg.sprite != null ? Color.white : UIDesignSystem.Danger;
        cancelImg.type = Image.Type.Sliced;
        var cancelBtn = cancelGO.AddComponent<Button>();
        cancelBtn.targetGraphic = cancelImg;
        cancelBtn.onClick.AddListener(OnCancelSearch);
        var cancelTxt = CreateRuntimeText(cancelGO.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 120));
        cancelTxt.text = "CANCEL SEARCH"; cancelTxt.fontSize = 34f; cancelTxt.alignment = TextAlignmentOptions.Center;
        cancelTxt.color = Color.white; cancelTxt.characterSpacing = 2f;
        UIFontProvider.Apply(cancelTxt, UIFontProvider.Bold);

        _runtimeLobbyPanel = panel;
    }

    void BuildLobbyCard(Transform parent, Vector2 pos, bool isYou, int charIndex)
    {
        var card = CreateRuntimeImage(parent, isYou ? "YouCard" : "OpponentCard",
            new Vector2(0.5f, 0.5f), pos, new Vector2(560, 640),
            Hex(isYou ? "#1B2034" : "#14182A", 0.94f), false);
        UIArtProvider.ApplySliced(card, UIArtProvider.Rounded32);

        // Border (gold for YOU, faint for opponent)
        var border = CreateRuntimeImage(card.transform, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 640),
            isYou ? new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, 0.9f) : new Color(1, 1, 1, 0.08f), false);
        Stretch(border.rectTransform);
        UIArtProvider.ApplySliced(border, UIArtProvider.Rounded32);
        // inner fill on top of the border to leave a rim
        var inner = CreateRuntimeImage(card.transform, "Inner", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            Hex(isYou ? "#1B2034" : "#14182A", 0.98f), false);
        var irt = inner.rectTransform; irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(5, 5); irt.offsetMax = new Vector2(-5, -5);
        UIArtProvider.ApplySliced(inner, UIArtProvider.Rounded32);

        // Character art (or silhouette for opponent)
        var art = CreateRuntimeImage(inner.transform, "Art", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(360, 360),
            Color.white, false);
        Sprite charSprite = Resources.Load<Sprite>(LobbyCharArt[charIndex]);
        if (charSprite != null)
        {
            art.sprite = charSprite;
            art.preserveAspect = true;
            art.color = isYou ? Color.white : new Color(0, 0, 0, 0.55f); // opponent = silhouette
        }
        else art.color = new Color(1, 1, 1, 0.1f);

        // Name
        var name = CreateRuntimeText(inner.transform, "Name", new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(520, 70));
        name.text = isYou ? LobbyCharNames[charIndex] : "???";
        name.fontSize = 46f; name.alignment = TextAlignmentOptions.Center;
        name.color = isYou ? Color.white : new Color(1, 1, 1, 0.5f);
        UIFontProvider.Apply(name, UIFontProvider.ExtraBold);

        // Label (YOU / OPPONENT)
        var label = CreateRuntimeText(inner.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(520, 44));
        label.text = isYou ? "YOU" : "OPPONENT";
        label.fontSize = 24f; label.alignment = TextAlignmentOptions.Center; label.characterSpacing = 6f;
        label.color = isYou ? UIDesignSystem.Gold : new Color(1, 1, 1, 0.4f);
        UIFontProvider.Apply(label, UIFontProvider.Bold);

        // Gold check badge on YOUR card
        if (isYou)
        {
            var badge = CreateRuntimeImage(card.transform, "CheckBadge", new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(72, 72),
                UIDesignSystem.Gold, false);
            UIArtProvider.ApplySliced(badge, UIArtProvider.Circle128);
            if (UIArtProvider.IconCheck != null)
            {
                var chk = CreateRuntimeImage(badge.transform, "Check", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44),
                    Hex("#141A29"), false);
                chk.sprite = UIArtProvider.IconCheck; chk.type = Image.Type.Simple;
            }
        }
    }

    void OnCancelSearch()
    {
        Debug.Log("[UIManager] Cancel search pressed.");
        _lobbyDismissed = true;
        _runtimeLobbyPanel = null;
        _runtimeLobbyStatus = null;

        // Bulletproof teardown: destroy every runtime lobby canvas in one pass,
        // independent of any cached field state.
        foreach (var c in FindObjectsOfType<Canvas>(true))
            if (c != null && c.gameObject.name == "RuntimeLobbyCanvas")
                Destroy(c.gameObject);

        try { NetworkManager.Instance?.ReturnToMenu(); }
        catch (System.Exception ex) { Debug.LogWarning("[UIManager] Cancel network teardown: " + ex.Message); }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    void ResolveResultPanel()
    {
        if (resultPanel != null) return;

        GameObject found = GameObject.Find("ResultPanel");
        if (found != null)
            resultPanel = found;
    }

    void BuildRuntimeResultPanel(bool localPlayerWon)
    {
        Canvas canvas = GetComponent<Canvas>() ?? FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[UIManager] Cannot show result: no Canvas found.");
            return;
        }

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child != null && child.name.StartsWith("ResultPanel_Runtime"))
                child.gameObject.SetActive(false);
        }

        if (resultPanel != null)
        {
            try { resultPanel.SetActive(false); }
            catch (MissingReferenceException) { resultPanel = null; }
        }

        Transform legacyPanel = canvas.transform.Find("ResultPanel");
        if (legacyPanel != null)
            legacyPanel.gameObject.SetActive(false);

        GameObject panel = new GameObject("ResultPanel_Runtime");
        panel.transform.SetParent(canvas.transform, false);
        Stretch(panel.AddComponent<RectTransform>());
        panel.transform.SetAsLastSibling();

        Image background = panel.AddComponent<Image>();
        if (localPlayerWon)
        {
            background.sprite = GetVictoryResultBackgroundSprite();
            background.type = Image.Type.Simple;
            background.color = Color.white;
        }
        else
        {
            background.sprite = GetDefeatResultBackgroundSprite();
            background.type = Image.Type.Simple;
            background.color = Color.white;
        }
        background.raycastTarget = true;

        GameObject glowGO = new GameObject("ResultGlow");
        glowGO.transform.SetParent(panel.transform, false);
        Image glow = glowGO.AddComponent<Image>();
        if (localPlayerWon)
        {
            SetRect(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1800f, 1000f));
            glow.sprite = GetVictorySpotlightSprite();
            glow.type = Image.Type.Simple;
            glow.color = Color.white;
            glowGO.SetActive(true);
        }
        else
        {
            Stretch(glow.rectTransform);
            glow.color = Color.clear;
            glowGO.SetActive(false);
        }
        glow.raycastTarget = false;

        if (localPlayerWon)
        {
            GameObject fxGO = new GameObject("VictoryEffects");
            fxGO.transform.SetParent(panel.transform, false);
            Stretch(fxGO.AddComponent<RectTransform>());
            fxGO.AddComponent<ConfettiBurst>();
            fxGO.SetActive(true);
            victoryEffects = fxGO;
        }
        else
        {
            victoryEffects = null;
        }

        TryBuildResultDecoration(panel.transform, localPlayerWon);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, localPlayerWon ? 280f : 220f),
            new Vector2(localPlayerWon ? 1400f : 1280f, localPlayerWon ? 240f : 240f));
        title.fontSize = localPlayerWon ? 220f : 200f;
        title.alignment = TextAlignmentOptions.Center;
        title.characterSpacing = 14f;
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Overflow;
        title.raycastTarget = false;
        ApplyResultTitleStyle(title, localPlayerWon);

        if (localPlayerWon)
            TryBuildVictoryStars(panel.transform);

        resultScoreText = BuildResultScoreCard(panel.transform, localPlayerWon);

        // Show the end-of-match progression card (coins earned + level/XP) on victory.
        if (localPlayerWon)
            TryBuildRewardsCard(panel.transform, localPlayerWon);

        float buttonY = localPlayerWon ? -445f : -410f;
        Button rematch = CreateResultPrimaryButton(panel.transform, "RematchButton",
            new Vector2(-270f, buttonY), new Vector2(500f, 120f), "REMATCH", localPlayerWon, localPlayerWon);
        Button menu = CreateResultOutlineButton(panel.transform, "MenuButton",
            new Vector2(270f, buttonY), new Vector2(500f, 120f), "MAIN MENU", localPlayerWon, localPlayerWon);

        // Anchor the action buttons to the BOTTOM edge so they're never clipped on wide /
        // width-matched device aspect ratios (where the canvas is short vertically).
        AnchorToBottom(rematch.GetComponent<RectTransform>(), new Vector2(-270f, 120f));
        AnchorToBottom(menu.GetComponent<RectTransform>(),    new Vector2( 270f, 120f));

        resultPanel = panel;
        resultBackground = background;
        resultTitleText = title;

        rematch.onClick.RemoveAllListeners();
        rematch.onClick.AddListener(OnRematchPressed);
        menu.onClick.RemoveAllListeners();
        menu.onClick.AddListener(OnMenuPressed);
    }

    void TryBuildResultDecoration(Transform parent, bool localPlayerWon)
    {
        try
        {
            if (!localPlayerWon)
                BuildResultRain(parent, localPlayerWon);
            BuildBrokenArrow(parent, localPlayerWon);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[UIManager] Result decoration skipped: " + ex.Message);
        }
    }

    void TryBuildVictoryStars(Transform parent)
    {
        try { BuildVictoryStars(parent); }
        catch (System.Exception ex)
        { Debug.LogWarning("[UIManager] Victory stars skipped: " + ex.Message); }
    }

    void BuildVictoryStars(Transform parent)
    {
        GameObject ornamentsGO = new GameObject("VictoryOrnaments");
        ornamentsGO.transform.SetParent(parent, false);
        SetRect(ornamentsGO.AddComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(480f, 36f));

        float[] xPositions = { -220f, -160f, 160f, 220f };
        float[] alphas = { 1f, 0.7f, 0.7f, 1f };
        float[] heights = { 32f, 24f, 24f, 32f };

        for (int i = 0; i < xPositions.Length; i++)
            CreateVictoryDiamond(ornamentsGO.transform, xPositions[i], heights[i], alphas[i]);
    }

    void CreateVictoryDiamond(Transform parent, float x, float height, float alpha)
    {
        const float width = 20f;
        GameObject go = new GameObject("Diamond");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        SetRect(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(width, height));
        img.color = new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, alpha);
        img.raycastTarget = false;
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    void AddResultButtonIcon(Transform buttonRoot, Sprite icon, Color color)
    {
        if (icon == null) return;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(buttonRoot, false);
        RectTransform rt = iconGO.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(84f, 0f);
        rt.sizeDelta = new Vector2(40f, 40f);

        Image img = iconGO.AddComponent<Image>();
        img.sprite = icon;
        img.type = Image.Type.Simple;
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    void TryBuildRewardsCard(Transform parent, bool localPlayerWon)
    {
        try
        {
            BuildRewardsCard(parent, localPlayerWon);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[UIManager] Result rewards card skipped: " + ex.Message);
        }
    }

    /// <summary>Builds the end-of-match progression card: coins earned + level/XP.
    /// Reads the reward snapshot from <see cref="ProfileManager"/>; no-ops if absent.</summary>
    void BuildRewardsCard(Transform parent, bool localPlayerWon)
    {
        var pm = ProfileManager.Instance;
        if (pm == null || pm.Profile == null) return;
        var profile = pm.Profile;

        Image card = CreateRuntimeImage(parent, "RewardsCard",
            new Vector2(0.5f, 0.5f), new Vector2(0f, -290f), new Vector2(1040f, 130f),
            localPlayerWon ? Hex("#1B2034", 0.94f) : Hex("#15101A", 0.94f), false);
        UIArtProvider.ApplySliced(card, UIArtProvider.Rounded24);

        Image cardBorder = CreateRuntimeImage(card.transform, "Border",
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.06f), false);
        Stretch(cardBorder.rectTransform);
        UIArtProvider.ApplySliced(cardBorder, UIArtProvider.Rounded24);

        // ── Coin icon (left of coins value) ──
        Image coinIcon = CreateRuntimeImage(card.transform, "CoinIcon",
            new Vector2(0.5f, 0.5f), new Vector2(-438f, 18f), new Vector2(40f, 40f),
            UIDesignSystem.Gold, false);
        if (UIArtProvider.IconCoin != null) { coinIcon.sprite = UIArtProvider.IconCoin; coinIcon.color = UIDesignSystem.Gold; }

        // ── Coins (left) ──
        TextMeshProUGUI coinsValue = CreateRuntimeText(card.transform, "CoinsValue",
            new Vector2(0.5f, 0.5f), new Vector2(-230f, 18f), new Vector2(420f, 72f));
        coinsValue.text = $"+{pm.LastRewardCoins}";
        coinsValue.fontSize = 58f;
        coinsValue.fontStyle = FontStyles.Bold;
        coinsValue.alignment = TextAlignmentOptions.Center;
        coinsValue.color = UIDesignSystem.Gold;

        TextMeshProUGUI coinsLabel = CreateRuntimeText(card.transform, "CoinsLabel",
            new Vector2(0.5f, 0.5f), new Vector2(-230f, -36f), new Vector2(420f, 34f));
        coinsLabel.text = $"COINS   •   {profile.coins:N0} TOTAL";
        coinsLabel.fontSize = 23f;
        coinsLabel.fontStyle = FontStyles.Bold;
        coinsLabel.alignment = TextAlignmentOptions.Center;
        coinsLabel.characterSpacing = 4f;
        coinsLabel.color = new Color(1f, 1f, 1f, 0.55f);

        // ── Divider ──
        CreateRuntimeImage(card.transform, "Divider",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 84f), new Color(1f, 1f, 1f, 0.1f), false);

        // ── Level + XP (right) ──
        TextMeshProUGUI levelValue = CreateRuntimeText(card.transform, "LevelValue",
            new Vector2(0.5f, 0.5f), new Vector2(250f, 24f), new Vector2(460f, 50f));
        levelValue.text = pm.LeveledUpLastMatch ? $"LEVEL {profile.level}  ▲" : $"LEVEL {profile.level}";
        levelValue.fontSize = 38f;
        levelValue.fontStyle = FontStyles.Bold;
        levelValue.alignment = TextAlignmentOptions.Center;
        levelValue.characterSpacing = 4f;
        levelValue.color = pm.LeveledUpLastMatch ? UIDesignSystem.Gold : Color.white;

        // XP progress bar (background + left-anchored fill).
        Image barBg = CreateRuntimeImage(card.transform, "XpBarBG",
            new Vector2(0.5f, 0.5f), new Vector2(250f, -32f), new Vector2(380f, 16f),
            new Color(1f, 1f, 1f, 0.12f), false);

        int needed = pm.XpForNextLevel();
        float frac = needed > 0 ? Mathf.Clamp01((float)profile.xp / needed) : 0f;

        GameObject fillGO = new GameObject("XpFill");
        fillGO.transform.SetParent(barBg.transform, false);
        Image fill = fillGO.AddComponent<Image>();
        fill.rectTransform.anchorMin = new Vector2(0f, 0f);
        fill.rectTransform.anchorMax = new Vector2(frac, 1f);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;
        Sprite xpGrad = UIArtProvider.BtnGold;
        if (xpGrad != null) { fill.sprite = xpGrad; fill.color = Color.white; }
        else fill.color = UIDesignSystem.Gold;
        fill.raycastTarget = false;

        TextMeshProUGUI xpText = CreateRuntimeText(barBg.transform, "XpText",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(380f, 16f));
        xpText.text = $"{profile.xp} / {needed} XP";
        xpText.fontSize = 14f;
        xpText.fontStyle = FontStyles.Bold;
        xpText.alignment = TextAlignmentOptions.Center;
        xpText.color = new Color(1f, 1f, 1f, 0.85f);

        // A little juice on the coins value (reuses the existing pop helper).
        ButtonAnimator.PopText(coinsValue.rectTransform, 1.5f);
    }

    Image CreateRuntimeImage(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size, Color color, bool sliced)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        SetRect(image.rectTransform, anchor, position, size);
        image.color = color;
        image.raycastTarget = false;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    TextMeshProUGUI CreateRuntimeText(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        SetRect(text.rectTransform, anchor, position, size);
        text.raycastTarget = false;
        return text;
    }

    static void ApplyCapsuleShape(Image image, int width, int height)
    {
        if (image == null) return;
        Sprite shape = GetCapsuleSprite(width, height);
        if (shape == null) return;
        image.sprite = shape;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
    }

    static void ApplyCardShape(Image image)
    {
        Sprite shape = UIArtProvider.Rounded32 ?? UIArtProvider.Rounded24;
        UIArtProvider.ApplySliced(image, shape);
    }

    void ApplyResultTitleStyle(TextMeshProUGUI title, bool localPlayerWon)
    {
        if (title == null) return;

        UIFontProvider.Apply(title, UIFontProvider.Black);
        title.fontStyle = FontStyles.Normal;
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Overflow;

        Shadow legacyShadow = title.GetComponent<Shadow>();
        if (legacyShadow != null)
            Destroy(legacyShadow);

        if (localPlayerWon)
        {
            title.enableVertexGradient = true;
            title.color = Color.white;
            title.colorGradient = new VertexGradient(
                Hex("#FFF3A0"), Hex("#FFD933"),
                Hex("#FFD933"), Hex("#B98A0A"));
            title.outlineColor = Hex("#3A2200");
            title.outlineWidth = 0.24f;
            UIFontProvider.ApplyTitleDropShadow(title);
        }
        else
        {
            title.enableVertexGradient = true;
            title.color = Color.white;
            title.colorGradient = new VertexGradient(
                Hex("#E2E2EA"), Hex("#E2E2EA"),
                Hex("#5A5A6A"), Hex("#5A5A6A"));
            title.outlineColor = Hex("#1A0E1C");
            title.outlineWidth = 0.24f;
            UIFontProvider.ApplyTitleDropShadow(title);
        }
    }

    TextMeshProUGUI BuildResultScoreCard(Transform parent, bool localPlayerWon)
    {
        const float cardW = 1040f;
        float cardH = localPlayerWon ? 280f : 260f;
        const int radius = 32;
        const float stroke = 2f;
        Vector2 cardSize = new Vector2(cardW, cardH);
        float cardY = localPlayerWon ? -80f : -90f;

        GameObject cardWrap = new GameObject("ScoreCard");
        cardWrap.transform.SetParent(parent, false);
        SetRect(cardWrap.AddComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, cardY), cardSize);

        GameObject shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(cardWrap.transform, false);
        Image shadowImg = shadowGO.AddComponent<Image>();
        SetRect(shadowImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), cardSize);
        shadowImg.sprite = GetRoundedRectSprite(Mathf.RoundToInt(cardW), Mathf.RoundToInt(cardH), radius);
        shadowImg.type = Image.Type.Simple;
        shadowImg.color = new Color(0f, 0f, 0f, 0.25f);
        shadowImg.raycastTarget = false;

        GameObject strokeGO = new GameObject("Stroke");
        strokeGO.transform.SetParent(cardWrap.transform, false);
        Image strokeImg = strokeGO.AddComponent<Image>();
        Stretch(strokeImg.rectTransform);
        strokeImg.sprite = GetRoundedRectSprite(Mathf.RoundToInt(cardW), Mathf.RoundToInt(cardH), radius);
        strokeImg.type = Image.Type.Simple;
        strokeImg.color = localPlayerWon
            ? new Color(1f, 0.85f, 0.2f, 0.4f)
            : new Color(1f, 1f, 1f, 0.08f);
        strokeImg.raycastTarget = false;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(cardWrap.transform, false);
        Image fillImg = fillGO.AddComponent<Image>();
        RectTransform fillRT = fillImg.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(stroke, stroke);
        fillRT.offsetMax = new Vector2(-stroke, -stroke);
        int innerW = Mathf.RoundToInt(cardW - stroke * 2f);
        int innerH = Mathf.RoundToInt(cardH - stroke * 2f);
        int innerRadius = Mathf.Max(1, radius - Mathf.RoundToInt(stroke));
        fillImg.sprite = localPlayerWon
            ? GetRoundedGradientSprite(innerW, innerH, innerRadius, Hex("#2A3258"), Hex("#161B30"))
            : GetRoundedGradientSprite(innerW, innerH, innerRadius, Hex("#2A1E2E"), Hex("#16101B"));
        fillImg.type = Image.Type.Simple;
        fillImg.color = Color.white;
        fillImg.raycastTarget = false;

        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(cardWrap.transform, false);
        Stretch(contentGO.AddComponent<RectTransform>());

        VerticalLayoutGroup layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI label = CreateLayoutText(contentGO.transform, "Label", 32f);
        label.text = "FINAL SCORE";
        label.fontSize = 26f;
        label.characterSpacing = 10f;
        label.color = localPlayerWon ? UIDesignSystem.Gold : new Color(1f, 1f, 1f, 0.6f);
        UIFontProvider.Apply(label, UIFontProvider.Bold);

        float scoreRowHeight = localPlayerWon ? 120f : 110f;
        TextMeshProUGUI score = CreateLayoutText(contentGO.transform, "Score", scoreRowHeight);
        score.fontSize = localPlayerWon ? 120f : 110f;
        score.enableAutoSizing = false;
        score.color = new Color(1f, 1f, 1f, localPlayerWon ? 0.86f : 0.7f);
        score.richText = true;
        UIFontProvider.Apply(score, UIFontProvider.Black);

        return score;
    }

    TextMeshProUGUI CreateLayoutText(Transform parent, string objectName, float height)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);

        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        return text;
    }

    void AddButtonDropShadow(Transform parent, Vector2 size)
    {
        int w = Mathf.RoundToInt(size.x);
        int h = Mathf.RoundToInt(size.y);

        GameObject shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(parent, false);
        Image shadowImg = shadowGO.AddComponent<Image>();
        SetRect(shadowImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), size);
        ApplyCapsuleShape(shadowImg, w, h);
        shadowImg.color = new Color(0f, 0f, 0f, 0.35f);
        shadowImg.raycastTarget = false;
        shadowGO.transform.SetAsFirstSibling();
    }

    Button CreateResultPrimaryButton(Transform parent, string objectName, Vector2 position, Vector2 size,
        string labelText, bool useSuccessStyle, bool showIcon = false)
    {
        int w = Mathf.RoundToInt(size.x);
        int h = Mathf.RoundToInt(size.y);

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        SetRect(go.AddComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);
        AddButtonDropShadow(go.transform, size);

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(go.transform, false);
        Stretch(bgGO.AddComponent<RectTransform>());
        Image bg = bgGO.AddComponent<Image>();
        ApplyCapsuleShape(bg, w, h);
        bg.color = Color.white;
        bg.raycastTarget = true;
        Mask mask = bgGO.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject fillGO = new GameObject("Gradient");
        fillGO.transform.SetParent(bgGO.transform, false);
        Stretch(fillGO.AddComponent<RectTransform>());
        Image fill = fillGO.AddComponent<Image>();
        fill.sprite = useSuccessStyle ? GetResultBtnSuccessGradientSprite() : GetResultBtnPrimaryGradientSprite();
        fill.type = Image.Type.Simple;
        fill.color = Color.white;
        fill.raycastTarget = false;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = bg;

        TextMeshProUGUI label = CreateRuntimeText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Stretch(label.rectTransform);
        label.text = labelText;
        label.fontSize = 36f;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 3f;
        label.color = Color.white;
        UIFontProvider.Apply(label, UIFontProvider.ExtraBold);

        if (showIcon)
            AddResultButtonIcon(go.transform, UIArtProvider.IconPlay, Color.white);

        return button;
    }

    Button CreateResultOutlineButton(Transform parent, string objectName, Vector2 position, Vector2 size,
        string labelText, bool localPlayerWon, bool showIcon = false)
    {
        int w = Mathf.RoundToInt(size.x);
        int h = Mathf.RoundToInt(size.y);
        float inset = 3f;
        Color ringFill = localPlayerWon ? Hex("#0A0E1C") : Hex("#0A060C");

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        SetRect(go.AddComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);

        Image outer = go.AddComponent<Image>();
        ApplyCapsuleShape(outer, w, h);
        outer.color = localPlayerWon
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(1f, 1f, 1f, 0.55f);
        outer.raycastTarget = true;

        GameObject innerGO = new GameObject("Inner");
        innerGO.transform.SetParent(go.transform, false);
        Image inner = innerGO.AddComponent<Image>();
        RectTransform innerRT = inner.rectTransform;
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(inset, inset);
        innerRT.offsetMax = new Vector2(-inset, -inset);
        ApplyCapsuleShape(inner, w - Mathf.RoundToInt(inset * 2f), h - Mathf.RoundToInt(inset * 2f));
        inner.color = ringFill;
        inner.raycastTarget = false;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = outer;

        TextMeshProUGUI label = CreateRuntimeText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Stretch(label.rectTransform);
        label.text = labelText;
        label.fontSize = 32f;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 3f;
        label.color = localPlayerWon ? Color.white : new Color(1f, 1f, 1f, 0.7f);
        UIFontProvider.Apply(label, UIFontProvider.Bold);

        if (showIcon)
            AddResultButtonIcon(go.transform, UIArtProvider.IconHome, Color.white);

        return button;
    }

    Button CreateRuntimeButton(Transform parent, string objectName, Vector2 position, Vector2 size, string labelText, bool primary)
    {
        return primary
            ? CreateResultPrimaryButton(parent, objectName, position, size, labelText, false)
            : CreateResultOutlineButton(parent, objectName, position, size, labelText, false);
    }

    void CreateFallbackResultPanel(bool forceNew)
    {
        Canvas canvas = GetComponent<Canvas>() ?? FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("ResultPanel");
        if (existing != null && !forceNew)
        {
            resultPanel = existing.gameObject;
            return;
        }

        if (existing != null && forceNew)
            existing.gameObject.SetActive(false);

        GameObject panel = new GameObject(forceNew ? "ResultPanel_RuntimeFallback" : "ResultPanel");
        panel.transform.SetParent(canvas.transform, false);
        Stretch(panel.AddComponent<RectTransform>());
        Image background = panel.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1200f, 220f));
        title.fontSize = 160f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;

        GameObject scoreGO = new GameObject("Score");
        scoreGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI score = scoreGO.AddComponent<TextMeshProUGUI>();
        SetRect(score.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(800f, 140f));
        score.fontSize = 96f;
        score.fontStyle = FontStyles.Bold;
        score.alignment = TextAlignmentOptions.Center;

        Button rematch = FindOrCreateButton(panel.transform, "RematchButton");
        SetRect(EnsureRect(rematch.gameObject), new Vector2(0.5f, 0.5f), new Vector2(-270f, -320f), new Vector2(500f, 120f));
        StyleResultButton(rematch, "REMATCH", true);

        Button menu = FindOrCreateButton(panel.transform, "MenuButton");
        SetRect(EnsureRect(menu.gameObject), new Vector2(0.5f, 0.5f), new Vector2(270f, -320f), new Vector2(500f, 120f));
        StyleResultButton(menu, "MAIN MENU", false);

        resultPanel = panel;
        resultBackground = background;
        resultTitleText = title;
        resultScoreText = score;
        WireButtons();
    }

    void NormalizeResultPanel(bool localPlayerWon)
    {
        RectTransform panelRT = EnsureRect(resultPanel);
        Stretch(panelRT);
        resultPanel.transform.SetAsLastSibling();

        Image background = EnsureImage(resultPanel.transform, "Background");
        Stretch(background.rectTransform);
        background.sprite = localPlayerWon ? null : GetDefeatResultBackgroundSprite();
        background.color = localPlayerWon ? Hex("#0A0E1C") : Color.white;
        background.type = Image.Type.Simple;
        background.raycastTarget = true;
        resultBackground = background;

        Image glow = EnsureImage(resultPanel.transform, "DefeatGlow");
        Stretch(glow.rectTransform);
        if (localPlayerWon)
        {
            glow.color = new Color(0.08f, 0.13f, 0.28f, 0.84f);
            glow.gameObject.SetActive(true);
        }
        else
        {
            glow.color = Color.clear;
            glow.gameObject.SetActive(false);
        }
        glow.raycastTarget = false;
        glow.transform.SetSiblingIndex(Mathf.Min(glow.transform.GetSiblingIndex(), background.transform.GetSiblingIndex() + 1));

        BuildResultRain(resultPanel.transform, localPlayerWon);
        BuildBrokenArrow(resultPanel.transform, localPlayerWon);

        resultTitleText = EnsureText(resultPanel.transform, "Title", "ResultTitle");
        SetRect(resultTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1280f, 240f));
        resultTitleText.fontSize = 200f;
        resultTitleText.fontStyle = FontStyles.Bold;
        resultTitleText.alignment = TextAlignmentOptions.Center;
        resultTitleText.characterSpacing = 14f;
        resultTitleText.raycastTarget = false;
        resultTitleText.outlineColor = Hex("#1A0E1C");
        resultTitleText.outlineWidth = 0.18f;

        Image scoreCard = EnsureImage(resultPanel.transform, "ScoreCard");
        SetRect(scoreCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(1040f, 260f));
        scoreCard.color = localPlayerWon ? Hex("#1F2438", 0.96f) : Hex("#16101B", 0.96f);
        scoreCard.type = Image.Type.Sliced;
        scoreCard.raycastTarget = false;

        Image scoreCardTint = EnsureImage(scoreCard.transform, "TopTint");
        Stretch(scoreCardTint.rectTransform);
        scoreCardTint.color = localPlayerWon ? Hex("#252B45", 0.28f) : Hex("#2A1E2E", 0.32f);
        scoreCardTint.raycastTarget = false;

        Image scoreBorder = EnsureImage(scoreCard.transform, "Border");
        Stretch(scoreBorder.rectTransform);
        scoreBorder.color = new Color(1f, 1f, 1f, 0.08f);
        scoreBorder.type = Image.Type.Sliced;
        scoreBorder.raycastTarget = false;

        TextMeshProUGUI label = EnsureText(scoreCard.transform, "Label", "FinalScoreLabel");
        SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 54f), new Vector2(1040f, 44f));
        label.text = "FINAL SCORE";
        label.fontSize = 26f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 10f;
        label.color = new Color(1f, 1f, 1f, 0.6f);
        label.raycastTarget = false;

        resultScoreText = EnsureText(scoreCard.transform, "Score", "ResultScoreText");
        SetRect(resultScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(1040f, 132f));
        resultScoreText.fontSize = 110f;
        resultScoreText.fontStyle = FontStyles.Bold;
        resultScoreText.alignment = TextAlignmentOptions.Center;
        resultScoreText.characterSpacing = 0f;
        resultScoreText.color = new Color(1f, 1f, 1f, localPlayerWon ? 0.86f : 0.7f);
        resultScoreText.richText = true;
        resultScoreText.raycastTarget = false;

        Button rematch = FindOrCreateButton(resultPanel.transform, "RematchButton");
        SetRect(EnsureRect(rematch.gameObject), new Vector2(0.5f, 0.5f), new Vector2(-270f, -410f), new Vector2(500f, 120f));
        StyleResultButton(rematch, "REMATCH", true);

        Button menu = FindOrCreateButton(resultPanel.transform, "MenuButton");
        SetRect(EnsureRect(menu.gameObject), new Vector2(0.5f, 0.5f), new Vector2(270f, -410f), new Vector2(500f, 120f));
        StyleResultButton(menu, "MAIN MENU", false);

        WireButtons();
    }

    public void ShowOpponentLeft()
    {
        if (opponentLeftPanel != null) opponentLeftPanel.SetActive(true);
    }

    void BuildResultRain(Transform parent, bool localPlayerWon)
    {
        Transform rainRoot = parent.Find("RainLines");
        if (rainRoot == null)
        {
            GameObject rainGO = new GameObject("RainLines");
            rainGO.transform.SetParent(parent, false);
            rainRoot = rainGO.transform;
            rainGO.AddComponent<RectTransform>();
        }

        Stretch(EnsureRect(rainRoot.gameObject));
        Color color = localPlayerWon ? new Color(1f, 1f, 1f, 0.035f) : new Color(1f, 1f, 1f, 0.06f);
        float[] xPositions = { 200f, 420f, 640f, 860f, 1080f, 1300f, 1520f, 1740f };

        for (int i = 0; i < xPositions.Length; i++)
        {
            Image line = EnsureImage(rainRoot, "RainLine" + i);
            SetRect(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(xPositions[i] - 960f, 450f), new Vector2(1f, 220f));
            line.color = color;
            line.raycastTarget = false;
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -5.2f);
        }
    }

    void BuildBrokenArrow(Transform parent, bool localPlayerWon)
    {
        Transform arrowRoot = parent.Find("BrokenArrow");
        if (localPlayerWon)
        {
            if (arrowRoot != null)
                arrowRoot.gameObject.SetActive(false);
            return;
        }

        if (arrowRoot == null)
        {
            GameObject arrowGO = new GameObject("BrokenArrow");
            arrowGO.transform.SetParent(parent, false);
            arrowRoot = arrowGO.transform;
            arrowGO.AddComponent<RectTransform>();
        }

        SetRect(EnsureRect(arrowRoot.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(240f, 40f));
        arrowRoot.gameObject.SetActive(true);
        Color color = new Color(0.66f, 0.66f, 0.71f, 0.5f);

        Image left = EnsureImage(arrowRoot, "LeftShaft");
        SetRect(left.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(80f, 6f));
        left.color = color;
        left.raycastTarget = false;

        Image right = EnsureImage(arrowRoot, "RightShaft");
        SetRect(right.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(80f, 6f));
        right.color = color;
        right.raycastTarget = false;

        Image head = EnsureImage(arrowRoot, "Head");
        SetRect(head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(114f, 0f), new Vector2(22f, 16f));
        head.color = color;
        head.raycastTarget = false;

        Image crackLeft = EnsureImage(arrowRoot, "CrackLeft");
        SetRect(crackLeft.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-24f, 0f), new Vector2(2f, 28f));
        crackLeft.color = color;
        crackLeft.raycastTarget = false;
        crackLeft.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -35f);

        Image crackRight = EnsureImage(arrowRoot, "CrackRight");
        SetRect(crackRight.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(2f, 28f));
        crackRight.color = color;
        crackRight.raycastTarget = false;
        crackRight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);
    }

    void StyleResultButton(Button button, string labelText, bool primary, bool localPlayerWon = false)
    {
        if (button == null) return;

        RectTransform rt = EnsureRect(button.gameObject);
        Vector2 size = rt.sizeDelta;
        if (size.x < 1f || size.y < 1f)
            size = new Vector2(500f, 120f);

        int w = Mathf.RoundToInt(size.x);
        int h = Mathf.RoundToInt(size.y);

        Image buttonImage = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
        Transform inner = button.transform.Find("Inner");
        if (inner != null) Destroy(inner.gameObject);
        Transform bgChild = button.transform.Find("Background");
        if (bgChild != null) Destroy(bgChild.gameObject);
        Transform gradChild = button.transform.Find("Gradient");
        if (gradChild != null) Destroy(gradChild.gameObject);
        Transform shadow = button.transform.Find("Shadow");
        if (shadow == null && primary)
            AddButtonDropShadow(button.transform, size);

        Outline oldOutline = button.GetComponent<Outline>();
        if (oldOutline != null)
            Destroy(oldOutline);

        if (primary)
        {
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(button.transform, false);
            Stretch(bgGO.AddComponent<RectTransform>());
            Image bg = bgGO.AddComponent<Image>();
            ApplyCapsuleShape(bg, w, h);
            bg.color = Color.white;
            bg.raycastTarget = true;
            Mask mask = bgGO.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject fillGO = new GameObject("Gradient");
            fillGO.transform.SetParent(bgGO.transform, false);
            Stretch(fillGO.AddComponent<RectTransform>());
            Image fill = fillGO.AddComponent<Image>();
            fill.sprite = localPlayerWon && primary
                ? GetResultBtnSuccessGradientSprite()
                : GetResultBtnPrimaryGradientSprite();
            fill.type = Image.Type.Simple;
            fill.color = Color.white;
            fill.raycastTarget = false;

            buttonImage.enabled = false;
            button.targetGraphic = bg;
        }
        else
        {
            buttonImage.enabled = true;
            ApplyCapsuleShape(buttonImage, w, h);
            buttonImage.color = localPlayerWon
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.55f);
            buttonImage.raycastTarget = true;
            button.targetGraphic = buttonImage;

            GameObject innerGO = new GameObject("Inner");
            innerGO.transform.SetParent(button.transform, false);
            Image innerImg = innerGO.AddComponent<Image>();
            RectTransform innerRT = innerImg.rectTransform;
            innerRT.anchorMin = Vector2.zero;
            innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = new Vector2(3f, 3f);
            innerRT.offsetMax = new Vector2(-3f, -3f);
            ApplyCapsuleShape(innerImg, w - 6, h - 6);
            innerImg.color = localPlayerWon ? Hex("#0A0E1C") : Hex("#0A060C");
            innerImg.raycastTarget = false;
            innerGO.transform.SetAsFirstSibling();
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(button.transform, false);
            label = labelGO.AddComponent<TextMeshProUGUI>();
        }

        Stretch(label.rectTransform);
        label.text = labelText;
        label.fontSize = primary ? 36f : 32f;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 3f;
        label.color = primary ? Color.white : new Color(1f, 1f, 1f, 0.7f);
        UIFontProvider.Apply(label, primary ? UIFontProvider.ExtraBold : UIFontProvider.Bold);
        label.raycastTarget = false;

        Transform icon = button.transform.Find("Icon");
        if (icon != null)
            icon.gameObject.SetActive(false);
    }

    Button FindOrCreateButton(Transform parent, string objectName)
    {
        Transform found = parent.Find(objectName);
        if (found == null)
        {
            GameObject buttonGO = new GameObject(objectName);
            buttonGO.transform.SetParent(parent, false);
            found = buttonGO.transform;
            buttonGO.AddComponent<RectTransform>();
            buttonGO.AddComponent<Image>();
            buttonGO.AddComponent<Button>();
        }

        Button button = found.GetComponent<Button>();
        if (button == null)
            button = found.gameObject.AddComponent<Button>();

        return button;
    }

    Image EnsureImage(Transform parent, string objectName)
    {
        Transform found = parent.Find(objectName);
        if (found == null)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            found = go.transform;
            go.AddComponent<RectTransform>();
        }

        Image image = found.GetComponent<Image>();
        if (image == null)
            image = found.gameObject.AddComponent<Image>();

        return image;
    }

    TextMeshProUGUI EnsureText(Transform parent, string primaryName, string fallbackName)
    {
        Transform found = parent.Find(primaryName) ?? parent.Find(fallbackName);
        if (found == null)
        {
            GameObject go = new GameObject(primaryName);
            go.transform.SetParent(parent, false);
            found = go.transform;
            go.AddComponent<RectTransform>();
        }

        found.name = primaryName;
        TextMeshProUGUI text = found.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = found.gameObject.AddComponent<TextMeshProUGUI>();

        return text;
    }

    RectTransform EnsureRect(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
            rt = go.AddComponent<RectTransform>();
        return rt;
    }

    void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localRotation = Quaternion.identity;
    }

    void SetRect(RectTransform rt, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        if (rt == null) return;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>Re-anchor a rect to the bottom-centre of the screen (offset = up from bottom),
    /// so it stays on-screen regardless of canvas aspect/scaler.</summary>
    void AnchorToBottom(RectTransform rt, Vector2 offsetFromBottom)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offsetFromBottom;
    }

    Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        color.a = alpha;
        return color;
    }

    static bool IsInsideRoundedRect(float x, float y, float width, float height, float radius)
    {
        if (x < 0f || y < 0f || x >= width || y >= height)
            return false;

        if (x >= radius && x < width - radius)
            return true;
        if (y >= radius && y < height - radius)
            return true;

        float r = radius - 0.5f;
        if (x < radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= r;
        if (x >= width - radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, radius)) <= r;
        if (x < radius && y >= height - radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius)) <= r;
        return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, height - radius)) <= r;
    }

    static Sprite GetRoundedRectSprite(int width, int height, int radius)
    {
        int key = width * 1000000 + height * 1000 + radius;
        if (roundedRectCache.TryGetValue(key, out Sprite cached))
            return cached;

        radius = Mathf.Clamp(radius, 1, Mathf.Min(width, height) / 2);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside = IsInsideRoundedRect(x + 0.5f, y + 0.5f, width, height, radius);
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        roundedRectCache[key] = sprite;
        return sprite;
    }

    static Sprite GetRoundedGradientSprite(int width, int height, int radius, Color top, Color bottom)
    {
        int colorKey = Mathf.RoundToInt(top.r * 255f) << 24 | Mathf.RoundToInt(top.g * 255f) << 16 |
                       Mathf.RoundToInt(bottom.r * 255f) << 8 | Mathf.RoundToInt(bottom.g * 255f);
        int key = width * 100000000 + height * 100000 + radius * 1000 + (colorKey & 0xFFFFFF);
        if (roundedRectCache.TryGetValue(key, out Sprite cached))
            return cached;

        radius = Mathf.Clamp(radius, 1, Mathf.Min(width, height) / 2);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color rowColor = Color.Lerp(bottom, top, t);
            for (int x = 0; x < width; x++)
            {
                bool inside = IsInsideRoundedRect(x + 0.5f, y + 0.5f, width, height, radius);
                texture.SetPixel(x, y, inside ? rowColor : Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        roundedRectCache[key] = sprite;
        return sprite;
    }

    Sprite GetDefeatCardGradientSprite()
    {
        if (defeatCardGradientSprite != null)
            return defeatCardGradientSprite;
        defeatCardGradientSprite = GetVerticalGradientSprite(Hex("#2A1E2E"), Hex("#16101B"));
        return defeatCardGradientSprite;
    }

    Sprite GetResultBtnPrimaryGradientSprite()
    {
        if (resultBtnPrimaryGradientSprite != null)
            return resultBtnPrimaryGradientSprite;
        resultBtnPrimaryGradientSprite = GetVerticalGradientSprite(Hex("#4DA3FF"), Hex("#1F73D9"));
        return resultBtnPrimaryGradientSprite;
    }

    static Sprite GetVerticalGradientSprite(Color top, Color bottom)
    {
        const int width = 8;
        const int height = 64;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            Color color = Color.Lerp(bottom, top, y / (float)(height - 1));
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, color);
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite GetCapsuleSprite(int width, int height)
    {
        int key = width * 10000 + height;
        if (capsuleSpriteCache.TryGetValue(key, out Sprite cached))
            return cached;

        int radius = Mathf.Max(1, height / 2);
        int texWidth = Mathf.Max(width, radius * 2 + 2);
        int texHeight = height;
        Texture2D texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 leftCenter = new Vector2(radius, radius);
        Vector2 rightCenter = new Vector2(texWidth - radius, radius);
        float r = radius - 0.5f;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                bool inside = false;
                if (x >= radius && x < texWidth - radius)
                    inside = y >= 0 && y < texHeight;
                else if (x < radius)
                    inside = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), leftCenter) <= r;
                else
                    inside = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), rightCenter) <= r;

                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        Vector4 border = new Vector4(radius, radius, radius, radius);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texWidth, texHeight),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
        capsuleSpriteCache[key] = sprite;
        return sprite;
    }

    Sprite GetResultBtnSuccessGradientSprite()
    {
        if (resultBtnSuccessGradientSprite != null)
            return resultBtnSuccessGradientSprite;
        resultBtnSuccessGradientSprite = UIArtProvider.BtnSuccess != null
            ? UIArtProvider.BtnSuccess
            : GetVerticalGradientSprite(Hex("#5BD980"), Hex("#258F44"));
        return resultBtnSuccessGradientSprite;
    }

    Sprite GetVictoryResultBackgroundSprite()
    {
        if (victoryResultBackgroundSprite != null)
            return victoryResultBackgroundSprite;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color center = Hex("#3A2A6A");
        Color mid = Hex("#1A1A4A");
        Color edge = Hex("#0A0E1C");
        Vector2 gradientCenter = new Vector2(0.5f, 0.4f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                float distance = Vector2.Distance(uv, gradientCenter) / 0.6f;
                Color color = distance <= 0.6f
                    ? Color.Lerp(center, mid, Mathf.Clamp01(distance / 0.6f))
                    : Color.Lerp(mid, edge, Mathf.Clamp01((distance - 0.6f) / 0.4f));
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        victoryResultBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return victoryResultBackgroundSprite;
    }

    Sprite GetVictorySpotlightSprite()
    {
        if (victorySpotlightSprite != null)
            return victorySpotlightSprite;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color center = new Color(1f, 0.85f, 0.2f, 0.4f);
        Vector2 gradientCenter = new Vector2(0.5f, 0.55f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                float distance = Vector2.Distance(uv, gradientCenter) / 0.4f;
                Color color = Color.Lerp(center, Color.clear, Mathf.Clamp01(distance));
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        victorySpotlightSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return victorySpotlightSprite;
    }

    Sprite GetDefeatResultBackgroundSprite()
    {
        if (defeatResultBackgroundSprite != null)
            return defeatResultBackgroundSprite;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color center = Hex("#3A1A28");
        Color mid = Hex("#1A0E1C");
        Color edge = Hex("#0A060C");
        Vector2 gradientCenter = new Vector2(0.5f, 0.4f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                float distance = Vector2.Distance(uv, gradientCenter) / 0.7f;
                Color color = distance <= 0.6f
                    ? Color.Lerp(center, mid, Mathf.Clamp01(distance / 0.6f))
                    : Color.Lerp(mid, edge, Mathf.Clamp01((distance - 0.6f) / 0.4f));
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        defeatResultBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return defeatResultBackgroundSprite;
    }

    void SetPanel(GameObject target)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenuPanel == target);
        if (lobbyPanel != null) lobbyPanel.SetActive(lobbyPanel == target);
        if (gameHUDPanel != null) gameHUDPanel.SetActive(gameHUDPanel == target);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (opponentLeftPanel != null) opponentLeftPanel.SetActive(false);
        if (_runtimeLobbyPanel != null) _runtimeLobbyPanel.SetActive(false);
    }

    // ── Button Wiring ───────────────────────────────────────────

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

    void WirePauseButton()
    {
        // PauseButton only exists in the GameArena scene
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameArena") return;

        if (pauseButton == null)
        {
            var pauseButtonObject = GameObject.Find("PauseBtn") ?? GameObject.Find("PauseButton");
            if (pauseButtonObject != null)
                pauseButton = pauseButtonObject.GetComponent<Button>();
        }

        if (pauseButton == null)
        {
            Debug.LogWarning("[UIManager] PauseButton is not assigned and could not be found.");
            return;
        }

        if (wiredPauseButton != null && wiredPauseButton != pauseButton)
            wiredPauseButton.onClick.RemoveListener(OnPauseButtonPressed);

        pauseButton.onClick.RemoveListener(OnPauseButtonPressed);
        pauseButton.onClick.AddListener(OnPauseButtonPressed);
        wiredPauseButton = pauseButton;
    }

    // ── Button Callbacks ────────────────────────────────────────

    static void OnPauseButtonPressed()
    {
        var pause = PauseMenuUI.Instance;
        if (pause == null)
        {
            var pauseMenus = UnityEngine.Object.FindObjectsOfType<PauseMenuUI>(true);
            if (pauseMenus.Length > 0)
                pause = pauseMenus[0];
        }

        if (pause == null)
        {
            Debug.LogWarning("[UIManager] Pause button clicked, but no PauseMenuUI exists in the scene.");
            return;
        }

        pause.TogglePause();
    }

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
}
