using System.Collections.Generic;
using UnityEngine;

namespace Cursor.Pool
{
    /// <summary>
    /// Generic object pool for a specific MonoBehaviour type.
    /// Keeps instances inactive in a queue and reuses them to avoid runtime allocations.
    /// </summary>
    public class ObjectPool<T> : IPool where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _container;
        private readonly Queue<T> _pool = new();

        public ObjectPool(T prefab, Transform container)
        {
            _prefab = prefab;
            _container = container;
        }

        /// <summary>
        /// Retrieves an active instance from the pool. Instantiates a new one if the pool is empty.
        /// </summary>
        public T Get()
        {
            T instance;

            if (_pool.Count > 0)
            {
                instance = _pool.Dequeue();
            }
            else
            {
                instance = Object.Instantiate(_prefab, _container);
            }

            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool, deactivating it.
        /// </summary>
        public void Return(T instance)
        {
            if (instance == null) return;

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_container, false);
            _pool.Enqueue(instance);
        }

        /// <summary>
        /// Pre-instantiates a number of inactive instances.
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T instance = Object.Instantiate(_prefab, _container);
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        // --- IPool explicit implementation (non-generic fallback) ---

        MonoBehaviour IPool.Get()
        {
            return Get();
        }

        void IPool.Return(MonoBehaviour obj)
        {
            if (obj is T typed) Return(typed);
        }
    }
}
