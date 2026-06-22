using UnityEngine;

/// <summary>
/// A bird that flies across the screen as a dynamic obstacle.
/// Arrows deflect off it (they don't stick). The bird is destroyed on hit.
/// Attach to any simple 2D bird sprite GameObject.
/// The GameObject needs a Collider2D (trigger) — added automatically if missing.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BirdController : MonoBehaviour
{
    [Header("Movement")]
    public float speed      = 4f;
    public int   direction  = 1;   // 1 = left→right,  -1 = right→left
    public float bobHeight  = 0.12f;
    public float bobSpeed   = 6f;

    [Header("Visual")]
    public Color birdColor = new Color(0.2f, 0.7f, 0.25f);

    private Rigidbody2D rb;
    private bool        isDead   = false;
    private float       startY;
    private float       bobPhase;
    private Transform   wingTransform;
    private Vector3     wingBaseScale;

    void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic  = true;

        // Auto-add collider trigger if missing
        if (GetComponent<Collider2D>() == null)
        {
            var c = gameObject.AddComponent<CircleCollider2D>();
            c.radius    = 0.3f;
            c.isTrigger = true;
        }
        else
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        // Auto-build a minimal bird visual if no SpriteRenderer is present
        if (GetComponent<SpriteRenderer>() == null)
            BuildBirdVisual();

        startY   = transform.position.y;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        // Flip sprite for rightward-flying birds
        if (direction == 1)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                                               transform.localScale.y, 1f);
    }

    void Update()
    {
        if (isDead) return;

        // Horizontal travel
        transform.position += Vector3.right * direction * speed * Time.deltaTime;

        // Smooth vertical bob
        bobPhase += Time.deltaTime * bobSpeed;
        float newY = startY + Mathf.Sin(bobPhase) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Wing flap — gentle Y scale pulse. When wingTransform == this transform
        // (sprite bird), keep the range subtle; for child-wing fallback, it's more dramatic.
        if (wingTransform != null)
        {
            float flap = Mathf.Sin(bobPhase * 1.6f) * 0.5f + 0.5f; // 0..1
            float yScale = (wingTransform == transform)
                ? Mathf.Lerp(0.92f, 1.08f, flap)   // Subtle for whole-bird
                : Mathf.Lerp(0.65f, 1.15f, flap);   // Dramatic for wing child
            wingTransform.localScale = new Vector3(wingBaseScale.x,
                                                    wingBaseScale.y * yScale,
                                                    wingBaseScale.z);
        }

        // Destroy once fully off screen
        if (Mathf.Abs(transform.position.x) > 16f)
            Destroy(gameObject);
    }

    /// <summary>Called by Arrow / ArrowLocal when an arrow collides with this bird.</summary>
    public void OnArrowHit(Vector2 arrowVelocity)
    {
        if (isDead) return;
        isDead = true;

        // Switch to physics-driven fall
        rb.isKinematic  = false;
        rb.gravityScale = 1.5f;
        rb.velocity     = new Vector2(arrowVelocity.x * 0.2f, 2f);
        rb.angularVelocity = Random.Range(-300f, 300f);

        // Disable trigger so no further arrow interactions
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Fade out and destroy
        StartCoroutine(FadeAndDestroy(1.2f));
    }

    System.Collections.IEnumerator FadeAndDestroy(float duration)
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = 1f - (elapsed / duration);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = new Color(startColors[i].r, startColors[i].g, startColors[i].b, a);
            yield return null;
        }
        Destroy(gameObject);
    }

    void BuildBirdVisual()
    {
        // Try loading real bird sprite (Kenney parrot, 137px)
        Sprite birdSprite = Resources.Load<Sprite>("Sprites/bird");
        if (birdSprite == null)
        {
            var tex = Resources.Load<Texture2D>("Sprites/bird");
            if (tex != null)
                birdSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 128f);
        }

        if (birdSprite != null)
        {
            // Single sprite bird — Kenney parrot, show natural colors
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = birdSprite;
            sr.color = Color.white; // No tint — use Kenney's original colors
            sr.sortingOrder = 15;
            // Kenney parrot: 137px at 128 PPU = ~1.07 units. Scale to 0.85 for a visible bird on 16:9.
            transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            // Use the root transform for flap animation (scale the whole bird gently)
            wingTransform = transform;
            wingBaseScale = transform.localScale;
        }
        else
        {
            // Fallback: procedural bird from colored shapes
            var body = new GameObject("BirdBody");
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.45f, 0.28f, 1f);
            var bsr = body.AddComponent<SpriteRenderer>();
            bsr.sprite = WhiteSquareSpriteCache.Get();
            bsr.color = birdColor;
            bsr.sortingOrder = 15;

            var head = new GameObject("BirdHead");
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0.22f, 0.10f, 0f);
            head.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
            var hsr = head.AddComponent<SpriteRenderer>();
            hsr.sprite = WhiteSquareSpriteCache.Get();
            hsr.color = birdColor * 1.15f;
            hsr.sortingOrder = 16;

            var wing = new GameObject("BirdWing");
            wing.transform.SetParent(transform, false);
            wing.transform.localPosition = new Vector3(-0.05f, 0.15f, 0f);
            wing.transform.localScale = new Vector3(0.38f, 0.14f, 1f);
            var wsr = wing.AddComponent<SpriteRenderer>();
            wsr.sprite = WhiteSquareSpriteCache.Get();
            wsr.color = birdColor * 0.8f;
            wsr.sortingOrder = 14;

            wingTransform = wing.transform;
            wingBaseScale = wing.transform.localScale;
        }
    }
}
