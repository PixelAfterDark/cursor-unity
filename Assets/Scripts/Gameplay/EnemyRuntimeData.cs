using UnityEngine;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Runtime data for a single active enemy instance.
    /// Stored in a cache-friendly list inside EnemyController.
    /// </summary>
    public struct EnemyRuntimeData
    {
        public Vector2 position;
        public Vector2 direction;
        public float speed;
        public float rotSpeed;
        public float currentHp;
        public float maxHp;
        public float radius;
        public float dmg;
        public Data.EnemyConfigSO config;
        public Enemy enemyRef;
        public bool isActive;
        public bool isOverlappingPlayer;
    }
}
