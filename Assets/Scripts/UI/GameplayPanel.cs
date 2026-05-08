using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cursor.Core;

namespace Cursor.UI
{
    /// <summary>
    /// HUD shown during Gameplay state. Updates HP bar, session timer and currency counters.
    /// Creates missing UI elements dynamically in Awake so the scene works without manual wiring.
    /// </summary>
    public class GameplayPanel : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Image hpBackgroundImage;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Currencies")]
        [SerializeField] private TextMeshProUGUI currencyAText;
        [SerializeField] private TextMeshProUGUI currencyBText;
        [SerializeField] private TextMeshProUGUI currencyCText;
        [SerializeField] private TextMeshProUGUI currencyDText;

        private Stats.StatsSystem _stats;
        private Gameplay.PlayerController _player;

        private void Awake()
        {
            EnsureUiElements();
        }

        private void OnEnable()
        {
            _stats = Core.SystemsManager.Instance?.StatsSystem;
            _player = Core.SystemsManager.Instance?.PlayerController;

            if (_stats != null)
                _stats.OnStatChanged += OnStatChanged;

            RefreshCurrencies();
        }

        private void OnDisable()
        {
            if (_stats != null)
                _stats.OnStatChanged -= OnStatChanged;
        }

        private void Update()
        {
            UpdateHpBar();
            UpdateTimer();
        }

        // --- Dynamic UI creation ---

        private void EnsureUiElements()
        {
            // Timer (top-right)
            if (timerText == null)
            {
                GameObject go = new GameObject("TimerText");
                go.transform.SetParent(transform, false);
                timerText = go.AddComponent<TextMeshProUGUI>();
                timerText.alignment = TextAlignmentOptions.TopRight;
                timerText.fontSize = 28;
                timerText.color = Color.white;
                RectTransform rt = timerText.rectTransform;
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-20, -20);
                rt.sizeDelta = new Vector2(200, 40);
            }

            // HP Bar background (top-left)
            if (hpBackgroundImage == null)
            {
                GameObject go = new GameObject("HpBarBg");
                go.transform.SetParent(transform, false);
                hpBackgroundImage = go.AddComponent<Image>();
                hpBackgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                RectTransform rt = hpBackgroundImage.rectTransform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(20, -20);
                rt.sizeDelta = new Vector2(300, 24);
            }

            // HP Bar fill
            if (hpFillImage == null)
            {
                GameObject go = new GameObject("HpBarFill");
                go.transform.SetParent(hpBackgroundImage.transform, false);
                hpFillImage = go.AddComponent<Image>();
                hpFillImage.color = new Color(0.9f, 0.2f, 0.2f, 1f);
                RectTransform rt = hpFillImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            // Currencies (bottom-left, stacked)
            if (currencyAText == null) currencyAText = CreateCurrencyText("CurrencyA", 0);
            if (currencyBText == null) currencyBText = CreateCurrencyText("CurrencyB", 1);
            if (currencyCText == null) currencyCText = CreateCurrencyText("CurrencyC", 2);
            if (currencyDText == null) currencyDText = CreateCurrencyText("CurrencyD", 3);
        }

        private TextMeshProUGUI CreateCurrencyText(string name, int index)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.BottomLeft;
            txt.fontSize = 22;
            txt.color = Color.white;
            RectTransform rt = txt.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(20, 20 + index * 30);
            rt.sizeDelta = new Vector2(200, 30);
            return txt;
        }

        // --- Updates ---

        private void UpdateHpBar()
        {
            if (hpFillImage == null || _player == null || _stats == null) return;

            float maxHp = _stats.GetStat(Stats.StatType.PlayerMaxHp);
            float currentHp = _player.CurrentHp;
            float ratio = maxHp > 0f ? currentHp / maxHp : 0f;

            RectTransform rt = hpFillImage.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        private void UpdateTimer()
        {
            if (timerText == null || GameManager.Instance == null) return;

            float time = GameManager.Instance.SessionTime;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
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
    }
}
