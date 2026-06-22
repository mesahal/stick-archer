namespace StickArcher.Progression
{
    /// <summary>
    /// Pluggable persistence for <see cref="PlayerProfile"/>. The game talks only to
    /// <see cref="ProfileManager"/>; swapping local-disk for a cloud backend
    /// (PlayFab / Firebase / UGS Cloud Save) is a one-class change behind this interface.
    /// </summary>
    public interface IProfileStore
    {
        /// <summary>Load the saved profile, or null if none exists yet.</summary>
        PlayerProfile Load();

        /// <summary>Persist the profile. Should be cheap/safe to call frequently.</summary>
        void Save(PlayerProfile profile);
    }
}
