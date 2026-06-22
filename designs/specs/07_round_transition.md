# 07 — Round Transition (build spec)

Blueprint: [`designs/07_round_transition.svg`](../07_round_transition.svg). Script: [`RoundTransition.cs`](../../Assets/Scripts/RoundTransition.cs) (already exists — fade + scale-in + diagonal swipe wipe). Read [`00_foundations.md`](00_foundations.md). Scene: `GameArena`.

The game calls `RoundTransition.ShowRound(roundNumber)` between rounds. The script **builds its own swipe-wipe panel** at runtime; you just provide the text + a CanvasGroup.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas (GameArena)
└─ RoundTransition     + CanvasGroup + RoundTransition.cs   (alpha starts 0)
   ├─ RoundText   TMP  "ROUND 3"                            → RoundTransition.roundText
   └─ ArenaName   TMP  (optional subtitle)                  → RoundTransition.arenaNameText
```
Place it high in the Canvas draw order (last sibling) so it overlays the HUD. Not under `Safe` (it's a full-screen moment).

## Elements (anchored px; center anchor)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| RoundTransition | stretch | 0,0 | stretch | empty RectTransform + CanvasGroup + script |
| RoundText | center | 0,0 | 1200,400 | TMP, **Display 120–360**, Black weight, Center, white. The script scales it in. |
| ArenaName | center | 0,-220 | 1000,60 | TMP, H2, TextSecondary, Center (optional; hidden if arena name empty) |

## Wiring — `RoundTransition`
Assign `roundText`, optional `arenaNameText`, and `canvasGroup` (auto-grabbed if left empty). Defaults are fine: `enableSwipeWipe = true`, `wipeColor` ≈ BgPanelDeep, `displayDuration ≈ 1.5s`.

**Optional — per-round color (green/gold/red, per the comp):** the script uses a single `textColor`. To color by round, set `roundText.color` just before calling `ShowRound` from your round logic, e.g. round 1–2 → `UIDesignSystem.Success`, mid → `UIDesignSystem.Gold`, final → `UIDesignSystem.Danger`. Small code change, not required for a working transition.

## Verify
Start a match → "ROUND 1" scales in with the diagonal wipe, holds, fades. Win a round → "ROUND 2" plays. Confirm it overlays the HUD and doesn't block input after it fades (CanvasGroup alpha returns to 0).
