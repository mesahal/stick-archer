# Stick Archer — Asset Generation Guide (design-system aligned)

> **Read `designs/specs/00_foundations.md` first — it is the visual source of truth.** This
> doc was reconciled against `designs/` and the existing `Assets/Art/` on 2026-06-07. Most of
> the UI kit **already exists**; only a few items are genuinely missing. Generate nothing that
> already exists, and follow the **white-shape + gradient-fill** architecture below.

---

## 0. The design-system architecture (do not break this)

The UI is **not** built from pre-colored PNGs. Per `00_foundations.md §3–4`:
- **Shapes** are plain **white** 9-slice sprites (`Assets/Art/UI/Shapes/`).
- **Color** comes from tiny **gradient strips** (`Assets/Art/UI/Gradients/`, 1×64 vertical)
  placed as a masked Fill inside the shape.
- **Icons** are flat **white** sprites, tinted in-engine via `Image.color`.

➡ So a "gold button" = `pill_128` shape + `title_gold` gradient fill. **Never bake a colored,
labeled button as a single PNG.** New elements reuse existing shapes/gradients + a tint.

Palette/sizes live in `UIDesignSystem.cs` and the foundations spec — reference those.

---

## 1. Inventory — ALREADY EXISTS (do NOT regenerate)

| Asset group | Files | Location |
|---|---|---|
| **UI shapes (9-slice)** | `rounded_16/24/32`, `pill_128`, `pill_bar`, `circle_128` | `Assets/Art/UI/Shapes/` |
| **UI gradients** | `btn_primary`, `btn_success`, `btn_danger`, `btn_warning`, `btn_gold`, `title_gold`, `panel_bg`, `panel_dark`, `hp_full`, `hp_low`, `charge_meter`, `bg_sky_menu` | `Assets/Art/UI/Gradients/` |
| **Icons (13)** | gear, globe, robot, play, pause, back, close, check, retry, home, sound, warning, spinner | `designs/icons/*.svg` + `Assets/Art/UI/Icons/*.png` |
| **Backgrounds** | bg_sky, bg_hills, bg_mountains_far/near, cloud1–3, castle, tree01/05 | `Assets/Art/Backgrounds/` |
| **Characters (base)** | adventurer + soldier: idle/charge/fire/ragdoll/stand/tilesheet | `Assets/Art/Sprites/Player{1,2}_*` |
| **Screen blueprints** | 11 SVG mockups + per-screen build specs | `designs/*.svg`, `designs/specs/*.md` |
| **Kenney source packs** | Already imported into `Assets/Art/` (Backgrounds, Platforms) |

If a screen needs a button/panel/bar, **assemble it from the above** per the foundations
"Chrome Recipes" — no new art required.

---

## 2. What's ACTUALLY missing

| Need | Why | Best source |
|---|---|---|
| **7 progression icons** — coin, xp/level-up, trophy, star, heart, target, bow | New economy/profile UI (P2) references them; not in the 13-icon set | **Hand-authored SVG** matching `designs/icons/` style (agent can write these) → existing `inkscape` PNG pipeline. *AI only as fallback.* |
| **VFX particle textures** | `Assets/Art/Particles/` is empty; Track V VFX needs them | Code/tool-generated soft greyscale, **or** AI (prompts in §4) |
| **App icon + splash** | Store submission (P0) + branding | **AI is appropriate here** (prompts in §4) |
| **Art-quality upgrade** (richer characters/animation, painted bg) | *Optional* — current art is functional | AI or Kenney packs in `Assets/Art/` |

> **Recommendation:** for the 7 icons, let me write them as SVGs in the existing style — it's
> more cohesive than AI and free. Use AI mainly for branding and the optional art upgrade.

---

## 3. Missing icons — SVG spec (preferred over AI)

Match `designs/icons/`: `viewBox="0 0 128 128"`, white fill (`#FFFFFF`), centered with
`translate(64,64)`, simple geometric shapes, ~12% padding. Tinted in-engine (coin/star→gold,
heart→red). Target: `designs/icons/<name>.svg` → export to `Assets/Art/UI/Icons/<name>.png`.

| File | Shape description |
|---|---|
| `coin.svg` | filled circle with a smaller inset ring + a tiny arrow/▲ emblem in the center |
| `xp.svg` | upward chevron/double-chevron inside a rounded badge |
| `trophy.svg` | cup bowl + two side handles + stepped base |
| `star.svg` | five-point rounded star |
| `heart.svg` | rounded heart |
| `target.svg` | three concentric rings + center dot |
| `bow.svg` | curved recurve bow arc + straight nocked arrow |

*(If you'd rather AI them, use the §4 icon prompt — but they must be flat white on transparent
to drop into the tint pipeline.)*

---

## 4. AI prompts — only for the genuinely AI-appropriate items

All prompts: transparent PNG (or flat bg + key out), generate variants, keep the cohesive one.
**Negative prompt (append to every):**
```
text, letters, watermark, signature, photo, realistic, 3d render, busy background, multiple
objects, cropped, blurry, low-res, jpeg artifacts, border, mockup, UI screenshot
```

### 4A. VFX particle textures — greyscale/white, soft, transparent
Frame:
```
A single soft game VFX particle texture: <SUBJECT>, white/greyscale on transparent background,
soft feathered edges, centered, no hard outline, no scene. [NEGATIVE PROMPT]
```
| File → `Assets/Art/Particles/` | Size | `<SUBJECT>` |
|---|---|---|
| `fx_glow.png` | 256² | soft radial glow, bright center fading to transparent |
| `fx_spark.png` | 128² | small four-point sparkle flare |
| `fx_smoke.png` | 256² | round soft smoke puff, wispy edges |
| `fx_dust.png` | 128² | small soft dust mote cloud |
| `fx_trail.png` | 256×64 | tapering streak, bright head → thin tail |
| `fx_impact_ring.png` | 256² | thin soft expanding shockwave ring |
| `fx_hit_burst.png` | 256² | radial burst of short white shards |

### 4B. App icon — 1024×1024, opaque (store requirement)
```
A mobile game app icon: a dynamic stick archer drawing a glowing gold bow, bold and readable
at small sizes, dark navy #141A29 background with a gold #FFD933 accent and a subtle radial
glow, centered hero composition, rounded-square framing, clean flat-vector with soft glossy
shading. No text. [NEGATIVE PROMPT]
```

### 4C. Splash logo emblem — 1024×1024, transparent
```
A game logo emblem: a gold #FFD933 bow-and-crossed-arrows crest, polished metallic shading,
centered, isolated, transparent background, clean vector. No text. [NEGATIVE PROMPT]
```

### 4D. (Optional) art-quality character upgrade — generate the SET together
Only if upgrading past the current sprites. Keep proportions consistent across poses and feet
on a constant baseline (engine aligns by feet, ~1.5 world-units tall).
```
Full-body side-view of <a brave adventurer in a green tunic with a scarf | a soldier in light
blue armor with a helmet>, holding a bow, facing right, simple cartoon proportions, clean
vector cel-shading, bright saturated colors, slight rim light, feet flat on an invisible
ground line. Pose: <idle relaxed | drawing the bowstring | releasing the arrow | flinching
from a hit | limp knocked-down ragdoll>. Isolated, transparent background. [NEGATIVE PROMPT]
```
→ `Assets/Resources/Characters/Player1|Player2/archer_{idle,charge,fire,hit,ragdoll}.png`
*(Alternative: Kenney platformer packs in `Assets/Art/` are vector and on-style.)*

### 4E. (Optional) background upgrade
Existing `Assets/Art/Backgrounds/` already covers sky/hills/mountains/clouds. Only regenerate
for a deliberate art-direction change; match the dusk navy palette
(`#1A2552 → #0F1A38 → #0A0E1C`) and keep parallax layers transparent above the horizon.

---

## 5. Fonts — not AI

The spec defaults to **LiberationSans SDF**. For a more "game" feel, download app-embeddable
fonts (Google Fonts / SIL OFL) and hand over `.ttf`: display **Luckiest Guy / Fredoka /
Baloo 2**; body **Nunito / Poppins**. TextMeshPro bakes them.

---

## 6. Import settings

Already documented authoritatively in **`designs/specs/00_foundations.md §3`** (Sprite type,
filter, mip maps, and the exact **9-slice border** values per shape). New assets:
- **Icons (incl. the 7 new):** Sprite, Bilinear, no mips, white (tint in-engine) → `Assets/Art/UI/Icons/`.
- **VFX:** Sprite/Default, Alpha Is Transparency, atlas → `Assets/Art/Particles/`.
- **Characters:** PPU so on-screen height ≈ 1.5 world units, pivot bottom-center → `Resources/Characters/...`.
- **App icon/splash:** Player Settings (Android icons) / Splash Image — not a runtime folder.
