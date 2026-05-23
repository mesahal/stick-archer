# Contributing Guide

Guidelines for contributing to **Stick Archers Battle**.

---

## Project Conventions

### Code Style

- **Language:** C# (Unity 2022 LTS / .NET Standard 2.1)
- **Naming:**
  - Classes: `PascalCase` (e.g., `ArrowLocal`, `CameraShaker`)
  - Public methods/properties: `PascalCase`
  - Private fields: `camelCase` with no prefix (e.g., `chargeTimer`, `isCharging`)
  - Hidden public fields: Mark with `[HideInInspector]`
  - Constants: `UPPER_SNAKE_CASE` or `PascalCase` (e.g., `OUTLINE`, `SAMPLE_RATE`)
  - Static readonly: `PascalCase` (e.g., `PlatformFill`, `BodyPartNames`)
- **Inspector Headers:** Group related fields with `[Header("Section Name")]`

### Architecture Rules

1. **Singleton Managers:** All managers use `public static ManagerName Instance;` set in `Awake()`. Check for duplicates before assigning.

2. **Dual Mode Pattern:** Gameplay classes come in pairs:
   - `Archer` (Photon) ↔ `ArcherLocal` (local)
   - `Arrow` (Photon) ↔ `ArrowLocal` (local)
   - `GameManager` (RPCs) ↔ `PracticeGameManager` (local)

   When modifying gameplay logic, **update both variants**.

3. **Runtime Auto-Creation:** Prefer creating game objects at runtime over scene-placed objects. This reduces merge conflicts and scene corruption.

4. **Null-safe Chaining:** Use `?.` operator when calling manager methods (e.g., `UIManager.Instance?.UpdateScore(...)`).

5. **DontDestroyOnLoad:** Only `NetworkManager` and `AudioManager` persist across scenes.

### File Organization

```
Assets/Scripts/      → All runtime game scripts
Assets/Editor/       → Unity Editor-only scripts (#if UNITY_EDITOR)
Assets/Prefabs/      → Game prefabs
Assets/Resources/    → Assets loaded via Resources.Load() (Photon requirement)
Assets/Scenes/       → Unity scenes (MainMenu, GameArena)
```

### Prefab Requirements

| Prefab | Required Components | Notes |
|--------|-------------------|-------|
| `ArcherLocal` | `Rigidbody2D`, `ArcherLocal`, `BoxCollider2D` | Placed in `Prefabs/` |
| `ArrowLocal` | `Rigidbody2D`, `ArrowLocal`, `BoxCollider2D` (trigger), `ArrowTrail` | Placed in `Prefabs/` |
| `Archer` | `Rigidbody2D`, `Archer`, `PhotonView`, `PhotonTransformView` | Must be in `Resources/` for Photon |
| `Arrow` | `Rigidbody2D`, `Arrow`, `PhotonView` | Must be in `Resources/` for Photon |

---

## Development Workflow

### Running Locally

1. Open `MainMenu` scene
2. Press Play
3. Select **VS COMPUTER** for practice mode (no Photon setup needed)

### Testing Online Mode

1. Install Photon PUN 2 from Asset Store
2. Configure App ID in `Window → Photon Unity Networking → PUN Wizard`
3. Build and run two instances (or use ParrelSync)

### Adding a New Arena Type

1. Open `ArenaGenerator.cs`
2. Add a new method (e.g., `BuildBridge()`) using `MakeBuilding()`, `MakeBlock()`, `MakeGround()`, `MakeSpawn()`
3. Add the case to `GenerateArena(int type)`
4. Update `Random.Range(0, N)` in `GameArenaBootstrap.GenerateArenaImmediate()`

### Adding a New Visual Effect

1. Create a new script inheriting `MonoBehaviour`
2. Add a setup method to `VisualEffectsManager.cs`
3. Add a toggle bool field (e.g., `enableMyEffect`)
4. Call your setup from `VisualEffectsManager.Start()`

### Adding a New SFX

1. Open `AudioManager.cs`
2. Add a new `AudioClip` field and public play method
3. Generate the clip in `Awake()` using `ProceduralAudio.Tone()`, `.Thud()`, `.Chord()`, or `.Arpeggio()`

---

## Common Pitfalls

| Pitfall | Explanation |
|---------|-------------|
| **Modifying only `Archer` but not `ArcherLocal`** | Both classes must stay in sync for consistent behavior across game modes |
| **Placing objects in scene instead of creating at runtime** | Scene objects cause merge conflicts. `GameArenaBootstrap.NukePrePlacedObjects()` destroys most scene objects anyway |
| **Forgetting `GetComponentInParent` in hit detection** | HitZone colliders are on child objects — `GetComponent` on the collider's GameObject won't find the parent archer |
| **Not guarding with `photonView.IsMine`** | All Photon callbacks must check ownership to avoid double-processing |
| **Forgetting to update `GameMode.IsPractice` checks** | New features must handle both practice and online code paths |

---

## Scene Hierarchy (Runtime)

After `GameArenaBootstrap.Start()` executes, the `GameArena` scene contains:

```
Main Camera
Canvas
  ├── GameHUDPanel
  │   ├── Scoreboard (P1Block, P1Score, VS, P2Score, P2Block)
  │   ├── P1HealthBG → P1HealthFill, P1Label
  │   ├── P2HealthBG → P2HealthFill, P2Label
  │   └── WindIndicator → WindText
  ├── RoundTransition → RoundText, ArenaName
  ├── HeadshotFeedback
  ├── KillFeed
  └── TouchControls
EventSystem
GameArenaBootstrap
ArenaGenerator
  └── (Generated blocks, caps, ground)
ArenaBackground
  ├── FarMountains → Peak, Peak, ...
  └── NearMountains → Peak, Peak, ...
WindSystem
VisualEffectsManager
  ├── CameraShaker
  └── AmbientEffects (if enabled)
SetupWizard
PracticeGameManager (practice mode only)
ArcherLocal (Player 1)
  ├── Body, Head, Hair, Pants, Legs, ArmBack, ArmFront
  ├── *_Outline (shadow for each body part)
  ├── AimLine
  └── HitZones → Head, Body, LeftArm, RightArm, LeftLeg, RightLeg
ArcherLocal (Player 2 / AI)
  └── (same children + AIController)
```

---

## Git Workflow

### Ignored Files (see `.gitignore`)
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` — Unity-generated
- `*.csproj`, `*.sln` — IDE project files
- `*.apk`, `*.aab`, `*.keystore` — Build artifacts
- `PhotonServerSettings.asset` — Contains secret App ID

### Commit Guidelines

- Prefix commits with the system: `[Combat]`, `[Arena]`, `[UI]`, `[VFX]`, `[Audio]`, `[Network]`, `[Editor]`, `[Docs]`
- Keep scene file changes minimal — prefer runtime creation
- Never commit `PhotonServerSettings.asset`
