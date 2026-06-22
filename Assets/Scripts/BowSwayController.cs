using UnityEngine;

/// <summary>
/// Continuous pendulum aiming — exactly how the reference game works. The bow arm
/// oscillates up and down ALL the time, so the aim direction is always changing; the
/// player presses (to charge power) and releases to fire at whatever angle the pendulum
/// is at that instant. Only drives the LOCAL player's archer — the AI drives its own
/// aim, and remote (online) archers receive their aim over the network.
/// Attach to the same GameObject as Archer or ArcherLocal.
/// </summary>
public class BowSwayController : MonoBehaviour
{
    [Header("Sway")]
    [Tooltip("Full oscillation cycles per second")]
    public float swayFrequency = 0.42f;
    [Tooltip("Lowest aim angle in degrees (0 = horizontal)")]
    public float minAngle = 15f;
    [Tooltip("Highest aim angle in degrees")]
    public float maxAngle = 75f;

    private Archer      _archer;
    private ArcherLocal _archerLocal;
    private float _phase;

    void Awake()
    {
        _archer      = GetComponent<Archer>();
        _archerLocal = GetComponent<ArcherLocal>();
        _phase = -Mathf.PI * 0.5f; // start low so the arm rises first
    }

    void Update()
    {
        if (IsDead()) return;
        if (!IsLocalPlayer()) return; // AI / remote archers aim themselves

        float effectiveFrequency = swayFrequency;
        if (GameSettings.AimAssist)
            effectiveFrequency *= GameSettings.AimAssistSwayScale;

        _phase += Time.deltaTime * effectiveFrequency * Mathf.PI * 2f;

        float t        = (Mathf.Sin(_phase) + 1f) * 0.5f;
        float angleDeg = Mathf.Lerp(minAngle, maxAngle, t);

        int   pDir = GetPlayerIndex() == 2 ? -1 : 1;
        float rad  = angleDeg * Mathf.Deg2Rad;
        Vector2 swayDir = new Vector2(Mathf.Cos(rad) * pDir, Mathf.Sin(rad)).normalized;

        if (_archerLocal != null && !_archerLocal.isDead) _archerLocal.SetAimDirection(swayDir);
        if (_archer      != null && !_archer.isDead)      _archer.SetAimDirection(swayDir);
    }

    bool IsLocalPlayer()
    {
        if (_archerLocal != null) return _archerLocal.isPlayerControlled;
        if (_archer      != null) return _archer.photonView != null && _archer.photonView.IsMine;
        return false;
    }

    bool IsDead()
    {
        if (_archer      != null) return _archer.isDead;
        if (_archerLocal != null) return _archerLocal.isDead;
        return false;
    }

    int GetPlayerIndex()
    {
        if (_archer      != null) return _archer.playerIndex;
        if (_archerLocal != null) return _archerLocal.playerIndex;
        return 1;
    }
}
