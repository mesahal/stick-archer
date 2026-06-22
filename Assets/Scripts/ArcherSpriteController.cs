using UnityEngine;
using System.Collections;

/// <summary>
/// Drives the archer's visual sprite based on game state.
/// Replaces the runtime procedural body-part construction (white-square rectangles)
/// with the actual sprite art from Resources/Characters/Player1 and Player2.
///
/// Each Archer/ArcherLocal calls Setup(playerIndex, characterIndex) on start; this script
/// then handles all subsequent sprite swaps automatically by polling its sibling state.
/// </summary>
[DisallowMultipleComponent]
public class ArcherSpriteController : MonoBehaviour
{
    [Header("Player 1 (Adventurer) Sprites")]
    public Sprite p1Idle;
    public Sprite p1Charge;
    public Sprite p1Fire;
    public Sprite p1Ragdoll;

    [Header("Player 2 (Soldier) Sprites")]
    public Sprite p2Idle;
    public Sprite p2Charge;
    public Sprite p2Fire;
    public Sprite p2Ragdoll;

    [Header("Visual Settings")]
    [Tooltip("How long the 'fire' pose holds after releasing the bow.")]
    public float fireFlashDuration = 0.18f;
    [Tooltip("Sorting order for the main archer sprite.")]
    public int spriteSortingOrder = 6;
    [Tooltip("Desired world-space height for the archer sprite, in Unity units.\n" +
             "All sprites are auto-scaled at runtime to match this height,\n" +
             "regardless of their source pixel dimensions or PPU.")]
    public float targetWorldHeight = 1.5f;

    [Header("Team Tint (subtle)")]
    public Color p1Tint = Color.white;
    public Color p2Tint = Color.white;

    [Header("Idle Breathing")]
    [Tooltip("Vertical amplitude of the idle breathing sway, in world units.")]
    public float breathAmplitude = 0.012f;
    [Tooltip("Idle breathing frequency in Hz.")]
    public float breathFrequency = 1.3f;
    [Tooltip("How strongly both players rotate with the current aim angle.")]
    public float aimLeanStrength = 0.46f;
    [Tooltip("Maximum pendulum-style body rotation in degrees.")]
    public float maxPendulumRotation = 30f;
    [Tooltip("Show the archer's ready pose whenever an aim direction exists, even before charge starts.")]
    public bool showReadyPoseWhileAiming = false;

    // Runtime state
    SpriteRenderer _sr;
    Transform      _spriteChild;
    Archer        _archer;
    ArcherLocal   _archerLocal;
    int           _playerIndex   = 1;
    int           _characterIndex = 0; // 0 = adventurer, 1 = soldier
    bool          _firingFlash   = false;
    bool          _wasCharging   = false;
    bool          _wasDead       = false;
    Coroutine     _fireCoroutine;

    // Spring-driven aim-lean angle (overshoots + wobbles like a weighted body) so the
    // living archer reads as physical, not a rigid sprite. _leanVel is the angular
    // velocity of the spring; hits inject velocity into it for a whip-back reaction.
    float         _leanZ         = 0f;
    float         _leanVel       = 0f;

    // Additive "impulse" offset applied on top of the idle/lean pose every frame.
    // Driven by a damped envelope from PlayImpulse() - used for both the fire recoil
    // and the get-hit flinch so they read as real physical reactions, not pose snaps.
    Vector3       _flinchOffset  = Vector3.zero;
    float         _flinchRot     = 0f;
    float         _flinchSquash  = 0f;
    Coroutine     _impulseCoroutine;

    void Awake()
    {
        _archer      = GetComponent<Archer>();
        _archerLocal = GetComponent<ArcherLocal>();

        // Put the SpriteRenderer on a dedicated child so its Y position can be
        // adjusted independently of the physics collider (which stays on the root).
        _spriteChild = transform.Find("__Sprite");
        if (_spriteChild == null)
        {
            var go = new GameObject("__Sprite");
            go.transform.SetParent(transform, false);
            _spriteChild = go.transform;
        }
        _spriteChild.localPosition = Vector3.zero;

        _sr = _spriteChild.GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = _spriteChild.gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingLayerID = 0;
        _sr.sortingOrder   = spriteSortingOrder;

        // If a legacy SpriteRenderer exists on the root, disable it so we don't
        // see two overlapping sprites.
        var rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null) rootSr.enabled = false;

        EnsureFootShadow();

        TryAutoLoadSprites();
    }

    /// <summary>
    /// Soft dark ellipse just above the feet - sells the "standing on something"
    /// feeling much more than perfect placement alone. Stays at Y=0 so it never
    /// participates in the breathing sway.
    /// </summary>
    Transform _footShadow;

    /// <summary>Toggle death-tumble visuals: hides the foot shadow while the body flies.</summary>
    public void SetRagdollMode(bool on)
    {
        if (_footShadow == null) _footShadow = transform.Find("__FootShadow");
        if (_footShadow != null) _footShadow.gameObject.SetActive(!on);
    }

    /// <summary>
    /// Show/hide the single character sprite (and its foot shadow). Used on death so the
    /// articulated ragdoll fully replaces the live sprite, and on respawn to restore it.
    /// </summary>
    public void SetBodyVisible(bool on)
    {
        if (_sr != null) _sr.enabled = on;
        if (_footShadow == null) _footShadow = transform.Find("__FootShadow");
        if (_footShadow != null) _footShadow.gameObject.SetActive(on);
    }

    void EnsureFootShadow()
    {
        var t = transform.Find("__FootShadow");
        if (t == null)
        {
            var go = new GameObject("__FootShadow");
            go.transform.SetParent(transform, false);
            t = go.transform;
        }
        _footShadow = t;
        // Tiny lift above the terrace surface to avoid Z-fighting with the grass cap.
        t.localPosition = new Vector3(0f, 0.02f, 0f);
        // Wide and flat, in transform-local units - final world size = this times root scale.
        t.localScale    = new Vector3(0.7f, 0.18f, 1f);

        var shadow = t.GetComponent<SpriteRenderer>();
        if (shadow == null) shadow = t.gameObject.AddComponent<SpriteRenderer>();
        if (shadow.sprite == null)
            shadow.sprite = Resources.Load<Sprite>("_WhiteSquare");
        shadow.color          = new Color(0f, 0f, 0f, 0.35f);
        shadow.sortingLayerID = 0;
        shadow.sortingOrder   = spriteSortingOrder - 2;
    }

    /// <summary>
    /// Called by Archer/ArcherLocal once playerIndex is known (and on Respawn).
    /// playerIndex controls side/direction; characterIndex controls the selected art.
    /// </summary>
    public void Setup(int playerIndex)
    {
        Setup(playerIndex, playerIndex == 2 ? 1 : 0);
    }

    public void Setup(int playerIndex, int characterIndex)
    {
        _playerIndex = playerIndex;
        _characterIndex = Mathf.Clamp(characterIndex, 0, 1);
        _wasDead     = false;
        _firingFlash = false;
        if (_fireCoroutine != null) { StopCoroutine(_fireCoroutine); _fireCoroutine = null; }
        if (_impulseCoroutine != null) { StopCoroutine(_impulseCoroutine); _impulseCoroutine = null; }
        _flinchOffset = Vector3.zero;
        _flinchRot    = 0f;
        _flinchSquash = 0f;
        _leanZ        = 0f;
        _leanVel      = 0f;

        ApplySprite(CurrentIdleSprite());
        _sr.color  = playerIndex == 2 ? p2Tint : p1Tint;

        HideLegacyBodyPartRenderers();
    }

    /// <summary>
    /// Returns the uniform scale factor needed so that the given sprite's
    /// world-space height equals targetWorldHeight.
    /// </summary>
    float ComputeUniformScale(Sprite sprite)
    {
        if (sprite == null) return 1f;
        float nativeHeight = sprite.bounds.size.y; // world units at scale 1
        if (nativeHeight <= 0.001f) return 1f;
        return targetWorldHeight / nativeHeight;
    }

    void Update()
    {
        if (_sr == null) return;

        bool isCharging = false;
        bool isDead     = false;
        Vector2 aimDir  = Vector2.zero;

        if (_archer != null)
        {
            isCharging = _archer.currentChargeRatio > 0.01f;
            isDead     = _archer.isDead;
            aimDir      = _archer.aimDirInput;
        }
        else if (_archerLocal != null)
        {
            isCharging = _archerLocal.currentChargeRatio > 0.01f;
            isDead     = _archerLocal.isDead;
            aimDir      = _archerLocal.aimDirInput;
        }

        // Death takes priority
        if (isDead && !_wasDead)
        {
            ApplySprite(CurrentRagdollSprite());
            _wasDead   = true;
            return;
        }
        if (!isDead && _wasDead)
        {
            // Respawned - reset to idle
            ApplySprite(CurrentIdleSprite());
            _wasDead   = false;
        }
        if (isDead) return;

        // Charging to fire transition
        if (_wasCharging && !isCharging && !_firingFlash)
        {
            // Just released - flash the fire pose briefly
            if (_fireCoroutine != null) StopCoroutine(_fireCoroutine);
            _fireCoroutine = StartCoroutine(FireFlash());
        }
        else if (!_firingFlash)
        {
            bool showReadyPose = isCharging || (showReadyPoseWhileAiming && aimDir.sqrMagnitude > 0.001f);
            Sprite newSprite = showReadyPose ? CurrentReadySprite() : CurrentIdleSprite();
            if (_sr.sprite != newSprite)
            {
                ApplySprite(newSprite);
            }
        }

        _wasCharging = isCharging;

        // Idle breathing - small vertical sway. Stops while charging/firing so it
        // doesn't fight the aim lean or the fire pose snap. The hit/fire impulse
        // offset is layered on top so a flinch reads even mid-charge.
        if (_spriteChild != null)
        {
            bool isIdle = !isCharging && !_firingFlash;
            float y = isIdle
                ? Mathf.Sin(Time.time * breathFrequency * Mathf.PI * 2f) * breathAmplitude
                : 0f;
            _spriteChild.localPosition = new Vector3(0f, y, 0f) + _flinchOffset;
            // Squash/stretch on the sprite child (not the root) so the team mirror
            // on transform.localScale.x is preserved.
            _spriteChild.localScale = new Vector3(1f + _flinchSquash, 1f - _flinchSquash, 1f);
        }

        // Tilt sprite to match the current aim direction (shows where arrow will fire),
        // plus a gentle always-on "alive" sway so the body never looks frozen.
        float targetLean = 0f;
        if (!_firingFlash && aimDir.sqrMagnitude > 0.001f)
        {
            float aimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            float neutralAngle = _playerIndex == 2 ? 180f : 0f;
            float pendulumAngle = Mathf.DeltaAngle(neutralAngle, aimAngle);
            targetLean = Mathf.Clamp(
                pendulumAngle * aimLeanStrength,
                -maxPendulumRotation,
                maxPendulumRotation);
        }
        // Subtle living wobble (small, slow) layered on the target so it breathes.
        targetLean += Mathf.Sin(Time.time * 1.7f) * 1.2f;

        // Critically-ish-damped angular spring: overshoots toward the target then
        // settles, giving the body weight. Hit reactions inject _leanVel for a whip.
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        const float stiffness = 110f;
        const float damping   = 13f;
        float accel = (targetLean - _leanZ) * stiffness - _leanVel * damping;
        _leanVel += accel * dt;
        _leanZ   += _leanVel * dt;

        // Final rotation = spring lean + the additive impulse kick.
        transform.rotation = Quaternion.Euler(0f, 0f, _leanZ + _flinchRot);
    }

    void LateUpdate()
    {
        if (_sr != null)
            HideLegacyBodyPartRenderers();
    }

    IEnumerator FireFlash()
    {
        _firingFlash = true;
        // Always lead with the drawn-bow (charge) pose for a brief moment.
        // For quick taps the charge sprite may not have been on screen long enough
        // to register, so this guarantees a clear draw/release sequence regardless
        // of how long the player held.
        ApplySprite(CurrentChargeSprite());
        yield return new WaitForSeconds(0.08f);

        ApplySprite(CurrentFireSprite());
        // Recoil: kick the body backward (away from the shot) with a little upward
        // hop and stretch, then settle. Turns the flat pose-swap into a release with
        // follow-through. facing = +1 for P1 (shoots right), -1 for P2.
        float facing = _playerIndex == 2 ? -1f : 1f;
        StartImpulse(new Vector3(-0.06f * facing, 0.03f, 0f), 7f * facing, -0.06f, 0.22f);
        yield return new WaitForSeconds(fireFlashDuration);
        _firingFlash = false;
        // Update() will pick up the next correct sprite on the following frame
    }

    /// <summary>
    /// Called when this archer takes a (non-fatal) arrow hit. Plays a directional
    /// flinch - the body is shoved along the arrow's travel direction, kicks back,
    /// and squashes before recovering. <paramref name="impactDir"/> is the arrow's
    /// world-space travel direction; <paramref name="strength"/> scales the reaction.
    /// </summary>
    public void TriggerHitReaction(Vector2 impactDir, float strength = 1f)
    {
        if (_wasDead) return;
        // Push the body the way the arrow was travelling.
        float push = (impactDir.x >= 0f) ? 1f : -1f;
        strength = Mathf.Clamp(strength, 0.4f, 2f);
        // Dramatic flail: bigger shove, harder spin, deeper squash, and kick the lean
        // spring so the whole body whips and wobbles back (reference-style reaction).
        StartImpulse(
            new Vector3(0.24f * strength * push, 0.07f * strength, 0f),
            -22f * strength * push,
            0.16f * strength,
            0.40f);
        _leanVel += -160f * strength * push;
    }

    void StartImpulse(Vector3 peakOffset, float peakRot, float peakSquash, float dur)
    {
        if (_impulseCoroutine != null) StopCoroutine(_impulseCoroutine);
        _impulseCoroutine = StartCoroutine(PlayImpulse(peakOffset, peakRot, peakSquash, dur));
    }

    /// <summary>
    /// Damped impulse: snaps to the peak, then settles back to rest with a single
    /// overshoot. One generic envelope drives both the fire recoil and the hit flinch.
    /// </summary>
    IEnumerator PlayImpulse(Vector3 peakOffset, float peakRot, float peakSquash, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / dur);
            float k = Mathf.Exp(-9f * n) * Mathf.Cos(n * Mathf.PI * 2.2f);
            _flinchOffset = peakOffset * k;
            _flinchRot    = peakRot * k;
            _flinchSquash = peakSquash * k;
            yield return null;
        }
        _flinchOffset = Vector3.zero;
        _flinchRot    = 0f;
        _flinchSquash = 0f;
        _impulseCoroutine = null;
    }

    /// <summary>
    /// Called directly by ArcherLocal/Archer when an arrow is fired.
    /// More reliable than the polling-based _wasCharging transition because it
    /// doesn't depend on script-execution order within the same frame.
    /// </summary>
    public void TriggerFireAnimation()
    {
        if (_firingFlash) return;          // already playing - don't restart
        _wasCharging = false;              // prevent Update()'s duplicate trigger
        if (_fireCoroutine != null) StopCoroutine(_fireCoroutine);
        _fireCoroutine = StartCoroutine(FireFlash());
    }

    Sprite CurrentIdleSprite()
    {
        return _characterIndex == 1
            ? (p2Idle != null ? p2Idle : p1Idle)
            : (p1Idle != null ? p1Idle : p2Idle);
    }

    Sprite CurrentChargeSprite()
    {
        return _characterIndex == 1
            ? (p2Charge != null ? p2Charge : CurrentIdleSprite())
            : (p1Charge != null ? p1Charge : CurrentIdleSprite());
    }

    Sprite CurrentReadySprite()
    {
        return CurrentChargeSprite();
    }

    void ApplySprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        _sr.sprite = sprite;
        float scale = ComputeUniformScale(sprite);
        transform.localScale = new Vector3(
            _playerIndex == 1 ? -scale : scale,
            scale,
            1f
        );
    }

    Sprite CurrentRagdollSprite()
    {
        return _characterIndex == 1
            ? (p2Ragdoll != null ? p2Ragdoll : CurrentIdleSprite())
            : (p1Ragdoll != null ? p1Ragdoll : CurrentIdleSprite());
    }

    Sprite CurrentFireSprite()
    {
        if (_characterIndex == 1) return p2Fire != null ? p2Fire : CurrentChargeSprite();
        return p1Fire != null ? p1Fire : CurrentChargeSprite();
    }

    /// <summary>
    /// Auto-load sprites from Resources/Characters/* if Inspector slots are empty.
    /// Lets the prefab work even without manual sprite-dragging.
    /// </summary>
    void TryAutoLoadSprites()
    {
        if (p1Idle    == null) p1Idle    = LoadSpriteOrTex("Characters/Player1/archer_idle");
        if (p1Charge  == null) p1Charge  = LoadSpriteOrTex("Characters/Player1/archer_charge");
        if (p1Fire    == null) p1Fire    = LoadSpriteOrTex("Characters/Player1/archer_fire");
        if (p1Ragdoll == null) p1Ragdoll = LoadSpriteOrTex("Characters/Player1/archer_ragdoll");

        if (p2Idle    == null) p2Idle    = LoadSpriteOrTex("Characters/Player2/archer_idle");
        if (p2Charge  == null) p2Charge  = LoadSpriteOrTex("Characters/Player2/archer_charge");
        if (p2Fire    == null) p2Fire    = LoadSpriteOrTex("Characters/Player2/archer_fire");
        if (p2Ragdoll == null) p2Ragdoll = LoadSpriteOrTex("Characters/Player2/archer_ragdoll");
    }

    static Sprite LoadSpriteOrTex(string path)
    {
        var s = Resources.Load<Sprite>(path);
        if (s != null) return s;

        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;

        // Re-encode as RGBA32 to guarantee alpha channel survives any import compression
        Texture2D rgba;
        if (tex.isReadable)
        {
            rgba = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            rgba.filterMode = FilterMode.Point;
            rgba.SetPixels32(tex.GetPixels32());
            rgba.Apply();
        }
        else
        {
            // Texture isn't CPU-readable; use as-is and hope import preserved alpha
            rgba = tex;
        }

        // Use the texture height as PPU so the sprite's native world height = 1 unit.
        // ComputeUniformScale() will then stretch it to targetWorldHeight.
        float ppu = Mathf.Max(rgba.height, 1);
        // Use bottom-center pivot to match the .meta-imported sprites - keeps
        // transform.position aligned with the character's feet.
        return Sprite.Create(rgba, new Rect(0, 0, rgba.width, rgba.height),
            new Vector2(0.5f, 0f), ppu);
    }

    /// <summary>
    /// Hides ALL legacy child SpriteRenderers (Head/Hair/Body/Pants/Legs and the old
    /// bow parts), keeping only the main body sprite and the foot shadow. The bow is
    /// now baked into the character sprite, so no separate bow objects are needed.
    /// </summary>
    void HideLegacyBodyPartRenderers()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == _sr) continue;
            if (sr.gameObject.name == "__FootShadow") continue;
            sr.enabled = false;
        }
    }
}
