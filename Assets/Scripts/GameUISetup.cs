using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Creates overlay UI effects at runtime (round transitions, headshot feedback).
/// These are temporary full-screen overlays that make sense to create from code
/// since they don't need manual layout tuning.
///
/// Health bars, scoreboard, wind indicator — all built in the Editor now.
/// </summary>
public class GameUISetup : MonoBehaviour
{
    [Header("Overlay Effects")]
    public bool createRoundDisplay = true;
    public bool createHeadshotFeedback = true;

    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        if (canvas == null) return;
        ApplyHudDesignUpdates();
        StartCoroutine(ReapplyHudDesignAfterButtonAnimations());
        BuildChargeBarLabels();
        if (createRoundDisplay) SetupRoundDisplay();
        if (createHeadshotFeedback) SetupHeadshotFeedback();
#if UNITY_EDITOR
        if (GetComponent<DebugUIHarness>() == null) gameObject.AddComponent<DebugUIHarness>();
#endif
    }

    IEnumerator ReapplyHudDesignAfterButtonAnimations()
    {
        yield return null;
        ApplyHudDesignUpdates();
        yield return new WaitForSecondsRealtime(1.5f);
        ApplyHudDesignUpdates();
    }

    void ApplyHudDesignUpdates()
    {
        Transform hudPanel = FindDeep(canvas.transform, "GameHUDPanel");
        if (hudPanel == null) return;

        DestroyIfFound(hudPanel, "RoundBadge");
        DestroyIfFound(hudPanel, "RoundIndicator");

        Button pause = MovePauseButton(hudPanel);
        TextMeshProUGUI windText = NormalizeWindIndicator(hudPanel);
        TextMeshProUGUI p1Score = NormalizePlayerHud(hudPanel, true);
        TextMeshProUGUI p2Score = NormalizePlayerHud(hudPanel, false);
        NormalizeHealthBars(hudPanel);

        UIManager ui = canvas.GetComponent<UIManager>() ?? FindObjectOfType<UIManager>(true);
        if (ui == null) return;

        ui.roundNumberText = null;
        if (pause != null) ui.pauseButton = pause;
        if (windText != null) ui.windText = windText;
        if (p1Score != null)
        {
            ui.player1ScoreText = p1Score;
            ui.player1ScoreBadge = p1Score;
        }
        if (p2Score != null)
        {
            ui.player2ScoreText = p2Score;
            ui.player2ScoreBadge = p2Score;
        }
    }

    void BuildChargeBarLabels()
    {
        Transform chargeMeter = FindDeep(canvas.transform, "ChargeMeter");
        if (chargeMeter == null) return;

        // Power is charge-based again (hold longer = faster arrow), so show the meter so
        // the player can read their power as they press.
        chargeMeter.gameObject.SetActive(true);
        BuildChargeBarLabels_Legacy(chargeMeter);
    }

    // Retained (unused) so the old charge-meter styling can be restored if needed.
    void BuildChargeBarLabels_Legacy(Transform chargeMeter)
    {
        NormalizeChargeMeter(chargeMeter);

        TextMeshProUGUI releaseLabel = EnsureTextLabel(
            chargeMeter,
            "ReleaseToFireLabel",
            "RELEASE TO FIRE",
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 74f),
            new Vector2(360f, 24f),
            14f,
            Color.white,
            TextAlignmentOptions.Center);
        releaseLabel.characterSpacing = 6f;
        releaseLabel.gameObject.SetActive(false);

        TextMeshProUGUI maxLabel = EnsureTextLabel(
            chargeMeter,
            "MaxChargeLabel",
            "MAX!",
            new Vector2(1f, 0.5f),
            new Vector2(-84f, 8f),
            new Vector2(120f, 24f),
            14f,
            new Color(0.95f, 0.25f, 0.25f),
            TextAlignmentOptions.Right);
        maxLabel.characterSpacing = 2f;
        maxLabel.gameObject.SetActive(false);

        Image fillImage = null;
        Slider slider = chargeMeter.GetComponent<Slider>();
        if (slider != null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();

        if (fillImage == null)
            fillImage = FindDeep(chargeMeter, "Fill")?.GetComponent<Image>()
                ?? FindDeep(chargeMeter, "ChargeFill")?.GetComponent<Image>();

        if (fillImage == null) return;

        RectTransform fillRT = EnsureRect(fillImage.gameObject);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;

        ChargeMeterUI chargeUI = fillImage.GetComponent<ChargeMeterUI>()
            ?? fillImage.gameObject.AddComponent<ChargeMeterUI>();
        chargeUI.fillImage = fillImage;
        chargeUI.releaseToFireLabel = releaseLabel;
        chargeUI.maxChargeLabel = maxLabel;
        chargeUI.SetCharge(slider != null ? slider.value : 0f);
    }

    void NormalizeChargeMeter(Transform chargeMeter)
    {
        RectTransform rootRT = EnsureRect(chargeMeter.gameObject);
        rootRT.anchorMin = rootRT.anchorMax = new Vector2(0.5f, 0f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = new Vector2(0f, 40f);
        rootRT.sizeDelta = UIDesignSystem.ChargeMeterSize;

        Image rootImage = chargeMeter.GetComponent<Image>() ?? chargeMeter.gameObject.AddComponent<Image>();
        rootImage.color = new Color(0.12f, 0.14f, 0.22f, 0.96f);
        rootImage.raycastTarget = false;
        UIArtProvider.ApplySliced(rootImage, UIArtProvider.PillBar);

        Transform label = chargeMeter.Find("ChargeLabel") ?? chargeMeter.Find("Label");
        if (label != null)
        {
            label.name = "ChargeLabel";
            TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.text = "CHARGE";
                labelText.fontSize = 14f;
                labelText.fontStyle = FontStyles.Bold;
                labelText.alignment = TextAlignmentOptions.Left;
                labelText.color = new Color(1f, 1f, 1f, 0.55f);
                labelText.characterSpacing = 4f;
            }

            RectTransform labelRT = EnsureRect(label.gameObject);
            labelRT.anchorMin = labelRT.anchorMax = new Vector2(0.5f, 0.5f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);
            labelRT.anchoredPosition = new Vector2(-276f, 8f);
            labelRT.sizeDelta = new Vector2(200f, 24f);
        }

        Transform track = FindDeep(chargeMeter, "Background") ?? FindDeep(chargeMeter, "ChargeTrack");
        if (track != null)
        {
            track.name = "Background";
            RectTransform trackRT = EnsureRect(track.gameObject);
            trackRT.anchorMin = trackRT.anchorMax = new Vector2(0.5f, 0.5f);
            trackRT.pivot = new Vector2(0.5f, 0.5f);
            trackRT.anchoredPosition = new Vector2(0f, -16f);
            trackRT.sizeDelta = new Vector2(752f, 24f);

            Image trackImage = track.GetComponent<Image>() ?? track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.04f, 0.05f, 0.11f, 1f);
            trackImage.raycastTarget = false;
            UIArtProvider.ApplySliced(trackImage, UIArtProvider.PillBar);
        }

        Transform fillArea = FindDeep(chargeMeter, "Fill Area");
        if (fillArea != null)
        {
            RectTransform fillAreaRT = EnsureRect(fillArea.gameObject);
            fillAreaRT.anchorMin = fillAreaRT.anchorMax = new Vector2(0.5f, 0.5f);
            fillAreaRT.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRT.anchoredPosition = new Vector2(0f, -16f);
            fillAreaRT.sizeDelta = new Vector2(752f, 24f);
        }
    }

    TextMeshProUGUI EnsureTextLabel(Transform parent, string objectName, string text,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 size, float fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(objectName);
        GameObject labelGO = existing != null ? existing.gameObject : CreateChild(parent, objectName);
        RectTransform rt = EnsureRect(labelGO);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>()
            ?? labelGO.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    Button MovePauseButton(Transform hudPanel)
    {
        Transform pause = FindDeep(hudPanel, "PauseBtn") ?? FindDeep(hudPanel, "PauseButton");
        if (pause == null) return null;

        pause.name = "PauseBtn";
        RectTransform rt = EnsureRect(pause.gameObject);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -80f);
        rt.sizeDelta = new Vector2(96f, 96f);

        return pause.GetComponent<Button>() ?? pause.gameObject.AddComponent<Button>();
    }

    TextMeshProUGUI NormalizeWindIndicator(Transform hudPanel)
    {
        Transform wind = FindDeep(hudPanel, "WindBadge") ?? FindDeep(hudPanel, "WindIndicator");
        if (wind == null) return null;

        wind.name = "WindBadge";
        RectTransform rt = EnsureRect(wind.gameObject);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -170f);
        rt.sizeDelta = UIDesignSystem.WindBadgeSize;

        Image windImage = wind.GetComponent<Image>() ?? wind.gameObject.AddComponent<Image>();
        windImage.color = new Color(0.06f, 0.08f, 0.13f, 0.94f);
        windImage.raycastTarget = false;
        UIArtProvider.ApplySliced(windImage, UIArtProvider.Pill128);

        TextMeshProUGUI label = EnsureTextLabel(
            wind,
            "WindLabel",
            "WIND",
            new Vector2(0.5f, 0.5f),
            new Vector2(-46f, 0f),
            new Vector2(70f, 24f),
            14f,
            new Color(1f, 1f, 1f, 0.58f),
            TextAlignmentOptions.Center);
        label.characterSpacing = 2f;

        TextMeshProUGUI arrow = EnsureTextLabel(
            wind,
            "WindArrow",
            "→",
            new Vector2(0.5f, 0.5f),
            new Vector2(14f, 0f),
            new Vector2(28f, 24f),
            22f,
            UIDesignSystem.Gold,
            TextAlignmentOptions.Center);
        arrow.characterSpacing = 0f;

        TextMeshProUGUI value = EnsureTextLabel(
            wind,
            "WindValue",
            "0.0",
            new Vector2(0.5f, 0.5f),
            new Vector2(58f, 0f),
            new Vector2(60f, 24f),
            16f,
            UIDesignSystem.Success,
            TextAlignmentOptions.Center);
        value.characterSpacing = 0f;
        return value;
    }

    TextMeshProUGUI NormalizePlayerHud(Transform hudPanel, bool isPlayerOne)
    {
        Transform hud = FindPlayerHud(hudPanel, isPlayerOne);
        if (hud == null) return null;

        Transform circle = hud.Find("ScoreCircle") ?? hud.Find("Portrait");
        if (circle == null)
            circle = CreateChild(hud, "ScoreCircle").transform;
        circle.name = "ScoreCircle";

        Transform oldBadge = hud.Find("ScoreBadge");
        TextMeshProUGUI score = oldBadge != null
            ? oldBadge.GetComponentInChildren<TextMeshProUGUI>(true)
            : circle.GetComponentInChildren<TextMeshProUGUI>(true);

        for (int i = circle.childCount - 1; i >= 0; i--)
        {
            Transform child = circle.GetChild(i);
            if (score == null || child != score.transform)
                Destroy(child.gameObject);
        }

        if (score == null)
        {
            GameObject scoreGO = new GameObject("Score");
            scoreGO.transform.SetParent(circle, false);
            score = scoreGO.AddComponent<TextMeshProUGUI>();
            score.text = "0";
        }
        else
        {
            score.transform.SetParent(circle, false);
            score.name = "Score";
        }

        RectTransform circleRT = EnsureRect(circle.gameObject);
        circleRT.anchorMin = circleRT.anchorMax = new Vector2(0.5f, 0.5f);
        circleRT.pivot = new Vector2(0.5f, 0.5f);
        circleRT.anchoredPosition = new Vector2(isPlayerOne ? -210f : 210f, 0f);
        circleRT.sizeDelta = new Vector2(96f, 96f);

        Image circleImage = circle.GetComponent<Image>() ?? circle.gameObject.AddComponent<Image>();
        Sprite circleSpr = UIArtProvider.Circle128;
        if (circleSpr != null) { circleImage.sprite = circleSpr; circleImage.type = Image.Type.Simple; }
        circleImage.color = UIDesignSystem.Gold;
        circleImage.raycastTarget = false;

        RectTransform scoreRT = EnsureRect(score.gameObject);
        scoreRT.anchorMin = scoreRT.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRT.pivot = new Vector2(0.5f, 0.5f);
        scoreRT.anchoredPosition = Vector2.zero;
        scoreRT.sizeDelta = new Vector2(96f, 96f);
        score.fontSize = 42f;
        score.fontStyle = FontStyles.Bold;
        score.alignment = TextAlignmentOptions.Center;
        score.color = new Color(0.10f, 0.10f, 0.10f, 1f);
        score.raycastTarget = false;

        if (oldBadge != null)
            Destroy(oldBadge.gameObject);

        return score;
    }

    void NormalizeHealthBars(Transform hudPanel)
    {
        Sprite pillBarShape = UIArtProvider.PillBar;
        if (pillBarShape == null) return;

        string[] barNames = { "P1HealthBar", "P1HP", "Player1Health",
                               "P2HealthBar", "P2HP", "Player2Health" };
        foreach (string n in barNames)
        {
            Transform bar = FindDeep(hudPanel, n);
            if (bar == null) continue;

            // Style the bar container background
            Image bg = bar.GetComponent<Image>();
            if (bg == null) continue;
            if (bg.sprite == null) // only override if no sprite already set
            {
                bg.sprite = pillBarShape;
                bg.type = Image.Type.Sliced;
            }

            // Style any fill child
            Transform fill = bar.Find("Fill") ?? bar.Find("HealthFill") ?? bar.Find("HPFill");
            if (fill != null)
            {
                Image fillImg = fill.GetComponent<Image>();
                if (fillImg != null && fillImg.sprite == null)
                {
                    fillImg.sprite = pillBarShape;
                    fillImg.type = Image.Type.Sliced;
                }
            }
        }
    }

    Transform FindPlayerHud(Transform hudPanel, bool isPlayerOne)
    {
        string[] names = isPlayerOne
            ? new[] { "P1Hud", "Player1HUD", "Player1Hud" }
            : new[] { "P2Hud", "Player2HUD", "Player2Hud" };

        foreach (string hudName in names)
        {
            Transform found = FindDeep(hudPanel, hudName);
            if (found != null) return found;
        }

        return null;
    }

    void DestroyIfFound(Transform root, string objectName)
    {
        Transform target = FindDeep(root, objectName);
        if (target != null)
            Destroy(target.gameObject);
    }

    Transform FindDeep(Transform root, string objectName)
    {
        if (root.name == objectName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), objectName);
            if (found != null) return found;
        }
        return null;
    }

    RectTransform EnsureRect(GameObject target)
    {
        return target.GetComponent<RectTransform>() ?? target.AddComponent<RectTransform>();
    }

    GameObject CreateChild(Transform parent, string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(parent, false);
        child.AddComponent<RectTransform>();
        return child;
    }

    void SetupRoundDisplay()
    {
        if (GameObject.Find("RoundTransition") != null) return;

        GameObject roundObj = new GameObject("RoundTransition");
        roundObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = roundObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 300);

        CanvasGroup cg = roundObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Round text
        GameObject textObj = new GameObject("RoundText");
        textObj.transform.SetParent(roundObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(0, 50);
        textRt.offsetMax = new Vector2(0, 0);

        TextMeshProUGUI roundText = textObj.AddComponent<TextMeshProUGUI>();
        roundText.text = "ROUND 1";
        roundText.fontSize = 72;
        roundText.fontStyle = FontStyles.Bold;
        roundText.alignment = TextAlignmentOptions.Center;
        roundText.color = Color.white;

        // Arena name text
        GameObject arenaObj = new GameObject("ArenaName");
        arenaObj.transform.SetParent(roundObj.transform, false);
        RectTransform arenaRt = arenaObj.AddComponent<RectTransform>();
        arenaRt.anchorMin = Vector2.zero;
        arenaRt.anchorMax = new Vector2(1f, 0.5f);
        arenaRt.offsetMin = Vector2.zero;
        arenaRt.offsetMax = Vector2.zero;

        TextMeshProUGUI arenaText = arenaObj.AddComponent<TextMeshProUGUI>();
        arenaText.text = "";
        arenaText.fontSize = 36;
        arenaText.alignment = TextAlignmentOptions.Center;
        arenaText.color = new Color(1f, 0.8f, 0.2f);

        // Wire component
        RoundTransition transition = roundObj.AddComponent<RoundTransition>();
        transition.roundText = roundText;
        transition.arenaNameText = arenaText;
        transition.canvasGroup = cg;
    }

    void SetupHeadshotFeedback()
    {
        if (GameObject.Find("HeadshotFeedback") != null) return;

        GameObject hsObj = new GameObject("HeadshotFeedback");
        hsObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = hsObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1100, 220);

        CanvasGroup cg = hsObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        TextMeshProUGUI hsText = hsObj.AddComponent<TextMeshProUGUI>();
        hsText.text = "HEADSHOT!";
        hsText.fontSize = 96;
        hsText.fontStyle = FontStyles.Bold;
        hsText.alignment = TextAlignmentOptions.Center;
        hsText.color = new Color(1f, 0.2f, 0.1f);
        // Never wrap to a second line on narrow/scaled screens.
        hsText.enableWordWrapping = false;
        hsText.overflowMode = TextOverflowModes.Overflow;

        HeadshotFeedback feedback = hsObj.AddComponent<HeadshotFeedback>();
        feedback.headshotText = hsText;
        feedback.canvasGroup = cg;
    }
}
