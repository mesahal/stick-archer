# 06 — Pause Menu (build spec)

Blueprint: [`designs/06_pause_menu.svg`](../06_pause_menu.svg). Script: [`PauseMenuUI.cs`](../../Assets/Scripts/UI/PauseMenuUI.cs) (already exists). Read [`00_foundations.md`](00_foundations.md). Scene: `GameArena`.

`PauseMenuUI` sets `Time.timeScale = 0` on open and animates the modal with **unscaled** time. The HUD pause button (`UIManager.pauseButton`, spec 05) is auto-wired to `TogglePause()`.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas (GameArena) › (NOT under Safe — full-screen dim covers everything)
└─ PauseOverlay        (full-rect; starts INACTIVE)         → PauseMenuUI.pauseOverlay
   ├─ Dim       Image #000 @70% (full-rect, raycast ON)     → PauseMenuUI.dimBackground
   └─ Modal     Card rounded_32 + CanvasGroup, 680×720      → PauseMenuUI.modalCanvasGroup
      ├─ TopAccent  gold 6px bar at top
      ├─ PauseIcon  circle (faint) + Icons/pause (Gold)
      ├─ Title      TMP "PAUSED"
      ├─ Subtitle   TMP "Take a breath, archer"
      ├─ ResumeBtn  Btn_Primary→Success "RESUME" (play icon)  → PauseMenuUI.resumeButton
      ├─ SettingsBtn Btn_Primary "SETTINGS" (gear icon)       → PauseMenuUI.settingsButton
      ├─ QuitBtn    Btn_Outline Danger "QUIT TO MENU" (exit)  → PauseMenuUI.quitButton
      └─ StatusText TMP                                       → PauseMenuUI.statusText
```

## Elements (anchored px, +y up; Modal 680×720 centered, children center anchor)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| Dim | stretch | 0,0 | stretch | black @70%, raycast ON |
| Modal | center | 0,0 | 680,720 | `Card` rounded_32, BgPanel |
| TopAccent | top-stretch | 0,0 | h=6 | Image Gold |
| PauseIcon | center | 0,220 | 128,128 | `circle_128` (Gold @12%) + `Icons/pause` Gold |
| Title | center | 0,110 | 600,80 | TMP "PAUSED", **Display-ish 56**, Bold, white, tracking +8 |
| Subtitle | center | 0,60 | 600,30 | TMP, Small, TextHint, Center |
| ResumeBtn | center | 0,-30 | 560,90 | `Btn` Success, label "RESUME", `Icons/play` |
| SettingsBtn | center | 0,-145 | 560,90 | `Btn` Primary, label "SETTINGS", `Icons/gear` |
| QuitBtn | center | 0,-260 | 560,90 | `Btn_Outline` Danger, "QUIT TO MENU", exit icon |
| StatusText | center | 0,-330 | 600,30 | TMP, Caption, TextDim, Center |

## Wiring — `PauseMenuUI` (add to PauseOverlay or the HUD object)
Assign `pauseOverlay`, `dimBackground`, `modalCanvasGroup` (the Modal's CanvasGroup), `resumeButton`, `settingsButton`, `quitButton`, `statusText`. Leave `animateOpen` on.
- **Resume** → unpause. **Settings** → opens `SettingsPanel` (spec 03) via `FindObjectOfType<SettingsPanel>().Toggle(true)`. **Quit** → returns to MainMenu (Practice or Online handled by the script).
- ESC / Android back also resumes.

## Verify
In a match, tap the HUD pause icon → game freezes (`Time.timeScale = 0`), modal scales in. Resume → unfreeze. Settings → settings modal over the pause. Quit → MainMenu.
