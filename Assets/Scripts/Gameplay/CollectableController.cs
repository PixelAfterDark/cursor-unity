using System.Collections.Generic;
using UnityEngine;
using Cursor.Pool;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Central controller managing all active collectable instances.
    /// Handles states: Waiting -> Debounce -> Homing -> Pickup.
    /// Iterates collectables in a cache-friendly loop every frame (no individual Updates).
    /// </summary>
    [DefaultExecutionOrder(-55)]
    public class CollectableController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float collectDistance = 2f;
        [SerializeField] private float debounceTime = 0.3f;
        [SerializeField] private float homingSpeed = 8f;
        [SerializeField] private Color[] typeColors = new Color[]
        {
            new Color(0.3f, 0.5f, 1f), // A — Blue
            new Color(0.3f, 1f, 0.3f), // B — Green
            new Color(1f, 0.3f, 0.3f), // C — Red
            new Color(0.8f, 0.3f, 1f), // D — Purple
        };

        private readonly List<CollectableData> _activeCollectables = new();
        private Stats.StatsSystem _statsSystem;
        private PlayerController _player;

        private void Awake()
        {
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        private void Start()
        {
            _statsSystem = Core.SystemsManager.Instance?.StatsSystem;
            _player = Core.SystemsManager.Instance?.PlayerController;
        }

        private void Update()
        {
            if (_player == null || _player.IsDead) return;

            Vector2 playerPos = _player.Position;
            float pickupRadius = _statsSystem != null ? _statsSystem.GetStat(Stats.StatType.PickupRadius) : 0.5f;
            float dt = Time.deltaTime;

            // Iterate backwards for safe removal (O(1) swap)
            for (int i = _activeCollectables.Count - 1; i >= 0; i--)
            {
                if (i >= _activeCollectables.Count) break;

                CollectableData c = _activeCollectables[i];
                if (c.transformRef == null) continue;

                switch (c.state)
                {
                    case CollectableState.Waiting:
                    {
                        float sqrDist = (c.position - playerPos).sqrMagnitude;
                        if (sqrDist < collectDistance * collectDistance)
                        {
                            c.state = CollectableState.Debounce;
                            c.debounceTimer = debounceTime;
                            c.debounceDirection = (c.position - playerPos).normalized;
                            _activeCollectables[i] = c;
                        }
                        break;
                    }

                    case CollectableState.Debounce:
                    {
                        c.debounceTimer -= dt;
                        c.position += c.debounceDirection * homingSpeed * dt;
                        c.transformRef.position = c.position;
                        _activeCollectables[i] = c;

                        if (c.debounceTimer <= 0f)
                        {
                            c.state = CollectableState.Homing;
                            _activeCollectables[i] = c;
                        }
                        break;
                    }

                    case CollectableState.Homing:
                    {
                        c.position = Vector2.MoveTowards(c.position, playerPos, homingSpeed * dt);
                        c.transformRef.position = c.position;
                        _activeCollectables[i] = c;

                        float sqrDist = (c.position - playerPos).sqrMagnitude;
                        if (sqrDist < pickupRadius * pickupRadius)
                        {
                            Pickup(i);
                        }
                        break;
                    }
                }
            }
        }

        // =========================================================
        // REGISTRATION / DESPAWN
        // =========================================================

        /// <summary>
        /// Spawns and registers a new collectable from the pool.
        /// </summary>
        public void RegisterCollectable(Vector2 position, CollectableType type, int amount)
        {
            var poolManager = Core.SystemsManager.Instance?.ObjectPoolManager;
            if (poolManager == null)
            {
                Debug.LogError("[CollectableController] ObjectPoolManager not available.");
                return;
            }

            CollectableEntity entity = poolManager.Get<CollectableEntity>(Pool.PoolKey.Collectable);
            if (entity == null)
            {
                Debug.LogError("[CollectableController] Failed to get collectable from pool.");
                return;
            }

            entity.transform.position = position;

            if (entity.SpriteRenderer != null && typeColors != null && (int)type < typeColors.Length)
            {
                entity.SpriteRenderer.color = typeColors[(int)type];
            }

            CollectableData data = new()
            {
                position = position,
                type = type,
                amount = amount,
                state = CollectableState.Waiting,
                debounceTimer = 0f,
                debounceDirection = Vector2.zero,
                entityRef = entity,
                transformRef = entity.transform,
            };

            _activeCollectables.Add(data);
        }

        /// <summary>
        /// Removes a collectable at the given index and returns it to the pool.
        /// Uses O(1) swap with the last element.
        /// </summary>
        public void UnregisterCollectable(int index)
        {
            if (index < 0 || index >= _activeCollectables.Count) return;

            CollectableData c = _activeCollectables[index];
            if (c.entityRef != null)
            {
                Core.SystemsManager.Instance?.ObjectPoolManager?.Return(Pool.PoolKey.Collectable, c.entityRef);
            }

            int last = _activeCollectables.Count - 1;
            if (index != last)
            {
                _activeCollectables[index] = _activeCollectables[last];
            }
            _activeCollectables.RemoveAt(last);
        }

        /// <summary>
        /// Despawns all active collectables and returns them to the pool.
        /// </summary>
        public void DespawnAll()
        {
            for (int i = _activeCollectables.Count - 1; i >= 0; i--)
            {
                CollectableData c = _activeCollectables[i];
                if (c.entityRef != null)
                {
                    Core.SystemsManager.Instance?.ObjectPoolManager?.Return(Pool.PoolKey.Collectable, c.entityRef);
                }
            }
            _activeCollectables.Clear();
        }

        // =========================================================
        // PICKUP
        // =========================================================

        private void Pickup(int index)
        {
            if (index < 0 || index >= _activeCollectables.Count) return;

            CollectableData c = _activeCollectables[index];

            if (_statsSystem != null)
            {
                _statsSystem.ModifyStat(c.type.ToCurrencyStat(), c.amount);
            }

            Core.EventSystem.Instance.Emit(new Core.CollectablePickedUpEventArgs
            {
                Type = c.type,
                Amount = c.amount
            });

            Debug.Log($"[CollectableController] Picked up {c.type} x{c.amount}");

            UnregisterCollectable(index);
        }
    }
}
