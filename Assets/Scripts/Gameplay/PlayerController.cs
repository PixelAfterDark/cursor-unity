using UnityEngine;
using UnityEngine.InputSystem;
using Cursor.Core;

namespace Cursor.Gameplay
{
    /// <summary>
    /// Player controller: follows mouse position 1:1, handles damage and HP.
    /// Emits OnPlayerDead when HP reaches zero. Resets HP at the start of each session.
    /// </summary>
    [DefaultExecutionOrder(-65)]
    public class PlayerController : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Force rendering on top of everything.")]
        [SerializeField] private int forcedSortingOrder = 100;

        // --- Runtime ---
        private Camera _cam;
        private float _currentHp;
        private bool _isDead;
        private bool _isGameplayActive;

        // --- Public Access ---
        public Vector2 Position => transform.position;
        public float CurrentHp => _currentHp;
        public bool IsDead => _isDead;

        /// <summary>
        /// Emitted when HP changes. Parameters: currentHp, maxHp.
        /// </summary>
        public event System.Action<float, float> OnHpChanged;

        // =========================================================
        // LIFECYCLE
        // =========================================================

        private void Awake()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                Debug.LogError("[PlayerController] Camera.main not found.");
            }

            // Ensure sprite renders on top
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = forcedSortingOrder;
            }

            Core.SystemsManager.Instance?.RegisterSystem(this);
        }

        private void Update()
        {
            if (!_isGameplayActive) return;
            if (_cam == null) return;
            if (Mouse.current == null) return;

            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = _cam.nearClipPlane;
            Vector3 worldPos = _cam.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f;

            // Clamp to viewport bounds
            float height = _cam.orthographicSize * 2f;
            float width = height * _cam.aspect;
            worldPos.x = Mathf.Clamp(worldPos.x, -width / 2f, width / 2f);
            worldPos.y = Mathf.Clamp(worldPos.y, -height / 2f, height / 2f);

            transform.position = worldPos;
        }

        private void OnEnable()
        {
            this.Subscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            this.Unsubscribe<GameStateChangedEventArgs>(OnGameStateChanged);
        }

        // =========================================================
        // HP / DAMAGE
        // =========================================================

        /// <summary>
        /// Resets HP to playerMaxHp from StatsSystem.
        /// Called automatically when Gameplay state starts.
        /// </summary>
        public void ResetHp()
        {
            var stats = Core.SystemsManager.Instance?.StatsSystem;
            float maxHp = stats != null ? stats.GetStat(Stats.StatType.PlayerMaxHp) : 100f;
            _currentHp = maxHp;
            _isDead = false;
            OnHpChanged?.Invoke(_currentHp, maxHp);
        }

        /// <summary>
        /// Applies damage reduced by playerDef. If HP drops to 0 or below, emits OnPlayerDead.
        /// </summary>
        public void TakeDamage(float dmg)
        {
            if (_isDead) return;

            var stats = Core.SystemsManager.Instance?.StatsSystem;
            float def = stats != null ? stats.GetStat(Stats.StatType.PlayerDef) : 0f;
            float actualDmg = Mathf.Max(0f, dmg - def);

            _currentHp -= actualDmg;

            float maxHp = stats != null ? stats.GetStat(Stats.StatType.PlayerMaxHp) : 100f;
            OnHpChanged?.Invoke(_currentHp, maxHp);

            if (_currentHp <= 0f)
            {
                _currentHp = 0f;
                _isDead = true;
                EventSystem.Instance.Emit(new PlayerDeadEventArgs());
            }
        }

        /// <summary>
        /// Heals the player up to max HP.
        /// </summary>
        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;

            var stats = Core.SystemsManager.Instance?.StatsSystem;
            float maxHp = stats != null ? stats.GetStat(Stats.StatType.PlayerMaxHp) : 100f;
            _currentHp = Mathf.Min(_currentHp + amount, maxHp);
            OnHpChanged?.Invoke(_currentHp, maxHp);
        }

        // =========================================================
        // EVENT HANDLERS
        // =========================================================

        private void OnGameStateChanged(GameStateChangedEventArgs args)
        {
            switch (args.State)
            {
                case GameState.Gameplay:
                    _isGameplayActive = true;
                    if (spriteRenderer != null) spriteRenderer.enabled = true;
                    ResetHp();
                    break;
                case GameState.Upgrade:
                case GameState.Summary:
                    _isGameplayActive = false;
                    if (spriteRenderer != null) spriteRenderer.enabled = false;
                    break;
            }
        }
    }
}
