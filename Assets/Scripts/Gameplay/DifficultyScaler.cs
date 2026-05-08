using System.Collections;
using UnityEngine;
using Cursor.Core;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Skaluje statystyki enemy w czasie trwania sesji.
    /// Subskrybuje OnGameStateChanged — reset przy starcie Gameplay.
    /// Co difficultyTickInterval sekund przelicza mnożniki na podstawie sessionTime.
    /// </summary>
    public class DifficultyScaler : MonoBehaviour
    {
        [Header("Tick Settings")]
        [SerializeField] private float difficultyTickInterval = 30f;
        [SerializeField] private float scaleDivisor = 120f;

        [Header("Caps")]
        [SerializeField] private float hpCap = 10f;
        [SerializeField] private float speedCap = 2.0f;
        [SerializeField] private float dmgCap = 5f;

        private float _difficultyMultiplier = 1f;
        private Coroutine _tickCoroutine;

        public float HpMultiplier => Mathf.Min(_difficultyMultiplier, hpCap);
        public float SpeedMultiplier => Mathf.Min(_difficultyMultiplier, speedCap);
        public float DmgMultiplier => Mathf.Min(_difficultyMultiplier, dmgCap);

        private void Awake()
        {
            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        private void OnEnable()
        {
            this.Subscribe<Core.GameStateChangedEventArgs>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            this.Unsubscribe<Core.GameStateChangedEventArgs>(OnGameStateChanged);
        }

        private void OnGameStateChanged(Core.GameStateChangedEventArgs args)
        {
            switch (args.State)
            {
                case Core.GameState.Gameplay:
                    ResetAndStart();
                    break;

                case Core.GameState.Summary:
                case Core.GameState.Upgrade:
                    StopTick();
                    break;
            }
        }

        private void ResetAndStart()
        {
            _difficultyMultiplier = 1f;
            StopTick();
            _tickCoroutine = StartCoroutine(TickRoutine());
        }

        private void StopTick()
        {
            if (_tickCoroutine != null)
            {
                StopCoroutine(_tickCoroutine);
                _tickCoroutine = null;
            }
        }

        private IEnumerator TickRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(difficultyTickInterval);

                if (Core.GameManager.Instance == null)
                    yield break;

                float sessionTime = Core.GameManager.Instance.SessionTime;
                _difficultyMultiplier = 1f + sessionTime / scaleDivisor;

                Debug.Log(
                    $"[DifficultyScaler] Tick @ {sessionTime:F1}s | " +
                    $"Multiplier={_difficultyMultiplier:F2} | " +
                    $"HP={HpMultiplier:F2} | DMG={DmgMultiplier:F2} | SPD={SpeedMultiplier:F2}");
            }
        }
    }
}
