# 08/09 — Results (Victory & Defeat) (build spec)

Blueprints: [`08_results_victory.svg`](../08_results_victory.svg) + [`09_results_defeat.svg`](../09_results_defeat.svg). Driven by [`UIManager.ShowResult(bool localPlayerWon)`](../../Assets/Scripts/UIManager.cs). Confetti = [`ConfettiBurst.cs`](../../Assets/Scripts/UI/ConfettiBurst.cs). Read [`00_foundations.md`](00_foundations.md). Scene: `GameArena`.

**One panel, two states.** `ShowResult` sets the title to "VICTORY!" (Gold) or "DEFEAT" (Danger), fills the score, and toggles `victoryEffects` on a win. Build it once.

> ⚠️ **Button names matter.** `UIManager.WireButtons()` finds the result buttons by exact child name: **`RematchButton`** and **`MenuButton`**. Name them precisely or they won't wire.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas (GameArena) › (full-screen, NOT under Safe)
└─ ResultPanel          (full-rect; starts INACTIVE)        → UIManager.resultPanel
   ├─ Background  Image (radial dark)                        → UIManager.resultBackground
   ├─ VictoryEffects  full-rect + ConfettiBurst (INACTIVE)   → UIManager.victoryEffects
   ├─ Title       TMP "VICTORY!"                             → UIManager.resultTitleText
   ├─ ScoreCard   Card rounded_32 (final score only)
   │  ├─ Label    TMP "FINAL SCORE"
   │  └─ Score    TMP "5 — 2"                                → UIManager.resultScoreText
   ├─ RewardsStrip  Card slim row (+coins, +XP, LEVEL UP?)   → new — see below
   ├─ RematchButton  Btn_Primary  (EXACT name)               (auto-wired → OnRematchPressed)
   └─ MenuButton     Btn_Outline  (EXACT name)               (auto-wired → OnMenuPressed)
```

## Elements (anchored px, +y up; center anchor)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| Background | stretch | 0,0 | stretch | Image dark radial (BgDark). For win, brighter; for lose, desaturated — static is fine. |
| VictoryEffects | stretch | 0,0 | stretch | empty full-rect + `ConfettiBurst`. **Start INACTIVE** (script enables on win). |
| Title | center | 0,240 | 1400,240 | TMP, **Display 200**, Black, Center. Text+color set by script (Gold win / Danger lose). Add dark outline for pop. |
| ScoreCard | center | 0,-10 | 1040,280 | `Card` rounded_32, BgPanel, gold-ish border |
| Label | (in card) | 0,90 | 1040,40 | TMP "FINAL SCORE", Small, Gold, Center, tracking +10 |
| Score | (in card) | 0,-20 | 1040,140 | TMP "5 — 2", **Display 110**, Black, white, Center → `resultScoreText` |
| **RewardsStrip** | center | 0,**-150** | 1040,**88** | Slim panel **below** score card. See rewards table. |
| RematchButton | center | -270,**-330** | 500,120 | `Btn` Success (win) / Primary (lose), label "REMATCH", `Icons/play` |
| MenuButton | center | 270,**-330** | 500,120 | `Btn_Outline`, label "MAIN MENU", `Icons/home` |

### RewardsStrip (progression — new)

**Do not overlap ScoreCard.** Place ~20px gap below card bottom.

| State | Columns | Notes |
|-------|---------|-------|
| **Victory** | +{coins} COINS · +{xp} XP · optional `LEVEL UP!` pill | Gold accents; show LEVEL UP only if `LeveledUpLastMatch` |
| **Defeat** | +{coins} COINS · +{xp} XP | Muted white/gold; no level-up pill |

Icons: `coin`, XP chevrons (or `icons/xp`). Data from `ProfileManager.LastRewardCoins`, `LastRewardXp`, `LeveledUpLastMatch`.

Blueprint: rewards row in [`08_results_victory.svg`](../08_results_victory.svg) @ y=780 and [`09_results_defeat.svg`](../09_results_defeat.svg) @ y=790.

## Wiring — `UIManager`
Assign `resultPanel`, `resultBackground`, `victoryEffects` (the ConfettiBurst object), `resultTitleText`, `resultScoreText`. The two buttons are auto-wired by name in `WireButtons()` — just name them **`RematchButton`** / **`MenuButton`** and parent them under `resultPanel`.

> **Heads-up:** today both `OnRematchPressed` and `OnMenuPressed` return to the Main Menu (no true rematch yet). Buttons work; "Rematch" just re-enters the menu. A real rematch is a later code change.

## Verify
Win a Practice match to `scoreToWin` (5): ResultPanel appears, "VICTORY!" in gold, confetti falls, "FINAL SCORE 5 — 2". Lose: "DEFEAT" in red, no confetti. Both buttons return to MainMenu. Confirm `victoryEffects` is **off** on a loss.
