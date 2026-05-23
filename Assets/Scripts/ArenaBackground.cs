using UnityEngine;

/// <summary>
/// Creates layered background matching the Stick Archers Battle reference:
/// Camera background = sky color, then 2 layers of simple mountain silhouette sprites.
/// No complex texture generation — just solid-colored triangle shapes.
/// </summary>
public class ArenaBackground : MonoBehaviour
{
    [Header("Dimensions")]
    public float worldWidth  = 24f;
    public float worldHeight = 12f;

    void Start()
    {
        // Sky is handled by Camera.backgroundColor (set in GameArenaBootstrap)
        BuildMountainLayer(true);
        BuildMountainLayer(false);
    }

    void BuildMountainLayer(bool far)
    {
        string name  = far ? "FarMountains" : "NearMountains";
        int order    = far ? -90 : -80;
        float baseY  = far ? -3.5f : -4.5f;
        float maxH   = far ? 7f : 5f;
        int count    = far ? 7 : 9;
        Color col    = far
            ? new Color(0.30f, 0.28f, 0.22f, 0.85f)   // dark earthy brown
            : new Color(0.38f, 0.35f, 0.28f, 0.70f);   // lighter brown

        var parent = new GameObject(name);
        parent.transform.SetParent(transform);

        float spacing = worldWidth / count;
        for (int i = 0; i < count; i++)
        {
            float x = -worldWidth * 0.5f + spacing * (i + 0.5f) + Random.Range(-0.3f, 0.3f);
            float h = maxH * Random.Range(0.45f, 1.0f);
            float w = spacing * Random.Range(1.0f, 2.0f);

            CreatePeak(parent.transform, x, baseY, w, h, col, order);
        }
    }

    void CreatePeak(Transform parent, float x, float baseY, float width, float height, Color col, int sortOrder)
    {
        // Create triangle texture
        int tw = 32, th = 64;
        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // pixel-art style
        Color clear = new Color(0, 0, 0, 0);

        for (int py = 0; py < th; py++)
        {
            float t = py / (float)(th - 1);
            float halfW = (1f - t) * tw * 0.5f;
            int centerX = tw / 2;
            for (int px = 0; px < tw; px++)
            {
                float dist = Mathf.Abs(px - centerX);
                tex.SetPixel(px, py, dist <= halfW ? col : clear);
            }
        }
        tex.Apply();

        var go = new GameObject("Peak");
        go.transform.SetParent(parent);
        // Position: x centered, y so the bottom edge sits at baseY
        go.transform.position = new Vector3(x, baseY + height * 0.5f, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        // PPU = tw/width so sprite bounds.x = width naturally
        float ppu = tw / width;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), ppu);
        sr.sortingOrder = sortOrder;
        sr.sortingLayerName = "Default";

        // Scale Y to desired height. Sprite natural height = th/ppu
        float naturalH = th / ppu;
        if (naturalH > 0)
            go.transform.localScale = new Vector3(1f, height / naturalH, 1f);
    }
}
