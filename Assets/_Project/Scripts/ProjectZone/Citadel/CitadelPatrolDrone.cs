using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    public enum DroneMovementType
    {
        WalkingRobot,
        HoveringDrone
    }

    public enum PatrolLoopMode
    {
        PingPong,
        Loop
    }

    /// <summary>
    /// Điều khiển Robot/Drone tuần tra sinh động bên trong Căn cứ (Citadel Interior):
    /// - Hỗ trợ cả Robot đi bộ 2 chân (Enemy_2Legs) và Drone bay lơ lửng (Enemy_Large).
    /// - Di chuyển mượt mà qua các điểm tuần tra (Waypoints) với giảm tốc và xoay góc tự nhiên.
    /// - Khi đến chốt tuần tra: Dừng lại quan sát, phát hoạt ảnh Idle và quét cảm biến.
    /// - Tương tác người chơi: Khi người chơi đến gần, Drone/Robot sẽ dừng lại, quay đầu chào & quét bảo mật.
    /// - Tích hợp âm thanh 3D giả lập (tiếng động cơ bay, tiếng bước chân servo, tiếng bíp quét điện tử).
    /// </summary>
    public class CitadelPatrolDrone : MonoBehaviour
    {
        [Header("Classification & Identity")]
        public string droneCallsign = "UNIT-01";
        public DroneMovementType movementType = DroneMovementType.WalkingRobot;
        public PatrolLoopMode loopMode = PatrolLoopMode.PingPong;

        [Header("Movement Settings")]
        public float moveSpeed = 1.6f;
        public float turnSpeed = 4.5f;
        public float waypointWaitTime = 2.5f;
        public float stoppingDistance = 0.25f;

        [Header("Hover Settings (For HoveringDrone)")]
        public float hoverBaseHeight = 1.90f;
        public float hoverBobAmplitude = 0.08f;
        public float hoverBobFrequency = 1.8f;
        public float bankAngleMax = 12f;

        [Header("Waypoints")]
        public List<Vector3> waypoints = new List<Vector3>();

        [Header("Player Interaction")]
        public bool reactToPlayer = true;
        public float playerDetectRadius = 3.8f;
        public float scanCooldown = 6.0f;
        public float scanDuration = 2.2f;

        [Header("Visual Effects & Scanner")]
        public Light scannerLight;
        public Color normalScannerColor = new Color(0.15f, 0.85f, 1.0f); // Cyan an ninh
        public Color playerScanColor = new Color(0.2f, 1.0f, 0.5f);     // Xanh lá xác thực
        public float lightPulseSpeed = 2.5f;

        [Header("Components")]
        public Animator animator;

        // Internal State
        private int _currentWaypointIndex = 0;
        private int _waypointDirection = 1;
        private bool _isWaiting = false;
        private float _waitTimer = 0f;
        private bool _isScanningPlayer = false;
        private float _scanTimer = 0f;
        private float _lastScanTime = -10f;
        private Vector3 _originalForward;

        // Audio
        private AudioSource _audioSource;
        private AudioSource _hoverHumSource;
        private AudioClip _servoStepClip;
        private AudioClip _scanBeepClip;
        private AudioClip _hoverHumClip;
        private float _stepTimer = 0f;

        // Visual
        private Transform _visualRoot;
        private float _currentBank = 0f;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _visualRoot = transform.Find("CharacterArmature") ?? transform;
            _originalForward = transform.forward;

            BuildAudioComponents();
            EnsureScannerLight();
        }

        private void Start()
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                // Tự tạo waypoint lân cận nếu chưa thiết lập
                waypoints = new List<Vector3>
                {
                    transform.position,
                    transform.position + transform.forward * 4.0f
                };
            }

            if (movementType == DroneMovementType.HoveringDrone && _hoverHumSource != null)
            {
                _hoverHumSource.Play();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Kiểm tra tương tác với người chơi
            if (reactToPlayer && CheckPlayerProximity())
            {
                HandlePlayerInteraction(dt);
                return;
            }

            // 2. Nếu đang trong trạng thái chờ tại Waypoint
            if (_isWaiting)
            {
                HandleWaypointWait(dt);
                return;
            }

            // 3. Di chuyển đến Waypoint hiện tại
            MoveTowardsTargetWaypoint(dt);

            // 4. Cập nhật đèn scanner nhấp nháy êm dịu
            UpdateScannerLight(false);
        }

        private bool CheckPlayerProximity()
        {
            if (Time.time < _lastScanTime + scanCooldown) return false;

            Transform playerTr = GetPlayerTransform();
            if (playerTr == null) return false;

            float dist = Vector3.Distance(transform.position, playerTr.position);
            return dist <= playerDetectRadius;
        }

        private Transform GetPlayerTransform()
        {
            if (Camera.main != null) return Camera.main.transform;
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) return playerGo.transform;
            return null;
        }

        private void HandlePlayerInteraction(float dt)
        {
            if (!_isScanningPlayer)
            {
                _isScanningPlayer = true;
                _scanTimer = scanDuration;
                _lastScanTime = Time.time;
                PlayScanBeep();
                if (animator != null) animator.SetBool("IsMoving", false);
            }

            Transform playerTr = GetPlayerTransform();
            if (playerTr != null)
            {
                Vector3 lookDir = playerTr.position - transform.position;
                if (movementType == DroneMovementType.WalkingRobot) lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * 1.5f * dt);
                }
            }

            // Hiệu ứng đèn quét xác thực
            UpdateScannerLight(true);

            // Hover bobbing nếu là drone
            if (movementType == DroneMovementType.HoveringDrone)
            {
                ApplyHoverMotion(Vector3.zero, dt);
            }

            _scanTimer -= dt;
            if (_scanTimer <= 0f)
            {
                _isScanningPlayer = false;
            }
        }

        private void HandleWaypointWait(float dt)
        {
            if (animator != null) animator.SetBool("IsMoving", false);

            if (movementType == DroneMovementType.HoveringDrone)
            {
                ApplyHoverMotion(Vector3.zero, dt);
            }

            _waitTimer -= dt;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                AdvanceWaypoint();
            }
        }

        private void MoveTowardsTargetWaypoint(float dt)
        {
            if (waypoints.Count == 0) return;

            Vector3 target = waypoints[_currentWaypointIndex];
            Vector3 currentPos = transform.position;

            Vector3 toTarget = target - currentPos;
            if (movementType == DroneMovementType.WalkingRobot)
            {
                toTarget.y = 0f; // Khóa độ cao mặt đất cho robot đi bộ
            }

            float dist = toTarget.magnitude;

            if (dist <= stoppingDistance)
            {
                // Đã đến điểm
                _isWaiting = true;
                _waitTimer = waypointWaitTime;
                if (animator != null) animator.SetBool("IsMoving", false);
                return;
            }

            // Di chuyển mượt
            Vector3 moveDir = toTarget.normalized;
            Vector3 newPos = currentPos + moveDir * (moveSpeed * dt);

            if (movementType == DroneMovementType.HoveringDrone)
            {
                float bobY = Mathf.Sin(Time.time * hoverBobFrequency) * hoverBobAmplitude;
                newPos.y = target.y + bobY;
            }

            transform.position = newPos;

            // Xoay hướng
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * dt);
            }

            // Hoạt ảnh
            if (animator != null) animator.SetBool("IsMoving", true);

            // Xử lý góc nghiêng lượn (Banking) cho drone bay
            if (movementType == DroneMovementType.HoveringDrone)
            {
                ApplyHoverMotion(moveDir, dt);
            }

            // Âm thanh bước chân cho robot đi bộ
            if (movementType == DroneMovementType.WalkingRobot)
            {
                _stepTimer += dt * moveSpeed;
                if (_stepTimer >= 0.85f)
                {
                    _stepTimer = 0f;
                    PlayServoStep();
                }
            }
        }

        private void ApplyHoverMotion(Vector3 moveDir, float dt)
        {
            // Nghiêng thân nhẹ theo góc cua
            float targetBank = 0f;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                float turnAngle = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
                targetBank = Mathf.Clamp(-turnAngle * 0.4f, -bankAngleMax, bankAngleMax);
            }
            _currentBank = Mathf.Lerp(_currentBank, targetBank, dt * 3.5f);

            if (_visualRoot != null && _visualRoot != transform)
            {
                Vector3 currentE = _visualRoot.localEulerAngles;
                // Giữ trục X chuẩn từ Blender (270) và bù Roll
                _visualRoot.localRotation = Quaternion.Euler(270f, 0f, _currentBank);
            }
        }

        private void AdvanceWaypoint()
        {
            if (waypoints.Count <= 1) return;

            if (loopMode == PatrolLoopMode.PingPong)
            {
                _currentWaypointIndex += _waypointDirection;
                if (_currentWaypointIndex >= waypoints.Count)
                {
                    _currentWaypointIndex = waypoints.Count - 2;
                    _waypointDirection = -1;
                }
                else if (_currentWaypointIndex < 0)
                {
                    _currentWaypointIndex = 1;
                    _waypointDirection = 1;
                }
            }
            else // Loop
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Count;
            }
        }

        private void UpdateScannerLight(bool isScanningPlayer)
        {
            if (scannerLight == null) return;

            Color targetColor = isScanningPlayer ? playerScanColor : normalScannerColor;
            float pulse = 1.0f + Mathf.Sin(Time.time * (isScanningPlayer ? lightPulseSpeed * 2.5f : lightPulseSpeed)) * 0.35f;

            scannerLight.color = Color.Lerp(scannerLight.color, targetColor, Time.deltaTime * 5f);
            scannerLight.intensity = (isScanningPlayer ? 2.8f : 1.6f) * pulse;
        }

        private void EnsureScannerLight()
        {
            if (scannerLight != null) return;

            var existingLight = transform.Find("Scanner_Light");
            if (existingLight != null)
            {
                scannerLight = existingLight.GetComponent<Light>();
                return;
            }

            GameObject lightGo = new GameObject("Scanner_Light");
            lightGo.transform.SetParent(transform, false);

            if (movementType == DroneMovementType.HoveringDrone)
            {
                lightGo.transform.localPosition = new Vector3(0f, -0.25f, 0.35f);
                lightGo.transform.localRotation = Quaternion.Euler(45f, 0f, 0f); // Chiếu chéo xuống đất
            }
            else
            {
                lightGo.transform.localPosition = new Vector3(0f, 0.45f, 0.25f);
                lightGo.transform.localRotation = Quaternion.Euler(15f, 0f, 0f); // Chiếu nhẹ phía trước mắt
            }

            scannerLight = lightGo.AddComponent<Light>();
            scannerLight.type = LightType.Spot;
            scannerLight.color = normalScannerColor;
            scannerLight.range = movementType == DroneMovementType.HoveringDrone ? 5.5f : 4.0f;
            scannerLight.spotAngle = 45f;
            scannerLight.intensity = 1.8f;
        }

        private void BuildAudioComponents()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1.0f; // 3D sound
            _audioSource.minDistance = 1.5f;
            _audioSource.maxDistance = 15.0f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.playOnAwake = false;

            if (movementType == DroneMovementType.HoveringDrone)
            {
                var humChild = transform.Find("Hover_Hum_Audio");
                GameObject humGo = humChild != null ? humChild.gameObject : new GameObject("Hover_Hum_Audio");
                humGo.transform.SetParent(transform, false);

                _hoverHumSource = humGo.GetComponent<AudioSource>();
                if (_hoverHumSource == null) _hoverHumSource = humGo.AddComponent<AudioSource>();

                _hoverHumSource.spatialBlend = 1.0f;
                _hoverHumSource.minDistance = 1.0f;
                _hoverHumSource.maxDistance = 12.0f;
                _hoverHumSource.rolloffMode = AudioRolloffMode.Linear;
                _hoverHumSource.volume = 0.22f;
                _hoverHumSource.loop = true;

                _hoverHumClip = CreateHoverHumClip();
                _hoverHumSource.clip = _hoverHumClip;
            }

            _servoStepClip = CreateServoClip();
            _scanBeepClip = CreateChirpClip();
        }

        private void PlayServoStep()
        {
            if (_audioSource != null && _servoStepClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                _audioSource.PlayOneShot(_servoStepClip, 0.35f);
            }
        }

        private void PlayScanBeep()
        {
            if (_audioSource != null && _scanBeepClip != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.PlayOneShot(_scanBeepClip, 0.55f);
            }
        }

        // ==================== PROCEDURAL SOUND SYNTHESIS ====================

        private static AudioClip CreateServoClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = 1f - ((float)i / samples);
                float freq = Mathf.Lerp(480f, 180f, (float)i / samples);
                float click = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = UnityEngine.Random.Range(-0.25f, 0.25f);
                data[i] = (click * 0.7f + noise * 0.3f) * decay;
            }
            var clip = AudioClip.Create("Servo_Step", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChirpClip()
        {
            int sampleRate = 44100;
            float duration = 0.24f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;
                float env = Mathf.Sin(progress * Mathf.PI);
                // Hai nốt điện tử vui tươi (Chirp tone 880Hz -> 1320Hz)
                float freq = (progress < 0.5f) ? 880f : 1320f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            var clip = AudioClip.Create("Scan_Chirp", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateHoverHumClip()
        {
            int sampleRate = 44100;
            float duration = 1.0f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Tiếng rù trầm ấm 75Hz + hòa âm 150Hz
                float baseHum = Mathf.Sin(2f * Mathf.PI * 75f * t) * 0.65f;
                float harmonic = Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.25f;
                float air = UnityEngine.Random.Range(-0.05f, 0.05f);
                data[i] = baseHum + harmonic + air;
            }
            var clip = AudioClip.Create("Hover_Hum_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Count == 0) return;

            Gizmos.color = movementType == DroneMovementType.HoveringDrone ? Color.cyan : Color.yellow;
            for (int i = 0; i < waypoints.Count; i++)
            {
                Gizmos.DrawWireSphere(waypoints[i], 0.25f);
                if (i < waypoints.Count - 1)
                {
                    Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
                }
                else if (loopMode == PatrolLoopMode.Loop && waypoints.Count > 2)
                {
                    Gizmos.DrawLine(waypoints[i], waypoints[0]);
                }
            }
        }
    }
}
