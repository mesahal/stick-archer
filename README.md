# 🏹 Stick Archers Battle

A 2D archery combat game built with **Unity 2022 LTS** and **Photon PUN 2** for real-time online multiplayer. Players control stick-figure archers standing on procedurally generated building platforms, aiming and firing arrows at each other with physics-based projectile trajectories affected by wind and gravity.

---

## Table of Contents

- [Features](#-features)
- [Game Modes](#-game-modes)
- [Architecture Overview](#-architecture-overview)
- [Project Structure](#-project-structure)
- [Core Systems](#-core-systems)
- [Visual Effects Pipeline](#-visual-effects-pipeline)
- [UI System](#-ui-system)
- [Editor Tools](#-editor-tools)
- [Setup & Installation](#-setup--installation)
- [Build for Android](#-build-for-android)
- [Configuration](#-configuration)
- [Troubleshooting](#-troubleshooting)

---

## ✨ Features

| Category | Details |
|----------|---------|
| **Combat** | Drag-to-aim touch controls (Angry Birds style), charge-based shot power, ballistic trajectory preview |
| **Hit Zones** | Head (100% instant kill), Body (30%), Limbs (15%) — auto-created at runtime via `ArcherAutoSetup` |
| **Physics** | Full 2D ragdoll on death, arrows affected by wind & gravity, arrows stick into terrain |
| **Arenas** | 6 procedurally generated arena layouts with building-style platforms |
| **Multiplayer** | Photon PUN 2 online matchmaking (2-player rooms) |
| **AI** | Projectile-motion-solving AI with 3 difficulty levels (Easy / Normal / Hard) |
| **Audio** | Fully procedural SFX (no audio files required) with persistent volume settings |
| **VFX** | Camera shake, hit flash, damage numbers, arrow trails, impact particles, headshot slow-mo, kill feed |
| **UI** | Runtime-built scoreboard, health bars, wind indicator, round transitions, settings panel |
| **Auto-Setup** | Nearly all game objects auto-create at runtime — minimal manual scene setup needed |

---

## 🎮 Game Modes

### Practice Mode (vs AI)
- **Entry:** Main Menu → "VS COMPUTER"
- **Flow:** `MainMenuController` sets `GameMode.Current = Practice` → loads `GameArena` scene → `GameArenaBootstrap` spawns two `ArcherLocal` instances (Player 1 = human, Player 2 = AI)
- **Scoring:** Managed by `PracticeGameManager` (first to 5 kills wins)
- **AI:** `AIController` solves ballistic equations to aim, with configurable difficulty noise

### Online Multiplayer
- **Entry:** Main Menu → "PLAY ONLINE"
- **Flow:** `NetworkManager` connects to Photon → joins/creates a 2-player room → master client loads `GameArena` → each client spawns a networked `Archer` prefab
- **Scoring:** Managed by `GameManager` via Photon RPCs (first to 5 kills wins)
- **Timer:** `MatchmakingTimer` runs a 3-minute countdown synced across clients

---

## 🏗 Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                     SCENES                               │
│  MainMenu                    GameArena                   │
│  ├─ MainMenuController       ├─ GameArenaBootstrap       │
│  ├─ NetworkManager (DDoL)    ├─ ArenaGenerator           │
│  └─ AudioManager (DDoL)     ├─ UIManager                │
│                               ├─ WindSystem              │
│                               ├─ VisualEffectsManager    │
│                               └─ SetupWizard             │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   GAME ENTITIES                          │
│                                                          │
│  Online Mode           Practice Mode                     │
│  ├─ Archer             ├─ ArcherLocal                    │
│  │  └─ PhotonView      │  └─ No networking               │
│  ├─ Arrow              ├─ ArrowLocal                     │
│  │  └─ PhotonView      │  └─ Local physics only          │
│  └─ GameManager        └─ PracticeGameManager            │
│     └─ RPCs               └─ Local scoring               │
└─────────────────────────────────────────────────────────┘
```

### Key Design Patterns

- **Singleton Pattern:** All managers (`UIManager`, `AudioManager`, `NetworkManager`, `CameraShaker`, `WindSystem`, `KillFeed`, `TouchFeedback`, etc.) use static `Instance` fields
- **DontDestroyOnLoad:** `NetworkManager` and `AudioManager` persist across scene loads
- **Runtime Auto-Creation:** `GameArenaBootstrap` orchestrates creating all required systems at startup, eliminating fragile scene-object dependencies
- **Dual Architecture:** Every gameplay class has an online variant (Photon-dependent) and a local variant for practice mode: `Archer`/`ArcherLocal`, `Arrow`/`ArrowLocal`, `GameManager`/`PracticeGameManager`

---

## 📁 Project Structure

```
stick-archer/
├── Assets/
│   ├── Art/
│   │   ├── Backgrounds/          # Background art assets
│   │   ├── Sprites/              # Character & object sprites
│   │   └── _WhiteSquare.png      # Fallback sprite for runtime rendering
│   ├── Audio/                    # (Empty — SFX are procedurally generated)
│   ├── Editor/                   # Unity Editor-only scripts
│   │   ├── AutoSetupOnLoad.cs    # Automatic scene setup on editor load
│   │   ├── BatchSetup.cs         # Batch configuration utilities
│   │   ├── AndroidBuildHelper.cs # Android build automation
│   │   ├── FixArenaAndRebuild.cs # Arena fix/rebuild tools
│   │   ├── FixEventSystem.cs     # EventSystem repair utility
│   │   ├── MainMenuSetupHelper.cs# Main menu scene setup helper
│   │   ├── PrefabCreatorHelper.cs# Prefab creation utilities
│   │   ├── RebuildAPK.cs         # APK build helper
│   │   ├── SceneSetupHelper.cs   # Scene setup automation
│   │   └── VisualOverhaul*.cs    # Visual overhaul iteration scripts (v1–v10)
│   ├── Photon/                   # Photon PUN 2 SDK
│   ├── Prefabs/                  # Game prefabs (Archer, Arrow variants)
│   ├── Resources/                # Unity Resources folder (Photon instantiation)
│   ├── Scenes/
│   │   ├── MainMenu.unity        # Main menu scene
│   │   └── GameArena.unity       # Gameplay scene
│   ├── Scripts/                  # All game scripts (detailed below)
│   └── TextMesh Pro/             # TextMeshPro assets
├── ProjectSettings/              # Unity project configuration
├── SETUP.md                      # Original setup guide (online multiplayer focus)
├── SETUP_README.md               # Streamlined setup guide (auto-setup focus)
├── open_project.sh               # macOS script to open project in Unity Hub
└── .gitignore                    # Unity-standard gitignore
```

---

## 🔧 Core Systems

### Combat System

| Script | Role | Key Methods |
|--------|------|-------------|
| `Archer.cs` | Online player character. Handles aiming, charging, firing, health, ragdoll death. Requires `PhotonView`. | `SetAimAndCharge()`, `FireArrow()`, `OnHitReceived()`, `TriggerRagdoll()`, `Respawn()` |
| `ArcherLocal.cs` | Practice mode character. Same logic as `Archer` but fully local (no Photon). | Same API as `Archer` |
| `Arrow.cs` | Online arrow projectile. Syncs launch via RPC, detects hits on enemy `Archer`. | `Launch()`, `RPC_OnHit()` |
| `ArrowLocal.cs` | Practice mode arrow. Uses `GetComponentInParent<ArcherLocal>()` to detect hits on child HitZone colliders. | `Launch()` |
| `HitZone.cs` | Body-part damage zones (Head/Body/Limbs). Attached as children of archer prefabs. | `OnTriggerEnter2D()`, `HandleArrowHit()`, `HandleLocalArrowHit()` |
| `ArcherAutoSetup.cs` | Auto-creates HitZone child objects with appropriate colliders at runtime. | `SetupHitZones()` |

### Aiming & Input

| Script | Role |
|--------|------|
| `TouchControls.cs` | Drag-to-aim input handler. Drag away from target to aim (Angry Birds style). Calculates aim direction and charge ratio from drag delta. Works with both touch and mouse. |
| `AIController.cs` | Solves projectile motion equations to compute optimal launch angle. Adds difficulty-based noise to angle and charge. Fires on a randomized timer. |

### Scoring & Flow

| Script | Role |
|--------|------|
| `GameManager.cs` | Online scoring via Photon RPCs. Awards kills, triggers round resets, determines winner at `scoreToWin` (default: 5). |
| `PracticeGameManager.cs` | Local scoring for practice mode. Same logic, no networking. |
| `GameMode.cs` | Static class storing current mode (`Online`/`Practice`) and AI difficulty (`Easy`/`Normal`/`Hard`). |
| `MatchmakingTimer.cs` | 3-minute match countdown synced via `IPunObservable`. Calls `GameManager.OnTimeUp()` when time expires. |

### Arena & Environment

| Script | Role |
|--------|------|
| `ArenaGenerator.cs` | Procedurally generates 6 arena layouts: Basic, Tall, Asymmetric, Stepped, LowWall, Wide. Creates outlined building blocks with grey caps and spawn points. |
| `ArenaBackground.cs` | Creates layered mountain silhouette backgrounds with parallax depth. |
| `EnvironmentManager.cs` | Manages arena transitions between rounds with fade effects. |
| `WindSystem.cs` | Randomizes wind force and gravity multiplier each round. Applies horizontal force to arrows via `FixedUpdate`. Updates wind direction UI text. |
| `GameArenaBootstrap.cs` | **Scene entry point.** Destroys pre-placed objects, generates arena, spawns archers, sets up all subsystems (VFX, UI, wind, touch controls). |

### Networking

| Script | Role |
|--------|------|
| `NetworkManager.cs` | Photon connection manager (singleton, DDoL). Handles `ConnectAndPlay()` → `JoinRandomRoom()` → room creation fallback → `LoadLevel("GameArena")`. Spawns `Archer` prefabs via `PhotonNetwork.Instantiate`. |
| `MainMenuController.cs` | Main menu UI controller. Extends `MonoBehaviourPunCallbacks` to show Photon connection status. Auto-creates Play Online and VS Computer buttons if missing. |

---

## 🎨 Visual Effects Pipeline

All VFX are managed by `VisualEffectsManager`, which auto-creates missing subsystems:

| System | Script | Description |
|--------|--------|-------------|
| **Camera Shake** | `CameraShaker.cs` | Perlin noise-based shake with 3 profiles: Hit (light), Kill (strong), GameOver (heavy) |
| **Hit Flash** | `HitFlash.cs` | Flashes all sprite renderers white on damage |
| **Damage Numbers** | `DamageNumber.cs` | Floating `TextMeshPro` numbers that drift upward and fade |
| **Impact Particles** | `ImpactEffect.cs` | Cone-shaped particle burst at arrow hit position |
| **Arrow Trails** | `ArrowTrail.cs` | `TrailRenderer`-based golden trail on arrows |
| **Kill Feed** | `KillFeed.cs` | Top-of-screen elimination notifications with auto-fade |
| **Headshot Feedback** | `HeadshotFeedback.cs` | "HEADSHOT!" text + slow-motion + camera zoom sequence |
| **Touch Ripples** | `TouchFeedback.cs` | Expanding ring ripple at touch position (object-pooled) |
| **Character Glow** | `CharacterGlow.cs` | Pulsing glow sprite behind character |
| **Bow Charge** | `BowChargeEffect.cs` | Color-shifting line renderer showing charge buildup |
| **Death Effect** | `DeathEffect.cs` | Fade-out + particle burst on character death |
| **Ragdoll** | `Ragdoll2D.cs` | Full skeletal ragdoll with 6 body parts connected via `HingeJoint2D` |
| **Parallax** | `SimpleParallax.cs` | Camera-relative parallax scrolling for background layers |
| **Ambient** | `AmbientEffects.cs` | Background environmental particle effects |

### Procedural Audio

All sound effects are **generated at runtime** by `ProceduralAudio` — no audio files needed:

| SFX | Generation Method |
|-----|-------------------|
| Bow Draw | Sine sweep 180Hz → 380Hz, 0.3s |
| Arrow Fire | Triangle wave 900Hz → 220Hz, 0.18s |
| Arrow Hit | Low-freq thud (90Hz sine + noise, sharp decay) |
| Point Scored | 3-note chord (C5-E5-G5) |
| Match Win | Ascending arpeggio (C5→C6) |
| Match Lose | Descending arpeggio (C5→Eb4) |

---

## 🖥 UI System

The UI is **built entirely at runtime** by `UIManager.cs` and `GameUISetup.cs`:

| Element | Builder | Description |
|---------|---------|-------------|
| **Scoreboard** | `UIManager.BuildScoreboard()` | Dark pill at top-center with team color blocks and score labels |
| **Health Bars** | `UIManager.BuildHealthBars()` | Thin horizontal bars below scoreboard, team-colored |
| **Wind Indicator** | `UIManager.BuildWindIndicator()` | Small pill showing wind direction arrow and strength |
| **Round Transition** | `GameUISetup.SetupRoundDisplay()` | Large "ROUND X" text with scale animation |
| **Headshot Text** | `GameUISetup.SetupHeadshotFeedback()` | "HEADSHOT!" overlay with glow effect |
| **Settings Panel** | `SettingsPanel.cs` | SFX/Music sliders + Mute toggle |
| **Charge Meter** | `ChargeMeterUI.cs` | Filled image with color gradient (green → yellow → red) |

---

## 🛠 Editor Tools

Access via Unity menu **Tools →**:

| Menu Item | Script | Purpose |
|-----------|--------|---------|
| Setup Stick Archers Tags | `AutoTagSetup.cs` | Creates required tags (`Arena`) and layers (`Ground`, `HitZone`, `Arrow`) |
| Check Stick Archers Setup | `AutoTagSetup.cs` | Validates all tags, layers, and prefab assignments |
| Validate Prefabs | `PrefabValidator.cs` | Creates placeholder `ArcherLocal` and `ArrowLocal` prefabs if missing |
| Assign Prefabs to Bootstrap | `PrefabValidator.cs` | Auto-assigns prefabs to `GameArenaBootstrap` in current scene |
| Setup Android Build | `PrefabValidator.cs` | Configures Android build settings (landscape, etc.) |

---

## 🚀 Setup & Installation

> See [SETUP_README.md](SETUP_README.md) for a quick-start checklist.

### Prerequisites
- **Unity 2022.3 LTS** with Android Build Support
- **Photon PUN 2 - FREE** (from Unity Asset Store) — required only for online multiplayer

### Steps

1. **Open in Unity Hub:** Add this folder as a project
2. **Setup Tags (one-time):** `Tools → Setup Stick Archers Tags`
3. **Validate Prefabs (one-time):** `Tools → Validate Prefabs`
4. **Play:** Open `MainMenu` scene → Press Play

Most systems auto-create at runtime. The game is playable with placeholder stick-figure art.

---

## 📱 Build for Android

1. `File → Build Settings → Android → Switch Platform`
2. `Tools → Setup Android Build` (or manually set Landscape orientation)
3. Add scenes: `MainMenu` (index 0), `GameArena` (index 1)
4. `Player Settings` → Package Name: `com.yourcompany.stickarchers` → Min API: 24
5. Click **Build**

---

## ⚙ Configuration

### Game Balance

| Parameter | Location | Default |
|-----------|----------|---------|
| Score to win | `GameManager.scoreToWin` | 5 |
| Max charge time | `Archer.maxChargeTime` | 1.5s |
| Launch force range | `Archer.minLaunchForce` / `maxLaunchForce` | 3 – 9 |
| Max health | `Archer.maxHealth` | 100 |
| Wind range | `WindSystem.maxWind` | ±8 |
| Gravity range | `WindSystem.minGravity` / `maxGravity` | 0.5 – 1.5× |
| Match duration | `MatchmakingTimer.matchDurationSeconds` | 180s |

### Hit Zone Damage

| Zone | Damage | Notes |
|------|--------|-------|
| Head | 100% | Instant kill + headshot feedback |
| Body | 30% | Standard torso hit |
| Limbs | 15% | Arms and legs |

---

## 🔍 Troubleshooting

| Issue | Solution |
|-------|----------|
| "No Canvas found" | Create: `GameObject → UI → Canvas` |
| "Missing tag: Arena" | Run: `Tools → Setup Stick Archers Tags` |
| "Prefab not assigned" | Run: `Tools → Validate Prefabs` then `Tools → Assign Prefabs to Bootstrap` |
| Archers float or sink | Check `ArenaGenerator.MakeSpawn()` offset (currently `capTopSurface + 0.70f`) |
| Arrows don't hit enemies | Verify `ArrowLocal` uses `GetComponentInParent<ArcherLocal>()` for HitZone child colliders |
| No sound | `AudioManager` auto-generates SFX — check it exists and isn't muted |
| Online mode fails | Ensure Photon PUN 2 is imported and App ID is configured |
| Run full diagnostics | `Tools → Check Stick Archers Setup` |

---

## 📄 License

This project is private. All rights reserved.
