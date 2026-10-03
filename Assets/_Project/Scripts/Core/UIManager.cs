using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;
using CodeDrive.UI;

namespace CodeDrive.Core
{
    /// <summary>
    /// Manages UI panels, interaction prompts, and overlay visibility.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels & Prompts")]
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private GameObject portfolioPanel;
        [SerializeField] private GameObject interactionPrompt;

        private PortfolioPanelUI _portfolioPanelUI;
        private Text _promptUiText;
        private TextMeshProUGUI _promptTmpText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CachePromptTextReferences();
            CachePortfolioPanelReferences();
        }

        private void Start()
        {
            HideAll();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPauseToggled += HandlePauseToggled;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPauseToggled -= HandlePauseToggled;
            }
        }

        private void CachePromptTextReferences()
        {
            if (interactionPrompt != null)
            {
                _promptTmpText = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>(true);
                _promptUiText = interactionPrompt.GetComponentInChildren<Text>(true);
            }
        }

        private void CachePortfolioPanelReferences()
        {
            if (portfolioPanel != null)
            {
                _portfolioPanelUI = portfolioPanel.GetComponent<PortfolioPanelUI>();
                if (_portfolioPanelUI == null)
                {
                    _portfolioPanelUI = portfolioPanel.AddComponent<PortfolioPanelUI>();
                }
            }
        }

        private void HideAll()
        {
            SetActive(pauseOverlay, false);
            SetActive(portfolioPanel, false);
            SetActive(interactionPrompt, false);
        }

        private void HandlePauseToggled()
        {
            if (GameManager.Instance == null) return;

            // If portfolio panel is open, do not show standard pause overlay
            if (IsPortfolioPanelOpen())
            {
                SetActive(pauseOverlay, false);
                return;
            }

            SetActive(pauseOverlay, GameManager.Instance.IsPaused);
        }

        /// <summary>
        /// Shows or hides the interaction prompt with dynamic text.
        /// </summary>
        public void ShowInteractionPrompt(bool show, string message = "")
        {
            if (interactionPrompt == null) return;

            if (show)
            {
                CachePromptTextReferences();

                string displayMsg = !string.IsNullOrEmpty(message) ? message : "Nhấn [E] để tương tác";
                if (_promptTmpText != null) _promptTmpText.text = displayMsg;
                if (_promptUiText != null) _promptUiText.text = displayMsg;

                SetActive(interactionPrompt, true);
            }
            else
            {
                SetActive(interactionPrompt, false);
            }
        }

        /// <summary>
        /// Opens the portfolio panel populated with information from the given area.
        /// </summary>
        public void OpenPortfolioPanel(PortfolioArea area = null)
        {
            CachePortfolioPanelReferences();

            if (_portfolioPanelUI != null && area != null)
            {
                _portfolioPanelUI.Populate(area);
            }

            SetActive(portfolioPanel, true);
            GameManager.Instance?.SetPaused(true);
        }

        public void OpenPortfolioPanel(PortfolioAreaType areaType, string label)
        {
            CachePortfolioPanelReferences();

            if (_portfolioPanelUI != null)
            {
                _portfolioPanelUI.Populate(areaType, label);
            }

            SetActive(portfolioPanel, true);
            GameManager.Instance?.SetPaused(true);
        }

        public void ClosePortfolioPanel()
        {
            SetActive(portfolioPanel, false);
            GameManager.Instance?.SetPaused(false);
        }

        public bool IsPortfolioPanelOpen()
        {
            return portfolioPanel != null && portfolioPanel.activeSelf;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
