using System;
using System.IO;
using UnityEngine;
using Cursor.Stats;

namespace Cursor.Core
{
    /// <summary>
    /// Handles persistence of game progress via JSON file in persistentDataPath.
    /// Saves currencies, persistent stats, and upgrade states (level + unlocked).
    /// </summary>
    public class SaveSystem : Singleton<SaveSystem>
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        /// <summary>
        /// Serializes current game data and writes it to disk.
        /// </summary>
        public void Save()
        {
            var saveData = new SaveData();
            var statsSystem = SystemsManager.Instance?.StatsSystem;

            if (statsSystem != null)
            {
                saveData.currencies.Add(new CurrencyEntry { key = "currency_A", value = (int)statsSystem.GetStat(StatType.Currency_A_Count) });
                saveData.currencies.Add(new CurrencyEntry { key = "currency_B", value = (int)statsSystem.GetStat(StatType.Currency_B_Count) });
                saveData.currencies.Add(new CurrencyEntry { key = "currency_C", value = (int)statsSystem.GetStat(StatType.Currency_C_Count) });
                saveData.currencies.Add(new CurrencyEntry { key = "currency_D", value = (int)statsSystem.GetStat(StatType.Currency_D_Count) });

                // Save all persistent stats (excluding currencies, which are stored separately)
                foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
                {
                    if (statType == StatType.Currency_A_Count ||
                        statType == StatType.Currency_B_Count ||
                        statType == StatType.Currency_C_Count ||
                        statType == StatType.Currency_D_Count)
                    {
                        continue;
                    }
                    saveData.stats.Add(new StatSaveEntry { key = statType.ToString(), value = statsSystem.GetStat(statType) });
                }
            }

            var upgradeSystem = SystemsManager.Instance?.UpgradeSystem;
            if (upgradeSystem != null)
            {
                saveData.upgrades = upgradeSystem.GetSaveData();
            }

            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SavePath, json);
                Debug.Log("[SaveSystem] Game saved.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save game: {ex.Message}");
            }
        }

        /// <summary>
        /// Deserializes save data from disk. Returns null if no save exists or load fails.
        /// </summary>
        public SaveData Load()
        {
            if (!SaveExists()) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);

                // Defensive: JsonUtility overwrites inline initializers, so lists may be null.
                if (saveData == null) return null;
                saveData.currencies ??= new System.Collections.Generic.List<CurrencyEntry>();
                saveData.stats ??= new System.Collections.Generic.List<StatSaveEntry>();
                saveData.upgrades ??= new System.Collections.Generic.List<UpgradeSaveEntry>();

                return saveData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load save: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns true if a save file exists on disk.
        /// </summary>
        public bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Deletes the save file if it exists (used for New Game).
        /// </summary>
        public void DeleteSave()
        {
            if (!SaveExists()) return;

            try
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save deleted.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save: {ex.Message}");
            }
        }
    }
}
