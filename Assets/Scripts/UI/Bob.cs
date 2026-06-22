using UnityEngine;

/// <summary>
/// Gentle looping oscillation for a UI flourish (e.g. the menu title).
/// Attach to a RectTransform; it bobs around its starting anchored position.
/// </summary>
[DisallowMultipleComponent]
public class Bob : MonoBehaviour
{
    [Tooltip("Travel distance in UI units (reference-resolution pixels).")]
    public float amplitude = 12f;

    [Tooltip("Oscillation speed (radians/sec multiplier).")]
    public float speed = 1.5f;

    [Tooltip("Direction of the bob; normalized at runtime.")]
    public Vector2 axis = Vector2.up;

    [Tooltip("Also breathe the scale slightly.")]
    public bool useScale = false;
    public float scaleAmount = 0.03f;

    RectTransform _rt;
    Vector2 _basePos;
    Vector3 _baseScale;
    float _phase;

    void Awake()
    {
        _rt = transform as RectTransform;
        if (_rt != null) _basePos = _rt.anchoredPosition;
        _baseScale = transform.localScale;
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        float t = Mathf.Sin(Time.time * speed + _phase);
        if (_rt != null)
            _rt.anchoredPosition = _basePos + axis.normalized * (t * amplitude);
        if (useScale)
            transform.localScale = _baseScale * (1f + t * scaleAmount);
    }

    void OnDisable()
    {
        if (_rt != null) _rt.anchoredPosition = _basePos;
        transform.localScale = _baseScale;
    }
}
