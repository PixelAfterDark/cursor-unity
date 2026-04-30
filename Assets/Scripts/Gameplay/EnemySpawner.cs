using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cursor.Core;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Spawns enemies from the object pool based on weighted probabilities from StatsSystem.
    /// Supports burst spawn (on session start) and interval spawn (continuous during gameplay).
    /// Must be started/stopped manually via StartSpawn() / StopSpawn() — typically called by GameManager.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Configs")]
        [Tooltip("All EnemyConfigSO assets available for spawning.")]
        [SerializeField] private Data.EnemyConfigSO[] allEnemyConfigs;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnMargin = 1f;
        [SerializeField] private float centerOffset = 2f;

        private Coroutine _spawnCoroutine;
        private bool _isSpawning = false;

        private void Awake()
        {
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        private void OnEnable()
        {
            this.Subscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            this.Unsubscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        // =========================================================
        // PUBLIC API
        // =========================================================

        /// <summary>
        /// Starts spawning: burst + continuous interval coroutine.
        /// </summary>
        public void StartSpawn()
        {
            if (_isSpawning) return;

            var statsSystem = Core.SystemsManager.Instance?.StatsSystem;
            if (statsSystem == null)
            {
                Debug.LogError("[EnemySpawner] StatsSystem not available.");
                return;
            }

            if (allEnemyConfigs == null || allEnemyConfigs.Length == 0)
            {
                Debug.LogWarning("[EnemySpawner] No enemy configs assigned.");
                return;
            }

            _isSpawning = true;

            int burstCount = Mathf.RoundToInt(statsSystem.GetStat(Stats.StatType.SpawnBurstCount));
            float interval = statsSystem.GetStat(Stats.StatType.SpawnInterval);
            int intervalCount = Mathf.RoundToInt(statsSystem.GetStat(Stats.StatType.SpawnIntervalCount));

            // Burst: spawn immediately
            for (int i = 0; i < burstCount; i++)
            {
                SpawnSingleEnemy();
            }

            // Interval: start coroutine
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = StartCoroutine(SpawnIntervalRoutine(interval, intervalCount));
        }

        /// <summary>
        /// Stops the interval coroutine. Existing enemies remain active.
        /// </summary>
        public void StopSpawn()
        {
            if (!_isSpawning) return;
            _isSpawning = false;

            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        // =========================================================
        // STATE MACHINE
        // =========================================================

        private void OnGameStateChanged(GameStateChangedEventArgs args)
        {
            switch (args.State)
            {
                case GameState.Gameplay:
                    StartSpawn();
                    break;
                case GameState.Summary:
                    StopSpawn();
                    Core.SystemsManager.Instance?.EnemyController?.DespawnAll();
                    break;
                case GameState.Upgrade:
                    StopSpawn();
                    Core.SystemsManager.Instance?.EnemyController?.DespawnAll();
                    break;
            }
        }

        // =========================================================
        // SPAWN LOGIC
        // =========================================================

        private IEnumerator SpawnIntervalRoutine(float interval, int count)
        {
            while (_isSpawning)
            {
                yield return new WaitForSeconds(interval);
                if (!_isSpawning) yield break;

                for (int i = 0; i < count; i++)
                {
                    SpawnSingleEnemy();
                }
            }
        }

        private void SpawnSingleEnemy()
        {
            // 1. Pick type
            Stats.EnemyType pickedType = PickRandomType();

            // 2. Pick subtype
            Data.EnemySubtype pickedSubtype = PickRandomSubtype(pickedType);

            // 3. Find matching config
            Data.EnemyConfigSO config = FindConfig(pickedType, pickedSubtype);
            if (config == null)
            {
                Debug.LogWarning($"[EnemySpawner] No config found for {pickedType} {pickedSubtype}. Skipping spawn.");
                return;
            }

            // 4. Random spawn position outside viewport
            Vector2 spawnPos = GetRandomSpawnPosition();

            // 5. Calculate direction (toward center + random offset)
            Vector2 targetPos = Random.insideUnitCircle * centerOffset;
            Vector2 direction = (targetPos - spawnPos).normalized;

            // 6. Register in EnemyController
            Core.SystemsManager.Instance.EnemyController.RegisterEnemy(config, spawnPos, direction);
        }

        // =========================================================
        // RANDOM SELECTION (weighted)
        // =========================================================

        private Stats.EnemyType PickRandomType()
        {
            var statsSystem = Core.SystemsManager.Instance?.StatsSystem;

            float wA = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeA) ?? 0f;
            float wB = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeB) ?? 0f;
            float wC = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeC) ?? 0f;
            float wD = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeD) ?? 0f;

            float total = wA + wB + wC + wD;
            if (total <= 0f) return Stats.EnemyType.A; // fallback

            float roll = Random.Range(0f, total);
            if (roll < wA) return Stats.EnemyType.A;
            if (roll < wA + wB) return Stats.EnemyType.B;
            if (roll < wA + wB + wC) return Stats.EnemyType.C;
            return Stats.EnemyType.D;
        }

        private Data.EnemySubtype PickRandomSubtype(Stats.EnemyType type)
        {
            var statsSystem = Core.SystemsManager.Instance?.StatsSystem;
            float wTri, wSqr, wHex, wCir;

            switch (type)
            {
                case Stats.EnemyType.A:
                    wTri = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeA_Triangle) ?? 0f;
                    wSqr = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeA_Square) ?? 0f;
                    wHex = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeA_Hexagon) ?? 0f;
                    wCir = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeA_Circle) ?? 0f;
                    break;
                case Stats.EnemyType.B:
                    wTri = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeB_Triangle) ?? 0f;
                    wSqr = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeB_Square) ?? 0f;
                    wHex = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeB_Hexagon) ?? 0f;
                    wCir = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeB_Circle) ?? 0f;
                    break;
                case Stats.EnemyType.C:
                    wTri = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeC_Triangle) ?? 0f;
                    wSqr = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeC_Square) ?? 0f;
                    wHex = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeC_Hexagon) ?? 0f;
                    wCir = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeC_Circle) ?? 0f;
                    break;
                case Stats.EnemyType.D:
                    wTri = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeD_Triangle) ?? 0f;
                    wSqr = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeD_Square) ?? 0f;
                    wHex = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeD_Hexagon) ?? 0f;
                    wCir = statsSystem?.GetStat(Stats.StatType.SpawnWeight_TypeD_Circle) ?? 0f;
                    break;
                default:
                    return Data.EnemySubtype.Triangle;
            }

            float total = wTri + wSqr + wHex + wCir;
            if (total <= 0f) return Data.EnemySubtype.Triangle; // fallback

            float roll = Random.Range(0f, total);
            if (roll < wTri) return Data.EnemySubtype.Triangle;
            if (roll < wTri + wSqr) return Data.EnemySubtype.Square;
            if (roll < wTri + wSqr + wHex) return Data.EnemySubtype.Hexagon;
            return Data.EnemySubtype.Circle;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private Data.EnemyConfigSO FindConfig(Stats.EnemyType type, Data.EnemySubtype subtype)
        {
            foreach (var config in allEnemyConfigs)
            {
                if (config != null && config.enemyType == type && config.enemySubtype == subtype)
                    return config;
            }
            return null;
        }

        private Vector2 GetRandomSpawnPosition()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[EnemySpawner] Camera.main not found. Defaulting spawn to zero.");
                return Vector2.zero;
            }

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;

            // Pick a random side (0=top, 1=bottom, 2=left, 3=right)
            int side = Random.Range(0, 4);
            float x, y;

            switch (side)
            {
                case 0: // top
                    x = Random.Range(-width / 2f - spawnMargin, width / 2f + spawnMargin);
                    y = height / 2f + spawnMargin;
                    break;
                case 1: // bottom
                    x = Random.Range(-width / 2f - spawnMargin, width / 2f + spawnMargin);
                    y = -height / 2f - spawnMargin;
                    break;
                case 2: // left
                    x = -width / 2f - spawnMargin;
                    y = Random.Range(-height / 2f - spawnMargin, height / 2f + spawnMargin);
                    break;
                default: // right
                    x = width / 2f + spawnMargin;
                    y = Random.Range(-height / 2f - spawnMargin, height / 2f + spawnMargin);
                    break;
            }

            return new Vector2(x, y);
        }
    }
}
