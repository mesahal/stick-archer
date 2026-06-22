# Analytics / Live-ops Layer (Phase 1)

Provider-agnostic instrumentation for the game. Gameplay code only ever talks to the
static **`Analytics`** facade (and **`RemoteConfig`**); concrete SDKs are pluggable
sinks behind `IAnalyticsBackend`, so swapping/adding a provider never touches call sites.

## Architecture

```
Game code ──> Analytics (static facade) ──> AnalyticsManager (singleton) ──┬─> DebugAnalyticsBackend   (always on)
                                                                           └─> FirebaseAnalyticsBackend (#if FIREBASE_ENABLED)
RemoteConfig (local defaults, backend may Apply() overrides)
```

- **`AnalyticsManager`** self-bootstraps via `[RuntimeInitializeOnLoadMethod]` — no scene
  or prefab wiring needed. Owns install id, session id, session duration, per-match
  context (duration/kills), and global error capture (`Application.logMessageReceived`).
- **`DebugAnalyticsBackend`** prints every event to the console so the full funnel is
  verifiable in-editor today, with zero external dependencies.

## Event taxonomy

Names live in `GameEvents` / `EventParams` — never hand-type strings at call sites.

| Event | Where it fires | Key params |
|---|---|---|
| `session_start` / `session_end` | app launch / pause / quit | `session_id`, `session_sec` |
| `menu_play_online` | main menu Play Online | — |
| `menu_practice` | main menu Practice | `difficulty` |
| `difficulty_changed` | difficulty dropdown | `difficulty` |
| `match_start` | GameArena loads (any mode) | `mode`, `difficulty`, `character` |
| `kill` | each scored kill (online + practice) | `shooter_slot`, `victim_slot`, `p1_score`, `p2_score` |
| `match_end` | match decided / time up | `winner_slot`, `local_won`, `p1_score`, `p2_score`, `kills`, `duration_sec` |
| `app_error` | logged error/exception | `error_type`, `message` |

A `kill` event carries both shooter and victim, so "deaths" are derivable without a
separate event.

## Remote Config

`RemoteConfig` ships safe local defaults and exposes typed getters. Today the game
managers read `score_to_win` through it (`GameManager`/`PracticeGameManager` Awake), so
match length is server-tunable once a backend calls `RemoteConfig.Apply(...)`.
Keys are pre-seeded for later phases (`interstitial_frequency`, `rewarded_coins`).

## Enabling Firebase (Analytics + Crashlytics) — your side, one time

Needs a Firebase project (external account + credentials this repo can't contain):

1. Create a Firebase project; add an Android app using the package id from Player
   Settings (change the placeholder `com.yourcompany.stickarchers` first).
2. Drop `google-services.json` into `Assets/`.
3. Import the Firebase Unity SDK packages (Analytics + Crashlytics).
4. Player Settings ▸ Android ▸ Scripting Define Symbols: add `FIREBASE_ENABLED`.

That's it — `FirebaseAnalyticsBackend` is already gated by that symbol and registered in
`AnalyticsManager.RegisterBackends()`. No call sites change. The same pattern works for
Unity Gaming Services or GameAnalytics: implement `IAnalyticsBackend`, register it.

## Verifying

Play in the editor and watch the Console: you should see `[Analytics] session_start`,
then `[Analytics] match_start { mode=practice ... }` on entering a match, `[Analytics] kill { ... }`
per score, and `[Analytics] match_end { ... duration_sec=N }` at the end.
