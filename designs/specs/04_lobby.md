# 04 — Lobby / Matchmaking (build spec)

Blueprint: [`designs/04_lobby.svg`](../04_lobby.svg). Driven by [`UIManager.ShowLobby(string)`](../../Assets/Scripts/UIManager.cs) + [`NetworkManager`](../../Assets/Scripts/NetworkManager.cs). Read [`00_foundations.md`](00_foundations.md).

This is the **`lobbyPanel`** that `UIManager` shows during online matchmaking. `NetworkManager` pushes status strings through `UIManager.ShowLobby("Connecting..." / "Finding opponent..." / "Waiting for opponent...")`. Lives in the **MainMenu scene** (online flow runs there until the GameArena scene auto-loads). The layout intentionally follows the character-select screen style so the transition out of archer selection feels continuous.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy
```
Canvas › Safe
└─ LobbyPanel            (full-rect; starts INACTIVE)        → UIManager.lobbyPanel
   ├─ Bg           gBgVert gradient + mountain silhouettes (meta tier)
   ├─ ProfileBadge compact 480×72 top-right (star+level, coins, gems)
   ├─ Title       TMP "FINDING OPPONENT"
   ├─ Subtitle    TMP status hint
   ├─ YouCard     Character card 660×700, gold stroke + 6px top accent
   │  ├─ Content  Avatar, label, name, tagline, gradient stat bars
   │  └─ CheckBadge
   ├─ VsBadge     circle + gold ring + TMP "VS" (goldTitle gradient)
   ├─ OppCard     Character card, spinner + "SEARCHING..."
   ├─ StatusBar   gPanel pill + shadowSoft + StatusText
   └─ CancelBtn   gDanger gradient + highlight strip + shadowSoft
```

## Elements (anchored px, +y up; under Safe, center anchor unless noted)

| Element | Anchor | Pos | Size | Content |
|---|---|---|---|---|
| Dim | stretch | 0,0 | full | Black overlay at 50% alpha, raycast target enabled |
| Title | top-center (.5,1) | 0,-70 | 1200,80 | TMP "FINDING OPPONENT", **H2 52**, Bold, white, Center, character spacing +6 |
| GhostTitle | top-center (.5,1) | 0,-115 | 1400,140 | TMP "STICK ARCHER", 120, Bold, Gold at 20% alpha, behind Title |
| Subtitle | top-center (.5,1) | 0,-245 | 900,40 | TMP "BATTLE OF THE BOWS", Small, TextHint, Center, character spacing +10 |
| YouCard | center | -410,-20 | 660,700 | Same card treatment as character select, gold frame, Adventurer sprite, "YOU", name, tagline, stats, check badge |
| VsBadge | center | 0,30 | 120,120 | Image `circle_128` BgPanelDeep + TMP "VS", Gold |
| OppCard | center | 410,-20 | 660,700 | Same card treatment as character select, faint frame, Soldier sprite, 72% content alpha |
| StatusBar | bottom-center (.5,0) | 0,150 | 640,56 | `Card` pill (BgPanelDeep) + **StatusText** TMP (Small Bold, TextSecondary), Center |
| CancelBtn | bottom-center (.5,0) | 0,40 | 520,86 | `Btn_Primary`, Danger gradient, "CANCEL SEARCH" |

## Wiring
| Target | Assign |
|---|---|
| `UIManager.lobbyPanel` | LobbyPanel |
| `UIManager.lobbyStatusText` | StatusText |
| **CancelBtn** OnClick | `NetworkManager.ReturnToMenu()` (drag the NetworkManager object, pick the method) |

`UIManager.ShowLobby(msg)` activates `lobbyPanel`, hides the others, and sets `lobbyStatusText`. `NetworkManager.ConnectAndPlay()` (called by the menu's Play Online) starts the flow and pushes status updates. When two players are in the room, `NetworkManager` loads `GameArena` automatically.

> The in-match countdown (`MatchmakingTimer`) is a **different** thing (the 3-minute match clock in the HUD) — not used here.

## Verify
From Main Menu tap **Play Online** → LobbyPanel shows, StatusText cycles "Connecting..." → "Finding opponent..." → "Waiting for opponent...". With a 2nd client, both load GameArena. **Cancel** → back to menu, `lobbyPanel` hidden.
