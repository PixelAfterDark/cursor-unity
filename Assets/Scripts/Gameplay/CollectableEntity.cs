using UnityEngine;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Component attached to collectable prefabs. Acts as a bridge between the pooled GameObject
    /// and the CollectableController's runtime data list.
    /// </summary>
    public class CollectableEntity : MonoBehaviour
    {
        public SpriteRenderer SpriteRenderer { get; private set; }

        private void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
