using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
/// <summary>
/// Handles the Pause Menu UI: Resume, Settings, Controls, Main Menu, Quit.
/// Attach to the PauseMenuPanel GameObject under Canvas.
/// Requires a CanvasGroup on this GameObject for the dim overlay effect.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Sub-Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject controlsPanel;

        private CanvasGroup _canvasGroup;
        private bool _isSettingsOpen = false;
        private bool _isControlsOpen = false;

        private bool _isShowing = false;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (!_isShowing)
            {
                SetActive(settingsPanel, false);
                SetActive(controlsPanel, false);
                HideOverlay();
            }
        }

        private void OnEnable()
        {
            Subscribe(resumeButton, OnResumeClicked);
            Subscribe(settingsButton, OnSettingsClicked);
            Subscribe(controlsButton, OnControlsClicked);
            Subscribe(mainMenuButton, OnMainMenuClicked);
            Subscribe(quitButton, OnQuitClicked);
        }

        private void OnDisable()
        {
            Unsubscribe(resumeButton, OnResumeClicked);
            Unsubscribe(settingsButton, OnSettingsClicked);
            Unsubscribe(controlsButton, OnControlsClicked);
            Unsubscribe(mainMenuButton, OnMainMenuClicked);
            Unsubscribe(quitButton, OnQuitClicked);
        }

        /// <summary>
        /// Shows the pause overlay (called by GameManager when pausing).
        /// </summary>
        public void ShowOverlay()
        {
            _isShowing = true;
            SetActive(gameObject, true);
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
            CloseSubPanels();
        }

        /// <summary>
        /// Hides the pause overlay (called by GameManager when resuming).
        /// </summary>
        public void HideOverlay()
        {
            _isShowing = false;
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            CloseSubPanels();
            SetActive(gameObject, false);
        }

        private void OnResumeClicked()
        {
            GameManager.Instance?.SetPaused(false);
        }

        private void OnSettingsClicked()
        {
            _isSettingsOpen = !_isSettingsOpen;
            SetActive(settingsPanel, _isSettingsOpen);
            if (_isSettingsOpen && _isControlsOpen)
            {
                _isControlsOpen = false;
                SetActive(controlsPanel, false);
            }
        }

        private void OnControlsClicked()
        {
            _isControlsOpen = !_isControlsOpen;
            SetActive(controlsPanel, _isControlsOpen);
            if (_isControlsOpen && _isSettingsOpen)
            {
                _isSettingsOpen = false;
                SetActive(settingsPanel, false);
            }
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
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

        /// <summary>
        /// Called externally to close all sub-panels (e.g., when pause menu closes).
        /// </summary>
        public void ForceCloseSubPanels()
        {
            CloseSubPanels();
        }

        private void CloseSubPanels()
        {
            _isSettingsOpen = false;
            _isControlsOpen = false;
            SetActive(settingsPanel, false);
            SetActive(controlsPanel, false);
        }

        private void Subscribe(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
            {
                btn.onClick.RemoveListener(action);
                btn.onClick.AddListener(action);
            }
        }

        private void Unsubscribe(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
                btn.onClick.RemoveListener(action);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
