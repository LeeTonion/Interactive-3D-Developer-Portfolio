using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeDrive.Core
{
    /// <summary>
    /// Manages top-level game state: pause and reset signals.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private InputActionAsset inputActions;

        private InputAction _pauseAction;
        private InputAction _resetAction;
        private bool _isPaused;

        public bool IsPaused => _isPaused;

        // Events
        public System.Action OnPauseToggled;
        public System.Action OnResetRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (inputActions == null) return;

            var map = inputActions.FindActionMap("CodeDrive", throwIfNotFound: false);
            if (map == null) return;

            _pauseAction = map.FindAction("Pause");
            _resetAction = map.FindAction("ResetVehicle");

            _pauseAction?.Enable();
            _resetAction?.Enable();

            if (_pauseAction != null) _pauseAction.performed += OnPause;
            if (_resetAction != null) _resetAction.performed += OnReset;
        }

        private void OnDisable()
        {
            if (_pauseAction != null) _pauseAction.performed -= OnPause;
            if (_resetAction != null) _resetAction.performed -= OnReset;

            _pauseAction?.Disable();
            _resetAction?.Disable();
        }

        private void OnPause(InputAction.CallbackContext ctx)
        {
            TogglePause();
        }

        private void OnReset(InputAction.CallbackContext ctx)
        {
            OnResetRequested?.Invoke();
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            OnPauseToggled?.Invoke();
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            Time.timeScale = _isPaused ? 0f : 1f;
            OnPauseToggled?.Invoke();
        }
    }
}
