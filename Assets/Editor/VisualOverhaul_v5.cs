#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// v5 — Final UI polish to match the Stick Archers Battle reference:
///   - Score HUD with team-colored P1 (blue) and P2 (red) panels
///   - Player labels and big score numbers
///   - Rebuild APK with all the new scripts (single-touch, hit-heart, etc.)
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v5
{
    const string DoneKey = "VisualOverhaul_v5_Done";

    static VisualOverhaul_v5()
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

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return;
        Debug.Log("[v5] Starting...");

        UpdateGameArenaHUD();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[v5] HUD updated. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ══════════════════════════════════════════════════════════
    //  REBUILD THE SCORE HUD WITH TEAM PANELS
    // ══════════════════════════════════════════════════════════
    static void UpdateGameArenaHUD()
    {
        string scenePath = "Assets/Scenes/GameArena.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find existing UIManager
        var ui = Object.FindObjectOfType<UIManager>();
        if (ui == null) { Debug.LogError("[v5] UIManager not found"); return; }

        var canvas = ui.GetComponent<Canvas>();
        if (canvas == null) { Debug.LogError("[v5] Canvas not found"); return; }
        var canvasRT = canvas.GetComponent<RectTransform>();

        // ── Remove old score HUD ────────────────────────────────
        var oldHud = canvasRT.Find("GameHUDPanel");
        if (oldHud != null) Object.DestroyImmediate(oldHud.gameObject);

        // ── New HUD: blue left, vs middle, red right ────────────
        var hud = MakePanel(canvasRT, "GameHUDPanel",
            new Vector2(0, 0.88f), new Vector2(1, 1f), new Color(0, 0, 0, 0));
        ui.gameHUDPanel = hud;

        // P1 panel (blue, left)
        var p1 = MakePanel(hud.transform as RectTransform, "P1Panel",
            new Vector2(0.05f, 0.10f), new Vector2(0.42f, 0.90f),
            new Color(0.20f, 0.42f, 0.85f, 0.95f));

        // P1 label
        MakeLabel(p1.transform as RectTransform, "P1Label", "P1",
            new Vector2(-130, 0), new Vector2(80, 60), 32,
            new Color(1, 1, 1, 0.95f), FontStyles.Bold);

        // P1 score
        ui.player1ScoreText = MakeLabel(p1.transform as RectTransform, "P1Score", "0",
            new Vector2(100, 0), new Vector2(160, 80), 56,
            Color.white, FontStyles.Bold);

        // P1 portrait box (simple skin colored square as placeholder)
        var p1Face = MakePanel(p1.transform as RectTransform, "P1Face",
            new Vector2(0.05f, 0.15f), new Vector2(0.22f, 0.85f),
            new Color(0.95f, 0.78f, 0.62f, 1f));
        // Add hair on top
        var p1Hair = MakePanel(p1Face.transform as RectTransform, "Hair",
            new Vector2(0, 0.78f), new Vector2(1, 1f),
            new Color(0.30f, 0.18f, 0.08f, 1f));

        // VS in center
        MakeLabel(hud.transform as RectTransform, "VS", "VS",
            new Vector2(0, 0), new Vector2(80, 60), 36,
            new Color(1f, 0.85f, 0.2f, 1f), FontStyles.Bold);

        // P2 panel (red, right)
        var p2 = MakePanel(hud.transform as RectTransform, "P2Panel",
            new Vector2(0.58f, 0.10f), new Vector2(0.95f, 0.90f),
            new Color(0.85f, 0.20f, 0.18f, 0.95f));

        ui.player2ScoreText = MakeLabel(p2.transform as RectTransform, "P2Score", "0",
            new Vector2(-100, 0), new Vector2(160, 80), 56,
            Color.white, FontStyles.Bold);

        MakeLabel(p2.transform as RectTransform, "P2Label", "P2",
            new Vector2(130, 0), new Vector2(80, 60), 32,
            new Color(1, 1, 1, 0.95f), FontStyles.Bold);

        var p2Face = MakePanel(p2.transform as RectTransform, "P2Face",
            new Vector2(0.78f, 0.15f), new Vector2(0.95f, 0.85f),
            new Color(0.95f, 0.78f, 0.62f, 1f));
        var p2Hair = MakePanel(p2Face.transform as RectTransform, "Hair",
            new Vector2(0, 0.78f), new Vector2(1, 1f),
            new Color(0.30f, 0.18f, 0.08f, 1f));

        // ── Remove the old ChargeMeter (we now have one inside TouchControls) ─
        var oldCharge = canvasRT.Find("ChargeMeter");
        if (oldCharge != null) Object.DestroyImmediate(oldCharge.gameObject);
        ui.chargeMeter = null; // clear ref

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[v5] HUD rebuilt with team panels (blue P1 | VS | red P2)");
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
            Debug.Log("[v5] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[v5] ❌ Build failed: " + report.summary.result);
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════
    static GameObject MakePanel(RectTransform parent, string name,
        Vector2 aMin, Vector2 aMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MakeLabel(RectTransform parent, string name, string text,
        Vector2 pos, Vector2 size, float fs, Color color, FontStyles style = FontStyles.Normal)
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
}
#endif
