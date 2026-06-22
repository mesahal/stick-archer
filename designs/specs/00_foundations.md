# 00 — Foundations (read this first)

Shared setup + reusable "recipes" every screen spec references. Build the recipes once as **prefabs** in `Assets/Resources/UI/` (or a `Prefabs/UI/` folder) and reuse them — you'll assemble screens by dropping in prefabs, not rebuilding buttons each time.

**Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) · Section 04–05 in [`../00_design_system.svg`](../00_design_system.svg).

All numbers are in **reference-resolution pixels (1920×1080)**. The Canvas Scaler converts them to device pixels automatically.

---

## 1. Design tokens

| Token | Hex | Use |
|---|---|---|
| BgDark | `#141A29` | screen background |
| BgPanel | `#1F2438` | panels (≈96% alpha) |
| BgPanelDeep | `#0F1421` | bar tracks, deep insets |
| Primary | `#268CF2` | primary action (blue) |
| Success | `#33B859` | confirm / vs-AI (green) |
| Warning | `#F28C1A` | charge mid, caution |
| Danger | `#F23F3F` | destructive, low HP |
| Neutral | `#737380` | disabled, secondary |
| Gold | `#FFD933` | titles, highlights, score, **coins** |
| Gem | `#6B8CFF` | **gems**, premium highlights |
| TextHi | `#FFFFFF` | primary text |
| TextMid | white @ 75% | secondary text |
| TextDim | white @ 50% | hints/captions |

**Type scale (TMP, LiberationSans SDF default):** Display 120 · H1 72 · H2 52 · Body 36 · Small 28 · Caption 22.
**Spacing (4pt grid):** 4 · 8 · 16 · 24 · 32 · 48 · 64. **Radii:** small 16 · med 24 · pill 60+.
**Touch:** every button ≥ 96×96; keep ≥16 between adjacent tappables.

> These tokens exist in code at [`Assets/Scripts/UI/UIDesignSystem.cs`](../../Assets/Scripts/UI/UIDesignSystem.cs) (palette, font sizes, spacing, button sizes, plus `GetHealthColor()` / `GetChargeColor()` helpers). Reference that class from scripts instead of hardcoding. Button sizes there: **primary 640×140 · secondary 560×90 · icon 96×96**.

---

## 2. Canvas (every scene's UI root)

### Canvas Scaler (Inspector)

| Setting | Value | Why |
|---|---|---|
| **Render Mode** | Screen Space - Overlay | Overlays gameplay |
| **UI Scale Mode** | Scale With Screen Size | Responsive to device size |
| **Reference Resolution** | 1920 × 1080 | Baseline (all specs in reference px) |
| **Screen Match Mode** | Match Width Or Height | Both dimensions scale smoothly |
| **Match** | 0.5 | Balances width/height scaling |

### Layer Structure

Every screen root uses this two-level hierarchy so background art bleeds to the edges while content respects safe area:

```
Canvas  (scale, raycast, canvas scaler as above)
├─ BG                           (full-bleed background)
│  └─ (art: bg_sky, parallax, etc. → Image, stretch anchor 0,0–1,1, offsets 0)
└─ Safe                         (child RectTransform, stretch anchor 0,0–1,1, offsets 0)
   ├─ SafeAreaFitter.cs        (re-anchors at runtime for notches/gesture bars)
   └─ (all UI panels/buttons/HUD children live here)
```

**SafeAreaFitter setup:**
- Add to `Safe` RectTransform as a component.
- It reads `Screen.safeArea` at `Awake()` and shifts `Safe` inward, leaving `BG` full-bleed.
- Result: HUD buttons clear the notch, title art still fills the screen.
- Location: `Assets/Scripts/UI/SafeAreaFitter.cs` (phase 1 deliverable).

---

## 3. Importing the UI sprites (one-time)

Select each sprite in `Assets/Art/UI/…` and set in the Inspector:

- Texture Type **Sprite (2D and UI)**, Sprite Mode **Single**
- Filter Mode **Bilinear**, Generate Mip Maps **OFF**, Wrap Mode **Clamp**
- Mesh Type **Full Rect**, Compression **None** (these are tiny)

Then set **9-slice borders** (Sprite Editor → drag the green guides, or type Border L/R/T/B):

| Sprite | Size | Image Type | Border L,R,T,B | Purpose |
|---|---|---|---|---|
| `Shapes/rounded_16` | 128² | **Sliced** | 18,18,18,18 | small panels |
| `Shapes/rounded_24` | 128² | **Sliced** | 26,26,26,26 | cards / modals |
| `Shapes/rounded_32` | 128² | **Sliced** | 34,34,34,34 | large cards |
| `Shapes/pill_128` | 128² | **Sliced** | 63,63,0,0 | pill buttons (stretches horizontally; caps stay round) |
| `Shapes/pill_bar` | 64×24 | **Sliced** | 12,12,0,0 | bar tracks |
| `Shapes/circle_128` | 128² | **Simple** | — | icon buttons, portraits (uniform scale only) |
| `Gradients/*` (`btn_*`, `panel_*`, `hp_*`) | 1×64 | **Simple** | — | vertical fill; stretch to any rect |
| `Gradients/charge_meter` | 128×1 | **Simple** | — | horizontal fill (left→right) |

> Border values for `rounded_16/24/32` assume the number = corner radius. If a corner looks distorted when stretched, open the Sprite Editor and drag each border line to exactly where the corner curve ends.

**Icons:** convert `designs/icons/*.svg` → PNG (Inkscape `inkscape -w 128 -h 128 in.svg -o out.png`, or any SVG→PNG tool), drop into `Assets/Art/UI/Icons/`. Import as Sprite, Bilinear, no mip maps. They're white, so tint them with the `Image.color` you need.

---

## 4. Chrome Recipes (reusable components for all screens)

### A. Gradient Pill Button

**Pattern:** pill shape (Image) + Mask + nested Fill gradient + optional Icon + Label.

**Hierarchy:**
```
Button (RectTransform)
├─ Image               sprite = Shapes/pill_128, Type = Sliced, Color = white
├─ Mask                (Show Mask Graphic = ON)
├─ ButtonAnimator
├─ Fill Image          sprite = Gradients/btn_primary (or btn_success/danger/gold)
│                      Type = Simple, anchors stretch, offsets (0,0,0,0),
│                      Raycast Target = OFF
├─ Icon Image          (optional) sprite = Icons/<name>, white, ~40×40,
│                      left-aligned, Raycast Target = OFF
└─ Label TMP           "BUTTON LABEL", Body 36 Bold, TextHi, Center, Raycast Target = OFF
```

**Variants** (swap Fill sprite):

| Button Type | Size | Fill Sprite | Use |
|---|---|---|---|
| **Btn_Primary** | 640×140 | `btn_primary` | Play, Continue, main action |
| **Btn_Primary_Sm** | 560×90 | `btn_primary` | Secondary action, Settings |
| **Btn_Success** | 640×140 | `btn_success` | Victory, Confirm, Rematch |
| **Btn_Danger** | 560×90 | `btn_danger` | Quit, Delete, Error |
| **Btn_Gold** | 640×140 | `title_gold` | Menu title (special) |

**Fallback (no Mask):** skip Fill+Mask, set pill `Image.color` to solid token color (e.g., Primary #268CF2). Still works, just flat.

### B. Outline Button

**Pattern:** faint pill + Label (no Fill gradient).

```
Button (RectTransform, 560×90)
├─ Image               sprite = Shapes/pill_128, Type = Sliced, Color = white @ 12% alpha
├─ ButtonAnimator
└─ Label TMP           "LABEL", Body 36, TextHi, Center, Raycast Target = OFF
```

Use for "Main Menu", "Cancel", "Back", secondary actions.

### C. Card / Panel

**Pattern:** rounded rectangle container with optional top accent bar.

```
Image (RectTransform, size 1040×280 typical, anchors center)
├─ (background) sprite = Shapes/rounded_24 or rounded_32, Type = Sliced, Color = BgPanel
├─ TopAccent Image     (optional) sprite = pill_bar, Type = Sliced, h=6,
│                      anchor top-stretch, Color = Primary/Gold/Danger
└─ (children) Labels, cards, nested panels, etc.
```

**For Modals** (full-screen with dim):
```
Canvas
├─ Dim Image           (full-stretch), Color = #000000 @ 65% alpha, Raycast Target = ON
└─ Modal Card          (card Image as above, 800×660 typical, anchors center)
   ├─ TopAccent        (6px color bar)
   └─ (content)        nested children
```

### D. Health / Charge Bar

**Pattern:** background track + fill Image with `fillAmount` driven by code.

```
Bar RectTransform (typical 800×40, anchors stretch)
├─ Track Image         sprite = Shapes/pill_bar or rounded_16, Type = Sliced,
│                      Color = BgPanelDeep
└─ Fill Image          sprite = Gradients/hp_full (health) or charge_meter (charge),
│                      Type = Simple, fillMethod = Horizontal, fillOrigin = Left,
│                      fillAmount = driven by HealthBarUI.SetHealth(0–1) or similar,
│                      anchors stretch, offsets (0,0,0,0), Raycast Target = OFF
```

Reuse existing: `HealthBarUI.cs` (swaps hp_full/hp_low) and `ChargeMeterUI.cs`.

### E. Icon Button (Circular)

**Pattern:** circle + centered white icon.

```
Button (RectTransform, 96×96)
├─ Image               sprite = Shapes/circle_128, Type = Simple, Color = BgPanel
├─ ButtonAnimator
└─ Icon Image          sprite = Icons/<name>, white, ~48×48, anchors center (0,0),
                       size (48,48), Raycast Target = OFF
```

Use for pause (HUD top-right) and settings gear.

---

## 5. Button feedback
Add `ButtonAnimator` (existing, `Assets/Scripts/ButtonAnimator.cs`) to every button — it does the press-scale punch and the slide-in-on-enable. No per-button config needed.

## 6. Notes carried from the comps
- Menu has **no difficulty selector** and **no status text** — `MainMenuController` already null-checks both, so leave those Inspector fields empty. AI difficulty defaults to Normal (`GameMode.Difficulty`).
- HUD has **no kill feed**.
- Results cards show **final score only**.

---

## 7. Shared meta components (Cricket League tier)

See also [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) for audit rules and the standard `<defs>` block every SVG must use.

### F. ProfileBadge

**480×72** single-row pill — Main Menu (top-left), Char Select, Lobby (top-right). No XP bar.

```
ProfileBadge (480×72)
├─ Image               rounded_16, BgPanel @ 34% alpha, stroke white @ 8%
├─ LevelIcon           Icons/star, Gold tint (star in faint gold circle)
├─ LevelLabel TMP      "{n}", TextHi Bold 28
├─ Divider             1px vertical white @ 8%
├─ CoinIcon            Icons/coin, Gold tint
├─ CoinLabel TMP       "{amount}", TextHi Bold 28
├─ Divider
├─ GemIcon             Icons/gem, Gem #6B8CFF tint
└─ GemLabel TMP        "{amount}", TextHi Bold 28
```

### G. RewardsStrip

**1040×88** — below score card on Victory/Defeat results.

```
RewardsStrip (1040×88, two pills side-by-side)
├─ CoinPill (520×88)   rounded_16, BgPanel @ 55%, coin icon + "+35 COINS" gold + "+70 XP" white
└─ LevelPill (520×88)  optional "LEVEL UP" badge when XP triggers level-up
```

### H. Modal shell

Used by Settings, Error, Level Up, Login Rewards.

```
ModalRoot
├─ Dim Image           full-stretch, #000 @ 65%
└─ ModalCard (800×600) rounded_24, gPanel fill, shadowDeep
   ├─ TopAccent        6px Gold (default) or Primary/Danger per context
   ├─ CloseBtn         96×96 circle, Icons/close
   └─ (content)
```

### I. Character card

**660×700** — Char Select and Lobby player cards.

| State | Border | Top accent |
|-------|--------|------------|
| Selected / YOU | 3px Gold gradient | 6px Gold |
| Opponent / default | 2px white @ 8% | none |
| Locked | Lock overlay + Neutral label | none |

### J. Premium checklist (every screen)

- [ ] Buttons use gradient fill + top highlight strip + `shadowSoft`
- [ ] Cards use `gPanel` + white @ 8-12% stroke + `shadowSoft`
- [ ] Standard defs IDs: `gPrimary`, `gSuccess`, `gDanger`, `gGold`, `gPanel`, `gBgVert`, `shadowSoft`, `shadowDeep`, `goldTitle`
- [ ] Meta screens show ProfileBadge where applicable
- [ ] ASCII-only text in SVG footer labels

