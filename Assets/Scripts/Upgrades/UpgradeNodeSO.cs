using System.Collections.Generic;
using UnityEngine;
using Cursor.Stats;

namespace Cursor.Upgrades
{
    [CreateAssetMenu(fileName = "UpgradeNode", menuName = "Cursor/Upgrade Node")]
    public class UpgradeNodeSO : ScriptableObject
    {
        [field: SerializeField] public string UpgradeId { get; private set; }
        [field: SerializeField] public string UpgradeName { get; private set; }
        [field: SerializeField, TextArea] public string Description { get; private set; }
        [field: SerializeField] public int MaxLevel { get; private set; } = 1;
        [field: SerializeField] public StatType TargetStat { get; private set; }

        [Tooltip("Bonus applied at each level. Index 0 = Level 1 bonus.")]
        [field: SerializeField] public float[] ValuesPerLevel { get; private set; }

        [Tooltip("Cost for each level. Index 0 = Level 1 cost.")]
        [field: SerializeField] public int[] CostsPerLevel { get; private set; }

        [field: SerializeField] public StatType CostCurrency { get; private set; }

        [Tooltip("Children unlocked when this node reaches level 1.")]
        [field: SerializeField] public List<UpgradeNodeSO> UnlocksOnLevel1 { get; private set; } = new List<UpgradeNodeSO>();

        [field: SerializeField] public bool IsRoot { get; private set; }

        [field: SerializeField] public Sprite Icon { get; private set; }

        private void OnValidate()
        {
            if (ValuesPerLevel != null && ValuesPerLevel.Length != MaxLevel)
                Debug.LogWarning($"[{name}] ValuesPerLevel length ({ValuesPerLevel.Length}) != MaxLevel ({MaxLevel})");
            if (CostsPerLevel != null && CostsPerLevel.Length != MaxLevel)
                Debug.LogWarning($"[{name}] CostsPerLevel length ({CostsPerLevel.Length}) != MaxLevel ({MaxLevel})");
        }
    }
}
