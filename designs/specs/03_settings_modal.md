# 03 — Settings Modal (build spec)

Blueprint: [`designs/03_settings_modal.svg`](../03_settings_modal.svg). Script: [`SettingsPanel.cs`](../../Assets/Scripts/SettingsPanel.cs) (already exists, drives `AudioManager`). Read [`00_foundations.md`](00_foundations.md).

The comp shows **SFX slider and Vibration toggle** only. Music, Mute All, and Aim Assist are **not** in the design — omit them from UI. Reachable from the gear button on the Main Menu and from the Pause menu's Settings button.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas (MainMenu and/or GameArena) › Safe
└─ SettingsPanel        (full-rect; starts INACTIVE)        → SettingsPanel.panel
   ├─ Dim       Image #000 @65% (full-rect, raycast ON)
   └─ Modal     Card (rounded_32, BgPanel), center, 800×440
      ├─ HeaderBar   Image rounded_32 top strip, BgPanelDeep
      │  ├─ Title    TMP "SETTINGS"  (H1 36, Bold, white)
      │  └─ CloseBtn IconBtn (close), top-right            → SettingsPanel.closeButton
      ├─ AudioHeader 32×32 icon tile + TMP "AUDIO" (label baseline y=23 in 32px row)
      ├─ SfxRow      Label "Sound Effects" + Slider + ValueText
      ├─ ControlsHeader 32×32 globe icon + TMP "CONTROLS"
      └─ VibrationRow  Label "Vibration" + Toggle
```

## Section header layout

Each section header is a **32px-tall row** at **`x=60`** — same left edge as the SFX slider track and vibration row content. Icon tile is 32×32 at `(0,0)` with glyph centered at `(16,16)`. Label TMP uses baseline **`y=23`** (26px Bold caps).

| Section | Icon | Tile tint |
|---------|------|-----------|
| AUDIO | `Icons/sound` | Gold @ 15% |
| CONTROLS | `Icons/globe` | Primary @ 15% |

## Elements (anchored px; Modal is 800×640 centered; children use Modal's center anchor)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| Dim | stretch | 0,0 | stretch | black @65% |
| Modal | center | 0,0 | 800,440 | `Card` (rounded_32, gPanel) — no top accent bar |
| HeaderBar | top-stretch | 0,0 | h=100 | rounded_32 BgPanelDeep |
| Title | top-left | 40,-50 | 400,60 | TMP "SETTINGS", Bold, white, H2 |
| CloseBtn | top-right (1,1) | -50,-50 | 64,64 | `IconBtn` + `Icons/close` |
| AudioHeader | top-left (0,1) | 60,-128 | 200,32 | Icon tile + "AUDIO" (baseline y=23); icon x aligns with slider |
| **SfxRow** | top-stretch (0,1)-(1,1) | 0,-180 | h=64 | "Sound Effects" + Slider + ValueText ("80"); slider starts x=60 |
| ControlsHeader | top-left (0,1) | 60,-280 | 220,32 | Globe icon tile + "CONTROLS"; same x column as slider |
| **VibrationRow** | top-stretch | 0,-332 | h=56 | "Vibration" + subtitle + Toggle (right) |

**Slider** build: Unity UI Slider, Background = `pill_bar` (BgPanelDeep), Fill = Primary gradient, Handle = `circle_128` white + shadowSoft. **Toggle** build: Background = `pill_128` (#3A3F55 off / Success on), knob = `circle_128` white.

## Wiring — `SettingsPanel`
| Field | Assign |
|---|---|
| `panel` | SettingsPanel root |
| `openButton` | the gear button (Main Menu **Gear**, or HUD gear) |
| `closeButton` | CloseBtn |
| `sfxSlider` | SFX Slider |
| `sfxValueText` | SFX value TMP |
| `muteToggle` / `musicSlider` / `musicValueText` | *(leave unassigned — not in design)* |

Vibration toggle: wire when haptics API exists; hide row until implemented.

It reads/writes `AudioManager` (SFX volume + mute) on change; values persist via PlayerPrefs. Pause menu's Settings button already calls `FindObjectOfType<SettingsPanel>().Toggle(true)`.

## Verify
From Main Menu, tap gear → modal opens; drag SFX → label updates and audio changes; Close → hides. Re-open → values persisted. No Music, Mute, or Aim Assist rows visible.
