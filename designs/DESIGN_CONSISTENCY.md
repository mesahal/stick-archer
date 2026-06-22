# Design Consistency Guide

> Cricket League-tier polish rules for all Stick Archer screens.
> Reference: [`00_design_system.svg`](00_design_system.svg) · [`specs/00_foundations.md`](specs/00_foundations.md)

**Last updated:** 2026-06-08

---

## 1. Premium rules (Cricket League adapted)

| # | Rule | Requirement |
|---|------|-------------|
| 1 | Palette lock | Only tokens from foundations §1 — no orphan hex values |
| 2 | Gradient depth | Buttons: `gPrimary`/`gSuccess` + top highlight strip white @ 15% + `shadowSoft` |
| 3 | Card recipe | `rounded_32`, `gPanel` fill, stroke white @ 8-12%, optional 6px gold top accent |
| 4 | Typography | Display 120 · H1 72 · H2 52 · Body 36 · Small 28 · Caption 22 |
| 5 | Shadows | All floating UI uses `shadowSoft` or `shadowDeep` |
| 6 | Currency chrome | ProfileBadge on every meta screen |
| 7 | Background tiers | Meta: `gBgVert` or menu composite · In-game: gameplay visible · Results: radial mood |
| 8 | ASCII footers | SVG label text uses ASCII only (no em dash / middle dot in XML) |

---

## 2. Standard `<defs>` block

Copy into every screen SVG `<defs>` section. Use these **exact IDs**:

```xml
<linearGradient id="gPrimary" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0%" stop-color="#4DA3FF"/>
  <stop offset="100%" stop-color="#1F73D9"/>
</linearGradient>
<linearGradient id="gSuccess" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0%" stop-color="#5BD980"/>
  <stop offset="100%" stop-color="#258F44"/>
</linearGradient>
<linearGradient id="gDanger" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0%" stop-color="#FF7070"/>
  <stop offset="100%" stop-color="#D63232"/>
</linearGradient>
<linearGradient id="gGold" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0%" stop-color="#FFE066"/>
  <stop offset="100%" stop-color="#E6B800"/>
</linearGradient>
<linearGradient id="goldTitle" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0%" stop-color="#FFF3A0"/>
  <stop offset="55%" stop-color="#FFD933"/>
  <stop offset="100%" stop-color="#C9990A"/>
</linearGradient>
<linearGradient id="gPanel" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0%" stop-color="#252B45"/>
  <stop offset="100%" stop-color="#181D30"/>
</linearGradient>
<linearGradient id="gBgVert" x1="0" y1="0" x2="0" y2="1">
  <stop offset="0%" stop-color="#1A2552"/>
  <stop offset="55%" stop-color="#0F1A38"/>
  <stop offset="100%" stop-color="#0A0E1C"/>
</linearGradient>
<linearGradient id="hpFull" x1="0" y1="0" x2="1" y2="0">
  <stop offset="0%" stop-color="#33B859"/>
  <stop offset="100%" stop-color="#5BD980"/>
</linearGradient>
<linearGradient id="hpLow" x1="0" y1="0" x2="1" y2="0">
  <stop offset="0%" stop-color="#F28C1A"/>
  <stop offset="100%" stop-color="#F23F3F"/>
</linearGradient>
<filter id="shadowSoft" x="-20%" y="-20%" width="140%" height="140%">
  <feGaussianBlur in="SourceAlpha" stdDeviation="6"/>
  <feOffset dx="0" dy="4"/>
  <feComponentTransfer><feFuncA type="linear" slope="0.45"/></feComponentTransfer>
  <feMerge><feMergeNode/><feMergeNode in="SourceGraphic"/></feMerge>
</filter>
<filter id="shadowDeep" x="-30%" y="-30%" width="160%" height="160%">
  <feGaussianBlur in="SourceAlpha" stdDeviation="10"/>
  <feOffset dx="0" dy="8"/>
  <feComponentTransfer><feFuncA type="linear" slope="0.55"/></feComponentTransfer>
  <feMerge><feMergeNode/><feMergeNode in="SourceGraphic"/></feMerge>
</filter>
```

Screen-specific gradients (e.g. victory radial BG) may be added **after** this block with unique IDs.

---

## 3. Shared components

| Component | Size | Used on |
|-----------|------|---------|
| ProfileBadge (full) | 480x72 | Main Menu, Char Select, Lobby |
| ProfileBadge (compact) | 480x72 | Same layout, top-right on meta sub-screens |
| RewardsStrip | 1040x88 | Victory, Defeat |
| Modal shell | 800x600 card + 65% dim | Settings, Error, Level Up, Login Rewards |
| Character card | 660x700 | Char Select, Lobby |
| Primary button | 640x140 / 500x120 | All screens |
| Icon button | 96x96 circle | Gear, Pause, Back |

Visual reference: Section 04-05 in [`00_design_system.svg`](00_design_system.svg).

---

## 4. Background tiers

| Tier | Screens | Treatment |
|------|---------|-------------|
| Meta composite | 01, 02, 04 | `gBgVert` + mountains (menu-style) |
| Modal overlay | 03, 10, 11, 12 | `gBgVert` + black @ 65% dim |
| Gameplay | 05, 06, 07 | Sky/arena visible; HUD uses dark panels |
| Results mood | 08, 09 | Purple (win) / crimson (lose) radial |

---

## 5. Audit matrix

| Screen | SVG | Palette | Defs IDs | Shadow | Badge | Pass |
|--------|-----|---------|----------|--------|-------|------|
| 00_design_system | yes | yes | yes | yes | comp | yes |
| 01_main_menu | yes | yes | yes | yes | yes | yes |
| 02_character_select | yes | yes | yes | yes | yes | yes |
| 03_settings_modal | yes | yes | yes | yes | n/a | yes |
| 04_lobby | yes | yes | yes | yes | yes | yes |
| 05_game_hud | yes | yes | yes | yes | n/a | yes |
| 06_pause_menu | yes | yes | yes | yes | n/a | yes |
| 07_round_transition | yes | yes | yes | yes | n/a | yes |
| 08_results_victory | yes | yes | yes | yes | n/a | yes |
| 09_results_defeat | yes | yes | yes | yes | n/a | yes |
| 10_error_modal | yes | yes | yes | yes | n/a | yes |
| 11_level_up | yes | yes | yes | yes | n/a | yes |
| 12_login_rewards | yes | yes | yes | yes | n/a | yes |

---

## 6. Cricket League comparison

| CL pattern | Stick Archer equivalent |
|------------|-------------------------|
| Gradient buttons with depth | `gPrimary` + highlight + `shadowSoft` |
| Gold selected card border | Character card gold stroke when selected |
| Currency always visible | ProfileBadge LV/coins/gems |
| Dark meta shell | `gBgVert` backgrounds |
| Post-match rewards row | RewardsStrip below score card |
| Modal overlays | Dim + gPanel card + top accent |

---

## 7. Validation

```bash
xmllint --noout designs/*.svg designs/icons/*.svg
```

All files must pass before Unity implementation begins.
