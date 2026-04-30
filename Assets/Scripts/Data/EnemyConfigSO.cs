using UnityEngine;

namespace Cursor.Data
{
    /// <summary>
    /// Static configuration for an enemy type × subtype combination.
    /// Shared asset — does NOT contain runtime data (position, HP, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Cursor/Enemy Config")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Identity")]
        public Stats.EnemyType enemyType;
        public EnemySubtype enemySubtype;

        [Header("Visual")]
        public Color color = Color.white;
        public Sprite sprite;

        [Header("Stat Multipliers")]
        [Tooltip("Multiplier applied to base HP from StatsSystem")]
        public float hpMultiplier = 1f;
        [Tooltip("Multiplier applied to base DMG from StatsSystem")]
        public float dmgMultiplier = 1f;
        [Tooltip("Multiplier applied to base speed from StatsSystem")]
        public float speedMultiplier = 1f;
        [Tooltip("Multiplier applied to collectable drop count from StatsSystem")]
        public float dropMultiplier = 1f;
    }
}
