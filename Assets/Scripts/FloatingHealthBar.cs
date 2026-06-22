using UnityEngine;

/// <summary>
/// A small health bar that floats above an archer's head (reference-game style).
/// It lives in world space as its own root object (NOT parented to the archer) so
/// it never inherits the archer's aim-lean / flinch rotation — it always stays
/// level and simply tracks the head position each LateUpdate.
/// </summary>
public class FloatingHealthBar : MonoBehaviour
{
    [Header("Layout")]
    public float width       = 0.95f;
    public float height      = 0.13f;
    public float heightAbove = 0.30f;   // gap between sprite top and bar
    public int   sortingOrder = 25;

    Transform      _bar;       // world-space holder
    Transform      _fill;
    SpriteRenderer _fillSr;
    Transform      _target;
    float          _ratio   = 1f;
    bool           _visible = true;

    static Sprite _sq;

    public void Init(Transform target)
    {
        _target = target;
        BuildIfNeeded();
        SetRatio(1f);
        Show(true);
    }

    void BuildIfNeeded()
    {
        if (_bar != null) return;
        if (_sq == null) _sq = Resources.Load<Sprite>("_WhiteSquare");

        var root = new GameObject("FloatingHealthBar");
        _bar = root.transform;

        MakeQuad(_bar, "BG",    new Color(0.05f, 0.06f, 0.09f, 0.92f), sortingOrder,     width + 0.07f, height + 0.07f);
        MakeQuad(_bar, "Track", new Color(0.20f, 0.22f, 0.27f, 1f),    sortingOrder + 1, width,          height);
        _fill   = MakeQuad(_bar, "Fill", HpColor(1f),                  sortingOrder + 2, width,          height);
        _fillSr = _fill.GetComponent<SpriteRenderer>();
    }

    Transform MakeQuad(Transform parent, string name, Color c, int order, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = _sq;
        sr.color        = c;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(w, h, 1f);
        return go.transform;
    }

    /// <summary>Set fill from a 0..1 ratio. Fill shrinks from the right (anchored left).</summary>
    public void SetRatio(float r)
    {
        _ratio = Mathf.Clamp01(r);
        if (_fill == null) return;
        _fill.localScale    = new Vector3(width * _ratio, height, 1f);
        _fill.localPosition = new Vector3(-width * 0.5f + width * _ratio * 0.5f, 0f, 0f);
        if (_fillSr != null) _fillSr.color = HpColor(_ratio);
    }

    public void SetHealth(float current, float max)
    {
        SetRatio(max > 0.001f ? current / max : 0f);
    }

    public void Show(bool on)
    {
        _visible = on;
        if (_bar != null) _bar.gameObject.SetActive(on);
    }

    static Color HpColor(float r)
    {
        // Red (low) -> amber (mid) -> green (full), matching the HUD bar feel.
        return r < 0.5f
            ? Color.Lerp(new Color(0.85f, 0.25f, 0.22f), new Color(0.92f, 0.74f, 0.20f), r * 2f)
            : Color.Lerp(new Color(0.92f, 0.74f, 0.20f), new Color(0.38f, 0.80f, 0.34f), (r - 0.5f) * 2f);
    }

    void LateUpdate()
    {
        if (_bar == null) return;
        if (_target == null) { _bar.gameObject.SetActive(false); return; }
        if (!_visible) return;

        float topY;
        var spriteChild = _target.Find("__Sprite");
        var sr = spriteChild != null ? spriteChild.GetComponent<SpriteRenderer>() : null;
        if (sr != null && sr.enabled && sr.sprite != null)
            topY = sr.bounds.max.y;
        else
        {
            var col = _target.GetComponent<Collider2D>();
            topY = col != null ? col.bounds.max.y : _target.position.y + 1.2f;
        }

        _bar.position = new Vector3(_target.position.x, topY + heightAbove, 0f);
        _bar.rotation = Quaternion.identity;
    }

    void OnDestroy()
    {
        if (_bar != null) Destroy(_bar.gameObject);
    }
}
