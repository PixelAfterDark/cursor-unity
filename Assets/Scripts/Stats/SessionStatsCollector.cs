using UnityEngine;
using Cursor.Core;
using Cursor.Gameplay;

namespace Cursor.Stats
{
    /// <summary>
    /// Collects per-session statistics during Gameplay state.
    /// Resets automatically when entering Gameplay and freezes when leaving.
    /// </summary>
    public class SessionStatsCollector : MonoBehaviour
    {
        public SessionStats CurrentStats;

        private bool _isCollecting;

        private void OnEnable()
        {
            this.Subscribe<GameStateChangedEventArgs>(OnGameStateChanged);
            this.Subscribe<EnemyKilledEventArgs>(OnEnemyKilled);
            this.Subscribe<CollectablePickedUpEventArgs>(OnCollectablePickedUp);
        }

        private void OnDisable()
        {
            this.Unsubscribe<GameStateChangedEventArgs>(OnGameStateChanged);
            this.Unsubscribe<EnemyKilledEventArgs>(OnEnemyKilled);
            this.Unsubscribe<CollectablePickedUpEventArgs>(OnCollectablePickedUp);
        }

        private void OnGameStateChanged(GameStateChangedEventArgs args)
        {
            if (args.State == GameState.Gameplay)
            {
                CurrentStats = default;
                CurrentStats.SessionTime = 0f;
                _isCollecting = true;
            }
            else if (args.State == GameState.Summary)
            {
                _isCollecting = false;
                CurrentStats.SessionTime = GameManager.Instance?.SessionTime ?? 0f;
            }
            else
            {
                _isCollecting = false;
            }
        }

        private void OnEnemyKilled(EnemyKilledEventArgs args)
        {
            if (!_isCollecting) return;
            CurrentStats.TotalKills++;
        }

        private void OnCollectablePickedUp(CollectablePickedUpEventArgs args)
        {
            if (!_isCollecting) return;

            switch (args.Type)
            {
                case CollectableType.A:
                    CurrentStats.Currency_A_Earned += args.Amount;
                    break;
                case CollectableType.B:
                    CurrentStats.Currency_B_Earned += args.Amount;
                    break;
                case CollectableType.C:
                    CurrentStats.Currency_C_Earned += args.Amount;
                    break;
                case CollectableType.D:
                    CurrentStats.Currency_D_Earned += args.Amount;
                    break;
            }
        }
    }
}
