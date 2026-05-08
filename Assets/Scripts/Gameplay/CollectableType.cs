using Cursor.Stats;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Collectable types matching the 4 enemy types (1:1 mapping).
    /// A=Shard (Blue), B=Core (Green), C=Crystal (Red), D=Essence (Purple).
    /// </summary>
    public enum CollectableType
    {
        A,
        B,
        C,
        D
    }

    public static class CollectableTypeExtensions
    {
        /// <summary>
        /// Maps a CollectableType to its corresponding currency stat in StatsSystem.
        /// </summary>
        public static StatType ToCurrencyStat(this CollectableType type)
        {
            return type switch
            {
                CollectableType.A => StatType.Currency_A_Count,
                CollectableType.B => StatType.Currency_B_Count,
                CollectableType.C => StatType.Currency_C_Count,
                CollectableType.D => StatType.Currency_D_Count,
                _ => StatType.Currency_A_Count,
            };
        }

        /// <summary>
        /// Maps a CollectableType to its corresponding drop count modifier stat in StatsSystem.
        /// </summary>
        public static StatType ToModifierStat(this CollectableType type)
        {
            return type switch
            {
                CollectableType.A => StatType.CollectableCountModifier_A,
                CollectableType.B => StatType.CollectableCountModifier_B,
                CollectableType.C => StatType.CollectableCountModifier_C,
                CollectableType.D => StatType.CollectableCountModifier_D,
                _ => StatType.CollectableCountModifier_A,
            };
        }
    }
}
