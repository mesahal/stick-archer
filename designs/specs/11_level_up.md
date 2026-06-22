# 11 — Level Up Modal (build spec)

Blueprint: [`designs/11_level_up.svg`](../11_level_up.svg).  
Feature rules: [`Documentation/FEATURES_COINS_GEMS_LEVEL.md`](../../Documentation/FEATURES_COINS_GEMS_LEVEL.md).

**Overlay modal** — shown on top of Main Menu or Result screen when `ProfileManager.OnLevelUp` fires. Blocks input until dismissed.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy

```
Canvas (overlay, sort order top)
└─ LevelUpModal              (full-screen dim + card)
   ├─ Overlay     Image rgba dim
   ├─ Card        rounded_32 panel 800×700
   │  ├─ Title    TMP "LEVEL UP!" gold gradient
   │  ├─ LevelNum TMP "{newLevel}" Display scale
   │  ├─ Subtitle TMP "NEW LEVEL REACHED"
   │  ├─ Unlocks   TMP list (bullets)
   │  ├─ CoinBonus row "+650 COINS" (if grant applies)
   │  └─ Continue  Btn_Primary "CONTINUE"
   └─ Sparkles    optional decorative Images
```

---

## Elements (center card @ 560,190 — size 800×700)

| Element | Pos (in card) | Size | Content |
|---------|---------------|------|---------|
| Title | center, y=100 | — | "LEVEL UP!", 72pt Black, gold gradient |
| Level number | center, y=290 | — | `{level}`, 160pt white |
| Subtitle | center, y=330 | — | "NEW LEVEL REACHED", 24pt TextDim |
| Unlocks header | x=80, y=400 | — | "UNLOCKED", 22pt Gold, tracked |
| Unlock list | x=80, y=430+ | — | Bullet lines from unlock table |
| Coin bonus | center, y=560 | 360×56 pill | `+{n} COINS` with coin icon |
| Continue | x=120, y=610 | 560×80 | Primary pill |

---

## Data & triggers

| Input | Source |
|-------|--------|
| New level | `OnLevelUp(int newLevel)` |
| Unlock lines | Lookup `level_unlocks` table by level |
| Coin bonus | `coins_per_level_up × newLevel` from RemoteConfig |

Dismiss: Continue button or tap outside card (product choice — spec shows button only).

---

## Verify

1. Force level-up in debug (grant large XP).
2. Modal appears with correct level number and unlock text.
3. Coin bonus matches RemoteConfig.
4. Profile badge already shows new level behind overlay.
