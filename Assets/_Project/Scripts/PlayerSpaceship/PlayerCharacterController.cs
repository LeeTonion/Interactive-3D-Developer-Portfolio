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

        [Header("Ship Boarding / Exiting")]
        public KeyCode interactKey = KeyCode.F;
        public KeyCode viewToggleKey = KeyCode.V;
        public float boardDistance = 7.5f;
        public float maxExitShipSpeed = 14f;
        public Vector3 exitSideOffset = new Vector3(3.8f, 0.8f, 0f);

        public bool IsInShip { get; private set; }

        private CharacterController _cc;
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

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
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
            if (playerCamera != null) UpdateCharacterCamera();
        }

        private void UpdateOnFoot()
        {
            if (Input.GetKeyDown(viewToggleKey))
            {
                _isThirdPerson = !_isThirdPerson;
                SetVisualVisible(_isThirdPerson);
            }

            // Mouse look (works with locked cursor or while holding right mouse button)
            if (Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, minPitch, maxPitch);
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
                _velocity.y = -2f;
            }
            if (_cc.isGrounded && Input.GetButtonDown("Jump"))
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _velocity.y += gravity * Time.deltaTime;

            Vector3 finalMove = moveDir * speed + Vector3.up * _velocity.y;
            _cc.Move(finalMove * Time.deltaTime);

            _currentHorizSpeed = moveDir.sqrMagnitude > 0.01f ? speed : 0f;
            UpdateLimbAnimation(Time.deltaTime);

            // Check boarding proximity
            _canBoard = false;
            if (spaceship != null)
            {
                float dist = Vector3.Distance(transform.position, spaceship.transform.position);
                if (dist <= boardDistance)
                {
                    _canBoard = true;
                    if (Input.GetKeyDown(interactKey))
                    {
                        BoardSpaceship();
                    }
                }
            }
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

        private void UpdateLimbAnimation(float dt)
        {
            if (_bodyTr == null && _legLeftTr == null) return;

            float blend = Mathf.Clamp01(dt * 12f);

            if (!_cc.isGrounded)
            {
                // Airborne / Jump pose
                if (_legLeftTr != null) _legLeftTr.localRotation = Quaternion.Slerp(_legLeftTr.localRotation, _initLegLRot * Quaternion.Euler(-24f, 0f, 0f), blend);
                if (_legRightTr != null) _legRightTr.localRotation = Quaternion.Slerp(_legRightTr.localRotation, _initLegRRot * Quaternion.Euler(18f, 0f, 0f), blend);
                if (_armLeftTr != null) _armLeftTr.localRotation = Quaternion.Slerp(_armLeftTr.localRotation, _initArmLRot * Quaternion.Euler(-30f, 0f, -18f), blend);
                if (_armRightTr != null) _armRightTr.localRotation = Quaternion.Slerp(_armRightTr.localRotation, _initArmRRot * Quaternion.Euler(-30f, 0f, 18f), blend);
                if (_bodyTr != null) _bodyTr.localRotation = Quaternion.Slerp(_bodyTr.localRotation, _initBodyRot * Quaternion.Euler(4f, 0f, 0f), blend);
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
            if (_isThirdPerson)
            {
                Quaternion camRot = Quaternion.Euler(_pitch, _yaw, 0f);
                Vector3 desiredPos = transform.position + camRot * thirdPersonOffset;
                Vector3 lookTarget = transform.position + Vector3.up * 1.45f;

                // Prevent camera clipping through hangar walls/floors
                Vector3 dir = desiredPos - lookTarget;
                if (Physics.Raycast(lookTarget, dir.normalized, out RaycastHit hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.transform.IsChildOf(transform))
                    {
                        desiredPos = hit.point - dir.normalized * 0.25f;
                    }
                }

                playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desiredPos, Time.deltaTime * 16f);
                playerCamera.transform.LookAt(lookTarget);
            }
            else
            {
                playerCamera.transform.position = transform.position + transform.TransformDirection(firstPersonEyeOffset);
                playerCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
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
            if (rb != null) rb.isKinematic = false;
            spaceship.enabled = true;
            SetShipHudVisible(true);

            if (spaceshipCamera != null)
            {
                spaceshipCamera.target = spaceship.transform;
                spaceshipCamera.spaceship = spaceship;
                spaceshipCamera.enabled = true;
            }

            // Spaceship mouse flight stick uses unlocked cursor
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
            if (Time.timeScale <= 0.01f) return;

            if ((!IsInShip && _canBoard) || (IsInShip && _canExit))
            {
                var style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                style.normal.textColor = new Color(0.45f, 1f, 0.9f, 1f);
                GUI.Box(new Rect(Screen.width * 0.5f - 28f, Screen.height - 95f, 56f, 34f), "[F]", style);
            }
        }
    }
}
