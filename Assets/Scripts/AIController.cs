using UnityEngine;
using System.Collections;

/// <summary>
/// Computer opponent for Practice mode.
/// Simulates projectile trajectories in world space, accounting for gravity and wind,
/// to accurately aim at the human player's position.
/// </summary>
[RequireComponent(typeof(ArcherLocal))]
public class AIController : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard }

    [Header("Difficulty")]
    public Difficulty difficulty = Difficulty.Hard;

    [Header("Reaction Timing (seconds between shots)")]
    public float minReactionTime = 0.5f;
    public float maxReactionTime = 1.2f;

    // Bow arc limits (must match BowSwayController)
    const float MinAimAngle = 5f;
    const float MaxAimAngle = 80f;
    const float SimDt       = 0.02f; // Match Unity fixedDeltaTime
    const float LaunchOffset = 1f;

    // Same sway range as BowSwayController defaults
    const float SwayMinAngle   = 15f;
    const float SwayMaxAngle   = 75f;
    const float SwayFrequency  =  0.48f; // cycles per second

    ArcherLocal _archer;
    ArcherLocal _target;

    private float _swayPhase = 0f;
    private bool  _isFiring  = false;
    private int   _groundMask;

    void Start()
    {
        _archer = GetComponent<ArcherLocal>();

        foreach (var a in FindObjectsOfType<ArcherLocal>())
            if (a != _archer) { _target = a; break; }

        _groundMask = LayerMask.GetMask("Ground");
        if (_groundMask == 0) _groundMask = 1 << 0;

        // Random phase so AI and human don't sway in sync
        _swayPhase = Random.Range(0f, Mathf.PI * 2f);

        ApplyDifficulty();
        StartCoroutine(AILoop());
    }

    void Update()
    {
        if (_archer == null || _archer.isDead) return;

        // Advance sway phase every frame
        _swayPhase += Time.deltaTime * SwayFrequency * Mathf.PI * 2f;

        // Drive arm rotation during idle/waiting periods (not while Fire() is running)
        if (!_isFiring)
        {
            float t        = (Mathf.Sin(_swayPhase) + 1f) * 0.5f;
            float angleDeg = Mathf.Lerp(SwayMinAngle, SwayMaxAngle, t);
            Vector2 swayDir = AimDirFromAngle(angleDeg);
            _archer.SetAimAndCharge(swayDir, 0f);
        }
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                minReactionTime = 1.9f; maxReactionTime = 3.2f; break;
            case Difficulty.Normal:
                minReactionTime = 1.1f; maxReactionTime = 2.0f; break;
            case Difficulty.Hard:
                minReactionTime = 0.7f; maxReactionTime = 1.3f; break;
        }
    }

    // Effective gravity matching the arrow rigidbody's actual acceleration
    float EffectiveGravity()
    {
        // WindSystem.ApplyGlobalGravity sets Physics2D.gravity = -9.81 * gravityMultiplier
        // Arrow's gravityScale then multiplies that, giving total downward accel:
        float physGrav = Mathf.Abs(Physics2D.gravity.y);
        if (physGrav < 0.01f) physGrav = 9.81f;
        float arrowGravScale = _archer.gravityScale > 0f ? _archer.gravityScale : 1.2f;
        return physGrav * arrowGravScale;
    }

    // Actual horizontal wind acceleration on the arrow.
    // WindSystem.ApplyWind does: rb.AddForce(right * windForce * fixedDeltaTime, ForceMode2D.Force)
    // ForceMode2D.Force applies: Δv = F / mass * fixedDeltaTime each step
    // So effective wind acceleration = windForce * fixedDeltaTime / mass
    float WindAccel()
    {
        if (WindSystem.Instance == null) return 0f;
        float mass = (_archer.arrowMass > 0f) ? _archer.arrowMass : 0.5f;
        return WindSystem.Instance.windForce * Time.fixedDeltaTime / mass;
    }

    float AccuracyNoiseDeg()
    {
        switch (difficulty)
        {
            // Easy/Normal miss often so the player can win; Hard is sharp but not perfect.
            case Difficulty.Easy:   return Random.Range(-17f, 17f);
            case Difficulty.Normal: return Random.Range(-7f, 7f);
            case Difficulty.Hard:   return Random.Range(-2.5f, 2.5f);
        }
        return 0f;
    }

    float ChargeFractionNoise()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:   return Random.Range(-0.15f, 0.08f);
            case Difficulty.Normal: return Random.Range(-0.05f, 0.03f);
            case Difficulty.Hard:   return Random.Range(-0.01f, 0.01f);
        }
        return 0f;
    }

    // Step size for the brute-force angle search — finer on Hard
    float AngleStep()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:   return 2f;
            case Difficulty.Normal: return 1f;
            case Difficulty.Hard:   return 0.5f;
        }
        return 1f;
    }

    // Charge ratio step — finer on Hard
    float RatioStep()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:   return 0.08f;
            case Difficulty.Normal: return 0.05f;
            case Difficulty.Hard:   return 0.03f;
        }
        return 0.05f;
    }

    int PlayerDir() => _archer.playerIndex == 2 ? -1 : 1;

    Vector2 AimDirFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        int pDir = PlayerDir();
        return new Vector2(Mathf.Cos(rad) * pDir, Mathf.Sin(rad)).normalized;
    }

    Vector2 SpawnPoint()
    {
        if (_archer.arrowSpawnPoint != null)
            return _archer.arrowSpawnPoint.position;
        return (Vector2)_archer.transform.position + Vector2.up * 0.5f;
    }

    Vector2 LaunchOrigin(Vector2 aimDir) =>
        SpawnPoint() + aimDir * LaunchOffset;

    static Vector2 TargetCenter(ArcherLocal archer)
    {
        Transform sprite = archer.transform.Find("__Sprite");
        if (sprite != null)
        {
            var sr = sprite.GetComponent<SpriteRenderer>();
            if (sr != null && sr.enabled)
                return sr.bounds.center;
        }
        return (Vector2)archer.transform.position + Vector2.up * 0.55f;
    }

    IEnumerator AILoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            // Re-find target if we lost the reference (shouldn't happen but be safe)
            if (_target == null)
            {
                foreach (var a in FindObjectsOfType<ArcherLocal>())
                    if (a != _archer) { _target = a; break; }
            }

            if (_archer == null || _archer.isDead || _target == null || _target.isDead)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            yield return new WaitForSeconds(Random.Range(minReactionTime, maxReactionTime));

            if (_archer.isDead || _target == null || _target.isDead) continue;

            Vector2 targetPos = TargetCenter(_target);
            float g    = EffectiveGravity();
            float wind = WindAccel();

            bool found = TryFindBestShot(targetPos, g, wind,
                out Vector2 aimDir, out float chosenRatio);

            if (!found)
            {
                // Analytical fallback: compute launch angle for a parabolic shot
                if (!TryAnalyticalShot(targetPos, g, wind, out aimDir, out chosenRatio))
                {
                    // Last resort: aim directly, full power
                    Vector2 toTarget = (targetPos - SpawnPoint()).normalized;
                    aimDir = toTarget.sqrMagnitude > 0.001f ? toTarget : AimDirFromAngle(20f);
                    chosenRatio = 1f;
                }
            }

            // Apply accuracy noise (angle only).
            aimDir = RotateDir(aimDir, AccuracyNoiseDeg());

            // Charge to full power, then release.
            yield return Fire(aimDir);

            // Hard mode: occasionally throw a second arrow quickly
            if (difficulty == Difficulty.Hard && Random.value < 0.15f
                && !_archer.isDead && _target != null && !_target.isDead)
            {
                yield return new WaitForSeconds(Random.Range(0.4f, 0.7f));

                if (!_archer.isDead && _target != null && !_target.isDead)
                {
                    // Vary the arc slightly for the follow-up shot
                    Vector2 followDir = RotateDir(aimDir, Random.Range(-5f, 5f));
                    yield return Fire(followDir);
                }
            }
        }
    }

    static Vector2 RotateDir(Vector2 dir, float deltaDeg)
    {
        if (dir.sqrMagnitude < 0.001f || Mathf.Abs(deltaDeg) < 0.01f)
            return dir.sqrMagnitude < 0.001f ? Vector2.right : dir.normalized;
        float rad = Mathf.Atan2(dir.y, dir.x) + deltaDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    /// <summary>
    /// Brute-force search across all bow angles and charge levels.
    /// Finds the combination whose simulated arc passes closest to targetPos.
    /// </summary>
    float FixedSpeed()
    {
        // AI always charges to full, so it solves arcs at max-power launch speed.
        float force = _archer.maxLaunchForce > 0f ? _archer.maxLaunchForce : 9f;
        return force / (_archer.arrowMass > 0f ? _archer.arrowMass : 0.5f);
    }

    bool TryFindBestShot(Vector2 targetPos, float g, float wind,
        out Vector2 aimDir, out float ratio)
    {
        aimDir = AimDirFromAngle(20f);
        ratio  = 1f; // unused now (fixed power) but kept for signature compatibility

        float bestErr   = float.MaxValue;
        Vector2 bestDir = aimDir;

        float aStep = AngleStep();
        float speed = FixedSpeed();

        // Fixed power now — only the launch ANGLE is searched. Prefer arcs that clear
        // cover (WorldTrajectoryError penalizes blocked paths), so rounds don't stall.
        for (float a = MinAimAngle; a <= MaxAimAngle + 1e-3f; a += aStep)
        {
            Vector2 dir = AimDirFromAngle(a);
            float err = WorldTrajectoryError(LaunchOrigin(dir), dir, speed, targetPos, g, wind);
            if (err < bestErr)
            {
                bestErr = err;
                bestDir = dir;
            }
        }

        aimDir = bestDir;
        // Accept any shot within ~one body-width of the target.
        return bestErr <= 1.6f;
    }

    /// <summary>
    /// Analytical fallback: use the range equation to find a valid launch angle.
    /// R = v²·sin(2θ)/g  (ignoring wind for simplicity)
    /// </summary>
    bool TryAnalyticalShot(Vector2 targetPos, float g, float wind,
        out Vector2 aimDir, out float ratio)
    {
        Vector2 spawn = SpawnPoint();
        float dx = targetPos.x - spawn.x;
        float dy = targetPos.y - spawn.y;

        ratio = 1f; // fixed power
        float speed = FixedSpeed();
        float v2 = speed * speed;

        // Solve: dy = dx·tan(θ) - g·dx²/(2v²·cos²(θ))
        float A = g * dx * dx / (2f * v2);
        float B = -Mathf.Abs(dx); // always aim toward opponent (horizontal magnitude)
        float C = dy + A;
        float disc = B * B - 4f * A * C;

        if (disc >= 0f)
        {
            float sqrtDisc = Mathf.Sqrt(disc);
            // Prefer the HIGH arc (s=1) first — it clears cover better.
            for (int s = 1; s >= 0; s--)
            {
                float tanTheta = s == 0
                    ? (-B - sqrtDisc) / (2f * A)
                    : (-B + sqrtDisc) / (2f * A);
                float angleDeg = Mathf.Atan(tanTheta) * Mathf.Rad2Deg;

                if (angleDeg < MinAimAngle || angleDeg > MaxAimAngle) continue;

                Vector2 dir = AimDirFromAngle(angleDeg);
                float err = WorldTrajectoryError(LaunchOrigin(dir), dir, speed, targetPos, g, wind);
                if (err <= 2f)
                {
                    aimDir = dir;
                    return true;
                }
            }
        }

        aimDir = AimDirFromAngle(20f);
        ratio  = 1f;
        return false;
    }

    /// <summary>
    /// Simulate a projectile arc and return how close it gets to the target.
    /// Accounts for gravity and the corrected wind acceleration.
    /// </summary>
    float WorldTrajectoryError(Vector2 origin, Vector2 aimDir, float speed,
        Vector2 target, float g, float windAccel)
    {
        Vector2 v0   = aimDir.normalized * speed;
        float   best = float.MaxValue;
        float   bestT = 0f;

        for (int i = 1; i <= 200; i++)
        {
            float t = i * SimDt;
            Vector2 p = origin
                + v0 * t
                + new Vector2(0.5f * windAccel * t * t, -0.5f * g * t * t);

            float err = (p - target).sqrMagnitude;
            if (err < best) { best = err; bestT = t; }

            if (p.y < target.y - 6f && t > 0.3f) break;
        }

        // Cover check: if the arc hits a solid (Ground-layer platform/crate/cactus)
        // BEFORE reaching its closest approach to the target, the shot is blocked —
        // heavily penalize so the AI prefers an arc that clears cover.
        for (int i = 1; i <= 200; i++)
        {
            float t = i * SimDt;
            if (t >= bestT) break;
            Vector2 p = origin
                + v0 * t
                + new Vector2(0.5f * windAccel * t * t, -0.5f * g * t * t);
            // Ignore the muzzle area (own platform) and the area right at the target.
            if ((p - origin).sqrMagnitude < 1.4f) continue;
            if ((p - target).sqrMagnitude < 0.7f) break;
            if (Physics2D.OverlapPoint(p, _groundMask) != null)
                return Mathf.Sqrt(best) + 100f; // blocked
        }

        return Mathf.Sqrt(best);
    }

    IEnumerator Fire(Vector2 aimDir)
    {
        _isFiring = true;
        _archer.SetAimDirection(aimDir);
        _archer.SetHoldInput(true);

        // Hold past full charge so the shot launches at max power, keeping the aim locked
        // the whole time (the archer's own HandleCharge builds the charge and fires on release).
        float hold = _archer.maxChargeTime + 0.1f;
        float elapsed = 0f;
        while (elapsed < hold)
        {
            _archer.SetAimDirection(aimDir);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _archer.SetHoldInput(false);
        yield return new WaitForSeconds(0.12f);
        _isFiring = false;
    }
}
