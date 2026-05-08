using UnityEngine;
using TMPro;
using Cursor.Core;

namespace Cursor.UI
{
    /// <summary>
    /// Upgrade panel placeholder. Displays currency counters and navigation buttons.
    /// Full upgrade tree will be added in Stage 7.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        [Header("Currencies")]
        [SerializeField] private TextMeshProUGUI currencyAText;
        [SerializeField] private TextMeshProUGUI currencyBText;
        [SerializeField] private TextMeshProUGUI currencyCText;
        [SerializeField] private TextMeshProUGUI currencyDText;

        private Stats.StatsSystem _stats;

        private void Awake()
        {
            EnsureUiElements();
        }

        private void OnEnable()
        {
            _stats = Core.SystemsManager.Instance?.StatsSystem;
            if (_stats != null)
                _stats.OnStatChanged += OnStatChanged;

            RefreshCurrencies();
        }

        private void OnDisable()
        {
            if (_stats != null)
                _stats.OnStatChanged -= OnStatChanged;
        }

        private void EnsureUiElements()
        {
            if (currencyAText == null)
            {
                currencyAText = CreateCurrencyText("CurrencyA", 0);
                currencyAText.text = "A: 0";
            }
            if (currencyBText == null)
            {
                currencyBText = CreateCurrencyText("CurrencyB", 1);
                currencyBText.text = "B: 0";
            }
            if (currencyCText == null)
            {
                currencyCText = CreateCurrencyText("CurrencyC", 2);
                currencyCText.text = "C: 0";
            }
            if (currencyDText == null)
            {
                currencyDText = CreateCurrencyText("CurrencyD", 3);
                currencyDText.text = "D: 0";
            }
        }

        private TextMeshProUGUI CreateCurrencyText(string name, int index)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.fontSize = 24;
            txt.color = Color.white;
            RectTransform rt = txt.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20 - index * 35);
            rt.sizeDelta = new Vector2(200, 30);
            return txt;
        }

        private void OnStatChanged(Stats.StatType stat, float value)
        {
            if (stat == Stats.StatType.Currency_A_Count ||
                stat == Stats.StatType.Currency_B_Count ||
                stat == Stats.StatType.Currency_C_Count ||
                stat == Stats.StatType.Currency_D_Count)
            {
                RefreshCurrencies();
            }
        }

        private void RefreshCurrencies()
        {
            if (_stats == null) return;

            if (currencyAText != null) currencyAText.text = "A: " + _stats.GetStat(Stats.StatType.Currency_A_Count).ToString();
            if (currencyBText != null) currencyBText.text = "B: " + _stats.GetStat(Stats.StatType.Currency_B_Count).ToString();
            if (currencyCText != null) currencyCText.text = "C: " + _stats.GetStat(Stats.StatType.Currency_C_Count).ToString();
            if (currencyDText != null) currencyDText.text = "D: " + _stats.GetStat(Stats.StatType.Currency_D_Count).ToString();
        }

        // --- Button handlers ---

        public void OnStartSessionButtonClicked()
        {
            GameManager.Instance?.StartSession();
        }

        public void OnMainMenuButtonClicked()
        {
            GameManager.Instance?.LoadMainMenu();
        }
    }
}
