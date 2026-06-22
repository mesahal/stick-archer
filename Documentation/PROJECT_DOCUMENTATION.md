# Stick Archer — Project Documentation

> Single source of truth for **what the game is, how it works (rules + formulas), what's
> implemented, and what isn't.** Read the [Implementation Status](#3-implementation-status-matrix)
> matrix for the at-a-glance "how much is done" view.

**Related docs:** [Documentation index](README.md) · [Architecture](ARCHITECTURE.md) · [Gameplay systems (detailed)](GAMEPLAY_SYSTEMS.md) · [Cricket League reference (feature study)](CRICKET_LEAGUE_REFERENCE.md) · [Screen fix guide](SCREEN_FIX_GUIDE.md)

- **Engine:** Unity 2022.3.55f1 LTS · Universal Render Pipeline 14 · new Input System
- **Target:** Android (landscape), min SDK 24, currently target SDK 33
- **Genre:** 1v1 turn-light physics archery duel — clone of *Stick Archers Battle*
- **Networking:** Photon PUN2 (online PvP) + fully local Practice (vs AI)
- **Last updated:** 2026-06-14 (presentation polish: hit flinch, fire recoil, app icon/logo + package id)

---

## 1. Game Overview

Two stick archers face off on a generated arena. Each player **taps & holds to charge**
a shot while the bow **sways automatically** up and down; **releasing fires** an arrow at
the current sway angle and charge power. Arrows are affected by **wind and gravity** that
randomize every round. First to **5 kills** wins. A physics **ragdoll** plays on death.

Two modes share one gameplay core:
- **Online** — Photon matchmaking, master-authoritative scoring, deterministic arena sync.
- **Practice** — local, vs an AI that solves real projectile math, 3 difficulties.

---

## 2. Development Plan & Roadmap

The project was assessed against production mobile standards (Miniclip-tier). The core
gameplay is production-quality; the commercial/live-ops shell was largely missing. Work is
phased so each stage is independently shippable.

| Phase | Scope | Status |
|---|---|---|
| **Core gameplay** | archery, ragdoll, AI, online PvP, UI, audio | ✅ **Done** (pre-existing) |
| **P0 — Store-ready build** | icons, signing, package id, SDK 34+, perf hot-paths, net resilience | ⬜ Not started |
| **P1 — Live-ops infra** | analytics + crash + remote config, funnel events | ✅ **Done** (this effort) |
| **P2 — Onboarding + progression** | currency/XP/levels, persistence, profile UI, FTUE, cloud save | 🟡 **Partial** — backend + UI done; FTUE + cloud save pending |
| **P3 — Monetization** | ad mediation (rewarded/interstitial), IAP | ⬜ Not started |
| **P4 — Meta & retention** | shop, daily rewards, quests, battle pass, leaderboards/ranked | ⬜ Not started — *see [CRICKET_LEAGUE_REFERENCE.md](CRICKET_LEAGUE_REFERENCE.md)* |
| **P5 — Hardening** | anti-cheat, atlasing/profiling, unit tests, CI, audio pass | ⬜ Not started |
| **Track V — Visual quality & art** | post-processing, sprite UI kit, VFX, character/env art | ⬜ Not started — *see [§12](#12-visual-quality--art-direction-plan)* |

> **Track V is a parallel workstream, not a sequential phase.** Its quick code-side wins
> (post-processing, juice) can land any time; its art-asset half runs alongside an artist.
> The original roadmap under-weighted visual quality — this track corrects that.

---

## 3. Implementation Status Matrix

✅ implemented & wired · 🟡 partial / foundation only · ⬜ not implemented

### Gameplay (the fun core)
| Feature | Status | Where |
|---|---|---|
| Charge-and-release shooting | ✅ | `Archer.cs`, `ArcherLocal.cs` |
| Auto bow sway aiming | ✅ | `BowSwayController.cs` |
| Ballistic trajectory preview (wind-aware) | ✅ | `ArcherLocal.UpdateAimLine` |
| Arrow physics + multi-stage hit detection | ✅ | `Arrow.cs`, `ArrowLocal.cs` |
| Segmented hit zones (head/body/limbs) | ✅ | `HitZone.cs` |
| Headshot instant-kill (opt-in) | ✅ | `HitZone.GetDamage` |
| 2D physics ragdoll on death | ✅ | `Ragdoll2D.cs` |
| Wind + variable gravity per round | ✅ | `WindSystem.cs` |
| AI opponent (real ballistic solver, 3 difficulties) | ✅ | `AIController.cs` |
| Round/score/win flow + respawn | ✅ | `GameManager.cs`, `PracticeGameManager.cs` |
| Arena generation (deterministic online) | ✅ | `ArenaGenerator.cs`, `GameArenaBootstrap.cs` |
| Character select (2 characters) | ✅ | `UI/CharacterSelectUI.cs` |
| Touch input | ✅ | `TouchControls.cs` |

### Online / Networking
| Feature | Status | Where |
|---|---|---|
| Photon matchmaking (random join/create) | ✅ | `NetworkManager.cs` |
| RPC score/arena/hit sync | ✅ | `GameManager.cs`, `Arrow.cs` |
| Reconnection / lobby timeout | ⬜ | — (P0) |
| Server-authoritative anti-cheat | ⬜ | — (P5) |

### UI / Audio
| Feature | Status | Where |
|---|---|---|
| HUD (score, health, charge, wind, pause) | ✅ | `UIManager.cs` |
| Pause menu, settings (audio sliders/mute) | ✅ | `UI/PauseMenuUI.cs`, `SettingsPanel.cs` |
| Result screen (victory/defeat) | ✅ | `UIManager.BuildRuntimeResultPanel` |
| Procedural SFX + music + persistence | ✅ | `AudioManager.cs` |
| Produced/licensed audio | ⬜ | — (P5) |

### Live-ops (P1 — done)
| Feature | Status | Where |
|---|---|---|
| Provider-agnostic analytics layer | ✅ | `Analytics/AnalyticsManager.cs` |
| Funnel events (session/menu/match/kill/end) | ✅ | instrumented across managers |
| Crash/error capture | ✅ | `AnalyticsManager.HandleLog` |
| Remote config (typed, local defaults) | ✅ | `Analytics/RemoteConfig.cs` |
| Firebase backend adapter | 🟡 | `FirebaseAnalyticsBackend.cs` (behind `FIREBASE_ENABLED`, needs account) |

### Progression / Economy (P2)
| Feature | Status | Where |
|---|---|---|
| Player profile (coins/XP/level/stats) | ✅ | `Progression/PlayerProfile.cs` |
| Match-end rewards (coins + XP) | ✅ | `ProfileManager.GrantMatchRewards` |
| Leveling curve + level-up events | ✅ | `ProfileManager.AddXp` |
| Local persistence (atomic JSON) | ✅ | `Progression/LocalProfileStore.cs` |
| Result-screen rewards card | 🟡 | Built (`BuildRewardsCard`) but **not shown on victory** (Fix 03); defeat may use later |
| Persistent profile badge (menu) | ✅ | `UI/ProfileBadge.cs` |
| Shop API (`TrySpendCoins`/`UnlockCharacter`) | ✅ (API only) | `ProfileManager.cs` |
| Gems currency + spend API | ⬜ | Spec: [FEATURES_COINS_GEMS_LEVEL.md](FEATURES_COINS_GEMS_LEVEL.md) |
| Hourly / daily login rewards | ⬜ | Spec: [FEATURES_COINS_GEMS_LEVEL.md](FEATURES_COINS_GEMS_LEVEL.md) |
| Level unlock table (arena/feature gates) | ⬜ | Spec: [FEATURES_COINS_GEMS_LEVEL.md](FEATURES_COINS_GEMS_LEVEL.md) |
| FTUE / tutorial | ⬜ | — (P2 remaining) |
| Cloud save backend | 🟡 | `CloudProfileStore.cs` (seam, behind `CLOUD_SAVE_ENABLED`) |

### Monetization / Meta / Store (P3–P5)
| Feature | Status |
|---|---|
| Ads (rewarded/interstitial/banner) | ⬜ |
| IAP (remove-ads, currency packs) | ⬜ |
| Shop UI, daily rewards, quests, battle pass | ⬜ |
| Leaderboards / ranked ELO / friends | ⬜ |
| App icons, signing, target SDK 34+ | ⬜ |
| Object pooling, sprite atlasing | ⬜ |
| Unit tests, CI/CD | ⬜ |

### Visual quality & art (Track V — see §12 + §13)
| Item | Status | Notes |
|---|---|---|
| URP post-processing (bloom/vignette/grade) | ⬜ | code-side, quick win |
| Sprite-based UI kit (9-slice, icons, fonts) | 🟡 | shapes/gradients/icons in Resources/UI/; UIArtProvider wired; procedural screens use them |
| VFX systems (trails, impacts, charge glow) | 🟡 | partial: shake, confetti, PostFXTriggers; + procedural fire recoil & get-hit flinch (`ArcherSpriteController` impulse system) |
| Screen/UI juice (eased transitions, count-ups) | 🟡 | partial: UITween, ButtonAnimator |
| Professional character art/animation | ⬜ | needs artist / asset pack |
| Environment & background art | 🟡 | sky gradient done; mountain layers exist but unwired in Main Menu |
| Per-scene design spec alignment | 🟡 | Main Menu, Defeat, Victory ~98%; HUD/lobby/etc. in §13 |

---

## 4. Game Rules

### Match flow
1. From the **Main Menu**: pick **Play Online** or **Practice** (+ difficulty); optional
   character select; then the **GameArena** scene loads.
2. `GameArenaBootstrap` builds the background, arena platforms, spawn points, then spawns
   the two archers and shows the HUD.
3. Players duel. Each kill scores a point for the shooter.
4. After each kill (not match-ending): **2-second** delay, then respawn. **Online** also
   rebuilds the arena from a master-broadcast seed so both clients match.
5. **Win condition:** first to **`score_to_win` (default 5)** kills. Result screen shows,
   touch input disabled, room closed (online).
6. Online **time-up** path: higher score wins, equal = draw.

### Combat rules
- **Health:** 100 per archer.
- **Default body hit:** 34 damage (`OnHitReceived` default).
- **Zone damage:** `damagePercent × 100` (default zone = **30**); headshot zone can be
  flagged **instant-kill (100)**.
- **Zones:** Head (circle r 0.25), Body (box 0.4×0.6), Limbs (capsule 0.15×0.4).
- **Spawn grace:** local arrows ignore the shooter's own hitzones for **0.15 s** after launch.
- **On death:** ragdoll activates with the last impact force; kill is recorded.

### Controls
- **Tap & hold** anywhere (except top ~12% HUD strip) to charge; **release** to fire.
- Aim is **not** dragged — the bow **sways automatically**; timing the release is the skill.
- Editor-only keyboard fallback: hold **Space** to charge, arrow keys nudge angle.

---

## 5. Formulas & Constants

### Charge & launch
```
chargeRatio   = clamp01(chargeTimer / maxChargeTime)        // maxChargeTime = 1.5 s
launchForce   = lerp(minLaunchForce, maxLaunchForce, ratio) // 3 → 9
minHoldToFire = 0.02 s (local) / 0.08 s (online)
arrow impulse = launchDir * launchForce   (ForceMode2D.Impulse)
arrow rb      = mass 0.5, gravityScale 1.2, continuous collision
spawnOffset   = 1.0 unit ahead of the spawn point along launchDir
arrow lifetime= 4 s
```

### Aiming sway
```
phase   += dt · swayFrequency · 2π            // swayFrequency = 0.48 Hz
t        = (sin(phase) + 1) / 2               // 0..1
aimAngle = lerp(minAngle, maxAngle, t)        // -30° → +58°
aimDir   = (cos(aimAngle)·facing, sin(aimAngle))   // facing = -1 for player 2
```
Each archer gets a random start phase so the two don't sway in sync.

### Ballistic trajectory preview
```
speed     = launchForce / arrowMass           // arrowMass = 0.5
g         = |Physics2D.gravity| · gravityScale // gravityScale = 1.2
windAccel = WindSystem.windForce
v0        = launchDir · speed
p(t)      = spawn + v0·t + (½·windAccel·t², −½·g·t²)
```
Sampled at `t = i·0.05 s` for 24 steps; stops early when it overlaps the Ground layer.

### Wind & gravity (randomized each round)
```
windForce         = random(−maxWind, +maxWind)        // maxWind = 8
gravityMultiplier = random(minGravity, maxGravity)    // 0.5 → 1.5
Physics2D.gravity = (0, −9.81 · gravityMultiplier)
wind on arrow     = AddForce(right · windForce · fixedDeltaTime, Force)  // every FixedUpdate
```

### AI ballistic solver (`AIController`)
Solves the launch angle θ to hit (dx, dy) at speed v under gravity g:
```
disc      = v⁴ − g·(g·dx² + 2·dy·v²)          // no solution if disc < 0
lowAngle  = atan2(v² − √disc, g·dx)           // flatter shot (preferred)
highAngle = atan2(v² + √disc, g·dx)           // lob
// pick the first of {low, high} within the bow's [5°, 85°] range
AI gravity = 9.81 · 1.2 ;  speed = force / 0.5
```
- Search: ramps charge ratio **0.45 → 1.0** in 0.05 steps, takes first feasible shot.
- Target directly behind (dx ≤ 0.05) → fixed **80° lob**.
- Hold time = `chosenRatio · maxChargeTime + 0.05 s`. Aims at target torso (+0.4 y).

**Difficulty modifiers** (accuracy/reaction, *not* damage):
| Difficulty | Reaction (s) | Aim noise | Charge noise |
|---|---|---|---|
| Easy | 1.6 – 3.0 | ±12° | −0.20 … +0.10 |
| Normal | 1.0 – 2.2 | ±5° | −0.08 … +0.05 |
| Hard | 0.6 – 1.4 | ±1.5° | −0.03 … +0.02 |

### Ragdoll (`Ragdoll2D`)
```
masses:   torso 2, head 1, limbs 0.5      gravityScale 1
joints:   arms −120°…+60°, legs −30°…+90°, head −45°…+45°
fade:     3 s hold, then 1 s alpha fade, parts destroyed (root kept for respawn)
impact:   force applied to the limb nearest the hit point
```

### Progression & economy
```
match reward coins = coins_per_match (10) + (won ? coins_per_win (25) : 0)
match reward xp    = xp_per_match  (20) + (won ? xp_per_win  (50) : 0)
XpToAdvance(level) = 100 + (level − 1) · 50      // 100,150,200,…  (starts at level 1)
AddXp rolls over multiple levels in one grant.
Lifetime stats count the LOCAL player's kills only.
minimum account level = 1
maximum account level = uncapped in current code; no "MAX" UI until max_account_level is approved/implemented
```

---

## 6. Architecture

### Scenes
- **MainMenu** — `MainMenuController` (play/practice/difficulty, mounts `ProfileBadge`).
- **GameArena** — `GameArenaBootstrap` builds everything at runtime; UIManager drives HUD.

### Managers (singletons, most self-bootstrapping)
| Manager | Role | Lifetime |
|---|---|---|
| `AnalyticsManager` | events, sessions, crash capture | self-boots `BeforeSceneLoad`, DontDestroyOnLoad |
| `ProfileManager` | profile, currency/XP, persistence | self-boots `BeforeSceneLoad`, DontDestroyOnLoad |
| `AudioManager` | SFX/music + settings | DontDestroyOnLoad |
| `NetworkManager` | Photon connect/matchmaking | scene |
| `GameManager` / `PracticeGameManager` | scoring, rounds, respawn | scene |
| `UIManager` | HUD/result/panels | scene |
| `WindSystem` | wind + gravity per round | scene |

### New layers added in P1/P2 (provider-agnostic)
```
Game code ─▶ Analytics (facade) ─▶ AnalyticsManager ─▶ IAnalyticsBackend ┬ Debug (default)
                                                                          └ Firebase (#if FIREBASE_ENABLED)
Game code ─▶ ProfileManager ─▶ IProfileStore ┬ LocalProfileStore (default, JSON)
                                              └ CloudProfileStore (#if CLOUD_SAVE_ENABLED)
RemoteConfig (local defaults; a backend may Apply() server overrides)
```
Folder READMEs: `Assets/Scripts/Analytics/README.md`, `Assets/Scripts/Progression/README.md`.

---

## 7. Tunable Config (RemoteConfig keys)

All server-tunable without a rebuild once a remote-config backend is wired. Defaults live
in `Analytics/RemoteConfig.cs`.

| Key | Default | Used by |
|---|---|---|
| `score_to_win` | 5 | `GameManager`, `PracticeGameManager` |
| `round_reset_delay_sec` | 2 | (reserved) |
| `coins_per_match` | 10 | match reward |
| `coins_per_win` | 25 | match reward |
| `xp_per_match` | 20 | match reward |
| `xp_per_win` | 50 | match reward |
| `interstitial_frequency` | 2 | reserved (P3) |
| `rewarded_coins` | 50 | reserved (P3) |

---

## 8. Analytics Event Taxonomy

Names in `Analytics/GameEvents.cs`. Every event also carries user properties
`install_id`, `platform`, `app_version`, `level`, `coins`.

| Event | Trigger | Key params |
|---|---|---|
| `session_start` / `session_end` | launch / pause / quit | `session_id`, `session_sec` |
| `menu_play_online` / `menu_practice` | menu buttons | `difficulty` (practice) |
| `difficulty_changed` | dropdown | `difficulty` |
| `match_start` | GameArena loads | `mode`, `difficulty`, `character` |
| `kill` | each scored kill | `shooter_slot`, `victim_slot`, `p1_score`, `p2_score` |
| `match_end` | match decided / time-up | `winner_slot`, `local_won`, `kills`, `duration_sec` |
| `currency_earned` / `currency_spent` | economy change | `amount`, `balance`, `reason` |
| `level_up` | level advance | `level` |
| `app_error` | logged error/exception | `error_type`, `message` |

---

## 9. What's NOT Implemented (gaps)

- **Onboarding/FTUE** — no first-time tutorial (P2 remaining; highest D1-retention item).
- **Cloud save / accounts** — local only; `CloudProfileStore` is a documented seam.
- **Monetization** — no ads, no IAP (P3).
- **Meta/retention** — no shop UI, daily rewards, quests, battle pass, leaderboards (P4).
- **Store readiness** — app launcher icon + adaptive icon now generated and assigned, and
  package id set to `com.stickarcher.battle` (`Tools ▸ Branding ▸ Generate Icon + Logo`,
  `Assets/Editor/BrandingSetup.cs`). Still missing: signing keystore, target SDK 34+ (P0).
  The generated icon is a clean procedural placeholder emblem, not a designed brand logo.
- **Performance** — `FindObjectsOfType<Archer>()` in an arrow hot path (`Arrow.cs`),
  no real object pooling, no sprite atlasing (P0/P5).
- **Network resilience** — no reconnection or lobby timeout (P0).
- **QA** — no unit tests, no CI/CD (P5).

---

## 10. Verification

> The P1/P2 code is written and reviewed but **not yet compiled in Unity** (it was authored
> outside the editor; `.meta` files generate on first import). Open the project in Unity and:

1. **Compile** — Console should show no errors; new scripts import under `Assets/Scripts/Analytics`,
   `Assets/Scripts/Progression`, `Assets/Scripts/UI/ProfileBadge.cs`.
2. **Funnel** — press Play, watch the Console:
   `session_start` → `match_start { mode=practice … }` → `kill { … }` per score →
   `match_end { … duration_sec=N }` → `currency_earned { amount=… }`.
3. **Progression** — finish a Practice match: the **result screen rewards card** shows
   `+N coins`, level, and the XP bar; the **main-menu badge** reflects the new total.
4. **Persistence** — relaunch the app: coins/level persist (`profile.json` in
   `Application.persistentDataPath`).
5. **Remote config** — temporarily edit `score_to_win` default in `RemoteConfig.cs` and
   confirm matches end at the new score.

---

## 11. Reference — Key Files

- **Gameplay:** `Archer.cs`, `ArcherLocal.cs`, `Arrow.cs`, `ArrowLocal.cs`, `AIController.cs`,
  `BowSwayController.cs`, `Ragdoll2D.cs`, `HitZone.cs`, `WindSystem.cs`
- **Flow:** `GameManager.cs`, `PracticeGameManager.cs`, `GameArenaBootstrap.cs`, `NetworkManager.cs`
- **UI:** `UIManager.cs`, `MainMenuController.cs`, `SettingsPanel.cs`, `UI/CharacterSelectUI.cs`,
  `UI/PauseMenuUI.cs`, `UI/ProfileBadge.cs`, `UI/UIDesignSystem.cs`
- **Live-ops (P1):** `Analytics/` (AnalyticsManager, Analytics, IAnalyticsBackend,
  DebugAnalyticsBackend, FirebaseAnalyticsBackend, RemoteConfig, GameEvents)
- **Progression (P2):** `Progression/` (ProfileManager, PlayerProfile, IProfileStore,
  LocalProfileStore, CloudProfileStore)
- **Config:** `ProjectSettings/`, `Packages/manifest.json`

---

## 12. Visual Quality & Art Direction Plan (Track V)

> **Why this track exists:** the original roadmap under-weighted graphics. The gameplay
> *systems* are strong, but the *presentation* is largely programmer-built: flat-color
> procedural UI, basic 4-frame character sprites, and only partial VFX. Production
> (Miniclip-tier) titles win on a polished, cohesive visual layer. This track makes visual
> quality a first-class workstream. **Status: documented, not started.**

### Current visual state (honest baseline — updated 2026-06-08)
- **UI:** Main Menu, Victory, and Defeat screens ~98% aligned to SVG specs (Fix 01–03).
  Runtime HUD still uses procedural rects; sprite kit partially wired via `UIArtProvider`.
- **Characters:** 4 sprites each (idle/charge/fire/ragdoll) + procedural breathing/lean; no
  skeletal animation. (`ArcherSpriteController`)
- **Environment:** layered parallax backgrounds + Kenney platform tiles. (`ArenaBackground`, `ArenaGenerator`)
- **Juice/VFX present but partial:** camera shake, confetti, PostFXTriggers, UITween — inconsistent on HUD/lobby screens.

### Target ("production-grade") definition
Cohesive art direction; sprite-based UI kit with states + icons + real fonts; layered VFX on
every key action (charge, fire, hit, death, score); full-scene post-processing; smooth screen
transitions; and professionally drawn/animated characters and backgrounds.

### The two halves (important — different owners)

**V-CODE — engineering-side polish (buildable in-repo, no external art needed):**
| Item | What it includes | Effort | Impact |
|---|---|---|---|
| **V1 · URP post-processing** | Global Volume: bloom, vignette, color grading, subtle CA/lens; wire existing `PostFXTriggers` to it | S | High (cheapest big lift) |
| **V2 · VFX systems** | Particle systems for arrow trail, fire burst, hit sparks, blood/dust, landing dust, charge glow; pooled | M | High |
| **V3 · Screen/UI juice** | Eased panel transitions, screen wipes between scenes, coin/XP count-up animations, squash-stretch on hits | M | Med-High |
| **V4 · Sprite-based UI kit** | 9-slice panels/buttons with normal/hover/press/disabled states, icon system, consistent typography; refactor `UIManager` procedural rects to use it | L | High (needs UI sprites — see V-ART) |
| **V5 · Perf for visuals** | Sprite atlasing + draw-call budget so the above holds 60fps on mid Android | M | Enabler |

**V-ART — asset creation (needs an artist, asset-store pack, or AI-generated art — NOT producible by the coding agent):**
| Item | Notes |
|---|---|
| **Character art + animation** | Higher-fidelity sprites or skeletal rig (Spine/DragonBones + Unity importer); more states (run, hurt, taunt, victory). *Interim:* procedural draw/recoil on fire and a directional flinch on getting hit are in (`ArcherSpriteController` impulse system) — covers ~70% of the perceived "proper animation" jump without new art. |
| **UI kit assets** | Button/panel/frame sprites, icon set, 1–2 licensed fonts feeding V4 |
| **Environment art** | Painted backgrounds, parallax layers, platform tilesets, props |
| **VFX textures** | Particle sprites/gradients feeding V2 |
| **Brand** | App icon, splash, store screenshots (also unblocks P0 store readiness) |

### Recommended sequencing
1. **Quick win first:** V1 (post-processing) — large perceived-quality jump for low effort, no assets.
2. **In parallel:** kick off V-ART (commission/buy character + UI kit) since it has the longest lead time and gates V4.
3. V2 + V3 (VFX + juice) next — mostly code, big "feel" payoff; slot particle textures from V-ART as they arrive.
4. V4 (sprite UI kit) once UI assets exist; refactor procedural UI onto it.
5. V5 (atlasing/perf) folds into P0/P5 hardening.

### Dependencies / interactions
- **Unblocks P0:** app icon + splash (V-ART brand) are also store-submission blockers.
- **Touches procedural UI:** V4 refactors `UIManager`/`VisualOverhaul_v*`; coordinate so the
  P2 result-rewards card and `ProfileBadge` migrate onto the new kit rather than being redone.
- **Asset pipeline:** enabling sprite atlases (V5) should precede importing large art sets.

### Asset-sourcing options (for the V-ART half)
- **Unity Asset Store** packs (fastest; risk: generic look).
- **Commission an artist** (best cohesion; longest lead/cost).
- **AI-generated 2D art** + cleanup (fast/cheap; needs consistency passes and licensing care).

---

## 13. Scene-by-Scene Visual Gap Analysis

> How each scene was built, what the design spec says, and the exact delta between them.
> Design source: `designs/*.svg`. Implementation status: ✅ matches · 🟡 partial/wrong · ❌ missing.
> **Last audited: 2026-06-07.**

---

### How scenes are built (architecture)

| Scene | Build method | When |
|---|---|---|
| **MainMenu** | `VisualOverhaul_v12.cs` wipes Canvas and rebuilds from scratch | Editor-time (menu item `Tools → Design Sync → 4`) |
| **GameArena HUD** | Scene-baked hierarchy + `GameUISetup.cs` runtime normalizer + `UIManager.cs` | Runtime on Start |
| **Result screen** | Fully runtime: `UIManager.BuildRuntimeResultPanel()` | Runtime on match end |
| **Pause menu** | Scene-baked, controlled by `PauseMenuUI.cs` | Runtime |
| **Character Select** | Scene-baked panel, styled by `CharacterSelectUI.cs` | Runtime |
| **Lobby** | Scene-baked panel, text updated by `NetworkManager` / `UIManager.ShowLobby()` | Runtime |
| **Settings modal** | Scene-baked, wired by `SettingsPanel.cs` | Runtime |
| **Error modal** | Scene-baked, shown by `NetworkErrorUI.cs` | Runtime |
| **Round transition** | Fully runtime: `RoundTransition.cs` / `GameUISetup.SetupRoundDisplay()` | Runtime |

**Key implication:** Editor-built scenes (MainMenu, pause, character select, lobby, settings, error) must be regenerated in Unity when the build script changes. Runtime-built UIs (result screen, round transition) update automatically on next play.

---

### 01 · Main Menu (`designs/01_main_menu.svg`)

**How built:** `VisualOverhaul_v12.cs` + runtime `MainMenuController.cs` (button wiring, ProfileBadge mount).

| Element | Design | Current | Status |
|---|---|---|---|
| Sky gradient background | `bg_sky_menu.png` full-screen | ✅ Applied | ✅ |
| "STICK ARCHER" title | Gold vertex gradient (#FFF3A0→#E6B800), outline #3A2200, bob animation | ✅ Implemented | ✅ |
| "BATTLE OF THE BOWS" subtitle | 28px, letter-spacing 12, mid-white | ✅ Implemented | ✅ |
| Decorative ornament `> • <` | Gold, below subtitle | ✅ Implemented | ✅ |
| PLAY ONLINE button | pill_128 shape, `btn_primary` gradient, globe icon 56×56 | ✅ Implemented | ✅ |
| VS COMPUTER button | pill_128 shape, `btn_success` gradient, robot icon 56×56 | ✅ Implemented | ✅ |
| Gear button (top-right) | circle_128 bg, gear icon | ✅ Implemented | ✅ |
| Footer text (version / copyright) | Both sides, dim white | ✅ Implemented | ✅ |
| **Far mountain silhouette** | `bg_mountains_far.png`, bottom-anchored, #1A2440 @ 85% alpha | Asset exists, never added to Canvas | ❌ |
| **Near mountain silhouette** | `bg_mountains_near.png`, bottom-anchored, #0F1A2E | Asset exists, never added to Canvas | ❌ |
| **Ground strip** | Near-black bar + 1px gold accent at very bottom | Not in scene | ❌ |
| **Title radial glow** | Yellow ellipse #FFE066 60% alpha, behind title | Not in scene | ❌ |
| Stars parallax layer | Tiny dots, 50% opacity | Not in scene | ❌ |

**Fix:** Add 4 layers (glow, far mtns, near mtns, ground) to `VisualOverhaul_v12.cs` immediately after the BG image. Assets are in `Assets/Art/Backgrounds/`. Re-run the menu item once.

---

### 02 · Character Select (`designs/02_character_select.svg`)

**How built:** Scene-baked panel; `CharacterSelectUI.cs` applies colors + card art at runtime.

| Element | Design | Current | Status |
|---|---|---|---|
| Character cards (gold border when selected) | 4px gold gradient border | Solid gold color via `UIDesignSystem.Gold` (no gradient) | 🟡 |
| Check badge on selected card | Gold circle check | GameObject toggled on/off | ✅ |
| Card opacity (inactive = 85%) | `CanvasGroup.alpha` | ✅ Implemented | ✅ |
| Character art | Loaded from Resources | ✅ Implemented | ✅ |
| **VS divider circle** | r=60, dark bg, gold "VS" 56px bold | Not in code or scene | ❌ |
| **Stat bars (Speed/Power)** | Numeric bars, values shown | `HideCardDetails()` hides them for both cards | ❌ |
| Title "CHOOSE YOUR ARCHER" | 56px bold, Editor-placed TMP | Must be manually placed in scene | 🟡 |
| Confirm / Back buttons | Pill shape, gradient fill | Referenced but no runtime styling | 🟡 |

**Fix priority:** VS divider (code addition), stats unhide/display (script change), button styling (UIArtProvider pill shape).

---

### 03 · Settings Modal (`designs/03_settings_modal.svg`)

**How built:** Scene-baked modal; `SettingsPanel.cs` wires sliders, toggle, and close.

| Element | Design | Current | Status |
|---|---|---|---|
| SFX slider + value text | Slider + "80%" | ✅ Implemented | ✅ |
| Music slider + value text | Slider + "55%" | ✅ Implemented | ✅ |
| Mute toggle (animated knob) | Green on / gray off with knob slide | ✅ Implemented | ✅ |
| Modal open/close animation | Scale + alpha fade | ✅ Implemented | ✅ |
| **Audio section icon** | Yellow icon box (speaker) + "AUDIO" label | Not in code | ❌ |
| **Controls section icon** | Green icon box (radio) + "CONTROLS" label | Not in code | ❌ |
| **Aim Assist toggle** | Custom toggle (on state) | Not wired in code | ❌ |
| **Reset progress link** | Red underlined text at bottom | Not implemented | ❌ |
| Modal background styling | Dark gradient + white border 0.08 | Deferred to Editor setup | 🟡 |
| Numeric display format | Design shows "80" (no %); code shows "80%" | Minor mismatch | 🟡 |

**Fix priority:** Section icons (runtime image add), Aim Assist toggle wiring, Reset link (confirmation modal needed).

---

### 04 · Lobby (`designs/04_lobby.svg`)

**How built:** Scene-baked panel; status text updated by `UIManager.ShowLobby()` + Photon callbacks.

| Element | Design | Current | Status |
|---|---|---|---|
| Status text ("Connecting…" / "Finding…") | Dynamic text | ✅ `lobbyStatusText` | ✅ |
| "FINDING OPPONENT" title | 52px bold, Editor-placed | Must be placed in scene | 🟡 |
| **Player card (YOU)** | 660×700, character art, name, stats, gold border | No code for card rendering | ❌ |
| **Opponent card** | Faded version, "???" | No code | ❌ |
| **VS circle** | r=60, dark bg, gold "VS" | Not implemented | ❌ |
| **Gold check badge** | On YOUR card top-right | Not implemented | ❌ |
| **Character art sync** | Show the character selected in previous screen | No code links CharacterSelectUI to lobby | ❌ |
| **Cancel Search button** | Red gradient pill, "CANCEL SEARCH" | Not in code or scene | ❌ |
| Watermark "STICK ARCHER" | 120px, gold, 20% opacity background | Not implemented | ❌ |

**Most work needed of all screens.** Requires a runtime lobby card builder that reads `CharacterSelectUI.SelectedCharacter` and displays the appropriate art/stats.

---

### 05 · Game HUD (`designs/05_game_hud.svg`)

**How built:** Scene-baked HUD + `GameUISetup.cs` normalizer runs on Start.

| Element | Design | Current | Status |
|---|---|---|---|
| P1/P2 score badge (gold circle) | circle_128, gold, score text inside | ✅ `circle_128` sprite applied | ✅ |
| Health bars | pill_bar shape, gradient fill | Shape applied; gradient fill = computed color | 🟡 |
| Charge meter | pill_bar container, green→red gradient fill | pill_bar shape applied; fill = ChargeMeterUI gradient | ✅ |
| Wind badge | pill_128 shape, WIND label + arrow + value | ✅ pill_128 applied | ✅ |
| Pause button | Circle top-center | ✅ 96×96, positioned | ✅ |
| "RELEASE TO FIRE" label | Appears at charge ≥ 50% | ✅ Implemented | ✅ |
| "MAX!" label | Appears at 100% charge, red pulse | ✅ Implemented | ✅ |
| Player names | Below score badge, 22px | Fields exist but may not be wired in scene | 🟡 |
| Round indicator "3/5" | Design shows badge | Intentionally destroyed by `GameUISetup` | ❌ (removed) |

**HUD is the best-aligned screen.** Main remaining gap is health bar gradient fill (needs hp_full/hp_low gradient sprite applied to fill Image via UIArtProvider).

---

### 06 · Pause Menu (`designs/06_pause_menu.svg`)

**How built:** Scene-baked modal; `PauseMenuUI.cs` handles show/hide and button callbacks.

| Element | Design | Current | Status |
|---|---|---|---|
| Dim overlay (black 70%) | Full-screen black alpha | ✅ `dimBackground` Image | ✅ |
| Resume / Settings / Quit buttons | Wired to callbacks | ✅ Wired | ✅ |
| Modal open animation (scale + fade) | 0.25s ease-out | ✅ `AnimateOpen()` coroutine | ✅ |
| Time.timeScale = 0 on pause | Required for gameplay freeze | ✅ Implemented | ✅ |
| **Gold accent top bar** | 6px gold gradient strip at top of modal | Not dynamically created | ❌ |
| **Pause icon circle** | r=64 circle with pause rectangles | Not dynamically created | ❌ |
| **"PAUSED" title** | 56px bold white | Must be in scene as TMP | 🟡 |
| **Subtitle** | "Take a breath, archer" | Must be in scene manually | 🟡 |
| Status text | "Score 3 — 2 · Tap Resume" | Round number missing from display | 🟡 |
| Button icons (play/gear/quit) | Icon on left of each button | Not in code | ❌ |

---

### 07 · Round Transition (`designs/07_round_transition.svg`)

**How built:** Fully runtime — `GameUISetup.SetupRoundDisplay()` creates GO; `RoundTransition.cs` animates.

| Element | Design | Current | Status |
|---|---|---|---|
| Display/fade durations | 0.3s in, 1.5s hold, 0.3s out | ✅ Matches | ✅ |
| Arena name text | Below round number | ✅ `arenaNameText` field | ✅ |
| **Diagonal swipe wipe color** | Gold gradient band | Dark panel color `(0.10, 0.12, 0.18)` | ❌ |
| **Wipe duration** | ~1.2s | 0.5s (too fast) | ❌ |
| **Round number size/color** | 380px, gold (#FFD933), stroke #3A2200 | Not enforced — color is white, no stroke | ❌ |
| **Score subtitle** | "SCORE 3 — 2" 34px below number | Not created | ❌ |
| **"FIRST TO 5 WINS"** | 22px faded, below score | Not created | ❌ |
| **Decorative arrows** | Two ornament arrows beside number | Not created | ❌ |
| **Dim overlay** | Black 45% behind panel | Not applied | ❌ |
| Round color states | Green R1-2, gold R3, red R5 | Always white | ❌ |

**Round transition is the most out-of-spec runtime screen.** Needs color, duration, size, and content fixes in `RoundTransition.cs`.

---

### 08/09 · Victory / Defeat Screens (`designs/08_results_victory.svg`, `09_results_defeat.svg`)

**How built:** Fully runtime — `UIManager.BuildRuntimeResultPanel()` + `StyleResultButton()`.

#### Victory
| Element | Design | Current | Status |
|---|---|---|---|
| Background | Radial gradient #3A2A6A→#0A0E1C | `bg_sky_menu` gradient sprite | 🟡 |
| Score card | Gradient fill, **gold** 2px border | Solid fill, **white** 0.08 border | 🟡 |
| "FINAL SCORE" label | Gold #FFD933 | White 0.6 alpha | ❌ |
| REMATCH button | Green gradient, pill shape | Blue primary (should be green success) | ❌ |
| Glow overlay | **Gold** radial spotlight | Blue tint overlay | ❌ |
| Confetti | 11 rects + 4 circles, various colors | `ConfettiBurst.cs` exists; `victoryEffects` set to null in builder | 🟡 |
| Decorative stars under title | 4 gold star ornaments | Not created | ❌ |

#### Defeat
| Element | Design | Current | Status |
|---|---|---|---|
| Background | Radial gradient #3A1A28→#0A060C | `panel_dark` sprite (close match) | 🟡 |
| Rain lines | 8 diagonal white lines 0.06 alpha | ✅ `BuildResultRain()` | ✅ |
| Broken arrow ornament | Shaft + head + crack marks | ✅ `BuildBrokenArrow()` (shafts + head, no cracks) | 🟡 |
| Score card | Subtle white border 0.08 | ✅ Correct | ✅ |
| REMATCH button (blue) | Blue gradient, pill | ✅ Applied | ✅ |
| Score display color logic | P1 white 0.7, P2 red #F23F3F | ✅ Correct | ✅ |

**Defeat screen is the best result-screen implementation.** Victory needs the most work: glow color, button color, star ornaments, "FINAL SCORE" label color.

---

### 10 · Error Modal (`designs/10_error_modal.svg`)

**How built:** Scene-baked; `NetworkErrorUI.cs` sets text and wires buttons.

| Element | Design | Current | Status |
|---|---|---|---|
| Title "CONNECTION LOST" | Set dynamically | ✅ Implemented | ✅ |
| Retry / Main Menu buttons | Wired to callbacks | ✅ Wired | ✅ |
| **Warning icon + glow** | Large triangle-in-circle, red radial glow | Not rendered — Editor-assigned Image expected | ❌ |
| **Auto-retry countdown** | "Auto-retry in 5s" with live timer | No timer logic | ❌ |
| Error code badge | Small labelled box with disconnect reason | Text shown but badge not styled | 🟡 |
| Retry/Back icons on buttons | Icon left of label | Not in code | ❌ |
| Modal border | Red top stripe 6px | Not dynamically created | ❌ |

---

### Gap Summary — Effort vs Impact (updated 2026-06-08)

| Screen | Design match % | Biggest remaining gap | Effort |
|---|---|---|---|
| Main Menu | ~98% | Minor spacing/QA | S |
| Defeat screen | ~98% | Minor spacing/QA | S |
| Victory screen | ~98% | Minor spacing/QA | S |
| Game HUD | ~85% | Health bar gradient fill (Fix 04) | S |
| Round transition | ~40% | Wipe color, number styling | M |
| Pause menu | ~65% | Icons, gold accent bar | M |
| Character select | ~60% | VS divider, stat bars | M |
| Settings modal | ~70% | Section icons, Aim Assist | S–M |
| Error modal | ~60% | Warning icon, auto-retry timer | M |
| Lobby | ~30% | Player card UI, character sync | L |

**Recommended fix order (Fix 04+):**
1. HUD health bar gradient (Fix 04)
2. Round transition styling (Fix 05)
3. Pause menu icons (Fix 06)
4. Character select VS divider (Fix 07)
5. Settings / Error / Lobby (Fix 08–10)

---

## 14. Codebase cleanup log

**2026-06-14** — Presentation polish pass (feel + branding):

| Change | Where |
|--------|-------|
| Procedural **get-hit flinch** — body shoved along arrow direction, rot-kick + squash, damage-scaled | `ArcherSpriteController.TriggerHitReaction`, called from `ArcherLocal`/`Archer.OnHitReceived` |
| Procedural **fire recoil / follow-through** on release (was a flat 2-frame pose swap) | `ArcherSpriteController.FireFlash` + shared `PlayImpulse` damped-envelope engine |
| **App icon + adaptive icon** generated & assigned; **package id** set `com.stickarcher.battle`; menu logo emblem | `Assets/Editor/BrandingSetup.cs` (`Tools ▸ Branding ▸ Generate Icon + Logo`), `VisualOverhaul_v12.cs` |

> Run order in Unity: generate branding (Tools menu) → rebuild Main Menu (Design Sync 4) →
> Play-test flinch/recoil. Impulse magnitudes are tunable at the call sites. Still pending for
> true production feel: drawn/skeletal character frames (V-ART) and a designed brand logo.

**2026-06-08** — Removed unused code before next feature phase:

| Removed | Reason |
|---------|--------|
| `HealthBarUI.cs` | Superseded by `UIManager.UpdateBar` |
| `CameraController.cs` | Fixed camera in bootstrap; never wired |
| `EnvironmentManager.cs` | Superseded by `ArenaGenerator` + round rebuild |
| Empty `CityAssets`, `Pixel Adventure 1`, `Audio`, `Prefabs` folders | Never populated |
| `_downloads/` Kenney zips | Assets already in `Assets/Art/` |
| `patch.cs` | Dummy root file |

See [ARCHITECTURE.md §11](ARCHITECTURE.md#11-removed--dead-code-2026-06-08-cleanup). Full gameplay narrative: [GAMEPLAY_SYSTEMS.md](GAMEPLAY_SYSTEMS.md).
