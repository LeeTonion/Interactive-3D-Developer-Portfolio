using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeDrive.Player
{
    /// <summary>
    /// Bộ điều khiển xe arcade đơn giản và mượt mà.
    /// Hỗ trợ cả Unity Input System (InputActionAsset) và tự động fallback sang Keyboard.current (WASD/Phím mũi tên).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ tối đa khi tiến (m/s)")]
        [SerializeField] private float maxSpeed = 15f;

        [Tooltip("Tốc độ tối đa khi lùi (m/s)")]
        [SerializeField] private float reverseSpeed = 8f;

        [Tooltip("Gia tốc tăng tốc")]
        [SerializeField] private float acceleration = 25f;

        [Tooltip("Tốc độ quay xe (độ/giây)")]
        [SerializeField] private float turnSpeed = 110f;

        [Tooltip("Lực phanh")]
        [SerializeField] private float brakePower = 35f;

        [Tooltip("Lực cản khi nhả ga (tự giảm tốc)")]
        [SerializeField] private float coastDamping = 10f;

        [Header("Input Configuration")]
        [Tooltip("Asset InputAction tùy chọn. Nếu để trống hoặc lỗi, script sẽ tự nhận bàn phím WASD/Arrows.")]
        [SerializeField] private InputActionAsset inputActions;

        // Public properties để UI hoặc script khác dễ dàng đọc trạng thái
        public Vector2 MoveInput => _moveInput;
        public bool IsBraking => _isBraking;
        public float CurrentSpeed => _currentSpeed;

        private Rigidbody _rb;
        private InputAction _moveAction;
        private InputAction _brakeAction;
        private InputAction _resetAction;

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        private Vector2 _moveInput;
        private bool _isBraking;
        private bool _inputEnabled = true;
        private float _currentSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // Khóa lật xe trục X, Z để giữ thăng bằng
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

            InitializeInput();
        }

        private void InitializeInput()
        {
            if (inputActions == null) return;

            // Kích hoạt toàn bộ asset để đảm bảo action map hoạt động
            inputActions.Enable();

            var map = inputActions.FindActionMap("CodeDrive", throwIfNotFound: false);
            if (map != null)
            {
                map.Enable();
                _moveAction  = map.FindAction("Move");
                _brakeAction = map.FindAction("Brake");
                _resetAction = map.FindAction("ResetVehicle");
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _brakeAction?.Enable();
            _resetAction?.Enable();

            if (_resetAction != null)
            {
                _resetAction.performed += OnResetActionPerformed;
            }
        }

        private void OnDisable()
        {
            if (_resetAction != null)
            {
                _resetAction.performed -= OnResetActionPerformed;
            }

            _moveAction?.Disable();
            _brakeAction?.Disable();
            _resetAction?.Disable();
        }

        private void Update()
        {
            if (!_inputEnabled)
            {
                _moveInput = Vector2.zero;
                _isBraking = false;
                return;
            }

            // Đọc input từ InputAction hoặc trực tiếp từ Keyboard
            _moveInput = ReadMoveInput();
            _isBraking = ReadBrakeInput();

            // Kiểm tra phím Reset (R)
            if (CheckResetInput())
            {
                ResetToSpawn();
            }

            // Tính toán tốc độ hiện tại theo hướng di chuyển của xe
            _currentSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        }

        private void FixedUpdate()
        {
            if (!_inputEnabled)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                return;
            }

            ApplyMovementPhysics();
        }

        #region Input Helper Methods
        /// <summary>
        /// Đọc hướng di chuyển (X: rẽ trái/phải, Y: tiến/lùi).
        /// Ưu tiên InputAction, nếu không có thì đọc trực tiếp từ Keyboard.
        /// </summary>
        private Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            // 1. Đọc từ InputAction nếu có
            if (_moveAction != null && _moveAction.enabled)
            {
                input = _moveAction.ReadValue<Vector2>();
            }

            // 2. Fallback trực tiếp sang Keyboard (WASD & Mũi tên) nếu InputAction chưa có giá trị
            if (input.sqrMagnitude < 0.01f && Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;

                input = new Vector2(x, y);
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Đọc trạng thái phanh (Phím Space hoặc nút Brake)
        /// </summary>
        private bool ReadBrakeInput()
        {
            if (_brakeAction != null && _brakeAction.IsPressed())
            {
                return true;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Đọc phím Reset xe (Phím R hoặc action ResetVehicle)
        /// </summary>
        private bool CheckResetInput()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        private void OnResetActionPerformed(InputAction.CallbackContext ctx)
        {
            ResetToSpawn();
        }
        #endregion

        #region Physics & Movement
        private void ApplyMovementPhysics()
        {
            float throttle = _moveInput.y; // 1: Tiến, -1: Lùi
            float steer    = _moveInput.x; // -1: Trái, 1: Phải

            // 1. Khử trôi ngang (triệt tiêu trượt ngang để xe bám đường theo kiểu arcade)
            Vector3 lateralVelocity = transform.right * Vector3.Dot(_rb.linearVelocity, transform.right);
            _rb.linearVelocity -= lateralVelocity * 0.9f;

            // 2. Xử lý gia tốc Tiến / Lùi (dùng ForceMode.Acceleration để không bị ảnh hưởng bởi khối lượng xe)
            if (Mathf.Abs(throttle) > 0.05f)
            {
                float targetMaxSpeed = throttle > 0 ? maxSpeed : reverseSpeed;
                float currentForwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);

                // Chỉ tăng tốc nếu chưa vượt quá tốc độ tối đa theo chiều đó
                bool canAccelerate = (throttle > 0 && currentForwardSpeed < targetMaxSpeed) ||
                                     (throttle < 0 && currentForwardSpeed > -targetMaxSpeed);

                if (canAccelerate)
                {
                    Vector3 accelForce = transform.forward * (throttle * acceleration);
                    _rb.AddForce(accelForce, ForceMode.Acceleration);
                }
            }
            else
            {
                // Khi không đạp ga: hãm tốc từ từ (nhả ga tự dừng)
                Vector3 forwardVel = transform.forward * Vector3.Dot(_rb.linearVelocity, transform.forward);
                _rb.AddForce(-forwardVel * coastDamping, ForceMode.Acceleration);
            }

            // 3. Xử lý Phanh
            if (_isBraking)
            {
                Vector3 forwardVel = transform.forward * Vector3.Dot(_rb.linearVelocity, transform.forward);
                _rb.AddForce(-forwardVel * brakePower, ForceMode.Acceleration);
            }

            // 4. Xử lý Bẻ lái (Steering)
            // Cho phép bẻ lái khi xe đang di chuyển hoặc có input ga
            float speedRatio = Mathf.Clamp01(Mathf.Abs(_currentSpeed) / 2f);
            if (speedRatio > 0.05f || Mathf.Abs(throttle) > 0.05f)
            {
                // Khi lùi xe, hướng bẻ lái đảo chiều tự nhiên
                float directionModifier = (_currentSpeed < -0.1f) ? -1f : 1f;
                float turnAngle = steer * turnSpeed * directionModifier * Time.fixedDeltaTime;
                
                Quaternion turnRotation = Quaternion.Euler(0f, turnAngle, 0f);
                _rb.MoveRotation(_rb.rotation * turnAngleQuaternion(turnRotation));
            }
        }

        private Quaternion turnAngleQuaternion(Quaternion delta)
        {
            return delta;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Đặt lại vị trí xe về điểm xuất phát ban đầu.
        /// </summary>
        public void ResetToSpawn()
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        }

        /// <summary>
        /// Bật/tắt khả năng điều khiển xe (ví dụ: khi mở popup/UI).
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _moveInput = Vector2.zero;
                _isBraking = false;
            }
        }
        #endregion
    }
}

