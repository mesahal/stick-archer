#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
#if URP_INSTALLED
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// One-time setup for Universal Render Pipeline.
///
/// Workflow:
///   1. After URP package finishes installing (added in Packages/manifest.json),
///      Unity will compile. Once compilation succeeds, run:
///        Stick Archer → Setup URP (Mobile Profile)
///
/// What this does:
///   - Creates Assets/Settings/URP/URP_Mobile.asset (the pipeline asset)
///   - Creates Assets/Settings/URP/URP_Mobile_Renderer.asset (2D renderer data)
///   - Assigns URP_Mobile to GraphicsSettings as the active pipeline
///   - Creates a Volume Profile at Assets/Settings/URP/GlobalVolumeProfile.asset
///
/// Note: The actual creation runs only when the URP_INSTALLED define is set.
/// To set that define after URP imports, this file also includes an asmdef-free
/// version-define check by reflection. See AddDefineSymbol() below for one-shot setup.
/// </summary>
public static class SetupURP
{
    const string URP_FOLDER          = "Assets/Settings/URP";
    const string PIPELINE_ASSET_PATH = URP_FOLDER + "/URP_Mobile.asset";
    const string RENDERER_ASSET_PATH = URP_FOLDER + "/URP_Mobile_Renderer.asset";
    const string VOLUME_PROFILE_PATH = URP_FOLDER + "/GlobalVolumeProfile.asset";

    [MenuItem("Stick Archer/Setup URP (Mobile Profile)")]
    public static void Run()
    {
#if !URP_INSTALLED
        EditorUtility.DisplayDialog("Stick Archer — URP Setup",
            "The Universal Render Pipeline package is not yet active in this project.\n\n" +
            "Step 1: Open Window → Package Manager and confirm 'Universal RP' is installed.\n" +
            "Step 2: Open Project Settings → Player → Other Settings → Scripting Define Symbols, " +
            "add 'URP_INSTALLED' (Android tab), then return here and run this menu item again.\n\n" +
            "Once URP_INSTALLED is defined, this menu will build the assets and wire them up.",
            "Got it");
        AddDefineSymbol();
        return;
#else
        EnsureFolder(URP_FOLDER);

        var renderer = CreateRendererAsset();
        var pipeline = CreatePipelineAsset(renderer);
        var profile  = CreateVolumeProfile();

        // Assign the pipeline to graphics settings
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline         = pipeline;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Stick Archer — URP Setup",
            "URP configured for mobile.\n\n" +
            $"Pipeline asset:  {PIPELINE_ASSET_PATH}\n" +
            $"Renderer asset:  {RENDERER_ASSET_PATH}\n" +
            $"Volume profile:  {VOLUME_PROFILE_PATH}\n\n" +
            "Next: open each scene, add an empty GameObject, attach a Volume component, " +
            "set its profile to GlobalVolumeProfile.asset, and enable Post-Processing on Main Camera.",
            "OK");
#endif
    }

#if URP_INSTALLED
    static ScriptableRendererData CreateRendererAsset()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RENDERER_ASSET_PATH);
        if (renderer != null) return renderer;

        // 2D Renderer for sprite-based games
        renderer = ScriptableObject.CreateInstance<Renderer2DData>();
        AssetDatabase.CreateAsset(renderer, RENDERER_ASSET_PATH);
        return renderer;
    }

    static UniversalRenderPipelineAsset CreatePipelineAsset(ScriptableRendererData renderer)
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PIPELINE_ASSET_PATH);
        if (pipeline != null) return pipeline;

        pipeline = UniversalRenderPipelineAsset.Create(renderer);

        // Mobile-tuned defaults
        pipeline.msaaSampleCount = 1;          // MSAA off
        pipeline.supportsHDR     = false;
        pipeline.renderScale     = 1.0f;

        AssetDatabase.CreateAsset(pipeline, PIPELINE_ASSET_PATH);
        return pipeline;
    }

    static VolumeProfile CreateVolumeProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VOLUME_PROFILE_PATH);
        if (profile != null) return profile;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VOLUME_PROFILE_PATH);

        // Add Bloom — subtle glow on bright sprites (charged bows, trails, sparks)
        var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>();
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.10f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.60f;
        bloom.scatter.overrideState   = true; bloom.scatter.value   = 0.70f;

        // Add Vignette — focus attention center
        var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>();
        vignette.intensity.overrideState  = true; vignette.intensity.value  = 0.25f;
        vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.50f;

        // Add Color Adjustments — slightly punchier saturation/contrast for cinematic look
        var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
        color.contrast.overrideState   = true; color.contrast.value   = 10f;
        color.saturation.overrideState = true; color.saturation.value = 8f;

        // Add Chromatic Aberration — held at 0 normally; PostFXTriggers script will pulse it on hits
        var ca = profile.Add<UnityEngine.Rendering.Universal.ChromaticAberration>();
        ca.intensity.overrideState = true; ca.intensity.value = 0.05f;

        EditorUtility.SetDirty(profile);
        return profile;
    }
#endif

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    /// <summary>
    /// Adds URP_INSTALLED to the Android scripting define symbols so the
    /// URP-specific code in this file becomes active on the next compile.
    /// </summary>
    static void AddDefineSymbol()
    {
        var target  = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        if (!defines.Contains("URP_INSTALLED"))
        {
            if (!string.IsNullOrEmpty(defines)) defines += ";";
            defines += "URP_INSTALLED";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
            Debug.Log("[SetupURP] Added URP_INSTALLED define for build target " + target +
                      ". Wait for recompile, then re-run Stick Archer → Setup URP (Mobile Profile).");
        }
    }
}
#endif
