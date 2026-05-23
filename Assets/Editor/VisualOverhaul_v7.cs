#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// v7 — Just rebuilds the APK after the bugfixes:
///   - Buttons (Rematch/Menu/Back) now wire up via UIManager.WireButtons()
///   - Respawn uses GameObject.Find for spawn position (no more (0,0,0) warp)
///   - ArenaManager no longer double-spawns
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v7
{
    const string DoneKey = "VisualOverhaul_v7_Done";

    static VisualOverhaul_v7()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged += WaitForEditMode;
            return;
        }
        EditorApplication.delayCall += Build;
    }

    static void WaitForEditMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= WaitForEditMode;
            if (!EditorPrefs.GetBool(DoneKey, false))
                EditorApplication.delayCall += Build;
        }
    }

    static void Build()
    {
        EditorApplication.delayCall -= Build;
        if (EditorApplication.isPlaying) return;

        string outputPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "StickArchers.apk");

        Debug.Log("[v7] Building APK with button/respawn bugfixes...");
        var report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameArena.unity" },
            outputPath, BuildTarget.Android, BuildOptions.None);

        EditorPrefs.SetBool(DoneKey, true);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[v7] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[v7] ❌ Build failed: " + report.summary.result);
    }
}
#endif
