namespace Cursor.Gameplay
{
    /// <summary>
    /// States of a collectable during its lifetime.
    /// </summary>
    public enum CollectableState
    {
        /// <summary>
        /// Idle, waiting for player to come within collect distance.
        /// </summary>
        Waiting,

        /// <summary>
        /// Briefly pushing away from player before homing starts.
        /// </summary>
        Debounce,

        /// <summary>
        /// Moving toward player.
        /// </summary>
        Homing
    }
}
