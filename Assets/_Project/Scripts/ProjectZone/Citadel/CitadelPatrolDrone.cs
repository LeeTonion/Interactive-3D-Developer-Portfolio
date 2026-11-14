using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    public enum DroneMovementType
    {
        WalkingRobot,  // Robot 2 chân đi bộ trên mặt sàn (Enemy_2Legs, Enemy_Large)
        HoveringDrone  // Drone bay lơ lửng trên không trung (Enemy_Flying - KHÔNG CÓ CHÂN)
    }

    public enum WorkActionType
    {
        None,               // Dừng quan sát hoặc tuần tra thường
        DataTerminalScan,   // Phân tích dữ liệu terminal / màn hình máy tính (Tia laser cyan + gõ mã)
        WeldingRepair,      // Hàn bảo trì đường ống / tủ điện (Tia lửa hàn hồ quang điện nhấp nháy + tiếng hàn)
        BioFloraScan,       // Quét phân tích sinh thái cây trồng (Tia quang phổ xanh ngọc + tiếng scanner)
        CargoInspection,    // Kiểm định container hàng hóa & airlock (Nón quét an ninh hổ phách)
        TacticalHoloUplink  // Nạp luồng dữ liệu vệ tinh sa bàn (Tia định vị chiến thuật cyan từ trên cao)
    }

    [System.Serializable]
    public class DroneWorkstation
    {
        public string stationName = "Trạm làm việc";
        public Vector3 position;
        public Vector3 lookDirection = Vector3.forward;
        public WorkActionType workAction = WorkActionType.DataTerminalScan;
        public float workDuration = 5.0f;
        public Vector3 workTargetOffset = new Vector3(0f, 0.5f, 1.5f);
    }

    /// <summary>
    /// Điều khiển Robot và Drone làm việc chân thực và sinh động trong Căn cứ (Citadel):
    /// 1. Robot đi bộ (WalkingRobot): Chân đặt vững chắc trên sàn (Y=0.282), tuyệt đối không đi bộ trên không!
    /// 2. Drone bay (HoveringDrone): Dùng mô hình Enemy_Flying (không chân), lơ lửng trên không tự nhiên.
    /// 3. Nhiệm vụ làm việc (Work Routines):
    ///    - Quét máy tính / kiểm tra mã màn hình (DataTerminalScan).
    ///    - Hàn hồ quang sửa chữa đường ống & tủ điện (WeldingRepair).
    ///    - Quét chỉ số quang phổ sinh thái cây trồng (BioFloraScan).
    ///    - Kiểm định container hàng hóa và khử khuẩn Airlock (CargoInspection).
    ///    - Truyền luồng dữ liệu chiến thuật xuống sa bàn Hologram (TacticalHoloUplink).
    /// 4. Tương tác khi người chơi đến gần: Tự động dừng công việc, quay đầu chào & quét an ninh.
    /// 5. Tích hợp âm thanh 3D thời gian thực (tiếng hàn điện, tiếng gõ phím cybernetic, tiếng sonar scanner).
    /// </summary>
    public class CitadelPatrolDrone : MonoBehaviour
    {
        [Header("Classification & Identity")]
        public string droneCallsign = "UNIT-01";
        public string jobTitle = "Kỹ thuật viên Trạm";
        public DroneMovementType movementType = DroneMovementType.WalkingRobot;

        [Header("Ground / Floor Settings")]
        public float floorY = 0.282f; // Cao độ sàn chuẩn của căn cứ

        [Header("Movement Speeds")]
        public float moveSpeed = 1.35f;
        public float turnSpeed = 4.5f;
        public float stoppingDistance = 0.25f;

        [Header("Hover Settings (For HoveringDrone Only)")]
        public float hoverBaseHeight = 1.95f;
        public float hoverBobAmplitude = 0.07f;
        public float hoverBobFrequency = 1.6f;
        public float bankAngleMax = 12f;

        [Header("Workstations Schedule")]
        public List<DroneWorkstation> workstations = new List<DroneWorkstation>();
        public bool loopForwardAndBack = true;

        [Header("Player Interaction")]
        public bool reactToPlayer = true;
        public float playerDetectRadius = 3.5f;
        public float scanCooldown = 6.0f;
        public float scanDuration = 2.0f;

        [Header("Visual Tools & Lights")]
        public Light scannerLight;
        public Color normalScannerColor = new Color(0.15f, 0.85f, 1.0f);
        public Color playerScanColor = new Color(0.2f, 1.0f, 0.5f);

        [Header("Components")]
        public Animator animator;

        // Internal State
        private int _currentStationIndex = 0;
        private int _stationDirection = 1;
        private bool _isWorking = false;
        private float _workTimer = 0f;
        private bool _isScanningPlayer = false;
        private float _playerScanTimer = 0f;
        private float _lastPlayerScanTime = -10f;

        // Visual Tools (Beam & Light)
        private LineRenderer _workBeamLine;
        private Light _workToolLight;
        private Transform _visualRoot;
        private float _currentBank = 0f;

        // Audio
        private AudioSource _audioSource;
        private AudioSource _workLoopSource;
        private AudioSource _hoverHumSource;
        private AudioClip _servoStepClip;
        private AudioClip _chirpClip;
        private AudioClip _dataProcessClip;
        private AudioClip _weldingSparkClip;
        private AudioClip _bioScanClip;
        private AudioClip _sonarPingClip;
        private AudioClip _hoverHumClip;
        private float _stepTimer = 0f;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _visualRoot = transform.Find("CharacterArmature") ?? transform;

            // Khóa cao độ sàn ngay lập tức nếu là robot đi bộ
            if (movementType == DroneMovementType.WalkingRobot)
            {
                Vector3 p = transform.position;
                p.y = floorY;
                transform.position = p;
            }

            BuildAudioComponents();
            EnsureScannerLight();
            EnsureWorkToolEffects();
        }

        private void Start()
        {
            if (workstations == null || workstations.Count == 0)
            {
                workstations = new List<DroneWorkstation>
                {
                    new DroneWorkstation
                    {
                        stationName = "Trạm Ban Đầu",
                        position = transform.position,
                        lookDirection = transform.forward,
                        workAction = WorkActionType.None,
                        workDuration = 3.0f
                    }
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

            // 1. Tương tác ưu tiên với người chơi khi lại gần
            if (reactToPlayer && CheckPlayerProximity())
            {
                HandlePlayerInteraction(dt);
                return;
            }

            // 2. Nếu đang làm việc tại trạm
            if (_isWorking)
            {
                HandleWorkingRoutine(dt);
                return;
            }

            // 3. Di chuyển đến trạm làm việc tiếp theo
            MoveTowardsStation(dt);

            // 4. Cập nhật đèn scanner nhấp nháy êm dịu khi di chuyển
            UpdateScannerLight(false);
        }

        private bool CheckPlayerProximity()
        {
            if (Time.time < _lastPlayerScanTime + scanCooldown) return false;

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
            // Tạm dừng công việc hiện tại để chào người chơi
            DeactivateWorkToolEffects();

            if (!_isScanningPlayer)
            {
                _isScanningPlayer = true;
                _playerScanTimer = scanDuration;
                _lastPlayerScanTime = Time.time;
                PlayChirp();
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

            UpdateScannerLight(true);

            if (movementType == DroneMovementType.HoveringDrone)
            {
                ApplyHoverMotion(Vector3.zero, dt);
            }

            _playerScanTimer -= dt;
            if (_playerScanTimer <= 0f)
            {
                _isScanningPlayer = false;
            }
        }

        private void MoveTowardsStation(float dt)
        {
            if (workstations.Count == 0) return;

            DroneWorkstation targetWs = workstations[_currentStationIndex];
            Vector3 targetPos = targetWs.position;
            Vector3 currentPos = transform.position;

            if (movementType == DroneMovementType.WalkingRobot)
            {
                targetPos.y = floorY;
                currentPos.y = floorY;
            }

            Vector3 toTarget = targetPos - currentPos;
            if (movementType == DroneMovementType.WalkingRobot) toTarget.y = 0f;

            float dist = toTarget.magnitude;

            if (dist <= stoppingDistance)
            {
                // Đã đến trạm làm việc -> Bắt đầu làm việc!
                _isWorking = true;
                _workTimer = targetWs.workDuration;
                if (animator != null) animator.SetBool("IsMoving", false);
                StartWorkAction(targetWs);
                return;
            }

            // Di chuyển mượt
            Vector3 moveDir = toTarget.normalized;
            Vector3 newPos = currentPos + moveDir * (moveSpeed * dt);

            if (movementType == DroneMovementType.WalkingRobot)
            {
                newPos.y = floorY; // LUÔN KHÓA VỮNG CHẮC TRÊN MẶT SÀN!
            }
            else // HoveringDrone
            {
                float bobY = Mathf.Sin(Time.time * hoverBobFrequency) * hoverBobAmplitude;
                newPos.y = targetWs.position.y + bobY;
            }

            transform.position = newPos;

            // Quay mặt về hướng di chuyển
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * dt);
            }

            if (animator != null) animator.SetBool("IsMoving", true);

            if (movementType == DroneMovementType.HoveringDrone)
            {
                ApplyHoverMotion(moveDir, dt);
            }
            else
            {
                // Âm thanh bước chân servo khi bước đi
                _stepTimer += dt * moveSpeed;
                if (_stepTimer >= 0.85f)
                {
                    _stepTimer = 0f;
                    PlayServoStep();
                }
            }
        }

        private void HandleWorkingRoutine(float dt)
        {
            if (animator != null) animator.SetBool("IsMoving", false);

            if (movementType == DroneMovementType.WalkingRobot)
            {
                Vector3 p = transform.position;
                p.y = floorY;
                transform.position = p;
            }
            else
            {
                ApplyHoverMotion(Vector3.zero, dt);
            }

            DroneWorkstation ws = workstations[_currentStationIndex];

            // Quay mặt chuẩn xác về hướng làm việc (máy tính, bồn cây, mối hàn, sa bàn)
            if (ws.lookDirection.sqrMagnitude > 0.001f)
            {
                Vector3 look = ws.lookDirection;
                if (movementType == DroneMovementType.WalkingRobot) look.y = 0f;
                Quaternion targetRot = Quaternion.LookRotation(look.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * dt);
            }

            // Cập nhật hiệu ứng tia làm việc sống động theo từng loại nghề
            UpdateWorkToolEffects(ws, dt);

            _workTimer -= dt;
            if (_workTimer <= 0f)
            {
                _isWorking = false;
                DeactivateWorkToolEffects();
                AdvanceStation();
            }
        }

        private void StartWorkAction(DroneWorkstation ws)
        {
            if (ws.workAction == WorkActionType.None) return;

            if (_workBeamLine != null) _workBeamLine.enabled = true;
            if (_workToolLight != null) _workToolLight.enabled = true;

            // Bật âm thanh làm việc tương ứng
            if (_workLoopSource != null)
            {
                switch (ws.workAction)
                {
                    case WorkActionType.DataTerminalScan:
                        _workLoopSource.clip = _dataProcessClip;
                        _workLoopSource.volume = 0.28f;
                        _workLoopSource.Play();
                        break;
                    case WorkActionType.WeldingRepair:
                        _workLoopSource.clip = _weldingSparkClip;
                        _workLoopSource.volume = 0.35f;
                        _workLoopSource.Play();
                        break;
                    case WorkActionType.BioFloraScan:
                        _workLoopSource.clip = _bioScanClip;
                        _workLoopSource.volume = 0.25f;
                        _workLoopSource.Play();
                        break;
                    case WorkActionType.CargoInspection:
                        _workLoopSource.clip = _sonarPingClip;
                        _workLoopSource.volume = 0.30f;
                        _workLoopSource.Play();
                        break;
                    case WorkActionType.TacticalHoloUplink:
                        _workLoopSource.clip = _dataProcessClip;
                        _workLoopSource.volume = 0.22f;
                        _workLoopSource.Play();
                        break;
                }
            }
        }

        private void UpdateWorkToolEffects(DroneWorkstation ws, float dt)
        {
            if (ws.workAction == WorkActionType.None) return;

            Vector3 startPos = transform.position + Vector3.up * (movementType == DroneMovementType.WalkingRobot ? 0.45f : 0.10f) + transform.forward * 0.15f;
            Vector3 targetPoint = transform.position + transform.rotation * ws.workTargetOffset;

            switch (ws.workAction)
            {
                case WorkActionType.DataTerminalScan:
                    // Tia laser cyan quét nhẹ lên xuống màn hình máy tính
                    float sweepY = Mathf.Sin(Time.time * 4.5f) * 0.20f;
                    Vector3 screenPoint = targetPoint + Vector3.up * sweepY;
                    DrawWorkBeam(startPos, screenPoint, new Color(0.1f, 0.85f, 1.0f), 0.025f);
                    if (_workToolLight != null)
                    {
                        _workToolLight.transform.position = screenPoint;
                        _workToolLight.color = new Color(0.1f, 0.85f, 1.0f);
                        _workToolLight.intensity = 1.4f + Mathf.Sin(Time.time * 8f) * 0.4f;
                        _workToolLight.range = 2.5f;
                    }
                    break;

                case WorkActionType.WeldingRepair:
                    // Mối hàn hồ quang điện: nhấp nháy cực nhanh màu vàng cam & phát xung
                    float flick = UnityEngine.Random.Range(0.4f, 1.0f);
                    DrawWorkBeam(startPos, targetPoint, new Color(1.0f, 0.7f, 0.1f) * flick, 0.035f);
                    if (_workToolLight != null)
                    {
                        _workToolLight.transform.position = targetPoint;
                        _workToolLight.color = new Color(1.0f, 0.85f, 0.3f);
                        _workToolLight.intensity = flick * 3.8f;
                        _workToolLight.range = 3.5f;
                    }
                    break;

                case WorkActionType.BioFloraScan:
                    // Tia quét quang phổ xanh ngọc rọi vào cây trồng
                    float bioSweep = Mathf.Sin(Time.time * 2.5f) * 0.35f;
                    Vector3 floraPoint = targetPoint + transform.right * bioSweep;
                    DrawWorkBeam(startPos, floraPoint, new Color(0.15f, 1.0f, 0.5f), 0.03f);
                    if (_workToolLight != null)
                    {
                        _workToolLight.transform.position = floraPoint;
                        _workToolLight.color = new Color(0.2f, 1.0f, 0.5f);
                        _workToolLight.intensity = 1.8f;
                        _workToolLight.range = 2.8f;
                    }
                    break;

                case WorkActionType.CargoInspection:
                    // Nón quét an ninh màu hổ phách rọi vào container hàng hóa
                    float cargoSweepX = Mathf.Sin(Time.time * 2.0f) * 0.40f;
                    Vector3 cargoPoint = targetPoint + transform.right * cargoSweepX;
                    DrawWorkBeam(startPos, cargoPoint, new Color(1.0f, 0.75f, 0.15f), 0.04f);
                    if (_workToolLight != null)
                    {
                        _workToolLight.transform.position = cargoPoint;
                        _workToolLight.color = new Color(1.0f, 0.75f, 0.15f);
                        _workToolLight.intensity = 2.2f;
                        _workToolLight.range = 3.0f;
                    }
                    break;

                case WorkActionType.TacticalHoloUplink:
                    // Tia định vị uplink rọi thẳng xuống tâm quả cầu sa bàn Hologram
                    Vector3 holoCenter = new Vector3(14.0f, 1.25f, 23.0f);
                    DrawWorkBeam(startPos, holoCenter, new Color(0.2f, 0.9f, 1.0f), 0.035f);
                    if (_workToolLight != null)
                    {
                        _workToolLight.transform.position = holoCenter;
                        _workToolLight.color = new Color(0.2f, 0.9f, 1.0f);
                        _workToolLight.intensity = 2.5f + Mathf.Sin(Time.time * 6f) * 0.8f;
                        _workToolLight.range = 4.0f;
                    }
                    break;
            }
        }

        private void DrawWorkBeam(Vector3 start, Vector3 end, Color col, float width)
        {
            if (_workBeamLine != null)
            {
                _workBeamLine.startColor = col;
                _workBeamLine.endColor = col;
                _workBeamLine.startWidth = width * 0.6f;
                _workBeamLine.endWidth = width;
                _workBeamLine.SetPosition(0, start);
                _workBeamLine.SetPosition(1, end);
            }
        }

        private void DeactivateWorkToolEffects()
        {
            if (_workBeamLine != null) _workBeamLine.enabled = false;
            if (_workToolLight != null) _workToolLight.enabled = false;
            if (_workLoopSource != null && _workLoopSource.isPlaying)
            {
                _workLoopSource.Stop();
            }
        }

        private void AdvanceStation()
        {
            if (workstations.Count <= 1) return;

            if (loopForwardAndBack)
            {
                _currentStationIndex += _stationDirection;
                if (_currentStationIndex >= workstations.Count)
                {
                    _currentStationIndex = workstations.Count - 2;
                    _stationDirection = -1;
                }
                else if (_currentStationIndex < 0)
                {
                    _currentStationIndex = 1;
                    _stationDirection = 1;
                }
            }
            else
            {
                _currentStationIndex = (_currentStationIndex + 1) % workstations.Count;
            }
        }

        private void ApplyHoverMotion(Vector3 moveDir, float dt)
        {
            float targetBank = 0f;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                float turnAngle = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
                targetBank = Mathf.Clamp(-turnAngle * 0.4f, -bankAngleMax, bankAngleMax);
            }
            _currentBank = Mathf.Lerp(_currentBank, targetBank, dt * 3.5f);

            if (_visualRoot != null && _visualRoot != transform)
            {
                _visualRoot.localRotation = Quaternion.Euler(270f, 0f, _currentBank);
            }
        }

        private void UpdateScannerLight(bool isScanningPlayer)
        {
            if (scannerLight == null) return;

            Color targetColor = isScanningPlayer ? playerScanColor : normalScannerColor;
            float pulse = 1.0f + Mathf.Sin(Time.time * (isScanningPlayer ? 6.0f : 2.5f)) * 0.35f;

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
                lightGo.transform.localPosition = new Vector3(0f, -0.15f, 0.30f);
                lightGo.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            }
            else
            {
                lightGo.transform.localPosition = new Vector3(0f, 0.45f, 0.25f);
                lightGo.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            }

            scannerLight = lightGo.AddComponent<Light>();
            scannerLight.type = LightType.Spot;
            scannerLight.color = normalScannerColor;
            scannerLight.range = (movementType == DroneMovementType.HoveringDrone) ? 5.0f : 3.8f;
            scannerLight.spotAngle = 45f;
            scannerLight.intensity = 1.8f;
        }

        private void EnsureWorkToolEffects()
        {
            // 1. LineRenderer cho tia Laser / Hàn / Quét
            var beamChild = transform.Find("Work_Tool_Beam");
            GameObject beamGo = beamChild != null ? beamChild.gameObject : new GameObject("Work_Tool_Beam");
            beamGo.transform.SetParent(transform, false);

            _workBeamLine = beamGo.GetComponent<LineRenderer>();
            if (_workBeamLine == null) _workBeamLine = beamGo.AddComponent<LineRenderer>();

            _workBeamLine.useWorldSpace = true;
            _workBeamLine.positionCount = 2;
            _workBeamLine.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            _workBeamLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _workBeamLine.receiveShadows = false;
            _workBeamLine.enabled = false;

            // 2. PointLight rọi tại điểm tiếp xúc làm việc
            var lightChild = transform.Find("Work_Contact_Light");
            GameObject lightChildGo = lightChild != null ? lightChild.gameObject : new GameObject("Work_Contact_Light");
            lightChildGo.transform.SetParent(transform, false);

            _workToolLight = lightChildGo.GetComponent<Light>();
            if (_workToolLight == null) _workToolLight = lightChildGo.AddComponent<Light>();
            _workToolLight.type = LightType.Point;
            _workToolLight.intensity = 2.0f;
            _workToolLight.range = 3.0f;
            _workToolLight.enabled = false;
        }

        private void BuildAudioComponents()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1.0f;
            _audioSource.minDistance = 1.5f;
            _audioSource.maxDistance = 15.0f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.playOnAwake = false;

            // Kênh âm thanh làm việc liên tục (Looping Work Sound)
            var workSoundChild = transform.Find("Work_Audio_Source");
            GameObject workAudioGo = workSoundChild != null ? workSoundChild.gameObject : new GameObject("Work_Audio_Source");
            workAudioGo.transform.SetParent(transform, false);
            _workLoopSource = workAudioGo.GetComponent<AudioSource>();
            if (_workLoopSource == null) _workLoopSource = workAudioGo.AddComponent<AudioSource>();
            _workLoopSource.spatialBlend = 1.0f;
            _workLoopSource.minDistance = 1.2f;
            _workLoopSource.maxDistance = 12.0f;
            _workLoopSource.rolloffMode = AudioRolloffMode.Linear;
            _workLoopSource.loop = true;
            _workLoopSource.playOnAwake = false;

            // Kênh âm thanh rù động cơ nếu là Drone bay
            if (movementType == DroneMovementType.HoveringDrone)
            {
                var humChild = transform.Find("Hover_Hum_Audio");
                GameObject humGo = humChild != null ? humChild.gameObject : new GameObject("Hover_Hum_Audio");
                humGo.transform.SetParent(transform, false);

                _hoverHumSource = humGo.GetComponent<AudioSource>();
                if (_hoverHumSource == null) _hoverHumSource = humGo.AddComponent<AudioSource>();

                _hoverHumSource.spatialBlend = 1.0f;
                _hoverHumSource.minDistance = 1.0f;
                _hoverHumSource.maxDistance = 10.0f;
                _hoverHumSource.rolloffMode = AudioRolloffMode.Linear;
                _hoverHumSource.volume = 0.18f;
                _hoverHumSource.loop = true;

                _hoverHumClip = CreateHoverHumClip();
                _hoverHumSource.clip = _hoverHumClip;
            }

            _servoStepClip = CreateServoClip();
            _chirpClip = CreateChirpClip();
            _dataProcessClip = CreateDataProcessClip();
            _weldingSparkClip = CreateWeldingClip();
            _bioScanClip = CreateBioScanClip();
            _sonarPingClip = CreateSonarPingClip();
        }

        private void PlayServoStep()
        {
            if (_audioSource != null && _servoStepClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                _audioSource.PlayOneShot(_servoStepClip, 0.35f);
            }
        }

        private void PlayChirp()
        {
            if (_audioSource != null && _chirpClip != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.PlayOneShot(_chirpClip, 0.50f);
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
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + UnityEngine.Random.Range(-0.25f, 0.25f) * 0.3f) * decay;
            }
            var clip = AudioClip.Create("Servo_Step", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChirpClip()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;
                float env = Mathf.Sin(progress * Mathf.PI);
                float freq = (progress < 0.5f) ? 880f : 1320f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            var clip = AudioClip.Create("Scan_Chirp", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDataProcessClip()
        {
            // Tiếng gõ phím / giải mã dữ liệu cybernetic vi mô
            int sampleRate = 44100;
            float duration = 0.85f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float cycle = (t * 14f) % 1f; // Nhịp lách cách 14 lần/giây
                float env = Mathf.Exp(-cycle * 25f);
                float tone = Mathf.Sin(2f * Mathf.PI * ((i % 2 == 0) ? 2200f : 3100f) * t);
                float noise = UnityEngine.Random.Range(-0.15f, 0.15f);
                data[i] = (tone * 0.75f + noise * 0.25f) * env * 0.45f;
            }
            var clip = AudioClip.Create("Data_Process_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateWeldingClip()
        {
            // Tiếng hàn hồ quang điện tử vi mô (tiếng xèo lách tách tia lửa)
            int sampleRate = 44100;
            float duration = 1.0f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float buzz = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.35f;
                float crackle = UnityEngine.Random.Range(-0.45f, 0.45f);
                data[i] = (buzz + crackle) * 0.40f;
            }
            var clip = AudioClip.Create("Welding_Spark_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateBioScanClip()
        {
            // Tiếng scanner quang phổ sinh học êm dịu (hòa âm 528Hz + 792Hz)
            int sampleRate = 44100;
            float duration = 1.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;
                float env = Mathf.Sin(progress * Mathf.PI);
                float h1 = Mathf.Sin(2f * Mathf.PI * 528f * t) * 0.6f;
                float h2 = Mathf.Sin(2f * Mathf.PI * 792f * t) * 0.3f;
                data[i] = (h1 + h2) * env * 0.35f;
            }
            var clip = AudioClip.Create("Bio_Scan_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSonarPingClip()
        {
            // Tiếng sonar ping quét container an ninh
            int sampleRate = 44100;
            float duration = 1.0f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float cycle = (t * 2.5f) % 1f;
                float decay = Mathf.Exp(-cycle * 8f);
                float ping = Mathf.Sin(2f * Mathf.PI * 950f * t);
                data[i] = ping * decay * 0.40f;
            }
            var clip = AudioClip.Create("Sonar_Ping_Loop", samples, 1, sampleRate, false);
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
                float baseHum = Mathf.Sin(2f * Mathf.PI * 75f * t) * 0.65f;
                float harmonic = Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.25f;
                float air = UnityEngine.Random.Range(-0.05f, 0.05f);
                data[i] = (baseHum + harmonic + air) * 0.35f;
            }
            var clip = AudioClip.Create("Hover_Hum_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDrawGizmosSelected()
        {
            if (workstations == null || workstations.Count == 0) return;

            Gizmos.color = (movementType == DroneMovementType.HoveringDrone) ? Color.cyan : Color.yellow;
            for (int i = 0; i < workstations.Count; i++)
            {
                var ws = workstations[i];
                Gizmos.DrawWireSphere(ws.position, 0.25f);
                Gizmos.DrawRay(ws.position, ws.lookDirection.normalized * 0.8f);
                if (i < workstations.Count - 1)
                {
                    Gizmos.DrawLine(ws.position, workstations[i + 1].position);
                }
            }
        }
    }
}
