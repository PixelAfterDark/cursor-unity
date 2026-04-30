using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cursor.Pool
{
    /// <summary>
    /// Central manager for all object pools.
    /// Configured via Inspector (PoolConfig list) and registered in SystemsManager.
    /// Provides typed Get / Return API for efficient object reuse without runtime allocations.
    /// </summary>
    [DefaultExecutionOrder(-75)]
    public class ObjectPoolManager : MonoBehaviour
    {
        [Header("Pool Configurations")]
        [SerializeField] private PoolConfig[] poolConfigs;

        private readonly Dictionary<PoolKey, IPool> _pools = new();

        private void Awake()
        {
            InitializePools();
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        /// <summary>
        /// Registers and prewarms all pools defined in the Inspector.
        /// </summary>
        private void InitializePools()
        {
            if (poolConfigs == null) return;

            foreach (var config in poolConfigs)
            {
                if (config.prefab == null)
                {
                    Debug.LogWarning($"[ObjectPoolManager] Pool '{config.key}' has no prefab assigned. Skipping.");
                    continue;
                }

                RegisterPoolInternal(config);
            }
        }

        /// <summary>
        /// Registers a pool using reflection to instantiate the correct generic ObjectPool&lt;T&gt;.
        /// </summary>
        private void RegisterPoolInternal(PoolConfig config)
        {
            // Create a container transform for cleanliness in the hierarchy
            Transform container = new GameObject($"Pool_{config.key}").transform;
            container.SetParent(transform, false);

            // Use reflection to create ObjectPool<T> where T is the prefab's MonoBehaviour type
            Type prefabType = config.prefab.GetType();
            Type poolType = typeof(ObjectPool<>).MakeGenericType(prefabType);
            IPool pool = (IPool)Activator.CreateInstance(poolType, config.prefab, container);

            pool.Prewarm(config.prewarmCount);
            _pools[config.key] = pool;

            Debug.Log($"[ObjectPoolManager] Registered pool '{config.key}' with {config.prewarmCount} prewarmed instances.");
        }

        /// <summary>
        /// Retrieves a typed instance from the specified pool.
        /// </summary>
        public T Get<T>(PoolKey key) where T : MonoBehaviour
        {
            if (!_pools.TryGetValue(key, out IPool pool))
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{key}' is not registered.");
                return null;
            }

            if (pool is ObjectPool<T> typedPool)
            {
                return typedPool.Get();
            }

            Debug.LogError($"[ObjectPoolManager] Pool '{key}' type mismatch. Expected {typeof(T).Name}.");
            return null;
        }

        /// <summary>
        /// Returns a typed instance to the specified pool.
        /// </summary>
        public void Return<T>(PoolKey key, T obj) where T : MonoBehaviour
        {
            if (obj == null) return;

            if (!_pools.TryGetValue(key, out IPool pool))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' is not registered. Destroying object instead.");
                Destroy(obj.gameObject);
                return;
            }

            if (pool is ObjectPool<T> typedPool)
            {
                typedPool.Return(obj);
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' type mismatch. Destroying object instead.");
                Destroy(obj.gameObject);
            }
        }

        /// <summary>
        /// Returns a non-generic instance to the specified pool (fallback for dynamic typing).
        /// </summary>
        public void Return(PoolKey key, MonoBehaviour obj)
        {
            if (obj == null) return;

            if (!_pools.TryGetValue(key, out IPool pool))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' is not registered. Destroying object instead.");
                Destroy(obj.gameObject);
                return;
            }

            pool.Return(obj);
        }
    }
}
