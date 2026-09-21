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

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _currentRearStiffness = normalSidewaysStiffness;
        InitializeInput();
    }

    private void Start()
    {
        if (_rb != null && centerOfMass != null)
            _rb.centerOfMass = centerOfMass.localPosition;

        // Initialise rear-wheel friction to normal value
        SetRearSidewaysStiffness(normalSidewaysStiffness);
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
    }

    private void FixedUpdate()
    {
        ApplyDownforce();
        ApplyDriftFriction();
        ApplyDriftStabilization();
        DriveWheels();
        UpdateWheelMeshes();
        UpdateSteeringWheel();
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

        // 2. Fallback: Keyboard + Gamepad
        if (!hasActionInput)
        {
            if (Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  _horizontalInput -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)  _horizontalInput += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)     _verticalInput   += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)   _verticalInput   -= 1f;
                if (kb.spaceKey.isPressed)                            _isHandbraking    = true;
                if (kb.rKey.wasPressedThisFrame)                      ResetVehicle();
            }

            if (Gamepad.current != null)
            {
                var gp    = Gamepad.current;
                var stick = gp.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.1f) _horizontalInput = stick.x;
                if (Mathf.Abs(stick.y) > 0.1f) _verticalInput   = stick.y;
                if (gp.buttonEast.isPressed || gp.rightTrigger.ReadValue() > 0.5f)
                    _isHandbraking = true;
            }
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
    #region Helpers

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

        Vector3 euler       = transform.eulerAngles;
        transform.rotation  = Quaternion.Euler(0f, euler.y, 0f);
        transform.position += Vector3.up * 1.5f;

        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _currentTurnAngle   = 0f;
    }

    #endregion
}
