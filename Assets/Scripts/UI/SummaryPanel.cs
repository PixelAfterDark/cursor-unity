using UnityEngine;
using TMPro;
using Cursor.Core;
using Cursor.Stats;

namespace Cursor.UI
{
    /// <summary>
    /// Summary panel shown after a session ends. Displays session stats.
    /// Creates missing UI elements dynamically in Awake.
    /// </summary>
    public class SummaryPanel : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI killsText;

        [Header("Currencies Earned")]
        [SerializeField] private TextMeshProUGUI currencyAText;
        [SerializeField] private TextMeshProUGUI currencyBText;
        [SerializeField] private TextMeshProUGUI currencyCText;
        [SerializeField] private TextMeshProUGUI currencyDText;

        private SessionStatsCollector _collector;

        private void Awake()
        {
            EnsureUiElements();
        }

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
            if (args.State == GameState.Summary)
            {
                Refresh();
            }
        }

        private void EnsureUiElements()
        {
            if (timeText == null)
            {
                timeText = CreateStatText("TimeText", 0);
                timeText.text = "Time: 00:00";
            }
            if (killsText == null)
            {
                killsText = CreateStatText("KillsText", 1);
                killsText.text = "Kills: 0";
            }
            if (currencyAText == null)
            {
                currencyAText = CreateStatText("CurrencyAText", 2);
                currencyAText.text = "Shards: 0";
            }
            if (currencyBText == null)
            {
                currencyBText = CreateStatText("CurrencyBText", 3);
                currencyBText.text = "Cores: 0";
            }
            if (currencyCText == null)
            {
                currencyCText = CreateStatText("CurrencyCText", 4);
                currencyCText.text = "Crystals: 0";
            }
            if (currencyDText == null)
            {
                currencyDText = CreateStatText("CurrencyDText", 5);
                currencyDText.text = "Essences: 0";
            }
        }

        private TextMeshProUGUI CreateStatText(string name, int index)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 24;
            txt.color = Color.white;
            RectTransform rt = txt.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -80 - index * 40);
            rt.sizeDelta = new Vector2(400, 35);
            return txt;
        }

        private void Refresh()
        {
            if (_collector == null)
                _collector = FindObjectOfType<SessionStatsCollector>();

            if (_collector == null)
            {
                Debug.LogWarning("[SummaryPanel] SessionStatsCollector not found in scene.");
                return;
            }

            SessionStats stats = _collector.CurrentStats;

            int minutes = Mathf.FloorToInt(stats.SessionTime / 60f);
            int seconds = Mathf.FloorToInt(stats.SessionTime % 60f);

            if (timeText != null) timeText.text = $"Time: {minutes:00}:{seconds:00}";
            if (killsText != null) killsText.text = $"Kills: {stats.TotalKills}";
            if (currencyAText != null) currencyAText.text = $"Shards: {stats.Currency_A_Earned}";
            if (currencyBText != null) currencyBText.text = $"Cores: {stats.Currency_B_Earned}";
            if (currencyCText != null) currencyCText.text = $"Crystals: {stats.Currency_C_Earned}";
            if (currencyDText != null) currencyDText.text = $"Essences: {stats.Currency_D_Earned}";
        }

        // --- Button handlers ---

        public void OnNewSessionButtonClicked()
        {
            GameManager.Instance?.StartSession();
        }

        public void OnUpgradeButtonClicked()
        {
            GameManager.Instance?.GoToUpgrade();
        }
    }
}
