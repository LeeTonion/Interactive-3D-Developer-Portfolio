using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
    /// <summary>
    /// Handles the Main Menu UI: Enter Portfolio button, input blocking, scene transition.
    /// Attach to the MainMenuCanvas GameObject.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button enterButton;
        [SerializeField] private string portfolioSceneName = "New Scene";
        
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        
        private InputAction _moveAction;
        private InputAction _brakeAction;
        private InputAction _interactAction;
        private InputAction _resetAction;
        
        private bool _hasTransitioned = false;
        
        private void Awake()
        {
            if (enterButton != null)
                enterButton.onClick.AddListener(OnEnterPortfolioClicked);
            
            // Block all gameplay input while in main menu
            BlockGameplayInput(true);
            
            // Ensure cursor is visible and unlocked for menu navigation
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Set timescale to normal (in case coming from paused state)
            Time.timeScale = 1f;

            // Debug log for verification
            Debug.Log("[MainMenuUI] Awake - Menu initialized");
        }

        private void OnDestroy()
        {
            if (enterButton != null)
                enterButton.onClick.RemoveListener(OnEnterPortfolioClicked);
            
            // Restore input when menu is destroyed
            BlockGameplayInput(false);
        }

        private void OnEnterPortfolioClicked()
        {
            if (_hasTransitioned) return;
            _hasTransitioned = true;
            
            // Disable button to prevent double-click
            if (enterButton != null)
                enterButton.interactable = false;
            
            // Unblock gameplay input before loading scene
            BlockGameplayInput(false);
            
            // Load the portfolio world scene
            SceneManager.LoadScene(portfolioSceneName);
        }

        /// <summary>
        /// Enables or disables the gameplay input actions.
        /// When in main menu, we disable gameplay input so the car doesn't move.
        /// </summary>
        private void BlockGameplayInput(bool block)
        {
            if (inputActions == null) return;
            
            var map = inputActions.FindActionMap("CodeDrive", throwIfNotFound: false)
                   ?? inputActions.FindActionMap("Player", throwIfNotFound: false);
            
            if (map == null) return;
            
            if (block)
            {
                map.Disable();
            }
            else
            {
                map.Enable();
            }
        }
    }
