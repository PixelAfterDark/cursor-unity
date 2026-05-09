using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Cursor.Upgrades;

namespace Cursor.UI
{
    [RequireComponent(typeof(Button))]
    public class UpgradeNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UpgradeNodeSO _nodeData;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color _lockedColor = Color.gray;
        [SerializeField] private Color _unlockedColor = Color.white;
        [SerializeField] private Color _affordableColor = Color.green;
        [SerializeField] private Color _maxedColor = Color.yellow;

        private Button _button;
        private UpgradeSystem _upgradeSystem;

        public UpgradeNodeSO NodeData => _nodeData;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
            _upgradeSystem = UpgradeSystem.Instance;
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        public void Refresh()
        {
            if (_nodeData == null) return;
            if (_upgradeSystem == null) _upgradeSystem = UpgradeSystem.Instance;
            if (_upgradeSystem == null) return;

            var state = _upgradeSystem.GetState(_nodeData.UpgradeId);
            if (state == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // Visibility: hidden if not unlocked and level 0
            bool visible = state.IsUnlocked || state.CurrentLevel > 0;
            gameObject.SetActive(visible);

            if (!visible) return;

            // Icon
            if (_iconImage != null && _nodeData.Icon != null)
                _iconImage.sprite = _nodeData.Icon;

            // Interactivity & Color
            bool isMaxed = state.CurrentLevel >= _nodeData.MaxLevel;
            bool canAfford = _upgradeSystem.CanAfford(_nodeData.UpgradeId);
            bool canUpgrade = state.IsUnlocked && !isMaxed && canAfford;

            _button.interactable = canUpgrade;

            Color targetColor;
            if (isMaxed) targetColor = _maxedColor;
            else if (canUpgrade) targetColor = _affordableColor;
            else if (state.IsUnlocked) targetColor = _unlockedColor;
            else targetColor = _lockedColor;

            if (_backgroundImage != null)
                _backgroundImage.color = targetColor;
        }

        private void OnClick()
        {
            if (_nodeData == null || _upgradeSystem == null) return;
            _upgradeSystem.TryPurchaseUpgrade(_nodeData.UpgradeId);
            // Refresh is handled by UpgradeTreeView listening to OnUpgradeStateChanged
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            UpgradeTreeView.Instance?.ShowTooltip(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UpgradeTreeView.Instance?.HideTooltip();
        }
    }
}
