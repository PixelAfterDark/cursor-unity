namespace Cursor.Stats
{
    /// <summary>
    /// Identifiers for all player and global stats managed by StatsSystem.
    /// </summary>
    public enum StatType
    {
        // --- Player Stats ---
        PlayerMaxHp,
        PlayerDmg,
        PlayerDef,
        PlayerRadius,
        CollectDistance,

        // --- Global Modifiers ---
        CollectableCountModifier,

        // --- Spawn Stats ---
        SpawnBurstCount,
        SpawnInterval,
        SpawnIntervalCount,

        // --- Spawn Weights (Type) ---
        SpawnWeight_TypeA,
        SpawnWeight_TypeB,
        SpawnWeight_TypeC,
        SpawnWeight_TypeD,

        // --- Spawn Weights (Subtype per Type) ---
        SpawnWeight_TypeA_Triangle,
        SpawnWeight_TypeA_Square,
        SpawnWeight_TypeA_Hexagon,
        SpawnWeight_TypeA_Circle,

        SpawnWeight_TypeB_Triangle,
        SpawnWeight_TypeB_Square,
        SpawnWeight_TypeB_Hexagon,
        SpawnWeight_TypeB_Circle,

        SpawnWeight_TypeC_Triangle,
        SpawnWeight_TypeC_Square,
        SpawnWeight_TypeC_Hexagon,
        SpawnWeight_TypeC_Circle,

        SpawnWeight_TypeD_Triangle,
        SpawnWeight_TypeD_Square,
        SpawnWeight_TypeD_Hexagon,
        SpawnWeight_TypeD_Circle,

        // --- Currencies (persistent between sessions) ---
        Currency_A_Count,
        Currency_B_Count,
        Currency_C_Count,
        Currency_D_Count,

        // --- Collectable System ---
        PickupRadius,
        CollectableCountModifier_A,
        CollectableCountModifier_B,
        CollectableCountModifier_C,
        CollectableCountModifier_D,
    }
}
