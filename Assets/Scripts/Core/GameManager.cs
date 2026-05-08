using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cursor.Core
{
    /// <summary>
    /// Central game orchestrator. Singleton, DontDestroyOnLoad.
    /// Manages game state machine (Menu -> Upgrade -> Gameplay -> Summary)
    /// and scene transitions. All state side-effects are emitted via EventSystem.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>
        /// Current game state. Read-only from outside; use ChangeState() to modify.
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.Menu;

        /// <summary>
        /// Czas trwania aktualnej sesji (sekundy). Resetowany przy starcie Gameplay.
        /// </summary>
        public float SessionTime { get; private set; } = 0f;

        private bool _isTransitioning = false;

        // --- State Machine ---

        /// <summary>
        /// Transition to a new game state. Emits OnGameStateChanged event.
        /// Guards against re-entrant calls from within event handlers.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[GameManager] Re-entrant state change attempted during transition to {newState}. Ignored.");
                return;
            }

            if (CurrentState == newState)
            {
                Debug.LogWarning($"[GameManager] Attempted to change to same state: {newState}");
                return;
            }

            GameState previousState = CurrentState;
            CurrentState = newState;
            _isTransitioning = true;

            if (newState == GameState.Gameplay)
            {
                SessionTime = 0f;
                SystemsManager.Instance?.StatsSystem?.ResetSessionStats();
            }

            if (newState == GameState.Summary)
            {
                SaveSystem.Instance?.Save();
            }

            Debug.Log($"[GameManager] State changed: {previousState} -> {newState}");

            EventSystem.Instance.Emit(new GameStateChangedEventArgs
            {
                State = newState
            });

            _isTransitioning = false;
        }

        // --- Navigation Helpers ---

        /// <summary>
        /// Load MainMenu scene and set state to Menu.
        /// Saves current progress before leaving.
        /// </summary>
        public void LoadMainMenu()
        {
            SaveSystem.Instance?.Save();
            SceneManager.LoadScene("MainMenu");
            ChangeState(GameState.Menu);
        }

        /// <summary>
        /// Start a new game: delete save, reset stats, load GameScene.
        /// State will auto-transition to Upgrade.
        /// </summary>
        public void StartNewGame()
        {
            SaveSystem.Instance?.DeleteSave();
            SystemsManager.Instance?.StatsSystem?.InitializeDefaults();
            SceneManager.LoadScene("GameScene");
            // State -> Upgrade is handled in OnSceneLoaded when GameScene finishes loading
        }

        /// <summary>
        /// Continue a saved game: load save data, then load GameScene.
        /// </summary>
        public void ContinueGame()
        {
            var saveData = SaveSystem.Instance?.Load();
            if (saveData != null)
            {
                SystemsManager.Instance?.StatsSystem?.LoadFrom(saveData);
            }
            SceneManager.LoadScene("GameScene");
            // State -> Upgrade is handled in OnSceneLoaded when GameScene finishes loading
        }

        /// <summary>
        /// Start a gameplay session from Upgrade view.
        /// </summary>
        public void StartSession()
        {
            ChangeState(GameState.Gameplay);
        }

        /// <summary>
        /// Manually stop the current session and go to Summary.
        /// </summary>
        public void StopSession()
        {
            ChangeState(GameState.Summary);
        }

        /// <summary>
        /// Return from Summary to Upgrade view.
        /// </summary>
        public void GoToUpgrade()
        {
            ChangeState(GameState.Upgrade);
        }

        // --- Scene Loading Handling ---

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "GameScene" && CurrentState != GameState.Upgrade)
            {
                // GameScene always starts in Upgrade view (player upgrades before session)
                ChangeState(GameState.Upgrade);
            }
        }

        // --- Event Listeners ---

        private void OnPlayerDead(PlayerDeadEventArgs args)
        {
            if (CurrentState == GameState.Gameplay)
            {
                ChangeState(GameState.Summary);
            }
        }

        private void OnSessionStop(SessionStopEventArgs args)
        {
            if (CurrentState == GameState.Gameplay)
            {
                ChangeState(GameState.Summary);
            }
        }

        // --- Lifecycle ---

        private void Update()
        {
            if (CurrentState == GameState.Gameplay)
            {
                SessionTime += Time.deltaTime;
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            // Set initial state to Menu (we assume the game starts from MainMenu scene)
            CurrentState = GameState.Menu;

            // Subscribe to global events
            EventSystem.Instance.Subscribe<PlayerDeadEventArgs>(OnPlayerDead, EventScope.Global);
            EventSystem.Instance.Subscribe<SessionStopEventArgs>(OnSessionStop, EventScope.Global);

            // Listen for scene loads to auto-set state after transition
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log($"[GameManager] Initialized. Current state: {CurrentState}");
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Defensive: destruction order during app quit is undefined.
            var eventSystem = EventSystem.Instance;
            if (eventSystem != null)
            {
                eventSystem.Unsubscribe<PlayerDeadEventArgs>(OnPlayerDead);
                eventSystem.Unsubscribe<SessionStopEventArgs>(OnSessionStop);
            }
        }
    }
}
