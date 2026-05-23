#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// v10 — Full MainMenu rebuild: properly wires ALL buttons/fields so
///       both "Play Online" and "VS Computer" actually work on device.
///
///   • Clears and rebuilds MainMenu Canvas from scratch
///   • Wires MainMenuController fields: playOnlineButton, practiceButton,
///     difficultyDropdown, statusText
///   • Keeps gear/settings button (calls SettingsPanel)
///   • Ensures NetworkManager + AudioManager are in MainMenu scene
///   • Rebuilds APK
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v10
{
    const string DoneKey = "VisualOverhaul_v10_Done";

    static VisualOverhaul_v10()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged += WaitForEditMode;
            return;
        }
        EditorApplication.delayCall += Run;
    }

    static void WaitForEditMode(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= WaitForEditMode;
            if (!EditorPrefs.GetBool(DoneKey, false))
                EditorApplication.delayCall += Run;
        }
    }

    static Sprite _ws;
    static Sprite WS => _ws ??= AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/_WhiteSquare.png");

    // ── brand colors ─────────────────────────────────────────────
    static readonly Color BG_DARK    = new Color(0.08f, 0.10f, 0.16f);
    static readonly Color BG_PANEL   = new Color(0.12f, 0.14f, 0.22f, 0.96f);
    static readonly Color BTN_ONLINE = new Color(0.15f, 0.55f, 0.95f);
    static readonly Color BTN_AI     = new Color(0.20f, 0.72f, 0.35f);
    static readonly Color BTN_CLOSE  = new Color(0.45f, 0.45f, 0.50f);
    static readonly Color GOLD       = new Color(1f, 0.85f, 0.20f);

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return;

        RebuildMainMenu();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[v10] MainMenu rebuilt. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ══════════════════════════════════════════════════════════════
    //  REBUILD MAIN MENU
    // ══════════════════════════════════════════════════════════════
    static void RebuildMainMenu()
    {
        string path = "Assets/Scenes/MainMenu.unity";
        var scene   = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        // ── 1. Guarantee required singletons exist ────────────────
        EnsureSingleton<NetworkManager>("NetworkManager");
        EnsureSingleton<AudioManager>("AudioManager");

        // ── 2. Find or create EventSystem ─────────────────────────
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── 3. Wipe old Canvas and rebuild fresh ───────────────────
        var oldCanvas = Object.FindObjectOfType<Canvas>();
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

        // ── 4. Camera background ───────────────────────────────────
        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = BG_DARK;
            cam.clearFlags      = CameraClearFlags.SolidColor;
        }

        // ── 5. Build new Canvas ───────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        RectTransform root = canvasGO.GetComponent<RectTransform>();

        // ── 6. Sky gradient background ────────────────────────────
        MakePanel(root, "BgTop",
            new Vector2(0, 0.5f), new Vector2(1, 1),
            new Color(0.06f, 0.10f, 0.22f));
        MakePanel(root, "BgBot",
            new Vector2(0, 0), new Vector2(1, 0.5f),
            new Color(0.12f, 0.18f, 0.30f));

        // ── 7. Title ──────────────────────────────────────────────
        var titleLbl = MakeLabel(root, "TitleText", "STICK\nARCHERS",
            new Vector2(0, 580), new Vector2(900, 260), 96,
            GOLD, FontStyles.Bold);
        titleLbl.alignment = TextAlignmentOptions.Center;

        var subLbl = MakeLabel(root, "SubText", "BATTLE",
            new Vector2(0, 430), new Vector2(900, 100), 52,
            new Color(1, 1, 1, 0.80f), FontStyles.Bold);
        subLbl.alignment = TextAlignmentOptions.Center;

        // ── 8. Center card panel ──────────────────────────────────
        var card = MakePanel(root, "CardPanel",
            new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.68f),
            BG_PANEL, radius: true);

        // ── 9. PLAY ONLINE button ─────────────────────────────────
        var (onlineBtn, onlineLbl) = MakeButton(card.GetComponent<RectTransform>(),
            "PlayOnlineButton", "⚡  PLAY ONLINE",
            new Vector2(0, 160), new Vector2(700, 120), BTN_ONLINE);

        // ── 10. VS COMPUTER button ────────────────────────────────
        var (aiBtn, aiLbl) = MakeButton(card.GetComponent<RectTransform>(),
            "PracticeButton", "🤖  VS COMPUTER",
            new Vector2(0, 10), new Vector2(700, 120), BTN_AI);

        // ── 11. Difficulty label + dropdown ───────────────────────
        MakeLabel(card.GetComponent<RectTransform>(), "DiffLabel",
            "AI DIFFICULTY",
            new Vector2(0, -120), new Vector2(600, 50), 28,
            new Color(1, 1, 1, 0.70f), FontStyles.Normal).alignment = TextAlignmentOptions.Center;

        var dropdown = MakeDropdown(card.GetComponent<RectTransform>(),
            "DifficultyDropdown",
            new Vector2(0, -195), new Vector2(500, 70),
            new[] { "Easy", "Normal", "Hard" });

        // ── 12. Status text ───────────────────────────────────────
        var statusTmp = MakeLabel(root, "StatusText", "",
            new Vector2(0, -760), new Vector2(900, 60), 30,
            new Color(1, 1, 1, 0.75f), FontStyles.Normal);
        statusTmp.alignment = TextAlignmentOptions.Center;

        // ── 13. Gear button (top-right) ───────────────────────────
        var gearGO = MakePanel(root, "GearButton",
            new Vector2(0, 0), new Vector2(0, 0),  // use anchored pos + sizeDelta
            new Color(0.20f, 0.20f, 0.30f, 0.85f));
        var gearRT = gearGO.GetComponent<RectTransform>();
        gearRT.anchorMin = new Vector2(1, 1); gearRT.anchorMax = new Vector2(1, 1);
        gearRT.pivot     = new Vector2(1, 1);
        gearRT.anchoredPosition = new Vector2(-24, -24);
        gearRT.sizeDelta = new Vector2(100, 100);
        var gearBtn = gearGO.AddComponent<Button>();
        MakeLabel(gearRT, "GearLabel", "⚙",
            Vector2.zero, new Vector2(100, 100), 58,
            Color.white, FontStyles.Bold).alignment = TextAlignmentOptions.Center;

        // ── 14. Settings panel ────────────────────────────────────
        var settingsPanel = BuildSettingsPanel(root);

        // ── 15. Wire MainMenuController ───────────────────────────
        var mmc = canvasGO.GetComponent<MainMenuController>()
                  ?? canvasGO.AddComponent<MainMenuController>();
        mmc.playOnlineButton  = onlineBtn;
        mmc.practiceButton    = aiBtn;
        mmc.statusText        = statusTmp;
        mmc.difficultyDropdown = dropdown;
        EditorUtility.SetDirty(mmc);

        // ── 16. Wire SettingsPanel ────────────────────────────────
        var sp = canvasGO.GetComponent<SettingsPanel>()
                 ?? canvasGO.AddComponent<SettingsPanel>();
        sp.panel       = settingsPanel.panelGO;
        sp.openButton  = gearBtn;
        sp.closeButton = settingsPanel.closeBtn;
        sp.sfxSlider   = settingsPanel.sfxSlider;
        sp.musicSlider = settingsPanel.musicSlider;
        sp.muteToggle  = settingsPanel.muteToggle;
        sp.sfxValueText   = settingsPanel.sfxText;
        sp.musicValueText = settingsPanel.musicText;
        EditorUtility.SetDirty(sp);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[v10] MainMenu fully rebuilt and wired.");
    }

    // ══════════════════════════════════════════════════════════════
    //  SETTINGS PANEL
    // ══════════════════════════════════════════════════════════════
    struct SettingsPanelRefs
    {
        public GameObject panelGO;
        public Button closeBtn;
        public Slider sfxSlider, musicSlider;
        public Toggle muteToggle;
        public TextMeshProUGUI sfxText, musicText;
    }

    static SettingsPanelRefs BuildSettingsPanel(RectTransform root)
    {
        var refs = new SettingsPanelRefs();

        var panelGO = MakePanel(root, "SettingsPanel",
            new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f),
            new Color(0.08f, 0.10f, 0.18f, 0.97f), radius: true);
        refs.panelGO = panelGO;
        panelGO.SetActive(false);
        var pRT = panelGO.GetComponent<RectTransform>();

        MakeLabel(pRT, "Title", "SOUND SETTINGS",
            new Vector2(0, 220), new Vector2(700, 70), 42,
            GOLD, FontStyles.Bold).alignment = TextAlignmentOptions.Center;

        // SFX row
        MakeLabel(pRT, "SFXLabel", "Sound Effects",
            new Vector2(-170, 110), new Vector2(320, 55), 30,
            Color.white, FontStyles.Normal);
        refs.sfxSlider = MakeSlider(pRT, "SFXSlider",
            new Vector2(110, 110), new Vector2(360, 34));
        refs.sfxText = MakeLabel(pRT, "SFXValue", "100%",
            new Vector2(320, 110), new Vector2(110, 44), 26,
            Color.white, FontStyles.Normal);

        // Music row
        MakeLabel(pRT, "MusicLabel", "Music",
            new Vector2(-170, 30), new Vector2(320, 55), 30,
            Color.white, FontStyles.Normal);
        refs.musicSlider = MakeSlider(pRT, "MusicSlider",
            new Vector2(110, 30), new Vector2(360, 34));
        refs.musicText = MakeLabel(pRT, "MusicValue", "100%",
            new Vector2(320, 30), new Vector2(110, 44), 26,
            Color.white, FontStyles.Normal);

        // Mute toggle
        refs.muteToggle = MakeToggle(pRT, "MuteToggle", "Mute All",
            new Vector2(0, -60), new Vector2(360, 55));

        // Close button
        var (cb, _) = MakeButton(pRT, "CloseButton", "CLOSE",
            new Vector2(0, -170), new Vector2(320, 80), BTN_CLOSE);
        refs.closeBtn = cb;

        return refs;
    }

    // ══════════════════════════════════════════════════════════════
    //  SINGLETONS
    // ══════════════════════════════════════════════════════════════
    static void EnsureSingleton<T>(string goName) where T : MonoBehaviour
    {
        if (Object.FindObjectOfType<T>() == null)
        {
            var go = new GameObject(goName);
            go.AddComponent<T>();
            Debug.Log($"[v10] Added {goName} to MainMenu");
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ══════════════════════════════════════════════════════════════
    static GameObject MakePanel(RectTransform parent, string name,
        Vector2 aMin, Vector2 aMax, Color color, bool radius = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = WS; img.color = color;
        return go;
    }

    static TextMeshProUGUI MakeLabel(RectTransform parent, string name, string text,
        Vector2 pos, Vector2 size, float fs, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fs; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }

    static (Button btn, TextMeshProUGUI lbl) MakeButton(RectTransform parent,
        string name, string labelText, Vector2 pos, Vector2 size, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>(); img.sprite = WS; img.color = bg;
        var btn = go.AddComponent<Button>();
        var cs  = btn.colors;
        cs.highlightedColor = bg * 1.3f;
        cs.pressedColor     = bg * 0.7f;
        btn.colors = cs;

        var lGO = new GameObject("Label"); lGO.transform.SetParent(go.transform, false);
        var lRT = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var tmp = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText; tmp.fontSize = 38; tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold; tmp.alignment = TextAlignmentOptions.Center;
        return (btn, tmp);
    }

    static TMP_Dropdown MakeDropdown(RectTransform parent, string name,
        Vector2 pos, Vector2 size, string[] options)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = WS; img.color = new Color(0.20f, 0.22f, 0.30f);

        // Label child
        var lGO = new GameObject("Label"); lGO.transform.SetParent(go.transform, false);
        var lRT = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.05f, 0); lRT.anchorMax = new Vector2(0.85f, 1);
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var lTMP = lGO.AddComponent<TextMeshProUGUI>();
        lTMP.text = "Normal"; lTMP.fontSize = 32; lTMP.color = Color.white;
        lTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Arrow child
        var arGO = new GameObject("Arrow"); arGO.transform.SetParent(go.transform, false);
        var arRT = arGO.AddComponent<RectTransform>();
        arRT.anchorMin = new Vector2(0.85f, 0.1f); arRT.anchorMax = new Vector2(1, 0.9f);
        arRT.offsetMin = arRT.offsetMax = Vector2.zero;
        var arImg = arGO.AddComponent<Image>(); arImg.sprite = WS;
        arImg.color = new Color(0.6f, 0.6f, 0.7f);

        // Template (hidden)
        var tmplGO = new GameObject("Template"); tmplGO.transform.SetParent(go.transform, false);
        tmplGO.SetActive(false);
        var tmplRT = tmplGO.AddComponent<RectTransform>();
        tmplRT.anchorMin = new Vector2(0, 0); tmplRT.anchorMax = new Vector2(1, 0);
        tmplRT.pivot     = new Vector2(0.5f, 1);
        tmplRT.anchoredPosition = Vector2.zero; tmplRT.sizeDelta = new Vector2(0, 150);
        var tmplImg = tmplGO.AddComponent<Image>(); tmplImg.sprite = WS;
        tmplImg.color = new Color(0.15f, 0.17f, 0.25f);
        var tmplScroll = tmplGO.AddComponent<ScrollRect>();
        tmplScroll.horizontal = false;

        var vpGO = new GameObject("Viewport"); vpGO.transform.SetParent(tmplGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<Image>().color = Color.clear;
        var mask = vpGO.AddComponent<Mask>(); mask.showMaskGraphic = false;
        tmplScroll.viewport = vpRT;

        var contentGO = new GameObject("Content"); contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1); contentRT.sizeDelta = new Vector2(0, 50);
        tmplScroll.content = contentRT;

        var itemGO = new GameObject("Item"); itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = itemGO.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f); itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 50);
        itemGO.AddComponent<Image>().color = Color.clear;
        var toggle = itemGO.AddComponent<Toggle>();

        var itemLblGO = new GameObject("Item Label"); itemLblGO.transform.SetParent(itemGO.transform, false);
        var itemLblRT = itemLblGO.AddComponent<RectTransform>();
        itemLblRT.anchorMin = Vector2.zero; itemLblRT.anchorMax = Vector2.one;
        itemLblRT.offsetMin = new Vector2(10, 0); itemLblRT.offsetMax = Vector2.zero;
        var itemTMP = itemLblGO.AddComponent<TextMeshProUGUI>();
        itemTMP.text = "Option"; itemTMP.fontSize = 30; itemTMP.color = Color.white;
        itemTMP.alignment = TextAlignmentOptions.MidlineLeft;
        toggle.targetGraphic = itemGO.GetComponent<Image>();

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.template    = tmplRT;
        dd.captionText = lTMP;
        dd.itemText    = itemTMP;
        dd.targetGraphic = img;
        dd.ClearOptions();
        foreach (var o in options)
            dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.value = 1; // default Normal
        dd.RefreshShownValue();
        return dd;
    }

    static Slider MakeSlider(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var bg = new GameObject("Background"); bg.transform.SetParent(go.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>(); bgImg.sprite = WS; bgImg.color = new Color(0.25f, 0.25f, 0.30f);

        var fa = new GameObject("Fill Area"); fa.transform.SetParent(go.transform, false);
        var faRT = fa.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = new Vector2(5, 5); faRT.offsetMax = new Vector2(-5, -5);
        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>(); fillImg.sprite = WS; fillImg.color = new Color(0.20f, 0.75f, 0.30f);

        var ha = new GameObject("Handle Slide Area"); ha.transform.SetParent(go.transform, false);
        var haRT = ha.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);
        var handle = new GameObject("Handle"); handle.transform.SetParent(ha.transform, false);
        var hRT = handle.AddComponent<RectTransform>(); hRT.sizeDelta = new Vector2(28, 44);
        var hImg = handle.AddComponent<Image>(); hImg.sprite = WS; hImg.color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.targetGraphic = hImg;
        slider.fillRect      = fillRT;
        slider.handleRect    = hRT;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        return slider;
    }

    static Toggle MakeToggle(RectTransform parent, string name, string label,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var bg = new GameObject("Background"); bg.transform.SetParent(go.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.1f); bgRT.anchorMax = new Vector2(0.14f, 0.9f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>(); bgImg.sprite = WS; bgImg.color = new Color(0.25f, 0.25f, 0.30f);

        var check = new GameObject("Checkmark"); check.transform.SetParent(bg.transform, false);
        var cRT = check.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.1f, 0.1f); cRT.anchorMax = new Vector2(0.9f, 0.9f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var cImg = check.AddComponent<Image>(); cImg.sprite = WS; cImg.color = new Color(0.95f, 0.35f, 0.20f);

        var lGO = new GameObject("Label"); lGO.transform.SetParent(go.transform, false);
        var lRT = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.18f, 0); lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var tmp = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 28; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic       = cImg;
        toggle.isOn          = false;
        return toggle;
    }

    // ══════════════════════════════════════════════════════════════
    //  BUILD APK
    // ══════════════════════════════════════════════════════════════
    static void BuildAPK()
    {
        EditorApplication.delayCall -= BuildAPK;
        string outputPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "StickArchers.apk");

        var report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameArena.unity" },
            outputPath, BuildTarget.Android, BuildOptions.None);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[v10] ✅ APK built → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[v10] ❌ Build failed: " + report.summary.result);
    }
}
#endif
