using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight screen-space confetti for the victory screen. Attach to the
/// full-screen "VictoryEffects" object that UIManager.ShowResult() toggles on a
/// win. Pieces are simple tinted Images (no sprite needed) that fall and spin,
/// recycling to the top so it loops while active.
///
/// SETUP: this object's RectTransform should stretch the full screen
/// (anchors 0,0–1,1, offsets 0). It builds its pieces on first enable.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class ConfettiBurst : MonoBehaviour
{
    [Header("Amount & motion")]
    public int pieceCount = 40;
    public float fallSpeedMin = 200f;
    public float fallSpeedMax = 520f;
    public float spinSpeedMax = 360f;
    public Vector2 pieceSize = new Vector2(16f, 24f);

    [Header("Colors (design palette)")]
    public Color[] colors = {
        new Color(1f, 0.85f, 0.20f),    // gold
        new Color(0.20f, 0.72f, 0.35f), // green
        new Color(0.15f, 0.55f, 0.95f), // blue
        new Color(0.95f, 0.25f, 0.25f), // red
    };

    RectTransform _rt;
    readonly List<Piece> _pieces = new List<Piece>();
    bool _built;

    class Piece { public RectTransform rt; public float fall; public float spin; }

    void OnEnable()
    {
        _rt = (RectTransform)transform;
        if (!_built) Build();
        Scatter();
    }

    void Build()
    {
        for (int i = 0; i < pieceCount; i++)
        {
            var go = new GameObject("confetti", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_rt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = pieceSize;

            var img = go.GetComponent<Image>();
            img.color = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
            img.raycastTarget = false;

            _pieces.Add(new Piece { rt = rt });
        }
        _built = true;
    }

    void Scatter()
    {
        float w = _rt.rect.width, h = _rt.rect.height;
        foreach (var p in _pieces)
        {
            p.rt.anchoredPosition = new Vector2(Random.Range(-w * 0.5f, w * 0.5f),
                                                Random.Range(-h * 0.5f, h * 0.5f));
            p.rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
            p.fall = Random.Range(fallSpeedMin, fallSpeedMax);
            p.spin = Random.Range(-spinSpeedMax, spinSpeedMax);
        }
    }

    void Update()
    {
        float w = _rt.rect.width, h = _rt.rect.height;
        float dt = Time.unscaledDeltaTime;
        foreach (var p in _pieces)
        {
            var pos = p.rt.anchoredPosition;
            pos.y -= p.fall * dt;
            if (pos.y < -h * 0.5f - 40f)
            {
                pos.y = h * 0.5f + Random.Range(0f, 60f);
                pos.x = Random.Range(-w * 0.5f, w * 0.5f);
            }
            p.rt.anchoredPosition = pos;
            p.rt.Rotate(0f, 0f, p.spin * dt);
        }
    }
}
