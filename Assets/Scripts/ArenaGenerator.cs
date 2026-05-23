using UnityEngine;

/// <summary>
/// Generates arena layouts matching the Stick Archers Battle reference:
/// two wide building-like platforms with grey caps and thick black outlines.
/// Characters stand on top of the platform caps.
/// </summary>
public class ArenaGenerator : MonoBehaviour
{
    [Header("Arena Types")]
    public bool generateOnStart = false;
    public int arenaType = 0;
    
    [Header("Layout")]
    public float groundY = -4.5f;

    // ── Color palette (reference-accurate earth tones) ───────────
    static readonly Color PlatformFill = new Color(0.82f, 0.58f, 0.35f);  // warm tan
    static readonly Color GroundFill   = new Color(0.55f, 0.38f, 0.22f);  // dark brown
    static readonly Color CapFill      = new Color(0.50f, 0.50f, 0.50f);  // grey roof cap
    static readonly Color OutlineColor = new Color(0.08f, 0.08f, 0.10f);  // near-black
    
    const float OUTLINE = 0.10f;

    static int SafeGroundLayer
    {
        get
        {
            int l = LayerMask.NameToLayer("Ground");
            return l >= 0 ? l : 0;
        }
    }

    void Start()
    {
        if (generateOnStart)
            GenerateArena(arenaType);
    }
    
    public void GenerateArena(int type)
    {
        switch (type)
        {
            case 0: BuildBasic(); break;
            case 1: BuildTall(); break;
            case 2: BuildAsymmetric(); break;
            case 3: BuildStepped(); break;
            case 4: BuildLowWall(); break;
            case 5: BuildWide(); break;
            default: BuildBasic(); break;
        }
    }
    
    // ── Arena layouts ────────────────────────────────────────────

    void BuildBasic()
    {
        MakeGround();
        MakeBuilding(-5f, 4.0f, 3.2f, "Player1Spawn");
        MakeBuilding( 5f, 4.0f, 3.2f, "Player2Spawn");
    }
    
    void BuildTall()
    {
        MakeGround();
        MakeBuilding(-5.5f, 5.5f, 3.5f, "Player1Spawn");
        MakeBuilding( 5.5f, 5.5f, 3.5f, "Player2Spawn");
    }

    void BuildAsymmetric()
    {
        MakeGround();
        MakeBuilding(-5f, 3.0f, 3.0f, "Player1Spawn");
        MakeBuilding( 5f, 5.0f, 2.8f, "Player2Spawn");
    }

    void BuildStepped()
    {
        MakeGround();
        // Two-tier left building
        MakeBlock(-5f, groundY + 1f, 3.8f, 2f, PlatformFill);
        MakeBlock(-5f, groundY + 3.2f, 2.6f, 1.5f, PlatformFill);
        MakeCap(-5f, groundY + 3.2f + 1.5f, 2.8f);
        MakeSpawn(-5f, groundY + 3.2f + 1.5f + 0.25f, "Player1Spawn");

        // Two-tier right building
        MakeBlock(5f, groundY + 1f, 3.8f, 2f, PlatformFill);
        MakeBlock(5f, groundY + 3.2f, 2.6f, 1.5f, PlatformFill);
        MakeCap(5f, groundY + 3.2f + 1.5f, 2.8f);
        MakeSpawn(5f, groundY + 3.2f + 1.5f + 0.25f, "Player2Spawn");
    }

    void BuildLowWall()
    {
        MakeGround();
        MakeBuilding(-5f, 3.5f, 3.0f, "Player1Spawn");
        MakeBuilding( 5f, 3.5f, 3.0f, "Player2Spawn");
        // Middle barrier
        MakeBlock(0, groundY + 1f, 0.8f, 2f, GroundFill);
    }

    void BuildWide()
    {
        MakeGround();
        MakeBuilding(-5.5f, 3.5f, 4.0f, "Player1Spawn");
        MakeBuilding( 5.5f, 3.5f, 4.0f, "Player2Spawn");
    }

    // ── Core building factory ────────────────────────────────────
    
    /// <summary>
    /// Creates a building: a rectangular body + grey cap on top.
    /// Spawn point placed ON TOP of the cap.
    /// </summary>
    void MakeBuilding(float x, float bodyH, float width, string spawnTag)
    {
        float bodyY = groundY + bodyH * 0.5f;
        float capH  = 0.25f;
        float capY  = groundY + bodyH; // top edge of body block

        // Body
        MakeBlock(x, bodyY, width, bodyH, PlatformFill);

        // Grey cap (slightly wider)
        MakeCap(x, capY, width + 0.3f);

        // Cap top surface = capY + capH = capY + 0.25
        // Character has GravityScale=0, so won't fall.
        // Character legs bottom is at transform.y - 0.55.
        // For feet ON cap: transform.y = capTopSurface + 0.55
        float capTopSurface = capY + capH;
        MakeSpawn(x, capTopSurface, spawnTag);
    }

    void MakeGround()
    {
        float w = 22f, h = 2f;
        float y = groundY - h * 0.5f;
        MakeBlock(0, y, w, h, GroundFill);
    }

    void MakeCap(float x, float topOfBody, float width)
    {
        float capH = 0.25f;
        float capY = topOfBody + capH * 0.5f;
        MakeBlock(x, capY, width, capH, CapFill);
    }

    void MakeSpawn(float x, float capTopSurfaceY, string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        var sp = new GameObject(tag);
        // Character legs bottom is at transform.y - 0.69 (from prefab analysis)
        // GravityScale=0, so character won't fall — must position precisely.
        // For feet ON cap surface: transform.y = capTopSurface + 0.70
        sp.transform.position = new Vector3(x, capTopSurfaceY + 0.70f, 0);
    }

    // ── Outlined block ───────────────────────────────────────────
    
    void MakeBlock(float x, float y, float w, float h, Color fill)
    {
        var go = new GameObject("Block");
        go.transform.position = new Vector3(x, y, 0);

        // Outline (near-black, slightly larger)
        var outGO = new GameObject("Outline");
        outGO.transform.SetParent(go.transform, false);
        var osr = outGO.AddComponent<SpriteRenderer>();
        osr.sprite = GetBlockSprite();
        osr.drawMode = SpriteDrawMode.Sliced;
        osr.size = new Vector2(w + OUTLINE * 2, h + OUTLINE * 2);
        osr.color = OutlineColor;
        osr.sortingOrder = -12;

        // Fill
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(go.transform, false);
        var fsr = fillGO.AddComponent<SpriteRenderer>();
        fsr.sprite = GetBlockSprite();
        fsr.drawMode = SpriteDrawMode.Sliced;
        fsr.size = new Vector2(w, h);
        fsr.color = fill;
        fsr.sortingOrder = -11;

        // Collider
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
        go.layer = SafeGroundLayer;

        // Tag for cleanup
        try { go.tag = "Arena"; } catch { /* tag may not exist */ }
    }

    // ── Sprite cache ─────────────────────────────────────────────
    static Sprite _blockSprite;
    static Sprite GetBlockSprite()
    {
        if (_blockSprite != null) return _blockSprite;
        var tex = new Texture2D(4, 4);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                tex.SetPixel(i, j, Color.white);
        tex.Apply();
        _blockSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f), 4, 0,
            SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
        return _blockSprite;
    }
}

/// <summary>Simple moving platform.</summary>
public class MovingPlatform : MonoBehaviour
{
    private Vector2 direction;
    private float distance, speed;
    private Vector2 startPos;
    private float timer;
    
    public void SetMovement(Vector2 dir, float dist, float spd)
    {
        direction = dir.normalized;
        distance = dist;
        speed = spd;
        startPos = transform.position;
    }
    
    void Update()
    {
        timer += Time.deltaTime * speed;
        transform.position = startPos + direction * (Mathf.Sin(timer) * distance);
    }
}
