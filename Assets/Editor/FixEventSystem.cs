#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Auto-runs on load. Adds EventSystem + StandaloneInputModule to
/// MainMenu and GameArena scenes (required for button/touch input).
/// Then rebuilds the APK to Desktop.
/// </summary>
[InitializeOnLoad]
public static class FixEventSystem
{
    const string DoneKey = "FixEventSystem_Done";

    static FixEventSystem()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        Debug.Log("[FixEventSystem] Adding EventSystem to scenes...");

        FixScene("Assets/Scenes/MainMenu.unity");
        FixScene("Assets/Scenes/GameArena.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorPrefs.SetBool(DoneKey, true);
        Debug.Log("[FixEventSystem] ✅ EventSystem added to both scenes. Rebuilding APK...");

        // Small delay then rebuild
        EditorApplication.delayCall += RebuildAPK;
    }

    static void FixScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Check if EventSystem already exists
        var existing = Object.FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            Debug.Log($"[FixEventSystem] {scenePath} already has EventSystem, skipping.");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // Add EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[FixEventSystem] ✅ EventSystem added to {scenePath}");
    }

    static void RebuildAPK()
    {
        EditorApplication.delayCall -= RebuildAPK;

        string outputPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "StickArchers.apk");

        Debug.Log("[FixEventSystem] Building APK → " + outputPath);

        var report = BuildPipeline.BuildPlayer(
            new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/GameArena.unity",
            },
            outputPath,
            BuildTarget.Android,
            BuildOptions.None
        );

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("[FixEventSystem] ✅ APK rebuilt → " + outputPath);
        else
            Debug.LogError("[FixEventSystem] ❌ Build failed: " + report.summary.result);
    }
}
#endif
