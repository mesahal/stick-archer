using UnityEngine;
using System.Collections;

/// <summary>
/// Computer opponent for Practice mode.
/// Solves projectile motion to actually aim at the human player,
/// with configurable difficulty (reaction speed + accuracy noise).
/// </summary>
[RequireComponent(typeof(ArcherLocal))]
public class AIController : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard }

    [Header("Difficulty")]
    public Difficulty difficulty = Difficulty.Normal;

    [Header("Reaction Timing (seconds between shots)")]
    public float minReactionTime = 1.0f;
    public float maxReactionTime = 2.4f;

    [Header("Physics (must match Arrow prefab)")]
    [Tooltip("9.81 * Rigidbody2D.gravityScale. Arrow's gravity scale is 1.2.")]
    public float gravity = 9.81f * 1.2f;
    [Tooltip("Arrow Rigidbody2D.mass. Velocity = impulse / mass.")]
    public float arrowMass = 0.5f;

    private ArcherLocal _archer;
    private ArcherLocal _target;

    void Start()
    {
        _archer = GetComponent<ArcherLocal>();
        // Find the human archer (playerIndex == 1)
        foreach (var a in FindObjectsOfType<ArcherLocal>())
            if (a.playerIndex == 1) { _target = a; break; }

        ApplyDifficulty();
        StartCoroutine(AILoop());
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                minReactionTime = 1.6f; maxReactionTime = 3.0f; break;
            case Difficulty.Normal:
                minReactionTime = 1.0f; maxReactionTime = 2.2f; break;
            case Difficulty.Hard:
                minReactionTime = 0.6f; maxReactionTime = 1.4f; break;
        }
    }

    float AccuracyNoiseDeg()
    {
        // Random angular error added to the perfect solution.
        switch (difficulty)
        {
            case Difficulty.Easy:   return Random.Range(-12f, 12f);
            case Difficulty.Normal: return Random.Range(-5f,  5f);
            case Difficulty.Hard:   return Random.Range(-1.5f, 1.5f);
        }
        return 0f;
    }

    float ChargeFractionNoise()
    {
        // Random under/over-charge to vary shot strength.
        switch (difficulty)
        {
            case Difficulty.Easy:   return Random.Range(-0.20f, 0.10f);
            case Difficulty.Normal: return Random.Range(-0.08f, 0.05f);
            case Difficulty.Hard:   return Random.Range(-0.03f, 0.02f);
        }
        return 0f;
    }

    IEnumerator AILoop()
    {
        // Small initial delay so the scene settles
        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            if (_archer == null || _archer.isDead || _target == null || _target.isDead)
            {
                yield return new WaitForSeconds(0.4f);
                continue;
            }

            yield return new WaitForSeconds(Random.Range(minReactionTime, maxReactionTime));

            if (_archer.isDead || _target == null || _target.isDead) continue;

            // ── Solve trajectory ──────────────────────────────────
            if (_archer.arrowSpawnPoint == null) continue;

            Vector2 origin = _archer.arrowSpawnPoint.position;
            Vector2 target = _target.transform.position + new Vector3(0f, 0.4f, 0f); // aim torso

            float shootDir = _archer.playerIndex == 2 ? -1f : 1f;
            float dx = (target.x - origin.x) * shootDir;
            float dy = target.y - origin.y;

            float chosenAngleDeg;
            float chosenRatio;

            if (dx <= 0.05f)
            {
                // Target directly behind — lob overhead
                chosenAngleDeg = 80f;
                chosenRatio    = Random.Range(0.6f, 1f);
            }
            else
            {
                chosenAngleDeg = -1f;
                chosenRatio    = 1f;

                for (float ratio = 0.45f; ratio <= 1.0f + 1e-3f; ratio += 0.05f)
                {
                    float force = Mathf.Lerp(_archer.minLaunchForce, _archer.maxLaunchForce, ratio);
                    float speed = force / arrowMass;

                    if (TrySolveAngle(speed, dx, dy, gravity, out float angleDeg))
                    {
                        chosenAngleDeg = angleDeg;
                        chosenRatio    = ratio;
                        break;
                    }
                }

                if (chosenAngleDeg < 0f)
                {
                    chosenAngleDeg = Mathf.Clamp(45f + Random.Range(-10f, 10f), 5f, 85f);
                    chosenRatio    = 1f;
                }
            }

            // Add difficulty inaccuracy
            chosenAngleDeg = Mathf.Clamp(chosenAngleDeg + AccuracyNoiseDeg(), 5f, 85f);
            chosenRatio    = Mathf.Clamp01(chosenRatio + ChargeFractionNoise());

            // Directly set aim — no sway polling needed
            float rad     = chosenAngleDeg * Mathf.Deg2Rad;
            Vector2 aimDir = new Vector2(Mathf.Cos(rad) * shootDir, Mathf.Sin(rad));
            _archer.SetAimAndCharge(aimDir, chosenRatio);

            float holdSeconds = chosenRatio * _archer.maxChargeTime + 0.05f;
            yield return Fire(holdSeconds);
        }
    }

    IEnumerator Fire(float holdSeconds)
    {
        _archer.SetHoldInput(true);
        yield return new WaitForSeconds(holdSeconds);
        _archer.SetHoldInput(false);
        yield return new WaitForSeconds(0.25f);
    }

    /// <summary>
    /// Solves projectile motion for the launch angle θ that hits (dx, dy)
    /// at speed v under gravity g. Prefers the lower (flatter) of the two
    /// solutions. Returns false if the target is out of range.
    /// </summary>
    static bool TrySolveAngle(float v, float dx, float dy, float g, out float angleDeg)
    {
        angleDeg = 0f;
        if (dx <= 0f || v <= 0f) return false;

        float v2 = v * v;
        float v4 = v2 * v2;
        float disc = v4 - g * (g * dx * dx + 2f * dy * v2);
        if (disc < 0f) return false;

        float sqrtDisc = Mathf.Sqrt(disc);
        float lowAngle  = Mathf.Atan2(v2 - sqrtDisc, g * dx);
        float highAngle = Mathf.Atan2(v2 + sqrtDisc, g * dx);

        // Prefer the flatter shot if it's within the bow's allowed [5°, 85°] range
        float lowDeg  = lowAngle  * Mathf.Rad2Deg;
        float highDeg = highAngle * Mathf.Rad2Deg;

        if (lowDeg >= 5f && lowDeg <= 85f) { angleDeg = lowDeg;  return true; }
        if (highDeg >= 5f && highDeg <= 85f) { angleDeg = highDeg; return true; }
        return false;
    }
}
