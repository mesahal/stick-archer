# Progression / Economy (Phase 2 — core)

Player profile, soft currency, XP/levels, and pluggable persistence. Same
provider-agnostic shape as the Analytics layer: gameplay talks only to
`ProfileManager`; the storage backend is swappable behind `IProfileStore`.

## Architecture

```
Game code ──> ProfileManager (singleton, self-bootstrapping) ──> IProfileStore ──┬─> LocalProfileStore  (JSON on disk, default)
                                                                                 └─> CloudProfileStore  (#if CLOUD_SAVE_ENABLED)
            holds PlayerProfile (coins, xp, level, stats, ownedCharacters)
```

- **`ProfileManager`** self-bootstraps via `[RuntimeInitializeOnLoadMethod]` — no scene
  wiring. All currency/XP mutations funnel through it (nothing edits the profile
  directly), which keeps the economy auditable and ready for later server validation.
- **`LocalProfileStore`** writes `profile.json` to `Application.persistentDataPath`
  atomically (temp-file + move) so a crash mid-save can't corrupt the existing file.
- **`CloudProfileStore`** is a documented seam (PlayFab / Firebase / UGS Cloud Save),
  compiled only behind `CLOUD_SAVE_ENABLED`.

## What's wired

- **Rewards on match end** — `GrantMatchRewards(won, mode)` is called from both
  `GameManager.EndMatch` (online) and `PracticeGameManager.EndMatch`. Amounts come from
  **RemoteConfig** (`coins_per_match`, `coins_per_win`, `xp_per_match`, `xp_per_win`) so
  the economy is server-tunable without a build.
- **Lifetime stats** — `matchesPlayed`, `matchesWon`, `totalKills` (local player's kills
  only, tallied in `RecordKill`).
- **Leveling** — `AddXp` rolls over multiple levels; curve `XpToAdvance(level) = 100 + (level-1)*50`.
  Profiles start at **level 1** and there is currently **no hard max account level**.
- **Analytics** — every economy move emits `currency_earned` / `currency_spent` /
  `level_up`, and `level`/`coins` are set as user properties for segmentation.

## Public API (for the upcoming Profile UI / shop)

```csharp
var p = ProfileManager.Instance.Profile;      // coins, level, xp, stats
ProfileManager.Instance.XpForNextLevel();      // for a progress bar
ProfileManager.Instance.TrySpendCoins(100, "buy_skin");   // false if unaffordable
ProfileManager.Instance.UnlockCharacter(1);
ProfileManager.Instance.OnProfileChanged += p => RefreshHud(p);
ProfileManager.Instance.OnLevelUp += lvl => PlayLevelUpFx(lvl);
```

## UI (done)
- **Result-screen rewards card** — coins earned + total, level, XP bar, "LEVEL UP!" flag.
  Built in `UIManager.BuildRewardsCard` (reads `ProfileManager.LastReward*`).
- **Persistent profile badge** — top-left level + XP bar + coins, live-refreshing via
  `OnProfileChanged`. Self-building `ProfileBadge` (`Assets/Scripts/UI/ProfileBadge.cs`),
  mounted on the main menu by `MainMenuController.EnsureProfileBadge`.

## Still TODO in Phase 2
- **FTUE tutorial** — guided first match teaching charge / aim / wind.
- **Gems currency** — see [FEATURES_COINS_GEMS_LEVEL.md](../../Documentation/FEATURES_COINS_GEMS_LEVEL.md).
- **Enable cloud save** — implement `CloudProfileStore` against a backend, define
  `CLOUD_SAVE_ENABLED`.

## Verifying (in Unity)
Play a Practice match to completion. Console shows `[Analytics] currency_earned { amount=… }`
and, on enough XP, `level_up`. Re-launch the game: coins/level persist
(`profile.json` in the persistent data path).
