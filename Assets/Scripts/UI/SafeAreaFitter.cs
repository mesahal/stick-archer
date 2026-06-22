using UnityEngine;

/// <summary>
/// Resizes this RectTransform to the device safe area (notches, rounded
/// corners, gesture bars). Put each screen's interactive content under a child
/// with this component; keep full-bleed backgrounds OUTSIDE it so the art still
/// fills the screen.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform _rt;
    Rect _lastSafe;
    Vector2Int _lastRes;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != _lastSafe ||
            Screen.width != _lastRes.x || Screen.height != _lastRes.y)
            Apply();
    }

    void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

        _lastSafe = Screen.safeArea;
        _lastRes = new Vector2Int(Screen.width, Screen.height);

        Vector2 min = _lastSafe.position;
        Vector2 max = _lastSafe.position + _lastSafe.size;
        min.x /= Screen.width;  min.y /= Screen.height;
        max.x /= Screen.width;  max.y /= Screen.height;

        if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y))
            return;

        _rt.anchorMin = min;
        _rt.anchorMax = max;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
