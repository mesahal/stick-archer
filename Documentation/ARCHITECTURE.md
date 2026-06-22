# Stick Archer — Architecture

> Code structure, design patterns, and extension points for future features.
> Last updated: 2026-06-08.

---

## 1. High-level overview

Stick Archer is a **1v1 physics archery duel** with two parallel execution paths:

| Path | When | Character | Arrow | Scoring |
|------|------|-----------|-------|---------|
| **Online** | Main Menu → Play Online | `Archer` + `PhotonView` | `Arrow` (RPC sync) | `GameManager` |
| **Practice** | Main Menu → VS Computer | `ArcherLocal` | `ArrowLocal` | `PracticeGameManager` |

Both paths share the same **gameplay rules**, **UI**, **VFX**, **wind/gravity**, and **arena generation**. The split exists because Photon networking adds RPC/ownership constraints that would complicate a single class.

```
MainMenu.unity
    │
    ├─ Practice ──► GameArena.unity ──► GameArenaBootstrap
    │                                      ├─ ArenaGenerator (platforms)
    │                                      ├─ ArenaBackground (parallax)
    │                                      ├─ Spawn ArcherLocal × 2
    │                                      └─ PracticeGameManager
    │
    └─ Online ───► Photon room ──► GameArena.unity
                                       ├─ NetworkManager.SpawnLocalPlayer()
                                       └─ GameManager (master-authoritative)
```

---

## 2. Scenes

| Scene | Build index | Entry | Key objects |
|-------|-------------|-------|-------------|
| `MainMenu` | 0 | App launch | `MainMenuController`, baked UI (VisualOverhaul_v12), `ProfileBadge` |
| `GameArena` | 1 | Match start | `GameArenaBootstrap`, optional legacy `ArenaManager` (no-op) |

**Important:** Most gameplay UI and arena geometry are **built at runtime**, not hand-placed in the scene. Main Menu UI is **editor-baked** via `VisualOverhaul_v12.cs`.

---

## 3. Bootstrap & lifecycle

### `GameArenaBootstrap` — arena scene entry point

Runs on `Start()` in this order:

1. Force Practice mode if editor-play without Photon
2. Load `ArcherLocal` / `ArrowLocal` from `Resources/` if unassigned
3. `SetupVisualEffects()` — ensures `VisualEffectsManager`, wind, etc.
4. `GenerateArenaImmediate()` — destroys legacy buildings, creates `ArenaBackground` + `ArenaGenerator`
5. `Analytics.MatchStarted(...)`
6. Branch:
   - **Practice:** spawn two `ArcherLocal`, attach `PracticeGameManager`, show HUD
   - **Online:** `NetworkManager.SpawnLocalPlayer()`, show HUD
7. Ensure `TouchControls` exists on Canvas

### Singleton managers

| Manager | Lifetime | Boot |
|---------|----------|------|
| `AnalyticsManager` | DontDestroyOnLoad | `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` |
| `ProfileManager` | DontDestroyOnLoad | same |
| `AudioManager` | DontDestroyOnLoad | scene or auto-create |
| `NetworkManager` | DontDestroyOnLoad | MainMenu |
| `UIManager` | per scene | auto-create if missing |
| `WindSystem` | per scene | bootstrap |
| `GameManager` / `PracticeGameManager` | per scene | bootstrap / network |

**Pattern:** Static `Instance` field + `Awake()` assignment. Most managers self-heal if missing.

---

## 4. Dual-class pattern (online vs local)

When adding gameplay features, **implement twice or extract shared logic**:

| Concern | Online | Local |
|---------|--------|-------|
| Character | `Archer.cs` | `ArcherLocal.cs` |
| Projectile | `Arrow.cs` | `ArrowLocal.cs` |
| Hit detection | RPC from `Arrow` | Direct in `ArrowLocal` |
| Scoring | `GameManager.RecordKill` RPC | `PracticeGameManager.RecordKill` |
| Spawn | `PhotonNetwork.Instantiate` | `Instantiate` |

**Shared components** (attach to both prefabs): `BowSwayController`, `ArcherSpriteController`, `ArcherAutoSetup`, `HitZone` children, `Ragdoll2D`, `HitFlash`.

---

## 5. Folder structure

```
Assets/
├── Art/                    # Source art (Backgrounds, Platforms, Sprites, UI)
├── Editor/                 # Menu tools (VisualOverhaul_v12, build helpers)
├── Photon/                 # PUN2 SDK (do not modify)
├── Resources/              # Runtime-loaded prefabs & UI sprites
│   ├── Archer.prefab, ArcherLocal.prefab
│   ├── Arrow.prefab, ArrowLocal.prefab
│   ├── Characters/         # Per-player sprite sheets
│   ├── Backgrounds/, Platforms/, UI/
├── Scenes/                 # MainMenu, GameArena only
└── Scripts/
    ├── Analytics/          # Event facade + backends
    ├── Progression/        # Profile, XP, coins, persistence
    ├── UI/                 # Design system, art provider, screen components
    └── *.cs                # Gameplay, managers, VFX
```

### Art vs Resources

- **`Assets/Art/`** — authoring source; editor tools import from here.
- **`Assets/Resources/`** — runtime `Resources.Load()` paths; mirrors key art for builds.

When adding sprites, follow existing mirror pattern (copy or sync via editor tool).

---

## 6. UI architecture

Two build strategies coexist:

| Strategy | Screens | Builder |
|----------|---------|---------|
| **Editor-baked** | Main Menu, Character Select (partial) | `VisualOverhaul_v12.cs` |
| **Runtime-built** | Game HUD, Pause, Settings, Result panels | `UIManager.cs`, `GameUISetup.cs` |

### Design system (`Assets/Scripts/UI/`)

| Class | Role |
|-------|------|
| `UIDesignSystem` | Color tokens, health/charge gradients |
| `UIArtProvider` | Sprite loading from `Resources/UI/` |
| `UIFontProvider` | Inter TMP fonts |
| `UITween`, `ButtonAnimator` | Motion / juice |

**Rule for new screens:** Add SVG to `designs/`, spec to `designs/specs/`, implement via runtime builder or editor tool following [SCREEN_FIX_GUIDE.md](SCREEN_FIX_GUIDE.md).

---

## 7. Networking (Photon PUN2)

```
NetworkManager.ConnectAndPlay()
  → ConnectUsingSettings
  → JoinRandomRoom (or CreateRoom)
  → PhotonNetwork.LoadLevel("GameArena")
  → SpawnLocalPlayer() → PhotonNetwork.Instantiate("Archer", ...)
```

- **Master client** owns arena seed broadcast and score RPCs.
- `MatchmakingTimer` syncs via `IPunObservable`.
- Prefabs **must** live under `Resources/` for `PhotonNetwork.Instantiate`.

---

## 8. Progression & analytics (provider-agnostic)

```
Game code
  → AnalyticsManager → IAnalyticsBackend (Debug default, Firebase optional)
  → ProfileManager   → IProfileStore (LocalProfileStore default, Cloud optional)
  → RemoteConfig     → tunable defaults (score_to_win, coin rates, etc.)
```

Feature flags: `FIREBASE_ENABLED`, `CLOUD_SAVE_ENABLED` (compile-time).

---

## 9. Extension points for planned features

| Feature area | Where to extend | Notes |
|--------------|-----------------|-------|
| **New game mode** | `GameMode.cs`, `MainMenuController`, bootstrap branch | Add enum + menu entry |
| **Shop / meta UI** | New scene or modal; `ProfileManager.TrySpendCoins` API exists | Design in `designs/` first |
| **New character** | `Resources/Characters/PlayerN/`, `CharacterSelectUI`, sprite controller | Mirror Player1/Player2 pattern |
| **New arena layout** | `ArenaGenerator.cs` layout array | Keep deterministic seed for online |
| **New HUD element** | `UIManager.Build*` or `GameUISetup` | Follow Fix 04 spec |
| **Server config** | `RemoteConfig.cs` keys | Already wired for score/rewards |
| **Ads / IAP** | New `Monetization/` folder suggested | Not started (P3) |

---

## 10. Editor tools

Access via **Tools → Design Sync** or **Tools →**:

| Tool | Script | Use |
|------|--------|-----|
| Polish MainMenu (v12) | `VisualOverhaul_v12.cs` | Rebuild main menu from design |
| Setup Stick Archers Tags | `AutoTagSetup.cs` | Tags/layers (one-time) |
| Validate Prefabs | `PrefabValidator.cs` | Create/assign Resources prefabs |
| Setup URP / Performance | `SetupURP.cs`, `PerformanceSetup.cs` | Render pipeline |
| Inter Font Setup | `InterFontSetup.cs` | TMP font assets |

**Deprecated / legacy:** `ArenaManager` in GameArena scene is a no-op stub (spawn moved to bootstrap). Old setup helpers (`SceneSetupHelper`, `MainMenuSetupHelper`) remain for greenfield scaffolding only.

---

## 11. Removed / dead code (2026-06-08 cleanup)

The following were removed as unused (superseded by other systems):

| Removed | Superseded by |
|---------|---------------|
| `HealthBarUI.cs` | `UIManager.UpdateBar()` |
| `CameraController.cs` | Fixed orthographic camera in bootstrap |
| `EnvironmentManager.cs` | `ArenaGenerator` + round rebuild in managers |
| Empty folders: `CityAssets`, `Pixel Adventure 1`, `Audio`, `Prefabs` | N/A |
| `_downloads/` Kenney zips | Assets already in `Assets/Art/` |
| `patch.cs` | Dummy test file |

---

## 12. Conventions for new code

1. **Minimize scope** — match existing naming and patterns in the file you edit.
2. **Online + local** — if it affects gameplay, plan both paths upfront.
3. **No SafeAreaFitter in editor scripts** — runtime only (see SCREEN_FIX_GUIDE lesson 1).
4. **No TMP for Unicode ornaments** — use `Image` shapes (lesson 2).
5. **Resources paths** — lowercase, no spaces; test on device build.
6. **Analytics** — fire events through `Analytics.*`, not directly to backends.
