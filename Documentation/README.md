# Stick Archer — Documentation Index

> **Start here** before adding features, updating designs, or implementing new screens.

| Document | Purpose | Audience |
|----------|---------|----------|
| [**FEATURES_COINS_GEMS_LEVEL.md**](FEATURES_COINS_GEMS_LEVEL.md) | Coins, gems & level feature spec (Cricket League–inspired) | Product / design |
| [**CRICKET_LEAGUE_REFERENCE.md**](CRICKET_LEAGUE_REFERENCE.md) | Miniclip Cricket League feature study + adoption checklist | Product / design |
| [**PROJECT_DOCUMENTATION.md**](PROJECT_DOCUMENTATION.md) | Single source of truth: roadmap, status matrix, rules, formulas, scene gaps | Everyone |
| [**ARCHITECTURE.md**](ARCHITECTURE.md) | Code structure, patterns, extension points for new features | Engineers |
| [**GAMEPLAY_SYSTEMS.md**](GAMEPLAY_SYSTEMS.md) | How the game actually works: movement, arrows, health, rounds, AI | Engineers + designers |
| [**SCREEN_FIX_GUIDE.md**](SCREEN_FIX_GUIDE.md) | Screen-by-screen visual fix briefs (Fix 01–10) | UI implementers |
| [**ART_ASSET_PROMPTS.md**](ART_ASSET_PROMPTS.md) | Art generation prompts and asset conventions | Artists / AI art |

### Related (repo root)

| Document | Purpose |
|----------|---------|
| [../README.md](../README.md) | Project overview, quick start, build |
| [../SETUP_README.md](../SETUP_README.md) | One-time Unity setup checklist |
| [../designs/specs/](../designs/specs/) | Per-screen design specifications (SVG companions) |
| [../designs/DESIGN_CONSISTENCY.md](../designs/DESIGN_CONSISTENCY.md) | Cricket League-tier design rules, standard defs, audit matrix |
| [../docs/SCRIPTS_REFERENCE.md](../docs/SCRIPTS_REFERENCE.md) | Per-script API reference (partial; see Architecture for current list) |
| [../docs/CONTRIBUTING.md](../docs/CONTRIBUTING.md) | Contribution workflow |

---

## Recommended workflow for new features

1. **Read** [ARCHITECTURE.md](ARCHITECTURE.md) — understand dual online/local pattern and manager layout.
2. **Check** [PROJECT_DOCUMENTATION.md §3](PROJECT_DOCUMENTATION.md#3-implementation-status-matrix) — confirm what exists vs planned.
3. **Update design** in `designs/*.svg` + `designs/specs/*.md` before coding UI changes. Follow [`designs/DESIGN_CONSISTENCY.md`](../designs/DESIGN_CONSISTENCY.md) for shared tokens and components.
4. **Implement** following [SCREEN_FIX_GUIDE.md](SCREEN_FIX_GUIDE.md) patterns for visual screens.
5. **Verify** gameplay against [GAMEPLAY_SYSTEMS.md](GAMEPLAY_SYSTEMS.md) if touching combat, physics, or scoring.

---

## Current visual polish status (2026-06-08)

| Fix | Screen | Status |
|-----|--------|--------|
| 01 | Main Menu | ✅ ~98% |
| 02 | Defeat | ✅ ~98% |
| 03 | Victory | ✅ ~98% |
| 04–10 | HUD, pause, lobby, etc. | ⬜ Pending |

Next planned work: implement progression UI from designs (badge → results strip → level-up → login rewards).

### Progression economy designs

| Screen | SVG |
|--------|-----|
| Main Menu badge | [`designs/01_main_menu.svg`](../designs/01_main_menu.svg) |
| Results rewards | [`designs/08_results_victory.svg`](../designs/08_results_victory.svg), [`09_results_defeat.svg`](../designs/09_results_defeat.svg) |
| Level up | [`designs/11_level_up.svg`](../designs/11_level_up.svg) |
| Login rewards | [`designs/12_login_rewards.svg`](../designs/12_login_rewards.svg) |
| Spec index | [`designs/specs/11_progression_economy.md`](../designs/specs/11_progression_economy.md) |
