using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates the app's brand assets - a bow-and-arrow emblem on the game's
/// gold-on-navy palette - and wires them into the Android player settings.
///
/// The project previously shipped with NO launcher icon (all icon slots empty)
/// and a placeholder package id. Run <b>Tools ▸ Branding ▸ Generate Icon + Logo</b>
/// once in the editor to fix both. It writes:
///   • Assets/Art/Branding/app_icon.png      (1024² square, legacy + adaptive fg)
///   • Assets/Art/Branding/app_icon_bg.png   (1024² gradient, adaptive background)
///   • Assets/Resources/UI/logo.png          (wide emblem sprite for in-game use)
/// then assigns the Android icons and sets the application identifier.
/// </summary>
public static class BrandingSetup
{
    const string BrandDir   = "Assets/Art/Branding";
    const string IconPath   = BrandDir + "/app_icon.png";
    const string IconBgPath = BrandDir + "/app_icon_bg.png";
    const string LogoPath   = "Assets/Resources/UI/logo.png";
    const string PackageId  = "com.stickarcher.battle";

    // Palette (matches Documentation gold #FFD933 on navy #0A0E1C).
    static readonly Color Gold      = new Color(1.00f, 0.85f, 0.20f, 1f);
    static readonly Color GoldDeep  = new Color(0.90f, 0.62f, 0.10f, 1f);
    static readonly Color NavyTop   = new Color(0.090f, 0.130f, 0.245f, 1f);
    static readonly Color NavyBot   = new Color(0.039f, 0.055f, 0.110f, 1f);
    static readonly Color Outline   = new Color(0.227f, 0.133f, 0.00f, 1f);

    [MenuItem("Tools/Branding/Generate Icon + Logo")]
    public static void Generate()
    {
        Directory.CreateDirectory(BrandDir);
        Directory.CreateDirectory(Path.GetDirectoryName(LogoPath));

        const int S = 1024;

        // --- App icon (full-bleed gradient badge + emblem) ---
        var icon = NewTex(S, S);
        FillGradient(icon, NavyTop, NavyBot);
        DrawGoldRing(icon, S);
        DrawEmblem(icon, S * 0.5f, S * 0.5f, S * 0.30f, withOutline: true);
        WritePng(icon, IconPath);

        // --- Adaptive-icon background (just the gradient, no emblem) ---
        var bg = NewTex(S, S);
        FillGradient(bg, NavyTop, NavyBot);
        WritePng(bg, IconBgPath);

        // --- In-game logo (transparent, wider, emblem only) ---
        var logo = NewTex(S, S);
        Clear(logo);
        DrawEmblem(logo, S * 0.5f, S * 0.5f, S * 0.34f, withOutline: true);
        WritePng(logo, LogoPath);

        AssetDatabase.Refresh();

        ConfigureImporter(IconPath, sprite: false);
        ConfigureImporter(IconBgPath, sprite: false);
        ConfigureImporter(LogoPath, sprite: true);

        AssignAndroidIcons();
        SetPackageId();

        AssetDatabase.SaveAssets();
        Debug.Log($"[BrandingSetup] Generated icon + logo, assigned Android icons, " +
                  $"set application id to '{PackageId}'. Logo sprite at Resources/UI/logo.");
    }

    // ---- Drawing -------------------------------------------------------------

    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    static void Clear(Texture2D t)
    {
        var px = new Color[t.width * t.height];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
        t.SetPixels(px); t.Apply();
    }

    static void FillGradient(Texture2D t, Color top, Color bot)
    {
        int w = t.width, h = t.height;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            Color row = Color.Lerp(bot, top, v);
            for (int x = 0; x < w; x++) px[y * w + x] = row;
        }
        t.SetPixels(px); t.Apply();
    }

    static void DrawGoldRing(Texture2D t, float s)
    {
        float cx = s * 0.5f, cy = s * 0.5f;
        float r = s * 0.46f, thick = s * 0.012f;
        for (int y = 0; y < t.height; y++)
        for (int x = 0; x < t.width; x++)
        {
            float d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - r);
            float a = 1f - Mathf.Clamp01((d - thick) / 2f);
            if (a > 0f) Blend(t, x, y, new Color(Gold.r, Gold.g, Gold.b, a * 0.5f));
        }
    }

    /// <summary>Stylised bow (arc + string) drawing an arrow to the right.</summary>
    static void DrawEmblem(Texture2D t, float cx, float cy, float scale, bool withOutline)
    {
        // Bow geometry: arc opening to the right, string between its tips, arrow through it.
        Vector2 bowC = new Vector2(cx - scale * 0.35f, cy);
        float bowR = scale;
        float a0 = -65f * Mathf.Deg2Rad, a1 = 65f * Mathf.Deg2Rad;
        Vector2 tipTop = bowC + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * bowR;
        Vector2 tipBot = bowC + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * bowR;

        float bowW = scale * 0.085f;
        float strW = scale * 0.030f;
        float arrW = scale * 0.060f;

        Vector2 arrowTail = new Vector2(cx - scale * 0.55f, cy);
        Vector2 arrowTip  = new Vector2(cx + scale * 0.95f, cy);
        Vector2 headUp    = arrowTip + new Vector2(-scale * 0.22f,  scale * 0.18f);
        Vector2 headDn    = arrowTip + new Vector2(-scale * 0.22f, -scale * 0.18f);
        // Fletching at the tail.
        Vector2 fletchU   = arrowTail + new Vector2( scale * 0.16f,  scale * 0.14f);
        Vector2 fletchD   = arrowTail + new Vector2( scale * 0.16f, -scale * 0.14f);

        float outline = withOutline ? Mathf.Max(2f, scale * 0.018f) : 0f;

        for (int y = 0; y < t.height; y++)
        for (int x = 0; x < t.width; x++)
        {
            Vector2 p = new Vector2(x, y);
            float d = float.MaxValue;
            d = Mathf.Min(d, DistArc(p, bowC, bowR, a0, a1) - bowW);
            d = Mathf.Min(d, DistSeg(p, tipTop, tipBot) - strW);
            d = Mathf.Min(d, DistSeg(p, arrowTail, arrowTip) - arrW * 0.5f);
            d = Mathf.Min(d, DistSeg(p, arrowTip, headUp)   - arrW * 0.5f);
            d = Mathf.Min(d, DistSeg(p, arrowTip, headDn)   - arrW * 0.5f);
            d = Mathf.Min(d, DistSeg(p, arrowTail, fletchU) - strW);
            d = Mathf.Min(d, DistSeg(p, arrowTail, fletchD) - strW);

            if (outline > 0f)
            {
                float oa = 1f - Mathf.Clamp01((d - outline) / 2f);
                if (oa > 0f) Blend(t, x, y, new Color(Outline.r, Outline.g, Outline.b, oa));
            }

            float fill = 1f - Mathf.Clamp01(d / 2f);
            if (fill > 0f)
            {
                // Subtle vertical gold sheen.
                Color g = Color.Lerp(GoldDeep, Gold, Mathf.InverseLerp(cy - scale, cy + scale, y));
                Blend(t, x, y, new Color(g.r, g.g, g.b, fill));
            }
        }
    }

    static float DistSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a, ap = p - a;
        float h = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
        return Vector2.Distance(p, a + ab * h);
    }

    static float DistArc(Vector2 p, Vector2 c, float r, float a0, float a1)
    {
        Vector2 d = p - c;
        float ang = Mathf.Atan2(d.y, d.x);
        if (ang >= a0 && ang <= a1)
            return Mathf.Abs(d.magnitude - r);
        // Outside the arc span - clamp to nearest endpoint.
        Vector2 e0 = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r;
        Vector2 e1 = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
        return Mathf.Min(Vector2.Distance(p, e0), Vector2.Distance(p, e1));
    }

    static void Blend(Texture2D t, int x, int y, Color c)
    {
        Color o = t.GetPixel(x, y);
        float a = c.a + o.a * (1f - c.a);
        Color outc = a <= 0f ? new Color(0, 0, 0, 0)
            : new Color(
                (c.r * c.a + o.r * o.a * (1f - c.a)) / a,
                (c.g * c.a + o.g * o.a * (1f - c.a)) / a,
                (c.b * c.a + o.b * o.a * (1f - c.a)) / a,
                a);
        t.SetPixel(x, y, outc);
    }

    static void WritePng(Texture2D t, string path)
    {
        t.Apply();
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    // ---- Import + player settings -------------------------------------------

    static void ConfigureImporter(string path, bool sprite)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
    }

    static void AssignAndroidIcons()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null) { Debug.LogError("[BrandingSetup] icon failed to import"); return; }

        // One emblem texture applied across every required legacy size. This is the
        // simplest API that works across Unity 2022.3 and reliably gives the app an
        // actual launcher icon (replacing the previously-empty slots).
#pragma warning disable 618
        int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
        var arr = new Texture2D[sizes.Length];
        for (int i = 0; i < arr.Length; i++) arr[i] = icon;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, arr);
#pragma warning restore 618
    }

    static void SetPackageId()
    {
        if (PlayerSettings.applicationIdentifier == "com.yourcompany.stickarchers" ||
            string.IsNullOrEmpty(PlayerSettings.applicationIdentifier))
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
        }
    }
}
