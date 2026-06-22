using UnityEngine;

/// <summary>
/// Builds a layered parallax background using Kenney's Background Elements
/// and Pixel Platformer packs. All layers are tinted from a single palette
/// for visual consistency, with the silhouette sprites providing shape.
/// </summary>
public class ArenaBackground : MonoBehaviour
{
    [Header("World Dimensions")]
    public float worldWidth  = 32f;
    public float worldHeight = 14f;
    public float horizonY    = -2.5f;

    // ── Palette ──────────────────────────────────────────────────
    static readonly Color SkyTop     = new Color(0.38f, 0.70f, 0.92f); // light blue
    static readonly Color SkyBottom  = new Color(0.55f, 0.80f, 0.95f); // pale blue
    static readonly Color MtnFar     = new Color(0.40f, 0.55f, 0.70f, 0.65f); // blue-grey
    static readonly Color MtnNear    = new Color(0.30f, 0.48f, 0.55f, 0.85f); // teal-grey
    static readonly Color HillColor  = new Color(0.25f, 0.55f, 0.35f); // forest green
    static readonly Color TreeColor  = new Color(0.18f, 0.42f, 0.28f); // dark green
    static readonly Color CloudColor = new Color(1f, 1f, 1f, 0.65f);   // soft white
    static readonly Color CastleCol  = new Color(0.35f, 0.45f, 0.55f); // distant grey

    void Start()
    {
        BuildSky();
        BuildClouds();
        BuildMountains();
        BuildHills();
        BuildCastle();
        BuildGround();
    }

    // ── Sky (camera-colored solid) ──────────────────────────────
    void BuildSky()
    {
        // Just set the camera background; cheapest solution
        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = SkyTop;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // Gradient panel at bottom for horizon blend
        var go = MakeSprite("BG_SkyGrad", LoadSprite("Backgrounds/bg_sky"), -100);
        if (go != null)
        {
            ScaleToFill(go, worldWidth * 2f, worldHeight);
            go.transform.position = new Vector3(0, 0, 0);
            go.GetComponent<SpriteRenderer>().color = SkyBottom;
        }
    }

    // ── Clouds ──────────────────────────────────────────────────
    void BuildClouds()
    {
        Sprite[] cloudSprites = {
            LoadSprite("Backgrounds/cloud1"),
            LoadSprite("Backgrounds/cloud2"),
            LoadSprite("Backgrounds/cloud3"),
        };

        float[] xPositions = { -7f, -2f, 4f, 8f, -10f };
        float[] yPositions = {  3.5f, 4.5f, 3f, 4f, 5f };
        float[] scales     = {  2.5f, 3f, 2f, 2.8f, 2.2f };

        for (int i = 0; i < xPositions.Length; i++)
        {
            var sprite = cloudSprites[i % cloudSprites.Length];
            if (sprite == null) continue;

            var go = MakeSprite($"Cloud_{i}", sprite, -90);
            go.transform.position = new Vector3(xPositions[i], yPositions[i], 0);
            go.transform.localScale = Vector3.one * scales[i];
            go.GetComponent<SpriteRenderer>().color = CloudColor;
        }
    }

    // ── Mountains ────────────────────────────────────────────────
    void BuildMountains()
    {
        // Far mountains (blue-grey, larger, slower parallax)
        var farSprite = LoadSprite("Backgrounds/bg_mountains_far");
        if (farSprite != null)
        {
            PlaceSilhouette("BG_MtnFar_L", farSprite, -8f, horizonY + 3f, 6f, MtnFar, -50, false);
            PlaceSilhouette("BG_MtnFar_R", farSprite, 6f,  horizonY + 2.5f, 5f, MtnFar, -50, true);
        }

        // Near mountains (teal, smaller, faster parallax)
        var nearSprite = LoadSprite("Backgrounds/bg_mountains_near");
        if (nearSprite != null)
        {
            PlaceSilhouette("BG_MtnNear_L", nearSprite, -5f, horizonY + 1.5f, 4.5f, MtnNear, -40, false);
            PlaceSilhouette("BG_MtnNear_R", nearSprite, 7f,  horizonY + 1f, 4f, MtnNear, -40, true);
        }
    }

    // ── Hills ────────────────────────────────────────────────────
    void BuildHills()
    {
        var hillSprite = LoadSprite("Backgrounds/bg_hills");
        if (hillSprite == null) return;

        PlaceSilhouette("BG_Hill_L", hillSprite, -8f, horizonY + 0.5f, 3.5f, HillColor, -30, false);
        PlaceSilhouette("BG_Hill_R", hillSprite, 8f,  horizonY + 0.3f, 3f, HillColor, -30, true);
    }

    // ── Castle silhouette ────────────────────────────────────────
    void BuildCastle()
    {
        var castleSprite = LoadSprite("Backgrounds/castle");
        if (castleSprite == null) return;

        PlaceSilhouette("BG_Castle", castleSprite, 0f, horizonY + 2f, 3f, CastleCol, -45, false);
    }

    // ── Ground fill ──────────────────────────────────────────────
    void BuildGround()
    {
        // Solid ground color below horizon
        var groundSprite = LoadSprite("Platforms/ground_texture");
        var go = MakeSprite("BG_Ground", groundSprite, -10);
        if (go == null)
        {
            // Fallback: simple colored rectangle
            go = new GameObject("BG_Ground");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFallbackSprite();
            sr.color = new Color(0.35f, 0.25f, 0.18f);
            sr.sortingOrder = -10;
        }
        ScaleToFill(go, worldWidth * 2f, 6f);
        go.transform.position = new Vector3(0, horizonY - 4f, 0);
        go.GetComponent<SpriteRenderer>().color = new Color(0.40f, 0.30f, 0.20f);
    }

    // ── Helpers ──────────────────────────────────────────────────

    void PlaceSilhouette(string name, Sprite sprite, float x, float y,
                         float targetHeight, Color tint, int sortOrder, bool flipX)
    {
        var go = MakeSprite(name, sprite, sortOrder);
        if (go == null) return;

        float scale = targetHeight / sprite.bounds.size.y;
        go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);
        go.transform.position = new Vector3(x, y, 0);
        go.GetComponent<SpriteRenderer>().color = tint;
    }

    GameObject MakeSprite(string name, Sprite sprite, int sortOrder)
    {
        if (sprite == null) return null;
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortOrder;
        return go;
    }

    void ScaleToFill(GameObject go, float width, float height)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        float sw = sr.sprite.bounds.size.x;
        float sh = sr.sprite.bounds.size.y;
        go.transform.localScale = new Vector3(width / sw, height / sh, 1f);
    }

    static Sprite LoadSprite(string resourcePath)
    {
        // Try loading as Sprite first (requires TextureImporter type = Sprite)
        var s = Resources.Load<Sprite>(resourcePath);
        if (s != null) return s;

        // Fallback: load as Texture2D and create Sprite at runtime
        var tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null)
        {
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            return s;
        }

        Debug.LogWarning($"[ArenaBackground] Missing: Resources/{resourcePath}");
        return null;
    }

    static Sprite _fallback;
    static Sprite CreateFallbackSprite()
    {
        if (_fallback != null) return _fallback;
        var tex = new Texture2D(4, 4);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                tex.SetPixel(i, j, Color.white);
        tex.Apply();
        _fallback = Sprite.Create(tex, new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f), 4f);
        return _fallback;
    }
}
