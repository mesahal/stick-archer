# 11 — Progression Economy (Coins, Gems, Level)

> Full feature rules: [`Documentation/FEATURES_COINS_GEMS_LEVEL.md`](../../Documentation/FEATURES_COINS_GEMS_LEVEL.md)  
> Cricket League reference: [`Documentation/CRICKET_LEAGUE_REFERENCE.md`](../../Documentation/CRICKET_LEAGUE_REFERENCE.md)

This doc maps **which design files** show coins, gems, and level — use it when updating scenes.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Design file index

| Screen | SVG | Spec | Progression elements |
|--------|-----|------|-------------------|
| Main Menu | [`01_main_menu.svg`](../01_main_menu.svg) | [`01_main_menu.md`](01_main_menu.md) | Profile badge: star+level, coins, gems |
| Character Select | [`02_character_select.svg`](../02_character_select.svg) | [`02_character_select.md`](02_character_select.md) | Compact badge; hero lock (LV + coin cost) |
| Results Victory | [`08_results_victory.svg`](../08_results_victory.svg) | [`08_results.md`](08_results.md) | Rewards strip: +coins, +XP, LEVEL UP badge |
| Results Defeat | [`09_results_defeat.svg`](../09_results_defeat.svg) | [`08_results.md`](08_results.md) | Rewards strip: +coins, +XP (no level-up) |
| Level Up Modal | [`11_level_up.svg`](../11_level_up.svg) | [`11_level_up.md`](11_level_up.md) | Full overlay after level increase |
| Login Rewards | [`12_login_rewards.svg`](../12_login_rewards.svg) | [`12_login_rewards.md`](12_login_rewards.md) | Hourly + 12h chests (coins + gems) |

**Icons:** [`icons/coin.svg`](../icons/coin.svg) · [`icons/gem.svg`](../icons/gem.svg) · [`icons/xp.svg`](../icons/xp.svg)

---

## Currencies

| Currency | Icon | Color token | Primary earn | Primary spend |
|----------|------|-------------|--------------|---------------|
| **Coins** | `UI/Icons/coin` | Gold `#FFD933` | Matches, login chests | Hero unlocks, upgrades |
| **Gems** | `UI/Icons/gem` | Gem `#6B8CFF` | 12h chest, quests, IAP | Daily deals, premium unlocks |

Add **Gem** to [`00_foundations.md`](00_foundations.md) token table when implementing.

---

## Shared component: ProfileBadge

Used on **Main Menu**, **Character Select**, and **Lobby** — same layout everywhere.

### ProfileBadge — 480×72

Single horizontal row, three segments separated by vertical dividers:

| Segment | Icon | Value |
|---------|------|-------|
| Level | `Icons/star` (gold) | `{n}` — level number only, no "LV" prefix |
| Coins | `Icons/coin` | `{amount}` |
| Gems | `Icons/gem` | `{amount}` |

No XP bar. No "COINS"/"GEMS" labels.

Positions: Main Menu @ (40, 40) top-left · Char Select / Lobby @ (1400, 40) top-right.

Data: `ProfileManager.Instance.Profile` + `OnProfileChanged`.

---

## Shared component: RewardsStrip

Used on **result screens** — **below** score card, **above** buttons. Never overlap score card (Fix 03).

| Variant | Height | Content |
|---------|--------|---------|
| Victory | 88px @ y=780 | +coins · +XP · optional `LEVEL UP!` pill |
| Defeat | 88px @ y=790 | +coins · +XP (muted) |

Buttons shift down ~90px to y=900 (victory) / y=920 (defeat).

---

## Level gating (Character Select)

Locked hero overlay on card:

- Padlock icon + `LOCKED`
- `REQUIRES LV {n}`
- Cost pill: `{amount} COINS` with coin icon

Unlock when `profile.level >= required` **and** `TrySpendCoins` succeeds (or already owned).

---

## Implementation order (design → code)

1. Export `gem.png` from `icons/gem.svg` → `Assets/Art/UI/Icons/` + `Resources/UI/Icons/`
2. Update Main Menu badge in `VisualOverhaul_v12` to match `01_main_menu.svg`
3. Result rewards strip in `UIManager.BuildRuntimeResultPanel`
4. Level-up modal from `11_level_up.svg`
5. Login rewards popup from `12_login_rewards.svg` (Main Menu entry point)
6. Character lock overlay in `CharacterSelectUI`
