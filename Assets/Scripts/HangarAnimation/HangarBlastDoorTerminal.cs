using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Redesigned AAA Diegetic 3-Monitor Hangar Control Station mounted onto desk.prefab + decorative_chair.
    /// Separates presentation from gameplay via event-driven subscriptions to HangarSimulationManager & BlastDoorController.
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

        // === Left Screen (Canvas_Left_Telemetry) ===
        private readonly HangarStatusIndicator[] _checkDots = new HangarStatusIndicator[5];
        private readonly TextMeshProUGUI[] _checkValues = new TextMeshProUGUI[5];
        private HangarAnimatedBar _powerBar;
        private HangarAnimatedBar _fuelBar;
        private HangarAnimatedBar _tempBar;

        // === Center Screen (Canvas_Center_BlastDoor) ===
        private HangarStatusIndicator _doorStatusDot;
        private TextMeshProUGUI _centerStatusText;
        private TextMeshProUGUI _centerLockText;
        private HangarAnimatedBar _centerDoorBar;
        private HangarSciFiButton _btnLaunchOpen;
        private HangarSciFiButton _btnForcefield;
        private GameObject _overrideBanner;
        private TextMeshProUGUI _centerLogText;
        private readonly List<string> _logEntries = new List<string>(8);

        // === Right Screen (Canvas_Right_Machinery) ===
        private HangarStatusIndicator _opsStatusDot;
        private TextMeshProUGUI _rightSpeedText;
        private HangarSciFiButton _btnSpeedPause;
        private HangarSciFiButton _btnSpeedNormal;
        private HangarSciFiButton _btnSpeedOverdrive;
        private HangarSciFiButton _btnClamps;

        // === Entrance & Diegetic Rig ===
        private readonly List<CanvasGroup> _screenGroups = new List<CanvasGroup>(3);
        private Light _statusLight;
        private Transform _player;
        private CharacterController _playerCc;
        private Camera _cam;

        // === Seated State Machine ===
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
        private bool _overrideActive;
        private bool _eventsBound;

        private HangarSimulationManager _boundManager;
        private BlastDoorController _boundDoor;

        private AudioSource _audioSource;
        private AudioClip _beepClip;
        private AudioClip _confirmClip;
        private AudioClip _alarmClip;
        private AudioClip _chairSlideClip;

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
            BindEvents();

            AppendSystemLog("SYSTEM", "Trạm điều khiển Hangar Bay 01 đã sẵn sàng.");
            SyncAllUI();
        }

        private void OnEnable()
        {
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
            if (_seatState != SeatState.Standing)
                ForceStandImmediate();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (_eventsBound) return;

            _boundManager = HangarSimulationManager.Instance;
            if (_boundManager != null)
            {
                _boundManager.OnEngineeringStateChanged += SyncAllUI;
                _boundManager.OnSystemLog += HandleSystemLog;
            }

            _boundDoor = blastDoor != null ? blastDoor : FindFirstObjectByType<BlastDoorController>();
            if (_boundDoor != null)
            {
                _boundDoor.OnDoorStateChanged += SyncAllUI;
                _boundDoor.OnDoorLogEvent += HandleSystemLog;
            }

            _eventsBound = (_boundManager != null || _boundDoor != null);
        }

        private void UnbindEvents()
        {
            if (_boundManager != null)
            {
                _boundManager.OnEngineeringStateChanged -= SyncAllUI;
                _boundManager.OnSystemLog -= HandleSystemLog;
                _boundManager = null;
            }
            if (_boundDoor != null)
            {
                _boundDoor.OnDoorStateChanged -= SyncAllUI;
                _boundDoor.OnDoorLogEvent -= HandleSystemLog;
                _boundDoor = null;
            }
            _eventsBound = false;
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

            _beepClip = MakeTone(860f, 0.05f, 0.20f);
            _confirmClip = MakeDualTone(640f, 1020f, 0.20f, 0.24f);
            _alarmClip = MakeDualTone(460f, 320f, 0.28f, 0.24f);
            _chairSlideClip = MakeTone(190f, 0.22f, 0.16f);
        }

        private void CacheChairPositions()
        {
            if (existingChair != null)
            {
                _chairIdleWorldPos = existingChair.position;
                Vector3 toDesk = transform.position - existingChair.position;
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

            _screenGroups.Clear();

            GameObject root = new GameObject("Desk_3Monitor_UI_Root");
            root.transform.SetParent(transform, false);

            // Exact calibrated transforms fitting desk.prefab's 3 real monitor bezels
            BuildLeftMonitorCanvas(
                root.transform,
                new Vector3(1.299004f, 1.646999f, 1.027998f),
                Quaternion.Euler(29.771f, -215.975f, -0.188f),
                0.00169f);

            BuildCenterMonitorCanvas(
                root.transform,
                new Vector3(0.008f, 1.684f, 0.64f),
                Quaternion.Euler(22.53f, 180.767f, 0.934f),
                0.00213f);

            BuildRightMonitorCanvas(
                root.transform,
                new Vector3(-1.34f, 1.616f, 0.938f),
                Quaternion.Euler(21.898f, 208.709f, -0.874f),
                0.00169f);

            GameObject ltGo = new GameObject("Desk_MonitorGlow");
            ltGo.transform.SetParent(root.transform, false);
            ltGo.transform.localPosition = new Vector3(0.008f, 1.95f, 1.15f);
            _statusLight = ltGo.AddComponent<Light>();
            _statusLight.type = LightType.Point;
            _statusLight.range = 3.8f;
            _statusLight.intensity = 1.6f;
            _statusLight.color = HangarUITheme.NeonCyan;

            AssignWorldCameras();
        }

        private Canvas CreateMonitorCanvas(Transform parent, string name, Vector3 localPos, Quaternion localRot, float scale, out Transform contentRoot)
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

            CanvasGroup cg = cGo.AddComponent<CanvasGroup>();
            cg.alpha = 0.88f;
            _screenGroups.Add(cg);

            // Glassmorphism dark metallic background + glowing cyan border
            GameObject frame = HangarUITheme.CreateGlassPanel(
                cGo.transform, "GlassFrame", Vector2.zero, new Vector2(580f, 380f),
                HangarUITheme.BgDeepNavy, new Color(HangarUITheme.NeonCyan.r, HangarUITheme.NeonCyan.g, HangarUITheme.NeonCyan.b, 0.72f));

            // Subtle mask + sweeping holographic scanline
            GameObject scanMask = new GameObject("ScanSweep");
            scanMask.transform.SetParent(frame.transform, false);
            RectTransform smRt = scanMask.AddComponent<RectTransform>();
            smRt.sizeDelta = new Vector2(560f, 34f);
            Image smImg = scanMask.AddComponent<Image>();
            smImg.sprite = HangarUITheme.GetScanlineSprite();
            smImg.raycastTarget = false;
            scanMask.AddComponent<HangarScanlineSweep>().Configure(165f, 44f);

            contentRoot = frame.transform;
            return canvas;
        }

        private void BuildHeaderBar(Transform root, string title, string badgeText)
        {
            GameObject hdr = HangarUITheme.CreateGlassPanel(
                root, "HeaderBar", new Vector2(0f, 162f), new Vector2(552f, 36f),
                HangarUITheme.BgHeaderStrip, new Color(HangarUITheme.NeonCyan.r, HangarUITheme.NeonCyan.g, HangarUITheme.NeonCyan.b, 0.45f));

            HangarUITheme.CreateTMP(hdr.transform, "Title", title, 18f, HangarUITheme.NeonCyan, new Vector2(-60f, 0f), new Vector2(400f, 30f), TextAlignmentOptions.Left);
            HangarUITheme.CreateTMP(hdr.transform, "Badge", badgeText, 12f, HangarUITheme.TextMuted, new Vector2(205f, 0f), new Vector2(120f, 26f), TextAlignmentOptions.Right);
        }

        // =====================================================================
        // 1. LEFT SCREEN — Canvas_Left_Telemetry
        // =====================================================================
        private void BuildLeftMonitorCanvas(Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            CreateMonitorCanvas(parent, "Canvas_Left_Telemetry", pos, rot, scale, out Transform root);
            BuildHeaderBar(root, "SYS // CHECKLIST CẤT CÁNH", "TELEMETRY v6");

            // 5 Live Checklist Rows inside a clean glass panel
            GameObject listPanel = HangarUITheme.CreateGlassPanel(
                root, "ChecklistPanel", new Vector2(0f, 66f), new Vector2(552f, 144f),
                HangarUITheme.BgGlassPanel, new Color(HangarUITheme.ElectricBlue.r, HangarUITheme.ElectricBlue.g, HangarUITheme.ElectricBlue.b, 0.35f));

            string[] rowNames =
            {
                "1. ĐIỆN LƯỚI (YÊU CẦU ≥ 75%)",
                "2. NHIÊN LIỆU (YÊU CẦU ≥ 99%)",
                "3. KẸP BÃI ĐÁP (4 KẸP THỦY LỰC)",
                "4. QUÉT THÂN VỎ (DRONE & ROBOT)",
                "5. NHIỆT ĐỘ XƯỞNG (AN TOÀN < 85°C)"
            };

            for (int i = 0; i < 5; i++)
            {
                float y = 50f - i * 25f;
                _checkDots[i] = HangarUITheme.CreateStatusDot(listPanel.transform, $"Dot_{i}", new Vector2(-252f, y), 10f);
                HangarUITheme.CreateTMP(listPanel.transform, $"Lbl_{i}", rowNames[i], 13.5f, HangarUITheme.TextPrimary, new Vector2(-65f, y), new Vector2(345f, 22f), TextAlignmentOptions.Left);
                _checkValues[i] = HangarUITheme.CreateTMP(listPanel.transform, $"Val_{i}", "--", 13.5f, HangarUITheme.StatusAmber, new Vector2(182f, y), new Vector2(150f, 22f), TextAlignmentOptions.Right);
            }

            // 3 Animated Status Bars
            GameObject barsPanel = HangarUITheme.CreateGlassPanel(
                root, "BarsPanel", new Vector2(0f, -52f), new Vector2(552f, 78f),
                HangarUITheme.BgGlassPanel, new Color(HangarUITheme.NeonCyan.r, HangarUITheme.NeonCyan.g, HangarUITheme.NeonCyan.b, 0.25f));

            _powerBar = HangarUITheme.CreateAnimatedBar(barsPanel.transform, "Bar_Power", "ĐIỆN", new Vector2(0f, 23f), 524f, HangarUITheme.NeonCyan);
            _fuelBar = HangarUITheme.CreateAnimatedBar(barsPanel.transform, "Bar_Fuel", "XĂNG", new Vector2(0f, 0f), 524f, HangarUITheme.StatusGreen);
            _tempBar = HangarUITheme.CreateAnimatedBar(barsPanel.transform, "Bar_Temp", "NHIỆT", new Vector2(0f, -23f), 524f, HangarUITheme.StatusAmber);

            // 2 Functional Buttons
            HangarUITheme.CreateSciFiButton(
                root, "Btn_ScanShip", "QUÉT TÀU & GỌI DRONE",
                new Vector2(-140f, -134f), new Vector2(268f, 60f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.NeonCyan, HangarUITheme.TextPrimary,
                () =>
                {
                    if (HangarSimulationManager.Instance != null)
                        HangarSimulationManager.Instance.StartFullShipScan();
                    PlayClip(_confirmClip);
                }, 14.5f);

            HangarUITheme.CreateSciFiButton(
                root, "Btn_Coolant", "XẢ NITƠ LÀM MÁT",
                new Vector2(140f, -134f), new Vector2(268f, 60f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.StatusGreen, HangarUITheme.StatusGreen,
                () =>
                {
                    if (HangarSimulationManager.Instance != null)
                        HangarSimulationManager.Instance.PurgeCoolant(32f);
                    PlayClip(_beepClip);
                }, 14.5f);
        }

        // =====================================================================
        // 2. CENTER SCREEN — Canvas_Center_BlastDoor
        // =====================================================================
        private void BuildCenterMonitorCanvas(Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            CreateMonitorCanvas(parent, "Canvas_Center_BlastDoor", pos, rot, scale, out Transform root);
            BuildHeaderBar(root, "BAY 01 // ĐIỀU KHIỂN CỬA LỚN", "PRIMARY GATE");

            // Large Door Status & Hydraulic Lock Card
            GameObject statusCard = HangarUITheme.CreateGlassPanel(
                root, "DoorStatusCard", new Vector2(0f, 102f), new Vector2(552f, 70f),
                HangarUITheme.BgGlassPanel, new Color(HangarUITheme.NeonCyan.r, HangarUITheme.NeonCyan.g, HangarUITheme.NeonCyan.b, 0.45f));

            _doorStatusDot = HangarUITheme.CreateStatusDot(statusCard.transform, "DoorDot", new Vector2(-252f, 16f), 12f);
            _centerStatusText = HangarUITheme.CreateTMP(statusCard.transform, "DoorState", "CỬA ĐANG ĐÓNG", 18f, HangarUITheme.StatusAmber, new Vector2(-42f, 16f), new Vector2(390f, 26f), TextAlignmentOptions.Left);
            _centerLockText = HangarUITheme.CreateTMP(statusCard.transform, "LockState", "CHỐT THỦY LỰC: ĐANG KHÓA CHẶT", 12.5f, HangarUITheme.TextMuted, new Vector2(150f, 16f), new Vector2(230f, 22f), TextAlignmentOptions.Right);

            _centerDoorBar = HangarUITheme.CreateAnimatedBar(statusCard.transform, "Bar_Door", "TIẾN ĐỘ", new Vector2(0f, -16f), 524f, HangarUITheme.NeonCyan);

            // Emergency Override Banner (shown when override is used)
            _overrideBanner = HangarUITheme.CreateGlassPanel(
                root, "OverrideBanner", new Vector2(0f, 58f), new Vector2(552f, 20f),
                HangarUITheme.BtnDanger, HangarUITheme.StatusRed);
            HangarUITheme.CreateTMP(_overrideBanner.transform, "WarnTxt", "CẢNH BÁO KHẨN CẤP: ĐÃ KÍCH HOẠT MỞ CỬA OVERRIDE!", 11.5f, HangarUITheme.StatusAmber, Vector2.zero, new Vector2(540f, 18f), TextAlignmentOptions.Center);
            _overrideBanner.SetActive(false);

            // 4 Large Interactive Buttons
            _btnLaunchOpen = HangarUITheme.CreateSciFiButton(
                root, "Btn_LaunchOpen", "MỞ CỬA LỚN\n(CHƯA ĐỦ 5 ĐIỀU KIỆN)",
                new Vector2(-140f, 18f), new Vector2(268f, 54f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.NeonCyan, HangarUITheme.TextPrimary,
                () =>
                {
                    var mgr = HangarSimulationManager.Instance;
                    if (mgr != null && mgr.IsLaunchReady && blastDoor != null)
                    {
                        _overrideActive = false;
                        blastDoor.OpenDoors();
                        PlayClip(_alarmClip);
                        AppendSystemLog("HANGAR", "Đủ 5 điều kiện — khởi động cất cánh & mở cửa lớn!");
                        SyncAllUI();
                    }
                    else
                    {
                        PlayClip(_beepClip);
                    }
                }, 13.5f);

            HangarUITheme.CreateSciFiButton(
                root, "Btn_SealDoor", "ĐÓNG & NIÊM PHONG CỬA LỚN",
                new Vector2(140f, 18f), new Vector2(268f, 54f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.ElectricBlue, HangarUITheme.TextPrimary,
                () =>
                {
                    _overrideActive = false;
                    if (blastDoor != null) blastDoor.CloseDoors();
                    PlayClip(_confirmClip);
                    SyncAllUI();
                }, 13.5f);

            HangarUITheme.CreateSciFiButton(
                root, "Btn_Override", "MỞ KHẨN CẤP (OVERRIDE)",
                new Vector2(-140f, -42f), new Vector2(268f, 50f),
                HangarUITheme.BtnDanger, HangarUITheme.BtnDangerHover, HangarUITheme.StatusRed, HangarUITheme.StatusAmber,
                () =>
                {
                    _overrideActive = true;
                    if (blastDoor != null) blastDoor.OpenDoors();
                    PlayClip(_alarmClip);
                    AppendSystemLog("OVERRIDE", "CẢNH BÁO: Cưỡng chế mở cửa lớn khẩn cấp!");
                    SyncAllUI();
                }, 13.5f);

            _btnForcefield = HangarUITheme.CreateSciFiButton(
                root, "Btn_Forcefield", "BẬT / TẮT MÀN CHẮN: OFF",
                new Vector2(140f, -42f), new Vector2(268f, 50f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.NeonCyan, HangarUITheme.NeonCyan,
                () =>
                {
                    if (blastDoor != null) blastDoor.ToggleForcefieldManual();
                    PlayClip(_beepClip);
                    SyncAllUI();
                }, 13.5f);

            // Live Scrolling System Log Panel
            GameObject logPanel = HangarUITheme.CreateGlassPanel(
                root, "SystemLogPanel", new Vector2(0f, -124f), new Vector2(552f, 94f),
                new Color(0.015f, 0.035f, 0.065f, 0.94f), new Color(HangarUITheme.ElectricBlue.r, HangarUITheme.ElectricBlue.g, HangarUITheme.ElectricBlue.b, 0.40f));

            HangarUITheme.CreateTMP(logPanel.transform, "LogHdr", "NHẬT KÝ HỆ THỐNG (LIVE SYSTEM LOG)", 11.5f, HangarUITheme.NeonCyan, new Vector2(0f, 34f), new Vector2(532f, 18f), TextAlignmentOptions.Left);
            _centerLogText = HangarUITheme.CreateTMP(logPanel.transform, "LogBody", "", 12f, HangarUITheme.TextPrimary, new Vector2(0f, -10f), new Vector2(532f, 68f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        }

        // =====================================================================
        // 3. RIGHT SCREEN — Canvas_Right_Machinery
        // =====================================================================
        private void BuildRightMonitorCanvas(Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            CreateMonitorCanvas(parent, "Canvas_Right_Machinery", pos, rot, scale, out Transform root);
            BuildHeaderBar(root, "OPS // ĐIỀU KHIỂN MÁY MÓC", "MACHINERY");

            // Current Speed Status Card
            GameObject spdCard = HangarUITheme.CreateGlassPanel(
                root, "SpeedCard", new Vector2(0f, 114f), new Vector2(552f, 44f),
                HangarUITheme.BgGlassPanel, new Color(HangarUITheme.NeonCyan.r, HangarUITheme.NeonCyan.g, HangarUITheme.NeonCyan.b, 0.35f));

            _opsStatusDot = HangarUITheme.CreateStatusDot(spdCard.transform, "OpsDot", new Vector2(-252f, 0f), 11f);
            _rightSpeedText = HangarUITheme.CreateTMP(spdCard.transform, "SpeedStatus", "TỐC ĐỘ DÂY CHUYỀN: 1.0x (TIÊU CHUẨN)", 16f, HangarUITheme.StatusGreen, new Vector2(10f, 0f), new Vector2(490f, 28f), TextAlignmentOptions.Left);

            // 3 Speed Selection Buttons
            _btnSpeedPause = HangarUITheme.CreateSciFiButton(
                root, "Btn_Pause", "TẠM DỪNG\n0.0x",
                new Vector2(-184f, 46f), new Vector2(174f, 66f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnSelected, HangarUITheme.StatusAmber, HangarUITheme.StatusAmber,
                () => SetHangarSpeed(0f, false), 14.5f);

            _btnSpeedNormal = HangarUITheme.CreateSciFiButton(
                root, "Btn_Normal", "TIÊU CHUẨN\n1.0x",
                new Vector2(0f, 46f), new Vector2(174f, 66f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnSelected, HangarUITheme.NeonCyan, HangarUITheme.TextPrimary,
                () => SetHangarSpeed(1f, true), 14.5f);

            _btnSpeedOverdrive = HangarUITheme.CreateSciFiButton(
                root, "Btn_Overdrive", "OVERDRIVE\n2.0x",
                new Vector2(184f, 46f), new Vector2(174f, 66f),
                HangarUITheme.BtnDanger, HangarUITheme.BtnDangerHover, HangarUITheme.StatusRed, HangarUITheme.StatusRed,
                () => SetHangarSpeed(2f, true), 14.5f);

            // 2 Large Mechanical Control Buttons
            _btnClamps = HangarUITheme.CreateSciFiButton(
                root, "Btn_Clamps", "MỞ / KHÓA KẸP BÃI ĐÁP\n(ĐANG KHÓA)",
                new Vector2(-140f, -40f), new Vector2(268f, 70f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.NeonCyan, HangarUITheme.TextPrimary,
                () =>
                {
                    if (HangarSimulationManager.Instance != null)
                        HangarSimulationManager.Instance.ToggleLandingClamps();
                    PlayClip(_confirmClip);
                }, 14f);

            HangarUITheme.CreateSciFiButton(
                root, "Btn_RobotWeld", "ROBOT HÀN BẢO TRÌ\n(KÍCH HOẠT 2 TAY ROBOT)",
                new Vector2(140f, -40f), new Vector2(268f, 70f),
                HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.StatusAmber, HangarUITheme.StatusAmber,
                () =>
                {
                    var arms = FindObjectsByType<RoboticArmController>(FindObjectsSortMode.None);
                    foreach (var arm in arms) arm.TriggerWeldNow(2.8f);
                    if (HangarSimulationManager.Instance != null)
                        HangarSimulationManager.Instance.EmitLog("OPS", $"Đã kích hoạt {arms.Length} tay robot hàn bảo trì.");
                    PlayClip(_confirmClip);
                }, 14f);

            // Prominent Exit Button
            HangarUITheme.CreateSciFiButton(
                root, "Btn_StandUp", "ĐỨNG DẬY / RỜI GHẾ  [E / ESC]",
                new Vector2(0f, -132f), new Vector2(552f, 64f),
                new Color(0.11f, 0.20f, 0.30f, 0.96f), new Color(0.18f, 0.32f, 0.46f, 1f), HangarUITheme.TextPrimary, HangarUITheme.TextPrimary,
                BeginStandUp, 16f);
        }

        // =====================================================================
        // EVENT-DRIVEN UI SYNCHRONIZATION
        // =====================================================================
        private void HandleSystemLog(string tag, string message)
        {
            AppendSystemLog(tag, message);
        }

        private void AppendSystemLog(string tag, string message)
        {
            string colorHex = tag switch
            {
                "WARNING" or "OVERRIDE" => "#FF5252",
                "DRONE" => "#38E8FF",
                "HANGAR" => "#3BFF96",
                "OPS" => "#FFC13B",
                _ => "#8CE6FF"
            };

            string line = $"<color={colorHex}>[{tag}]</color> {message}";
            _logEntries.Add(line);
            if (_logEntries.Count > 4)
                _logEntries.RemoveAt(0);

            if (_centerLogText != null)
                _centerLogText.text = string.Join("\n", _logEntries);
        }

        private void SyncAllUI()
        {
            var mgr = HangarSimulationManager.Instance;
            float pwr = mgr != null ? mgr.powerGridPercent : 100f;
            float fuel = mgr != null ? mgr.fuelPercent : 100f;
            float temp = mgr != null ? mgr.coreTemperatureC : 40f;
            bool pwrOk = pwr >= 75f;
            bool fuelOk = fuel >= 99f;
            bool clampOk = mgr != null && mgr.clampsReleased;
            bool scanOk = mgr != null && mgr.hullScanComplete;
            bool scanning = mgr != null && mgr.isHullScanning;
            bool tempOk = mgr == null || !mgr.IsOverheating;
            bool ready = mgr != null && mgr.IsLaunchReady;

            // --- Left Screen ---
            int cableCount = mgr != null ? mgr.ReleasedCableCount : 4;
            UpdateCheckRow(0, pwrOk, false, $"{Mathf.RoundToInt(pwr)}%");
            UpdateCheckRow(1, fuelOk, false, $"{Mathf.RoundToInt(fuel)}%");
            UpdateCheckRow(2, clampOk, false, clampOk ? "ĐÃ MỞ (4/4)" : $"ĐANG KHÓA ({cableCount}/4)");
            UpdateCheckRow(3, scanOk, scanning, scanOk ? "HOÀN TẤT" : (scanning ? $"SCANNING {Mathf.RoundToInt(mgr.hullScanProgress)}%" : "CHƯA"));
            UpdateCheckRow(4, tempOk, !tempOk, $"{Mathf.RoundToInt(temp)}°C" + (tempOk ? "" : " (!)") );

            if (_powerBar != null)
                _powerBar.SetProgress(pwr / 100f, $"{Mathf.RoundToInt(pwr)}%", pwrOk ? HangarUITheme.NeonCyan : HangarUITheme.StatusAmber);
            if (_fuelBar != null)
                _fuelBar.SetProgress(fuel / 100f, $"{Mathf.RoundToInt(fuel)}%", HangarUITheme.StatusGreen);
            if (_tempBar != null)
            {
                float tempNorm = Mathf.Clamp01((temp - 20f) / 100f);
                Color tempCol = temp >= 85f ? HangarUITheme.StatusRed : Color.Lerp(HangarUITheme.StatusAmber, HangarUITheme.StatusRed, Mathf.Clamp01((temp - 65f) / 20f));
                _tempBar.SetProgress(tempNorm, $"{Mathf.RoundToInt(temp)}°C", tempCol);
            }

            // --- Center Screen ---
            if (blastDoor != null)
            {
                float prog = blastDoor.Progress01;
                bool moving = blastDoor.IsTransitioning;
                bool open = blastDoor.IsTargetOpen;

                if (_doorStatusDot != null)
                    _doorStatusDot.SetState(moving ? HangarUITheme.StatusAmber : (open ? HangarUITheme.StatusGreen : HangarUITheme.NeonCyan), moving ? 3.5f : 0f);

                if (_centerStatusText != null)
                {
                    _centerStatusText.text = blastDoor.StatusLabel;
                    _centerStatusText.color = moving ? HangarUITheme.StatusAmber : (open ? HangarUITheme.StatusGreen : HangarUITheme.NeonCyan);
                }
                if (_centerLockText != null)
                    _centerLockText.text = $"CHỐT THỦY LỰC: {blastDoor.HydraulicLockStatusLabel}";

                if (_centerDoorBar != null)
                    _centerDoorBar.SetProgress(prog, $"{Mathf.RoundToInt(prog * 100f)}%", moving ? HangarUITheme.StatusAmber : (open ? HangarUITheme.StatusGreen : HangarUITheme.NeonCyan));

                if (_overrideBanner != null)
                    _overrideBanner.SetActive(_overrideActive && (open || moving));

                if (_btnLaunchOpen != null)
                {
                    _btnLaunchOpen.SetInteractable(ready);
                    if (ready)
                    {
                        _btnLaunchOpen.SetPalette(HangarUITheme.BtnGreen, HangarUITheme.BtnGreenHover, HangarUITheme.StatusGreen, HangarUITheme.TextPrimary);
                        _btnLaunchOpen.SetText("KHỞI ĐỘNG CẤT CÁNH\n& MỞ CỬA LỚN");
                    }
                    else
                    {
                        _btnLaunchOpen.SetPalette(HangarUITheme.BtnNormal, HangarUITheme.BtnHover, HangarUITheme.NeonCyan, HangarUITheme.TextPrimary);
                        _btnLaunchOpen.SetText("MỞ CỬA LỚN\n(CẦN ĐỦ 5 ĐIỀU KIỆN)");
                    }
                }

                if (_btnForcefield != null)
                {
                    bool ffOn = blastDoor.IsForcefieldActive;
                    _btnForcefield.SetText(ffOn ? "MÀN CHẮN: ON\n(BẤM ĐỂ TẮT)" : "MÀN CHẮN: OFF\n(BẤM ĐỂ BẬT)");
                    _btnForcefield.SetSelected(ffOn);
                }

                if (_statusLight != null)
                {
                    _statusLight.color = _overrideActive ? HangarUITheme.StatusRed : (ready ? HangarUITheme.StatusGreen : HangarUITheme.NeonCyan);
                    _statusLight.intensity = moving ? 2.4f : 1.6f;
                }
            }

            // --- Right Screen ---
            if (mgr != null)
            {
                bool active = mgr.simulationActive;
                bool isPause = !active || mgr.globalSpeed <= 0.05f;
                bool isOverdrive = active && mgr.globalSpeed >= 1.5f;
                bool isNormal = active && !isPause && !isOverdrive;

                if (_opsStatusDot != null)
                    _opsStatusDot.SetState(isPause ? HangarUITheme.StatusAmber : (isOverdrive ? HangarUITheme.StatusRed : HangarUITheme.StatusGreen), isOverdrive ? 3f : 0f);

                if (_rightSpeedText != null)
                {
                    string mode = isPause ? "TẠM DỪNG" : (isOverdrive ? "OVERDRIVE" : "TIÊU CHUẨN");
                    _rightSpeedText.text = $"TỐC ĐỘ DÂY CHUYỀN: {(active ? mgr.globalSpeed : 0f):0.0}x ({mode})";
                    _rightSpeedText.color = isPause ? HangarUITheme.StatusAmber : (isOverdrive ? HangarUITheme.StatusRed : HangarUITheme.StatusGreen);
                }

                if (_btnSpeedPause != null) _btnSpeedPause.SetSelected(isPause);
                if (_btnSpeedNormal != null) _btnSpeedNormal.SetSelected(isNormal);
                if (_btnSpeedOverdrive != null) _btnSpeedOverdrive.SetSelected(isOverdrive);

                if (_btnClamps != null)
                {
                    _btnClamps.SetText(clampOk ? "MỞ / KHÓA KẸP BÃI ĐÁP\n(TRẠNG THÁI: ĐÃ MỞ)" : "MỞ / KHÓA KẸP BÃI ĐÁP\n(TRẠNG THÁI: ĐANG KHÓA)");
                    _btnClamps.SetSelected(clampOk);
                }
            }
        }

        private void UpdateCheckRow(int idx, bool ok, bool blinkWarning, string text)
        {
            Color col = ok ? HangarUITheme.StatusGreen : (blinkWarning ? HangarUITheme.StatusRed : HangarUITheme.StatusAmber);
            if (_checkDots[idx] != null)
                _checkDots[idx].SetState(col, blinkWarning ? 3.5f : 0f);
            if (_checkValues[idx] != null)
            {
                _checkValues[idx].text = text;
                _checkValues[idx].color = col;
            }
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
            if (!_eventsBound) BindEvents();

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
            SyncAllUI();
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
            SetScreenGroupAlpha(0.88f);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetScreenGroupAlpha(float alpha)
        {
            for (int i = 0; i < _screenGroups.Count; i++)
            {
                if (_screenGroups[i] != null)
                    _screenGroups[i].alpha = alpha;
            }
        }

        private void UpdateSeatAnimation(float dt)
        {
            if (_seatState == SeatState.Standing || _cam == null) return;

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

                SetScreenGroupAlpha(Mathf.Lerp(0.72f, 1f, eased));

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
                    SetScreenGroupAlpha(1f);
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

                SetScreenGroupAlpha(Mathf.Lerp(1f, 0.88f, eased));

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

        private void SetHangarSpeed(float spd, bool active)
        {
            if (HangarSimulationManager.Instance != null)
                HangarSimulationManager.Instance.SetSimulationSpeed(spd, active);
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
