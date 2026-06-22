# Scripts API Reference

> **Note:** This reference covers core scripts from an earlier snapshot (~48 files).
> The project now has 70+ scripts including `Analytics/`, `Progression/`, and `UI/` subfolders.
> For current architecture and file roles, see **[Documentation/ARCHITECTURE.md](../Documentation/ARCHITECTURE.md)**.
> For gameplay behavior, see **[Documentation/GAMEPLAY_SYSTEMS.md](../Documentation/GAMEPLAY_SYSTEMS.md)**.

Complete reference for C# scripts in `Assets/Scripts/`. Each entry documents the script's purpose, public API, Inspector fields, and dependencies.

---

## Table of Contents

1. [Core Gameplay](#1-core-gameplay)
2. [Projectiles](#2-projectiles)
3. [Game Management](#3-game-management)
4. [Arena & Environment](#4-arena--environment)
5. [Networking](#5-networking)
6. [Input](#6-input)
7. [AI](#7-ai)
8. [UI](#8-ui)
9. [Visual Effects](#9-visual-effects)
10. [Audio](#10-audio)
11. [Editor Tools](#11-editor-tools)
12. [Utilities](#12-utilities)

---

## 1. Core Gameplay

### `Archer.cs`

**Purpose:** Online (Photon-networked) player character.

**Inherits:** `MonoBehaviourPun`, `IPunInstantiateMagicCallback`, `IPunObservable`

**Required Components:** `PhotonView`, `Rigidbody2D`

**Inspector Fields:**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `arrowPrefab` | `GameObject` | — | Arrow prefab to instantiate |
| `arrowSpawnPoint` | `Transform` | — | Firing position |
| `maxChargeTime` | `float` | 1.5 | Seconds to reach full charge |
| `minLaunchForce` / `maxLaunchForce` | `float` | 3 / 9 | Force range mapped to charge |
| `maxHealth` | `float` | 100 | Starting HP |
| `arrowMass` | `float` | 0.5 | Used for ballistic preview math |
| `gravityScale` | `float` | 1.2 | Used for ballistic preview math |
| `aimLineSteps` | `int` | 24 | Trajectory preview resolution |
| `groundLayer` | `LayerMask` | Auto | Layer mask for aim line ground detection |

**Key Public Methods:**
| Method | Description |
|--------|-------------|
| `SetAimAndCharge(Vector2 aimDir, float chargeRatio01)` | Called by `TouchControls`/`AIController` each frame during aim |
| `SetHoldInput(bool holding)` | Begin/end charge hold |
| `OnHitReceived(int shooterActorNumber, float damage)` | Process incoming damage, update health, trigger ragdoll on death |
| `SetLastHit(Vector3 force, Vector3 point)` | Store impact data for ragdoll activation |
| `Respawn()` | Reset health, position, and state for new round |
| `TriggerRagdoll()` | Activate `Ragdoll2D` and hide body sprites |

**Network Sync:** Aim direction (`aimDirInput`) is synced via `OnPhotonSerializeView`.

---

### `ArcherLocal.cs`

**Purpose:** Practice mode (non-networked) player character. Mirrors `Archer.cs` API exactly.

**Inherits:** `MonoBehaviour`

**Required Components:** `Rigidbody2D`

**Additional Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `arrowLocalPrefab` | `GameObject` | Auto-loads from `Resources/ArrowLocal` if null |
| `isPlayerControlled` | `bool` | `true` for human, `false` for AI |

**Key Difference from `Archer`:** Uses `Instantiate()` instead of `PhotonNetwork.Instantiate()`. Creates arrows from scratch as fallback if no prefab is assigned.

---

### `HitZone.cs`

**Purpose:** Damage zone collider for body parts. Attached as children of archer objects.

**Zone Types:** `Head`, `Body`, `LeftArm`, `RightArm`, `LeftLeg`, `RightLeg`

**Inspector Fields:**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `zoneType` | `ZoneType` | Body | Which body part |
| `damagePercent` | `float` | 0.3 | Damage fraction (0–1) |
| `isInstantKill` | `bool` | false | Override to 100% damage (Head) |
| `knockbackMultiplier` | `float` | 1 | Force multiplier on ragdoll |

**Collision Flow:** `OnTriggerEnter2D` → detects `Arrow`/`ArrowLocal` → calls parent archer's `OnHitReceived()`.

---

### `ArcherAutoSetup.cs`

**Purpose:** Auto-creates a `HitZones` container with 6 child hit zone objects (Head, Body, LeftArm, RightArm, LeftLeg, RightLeg) at runtime.

**Usage:** Add to archer prefab root. Set `autoSetupOnStart = true`.

**Created Colliders:**
- Head: `CircleCollider2D` (r=0.3)
- Body: `BoxCollider2D` (0.4×0.6)
- Limbs: `BoxCollider2D` (0.12×0.35)

All colliders are triggers. Editor mode adds debug visualization sprites.

---

## 2. Projectiles

### `Arrow.cs`

**Purpose:** Online arrow projectile with Photon networking.

**Required Components:** `Rigidbody2D`, `PhotonView`

**Key Methods:**
| Method | Description |
|--------|-------------|
| `Launch(Vector2 force, int shooterActorNumber)` | Apply impulse, sync via RPC, auto-destroy after 4s |
| `RPC_OnHit(int archerViewID, int shooterActorNumber, ...)` | Propagate hit to all clients |

**Physics:** Wind applied via `WindSystem.ApplyWind(rb)` in `FixedUpdate`. Arrow rotates to match velocity direction.

---

### `ArrowLocal.cs`

**Purpose:** Practice mode arrow (no Photon).

**Hit Detection:** Uses `GetComponentInParent<ArcherLocal>()` to handle HitZone child colliders. Also checks for `Archer` (online) as fallback. Sticks into non-trigger terrain surfaces.

---

### `ArrowTrail.cs`

**Purpose:** Golden `TrailRenderer` effect on arrows. Auto-created if missing. Mobile-optimized (max 20 points).

**Public Methods:** `StartTrail()`, `StopTrail()`

---

### `ArrowStuck.cs`

**Purpose:** Arrow behavior when embedded in terrain. Wobble animation, fade-out near end of life (10s default).

---

## 3. Game Management

### `GameManager.cs`

**Purpose:** Online mode scoring and round management via Photon RPCs.

**Singleton:** `GameManager.Instance`

**Key Methods:**
| Method | Description |
|--------|-------------|
| `RecordKill(int shooterActorNumber)` | Master client only — broadcasts `RPC_AddScore` |
| `RPC_AddScore(int actorNumber)` | Updates scores, triggers VFX, checks win condition |
| `OnTimeUp()` | Called by `MatchmakingTimer` — determines winner by score |

**Win Condition:** First player to `scoreToWin` (default: 5). Closes room, shows result.

---

### `PracticeGameManager.cs`

**Purpose:** Local scoring for practice mode. Same logic as `GameManager` without RPCs.

**Singleton:** `PracticeGameManager.Instance`

---

### `GameMode.cs`

**Purpose:** Static configuration class. No MonoBehaviour.

```csharp
public static class GameMode
{
    public static Mode Current;           // Online or Practice
    public static AIDifficulty Difficulty; // Easy, Normal, Hard
    public static bool IsPractice => Current == Mode.Practice;
}
```

---

### `GameArenaBootstrap.cs`

**Purpose:** Master orchestrator for the `GameArena` scene. Runs in `Start()`:

1. `NukePrePlacedObjects()` — Destroys all non-essential scene objects
2. `GenerateArenaImmediate()` — Creates arena + background synchronously
3. `SetupVisualEffects()` — Creates `VisualEffectsManager`
4. `SetupOtherSystems()` — Creates `SetupWizard`, `GameUISetup`, `WindSystem`
5. Spawns archers (practice or online)
6. Creates `TouchControls` if missing

---

## 4. Arena & Environment

### `ArenaGenerator.cs`

**Purpose:** Procedural arena generation.

**6 Arena Types:**
| Index | Name | Description |
|-------|------|-------------|
| 0 | Basic | Two equal-height buildings, gap=10 |
| 1 | Tall | Taller buildings, wider spacing |
| 2 | Asymmetric | Left=short, Right=tall |
| 3 | Stepped | Two-tier layered buildings |
| 4 | LowWall | Equal buildings + center barrier |
| 5 | Wide | Equal buildings, wider platforms |

**Block Rendering:** Uses Kenney pixel platformer tiles. Walls use `building_wall` or `building_wall_alt`, ground uses `grass_top` and `dirt_fill`. Fallbacks to procedural white blocks if sprites are missing.

**Spawn Point Positioning:** `capTopSurface + 0.70f` (accounts for character leg offset).

---

### `ArenaBackground.cs`

**Purpose:** Generates mountain silhouette layers behind the arena. Uses Kenney background elements (hills, mountains, clouds, castles) mapped to multiple parallax depth layers with runtime color tinting.

---

### `WindSystem.cs`

**Purpose:** Per-round wind and gravity randomization.

**Singleton:** `WindSystem.Instance`

**Public Fields:**
| Field | Range | Description |
|-------|-------|-------------|
| `windForce` | -10 to +10 | Horizontal force on arrows |
| `gravityMultiplier` | 0.3 to 2.0 | Global gravity scale |

**Key Methods:**
| Method | Description |
|--------|-------------|
| `ApplyWind(Rigidbody2D rb)` | Call in arrow's `FixedUpdate` |
| `RandomizeConditions()` | New random wind + gravity |
| `GetGravity()` | Returns `9.81 * gravityMultiplier` |

---

### `EnvironmentManager.cs`

**Purpose:** Arena transitions with fade-in/fade-out overlay. Used for between-round environment changes.

---

### `MovingPlatform` (in ArenaGenerator.cs)

**Purpose:** Simple sinusoidal platform movement. Not currently used by any arena type.

---

### `BirdSpawner.cs` / `BirdController.cs`

**Purpose:** Dynamic environmental obstacles. `BirdSpawner` periodically spawns birds that fly horizontally across the arena. `BirdController` manages their sinusoidal flight path and wing flapping animation. Arrows deflect off birds and destroy them. Uses generated pixel-art bird sprites.

---

## 5. Networking

### `NetworkManager.cs`

**Purpose:** Photon PUN 2 connection lifecycle manager.

**Singleton:** `NetworkManager.Instance` (DontDestroyOnLoad)

**Connection Flow:**
```
ConnectAndPlay() → OnConnectedToMaster() → JoinRandomRoom()
                                            ├─ Success → OnJoinedRoom() → StartGame()
                                            └─ Fail → OnJoinRandomFailed() → CreateRoom()
```

**Spawn Flow:** `SpawnLocalPlayer()` → `PhotonNetwork.Instantiate("Archer", ...)` with player index as instantiation data.

---

### `MatchmakingTimer.cs`

**Purpose:** Shared match countdown timer. Master client ticks time; synced via `IPunObservable`.

---

## 6. Input

### `TouchControls.cs`

**Purpose:** Drag-to-aim touch input (Angry Birds style).

**Mechanic:** Drag start → hold → drag delta determines aim direction and charge ratio → release fires.

**Constants:**
- `DragFullChargeFraction = 0.35f` — Drag 35% of screen height for full charge
- `HUD_TOP_FRACTION = 0.12f` — Top 12% of screen ignores touches (HUD area)

**Editor:** Mouse click/drag fallback via `#if UNITY_EDITOR`.

**Archer Detection:** Finds player-controlled `ArcherLocal` (practice) or `photonView.IsMine` `Archer` (online).

---

## 7. AI

### `AIController.cs`

**Purpose:** Practice mode computer opponent. Solves projectile physics to aim at human player.

**Difficulty Profiles:**
| Difficulty | Reaction Time | Angle Noise | Charge Noise |
|------------|---------------|-------------|--------------|
| Easy | 1.6–3.0s | ±12° | -20%/+10% |
| Normal | 1.0–2.2s | ±5° | -8%/+5% |
| Hard | 0.6–1.4s | ±1.5° | -3%/+2% |

**Trajectory Solver:** `TrySolveAngle(speed, dx, dy, gravity)` — Solves the quadratic for launch angle θ using the discriminant `v⁴ - g(g·dx² + 2·dy·v²)`. Prefers the flatter (lower) of two solutions.

**Loop:** Coroutine-based — wait → solve → aim → hold for `chargeRatio * maxChargeTime` → release.

---

## 8. UI

### `UIManager.cs`

**Purpose:** Central HUD manager. Builds scoreboard, health bars, and wind indicator at runtime.

**Singleton:** `UIManager.Instance`

**Panel Management:**
| Method | Shows |
|--------|-------|
| `ShowMainMenu()` | Main menu panel |
| `ShowLobby(string)` | Lobby panel with status text |
| `ShowGameHUD()` | Game HUD panel |
| `ShowResult(bool)` | Win/lose result panel |
| `ShowOpponentLeft()` | Opponent disconnected panel |

**HUD Updates:**
| Method | Description |
|--------|-------------|
| `UpdateScore(int, int)` | Sets P1/P2 score text |
| `UpdateChargeMeter(float)` | Sets charge slider value |
| `SetPlayerHealth(int, float, float)` | Updates health bar fill + legacy hearts |

---

### `MainMenuController.cs`

**Purpose:** Main menu scene controller. Auto-creates Play Online / VS Computer buttons. Shows Photon connection status.

---

### `GameUISetup.cs`

**Purpose:** Creates round transition and headshot feedback UI elements at runtime on the Canvas.

---

### `HealthBarUI.cs`

**Purpose:** Smooth animated health bar with color transitions (green → yellow → red).

---

### `RoundTransition.cs`

**Purpose:** "ROUND X" text with fade-in → scale → hold → fade-out animation.

---

### `ChargeMeterUI.cs`

**Purpose:** Charge-to-fire meter. Image `fillAmount` driven by charge ratio. Color from `Gradient`.

---

### `SettingsPanel.cs`

**Purpose:** SFX slider, Music slider, Mute toggle. Drives `AudioManager` methods. Panel toggled by gear button.

---

## 9. Visual Effects

### `CameraShaker.cs`

**Purpose:** Perlin noise camera shake.

**Profiles:** `hitShake` (0.15s, mag 0.08), `killShake` (0.3s, mag 0.15), `gameOverShake` (0.5s, mag 0.25)

---

### `HitFlash.cs`

**Purpose:** Flash all child `SpriteRenderer`s white for 0.15s on damage.

---

### `DamageNumber.cs`

**Purpose:** Floating damage text using `TextMeshPro`. Drifts up with random horizontal offset, fades out.

**Static Factory:** `DamageNumber.Spawn(int damage, Vector3 pos, Color color)`

---

### `ImpactEffect.cs`

**Purpose:** Particle burst at arrow impact point using `ParticleSystem`.

**Static Factory:** `ImpactEffect.Spawn(Vector3 pos, Vector2 hitNormal, Color? color)`

---

### `HeadshotFeedback.cs`

**Purpose:** Headshot sequence: slow-motion (`Time.timeScale = 0.2`) → camera zoom → "HEADSHOT!" text → restore.

---

### `KillFeed.cs`

**Purpose:** Elimination notifications. Queue-based (max 3 entries), auto-fade after 2.5s.

---

### `TouchFeedback.cs`

**Purpose:** Ripple ring effect at touch position. Object-pooled (pool size: 5).

---

### `CharacterGlow.cs`

**Purpose:** Pulsing glow sprite (scaled 1.15×) behind character. Sorted behind main sprite.

---

### `BowChargeEffect.cs`

**Purpose:** Line renderer from bow to spawn point that changes color (blue → red) as charge increases.

---

### `DeathEffect.cs`

**Purpose:** Fade-out all sprites + optional particle burst on death. Call `Die()` instead of `Destroy()`.

---

### `Ragdoll2D.cs`

**Purpose:** 6-part skeletal ragdoll (head, torso, 2 arms, 2 legs) connected by `HingeJoint2D` with angle limits. Impact force applied to nearest limb.

**Key Methods:** `Activate(Vector3 force, Vector3 hitPoint)`, `IsActive()`

**Auto-destruct:** Fades out after 3s, destroys after 4s.

---

### `VisualEffectsManager.cs`

**Purpose:** Central VFX bootstrapper. Creates missing `CameraShaker`, `TouchFeedback`, `KillFeed`, `AmbientEffects`, `SimpleParallax` at runtime.

---

### `ArcherSpriteController.cs`

**Purpose:** Drives the archer's visual sprite based on game state (idle, charge, fire, ragdoll) without using procedural primitives. Auto-loads Kenney character sprites and applies team tint.

---

### `SimpleParallax.cs`

**Purpose:** Camera-relative parallax for background layers. Each layer has a `parallaxFactor` (0=moves with camera, 1=stationary).

---

### `ButtonAnimator.cs` / `ScorePopAnimator`

**Purpose:** Press-scale animation for UI buttons (0.92× punch with overshoot). `ScorePopAnimator` handles score text pop on kill.

---

## 10. Audio

### `AudioManager.cs`

**Purpose:** Singleton audio manager (DDoL). Generates all SFX procedurally via `ProceduralAudio`.

**Public API:**
| Method | SFX |
|--------|-----|
| `PlayBowDraw()` | Charging sound |
| `PlayArrowFire()` | Release sound |
| `PlayArrowHit()` | Impact thud |
| `PlayPointScored()` | Score chord |
| `PlayWin()` / `PlayLose()` | Match end arpeggio |

**Settings:** `SetSFXVolume(float)`, `SetMusicVolume(float)`, `SetMuted(bool)` — persisted via `PlayerPrefs`.

### `ProceduralAudio` (static class)

**Methods:** `Tone()`, `Thud()`, `Chord()`, `Arpeggio()` — generate `AudioClip` at 44100Hz with waveform selection (Sine/Square/Triangle/Saw).

---

## 11. Editor Tools

### `AutoTagSetup.cs`

**Menu:** `Tools → Setup Stick Archers Tags`, `Tools → Check Stick Archers Setup`

**Creates:** Tag `Arena`, Layers `Ground`/`HitZone`/`Arrow`

---

---

### `PrefabValidator.cs`

**Menu:** `Tools → Validate Prefabs`, `Tools → Assign Prefabs to Bootstrap`, `Tools → Setup Android Build`

**Creates:** Placeholder `ArcherLocal.prefab` and `ArrowLocal.prefab` with required components.

---

### `SpriteImportSetup.cs`

**Menu:** Runs automatically on editor load

**Purpose:** Configures imported sprites with correct settings (PPU, Filter Mode, Compression) to ensure pixel art remains crisp and scales correctly. Crucial for Kenney pixel art assets.

---

### `SetupWizard.cs`

**Purpose:** Runtime setup validator. Auto-creates missing `GameArenaBootstrap`, `AudioManager`, `UIManager`. Checks gravity settings.

---

## 12. Utilities

### `WhiteSquareSpriteCache` (in Archer.cs)

**Purpose:** Static sprite cache. Loads `Resources/_WhiteSquare` or creates a 2×2 white texture at runtime. Used throughout for placeholder visuals.

### `CameraController.cs`

**Purpose:** Smoothly centers camera between both archers. Falls back to fixed position.

### `GameMode.cs`

**Purpose:** Static state holder for current game mode and AI difficulty. No MonoBehaviour.
