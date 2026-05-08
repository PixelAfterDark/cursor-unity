using System.Collections.Generic;
using UnityEngine;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Central controller managing all active enemy instances.
    /// Handles movement, rotation, viewport bounds checking, and pooling.
    /// Iterates enemies in a cache-friendly loop every frame (no individual Updates).
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class EnemyController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float despawnMargin = 1f;

        private readonly List<EnemyRuntimeData> _activeEnemies = new();
        private Rect _playArea;

        public IReadOnlyList<EnemyRuntimeData> ActiveEnemies => _activeEnemies;

        private void Awake()
        {
            CalculatePlayArea();
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Iterate backwards for safe removal (O(1) swap)
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                // List may have been cleared by external events (e.g. player death -> DespawnAll)
                if (i >= _activeEnemies.Count) break;

                EnemyRuntimeData enemy = _activeEnemies[i];
                if (!enemy.isActive) continue;

                // --- Movement ---
                enemy.position += enemy.direction * enemy.speed * dt;
                enemy.enemyRef.transform.position = enemy.position;

                // --- Rotation ---
                enemy.enemyRef.transform.Rotate(0f, 0f, enemy.rotSpeed * dt);

                // --- Viewport bounds check ---
                if (IsOutsidePlayArea(enemy.position))
                {
                    UnregisterEnemy(i);
                }
                else
                {
                    // Write back modified struct (position was modified)
                    _activeEnemies[i] = enemy;

                    // --- Collision with Player (Enter/Exit) ---
                    var player = Core.SystemsManager.Instance?.PlayerController;
                    if (player != null && !player.IsDead)
                    {
                        float sqrDist = (enemy.position - player.Position).sqrMagnitude;
                        float playerRadius = Core.SystemsManager.Instance.StatsSystem.GetStat(Stats.StatType.PlayerRadius);
                        float radiusSum = enemy.radius + playerRadius;
                        bool isOverlapping = sqrDist < radiusSum * radiusSum;

                        if (isOverlapping)
                        {
                            if (!enemy.isOverlappingPlayer)
                            {
                                // ENTRY — apply damage once per overlap session
                                enemy.isOverlappingPlayer = true;
                                _activeEnemies[i] = enemy;

                                float playerDmg = Core.SystemsManager.Instance.StatsSystem.GetStat(Stats.StatType.PlayerDmg);
                                player.TakeDamage(enemy.dmg);
                                TakeDamage(i, playerDmg);

                                // TakeDamage may have called UnregisterEnemy (swap-remove).
                                // Safe to continue because we iterate backwards.
                                continue;
                            }
                        }
                        else
                        {
                            if (enemy.isOverlappingPlayer)
                            {
                                enemy.isOverlappingPlayer = false;
                                _activeEnemies[i] = enemy;
                            }
                        }
                    }
                }
            }
        }

        // =========================================================
        // REGISTRATION / DESPAWN
        // =========================================================

        /// <summary>
        /// Spawns and registers a new enemy from the pool with the given config.
        /// </summary>
        public EnemyRuntimeData RegisterEnemy(Data.EnemyConfigSO config, Vector2 spawnPosition, Vector2 direction)
        {
            if (config == null)
            {
                Debug.LogError("[EnemyController] Cannot register enemy with null config.");
                return default;
            }

            var statsSystem = Core.SystemsManager.Instance?.StatsSystem;
            var poolManager = Core.SystemsManager.Instance?.ObjectPoolManager;

            if (statsSystem == null || poolManager == null)
            {
                Debug.LogError("[EnemyController] StatsSystem or ObjectPoolManager is not available.");
                return default;
            }

            // Get base stats and apply multipliers
            var baseStats = statsSystem.GetEnemyStats(config.enemyType);
            float speed = Random.Range(baseStats.speedMin, baseStats.speedMax) * config.speedMultiplier;
            float rotSpeed = Random.Range(baseStats.rotSpeedMin, baseStats.rotSpeedMax);

            var difficultyScaler = Core.SystemsManager.Instance?.DifficultyScaler;
            float hpMult = difficultyScaler?.HpMultiplier ?? 1f;
            float dmgMult = difficultyScaler?.DmgMultiplier ?? 1f;

            float maxHp = baseStats.baseHp * config.hpMultiplier * hpMult;

            // Pull from pool
            Enemy enemy = poolManager.Get<Enemy>(Pool.PoolKey.Enemy);
            if (enemy == null)
            {
                Debug.LogError("[EnemyController] Failed to get enemy from pool.");
                return default;
            }

            enemy.SetupVisual(config);

            EnemyRuntimeData data = new()
            {
                position = spawnPosition,
                direction = direction.normalized,
                speed = speed,
                rotSpeed = rotSpeed,
                maxHp = maxHp,
                currentHp = maxHp,
                radius = baseStats.radius,
                dmg = baseStats.baseDmg * config.dmgMultiplier * dmgMult,
                collectableCount = config.collectableCount,
                config = config,
                enemyRef = enemy,
                isActive = true,
                isOverlappingPlayer = false,
            };

            enemy.RuntimeIndex = _activeEnemies.Count;
            enemy.transform.position = spawnPosition;
            _activeEnemies.Add(data);

            return data;
        }

        /// <summary>
        /// Removes an enemy at the given index and returns it to the pool.
        /// Uses O(1) swap with the last element.
        /// </summary>
        public void UnregisterEnemy(int index)
        {
            if (index < 0 || index >= _activeEnemies.Count) return;

            EnemyRuntimeData enemy = _activeEnemies[index];
            enemy.isActive = false;

            // Return to pool
            Core.SystemsManager.Instance?.ObjectPoolManager?.Return(Pool.PoolKey.Enemy, enemy.enemyRef);

            // O(1) removal — swap with last element
            int last = _activeEnemies.Count - 1;
            if (index != last)
            {
                _activeEnemies[index] = _activeEnemies[last];
                _activeEnemies[index].enemyRef.RuntimeIndex = index;
            }
            _activeEnemies.RemoveAt(last);
        }

        /// <summary>
        /// Despawns all active enemies and returns them to the pool.
        /// </summary>
        public void DespawnAll()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                EnemyRuntimeData enemy = _activeEnemies[i];
                Core.SystemsManager.Instance?.ObjectPoolManager?.Return(Pool.PoolKey.Enemy, enemy.enemyRef);
            }
            _activeEnemies.Clear();
        }

        // =========================================================
        // DAMAGE / HELPERS
        // =========================================================

        /// <summary>
        /// Applies damage to an enemy at the given index.
        /// </summary>
        public void TakeDamage(int index, float damage)
        {
            if (index < 0 || index >= _activeEnemies.Count) return;

            EnemyRuntimeData enemy = _activeEnemies[index];
            enemy.currentHp -= damage;

            if (enemy.currentHp <= 0f)
            {
                Core.EventSystem.Instance.Emit(new Core.EnemyKilledEventArgs
                {
                    Position = enemy.position,
                    Type = enemy.config.enemyType,
                    Subtype = enemy.config.enemySubtype,
                    CollectableCount = enemy.collectableCount
                });
                UnregisterEnemy(index);
            }
            else
            {
                _activeEnemies[index] = enemy;
            }
        }

        /// <summary>
        /// Calculates the squared distance between an enemy and a point.
        /// </summary>
        public float GetSqrDistanceTo(int index, Vector2 point)
        {
            if (index < 0 || index >= _activeEnemies.Count) return float.MaxValue;
            return (_activeEnemies[index].position - point).sqrMagnitude;
        }

        // =========================================================
        // PLAY AREA
        // =========================================================

        private void CalculatePlayArea()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[EnemyController] Camera.main not found. Play area defaults to zero.");
                _playArea = new Rect(0f, 0f, 0f, 0f);
                return;
            }

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            _playArea = new Rect(-width / 2f, -height / 2f, width, height);
        }

        private bool IsOutsidePlayArea(Vector2 position)
        {
            return position.x < _playArea.xMin - despawnMargin
                || position.x > _playArea.xMax + despawnMargin
                || position.y < _playArea.yMin - despawnMargin
                || position.y > _playArea.yMax + despawnMargin;
        }
    }
}
