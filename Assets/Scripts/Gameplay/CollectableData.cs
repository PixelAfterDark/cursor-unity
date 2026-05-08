using UnityEngine;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Runtime data for a single active collectable instance.
    /// Stored in a cache-friendly list inside CollectableController.
    /// </summary>
    public struct CollectableData
    {
        public Vector2 position;
        public CollectableType type;
        public int amount;
        public CollectableState state;
        public float debounceTimer;
        public Vector2 debounceDirection;
        public CollectableEntity entityRef;
        public Transform transformRef;
    }
}
