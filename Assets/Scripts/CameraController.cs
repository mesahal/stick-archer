using UnityEngine;

/// <summary>
/// Keeps the camera centred between both archers.
/// Falls back to a fixed position if no archers are found.
/// </summary>
public class CameraController : MonoBehaviour
{
    public float smoothSpeed  = 5f;
    public float minX = -6f;
    public float maxX =  6f;
    public float fixedY = 1f;
    public float fixedZ = -10f;

    void LateUpdate()
    {
        var archers = FindObjectsOfType<Archer>();

        Vector3 target;
        if (archers.Length >= 2)
        {
            Vector3 mid = (archers[0].transform.position + archers[1].transform.position) / 2f;
            target = new Vector3(Mathf.Clamp(mid.x, minX, maxX), fixedY, fixedZ);
        }
        else if (archers.Length == 1)
        {
            target = new Vector3(archers[0].transform.position.x, fixedY, fixedZ);
        }
        else
        {
            target = new Vector3(0, fixedY, fixedZ);
        }

        transform.position = Vector3.Lerp(transform.position, target, smoothSpeed * Time.deltaTime);
    }
}
