# 05 — In-Game HUD (build spec)

Blueprint: [`designs/05_game_hud.svg`](../05_game_hud.svg). Driven by [`UIManager.cs`](../../Assets/Scripts/UIManager.cs) (the game calls its methods). Read [`00_foundations.md`](00_foundations.md). Scene: `Assets/Scenes/GameArena.unity`. **No kill feed.**

The game already calls `UIManager.Instance` → `UpdateScore`, `UpdateRound`, `SetPlayerHealth`, `UpdateChargeMeter`. You build the visuals and assign the matching fields. `UpdateRound` may remain unassigned because this HUD design does not show a round pill.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas (GameArena) › Safe
└─ GameHUDPanel                                  → UIManager.gameHUDPanel
   ├─ P1Hud   (top-left)
   │  ├─ ScoreCircle circle_128 + TMP            → UIManager.player1ScoreBadge
   │  ├─ Name      TMP
   │  ├─ HealthTrack pill_bar (BgPanelDeep)
   │  │  └─ HealthFill Image                      → UIManager.player1HealthBar
   │  └─ HealthText TMP                           → UIManager.player1HealthText
   ├─ P2Hud   (top-right, mirrored)              → player2ScoreBadge / player2HealthBar / player2HealthText
   ├─ WindBadge    Card pill + arrow + TMP       → UIManager.windText
   ├─ ChargeMeter  Slider (track + fill)         → UIManager.chargeMeter
   └─ PauseBtn     IconBtn (pause, top-center)   → UIManager.pauseButton
(also present, hidden until needed: ResultPanel → spec 08, OpponentLeftPanel)
ScoreText (P1/P2) — keep two TMPs (can be hidden) → UIManager.player1ScoreText / player2ScoreText
```
> `player1ScoreText`/`player2ScoreText` are still read by `GameManager`/`PracticeGameManager` for the score-pop and by results. Keep two TMP objects assigned even if you visually rely on the score circles — you can assign both score text fields and score badge fields to the same TMP objects.

## Elements (anchored px, +y up; under Safe)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| P1Hud | top-left (0,1) | 40,-40 | 540,120 | `Card` pill/rounded, BgPanel |
| P2Hud | top-right (1,1) | -40,-40 | 540,120 | mirror (portrait on right) |
| WindBadge | top-center (.5,1) | 0,-170 | 180,44 | `Card` pill; "WIND" label + arrow Image + value TMP |
| ChargeMeter | bottom-center (.5,0) | 0,40 | 800,80 | see below |
| PauseBtn | top-center (.5,1) | 0,-80 | 96,96 | `IconBtn` + `Icons/pause`; replaces the removed RoundBadge position |

**Inside P1Hud** (anchored within 540×120):
| Child | Pos | Size | Content |
|---|---|---|---|
| ScoreCircle | left 60,0 | 96,96 | `circle_128` Gold score circle + TMP score only (42, #1A1A1A) — assign `player1ScoreBadge`; no separate badge |
| Name | 130,30 | 280,30 | TMP, Small, Bold, white. Show player name only; no level badge. |
| HealthTrack | 130,-18 | 380,24 | `pill_bar`, BgPanelDeep |
| HealthFill | (child of track) | stretch | Image `Gradients/hp_full` (green→lime) when HP > 30%; swap to `hp_low` (orange→red) below threshold — horizontal fill left→right; assign `player1HealthBar` |
| HealthText | over track | 380,24 | TMP, FontHudLabel 14, white, Center — assign `player1HealthText` |

Do not include the old arrows-remaining row below the health bar.

**ChargeMeter** (Slider, non-interactable): Background = `pill_bar` (#0A0E1C); Fill Area→Fill = white `pill_bar`; Handle = none. Assign the Slider to `UIManager.chargeMeter`. The script sets `value` (0–1) and tints the fill via `GetChargeColor` (green→gold→red). Add "CHARGE" label (Caption, TextHint) above-left.

## Wiring — `UIManager` (on the GameArena HUD object; it already exists)
Assign: `gameHUDPanel`, `player1ScoreText`/`player2ScoreText`, `player1ScoreBadge`/`player2ScoreBadge`, `windText`, `player1HealthBar`/`player2HealthBar` (the fills), `player1HealthText`/`player2HealthText`, `chargeMeter` (the Slider), `pauseButton`. Leave `roundNumberText` empty unless a separate round display is reintroduced.
- `pauseButton` is auto-wired in `UIManager.Start()` to `PauseMenuUI.TogglePause()` — just assign it (build the Pause overlay per spec 06).
- `windText` is auto-linked to `WindSystem`.

## Verify
Start **VS Computer**. Health bars deplete + recolor on hits; charge meter fills green→red while drawing the bow and resets on release; score badges bump on a kill; wind value updates; top-center pause button opens the pause menu (spec 06).
