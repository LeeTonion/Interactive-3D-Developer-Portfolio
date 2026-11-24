using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Attaches directly onto the existing 3-Monitor Desk (desk.prefab) + Chair (decorative_chair) in Room01_Spaceship_Hangar.
    /// - Mounts 3 crisp, clickable WorldSpace UI screens directly onto the desk's Left, Center, and Right monitors.
    /// - Press [E] near the chair to smoothly sit down (chair slides in + camera frames all 3 monitors cleanly).
    /// - ZERO grey OnGUI boxes, ZERO duplicate vertical console panels.
    /// </summary>
    public class HangarBlastDoorTerminal : MonoBehaviour
    {
        [Header("References")]
        public BlastDoorController blastDoor;
        public Transform existingChair;
        public float interactRadius = 4.0f;
        public KeyCode interactKey = KeyCode.E;

        [Header("Seating & Camera Animation")]
        public float sitTransitionDuration = 0.55f;
        public float seatedFov = 65f;
        public float mouseParallaxDegrees = 2.4f;

        // Runtime UI references across all 3 desk monitors
        private TextMeshProUGUI _leftChecklistText;
        private Image _leftPowerBar;
        private Image _leftFuelBar;
        private Image _leftTempBar;

        private TextMeshProUGUI _centerStatusText;
        private Image _centerDoorBar;
        private TextMeshProUGUI _centerOpenBtnLabel;
        private Image _centerOpenBtnImg;
        private TextMeshProUGUI _centerFfBtnLabel;
        private TextMeshProUGUI _centerLogText;

        private TextMeshProUGUI _rightSpeedText;
        private TextMeshProUGUI _rightClampBtnLabel;

        private Light _statusLight;
        private Transform _player;
        private CharacterController _playerCc;
        private Camera _cam;

        // Seated state machine
        private enum SeatState { Standing, SittingDown, Seated, StandingUp }
        private SeatState _seatState = SeatState.Standing;
        private float _seatAnimT;
        private Vector3 _savedPlayerPos;
        private Quaternion _savedPlayerRot;
        private Vector3 _camStartPos;
        private Quaternion _camStartRot;
        private float _savedFov = 60f;
        private Vector3 _chairIdleWorldPos;
        private Vector3 _chairSeatedWorldPos;

        private bool _inRange;
        private float _scanPulse;
        private string _logMessage = "HỆ THỐNG SẴN SÀNG // BẤM CHỌN TRỰC TIẾP TRÊN 3 MÀN HÌNH";

        private AudioSource _audioSource;
        private AudioClip _beepClip;
        private AudioClip _confirmClip;
        private AudioClip _alarmClip;
        private AudioClip _chairSlideClip;

        private static readonly Color ColCyan = new Color(0.14f, 0.92f, 1f);
        private static readonly Color ColGreen = new Color(0.20f, 1f, 0.52f);
        private static readonly Color ColAmber = new Color(1f, 0.72f, 0.14f);
        private static readonly Color ColRed = new Color(1f, 0.25f, 0.22f);
        private static readonly Color ColBg = new Color(0.02f, 0.06f, 0.11f, 0.96f);
        private static readonly Color ColBtn = new Color(0.06f, 0.18f, 0.28f, 0.96f);
        private static readonly Color ColBtnDanger = new Color(0.32f, 0.08f, 0.08f, 0.96f);
        private static readonly Color ColBtnReady = new Color(0.08f, 0.36f, 0.20f, 0.96f);

        private void Awake()
        {
            EnsureAudio();
            EnsureEventSystem();
        }

        private void Start()
        {
            if (blastDoor == null)
                blastDoor = FindFirstObjectByType<BlastDoorController>();

            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null)
            {
                _player = pc.transform;
                _playerCc = pc.GetComponent<CharacterController>();
            }
            _cam = Camera.main;

            EnsureVisualRig();
            CacheChairPositions();
            AssignWorldCameras();
        }

        private void OnDisable()
        {
            if (_seatState != SeatState.Standing)
                ForceStandImmediate();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private void EnsureAudio()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0.65f;
            _audioSource.playOnAwake = false;

            _beepClip = MakeTone(840f, 0.055f, 0.22f);
            _confirmClip = MakeDualTone(620f, 980f, 0.22f, 0.26f);
            _alarmClip = MakeDualTone(460f, 320f, 0.30f, 0.25f);
            _chairSlideClip = MakeTone(190f, 0.24f, 0.18f);
        }

        private void CacheChairPositions()
        {
            if (existingChair != null)
            {
                _chairIdleWorldPos = existingChair.position;
                // Slide chair 0.38m closer toward the desk when seated
                Vector3 toDesk = (transform.position - existingChair.position);
                toDesk.y = 0f;
                Vector3 dir = toDesk.sqrMagnitude > 0.01f ? toDesk.normalized : -transform.forward;
                _chairSeatedWorldPos = _chairIdleWorldPos + dir * 0.38f;
            }
            else
            {
                _chairIdleWorldPos = transform.TransformPoint(new Vector3(0f, 0f, 1.45f));
                _chairSeatedWorldPos = transform.TransformPoint(new Vector3(0f, 0f, 1.10f));
            }
        }

        public void EnsureVisualRig()
        {
            Transform oldRig = transform.Find("Desk_3Monitor_UI_Root");
            if (oldRig != null)
            {
                if (_centerStatusText != null) return;
                if (Application.isPlaying) Destroy(oldRig.gameObject);
                else DestroyImmediate(oldRig.gameObject);
            }

            GameObject root = new GameObject("Desk_3Monitor_UI_Root");
            root.transform.SetParent(transform, false);

            // Exact hand-calibrated transforms fitting desk.prefab's 3 real monitor bezels
            // 1. LEFT MONITOR — Pre-Flight Checklist & Telemetry
            BuildLeftMonitorCanvas(
                root.transform,
                new Vector3(1.299004f, 1.646999f, 1.027998f),
                Quaternion.Euler(29.771f, -215.975f, -0.188f),
                0.00169f);

            // 2. CENTER MONITOR — Blast Door Command & Launch
            BuildCenterMonitorCanvas(
                root.transform,
                new Vector3(0.008f, 1.684f, 0.64f),
                Quaternion.Euler(22.53f, 180.767f, 0.934f),
                0.00213f);

            // 3. RIGHT MONITOR — Hangar Machinery & Stand Up
            BuildRightMonitorCanvas(
                root.transform,
                new Vector3(-1.34f, 1.616f, 0.938f),
                Quaternion.Euler(21.898f, 208.709f, -0.874f),
                0.00169f);

            // Soft desk glow light
            GameObject ltGo = new GameObject("Desk_MonitorGlow");
            ltGo.transform.SetParent(root.transform, false);
            ltGo.transform.localPosition = new Vector3(0.008f, 1.95f, 1.15f);
            _statusLight = ltGo.AddComponent<Light>();
            _statusLight.type = LightType.Point;
            _statusLight.range = 3.8f;
            _statusLight.intensity = 1.8f;
            _statusLight.color = ColCyan;

            AssignWorldCameras();
        }

        private Canvas CreateMonitorCanvas(Transform parent, string name, Vector3 localPos, Quaternion localRot, float scale, out Transform panelRoot)
        {
            GameObject cGo = new GameObject(name);
            cGo.transform.SetParent(parent, false);
            cGo.transform.localPosition = localPos;
            cGo.transform.localRotation = localRot;
            cGo.transform.localScale = Vector3.one * scale;

            Canvas canvas = cGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            cGo.AddComponent<CanvasScaler>();
            var raycaster = cGo.AddComponent<GraphicRaycaster>();
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(580f, 380f);

            GameObject borderGo = new GameObject("FrameBorder");
            borderGo.transform.SetParent(cGo.transform, false);
            Image bImg = borderGo.AddComponent<Image>();
            bImg.color = new Color(0.14f, 0.92f, 1f, 0.65f);
            borderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 380f);

            GameObject bgGo = new GameObject("ScreenBG");
            bgGo.transform.SetParent(cGo.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = ColBg;
            bgGo.GetComponent<RectTransform>().sizeDelta = new Vector2(568f, 368f);

            panelRoot = bgGo.transform;
            return canvas;
        }

        private void BuildLeftMonitorCanvas(Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            CreateMonitorCanvas(parent, "Canvas_Left_Telemetry", pos, rot, scale, out Transform root);

            CreateTMP(root, "Title", "SYS // CHECKLIST CẤT CÁNH", 24, ColCyan, new Vector2(0f, 152f), new Vector2(540f, 36f), TextAlignmentOptions.Center);

            _leftChecklistText = CreateTMP(root, "ChecklistBody",
                "[  ] 1. ĐIỆN LƯỚI: 50%\n[  ] 2. NHIÊN LIỆU: 25%\n[  ] 3. KẸP BÃI ĐÁP: KHÓA\n[  ] 4. QUÉT THÂN VỎ: CHƯA\n[OK] 5. NHIỆT ĐỘ: 42°C",
                18, ColAmber, new Vector2(0f, 66f), new Vector2(520f, 130f), TextAlignmentOptions.Left);

            _leftPowerBar = CreateLabeledBar(root, "ĐIỆN", new Vector2(0f, -18f), ColCyan);
            _leftFuelBar = CreateLabeledBar(root, "XĂNG", new Vector2(0f, -50f), ColGreen);
            _leftTempBar = CreateLabeledBar(root, "NHIỆT", new Vector2(0f, -82f), ColAmber);

            CreateButton(root, "Btn_ScanShip", "QUÉT TÀU & GỌI DRONE", new Vector2(-130f, -142f), new Vector2(250f, 56f), ColBtn, ColCyan, () =>
            {
                if (HangarSimulationManager.Instance != null)
                    HangarSimulationManager.Instance.StartFullShipScan();
                PlayClip(_confirmClip);
                _logMessage = "ĐÃ LỆNH 2 DRONE & ROBOT QUÉT KIỂM TRA THÂN VỎ!";
            });

            CreateButton(root, "Btn_Coolant", "XẢ NITƠ LÀM MÁT", new Vector2(130f, -142f), new Vector2(250f, 56f), ColBtn, ColGreen, () =>
            {
                if (HangarSimulationManager.Instance != null)
                    HangarSimulationManager.Instance.PurgeCoolant(32f);
                PlayClip(_beepClip);
                _logMessage = "ĐÃ XẢ NITƠ LÀM MÁT TOÀN BỘ KHOANG XƯỞNG!";
            });
        }

        private void BuildCenterMonitorCanvas(Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            CreateMonitorCanvas(parent, "Canvas_Center_BlastDoor", pos, rot, scale, out Transform root);

            CreateTMP(root, "Title", "BAY 01 // ĐIỀU KHIỂN CỬA LỚN", 25, ColCyan, new Vector2(0f, 152f), new Vector2(540f, 36f), TextAlignmentOptions.Center);

            _centerStatusText = CreateTMP(root, "DoorStatus", "CỬA ĐANG ĐÓNG // ÁP SUẤT ỔN ĐỊNH (0%)", 20, ColAmber, new Vector2(0f, 110f), new Vector2(530f, 34f), TextAlignmentOptions.Center);

            _centerDoorBar = CreateLabeledBar(root, "CỬA", new Vector2(0f, 72f), ColCyan);

            var openBtn = CreateButton(root, "Btn_LaunchOpen", "MỞ CỬA LỚN\n(CẦN ĐỦ 5 BƯỚC)", new Vector2(-132f, 4f), new Vector2(254f, 68f), ColBtn, Color.white, () =>
            {
                var mgr = HangarSimulationManager.Instance;
                if (mgr != null && mgr.IsLaunchReady)
                {
                    if (blastDoor != null) blastDoor.OpenDoors();
                    PlayClip(_alarmClip);
                    _logMessage = "ĐỦ ĐIỀU KIỆN CẤT CÁNH! ĐANG RÚT CHỐT & MỞ CỬA LỚN!";
                }
                else
                {
                    PlayClip(_beepClip);
                    _logMessage = "CHƯA ĐỦ 5 BƯỚC CHECKLIST! HÃY HOÀN TẤT HOẶC DÙNG OVERRIDE.";
                }
            });
            _centerOpenBtnImg = openBtn.GetComponent<Image>();
            _centerOpenBtnLabel = openBtn.GetComponentInChildren<TextMeshProUGUI>();

            CreateButton(root, "Btn_SealDoor", "ĐÓNG & NIÊM PHONG\nCỬA LỚN", new Vector2(132f, 4f), new Vector2(254f, 68f), ColBtn, ColCyan, () =>
            {
                if (blastDoor != null) blastDoor.CloseDoors();
                PlayClip(_confirmClip);
                _logMessage = "ĐANG ĐÓNG CỬA LỚN & KHÓA CHỐT THỦY LỰC!";
            });

            CreateButton(root, "Btn_Override", "MỞ KHẨN CẤP (OVERRIDE)", new Vector2(-132f, -72f), new Vector2(254f, 56f), ColBtnDanger, ColAmber, () =>
            {
                if (blastDoor != null) blastDoor.OpenDoors();
                PlayClip(_alarmClip);
                _logMessage = "OVERRIDE: ĐANG CƯỠNG CHẾ MỞ CỬA HANGAR!";
            });

            var ffBtn = CreateButton(root, "Btn_Forcefield", "BẬT / TẮT MÀN CHẮN", new Vector2(132f, -72f), new Vector2(254f, 56f), ColBtn, ColCyan, () =>
            {
                if (blastDoor != null) blastDoor.ToggleForcefieldManual();
                PlayClip(_beepClip);
                _logMessage = "ĐÃ CHUYỂN TRẠNG THÁI MÀN CHẮN NĂNG LƯỢNG!";
            });
            _centerFfBtnLabel = ffBtn.GetComponentInChildren<TextMeshProUGUI>();

            GameObject logBg = new GameObject("LogBox");
            logBg.transform.SetParent(root, false);
            Image logImg = logBg.AddComponent<Image>();
            logImg.color = new Color(0.03f, 0.10f, 0.16f, 0.95f);
            RectTransform logRt = logBg.GetComponent<RectTransform>();
            logRt.anchoredPosition = new Vector2(0f, -142f);
            logRt.sizeDelta = new Vector2(524f, 56f);

            _centerLogText = CreateTMP(logBg.transform, "LogText", _logMessage, 16, ColGreen, Vector2.zero, new Vector2(506f, 50f), TextAlignmentOptions.Center);
        }

        private void BuildRightMonitorCanvas(Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            CreateMonitorCanvas(parent, "Canvas_Right_Machinery", pos, rot, scale, out Transform root);

            CreateTMP(root, "Title", "OPS // ĐIỀU KHIỂN MÁY MÓC", 24, ColCyan, new Vector2(0f, 152f), new Vector2(540f, 36f), TextAlignmentOptions.Center);

            _rightSpeedText = CreateTMP(root, "SpeedStatus", "DÂY CHUYỀN: 1.0x (TIÊU CHUẨN)", 19, ColGreen, new Vector2(0f, 108f), new Vector2(530f, 32f), TextAlignmentOptions.Center);

            CreateButton(root, "Btn_Pause", "TẠM DỪNG\n0.0x", new Vector2(-174f, 44f), new Vector2(164f, 64f), ColBtn, ColAmber, () =>
            {
                SetHangarSpeed(0f, false);
                _logMessage = "ĐÃ TẠM DỪNG TOÀN BỘ MÁY MÓC TRONG HANGAR.";
            });
            CreateButton(root, "Btn_Normal", "TIÊU CHUẨN\n1.0x", new Vector2(0f, 44f), new Vector2(164f, 64f), ColBtn, ColCyan, () =>
            {
                SetHangarSpeed(1f, true);
                _logMessage = "XƯỞNG BẢO TRÌ ĐANG VẬN HÀNH Ở MỨC 1.0x.";
            });
            CreateButton(root, "Btn_Overdrive", "OVERDRIVE\n2.0x", new Vector2(174f, 44f), new Vector2(164f, 64f), ColBtnDanger, ColRed, () =>
            {
                SetHangarSpeed(2f, true);
                _logMessage = "OVERDRIVE 2.0x! LƯU Ý NHIỆT ĐỘ XƯỞNG ĐANG TĂNG!";
            });

            var clampBtn = CreateButton(root, "Btn_Clamps", "MỞ / KHÓA KẸP BÃI ĐÁP", new Vector2(-132f, -38f), new Vector2(254f, 60f), ColBtn, ColCyan, () =>
            {
                if (HangarSimulationManager.Instance != null)
                    HangarSimulationManager.Instance.ToggleLandingClamps();
                PlayClip(_confirmClip);
                _logMessage = "ĐÃ CHUYỂN TRẠNG THÁI 4 KẸP THỦY LỰC BÃI ĐÁP!";
            });
            _rightClampBtnLabel = clampBtn.GetComponentInChildren<TextMeshProUGUI>();

            CreateButton(root, "Btn_RobotWeld", "ROBOT HÀN BẢO TRÌ", new Vector2(132f, -38f), new Vector2(254f, 60f), ColBtn, ColAmber, () =>
            {
                var arms = FindObjectsByType<RoboticArmController>(FindObjectsSortMode.None);
                foreach (var arm in arms) arm.TriggerWeldNow(2.8f);
                PlayClip(_confirmClip);
                _logMessage = $"ĐÃ LỆNH {arms.Length} CÁNH TAY ROBOT HÀN BẢO TRÌ NGAY!";
            });

            CreateButton(root, "Btn_StandUp", "ĐỨNG DẬY / RỜI GHẾ  [E / ESC]", new Vector2(0f, -134f), new Vector2(516f, 64f), new Color(0.16f, 0.24f, 0.34f, 0.98f), Color.white, () =>
            {
                BeginStandUp();
            });
        }

        private Image CreateLabeledBar(Transform parent, string label, Vector2 pos, Color fillCol)
        {
            CreateTMP(parent, "Lbl_" + label, label, 15, Color.white, new Vector2(-225f, pos.y), new Vector2(75f, 24f), TextAlignmentOptions.Left);

            GameObject track = new GameObject("Track_" + label);
            track.transform.SetParent(parent, false);
            Image trImg = track.AddComponent<Image>();
            trImg.color = new Color(0.06f, 0.14f, 0.22f, 1f);
            RectTransform trRt = track.GetComponent<RectTransform>();
            trRt.anchoredPosition = new Vector2(40f, pos.y);
            trRt.sizeDelta = new Vector2(435f, 18f);

            GameObject fill = new GameObject("Fill_" + label);
            fill.transform.SetParent(track.transform, false);
            Image fImg = fill.AddComponent<Image>();
            fImg.color = fillCol;
            fImg.type = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillAmount = 0.5f;
            RectTransform fRt = fill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            return fImg;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgCol, Color txtCol, Action onClick)
        {
            GameObject bGo = new GameObject(name);
            bGo.transform.SetParent(parent, false);
            Image img = bGo.AddComponent<Image>();
            img.color = bgCol;
            RectTransform rt = bGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Button btn = bGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.35f, 1.45f, 1f);
            cb.pressedColor = new Color(0.75f, 0.9f, 1f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());

            CreateTMP(bGo.transform, "Label", label, 17, txtCol, Vector2.zero, size - new Vector2(10f, 6f), TextAlignmentOptions.Center);
            return btn;
        }

        private void AssignWorldCameras()
        {
            Camera c = Camera.main;
            if (c == null) return;
            foreach (var canvas in GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                    canvas.worldCamera = c;
            }
        }

        private void Update()
        {
            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerCharacterController>();
                if (pc != null)
                {
                    _player = pc.transform;
                    _playerCc = pc.GetComponent<CharacterController>();
                }
            }
            if (_cam == null) _cam = Camera.main;
            if (blastDoor == null) blastDoor = FindFirstObjectByType<BlastDoorController>();

            float dt = Time.deltaTime;
            _scanPulse += dt;

            Vector3 chairPos = existingChair != null ? existingChair.position : _chairIdleWorldPos;
            float dist = 999f;
            if (_player != null && _player.gameObject.activeInHierarchy)
                dist = Vector3.Distance(_player.position, chairPos);
            _inRange = (_seatState == SeatState.Standing) && (dist <= interactRadius);

            if (_inRange && Input.GetKeyDown(interactKey))
            {
                BeginSitDown();
            }
            else if (_seatState == SeatState.Seated && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey)))
            {
                BeginStandUp();
            }

            UpdateSeatAnimation(dt);
            UpdateAllThreeMonitors();
        }

        private void BeginSitDown()
        {
            if (_seatState != SeatState.Standing || _player == null || _cam == null) return;

            CacheChairPositions();
            _savedPlayerPos = _player.position;
            _savedPlayerRot = _player.rotation;
            _camStartPos = _cam.transform.position;
            _camStartRot = _cam.transform.rotation;
            _savedFov = _cam.fieldOfView;

            if (_playerCc != null) _playerCc.enabled = false;
            PlayerCharacterController.AnyUiPanelOpen = true;
            PlayerCharacterController.ExternalCameraOverride = true;

            _seatState = SeatState.SittingDown;
            _seatAnimT = 0f;
            PlayClip(_chairSlideClip);
        }

        private void BeginStandUp()
        {
            if (_seatState != SeatState.Seated && _seatState != SeatState.SittingDown) return;

            if (_cam != null)
            {
                _camStartPos = _cam.transform.position;
                _camStartRot = _cam.transform.rotation;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _seatState = SeatState.StandingUp;
            _seatAnimT = 0f;
            PlayClip(_chairSlideClip);
        }

        private void ForceStandImmediate()
        {
            _seatState = SeatState.Standing;
            PlayerCharacterController.AnyUiPanelOpen = false;
            PlayerCharacterController.ExternalCameraOverride = false;
            if (_player != null)
            {
                _player.position = _savedPlayerPos;
                _player.rotation = _savedPlayerRot;
            }
            if (_playerCc != null) _playerCc.enabled = true;
            if (_cam != null) _cam.fieldOfView = _savedFov;
            if (existingChair != null) existingChair.position = _chairIdleWorldPos;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UpdateSeatAnimation(float dt)
        {
            if (_seatState == SeatState.Standing || _cam == null) return;

            // Pull camera back slightly behind the chair so ALL 3 monitors (Left + Center + Right) fit cleanly in frame
            Vector3 toDeskFlat = transform.position - _chairSeatedWorldPos;
            toDeskFlat.y = 0f;
            Vector3 toDeskDir = toDeskFlat.sqrMagnitude > 0.01f ? toDeskFlat.normalized : -transform.forward;

            Vector3 seatedEyeWorld = _chairSeatedWorldPos - toDeskDir * 0.65f + Vector3.up * 1.72f;
            Vector3 monitorCenterWorld = transform.TransformPoint(new Vector3(0.008f, 1.65f, 0.72f));
            Quaternion seatedLookRot = Quaternion.LookRotation((monitorCenterWorld - seatedEyeWorld).normalized, Vector3.up);

            if (_seatState == SeatState.SittingDown)
            {
                _seatAnimT = Mathf.MoveTowards(_seatAnimT, 1f, dt / Mathf.Max(0.15f, sitTransitionDuration));
                float eased = Mathf.SmoothStep(0f, 1f, _seatAnimT);

                float sitDip = Mathf.Sin(eased * Mathf.PI) * -0.14f;
                _cam.transform.position = Vector3.Lerp(_camStartPos, seatedEyeWorld, eased) + Vector3.up * sitDip;
                _cam.transform.rotation = Quaternion.Slerp(_camStartRot, seatedLookRot, eased);
                _cam.fieldOfView = Mathf.Lerp(_savedFov, seatedFov, eased);

                if (existingChair != null)
                    existingChair.position = Vector3.Lerp(_chairIdleWorldPos, _chairSeatedWorldPos, eased);
                if (_player != null)
                    _player.position = Vector3.Lerp(_savedPlayerPos, _chairSeatedWorldPos, eased);

                if (_seatAnimT >= 1f)
                {
                    _seatState = SeatState.Seated;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    AssignWorldCameras();
                    PlayClip(_confirmClip);
                }
            }
            else if (_seatState == SeatState.Seated)
            {
                float nx = Mathf.Clamp((Input.mousePosition.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f, -1f, 1f);
                float ny = Mathf.Clamp((Input.mousePosition.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f, -1f, 1f);
                float breathY = Mathf.Sin(_scanPulse * 1.6f) * 0.007f;

                Quaternion parallaxRot = seatedLookRot * Quaternion.Euler(-ny * mouseParallaxDegrees, nx * mouseParallaxDegrees * 1.4f, 0f);
                _cam.transform.position = Vector3.Lerp(_cam.transform.position, seatedEyeWorld + Vector3.up * breathY, dt * 8f);
                _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, parallaxRot, dt * 8f);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, seatedFov, dt * 8f);
            }
            else if (_seatState == SeatState.StandingUp)
            {
                _seatAnimT = Mathf.MoveTowards(_seatAnimT, 1f, dt / Mathf.Max(0.15f, sitTransitionDuration * 0.85f));
                float eased = Mathf.SmoothStep(0f, 1f, _seatAnimT);

                Vector3 standEyeWorld = _savedPlayerPos + Vector3.up * 1.62f;
                _cam.transform.position = Vector3.Lerp(_camStartPos, standEyeWorld, eased);
                _cam.transform.rotation = Quaternion.Slerp(_camStartRot, _savedPlayerRot, eased);
                _cam.fieldOfView = Mathf.Lerp(seatedFov, _savedFov, eased);

                if (existingChair != null)
                    existingChair.position = Vector3.Lerp(_chairSeatedWorldPos, _chairIdleWorldPos, eased);
                if (_player != null)
                    _player.position = Vector3.Lerp(_chairSeatedWorldPos, _savedPlayerPos, eased);

                if (_seatAnimT >= 1f)
                {
                    _seatState = SeatState.Standing;
                    if (_player != null)
                    {
                        _player.position = _savedPlayerPos;
                        _player.rotation = _savedPlayerRot;
                    }
                    if (_playerCc != null) _playerCc.enabled = true;
                    PlayerCharacterController.AnyUiPanelOpen = false;
                    PlayerCharacterController.ExternalCameraOverride = false;
                }
            }
        }

        private void UpdateAllThreeMonitors()
        {
            var mgr = HangarSimulationManager.Instance;
            int pwr = mgr != null ? Mathf.RoundToInt(mgr.powerGridPercent) : 100;
            int fuel = mgr != null ? Mathf.RoundToInt(mgr.fuelPercent) : 100;
            int temp = mgr != null ? Mathf.RoundToInt(mgr.coreTemperatureC) : 40;
            bool pwrOk = pwr >= 75;
            bool fuelOk = fuel >= 99;
            bool clampOk = mgr != null && mgr.clampsReleased;
            bool scanOk = mgr != null && mgr.hullScanComplete;
            bool tempOk = mgr == null || !mgr.IsOverheating;
            bool ready = mgr != null && mgr.IsLaunchReady;

            if (_leftChecklistText != null)
            {
                _leftChecklistText.text =
                    $"[{(pwrOk ? "OK" : "  ")}] 1. ĐIỆN LƯỚI: {pwr}%\n" +
                    $"[{(fuelOk ? "OK" : "  ")}] 2. NHIÊN LIỆU: {fuel}%\n" +
                    $"[{(clampOk ? "OK" : "  ")}] 3. KẸP BÃI ĐÁP: {(clampOk ? "ĐÃ MỞ" : "ĐANG KHÓA")}\n" +
                    $"[{(scanOk ? "OK" : "  ")}] 4. QUÉT THÂN VỎ: {(scanOk ? "HOÀN TẤT" : (mgr != null && mgr.isHullScanning ? Mathf.RoundToInt(mgr.hullScanProgress) + "%" : "CHƯA"))}\n" +
                    $"[{(tempOk ? "OK" : "!!")}] 5. NHIỆT ĐỘ: {temp}°C {(tempOk ? "" : "(QUÁ NHIỆT!)")}";
                _leftChecklistText.color = ready ? ColGreen : (tempOk ? ColAmber : ColRed);
            }
            if (_leftPowerBar != null) _leftPowerBar.fillAmount = pwr / 100f;
            if (_leftFuelBar != null) _leftFuelBar.fillAmount = fuel / 100f;
            if (_leftTempBar != null)
            {
                _leftTempBar.fillAmount = Mathf.Clamp01((temp - 20f) / 100f);
                _leftTempBar.color = tempOk ? ColAmber : ColRed;
            }

            if (blastDoor != null)
            {
                float prog = blastDoor.Progress01;
                bool moving = blastDoor.IsTransitioning;
                bool open = blastDoor.IsTargetOpen;

                if (_centerStatusText != null)
                {
                    _centerStatusText.text = $"{blastDoor.StatusLabel} ({Mathf.RoundToInt(prog * 100f)}%)";
                    _centerStatusText.color = moving ? ColRed : (open ? ColGreen : ColAmber);
                }
                if (_centerDoorBar != null)
                {
                    _centerDoorBar.fillAmount = prog;
                    _centerDoorBar.color = moving ? ColAmber : (open ? ColGreen : ColCyan);
                }
                if (_centerOpenBtnLabel != null)
                    _centerOpenBtnLabel.text = ready ? "KHỞI ĐỘNG CẤT CÁNH\n& MỞ CỬA LỚN" : "MỞ CỬA LỚN\n(CẦN ĐỦ 5 BƯỚC)";
                if (_centerOpenBtnImg != null)
                    _centerOpenBtnImg.color = ready ? ColBtnReady : ColBtn;
                if (_centerFfBtnLabel != null)
                    _centerFfBtnLabel.text = blastDoor.IsForcefieldActive ? "TẮT MÀN CHẮN" : "BẬT MÀN CHẮN";
                if (_centerLogText != null)
                    _centerLogText.text = _logMessage;

                if (_statusLight != null)
                {
                    _statusLight.color = moving ? ColRed : (ready ? ColGreen : ColCyan);
                    _statusLight.intensity = moving
                        ? (1.5f + 1.8f * Mathf.Abs(Mathf.Sin(_scanPulse * 8f)))
                        : (1.7f + 0.35f * Mathf.Sin(_scanPulse * 2.4f));
                }
            }

            if (_rightSpeedText != null && mgr != null)
            {
                string mode = !mgr.simulationActive ? "TẠM DỪNG" : (mgr.globalSpeed > 1.3f ? "OVERDRIVE" : "TIÊU CHUẨN");
                _rightSpeedText.text = $"DÂY CHUYỀN: {mgr.globalSpeed:0.0}x ({mode})";
                _rightSpeedText.color = !mgr.simulationActive ? ColAmber : (mgr.globalSpeed > 1.3f ? ColRed : ColGreen);
            }
            if (_rightClampBtnLabel != null && mgr != null)
            {
                _rightClampBtnLabel.text = mgr.clampsReleased ? "KHÓA KẸP BÃI ĐÁP" : "MỞ KẸP BÃI ĐÁP";
            }
        }

        private void SetHangarSpeed(float spd, bool active)
        {
            if (HangarSimulationManager.Instance != null)
            {
                HangarSimulationManager.Instance.simulationActive = active;
                HangarSimulationManager.Instance.globalSpeed = spd;
            }
            PlayClip(_beepClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
                _audioSource.PlayOneShot(clip);
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0.01f) return;

            if (_inRange && _seatState == SeatState.Standing)
            {
                Vector3 chairAnchor = (existingChair != null ? existingChair.position : _chairIdleWorldPos) + Vector3.up * 1.05f;
                WorldInteractPromptUI.DrawAtWorldPos(chairAnchor, "E", "NGỒI VÀO BÀN ĐIỀU KHIỂN");
            }
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }

        private static AudioClip MakeTone(float freq, float duration, float vol)
        {
            int sr = 44100;
            int n = Mathf.Max(64, (int)(sr * duration));
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (float)i / n) * vol;
            }
            AudioClip clip = AudioClip.Create("TermTone", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeDualTone(float f1, float f2, float duration, float vol)
        {
            int sr = 44100;
            int n = Mathf.Max(64, (int)(sr * duration));
            float[] data = new float[n];
            int half = n / 2;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float f = (i < half) ? f1 : f2;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * (1f - (float)i / n) * vol;
            }
            AudioClip clip = AudioClip.Create("TermDualTone", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
