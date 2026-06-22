using System;
using UnityEngine;
using StickArcher.Analytics;
using AnalyticsFacade = StickArcher.Analytics.Analytics;

namespace StickArcher.Progression
{
    /// <summary>
    /// Owns the live <see cref="PlayerProfile"/>: loads it on boot, grants rewards,
    /// applies the leveling curve, persists via a pluggable <see cref="IProfileStore"/>,
    /// and raises change events for UI. Self-bootstraps (no scene wiring), mirroring
    /// <c>AnalyticsManager</c>.
    ///
    /// All economy mutations funnel through here so currency can never be edited from a
    /// random call site — important once a shop and (later) server validation exist.
    /// </summary>
    public class ProfileManager : MonoBehaviour
    {
        public static ProfileManager Instance { get; private set; }

        public PlayerProfile Profile { get; private set; }

        // Snapshot of the most recent match's rewards, for the result screen to display.
        public int LastRewardCoins { get; private set; }
        public int LastRewardXp { get; private set; }
        public bool LeveledUpLastMatch { get; private set; }

        /// <summary>Fired whenever coins/xp/level/stats change (UI should refresh).</summary>
        public event Action<PlayerProfile> OnProfileChanged;
        /// <summary>Fired when the player advances a level (newLevel).</summary>
        public event Action<int> OnLevelUp;

        IProfileStore _store;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("ProfileManager");
            go.AddComponent<ProfileManager>();
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _store = CreateStore();
            Profile = _store.Load() ?? new PlayerProfile();

            // Seed segmentation properties for analytics dashboards.
            AnalyticsFacade.SetUserProperty("level", Profile.level.ToString());
            AnalyticsFacade.SetUserProperty("coins", Profile.coins.ToString());
        }

        static IProfileStore CreateStore()
        {
#if CLOUD_SAVE_ENABLED
            return new CloudProfileStore();
#else
            return new LocalProfileStore();
#endif
        }

        // ── Rewards ──────────────────────────────────────────────────

        /// <summary>Grant end-of-match rewards. Amounts come from RemoteConfig so the
        /// economy is server-tunable without a new build.</summary>
        public void GrantMatchRewards(bool won, string mode)
        {
            Profile.matchesPlayed++;
            if (won) Profile.matchesWon++;

            int coins = RemoteConfig.GetInt("coins_per_match", 10)
                      + (won ? RemoteConfig.GetInt("coins_per_win", 25) : 0);
            int xp = RemoteConfig.GetInt("xp_per_match", 20)
                   + (won ? RemoteConfig.GetInt("xp_per_win", 50) : 0);

            int levelBefore = Profile.level;
            AddCoins(coins, won ? "match_win" : "match_complete");
            AddXp(xp);

            // Record this match's rewards so the result screen can show them.
            LastRewardCoins = coins;
            LastRewardXp = xp;
            LeveledUpLastMatch = Profile.level > levelBefore;

            Save();
            AnalyticsFacade.Log(GameEvents.MatchEnd + "_reward",
                EventParams.Mode, mode,
                EventParams.LocalWon, won,
                "coins", coins, "xp", xp);
        }

        /// <summary>Tally a kill into lifetime stats (call once per scored kill).</summary>
        public void RecordKill()
        {
            Profile.totalKills++;
            // No save here — match-end Save() captures it; kills are frequent.
            OnProfileChanged?.Invoke(Profile);
        }

        // ── Currency ─────────────────────────────────────────────────

        public void AddCoins(int amount, string reason)
        {
            if (amount <= 0) return;
            Profile.coins += amount;
            AnalyticsFacade.Log(GameEvents.CurrencyEarned,
                EventParams.Amount, amount, EventParams.Balance, Profile.coins, EventParams.Reason, reason);
            AnalyticsFacade.SetUserProperty("coins", Profile.coins.ToString());
            OnProfileChanged?.Invoke(Profile);
        }

        /// <summary>Spend coins. Returns false (and changes nothing) if unaffordable.</summary>
        public bool TrySpendCoins(int amount, string reason)
        {
            if (amount <= 0 || Profile.coins < amount) return false;
            Profile.coins -= amount;
            AnalyticsFacade.Log(GameEvents.CurrencySpent,
                EventParams.Amount, amount, EventParams.Balance, Profile.coins, EventParams.Reason, reason);
            AnalyticsFacade.SetUserProperty("coins", Profile.coins.ToString());
            Save();
            OnProfileChanged?.Invoke(Profile);
            return true;
        }

        // ── Gems (hard currency) ─────────────────────────────────────

        public void AddGems(int amount, string reason)
        {
            if (amount <= 0) return;
            Profile.gems += amount;
            AnalyticsFacade.Log(GameEvents.CurrencyEarned,
                EventParams.Amount, amount, EventParams.Balance, Profile.gems, EventParams.Reason, reason);
            Save();
            OnProfileChanged?.Invoke(Profile);
        }

        /// <summary>Spend gems. Returns false (and changes nothing) if unaffordable.</summary>
        public bool TrySpendGems(int amount, string reason)
        {
            if (amount <= 0 || Profile.gems < amount) return false;
            Profile.gems -= amount;
            AnalyticsFacade.Log(GameEvents.CurrencySpent,
                EventParams.Amount, amount, EventParams.Balance, Profile.gems, EventParams.Reason, reason);
            Save();
            OnProfileChanged?.Invoke(Profile);
            return true;
        }

        // ── Daily / timed rewards ─────────────────────────────────────

        public const int HourlyRewardCoins = 100;
        public const int TwelveHourRewardCoins = 600;
        public const int TwelveHourRewardGems = 5;
        static readonly System.TimeSpan HourlyCooldown = System.TimeSpan.FromHours(1);
        static readonly System.TimeSpan TwelveHourCooldown = System.TimeSpan.FromHours(12);

        public bool CanClaimHourly() => Remaining(Profile.lastHourlyClaimTicks, HourlyCooldown) <= System.TimeSpan.Zero;
        public bool CanClaim12h()   => Remaining(Profile.last12hClaimTicks, TwelveHourCooldown) <= System.TimeSpan.Zero;
        public System.TimeSpan HourlyRemaining() => Remaining(Profile.lastHourlyClaimTicks, HourlyCooldown);
        public System.TimeSpan TwelveHourRemaining() => Remaining(Profile.last12hClaimTicks, TwelveHourCooldown);

        static System.TimeSpan Remaining(long lastTicks, System.TimeSpan cooldown)
        {
            if (lastTicks == 0) return System.TimeSpan.Zero; // never claimed → available now
            var elapsed = System.DateTime.UtcNow - new System.DateTime(lastTicks, System.DateTimeKind.Utc);
            var rem = cooldown - elapsed;
            return rem > System.TimeSpan.Zero ? rem : System.TimeSpan.Zero;
        }

        /// <summary>Claim the hourly reward (coins). Returns false if still on cooldown.</summary>
        public bool ClaimHourly()
        {
            if (!CanClaimHourly()) return false;
            Profile.lastHourlyClaimTicks = System.DateTime.UtcNow.Ticks;
            AddCoins(HourlyRewardCoins, "daily_hourly");
            Save();
            return true;
        }

        /// <summary>Claim the 12-hour reward (coins + gems). Returns false if still on cooldown.</summary>
        public bool Claim12h()
        {
            if (!CanClaim12h()) return false;
            Profile.last12hClaimTicks = System.DateTime.UtcNow.Ticks;
            AddCoins(TwelveHourRewardCoins, "daily_12h");
            AddGems(TwelveHourRewardGems, "daily_12h");
            Save();
            return true;
        }

        /// <summary>True if any timed reward is currently claimable (for launch prompt).</summary>
        public bool HasClaimableReward() => CanClaimHourly() || CanClaim12h();

        // ── XP / leveling ────────────────────────────────────────────

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Profile.xp += amount;

            // Roll over as many levels as the XP covers.
            int needed;
            while (Profile.xp >= (needed = PlayerProfile.XpToAdvance(Profile.level)))
            {
                Profile.xp -= needed;
                Profile.level++;
                // Level-up coin reward (scales with level).
                int reward = LevelUpCoins(Profile.level);
                Profile.coins += reward;
                LastLevelUpCoins = reward;
                AnalyticsFacade.Log(GameEvents.LevelUp, EventParams.Level, Profile.level);
                AnalyticsFacade.SetUserProperty("level", Profile.level.ToString());
                AnalyticsFacade.SetUserProperty("coins", Profile.coins.ToString());
                OnLevelUp?.Invoke(Profile.level);
            }
            OnProfileChanged?.Invoke(Profile);
        }

        /// <summary>Coins granted when reaching the given level.</summary>
        public static int LevelUpCoins(int level) => Mathf.Max(0, level) * 50;

        /// <summary>Coins granted by the most recent level-up (for the level-up modal).</summary>
        public int LastLevelUpCoins { get; private set; }

        /// <summary>XP needed to reach the next level (for progress bars).</summary>
        public int XpForNextLevel() => PlayerProfile.XpToAdvance(Profile.level);

        // ── Cosmetics ────────────────────────────────────────────────

        public bool UnlockCharacter(int index)
        {
            if (Profile.OwnsCharacter(index)) return false;
            var owned = Profile.ownedCharacters ?? new int[0];
            var grown = new int[owned.Length + 1];
            owned.CopyTo(grown, 0);
            grown[owned.Length] = index;
            Profile.ownedCharacters = grown;
            Save();
            OnProfileChanged?.Invoke(Profile);
            return true;
        }

        // ── Persistence ──────────────────────────────────────────────

        public void Save() => _store?.Save(Profile);

        /// <summary>
        /// Wipe all progression back to a fresh profile (coins/XP/level/stats/unlocks)
        /// and persist immediately. Backs the Settings "Reset Progress" action.
        /// </summary>
        public void ResetProgress()
        {
            Profile = new PlayerProfile();
            LastRewardCoins = 0;
            LastRewardXp = 0;
            LeveledUpLastMatch = false;
            Save();

            AnalyticsFacade.SetUserProperty("level", Profile.level.ToString());
            AnalyticsFacade.SetUserProperty("coins", Profile.coins.ToString());

            OnProfileChanged?.Invoke(Profile);
        }
    }
}
