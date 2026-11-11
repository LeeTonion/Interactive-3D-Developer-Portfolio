using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Bộ điều khiển phi thuyền không gian Cyberpunk (Flight, Hover, Twin Plasma Laser & Sci-Fi Combat HUD):
    /// - Đệm nâng phản trọng lực + Trọng lực thực tế.
    /// - Pháo kép Laser Plasma (Chuột Trái / Phím J).
    /// - Khi chết sẽ nổ tung và Hồi Sinh chính xác tại Điểm Spawn ban đầu (PlayerSpawnPoint).
    /// - Thanh máu Cyberpunk dạng 12 ô năng lượng phát sáng trên Canvas (chỉ hiện khi bị truy đuổi) + Nháy đỏ màn hình khi sắp hết máu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class PlayerSpaceshipController : MonoBehaviour
    {
        [Header("Khu Vực Xuất Phát Động (Spawn_Zone + Offset)")]
        [Tooltip("Object gốc của vùng Spawn (Spawn_Zone) — khi di chuyển vùng này, vị trí tàu spawn sẽ tự động đi theo")]
        public Transform spawnZone;
        [Tooltip("Tọa độ tương đối (Local Offset) của tàu tính từ tâm Spawn_Zone")]
        public Vector3 spawnPositionOffset = new Vector3(0f, 3.85f, -1.5f);
        [Tooltip("Góc xoay tương đối (Local Euler) của tàu so với hướng của Spawn_Zone")]
        public Vector3 spawnRotationOffset = Vector3.zero;
        [Tooltip("Tự động đặt tàu vào (Spawn_Zone + Offset) ngay khi bấm Play")]
        public bool snapToSpawnZoneOnStart = true;
        [Tooltip("Điểm neo phụ (tùy chọn)")]
        public Transform spawnPoint;

        [Header("Trọng Lực & Đệm Nâng Lơ Lửng (Hover & Gravity)")]
        public float gravity = 12f;
        public float targetHoverHeight = 2.2f;
        public float hoverSpring = 28f;
        public float hoverDamping = 8f;
        public float bobAmplitude = 0.15f;
        public float bobFrequency = 2.2f;
        public LayerMask groundLayer = ~0;

        [Header("Tốc Độ & Quán Tính Không Gian (Realistic Space Propulsion)")]
        public float cruiseSpeed = 34f;
        public float boostSpeed = 68f;
        public float reverseSpeed = 16f;
        public float verticalSpeed = 18f;
        public float acceleration = 10.5f;
        [Tooltip("Lực cản trôi tự do khi thả ga ngoài vũ trụ (càng nhỏ trôi quán tính càng lâu)")]
        public float spaceCoastDrag = 1.9f;
        [Tooltip("Lực phanh ngược Retro-Thruster khi nhấn S")]
        public float retroBrakeRate = 18f;
        [Tooltip("Độ bám vector vận tốc vào mũi tàu (càng nhỏ tàu càng có độ drift G-force khi cua gấp)")]
        public float velocityResponsiveness = 6.2f;
        public float hoverVelocityResponsiveness = 11f;

        [Header("Cơ Chế Lái Thực Tế & Cần Điều Khiển Ảo (Flight Stick & Aerodynamics)")]
        public bool enableMouseFlightStick = true;
        public float mouseSteerSensitivity = 0.90f;
        public float mouseSteerDeadzone = 0.075f;
        public bool invertMousePitch = false;
        public KeyCode toggleMouseStickKey = KeyCode.M;
        public float turnSpeed = 78f;
        public float pitchSpeed = 66f;
        public float angularResponse = 6.8f;
        public float maxBankAngle = 42f;
        public float bankSmoothing = 6.5f;

        [Header("Hệ Thống Nhào Lộn Không Gian (Acrobatic Flight 360°)")]
        [Tooltip("Bật chế độ nhào lộn tự do linh hoạt (Barrel Roll, Vertical Loop 360 độ)")]
        public bool enableAcrobaticFlight = true;
        [Tooltip("Tốc độ lăn lộn vòng (độ/giây)")]
        public float rollSpeed = 160f;
        [Tooltip("Thời gian thực hiện 1 vòng Barrel Roll 360 độ (giây)")]
        public float barrelRollDuration = 0.55f;
        [Tooltip("Thời gian thực hiện 1 vòng Vertical Loop 360 độ (giây)")]
        public float loopDuration = 0.85f;

        [Header("Cơ Chế Bay Tự Do Khi Va Chạm (Free Flight On Collision)")]
        [Tooltip("Tắt tự động phanh lùi / thắng giật lùi và ngắt động cơ khi va chạm, cho phép phi thuyền lướt tự do")]
        public bool disableCollisionBrake = true;
        [Tooltip("Tắt cưỡng bức cân bằng góc nghiêng khi va đập, cho phép tàu di chuyển tự do mềm mại")]
        public bool freeFlightOnCollision = true;

        [Header("Hệ Thống Tự Cân Bằng Sau Va Chạm (Gyro Auto-Stabilizer)")]
        [Tooltip("Tốc độ con quay hồi chuyển tự lấy lại thăng bằng phương ngang sau va chạm")]
        public float horizonStabilizeSpeed = 6f;
        [Tooltip("Tốc độ tự trả mũi tàu (Pitch) về mặt phẳng ngang khi không điều khiển")]
        public float pitchAutoLevelSpeed = 4.5f;
        [Tooltip("Độ triệt tiêu lực xoay văng vật lý sau khi va đập")]
        public float gyroAngularDamping = 10f;
        [Tooltip("Góc ngóc/chúi tối đa cho phép (độ) - khi nhào lộn có thể đạt 360 độ tự do")]
        public float maxPitchAngle = 88f;

        [Header("Hệ Thống Vũ Khí & Giáp Chiến Đấu (Space Combat)")]
        public float fireInterval = 0.16f;
        public float laserDamage = 35f;
        public float maxHealth = 100f;
        public float maxShield = 100f;
        public float shieldRegenRate = 16f;
        public float maxBoostEnergy = 100f;
        public float boostDrainRate = 22f;
        public float boostRegenRate = 18f;

        [Header("Hiệu Ứng Động Cơ Phản Lực (VFX)")]
        public Transform visualModel;
        public Light engineLight;
        public ParticleSystem leftThrusterParticle;
        public ParticleSystem rightThrusterParticle;

        private Rigidbody _rb;
        private float _currentForwardSpeed;
        private float _targetForwardSpeed;
        private float _currentVerticalSpeed;
        private float _targetVerticalSpeed;
        private float _currentBank;
        private float _currentYaw;
        private float _currentPitch;
        private float _yawRate;
        private float _pitchRate;
        private float _rollRate;
        private float _turnInput;
        private float _pitchInput;
        private float _rollInput;
        private float _throttleInput;
        private Vector2 _virtualStick;
        private Vector2 _virtualStickVel;
        private Vector2 _rmbStick;
        private Vector2 _rmbStickVel;
        private float _smoothedTurnCmd;
        private float _smoothedPitchCmd;
        private float _smoothedRollCmd;
        private bool _mouseSteerArmed;
        private Vector3 _lastMouseScreenPos;
        private float _mouseMovedPixels;
        private float _freeRollHoldTimer;
        private float _currentGForce = 1f;
        private Vector3 _prevWorldVel;
        private float _lastFireTime = -99f;
        private SpaceshipCockpit1P _cockpit1P;
        private bool _isAcrobaticRolling;
        private float _acrobaticRollTimer;
        private float _acrobaticRollDirection;
        private bool _isAcrobaticLooping;
        private float _acrobaticLoopTimer;
        private float _lastTapATime = -99f;
        private float _lastTapDTime = -99f;
        private float _lastTapQTime = -99f;
        private float _lastTapETime = -99f;
        private float _lastSteeringInputTime = 0f;
        private float _impactRollJolt;
        private float _impactPitchJolt;
        private bool _isBoosting;
        private bool _isHoveringOverGround;
        private float _groundDistance;

        private float _currentHealth;
        private float _currentShield;
        private float _currentBoostEnergy;
        private float _displayedHpRatio = 1f;
        private float _displayedShieldRatio = 1f;
        private float _displayedBoostRatio = 1f;
        private float _displayedKmh = 0f;

        private float _nextFireTime;
        private float _lastDamageTime = -99f;
        private float _collisionRecoilTimer;
        private Vector3 _contactNormalSum;
        private bool _isRespawning;
        private bool _thirdPersonVisualsVisible = true;
        private GameObject _cachedShieldBubbleGo;
        private Vector3 _spawnPos = new Vector3(-65f, 28.7f, 45f);
        private Quaternion _spawnRot = Quaternion.Euler(0f, 115f, 0f);

        private PlayerSpaceshipShieldEffect _shieldEffect;
        private SpaceshipTargetLockSystem _targetLockSystem;

        // UI Canvas Sci-Fi HUD Elements (Theo thiết kế ảnh 1)
        private GameObject _playerHudRoot;
        private RectTransform _compassTapeRoot;
        private RectTransform[] _compassTickLines;
        private TMP_Text _compassArrow;
        private GameObject _crosshairRoot;
        private GameObject _virtualStickHudRoot;
        private RectTransform _stickLineRt;
        private RectTransform _stickPipRt;
        private Image _stickLineImg;
        private Image _stickPipImg;

        // Bottom Left HP & SHD Bars
        private Image _hpBarFill;
        private Image _hpBarLag;
        private Image _shdBarFill;
        private Image _shdBarLag;
        private TMP_Text _hpLabel;
        private TMP_Text _shdLabel;

        // Bottom Right SPEED & BOOST
        private TMP_Text _speedLabelText;
        private TMP_Text _speedBigNumberText;
        private Image _boostBarFill;

        // Warning Alert (Chỉ hiện khi bị tấn công)
        private GameObject _warningCardRoot;
        private TMP_Text _warningTitleText;
        private TMP_Text _warningSubText;
        private Image _redScreenOverlay;
        private Image[] _redBorders;

        public bool IsBoosting => _isBoosting;
        public bool IsHovering => _isHoveringOverGround;
        public float CurrentForwardSpeed => _currentForwardSpeed;
        public float CurrentBankAngle => _currentBank;
        public float CurrentPitchRate => _pitchRate;
        public float CurrentYawRate => _yawRate;
        public float CurrentRollRate => _rollRate;
        public float CurrentGForce => _currentGForce;
        public float ThrottleRatio => Mathf.Clamp01(_currentForwardSpeed / Mathf.Max(1f, boostSpeed));
        public bool IsBraking => _throttleInput < -0.1f;
        public bool IsCoasting => !_isHoveringOverGround && Mathf.Abs(_throttleInput) < 0.05f && Mathf.Abs(_currentForwardSpeed) > 3f;
        public bool IsMouseSteerArmed => enableMouseFlightStick && _mouseSteerArmed;
        public Vector2 VirtualStick => _virtualStick;
        public float TurnInput => _turnInput;
        public float PitchInput => _pitchInput;
        public float RollInput => _rollInput;
        public float LastFireTime => _lastFireTime;
        public bool IsRespawning => _isRespawning;
        public bool IsUnderFire => _lastDamageTime > 0f && (Time.time - _lastDamageTime) < 3.5f;
        public float CurrentSpeedRatio => Mathf.Abs(_currentForwardSpeed) / boostSpeed;
        public float HealthRatio => maxHealth > 0f ? Mathf.Clamp01(_currentHealth / maxHealth) : 0f;
        public float ShieldRatio => maxShield > 0f ? Mathf.Clamp01(_currentShield / maxShield) : 0f;
        public float BoostRatio => maxBoostEnergy > 0f ? Mathf.Clamp01(_currentBoostEnergy / maxBoostEnergy) : 0f;

        /// <summary>
        /// Ẩn/hiện phần thân tàu góc nhìn thứ 3 (mesh, thruster, shield bubble) khi chuyển sang buồng lái 1P
        /// mà vẫn giữ nguyên toàn bộ Collider vật lý.
        /// </summary>
        public void SetThirdPersonShipVisualsVisible(bool visible)
        {
            _thirdPersonVisualsVisible = visible;

            if (visualModel != null)
            {
                var renderers = visualModel.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) renderers[i].enabled = visible;
                }
            }

            SetThrusterRenderersVisible(leftThrusterParticle, visible);
            SetThrusterRenderersVisible(rightThrusterParticle, visible);

            if (engineLight != null)
            {
                engineLight.enabled = visible;
            }

            if (_cachedShieldBubbleGo == null)
            {
                var sb = transform.Find("Player_HexShield_Bubble");
                if (sb != null) _cachedShieldBubbleGo = sb.gameObject;
            }
            if (_cachedShieldBubbleGo != null && _cachedShieldBubbleGo.activeSelf != visible)
            {
                _cachedShieldBubbleGo.SetActive(visible);
            }
        }

        private static void SetThrusterRenderersVisible(ParticleSystem ps, bool visible)
        {
            if (ps == null) return;
            var psRenderers = ps.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < psRenderers.Length; i++)
            {
                if (psRenderers[i] != null) psRenderers[i].enabled = visible;
            }
        }

        private void EnsureRigidbody()
        {
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
                if (_rb != null)
                {
                    _rb.useGravity = false;
                    _rb.interpolation = RigidbodyInterpolation.Interpolate;
                    _rb.linearDamping = 1.8f;
                    _rb.angularDamping = 3.5f;
                    _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
            }
        }

        private void Awake()
        {
            EnsureRigidbody();

            CaptureSpawnPose();
            _currentYaw = transform.eulerAngles.y;
            _currentPitch = 0f;
            _currentHealth = maxHealth;
            _currentShield = maxShield;
            _currentBoostEnergy = maxBoostEnergy;
            _displayedHpRatio = 1f;
            _displayedShieldRatio = 1f;
            _displayedBoostRatio = 1f;

            _shieldEffect = GetComponent<PlayerSpaceshipShieldEffect>();
            if (_shieldEffect == null) _shieldEffect = gameObject.AddComponent<PlayerSpaceshipShieldEffect>();

            _targetLockSystem = GetComponent<SpaceshipTargetLockSystem>();
            if (_targetLockSystem == null) _targetLockSystem = gameObject.AddComponent<SpaceshipTargetLockSystem>();

            if (GetComponent<SciFiRadarMinimap>() == null)
            {
                gameObject.AddComponent<SciFiRadarMinimap>();
            }

            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = false;
                col.size = new Vector3(5.2f, 1.8f, 6.5f);
                col.center = new Vector3(0f, 0.85f, 0.15f);
            }

            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
            }
        }

        private void OnEnable()
        {
            if (_currentHealth <= 0f) _currentHealth = maxHealth;
            CaptureSpawnPose();
            _currentYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            if (snapToSpawnZoneOnStart)
            {
                RespawnImmediate();
            }
        }

        /// <summary>
        /// Tính toán vị trí & góc xoay xuất phát động theo công thức: Vị trí Spawn_Zone hiện tại + Local Offset.
        /// Nhờ đó khi kéo vùng Spawn_Zone đi bất cứ đâu trong Scene, điểm hồi sinh luôn đi theo.
        /// </summary>
        public void GetDynamicSpawnPose(out Vector3 pos, out Quaternion rot)
        {
            if (spawnZone == null)
            {
                var sz = GameObject.Find("Spawn_Zone");
                if (sz != null) spawnZone = sz.transform;
            }

            if (spawnZone != null)
            {
                pos = spawnZone.TransformPoint(spawnPositionOffset);
                rot = spawnZone.rotation * Quaternion.Euler(spawnRotationOffset);
                if (spawnPoint != null)
                {
                    spawnPoint.SetPositionAndRotation(pos, rot);
                }
                return;
            }

            if (spawnPoint == null)
            {
                var spGo = GameObject.Find("PlayerSpawnPoint");
                if (spGo != null) spawnPoint = spGo.transform;
            }

            if (spawnPoint != null)
            {
                pos = spawnPoint.position;
                rot = spawnPoint.rotation;
                return;
            }

            pos = _spawnPos;
            rot = _spawnRot;
        }

        private void CaptureSpawnPose()
        {
            GetDynamicSpawnPose(out _spawnPos, out _spawnRot);
        }

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                if (_playerHudRoot != null) _playerHudRoot.SetActive(false);
                return;
            }

            UpdateCombatHUD();

            if (_isRespawning) return;

            ReadPlayerInput();
            HandleWeaponFire();
            UpdateShieldRegen();
            UpdateVisualEffects();
        }

        private void FixedUpdate()
        {
            if (_isRespawning) return;
            EnsureRigidbody();
            if (_rb == null || _rb.isKinematic) return;

            if (_collisionRecoilTimer > 0f)
            {
                _collisionRecoilTimer -= Time.fixedDeltaTime;
            }

            ApplyHoverAndGravityPhysics();
            ApplyPropulsionPhysics();
            ApplySteeringPhysics();
        }

        private void ReadPlayerInput()
        {
            if (Input.GetKeyDown(toggleMouseStickKey))
            {
                enableMouseFlightStick = !enableMouseFlightStick;
                _virtualStick = Vector2.zero;
                _virtualStickVel = Vector2.zero;
                _mouseMovedPixels = 0f;
                _mouseSteerArmed = false;
                _lastMouseScreenPos = Input.mousePosition;
            }

            bool wantsBoost = Input.GetKey(KeyCode.LeftShift);
            if (wantsBoost && _currentBoostEnergy > 2f)
            {
                _isBoosting = true;
                _currentBoostEnergy = Mathf.Max(0f, _currentBoostEnergy - boostDrainRate * Time.deltaTime);
            }
            else
            {
                _isBoosting = false;
                _currentBoostEnergy = Mathf.Min(maxBoostEnergy, _currentBoostEnergy + boostRegenRate * Time.deltaTime);
            }

            // 1. Điều khiển ga & quán tính vũ trụ (W tăng ga, thả W lướt quán tính vũ trụ, S phanh ngược / lùi)
            float forwardInput = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) forwardInput = 1f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) forwardInput = -1f;
            _throttleInput = forwardInput;

            if (forwardInput > 0f)
            {
                _targetForwardSpeed = _isBoosting ? boostSpeed : cruiseSpeed;
            }
            else if (forwardInput < 0f)
            {
                _targetForwardSpeed = -reverseSpeed;
            }
            else
            {
                _targetForwardSpeed = 0f;
            }

            // Trong lúc đang nhào lộn (Roll / Loop): duy trì tốc độ lướt quán tính cao
            if (_isAcrobaticRolling || _isAcrobaticLooping)
            {
                _targetForwardSpeed = Mathf.Max(_targetForwardSpeed, cruiseSpeed * 1.15f);
            }

            float speedLerpRate;
            if (forwardInput > 0f)
                speedLerpRate = acceleration;
            else if (forwardInput < 0f)
                speedLerpRate = _currentForwardSpeed > 0f ? retroBrakeRate : acceleration;
            else
                speedLerpRate = _isHoveringOverGround ? (acceleration * 0.9f) : spaceCoastDrag;

            _currentForwardSpeed = Mathf.Lerp(_currentForwardSpeed, _targetForwardSpeed, Time.deltaTime * speedLerpRate);

            float vertInput = 0f;
            if (Input.GetKey(KeyCode.Space)) vertInput = 1f;
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) vertInput = -1f;

            _targetVerticalSpeed = vertInput * verticalSpeed;
            _currentVerticalSpeed = Mathf.Lerp(_currentVerticalSpeed, _targetVerticalSpeed, Time.deltaTime * acceleration);

            // 2. Cần lái ảo bằng chuột (Virtual Flight Stick) — chỉ kích hoạt sau khi chuột di chuyển trong khung game
            UpdateMouseVirtualStick();

            // 3. Kích hoạt các đòn Nhào Lộn (Acrobatic Maneuvers):
            // Q / E giữ nguyên để lăn cánh liên tục; nhấp đúp A/D hoặc Q/E để Barrel Roll 360 độ; R/F để Vertical Loop 360 độ.
            if (enableAcrobaticFlight)
            {
                if (Input.GetKeyDown(KeyCode.A))
                {
                    if (Time.time - _lastTapATime < 0.28f && !_isAcrobaticRolling && !_isAcrobaticLooping)
                        TriggerBarrelRoll(-1f);
                    _lastTapATime = Time.time;
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    if (Time.time - _lastTapDTime < 0.28f && !_isAcrobaticRolling && !_isAcrobaticLooping)
                        TriggerBarrelRoll(1f);
                    _lastTapDTime = Time.time;
                }

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    if (Time.time - _lastTapQTime < 0.25f && !_isAcrobaticRolling && !_isAcrobaticLooping)
                        TriggerBarrelRoll(-1f);
                    _lastTapQTime = Time.time;
                }
                else if (Input.GetKeyDown(KeyCode.E))
                {
                    if (Time.time - _lastTapETime < 0.25f && !_isAcrobaticRolling && !_isAcrobaticLooping)
                        TriggerBarrelRoll(1f);
                    _lastTapETime = Time.time;
                }

                if ((Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F)) && !_isAcrobaticRolling && !_isAcrobaticLooping)
                {
                    TriggerVerticalLoop();
                }
            }
        }

        private void UpdateMouseVirtualStick()
        {
            if (AboutInfoStation.AnyStationActive)
            {
                _virtualStick = Vector2.zero;
                _virtualStickVel = Vector2.zero;
                _mouseSteerArmed = false;
                _mouseMovedPixels = 0f;
                return;
            }

            Vector3 mPos = Input.mousePosition;
            bool insideViewport = Screen.width > 10 && Screen.height > 10
                && mPos.x >= 2f && mPos.x <= Screen.width - 2f
                && mPos.y >= 2f && mPos.y <= Screen.height - 2f;

            if (!_mouseSteerArmed)
            {
                if (insideViewport)
                {
                    if (_lastMouseScreenPos.sqrMagnitude < 0.01f)
                    {
                        _lastMouseScreenPos = mPos;
                    }
                    else
                    {
                        _mouseMovedPixels += Vector2.Distance(mPos, _lastMouseScreenPos);
                        _lastMouseScreenPos = mPos;
                        if (_mouseMovedPixels > 10f || Input.GetMouseButton(1))
                        {
                            _mouseSteerArmed = true;
                        }
                    }
                }
                _virtualStick = Vector2.zero;
                _virtualStickVel = Vector2.zero;
                return;
            }

            _lastMouseScreenPos = mPos;
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!enableMouseFlightStick || !insideViewport || (pointerOverUI && !Input.GetMouseButton(1)))
            {
                _virtualStick = Vector2.SmoothDamp(_virtualStick, Vector2.zero, ref _virtualStickVel, 0.08f, Mathf.Infinity, dt);
                return;
            }

            float minDim = Mathf.Max(200f, Mathf.Min(Screen.width, Screen.height));
            float radiusPx = minDim * 0.44f;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 rawOffset = ((Vector2)mPos - center) / radiusPx;
            float mag = rawOffset.magnitude;

            if (mag <= mouseSteerDeadzone)
            {
                _virtualStick = Vector2.SmoothDamp(_virtualStick, Vector2.zero, ref _virtualStickVel, 0.06f, Mathf.Infinity, dt);
                return;
            }

            float scaledMag = Mathf.Clamp01((mag - mouseSteerDeadzone) / Mathf.Max(0.15f, 1f - mouseSteerDeadzone));
            float precisionCurve = Mathf.Lerp(scaledMag * scaledMag, Mathf.Pow(scaledMag, 1.55f), 0.45f);
            float curvedMag = precisionCurve * mouseSteerSensitivity;
            Vector2 targetStick = rawOffset.normalized * Mathf.Clamp01(curvedMag);
            if (invertMousePitch) targetStick.y = -targetStick.y;

            _virtualStick = Vector2.SmoothDamp(_virtualStick, targetStick, ref _virtualStickVel, 0.065f, Mathf.Infinity, dt);
        }

        public void TriggerBarrelRoll(float direction)
        {
            if (_isAcrobaticRolling || _isAcrobaticLooping) return;
            _isAcrobaticRolling = true;
            _acrobaticRollTimer = 0f;
            _acrobaticRollDirection = direction;
            _lastSteeringInputTime = Time.time;

            if (_currentForwardSpeed < cruiseSpeed * 0.9f)
            {
                _currentForwardSpeed = cruiseSpeed;
            }
        }

        public void TriggerVerticalLoop()
        {
            if (_isAcrobaticRolling || _isAcrobaticLooping) return;
            _isAcrobaticLooping = true;
            _acrobaticLoopTimer = 0f;
            _lastSteeringInputTime = Time.time;

            if (_currentForwardSpeed < cruiseSpeed * 0.9f)
            {
                _currentForwardSpeed = cruiseSpeed * 1.1f;
            }
        }

        private void HandleWeaponFire()
        {
            if (AboutInfoStation.AnyStationActive) return;

            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool firePressed = (!pointerOverUI && Input.GetMouseButton(0)) || Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.LeftAlt);

            if (firePressed && Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + fireInterval;
                FireTwinCannons();
            }
        }

        private void FireTwinCannons()
        {
            if (_targetLockSystem == null) _targetLockSystem = GetComponent<SpaceshipTargetLockSystem>();

            Vector3 convergePoint = transform.position + transform.forward * 90f;
            Transform homingTarget = null;

            if (_targetLockSystem != null && _targetLockSystem.HasLockedTarget)
            {
                if (_targetLockSystem.TryGetLockAimPoint(out Vector3 lockPoint))
                {
                    convergePoint = lockPoint;
                    homingTarget = _targetLockSystem.CurrentLockedTarget;
                }
            }

            // Tọa độ pháo kép mở rộng theo thân tàu phóng to 1.5x
            Vector3 leftMuzzle = transform.position + transform.forward * 4.2f - transform.right * 2.05f + transform.up * 0.38f;
            Vector3 rightMuzzle = transform.position + transform.forward * 4.2f + transform.right * 2.05f + transform.up * 0.38f;

            Vector3 leftDir = (convergePoint - leftMuzzle).normalized;
            Vector3 rightDir = (convergePoint - rightMuzzle).normalized;

            SpaceLaserProjectile.SpawnBolt(leftMuzzle, leftDir, true, gameObject, 125f, laserDamage, homingTarget);
            SpaceLaserProjectile.SpawnBolt(rightMuzzle, rightDir, true, gameObject, 125f, laserDamage, homingTarget);

            _lastFireTime = Time.time;
            if (_cockpit1P == null) _cockpit1P = FindFirstObjectByType<SpaceshipCockpit1P>();
            if (_cockpit1P != null) _cockpit1P.NotifyWeaponFire();
        }

        private void UpdateShieldRegen()
        {
            if (Time.time - _lastDamageTime > 3.5f)
            {
                if (_currentShield < maxShield)
                {
                    bool wasDepleted = _currentShield <= 0.05f;
                    _currentShield = Mathf.Min(maxShield, _currentShield + shieldRegenRate * Time.deltaTime);
                    if (wasDepleted && _shieldEffect != null)
                    {
                        _shieldEffect.TriggerShieldRestore();
                    }
                }
                else if (_currentHealth < maxHealth)
                {
                    _currentHealth = Mathf.Min(maxHealth, _currentHealth + (shieldRegenRate * 0.6f) * Time.deltaTime);
                }
            }
        }

        public void RechargeEnergy(float rate, bool instantFull = false)
        {
            if (_isRespawning) return;
            if (instantFull)
            {
                _currentHealth = maxHealth;
                _currentShield = maxShield;
                _currentBoostEnergy = maxBoostEnergy;
                if (_shieldEffect != null && _shieldEffect.IsDissolved)
                {
                    _shieldEffect.TriggerShieldRestore();
                }
            }
            else
            {
                if (_currentHealth < maxHealth)
                {
                    _currentHealth = Mathf.Min(maxHealth, _currentHealth + rate * Time.deltaTime);
                }
                if (_currentShield < maxShield)
                {
                    bool wasDepleted = _currentShield <= 0.05f;
                    _currentShield = Mathf.Min(maxShield, _currentShield + rate * Time.deltaTime);
                    if (wasDepleted && _shieldEffect != null)
                    {
                        _shieldEffect.TriggerShieldRestore();
                    }
                }
                _currentBoostEnergy = Mathf.Min(maxBoostEnergy, _currentBoostEnergy + rate * 1.5f * Time.deltaTime);
            }
            _lastDamageTime = -99f;
        }

        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (_isRespawning || AboutInfoStation.AnyStationActive) return;

            _lastDamageTime = Time.time;
            if (_cockpit1P == null) _cockpit1P = FindFirstObjectByType<SpaceshipCockpit1P>();
            if (_cockpit1P != null) _cockpit1P.NotifyDamageImpact();

            if (_currentShield > 0f)
            {
                float absorbed = Mathf.Min(_currentShield, damage);
                _currentShield -= absorbed;
                float leftover = damage - absorbed;

                if (_shieldEffect != null)
                {
                    _shieldEffect.OnShieldHit(hitPoint, _currentShield / maxShield);
                    if (_currentShield <= 0.05f)
                    {
                        _shieldEffect.TriggerShieldDissolve();
                    }
                }

                if (leftover > 0f)
                {
                    _currentHealth = Mathf.Max(0f, _currentHealth - leftover);
                }
            }
            else
            {
                _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            }

            if (_currentHealth <= 0f)
            {
                StartCoroutine(ExplodeAndRespawnRoutine());
            }
        }

        public void RespawnImmediate()
        {
            CaptureSpawnPose();
            SpaceTrafficShipAI.ClearFleetHostileAlert();

            _currentForwardSpeed = 0f;
            _targetForwardSpeed = 0f;
            _currentVerticalSpeed = 0f;
            _targetVerticalSpeed = 0f;
            _currentBank = 0f;
            _currentPitch = 0f;
            _isAcrobaticRolling = false;
            _isAcrobaticLooping = false;
            _impactRollJolt = 0f;
            _impactPitchJolt = 0f;
            _yawRate = 0f;
            _pitchRate = 0f;
            _rollRate = 0f;
            _turnInput = 0f;
            _pitchInput = 0f;
            _rollInput = 0f;
            _throttleInput = 0f;
            _virtualStick = Vector2.zero;
            _virtualStickVel = Vector2.zero;
            _rmbStick = Vector2.zero;
            _rmbStickVel = Vector2.zero;
            _smoothedTurnCmd = 0f;
            _smoothedPitchCmd = 0f;
            _smoothedRollCmd = 0f;
            _mouseSteerArmed = false;
            _mouseMovedPixels = 0f;
            _freeRollHoldTimer = 0f;
            _currentGForce = 1f;
            _prevWorldVel = Vector3.zero;
            _currentYaw = _spawnRot.eulerAngles.y;
            _currentHealth = maxHealth;
            _currentShield = maxShield;
            _currentBoostEnergy = maxBoostEnergy;
            _displayedHpRatio = 1f;
            _displayedShieldRatio = 1f;
            _displayedBoostRatio = 1f;
            _lastDamageTime = -99f;

            if (_shieldEffect != null)
            {
                _shieldEffect.SetDissolveInstant(0f);
            }

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.position = _spawnPos;
                _rb.rotation = _spawnRot;
            }
            transform.SetPositionAndRotation(_spawnPos, _spawnRot);
            Physics.SyncTransforms();

            if (visualModel != null)
            {
                visualModel.gameObject.SetActive(true);
                visualModel.localRotation = Quaternion.identity;
            }
            SetThirdPersonShipVisualsVisible(_thirdPersonVisualsVisible);

            var camFollow = FindFirstObjectByType<SpaceshipCameraFollow>();
            if (camFollow != null)
            {
                camFollow.SnapToTargetImmediate();
            }
        }

        private IEnumerator ExplodeAndRespawnRoutine()
        {
            _isRespawning = true;
            SpaceExplosionVFX.SpawnExplosion(transform.position, 5.2f, false, null);
            SpaceTrafficShipAI.ClearFleetHostileAlert();

            if (_shieldEffect != null)
            {
                _shieldEffect.SetDissolveInstant(1f);
            }

            if (visualModel != null) visualModel.gameObject.SetActive(false);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            yield return new WaitForSeconds(1.5f);

            RespawnImmediate();
            _isRespawning = false;
        }

        private float _computedHoverVerticalSpeed = 0f;

        private void ApplyHoverAndGravityPhysics()
        {
            Vector3 rayOrigin = transform.position + (Vector3.up * 0.2f);
            float checkDist = targetHoverHeight + 4.5f;

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, checkDist, groundLayer, QueryTriggerInteraction.Ignore);
            bool hitGround = false;
            float closestDist = 999f;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.transform.root != transform.root)
                {
                    if (hits[i].distance < closestDist)
                    {
                        closestDist = hits[i].distance;
                        hitGround = true;
                    }
                }
            }

            _isHoveringOverGround = hitGround;
            _groundDistance = hitGround ? (closestDist - 0.2f) : 999f;

            if (hitGround && !_isAcrobaticRolling && !_isAcrobaticLooping)
            {
                // Chỉ áp dụng đệm nâng khi tàu ở tư thế gần bằng phẳng và không đang nhào lộn
                float upDot = Vector3.Dot(transform.up, Vector3.up);
                if (upDot > 0.35f)
                {
                    float baseHeight = (_targetVerticalSpeed < -0.1f) ? 0.35f : targetHoverHeight;
                    float bobOffset = (_targetVerticalSpeed < -0.1f && _groundDistance < 0.6f) ? 0f : (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
                    float desiredHeight = baseHeight + bobOffset;

                    float error = desiredHeight - _groundDistance;
                    _computedHoverVerticalSpeed = Mathf.Clamp(error * 6.5f, -6f, 8f);

                    // Tiếp đất ổn định: triệt tiêu trượt ngang khi đỗ
                    if (_groundDistance <= 0.45f && Mathf.Abs(_currentForwardSpeed) < 3f)
                    {
                        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                        _rb.AddForce(-flatVel * 6f, ForceMode.Acceleration);
                    }
                }
                else
                {
                    _computedHoverVerticalSpeed = 0f;
                }
            }
            else
            {
                _computedHoverVerticalSpeed = 0f;
            }
        }

        private void ApplyPropulsionPhysics()
        {
            EnsureRigidbody();
            if (_rb == null || _rb.isKinematic) return;

            if (!disableCollisionBrake && _collisionRecoilTimer > 0f)
            {
                _contactNormalSum = Vector3.zero;
                return;
            }

            // Hướng bay 3D thực của phi thuyền + đẩy dọc theo trục nâng của tàu khi ở ngoài vũ trụ
            Vector3 forwardMove = transform.forward * _currentForwardSpeed;
            Vector3 desiredVel;

            if (!_isHoveringOverGround && Mathf.Abs(_targetVerticalSpeed) >= 0.05f && !_isAcrobaticRolling && !_isAcrobaticLooping)
            {
                desiredVel = forwardMove + transform.up * _currentVerticalSpeed;
            }
            else
            {
                float vertVel = _currentVerticalSpeed;
                if (Mathf.Abs(_targetVerticalSpeed) < 0.1f)
                {
                    if (_isHoveringOverGround && !_isAcrobaticRolling && !_isAcrobaticLooping)
                    {
                        vertVel = _computedHoverVerticalSpeed;
                    }
                    else if (!_isHoveringOverGround && !_isAcrobaticRolling && !_isAcrobaticLooping)
                    {
                        if (_currentForwardSpeed > 3f)
                        {
                            vertVel = Mathf.Max(0f, _rb.linearVelocity.y * 0.96f);
                        }
                        else
                        {
                            vertVel = Mathf.Lerp(_rb.linearVelocity.y, -gravity * 0.35f, Time.fixedDeltaTime * 2.5f);
                        }
                    }
                }
                desiredVel = forwardMove + Vector3.up * vertVel;
            }

            // 1. Trượt lướt mượt mà khi tiếp xúc sườn vật cản, không khựng phanh
            if (_contactNormalSum.sqrMagnitude > 0.0001f)
            {
                Vector3 contactNormal = _contactNormalSum.normalized;
                if (Vector3.Dot(desiredVel, contactNormal) < 0f)
                {
                    desiredVel = Vector3.ProjectOnPlane(desiredVel, contactNormal);
                }
                _contactNormalSum = Vector3.zero;
            }

            // 2. Chống xuyên tường (Anti-tunneling): Quét chướng ngại vật phía trước thân tàu
            float speedMag = desiredVel.magnitude;
            if (speedMag > 0.5f)
            {
                Vector3 moveDir = desiredVel / speedMag;
                float sweepDist = Mathf.Max(1.8f, speedMag * Time.fixedDeltaTime * 1.5f + 1.0f);
                Vector3 castOrigin = transform.position + Vector3.up * 0.75f;
                RaycastHit[] hits = Physics.SphereCastAll(castOrigin, 0.85f, moveDir, sweepDist, ~0, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider col = hits[i].collider;
                    if (col == null || col.isTrigger || col.transform.root == transform.root) continue;
                    if (hits[i].distance <= 0.001f) continue;

                    if (hits[i].point.y < transform.position.y + 0.15f) continue;
                    Vector3 hitNormal = hits[i].normal;
                    if (Mathf.Abs(hitNormal.y) > 0.65f) continue;

                    float intoWall = Vector3.Dot(desiredVel, hitNormal);
                    if (intoWall < 0f && hits[i].distance < 1.8f)
                    {
                        desiredVel = Vector3.ProjectOnPlane(desiredVel, hitNormal);
                    }
                }
            }

            // 3. Nội suy vận tốc quán tính thực tế (tạo cảm giác slip khi cua gấp & bảo toàn xung lực từ SpeedBoostRing)
            Vector3 currentVel = _rb.linearVelocity;
            float resp = _isHoveringOverGround ? hoverVelocityResponsiveness : velocityResponsiveness;
            if (!_isHoveringOverGround && _throttleInput >= 0f && currentVel.sqrMagnitude > desiredVel.sqrMagnitude + 16f && Vector3.Dot(currentVel, transform.forward) > 10f)
            {
                resp = 1.8f;
            }

            Vector3 nextVel = Vector3.Lerp(currentVel, desiredVel, Time.fixedDeltaTime * resp);
            _rb.linearVelocity = nextVel;

            float dt = Mathf.Max(0.005f, Time.fixedDeltaTime);
            Vector3 accel = (nextVel - _prevWorldVel) / dt;
            float turnG = Mathf.Abs(Vector3.Dot(accel, transform.up) + Vector3.Dot(accel, transform.right) * 0.55f) / 9.81f;
            float fwdG = Vector3.Dot(accel, transform.forward) / 9.81f;
            float rawG = 1f + Mathf.Clamp(turnG + Mathf.Max(0f, fwdG) * 0.65f, 0f, 8.5f);
            _currentGForce = Mathf.Lerp(_currentGForce, rawG, dt * 7f);
            _prevWorldVel = nextVel;
        }

        private void ApplySteeringPhysics()
        {
            EnsureRigidbody();
            if (_rb == null || _rb.isKinematic) return;

            float dt = Time.fixedDeltaTime;

            // 1. Triệt tiêu lực xoáy văng vật lý sau va chạm
            if (_rb.angularVelocity.sqrMagnitude > 0.0001f)
            {
                _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, dt * gyroAngularDamping);
            }

            // 2. Thực hiện cú Barrel Roll 360 độ (lăn lộn vòng quanh trục mũi tàu)
            if (_isAcrobaticRolling)
            {
                _acrobaticRollTimer += dt;
                float rollDelta = (_acrobaticRollDirection * 360f / barrelRollDuration) * dt;
                Quaternion rollStep = Quaternion.AngleAxis(rollDelta, transform.forward);
                _rb.MoveRotation(rollStep * _rb.rotation);

                if (_acrobaticRollTimer >= barrelRollDuration)
                {
                    _isAcrobaticRolling = false;
                    _currentYaw = transform.eulerAngles.y;
                    _currentPitch = NormalizeAngle(transform.eulerAngles.x);
                    _rollRate = 0f;
                }
                return;
            }

            // 3. Thực hiện cú Vertical Loop 360 độ (lộn nhào vòng cung thẳng đứng qua đỉnh đầu)
            if (_isAcrobaticLooping)
            {
                _acrobaticLoopTimer += dt;
                float pitchDelta = (-360f / loopDuration) * dt;
                Quaternion pitchStep = Quaternion.AngleAxis(pitchDelta, transform.right);
                _rb.MoveRotation(pitchStep * _rb.rotation);

                if (_acrobaticLoopTimer >= loopDuration)
                {
                    _isAcrobaticLooping = false;
                    _currentYaw = transform.eulerAngles.y;
                    _currentPitch = NormalizeAngle(transform.eulerAngles.x);
                    _pitchRate = 0f;
                }
                return;
            }

            // 4. Lái thực tế theo hệ trục cục bộ: Phím (A/D/I/K/Q/E) + Cần lái ảo chuột (Virtual Stick) + Giữ chuột phải
            float turnInput = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turnInput += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turnInput -= 1f;

            float pitchInput = 0f;
            if (Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.DownArrow)) pitchInput += 1f;
            if (Input.GetKey(KeyCode.I) || Input.GetKey(KeyCode.UpArrow)) pitchInput -= 1f;

            if (enableMouseFlightStick && _mouseSteerArmed)
            {
                turnInput += _virtualStick.x;
                pitchInput -= _virtualStick.y;
                _rmbStick = Vector2.zero;
                _rmbStickVel = Vector2.zero;
            }
            else if (Input.GetMouseButton(1))
            {
                Vector2 rmbTarget = new Vector2(
                    Mathf.Clamp(Input.GetAxis("Mouse X") * 2.2f, -1f, 1f),
                    Mathf.Clamp(-Input.GetAxis("Mouse Y") * 2.2f, -1f, 1f));
                _rmbStick = Vector2.SmoothDamp(_rmbStick, rmbTarget, ref _rmbStickVel, 0.045f, Mathf.Infinity, dt);
                turnInput += _rmbStick.x;
                pitchInput += _rmbStick.y;
            }
            else
            {
                _rmbStick = Vector2.SmoothDamp(_rmbStick, Vector2.zero, ref _rmbStickVel, 0.05f, Mathf.Infinity, dt);
            }

            float rollInput = 0f;
            if (Input.GetKey(KeyCode.Q)) rollInput -= 1f;
            if (Input.GetKey(KeyCode.E)) rollInput += 1f;

            turnInput = Mathf.Clamp(turnInput, -1f, 1f);
            pitchInput = Mathf.Clamp(pitchInput, -1f, 1f);
            rollInput = Mathf.Clamp(rollInput, -1f, 1f);

            float cmdBlend = 1f - Mathf.Exp(-14f * dt);
            _smoothedTurnCmd = Mathf.Lerp(_smoothedTurnCmd, turnInput, cmdBlend);
            _smoothedPitchCmd = Mathf.Lerp(_smoothedPitchCmd, pitchInput, cmdBlend);
            _smoothedRollCmd = Mathf.Lerp(_smoothedRollCmd, rollInput, cmdBlend);

            _turnInput = _smoothedTurnCmd;
            _pitchInput = _smoothedPitchCmd;
            _rollInput = _smoothedRollCmd;

            bool hasSteeringInput = (Mathf.Abs(turnInput) > 0.03f || Mathf.Abs(pitchInput) > 0.03f || Mathf.Abs(rollInput) > 0.03f);
            if (hasSteeringInput)
            {
                _lastSteeringInputTime = Time.time;
            }

            if (Mathf.Abs(rollInput) > 0.04f)
            {
                _freeRollHoldTimer = 1.6f;
            }
            else
            {
                _freeRollHoldTimer = Mathf.Max(0f, _freeRollHoldTimer - dt);
            }

            float rateBlend = 1f - Mathf.Exp(-angularResponse * dt);
            _yawRate = Mathf.Lerp(_yawRate, _smoothedTurnCmd * turnSpeed, rateBlend);
            _pitchRate = Mathf.Lerp(_pitchRate, _smoothedPitchCmd * pitchSpeed, rateBlend);
            _rollRate = Mathf.Lerp(_rollRate, _smoothedRollCmd * rollSpeed, rateBlend);

            // Tích hợp góc xoay Quaternion cục bộ (không khóa Gimbal khi bay chúc/ngóc góc đứng 90° hay nhào lộn)
            Quaternion nextRot = _rb.rotation
                * Quaternion.AngleAxis(_yawRate * dt, Vector3.up)
                * Quaternion.AngleAxis(_pitchRate * dt, Vector3.right)
                * Quaternion.AngleAxis(-_rollRate * dt, Vector3.forward);

            // Cân bằng chân trời tự nhiên khi thả cần roll hoặc chuẩn bị hạ cánh trên bãi đỗ
            if (_freeRollHoldTimer <= 0f && (!hasSteeringInput || _groundDistance < 5.0f))
            {
                Vector3 fwd = nextRot * Vector3.forward;
                Vector3 up = nextRot * Vector3.up;
                if (up.y > 0.25f && Mathf.Abs(fwd.y) < 0.88f)
                {
                    Vector3 targetUp = Vector3.ProjectOnPlane(Vector3.up, fwd).normalized;
                    if (targetUp.sqrMagnitude > 0.01f)
                    {
                        float rollErr = Vector3.SignedAngle(up, targetUp, fwd);
                        float alignSpeed = _groundDistance < 5.0f ? horizonStabilizeSpeed : 1.35f;
                        nextRot = Quaternion.AngleAxis(rollErr * Mathf.Clamp01(dt * alignSpeed), fwd) * nextRot;
                    }

                    if (_groundDistance < 4.5f && !hasSteeringInput && (Time.time - _lastSteeringInputTime) > 0.9f)
                    {
                        Vector3 flatFwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
                        if (flatFwd.sqrMagnitude > 0.01f)
                        {
                            Quaternion levelRot = Quaternion.LookRotation(flatFwd, Vector3.up);
                            nextRot = Quaternion.Slerp(nextRot, levelRot, dt * pitchAutoLevelSpeed);
                        }
                    }
                }
            }

            _rb.MoveRotation(nextRot);

            // Đồng bộ Euler tham khảo cho các hệ thống ngoài
            _currentYaw = nextRot.eulerAngles.y;
            _currentPitch = NormalizeAngle(nextRot.eulerAngles.x);

            // Hiệu ứng nghiêng cánh khí động học & rung buồng lái (Aerodynamic Banking)
            _impactRollJolt = Mathf.Lerp(_impactRollJolt, 0f, dt * 7.5f);
            _impactPitchJolt = Mathf.Lerp(_impactPitchJolt, 0f, dt * 7.5f);

            float targetBank = -_turnInput * maxBankAngle - (_rollInput * 12f);
            float swayOffset = Mathf.Sin(Time.time * (bobFrequency * 0.8f)) * 1.1f;
            float bankAlpha = 1f - Mathf.Exp(-bankSmoothing * dt);
            _currentBank = Mathf.Lerp(_currentBank, targetBank + swayOffset, bankAlpha);

            if (visualModel != null)
            {
                visualModel.localRotation = Quaternion.Euler(_impactPitchJolt, 0f, _currentBank + _impactRollJolt);
            }
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private void OnCollisionEnter(Collision collision)
        {
            EnsureRigidbody();
            if (_rb == null || _isRespawning) return;

            if (collision.relativeVelocity.sqrMagnitude > 2.2f)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 normal = contact.normal;
                _contactNormalSum += normal;

                if (disableCollisionBrake)
                {
                    // Chế độ bay tự do: trượt mượt mà theo bề mặt phẳng của vật cản, không thắng giật lùi, không ngắt động cơ
                    if (!_rb.isKinematic)
                    {
                        Vector3 vel = _rb.linearVelocity;
                        if (Vector3.Dot(vel, normal) < 0f)
                        {
                            _rb.linearVelocity = Vector3.ProjectOnPlane(vel, normal);
                        }
                    }
                    return;
                }

                if (Vector3.Dot(normal, Vector3.up) < 0.65f)
                {
                    _collisionRecoilTimer = 0.24f;
                    _currentForwardSpeed *= -0.35f;
                    _rb.AddForce(normal * Mathf.Clamp(collision.relativeVelocity.magnitude * 0.5f, 4.5f, 18f), ForceMode.VelocityChange);

                    // Tạo hiệu ứng nghiêng chấn động ngắn trên thân tàu rồi tự cân bằng lại mượt mà
                    Vector3 localHit = transform.InverseTransformPoint(contact.point);
                    float impactStrength = Mathf.Clamp01(collision.relativeVelocity.magnitude / 30f);
                    _impactRollJolt = Mathf.Clamp(-localHit.x * 18f * impactStrength, -25f, 25f);
                    _impactPitchJolt = Mathf.Clamp(localHit.z * 12f * impactStrength, -18f, 18f);
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            EnsureRigidbody();
            if (_rb == null || _isRespawning) return;

            int count = collision.contactCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 n = collision.GetContact(i).normal;
                // Chỉ nhận các vách tường bên thẳng đứng thực sự (bỏ qua sàn và mép gờ dưới bụng)
                if (Mathf.Abs(Vector3.Dot(n, Vector3.up)) < 0.45f)
                {
                    _contactNormalSum += n;
                }
            }
        }

        private void UpdateVisualEffects()
        {
            if (_cachedShieldBubbleGo == null)
            {
                var sb = transform.Find("Player_HexShield_Bubble");
                if (sb != null) _cachedShieldBubbleGo = sb.gameObject;
            }
            if (_cachedShieldBubbleGo != null && _cachedShieldBubbleGo.activeSelf != _thirdPersonVisualsVisible)
            {
                _cachedShieldBubbleGo.SetActive(_thirdPersonVisualsVisible);
            }

            if (!_thirdPersonVisualsVisible)
            {
                if (engineLight != null && engineLight.enabled) engineLight.enabled = false;
                return;
            }

            if (engineLight != null)
            {
                if (!engineLight.enabled) engineLight.enabled = true;
                float baseIntensity = _isBoosting ? 4.5f : (_currentForwardSpeed > 1f ? 2.2f : 0.8f);
                float pulse = Mathf.Sin(Time.time * 15f) * 0.15f;
                engineLight.intensity = Mathf.Lerp(engineLight.intensity, baseIntensity + pulse, Time.deltaTime * 10f);
                engineLight.color = _isBoosting ? new Color(1f, 0.4f, 0.1f) : new Color(0.2f, 0.8f, 1f);
            }

            UpdateThrusterParticle(leftThrusterParticle);
            UpdateThrusterParticle(rightThrusterParticle);
        }

        private void UpdateThrusterParticle(ParticleSystem ps)
        {
            if (ps == null) return;

            var emission = ps.emission;
            var main = ps.main;

            if (_isBoosting)
            {
                emission.rateOverTime = 80f;
                main.startSpeed = 8f;
                main.startSize = 0.55f;
            }
            else if (_currentForwardSpeed > 1f)
            {
                emission.rateOverTime = 40f;
                main.startSpeed = 4f;
                main.startSize = 0.35f;
            }
            else
            {
                emission.rateOverTime = 15f;
                main.startSpeed = 1.8f;
                main.startSize = 0.22f;
            }
        }

        // ─────────────────── SCI-FI CYBERPUNK CANVAS FLIGHT HUD (THEO THIẾT KẾ ẢNH 1) ───────────────────
        private void EnsurePlayerSciFiHUD()
        {
            if (_playerHudRoot != null) return;

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            var oldCombat = canvas.transform.Find("CombatHUD_Root");
            if (oldCombat != null) Destroy(oldCombat.gameObject);

            var oldSciFi = canvas.transform.Find("PlayerSciFiHUD");
            if (oldSciFi != null) Destroy(oldSciFi.gameObject);

            _playerHudRoot = new GameObject("PlayerSciFiHUD", typeof(RectTransform));
            _playerHudRoot.transform.SetParent(canvas.transform, false);
            _playerHudRoot.transform.SetAsFirstSibling();

            var rootRt = _playerHudRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // 1. Màn hình nháy đỏ & 4 viền cảnh báo khi bị trúng đạn (Hit Vignette)
            _redScreenOverlay = CreateHudImage("RedScreenFlash", _playerHudRoot.transform, new Color(0.95f, 0.04f, 0.06f, 0f));
            var overlayRt = _redScreenOverlay.rectTransform;
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            _redBorders = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                _redBorders[i] = CreateHudImage("RedEdge_" + i, _playerHudRoot.transform, new Color(1f, 0.05f, 0.08f, 0f));
                var brt = _redBorders[i].rectTransform;
                if (i == 0) { brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0.5f, 1f); brt.sizeDelta = new Vector2(0, 22f); }
                if (i == 1) { brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0.5f, 0f); brt.sizeDelta = new Vector2(0, 22f); }
                if (i == 2) { brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(0, 1); brt.pivot = new Vector2(0f, 0.5f); brt.sizeDelta = new Vector2(22f, 0); }
                if (i == 3) { brt.anchorMin = new Vector2(1, 0); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(1f, 0.5f); brt.sizeDelta = new Vector2(22f, 0); }
            }

            // 2. Thước đo la bàn trên đỉnh (Heading Compass Tape)
            BuildCompassHeadingTape(_playerHudRoot.transform);

            // 3. Tâm ngắm trung tâm (Hexagon Lock Reticle HUD do SpaceshipTargetLockSystem quản lý)
            BuildCenterCrosshair(_playerHudRoot.transform);
            if (_crosshairRoot != null) _crosshairRoot.SetActive(false);
            BuildVirtualStickOverlay(_playerHudRoot.transform);

            // 4. Góc dưới trái: Cụm thanh HP & SHD
            BuildBottomLeftHealthShieldBars(_playerHudRoot.transform);

            // 5. Góc dưới phải: SPEED & BOOST
            BuildBottomRightSpeedBoostBars(_playerHudRoot.transform);

            // 6. Bảng cảnh báo: Chỉ hiện khi bị tấn công ("bị tấn công thì ms hiện cái cảnh báo")
            BuildAttackWarningBanner(_playerHudRoot.transform);
        }

        private void BuildCompassHeadingTape(Transform parent)
        {
            var compassGo = new GameObject("CompassHeadingTape", typeof(RectTransform));
            compassGo.transform.SetParent(parent, false);
            _compassTapeRoot = compassGo.GetComponent<RectTransform>();
            _compassTapeRoot.anchorMin = new Vector2(0.5f, 1f);
            _compassTapeRoot.anchorMax = new Vector2(0.5f, 1f);
            _compassTapeRoot.pivot = new Vector2(0.5f, 1f);
            _compassTapeRoot.anchoredPosition = new Vector2(0f, -14f);
            _compassTapeRoot.sizeDelta = new Vector2(460f, 26f);

            // Mũi tên chỉ hướng chính giữa đỉnh 'v'
            _compassArrow = CreateHudText("CenterArrow", compassGo.transform, "v", 12f, new Color(0.85f, 0.95f, 1f, 0.95f), TextAlignmentOptions.Center);
            var arrowRt = _compassArrow.rectTransform;
            arrowRt.anchorMin = new Vector2(0.5f, 1f);
            arrowRt.anchorMax = new Vector2(0.5f, 1f);
            arrowRt.pivot = new Vector2(0.5f, 1f);
            arrowRt.anchoredPosition = new Vector2(0f, 0f);
            arrowRt.sizeDelta = new Vector2(30f, 14f);

            // Đường kẻ ngang mảnh chạy dọc thước đo
            var baseLine = CreateHudImage("CompassBaseLine", compassGo.transform, new Color(0.7f, 0.85f, 1f, 0.45f));
            baseLine.rectTransform.anchorMin = new Vector2(0f, 0f);
            baseLine.rectTransform.anchorMax = new Vector2(1f, 0f);
            baseLine.rectTransform.pivot = new Vector2(0.5f, 0f);
            baseLine.rectTransform.sizeDelta = new Vector2(0f, 1.5f);
            baseLine.rectTransform.anchoredPosition = new Vector2(0f, 12f);

            // Các vạch chia độ trượt ngang (21 vạch)
            int tickCount = 21;
            _compassTickLines = new RectTransform[tickCount];
            for (int i = 0; i < tickCount; i++)
            {
                var tickImg = CreateHudImage("Tick_" + i, compassGo.transform, new Color(0.75f, 0.9f, 1f, 0.65f));
                var trt = tickImg.rectTransform;
                trt.anchorMin = new Vector2(0.5f, 0f);
                trt.anchorMax = new Vector2(0.5f, 0f);
                trt.pivot = new Vector2(0.5f, 0f);
                trt.sizeDelta = new Vector2(1.5f, (i % 2 == 0) ? 9f : 5f);
                _compassTickLines[i] = trt;
            }
        }

        private void BuildCenterCrosshair(Transform parent)
        {
            _crosshairRoot = new GameObject("CrosshairReticle", typeof(RectTransform));
            _crosshairRoot.transform.SetParent(parent, false);
            var crt = _crosshairRoot.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(80f, 80f);

            Color reticleColor = new Color(0.85f, 0.95f, 1f, 0.75f);

            // Điểm chấm tròn nhỏ ngay tâm
            var dot = CreateHudImage("CenterDot", _crosshairRoot.transform, reticleColor);
            dot.rectTransform.anchoredPosition = Vector2.zero;
            dot.rectTransform.sizeDelta = new Vector2(4f, 4f);

            // 4 Góc ngoặc vuông [ · ]
            float bracketDist = 32f;
            float segLen = 14f;
            float segThick = 2f;

            CreateCornerBracket("TL", _crosshairRoot.transform, new Vector2(-bracketDist, bracketDist), segLen, segThick, true, true, reticleColor);
            CreateCornerBracket("TR", _crosshairRoot.transform, new Vector2(bracketDist, bracketDist), segLen, segThick, false, true, reticleColor);
            CreateCornerBracket("BL", _crosshairRoot.transform, new Vector2(-bracketDist, -bracketDist), segLen, segThick, true, false, reticleColor);
            CreateCornerBracket("BR", _crosshairRoot.transform, new Vector2(bracketDist, -bracketDist), segLen, segThick, false, false, reticleColor);
        }

        private static void CreateCornerBracket(string name, Transform parent, Vector2 pos, float len, float thick, bool isLeft, bool isTop, Color color)
        {
            var h = CreateHudImage(name + "_H", parent, color);
            h.rectTransform.sizeDelta = new Vector2(len, thick);
            h.rectTransform.pivot = new Vector2(isLeft ? 0f : 1f, isTop ? 1f : 0f);
            h.rectTransform.anchoredPosition = pos;

            var v = CreateHudImage(name + "_V", parent, color);
            v.rectTransform.sizeDelta = new Vector2(thick, len);
            v.rectTransform.pivot = new Vector2(isLeft ? 0f : 1f, isTop ? 1f : 0f);
            v.rectTransform.anchoredPosition = pos;
        }

        private void BuildBottomLeftHealthShieldBars(Transform parent)
        {
            var panel = new GameObject("BottomLeft_HP_SHD_Cluster", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 0f);
            prt.pivot = new Vector2(0f, 0f);
            prt.anchoredPosition = new Vector2(40f, 32f);
            prt.sizeDelta = new Vector2(260f, 48f);

            // 1. Dòng HP
            _hpLabel = CreateHudText("HP_Label", panel.transform, "HP", 14f, new Color(0.85f, 0.95f, 1f, 0.9f), TextAlignmentOptions.MidlineLeft);
            var hpLblRt = _hpLabel.rectTransform;
            hpLblRt.anchorMin = new Vector2(0f, 0.72f);
            hpLblRt.anchorMax = new Vector2(0f, 0.72f);
            hpLblRt.pivot = new Vector2(0f, 0.5f);
            hpLblRt.anchoredPosition = new Vector2(0f, 0f);
            hpLblRt.sizeDelta = new Vector2(36f, 18f);

            var hpTrack = CreateHudImage("HP_Track", panel.transform, new Color(0.04f, 0.08f, 0.14f, 0.85f));
            var hpTrackRt = hpTrack.rectTransform;
            hpTrackRt.anchorMin = new Vector2(0f, 0.72f);
            hpTrackRt.anchorMax = new Vector2(0f, 0.72f);
            hpTrackRt.pivot = new Vector2(0f, 0.5f);
            hpTrackRt.anchoredPosition = new Vector2(42f, 0f);
            hpTrackRt.sizeDelta = new Vector2(185f, 11f);

            var hpBorder = CreateHudImage("Border", hpTrack.transform, new Color(0.4f, 0.6f, 0.8f, 0.4f));
            hpBorder.rectTransform.anchorMin = Vector2.zero;
            hpBorder.rectTransform.anchorMax = Vector2.one;
            hpBorder.rectTransform.offsetMin = new Vector2(-1f, -1f);
            hpBorder.rectTransform.offsetMax = new Vector2(1f, 1f);
            hpBorder.transform.SetAsFirstSibling();

            _hpBarLag = CreateHudImage("HP_Lag", hpTrack.transform, new Color(1f, 0.35f, 0.2f, 0.55f));
            _hpBarLag.rectTransform.anchorMin = Vector2.zero;
            _hpBarLag.rectTransform.anchorMax = Vector2.one;
            _hpBarLag.rectTransform.pivot = new Vector2(0f, 0.5f);
            _hpBarLag.rectTransform.offsetMin = Vector2.zero;
            _hpBarLag.rectTransform.offsetMax = Vector2.zero;

            _hpBarFill = CreateHudImage("HP_Fill", hpTrack.transform, new Color(0.92f, 0.96f, 1f, 0.95f));
            _hpBarFill.rectTransform.anchorMin = Vector2.zero;
            _hpBarFill.rectTransform.anchorMax = Vector2.one;
            _hpBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _hpBarFill.rectTransform.offsetMin = Vector2.zero;
            _hpBarFill.rectTransform.offsetMax = Vector2.zero;

            // 2. Dòng SHD
            _shdLabel = CreateHudText("SHD_Label", panel.transform, "SHD", 14f, new Color(0.25f, 0.88f, 1f, 0.95f), TextAlignmentOptions.MidlineLeft);
            var shdLblRt = _shdLabel.rectTransform;
            shdLblRt.anchorMin = new Vector2(0f, 0.28f);
            shdLblRt.anchorMax = new Vector2(0f, 0.28f);
            shdLblRt.pivot = new Vector2(0f, 0.5f);
            shdLblRt.anchoredPosition = new Vector2(0f, 0f);
            shdLblRt.sizeDelta = new Vector2(36f, 18f);

            var shdTrack = CreateHudImage("SHD_Track", panel.transform, new Color(0.04f, 0.08f, 0.14f, 0.85f));
            var shdTrackRt = shdTrack.rectTransform;
            shdTrackRt.anchorMin = new Vector2(0f, 0.28f);
            shdTrackRt.anchorMax = new Vector2(0f, 0.28f);
            shdTrackRt.pivot = new Vector2(0f, 0.5f);
            shdTrackRt.anchoredPosition = new Vector2(42f, 0f);
            shdTrackRt.sizeDelta = new Vector2(185f, 11f);

            var shdBorder = CreateHudImage("Border", shdTrack.transform, new Color(0.2f, 0.7f, 0.9f, 0.4f));
            shdBorder.rectTransform.anchorMin = Vector2.zero;
            shdBorder.rectTransform.anchorMax = Vector2.one;
            shdBorder.rectTransform.offsetMin = new Vector2(-1f, -1f);
            shdBorder.rectTransform.offsetMax = new Vector2(1f, 1f);
            shdBorder.transform.SetAsFirstSibling();

            _shdBarLag = CreateHudImage("SHD_Lag", shdTrack.transform, new Color(0.3f, 0.6f, 1f, 0.5f));
            _shdBarLag.rectTransform.anchorMin = Vector2.zero;
            _shdBarLag.rectTransform.anchorMax = Vector2.one;
            _shdBarLag.rectTransform.pivot = new Vector2(0f, 0.5f);
            _shdBarLag.rectTransform.offsetMin = Vector2.zero;
            _shdBarLag.rectTransform.offsetMax = Vector2.zero;

            _shdBarFill = CreateHudImage("SHD_Fill", shdTrack.transform, new Color(0.2f, 0.85f, 1f, 0.95f));
            _shdBarFill.rectTransform.anchorMin = Vector2.zero;
            _shdBarFill.rectTransform.anchorMax = Vector2.one;
            _shdBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _shdBarFill.rectTransform.offsetMin = Vector2.zero;
            _shdBarFill.rectTransform.offsetMax = Vector2.zero;
        }

        private void BuildBottomRightSpeedBoostBars(Transform parent)
        {
            var panel = new GameObject("BottomRight_Speed_Boost_Cluster", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(1f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(1f, 0f);
            prt.anchoredPosition = new Vector2(-40f, 32f);
            prt.sizeDelta = new Vector2(260f, 48f);

            // Dòng SPEED: {kmh} KM/H  {BigNumber}
            _speedLabelText = CreateHudText("SpeedLabel", panel.transform, "SPEED: 742 KM/H", 13f, new Color(0.85f, 0.95f, 1f, 0.85f), TextAlignmentOptions.MidlineRight);
            var sLblRt = _speedLabelText.rectTransform;
            sLblRt.anchorMin = new Vector2(0f, 0.72f);
            sLblRt.anchorMax = new Vector2(1f, 0.72f);
            sLblRt.pivot = new Vector2(1f, 0.5f);
            sLblRt.anchoredPosition = new Vector2(-54f, 0f);
            sLblRt.sizeDelta = new Vector2(160f, 20f);

            _speedBigNumberText = CreateHudText("SpeedBigNumber", panel.transform, "742", 24f, new Color(0.95f, 0.98f, 1f, 1f), TextAlignmentOptions.MidlineRight);
            var sNumRt = _speedBigNumberText.rectTransform;
            sNumRt.anchorMin = new Vector2(1f, 0.72f);
            sNumRt.anchorMax = new Vector2(1f, 0.72f);
            sNumRt.pivot = new Vector2(1f, 0.5f);
            sNumRt.anchoredPosition = new Vector2(0f, 0f);
            sNumRt.sizeDelta = new Vector2(50f, 26f);

            // Dòng BOOST
            var boostLabel = CreateHudText("BoostLabel", panel.transform, "BOOST", 13f, new Color(0.85f, 0.95f, 1f, 0.85f), TextAlignmentOptions.MidlineLeft);
            var bLblRt = boostLabel.rectTransform;
            bLblRt.anchorMin = new Vector2(1f, 0.28f);
            bLblRt.anchorMax = new Vector2(1f, 0.28f);
            bLblRt.pivot = new Vector2(1f, 0.5f);
            bLblRt.anchoredPosition = new Vector2(-155f, 0f);
            bLblRt.sizeDelta = new Vector2(50f, 18f);

            var boostTrack = CreateHudImage("Boost_Track", panel.transform, new Color(0.04f, 0.08f, 0.14f, 0.85f));
            var btrRt = boostTrack.rectTransform;
            btrRt.anchorMin = new Vector2(1f, 0.28f);
            btrRt.anchorMax = new Vector2(1f, 0.28f);
            btrRt.pivot = new Vector2(1f, 0.5f);
            btrRt.anchoredPosition = new Vector2(0f, 0f);
            btrRt.sizeDelta = new Vector2(148f, 10f);

            var bBorder = CreateHudImage("Border", boostTrack.transform, new Color(0.3f, 0.7f, 0.9f, 0.35f));
            bBorder.rectTransform.anchorMin = Vector2.zero;
            bBorder.rectTransform.anchorMax = Vector2.one;
            bBorder.rectTransform.offsetMin = new Vector2(-1f, -1f);
            bBorder.rectTransform.offsetMax = new Vector2(1f, 1f);
            bBorder.transform.SetAsFirstSibling();

            _boostBarFill = CreateHudImage("Boost_Fill", boostTrack.transform, new Color(0.2f, 0.85f, 1f, 0.95f));
            _boostBarFill.rectTransform.anchorMin = Vector2.zero;
            _boostBarFill.rectTransform.anchorMax = Vector2.one;
            _boostBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _boostBarFill.rectTransform.offsetMin = Vector2.zero;
            _boostBarFill.rectTransform.offsetMax = Vector2.zero;
        }

        private void BuildAttackWarningBanner(Transform parent)
        {
            var card = CreateHudImage("AttackWarningCard", parent, new Color(0.06f, 0.03f, 0.04f, 0.92f));
            _warningCardRoot = card.gameObject;
            var alertRt = card.rectTransform;
            alertRt.anchorMin = new Vector2(0.5f, 1f);
            alertRt.anchorMax = new Vector2(0.5f, 1f);
            alertRt.pivot = new Vector2(0.5f, 1f);
            alertRt.anchoredPosition = new Vector2(0f, -54f);
            alertRt.sizeDelta = new Vector2(520f, 56f);

            var alertTopTrim = CreateHudImage("TopTrim", card.transform, new Color(1f, 0.18f, 0.22f, 1f));
            alertTopTrim.rectTransform.anchorMin = new Vector2(0f, 1f);
            alertTopTrim.rectTransform.anchorMax = new Vector2(1f, 1f);
            alertTopTrim.rectTransform.pivot = new Vector2(0.5f, 1f);
            alertTopTrim.rectTransform.sizeDelta = new Vector2(0f, 3f);

            var alertBottomTrim = CreateHudImage("BottomTrim", card.transform, new Color(1f, 0.22f, 0.25f, 0.75f));
            alertBottomTrim.rectTransform.anchorMin = new Vector2(0.08f, 0f);
            alertBottomTrim.rectTransform.anchorMax = new Vector2(0.92f, 0f);
            alertBottomTrim.rectTransform.pivot = new Vector2(0.5f, 0f);
            alertBottomTrim.rectTransform.sizeDelta = new Vector2(0f, 2f);

            _warningTitleText = CreateHudText("AlertTitle", card.transform, "[ ! ] CẢNH BÁO: TÀU ĐANG BỊ TẤN CÔNG [ ! ]", 15.5f, new Color(1f, 0.25f, 0.3f, 1f), TextAlignmentOptions.Center);
            _warningTitleText.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            _warningTitleText.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            _warningTitleText.rectTransform.offsetMin = Vector2.zero;
            _warningTitleText.rectTransform.offsetMax = Vector2.zero;

            _warningSubText = CreateHudText("AlertSub", card.transform, "HỆ THỐNG KHIÊN BẢO VỆ ĐANG HẤP THỤ SÁT THƯƠNG", 12f, new Color(0.9f, 0.93f, 1f, 0.9f), TextAlignmentOptions.Center);
            _warningSubText.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            _warningSubText.rectTransform.anchorMax = new Vector2(1f, 0.48f);
            _warningSubText.rectTransform.offsetMin = Vector2.zero;
            _warningSubText.rectTransform.offsetMax = Vector2.zero;

            _warningCardRoot.SetActive(false); // Mặc định ẩn, chỉ hiện khi bị tấn công!
        }

        private void BuildVirtualStickOverlay(Transform parent)
        {
            _virtualStickHudRoot = new GameObject("VirtualFlightStick_Overlay", typeof(RectTransform));
            _virtualStickHudRoot.transform.SetParent(parent, false);
            var rootRt = _virtualStickHudRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = Vector2.zero;

            _stickLineImg = CreateHudImage("StickTetherLine", _virtualStickHudRoot.transform, new Color(0.28f, 0.94f, 1.0f, 0.42f));
            _stickLineRt = _stickLineImg.rectTransform;
            _stickLineRt.pivot = new Vector2(0f, 0.5f);
            _stickLineRt.anchoredPosition = Vector2.zero;
            _stickLineRt.sizeDelta = new Vector2(0f, 1.5f);

            _stickPipImg = CreateHudImage("StickPip", _virtualStickHudRoot.transform, new Color(0.32f, 0.96f, 1.0f, 0.85f));
            _stickPipRt = _stickPipImg.rectTransform;
            _stickPipRt.sizeDelta = new Vector2(7f, 7f);
            _stickPipRt.localRotation = Quaternion.Euler(0f, 0f, 45f);

            _virtualStickHudRoot.SetActive(false);
        }

        private void UpdateVirtualStickHUD()
        {
            if (_virtualStickHudRoot == null) return;

            float stickMag = _virtualStick.magnitude;
            bool show = enableMouseFlightStick && _mouseSteerArmed && stickMag > 0.015f;
            if (_virtualStickHudRoot.activeSelf != show)
            {
                _virtualStickHudRoot.SetActive(show);
            }
            if (!show) return;

            float fade = Mathf.Clamp01((stickMag - 0.015f) / 0.10f);
            Vector2 pxOffset = _virtualStick * 105f;
            float len = pxOffset.magnitude;
            float angle = Mathf.Atan2(pxOffset.y, pxOffset.x) * Mathf.Rad2Deg;

            if (_stickLineRt != null)
            {
                _stickLineRt.sizeDelta = new Vector2(Mathf.Max(0f, len - 5f), 1.5f);
                _stickLineRt.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            if (_stickLineImg != null)
            {
                _stickLineImg.color = new Color(0.28f, 0.94f, 1.0f, 0.40f * fade);
            }
            if (_stickPipRt != null)
            {
                _stickPipRt.anchoredPosition = pxOffset;
            }
            if (_stickPipImg != null)
            {
                _stickPipImg.color = new Color(0.32f, 0.96f, 1.0f, 0.85f * fade);
            }
        }

        private static Image CreateHudImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text CreateHudText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void UpdateCompassTape()
        {
            if (_compassTickLines == null) return;
            float stepPx = 22f;
            float yawMod = (_currentYaw * 1.5f) % stepPx;

            int n = _compassTickLines.Length;
            int half = n / 2;
            for (int i = 0; i < n; i++)
            {
                int idx = i - half;
                float x = idx * stepPx - yawMod;
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(x) / 220f);
                _compassTickLines[i].anchoredPosition = new Vector2(x, 12f);
                var img = _compassTickLines[i].GetComponent<Image>();
                if (img != null) img.color = new Color(0.8f, 0.92f, 1f, 0.65f * alpha);
            }
        }

        private void UpdateCombatHUD()
        {
            EnsurePlayerSciFiHUD();
            if (_playerHudRoot == null) return;

            bool isPiloting = !AboutInfoStation.AnyStationActive;
            if (_playerHudRoot.activeSelf != isPiloting)
            {
                _playerHudRoot.SetActive(isPiloting);
            }
            if (!isPiloting) return;

            // 1. Cập nhật thước đo la bàn trên đỉnh & kim cần lái ảo
            UpdateCompassTape();
            UpdateVirtualStickHUD();

            // 2. Cập nhật thanh HP và SHD
            float targetHpRatio = HealthRatio;
            float targetShdRatio = ShieldRatio;
            _displayedHpRatio = Mathf.Lerp(_displayedHpRatio, targetHpRatio, Time.deltaTime * 5f);
            _displayedShieldRatio = Mathf.Lerp(_displayedShieldRatio, targetShdRatio, Time.deltaTime * 5f);

            if (_hpBarFill != null)
            {
                _hpBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(targetHpRatio), 1f);
            }
            if (_hpBarLag != null)
            {
                _hpBarLag.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_displayedHpRatio), 1f);
            }

            if (_shdBarFill != null)
            {
                _shdBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(targetShdRatio), 1f);
            }
            if (_shdBarLag != null)
            {
                _shdBarLag.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_displayedShieldRatio), 1f);
            }

            // 3. Cập nhật SPEED & BOOST
            float currentSpeed = Mathf.Abs(_currentForwardSpeed);
            float targetKmh = 0f;
            if (currentSpeed > 0.5f)
            {
                float norm = Mathf.Clamp01(currentSpeed / boostSpeed);
                targetKmh = Mathf.Lerp(120f, 742f, norm);
            }
            _displayedKmh = Mathf.Lerp(_displayedKmh, targetKmh, Time.deltaTime * 6f);
            int kmhInt = Mathf.RoundToInt(_displayedKmh);

            if (_speedLabelText != null)
            {
                _speedLabelText.text = $"SPEED: {kmhInt} KM/H";
            }
            if (_speedBigNumberText != null)
            {
                _speedBigNumberText.text = $"{kmhInt}";
            }

            float boostRatio = BoostRatio;
            _displayedBoostRatio = Mathf.Lerp(_displayedBoostRatio, boostRatio, Time.deltaTime * 8f);
            if (_boostBarFill != null)
            {
                _boostBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_displayedBoostRatio), 1f);
                _boostBarFill.color = _isBoosting ? new Color(1f, 0.45f, 0.15f, 0.95f) : new Color(0.2f, 0.85f, 1f, 0.95f);
            }

            // 4. BẢNG CẢNH BÁO: CHỈ HIỆN KHI BỊ TẤN CÔNG ("bị tấn công thì ms hiện cái cảnh báo")
            bool isUnderAttack = (Time.time - _lastDamageTime < 4.5f) || (SpaceTrafficShipAI.IsFleetHostile && (Time.time - _lastDamageTime < 10f));
            if (_warningCardRoot != null)
            {
                if (_warningCardRoot.activeSelf != isUnderAttack)
                {
                    _warningCardRoot.SetActive(isUnderAttack);
                }

                if (isUnderAttack)
                {
                    if (_currentShield <= 0.05f)
                    {
                        if (_warningTitleText != null) _warningTitleText.text = "[ ! ] NGUY CẤP: KHIÊN NĂNG LƯỢNG ĐÃ VỠ [ ! ]";
                        if (_warningSubText != null) _warningSubText.text = "THÂN TÀU ĐANG CHỊU THIỆT HẠI TRỰC TIẾP!";
                    }
                    else
                    {
                        if (_warningTitleText != null) _warningTitleText.text = "[ ! ] CẢNH BÁO: TÀU ĐANG BỊ TẤN CÔNG [ ! ]";
                        if (_warningSubText != null) _warningSubText.text = "HỆ THỐNG KHIÊN BẢO VỆ ĐANG HẤP THỤ SÁT THƯƠNG";
                    }
                }
            }

            // Nháy đỏ màn hình khi bị trúng đạn hoặc nguy kịch
            float hitFlash = Mathf.Clamp01(1f - (Time.time - _lastDamageTime) / 0.35f) * 0.28f;
            float lowHealthFlash = 0f;
            if (targetHpRatio <= 0.35f && !_isRespawning)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 11f);
                lowHealthFlash = 0.08f + pulse * 0.25f;
            }
            float redAlpha = Mathf.Max(hitFlash, lowHealthFlash);
            if (_redScreenOverlay != null)
                _redScreenOverlay.color = new Color(0.95f, 0.03f, 0.06f, redAlpha);

            if (_redBorders != null)
            {
                float borderAlpha = Mathf.Clamp01(redAlpha * 2.2f);
                for (int i = 0; i < _redBorders.Length; i++)
                {
                    if (_redBorders[i] != null)
                        _redBorders[i].color = new Color(1f, 0.04f, 0.08f, borderAlpha);
                }
            }
        }
    }
}
