using UnityEngine;

namespace Cursor.Core
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviour-based systems.
    /// Ensures only one instance exists, persists across scene loads,
    /// and destroys any duplicate instances automatically.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject(typeof(T).Name);
                            _instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Returns true if the singleton instance has been created.
        /// Safe to use in OnDestroy without triggering instance creation.
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate instance of '{typeof(T)}' detected. Destroying: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            OnAwake();
        }

        /// <summary>
        /// Override this instead of Awake() in derived classes.
        /// </summary>
        protected virtual void OnAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
