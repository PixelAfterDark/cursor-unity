using UnityEngine;
using Cursor.Core;

namespace Cursor.UI
{
    /// <summary>
    /// Manages UI panels on GameScene based on game state.
    /// Subscribe to GameStateChangedEventArgs to toggle Upgrade/Gameplay/Summary panels.
    /// Provides public methods for UI button OnClick events.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject summaryPanel;

        private void OnEnable()
        {
            this.Subscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            this.Unsubscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEventArgs args)
        {
            switch (args.State)
            {
                case GameState.Upgrade:
                    upgradePanel?.SetActive(true);
                    gameplayPanel?.SetActive(false);
                    summaryPanel?.SetActive(false);
                    break;

                case GameState.Gameplay:
                    upgradePanel?.SetActive(false);
                    gameplayPanel?.SetActive(true);
                    summaryPanel?.SetActive(false);
                    break;

                case GameState.Summary:
                    upgradePanel?.SetActive(false);
                    gameplayPanel?.SetActive(false);
                    summaryPanel?.SetActive(true);
                    break;
            }
        }

        // --- Button handlers (wire in Inspector OnClick) ---

        public void OnPlayButtonClicked()
        {
            GameManager.Instance?.StartSession();
        }

        public void OnStopButtonClicked()
        {
            GameManager.Instance?.StopSession();
        }

        public void OnUpgradeButtonClicked()
        {
            GameManager.Instance?.GoToUpgrade();
        }

        public void OnGameButtonClicked()
        {
            GameManager.Instance?.StartSession();
        }
    }
}
