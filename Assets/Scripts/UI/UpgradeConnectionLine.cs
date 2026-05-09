using UnityEngine;
using UnityEngine.UI;
using Cursor.Upgrades;

namespace Cursor.UI
{
    public class UpgradeConnectionLine : MonoBehaviour
    {
        [SerializeField] private UpgradeNodeUI _parentNode;
        [SerializeField] private UpgradeNodeUI _childNode;
        [SerializeField] private Image _lineImage;

        public void Refresh()
        {
            if (_childNode == null || _childNode.NodeData == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var system = UpgradeSystem.Instance;
            if (system == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var childState = system.GetState(_childNode.NodeData.UpgradeId);
            bool visible = childState != null && childState.IsUnlocked;
            gameObject.SetActive(visible);
        }
    }
}
