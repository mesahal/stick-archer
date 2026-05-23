#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// v6 — HP heart system + gameplay tuning + APK rebuild.
///   - Moves _WhiteSquare to Resources/ so it's accessible at runtime
///   - Adds 3 hearts to each player's HUD panel
///   - Wires UIManager.player1Hearts / player2Hearts references
///   - Rebuilds APK
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v6
{
    const string DoneKey = "VisualOverhaul_v6_Done";

    static VisualOverhaul_v6()
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
        Debug.Log("[v6] Starting...");

        EnsureWhiteSquareInResources();
        UpdateHUD();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[v6] HP hearts added. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ══════════════════════════════════════════════════════════
    //  Move _WhiteSquare into Resources so Resources.Load works
    // ══════════════════════════════════════════════════════════
    static void EnsureWhiteSquareInResources()
    {
        const string src  = "Assets/Art/_WhiteSquare.png";
        const string dest = "Assets/Resources/_WhiteSquare.png";

        if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");

        if (!File.Exists(dest))
        {
            if (File.Exists(src))
            {
                AssetDatabase.CopyAsset(src, dest);
            }
            else
            {
                // Build a fresh one
                var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color32[32 * 32];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255,255,255,255);
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(dest, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(dest);
            }

            var imp = (TextureImporter)AssetImporter.GetAtPath(dest);
            imp.textureType         = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 32f;
            imp.filterMode          = FilterMode.Point;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled       = false;
            AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceUpdate);
        }

        _whiteSquare = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
        Debug.Log("[v6] _WhiteSquare available in Resources/");
    }

    // ══════════════════════════════════════════════════════════
    //  ADD HP HEARTS TO THE HUD
    // ══════════════════════════════════════════════════════════
    static void UpdateHUD()
    {
        string scenePath = "Assets/Scenes/GameArena.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var ui = Object.FindObjectOfType<UIManager>();
        if (ui == null) { Debug.LogError("[v6] UIManager not found"); return; }

        var hud = ui.gameHUDPanel;
        if (hud == null) { Debug.LogError("[v6] HUD panel not found"); return; }

        var p1Panel = hud.transform.Find("P1Panel");
        var p2Panel = hud.transform.Find("P2Panel");
        if (p1Panel == null || p2Panel == null)
        {
            Debug.LogError("[v6] P1Panel/P2Panel not found");
            return;
        }

        // Remove any existing hearts (re-runs)
        foreach (var name in new[] { "Heart0","Heart1","Heart2","HeartRow" })
        {
            var existing1 = p1Panel.Find(name);
            if (existing1 != null) Object.DestroyImmediate(existing1.gameObject);
            var existing2 = p2Panel.Find(name);
            if (existing2 != null) Object.DestroyImmediate(existing2.gameObject);
        }

        ui.player1Hearts = CreateHeartRow(p1Panel, true);
        ui.player2Hearts = CreateHeartRow(p2Panel, false);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[v6] HP hearts added to HUD panels");
    }

    static Image[] CreateHeartRow(Transform parent, bool isP1)
    {
        // Heart row container at top-center of the player panel
        var rowGO = new GameObject("HeartRow");
        rowGO.transform.SetParent(parent, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(isP1 ? 0.35f : 0.30f, 0.70f);
        rowRT.anchorMax = new Vector2(isP1 ? 0.65f : 0.60f, 0.98f);
        rowRT.offsetMin = rowRT.offsetMax = Vector2.zero;

        var hearts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("Heart" + i);
            go.transform.SetParent(rowGO.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(i / 3f, 0);
            rt.anchorMax = new Vector2((i + 1) / 3f, 1f);
            rt.offsetMin = new Vector2(2, 2);
            rt.offsetMax = new Vector2(-2, -2);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSquare;
            img.color  = new Color(1f, 0.25f, 0.30f, 1f);
            hearts[i] = img;
        }
        return hearts;
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
            Debug.Log("[v6] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[v6] ❌ Build failed: " + report.summary.result);
    }
}
#endif
