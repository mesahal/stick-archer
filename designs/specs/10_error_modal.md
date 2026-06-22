# 10 — Error / Connection Lost (build spec)

Blueprint: [`designs/10_error_modal.svg`](../10_error_modal.svg). Script: [`ErrorModalUI.cs`](../../Assets/Scripts/UI/ErrorModalUI.cs) (already exists, reusable in any scene). Read [`00_foundations.md`](00_foundations.md).

Call from code: `ErrorModalUI.Instance.ShowConnectionError(details)` or `Show(title, body, onOk)` / `ShowWithRetry(title, body, onOk, onRetry)`. The script animates open with unscaled time and hides the retry button for simple messages.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas › (full-screen, NOT under Safe)
└─ ErrorOverlay         (full-rect; starts INACTIVE)        → ErrorModalUI.errorOverlay
   ├─ Dim       Image #000 @70% (raycast ON)
   └─ Modal     Card rounded_32 + CanvasGroup, 800×660      → ErrorModalUI.modalCanvasGroup
      ├─ TopAccent   Danger 6px bar
      ├─ WarningIcon Image circle glow + Icons/warning       → ErrorModalUI.warningIcon
      ├─ Title       TMP "CONNECTION LOST"                   → ErrorModalUI.titleText
      ├─ Body        TMP (message + error code)              → ErrorModalUI.bodyText
      ├─ RetryButton Btn_Primary "RETRY" (retry icon)        → ErrorModalUI.retryButton
      └─ OkButton    Btn_Outline "MAIN MENU" (back icon)     → ErrorModalUI.okButton
```

## Elements (anchored px, +y up; Modal 800×660 centered, children center anchor)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| Dim | stretch | 0,0 | stretch | black @70%, raycast ON |
| Modal | center | 0,0 | 800,660 | `Card` rounded_32, BgPanel (slightly warm dark) |
| TopAccent | top-stretch | 0,0 | h=6 | Image Danger |
| WarningIcon | center | 0,150 | 160,160 | `circle_128` (Danger glow @ low alpha) + `Icons/warning` tinted Danger → assign the **Image** to `warningIcon` |
| Title | center | 0,0 | 720,70 | TMP, **H1 52**, Black, Danger/white, Center, tracking +6 |
| Body | center | 0,-90 | 700,120 | TMP, Small, TextSecondary, Center (script sets the message) |
| RetryButton | center | -170,-230 | 320,80 | `Btn` Primary, "RETRY", `Icons/retry` |
| OkButton | center | 170,-230 | 320,80 | `Btn_Outline`, "MAIN MENU", `Icons/back` |

## Wiring — `ErrorModalUI` (add to ErrorOverlay; it's a singleton)
Assign `errorOverlay`, `modalCanvasGroup` (Modal's CanvasGroup), `titleText`, `bodyText`, `warningIcon`, `retryButton`, `okButton`.
- `Show(...)` hides the retry button; `ShowWithRetry(...)` / `ShowConnectionError(...)` show it (retry → `NetworkManager.ConnectAndPlay()`).
- For the "MAIN MENU" action, pass an `onOk` that returns to the menu, e.g. `ErrorModalUI.Instance.ShowWithRetry("CONNECTION LOST", msg, onOk: () => NetworkManager.Instance.ReturnToMenu(), onRetry: () => NetworkManager.Instance.ConnectAndPlay())`.

## Integration (optional)
Nothing calls the modal yet. To surface real disconnects, call `ErrorModalUI.Instance.ShowConnectionError(cause.ToString())` from `MainMenuController.OnDisconnected` (and/or `NetworkManager.OnDisconnected`). Small code hook; building the modal now is still correct.

## Verify
Temporarily call `ErrorModalUI.Instance.ShowConnectionError("DISCONNECT_BY_PEER")` (e.g., from a debug key): modal scales in over a dim screen, warning icon + title + body show, Retry reconnects, Main Menu dismisses/returns.
