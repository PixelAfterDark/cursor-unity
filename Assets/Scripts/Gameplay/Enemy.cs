using UnityEngine;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Component attached to enemy prefabs. Acts as a bridge between the pooled GameObject
    /// and the EnemyController's runtime data list.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        /// <summary>
        /// Index of this enemy inside EnemyController's active list.
        /// Used for O(1) lookups and safe removals.
        /// </summary>
        public int RuntimeIndex { get; set; }

        /// <summary>
        /// Applies visual configuration (sprite, color) from the enemy config.
        /// Called by EnemyController when spawning.
        /// </summary>
        public void SetupVisual(Data.EnemyConfigSO config)
        {
            if (config == null) return;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = config.sprite;
                sr.color = config.color;
            }
        }
    }
}
