using System;

namespace Cursor.Stats
{
    /// <summary>
    /// Base stats for a single enemy type.
    /// These values are scaled by DifficultyScaler at spawn time.
    /// </summary>
    [Serializable]
    public struct EnemyStats
    {
        public float baseHp;
        public float baseDmg;
        public float speedMin;
        public float speedMax;
        public float rotSpeedMin;
        public float rotSpeedMax;
        public float radius;
        public int collectableCount;
    }
}
