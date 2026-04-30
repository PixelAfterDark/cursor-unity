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
            void ClearAll();
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
            private int _emitCount = 0;
            private const int CleanupInterval = 32;

            public void Subscribe(Action<T> handler, EventScope scope)
            {
                if (handler == null) return;
                _subscriptions.Add(new Subscription(handler, scope));
            }

            public void Unsubscribe(Action<T> handler)
            {
                if (handler == null) return;

                for (int i = _subscriptions.Count - 1; i >= 0; i--)
                {
                    if (_subscriptions[i].Handler == handler)
                    {
                        _subscriptions.RemoveAt(i);
                    }
                }
            }

            public void Emit(T args)
            {
                _emitCount++;
                bool shouldCleanup = (_emitCount % CleanupInterval) == 0;

                // Iterate backwards so we can safely remove dead subscriptions inline.
                // Unity is single-threaded, so no snapshot/lock is needed.
                for (int i = _subscriptions.Count - 1; i >= 0; i--)
                {
                    var sub = _subscriptions[i];
                    if (sub.Handler == null)
                    {
                        _subscriptions.RemoveAt(i);
                        continue;
                    }

                    // Periodic auto-cleanup: remove delegates targeting destroyed MonoBehaviours
                    if (shouldCleanup && sub.Handler.Target is UnityEngine.Object unityObj && unityObj == null)
                    {
                        _subscriptions.RemoveAt(i);
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
            }

            public void ClearScope(EventScope scope)
            {
                for (int i = _subscriptions.Count - 1; i >= 0; i--)
                {
                    if (_subscriptions[i].Scope == scope)
                    {
                        _subscriptions.RemoveAt(i);
                    }
                }
            }

            public void ClearAll()
            {
                _subscriptions.Clear();
                _emitCount = 0;
            }
        }

        // --- EventSystem Core ---

        private readonly Dictionary<Type, object> _buses = new Dictionary<Type, object>();

        private EventBus<T> GetBus<T>()
        {
            Type type = typeof(T);
            if (!_buses.TryGetValue(type, out object bus))
            {
                bus = new EventBus<T>();
                _buses[type] = bus;
            }
            return (EventBus<T>)bus;
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
            foreach (var bus in _buses.Values)
            {
                (bus as IEventBus)?.ClearScope(scope);
            }
        }

        /// <summary>
        /// Remove ALL subscriptions across all scopes and event types. Use with caution.
        /// </summary>
        public void ClearAll()
        {
            foreach (var bus in _buses.Values)
            {
                (bus as IEventBus)?.ClearAll();
            }
            _buses.Clear();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
        }
    }
}
