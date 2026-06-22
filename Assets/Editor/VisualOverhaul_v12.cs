#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StickArcher.UI;

/// <summary>
/// v12 — Main Menu visual polish pass.
///   • Background: single composite menu_bg.png (1920×1080, matches design SVG)
///   • ProfileBadge baked under Safe (level + XP bar + coins)
///   • Title, buttons, gear, footers per designs/01_main_menu.svg
///
/// Menu: Tools / Design Sync / 4 – Polish MainMenu (v12)
/// </summary>
public static class VisualOverhaul_v12
{
    [MenuItem("Tools/Design Sync/4 – Polish MainMenu (v12)")]
    static void MenuPolishMainMenu() => PolishMainMenu();

    // ── Design tokens ──────────────────────────────────────────────
    static readonly Color Gold      = new Color(1.000f, 0.851f, 0.200f);   // #FFD933
    static readonly Color GoldTop   = new Color(1.000f, 0.953f, 0.627f);  // #FFF3A0
    static readonly Color GoldBottom= new Color(0.788f, 0.600f, 0.039f);  // #C9990A
    static readonly Color TextMid   = new Color(1f, 1f, 1f, 0.75f);
    static readonly Color TextDim   = new Color(1f, 1f, 1f, 0.50f);
    static readonly Color TextHi    = Color.white;
    static readonly Color BgDark    = new Color(0.078f, 0.102f, 0.161f);
    static readonly Color BgPanel   = new Color(0.122f, 0.141f, 0.220f, 0.96f);
    static readonly Color HudBg     = new Color(0.06f,  0.09f,  0.15f,  0.88f);

    // ── Sprite loaders ─────────────────────────────────────────────
    static Sprite WS        => Spr("Assets/Resources/_WhiteSquare.png");
    static Sprite Pill128   => Spr("Assets/Art/UI/Shapes/pill_128.png");
    static Sprite PillBar   => Spr("Assets/Art/UI/Shapes/pill_bar.png");
    static Sprite Circle128 => Spr("Assets/Art/UI/Shapes/circle_128.png");
    static Sprite Rounded32 => Spr("Assets/Art/UI/Shapes/rounded_32.png");
    static Sprite MenuBg    => Spr("Assets/Art/UI/Backgrounds/menu_bg.png");
    static Sprite GBtnP     => Spr("Assets/Art/UI/Gradients/btn_primary.png");
    static Sprite GBtnS     => Spr("Assets/Art/UI/Gradients/btn_success.png");
    static Sprite IcoGear   => Spr("Assets/Art/UI/Icons/gear.png");
    static Sprite IcoGlobe  => Spr("Assets/Art/UI/Icons/globe.png");
    static Sprite IcoRobot  => Spr("Assets/Art/UI/Icons/robot.png");

    static Sprite Spr(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[v12] Missing sprite: {path}");
        return s != null ? s : WS;
    }

    static Sprite SprCoin()
    {
        foreach (var path in new[] {
            "Assets/Art/UI/Icons/coin.png",
            "Assets/Resources/UI/Icons/coin.png" })
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
        }
        Debug.LogWarning("[v12] Missing coin sprite — using gold circle fallback");
        return Circle128;
    }

    static TMP_FontAsset TmpFont(string name) => InterFontSetup.Load(name);

    static void ApplyFont(TextMeshProUGUI tmp, TMP_FontAsset font)
    {
        if (font == null) return;
        tmp.font = font;
        tmp.fontStyle = FontStyles.Normal;
        tmp.fontWeight = FontWeight.Regular;
    }

    /// <summary>SVG titleShadow — soft black fade (high underlay softness, low dilate).</summary>
    static void ApplyTitleDropShadow(TextMeshProUGUI tmp)
    {
        var mat = Object.Instantiate(tmp.fontSharedMaterial);
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.55f));
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.85f);
        tmp.fontMaterial = mat;
    }

    // ══════════════════════════════════════════════════════════════
    //  POLISH MAIN MENU
    // ══════════════════════════════════════════════════════════════
    static void PolishMainMenu()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[v12] Stop Play mode first."); return; }
        InterFontSetup.EnsureAll();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        EnsureSingleton<NetworkManager>("NetworkManager");
        EnsureSingleton<AudioManager>("AudioManager");
        EnsureEventSystem();

        // Wipe old canvas and rebuild fresh (guarantees clean hierarchy)
        foreach (var c in Object.FindObjectsOfType<Canvas>())
            Object.DestroyImmediate(c.gameObject);

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = BgDark;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }

        // ── Canvas ──────────────────────────────────────────────────
        var canvasGO = new GameObject("MainMenuCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cscaler = canvasGO.AddComponent<CanvasScaler>();
        cscaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cscaler.referenceResolution = new Vector2(1920, 1080);
        cscaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var root = canvasGO.GetComponent<RectTransform>();

        // ── BACKGROUND — single composite PNG (sky + stars + mountains + ground) ──
        var bg = MakeChild(root, "BG");
        Stretch(bg);
        var bgImg = SetImg(bg, MenuBg, Color.white, Image.Type.Simple);
        bgImg.raycastTarget = false;

        // ── Safe ────────────────────────────────────────────────────
        // SafeAreaFitter is NOT added here — it fires Awake() immediately in Unity 2022
        // editor scripts, which shrinks the safe container's anchors based on the device
        // safe area and pushes children off-screen. Add it at runtime via the prefab instead.
        var safe = MakeChild(root, "Safe");
        Stretch(safe);

        // ── PROFILE BADGE — top-left level / XP / coins (see designs/01_main_menu.svg) ──
        BuildProfileBadge(safe);

        // ── LOGO EMBLEM — bow-and-arrow brand mark above the title ──
        // Generated by Tools ▸ Branding ▸ Generate Icon + Logo (BrandingSetup.cs).
        // Skipped gracefully if the asset hasn't been generated yet.
        var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/logo.png");
        if (logoSprite != null)
        {
            var logoRT = MakeChild(safe, "LogoEmblem");
            logoRT.anchorMin = logoRT.anchorMax = new Vector2(0.5f, 1f);
            logoRT.pivot = new Vector2(0.5f, 1f);
            logoRT.anchoredPosition = new Vector2(0, -6);
            logoRT.sizeDelta = new Vector2(84, 84);
            var logoImg = SetImg(logoRT, logoSprite, Color.white, Image.Type.Simple);
            logoImg.raycastTarget = false;
            logoImg.preserveAspect = true;
        }

        // ── TITLE — SVG: 160pt / weight 900, letter-spacing 8, titleShadow filter ──
        var titleRT = MakeChild(safe, "Title");
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -95);
        titleRT.sizeDelta = new Vector2(1600, 240);

        var titleTMP = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        titleTMP.text       = "STICK ARCHER";
        titleTMP.fontSize   = 152;
        ApplyFont(titleTMP, TmpFont("Inter ExtraBold SDF"));
        titleTMP.alignment  = TextAlignmentOptions.Center;
        titleTMP.color      = Gold;
        titleTMP.characterSpacing = 8f;
        titleTMP.enableVertexGradient = true;
        titleTMP.colorGradient = new VertexGradient(GoldTop, GoldTop, GoldBottom, GoldBottom);
        titleTMP.outlineWidth = 0f;
        ApplyTitleDropShadow(titleTMP);

        var bob = titleRT.gameObject.AddComponent<Bob>();
        bob.amplitude = 12f;
        bob.speed     = 1.2f;

        // ── SUBTITLE — top-center anchor, tracked letters ───────────
        // Spec: anchor (.5,1) pos (0,-322) size (1200,50) font 28 letter-spacing 12
        var subRT = MakeChild(safe, "Subtitle");
        subRT.anchorMin = subRT.anchorMax = new Vector2(0.5f, 1f);
        subRT.pivot = new Vector2(0.5f, 1f);
        subRT.anchoredPosition = new Vector2(0, -340);
        subRT.sizeDelta = new Vector2(1200, 50);

        var subTMP = subRT.gameObject.AddComponent<TextMeshProUGUI>();
        subTMP.text             = "BATTLE OF THE BOWS";
        subTMP.fontSize         = 28;
        ApplyFont(subTMP, TmpFont("Inter Medium SDF"));
        subTMP.alignment        = TextAlignmentOptions.Center;
        subTMP.color            = new Color(1f, 1f, 1f, 0.7f);
        subTMP.characterSpacing = 12f;

        // ── ORNAMENT — below subtitle (SVG y=370; TMP subtitle top-anchored at -340 needs extra gap)
        var ornGold = new Color(Gold.r, Gold.g, Gold.b, 0.7f);
        var ornRT = MakeChild(safe, "SubtitleOrnament");
        ornRT.anchorMin = ornRT.anchorMax = new Vector2(0.5f, 1f);
        ornRT.pivot = new Vector2(0.5f, 0.5f);
        ornRT.anchoredPosition = new Vector2(0, -398);
        ornRT.sizeDelta = new Vector2(400, 20);

        // SVG: line -200→-40 and 40→200, stroke-width 2
        var ornLineL = MakeChild(ornRT, "LineL");
        ornLineL.anchorMin = ornLineL.anchorMax = new Vector2(0.5f, 0.5f);
        ornLineL.pivot = new Vector2(0.5f, 0.5f);
        ornLineL.anchoredPosition = new Vector2(-120f, 0f);
        ornLineL.sizeDelta = new Vector2(160f, 2f);
        SetImg(ornLineL, WS, ornGold).raycastTarget = false;

        var ornLineR = MakeChild(ornRT, "LineR");
        ornLineR.anchorMin = ornLineR.anchorMax = new Vector2(0.5f, 0.5f);
        ornLineR.pivot = new Vector2(0.5f, 0.5f);
        ornLineR.anchoredPosition = new Vector2(120f, 0f);
        ornLineR.sizeDelta = new Vector2(160f, 2f);
        SetImg(ornLineR, WS, ornGold).raycastTarget = false;

        AddFilledChevron(ornRT, "ChevronL", -24f, pointLeft: true);
        AddFilledChevron(ornRT, "ChevronR", 24f, pointLeft: false);

        var ornDot = MakeChild(ornRT, "Dot");
        ornDot.anchorMin = ornDot.anchorMax = new Vector2(0.5f, 0.5f);
        ornDot.pivot = new Vector2(0.5f, 0.5f);
        ornDot.anchoredPosition = Vector2.zero; ornDot.sizeDelta = new Vector2(12f, 12f);
        SetImg(ornDot, Circle128, ornGold).raycastTarget = false;

        // ── PLAY ONLINE / VS COMPUTER — matches designs/01_main_menu.svg ──
        const float menuBtnW = 640f;
        const float menuBtnH = 140f;
        var (onlineBtn, _) = PillBtn(safe, "PlayOnlineButton", "PLAY ONLINE",
            GBtnP, IcoGlobe, new Vector2(0, 0), new Vector2(menuBtnW, menuBtnH));

        var (aiBtn, _) = PillBtn(safe, "VsComputerButton", "VS COMPUTER",
            GBtnS, IcoRobot, new Vector2(0, -180), new Vector2(menuBtnW, menuBtnH));

        // ── GEAR button — top-right with drop shadow ─────────────────
        var gearWrap = MakeChild(safe, "GearButton");
        gearWrap.anchorMin = gearWrap.anchorMax = new Vector2(1f, 1f);
        gearWrap.pivot = new Vector2(1f, 1f);
        gearWrap.anchoredPosition = new Vector2(-60, -60);
        gearWrap.sizeDelta = new Vector2(96, 96);
        AddDropShadow(gearWrap, new Vector2(96, 96), Circle128, Image.Type.Sliced);

        var gearRT = MakeChild(gearWrap, "Btn");
        Stretch(gearRT);
        SetImg(gearRT, Circle128, HudBg, Image.Type.Sliced);
        var gearIconRT = MakeChild(gearRT, "Icon"); Stretch(gearIconRT);
        SetImg(gearIconRT, IcoGear, Color.white);
        var gearBtn = gearRT.gameObject.AddComponent<Button>();
        AddButtonAnim(gearRT.gameObject);

        // ── FOOTER texts ────────────────────────────────────────────
        var flRT = MakeChild(safe, "FooterLeft");
        flRT.anchorMin = flRT.anchorMax = Vector2.zero;
        flRT.pivot = Vector2.zero;
        flRT.anchoredPosition = new Vector2(40, 30);
        flRT.sizeDelta = new Vector2(420, 30);
        var flTMP = flRT.gameObject.AddComponent<TextMeshProUGUI>();
        flTMP.text = "v1.0.0 · Build 142"; flTMP.fontSize = 20;
        flTMP.color = TextDim; flTMP.alignment = TextAlignmentOptions.Left;

        var frRT = MakeChild(safe, "FooterRight");
        frRT.anchorMin = frRT.anchorMax = new Vector2(1f, 0f);
        frRT.pivot = new Vector2(1f, 0f);
        frRT.anchoredPosition = new Vector2(-40, 30);
        frRT.sizeDelta = new Vector2(420, 30);
        var frTMP = frRT.gameObject.AddComponent<TextMeshProUGUI>();
        frTMP.text = "© Stick Archer 2026"; frTMP.fontSize = 20;
        frTMP.color = TextDim; frTMP.alignment = TextAlignmentOptions.Right;

        // ── CharacterSelectPanel (inactive) ─────────────────────────
        var csPanel = MakeChild(root, "CharacterSelectPanel");
        Stretch(csPanel);
        csPanel.gameObject.SetActive(false);
        SetImg(csPanel, WS, new Color(0, 0, 0, 0.9f));

        // ── Wire MainMenuController ─────────────────────────────────
        var mmc = canvasGO.GetComponent<MainMenuController>() ?? canvasGO.AddComponent<MainMenuController>();
        mmc.playOnlineButton = onlineBtn;
        mmc.practiceButton   = aiBtn;
        EditorUtility.SetDirty(mmc);

        // ── Wire UIManager ──────────────────────────────────────────
        var uim = canvasGO.GetComponent<UIManager>() ?? canvasGO.AddComponent<UIManager>();
        uim.mainMenuPanel = canvasGO;
        EditorUtility.SetDirty(uim);

        // ── Wire SettingsPanel ──────────────────────────────────────
        var sp = canvasGO.GetComponent<SettingsPanel>();
        if (sp != null) { sp.openButton = gearBtn; EditorUtility.SetDirty(sp); }

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[v12] MainMenu polished successfully.");
    }

    // ── Profile badge — layout matches designs/01_main_menu.svg exactly ──
    static void BuildProfileBadge(RectTransform safe)
    {
        var root = MakeChild(safe, "ProfileBadge");
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = Vector2.zero;
        root.gameObject.AddComponent<ProfileBadge>();

        var pill = MakeChild(root, "ProfilePill");
        pill.anchorMin = pill.anchorMax = new Vector2(0f, 1f);
        pill.pivot = new Vector2(0f, 1f);
        pill.anchoredPosition = new Vector2(40f, -40f);
        pill.sizeDelta = new Vector2(440f, 104f);
        var pillBg = new Color(0.102f, 0.122f, 0.200f, 0.34f);
        SetImg(pill, Rounded32, pillBg, Image.Type.Sliced).raycastTarget = false;

        var border = MakeChild(pill, "Border");
        Stretch(border);
        SetImg(border, WS, new Color(1f, 1f, 1f, 0.08f)).raycastTarget = false;

        // Profile badge — LV + XP bar left; coin count block right (SVG: x=416, 24px inset)
        const float barWidth = 250f;

        var level = AddTMPFromTopLeft(pill, "Level", 28f, 8f, new Vector2(170f, 44f), new Vector2(0f, 1f));
        level.text = "LV 1";
        level.fontSize = 36f;
        ApplyFont(level, TmpFont("Inter ExtraBold SDF"));
        level.characterSpacing = 2f;
        level.color = Gold;
        level.alignment = TextAlignmentOptions.Left;

        var barBg = MakeChild(pill, "XpBarBG");
        barBg.anchorMin = barBg.anchorMax = new Vector2(0f, 1f);
        barBg.pivot = new Vector2(0f, 0.5f);
        barBg.anchoredPosition = new Vector2(28f, -62f);
        barBg.sizeDelta = new Vector2(barWidth, 12f);
        var barImg = SetImg(barBg, PillBar, new Color(1f, 1f, 1f, 0.14f), Image.Type.Sliced);
        barImg.raycastTarget = false;
        barImg.pixelsPerUnitMultiplier = 1f;

        var fill = MakeChild(barBg, "XpFill");
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = Vector2.zero;
        fill.sizeDelta = new Vector2(barWidth * 0.5f, 0f);
        var fillImg = SetImg(fill, PillBar, Gold, Image.Type.Sliced);
        fillImg.raycastTarget = false;
        fillImg.pixelsPerUnitMultiplier = 1f;

        // Coin icon + count + label grouped and right-aligned in the pill
        BuildCoinsBlock(pill);
    }

    static void BuildCoinsBlock(RectTransform pill)
    {
        const float rightPad = 28f; // designs/01_main_menu.svg — text ends at x=412 in 440px pill

        var block = MakeChild(pill, "CoinsBlock");
        block.anchorMin = block.anchorMax = new Vector2(1f, 0.5f);
        block.pivot = new Vector2(1f, 0.5f);
        block.anchoredPosition = new Vector2(-rightPad, -1f);
        block.sizeDelta = new Vector2(120f, 54f);

        var row = block.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleRight;
        row.spacing = 12f;
        row.childControlWidth = false;
        row.childControlHeight = false;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        var coinRT = MakeChild(block, "CoinIcon");
        coinRT.sizeDelta = new Vector2(22f, 22f);
        var coinLE = coinRT.gameObject.AddComponent<LayoutElement>();
        coinLE.preferredWidth = 22f;
        coinLE.preferredHeight = 22f;
        SetImg(coinRT, Circle128, Gold, Image.Type.Simple).raycastTarget = false;

        var col = MakeChild(block, "CoinsColumn");
        col.sizeDelta = new Vector2(78f, 54f);
        var colLE = col.gameObject.AddComponent<LayoutElement>();
        colLE.preferredWidth = 78f;
        colLE.preferredHeight = 54f;
        var vcol = col.gameObject.AddComponent<VerticalLayoutGroup>();
        vcol.childAlignment = TextAnchor.UpperRight;
        vcol.spacing = -2f;
        vcol.childControlWidth = false;
        vcol.childControlHeight = false;
        vcol.childForceExpandWidth = false;
        vcol.childForceExpandHeight = false;

        var coinsRT = MakeChild(col, "Coins");
        coinsRT.sizeDelta = new Vector2(78f, 32f);
        var coins = coinsRT.gameObject.AddComponent<TextMeshProUGUI>();
        coins.text = "0";
        coins.fontSize = 30f;
        ApplyFont(coins, TmpFont("Inter ExtraBold SDF"));
        coins.color = Color.white;
        coins.alignment = TextAlignmentOptions.Right;
        coins.raycastTarget = false;

        var coinsLabelRT = MakeChild(col, "CoinsLabel");
        coinsLabelRT.sizeDelta = new Vector2(78f, 22f);
        var coinsLabel = coinsLabelRT.gameObject.AddComponent<TextMeshProUGUI>();
        coinsLabel.text = "COINS";
        coinsLabel.fontSize = 15f;
        ApplyFont(coinsLabel, TmpFont("Inter Bold SDF"));
        coinsLabel.characterSpacing = 4f;
        coinsLabel.color = new Color(1f, 1f, 1f, 0.5f);
        coinsLabel.alignment = TextAlignmentOptions.Right;
        coinsLabel.raycastTarget = false;
    }

    static TextMeshProUGUI AddTMPFromTopLeft(RectTransform parent, string name,
        float xFromLeft, float yFromTop, Vector2 size, Vector2? pivot = null)
    {
        var p = pivot ?? new Vector2(0f, 1f);
        var rt = MakeChild(parent, name);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = p;
        rt.anchoredPosition = new Vector2(xFromLeft, -yFromTop);
        rt.sizeDelta = size;
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        return tmp;
    }

    static TextMeshProUGUI AddTMP(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = MakeChild(parent, name);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── Gradient Pill Button ────────────────────────────────────────
    // SVG (640×140): icon centre x=100, size ~56; label text-anchor=middle at x=340.
    static (Button btn, TextMeshProUGUI lbl) PillBtn(RectTransform parent,
        string name, string label, Sprite gradient, Sprite icon, Vector2 pos, Vector2 size)
    {
        const float refW = 640f;
        float sx = size.x / refW;

        var wrap = MakeChild(parent, name);
        wrap.anchorMin = wrap.anchorMax = new Vector2(0.5f, 0.5f);
        wrap.pivot = new Vector2(0.5f, 0.5f);
        wrap.anchoredPosition = pos;
        wrap.sizeDelta = size;
        AddDropShadow(wrap, size, Pill128, Image.Type.Sliced);

        var btnRT = MakeChild(wrap, "Btn");
        Stretch(btnRT);

        SetImg(btnRT, Pill128, Color.white, Image.Type.Sliced);
        btnRT.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        var btn = btnRT.gameObject.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        cb.pressedColor     = new Color(0.82f, 0.82f, 0.82f);
        btn.colors = cb;

        if (gradient != null)
        {
            var fill = MakeChild(btnRT, "Fill");
            Stretch(fill);
            SetImg(fill, gradient, Color.white, Image.Type.Sliced);
        }

        const float iconSize = 56f;
        const float iconX    = 100f;

        if (icon != null)
        {
            var iconRT = MakeChild(btnRT, "Icon");
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(iconX * sx, 0f);
            iconRT.sizeDelta = new Vector2(iconSize * sx, iconSize * sx);
            var iconImg = SetImg(iconRT, icon, Color.white, Image.Type.Simple);
            iconImg.preserveAspect = true;
        }

        var lblRT = MakeChild(btnRT, "Label");
        Stretch(lblRT);
        var lbl = lblRT.gameObject.AddComponent<TextMeshProUGUI>();
        lbl.text       = label;
        lbl.fontSize   = 44;
        ApplyFont(lbl, TmpFont("Inter ExtraBold SDF"));
        lbl.color      = Color.white;
        lbl.characterSpacing = 3f;
        lbl.alignment  = TextAlignmentOptions.Center;
        lbl.enableWordWrapping = false;
        lbl.raycastTarget = false;

        AddButtonAnim(btnRT.gameObject);
        return (btn, lbl);
    }

    /// <summary>SVG dropSoft: stdDeviation 8, dy 6, alpha ~50%.</summary>
    static void AddDropShadow(RectTransform parent, Vector2 size, Sprite shape, Image.Type type)
    {
        var shadowRT = MakeChild(parent, "DropShadow");
        shadowRT.anchorMin = shadowRT.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRT.pivot = new Vector2(0.5f, 0.5f);
        shadowRT.anchoredPosition = new Vector2(0f, -6f);
        shadowRT.sizeDelta = size;
        var shadow = SetImg(shadowRT, shape, new Color(0f, 0f, 0f, 0.35f), type);
        shadow.raycastTarget = false;
        shadowRT.SetAsFirstSibling();
    }

    /// <summary>SVG filled chevron polygons at ±24 (tip ±30, base ±18).</summary>
    static void AddFilledChevron(RectTransform parent, string name, float centerX, bool pointLeft)
    {
        var root = MakeChild(parent, name);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(centerX, 0f);
        root.sizeDelta = new Vector2(12f, 12f);

        var fill = new Color(Gold.r, Gold.g, Gold.b, 0.7f);
        float pivotX = pointLeft ? 1f : 0f;
        float tipOffsetX = pointLeft ? 6f : -6f;
        const float armLen = 13.5f;
        const float armAngle = 26.6f;

        var top = MakeChild(root, "ArmT");
        top.anchorMin = top.anchorMax = new Vector2(0.5f, 0.5f);
        top.pivot = new Vector2(pivotX, 0.5f);
        top.anchoredPosition = new Vector2(tipOffsetX, 3f);
        top.sizeDelta = new Vector2(armLen, 2f);
        top.localEulerAngles = new Vector3(0f, 0f, pointLeft ? armAngle : -armAngle);
        SetImg(top, WS, fill).raycastTarget = false;

        var bot = MakeChild(root, "ArmB");
        bot.anchorMin = bot.anchorMax = new Vector2(0.5f, 0.5f);
        bot.pivot = new Vector2(pivotX, 0.5f);
        bot.anchoredPosition = new Vector2(tipOffsetX, -3f);
        bot.sizeDelta = new Vector2(armLen, 2f);
        bot.localEulerAngles = new Vector3(0f, 0f, pointLeft ? -armAngle : armAngle);
        SetImg(bot, WS, fill).raycastTarget = false;
    }

    // ── Helpers ─────────────────────────────────────────────────────
    static RectTransform MakeChild(RectTransform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image SetImg(RectTransform rt, Sprite spr, Color col,
        Image.Type type = Image.Type.Simple)
    {
        var img = rt.gameObject.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
        img.sprite = spr; img.color = col; img.type = type;
        return img;
    }

    static void AddButtonAnim(GameObject go)
    {
        var t = System.Type.GetType("ButtonAnimator") ??
                System.Type.GetType("ButtonAnimator, Assembly-CSharp");
        if (t != null && go.GetComponent(t) == null)
            go.AddComponent(t);
    }

    static void EnsureSingleton<T>(string goName) where T : MonoBehaviour
    {
        if (Object.FindObjectOfType<T>() == null)
            new GameObject(goName).AddComponent<T>();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
#endif
