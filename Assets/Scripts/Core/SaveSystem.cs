using UnityEngine;
using UnityEngine.UI;

namespace Cursor.Core
{
    /// <summary>
    /// Stub save system. Will be fully implemented in Stage 6.
    /// </summary>
    public class SaveSystem : Singleton<SaveSystem>
    {
        public bool SaveExists() => false;
    }
}
