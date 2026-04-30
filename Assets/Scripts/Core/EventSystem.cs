using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cursor.Core
{
    /// <summary>
    /// Generic, type-safe, scoped publish-subscribe event system.
    /// All events use strongly-typed payloads (structs). No string keys.
    /// </summary>
    public class EventSystem : Singleton<EventSystem>
    {
        // --- Internal EventBus<T> Implementation ---

        private interface IEventBus
        {
            void ClearScope(EventScope scope);
        }

        private class EventBus<T> : IEventBus
        {
            private class Subscription
            {
                public Action<T> Handler;
                public EventScope Scope;

                public Subscription(Action<T> handler, EventScope scope)
                {
                    Handler = handler;
                    Scope = scope;
                }
            }

            private readonly List<Subscription> _subscriptions = new List<Subscription>();
            private readonly object _lock = new object();

            public void Subscribe(Action<T> handler, EventScope scope)
            {
                if (handler == null) return;

                lock (_lock)
                {
                    _subscriptions.Add(new Subscription(handler, scope));
                }
            }

            public void Unsubscribe(Action<T> handler)
            {
                if (handler == null) return;

                lock (_lock)
                {
                    for (int i = _subscriptions.Count - 1; i >= 0; i--)
                    {
                        if (_subscriptions[i].Handler == handler)
                        {
                            _subscriptions.RemoveAt(i);
                        }
                    }
                }
            }

            public void Emit(T args)
            {
                List<Subscription> snapshot;
                lock (_lock)
                {
                    snapshot = new List<Subscription>(_subscriptions);
                }

                List<Subscription> toRemove = null;

                for (int i = 0; i < snapshot.Count; i++)
                {
                    var sub = snapshot[i];
                    if (sub.Handler == null)
                        continue;

                    // Auto-cleanup: remove delegates targeting destroyed MonoBehaviours
                    if (sub.Handler.Target is UnityEngine.Object unityObj && unityObj == null)
                    {
                        toRemove ??= new List<Subscription>();
                        toRemove.Add(sub);
                        continue;
                    }

                    try
                    {
                        sub.Handler.Invoke(args);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (toRemove != null)
                {
                    lock (_lock)
                    {
                        foreach (var sub in toRemove)
                        {
                            _subscriptions.Remove(sub);
                        }
                    }
                }
            }

            public void ClearScope(EventScope scope)
            {
                lock (_lock)
                {
                    for (int i = _subscriptions.Count - 1; i >= 0; i--)
                    {
                        if (_subscriptions[i].Scope == scope)
                        {
                            _subscriptions.RemoveAt(i);
                        }
                    }
                }
            }
        }

        // --- EventSystem Core ---

        private readonly Dictionary<Type, object> _buses = new Dictionary<Type, object>();
        private readonly object _busesLock = new object();

        private EventBus<T> GetBus<T>()
        {
            Type type = typeof(T);
            lock (_busesLock)
            {
                if (!_buses.TryGetValue(type, out object bus))
                {
                    bus = new EventBus<T>();
                    _buses[type] = bus;
                }
                return (EventBus<T>)bus;
            }
        }

        // --- Public API ---

        /// <summary>
        /// Subscribe to an event of type T. Default scope is Global.
        /// For MonoBehaviours, prefer the extension method this.Subscribe().
        /// </summary>
        public void Subscribe<T>(Action<T> handler, EventScope scope = EventScope.Global)
        {
            GetBus<T>().Subscribe(handler, scope);
        }

        /// <summary>
        /// Unsubscribe from an event of type T.
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            GetBus<T>().Unsubscribe(handler);
        }

        /// <summary>
        /// Emit an event of type T to all subscribers.
        /// </summary>
        public void Emit<T>(T args)
        {
            GetBus<T>().Emit(args);
        }

        /// <summary>
        /// Remove all subscriptions for a given scope.
        /// Call this on session start/end or scene transitions.
        /// </summary>
        public void ClearScope(EventScope scope)
        {
            lock (_busesLock)
            {
                foreach (var bus in _buses.Values)
                {
                    (bus as IEventBus)?.ClearScope(scope);
                }
            }
        }

        /// <summary>
        /// Remove ALL subscriptions across all scopes and event types. Use with caution.
        /// </summary>
        public void ClearAll()
        {
            lock (_busesLock)
            {
                _buses.Clear();
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
        }
    }
}
