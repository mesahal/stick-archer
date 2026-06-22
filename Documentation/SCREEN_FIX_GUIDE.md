# Stick Archer — Screen-by-Screen Visual Fix Guide

> Each section is a self-contained brief for one screen.
> Hand any section to an AI coding tool and say "implement this fix."
> Design source files are in `designs/*.svg`. All art assets are in `Assets/Art/`.
> The project is Unity 2022.3 LTS, URP, C#, TextMeshPro.

---

## Status

| Fix | Screen | Code | Design SVG | Design Spec |
|-----|--------|------|------------|-------------|
| 01 | Main Menu — Composite BG + Profile Badge | ✅ DONE | ✅ | ✅ |
| 02 | Defeat Screen — Radial Gradient Background | ✅ DONE | ✅ | ✅ |
| 03 | Victory Screen — Glow, Button Color, Label, Stars | ✅ DONE | ✅ | ✅ |
| 04 | Game HUD — Health Bar Gradient Fill | ⬜ TODO | ✅ | ✅ |
| 05 | Round Transition — Color, Duration, Styling | ⬜ TODO | ✅ | ✅ |
| 06 | Pause Menu — Icons, Accent Bar, Subtitle | ⬜ TODO | ✅ | ✅ |
| 07 | Character Select — VS Divider & Stat Bars | ⬜ TODO | ✅ | ✅ |
| 08 | Settings Modal — Section Icons & Aim Assist | ⬜ TODO | ✅ | ✅ |
| 09 | Error Modal — Warning Icon & Auto-Retry Timer | ⬜ TODO | ✅ | ✅ |
| 10 | Lobby — Player Cards & Character Art Sync | ⬜ TODO | ✅ | ✅ |

> Design pass complete (2026-06-08). See [`designs/DESIGN_CONSISTENCY.md`](../designs/DESIGN_CONSISTENCY.md) before Unity implementation.

---

## CRITICAL LESSONS LEARNED — read before touching any editor script

These were discovered during FIX 01. They apply to all editor-script–based fixes.

### 1. SafeAreaFitter must NOT be added via AddComponent in editor scripts

`SafeAreaFitter.Awake()` fires **immediately** when `AddComponent<SafeAreaFitter>()` is called
from an editor (non-Play-mode) script in Unity 2022. It reads `Screen.safeArea`, which in the
editor reflects the host Mac's notch/safe area, and shrinks the container's anchors accordingly.
Every child anchored to the top of the safe container gets displaced off-screen.

**Rule:** never call `safe.gameObject.AddComponent<SafeAreaFitter>()` inside a `[MenuItem]`
editor tool. Add SafeAreaFitter at runtime only (via a prefab or from `Start()`). In the editor
script just use `Stretch(safe)` and leave it at that.

### 2. TMP default font atlas contains only printable ASCII (32–126)

Unicode Geometric Shapes (◄ ► ▲ ▼, U+25A0–U+25FF), em dash (—), and even bullet (•) may NOT
be in the default TMP font atlas. Characters outside the atlas render invisible — no error is
thrown. Do NOT use these characters in TMP text set from editor scripts.

**Rule:** for ornaments/decorators, build them from Unity Image components (WS for lines,
Circle128 for dots). Never rely on TMP for non-ASCII symbols in editor-built UI.

### 3. TitleGlow panel artifact — alpha must be 0 or very close

A `WS` (white-square) Image stretched over 1100×420 pixels even at `alpha = 0.10` (A=26/255)
creates a clearly visible warm brownish rectangle over the title area. The same happens with
`title_gold.png` (it is a narrow vertical bar, not a radial glow).

**Rule:** either use `Circle128` (round edges) with alpha ≤ 0.06, or set alpha = 0.0 entirely.
The current code has it at 0.0; do not raise it without testing.

### 4. Canvas child order = render order

In Screen Space Overlay, a later sibling renders **on top of** an earlier sibling. Main menu order (back → front):
`BG` (menu_bg.png) → `Safe` (ProfileBadge, Title, buttons, …)

### 5. Use menu_bg.png for the background — do not stack layers

The composite `Assets/Art/UI/Backgrounds/menu_bg.png` (1920×1080, from `menu_bg.svg`) matches the design exactly. Stacking `bg_sky_menu` + mountain PNGs + procedural stars produces a visibly different result because the mountain PNGs are not the SVG silhouettes and `title_gold.png` is a 1×64 bar, not a radial glow.

**Rule:** one full-screen `menu_bg.png` Image for BG. ProfileBadge is part of the foreground UI under `Safe`, not the background.

### 6. Anchor math cheatsheet for this canvas (1920×1080 reference)

- `anchorMin = anchorMax = (0.5f, 1f)`, `pivot = (0.5f, 1f)`:
  `anchoredPosition.y = -N` places the element's **top edge** N units below the canvas top.
- `anchorMin = anchorMax = (0.5f, 0.5f)`, `pivot = (0.5f, 0.5f)`:
  `anchoredPosition.y = 0` places the element's **center** at the canvas center (Y=540 from bottom).
- SVG Y coordinates run top-down. Canvas anchoredPosition Y runs bottom-up from the anchor point.
  Convert: `anchoredPosition.y ≈ -(SVG_Y_from_top)` when using a top anchor.

---

## How to read this document

**Current situation** — what the code actually produces today.  
**Goal** — what `designs/*.svg` specifies it should look like.  
**Assets available** — sprites already in the project that must be used.  
**Files to change** — exact file path + method name to edit.  
**Changes needed** — concrete steps, pseudocode, or code snippets.

---

---

## FIX 01 · Main Menu — Composite Background + Profile Badge ✅ DONE

**Design file:** `designs/01_main_menu.svg`, `designs/specs/01_main_menu.md`  
**Build method:** Editor script — `Assets/Editor/VisualOverhaul_v12.cs`, method `PolishMainMenu()`  
**Apply:** `Tools → Design Sync → 4 – Polish MainMenu (v12)` (stop Play mode first)

### What was built

| Element | Implementation |
|---|---|
| **BG** | Single `menu_bg.png` (1920×1080 composite from `menu_bg.svg`) — full-screen stretch |
| **ProfileBadge** | Baked under `Safe`: level, XP bar (`pill_bar`), coins + `coin` icon |
| **Title / Subtitle / Ornament / Buttons / Gear / Footers** | Per `01_main_menu.svg` under `Safe` |
| **Safe** | Stretch-to-fill; **no SafeAreaFitter** in editor tool (see Lesson 1) |

ProfileBadge data refreshes at runtime via `ProfileManager`. `MainMenuController` only creates a fallback badge on old scenes.

**Foreground polish (v12):** title 160pt + drop shadow; ornament chevrons; pill-button drop shadows (SVG `dropSoft` filter).

### Do NOT regress
- Do not revert to stacked `bg_sky_menu` + mountain PNG layers — use `menu_bg.png`.
- Do not add `SafeAreaFitter` inside the editor tool.
- Do not use TMP Unicode for subtitle ornament chevrons (see Lesson 2).

---

---

## FIX 02 · Defeat Screen — Radial Gradient Background ✅ DONE

**Design file:** `designs/09_results_defeat.svg`  
**Build method:** Runtime — `Assets/Scripts/UIManager.cs`, method `BuildRuntimeResultPanel(bool localPlayerWon)`  
**How to apply:** Edit the script; the fix takes effect next time a match ends in defeat.

### What was fixed

Defeat background now **always** uses `GetDefeatResultBackgroundSprite()` (dark radial gradient `#3A1A28` → `#0A060C`). Victory still uses `UIArtProvider.BgSkyMenu` with a sky tint.

### Original brief (for reference)

### Current situation
The defeat screen background tries to use `UIArtProvider.PanelDark` (a flat gradient sprite), but if that returns null the fallback is `Color.white` — making the background bright white. Even when the sprite loads, it is a simple flat-color gradient, not the dark radial gradient the design specifies.

### Goal
A **dark radial gradient background**: deep purple-red at the center fading to near-black at the edges — color `#3A1A28` at center → `#0A060C` at edges. This creates an ominous atmospheric feel for the defeat state.

The existing method `GetDefeatResultBackgroundSprite()` already generates this radial gradient procedurally as a `Texture2D` — it just needs to be wired back into the panel background correctly.

### Files to change
`Assets/Scripts/UIManager.cs` — inside `BuildRuntimeResultPanel(bool localPlayerWon)`, the `Image background` block near the top of the method.

### Current code (approximately lines 294–306)
```csharp
Image background = panel.AddComponent<Image>();
Sprite bgSprite = localPlayerWon ? UIArtProvider.BgSkyMenu : UIArtProvider.PanelDark;
if (bgSprite != null)
{
    background.sprite = bgSprite;
    background.type = Image.Type.Simple;
    background.color = Color.white;
}
else
{
    background.sprite = localPlayerWon ? null : GetDefeatResultBackgroundSprite();
    background.color = localPlayerWon ? Hex("#0A0E1C") : Color.white;
}
background.raycastTarget = true;
```

### Changes needed
Replace the background block so that:
- **Victory** uses `UIArtProvider.BgSkyMenu` sprite (sky blue) tinted dark
- **Defeat** uses the procedural `GetDefeatResultBackgroundSprite()` which already produces the correct dark radial gradient — **always**, not only as a fallback

```csharp
Image background = panel.AddComponent<Image>();
if (localPlayerWon)
{
    Sprite skySprite = UIArtProvider.BgSkyMenu;
    background.sprite = skySprite;
    background.type = Image.Type.Simple;
    background.color = skySprite != null ? new Color(0.6f, 0.7f, 1f, 1f) : Hex("#0A0E1C");
}
else
{
    // Always use the procedural radial gradient — dark purple-red centre fading to near-black
    background.sprite = GetDefeatResultBackgroundSprite();
    background.type = Image.Type.Simple;
    background.color = Color.white; // sprite is full-color, tint to white
}
background.raycastTarget = true;
```

The existing `GetDefeatResultBackgroundSprite()` helper method (already in UIManager.cs) creates a 96×96 radial texture with colors:
- Center `#3A1A28` (dark red-purple)
- Mid `#1A0E1C`
- Edge `#0A060C` (near black)

No changes needed to that helper — it's correct already.

### Verification
Play a practice match and intentionally lose (let the AI kill you 5 times). The result screen should show a dark red-purple radial gradient background, not a white or flat background.

---

---

## FIX 03 · Victory Screen — Glow, Button Color, Label Color, Star Ornaments ✅ DONE

**Design file:** `designs/08_results_victory.svg`  
**Build method:** Runtime — `Assets/Scripts/UIManager.cs`, methods `BuildRuntimeResultPanel()`, `BuildVictoryStars()`, `CreateResultPrimaryButton()`  
**How to apply:** Edit the script; takes effect next time a match ends in victory.

### What was built

| Element | Implementation |
|---|---|
| **Background** | Procedural purple radial `GetVictoryResultBackgroundSprite()` |
| **Spotlight** | Gold radial ellipse `GetVictorySpotlightSprite()` behind title |
| **Confetti** | Runtime `VictoryEffects` + `ConfettiBurst` on win |
| **Title** | Single-line `VICTORY!` — 220pt Inter Black, gold gradient, no-wrap, 1400×240 rect |
| **Ornaments** | 4 gold diamond chevrons at y=140 (`BuildVictoryStars`, fresh create each build) |
| **Score card** | Gold label, gold border, `#2A3258→#161B30` fill, label+score only (no rewards) |
| **REMATCH** | Green success gradient pill + play icon (victory only) |
| **MAIN MENU** | White outline pill + home icon (victory only) |

### Do NOT regress
- Do not re-add `TryBuildRewardsCard` on victory — design has no rewards block on this screen.
- Keep `title.enableWordWrapping = false` or `VICTORY!` will break onto two lines.
- Build ornaments **after** title creation (z-order: above BG, below title text is OK since title renders on top as later sibling).

### Verification
Win a practice match (score to 5). Confirm:
- One-line **VICTORY!** (not split)
- 4 gold diamonds under title
- Score card shows only FINAL SCORE + numbers (no coins/XP block)
- Green REMATCH with play icon, white MAIN MENU with home icon
- No `[UIManager] Victory stars skipped` warning in Console

### Original brief (for reference)
The victory screen has four specific issues compared to the design:
1. **Glow overlay** is blue (`#141F47`) — design specifies a **gold** spotlight glow
2. **REMATCH button** uses blue primary color — design specifies **green** (success/win color)
3. **"FINAL SCORE" label** is white 60% alpha — design specifies **gold** `#FFD933`
4. **Decorative stars** (4 small gold star ornaments below the title) — not created at all

### Goal
- Gold radial glow behind/around the title on victory
- Green REMATCH button (matching `btn_success.png` gradient and `UIDesignSystem.Success` color)
- Gold "FINAL SCORE" label
- 4 small gold star/diamond shapes below the title

### Assets available
```
Assets/Resources/UI/Icons/star.png          ← white star icon (tint gold in code)
Assets/Resources/UI/Gradients/btn_success.png ← green gradient for REMATCH button
UIDesignSystem.Gold     = #FFD933
UIDesignSystem.Success  = #33B859
UIArtProvider.BtnSuccess  ← loads btn_success.png at runtime
UIArtProvider.IconStar    ← loads star.png at runtime
```

### Files to change
`Assets/Scripts/UIManager.cs` — four targeted changes inside `BuildRuntimeResultPanel(bool localPlayerWon)`.

### Changes needed

**Change 1 — Gold glow overlay (victory only)**  
Find the `ResultGlow` image block and change its color for the victory case:
```csharp
// Replace:
glow.color = localPlayerWon ? new Color(0.08f, 0.13f, 0.28f, 0.84f) : Hex("#3A1A28", 0.84f);

// With:
glow.color = localPlayerWon
    ? new Color(1f, 0.85f, 0.1f, 0.18f)   // warm gold glow for victory
    : Hex("#3A1A28", 0.84f);               // dark red tint for defeat (unchanged)
```

**Change 2 — Green REMATCH button**  
Find where `RematchButton` is created with `CreateRuntimeButton(... "REMATCH", true)`. The `true` argument means "primary" (blue). Change it to use the success (green) color for victory. The cleanest approach is to add a `bool isVictory` parameter or just override the button color after creation:

Option A — change the primary color flag so REMATCH is always styled distinctly:
```csharp
// After creating the rematch button, if it's a victory override its color:
if (localPlayerWon)
{
    Image rematchImg = rematch.GetComponent<Image>();
    if (rematchImg != null)
    {
        rematchImg.color = UIDesignSystem.Success;
        // Replace the gradient overlay with the success gradient
        Transform oldGrad = rematch.transform.Find("Gradient");
        if (oldGrad != null) Object.Destroy(oldGrad.gameObject);
        UIArtProvider.AddGradientOverlay(rematch.transform, UIArtProvider.BtnSuccess);
    }
}
```

**Change 3 — Gold "FINAL SCORE" label**  
Find the `label` TextMeshProUGUI inside the score card:
```csharp
// Replace:
label.color = new Color(1f, 1f, 1f, 0.6f);

// With:
label.color = localPlayerWon ? UIDesignSystem.Gold : new Color(1f, 1f, 1f, 0.6f);
```

**Change 4 — Star ornaments below title (victory only)**  
Add this block after the `TryBuildResultDecoration()` call, inside `BuildRuntimeResultPanel`, gated on `localPlayerWon`:

```csharp
if (localPlayerWon)
{
    TryBuildVictoryStars(panel.transform);
}
```

Then add a new method `TryBuildVictoryStars(Transform parent)`:
```csharp
void TryBuildVictoryStars(Transform parent)
{
    try { BuildVictoryStars(parent); }
    catch (System.Exception ex)
    { Debug.LogWarning("[UIManager] Victory stars skipped: " + ex.Message); }
}

void BuildVictoryStars(Transform parent)
{
    // Four small gold stars spaced horizontally below the VICTORY! title
    float[] xPositions = { -195f, -65f, 65f, 195f };
    Sprite starSprite = UIArtProvider.IconStar;

    for (int i = 0; i < xPositions.Length; i++)
    {
        Image star = CreateRuntimeImage(parent, "Star" + i,
            new Vector2(0.5f, 0.5f),
            new Vector2(xPositions[i], 140f),   // just below title area
            new Vector2(36f, 36f),
            UIDesignSystem.Gold, false);
        if (starSprite != null)
        {
            star.sprite = starSprite;
            star.color  = UIDesignSystem.Gold;
        }
    }
}
```

### Verification
Win a practice match (kill the AI 5 times on Easy difficulty). The victory screen should show:
- A warm gold/yellow glow (not blue)
- A green REMATCH button
- The "FINAL SCORE" label in gold
- Four small gold stars between the title and the score card

---

---

## FIX 04 · Game HUD — Health Bar Gradient Fill

**Design file:** `designs/05_game_hud.svg`  
**Build method:** Runtime normalizer — `Assets/Scripts/GameUISetup.cs`, method `NormalizeHealthBars(Transform hudPanel)`  
**How to apply:** Edit the script; takes effect immediately on entering any match.

### Current situation
Health bars have the `pill_bar` shape applied to their container (if found). The fill image inside each bar is colored programmatically using `UIDesignSystem.GetHealthColor(pct)` — a computed color that transitions green → orange → red. This works functionally but the bar looks flat.

The design specifies gradient sprite fills: `hp_full.png` (green gradient) when health is high and `hp_low.png` (red gradient) when health is low, giving the bars visual depth.

### Goal
The health bar fill Image should use `hp_full.png` as its sprite when HP > 60% and `hp_low.png` when HP ≤ 30%. At runtime `UIManager.UpdateBar()` still controls the bar width via `anchorMax.x`, but the sprite provides the gradient texture so the fill has depth rather than flat color.

The color tint from `UIDesignSystem.GetHealthColor()` should still be applied (it will tint the gradient, which is acceptable), OR the color can be set to `Color.white` to show the pure gradient — white is preferred.

### Assets available
```
Assets/Resources/UI/Gradients/hp_full.png   ← green gradient bar fill
Assets/Resources/UI/Gradients/hp_low.png    ← red gradient bar fill
UIArtProvider.HpFull  ← runtime loader
UIArtProvider.HpLow   ← runtime loader
UIArtProvider.PillBar ← shape for bar containers
```

### Files to change
`Assets/Scripts/UIManager.cs` — method `UpdateBar(Image bar, float health, float maxHealth)`

### Current code
```csharp
void UpdateBar(Image bar, float health, float maxHealth)
{
    if (bar == null) return;
    float pct = maxHealth > 0 ? health / maxHealth : 0f;
    bar.rectTransform.anchorMax = new Vector2(pct, 1f);
    bar.color = UIDesignSystem.GetHealthColor(pct);
}
```

### Changes needed
```csharp
void UpdateBar(Image bar, float health, float maxHealth)
{
    if (bar == null) return;
    float pct = maxHealth > 0 ? health / maxHealth : 0f;
    bar.rectTransform.anchorMax = new Vector2(pct, 1f);

    // Apply gradient sprite for visual depth; tint with health color
    Sprite grad = pct > 0.3f ? UIArtProvider.HpFull : UIArtProvider.HpLow;
    if (grad != null)
    {
        bar.sprite = grad;
        bar.type   = Image.Type.Simple;
        bar.color  = Color.white; // let the sprite show its natural gradient
    }
    else
    {
        bar.color = UIDesignSystem.GetHealthColor(pct);
    }
}
```

Note: this changes the fill image directly. If the fill is a child of a container that already has `pill_bar` sliced (applied by `NormalizeHealthBars`), the gradient will show inside the pill shape automatically.

### Verification
Enter any match. Watch the health bars. When above 30% they should show a green gradient bar; when below 30% they should switch to a red gradient bar. The bar width still shrinks correctly as HP decreases.

---

---

## FIX 05 · Round Transition — Color, Duration, Number Styling, Content

**Design file:** `designs/07_round_transition.svg`  
**Build method:** Runtime — `Assets/Scripts/RoundTransition.cs` (created by `GameUISetup.SetupRoundDisplay()`)  
**How to apply:** Edit `RoundTransition.cs`; takes effect immediately in-match.

### Current situation
The round transition has six deviations from the design:
1. **Wipe color** — dark panel color `(0.10, 0.12, 0.18)` instead of gold gradient
2. **Wipe duration** — 0.5s total, design shows ~1.2s
3. **Round number color** — white, no stroke; design shows large gold text with brown stroke
4. **Round number size** — not enforced to 380px from code
5. **Score subtitle** missing — design shows "SCORE 3 — 2" below the round number
6. **"FIRST TO 5 WINS"** text missing — design shows small faded text below score

### Goal
- Diagonal wipe band is **gold** (`#FFD933`) with slight transparency
- Wipe takes **1.2 seconds** to sweep across
- Round number is **large** (300pt), **gold**, with a **dark brown outline** (`#3A2200`)
- Below the round number: `"SCORE X — Y"` (34px, white)
- Below that: `"FIRST TO [N] WINS"` (22px, faded white 0.5 alpha)

### Files to change
`Assets/Scripts/RoundTransition.cs`

### Changes needed

**Find the `DoSwipeWipe()` coroutine** and change:
- The wipe Image color from the dark panel color to gold: `new Color(1f, 0.851f, 0.2f, 0.65f)` (gold at 65% alpha)
- The total wipe duration constant/field: change `wipeDuration` from `0.5f` to `1.2f`

**Find the `ShowRound()` method** (or wherever `roundText` is styled) and apply:
```csharp
roundText.fontSize   = 300f;
roundText.fontStyle  = FontStyles.Bold;
roundText.color      = UIDesignSystem.Gold;       // #FFD933
roundText.outlineColor = new Color(0.227f, 0.133f, 0f); // #3A2200
roundText.outlineWidth = 0.15f;
```

**Add score subtitle text** — create a new TextMeshProUGUI below the round number when `ShowRound()` is called. It reads the current score from `GameManager.Instance` (or from parameters passed to `ShowRound()`):
```csharp
// Add after roundText is configured:
// Create a score display: "SCORE 3 — 2"
GameObject scoreGO = new GameObject("ScoreDisplay");
scoreGO.transform.SetParent(roundObj.transform, false);
TextMeshProUGUI scoreText = scoreGO.AddComponent<TextMeshProUGUI>();
// Position below round number
RectTransform scoreRT = scoreGO.GetComponent<RectTransform>();
scoreRT.anchorMin = new Vector2(0f, 0f);
scoreRT.anchorMax = new Vector2(1f, 0.42f);
scoreRT.offsetMin = scoreRT.offsetMax = Vector2.zero;
scoreText.fontSize  = 34f;
scoreText.fontStyle = FontStyles.Bold;
scoreText.alignment = TextAlignmentOptions.Center;
scoreText.color     = new Color(1f, 1f, 1f, 0.85f);
// Populate from GameManager if available:
if (GameManager.Instance != null)
    scoreText.text = $"SCORE  {GameManager.Instance.player1Score}  —  {GameManager.Instance.player2Score}";
```

**Add "FIRST TO N WINS" text** similarly, positioned below the score:
```csharp
GameObject hintGO = new GameObject("FirstToWins");
hintGO.transform.SetParent(roundObj.transform, false);
TextMeshProUGUI hintText = hintGO.AddComponent<TextMeshProUGUI>();
RectTransform hintRT = hintGO.GetComponent<RectTransform>();
hintRT.anchorMin = new Vector2(0f, 0f);
hintRT.anchorMax = new Vector2(1f, 0.25f);
hintRT.offsetMin = hintRT.offsetMax = Vector2.zero;
hintText.fontSize  = 22f;
hintText.alignment = TextAlignmentOptions.Center;
hintText.color     = new Color(1f, 1f, 1f, 0.5f);
int scoreToWin = GameManager.Instance != null ? GameManager.Instance.scoreToWin : 5;
hintText.text = $"FIRST TO {scoreToWin} WINS";
```

Note: `GameManager.Instance.player1Score`, `player2Score`, and `scoreToWin` must be public fields — check `GameManager.cs` and add `public` visibility if needed.

### Verification
Play a match. After a kill, the round transition overlay should sweep across in gold (not dark blue), take about 1.2 seconds, show the round number in large gold text, and display the current score and win target below it.

---

---

## FIX 06 · Pause Menu — Icons on Buttons, Gold Accent Bar, Subtitle

**Design file:** `designs/06_pause_menu.svg`  
**Build method:** Scene-baked + `Assets/Scripts/UI/PauseMenuUI.cs`  
**How to apply:** Either (a) edit `PauseMenuUI.cs` to add elements at runtime when the pause menu opens, OR (b) rebuild the scene panel manually in the Unity Editor.

### Current situation
The pause menu panel, dim overlay, and three buttons exist and are functional. However:
- No icons on any of the three buttons (Resume, Settings, Quit)
- No "PAUSED" title text (must be manually placed in scene)
- No gold accent strip at the top of the modal
- Status text at the bottom doesn't include the round number

### Goal
- **Gold accent bar** — 6px tall, full-width strip at the very top of the modal panel, gold gradient
- **"PAUSED" title** — 56px bold white, centered at top of modal
- **Subtitle** — "Take a breath, archer" 20px, faded white (0.55 alpha), below title
- **Button icons** — Resume: `play.png`, Settings: `gear.png`, Quit: `close.png` — white, 36×36, left of label
- **Status text** — include round: `"Round 3 / 5  ·  Score 3 — 2  ·  Tap Resume to continue"`

### Assets available
```
Assets/Resources/UI/Icons/play.png    ← resume icon
Assets/Resources/UI/Icons/gear.png   ← settings icon
Assets/Resources/UI/Icons/close.png  ← quit icon
Assets/Resources/UI/Gradients/btn_gold.png ← gold gradient for accent bar
UIArtProvider.IconPlay, IconGear, IconClose, BtnGold
```

### Files to change
`Assets/Scripts/UI/PauseMenuUI.cs` — add a `BuildDecoration()` method called from `Show()` that adds missing elements programmatically (so they survive scene reloads and don't require manual Editor work).

### Changes needed

Add at the end of `Show()`:
```csharp
BuildDecoration();
```

Add new method:
```csharp
void BuildDecoration()
{
    if (modal == null) return; // modal is the RectTransform of the pause panel

    // Gold accent bar — 6px at top of modal
    if (modal.Find("GoldAccent") == null)
    {
        GameObject accentGO = new GameObject("GoldAccent");
        accentGO.transform.SetParent(modal, false);
        Image accent = accentGO.AddComponent<Image>();
        RectTransform rt = accent.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 6f);
        Sprite gold = UIArtProvider.BtnGold;
        accent.sprite = gold;
        accent.color  = gold != null ? Color.white : UIDesignSystem.Gold;
        accent.raycastTarget = false;
    }

    // Add icon to Resume button
    AddButtonIcon(resumeButton, UIArtProvider.IconPlay);
    // Add icon to Settings button
    AddButtonIcon(settingsButton, UIArtProvider.IconGear);
    // Add icon to Quit button
    AddButtonIcon(quitButton, UIArtProvider.IconClose);

    // Update status text to include round info
    UpdateStatusText();
}

void AddButtonIcon(Button btn, Sprite icon)
{
    if (btn == null || icon == null) return;
    if (btn.transform.Find("Icon") != null) return; // already added

    GameObject iconGO = new GameObject("Icon");
    iconGO.transform.SetParent(btn.transform, false);
    Image img = iconGO.AddComponent<Image>();
    img.sprite = icon;
    img.color  = Color.white;
    img.raycastTarget = false;
    RectTransform rt = img.rectTransform;
    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
    rt.pivot     = new Vector2(0f, 0.5f);
    rt.anchoredPosition = new Vector2(24f, 0f);
    rt.sizeDelta = new Vector2(36f, 36f);
}
```

For the status text update, find the existing `UpdateStatusText()` method and change it to include round info:
```csharp
void UpdateStatusText()
{
    if (statusText == null) return;
    string score = "";
    if (GameManager.Instance != null)
    {
        int p1 = GameManager.Instance.player1Score;
        int p2 = GameManager.Instance.player2Score;
        int toWin = GameManager.Instance.scoreToWin;
        score = $"Score {p1} — {p2}  ·  First to {toWin}  ·  Tap Resume to continue";
    }
    else
    {
        score = "Tap Resume to continue";
    }
    statusText.text = score;
}
```

### Verification
During a match, press the pause button. The pause modal should show a thin gold line at the top, and each button should have a small icon on its left side. The status text at the bottom should show the current score.

---

---

## FIX 07 · Character Select — VS Divider & Stat Bars

**Design file:** `designs/02_character_select.svg`  
**Build method:** Runtime — `Assets/Scripts/UI/CharacterSelectUI.cs`  
**How to apply:** Edit the script; the character select panel is built from the scene but this adds runtime elements.

### Current situation
Character cards exist with selection borders and character art. However:
- The **VS circle** divider between the two cards is completely absent
- **Stat bars** (Speed / Power) are hidden via `HideCardDetails()` which disables both cards' stat elements on Awake
- Card **border gradient** is a solid gold color instead of a gradient

### Goal
- A centered circle (diameter 120px) between the two cards showing bold gold "VS" text
- Stats visible on the **selected** card (hidden on unselected) — show Speed and Power bars with fill percentages
- Character stats: Adventurer = Speed 9/10, Power 6/10 · Soldier = Speed 5/10, Power 9/10

### Assets available
```
Assets/Resources/UI/Shapes/circle_128.png  ← circle shape for VS badge
UIArtProvider.Circle128
UIDesignSystem.Gold, UIDesignSystem.BgDark
```

### Files to change
`Assets/Scripts/UI/CharacterSelectUI.cs`

### Changes needed

**Step 1 — Remove the stat-hiding behavior.** Delete or comment out the `HideCardDetails()` calls in `ApplyCurrentCharacterArt()`. Stats should be visible; the selected card shows them at full opacity while the unselected card shows them at 85% opacity (controlled by `CanvasGroup.alpha` which already exists).

**Step 2 — Build VS circle** at runtime in `Start()`:
```csharp
void BuildVsDivider()
{
    // Find the character select panel's RectTransform to parent the VS circle
    if (characterSelectPanel == null) return;
    RectTransform panelRT = characterSelectPanel.GetComponent<RectTransform>();
    if (panelRT == null) return;

    if (panelRT.Find("VsCircle") != null) return; // already built

    GameObject vsGO = new GameObject("VsCircle");
    vsGO.transform.SetParent(panelRT, false);

    Image circle = vsGO.AddComponent<Image>();
    circle.sprite = UIArtProvider.Circle128;
    circle.color  = UIDesignSystem.BgDark;
    circle.type   = Image.Type.Simple;
    circle.raycastTarget = false;

    RectTransform rt = circle.rectTransform;
    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.pivot     = new Vector2(0.5f, 0.5f);
    rt.anchoredPosition = Vector2.zero;
    rt.sizeDelta = new Vector2(120f, 120f);

    // "VS" text label
    GameObject textGO = new GameObject("VsText");
    textGO.transform.SetParent(vsGO.transform, false);
    TextMeshProUGUI vs = textGO.AddComponent<TextMeshProUGUI>();
    vs.text      = "VS";
    vs.fontSize  = 56f;
    vs.fontStyle = FontStyles.Bold;
    vs.alignment = TextAlignmentOptions.Center;
    vs.color     = UIDesignSystem.Gold;
    vs.raycastTarget = false;
    RectTransform textRT = vs.rectTransform;
    textRT.anchorMin = Vector2.zero;
    textRT.anchorMax = Vector2.one;
    textRT.offsetMin = textRT.offsetMax = Vector2.zero;
}
```

Call `BuildVsDivider()` from `Start()`.

**Step 3 — Show stats on selected card, hide on deselected** — modify `UpdateVisuals()`:
```csharp
// Add after opacity control:
ShowCardStats(adventurerCard, isAdventurer);
ShowCardStats(soldierCard,    !isAdventurer);

// Stat data: index 0 = Adventurer, index 1 = Soldier
static readonly float[] SpeedStat = { 0.9f, 0.5f };
static readonly float[] PowerStat = { 0.6f, 0.9f };

void ShowCardStats(Button card, bool show)
{
    if (card == null) return;
    int charIndex = card == adventurerCard ? 0 : 1;

    // Find or create stat bar roots
    SetStatBar(card.transform, "StatSpeed", "SPD", SpeedStat[charIndex], show);
    SetStatBar(card.transform, "StatPower", "PWR", PowerStat[charIndex], show);
}

void SetStatBar(Transform card, string statName, string label, float fill, bool visible)
{
    Transform stat = card.Find("Content/" + statName);
    if (stat == null) return;
    stat.gameObject.SetActive(visible);

    // Set fill amount if there's an Image fill child
    Transform fillT = stat.Find("Fill") ?? stat.Find("BarFill");
    if (fillT != null)
    {
        Image fillImg = fillT.GetComponent<Image>();
        if (fillImg != null)
            fillImg.rectTransform.anchorMax = new Vector2(fill, 1f);
    }
}
```

### Verification
Open character select. You should see a gold "VS" circle in the center. The selected character's card shows stat bars (Speed/Power). Clicking the other card transfers the stat display.

---

---

## FIX 08 · Settings Modal — Section Icons & Aim Assist Toggle

**Design file:** `designs/03_settings_modal.svg`  
**Build method:** Scene-baked + `Assets/Scripts/UI/SettingsPanel.cs` (or `Assets/Scripts/SettingsPanel.cs`)  
**How to apply:** Modify `SettingsPanel.cs` to add decorations at runtime in `Start()`.

### Current situation
The settings modal has functional sliders and the mute toggle. Missing:
- Section header icons (yellow speaker box for AUDIO, green radio box for CONTROLS)
- Aim Assist toggle (no reference or wiring in code)
- Reset progress link (not implemented)

### Goal
- **AUDIO section header**: small yellow icon box (32×32, rounded, `#FFD933` bg) with speaker icon + bold "AUDIO" label
- **CONTROLS section header**: small green icon box (32×32, rounded, `#33B859` bg) with an icon + bold "CONTROLS" label  
- **Aim Assist toggle**: same custom toggle component as the mute toggle, wired to a PlayerPrefs key `"AimAssist"` (default: 1 = on)
- **Reset Progress**: a small red underlined text link at the bottom; tapping it opens a confirmation and then calls `ProfileManager.Instance?.ResetProfile()` if confirmed

### Assets available
```
Assets/Resources/UI/Icons/sound.png   ← speaker icon for AUDIO
Assets/Resources/UI/Icons/target.png  ← target icon for CONTROLS  
Assets/Resources/UI/Shapes/rounded_16.png ← small rounded box background
UIArtProvider.IconSound, IconTarget, Rounded16
UIDesignSystem.Gold (#FFD933), UIDesignSystem.Success (#33B859), UIDesignSystem.Danger (#F23F3F)
```

### Files to change
`Assets/Scripts/UI/SettingsPanel.cs` (or `SettingsPanel.cs`) — add runtime decoration in `Start()` or `OnEnable()`.

### Changes needed

**Add section icon method:**
```csharp
void AddSectionIcon(Transform parent, string sectionName, string labelText,
                    Sprite icon, Color bgColor)
{
    if (parent == null) return;
    Transform header = parent.Find(sectionName + "Header");
    if (header != null) return; // already added

    GameObject headerGO = new GameObject(sectionName + "Header");
    headerGO.transform.SetParent(parent, false);
    headerGO.transform.SetAsFirstSibling(); // or set precise sibling index

    // Icon box
    Image box = headerGO.AddComponent<Image>();
    box.sprite = UIArtProvider.Rounded16;
    box.type   = Image.Type.Sliced;
    box.color  = bgColor;
    box.raycastTarget = false;
    RectTransform boxRT = box.rectTransform;
    boxRT.anchorMin = boxRT.anchorMax = new Vector2(0f, 0.5f);
    boxRT.pivot = new Vector2(0f, 0.5f);
    boxRT.sizeDelta = new Vector2(32f, 32f);
    boxRT.anchoredPosition = Vector2.zero;

    // Icon inside box
    if (icon != null)
    {
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(headerGO.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = icon;
        iconImg.color  = Color.white;
        iconImg.raycastTarget = false;
        RectTransform iconRT = iconImg.rectTransform;
        iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(4f, 4f);
        iconRT.offsetMax = new Vector2(-4f, -4f);
    }

    // Label
    GameObject labelGO = new GameObject("Label");
    labelGO.transform.SetParent(headerGO.transform, false);
    TextMeshProUGUI lbl = labelGO.AddComponent<TextMeshProUGUI>();
    lbl.text = labelText;
    lbl.fontSize = 26f;
    lbl.fontStyle = FontStyles.Bold;
    lbl.color = Color.white;
    lbl.raycastTarget = false;
    RectTransform lblRT = lbl.rectTransform;
    lblRT.anchorMin = lblRT.anchorMax = new Vector2(0f, 0.5f);
    lblRT.pivot = new Vector2(0f, 0.5f);
    lblRT.anchoredPosition = new Vector2(40f, 0f);
    lblRT.sizeDelta = new Vector2(200f, 32f);
}
```

Call from `Start()`:
```csharp
// Find the settings panel content root (adjust name as needed)
Transform content = transform.Find("Modal/Content") ?? transform.Find("Content");
if (content != null)
{
    AddSectionIcon(content.Find("AudioSection"), "Audio",
        "AUDIO", UIArtProvider.IconSound, UIDesignSystem.Gold);
    AddSectionIcon(content.Find("ControlsSection"), "Controls",
        "CONTROLS", UIArtProvider.IconTarget, UIDesignSystem.Success);
}
```

**Aim Assist toggle:** Find the existing mute toggle in the scene hierarchy, duplicate its setup pattern, and wire to `PlayerPrefs.GetInt("AimAssist", 1)` / `PlayerPrefs.SetInt("AimAssist", value)`.

**Reset Progress link:** Add a TextMeshProUGUI with a red underline and a `Button` component at the bottom of the modal. On click, show a simple confirmation text ("Tap again to confirm") and on second tap call `ProfileManager.Instance?.ResetProfile()` then reload the scene.

### Verification
Open settings during a match or from the main menu. The modal should show "AUDIO" with a yellow icon and "CONTROLS" with a green icon as section headers.

---

---

## FIX 09 · Error Modal — Warning Icon & Auto-Retry Timer

**Design file:** `designs/10_error_modal.svg`  
**Build method:** Scene-baked + `Assets/Scripts/NetworkErrorUI.cs` (or similar)  
**How to apply:** Modify the NetworkErrorUI script to add the warning icon and timer at runtime.

### Current situation
The error modal shows title text ("CONNECTION LOST") and has functional Retry/Main Menu buttons. Missing:
- Warning icon (triangle-in-circle with red glow)
- Auto-retry countdown (design shows "Auto-retry in 5s")
- Red top border stripe on the modal
- Icons on Retry and Main Menu buttons

### Goal
- Large **warning icon** — `warning.png` tinted red, with a soft red radial glow behind it
- **Auto-retry countdown**: a coroutine counts from 5 to 0 and then fires the retry action automatically; UI shows "Auto-retry in Xs · Tap RETRY to reconnect now"
- **Red top border** — 6px stripe at top of modal, `#F23F3F`
- **Icons on buttons** — `retry.png` on Retry button, `back.png` or `home.png` on Main Menu button

### Assets available
```
Assets/Resources/UI/Icons/warning.png  ← white warning triangle (tint red)
Assets/Resources/UI/Icons/retry.png   ← retry icon
Assets/Resources/UI/Icons/home.png    ← home/menu icon
Assets/Resources/UI/Shapes/circle_128.png ← for icon background circle
UIArtProvider.IconWarning (loads warning.png), IconRetry, IconHome, Circle128
UIDesignSystem.Danger = #F23F3F
```

### Files to change
The NetworkErrorUI script (find the exact filename by searching for `ShowConnectionError` in `Assets/Scripts/`).

### Changes needed

**Add BuildWarningIcon(Transform modal):**
```csharp
void BuildWarningIcon(Transform modal)
{
    if (modal.Find("WarningIcon") != null) return;

    // Soft red glow behind icon
    GameObject glowGO = new GameObject("WarningGlow");
    glowGO.transform.SetParent(modal, false);
    Image glow = glowGO.AddComponent<Image>();
    glow.color = new Color(0.95f, 0.25f, 0.25f, 0.18f);
    glow.raycastTarget = false;
    RectTransform glowRT = glow.rectTransform;
    glowRT.anchorMin = glowRT.anchorMax = new Vector2(0.5f, 1f);
    glowRT.pivot = new Vector2(0.5f, 1f);
    glowRT.anchoredPosition = new Vector2(0f, -60f);
    glowRT.sizeDelta = new Vector2(240f, 200f);

    // Warning icon circle
    GameObject iconGO = new GameObject("WarningIcon");
    iconGO.transform.SetParent(modal, false);
    Image circle = iconGO.AddComponent<Image>();
    circle.sprite = UIArtProvider.Circle128;
    circle.color  = new Color(0.95f, 0.25f, 0.25f, 0.15f);
    circle.raycastTarget = false;
    RectTransform iconRT = circle.rectTransform;
    iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 1f);
    iconRT.pivot = new Vector2(0.5f, 1f);
    iconRT.anchoredPosition = new Vector2(0f, -80f);
    iconRT.sizeDelta = new Vector2(128f, 128f);

    // Warning sprite on top
    GameObject warnGO = new GameObject("WarnSprite");
    warnGO.transform.SetParent(iconGO.transform, false);
    Image warn = warnGO.AddComponent<Image>();
    warn.sprite = UIArtProvider.IconWarning; // white warning, tinted red
    warn.color  = UIDesignSystem.Danger;
    warn.raycastTarget = false;
    RectTransform warnRT = warn.rectTransform;
    warnRT.anchorMin = Vector2.zero; warnRT.anchorMax = Vector2.one;
    warnRT.offsetMin = new Vector2(16f, 16f);
    warnRT.offsetMax = new Vector2(-16f, -16f);
}
```

**Add auto-retry coroutine:**
```csharp
IEnumerator AutoRetryCountdown(int seconds, System.Action onRetry)
{
    for (int i = seconds; i > 0; i--)
    {
        if (retryButton != null)
        {
            TextMeshProUGUI hintText = GetOrCreateHintText();
            if (hintText != null)
                hintText.text = $"Auto-retry in {i}s  ·  Tap RETRY to reconnect now";
        }
        yield return new WaitForSecondsRealtime(1f);
    }
    onRetry?.Invoke();
}

TextMeshProUGUI GetOrCreateHintText()
{
    // find or create a small hint TMP at the bottom of the modal
    // implementation depends on modal hierarchy
    return null; // replace with actual reference
}
```

Call from `ShowConnectionError()`:
```csharp
BuildWarningIcon(modal.transform);
StartCoroutine(AutoRetryCountdown(5, () => OnRetryPressed()));
```

### Verification
Disconnect network while in a match lobby (or simulate by testing in the editor with Photon disconnected). The error modal should show a large red warning icon, and the countdown text should tick down from 5 to 0 before auto-retrying.

---

---

## FIX 10 · Lobby — Player Cards & Character Art Sync

**Design file:** `designs/04_lobby.svg`  
**Build method:** Scene-baked panel + `UIManager.ShowLobby()` / Photon callbacks  
**How to apply:** Create a new `LobbyUI.cs` script (or extend `NetworkManager.cs`) to build the lobby card UI at runtime.

### Current situation
The lobby panel shows only a text field ("Connecting...", "Finding opponent..."). The design specifies a full two-card layout showing YOUR character on the left and the opponent's character placeholder on the right, with a VS divider, character art, name, stats, and a Cancel button.

There is also **no link** between the character the player selected in `CharacterSelectUI` and anything shown in the lobby screen.

### Goal
- **Your card** (left, 660×700): shows selected character art + name + Speed/Power stat bars + gold border + gold check badge
- **Opponent card** (right): shows "???" / silhouette, faded, no stats until opponent connects
- **VS circle** — centered between cards, dark bg, gold "VS"
- **Cancel Search button** — red pill at bottom, "CANCEL SEARCH", calls `NetworkManager.Instance?.Disconnect()` or `ReturnToMenu()`
- **Character art in lobby reads from** `CharacterSelectUI.SelectedCharacter` (PlayerPrefs `"SelectedCharacter"`, 0 = Adventurer, 1 = Soldier)
- **Character resources:**
  - Adventurer: `Resources.Load<Sprite>("Characters/Player1/archer_idle")`
  - Soldier: `Resources.Load<Sprite>("Characters/Player2/archer_idle")`

### Files to change / create
Create `Assets/Scripts/UI/LobbyUI.cs` and attach it to the LobbyPanel GameObject in the scene (or add to the MainMenuCanvas object and call from `UIManager.ShowLobby()`).

### Changes needed

Create `LobbyUI.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RectTransform lobbyPanel;   // the lobby panel RectTransform
    public TextMeshProUGUI statusText; // existing status text

    bool built = false;

    public void BuildCards()
    {
        if (built || lobbyPanel == null) return;
        built = true;

        int myChar = CharacterSelectUI.SelectedCharacter; // 0 or 1

        BuildPlayerCard(lobbyPanel, myChar, isMe: true,  xOffset: -370f);
        BuildPlayerCard(lobbyPanel, -1,    isMe: false, xOffset:  370f);
        BuildVsDivider(lobbyPanel);
        BuildCancelButton(lobbyPanel);
    }

    void BuildPlayerCard(RectTransform parent, int charIndex, bool isMe, float xOffset)
    {
        GameObject card = new GameObject(isMe ? "YourCard" : "OpponentCard");
        card.transform.SetParent(parent, false);

        Image bg = card.AddComponent<Image>();
        bg.sprite = UIArtProvider.Rounded32;
        bg.type   = Image.Type.Sliced;
        bg.color  = isMe
            ? new Color(0.122f, 0.141f, 0.220f, 0.96f)
            : new Color(0.08f,  0.10f,  0.16f,  0.85f);
        bg.raycastTarget = false;

        RectTransform rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xOffset, 0f);
        rt.sizeDelta = new Vector2(560f, 600f);

        // Gold border on your card
        if (isMe)
        {
            Image border = CreateChild<Image>(card.transform, "Border");
            border.sprite = UIArtProvider.Rounded32;
            border.type   = Image.Type.Sliced;
            border.color  = new Color(1f, 0.851f, 0.2f, 0.6f); // gold
            RectTransform brt = border.rectTransform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-3f, -3f);
            brt.offsetMax = new Vector2(3f, 3f);
        }

        // Character art
        string[] artPaths = { "Characters/Player1/archer_idle", "Characters/Player2/archer_idle" };
        Sprite art = charIndex >= 0 ? Resources.Load<Sprite>(artPaths[charIndex]) : null;

        Image artImg = CreateChild<Image>(card.transform, "CharArt");
        artImg.sprite = art;
        artImg.color  = art != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
        artImg.preserveAspect = true;
        RectTransform artRT = artImg.rectTransform;
        artRT.anchorMin = new Vector2(0.1f, 0.2f);
        artRT.anchorMax = new Vector2(0.9f, 0.95f);
        artRT.offsetMin = artRT.offsetMax = Vector2.zero;

        // Name label
        string[] names = { "ADVENTURER", "SOLDIER" };
        TextMeshProUGUI nameText = CreateChild<TextMeshProUGUI>(card.transform, "Name");
        nameText.text      = isMe ? (charIndex >= 0 ? names[charIndex] : "???") : "???";
        nameText.fontSize  = 28f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color     = isMe ? UIDesignSystem.Gold : new Color(1f, 1f, 1f, 0.45f);
        nameText.raycastTarget = false;
        RectTransform nameRT = nameText.rectTransform;
        nameRT.anchorMin = new Vector2(0f, 0f);
        nameRT.anchorMax = new Vector2(1f, 0.18f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        // "YOU" / "OPPONENT" sub-label
        TextMeshProUGUI subLabel = CreateChild<TextMeshProUGUI>(card.transform, "SubLabel");
        subLabel.text      = isMe ? "YOU" : "WAITING...";
        subLabel.fontSize  = 18f;
        subLabel.alignment = TextAlignmentOptions.Center;
        subLabel.color     = new Color(1f, 1f, 1f, isMe ? 0.55f : 0.3f);
        subLabel.characterSpacing = 4f;
        subLabel.raycastTarget = false;
        RectTransform subRT = subLabel.rectTransform;
        subRT.anchorMin = new Vector2(0f, 0f);
        subRT.anchorMax = new Vector2(1f, 0.1f);
        subRT.offsetMin = subRT.offsetMax = Vector2.zero;
    }

    void BuildVsDivider(RectTransform parent)
    {
        GameObject vsGO = new GameObject("VsCircle");
        vsGO.transform.SetParent(parent, false);
        Image circle = vsGO.AddComponent<Image>();
        circle.sprite = UIArtProvider.Circle128;
        circle.color  = new Color(0.06f, 0.09f, 0.15f, 1f);
        circle.raycastTarget = false;
        RectTransform rt = circle.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(120f, 120f);

        TextMeshProUGUI vs = CreateChild<TextMeshProUGUI>(vsGO.transform, "VsText");
        vs.text = "VS";
        vs.fontSize = 56f;
        vs.fontStyle = FontStyles.Bold;
        vs.alignment = TextAlignmentOptions.Center;
        vs.color = UIDesignSystem.Gold;
        vs.raycastTarget = false;
        RectTransform vsRT = vs.rectTransform;
        vsRT.anchorMin = Vector2.zero; vsRT.anchorMax = Vector2.one;
        vsRT.offsetMin = vsRT.offsetMax = Vector2.zero;
    }

    void BuildCancelButton(RectTransform parent)
    {
        GameObject btnGO = new GameObject("CancelButton");
        btnGO.transform.SetParent(parent, false);
        Image bg = btnGO.AddComponent<Image>();
        bg.sprite = UIArtProvider.Pill128;
        bg.type   = Image.Type.Sliced;
        bg.color  = new Color(0.95f, 0.25f, 0.25f, 0.85f);
        bg.raycastTarget = true;
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 60f);
        rt.sizeDelta = new Vector2(520f, 86f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => NetworkManager.Instance?.ReturnToMenu());

        TextMeshProUGUI lbl = CreateChild<TextMeshProUGUI>(btnGO.transform, "Label");
        lbl.text = "CANCEL SEARCH";
        lbl.fontSize = 28f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;
        lbl.raycastTarget = false;
        RectTransform lblRT = lbl.rectTransform;
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
    }

    T CreateChild<T>(Transform parent, string name) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go.AddComponent<T>();
    }
}
```

**Wire it:** In `UIManager.ShowLobby()`, after setting the panel active, find or get `LobbyUI` and call `BuildCards()`:
```csharp
public void ShowLobby(string statusMessage)
{
    SetPanel(lobbyPanel);
    if (lobbyStatusText != null) lobbyStatusText.text = statusMessage;

    LobbyUI lobbyUI = lobbyPanel?.GetComponent<LobbyUI>()
                   ?? lobbyPanel?.gameObject.AddComponent<LobbyUI>();
    if (lobbyUI != null)
    {
        lobbyUI.lobbyPanel = lobbyPanel?.GetComponent<RectTransform>();
        lobbyUI.BuildCards();
    }
}
```

### Verification
Click PLAY ONLINE. The lobby screen should show your selected character on the left with a gold border and "YOU" label, a "???" opponent card on the right, a VS circle in the center, and a red CANCEL SEARCH button at the bottom. The existing "Connecting..." / "Finding opponent..." status text should still update normally.

---

*End of Screen Fix Guide — 10 screens documented.*
