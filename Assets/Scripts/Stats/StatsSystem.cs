using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cursor.Stats
{
    /// <summary>
    /// Central source of truth for all game statistics.
    /// Provides API for querying and modifying player/global stats and per-enemy-type stats.
    /// Emits events on stat changes so other systems (UI, SaveSystem) can react.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class StatsSystem : Core.Singleton<StatsSystem>
    {
        // =========================================================
        // EVENTS
        // =========================================================
        public event Action<StatType, float> OnStatChanged;

        // =========================================================
        // PLAYER / GLOBAL STATS
        // =========================================================
        private readonly Dictionary<StatType, float> _stats = new();

        // =========================================================
        // ENEMY STATS (per type)
        // =========================================================
        private readonly Dictionary<EnemyType, EnemyStats> _enemyStats = new();

        // =========================================================
        // LIFECYCLE
        // =========================================================
        protected override void OnAwake()
        {
            base.OnAwake();
            InitializeDefaults();
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        // =========================================================
        // INITIALIZATION — hard-coded defaults (pre-UpgradeSystem)
        // =========================================================
        public void InitializeDefaults()
        {
            _stats.Clear();
            _enemyStats.Clear();

            // --- Player stats (GDD Section 5) ---
            _stats[StatType.PlayerMaxHp] = 100f;
            _stats[StatType.PlayerDmg] = 10f;
            _stats[StatType.PlayerDef] = 0f;
            _stats[StatType.PlayerRadius] = 0.5f;
            _stats[StatType.CollectDistance] = 2.0f;

            // --- Global modifiers ---
            _stats[StatType.CollectableCountModifier] = 1.0f;

            // --- Spawn stats ---
            _stats[StatType.SpawnBurstCount] = 5f;
            _stats[StatType.SpawnInterval] = 3.0f;
            _stats[StatType.SpawnIntervalCount] = 2f;

            // --- Spawn weights (Type) ---
            _stats[StatType.SpawnWeight_TypeA] = 1f;
            _stats[StatType.SpawnWeight_TypeB] = 0f;
            _stats[StatType.SpawnWeight_TypeC] = 0f;
            _stats[StatType.SpawnWeight_TypeD] = 0f;

            // --- Spawn weights (Subtype) ---
            // Type A: only Triangle on start
            _stats[StatType.SpawnWeight_TypeA_Triangle] = 1f;
            _stats[StatType.SpawnWeight_TypeA_Square] = 0f;
            _stats[StatType.SpawnWeight_TypeA_Hexagon] = 0f;
            _stats[StatType.SpawnWeight_TypeA_Circle] = 0f;

            // Type B: none on start
            _stats[StatType.SpawnWeight_TypeB_Triangle] = 0f;
            _stats[StatType.SpawnWeight_TypeB_Square] = 0f;
            _stats[StatType.SpawnWeight_TypeB_Hexagon] = 0f;
            _stats[StatType.SpawnWeight_TypeB_Circle] = 0f;

            // Type C: none on start
            _stats[StatType.SpawnWeight_TypeC_Triangle] = 0f;
            _stats[StatType.SpawnWeight_TypeC_Square] = 0f;
            _stats[StatType.SpawnWeight_TypeC_Hexagon] = 0f;
            _stats[StatType.SpawnWeight_TypeC_Circle] = 0f;

            // Type D: none on start
            _stats[StatType.SpawnWeight_TypeD_Triangle] = 0f;
            _stats[StatType.SpawnWeight_TypeD_Square] = 0f;
            _stats[StatType.SpawnWeight_TypeD_Hexagon] = 0f;
            _stats[StatType.SpawnWeight_TypeD_Circle] = 0f;

            // --- Currencies (start at 0) ---
            _stats[StatType.Currency_A_Count] = 0f;
            _stats[StatType.Currency_B_Count] = 0f;
            _stats[StatType.Currency_C_Count] = 0f;
            _stats[StatType.Currency_D_Count] = 0f;

            // --- Collectable System ---
            _stats[StatType.PickupRadius] = 0.5f;
            _stats[StatType.CollectableCountModifier_A] = 1f;
            _stats[StatType.CollectableCountModifier_B] = 1f;
            _stats[StatType.CollectableCountModifier_C] = 1f;
            _stats[StatType.CollectableCountModifier_D] = 1f;

            // --- Enemy base stats (GDD Section 6) ---
            // Type A — Blue: small, fast, weak, drops Shard
            _enemyStats[EnemyType.A] = new EnemyStats
            {
                baseHp = 20f,
                baseDmg = 5f,
                speedMin = 2.0f,
                speedMax = 4.0f,
                rotSpeedMin = 30f,
                rotSpeedMax = 90f,
                radius = 0.3f,
                collectableCount = 1,
            };

            // Type B — Green: balanced, drops Core
            _enemyStats[EnemyType.B] = new EnemyStats
            {
                baseHp = 40f,
                baseDmg = 10f,
                speedMin = 1.5f,
                speedMax = 3.0f,
                rotSpeedMin = 20f,
                rotSpeedMax = 60f,
                radius = 0.5f,
                collectableCount = 1,
            };

            // Type C — Red: big, slow, high HP/DMG, drops Crystal
            _enemyStats[EnemyType.C] = new EnemyStats
            {
                baseHp = 80f,
                baseDmg = 20f,
                speedMin = 0.8f,
                speedMax = 1.5f,
                rotSpeedMin = 10f,
                rotSpeedMax = 30f,
                radius = 0.7f,
                collectableCount = 2,
            };

            // Type D — Purple: special / rare, drops Essence
            _enemyStats[EnemyType.D] = new EnemyStats
            {
                baseHp = 150f,
                baseDmg = 35f,
                speedMin = 0.5f,
                speedMax = 1.0f,
                rotSpeedMin = 5f,
                rotSpeedMax = 20f,
                radius = 1.0f,
                collectableCount = 3,
            };
        }

        // =========================================================
        // PLAYER / GLOBAL STAT API
        // =========================================================

        /// <summary>
        /// Returns the current value of a player/global stat.
        /// Returns 0 if the stat has never been set.
        /// </summary>
        public float GetStat(StatType stat)
        {
            return _stats.TryGetValue(stat, out float value) ? value : 0f;
        }

        /// <summary>
        /// Sets a stat to an absolute value and emits OnStatChanged.
        /// </summary>
        public void SetStat(StatType stat, float value)
        {
            _stats[stat] = value;
            OnStatChanged?.Invoke(stat, value);
        }

        /// <summary>
        /// Adds a delta to the current stat value (additive modification).
        /// </summary>
        public void ModifyStat(StatType stat, float delta)
        {
            float newValue = GetStat(stat) + delta;
            _stats[stat] = newValue;
            OnStatChanged?.Invoke(stat, newValue);
        }

        /// <summary>
        /// Multiplies the current stat value by a multiplier.
        /// </summary>
        public void MultiplyStat(StatType stat, float multiplier)
        {
            float newValue = GetStat(stat) * multiplier;
            _stats[stat] = newValue;
            OnStatChanged?.Invoke(stat, newValue);
        }

        // =========================================================
        // ENEMY STAT API
        // =========================================================

        /// <summary>
        /// Returns the base stats for a given enemy type.
        /// </summary>
        public EnemyStats GetEnemyStats(EnemyType type)
        {
            return _enemyStats.TryGetValue(type, out EnemyStats stats) ? stats : default;
        }

        /// <summary>
        /// Overwrites all base stats for a given enemy type.
        /// </summary>
        public void SetEnemyStats(EnemyType type, EnemyStats stats)
        {
            _enemyStats[type] = stats;
        }

        /// <summary>
        /// Convenience helper to add a delta to a single field inside enemy stats.
        /// </summary>
        public void ModifyEnemyStat(EnemyType type, Func<EnemyStats, float> getter, Action<EnemyStats, float> setter, float delta)
        {
            if (!_enemyStats.TryGetValue(type, out EnemyStats stats))
                stats = default;

            float current = getter(stats);
            setter(stats, current + delta);
            _enemyStats[type] = stats;
        }

        /// <summary>
        /// Attempts to spend currency. Returns false if balance is insufficient.
        /// </summary>
        public bool SpendCurrency(StatType currencyStat, int amount)
        {
            if (amount <= 0) return true;
            float current = GetStat(currencyStat);
            if (current < amount) return false;

            float newValue = current - amount;
            _stats[currencyStat] = newValue;
            OnStatChanged?.Invoke(currencyStat, newValue);
            return true;
        }

        // =========================================================
        // SESSION MANAGEMENT
        // =========================================================

        /// <summary>
        /// Resets session-only stats. Called by GameManager when a new gameplay session starts.
        /// Currencies are persistent across sessions and are NOT reset here.
        /// Player current HP is managed by PlayerController and resets on Gameplay state entry.
        /// </summary>
        public void ResetSessionStats()
        {
            // Session-bound resets happen here when needed.
            // Example: any temporary buffs or per-session modifiers would be cleared.
        }

        // =========================================================
        // SAVE / LOAD from SaveData (Stage 6+)
        // =========================================================

        /// <summary>
        /// Restores persistent data from a SaveData object.
        /// Starts from defaults and overlays saved currencies.
        /// </summary>
        public void LoadFrom(Core.SaveData saveData)
        {
            if (saveData == null) return;

            // Start from a clean slate so missing fields fall back to defaults.
            InitializeDefaults();

            foreach (var entry in saveData.currencies)
            {
                if (entry == null) continue;
                if (TryParseCurrencyKey(entry.key, out StatType currencyStat))
                {
                    _stats[currencyStat] = entry.value;
                }
            }

            foreach (var entry in saveData.stats)
            {
                if (entry == null) continue;
                if (System.Enum.TryParse<StatType>(entry.key, out StatType statType))
                {
                    _stats[statType] = entry.value;
                }
            }

            // Notify listeners so UI refreshes.
            OnStatChanged?.Invoke(StatType.Currency_A_Count, GetStat(StatType.Currency_A_Count));
            OnStatChanged?.Invoke(StatType.Currency_B_Count, GetStat(StatType.Currency_B_Count));
            OnStatChanged?.Invoke(StatType.Currency_C_Count, GetStat(StatType.Currency_C_Count));
            OnStatChanged?.Invoke(StatType.Currency_D_Count, GetStat(StatType.Currency_D_Count));
        }

        private bool TryParseCurrencyKey(string key, out StatType statType)
        {
            statType = default;
            switch (key)
            {
                case "currency_A": statType = StatType.Currency_A_Count; return true;
                case "currency_B": statType = StatType.Currency_B_Count; return true;
                case "currency_C": statType = StatType.Currency_C_Count; return true;
                case "currency_D": statType = StatType.Currency_D_Count; return true;
            }
            return false;
        }

        // =========================================================
        // SAVE / LOAD (prepared for future SaveSystem)
        // =========================================================

        /// <summary>
        /// Serializable snapshot of the entire StatsSystem state.
        /// </summary>
        [Serializable]
        public class StatsData
        {
            public List<StatEntry> stats = new();
            public List<EnemyStatEntry> enemyStats = new();
        }

        [Serializable]
        public class StatEntry
        {
            public StatType type;
            public float value;
        }

        [Serializable]
        public class EnemyStatEntry
        {
            public EnemyType type;
            public EnemyStats stats;
        }

        /// <summary>
        /// Exports current state into a serializable data object.
        /// </summary>
        public StatsData GetSaveData()
        {
            StatsData data = new();

            foreach (var kvp in _stats)
            {
                data.stats.Add(new StatEntry { type = kvp.Key, value = kvp.Value });
            }

            foreach (var kvp in _enemyStats)
            {
                data.enemyStats.Add(new EnemyStatEntry { type = kvp.Key, stats = kvp.Value });
            }

            return data;
        }

        /// <summary>
        /// Restores state from a serializable data object.
        /// </summary>
        public void LoadSaveData(StatsData data)
        {
            if (data == null) return;

            _stats.Clear();
            foreach (var entry in data.stats)
            {
                _stats[entry.type] = entry.value;
            }

            _enemyStats.Clear();
            foreach (var entry in data.enemyStats)
            {
                _enemyStats[entry.type] = entry.stats;
            }
        }
    }
}
