# 12 — Login Rewards (build spec)

Blueprint: [`designs/12_login_rewards.svg`](../12_login_rewards.svg).  
Feature rules: [`Documentation/FEATURES_COINS_GEMS_LEVEL.md`](../../Documentation/FEATURES_COINS_GEMS_LEVEL.md).

**Overlay modal** on Main Menu — entry via Home promo chip ("Free Coins") or auto-prompt when chest ready.

---

> **Design consistency:** [`../DESIGN_CONSISTENCY.md`](../DESIGN_CONSISTENCY.md) — standard defs (`gPrimary`, `gSuccess`, `gPanel`, `shadowSoft`, `goldTitle`), shared components in [`00_foundations.md`](00_foundations.md) §7.

---

## Hierarchy

```
Canvas › Safe (Main Menu)
└─ LoginRewardsModal           (overlay)
   ├─ Overlay    full-screen dim
   └─ Card       800×600 centered
      ├─ Title   "DAILY REWARDS"
      ├─ HourlyChest   panel 300×320
      │  ├─ Chest art
      │  ├─ Reward  +100 coins
      │  └─ Claim   Btn_Success
      ├─ MegaChest     panel 300×320 (12-hour)
      │  ├─ Chest art (purple)
      │  ├─ Rewards +600 coins, +5 gems
      │  ├─ Watch Ad / Claim button
      │  └─ Timer   "Available in 4h 12m" when locked
      └─ Close     icon top-right
```

---

## Rewards (defaults — RemoteConfig)

| Chest | Cooldown | Coins | Gems | Ad required |
|-------|----------|-------|------|-------------|
| Hourly | 60 min | 100 | 0 | No |
| 12-hour | 12 h | 600 | 5 | Optional (double if ad) |

---

## States

| Chest | Ready | Claimed / waiting |
|-------|-------|-------------------|
| Hourly | Green CLAIM button | Grey button + countdown |
| 12-hour | CLAIM or WATCH AD | Timer text below |

On claim: animate coin/gem fly to ProfileBadge; `ProfileManager.AddCoins` / `AddGems`; save timestamps.

---

## Verify

1. Open from Main Menu — modal matches SVG.
2. Claim hourly → +100 coins, badge updates.
3. Re-open within 60 min — hourly locked with timer.
4. 12h chest shows gems row with gem icon.
