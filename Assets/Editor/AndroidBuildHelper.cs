#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Stick Archers → Configure Android Settings
/// Sets all required Android player settings for the APK build.
/// Run this once, then use File → Build Settings → Build to create the APK.
/// </summary>
public static class AndroidBuildHelper
{
    [MenuItem("Tools/Stick Archers/Configure Android Settings")]
    public static void ConfigureAndroid()
    {
        // ── Package & version ──────────────────────────────────────
        PlayerSettings.SetApplicationIdentifier(
            BuildTargetGroup.Android, "com.yourcompany.stickarchers");
        PlayerSettings.productName      = "Stick Archers Battle";
        PlayerSettings.bundleVersion    = "1.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        // ── SDK targets ────────────────────────────────────────────
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel24; // Android 7
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33; // Android 13

        // ── Orientation ────────────────────────────────────────────
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft  = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait       = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // ── Graphics ───────────────────────────────────────────────
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

        // ── Scripting backend: IL2CPP for release, Mono for fast dev builds
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
            ScriptingImplementation.Mono2x);   // change to IL2CPP for Play Store

        // ── Internet permission (required for Photon) ──────────────
        PlayerSettings.Android.forceInternetPermission = true;

        // ── Misc ───────────────────────────────────────────────────
        PlayerSettings.Android.startInFullscreen = true;
        PlayerSettings.runInBackground           = true;

        // Splash screen (disable for cleaner look)
        PlayerSettings.SplashScreen.show = false;

        AssetDatabase.SaveAssets();

        Debug.Log("[AndroidBuildHelper] ✅ Android settings configured.");
        EditorUtility.DisplayDialog("Android Settings Applied",
            "All Android player settings have been configured.\n\n" +
            "Next:\n" +
            "1. File → Build Settings\n" +
            "2. Select Android in the platform list\n" +
            "3. Click 'Switch Platform'\n" +
            "4. Click 'Build' and save as StickArchers.apk",
            "Got it");
    }

    // ── Quick-build helper (optional) ──────────────────────────────
    [MenuItem("Tools/Stick Archers/Build Android APK")]
    public static void BuildAPK()
    {
        string outputPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "StickArchers.apk");

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
        {
            Debug.Log($"[AndroidBuildHelper] ✅ APK built → {outputPath}");
            EditorUtility.RevealInFinder(outputPath);
        }
        else
        {
            Debug.LogError("[AndroidBuildHelper] ❌ Build failed: " + report.summary.result);
        }
    }
}
#endif
