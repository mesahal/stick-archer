# Stick Archer — Gameplay Systems

> Detailed description of **what happens in the game** and **how the code implements it**.
> Use this as context when planning features or updating designs.
> Last updated: 2026-06-08.

---

## 1. Match flow (end to end)

### Starting a match

1. **Main Menu** — player picks Practice or Online, optional difficulty (Practice) and character.
2. `GameMode.Current` and `CharacterSelectUI.SelectedCharacter` are set.
3. **GameArena** scene loads; `GameArenaBootstrap.Start()` runs.
4. Arena platforms and background are generated; two archers spawn on opposite platforms.
5. HUD appears: score pill, health bars, wind indicator, charge meter (when charging).

### During a round

- Both archers are alive at full health (100 HP).
- Wind and gravity are fixed for the round (randomized at round start).
- Players charge and fire arrows; damage reduces HP.
- When HP reaches 0 → ragdoll death → kill credited to shooter → score +1.

### After a kill (not match end)

1. **2 second delay** (`Invoke ResetRound`).
2. Dead archer **respawns** at spawn point, health reset to 100.
3. **Online only:** master broadcasts new arena seed; both clients rebuild platforms.
4. Wind/gravity re-randomize (`WindSystem.RandomizeConditions`).
5. Round counter increments; "ROUND X" transition may show.

### Match end

- First to **5 kills** (`RemoteConfig score_to_win`) wins.
- `UIManager.ShowResult(true/false)` builds victory/defeat panel at runtime.
- Practice: `ProfileManager.GrantMatchRewards` runs (coins/XP persisted locally).
- Touch input disabled; REMATCH / MAIN MENU buttons shown.

---

## 2. Character movement & positioning

### What the player sees

Archers **do not walk or run**. Each stands on a platform and:

- **Bobs slightly** via `ArcherSpriteController` (idle breathing animation).
- **Aims automatically** via `BowSwayController` — bow angle oscillates up/down continuously.
- **Faces the opponent** — Player 1 faces right, Player 2 faces left (sprite mirrored).

### Spawn alignment

When spawning or respawning, feet align to platform surface:

```
SpawnAlignment.AlignFeetTo(archer, platformPoint)
```

Uses collider/sprite bounds so characters stand on grass tops, not inside tiles.

### Ragdoll (death)

On fatal hit, `Ragdoll2D` activates:

- Torso, head, and limb segments become independent `Rigidbody2D` pieces.
- Impact force applied at hit point.
- Character sprite hidden; ragdoll pieces fade after ~3s.
- Root transform kept for respawn.

---

## 3. Aiming system

### Auto sway (primary mechanic)

`BowSwayController` drives aim every frame:

```
phase += dt × swayFrequency × 2π     // 0.48 Hz default
t = (sin(phase) + 1) / 2              // 0..1
angle = lerp(-30°, +58°, t)
aimDir = (cos(angle)×facing, sin(angle))
```

- Each archer gets a **random start phase** so they don't sway in sync.
- Player 2's X component is flipped (`facing = -1`).

### Charge

While finger/mouse is held (`TouchControls` or AI hold timer):

```
chargeTimer += dt (capped at maxChargeTime = 1.5s)
chargeRatio = chargeTimer / maxChargeTime
launchForce = lerp(3, 9, chargeRatio)
```

Release fires at **current sway angle** + **current charge power**.

### Trajectory preview

While charging, `ArcherLocal.UpdateAimLine()` draws a yellow `LineRenderer` arc:

```
speed = launchForce / arrowMass          // mass = 0.5
g = |Physics2D.gravity| × gravityScale   // scale = 1.2
position(t) = origin + v0·t + (½·wind·t², -½·g·t²)
```

- 24 samples at 0.05s steps.
- Stops early if point hits **Ground** layer.

### Input (`TouchControls`)

- Tap/hold anywhere except top ~12% HUD strip → charge.
- Release → fire.
- **Not** drag-to-aim (Angry Birds style was removed); timing the sway is the skill.

---

## 4. Arrow physics & firing

### Launch

On fire (`ArcherLocal.FireArrow` / `Archer.FireArrow`):

1. Compute direction from `aimDirInput` (set by sway).
2. Spawn arrow at `spawnPoint + direction × 1.0` (offset avoids self-collision).
3. Apply impulse: `rb.AddForce(direction × launchForce, ForceMode2D.Impulse)`.
4. Pass owner `playerIndex` to arrow for friendly-fire checks.

### Arrow rigidbody defaults

| Property | Value |
|----------|-------|
| Mass | 0.5 |
| Gravity scale | 1.2 |
| Collision | Continuous |
| Lifetime | 4 seconds |

### In flight

Every **FixedUpdate**, `WindSystem.ApplyWind(rb)` adds horizontal force:

```
rb.AddForce(right × windForce × fixedDeltaTime)
```

Global gravity set each round: `Physics2D.gravity = (0, -9.81 × gravityMultiplier)`.

Arrow rotates to match velocity vector each frame.

### Spawn grace period

For **0.15s** after launch, arrow ignores **all** trigger collisions so it clears the shooter's body hitboxes.

---

## 5. Hit detection

### Hit zones (`HitZone` + `ArcherAutoSetup`)

At runtime, each archer gets child colliders:

| Zone | Shape | Size | Damage |
|------|-------|------|--------|
| Head | Circle | r=0.25 | 100 (instant kill) |
| Body | Box | 0.4×0.6 | 30% → 30 HP |
| Limbs | Capsule | 0.15×0.4 | 15% → 15 HP |

All zones are **triggers** on the `HitZone` layer.

### Local hit pipeline (`ArrowLocal`)

Uses multi-stage detection to avoid missed fast arrows:

1. **Grace period** — skip if active.
2. **Swept samples** — linecast from previous to current position.
3. **Overlap circle** at current position (radius 0.14).
4. **Visual body samples** — ray against opponent sprite bounds (fallback).
5. On hit → resolve zone damage → `ArcherLocal.OnHitReceived(shooter, damage)`.

Online arrows delegate damage through `Arrow.cs` RPCs so all clients stay in sync.

### Arrow sticking

On ground/obstacle hit (non-player), arrow may convert to `ArrowStuck` — kinematic, parented to surface.

### Birds

`BirdSpawner` / `BirdController` — flying obstacles that deflect or block arrows (optional arena hazard).

---

## 6. Health & damage

### Health pool

- **Max HP:** 100 per archer.
- **HUD update:** `UIManager.SetPlayerHealth(playerIndex, current, max)`.

### Damage application

```csharp
currentHealth = max(0, currentHealth - damage);
UIManager.SetPlayerHealth(...);
HitFlash.Flash();
DamageNumber.Spawn(...)  // optional floating text
```

### Health bar rendering (`UIManager.UpdateBar`)

Health bars are **horizontal fill images**, not sliders:

```
bar.rectTransform.anchorMax.x = health / maxHealth   // 0..1 width
bar.color = UIDesignSystem.GetHealthColor(pct)     // green → orange → red
```

Text overlay shows `"72 / 100"`.

Legacy heart icons still sync via `SetPlayerHP` but are secondary.

### Death

When `currentHealth <= 0`:

1. `isDead = true`
2. `TriggerRagdoll()` with last hit force/point
3. `PracticeGameManager.RecordKill(shooterIndex)` or online RPC equivalent
4. Headshot triggers extra FX: `HeadshotFeedback`, camera shake, post-FX punch

---

## 7. Scoring & rounds

### Kill credit

Scored when victim's HP hits 0, not on first hit. `RecordKill`:

- Increments shooter score.
- Updates HUD score pill (`UIManager.UpdateScore`).
- Plays point SFX, kill feed, camera shake.
- Checks win condition.

### Win / lose

| Condition | Result |
|-----------|--------|
| Score ≥ 5 | Match ends, result screen |
| Online timer expires | Higher score wins; tie = draw |

### Respawn (`ArcherLocal.Respawn`)

- Reset health, `isDead = false`
- Teleport to spawn position (feet aligned)
- Disable ragdoll, show sprite
- Re-enable sway and input

---

## 8. AI opponent

`AIController` on Player 2 in Practice mode.

### Targeting

Solves **projectile motion** for flat/lob angles:

```
disc = v⁴ - g·(g·dx² + 2·dy·v²)
θ = atan2(v² ± √disc, g·dx)
```

- Searches charge ratios 0.45 → 1.0 in 0.05 steps.
- Prefers lower (flatter) angle when both valid.
- Adds difficulty noise to angle and charge.

### Difficulty

| Level | Reaction delay | Aim noise | Charge noise |
|-------|----------------|-----------|--------------|
| Easy | 1.6–3.0s | ±12° | −0.20…+0.10 |
| Normal | 1.0–2.2s | ±5° | −0.08…+0.05 |
| Hard | 0.6–1.4s | ±1.5° | −0.03…+0.02 |

AI uses same sway/charge/fire pipeline as human (`SetAimAndCharge`, `SetHoldInput`).

---

## 9. Arena & environment

### Generation (`ArenaGenerator`)

- **6 layout variants** — building-style platforms at different heights.
- Uses Kenney platform sprites from `Resources/Platforms/`.
- Creates `Player1Spawn` / `Player2Spawn` empty transforms.
- **Online:** layout index from synced seed so both clients match.

### Background (`ArenaBackground`)

Layered parallax: sky gradient, far/near mountains, hills, clouds, trees, optional castle.

### Wind display

`WindSystem` pushes value to `UIManager.UpdateWind(windForce)` — arrow icon + strength in HUD pill.

---

## 10. Visual & audio feedback chain

| Event | Systems triggered |
|-------|-------------------|
| Charge start | Bow draw SFX, charge meter, optional glow |
| Fire | Release SFX, arrow trail, camera micro-shake |
| Body hit | Hit flash, damage number, impact particles, light shake |
| Headshot | Headshot text, slow-mo, heavy shake, post-FX |
| Kill | Kill feed, point SFX, score pop animation, strong shake |
| Match win | Victory/defeat panel, confetti (win), music stinger |

All SFX from `AudioManager` + `ProceduralAudio` (no audio files required).

---

## 11. Progression (post-match)

After Practice/Online match ends:

```
ProfileManager.GrantMatchRewards(won)
  coins = coins_per_match + (won ? coins_per_win : 0)
  xp    = xp_per_match    + (won ? xp_per_win    : 0)
```

- Persisted to `profile.json` in `Application.persistentDataPath`.
- Main menu `ProfileBadge` shows level, XP bar, coins.
- Victory screen shows score only (rewards card removed per design Fix 03).

---

## 12. Key files quick reference

| System | Primary files |
|--------|---------------|
| Bootstrap | `GameArenaBootstrap.cs` |
| Character | `ArcherLocal.cs`, `Archer.cs`, `BowSwayController.cs`, `ArcherSpriteController.cs` |
| Arrow | `ArrowLocal.cs`, `Arrow.cs`, `WindSystem.cs` |
| Damage | `HitZone.cs`, `ArcherAutoSetup.cs`, `Ragdoll2D.cs` |
| Input | `TouchControls.cs` |
| AI | `AIController.cs` |
| Scoring | `PracticeGameManager.cs`, `GameManager.cs` |
| HUD | `UIManager.cs`, `GameUISetup.cs`, `ChargeMeterUI.cs` |
| Arena | `ArenaGenerator.cs`, `ArenaBackground.cs` |

For architecture and extension points, see [ARCHITECTURE.md](ARCHITECTURE.md).
