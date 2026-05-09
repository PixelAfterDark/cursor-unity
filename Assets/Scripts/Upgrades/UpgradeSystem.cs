using System;
using System.Collections.Generic;
using UnityEngine;
using Cursor.Core;
using Cursor.Stats;

namespace Cursor.Upgrades
{
    [DefaultExecutionOrder(-70)]
    public class UpgradeSystem : Singleton<UpgradeSystem>
    {
        [SerializeField] private List<UpgradeNodeSO> _allNodes = new List<UpgradeNodeSO>();

        private UpgradeNodeSO _rootNode;

        private readonly Dictionary<string, UpgradeRuntimeState> _runtimeStates = new();
        private List<UpgradeSaveEntry> _pendingSaveEntries;

        public IReadOnlyList<UpgradeNodeSO> AllNodes => _allNodes;
        public event Action<string> OnUpgradeStateChanged;

        protected override void OnAwake()
        {
            base.OnAwake();
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        /// <summary>
        /// Registers the node definitions and initializes runtime states.
        /// Does NOT reset runtime state if the provided definitions are identical
        /// to the already-registered ones AND runtime state already exists.
        /// Applies any pending save data after (re-)initialization.
        /// </summary>
        public void SetNodeDefinitions(List<UpgradeNodeSO> nodes)
        {
            if (nodes == null) return;

            bool same = _allNodes != null && _allNodes.Count == nodes.Count;
            if (same)
            {
                for (int i = 0; i < _allNodes.Count; i++)
                {
                    if (_allNodes[i] != nodes[i])
                    {
                        same = false;
                        break;
                    }
                }
            }

            if (!same || _runtimeStates.Count == 0)
            {
                _allNodes = new List<UpgradeNodeSO>(nodes);
                InitializeRuntimeStates();

                // Apply pending save if loaded before definitions were set
                if (_pendingSaveEntries != null && _pendingSaveEntries.Count > 0)
                {
                    ApplySaveEntries(_pendingSaveEntries);
                }
                _pendingSaveEntries = null;
            }

            // Always notify UI to refresh, regardless of whether definitions changed.
            OnUpgradeStateChanged?.Invoke(null);
        }

        private void InitializeRuntimeStates()
        {
            _runtimeStates.Clear();
            _rootNode = null;

            foreach (var node in _allNodes)
            {
                if (node == null) continue;
                _runtimeStates[node.UpgradeId] = new UpgradeRuntimeState
                {
                    CurrentLevel = 0,
                    IsUnlocked = false
                };

                if (node.IsRoot)
                    _rootNode = node;
            }

            if (_rootNode != null && _runtimeStates.TryGetValue(_rootNode.UpgradeId, out var rootState))
            {
                rootState.IsUnlocked = true;
            }
        }

        public UpgradeRuntimeState GetState(string upgradeId)
        {
            _runtimeStates.TryGetValue(upgradeId, out var state);
            return state;
        }

        public bool CanAfford(string upgradeId)
        {
            if (!_runtimeStates.TryGetValue(upgradeId, out var state)) return false;
            if (!state.IsUnlocked) return false;

            var node = FindNode(upgradeId);
            if (node == null) return false;
            if (state.CurrentLevel >= node.MaxLevel) return false;

            int cost = node.CostsPerLevel[state.CurrentLevel];
            var stats = SystemsManager.Instance?.StatsSystem;
            if (stats == null) return false;

            return stats.GetStat(node.CostCurrency) >= cost;
        }

        public bool TryPurchaseUpgrade(string upgradeId)
        {
            if (!_runtimeStates.TryGetValue(upgradeId, out var state))
            {
                Debug.Log($"[UpgradeSystem] Unknown upgradeId: {upgradeId}");
                return false;
            }

            var node = FindNode(upgradeId);
            if (node == null)
            {
                Debug.Log($"[UpgradeSystem] Node not found for id: {upgradeId}");
                return false;
            }

            // Validation
            if (!state.IsUnlocked)
            {
                Debug.Log($"[UpgradeSystem] {upgradeId} is locked.");
                return false;
            }

            if (state.CurrentLevel >= node.MaxLevel)
            {
                Debug.Log($"[UpgradeSystem] {upgradeId} is already max level ({node.MaxLevel}).");
                return false;
            }

            int cost = node.CostsPerLevel[state.CurrentLevel];
            var stats = SystemsManager.Instance?.StatsSystem;
            if (stats == null) return false;

            if (!stats.SpendCurrency(node.CostCurrency, cost))
            {
                Debug.Log($"[UpgradeSystem] Cannot afford {upgradeId}. Cost: {cost} {node.CostCurrency}");
                return false;
            }

            // Apply upgrade
            float value = node.ValuesPerLevel[state.CurrentLevel];
            state.CurrentLevel++;
            stats.ModifyStat(node.TargetStat, value);

            // Unlock children on first purchase (level reaches 1)
            if (state.CurrentLevel == 1)
            {
                foreach (var child in node.UnlocksOnLevel1)
                {
                    if (child != null && _runtimeStates.TryGetValue(child.UpgradeId, out var childState))
                    {
                        childState.IsUnlocked = true;
                    }
                }
            }

            EventSystem.Instance?.Emit(new UpgradePurchasedEventArgs { UpgradeId = upgradeId });
            OnUpgradeStateChanged?.Invoke(upgradeId);

            Debug.Log($"[UpgradeSystem] Purchased {upgradeId} level {state.CurrentLevel}. Added {value} to {node.TargetStat}");
            return true;
        }

        private UpgradeNodeSO FindNode(string upgradeId)
        {
            foreach (var node in _allNodes)
            {
                if (node != null && node.UpgradeId == upgradeId) return node;
            }
            return null;
        }

        // --- Save / Load ---

        public List<UpgradeSaveEntry> GetSaveData()
        {
            var list = new List<UpgradeSaveEntry>();
            foreach (var kvp in _runtimeStates)
            {
                list.Add(new UpgradeSaveEntry
                {
                    id = kvp.Key,
                    level = kvp.Value.CurrentLevel,
                    isUnlocked = kvp.Value.IsUnlocked
                });
            }
            return list;
        }

        public void LoadFrom(SaveData saveData)
        {
            if (saveData == null) return;

            var entries = saveData.upgrades != null
                ? new List<UpgradeSaveEntry>(saveData.upgrades)
                : null;

            // If definitions are already known, apply immediately.
            // Otherwise defer until SetNodeDefinitions / Start runs.
            if (_allNodes != null && _allNodes.Count > 0)
            {
                ApplySaveEntries(entries);
            }
            else
            {
                _pendingSaveEntries = entries;
            }
        }

        private void ApplySaveEntries(List<UpgradeSaveEntry> entries)
        {
            InitializeRuntimeStates();

            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (_runtimeStates.TryGetValue(entry.id, out var state))
                {
                    state.CurrentLevel = entry.level;
                    state.IsUnlocked = entry.isUnlocked;
                }
            }

            OnUpgradeStateChanged?.Invoke(null); // null = refresh all
        }

        /// <summary>
        /// Resets all upgrades to default (level 0, only root unlocked).
        /// Does NOT revert stats — caller should reinitialize StatsSystem.
        /// </summary>
        public void ResetToDefaults()
        {
            InitializeRuntimeStates();
            OnUpgradeStateChanged?.Invoke(null);
        }
    }
}
