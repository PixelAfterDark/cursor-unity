using UnityEngine;
using Cursor.Core;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Listens for enemy deaths and spawns collectables at the death position.
    /// Respects per-enemy collectable count and per-type drop modifiers from StatsSystem.
    /// </summary>
    public class CollectableSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Random scatter radius around the enemy death position.")]
        [SerializeField] private float spawnScatter = 0.5f;

        private void Awake()
        {
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        private void OnEnable()
        {
            this.Subscribe<EnemyKilledEventArgs>(OnEnemyKilled);
            this.Subscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            this.Unsubscribe<EnemyKilledEventArgs>(OnEnemyKilled);
            this.Unsubscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        // =========================================================
        // EVENT HANDLERS
        // =========================================================

        private void OnEnemyKilled(EnemyKilledEventArgs args)
        {
            var statsSystem = Core.SystemsManager.Instance?.StatsSystem;
            var collectableController = Core.SystemsManager.Instance?.CollectableController;

            if (statsSystem == null || collectableController == null)
            {
                Debug.LogWarning("[CollectableSpawner] Missing StatsSystem or CollectableController.");
                return;
            }

            CollectableType cType = args.Type switch
            {
                Stats.EnemyType.A => CollectableType.A,
                Stats.EnemyType.B => CollectableType.B,
                Stats.EnemyType.C => CollectableType.C,
                Stats.EnemyType.D => CollectableType.D,
                _ => CollectableType.A,
            };

            float modifier = statsSystem.GetStat(cType.ToModifierStat());
            int count = Mathf.RoundToInt(args.CollectableCount * modifier);
            count = Mathf.Max(0, count);

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * spawnScatter;
                collectableController.RegisterCollectable(args.Position + offset, cType, 1);
            }

            Debug.Log($"[CollectableSpawner] Spawned {count} collectables of type {cType} at {args.Position}");
        }

        private void OnGameStateChanged(GameStateChangedEventArgs args)
        {
            if (args.State == GameState.Summary || args.State == GameState.Upgrade)
            {
                Core.SystemsManager.Instance?.CollectableController?.DespawnAll();
            }
        }
    }
}
