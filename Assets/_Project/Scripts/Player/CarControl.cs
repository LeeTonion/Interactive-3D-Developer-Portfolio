using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bộ điều khiển xe sử dụng WheelCollider với cơ chế Drift / Handbrake thực tế:
/// - Phanh tay (Space) chỉ khoá bánh SAU để văng đuôi drift, bánh trước vẫn bẻ lái.
/// - Ma sát ngang bánh sau giảm mượt khi drift và khôi phục khi nhả phanh.
/// - Góc lái thích ứng theo tốc độ (giảm góc ở tốc độ cao).
/// - Engine braking tự nhiên khi nhả ga.
/// - Downforce khí động học theo tốc độ.
/// - Trợ lực ổn định drift chống xoay vòng mất kiểm soát.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarControl : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Movement Settings")]
    [Tooltip("Engine power applied to drive wheels (Nm)")]
    public float enginePower = 2000f;
    [Tooltip("Maximum motor torque while reversing (Nm)")]
    public float reversePower = 2000f;
    [Tooltip("Maximum steering angle at low speed (degrees)")]
    public float turnSpeed = 35f;
    [Tooltip("Minimum steering angle at high speed (degrees)")]
    public float highSpeedTurnAngle = 18f;
    [Tooltip("Speed at which steering is fully reduced (m/s)")]
    public float steerSpeedFull = 20f;
    [Tooltip("Steering lerp smoothness")]
    public float turnSmoothness = 5f;

    // ── Braking ───────────────────────────────────────────────────────────────
    [Header("Braking Settings")]
    [Tooltip("Front axle brake torque (footbrake) (Nm)")]
    public float frontBrakePower = 2500f;
    [Tooltip("Rear axle brake torque (footbrake) (Nm)")]
    public float rearBrakePower = 1500f;
    [Tooltip("Handbrake torque applied only to rear wheels (Nm). Used for drift.")]
    public float handbrakePower = 2500f;
    [Tooltip("Light engine-braking torque when coasting (no throttle, no brake)")]
    public float engineBrakePower = 400f;

    // ── Drift & Friction ──────────────────────────────────────────────────────
    [Header("Drift / Friction Settings")]
    [Tooltip("Normal sideways friction stiffness on rear wheels")]
    [Range(0.5f, 4f)] public float normalSidewaysStiffness = 2.5f;
    [Tooltip("Sideways friction stiffness on rear wheels while handbrake/drift is active")]
    [Range(0.2f, 1.5f)] public float driftSidewaysStiffness = 1.1f;
    [Tooltip("How fast friction recovers after releasing handbrake (1 = ~1 second)")]
    public float frictionRecoverySpeed = 6f;
    [Tooltip("Anti-spinout: counter-torque coefficient to resist uncontrolled yaw (0 = off)")]
    [Range(0f, 1f)] public float driftStabilization = 0.75f;

    // ── Downforce ─────────────────────────────────────────────────────────────
    [Header("Downforce")]
    [Tooltip("Downforce multiplied by speed squared (kg/m). Keeps car planted at speed.")]
    public float downforceCoefficient = 5f;

    // ── Reset & Flip Recovery ──────────────────────────────────────────────────
    [Header("Reset & Flip Recovery")]
    [Tooltip("Enable automatic reset when the vehicle is flipped over")]
    public bool enableAutoReset = true;
    [Tooltip("Tilt angle (degrees) from upright before considering the car flipped")]
    [Range(45f, 90f)] public float flipAngleThreshold = 65f;
    [Tooltip("Maximum vehicle velocity (m/s) to trigger flip countdown")]
    public float flipSpeedThreshold = 1.5f;
    [Tooltip("Seconds the car must remain flipped before auto-reset triggers")]
    public float autoResetDelay = 2.0f;
    [Tooltip("Height offset above ground when resetting (meters)")]
    public float resetHeightOffset = 1.0f;

    // ── Upright Stability Assistance ───────────────────────────────────────────
    [Header("Active Upright Stabilization")]
    [Tooltip("Enable active balance assist to prevent rolling and right the vehicle")]
    public bool enableUprightAssist = true;
    [Tooltip("Strength of upright correcting torque")]
    [Range(1f, 50f)] public float uprightTorqueStrength = 15f;
    [Tooltip("Angular velocity damping to prevent wobble")]
    [Range(0.1f, 10f)] public float uprightDamping = 2.5f;
    [Tooltip("Minimum tilt angle (degrees) before upright assist activates")]
    [Range(5f, 45f)] public float uprightMinAngle = 12f;

    // ── Unstuck Assistance ─────────────────────────────────────────────────────
    [Header("Unstuck Assistance")]
    [Tooltip("Automatically apply a push force when wheels are stranded or car is beached/stuck")]
    public bool enableUnstuckAssist = true;
    [Tooltip("Seconds stuck with throttle pressed before applying unstuck impulse")]
    public float unstuckDelay = 1.0f;
    [Tooltip("Forward/backward impulse force to free the vehicle")]
    public float unstuckPushForce = 3500f;
    [Tooltip("Upward hop force to unstick from ridges/obstacles")]
    public float unstuckLiftForce = 1500f;

    // ── Fall & Out of Bounds Respawn ──────────────────────────────────────────
    [Header("Fall & Map Bounds")]
    [Tooltip("Enable automatic respawn when falling out of the map")]
    public bool enableFallRespawn = true;
    [Tooltip("Y-coordinate below which the vehicle is considered fallen off the map")]
    public float fallRespawnThresholdY = -15.0f;

    // ── Wheels ────────────────────────────────────────────────────────────────
    [Header("Wheel References")]
    [Tooltip("Order: FL, FR, RL, RR (same order for wheelMeshes)")]
    public Transform[] wheels;       // WheelCollider Transforms
    public Transform[] wheelMeshes;  // Visual mesh Transforms
    public Transform centerOfMass;
    public GameObject steeringWheel;

    // ── Input ─────────────────────────────────────────────────────────────────
    [Header("Input System")]
    [Tooltip("Optional InputActionAsset. Leave empty for automatic WASD / Arrow / Gamepad fallback.")]
    [SerializeField] private InputActionAsset inputActions;

    // ── Private state ─────────────────────────────────────────────────────────
    private Rigidbody _rb;
    private float _currentTurnAngle;
    private float _currentRearStiffness;
    private float _flipTimer;
    private bool  _isFlipped;
    private float _stuckTimer;
    private float _safeRecordTimer;
    private Vector3 _lastSafePosition;
    private Quaternion _lastSafeRotation;

    private InputAction _moveAction;
    private InputAction _brakeAction;
    private InputAction _resetAction;

    private float _horizontalInput;
    private float _verticalInput;
    private bool  _isHandbraking;

    // Public read-only properties for external scripts (camera, UI, etc.)
    public Vector2 MoveInput    => new Vector2(_horizontalInput, _verticalInput);
    public bool    IsBraking    => _isHandbraking;
    public float   CurrentSpeed => _rb != null ? _rb.linearVelocity.magnitude : 0f;
    public bool    IsFlipped    => _isFlipped;
    public float   FlipTimer    => _flipTimer;
    public float   FlipProgress => enableAutoReset && autoResetDelay > 0f ? Mathf.Clamp01(_flipTimer / autoResetDelay) : 0f;
    public float   StuckTimer   => _stuckTimer;

    // Events
    public event System.Action OnVehicleReset;
    public event System.Action<bool> OnFlipStateChanged;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        _currentRearStiffness = normalSidewaysStiffness;
        InitializeInput();
    }

    private void Start()
    {
        if (_rb != null && centerOfMass != null)
            _rb.centerOfMass = centerOfMass.localPosition;

        _lastSafePosition = transform.position;
        _lastSafeRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Initialise rear-wheel friction to normal value
        SetRearSidewaysStiffness(normalSidewaysStiffness);

        if (CodeDrive.Core.GameManager.Instance != null)
            CodeDrive.Core.GameManager.Instance.OnResetRequested += ResetVehicle;
    }

    private void OnDestroy()
    {
        if (CodeDrive.Core.GameManager.Instance != null)
            CodeDrive.Core.GameManager.Instance.OnResetRequested -= ResetVehicle;
    }

    private void OnEnable()
    {
        _moveAction?.Enable();
        _brakeAction?.Enable();
        _resetAction?.Enable();

        if (_resetAction != null)
            _resetAction.performed += OnResetPerformed;
    }

    private void OnDisable()
    {
        if (_resetAction != null)
            _resetAction.performed -= OnResetPerformed;

        _moveAction?.Disable();
        _brakeAction?.Disable();
        _resetAction?.Disable();
    }

    private void Update()
    {
        ReadInput();
        CheckFlipStatus();
        CheckFallRespawn();
        UpdateSafePosition();
        UpdateWheelMeshes();
        UpdateSteeringWheel();
    }

    private void FixedUpdate()
    {
        ApplyDownforce();
        ApplyDriftFriction();
        ApplyDriftStabilization();
        ApplyActiveUprightStabilization();
        CheckAndApplyUnstuckAssist();
        DriveWheels();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Input

    private void InitializeInput()
    {
        if (inputActions == null) return;

        inputActions.Enable();
        var map = inputActions.FindActionMap("CodeDrive", throwIfNotFound: false)
               ?? inputActions.FindActionMap("Player",    throwIfNotFound: false);

        if (map == null) return;

        map.Enable();
        _moveAction  = map.FindAction("Move");
        _brakeAction = map.FindAction("Brake");
        _resetAction = map.FindAction("ResetVehicle");
    }

    private void ReadInput()
    {
        _horizontalInput = 0f;
        _verticalInput   = 0f;
        _isHandbraking   = false;

        bool hasActionInput = false;

        // 1. New Input System (InputActionAsset)
        if (_moveAction != null && _moveAction.enabled)
        {
            Vector2 v    = _moveAction.ReadValue<Vector2>();
            _horizontalInput = v.x;
            _verticalInput   = v.y;
            hasActionInput   = true;
        }

        if (_brakeAction != null && _brakeAction.enabled)
            _isHandbraking = _brakeAction.IsPressed();

        // 2. Direct R-key fallback for quick manual vehicle reset
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetVehicle();
        }
    }

    /// <summary>
    /// Monitors vehicle tilt and speed to automatically upright the vehicle if flipped.
    /// </summary>
    private void CheckFlipStatus()
    {
        if (_rb == null) return;

        // Angle between car UP vector and World UP vector (0° = upright, 180° = upside down)
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        bool isCurrentlyTilted = tiltAngle > flipAngleThreshold;
        bool isStationaryOrSlow = _rb.linearVelocity.magnitude < flipSpeedThreshold;

        bool flipped = isCurrentlyTilted && isStationaryOrSlow;

        if (flipped != _isFlipped)
        {
            _isFlipped = flipped;
            OnFlipStateChanged?.Invoke(_isFlipped);
        }

        if (_isFlipped && enableAutoReset)
        {
            _flipTimer += Time.deltaTime;
            if (_flipTimer >= autoResetDelay)
            {
                ResetVehicle();
                _flipTimer = 0f;
            }
        }
        else
        {
            _flipTimer = Mathf.Max(0f, _flipTimer - Time.deltaTime * 2f);
        }
    }

    private void OnResetPerformed(InputAction.CallbackContext ctx) => ResetVehicle();

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Physics Systems

    /// <summary>Aerodynamic downforce — scales with velocity squared.</summary>
    private void ApplyDownforce()
    {
        float speedSq = _rb.linearVelocity.sqrMagnitude;
        _rb.AddForce(-transform.up * downforceCoefficient * speedSq);
    }

    /// <summary>
    /// Smoothly adjusts rear-axle sideways friction stiffness.
    /// During handbrake → drift value; otherwise → lerp back to normal.
    /// </summary>
    private void ApplyDriftFriction()
    {
        float targetStiffness = _isHandbraking ? driftSidewaysStiffness : normalSidewaysStiffness;
        _currentRearStiffness = Mathf.Lerp(
            _currentRearStiffness,
            targetStiffness,
            Time.fixedDeltaTime * frictionRecoverySpeed
        );
        SetRearSidewaysStiffness(_currentRearStiffness);
    }

    /// <summary>
    /// Applies a counter-yaw force proportional to angular velocity to
    /// prevent uncontrolled full-rotation spinouts during drift.
    /// </summary>
    private void ApplyDriftStabilization()
    {
        if (driftStabilization <= 0f || !_isHandbraking) return;

        float yawRate = _rb.angularVelocity.y;
        _rb.AddTorque(
            Vector3.up * (-yawRate * driftStabilization * _rb.mass),
            ForceMode.Force
        );
    }

    /// <summary>
    /// Actively stabilizes the vehicle upright when it begins tilting or rolling excessively.
    /// </summary>
    private void ApplyActiveUprightStabilization()
    {
        if (!enableUprightAssist || _rb == null) return;

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        // Apply upright assistance if car is tilting but not completely upside-down
        if (tiltAngle > uprightMinAngle && tiltAngle < flipAngleThreshold)
        {
            Vector3 uprightAxis = Vector3.Cross(transform.up, Vector3.up);
            float angleFactor = Mathf.Clamp01(tiltAngle / 45f);

            Vector3 correctingTorque = uprightAxis * (uprightTorqueStrength * angleFactor * _rb.mass);
            Vector3 dampingTorque = -_rb.angularVelocity * (uprightDamping * _rb.mass);

            _rb.AddTorque(correctingTorque + dampingTorque, ForceMode.Force);
        }
    }

    /// <summary>
    /// Detects if the vehicle is stranded/beached (e.g. resting on an obstacle with wheels off ground)
    /// and applies an unstuck impulse after the delay threshold.
    /// </summary>
    private void CheckAndApplyUnstuckAssist()
    {
        if (!enableUnstuckAssist || _rb == null) return;

        bool isTryingToMove = Mathf.Abs(_verticalInput) > 0.1f;
        int groundedCount = GetGroundedWheelCount();
        bool isAlmostStationary = _rb.linearVelocity.magnitude < 0.35f;

        // Vehicle is stuck if player gives throttle but car can't move or wheels are not properly touching ground
        bool isStuck = isTryingToMove && (isAlmostStationary || groundedCount <= 1);

        if (isStuck)
        {
            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer >= unstuckDelay)
            {
                float pushDir = _verticalInput < -0.1f ? -1f : 1f;
                Vector3 impulse = (transform.forward * (unstuckPushForce * pushDir)) + (Vector3.up * unstuckLiftForce);
                _rb.AddForce(impulse, ForceMode.Impulse);

                _stuckTimer = 0f;
            }
        }
        else
        {
            _stuckTimer = Mathf.Max(0f, _stuckTimer - Time.fixedDeltaTime * 2f);
        }
    }

    /// <summary>Main wheel driving / braking logic.</summary>
    private void DriveWheels()
    {
        if (wheels == null) return; 

        float speed = Vector3.Dot(_rb.linearVelocity, transform.forward);

        // ── Speed-sensitive steering ──────────────────────────────────────────
        float speedRatio   = Mathf.Clamp01(Mathf.Abs(speed) / steerSpeedFull);
        float maxSteer     = Mathf.Lerp(turnSpeed, highSpeedTurnAngle, speedRatio);
        float targetAngle  = _horizontalInput * maxSteer;
        _currentTurnAngle  = Mathf.Lerp(_currentTurnAngle, targetAngle,
                                         Time.fixedDeltaTime * turnSmoothness);

        // ── Footbrake detection ───────────────────────────────────────────────
        bool isFootBraking = (_verticalInput < -0.05f && speed > 0.5f) ||
                             (_verticalInput >  0.05f && speed < -0.5f);

        // ── Throttle torque ───────────────────────────────────────────────────
        float throttle;
        if (isFootBraking)
        {
            throttle = 0f;   // engine off while foot-braking
        }
        else if (speed < -0.1f && _verticalInput > 0.05f)
        {
            throttle = 0f;   // transition reverse→forward handled by foot-brake above
        }
        else
        {
            // Normal drive: forward or reverse
            float maxPower = (_verticalInput < 0f) ? -reversePower : enginePower;
            throttle = _verticalInput * Mathf.Abs(maxPower);
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;

            WheelCollider wc = wheels[i].GetComponent<WheelCollider>();
            if (wc == null) continue;

            bool isFront = (i < 2);
            bool isRear  = !isFront;

            // ── Steering ──────────────────────────────────────────────────────
            wc.steerAngle = isFront ? _currentTurnAngle : 0f;

            // ── Torque ────────────────────────────────────────────────────────
            if (_isHandbraking && isRear)
            {
                // Handbrake drift: rear locked, no drive torque
                wc.motorTorque = 0f;
                wc.brakeTorque = handbrakePower;
            }
            else if (isFootBraking)
            {
                wc.motorTorque = 0f;
                wc.brakeTorque = isFront ? frontBrakePower : rearBrakePower;
            }
            else if (Mathf.Approximately(throttle, 0f))
            {
                // Coasting — engine braking
                wc.motorTorque = 0f;
                wc.brakeTorque = engineBrakePower;
            }
            else
            {
                // Normal driving — all-wheel drive
                wc.brakeTorque = 0f;
                wc.motorTorque = throttle;
            }
        }
    }

    /// <summary>Sync visual wheel meshes to WheelCollider world poses.</summary>
    private void UpdateWheelMeshes()
    {
        if (wheels == null || wheelMeshes == null) return;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;
            WheelCollider wc = wheels[i].GetComponent<WheelCollider>();
            if (wc == null) continue;
            if (i >= wheelMeshes.Length || wheelMeshes[i] == null) continue;

            wc.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelMeshes[i].position = pos;
            wheelMeshes[i].rotation = rot;
        }
    }

    /// <summary>Rotate interior steering wheel mesh to match wheel angle.</summary>
    private void UpdateSteeringWheel()
    {
        if (steeringWheel == null) return;
        steeringWheel.transform.localEulerAngles =
            new Vector3(-64f, 0f, _currentTurnAngle * 3f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Helpers & Respawn

    /// <summary>
    /// Periodically records safe grounded positions on the map for fall recovery.
    /// </summary>
    private void UpdateSafePosition()
    {
        if (_rb == null) return;

        _safeRecordTimer += Time.deltaTime;
        if (_safeRecordTimer >= 0.5f)
        {
            _safeRecordTimer = 0f;

            int grounded = GetGroundedWheelCount();
            float tilt = Vector3.Angle(transform.up, Vector3.up);

            // Save position if vehicle is grounded, upright, and not falling
            if (grounded >= 2 && tilt < 35f && transform.position.y > fallRespawnThresholdY + 5f)
            {
                _lastSafePosition = transform.position;
                _lastSafeRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
        }
    }

    /// <summary>
    /// Checks if the vehicle has fallen below the map threshold and respawns it.
    /// </summary>
    private void CheckFallRespawn()
    {
        if (!enableFallRespawn) return;

        if (transform.position.y < fallRespawnThresholdY)
        {
            RespawnAtSafePosition();
        }
    }

    /// <summary>
    /// Respawns the vehicle at the last recorded safe position on the map.
    /// </summary>
    public void RespawnAtSafePosition()
    {
        if (_rb == null) return;

        transform.position = _lastSafePosition + Vector3.up * resetHeightOffset;
        transform.rotation = _lastSafeRotation;

        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _currentTurnAngle   = 0f;
        _flipTimer          = 0f;
        _stuckTimer         = 0f;
        _isFlipped          = false;

        ZeroWheelTorques();
        OnVehicleReset?.Invoke();
    }

    /// <summary>
    /// Returns the number of wheels currently contacting ground surfaces.
    /// </summary>
    public int GetGroundedWheelCount()
    {
        if (wheels == null) return 0;
        int count = 0;
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;
            WheelCollider wc = wheels[i].GetComponent<WheelCollider>();
            if (wc != null && wc.isGrounded)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Zeroes all wheel motor and brake torques.
    /// </summary>
    private void ZeroWheelTorques()
    {
        if (wheels == null) return;
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;
            WheelCollider wc = wheels[i].GetComponent<WheelCollider>();
            if (wc != null)
            {
                wc.motorTorque = 0f;
                wc.brakeTorque = 0f;
                wc.steerAngle  = 0f;
            }
        }
    }

    /// <summary>
    /// Set the sideways friction stiffness on the rear two WheelColliders
    /// (index 2 = RL, index 3 = RR).
    /// </summary>
    private void SetRearSidewaysStiffness(float stiffness)
    {
        if (wheels == null) return;
        for (int i = 2; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;
            WheelCollider wc = wheels[i].GetComponent<WheelCollider>();
            if (wc == null) continue;

            WheelFrictionCurve sf = wc.sidewaysFriction;
            sf.stiffness          = stiffness;
            wc.sidewaysFriction   = sf;
        }
    }

    /// <summary>Reset vehicle to upright position and zero velocity.</summary>
    public void ResetVehicle()
    {
        if (_rb == null) return;

        // Keep current yaw (heading) direction, remove pitch and roll
        float currentYaw = transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        // Raycast down to find ground safely without getting stuck
        Vector3 rayOrigin = transform.position + Vector3.up * 2f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + Vector3.up * resetHeightOffset;
        }
        else
        {
            // Fallback: use last safe position or raise from current position
            if (_lastSafePosition != Vector3.zero)
                transform.position = _lastSafePosition + Vector3.up * resetHeightOffset;
            else
                transform.position += Vector3.up * resetHeightOffset;
        }

        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _currentTurnAngle   = 0f;
        _flipTimer          = 0f;
        _stuckTimer         = 0f;
        _isFlipped          = false;

        ZeroWheelTorques();
        OnVehicleReset?.Invoke();
    }

    #endregion
}
