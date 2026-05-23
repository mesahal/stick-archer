#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// v8 — Adds sound settings UI:
///   • Gear button (top-right) on MainMenu
///   • Settings panel: SFX vol slider, Music vol slider, Mute toggle
///   • Ensures AudioManager exists in MainMenu (so settings persist into GameArena)
///   • Rebuilds APK
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v8
{
    const string DoneKey = "VisualOverhaul_v8_Done";

    static VisualOverhaul_v8()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged += WaitForEditMode;
            return;
        }
        EditorApplication.delayCall += Run;
    }

    static void WaitForEditMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= WaitForEditMode;
            if (!EditorPrefs.GetBool(DoneKey, false))
                EditorApplication.delayCall += Run;
        }
    }

    static Sprite _whiteSquare;
    static Sprite WhiteSquare => _whiteSquare ??=
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/_WhiteSquare.png");

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return;

        AddSettingsToMainMenu();
        EnsureAudioManagerInMainMenu();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[v8] Settings panel added. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ══════════════════════════════════════════════════════════
    //  ADD SETTINGS PANEL TO MAIN MENU
    // ══════════════════════════════════════════════════════════
    static void AddSettingsToMainMenu()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("[v8] No Canvas in MainMenu"); return; }
        RectTransform root = canvas.GetComponent<RectTransform>();

        // Remove any previous instance
        var oldSettings = root.Find("SettingsPanel");
        if (oldSettings != null) Object.DestroyImmediate(oldSettings.gameObject);
        var oldGear = root.Find("GearButton");
        if (oldGear != null) Object.DestroyImmediate(oldGear.gameObject);

        // ── Gear button top-right ────────────────────────────────
        var gearGO = new GameObject("GearButton");
        gearGO.transform.SetParent(root, false);
        var gearRT = gearGO.AddComponent<RectTransform>();
        gearRT.anchorMin = new Vector2(1, 1);
        gearRT.anchorMax = new Vector2(1, 1);
        gearRT.pivot     = new Vector2(1, 1);
        gearRT.anchoredPosition = new Vector2(-20, -20);
        gearRT.sizeDelta = new Vector2(90, 90);
        var gearImg = gearGO.AddComponent<Image>();
        gearImg.sprite = WhiteSquare;
        gearImg.color = new Color(0.20f, 0.20f, 0.30f, 0.85f);
        var gearBtn = gearGO.AddComponent<Button>();

        // Gear glyph
        MakeLabel(gearRT, "GearLabel", "⚙",
            Vector2.zero, new Vector2(90, 90), 56,
            new Color(1, 1, 1, 0.95f), FontStyles.Bold);

        // ── Settings panel (hidden by default) ───────────────────
        var panelGO = new GameObject("SettingsPanel");
        panelGO.transform.SetParent(root, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(700, 500);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.10f, 0.10f, 0.18f, 0.95f);

        // Title
        MakeLabel(panelRT, "Title", "SOUND SETTINGS",
            new Vector2(0, 190), new Vector2(600, 60), 36,
            new Color(1, 0.85f, 0.2f, 1f), FontStyles.Bold);

        // ── SFX section ──────────────────────────────────────────
        MakeLabel(panelRT, "SFXLabel", "Sound Effects",
            new Vector2(-200, 90), new Vector2(280, 50), 26,
            Color.white, FontStyles.Normal);
        var sfxSlider = MakeSlider(panelRT, "SFXSlider",
            new Vector2(70, 90), new Vector2(300, 30));
        var sfxValueText = MakeLabel(panelRT, "SFXValue", "100%",
            new Vector2(260, 90), new Vector2(120, 40), 24, Color.white, FontStyles.Normal);

        // ── Music section ────────────────────────────────────────
        MakeLabel(panelRT, "MusicLabel", "Music",
            new Vector2(-200, 30), new Vector2(280, 50), 26,
            Color.white, FontStyles.Normal);
        var musicSlider = MakeSlider(panelRT, "MusicSlider",
            new Vector2(70, 30), new Vector2(300, 30));
        var musicValueText = MakeLabel(panelRT, "MusicValue", "40%",
            new Vector2(260, 30), new Vector2(120, 40), 24, Color.white, FontStyles.Normal);

        // ── Mute toggle ──────────────────────────────────────────
        var muteToggle = MakeToggle(panelRT, "MuteToggle", "Mute All",
            new Vector2(0, -50), new Vector2(300, 50));

        // ── Close button ─────────────────────────────────────────
        var (closeBtn, _) = MakeButton(panelRT, "CloseButton", "CLOSE",
            new Vector2(0, -160), new Vector2(280, 70),
            new Color(0.55f, 0.55f, 0.55f));

        // Wire up SettingsPanel component
        var sp = canvas.gameObject.GetComponent<SettingsPanel>() ?? canvas.gameObject.AddComponent<SettingsPanel>();
        sp.panel          = panelGO;
        sp.openButton     = gearBtn;
        sp.closeButton    = closeBtn;
        sp.sfxSlider      = sfxSlider;
        sp.musicSlider    = musicSlider;
        sp.muteToggle     = muteToggle;
        sp.sfxValueText   = sfxValueText;
        sp.musicValueText = musicValueText;

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[v8] Settings panel + gear button added to MainMenu");
    }

    // ══════════════════════════════════════════════════════════
    //  Ensure AudioManager exists in MainMenu (DontDestroyOnLoad
    //  carries it into GameArena automatically)
    // ══════════════════════════════════════════════════════════
    static void EnsureAudioManagerInMainMenu()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var am = Object.FindObjectOfType<AudioManager>();
        if (am == null)
        {
            var amGO = new GameObject("AudioManager");
            amGO.AddComponent<AudioManager>();
            Debug.Log("[v8] Added AudioManager to MainMenu");
        }

        EditorSceneManager.SaveScene(scene);
    }

    // ══════════════════════════════════════════════════════════
    //  BUILD APK
    // ══════════════════════════════════════════════════════════
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
            Debug.Log("[v8] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[v8] ❌ Build failed: " + report.summary.result);
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════
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
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static (Button btn, TextMeshProUGUI label) MakeButton(RectTransform parent, string name,
        string labelText, Vector2 pos, Vector2 size, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = WhiteSquare; img.color = bg;
        var btn = go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText; tmp.fontSize = 30; tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        return (btn, tmp);
    }

    static Slider MakeSlider(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = WhiteSquare;
        bgImg.color = new Color(0.25f, 0.25f, 0.30f, 1f);

        // Fill area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = new Vector2(5, 5); faRT.offsetMax = new Vector2(-5, -5);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = WhiteSquare;
        fillImg.color = new Color(0.20f, 0.75f, 0.30f, 1f);

        // Handle
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRT = handle.AddComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(28, 38);
        var hImg = handle.AddComponent<Image>();
        hImg.sprite = WhiteSquare;
        hImg.color = Color.white;

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

        // Background box
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.1f);
        bgRT.anchorMax = new Vector2(0.13f, 0.9f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.sprite = WhiteSquare;
        bgImg.color = new Color(0.25f, 0.25f, 0.30f);

        // Checkmark
        var check = new GameObject("Checkmark");
        check.transform.SetParent(bg.transform, false);
        var cRT = check.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.15f, 0.15f);
        cRT.anchorMax = new Vector2(0.85f, 0.85f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var cImg = check.AddComponent<Image>();
        cImg.sprite = WhiteSquare;
        cImg.color = new Color(0.95f, 0.35f, 0.20f);

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lRT = labelGO.AddComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.18f, 0); lRT.anchorMax = new Vector2(1, 1);
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 26; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic       = cImg;
        toggle.isOn          = false;
        return toggle;
    }
}
#endif
