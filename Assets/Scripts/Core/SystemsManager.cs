using UnityEngine;

namespace Cursor.Core
{
    /// <summary>
    /// Central singleton that holds references to all game systems.
    /// Initialized before all other systems via Script Execution Order.
    /// Provides global access to systems: SystemsManager.Instance.StatsSystem etc.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SystemsManager : Singleton<SystemsManager>
    {
        // --- System References (registered at runtime) ---
        public Stats.StatsSystem StatsSystem { get; private set; }
        public Pool.ObjectPoolManager ObjectPoolManager { get; private set; }
        public Gameplay.EnemyController EnemyController { get; private set; }
        public Gameplay.EnemySpawner EnemySpawner { get; private set; }
        public Gameplay.PlayerController PlayerController { get; private set; }

        /// <summary>
        /// Registers a system reference. Called by each system in its Awake/Start.
        /// </summary>
        public void RegisterSystem<T>(T system) where T : class
        {
            switch (system)
            {
                case Stats.StatsSystem stats:
                    StatsSystem = stats;
                    break;
                case Pool.ObjectPoolManager poolManager:
                    ObjectPoolManager = poolManager;
                    break;
                case Gameplay.EnemyController enemyController:
                    EnemyController = enemyController;
                    break;
                case Gameplay.EnemySpawner enemySpawner:
                    EnemySpawner = enemySpawner;
                    break;
                case Gameplay.PlayerController playerController:
                    PlayerController = playerController;
                    break;
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
        }
    }
}
