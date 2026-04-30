using System;
using UnityEngine;

namespace Cursor.Core
{
    /// <summary>
    /// Extension methods for MonoBehaviour to simplify event subscription.
    /// Default scope is Session — safe for scene-bound objects.
    /// </summary>
    public static class EventSystemExtensions
    {
        /// <summary>
        /// Subscribe to an event of type T. Default scope: Session.
        /// Remember to call Unsubscribe in OnDisable/OnDestroy to avoid leaks.
        /// </summary>
        public static void Subscribe<T>(this MonoBehaviour monoBehaviour, Action<T> handler, EventScope scope = EventScope.Session)
        {
            EventSystem.Instance.Subscribe(handler, scope);
        }

        /// <summary>
        /// Unsubscribe from an event of type T.
        /// </summary>
        public static void Unsubscribe<T>(this MonoBehaviour monoBehaviour, Action<T> handler)
        {
            EventSystem.Instance.Unsubscribe(handler);
        }
    }
}
