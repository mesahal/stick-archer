using UnityEngine;

/// <summary>
/// Builds the arena: a flat green ground strip and simple flat-colour wooden
/// platforms for each archer, matching the design (designs/05_game_hud.svg).
/// All geometry is drawn from a 1×1 white-square sprite tinted per layer, so it
/// renders reliably without depending on external tile art.
/// </summary>
public class ArenaGenerator : MonoBehaviour
{
    [Header("Arena Type")]
    public bool generateOnStart = false;
    public int arenaType = 0;

    [Header("Layout")]
    public float groundY = -4.5f;

    // 1×1-unit white square used as the building block for every solid rect.
    static Sprite _fallbackSprite;

    static int SafeGroundLayer
    {
        get { int l = LayerMask.NameToLayer("Ground"); return l >= 0 ? l : 0; }
    }

    void Start()
    {
        LoadSprites();
        if (generateOnStart) GenerateArena(arenaType);
    }

    static void LoadSprites()
    {
        if (_fallbackSprite == null)
        {
            var tex = new Texture2D(4, 4);
            for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
            tex.Apply();
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect);
        }
    }

    public void GenerateArena(int type)
    {
        GenerateArena(type, Random.Range(int.MinValue, int.MaxValue));
    }

    /// <summary>Deterministic generation — both clients seeded the same get identical buildings.</summary>
    public void GenerateArena(int type, int seed)
    {
        LoadSprites();
        Random.State prev = Random.state;
        Random.InitState(seed);
        try
        {
            BuildRandomized();
            SpawnProps();
        }
        finally
        {
            Random.state = prev;
        }
    }

    // ── Layouts ──────────────────────────────────────────────────

    // Last-generated center-blocker Y, so callers (e.g. props) could align if needed.
    public float LastCenterY { get; private set; }

    /// <summary>
    /// Randomized layout (seeded): each round the two spawn platforms and the centre
    /// blocker get fresh vertical positions, like the reference game. Stays within the
    /// camera frame and keeps the spawns reachable.
    /// </summary>
    void BuildRandomized()
    {
        MakeGround();

        // Spawn platforms: independent heights (so layouts can be symmetric or skewed).
        // Wider vertical range than before so one archer is often clearly higher than the
        // other — the reference game's per-round elevation changes that make you re-judge
        // the arc (aim higher/lower) every round.
        float p1Y = Random.Range(-2.8f, 1.4f);
        float p2Y = Random.Range(-2.8f, 1.4f);
        // Allow a big, readable height gap but not an impossible one.
        if (Mathf.Abs(p1Y - p2Y) > 3.0f)
            p2Y = p1Y + Mathf.Sign(p2Y - p1Y) * 3.0f;

        MakePlatform(-5.5f, p1Y, 3.5f, "Player1Spawn");
        MakePlatform( 5.5f, p2Y, 3.5f, "Player2Spawn");

        // Centre blocker: a TALL pillar rising from the ground. Its top usually sits ABOVE
        // the line between the two archers' bows, so a flat/direct shot is blocked and the
        // player must arc the projectile over it (reference feel). Occasionally it's low
        // enough for a rare direct hit. Height varies each round.
        float bowLine   = Mathf.Max(p1Y, p2Y) + 0.7f;            // ~bow height of the higher archer
        float pillarTop = bowLine + Random.Range(-0.2f, 1.8f);   // usually above the bow line
        pillarTop = Mathf.Clamp(pillarTop, groundY + 1.4f, 3.3f);
        float pillarWidth = Random.Range(0.7f, 1.1f);
        MakeCenterPillar(0f, pillarTop, pillarWidth);
    }

    /// <summary>Tall central pillar from the ground up to <paramref name="topY"/>.</summary>
    void MakeCenterPillar(float centerX, float topY, float width)
    {
        float bottomY = groundY;
        float height  = Mathf.Max(0.6f, topY - bottomY);
        float centerY = bottomY + height * 0.5f;
        LastCenterY = topY;

        var parent = new GameObject("Platform_Center");
        parent.transform.position = new Vector3(centerX, centerY, 0f);
        try { parent.tag = "Arena"; } catch { }

        MakeRect(parent.transform, "PillarBody", Vector3.zero,
            new Vector2(width, height), 0, WoodBody);
        MakeRect(parent.transform, "PillarTop", new Vector3(0f, height * 0.5f - 0.09f, 0f),
            new Vector2(width, 0.18f), 1, WoodTop);
        MakeRect(parent.transform, "PillarEdge", new Vector3(-width * 0.5f + 0.06f, 0f, 0f),
            new Vector2(0.12f, height), 1, WoodEdge);

        var col = parent.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
        parent.layer = SafeGroundLayer;
    }

    // ── Simple wooden plank platform (flat-colour, matches the design) ──

    static readonly Color WoodBody = new Color(0.55f, 0.40f, 0.24f); // plank brown
    static readonly Color WoodTop  = new Color(0.68f, 0.52f, 0.34f); // lit top edge
    static readonly Color WoodEdge = new Color(0.38f, 0.26f, 0.15f); // shadowed bottom
    static readonly Color GrassTop = new Color(0.48f, 0.72f, 0.31f); // #7BA850

    /// <summary>
    /// Builds a single horizontal wooden platform. <paramref name="topSurfaceY"/> is
    /// the world Y of the walkable top edge; the plank hangs below it. When
    /// <paramref name="spawnTag"/> is non-null a spawn point is placed on the surface.
    /// </summary>
    void MakePlatform(float centerX, float topSurfaceY, float widthUnits, string spawnTag)
    {
        const float thickness = 0.6f;

        var parent = new GameObject(spawnTag != null ? $"Platform_{spawnTag}" : "Platform_Center");
        parent.transform.position = new Vector3(centerX, topSurfaceY, 0);
        try { parent.tag = "Arena"; } catch { }

        float bodyCenterY = -thickness * 0.5f; // local Y, below the top surface

        // Plank body
        MakeRect(parent.transform, "PlankBody",
            new Vector3(0f, bodyCenterY, 0f),
            new Vector2(widthUnits, thickness), 0, WoodBody);

        // Lit top strip
        MakeRect(parent.transform, "PlankTop",
            new Vector3(0f, -0.06f, 0f),
            new Vector2(widthUnits, 0.12f), 1, WoodTop);

        // Grass cap on top of the plank.
        MakeRect(parent.transform, "PlankGrass",
            new Vector3(0f, 0f, 0f),
            new Vector2(widthUnits, 0.15f), 2, GrassTop);

        // Shadowed bottom strip
        MakeRect(parent.transform, "PlankEdge",
            new Vector3(0f, -thickness + 0.05f, 0f),
            new Vector2(widthUnits, 0.10f), 1, WoodEdge);

        // Solid collider so arrows land and archers stand on the plank.
        var col2d = parent.AddComponent<BoxCollider2D>();
        col2d.size   = new Vector2(widthUnits, thickness);
        col2d.offset = new Vector2(0f, bodyCenterY);
        parent.layer = SafeGroundLayer;

        // Spawn point on the plank's top surface. Archers offset up by their own
        // collider bottom in Respawn(), so this is the exact standing surface.
        // Reuse a pre-placed spawn object of the same name if the scene has one
        // (GameArena ships with Player1Spawn/Player2Spawn) so GameObject.Find()
        // resolves to this platform, not a stale scene position.
        if (spawnTag != null)
        {
            var existing = GameObject.Find(spawnTag);
            var sp = existing != null ? existing : new GameObject(spawnTag);
            sp.transform.position = new Vector3(centerX, topSurfaceY, 0f);
        }
    }

    // ── Interactive physics props (crates + a teetering plank) ──────
    // Loose rigidbodies that arrows thunk into, archers get knocked into, and that
    // can topple/fall — the emergent "physics" chaos of the reference game.

    static readonly Color CrateBody = new Color(0.62f, 0.45f, 0.26f);
    static readonly Color CrateEdge = new Color(0.42f, 0.30f, 0.17f);

    void SpawnProps()
    {
        // Central stack tall enough to reach the firing lane (acts as destructible-ish
        // cover that arrows thunk into and that can topple), plus a couple of loose
        // crates and a teeter plank for emergent physics chaos.
        float top = groundY; // ground top surface

        // A couple of loose side crates for physics chaos. The centre is now the tall
        // pillar, so no central stack/plank (they'd overlap it).
        MakeCrate(new Vector3(-3.0f, top + 0.3f, 0f), 0.6f, 1f);
        MakeCrate(new Vector3( 3.0f, top + 0.3f, 0f), 0.6f, 1f);
    }

    void MakeCrate(Vector3 pos, float size, float mass)
    {
        var go = new GameObject("Crate");
        go.transform.position = pos;
        try { go.tag = "Arena"; } catch { }
        go.layer = SafeGroundLayer; // solid so arrows thunk in and archers stand/collide

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _fallbackSprite;
        sr.color = CrateBody;
        sr.sortingOrder = 3;
        go.transform.localScale = new Vector3(size, size, 1f);

        // Darker border via a slightly smaller inner highlight.
        var inner = new GameObject("CrateInner");
        inner.transform.SetParent(go.transform, false);
        inner.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
        var isr = inner.AddComponent<SpriteRenderer>();
        isr.sprite = _fallbackSprite; isr.color = CrateEdge; isr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        // Collider is in local space; scale already applied via transform, so size 1.
        col.size = Vector2.one;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = mass;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void MakePlank(Vector3 pos, Vector2 size, float mass)
    {
        var go = new GameObject("PropPlank");
        go.transform.position = pos;
        try { go.tag = "Arena"; } catch { }
        go.layer = SafeGroundLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _fallbackSprite;
        sr.color = WoodTop;
        sr.sortingOrder = 5;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = mass;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>Solid-colour rectangle built from the 1×1 white-square sprite.</summary>
    void MakeRect(Transform parent, string name, Vector3 localPos, Vector2 size, int sortOrder, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = _fallbackSprite; // bounds are exactly 1×1 unit
        sr.color        = color;
        sr.sortingOrder = sortOrder;

        go.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    // ── Ground: flat green strip across the bottom (matches the design) ──

    static readonly Color GroundGrass = new Color(0.42f, 0.62f, 0.30f); // lit grass top
    static readonly Color GroundBody  = new Color(0.34f, 0.52f, 0.24f); // green body

    void MakeGround()
    {
        const float width = 34f;   // wider than the camera view
        const float depth = 6f;    // extends below the screen

        var parent = new GameObject("Ground");
        parent.transform.position = new Vector3(0, groundY, 0);
        try { parent.tag = "Arena"; } catch { }

        // Green body
        MakeRect(parent.transform, "GroundBody",
            new Vector3(0f, -depth * 0.5f, 0f),
            new Vector2(width, depth), -1, GroundBody);

        // Lit grass top strip
        MakeRect(parent.transform, "GroundGrass",
            new Vector3(0f, -0.15f, 0f),
            new Vector2(width, 0.3f), 0, GroundGrass);

        // Collider (top aligned with groundY)
        var col2d = parent.AddComponent<BoxCollider2D>();
        col2d.size   = new Vector2(width, depth);
        col2d.offset = new Vector2(0f, -depth * 0.5f);
        parent.layer = SafeGroundLayer;
    }

}

/// <summary>Simple moving platform (kept for compatibility).</summary>
public class MovingPlatform : MonoBehaviour
{
    private Vector2 direction;
    private float distance, speed;
    private Vector2 startPos;
    private float timer;

    public void SetMovement(Vector2 dir, float dist, float spd)
    {
        direction = dir.normalized; distance = dist; speed = spd;
        startPos = transform.position;
    }

    void Update()
    {
        timer += Time.deltaTime * speed;
        transform.position = startPos + direction * (Mathf.Sin(timer) * distance);
    }
}
