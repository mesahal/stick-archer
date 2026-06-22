#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes Inter TMP SDF assets from static TTF files (matches design spec font-family: Inter).
/// Run via Tools / Design Sync / 0 – Setup Inter Fonts, or automatically from v12 polish.
/// </summary>
public static class InterFontSetup
{
    const string FontsDir = "Assets/Art/UI/Fonts";
    const string OutDir   = "Assets/TextMesh Pro/Resources/Fonts & Materials";

    static readonly (string ttf, string asset)[] Weights =
    {
        ("Inter-Black.ttf",     "Inter Black SDF"),
        ("Inter-ExtraBold.ttf", "Inter ExtraBold SDF"),
        ("Inter-Bold.ttf",      "Inter Bold SDF"),
        ("Inter-Medium.ttf",    "Inter Medium SDF"),
        ("Inter-Regular.ttf",   "Inter Regular SDF"),
    };

    [MenuItem("Tools/Design Sync/0 – Setup Inter Fonts")]
    public static void SetupAll() => EnsureAll(force: true);

    /// <summary>Creates missing or broken Inter SDF assets. Safe to call repeatedly.</summary>
    public static void EnsureAll(bool force = false)
    {
        int created = 0;
        foreach (var (ttf, asset) in Weights)
        {
            if (EnsureOne(ttf, asset, force)) created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[InterFontSetup] Ready — {created} asset(s) created/updated.");
    }

    static bool EnsureOne(string ttfFile, string assetName, bool force)
    {
        string ttfPath = $"{FontsDir}/{ttfFile}";
        string outPath = $"{OutDir}/{assetName}.asset";

        if (!System.IO.File.Exists(ttfPath))
        {
            Debug.LogError($"[InterFontSetup] Missing {ttfPath}");
            return false;
        }

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
        if (!force && IsValid(existing))
            return false;

        AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceUpdate);
        Font source = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (source == null)
        {
            Debug.LogError($"[InterFontSetup] Could not load Font at {ttfPath}");
            return false;
        }

        if (existing != null)
            AssetDatabase.DeleteAsset(outPath);

        if (!CreateAndSaveFontAsset(source, assetName, outPath))
            return false;

        Debug.Log($"[InterFontSetup] Created {outPath}");
        return true;
    }

    /// <summary>CreateFontAsset + embed atlas texture and material as sub-assets (public TMP API only).</summary>
    static bool CreateAndSaveFontAsset(Font sourceFont, string assetName, string outPath)
    {
        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset == null)
        {
            Debug.LogError($"[InterFontSetup] TMP failed for [{sourceFont.name}] — enable Include Font Data on the TTF import.");
            return false;
        }

        fontAsset.name = assetName;
        AssetDatabase.CreateAsset(fontAsset, outPath);

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var tex = fontAsset.atlasTextures[i];
                if (tex == null) continue;
                tex.name = assetName + " Atlas" + (i > 0 ? i.ToString() : "");
                AssetDatabase.AddObjectToAsset(tex, fontAsset);
            }
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = assetName + " Atlas Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        return true;
    }

    static bool IsValid(TMP_FontAsset fa) =>
        fa != null
        && fa.material != null
        && fa.atlasTextures != null
        && fa.atlasTextures.Length > 0
        && fa.atlasTextures[0] != null;

    public static TMP_FontAsset Load(string assetName) =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{OutDir}/{assetName}.asset");
}

#endif
