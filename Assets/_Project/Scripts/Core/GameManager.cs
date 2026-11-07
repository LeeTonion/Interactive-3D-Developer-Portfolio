using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


    /// <summary>
    /// Manages top-level game state: reset signal.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private InputActionAsset inputActions;

        private InputAction _resetAction;
        private InputAction _pauseAction;
        private bool _isPaused;

        [Header("Pause UI")]
        [SerializeField] private PauseMenuUI pauseMenuUI;

        // Events
        public System.Action OnPauseToggled;
        public System.Action OnResetRequested;

        private SpaceCyberpunk.PlayerSpaceshipController _spaceship;
        private Vector3 _shipSpawnPos;
        private Quaternion _shipSpawnRot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (pauseMenuUI == null)
                pauseMenuUI = FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            if (pauseMenuUI == null)
                pauseMenuUI = FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);

            _spaceship = FindFirstObjectByType<SpaceCyberpunk.PlayerSpaceshipController>();
            if (_spaceship != null)
            {
                _shipSpawnPos = _spaceship.transform.position;
                _shipSpawnRot = _spaceship.transform.rotation;
            }
            OnResetRequested += ResetSpaceship;
        }

        private void OnDestroy()
        {
            OnResetRequested -= ResetSpaceship;
        }

        private void ResetSpaceship()
        {
            SpaceCyberpunk.SpaceTrafficShipAI.ClearFleetHostileAlert();
            if (_spaceship == null)
                _spaceship = FindFirstObjectByType<SpaceCyberpunk.PlayerSpaceshipController>();
            if (_spaceship == null) return;

            _spaceship.RespawnImmediate();
        }

        private void OnEnable()
        {
            // ESC / Pause key (works without an InputActionAsset)
            _pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            _pauseAction.performed += OnPausePressed;
            _pauseAction.Enable();

            if (inputActions == null) return;

            var map = inputActions.FindActionMap("CodeDrive", throwIfNotFound: false);
            if (map == null) return;

            _resetAction = map.FindAction("ResetVehicle");

            _resetAction?.Enable();

            if (_resetAction != null) _resetAction.performed += OnReset;
        }

        private void OnDisable()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed -= OnPausePressed;
                _pauseAction.Disable();
                _pauseAction.Dispose();
            }

            if (_resetAction != null) _resetAction.performed -= OnReset;

            _resetAction?.Disable();
        }

        private int _lastToggleFrame = -1;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TryTogglePause();
            }
            if (!_isPaused && !SpaceCyberpunk.AboutInfoStation.AnyStationActive && Input.GetKeyDown(KeyCode.R))
            {
                OnResetRequested?.Invoke();
            }
        }

        private void OnPausePressed(InputAction.CallbackContext ctx)
        {
            TryTogglePause();
        }

        public void TryTogglePause()
        {
            if (Time.frameCount == _lastToggleFrame) return;
            if (SpaceCyberpunk.AboutInfoStation.AnyStationActive) return;

            _lastToggleFrame = Time.frameCount;
            SetPaused(!_isPaused);
        }

        public void SetPaused(bool paused)
        {
            if (_isPaused == paused) return;
            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (pauseMenuUI == null)
                pauseMenuUI = FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);

            // Show/hide pause menu UI
            if (pauseMenuUI != null)
            {
                if (paused) pauseMenuUI.ShowOverlay();
                else        pauseMenuUI.HideOverlay();
            }

            // Giữ con trỏ chuột luôn hiển thị để có thể tương tác nút 3D trên trạm và UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            OnPauseToggled?.Invoke();
        }

        public bool IsPaused => _isPaused;

        private void OnReset(InputAction.CallbackContext ctx)
        {
            OnResetRequested?.Invoke();
        }
    }
