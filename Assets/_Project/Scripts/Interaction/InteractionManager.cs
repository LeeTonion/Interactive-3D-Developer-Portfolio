using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using CodeDrive.Portfolio;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Listens for player-entered trigger events, shows interaction prompt with area name,
    /// and handles the Interact input to trigger 3D Showcase Booth focus view directly.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Core.UIManager uiManager;

        private InputAction _interactAction;
        private InteractionTrigger _currentTrigger;

        public InteractionTrigger CurrentTrigger => _currentTrigger;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (uiManager == null)
            {
                uiManager = Core.UIManager.Instance != null ? Core.UIManager.Instance : UnityEngine.Object.FindObjectOfType<Core.UIManager>();
            }
        }

        private void OnEnable()
        {
            if (inputActions == null) return;

            var map = inputActions.FindActionMap("CodeDrive", throwIfNotFound: false);
            if (map == null) return;

            _interactAction = map.FindAction("Interact");
            _interactAction?.Enable();

            if (_interactAction != null)
                _interactAction.performed += OnInteract;
        }

        private void OnDisable()
        {
            if (_interactAction != null)
                _interactAction.performed -= OnInteract;

            _interactAction?.Disable();
        }

        public void RegisterTrigger(InteractionTrigger trigger)
        {
            if (trigger == null) return;
            trigger.OnPlayerEntered -= HandlePlayerEntered;
            trigger.OnPlayerExited  -= HandlePlayerExited;
            trigger.OnPlayerEntered += HandlePlayerEntered;
            trigger.OnPlayerExited  += HandlePlayerExited;
        }

        public void UnregisterTrigger(InteractionTrigger trigger)
        {
            if (trigger == null) return;
            trigger.OnPlayerEntered -= HandlePlayerEntered;
            trigger.OnPlayerExited  -= HandlePlayerExited;
        }

        public void HandlePlayerEntered(InteractionTrigger trigger)
        {
            _currentTrigger = trigger;
            string areaName = trigger != null ? trigger.DisplayName : "Khu vực";
            string promptText = $"Click chuột vào điểm sáng để xem {areaName}";
            
            var targetUi = uiManager != null ? uiManager : Core.UIManager.Instance;
            targetUi?.ShowInteractionPrompt(true, promptText);
        }

        public void HandlePlayerExited(InteractionTrigger trigger)
        {
            if (_currentTrigger == trigger || trigger == null)
            {
                _currentTrigger = null;

                var targetUi = uiManager != null ? uiManager : Core.UIManager.Instance;
                if (targetUi != null)
                {
                    targetUi.ShowInteractionPrompt(false);

                    if (targetUi.IsPortfolioPanelOpen())
                        targetUi.ClosePortfolioPanel();
                }
            }
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if (_currentTrigger == null) return;

            // Check if there is a 3D Showcase Booth in this area
            var booth = _currentTrigger.GetComponentInChildren<ShowcaseBooth3D>();
            if (booth == null && _currentTrigger.AssociatedArea != null)
            {
                booth = _currentTrigger.AssociatedArea.GetComponentInChildren<ShowcaseBooth3D>();
            }

            if (booth != null)
            {
                if (booth.IsFocused)
                {
                    booth.ExitFocusView();
                }
                else
                {
                    booth.EnterFocusView();
                }
                return;
            }

            // Fallback for areas without 3D booths
            var targetUi = uiManager != null ? uiManager : Core.UIManager.Instance;
            if (targetUi == null) return;

            if (targetUi.IsPortfolioPanelOpen())
            {
                targetUi.ClosePortfolioPanel();
            }
            else
            {
                var area = _currentTrigger.AssociatedArea != null 
                    ? _currentTrigger.AssociatedArea 
                    : _currentTrigger.GetComponent<PortfolioArea>();

                if (area != null)
                {
                    targetUi.OpenPortfolioPanel(area);
                }
                else
                {
                    targetUi.OpenPortfolioPanel(_currentTrigger.AreaType, _currentTrigger.DisplayName);
                }

                AreaManager.Instance?.NotifyAreaInteracted(area);
            }
        }
    }
}
