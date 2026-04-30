using UnityEngine;

namespace Cursor.Pool
{
    /// <summary>
    /// Non-generic interface for object pools so they can be stored in a mixed dictionary.
    /// </summary>
    public interface IPool
    {
        /// <summary>
        /// Retrieves an active instance from the pool. Instantiates a new one if empty.
        /// </summary>
        MonoBehaviour Get();

        /// <summary>
        /// Returns an instance to the pool (deactivates it).
        /// </summary>
        void Return(MonoBehaviour obj);

        /// <summary>
        /// Pre-instantiates a number of inactive instances for the pool.
        /// </summary>
        void Prewarm(int count);
    }
}
