using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Listens for player-entered trigger events, shows interaction prompt,
    /// and handles the Interact input to open/close the portfolio panel.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Core.UIManager uiManager;

        private InputAction _interactAction;
        private InteractionTrigger _currentTrigger;

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
            trigger.OnPlayerEntered += HandlePlayerEntered;
            trigger.OnPlayerExited  += HandlePlayerExited;
        }

        private void HandlePlayerEntered(InteractionTrigger trigger)
        {
            _currentTrigger = trigger;
            uiManager?.ShowInteractionPrompt(true);
        }

        private void HandlePlayerExited(InteractionTrigger trigger)
        {
            if (_currentTrigger == trigger)
            {
                _currentTrigger = null;
                uiManager?.ShowInteractionPrompt(false);

                if (uiManager != null && uiManager.IsPortfolioPanelOpen())
                    uiManager.ClosePortfolioPanel();
            }
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if (_currentTrigger == null) return;

            if (uiManager == null) return;

            if (uiManager.IsPortfolioPanelOpen())
                uiManager.ClosePortfolioPanel();
            else
                uiManager.OpenPortfolioPanel();
        }
    }
}
