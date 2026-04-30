using UnityEngine;

namespace Cursor.Core
{
    /// <summary>
    /// Central singleton that holds references to all game systems.
    /// Initialized before all other systems via Script Execution Order.
    /// Provides global access to systems: SystemsManager.Instance.StatsSystem etc.
    /// </summary>
    public class SystemsManager : Singleton<SystemsManager>
    {
        // --- System References (registered at runtime) ---
        // These will be populated as we implement each system.
        // Example: public StatsSystem StatsSystem { get; private set; }

        /// <summary>
        /// Registers a system reference. Called by each system in its Awake/Start.
        /// </summary>
        public void RegisterSystem<T>(T system) where T : class
        {
            // Future systems will be registered here.
            // Example:
            // if (system is StatsSystem stats) StatsSystem = stats;
        }

        protected override void OnAwake()
        {
            base.OnAwake();
        }
    }
}
