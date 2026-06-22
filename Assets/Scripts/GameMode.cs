public static class GameMode
{
    public enum Mode { Online, Practice }
    public enum AIDifficulty { Easy, Normal, Hard }

    public static Mode Current = Mode.Online;
    public static AIDifficulty Difficulty = AIDifficulty.Hard;

    public static bool IsPractice => Current == Mode.Practice;
}
