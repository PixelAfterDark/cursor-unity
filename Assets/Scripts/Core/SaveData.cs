using System;
using System.Collections.Generic;

namespace Cursor.Core
{
    /// <summary>
    /// Root serializable data object for the save file.
    /// Uses lists instead of dictionaries because Unity's JsonUtility
    /// does not natively support Dictionary serialization.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string version = "1.0";
        public List<CurrencyEntry> currencies = new();
        public List<StatSaveEntry> stats = new();
        public List<UpgradeSaveEntry> upgrades = new();
    }

    [Serializable]
    public class CurrencyEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class StatSaveEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class UpgradeSaveEntry
    {
        public string id;
        public int level;
        public bool isUnlocked;
    }
}
