using System;

namespace StickArcher.Progression
{
    /// <summary>
    /// Serializable player progression data. Persisted as JSON by an
    /// <see cref="IProfileStore"/>. Keep fields simple (JsonUtility-friendly) and
    /// additive — never rename/remove a field without a migration, or saved data breaks.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        // Bump when the schema changes so a store can migrate old saves.
        public int schemaVersion = 1;

        // Soft currency.
        public int coins = 0;

        // Hard currency (premium).
        public int gems = 0;

        // Daily-reward claim timestamps (DateTime.UtcNow.Ticks; 0 = never claimed).
        public long lastHourlyClaimTicks = 0;
        public long last12hClaimTicks = 0;

        // Progression.
        public int level = 1;
        public int xp = 0;            // xp accumulated within the current level

        // Lifetime stats (cheap retention/segmentation signals).
        public int matchesPlayed = 0;
        public int matchesWon = 0;
        public int totalKills = 0;

        // Cosmetics already owned (character indices). Character 0 is free by default.
        public int[] ownedCharacters = { 0 };

        // ── Leveling curve ───────────────────────────────────────────
        // XP required to advance FROM the given level to the next.
        // Gentle linear ramp: 100, 150, 200, ... keeps early levels fast.
        public static int XpToAdvance(int level) => 100 + Math.Max(0, level - 1) * 50;

        public bool OwnsCharacter(int index)
        {
            if (ownedCharacters == null) return index == 0;
            for (int i = 0; i < ownedCharacters.Length; i++)
                if (ownedCharacters[i] == index) return true;
            return false;
        }
    }
}
