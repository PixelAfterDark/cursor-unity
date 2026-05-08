using UnityEngine;
using UnityEngine.UI;

namespace Cursor.UI
{
    /// <summary>
    /// Controls the MainMenu scene. Wires buttons and handles Continue visibility.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            FindAndFixButtons();
            EnsureContinueButton();
        }

        private void Start()
        {
            if (continueButton != null)
            {
                bool hasSave = Core.SaveSystem.Instance?.SaveExists() ?? false;
                continueButton.interactable = hasSave;
            }
        }

        /// <summary>
        /// Finds buttons by name and overrides their OnClick listeners at runtime.
        /// This repairs any broken inspector references safely.
        /// </summary>
        private void FindAndFixButtons()
        {
            if (startGameButton == null)
                startGameButton = transform.Find("StartGame")?.GetComponent<Button>();
            if (optionsButton == null)
                optionsButton = transform.Find("Options")?.GetComponent<Button>();
            if (creditsButton == null)
                creditsButton = transform.Find("Credits")?.GetComponent<Button>();
            if (quitButton == null)
                quitButton = transform.Find("Quit")?.GetComponent<Button>();

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }
            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveAllListeners();
                optionsButton.onClick.AddListener(OnOptionsClicked);
            }
            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveAllListeners();
                creditsButton.onClick.AddListener(OnCreditsClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        /// <summary>
        /// Creates a Continue button dynamically if it doesn't exist in the scene.
        /// </summary>
        private void EnsureContinueButton()
        {
            if (continueButton != null) return;

            var existing = transform.Find("Continue");
            if (existing != null)
            {
                continueButton = existing.GetComponent<Button>();
                if (continueButton != null)
                {
                    continueButton.onClick.RemoveAllListeners();
                    continueButton.onClick.AddListener(OnContinueClicked);
                }
                return;
            }

            if (startGameButton == null) return;

            // Clone StartGame button and place it above
            GameObject go = Instantiate(startGameButton.gameObject, transform);
            go.name = "Continue";
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y + 80);
            }

            var txt = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (txt != null) txt.text = "Continue";

            continueButton = go.GetComponent<Button>();
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        // --- Button handlers ---

        public void OnStartGameClicked()
        {
            Core.GameManager.Instance?.StartNewGame();
        }

        public void OnContinueClicked()
        {
            Core.GameManager.Instance?.ContinueGame();
        }

        public void OnOptionsClicked()
        {
            Debug.Log("[MainMenu] Options - not implemented yet.");
        }

        public void OnCreditsClicked()
        {
            Debug.Log("[MainMenu] Credits - not implemented yet.");
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
