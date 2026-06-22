using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Articulated 2D ragdoll: on death the single-sprite archer is hidden and replaced
/// by a jointed skeleton (head, torso, two arms, two legs) connected with HingeJoint2D.
/// The parts are tinted to match the character, launch with the killing blow, flail on
/// their joints and tumble/bounce off the platforms — the reference game's signature
/// floppy death.
///
/// The skeleton is built under its own world-space root (NOT parented to the archer)
/// so it never inherits the archer transform's mirror-scale or aim-lean rotation.
/// </summary>
public class Ragdoll2D : MonoBehaviour
{
    [Header("Death Settings")]
    public float fadeOutDelay     = 3.0f;
    public float fadeOutDuration  = 1.0f;
    public bool  autoDestroy      = true;

    GameObject _root;
    readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
    readonly List<Collider2D>     _colliders = new List<Collider2D>();
    Rigidbody2D _torso;
    bool _isActive;

    static Sprite _square;
    static Sprite _circle;

    // ── Public API (kept stable for both ArcherLocal and online Archer) ──

    public bool IsActive() => _isActive;

    /// <summary>Spawn the ragdoll at the archer's body, launch it with the impact.</summary>
    public void Activate(Vector3 force, Vector3 hitPoint)
    {
        if (_isActive) return;
        _isActive = true;

        EnsureSprites();
        ResolveColors(out Color skin, out Color shirt, out Color pants);
        float h = ResolveHeight();

        Vector3 feet   = transform.position;
        Vector3 center = feet + Vector3.up * (h * 0.48f);

        Build(center, h, skin, shirt, pants);
        Launch(force, hitPoint);

        if (autoDestroy) StartCoroutine(FadeOutAndCleanup());
    }

    /// <summary>Destroy the ragdoll skeleton immediately (called before Respawn()).</summary>
    public void ForceCleanup()
    {
        StopAllCoroutines();
        if (_root != null) Destroy(_root);
        _root = null;
        _isActive = false;
    }

    void OnDestroy()
    {
        if (_root != null) Destroy(_root);
    }

    // ── Build ────────────────────────────────────────────────────────

    void Build(Vector3 center, float h, Color skin, Color shirt, Color pants)
    {
        float s = h / 1.5f; // proportion scale relative to the 1.5-unit reference build

        _root = new GameObject("RagdollRoot");
        _root.transform.position = center;

        // Torso (root body)
        _torso = MakeBox("Torso", center, new Vector2(0.34f, 0.52f) * s, shirt, 2.0f * s, 13);

        // Head
        var head = MakeCircle("Head", center + new Vector3(0f, 0.42f * s, 0f), 0.18f * s, skin, 1.0f * s, 14);
        Join(head, _torso, new Vector2(0f, -0.16f * s), new Vector2(0f, 0.26f * s), -35f, 35f);

        // Arms (sleeves = shirt colour)
        var armL = MakeBox("ArmL", center + new Vector3(-0.26f * s, 0.12f * s, 0f), new Vector2(0.12f, 0.42f) * s, shirt, 0.5f * s, 15);
        Join(armL, _torso, new Vector2(0.09f * s, 0.12f * s), new Vector2(-0.17f * s, 0.22f * s), -110f, 110f);
        var armR = MakeBox("ArmR", center + new Vector3(0.26f * s, 0.12f * s, 0f), new Vector2(0.12f, 0.42f) * s, shirt, 0.5f * s, 15);
        Join(armR, _torso, new Vector2(-0.09f * s, 0.12f * s), new Vector2(0.17f * s, 0.22f * s), -110f, 110f);

        // Legs (pants = dark)
        var legL = MakeBox("LegL", center + new Vector3(-0.10f * s, -0.46f * s, 0f), new Vector2(0.14f, 0.50f) * s, pants, 0.7f * s, 12);
        Join(legL, _torso, new Vector2(0f, 0.20f * s), new Vector2(-0.10f * s, -0.26f * s), -70f, 70f);
        var legR = MakeBox("LegR", center + new Vector3(0.10f * s, -0.46f * s, 0f), new Vector2(0.14f, 0.50f) * s, pants, 0.7f * s, 12);
        Join(legR, _torso, new Vector2(0f, 0.20f * s), new Vector2(0.10f * s, -0.26f * s), -70f, 70f);

        // Stop the parts from colliding with each other (avoids jitter/explosion) while
        // still colliding with the ground/platforms.
        for (int i = 0; i < _colliders.Count; i++)
            for (int j = i + 1; j < _colliders.Count; j++)
                if (_colliders[i] != null && _colliders[j] != null)
                    Physics2D.IgnoreCollision(_colliders[i], _colliders[j], true);
    }

    Rigidbody2D MakeBox(string name, Vector3 worldPos, Vector2 size, Color color, float mass, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root.transform, true);
        go.transform.position = worldPos;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = Mathf.Max(0.05f, mass);
        rb.gravityScale = 1.7f;
        rb.angularDrag = 0.6f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        _colliders.Add(col);

        AddVisual(go.transform, size, _square, color, order);
        return rb;
    }

    Rigidbody2D MakeCircle(string name, Vector3 worldPos, float radius, Color color, float mass, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root.transform, true);
        go.transform.position = worldPos;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = Mathf.Max(0.05f, mass);
        rb.gravityScale = 1.7f;
        rb.angularDrag = 0.6f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        _colliders.Add(col);

        AddVisual(go.transform, new Vector2(radius * 2f, radius * 2f), _circle, color, order);
        return rb;
    }

    void AddVisual(Transform parent, Vector2 size, Sprite sprite, Color color, int order)
    {
        var vis = new GameObject("Vis");
        vis.transform.SetParent(parent, false);
        vis.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        _renderers.Add(sr);
    }

    void Join(Rigidbody2D part, Rigidbody2D connected, Vector2 anchorOnPart, Vector2 anchorOnConnected, float min, float max)
    {
        var j = part.gameObject.AddComponent<HingeJoint2D>();
        j.connectedBody = connected;
        j.autoConfigureConnectedAnchor = false;
        j.anchor = anchorOnPart;
        j.connectedAnchor = anchorOnConnected;
        j.useLimits = true;
        j.limits = new JointAngleLimits2D { min = min, max = max };
        j.enableCollision = false;
    }

    // ── Launch ───────────────────────────────────────────────────────

    void Launch(Vector3 force, Vector3 hitPoint)
    {
        Vector2 f = force;
        if (f.sqrMagnitude < 4f) f = new Vector2(Random.Range(-3f, 3f), 5f);

        // Drive the torso hardest so the whole body launches, then scatter the limbs and
        // add spin for a loose, flailing tumble.
        if (_torso != null)
        {
            _torso.AddForce(f, ForceMode2D.Impulse);
            _torso.AddTorque(Random.Range(-10f, 10f), ForceMode2D.Impulse);
        }

        var bodies = _root.GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in bodies)
        {
            if (rb == _torso) continue;
            rb.AddForce(f * Random.Range(0.25f, 0.6f), ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-6f, 6f), ForceMode2D.Impulse);
        }
    }

    IEnumerator FadeOutAndCleanup()
    {
        yield return new WaitForSeconds(fadeOutDelay);

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float a = 1f - (t / fadeOutDuration);
            foreach (var sr in _renderers)
                if (sr != null) { var c = sr.color; c.a = a; sr.color = c; }
            yield return null;
        }

        ForceCleanup();
    }

    // ── Helpers: resolve look from the sibling archer ────────────────

    int ResolveCharacterIndex()
    {
        var local = GetComponent<ArcherLocal>();
        if (local != null) return local.CharacterIndex;
        var online = GetComponent<Archer>();
        if (online != null) return online.CharacterIndex;
        return 0;
    }

    void ResolveColors(out Color skin, out Color shirt, out Color pants)
    {
        skin  = new Color(0.96f, 0.80f, 0.66f);
        pants = new Color(0.22f, 0.22f, 0.26f);
        // 0 = Adventurer (green), 1 = Soldier (blue)
        shirt = ResolveCharacterIndex() == 1
            ? new Color(0.20f, 0.38f, 0.72f)
            : new Color(0.27f, 0.50f, 0.33f);
    }

    float ResolveHeight()
    {
        var sc = GetComponent<ArcherSpriteController>();
        if (sc != null && sc.targetWorldHeight > 0.1f) return sc.targetWorldHeight;
        return 1.5f;
    }

    static void EnsureSprites()
    {
        if (_square == null) _square = Resources.Load<Sprite>("_WhiteSquare");
        if (_circle == null) _circle = MakeCircleSprite();
    }

    static Sprite MakeCircleSprite()
    {
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        float r = N * 0.5f;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                bool inside = dx * dx + dy * dy <= r * r;
                tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
    }
}
