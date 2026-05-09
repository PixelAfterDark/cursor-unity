using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cursor.Stats;
using Cursor.Upgrades;

namespace Cursor.UI
{
    public class UpgradeDetailTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _currentValueText;
        [SerializeField] private TextMeshProUGUI _nextValueText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Image _costIcon;

        [Header("Positioning")]
        [SerializeField] private Vector2 _offset = new Vector2(0, 60f);

        private void Awake()
        {
            if (_panel == null) _panel = GetComponent<RectTransform>();
            Hide();
        }

        public void Show(UpgradeNodeUI node)
        {
            if (node == null || node.NodeData == null) return;
            var data = node.NodeData;
            var system = UpgradeSystem.Instance;
            if (system == null) return;

            var state = system.GetState(data.UpgradeId);
            if (state == null) return;

            if (_nameText != null) _nameText.text = data.UpgradeName;
            if (_descriptionText != null) _descriptionText.text = data.Description;
            if (_levelText != null) _levelText.text = $"Level: {state.CurrentLevel} / {data.MaxLevel}";

            float currentStatValue = 0f;
            var stats = Core.SystemsManager.Instance?.StatsSystem;
            if (stats != null) currentStatValue = stats.GetStat(data.TargetStat);

            if (_currentValueText != null)
                _currentValueText.text = $"Current {data.TargetStat}: {currentStatValue:F1}";

            if (state.CurrentLevel < data.MaxLevel)
            {
                float nextBonus = data.ValuesPerLevel[state.CurrentLevel];
                if (_nextValueText != null)
                    _nextValueText.text = $"Next: +{nextBonus:F1} {data.TargetStat}";

                int cost = data.CostsPerLevel[state.CurrentLevel];
                if (_costText != null) _costText.text = $"{cost} {data.CostCurrency}";
                if (_costIcon != null) _costIcon.gameObject.SetActive(true);
            }
            else
            {
                if (_nextValueText != null) _nextValueText.text = "MAXED";
                if (_costText != null) _costText.text = "";
                if (_costIcon != null) _costIcon.gameObject.SetActive(false);
            }

            // Position above node
            RectTransform nodeRect = node.GetComponent<RectTransform>();
            if (nodeRect != null && _panel != null)
            {
                Vector3 worldPos = nodeRect.position + new Vector3(_offset.x, _offset.y, 0);
                _panel.position = worldPos;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
