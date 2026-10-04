using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;
using CodeDrive.UI;
using System.Collections.Generic;

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
        [SerializeField] private GameObject interactionPrompt;

        [Header("Portfolio Panels (assign one per AreaType)")]
        [Tooltip("About Me panel — attach AboutPanelUI component")]
        [SerializeField] private GameObject aboutPanel;
        [Tooltip("Skills panel — attach SkillsPanelUI component")]
        [SerializeField] private GameObject skillsPanel;
        [Tooltip("Education + Experience panel — attach EduExpPanelUI component")]
        [SerializeField] private GameObject eduExpPanel;
        [Tooltip("Projects panel — attach ProjectsPanelUI component")]
        [SerializeField] private GameObject projectsPanel;
        [Tooltip("Fallback panel for Contact / unhandled types — attach PortfolioPanelUI component")]
        [SerializeField] private GameObject fallbackPanel;

        private PortfolioPanelUI _fallbackPanelUI;
        private Text _promptUiText;
        private TextMeshProUGUI _promptTmpText;

        // Active panel tracked so ClosePortfolioPanel knows what to hide
        private GameObject _activePortfolioPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CachePromptTextReferences();
            CacheFallbackPanelReference();
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

        private void CacheFallbackPanelReference()
        {
            if (fallbackPanel != null)
            {
                _fallbackPanelUI = fallbackPanel.GetComponent<PortfolioPanelUI>();
                if (_fallbackPanelUI == null)
                    _fallbackPanelUI = fallbackPanel.AddComponent<PortfolioPanelUI>();
            }
        }

        private void HideAll()
        {
            SetActive(pauseOverlay,     false);
            SetActive(interactionPrompt, false);
            HideAllPortfolioPanels();
        }

        private void HideAllPortfolioPanels()
        {
            SetActive(aboutPanel,    false);
            SetActive(skillsPanel,   false);
            SetActive(eduExpPanel,   false);
            SetActive(projectsPanel, false);
            SetActive(fallbackPanel, false);
            _activePortfolioPanel = null;
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
        /// Opens the correct portfolio panel for the given area.
        /// </summary>
        public void OpenPortfolioPanel(PortfolioArea area = null)
        {
            var areaType = area != null ? area.AreaType : PortfolioAreaType.About;
            OpenPortfolioPanelByType(areaType, area?.AreaLabel);
        }

        public void OpenPortfolioPanel(PortfolioAreaType areaType, string label)
        {
            OpenPortfolioPanelByType(areaType, label);
        }

        private void OpenPortfolioPanelByType(PortfolioAreaType areaType, string label)
        {
            HideAllPortfolioPanels();

            GameObject target = areaType switch
            {
                PortfolioAreaType.About      => aboutPanel,
                PortfolioAreaType.Skills     => skillsPanel,
                PortfolioAreaType.Education  => eduExpPanel,
                PortfolioAreaType.Experience => eduExpPanel,
                PortfolioAreaType.Projects   => projectsPanel,
                _                            => fallbackPanel
            };

            // Fallback: if dedicated panel not assigned, use fallbackPanel
            if (target == null) target = fallbackPanel;

            // If we still end up on the fallback, populate it
            if (target == fallbackPanel)
            {
                CacheFallbackPanelReference();
                _fallbackPanelUI?.Populate(areaType, label ?? areaType.ToString());
            }

            _activePortfolioPanel = target;
            SetActive(target, true);
            GameManager.Instance?.SetPaused(true);
        }

        public void ClosePortfolioPanel()
        {
            HideAllPortfolioPanels();
            GameManager.Instance?.SetPaused(false);
        }

        public bool IsPortfolioPanelOpen()
        {
            return _activePortfolioPanel != null && _activePortfolioPanel.activeSelf;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
