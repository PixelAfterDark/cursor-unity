using System;
using UnityEngine;

namespace Cursor.Pool
{
    /// <summary>
    /// Configuration for a single object pool entry.
    /// Configured in the Unity Inspector on ObjectPoolManager.
    /// </summary>
    [Serializable]
    public struct PoolConfig
    {
        public PoolKey key;
        public MonoBehaviour prefab;
        public int prewarmCount;
    }
}
