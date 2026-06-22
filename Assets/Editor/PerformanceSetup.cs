#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// One-shot performance setup helpers for the mobile build.
///
/// Menu items:
///   Stick Archer/Performance/Build Character Sprite Atlas
///   Stick Archer/Performance/Apply Android Build Settings
/// </summary>
public static class PerformanceSetup
{
    const string ATLAS_PATH = "Assets/Art/Sprites/Characters.spriteatlas";

    [MenuItem("Stick Archer/Performance/Build Character Sprite Atlas")]
    public static void BuildCharacterAtlas()
    {
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ATLAS_PATH);
        bool created = false;
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, ATLAS_PATH);
            created = true;
        }

        // Find the two character sprite folders
        var p1 = AssetDatabase.LoadAssetAtPath<Object>("Assets/Art/Sprites/Player1_Adventurer");
        var p2 = AssetDatabase.LoadAssetAtPath<Object>("Assets/Art/Sprites/Player2_Soldier");

        var packables = new List<Object>();
        if (p1 != null) packables.Add(p1);
        if (p2 != null) packables.Add(p2);

        if (packables.Count == 0)
        {
            EditorUtility.DisplayDialog("Stick Archer",
                "Could not find character sprite folders to add to the atlas.\n\n" +
                "Expected: Assets/Art/Sprites/Player1_Adventurer and Player2_Soldier.",
                "OK");
            return;
        }

        atlas.Add(packables.ToArray());

        // Mobile-tuned settings
        var packSettings = atlas.GetPackingSettings();
        packSettings.padding = 4;
        packSettings.enableRotation = false;
        packSettings.enableTightPacking = false;
        atlas.SetPackingSettings(packSettings);

        var textureSettings = atlas.GetTextureSettings();
        textureSettings.generateMipMaps = false;
        textureSettings.sRGB = true;
        textureSettings.filterMode = FilterMode.Bilinear;
        atlas.SetTextureSettings(textureSettings);

        // Android-specific texture settings
        var androidSettings = new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = 2048,
            format = TextureImporterFormat.ASTC_6x6,
            compressionQuality = 50,
        };
        atlas.SetPlatformSettings(androidSettings);

        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);

        Debug.Log($"[PerformanceSetup] Sprite atlas {(created ? "created" : "updated")}: {ATLAS_PATH}");
        EditorUtility.DisplayDialog("Stick Archer",
            $"Sprite atlas {(created ? "created" : "updated")} at:\n{ATLAS_PATH}\n\n" +
            "Sprites are now packed for fewer draw calls on Android.",
            "OK");
    }

    [MenuItem("Stick Archer/Performance/Apply Android Build Settings")]
    public static void ApplyAndroidBuildSettings()
    {
        // Graphics APIs: prefer Vulkan, fall back to GLES3
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
        {
            UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
            UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
        });
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

        // Multithreaded rendering
        PlayerSettings.MTRendering = true;
#pragma warning disable 0618
        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
#pragma warning restore 0618

        // Sensible mobile defaults
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0+
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // Drop unused color space conversions
        PlayerSettings.colorSpace = ColorSpace.Linear;

        AssetDatabase.SaveAssets();
        Debug.Log("[PerformanceSetup] Android build settings updated for mid-range mobile target.");
        EditorUtility.DisplayDialog("Stick Archer",
            "Android build settings applied:\n\n" +
            "• Graphics APIs: Vulkan (fallback GLES3)\n" +
            "• Multithreaded rendering: ON\n" +
            "• Architecture: ARM64\n" +
            "• Min SDK: Android 7.0 (API 24)\n" +
            "• Color space: Linear\n\n" +
            "Verify under Edit → Project Settings → Player → Android.",
            "OK");
    }
}
#endif
