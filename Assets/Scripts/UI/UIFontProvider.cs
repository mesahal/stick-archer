using TMPro;
using UnityEngine;

/// <summary>
/// Lazy-loads Inter TMP font assets from Resources (baked by InterFontSetup).
/// Design spec uses Inter at weights 500–900.
/// </summary>
public static class UIFontProvider
{
    public static TMP_FontAsset Black     => Load(ref _black,     "Inter Black SDF");
    public static TMP_FontAsset ExtraBold => Load(ref _extraBold, "Inter ExtraBold SDF");
    public static TMP_FontAsset Bold      => Load(ref _bold,      "Inter Bold SDF");
    public static TMP_FontAsset Medium    => Load(ref _medium,    "Inter Medium SDF");
    public static TMP_FontAsset Regular   => Load(ref _regular,   "Inter Regular SDF");

    static TMP_FontAsset _black, _extraBold, _bold, _medium, _regular;

    static TMP_FontAsset Load(ref TMP_FontAsset field, string name)
    {
        if (field == null)
            field = Resources.Load<TMP_FontAsset>($"Fonts & Materials/{name}");
        return field;
    }

    /// <summary>Assigns a weight-specific Inter face (weight is baked into the asset).</summary>
    public static void Apply(TextMeshProUGUI tmp, TMP_FontAsset font)
    {
        if (tmp == null || font == null) return;
        tmp.font = font;
        tmp.fontStyle = FontStyles.Normal;
        tmp.fontWeight = FontWeight.Regular;
    }

    /// <summary>Soft drop shadow for large display titles (matches designs titleShadow filter).</summary>
    public static void ApplyTitleDropShadow(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        Material mat = Object.Instantiate(tmp.fontSharedMaterial);
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.55f));
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.85f);
        tmp.fontMaterial = mat;
    }
}
