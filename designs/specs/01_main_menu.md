# 01 — Main Menu (build spec)

Blueprint: [`designs/01_main_menu.svg`](../01_main_menu.svg). Read [`00_foundations.md`](00_foundations.md) first (Canvas, recipes, prefabs).

Scene: `Assets/Scenes/MainMenu.unity`. Reuse the existing Canvas + `MainMenuController`; rebuild via `Tools → Design Sync → 4 – Polish MainMenu (v12)`.

All positions are **anchoredPosition (x,y)** in 1920×1080 px. "+y is up." Sizes are **sizeDelta (w,h)**.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy

```
Canvas  (Screen Space-Overlay, scaler 1920×1080 @0.5)
├─ BG                Image  UI/Backgrounds/menu_bg   (stretch, full-bleed — composite sky+stars+mountains+ground)
└─ Safe              stretch 0,0–1,1, offsets 0  (no SafeAreaFitter in editor build)
   ├─ ProfileBadge   ProfileBadge component + pill UI (level, coins, gems)
   ├─ Title          TMP  "STICK ARCHER"   + Bob
   ├─ Subtitle       TMP  "BATTLE OF THE BOWS"
   ├─ SubtitleOrnament  image lines + dot
   ├─ PlayOnline     pill button  (globe icon)
   ├─ VsComputer     pill button  (robot icon)
   ├─ Gear           icon button  (gear icon)
   ├─ FooterL        TMP  version
   └─ FooterR        TMP  copyright
```

**Background approach:** use the pre-rendered **`menu_bg.png`** (1920×1080, exported from `menu_bg.svg`) as a single full-screen Image. Do **not** stack separate mountain/star/glow layers — the composite PNG matches the design exactly.

---

## Elements

| Element | Anchor | Pivot | Pos (x,y) | Size (w,h) | Content / settings |
|---|---|---|---|---|---|
| **BG** | stretch | .5,.5 | 0,0 | stretch | Image `Art/UI/Backgrounds/menu_bg.png`, Simple, white. Raycast off. |
| **ProfileBadge** | top-left (0,1) | 0,1 | 0,0 | 0,0 | Empty root; child **ProfilePill** at (40,-40), size **480×72**. See profile table below. |
| **Title** | top-center (.5,1) | .5,1 | 0,-95 | 1600,240 | TMP "STICK ARCHER", size **160**, Bold/Black, Center, Gold gradient `#FFF3A0`→`#C9990A`. Letter spacing +8. Outline `#3A2200` ~0.22. **Shadow** dy=-8 α35%. `Bob` (amp 12, speed 1.2). |
| **Subtitle** | top-center (.5,1) | .5,1 | 0,-322 | 1200,50 | TMP "BATTLE OF THE BOWS", size **28**, Center, TextMid (white 75%). Letter spacing +12. |
| **SubtitleOrnament** | top-center (.5,1) | .5,.5 | 0,-370 | 400,20 | Gold WS lines (±200→±40), **chevron** arrows at ±30, Circle128 dot r=6. |
| **PlayOnline** | center (.5,.5) | .5,.5 | 0,**0** | 640,140 | `btn_primary` fill. Label "PLAY ONLINE" **44** Bold, letter-spacing +3. Icon `globe` 56×56 at x=-230. **DropShadow** dy=-6. |
| **VsComputer** | center (.5,.5) | .5,.5 | 0,**-180** | 640,140 | `btn_success` fill. Label "VS COMPUTER" 44 Bold. Icon `robot`. DropShadow. |
| **Gear** | top-right (1,1) | 1,1 | -40,-40 | 72,72 | Circle128 bg, icon `gear`; **same top edge as ProfileBadge** (y=40), 40px from right. OnClick → SettingsPanel. |
| **FooterL** | bottom-left (0,0) | 0,0 | 40,30 | 420,30 | TMP "v1.0.0 · Build 142", size 20, TextDim, Left. |
| **FooterR** | bottom-right (1,0) | 1,0 | -40,30 | 420,30 | TMP "© Stick Archer 2026", size 20, TextDim, Right. |

### ProfileBadge (ProfilePill children)

| Child | Anchor | Pos | Size | Notes |
|---|---|---|---|---|
| **ProfilePill** | top-left | (40,-40) | **480×72** | `rounded_16` sliced, fill `rgba(26,31,51,0.34)`, subtle white border |
| **LevelIcon** | left-center | (36,0) | 40×40 | `Icons/star`, Gold |
| **Level** | left-center | (68,0) | 48×36 | TMP "{n}", 28 Bold, TextHi |
| **Divider1** | — | x=128 | — | 1px vertical white 8% alpha |
| **CoinIcon** | left-center | (168,0) | 32×32 | `Icons/coin` |
| **Coins** | left-center | (196,0) | 100×36 | TMP amount, 28 Bold, white |
| **Divider2** | — | x=308 | — | 1px vertical white 8% alpha |
| **GemIcon** | left-center | (348,0) | 32×32 | `Icons/gem` |
| **Gems** | left-center | (384,0) | 64×36 | TMP amount, 28 Bold, white |

Data source: `ProfileManager.Instance.Profile` (level, xp, coins, **gems**). Refreshes on `OnProfileChanged`.

---

## Wiring — `MainMenuController`

| Field | Assign |
|---|---|
| `playOnlineButton` | **PlayOnline** → Button |
| `practiceButton` | **VsComputer** → Button |
| `difficultyDropdown` | *(leave empty)* |
| `statusText` | *(leave empty)* |

ProfileBadge is **baked into the scene** by the v12 editor tool. `MainMenuController` only creates one at runtime if missing (e.g. old scenes).

---

## Verify
1. Run `Tools → Design Sync → 4 – Polish MainMenu (v12)` (stop Play mode first).
2. Play MainMenu — background matches `01_main_menu.svg`; profile badge shows **level, XP, coins, and gems** top-left.
3. **VS Computer** → Practice mode. **Play Online** → Photon connect, no errors.
4. Resize Game view → layout holds; `menu_bg` still full-bleed.
