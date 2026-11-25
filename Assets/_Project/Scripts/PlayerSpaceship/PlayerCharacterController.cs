using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Playable Character Host (Astronaut) with CharacterController movement, 3rd/1st-person camera,
    /// and seamless boarding / exiting of PlayerSpaceship.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")]
        public PlayerSpaceshipController spaceship;
        public SpaceshipCameraFollow spaceshipCamera;
        public Camera playerCamera;
        public Transform visualRoot;

        [Header("On-Foot Movement")]
        public float walkSpeed = 6.0f;
        public float sprintSpeed = 10.5f;
        public float jumpHeight = 1.6f;
        public float gravity = -19.6f;
        public float rotationSmoothSpeed = 14f;

        [Header("Camera Settings")]
        public float mouseSensitivity = 2.2f;
        public float minPitch = -60f;
        public float maxPitch = 70f;
        public Vector3 thirdPersonOffset = new Vector3(0.65f, 1.95f, -3.8f);
        public Vector3 firstPersonEyeOffset = new Vector3(0f, 1.62f, 0.15f);
        public bool thirdPersonDefault = false;

        [Header("Camera Feel & Juice")]
        public float walkBobFrequency = 9.5f;
        public float sprintBobFrequency = 13.8f;
        public float walkBobAmplitude = 0.042f;
        public float sprintBobAmplitude = 0.075f;
        public float swayAmplitude = 0.048f;
        public float walkRollDegrees = 1.1f;
        public float sprintRollDegrees = 2.0f;
        public float strafeTiltDegrees = 2.4f;
        public float rollDuration = 0.42f;

        [Header("Ship Boarding / Exiting")]
        public KeyCode interactKey = KeyCode.F;
        public KeyCode viewToggleKey = KeyCode.V;
        public float boardDistance = 7.5f;
        public float maxExitShipSpeed = 14f;
        public Vector3 exitSideOffset = new Vector3(3.8f, 0.8f, 0f);

        public bool IsInShip { get; private set; }
        public static bool AnyUiPanelOpen { get; set; }
        public static bool ExternalCameraOverride { get; set; }

        private CharacterController _cc;
        private Animator _animator;
        private Transform _swordTr;
        private int _combatMode; // 0=Normal, 1=Sword, 2=Gun
        private int _attackIndex;
        private float _moveX, _moveZ;
        private float _airborneTimer;
        private float _jumpBlend;
        private float _rollTimer;
        private float _upperLayerWeight = 1f;
        private Vector3 _rollDir;
        private Transform _rigUpperLegL, _rigLowerLegL, _rigUpperLegR, _rigLowerLegR, _rigTorso, _rigFootL, _rigFootR;
        private Renderer[] _visualRenderers;
        private GameObject _shipHudGo;
        private GameObject _targetHudGo;
        private SciFiRadarMinimap _radarMinimap;
        private SpaceshipTargetLockSystem _targetLock;
        private Transform _headTr, _bodyTr, _armLeftTr, _armRightTr, _legLeftTr, _legRightTr;
        private Vector3 _initHeadPos, _initBodyPos;
        private Quaternion _initHeadRot, _initBodyRot, _initArmLRot, _initArmRRot, _initLegLRot, _initLegRRot;
        private float _animCycle;
        private float _currentHorizSpeed;
        private Vector3 _velocity;
        private float _yaw;
        private float _pitch = 0f;
        private bool _isThirdPerson;
        private bool _canBoard;
        private bool _canExit;

        // Camera feel & physical spring state
        private float _baseFov = 60f;
        private float _fovOffset;
        private float _bobPhase;
        private float _bobWeight;
        private float _strafeTilt;
        private bool _wasGrounded = true;
        private float _lastAirborneVy;
        private float _camDipOffset;
        private float _camDipVel;
        private float _camImpactPitch;
        private float _camImpactPitchVel;
        private float _shakeIntensity;
        private float _shakeSeed;
        private float _smoothedCamY;
        private float _smoothedCamYVel;
        private bool _hasSmoothedCamY;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = visualRoot != null
                ? visualRoot.GetComponentInChildren<Animator>(true)
                : GetComponentInChildren<Animator>(true);
            if (_animator != null) _animator.applyRootMotion = false;
            Transform vRoot = visualRoot != null ? visualRoot : transform;
            _swordTr = vRoot.Find("Sword");
            if (_swordTr != null) _swordTr.gameObject.SetActive(false);
            _visualRenderers = visualRoot != null
                ? visualRoot.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);
            thirdPersonDefault = false;
            _isThirdPerson = false;
            SetVisualVisible(false);
            ResolveVisualLimbs();
        }

        private void ResolveVisualLimbs()
        {
            Transform root = visualRoot != null ? visualRoot : transform;
            _rigUpperLegL = root.Find("CharacterArmature/Root/Body/UpperLeg.L");
            _rigLowerLegL = root.Find("CharacterArmature/Root/Body/UpperLeg.L/LowerLeg.L");
            _rigUpperLegR = root.Find("CharacterArmature/Root/Body/UpperLeg.R");
            _rigLowerLegR = root.Find("CharacterArmature/Root/Body/UpperLeg.R/LowerLeg.R");
            _rigTorso = root.Find("CharacterArmature/Root/Body/Hips/Abdomen/Torso");
            _rigFootL = root.Find("CharacterArmature/Root/Foot.L");
            _rigFootR = root.Find("CharacterArmature/Root/Foot.R");

            _headTr = root.Find("head");
            _bodyTr = root.Find("body");
            _armLeftTr = root.Find("armLeft");
            _armRightTr = root.Find("armRight");
            _legLeftTr = root.Find("legLeft");
            _legRightTr = root.Find("legRight");

            if (_headTr != null) { _initHeadPos = _headTr.localPosition; _initHeadRot = _headTr.localRotation; }
            if (_bodyTr != null) { _initBodyPos = _bodyTr.localPosition; _initBodyRot = _bodyTr.localRotation; }
            if (_armLeftTr != null) _initArmLRot = _armLeftTr.localRotation;
            if (_armRightTr != null) _initArmRRot = _armRightTr.localRotation;
            if (_legLeftTr != null) _initLegLRot = _legLeftTr.localRotation;
            if (_legRightTr != null) _initLegRRot = _legRightTr.localRotation;
        }

        private void Start()
        {
            if (spaceship == null) spaceship = FindFirstObjectByType<PlayerSpaceshipController>();
            if (spaceshipCamera == null) spaceshipCamera = FindFirstObjectByType<SpaceshipCameraFollow>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera != null) _baseFov = playerCamera.fieldOfView;
            _shakeSeed = Random.Range(0f, 100f);

            if (spaceship != null)
            {
                _radarMinimap = spaceship.GetComponent<SciFiRadarMinimap>();
                _targetLock = spaceship.GetComponent<SpaceshipTargetLockSystem>();
            }
            var hudTr = GameObject.Find("Canvas/PlayerSciFiHUD");
            if (hudTr != null) _shipHudGo = hudTr;
            var targetHudTr = GameObject.Find("Canvas/TargetTracking_HUD_Root");
            if (targetHudTr != null) _targetHudGo = targetHudTr;

            _yaw = transform.eulerAngles.y;
            EnterOnFootMode(snapCursor: true);
        }

        private void Update()
        {
            if (Time.timeScale <= 0.01f) return;

            if (IsInShip)
            {
                UpdateInShip();
            }
            else
            {
                UpdateOnFoot();
            }
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0.01f || IsInShip) return;
            if ((_shipHudGo != null && _shipHudGo.activeSelf) || (_targetHudGo != null && _targetHudGo.activeSelf))
            {
                SetShipHudVisible(false);
            }
            if (ExternalCameraOverride) return;
            ApplyAirborneKneeTuck();
            if (playerCamera != null) UpdateCharacterCamera();
        }

        private void ApplyAirborneKneeTuck()
        {
            if (_animator == null || _rigUpperLegL == null) return;
            bool isRolling = _rollTimer > 0f;
            float targetJump = (!_cc.isGrounded && _airborneTimer > 0.05f && !isRolling) ? 1f : 0f;
            float blendRate = targetJump > 0.5f ? 10f : 22f; // Release leg smoothly & quickly on touchdown
            _jumpBlend = Mathf.MoveTowards(_jumpBlend, targetJump, Time.deltaTime * blendRate);
            if (_jumpBlend <= 0.001f) return;

            Transform vRoot = visualRoot != null ? visualRoot : transform;
            Vector3 pitchAxis = vRoot.right;

            // Clean single-leg mid-air tuck (tuck left knee up, keep right leg naturally extended)
            _rigUpperLegL.Rotate(pitchAxis, -52f * _jumpBlend, Space.World);
            if (_rigLowerLegL != null) _rigLowerLegL.Rotate(pitchAxis, 82f * _jumpBlend, Space.World);
            if (_rigFootL != null) _rigFootL.position += Vector3.up * (0.24f * _jumpBlend);

            if (_rigTorso != null) _rigTorso.Rotate(pitchAxis, 5f * _jumpBlend, Space.World);
        }

        private void UpdateOnFoot()
        {
            if (ExternalCameraOverride) return;

            if (Input.GetKeyDown(viewToggleKey))
            {
                _isThirdPerson = !_isThirdPerson;
                SetVisualVisible(_isThirdPerson);
            }

            // Mouse look (disabled while any interactive terminal/UI panel is open; clamp delta to prevent 1-frame spin spikes)
            if (AnyUiPanelOpen)
            {
                // Keep cursor free for UI clicks
            }
            else if (Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1))
            {
                float mx = Mathf.Clamp(Input.GetAxis("Mouse X"), -12f, 12f);
                float my = Mathf.Clamp(Input.GetAxis("Mouse Y"), -12f, 12f);
                _yaw += mx * mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch - my * mouseSensitivity, minPitch, maxPitch);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Movement relative to camera yaw
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
            Vector3 moveDir = (forward * v + right * h).normalized;

            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            if (moveDir.sqrMagnitude > 0.01f && _isThirdPerson)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothSpeed);
            }
            else if (!_isThirdPerson)
            {
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }

            if (_cc.isGrounded && _velocity.y < 0f)
            {
                // Keep grounded firmly when walking down stairs/ramps so CharacterController doesn't bounce
                _velocity.y = -8f;
            }
            if (_cc.isGrounded && Input.GetButtonDown("Jump"))
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _velocity.y += gravity * Time.deltaTime;

            if (!_cc.isGrounded)
            {
                _lastAirborneVy = Mathf.Min(_lastAirborneVy, _velocity.y);
            }

            Vector3 horizMove = moveDir * speed;
            if (_rollTimer > 0f)
            {
                _rollTimer -= Time.deltaTime;
                horizMove = _rollDir * 11.5f;
                if (_isThirdPerson && _rollDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_rollDir, Vector3.up), Time.deltaTime * 18f);
                }
            }

            Vector3 finalMove = horizMove + Vector3.up * _velocity.y;
            _cc.Move(finalMove * Time.deltaTime);

            // Zero camera hitch/slam on landing (vertical smoothing in UpdateCharacterCamera handles touchdown fluidly)
            if (_cc.isGrounded && !_wasGrounded)
            {
                _lastAirborneVy = 0f;
            }
            _wasGrounded = _cc.isGrounded;

            _currentHorizSpeed = moveDir.sqrMagnitude > 0.01f ? speed : 0f;
            float speedFactor = _currentHorizSpeed > 0.1f ? (Input.GetKey(KeyCode.LeftShift) ? 1.0f : 0.5f) : 0f;
            if (_isThirdPerson)
            {
                _moveX = 0f;
                _moveZ = speedFactor;
            }
            else
            {
                Vector2 rawDir = new Vector2(h, v);
                if (rawDir.sqrMagnitude > 1f) rawDir.Normalize();
                _moveX = rawDir.x * speedFactor;
                _moveZ = rawDir.y * speedFactor;
            }

            HandleCharacterActions(moveDir);
            UpdateLimbAnimation(Time.deltaTime);

            // Check boarding proximity (must also be visible in player camera view)
            _canBoard = false;
            if (spaceship != null)
            {
                float dist = Vector3.Distance(transform.position, spaceship.transform.position);
                if (dist <= boardDistance && IsShipVisibleInView())
                {
                    _canBoard = true;
                    if (Input.GetKeyDown(interactKey))
                    {
                        BoardSpaceship();
                    }
                }
            }
        }

        private bool IsShipVisibleInView()
        {
            if (playerCamera == null || spaceship == null) return true;
            Vector3 target = spaceship.transform.position + Vector3.up * 1.2f;
            Vector3 toShip = (target - playerCamera.transform.position).normalized;
            if (Vector3.Dot(playerCamera.transform.forward, toShip) < 0.20f) return false;
            Vector3 vp = playerCamera.WorldToViewportPoint(target);
            return vp.z > 0.1f && vp.x >= 0.02f && vp.x <= 0.98f && vp.y >= 0.02f && vp.y <= 0.98f;
        }

        private void UpdateInShip()
        {
            _canExit = false;
            if (spaceship == null) return;

            transform.position = spaceship.transform.position;
            var rb = spaceship.GetComponent<Rigidbody>();
            float speed = rb != null ? rb.linearVelocity.magnitude : 0f;

            if (speed <= maxExitShipSpeed)
            {
                _canExit = true;
                if (Input.GetKeyDown(interactKey))
                {
                    ExitSpaceship();
                }
            }
        }

        private void HandleCharacterActions(Vector3 moveDir)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) SetCombatMode(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetCombatMode(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetCombatMode(2);

            if ((Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl)) && _rollTimer <= 0.05f)
            {
                _rollTimer = rollDuration;
                _rollDir = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : transform.forward;
                _shakeIntensity = Mathf.Max(_shakeIntensity, 0.35f);
                _animator.ResetTrigger("Roll");
                _animator.SetTrigger("Roll");
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                _animator.ResetTrigger("Wave");
                _animator.SetTrigger("Wave");
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(interactKey))
            {
                _animator.ResetTrigger("Interact");
                _animator.SetTrigger("Interact");
            }

            if (Input.GetKeyDown(KeyCode.R) || (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButtonDown(0)))
            {
                _shakeIntensity = Mathf.Max(_shakeIntensity, 0.28f);
                _camImpactPitchVel += 22f;
                _animator.ResetTrigger("Attack");
                _animator.SetInteger("AttackIndex", _attackIndex);
                _animator.SetTrigger("Attack");
                _attackIndex = (_attackIndex + 1) % 4;
            }
        }

        private void SetCombatMode(int mode)
        {
            _combatMode = mode;
            if (_animator != null) _animator.SetInteger("CombatMode", _combatMode);
            if (_swordTr != null) _swordTr.gameObject.SetActive(_combatMode == 1);
        }

        private void UpdateLimbAnimation(float dt)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                if (_cc.isGrounded) _airborneTimer = 0f;
                else _airborneTimer += dt;

                bool stableGrounded = _cc.isGrounded || _airborneTimer < 0.12f;

                float targetUpperWeight = _rollTimer > 0f ? 0f : 1f;
                _upperLayerWeight = Mathf.MoveTowards(_upperLayerWeight, targetUpperWeight, dt * 14f);
                if (_animator.layerCount > 1) _animator.SetLayerWeight(1, _upperLayerWeight);

                _animator.SetFloat("Speed", _currentHorizSpeed);
                _animator.SetFloat("MoveX", _moveX, 0.06f, dt);
                _animator.SetFloat("MoveZ", _moveZ, 0.06f, dt);
                _animator.SetBool("IsGrounded", stableGrounded);
                _animator.SetInteger("CombatMode", _combatMode);
                return;
            }

            if (_bodyTr == null && _legLeftTr == null) return;

            float blend = Mathf.Clamp01(dt * 12f);

            if (!_cc.isGrounded)
            {
                // Airborne / Jump pose (single-leg tuck)
                if (_legLeftTr != null) _legLeftTr.localRotation = Quaternion.Slerp(_legLeftTr.localRotation, _initLegLRot * Quaternion.Euler(-38f, 0f, 0f), blend);
                if (_legRightTr != null) _legRightTr.localRotation = Quaternion.Slerp(_legRightTr.localRotation, _initLegRRot, blend);
                if (_armLeftTr != null) _armLeftTr.localRotation = Quaternion.Slerp(_armLeftTr.localRotation, _initArmLRot * Quaternion.Euler(-15f, 0f, -10f), blend);
                if (_armRightTr != null) _armRightTr.localRotation = Quaternion.Slerp(_armRightTr.localRotation, _initArmRRot * Quaternion.Euler(-15f, 0f, 10f), blend);
                if (_bodyTr != null) _bodyTr.localRotation = Quaternion.Slerp(_bodyTr.localRotation, _initBodyRot * Quaternion.Euler(3f, 0f, 0f), blend);
                return;
            }

            if (_currentHorizSpeed > 0.2f)
            {
                bool sprinting = _currentHorizSpeed > (walkSpeed + 1.5f);
                _animCycle += dt * _currentHorizSpeed * 2.15f;
                float legAmp = sprinting ? 46f : 30f;
                float armAmp = sprinting ? 40f : 24f;

                float legSwing = Mathf.Sin(_animCycle) * legAmp;
                float armSwing = -Mathf.Sin(_animCycle) * armAmp;
                float bobY = Mathf.Abs(Mathf.Sin(_animCycle)) * (sprinting ? 0.045f : 0.025f);
                float forwardLean = sprinting ? 9f : 4f;

                if (_legLeftTr != null) _legLeftTr.localRotation = Quaternion.Slerp(_legLeftTr.localRotation, _initLegLRot * Quaternion.Euler(legSwing, 0f, 0f), blend);
                if (_legRightTr != null) _legRightTr.localRotation = Quaternion.Slerp(_legRightTr.localRotation, _initLegRRot * Quaternion.Euler(-legSwing, 0f, 0f), blend);
                if (_armLeftTr != null) _armLeftTr.localRotation = Quaternion.Slerp(_armLeftTr.localRotation, _initArmLRot * Quaternion.Euler(armSwing, 0f, -4f), blend);
                if (_armRightTr != null) _armRightTr.localRotation = Quaternion.Slerp(_armRightTr.localRotation, _initArmRRot * Quaternion.Euler(-armSwing, 0f, 4f), blend);

                if (_bodyTr != null)
                {
                    _bodyTr.localPosition = Vector3.Lerp(_bodyTr.localPosition, _initBodyPos + new Vector3(0f, bobY, 0f), blend);
                    _bodyTr.localRotation = Quaternion.Slerp(_bodyTr.localRotation, _initBodyRot * Quaternion.Euler(forwardLean, Mathf.Cos(_animCycle) * 3f, 0f), blend);
                }
                if (_headTr != null)
                {
                    _headTr.localPosition = Vector3.Lerp(_headTr.localPosition, _initHeadPos + new Vector3(0f, bobY, 0f), blend);
                    _headTr.localRotation = Quaternion.Slerp(_headTr.localRotation, _initHeadRot, blend);
                }
            }
            else
            {
                // Idle micro-breathing
                float t = Time.time * 2.0f;
                float breath = Mathf.Sin(t) * 0.012f;
                float armIdle = Mathf.Sin(t) * 2.5f;

                if (_legLeftTr != null) _legLeftTr.localRotation = Quaternion.Slerp(_legLeftTr.localRotation, _initLegLRot, blend);
                if (_legRightTr != null) _legRightTr.localRotation = Quaternion.Slerp(_legRightTr.localRotation, _initLegRRot, blend);
                if (_armLeftTr != null) _armLeftTr.localRotation = Quaternion.Slerp(_armLeftTr.localRotation, _initArmLRot * Quaternion.Euler(armIdle, 0f, -3f), blend);
                if (_armRightTr != null) _armRightTr.localRotation = Quaternion.Slerp(_armRightTr.localRotation, _initArmRRot * Quaternion.Euler(-armIdle, 0f, 3f), blend);

                if (_bodyTr != null)
                {
                    _bodyTr.localPosition = Vector3.Lerp(_bodyTr.localPosition, _initBodyPos + new Vector3(0f, breath, 0f), blend);
                    _bodyTr.localRotation = Quaternion.Slerp(_bodyTr.localRotation, _initBodyRot, blend);
                }
                if (_headTr != null)
                {
                    _headTr.localPosition = Vector3.Lerp(_headTr.localPosition, _initHeadPos + new Vector3(0f, breath, 0f), blend);
                    _headTr.localRotation = Quaternion.Slerp(_headTr.localRotation, _initHeadRot * Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.8f) * 5f, 0f), blend);
                }
            }
        }

        private void UpdateCharacterCamera()
        {
            float dt = Time.deltaTime;
            bool sprinting = _currentHorizSpeed > (walkSpeed + 1.5f);
            bool movingGrounded = _cc.isGrounded && _currentHorizSpeed > 0.25f && _rollTimer <= 0f;

            // 1. Head-bob & left-right stride sway
            float targetBobWeight = movingGrounded ? 1f : 0f;
            _bobWeight = Mathf.MoveTowards(_bobWeight, targetBobWeight, dt * 8f);
            if (movingGrounded)
            {
                float freq = sprinting ? sprintBobFrequency : walkBobFrequency;
                _bobPhase += dt * freq;
            }

            float bobY = Mathf.Sin(_bobPhase * 2f) * (sprinting ? sprintBobAmplitude : walkBobAmplitude) * _bobWeight;
            float swayX = Mathf.Cos(_bobPhase) * swayAmplitude * (sprinting ? 1.3f : 0.85f) * _bobWeight;
            float strideRoll = Mathf.Cos(_bobPhase) * (sprinting ? sprintRollDegrees : walkRollDegrees) * _bobWeight;

            // Subtle idle breathing when standing still
            float idleBreathY = (1f - _bobWeight) * Mathf.Sin(Time.time * 1.85f) * 0.009f;
            float idleBreathPitch = (1f - _bobWeight) * Mathf.Sin(Time.time * 1.85f) * 0.18f;

            // Strafe lean tilt
            float targetStrafeTilt = -Input.GetAxisRaw("Horizontal") * strafeTiltDegrees;
            _strafeTilt = Mathf.Lerp(_strafeTilt, targetStrafeTilt, dt * 10f);

            // 2. Smooth vertical Y tracking (eliminates abrupt 1-frame camera stop/hitch on landing)
            float safeDt = Mathf.Min(dt, 0.033f);
            if (!_hasSmoothedCamY || Mathf.Abs(transform.position.y - _smoothedCamY) > 2.5f)
            {
                _smoothedCamY = transform.position.y;
                _smoothedCamYVel = 0f;
                _hasSmoothedCamY = true;
            }
            else
            {
                _smoothedCamY = Mathf.SmoothDamp(_smoothedCamY, transform.position.y, ref _smoothedCamYVel, 0.045f, Mathf.Infinity, safeDt);
            }

            // Decay any residual dip/pitch smoothly (no unstable high-stiffness spring bounce)
            _camDipOffset = Mathf.Lerp(_camDipOffset, 0f, safeDt * 12f);
            _camImpactPitch = Mathf.Lerp(_camImpactPitch, 0f, safeDt * 12f);
            _camDipVel = 0f;
            _camImpactPitchVel = 0f;

            // 3. Organic Perlin camera shake (decays smoothly)
            _shakeIntensity = Mathf.MoveTowards(_shakeIntensity, 0f, dt * 3.4f);
            float shakeT = Time.time * 26f + _shakeSeed;
            float shakeSq = _shakeIntensity * _shakeIntensity;
            float shakePitch = (Mathf.PerlinNoise(shakeT, 0.3f) - 0.5f) * 4.5f * shakeSq;
            float shakeYaw = (Mathf.PerlinNoise(0.7f, shakeT) - 0.5f) * 3.5f * shakeSq;
            float shakeRoll = (Mathf.PerlinNoise(shakeT, shakeT) - 0.5f) * 5.0f * shakeSq;
            Vector3 shakePosOffset = new Vector3(
                (Mathf.PerlinNoise(shakeT + 3f, 1.1f) - 0.5f) * 0.07f * shakeSq,
                (Mathf.PerlinNoise(2.2f, shakeT + 5f) - 0.5f) * 0.09f * shakeSq,
                0f
            );

            // 4. Roll / Somersault camera perspective change
            float rollProgress = (_rollTimer > 0f && rollDuration > 0.01f) ? Mathf.Clamp01(1f - (_rollTimer / rollDuration)) : 0f;
            float rollCurve = Mathf.Sin(rollProgress * Mathf.PI);
            float rollCrouchY = _rollTimer > 0f ? -0.72f * rollCurve : 0f;
            float rollSomersaultPitch = _rollTimer > 0f && !_isThirdPerson ? Mathf.SmoothStep(0f, 360f, rollProgress) : 0f;
            float rollDutchTilt = _rollTimer > 0f ? 16f * rollCurve : 0f;

            // 5. Dynamic FOV (sprint & roll speed sensation)
            float targetFovOffset = _rollTimer > 0f ? 9.5f * rollCurve : (sprinting && _currentHorizSpeed > 0.5f ? 6.5f : 0f);
            _fovOffset = Mathf.Lerp(_fovOffset, targetFovOffset, dt * 9f);
            playerCamera.fieldOfView = _baseFov + _fovOffset;

            Vector3 camAnchorPos = new Vector3(transform.position.x, _smoothedCamY, transform.position.z);

            if (_isThirdPerson)
            {
                Quaternion camRot = Quaternion.Euler(_pitch + _camImpactPitch * 0.55f + shakePitch, _yaw + shakeYaw, strideRoll * 0.45f + _strafeTilt * 0.6f + rollDutchTilt * 0.7f + shakeRoll);
                Vector3 targetOffset = thirdPersonOffset + new Vector3(swayX * 0.4f, bobY * 0.45f + _camDipOffset * 0.75f + rollCrouchY * 0.45f, _rollTimer > 0f ? 0.55f * rollCurve : 0f);
                Vector3 desiredPos = camAnchorPos + camRot * targetOffset + shakePosOffset;
                Vector3 lookTarget = camAnchorPos + Vector3.up * (1.45f + _camDipOffset * 0.6f + rollCrouchY * 0.5f);

                // Prevent camera clipping through walls/floors
                Vector3 dir = desiredPos - lookTarget;
                if (Physics.Raycast(lookTarget, dir.normalized, out RaycastHit hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.transform.IsChildOf(transform))
                    {
                        desiredPos = hit.point - dir.normalized * 0.25f;
                    }
                }

                float camFollow = 1f - Mathf.Exp(-18f * safeDt);
                playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desiredPos, camFollow);
                playerCamera.transform.LookAt(lookTarget);
                playerCamera.transform.Rotate(0f, 0f, strideRoll * 0.45f + _strafeTilt * 0.6f + rollDutchTilt * 0.7f + shakeRoll, Space.Self);
            }
            else
            {
                Vector3 localEye = firstPersonEyeOffset + new Vector3(swayX, bobY + idleBreathY + _camDipOffset + rollCrouchY, 0f);
                playerCamera.transform.position = camAnchorPos + transform.TransformDirection(localEye) + shakePosOffset;
                playerCamera.transform.rotation = Quaternion.Euler(
                    _pitch + idleBreathPitch + _camImpactPitch + shakePitch + rollSomersaultPitch,
                    _yaw + shakeYaw,
                    strideRoll + _strafeTilt + rollDutchTilt + shakeRoll
                );
            }
        }

        public void BoardSpaceship()
        {
            if (spaceship == null) return;
            IsInShip = true;
            _canBoard = false;

            _cc.enabled = false;
            SetVisualVisible(false);

            var rb = spaceship.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            spaceship.enabled = true;
            SetShipHudVisible(true);

            if (spaceshipCamera != null)
            {
                spaceshipCamera.target = spaceship.transform;
                spaceshipCamera.spaceship = spaceship;
                spaceshipCamera.enabled = true;
                spaceshipCamera.SnapToTargetImmediate();
            }

            // Spaceship unlocked cursor (use WASD/Arrows or Right-Click drag to steer; no accidental runaway spin)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ExitSpaceship()
        {
            if (spaceship == null) return;

            var rb = spaceship.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            spaceship.enabled = false;

            Vector3 spawnPos = spaceship.transform.position + spaceship.transform.TransformDirection(exitSideOffset);
            if (Physics.Raycast(spawnPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
            {
                spawnPos = hit.point + Vector3.up * 0.1f;
            }

            transform.position = spawnPos;
            _yaw = spaceship.transform.eulerAngles.y;
            _velocity = Vector3.zero;

            EnterOnFootMode(snapCursor: true);
        }

        private void EnterOnFootMode(bool snapCursor)
        {
            IsInShip = false;
            _canExit = false;

            if (spaceshipCamera != null) spaceshipCamera.enabled = false;
            if (spaceship != null)
            {
                spaceship.enabled = false;
                var rb = spaceship.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }

            _cc.enabled = true;
            SetVisualVisible(_isThirdPerson);
            SetShipHudVisible(false);
            if (playerCamera != null) UpdateCharacterCamera();

            if (snapCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void SetShipHudVisible(bool visible)
        {
            if (_shipHudGo == null) _shipHudGo = GameObject.Find("Canvas/PlayerSciFiHUD");
            if (_targetHudGo == null) _targetHudGo = GameObject.Find("Canvas/TargetTracking_HUD_Root");

            if (_shipHudGo != null && _shipHudGo.activeSelf != visible) _shipHudGo.SetActive(visible);
            if (_targetHudGo != null && _targetHudGo.activeSelf != visible) _targetHudGo.SetActive(visible);
            if (_radarMinimap != null && _radarMinimap.enabled != visible) _radarMinimap.enabled = visible;
            if (_targetLock != null && _targetLock.enabled != visible) _targetLock.enabled = visible;
        }

        private void SetVisualVisible(bool visible)
        {
            if (_visualRenderers == null) return;
            foreach (var r in _visualRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0.01f || AnyUiPanelOpen) return;

            if (!IsInShip && _canBoard && spaceship != null)
            {
                Vector3 anchor = spaceship.transform.position + Vector3.up * 1.8f;
                WorldInteractPromptUI.DrawAtWorldPos(anchor, "F", "LÊN TÀU");
            }
            else if (IsInShip && _canExit)
            {
                WorldInteractPromptUI.DrawAtScreenPos(Screen.width * 0.5f - 60f, Screen.height - 82f, "F", "RỜI TÀU");
            }
        }
    }
}
