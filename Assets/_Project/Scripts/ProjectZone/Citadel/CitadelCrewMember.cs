using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    public enum CrewRole
    {
        ConsoleOperator,    // Ngồi/Đứng gõ bàn phím điều khiển radar/máy tính
        TacticalCommander,  // Đứng chỉ đạo tại bàn sa bàn Hologram
        PatrolEngineer,     // Kỹ sư đi bộ tuần tra kiểm tra các tủ server & bảng điện
        BioScientist        // Nhà khoa học phân tích mẫu sinh học tại bàn kính
    }

    [System.Serializable]
    public class CrewPatrolWaypoint
    {
        public Vector3 position;
        public Vector3 workLookDirection = Vector3.forward;
        public string actionDescription = "Kiểm tra hệ thống";
        public float inspectDuration = 5.0f;
    }

    /// <summary>
    /// Điều khiển các thành viên phi hành đoàn (Astronaut A & B) làm việc chân thực trong Căn cứ (Citadel):
    /// 1. Hoạt ảnh Procedural mượt mà không cần clip ngoài:
    ///    - Gõ bàn phím / điều khiển màn hình với 2 tay nhịp nhàng luân phiên.
    ///    - Dáng đứng tự nhiên, nhịp thở vi mô (micro-breathing) và xoay đầu quan sát xung quanh.
    ///    - Dáng đi bộ uyển chuyển (chân tay vung chéo nhịp nhàng) khi đi tuần kiểm tra máy móc.
    /// 2. Phản ứng sinh động khi người chơi lại gần:
    ///    - Tạm dừng công việc, xoay đầu nhìn về phía người chơi.
    ///    - Giơ tay chào thân thiện (Wave / Salute) và phát âm thanh đàm thoại radio ("Commander on deck!").
    ///    - Quay lại làm việc mượt mà sau khi chào xong.
    /// 3. Hiệu ứng giao diện bàn phím phát sáng và âm thanh thao tác 3D chân thực.
    /// </summary>
    public class CitadelCrewMember : MonoBehaviour
    {
        [Header("Identity & Role")]
        public string memberName = "Sarah Connor";
        public string title = "Sĩ quan Radar";
        public CrewRole role = CrewRole.ConsoleOperator;

        [Header("Transforms (Auto-Resolved)")]
        public Transform headTr;
        public Transform bodyTr;
        public Transform armLeftTr;
        public Transform armRightTr;
        public Transform legLeftTr;
        public Transform legRightTr;

        [Header("Movement & Floor Settings")]
        public float floorY = 0.282f;
        public float moveSpeed = 1.15f;
        public float turnSpeed = 4.5f;

        [Header("Seated / Chair Settings")]
        [Tooltip("Bật nếu nhân viên ngồi làm việc trên ghế đàng hoàng")]
        public bool isSeated = false;
        public float seatSittingHeight = 0.45f;
        public float sittingLegPitch = -80f;

        [Header("Work & Patrol Waypoints (For PatrolEngineer)")]
        public List<CrewPatrolWaypoint> waypoints = new List<CrewPatrolWaypoint>();
        public bool loopPingPong = true;

        [Header("Typing & Working Details")]
        public float typingSpeed = 7.5f;
        public float typingAmplitude = 9.0f;
        public float baseArmPitch = -62f; // Góc vươn tay về phía bàn phím (-62 độ)

        [Header("Player Interaction")]
        public bool reactToPlayer = true;
        public float playerDetectRadius = 2.8f;
        public float greetingCooldown = 8.0f;
        public float greetingDuration = 2.4f;

        // Internal State
        private Vector3 _initialHeadPos;
        private Quaternion _initialHeadRot;
        private Vector3 _initialBodyPos;
        private Quaternion _initialBodyRot;
        private Quaternion _initialArmLRot;
        private Quaternion _initialArmRRot;
        private Quaternion _initialLegLRot;
        private Quaternion _initialLegRRot;

        private int _currentWaypointIndex = 0;
        private int _waypointDirection = 1;
        private bool _isInspecting = false;
        private float _inspectTimer = 0f;

        // Player greeting
        private bool _isGreetingPlayer = false;
        private float _greetingTimer = 0f;
        private float _lastGreetingTime = -10f;

        // Micro-actions
        private float _headLookTimer = 0f;
        private float _targetHeadYaw = 0f;
        private float _targetHeadPitch = 0f;
        private float _typingPauseTimer = 0f;
        private bool _isTypingPaused = false;
        private float _walkCycle = 0f;

        // Audio
        private AudioSource _audioSource;
        private AudioSource _typingAudioSource;
        private AudioClip _typingClip;
        private AudioClip _radioChimeClip;
        private AudioClip _bootStepClip;
        private float _stepDistance = 0f;

        // Optional terminal light
        private Light _terminalGlow;

        private void Awake()
        {
            ResolveTransforms();
            CacheInitialTransforms();
            BuildAudioComponents();
            EnsureTerminalGlow();

            // Khóa cao độ sàn hoặc ghế ngồi chuẩn
            Vector3 p = transform.position;
            p.y = isSeated ? seatSittingHeight : floorY;
            transform.position = p;
        }

        private void ResolveTransforms()
        {
            if (headTr == null) headTr = transform.Find("head");
            if (bodyTr == null) bodyTr = transform.Find("body");
            if (armLeftTr == null) armLeftTr = transform.Find("armLeft");
            if (armRightTr == null) armRightTr = transform.Find("armRight");
            if (legLeftTr == null) legLeftTr = transform.Find("legLeft");
            if (legRightTr == null) legRightTr = transform.Find("legRight");
        }

        private void CacheInitialTransforms()
        {
            if (headTr != null) { _initialHeadPos = headTr.localPosition; _initialHeadRot = headTr.localRotation; }
            if (bodyTr != null) { _initialBodyPos = bodyTr.localPosition; _initialBodyRot = bodyTr.localRotation; }
            if (armLeftTr != null) _initialArmLRot = armLeftTr.localRotation;
            if (armRightTr != null) _initialArmRRot = armRightTr.localRotation;
            if (isSeated)
            {
                if (legLeftTr != null) _initialLegLRot = Quaternion.Euler(sittingLegPitch, 0f, 0f);
                if (legRightTr != null) _initialLegRRot = Quaternion.Euler(sittingLegPitch, 0f, 0f);
            }
            else
            {
                if (legLeftTr != null) _initialLegLRot = legLeftTr.localRotation;
                if (legRightTr != null) _initialLegRRot = legRightTr.localRotation;
            }
        }

        private void Start()
        {
            if (role == CrewRole.ConsoleOperator || role == CrewRole.TacticalCommander || role == CrewRole.BioScientist)
            {
                if (_typingAudioSource != null) _typingAudioSource.Play();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Kiểm tra tương tác khi người chơi lại gần
            if (reactToPlayer && CheckPlayerProximity())
            {
                HandlePlayerGreeting(dt);
                return;
            }

            // 2. Nếu đang trong trạng thái chào người chơi
            if (_isGreetingPlayer)
            {
                UpdateGreetingAnimation(dt);
                return;
            }

            // 3. Phân luồng công việc theo vai trò
            if (role == CrewRole.PatrolEngineer)
            {
                HandlePatrolEngineer(dt);
            }
            else
            {
                HandleStationaryWorker(dt);
            }
        }

        private bool CheckPlayerProximity()
        {
            if (Time.time < _lastGreetingTime + greetingCooldown) return false;

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

        private void HandlePlayerGreeting(float dt)
        {
            _isGreetingPlayer = true;
            _greetingTimer = greetingDuration;
            _lastGreetingTime = Time.time;
            PlayRadioChime();

            if (_typingAudioSource != null && _typingAudioSource.isPlaying)
            {
                _typingAudioSource.Pause();
            }
        }

        private void UpdateGreetingAnimation(float dt)
        {
            _greetingTimer -= dt;

            // Xoay đầu nhìn thẳng vào người chơi
            Transform playerTr = GetPlayerTransform();
            if (playerTr != null && headTr != null)
            {
                Vector3 toPlayer = playerTr.position - (transform.position + Vector3.up * 1.1f);
                Vector3 localDir = transform.InverseTransformDirection(toPlayer);
                float targetYaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -65f, 65f);
                float targetPitch = Mathf.Clamp(-localDir.y * 30f, -25f, 25f);
                headTr.localRotation = Quaternion.Slerp(headTr.localRotation, Quaternion.Euler(targetPitch, targetYaw, 0f), dt * 6f);
            }

            // Giơ tay phải vẫy chào người chơi (Wave salute)
            if (armRightTr != null)
            {
                float wave = Mathf.Sin(Time.time * 9f) * 16f;
                // Vươn tay lên chào: X = -120deg, Z = wave
                armRightTr.localRotation = Quaternion.Slerp(armRightTr.localRotation, Quaternion.Euler(-120f, 0f, 20f + wave), dt * 7f);
            }

            // Tay trái hạ nhẹ hoặc đặt bên sườn
            if (armLeftTr != null)
            {
                armLeftTr.localRotation = Quaternion.Slerp(armLeftTr.localRotation, Quaternion.Euler(-15f, 0f, 0f), dt * 5f);
            }

            // Nếu đang ngồi thì giữ nguyên tư thế chân ngồi trên ghế
            if (isSeated)
            {
                if (legLeftTr != null) legLeftTr.localRotation = Quaternion.Euler(sittingLegPitch, 0f, 0f);
                if (legRightTr != null) legRightTr.localRotation = Quaternion.Euler(sittingLegPitch, 0f, 0f);
            }

            if (_greetingTimer <= 0f)
            {
                _isGreetingPlayer = false;
                if (_typingAudioSource != null) _typingAudioSource.UnPause();
            }
        }

        // ==================== CHẾ ĐỘ 1: LÀM VIỆC TẠI CHỖ (Console / Commander / Scientist) ====================

        private void HandleStationaryWorker(float dt)
        {
            // Đảm bảo tư thế chân (ngồi gập đùi trên ghế hoặc đứng thẳng)
            if (isSeated)
            {
                if (legLeftTr != null) legLeftTr.localRotation = Quaternion.Euler(sittingLegPitch, 0f, 0f);
                if (legRightTr != null) legRightTr.localRotation = Quaternion.Euler(sittingLegPitch, 0f, 0f);
            }
            else
            {
                if (legLeftTr != null) legLeftTr.localRotation = _initialLegLRot;
                if (legRightTr != null) legRightTr.localRotation = _initialLegRRot;
            }

            // 1. Nhịp thở vi mô (Micro-breathing)
            float breath = Mathf.Sin(Time.time * 2.2f) * 0.008f;
            if (bodyTr != null) bodyTr.localPosition = _initialBodyPos + new Vector3(0f, breath, 0f);

            // 2. Quản lý nhịp gõ phím / làm việc (Typing & Pausing)
            _typingPauseTimer -= dt;
            if (_typingPauseTimer <= 0f)
            {
                _isTypingPaused = !_isTypingPaused;
                _typingPauseTimer = _isTypingPaused ? UnityEngine.Random.Range(1.0f, 2.2f) : UnityEngine.Random.Range(3.5f, 7.0f);
                if (_typingAudioSource != null)
                {
                    if (_isTypingPaused) _typingAudioSource.Pause();
                    else _typingAudioSource.UnPause();
                }
            }

            // 3. Hoạt ảnh gõ bàn phím luân phiên 2 tay
            if (!_isTypingPaused)
            {
                float tapL = Mathf.Sin(Time.time * typingSpeed) * typingAmplitude;
                float tapR = Mathf.Sin(Time.time * typingSpeed + Mathf.PI * 0.5f) * typingAmplitude;

                if (role == CrewRole.TacticalCommander)
                {
                    // Chỉ huy: Tay trái chống bàn / chỉ trỏ, tay phải thao tác hologram
                    if (armLeftTr != null) armLeftTr.localRotation = Quaternion.Euler(baseArmPitch * 0.65f, -12f, 0f);
                    if (armRightTr != null) armRightTr.localRotation = Quaternion.Euler(baseArmPitch + tapR, 10f, 0f);
                }
                else
                {
                    // Gõ bàn phím cả 2 tay
                    if (armLeftTr != null) armLeftTr.localRotation = Quaternion.Euler(baseArmPitch + tapL, -8f, 0f);
                    if (armRightTr != null) armRightTr.localRotation = Quaternion.Euler(baseArmPitch + tapR, 8f, 0f);
                }

                if (_terminalGlow != null)
                {
                    _terminalGlow.intensity = 1.2f + Mathf.Sin(Time.time * 12f) * 0.35f;
                }
            }
            else
            {
                // Khi tạm dừng gõ: Tay hạ nhẹ, người ngước nhìn màn hình
                if (armLeftTr != null) armLeftTr.localRotation = Quaternion.Slerp(armLeftTr.localRotation, Quaternion.Euler(baseArmPitch + 15f, -4f, 0f), dt * 4f);
                if (armRightTr != null) armRightTr.localRotation = Quaternion.Slerp(armRightTr.localRotation, Quaternion.Euler(baseArmPitch + 15f, 4f, 0f), dt * 4f);
            }

            // 4. Hoạt ảnh xoay đầu quan sát ngẫu nhiên
            _headLookTimer -= dt;
            if (_headLookTimer <= 0f)
            {
                _headLookTimer = UnityEngine.Random.Range(3.0f, 6.0f);
                _targetHeadYaw = UnityEngine.Random.Range(-25f, 25f);
                _targetHeadPitch = (role == CrewRole.ConsoleOperator) ? UnityEngine.Random.Range(-12f, 5f) : UnityEngine.Random.Range(-8f, 15f);
            }

            if (headTr != null)
            {
                Quaternion targetHeadRot = Quaternion.Euler(_targetHeadPitch, _targetHeadYaw, 0f);
                headTr.localRotation = Quaternion.Slerp(headTr.localRotation, targetHeadRot, dt * 3.5f);
            }
        }

        // ==================== CHẾ ĐỘ 2: KỸ SƯ ĐI BỘ TUẦN TRA (Patrol Engineer) ====================

        private void HandlePatrolEngineer(float dt)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                HandleStationaryWorker(dt);
                return;
            }

            // Nếu đang kiểm tra tại trạm máy
            if (_isInspecting)
            {
                HandleStationaryWorker(dt);
                _inspectTimer -= dt;
                if (_inspectTimer <= 0f)
                {
                    _isInspecting = false;
                    AdvanceWaypoint();
                }
                return;
            }

            // Di chuyển đến Waypoint tiếp theo
            CrewPatrolWaypoint targetWp = waypoints[_currentWaypointIndex];
            Vector3 targetPos = targetWp.position;
            targetPos.y = floorY;

            Vector3 currentPos = transform.position;
            currentPos.y = floorY;

            Vector3 toTarget = targetPos - currentPos;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist <= 0.20f)
            {
                // Đến nơi -> Bắt đầu kiểm tra
                _isInspecting = true;
                _inspectTimer = targetWp.inspectDuration;

                // Xoay mặt về hướng làm việc
                if (targetWp.workLookDirection.sqrMagnitude > 0.001f)
                {
                    Vector3 look = targetWp.workLookDirection;
                    look.y = 0f;
                    transform.rotation = Quaternion.LookRotation(look);
                }
                return;
            }

            // Di chuyển
            Vector3 moveDir = toTarget.normalized;
            transform.position = currentPos + moveDir * (moveSpeed * dt);

            // Quay mặt theo hướng đi
            if (moveDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), turnSpeed * dt);
            }

            // Hoạt ảnh bước chân và vung tay đi bộ tự nhiên
            _walkCycle += dt * (moveSpeed * 6.5f);
            float legSwing = Mathf.Sin(_walkCycle) * 32f;
            float armSwing = -legSwing * 0.85f;

            if (legLeftTr != null) legLeftTr.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
            if (legRightTr != null) legRightTr.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);

            if (armLeftTr != null) armLeftTr.localRotation = Quaternion.Euler(armSwing, 0f, 0f);
            if (armRightTr != null) armRightTr.localRotation = Quaternion.Euler(-armSwing, 0f, 0f);

            if (bodyTr != null)
            {
                float bobY = Mathf.Abs(Mathf.Sin(_walkCycle)) * 0.025f;
                bodyTr.localPosition = _initialBodyPos + new Vector3(0f, bobY, 0f);
            }

            // Âm thanh bước chân
            _stepDistance += dt * moveSpeed;
            if (_stepDistance >= 0.70f)
            {
                _stepDistance = 0f;
                PlayFootstep();
            }
        }

        private void AdvanceWaypoint()
        {
            if (waypoints.Count <= 1) return;

            if (loopPingPong)
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
            else
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Count;
            }
        }

        private void EnsureTerminalGlow()
        {
            if (role == CrewRole.PatrolEngineer) return;

            var existingLight = transform.Find("Terminal_Glow_Light");
            if (existingLight != null)
            {
                _terminalGlow = existingLight.GetComponent<Light>();
                return;
            }

            GameObject lightGo = new GameObject("Terminal_Glow_Light");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.55f, 0.40f);

            _terminalGlow = lightGo.AddComponent<Light>();
            _terminalGlow.type = LightType.Point;
            _terminalGlow.color = (role == CrewRole.TacticalCommander) ? new Color(0.2f, 0.85f, 1.0f) : new Color(0.25f, 0.95f, 0.75f);
            _terminalGlow.intensity = 1.2f;
            _terminalGlow.range = 1.8f;
        }

        private void BuildAudioComponents()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1.0f;
            _audioSource.minDistance = 1.2f;
            _audioSource.maxDistance = 10.0f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.playOnAwake = false;

            // Kênh âm thanh gõ phím liên tục
            var typingChild = transform.Find("Typing_Audio_Source");
            GameObject typingGo = typingChild != null ? typingChild.gameObject : new GameObject("Typing_Audio_Source");
            typingGo.transform.SetParent(transform, false);

            _typingAudioSource = typingGo.GetComponent<AudioSource>();
            if (_typingAudioSource == null) _typingAudioSource = typingGo.AddComponent<AudioSource>();
            _typingAudioSource.spatialBlend = 1.0f;
            _typingAudioSource.minDistance = 0.8f;
            _typingAudioSource.maxDistance = 6.0f;
            _typingAudioSource.rolloffMode = AudioRolloffMode.Linear;
            _typingAudioSource.volume = 0.18f;
            _typingAudioSource.loop = true;
            _typingAudioSource.playOnAwake = false;

            _typingClip = CreateTypingClip();
            _typingAudioSource.clip = _typingClip;

            _radioChimeClip = CreateRadioChimeClip();
            _bootStepClip = CreateBootStepClip();
        }

        private void PlayRadioChime()
        {
            if (_audioSource != null && _radioChimeClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                _audioSource.PlayOneShot(_radioChimeClip, 0.55f);
            }
        }

        private void PlayFootstep()
        {
            if (_audioSource != null && _bootStepClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                _audioSource.PlayOneShot(_bootStepClip, 0.30f);
            }
        }

        // ==================== PROCEDURAL SOUND SYNTHESIS ====================

        private static AudioClip CreateTypingClip()
        {
            // Tiếng gõ phím cơ học không gian nhẹ nhàng
            int sampleRate = 44100;
            float duration = 1.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float keyCycle = (t * 6.5f) % 1f;
                float env = Mathf.Exp(-keyCycle * 35f);
                float click = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.7f + UnityEngine.Random.Range(-0.25f, 0.25f) * 0.3f;
                data[i] = click * env * 0.35f;
            }
            var clip = AudioClip.Create("Keyboard_Typing_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateRadioChimeClip()
        {
            // Chuông đàm thoại vô tuyến (Double-chime 1046Hz -> 1318Hz: C6 -> E6)
            int sampleRate = 44100;
            float duration = 0.30f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;
                float env = Mathf.Sin(progress * Mathf.PI);
                float freq = (progress < 0.5f) ? 1046.5f : 1318.5f;
                float radioFilter = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.8f + UnityEngine.Random.Range(-0.1f, 0.1f) * 0.2f);
                data[i] = radioFilter * env * 0.50f;
            }
            var clip = AudioClip.Create("Radio_Greeting_Chime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateBootStepClip()
        {
            int sampleRate = 44100;
            float duration = 0.09f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = 1f - ((float)i / samples);
                float thud = Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.7f;
                float friction = UnityEngine.Random.Range(-0.2f, 0.2f) * 0.3f;
                data[i] = (thud + friction) * decay;
            }
            var clip = AudioClip.Create("Astronaut_Boot_Step", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        [ContextMenu("Thiết lập tư thế Ngồi Ghế (Pose Seated)")]
        public void PoseSeatedInEditor()
        {
            isSeated = true;
            Vector3 p = transform.position;
            p.y = seatSittingHeight;
            transform.position = p;
            ResolveTransforms();
            if (legLeftTr != null) legLeftTr.localRotation = Quaternion.Euler(sittingLegPitch, 0f, 0f);
            if (legRightTr != null) legRightTr.localRotation = Quaternion.Euler(sittingLegPitch, 0f, 0f);
            if (armLeftTr != null) armLeftTr.localRotation = Quaternion.Euler(-58f, -6f, 0f);
            if (armRightTr != null) armRightTr.localRotation = Quaternion.Euler(-58f, 6f, 0f);
            if (bodyTr != null) bodyTr.localRotation = Quaternion.Euler(4f, 0f, 0f);
            if (headTr != null) headTr.localRotation = Quaternion.Euler(-4f, 0f, 0f);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Thiết lập tư thế Đứng (Pose Standing)")]
        public void PoseStandingInEditor()
        {
            isSeated = false;
            Vector3 p = transform.position;
            p.y = floorY;
            transform.position = p;
            ResolveTransforms();
            if (legLeftTr != null) legLeftTr.localRotation = Quaternion.identity;
            if (legRightTr != null) legRightTr.localRotation = Quaternion.identity;
            if (armLeftTr != null) armLeftTr.localRotation = Quaternion.identity;
            if (armRightTr != null) armRightTr.localRotation = Quaternion.identity;
            if (bodyTr != null) bodyTr.localRotation = Quaternion.identity;
            if (headTr != null) headTr.localRotation = Quaternion.identity;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
