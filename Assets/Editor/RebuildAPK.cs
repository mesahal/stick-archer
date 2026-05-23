#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Auto-rebuilds the APK once after script recompile.
/// Fixes: status text not updating + NetworkManager duplicate crash.
/// </summary>
[InitializeOnLoad]
public static class RebuildAPK
{
    const string DoneKey = "RebuildAPK_v3_Done";

    static RebuildAPK()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        EditorApplication.delayCall += Build;
    }

    static void Build()
    {
        EditorApplication.delayCall -= Build;

        string path = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "StickArchers.apk");

        Debug.Log("[RebuildAPK] Building updated APK → " + path);

        var report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameArena.unity" },
            path,
            BuildTarget.Android,
            BuildOptions.None
        );

        EditorPrefs.SetBool(DoneKey, true);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[RebuildAPK] ✅ APK ready → " + path);
            EditorUtility.RevealInFinder(path);
        }
        else
            Debug.LogError("[RebuildAPK] ❌ Build failed: " + report.summary.result);
    }
}
#endif
