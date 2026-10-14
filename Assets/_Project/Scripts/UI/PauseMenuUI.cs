using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using CodeDrive.Core;
using CodeDrive.UI;

namespace CodeDrive.UI
{
    /// <summary>
    /// Handles the Pause Menu UI: Resume, Settings, Controls, Main Menu.
    /// Attach to the PauseMenuPanel GameObject under Canvas.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton; // Optional: for standalone builds

        [Header("Sub-Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject controlsPanel;

        [Header("Theme")]
        [SerializeField] private UITheme uiTheme;

        private bool _isSettingsOpen = false;
        private bool _isControlsOpen = false;

        private void Awake()
        {
            // Force compilation timestamp
            System.Diagnostics.Debug.WriteLine($"PauseMenuUI compiled at {System.DateTime.Now}");
            
            // Button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (controlsButton != null)
                controlsButton.onClick.AddListener(OnControlsClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Initially hide sub-panels
            SetActive(settingsPanel, false);
            SetActive(controlsPanel, false);

            // Apply theme if available
            if (uiTheme != null)
                ApplyTheme();
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (controlsButton != null)
                controlsButton.onClick.RemoveListener(OnControlsClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void ApplyTheme()
        {
            if (uiTheme == null) return;

            uiTheme.ApplyToButton(resumeButton, resumeButton?.GetComponentInChildren<TMP_Text>());
            uiTheme.ApplyToButton(settingsButton, settingsButton?.GetComponentInChildren<TMP_Text>());
            uiTheme.ApplyToButton(controlsButton, controlsButton?.GetComponentInChildren<TMP_Text>());
            uiTheme.ApplyToButton(mainMenuButton, mainMenuButton?.GetComponentInChildren<TMP_Text>());
            if (quitButton != null)
                uiTheme.ApplyToButton(quitButton, quitButton?.GetComponentInChildren<TMP_Text>());

            // Apply dim overlay to fullscreen background
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(0.04f, 0.04f, 0.06f, 0.85f);

            // Apply panel background to MenuCard if present
            var menuCard = transform.Find("MenuCard");
            var menuCardImage = menuCard != null ? menuCard.GetComponent<Image>() : null;
            if (menuCardImage != null)
                menuCardImage.color = uiTheme.PanelBackground;
        }

        private void OnResumeClicked()
        {
            // Close any open sub-panels first
            CloseSubPanels();
            
            // Resume game via GameManager
            GameManager.Instance?.SetPaused(false);
        }

        private void OnSettingsClicked()
        {
            _isSettingsOpen = !_isSettingsOpen;
            SetActive(settingsPanel, _isSettingsOpen);
            
            // Close controls if open
            if (_isControlsOpen)
            {
                _isControlsOpen = false;
                SetActive(controlsPanel, false);
            }
        }

        private void OnControlsClicked()
        {
            _isControlsOpen = !_isControlsOpen;
            SetActive(controlsPanel, _isControlsOpen);
            
            // Close settings if open
            if (_isSettingsOpen)
            {
                _isSettingsOpen = false;
                SetActive(settingsPanel, false);
            }
        }

        private void OnMainMenuClicked()
        {
            // Ensure timescale is restored before scene change
            Time.timeScale = 1f;
            
            // Unload current scene and load main menu
            SceneManager.LoadScene("MainMenu");
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CloseSubPanels()
        {
            _isSettingsOpen = false;
            _isControlsOpen = false;
            SetActive(settingsPanel, false);
            SetActive(controlsPanel, false);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        /// <summary>
        /// Called externally to close all sub-panels (e.g., when pause menu closes)
        /// </summary>
        public void ForceCloseSubPanels()
        {
            CloseSubPanels();
        }
    }
}