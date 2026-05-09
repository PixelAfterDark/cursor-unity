using System.Collections.Generic;
using UnityEngine;
using Cursor.Core;
using Cursor.Upgrades;

namespace Cursor.UI
{
    public class UpgradeTreeView : MonoBehaviour
    {
        public static UpgradeTreeView Instance { get; private set; }

        [SerializeField] private UpgradeDetailTooltip _tooltip;
        [SerializeField] private UpgradeNodeUI[] _nodeUIs;
        [SerializeField] private UpgradeConnectionLine[] _connectionLines;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void Start()
        {
            var upgradeSystem = UpgradeSystem.Instance;
            if (upgradeSystem != null)
            {
                var nodes = new List<UpgradeNodeSO>();

                foreach (var ui in _nodeUIs)
                {
                    if (ui == null || ui.NodeData == null) continue;
                    if (!nodes.Contains(ui.NodeData))
                        nodes.Add(ui.NodeData);
                }

                upgradeSystem.SetNodeDefinitions(nodes);
                upgradeSystem.OnUpgradeStateChanged += OnUpgradeStateChanged;
            }

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            var upgradeSystem = UpgradeSystem.Instance;
            if (upgradeSystem != null)
                upgradeSystem.OnUpgradeStateChanged -= OnUpgradeStateChanged;
        }

        private void OnUpgradeStateChanged(string upgradeId)
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            if (_nodeUIs != null)
            {
                foreach (var node in _nodeUIs)
                {
                    node?.Refresh();
                }
            }

            if (_connectionLines != null)
            {
                foreach (var line in _connectionLines)
                {
                    line?.Refresh();
                }
            }
        }

        public void ShowTooltip(UpgradeNodeUI node)
        {
            _tooltip?.Show(node);
        }

        public void HideTooltip()
        {
            _tooltip?.Hide();
        }
    }
}
