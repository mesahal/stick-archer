using UnityEngine;

/// <summary>
/// Snap an archer so its feet (collider bottom, or sprite bottom as a fallback)
/// rest exactly on a spawn point's Y, instead of having the transform pivot
/// land at that Y and leave the visual floating.
/// </summary>
public static class SpawnAlignment
{
    public static Vector3 AlignFeetTo(GameObject go, Vector3 spawnPoint)
    {
        if (go == null) return spawnPoint;

        // Provisionally move so we can read bounds in the target X column.
        // Bounds are world-space and depend on transform position, so set X/Z first.
        var t = go.transform;
        Vector3 provisional = new Vector3(spawnPoint.x, t.position.y, spawnPoint.z);
        t.position = provisional;

        float feetY = float.NaN;

        // Prefer the main collider (excludes ragdoll children — they're disabled until death).
        var col = go.GetComponent<Collider2D>();
        if (col != null && col.enabled)
            feetY = col.bounds.min.y;
        else
        {
            // Fallback: tightest visible sprite bottom on the archer itself (not children,
            // to avoid picking up AimLine / FX renderers placed elsewhere).
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                feetY = sr.bounds.min.y;
        }

        if (float.IsNaN(feetY))
            return new Vector3(spawnPoint.x, spawnPoint.y, spawnPoint.z);

        float delta = t.position.y - feetY;       // how far the pivot sits above feet
        return new Vector3(spawnPoint.x, spawnPoint.y + delta, spawnPoint.z);
    }
}
