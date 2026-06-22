using UnityEngine;

/// <summary>
/// Rotates a UI element continuously — e.g. the lobby "searching" spinner arc
/// (use the Icons/spinner sprite). Uses unscaled time so it keeps spinning even
/// while Time.timeScale = 0.
/// </summary>
[DisallowMultipleComponent]
public class UISpinner : MonoBehaviour
{
    [Tooltip("Degrees per second. Negative spins clockwise.")]
    public float degreesPerSecond = -200f;

    void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
    }
}
