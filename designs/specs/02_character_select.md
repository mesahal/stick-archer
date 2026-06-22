# 02 — Character Select (build spec)

Blueprint: [`designs/02_character_select.svg`](../02_character_select.svg). Script: [`CharacterSelectUI.cs`](../../Assets/Scripts/UI/CharacterSelectUI.cs) (already exists). Read [`00_foundations.md`](00_foundations.md).

Lives as a **panel in the MainMenu scene**, toggled by `CharacterSelectUI.Show()`. Selection is stored in `PlayerPrefs` (`CharacterSelectUI.SelectedCharacter`, 0 = Adventurer, 1 = Soldier).

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas › Safe
└─ CharacterSelectPanel        (full-rect; starts INACTIVE)   → CharacterSelectUI.characterSelectPanel
   ├─ Dim            Image #000 @ 50% (full-rect)
   ├─ BackBtn        IconBtn (back icon), top-left            → CharacterSelectUI.backButton
   ├─ ProfileBadge   compact pill (LV, XP, coins, gems)       top-right — see 11_progression_economy.md
   ├─ Title          TMP "CHOOSE YOUR ARCHER"
   ├─ AdventurerCard Button                                    → CharacterSelectUI.adventurerCard
   │  ├─ Border      Image rounded_32 (frame)                  → CharacterSelectUI.adventurerBorder
   │  ├─ Content     CanvasGroup                               → CharacterSelectUI.adventurerContent
   │  │  ├─ Art      Image (Adventurer sprite)
   │  │  ├─ Name     TMP "ADVENTURER"
   │  │  ├─ Tagline  TMP "QUICK · NIMBLE · LIGHT"
   │  │  └─ StatSpeed / StatPower   (Bar recipe ×2)
   │  └─ CheckBadge  gold circle + check icon                  → CharacterSelectUI.adventurerCheckBadge
   ├─ SoldierCard    (mirror of Adventurer)                    → soldierCard / soldierBorder / soldierContent / soldierCheckBadge
   ├─ VsBadge        circle_128 + TMP "VS"
   └─ ConfirmButton  Btn_Primary, label "CONFIRM SELECTION"    → CharacterSelectUI.confirmButton
```

## Elements (anchored px, +y up; under Safe, center anchor unless noted)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| Dim | stretch | 0,0 | stretch | Image black @50%, raycast ON (blocks menu behind) |
| BackBtn | top-left (0,1) | 60,-60 | 80,80 | `IconBtn` + `Icons/back` |
| **ProfileBadge** | top-right (1,1) | -40,-40 | **480×72** | Star icon + level, coin icon + amount, gem icon + amount — [`02_character_select.svg`](../02_character_select.svg) |
| Title | top-center (.5,1) | 0,-70 | 1200,80 | TMP "CHOOSE YOUR ARCHER", **H2 52**, Bold, Center, white |
| AdventurerCard | center | -410,-20 | 660,700 | Button. Card bg = **Border** child (rounded_32) + inset Content panel (rounded_24, BgPanel). |
| SoldierCard | center | 410,-20 | 660,700 | mirror |
| VsBadge | center | 0,30 | 120,120 | Image `circle_128` (BgPanelDeep) + TMP "VS" Gold, H1 |
| ConfirmButton | bottom-center (.5,0) | 0,40 | 560,90 | `Btn_Primary`, label "CONFIRM SELECTION" |

**Inside each card** (anchored within the 660×700 card, center anchor):
| Child | Pos | Size | Content |
|---|---|---|---|
| Border | 0,0 | 660,700 | Image `Shapes/rounded_32`, Sliced. Color set by script (Gold when selected, white@8% when not). |
| Content (panel) | 0,0 | 636,676 | Image `Shapes/rounded_24`, Sliced, BgPanel. + CanvasGroup. Holds the rest. |
| Art | 0,60 | 360,360 | Image of the real character sprite (`Art/Sprites/Player1_Adventurer/archer_idle` etc.) |
| Name | 0,-210 | 600,70 | TMP, **H1 58**, Bold, Gold (Adventurer) / white (Soldier), Center |
| Tagline | 0,-270 | 600,40 | TMP, **Caption 22**, TextHint, Center, tracking +4 |
| StatSpeed | 0,-320 | 500,28 | `Bar` recipe + label "SPEED", fill ~Gold |
| StatPower | 0,-360 | 500,28 | `Bar` recipe + label "POWER", fill ~Success |
| CheckBadge | top-right (1,1) -40,-40 | 64,64 | `circle_128` Gold + `Icons/check`. **Toggled** by script. |
| **LockOverlay** | stretch (Soldier only) | 0,0 | 660,700 | When locked: dim + padlock + "REQUIRES LV 5" + "500 COINS" pill. Hidden when owned. |

### Level / coin gating (Soldier)

| Requirement | Default |
|-------------|---------|
| Account level | 5 |
| Coin cost | 500 (spent on unlock) |
| Free by default | Adventurer (index 0) |

Blueprint: lock overlay in [`02_character_select.svg`](../02_character_select.svg). Rules: [`11_progression_economy.md`](11_progression_economy.md).

## Wiring — `CharacterSelectUI` (add to the CharacterSelectPanel or Canvas)
Map every field above (arrows in the hierarchy). The script handles: click a card → highlight + show its check badge + fade the other's content; **Confirm** → saves `SelectedCharacter`, then (Practice) loads `GameArena`, or (Online) calls `NetworkManager.ConnectAndPlay()`; **Back** → hides the panel.

## Integration (flow)
Currently `MainMenuController` starts the match directly. To insert character-select first, have the menu's Play buttons call `CharacterSelectUI.Show()` instead — this is a small wiring/code change you can do later. Until then the panel is reachable but not in the default path; building it now is still correct.

## Verify
Open the panel (temporarily call `Show()` or enable it). Click each card → gold frame + check badge swap, other card dims. Confirm (Practice) → loads GameArena. Back → hides.
