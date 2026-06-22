# Cricket League (Miniclip) — Feature Reference & Stick Archer Adoption Guide

> **Purpose:** Study Miniclip's *Cricket League* to decide which commercial/meta features
> to bring into Stick Archer. This is a **reference document**, not a clone spec — archery
> mechanics stay; the meta shell is what we borrow from.
>
> **Sources:** App Store / Google Play listings, Miniclip-published descriptions, IGN &
> GamingOnPhone guides (2024–2025). Features evolve with seasons; verify in-game before
> copying exact numbers.
>
> **Last updated:** 2026-06-08

**Related:** [PROJECT_DOCUMENTATION.md](PROJECT_DOCUMENTATION.md) · [ARCHITECTURE.md](ARCHITECTURE.md) · [Documentation index](README.md)

---

## 1. What Cricket League is

| Attribute | Detail |
|-----------|--------|
| **Developer / publisher** | Miniclip |
| **Genre** | Real-time **1v1** multiplayer sports (cricket) |
| **Session length** | ~3–5 minutes per match (2-over format) |
| **Platforms** | iOS, Android (100M+ downloads cited in press) |
| **Core fantasy** | Quick duel → earn currency → upgrade roster → climb leagues |
| **Stick Archer parallel** | Same **1v1 duel scale** and **mobile F2P meta** pattern |

Cricket League succeeds because the **skill moment is simple** (timing bat/bowl) but the **retention layer is deep** (collection, upgrades, tours, seasons, ranked).

---

## 2. Main navigation (shell structure)

Bottom tab bar — five primary destinations:

| Tab | Purpose |
|-----|---------|
| **Home** | Play button, seasonal banners, quick events (Spin & Win, Score Smash, Challenges) |
| **Friends** | Social graph, gifts, account link for cloud save + gem bonus |
| **Team** | Roster of collectible player cards, upgrades, mastery |
| **Shop** | IAP, daily deals, card packs, equipment (bats, balls), currencies |
| **Leagues** | Ranked standings: League / Country / World tiers |

**Home hub promos** (rotating): Koukaburra Season, Free Coins, Score Smash, Challenges, Spin & Win, Offers.

### Stick Archer equivalent (proposed)

| Cricket League | Stick Archer today | Proposed |
|----------------|-------------------|----------|
| Home | Main Menu (Play, Practice, badge) | Add event carousel + primary CTA |
| Friends | — | P4 — friends list, gifts |
| Team | Character select (2 chars) | Expand to roster + upgrades |
| Shop | API only (`ProfileManager`) | P3/P4 — shop UI |
| Leagues | — | P4 — ranked leaderboard |

---

## 3. Core gameplay features

### 3.1 Match format

| Feature | Cricket League | Stick Archer today |
|---------|----------------|-------------------|
| Format | 2 overs (bat + bowl phases) | First to 5 kills |
| Duration | 3–5 min | ~2–4 min (varies) |
| Roles | Both players bat **and** bowl | Both players shoot only |
| Tutorial | 3 practice balls on first launch | — (FTUE not built) |
| Difficulty ramp | 1-over matches early → 2-over unlocked by level | Practice AI difficulties ✅ |
| Real-time PvP | ✅ worldwide matchmaking | ✅ Photon random room |
| Offline / AI | Effectively online-first | ✅ Practice vs AI |
| Low-bandwidth polish | Claims 2G/3G smooth play | Not optimized yet (P0/P5) |

### 3.2 Skill & variety (in-match)

| Feature | Cricket League | Stick Archer parallel |
|---------|----------------|----------------------|
| Simple controls | Tap/hold timing | Tap/hold charge + auto sway ✅ |
| Special deliveries | Doosra, sling, in/out swing | Wind + gravity per round ✅ |
| Equipment affecting play | Ball types change odds | **Not yet** — arrow types / bows (future) |
| Multiple stadiums | 8 tour cities (visual + unlock) | 6 arena layouts ✅ (could add themed skins) |
| Character roster | 25+ collectible players | 2 characters ✅ |

**Adoption idea:** Treat **arrow types** or **bow perks** like cricket **ball types** — meta upgrade that slightly changes physics (not raw damage pay-to-win).

---

## 4. Progression & economy

### 4.1 Currencies

| Currency | Earned from | Spent on |
|----------|-------------|----------|
| **Coins** | Match wins, hourly login, ads, packs | Player upgrades, match entry fee (some modes), shop |
| **Gems** | Achievements, IAP, friends link, packs | Daily deals, premium packs, speed-ups |

**Stick Archer today:** Coins + XP + player level only (`PlayerProfile`, `ProfileManager`). **No gems**, no match entry fee.

### 4.2 Player / character progression

| Feature | Cricket League | Stick Archer |
|---------|----------------|--------------|
| Collectible roster | 25+ characters (Common/Rare/Epic) | 2 characters |
| Card upgrades | Spend coins to level cards → better stats | Level exists globally, not per-character |
| Mastery (2025) | Per-player challenge tracks → coins, gems, gear | — |
| Team building | Pick squad for matches | Pre-match character select ✅ |
| Unlock gating | Tours locked by **account level** | Character unlock API exists, no shop UI |

### 4.3 Tours / locations (content gating)

Eight iconic cities: Mumbai (free start), Karachi, Adelaide, Dubai, Johannesburg, Dhaka, Melbourne, London.

- Unlocked by **reaching account levels**
- Higher tours = **more coin rewards**
- Visual stadium change per tour

**Stick Archer parallel:** Unlock **arena themes** or **background sets** by level — same retention hook, fits `ArenaGenerator` + `ArenaBackground`.

### 4.4 Post-match rewards

| Reward type | Cricket League | Stick Archer |
|-------------|----------------|--------------|
| Base coins for playing | ✅ | ✅ `GrantMatchRewards` |
| Win bonus | ✅ | ✅ |
| Performance packs | Basic / Deluxe / Elite player packs (timed unlock) | — |
| XP / level | ✅ | ✅ |

**Adoption priority:** **Timed loot packs** after good performances — strong retention, medium build effort (P4).

---

## 5. Retention systems (daily / hourly)

| System | How it works | Stick Archer status |
|--------|--------------|---------------------|
| **Hourly free coins** | Login every hour → 100 coins | ⬜ |
| **12-hour mega reward** | 600 coins + 5 gems + Basic Pack (often ad-gated) | ⬜ |
| **Daily Deals** | Rotating gem-priced offers in shop | ⬜ |
| **Daily login streak** | Escalating rewards | ⬜ |
| **Challenges / quests** | Complete tasks for currency | ⬜ |
| **Spin & Win** | Wheel mini-game (IAP spins too) | ⬜ |
| **Score Smash** | Limited-time score event mode | ⬜ |

These are the **highest ROI retention** features Cricket League uses — Stick Archer has analytics to measure them once built (P1 ✅).

---

## 6. Social features

| Feature | Cricket League | Stick Archer |
|---------|----------------|--------------|
| Friends list | ✅ dedicated tab | ⬜ |
| Play with friends | Invite / rematch | ⬜ (Photon rooms possible) |
| Gifts | Exchange gifts with friends | ⬜ |
| Account link bonus | Gems for linking (Facebook etc.) | Cloud save seam exists 🟡 |
| Leaderboards | League / Country / World | ⬜ |
| Chat | Limited social in friends | ⬜ |

**Adoption note:** Friends + private rooms are natural fits for Photon; gifts need economy design to avoid inflation.

---

## 7. Live ops & seasons

| Feature | Description |
|---------|-------------|
| **Seasonal events** | e.g. "Premier League Finals Season" — themed UI, prizes |
| **Elite Pass** | ~$5 seasonal battle-pass track (premium + free tiers) |
| **Event modes** | Score Smash, Koukaburra Season, limited challenges |
| **Mastery** | Per-character challenge ladders (2025) |
| **Remote-tunable offers** | Welcome Kit, Power Hit Offer, Golden Spin Wheel |

**Stick Archer fit:** `RemoteConfig` + `Analytics` already exist (P1). Elite Pass maps directly to **P4 battle pass** in our roadmap.

---

## 8. Monetization (hybrid F2P)

### 8.1 Ad types (typical for this genre)

| Ad placement | Purpose |
|--------------|---------|
| Rewarded video | Double match coins, 12h reward, free pack |
| Interstitial | Between matches (frequency capped) |
| Banner | Menu (lower priority on modern Miniclip titles) |

**Stick Archer:** `RemoteConfig.interstitial_frequency` reserved; no SDK wired (P3).

### 8.2 IAP catalog (examples from store)

| Product type | Examples |
|--------------|----------|
| Soft currency | Bunch/Pile of Coins |
| Hard currency | Bunch/Bag/Pile of Gems |
| Bundles | Welcome Kit, Epic Batsman packs |
| Season pass | Elite Pass |
| Gacha / random | Card packs, Golden Spin Wheel |
| Consumable boosts | Power Hit Offer, special balls |

**Design principle:** Cricket League sells **convenience and collection**, not guaranteed wins. Stick Archer should keep **skill-first** — cosmetics, arrow skins, arena themes, XP boosts, not raw damage.

---

## 9. Onboarding (FTUE)

Cricket League first session:

1. Short **batting/bowling tutorial** (3 balls each)
2. First real match vs auto-matched opponent (1 over)
3. Immediate **currency shower** (gems + ~2000 coins)
4. Guided **first upgrade** / pack open
5. Unlock roadmap shown (tours, 2-over matches)

**Stick Archer gap:** No FTUE (P2 remaining). **High priority** — biggest D1 retention lever in the reference game.

---

## 10. Technical / production notes

| Area | Cricket League approach | Stick Archer implication |
|------|-------------------------|-------------------------|
| 3D vs 2D | Full 3D characters + stadiums | Stay 2D — art cost lower |
| Matchmaking | Dedicated backend (not Photon) | Photon OK for MVP; scale later |
| Cloud save | Account-linked progress | `CloudProfileStore` seam ready |
| Analytics | Heavy funnel + live ops | P1 instrumentation ✅ |
| Store compliance | Random items disclosed in store | Plan gacha legality if adding packs |
| Network | Optimized for weak networks | P0 perf + reconnect work |

---

## 11. Full feature checklist (study sheet)

Use this table when deciding **yes / no / later** for Stick Archer.

See **[FEATURES_COINS_GEMS_LEVEL.md](FEATURES_COINS_GEMS_LEVEL.md)** for the full coins / gems / level spec (Cricket League–inspired).

| # | Feature | Cricket League | Stick Archer now | Suggested phase | Priority |
|---|---------|:--------------:|:----------------:|:---------------:|:--------:|
| 1 | Real-time 1v1 online | ✅ | ✅ | — | — |
| 2 | Quick sessions (≤5 min) | ✅ | ✅ | — | — |
| 3 | Simple skill controls | ✅ | ✅ | — | — |
| 4 | vs AI practice | partial | ✅ | — | — |
| 5 | First-time tutorial | ✅ | ⬜ | P2 | **High** |
| 6 | Account level + XP | ✅ | ✅ | — | — |
| 7 | Coins from matches | ✅ | ✅ | — | — |
| 8 | Premium currency (gems) | ✅ | ⬜ | P3 | Med |
| 9 | Post-match reward UI | ✅ | 🟡 | P2 | **High** |
| 10 | Character collection (25+) | ✅ | 🟡 (2) | P4 | Med |
| 11 | Per-character upgrades | ✅ | ⬜ | P4 | Med |
| 12 | Equipment meta (balls/bats) | ✅ | ⬜ | P4 | Med |
| 13 | Themed locations / tours | ✅ | 🟡 (arenas) | P4 | Med |
| 14 | Loot packs (timed) | ✅ | ⬜ | P4 | Med |
| 15 | Hourly / 12h login rewards | ✅ | ⬜ | P4 | **High** |
| 16 | Daily deals shop | ✅ | ⬜ | P4 | Med |
| 17 | Daily quests / challenges | ✅ | ⬜ | P4 | **High** |
| 18 | Shop UI | ✅ | ⬜ | P3/P4 | **High** |
| 19 | Rewarded ads | ✅ | ⬜ | P3 | **High** |
| 20 | Interstitial ads | ✅ | ⬜ | P3 | Med |
| 21 | IAP currency packs | ✅ | ⬜ | P3 | Med |
| 22 | Season / Elite Pass | ✅ | ⬜ | P4 | Med |
| 23 | Limited-time events | ✅ | ⬜ | P4 | Med |
| 24 | Friends + gifts | ✅ | ⬜ | P4 | Low–Med |
| 25 | Ranked leagues (local/global) | ✅ | ⬜ | P4 | **High** |
| 26 | Play with friends | ✅ | ⬜ | P4 | Med |
| 27 | Cloud save + link bonus | ✅ | 🟡 | P2 | Med |
| 28 | Mastery / per-char challenges | ✅ | ⬜ | P5+ | Low |
| 29 | Spin & Win / mini-games | ✅ | ⬜ | P5+ | Low |
| 30 | Match entry fee (coins) | ✅ | ⬜ | Optional | Low |
| 31 | Reconnect / resilient net | ✅ | ⬜ | P0 | **High** |
| 32 | Gacha random packs | ✅ | ⬜ | Optional | Controversial |

**Legend:** ✅ = shipped · 🟡 = partial · ⬜ = not built

---

## 12. Recommended adoption order for Stick Archer

Ordered by **retention impact ÷ build cost**, aligned with existing roadmap phases:

### Phase A — Finish commercial foundation (before new meta)

1. **P0** Store-ready build (icons, SDK 34, signing)
2. **P0** Network reconnect / lobby timeout
3. **Fix 04–10** Remaining UI screens to Cricket-League-level polish

### Phase B — First retention loop (copy Cricket League's first week)

4. **FTUE tutorial** — 30s aim + fire + one practice kill (Cricket League's 3-ball tutorial)
5. **Post-match rewards card** — show coins/XP on result (code exists; re-enable on defeat or separate screen)
6. **Daily login** — hourly coin drip + 12h chest (rewarded ad optional)

### Phase C — Monetization (when DAU justifies it)

7. **Rewarded ads** — double coins on victory
8. **Interstitials** — every N matches (`RemoteConfig.interstitial_frequency`)
9. **Shop UI** — coin/gem packs + remove-ads IAP
10. **Second premium currency (gems)** — only if shop has enough sinks

### Phase D — Long-term meta (Cricket League's "Team" + "Leagues" tabs)

11. **Ranked leaderboard** — weekly coin-won ranking (League / Region / Global)
12. **Character roster expansion** — 4–6 archers with unlock costs
13. **Arrow/bow equipment** — cosmetic + slight physics variety (not raw damage)
14. **Arena tours** — themed backgrounds gated by level
15. **Daily quests** — "Win 3 matches", "Land 5 headshots"
16. **Season pass (Elite Pass)** — 30-day free + premium track
17. **Friends + private room** — Photon room codes

### Phase E — Optional / later

- Loot packs with timer
- Mastery per character
- Spin & Win
- Match entry fees (risky for casual audience)

---

## 13. Stick Archer translations (cricket → archery)

When implementing, **rename the fantasy** but keep the **economic loop**:

| Cricket League concept | Stick Archer translation |
|------------------------|--------------------------|
| Player card | Archer hero (Adventurer, Soldier, …) |
| Upgrade card level | Hero rank / star level |
| Ball type | Arrow type (normal, fire, heavy…) |
| Bat skin | Bow skin (cosmetic) |
| Stadium / Tour | Arena biome (Forest, Castle, Desert…) |
| 2-over match | Best-of-5 kills (already similar length) |
| Bowling special | Wind gust + gravity modifier round |
| Deluxe pack | "Quiver Pack" — hero shards + coins |
| Elite Pass | "Archer's Season" battle pass |
| League ranking | "Trophy Road" by weekly wins |

---

## 14. What NOT to copy blindly

| Cricket League pattern | Why skip or defer |
|------------------------|-------------------|
| 25+ characters at launch | Art cost; ship 4–6 first |
| Pay-to-win stat bundles | Kills skill-based reputation |
| Heavy gacha / random IAP | Regulatory + player trust; disclose if added |
| 3D stadium production | Stick Archer is 2D; use parallax biomes instead |
| Both bat **and** bowl roles | Archery is symmetric — no phase switch needed |
| Match entry fees early | Can frustrate before economy is fun |

---

## 15. How to use this document

1. **Review §11 checklist** — mark yes/no/later for each row in a design meeting.
2. **Pick a phase from §12** — don't build Phase D before FTUE + daily login.
3. **Update designs** — new screens (Shop, Leagues, Daily) need `designs/*.svg` first.
4. **Update** [PROJECT_DOCUMENTATION.md §2](PROJECT_DOCUMENTATION.md#2-development-plan--roadmap) when priorities change.
5. **Play Cricket League** for 2–3 sessions and validate anything marked "High priority."

---

## 16. External references

- [Google Play — Cricket League](https://play.google.com/store/apps/details?id=com.miniclip.cricketleague)
- [App Store — Cricket League](https://apps.apple.com/app/cricket-league/id1580603339)
- [IGN — Getting started guide](https://in.ign.com/cricket-league/245606/guide/cricket-league-guide-how-to-get-started)
- [IGN — Main menu features](https://in.ign.com/cricket-league/246008/guide/exploring-the-various-features-of-cricket-league)
- [GamingOnPhone — Beginners guide (economy & packs)](https://gamingonphone.com/guides/cricket-league-beginners-guide-tips-and-strategies/)
