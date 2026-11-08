using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Trạm thông tin PROJECTS tương tác (phong cách Sci-Fi Cyberpunk Kiosk).
    /// Tính năng & Hoạt họa (Animations):
    /// - Khi tàu đáp xuống bãi đáp, camera tự động bay mượt tới trước trạm.
    /// - Hiệu ứng sọc ngang màn hình CRT (Scrolling Scanlines) + tia quét Laser (Laser Scan Beam) chạy dọc màn hình.
    /// - Hiệu ứng giải mã chữ Cyberpunk (Scramble Decode Title) + gõ chữ máy tính (Typewriter + con trỏ nhấp nháy █).
    /// - Các màn hình Hologram vệ tinh lơ lửng nhấp nhô (Anti-Gravity Floating Bobbing) & tự xoay hướng về Camera.
    /// - Khối cầu Gyroscope Hologram 3D đa vòng xoay 3 trục trên bàn máy tính trung tâm, đổi màu theo từng Project.
    /// - Biểu tượng Hologram dưới sàn xoay tròn & radar trên đảo tự động quay.
    /// </summary>
    public class AboutInfoStation : MonoBehaviour
    {
        [Serializable]
        public class Slide
        {
            public string title = "PROJECT TITLE";
            public string linkLabel = "github.com/LeeTonion";
            public string url = "https://github.com/LeeTonion";
            [TextArea(3, 8)] public string body = "";
            [Tooltip("Tên ngắn hiển thị trên màn hình cánh PREV / NEXT")]
            public string shortLabel = "PROJECT";
            public string role = "CREATIVE DEV";
            public string with = "CODEDRIVE STUDIO";
            [TextArea(2, 4)] public string distinctions = "• Featured 3D Showcase\n• High-Performance URP";
            [Tooltip("Ảnh minh hoạ của Project")]
            public Texture2D image;
            public Color accent = new Color(0.25f, 0.9f, 1f);
        }

        [Header("Nội dung các Dự án (Projects)")]
        public Slide[] slides = new Slide[0];
        [TextArea(2, 6)] public string achievements = "• Unity Certified\n• Game Jam Winner\n• 20+ 3D Projects";

        [Header("Tàu & Camera (tự tìm nếu để trống)")]
        public PlayerSpaceshipController ship;
        public SpaceshipCameraFollow cameraFollow;
        public Camera viewCamera;

        [Header("Vùng đáp tàu kích hoạt")]
        public Transform landingPadCenter;
        public float landingRadius = 9.5f;
        public float landedMaxSpeed = 3f;
        public float landedHoldTime = 0.8f;

        [Header("Góc nhìn camera khi mở trạm")]
        public Transform cameraViewPoint;
        public Transform cameraLookTarget;
        public float viewFov = 48f;
        public float transitionTime = 1.3f;

        [Header("Tham chiếu hiển thị (tự tìm theo tên con nếu để trống)")]
        public TMP_Text titleText;
        public TMP_Text linkText;
        public TMP_Text bodyText;
        public TMP_Text prevLabelText;
        public TMP_Text nextLabelText;
        public TMP_Text roleText;
        public TMP_Text withText;
        public TMP_Text pageText;
        public TMP_Text achievementsText;
        public TMP_Text promptText;
        public Renderer imageRenderer;
        public Renderer scanlineRenderer;
        public Renderer[] allScanlineRenderers;
        public Transform laserScanBar;
        public Renderer[] pageDots;
        public Collider prevButton;
        public Collider nextButton;
        public Collider openButton;
        public Collider prevWingCollider;
        public Collider nextWingCollider;
        public Renderer[] accentRenderers;
        public Light[] accentLights;
        [Tooltip("Các biển phụ luôn tự động xoay hướng mặt chữ về phía Camera")]
        public Transform[] cameraFacingSigns;

        [Header("Hoạt họa 3D (Sci-Fi Animations)")]
        [Tooltip("Các màn hình hologram lơ lửng nhấp nhô")]
        public Transform[] floatingMonitors;
        [Tooltip("Các vòng xoay Gyroscope Hologram trên bàn trung tâm")]
        public Transform[] gyroRings;
        [Tooltip("Biểu tượng Hologram dưới sàn xoay tròn")]
        public Transform floorEmblem;
        [Tooltip("Các đĩa radar / máy chiếu sao trên đảo quay tự động")]
        public Transform[] islandRotators;
        [Tooltip("Các thiên thạch nhỏ trôi nổi quanh đảo")]
        public Transform[] floatingRocks;
        [Tooltip("Đèn dẫn đường xung quanh bãi đáp chữ H chạy hiệu ứng sóng đuổi")]
        public Renderer[] landingBeacons;

        private enum State { Idle, Entering, Viewing, Exiting }
        private State _state = State.Idle;
        private int _index;
        private float _landedTimer;
        private bool _armed = true;
        private float _t;
        private Vector3 _fromPos, _toPos;
        private Quaternion _fromRot, _toRot;
        private float _fromFov, _toFov;
        private float _savedFov;
        private Rigidbody _shipRb;
        private bool _savedKinematic;
        private Coroutine _activeCinematicRoutine;
        private GameObject _cinematicSkipHUD;
        private float _contentAnim = 1f;
        private MaterialPropertyBlock _mpb;
        private Texture2D _fallbackImage;
        private Transform _hovered;
        private Vector3 _hoveredBaseScale;
        private float[] _floatingBaseY;
        private float[] _rockBaseY;
        private string _targetTitle = "";
        private string _targetBody = "";
        private const string CyberChars = "01ABCDEF#<>/*+=-_[]";

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public static bool AnyStationActive { get; private set; }
        public bool IsViewing => _state != State.Idle;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (ship == null) ship = FindFirstObjectByType<PlayerSpaceshipController>();
            if (cameraFollow == null) cameraFollow = FindFirstObjectByType<SpaceshipCameraFollow>();
            if (viewCamera == null) viewCamera = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;
            if (ship != null) _shipRb = ship.GetComponent<Rigidbody>();

            titleText = titleText ? titleText : FindText("Screen_Title");
            linkText = linkText ? linkText : FindText("Screen_Link");
            bodyText = bodyText ? bodyText : FindText("Screen_Body");
            prevLabelText = prevLabelText ? prevLabelText : FindText("Sign_Prev_Label");
            nextLabelText = nextLabelText ? nextLabelText : FindText("Sign_Next_Label");
            roleText = roleText ? roleText : FindText("Role_Value");
            withText = withText ? withText : FindText("With_Value");
            pageText = pageText ? pageText : FindText("Screen_Page");
            achievementsText = achievementsText ? achievementsText : FindText("Achievements_Text");
            promptText = promptText ? promptText : FindText("Landing_Prompt");

            if (floatingMonitors != null && floatingMonitors.Length > 0)
            {
                _floatingBaseY = new float[floatingMonitors.Length];
                for (int i = 0; i < floatingMonitors.Length; i++)
                    if (floatingMonitors[i] != null) _floatingBaseY[i] = floatingMonitors[i].localPosition.y;
            }
            if (floatingRocks != null && floatingRocks.Length > 0)
            {
                _rockBaseY = new float[floatingRocks.Length];
                for (int i = 0; i < floatingRocks.Length; i++)
                    if (floatingRocks[i] != null) _rockBaseY[i] = floatingRocks[i].localPosition.y;
            }

            if (slides == null || slides.Length == 0) slides = DefaultSlides();
            if (imageRenderer != null && imageRenderer.sharedMaterial != null)
                _fallbackImage = imageRenderer.sharedMaterial.GetTexture(BaseMapId) as Texture2D;
            if (_fallbackImage == null) _fallbackImage = BuildFallbackImage();

            ShowSlide(0, true);
            SetPrompt(false, "");
        }

        private TMP_Text FindText(string childName)
        {
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
                if (t.name == childName) return t;
            return null;
        }

        private void OnDisable()
        {
            if (_state != State.Idle) AnyStationActive = false;
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;

            switch (_state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.Entering: 
                    if (_activeCinematicRoutine == null) UpdateTransition(State.Viewing); 
                    break;
                case State.Viewing: UpdateViewing(); break;
                case State.Exiting: 
                    if (_activeCinematicRoutine == null) UpdateTransition(State.Idle); 
                    break;
            }
            AnyStationActive = _state != State.Idle;
            AnimateVisuals();
        }

        private bool _isLandingSequence;

        // ───────────────────────── Idle: phát hiện đáp tàu ─────────────────────────
        private void UpdateIdle()
        {
            if (ship == null || landingPadCenter == null || _isLandingSequence) return;

            Vector3 d = ship.transform.position - landingPadCenter.position;
            float flatDist = new Vector2(d.x, d.z).magnitude;
            bool inZone = flatDist < landingRadius && d.y < 8f && d.y > -1f;
            float speed = _shipRb != null ? _shipRb.linearVelocity.magnitude : 0f;

            if (!inZone)
            {
                _landedTimer = 0f;
                if (flatDist > landingRadius + 2f) _armed = true;
                SetPrompt(flatDist < landingRadius * 2.5f, "HẠ CÁNH XUỐNG BÃI ĐÁP\nĐỂ XEM PROJECTS");
                return;
            }

            // KHI VÀO CĂN CỨ (BÃI ĐÁP PROJECT): TẮT NGAY TRẠNG THÁI BỊ TẤN CÔNG
            if (SpaceTrafficShipAI.IsFleetHostile)
            {
                SpaceTrafficShipAI.ClearFleetHostileAlert();
            }

            if (_armed && speed < landedMaxSpeed && ship.IsHovering)
            {
                _landedTimer += Time.deltaTime;
                SetPrompt(true, "ĐANG ĐÁP XUỐNG BÃI...");
                if (_landedTimer >= 0.4f)
                {
                    StartCoroutine(PerformLandingAndConnectingRoutine());
                }
            }
            else
            {
                _landedTimer = 0f;
                SetPrompt(true, _armed ? "GIẢM TỐC ĐỂ ĐÁP" : "NHẤN [F] ĐỂ MỞ TRẠM PROJECTS");
                if (Input.GetKeyDown(KeyCode.F))
                {
                    StartCoroutine(PerformLandingAndConnectingRoutine());
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // REWORKED CINEMATIC SPACESHIP LANDING & DATA UPLINK SEQUENCE
        // Strict adherence to Sci-Fi Visual Limits:
        // - NO oversized discs, NO solid cylinders, NO giant floating text.
        // - Thin wireframe rings, sleek realistic mechanical clamps, localized sparks.
        // - Single compact Top-Right Screen-Space HUD panel (<20% width), camera view 100% clear.
        // ─────────────────────────────────────────────────────────────────────────────

        private readonly List<GameObject> _activeDockingClamps = new List<GameObject>();
        private GameObject _activeDockingHUD;

        private System.Collections.IEnumerator PerformLandingAndConnectingRoutine()
        {
            if (_isLandingSequence) yield break;
            _isLandingSequence = true;
            _armed = false;
            _landedTimer = 0f;

            // 1. Tắt ngay báo động hạm đội địch
            SpaceTrafficShipAI.ClearFleetHostileAlert();

            // 2. Khóa điều khiển & tắt va chạm vật lý để đáp êm ái
            if (ship != null)
            {
                ship.enabled = false;
                if (_shipRb != null)
                {
                    _savedKinematic = _shipRb.isKinematic;
                    _shipRb.linearVelocity = Vector3.zero;
                    _shipRb.angularVelocity = Vector3.zero;
                    _shipRb.isKinematic = true;
                }
            }

            Vector3 padPos = landingPadCenter != null ? landingPadCenter.position : transform.position;
            Vector3 startShipPos = ship != null ? ship.transform.position : padPos;
            
            // KHÔNG ÉP TÀU VỀ TÂM BÃI ĐÁP: Tàu đỗ ở đâu trong bãi thì hạ cánh ngay tại vị trí X, Z đó!
            Vector3 targetShipPos = new Vector3(startShipPos.x, padPos.y + 0.72f, startShipPos.z);
            Quaternion startRot = ship != null ? ship.transform.rotation : Quaternion.identity;
            // Cân bằng góc nghiêng pitch/roll phẳng với mặt sàn, giữ nguyên hướng xoay Y của người chơi
            Quaternion targetRot = Quaternion.Euler(0f, startRot.eulerAngles.y, 0f);

            // Khởi tạo Compact HUD panel ở góc trên bên phải màn hình (Top-Right Screen Space)
            RectTransform hudFillBar;
            TMP_Text hudStatusText;
            _activeDockingHUD = CreateDockingHUDPanel(out hudStatusText, out hudFillBar);

            // ═════════════════════════════════════════════════════════════════════════
            // STAGE 1: APPROACH & LANDING (Khoảng 1.5 giây)
            // ═════════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(Stage1_ApproachAndLanding(startShipPos, targetShipPos, startRot, targetRot, padPos, hudStatusText, hudFillBar));

            // ═════════════════════════════════════════════════════════════════════════
            // STAGE 2: DOCKING SYSTEM (Khoảng 0.9 giây)
            // ═════════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(Stage2_DockingClamps(padPos, targetShipPos, targetRot, hudStatusText, hudFillBar));

            // ═════════════════════════════════════════════════════════════════════════
            // STAGE 3: DATA UPLINK (Khoảng 1.8 giây)
            // ═════════════════════════════════════════════════════════════════════════
            yield return StartCoroutine(Stage3_DataUplink(padPos, targetShipPos, hudStatusText, hudFillBar));

            // Dọn dẹp HUD sau khi hoàn tất
            if (_activeDockingHUD != null) Destroy(_activeDockingHUD);

            // Dọn sạch ngàm kẹp bãi đáp để giải phóng hoàn toàn chân phi thuyền
            foreach (var c in _activeDockingClamps) { if (c != null) Destroy(c); }
            _activeDockingClamps.Clear();

            _isLandingSequence = false;

            // Kết nối dữ liệu thành công -> Tự động chuyển camera vào xem trạm thông tin Projects
            Enter();
        }

        private System.Collections.IEnumerator Stage1_ApproachAndLanding(Vector3 startPos, Vector3 targetPos, Quaternion startRot, Quaternion targetRot, Vector3 padPos, TMP_Text hudStatus, RectTransform hudFill)
        {
            float stageDuration = 1.5f;
            float elapsed = 0f;
            float groundY = padPos.y + 0.08f;

            // 4 Đèn báo vị trí chân đáp nhỏ tinh tế
            List<GameObject> gearIndicators = new List<GameObject>();
            if (ship != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    float gx = (i == 0 || i == 1) ? 1.05f : -1.05f;
                    float gz = (i == 0 || i == 2) ? 1.05f : -1.05f;
                    var dot = CreateTinyIndicatorDot("Gear_Indicator_" + i, ship.transform, new Vector3(gx, -0.32f, gz), new Color(0f, 0.85f, 1f, 0.9f));
                    gearIndicators.Add(dot);
                }
            }

            // ──────── 2 LUỒNG PHỤT KHÍ PHẢN LỰC VTOL THẲNG ĐỨNG (Dual Downward Thruster Jets) ────────
            var jetLeft = new VTOLThrusterJet("VTOL_Thruster_Jet_Left");
            var jetRight = new VTOLThrusterJet("VTOL_Thruster_Jet_Right");

            // ──────── 2 HỆ THỐNG KHÍ ĐẨY & KHÓI BỤI CUỘN TRÊN MẶT ĐẤT (Ground Impact Dust & Air Blast) ────────
            Vector3 initNozzleL = ship != null ? ship.transform.TransformPoint(new Vector3(-0.65f, 0.45f, -1.90f)) : startPos;
            Vector3 initNozzleR = ship != null ? ship.transform.TransformPoint(new Vector3(0.65f, 0.45f, -1.90f)) : startPos;
            var dustLeftGo = CreateGroundDustEmitter("Ground_Dust_Left", new Vector3(initNozzleL.x, groundY, initNozzleL.z));
            var dustRightGo = CreateGroundDustEmitter("Ground_Dust_Right", new Vector3(initNozzleR.x, groundY, initNozzleR.z));
            var psLeft = dustLeftGo != null ? dustLeftGo.GetComponent<ParticleSystem>() : null;
            var psRight = dustRightGo != null ? dustRightGo.GetComponent<ParticleSystem>() : null;

            bool touchdownTriggered = false;

            while (elapsed < stageDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / stageDuration);

                // Update HUD Progress (0% -> 30%)
                UpdateHUD(hudStatus, hudFill, "STAGE 1: VTOL DESCENT & TOUCHDOWN...", p * 0.30f);

                // ──────── Chuyển động hạ cánh mượt mà liên tục (Smooth Continuous Descent) ────────
                if (elapsed <= 0.90f)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.90f);
                    if (ship != null)
                    {
                        ship.transform.position = Vector3.Lerp(startPos, targetPos, t);
                        ship.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    }
                }
                // ──────── Chạm đất có nhún giảm chấn thủy lực (Hydraulic Suspension Settle) ────────
                else
                {
                    float bouncePhase = (elapsed - 0.90f) / 0.60f;
                    // Tàu nén nhẹ xuống sàn 0.045m rồi nảy êm ái về vị trí cân bằng
                    float compression = Mathf.Sin(bouncePhase * Mathf.PI) * Mathf.Exp(-bouncePhase * 3.2f) * 0.045f;
                    if (ship != null)
                    {
                        ship.transform.position = targetPos + Vector3.down * compression;
                        ship.transform.rotation = targetRot;
                    }

                    // KHOẢNH KHẮC TIẾP ĐẤT (TOUCHDOWN MOMENT): Thổi bùng luồng không khí đẩy & khói nén
                    if (!touchdownTriggered)
                    {
                        touchdownTriggered = true;

                        // Thổi bùng luồng không khí đẩy & bụi nén tỏa ra mạnh mẽ tại 2 vị trí tiếp đất (đúng yêu cầu!)
                        if (psLeft != null) psLeft.Emit(28);
                        if (psRight != null) psRight.Emit(28);

                        // 2 Vòng sóng áp suất mỏng quét nhanh trên mặt sàn bãi đáp
                        Vector3 currentNozzleL = ship != null ? ship.transform.TransformPoint(new Vector3(-0.65f, 0.45f, -1.90f)) : targetPos;
                        Vector3 currentNozzleR = ship != null ? ship.transform.TransformPoint(new Vector3(0.65f, 0.45f, -1.90f)) : targetPos;
                        StartCoroutine(ExpandThinWireframeShockwave(new Vector3(currentNozzleL.x, groundY, currentNozzleL.z), 0.2f, 2.5f, 0.45f, new Color(0.15f, 0.95f, 1f, 0.8f)));
                        StartCoroutine(ExpandThinWireframeShockwave(new Vector3(currentNozzleR.x, groundY, currentNozzleR.z), 0.2f, 2.5f, 0.45f, new Color(0.15f, 0.95f, 1f, 0.8f)));
                        StartCoroutine(ExpandThinWireframeShockwave(targetPos, 0.8f, 5.2f, 0.55f, new Color(0.1f, 0.8f, 1f, 0.45f)));
                    }
                }

                // Cập nhật vị trí & độ mạnh của 2 luồng phụt xả thẳng đứng
                Vector3 nozzleL = ship != null ? ship.transform.TransformPoint(new Vector3(-0.65f, 0.45f, -1.90f)) : targetPos;
                Vector3 nozzleR = ship != null ? ship.transform.TransformPoint(new Vector3(0.65f, 0.45f, -1.90f)) : targetPos;
                Vector3 impactL = new Vector3(nozzleL.x, groundY, nozzleL.z);
                Vector3 impactR = new Vector3(nozzleR.x, groundY, nozzleR.z);

                float jetPower = 1.0f;
                if (elapsed > 0.90f)
                {
                    // Sau khi tiếp đất, động cơ giảm công suất dần về 0
                    jetPower = Mathf.Clamp01(1f - (elapsed - 0.90f) / 0.45f);
                    // Dừng tạo khói bụi liên tục để bụi cũ tản dần tự nhiên
                    if (psLeft != null) { var em = psLeft.emission; em.rateOverTime = 0f; }
                    if (psRight != null) { var em = psRight.emission; em.rateOverTime = 0f; }
                }

                jetLeft.UpdateJet(nozzleL, impactL, jetPower);
                jetRight.UpdateJet(nozzleR, impactR, jetPower);

                if (dustLeftGo != null) dustLeftGo.transform.position = impactL;
                if (dustRightGo != null) dustRightGo.transform.position = impactR;

                yield return null;
            }

            // Dọn dẹp gear indicators & VTOL VFX
            foreach (var g in gearIndicators) { if (g != null) Destroy(g); }
            jetLeft.Destroy();
            jetRight.Destroy();
            if (dustLeftGo != null) Destroy(dustLeftGo, 0.6f);
            if (dustRightGo != null) Destroy(dustRightGo, 0.6f);
        }

        private System.Collections.IEnumerator Stage2_DockingClamps(Vector3 padPos, Vector3 shipPos, Quaternion shipRot, TMP_Text hudStatus, RectTransform hudFill)
        {
            float stageDuration = 0.9f;
            float elapsed = 0f;

            // Dọn sạch clamps cũ nếu có
            foreach (var c in _activeDockingClamps) { if (c != null) Destroy(c); }
            _activeDockingClamps.Clear();

            // 4 Vị trí trụ ngàm kẹp cơ khí bãi đáp (Mechanical Docking Pylons)
            List<Transform> clampArms = new List<Transform>();
            List<Vector3> startArmPos = new List<Vector3>();
            List<Vector3> lockedArmPos = new List<Vector3>();

            Vector3[] clampSockets = new Vector3[]
            {
                new Vector3( 2.1f, 0f,  1.7f),
                new Vector3(-2.1f, 0f,  1.7f),
                new Vector3( 2.1f, 0f, -1.7f),
                new Vector3(-2.1f, 0f, -1.7f)
            };

            Vector3[] clampTargetOffsets = new Vector3[]
            {
                new Vector3( 1.15f, 0f,  1.15f),
                new Vector3(-1.15f, 0f,  1.15f),
                new Vector3( 1.15f, 0f, -1.15f),
                new Vector3(-1.15f, 0f, -1.15f)
            };

            Material pylonMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pylonMat.color = new Color(0.18f, 0.22f, 0.28f);

            Material pistonMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pistonMat.color = new Color(0.45f, 0.52f, 0.60f);

            Material tipMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            tipMat.EnableKeyword("_EMISSION");
            tipMat.color = new Color(0.1f, 0.4f, 0.55f);
            tipMat.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 0.55f) * 1.5f);

            float groundY = padPos.y + 0.08f; // Mặt sàn bãi đáp
            float clampHeightY = shipPos.y - 0.12f; // Độ cao ngang chân đáp / hông tàu (~0.72m)

            for (int i = 0; i < 4; i++)
            {
                Vector3 socketPos = shipPos + shipRot * clampSockets[i];
                socketPos.y = groundY;
                Vector3 targetPos = shipPos + shipRot * clampTargetOffsets[i];
                targetPos.y = clampHeightY;

                GameObject clampRoot = new GameObject($"Docking_Clamp_Pylon_{i}");
                clampRoot.transform.position = socketPos;
                Vector3 lookDir = (new Vector3(targetPos.x, groundY, targetPos.z) - socketPos).normalized;
                clampRoot.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);

                // 1. KHỐI ĐẾ NỔI TRÊN MẶT ĐẤT (0.50m x 0.18m x 0.50m)
                GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseBox.name = "Base_Socket";
                if (baseBox.GetComponent<Collider>() != null) Destroy(baseBox.GetComponent<Collider>());
                baseBox.transform.SetParent(clampRoot.transform, false);
                baseBox.transform.localPosition = new Vector3(0f, 0.09f, 0f);
                baseBox.transform.localScale = new Vector3(0.50f, 0.18f, 0.50f);
                baseBox.GetComponent<MeshRenderer>().material = pylonMat;

                // 2. TRỤ ĐỨNG THỦY LỰC NHÔ CAO LÊN KHỎI MẶT ĐẤT (CAO 0.65m NỔI RÕ TRÊN BÃI ĐÁP)
                GameObject verticalPylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                verticalPylon.name = "Vertical_Hydraulic_Pylon";
                if (verticalPylon.GetComponent<Collider>() != null) Destroy(verticalPylon.GetComponent<Collider>());
                verticalPylon.transform.SetParent(clampRoot.transform, false);
                verticalPylon.transform.localPosition = new Vector3(0f, 0.38f, 0f);
                verticalPylon.transform.localScale = new Vector3(0.24f, 0.58f, 0.24f);
                verticalPylon.GetComponent<MeshRenderer>().material = pylonMat;

                // 3. KHỚP NỐI ĐỈNH TRỤ (TOP CAP)
                GameObject topCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                topCap.name = "Swivel_Joint";
                if (topCap.GetComponent<Collider>() != null) Destroy(topCap.GetComponent<Collider>());
                topCap.transform.SetParent(clampRoot.transform, false);
                topCap.transform.localPosition = new Vector3(0f, 0.68f, 0f);
                topCap.transform.localScale = new Vector3(0.26f, 0.08f, 0.26f);
                topCap.GetComponent<MeshRenderer>().material = pistonMat;

                // 4. CÁNH TAY NGÀM TRƯỢT NGANG VƯƠN VÀO HÔNG/CHÂN TÀU (Clamp Arm)
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Clamp_Arm";
                if (arm.GetComponent<Collider>() != null) Destroy(arm.GetComponent<Collider>());
                arm.transform.SetParent(clampRoot.transform, false);
                arm.transform.localPosition = new Vector3(0f, 0.68f, 0.12f);
                arm.transform.localScale = new Vector3(0.18f, 0.12f, 0.72f);
                arm.GetComponent<MeshRenderer>().material = pistonMat;
                clampArms.Add(arm.transform);
                startArmPos.Add(arm.transform.localPosition);
                lockedArmPos.Add(new Vector3(0f, 0.68f, 0.62f)); // Vươn vào 0.5m ôm sát chân đáp

                // 5. MÓNG KẸP TỪ TÍNH (Magnetic Claw Pad: 0.22m)
                GameObject claw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                claw.name = "Magnetic_Claw_Pad";
                if (claw.GetComponent<Collider>() != null) Destroy(claw.GetComponent<Collider>());
                claw.transform.SetParent(arm.transform, false);
                claw.transform.localPosition = new Vector3(0f, 0f, 0.36f);
                claw.transform.localScale = new Vector3(0.26f, 0.18f, 0.08f);
                claw.GetComponent<MeshRenderer>().material = pylonMat;

                // 6. ĐÈN LED CHỈ BÁO CYAN LỚN NỔI BẬT TRÊN ĐẦU NGÀM
                GameObject tipLed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tipLed.name = "Indicator_LED";
                if (tipLed.GetComponent<Collider>() != null) Destroy(tipLed.GetComponent<Collider>());
                tipLed.transform.SetParent(claw.transform, false);
                tipLed.transform.localPosition = new Vector3(0f, 0.10f, 0f);
                tipLed.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
                tipLed.GetComponent<MeshRenderer>().material = tipMat;

                _activeDockingClamps.Add(clampRoot);
            }

            // Vòng năng lượng quanh chân đáp nổi trên mặt sàn (ngang chân đáp Y)
            var energyRing = CreateThinWireframeRing("Gear_Energy_Ring", 1.35f, 0.022f, new Color(0f, 0.95f, 1f, 0.65f));
            energyRing.transform.position = new Vector3(shipPos.x, groundY + 0.12f, shipPos.z);

            int beaconCount = landingBeacons != null ? landingBeacons.Length : 0;
            bool lockedTriggered = false;

            while (elapsed < stageDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / stageDuration);

                // Cập nhật HUD (30% -> 60%)
                UpdateHUD(hudStatus, hudFill, "STAGE 2: EXTENDING CLAMPS...", 0.30f + p * 0.30f);

                // 0.0 - 0.55s: Cánh tay ngàm trượt vươn tới chân đáp
                float armProgress = Mathf.Clamp01(elapsed / 0.55f);
                float armK = Mathf.SmoothStep(0f, 1f, armProgress);
                for (int i = 0; i < clampArms.Count; i++)
                {
                    if (clampArms[i] != null)
                    {
                        clampArms[i].localPosition = Vector3.Lerp(startArmPos[i], lockedArmPos[i], armK);
                    }
                }

                // 0.55s: Khóa ngàm (Lock), bật đèn chỉ báo cyan rực rỡ, phát tia hồ quang từ tính
                if (!lockedTriggered && elapsed >= 0.55f)
                {
                    lockedTriggered = true;
                    tipMat.color = new Color(0f, 1f, 1f);
                    tipMat.SetColor("_EmissionColor", new Color(0f, 1f, 1f) * 4.0f);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 sparkP = shipPos + shipRot * clampTargetOffsets[i];
                        sparkP.y = clampHeightY;
                        SpaceExplosionVFX.SpawnHitSpark(sparkP, false);
                    }
                }

                // Kích hoạt đèn bãi đáp tuần tự trong Stage 2 (LIGHT 01 -> LIGHT 02...)
                if (beaconCount > 0)
                {
                    int activeBeaconIdx = Mathf.FloorToInt(p * beaconCount * 1.5f) % beaconCount;
                    for (int i = 0; i < beaconCount; i++)
                    {
                        var b = landingBeacons[i];
                        if (b != null && b.material != null)
                        {
                            b.material.EnableKeyword("_EMISSION");
                            bool isLit = (i == activeBeaconIdx);
                            b.material.SetColor("_EmissionColor", isLit ? new Color(0f, 0.85f, 1f) * 3.5f : new Color(0.04f, 0.2f, 0.35f));
                        }
                    }
                }

                yield return null;
            }

            UpdateHUD(hudStatus, hudFill, "<color=#00e5ff>STAGE 2: DOCKING SECURED</color>", 0.60f);
            if (energyRing != null) Destroy(energyRing);
        }

        private System.Collections.IEnumerator Stage3_DataUplink(Vector3 padPos, Vector3 shipPos, TMP_Text hudStatus, RectTransform hudFill)
        {
            float stageDuration = 1.8f;
            float elapsed = 0f;

            // ──────────────── 1. 3D MODEL SCANNING SHADER (Unity 3D Model Scanning Shader Effect) ────────────────
            var playerShip = FindFirstObjectByType<PlayerSpaceshipController>();
            List<GameObject> scanOverlays = new List<GameObject>();
            List<Material> scanMaterials = new List<Material>();

            // Quét dọc theo Object Space Z của phi thuyền (0.0 = Đuôi động cơ, 1.0 = Mũi tàu)
            float scanStart = -0.15f;
            float scanEnd = 1.15f;

            Shader scanShader = Shader.Find("SpaceCyberpunk/ModelScanningShader");
            if (playerShip != null)
            {
                var meshFilters = playerShip.GetComponentsInChildren<MeshFilter>(true);
                if (meshFilters.Length > 0 && scanShader != null)
                {
                    foreach (var mf in meshFilters)
                    {
                        if (mf.sharedMesh == null) continue;
                        GameObject overlayObj = new GameObject("3D_Model_Scan_Overlay");
                        overlayObj.transform.SetParent(mf.transform, false);
                        overlayObj.transform.localPosition = Vector3.zero;
                        overlayObj.transform.localRotation = Quaternion.identity;
                        overlayObj.transform.localScale = Vector3.one;

                        var overlayMf = overlayObj.AddComponent<MeshFilter>();
                        overlayMf.sharedMesh = mf.sharedMesh;

                        var overlayMr = overlayObj.AddComponent<MeshRenderer>();
                        Material overlayMat = new Material(scanShader);
                        overlayMat.SetColor("_ScanColor", new Color(0f, 1f, 1f, 1f));
                        overlayMat.SetColor("_SecondaryColor", new Color(0f, 0.75f, 1f, 1f));
                        overlayMat.SetFloat("_ScanProgress", scanStart);
                        overlayMat.SetFloat("_BandWidth", 0.08f);
                        overlayMat.SetFloat("_TrailLength", 0.40f);
                        overlayMat.SetFloat("_GridFreq", 28.0f);
                        overlayMat.SetFloat("_FresnelPower", 2.5f);
                        overlayMat.SetFloat("_Alpha", 1.0f);

                        overlayMr.material = overlayMat;
                        overlayMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        overlayMr.receiveShadows = false;

                        scanOverlays.Add(overlayObj);
                        scanMaterials.Add(overlayMat);
                    }
                }
            }

            // ──────────────── 2. DATA CONNECTION TO TERMINAL ────────────────
            Vector3 terminalPos = new Vector3(-10.2f, 0.25f, 20.1f);
            GameObject dataLineGo = new GameObject("Terminal_Data_Link_Line");
            var dataLr = dataLineGo.AddComponent<LineRenderer>();
            dataLr.useWorldSpace = true;
            dataLr.startWidth = 0.025f;
            dataLr.endWidth = 0.025f;
            dataLr.positionCount = 3;
            Vector3 padEdge = padPos + (terminalPos - padPos).normalized * 3.8f + Vector3.up * 0.08f;
            Vector3 midWaypoint = (padEdge + terminalPos) * 0.5f + Vector3.up * 0.12f;
            dataLr.SetPosition(0, padEdge);
            dataLr.SetPosition(1, midWaypoint);
            dataLr.SetPosition(2, terminalPos);
            var dataMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            dataMat.color = new Color(0f, 0.95f, 1f, 0.75f);
            dataLr.material = dataMat;

            // Đốm sáng xung dữ liệu di chuyển tới trạm điều khiển
            GameObject dataPulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dataPulse.name = "Data_Pulse_Dot";
            if (dataPulse.GetComponent<Collider>() != null) Destroy(dataPulse.GetComponent<Collider>());
            dataPulse.transform.localScale = Vector3.one * 0.10f;
            var pulseMr = dataPulse.GetComponent<MeshRenderer>();
            var pulseMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pulseMat.EnableKeyword("_EMISSION");
            pulseMat.color = new Color(0f, 1f, 1f);
            pulseMat.SetColor("_EmissionColor", new Color(0f, 1f, 1f) * 3.5f);
            pulseMr.material = pulseMat;

            int beaconCount = landingBeacons != null ? landingBeacons.Length : 0;
            float pulseTimer = 0f;

            while (elapsed < stageDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / stageDuration);

                // Update HUD (60% -> 100%)
                int pct = Mathf.RoundToInt(60f + p * 40f);
                UpdateHUD(hudStatus, hudFill, $"STAGE 3: 3D MODEL SCANNING ({pct}%)", 0.60f + p * 0.40f);

                // Quét dọc theo chiều dài tàu từ đuôi ra mũi
                float currentScanPos = Mathf.Lerp(scanStart, scanEnd, p);

                // Cập nhật vị trí quét cho 3D Model Scanning Shader
                for (int i = 0; i < scanMaterials.Count; i++)
                {
                    if (scanMaterials[i] != null)
                    {
                        scanMaterials[i].SetFloat("_ScanProgress", currentScanPos);
                    }
                }

                // Hạt data pulse di chuyển dọc theo đường truyền dữ liệu tới terminal
                pulseTimer += Time.deltaTime * 1.8f;
                float pulseT = pulseTimer % 1f;
                if (dataPulse != null)
                {
                    Vector3 posOnLine = pulseT < 0.5f
                        ? Vector3.Lerp(padEdge, midWaypoint, pulseT * 2f)
                        : Vector3.Lerp(midWaypoint, terminalPos, (pulseT - 0.5f) * 2f);
                    dataPulse.transform.position = posOnLine;
                }

                // Docking Beacons tiếp tục xoay tuần tự cho đến khi hoàn thành
                if (beaconCount > 0)
                {
                    int activeBeaconIdx = Mathf.FloorToInt(p * beaconCount * 2f) % beaconCount;
                    for (int i = 0; i < beaconCount; i++)
                    {
                        var b = landingBeacons[i];
                        if (b != null && b.material != null)
                        {
                            b.material.EnableKeyword("_EMISSION");
                            bool isLit = (i == activeBeaconIdx);
                            b.material.SetColor("_EmissionColor", isLit ? new Color(0f, 1f, 1f) * 4f : new Color(0.04f, 0.2f, 0.35f));
                        }
                    }
                }

                yield return null;
            }

            // ─────────────── HOÀN TẤT: TOÀN THÂN TÀU SÁNG RỰC XANH LỤC + ĐÈN BÃI ĐÁP XANH LÁ ───────────────
            Color verifiedGreen = new Color(0.2f, 1.0f, 0.4f, 1f);
            foreach (var m in scanMaterials)
            {
                if (m != null)
                {
                    m.SetColor("_ScanColor", verifiedGreen);
                    m.SetColor("_SecondaryColor", verifiedGreen);
                }
            }

            if (beaconCount > 0)
            {
                for (int i = 0; i < beaconCount; i++)
                {
                    var b = landingBeacons[i];
                    if (b != null && b.material != null)
                    {
                        b.material.EnableKeyword("_EMISSION");
                        b.material.SetColor("_EmissionColor", verifiedGreen * 3.8f);
                    }
                }
            }

            // Kích hoạt phát sáng màn hình bàn điều khiển Terminal
            var ltc = GameObject.Find("Landing_Telemetry_Console");
            if (ltc != null)
            {
                var mr = ltc.GetComponent<MeshRenderer>();
                if (mr != null && mr.material != null)
                {
                    mr.material.EnableKeyword("_EMISSION");
                    mr.material.SetColor("_EmissionColor", verifiedGreen * 2.2f);
                }
            }

            // HUD hiển thị kết nối hoàn tất
            UpdateHUD(hudStatus, hudFill, "<color=#44ff88>UPLINK SYNCHRONIZED • ACCESS GRANTED</color>", 1.0f);
            yield return new WaitForSeconds(0.45f);

            // Mờ dần overlay sau khi xác thực thành công
            float fadeOut = 0f;
            while (fadeOut < 0.3f)
            {
                fadeOut += Time.deltaTime;
                float a = 1f - (fadeOut / 0.3f);
                foreach (var m in scanMaterials)
                {
                    if (m != null) m.SetFloat("_Alpha", a);
                }
                yield return null;
            }

            // Dọn dẹp 3D Model Scanning Overlays & Materials
            foreach (var o in scanOverlays) { if (o != null) Destroy(o); }
            scanOverlays.Clear();
            foreach (var m in scanMaterials) { if (m != null) Destroy(m); }
            scanMaterials.Clear();

            // Dọn dẹp các vật thể quét và tia truyền dữ liệu
            if (dataLineGo != null) Destroy(dataLineGo);
            if (dataPulse != null) Destroy(dataPulse);
            if (dataMat != null) Destroy(dataMat);
            if (pulseMat != null) Destroy(pulseMat);
        }

        private System.Collections.IEnumerator ExpandThinWireframeShockwave(Vector3 center, float startR, float endR, float duration, Color col)
        {
            GameObject waveGo = new GameObject("Thin_Shockwave_Ring");
            waveGo.transform.position = center + Vector3.up * 0.05f;
            var lr = waveGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.035f;
            lr.endWidth = 0.035f;
            lr.positionCount = 36;
            for (int i = 0; i < 36; i++)
            {
                float a = i * Mathf.PI * 2f / 36f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * startR, 0f, Mathf.Sin(a) * startR));
            }
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            mat.color = col;
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = t / duration;
                float r = Mathf.Lerp(startR, endR, Mathf.Sqrt(k));
                for (int i = 0; i < 36; i++)
                {
                    float a = i * Mathf.PI * 2f / 36f;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
                float alpha = (1f - k) * col.a;
                mat.color = new Color(col.r, col.g, col.b, alpha);
                yield return null;
            }

            Destroy(waveGo);
        }

        // ──────── VTOL THRUSTER JET HELPER ────────
        private class VTOLThrusterJet
        {
            public GameObject root;
            public LineRenderer coreLr;
            public LineRenderer sheathLr;
            public Material coreMat;
            public Material sheathMat;

            public VTOLThrusterJet(string name)
            {
                root = new GameObject(name);

                // Lớp vỏ phát quang cyan mềm (Outer Cyan Glow Sheath)
                GameObject sheathGo = new GameObject("Sheath");
                sheathGo.transform.SetParent(root.transform, false);
                sheathLr = sheathGo.AddComponent<LineRenderer>();
                sheathLr.useWorldSpace = true;
                sheathLr.positionCount = 2;
                sheathLr.startWidth = 0.22f;
                sheathLr.endWidth = 0.40f;
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                sheathMat = new Material(sh);
                sheathMat.color = new Color(0f, 0.85f, 1f, 0.55f);
                sheathLr.material = sheathMat;
                sheathLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sheathLr.receiveShadows = false;

                // Lõi phản lực trắng rực rỡ (Inner Bright Core)
                GameObject coreGo = new GameObject("Core");
                coreGo.transform.SetParent(root.transform, false);
                coreLr = coreGo.AddComponent<LineRenderer>();
                coreLr.useWorldSpace = true;
                coreLr.positionCount = 2;
                coreLr.startWidth = 0.09f;
                coreLr.endWidth = 0.16f;
                coreMat = new Material(sh);
                coreMat.color = new Color(0.92f, 1f, 1f, 0.95f);
                coreLr.material = coreMat;
                coreLr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                coreLr.receiveShadows = false;
            }

            public void UpdateJet(Vector3 nozzlePos, Vector3 groundPos, float power)
            {
                if (root == null) return;
                if (power <= 0.01f)
                {
                    if (coreLr != null) coreLr.enabled = false;
                    if (sheathLr != null) sheathLr.enabled = false;
                    return;
                }

                float flicker = 1f + UnityEngine.Random.Range(-0.06f, 0.06f);
                float p = power * flicker;

                if (coreLr != null)
                {
                    coreLr.enabled = true;
                    coreLr.SetPosition(0, nozzlePos);
                    coreLr.SetPosition(1, groundPos);
                    coreLr.startWidth = 0.09f * p;
                    coreLr.endWidth = 0.16f * p;
                    if (coreMat != null)
                    {
                        coreMat.color = new Color(0.92f, 1f, 1f, 0.95f * power);
                    }
                }

                if (sheathLr != null)
                {
                    sheathLr.enabled = true;
                    sheathLr.SetPosition(0, nozzlePos);
                    sheathLr.SetPosition(1, groundPos);
                    sheathLr.startWidth = 0.22f * p;
                    sheathLr.endWidth = 0.40f * p;
                    if (sheathMat != null)
                    {
                        sheathMat.color = new Color(0f, 0.85f, 1f, 0.55f * power);
                    }
                }
            }

            public void Destroy()
            {
                if (root != null) GameObject.Destroy(root);
                if (coreMat != null) GameObject.Destroy(coreMat);
                if (sheathMat != null) GameObject.Destroy(sheathMat);
            }
        }

        // ──────── GROUND DUST & AIR BLAST PARTICLE SYSTEM ────────
        private static GameObject CreateGroundDustEmitter(string name, Vector3 groundPos)
        {
            var go = new GameObject(name);
            go.transform.position = groundPos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.65f, 0.92f, 1.0f, 0.55f);

            var emission = ps.emission;
            emission.rateOverTime = 35f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;
            shape.rotation = new Vector3(90f, 0f, 0f); // Xoay ngang song song với mặt sàn bãi đáp

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.4f);
            sizeCurve.AddKey(1f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.85f, 0.98f, 1.0f), 0.0f),
                    new GradientColorKey(new Color(0.25f, 0.75f, 0.95f), 0.4f),
                    new GradientColorKey(new Color(0.15f, 0.35f, 0.50f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.65f, 0.0f),
                    new GradientAlphaKey(0.40f, 0.3f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = grad;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(sh);
            mat.color = new Color(0.7f, 0.92f, 1f, 0.6f);
            psr.material = mat;

            return go;
        }

        private static GameObject CreateThinWireframeRing(string name, float radius, float lineWidth, Color color, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 36;
            for (int i = 0; i < 36; i++)
            {
                float angle = i * Mathf.PI * 2f / 36f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            mat.color = color;
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return go;
        }

        private static GameObject CreateTinyIndicatorDot(string name, Transform parent, Vector3 localPos, Color color)
        {
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = name;
            if (dot.GetComponent<Collider>() != null) Destroy(dot.GetComponent<Collider>());
            dot.transform.SetParent(parent, false);
            dot.transform.localPosition = localPos;
            dot.transform.localScale = Vector3.one * 0.08f;
            var mr = dot.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.EnableKeyword("_EMISSION");
            mat.color = color;
            mat.SetColor("_EmissionColor", color * 2f);
            mr.material = mat;
            return dot;
        }

        private GameObject CreateDockingHUDPanel(out TMP_Text statusText, out RectTransform fillBar)
        {
            statusText = null;
            fillBar = null;

            Canvas targetCanvas = null;
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo != null) targetCanvas = canvasGo.GetComponent<Canvas>();
            if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null) return null;

            GameObject panelGo = new GameObject("Docking_HUD_Panel");
            panelGo.transform.SetParent(targetCanvas.transform, false);

            var rt = panelGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-22f, -22f);
            rt.sizeDelta = new Vector2(270f, 82f);

            var img = panelGo.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.02f, 0.05f, 0.10f, 0.88f);

            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(12f, -8f);
            titleRt.sizeDelta = new Vector2(250f, 20f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.fontSize = 12f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0f, 0.9f, 1f, 0.95f);
            titleTmp.text = "[ DOCKING SYSTEM // CODE-DRIVE ]";

            GameObject statusGo = new GameObject("Status");
            statusGo.transform.SetParent(panelGo.transform, false);
            var statusRt = statusGo.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 1f);
            statusRt.anchorMax = new Vector2(1f, 1f);
            statusRt.pivot = new Vector2(0f, 1f);
            statusRt.anchoredPosition = new Vector2(12f, -32f);
            statusRt.sizeDelta = new Vector2(250f, 18f);
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.fontSize = 10.5f;
            statusTmp.color = new Color(0.9f, 0.95f, 1f, 0.9f);
            statusTmp.text = "INITIALIZING APPROACH...";
            statusText = statusTmp;

            GameObject barBgGo = new GameObject("ProgressBar_BG");
            barBgGo.transform.SetParent(panelGo.transform, false);
            var barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0f, 0f);
            barBgRt.anchorMax = new Vector2(1f, 0f);
            barBgRt.pivot = new Vector2(0.5f, 0f);
            barBgRt.anchoredPosition = new Vector2(0f, 12f);
            barBgRt.sizeDelta = new Vector2(-24f, 7f);
            var barBgImg = barBgGo.AddComponent<UnityEngine.UI.Image>();
            barBgImg.color = new Color(0.08f, 0.16f, 0.25f, 0.75f);

            GameObject barFillGo = new GameObject("ProgressBar_Fill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var fillRt = barFillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = barFillGo.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = new Color(0f, 0.95f, 1f, 0.95f);
            fillBar = fillRt;

            return panelGo;
        }

        private void UpdateHUD(TMP_Text statusText, RectTransform fillBar, string statusMsg, float progress)
        {
            if (statusText != null) statusText.text = statusMsg;
            if (fillBar != null)
            {
                fillBar.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            }
        }

        private struct TourWaypoint
        {
            public Vector3 position;
            public Vector3 lookTarget;
            public float fov;

            public TourWaypoint(Vector3 pos, Vector3 look, float fieldOfView)
            {
                position = pos;
                lookTarget = look;
                fov = fieldOfView;
            }
        }

        private void Enter()
        {
            if (viewCamera == null || cameraViewPoint == null) return;
            _armed = false;
            _landedTimer = 0f;
            SetPrompt(false, "");

            // Tắt trạng thái bị tấn công
            SpaceTrafficShipAI.ClearFleetHostileAlert();

            if (ship != null)
            {
                ship.enabled = false;
                if (_shipRb != null)
                {
                    _savedKinematic = false; // Luôn là false để khi thoát chắc chắn bay được
                    if (!_shipRb.isKinematic)
                    {
                        _shipRb.linearVelocity = Vector3.zero;
                        _shipRb.angularVelocity = Vector3.zero;
                    }
                    _shipRb.isKinematic = true;
                }
            }
            if (cameraFollow != null) cameraFollow.enabled = false;

            _savedFov = viewCamera.fieldOfView;

            if (_activeCinematicRoutine != null) StopCoroutine(_activeCinematicRoutine);
            _activeCinematicRoutine = StartCoroutine(PerformCitadelTourCinematicRoutine());
        }

        private System.Collections.IEnumerator PerformCitadelTourCinematicRoutine()
        {
            _state = State.Entering;
            ShowSlide(_index, false);

            var autoDoor = FindFirstObjectByType<CitadelAutomaticDoor>();
            if (autoDoor == null)
            {
                var dGo = GameObject.Find("West_Gate_Airlock_Portal");
                if (dGo != null) autoDoor = dGo.GetComponent<CitadelAutomaticDoor>() ?? dGo.AddComponent<CitadelAutomaticDoor>();
            }
            if (autoDoor != null) autoDoor.SetOpenImmediate(0f);

            // Bảng gợi ý bỏ qua cinematic
            _cinematicSkipHUD = CreateSkipCinematicPrompt();

            // Điểm bắt đầu của camera
            Vector3 startCamPos = viewCamera.transform.position;
            Quaternion startCamRot = viewCamera.transform.rotation;
            float startFov = viewCamera.fieldOfView;

            Vector3 finalCamPos = cameraViewPoint.position;
            Vector3 finalLook = cameraLookTarget != null ? cameraLookTarget.position : (transform.position + Vector3.up * 2f);
            Quaternion finalCamRot = Quaternion.LookRotation(finalLook - finalCamPos, Vector3.up);
            float finalFov = viewFov;

            // Xây dựng chuỗi Waypoints mượt mà theo yêu cầu:
            // 1. Bay từ từ dọc theo đường băng dẫn vào cửa căn cứ
            // 2. Cửa tự động mở khi camera tiếp cận
            // 3. Camera bay xuyên qua cửa vào trong
            // 4. Lượn quét toàn bộ không gian căn cứ (quét qua Mission Control cánh Bắc, vòm trần ánh sáng, vòng xoay trung tâm)
            // 5. Từ từ hạ cánh nhẹ nhàng đối diện bàn điều khiển 3D Portfolio World ("cái chính")
            List<TourWaypoint> waypoints = new List<TourWaypoint>();

            // P0: Vị trí hiện tại của camera sau khi hạ cánh
            waypoints.Add(new TourWaypoint(startCamPos, new Vector3(-10.0f, 2.0f, 23.0f), startFov));

            // P1: Lướt chậm trên đường băng ngoài trời hướng vào cổng Tây
            waypoints.Add(new TourWaypoint(new Vector3(-21.0f, 2.30f, 23.0f), new Vector3(-10.0f, 2.0f, 23.0f), 52f));

            // P2: Đến gần cửa (X = -13.5m) -> Kích hoạt cửa mở
            waypoints.Add(new TourWaypoint(new Vector3(-13.5f, 2.10f, 23.0f), new Vector3(-2.0f, 2.0f, 23.0f), 50f));

            // P3: Bay xuyên qua cửa đã mở (X = -7.0m)
            waypoints.Add(new TourWaypoint(new Vector3(-7.0f, 2.10f, 23.0f), new Vector3(10.0f, 2.5f, 23.0f), 50f));

            // P4: Thoát khỏi hầm cửa bước vào Đại sảnh Rotunda (X = -1.0m)
            waypoints.Add(new TourWaypoint(new Vector3(-1.0f, 2.40f, 23.0f), new Vector3(14.0f, 2.6f, 23.0f), 52f));

            // P5: Nâng cao độ & lượn sang cánh Bắc ngắm dàn màn hình Mission Control khổng lồ
            waypoints.Add(new TourWaypoint(new Vector3(4.5f, 3.40f, 27.5f), new Vector3(14.0f, 2.8f, 46.0f), 55f));

            // P6: Quét toàn cảnh kiến trúc mái vòm trần cao & toàn bộ không gian căn cứ
            waypoints.Add(new TourWaypoint(new Vector3(9.5f, 3.90f, 32.0f), new Vector3(16.0f, 2.6f, 23.0f), 56f));

            // P7: Lượn vòng cung mềm mại quanh lõi mạch điện trung tâm và dàn server
            waypoints.Add(new TourWaypoint(new Vector3(12.5f, 3.20f, 19.5f), new Vector3(15.2f, 2.5f, 23.0f), 52f));

            // P8: Vị trí quan sát chính thức trước bàn kiosk Portfolio World
            waypoints.Add(new TourWaypoint(finalCamPos, finalLook, finalFov));

            float totalFlightDuration = 8.5f; // Di chuyển nhịp nhàng 8.5s cho toàn bộ hành trình
            float elapsed = 0f;
            bool doorOpened = false;

            while (elapsed < totalFlightDuration)
            {
                // Kiểm tra phím Bỏ qua (Skip)
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
                {
                    break;
                }

                elapsed += Time.deltaTime;
                float globalT = Mathf.Clamp01(elapsed / totalFlightDuration);

                // Kích hoạt cửa tự động mở khi camera bắt đầu tiếp cận (khoảng 20% hành trình)
                if (!doorOpened && globalT >= 0.20f)
                {
                    doorOpened = true;
                    if (autoDoor != null) autoDoor.OpenDoor(locked: true);
                }

                // Nội suy mượt mà qua các waypoints bằng đường cong Catmull-Rom đa đoạn
                Vector3 curPos;
                Vector3 curLook;
                float curFov;
                SampleSplinePath(waypoints, globalT, out curPos, out curLook, out curFov);

                viewCamera.transform.position = curPos;
                viewCamera.transform.rotation = Quaternion.Slerp(viewCamera.transform.rotation, Quaternion.LookRotation(curLook - curPos, Vector3.up), Time.deltaTime * 10f);
                viewCamera.fieldOfView = curFov;

                yield return null;
            }

            // Đảm bảo cửa đã mở
            if (autoDoor != null) autoDoor.OpenDoor(locked: true);

            // Chuyển êm ái vào vị trí quan sát cuối cùng
            float settleElapsed = 0f;
            Vector3 settleStartPos = viewCamera.transform.position;
            Quaternion settleStartRot = viewCamera.transform.rotation;
            float settleStartFov = viewCamera.fieldOfView;

            while (settleElapsed < 0.35f)
            {
                settleElapsed += Time.deltaTime;
                float st = Mathf.SmoothStep(0f, 1f, settleElapsed / 0.35f);
                viewCamera.transform.position = Vector3.Lerp(settleStartPos, finalCamPos, st);
                viewCamera.transform.rotation = Quaternion.Slerp(settleStartRot, finalCamRot, st);
                viewCamera.fieldOfView = Mathf.Lerp(settleStartFov, finalFov, st);
                yield return null;
            }

            viewCamera.transform.position = finalCamPos;
            viewCamera.transform.rotation = finalCamRot;
            viewCamera.fieldOfView = finalFov;

            if (_cinematicSkipHUD != null) Destroy(_cinematicSkipHUD);

            _state = State.Viewing;
            ShowSlide(_index, true);
            _activeCinematicRoutine = null;
        }

        private void SampleSplinePath(List<TourWaypoint> waypoints, float t, out Vector3 pos, out Vector3 look, out float fov)
        {
            if (waypoints.Count == 0)
            {
                pos = Vector3.zero; look = Vector3.forward; fov = 50f; return;
            }
            if (waypoints.Count == 1 || t <= 0f)
            {
                pos = waypoints[0].position; look = waypoints[0].lookTarget; fov = waypoints[0].fov; return;
            }
            if (t >= 1f)
            {
                var last = waypoints[waypoints.Count - 1];
                pos = last.position; look = last.lookTarget; fov = last.fov; return;
            }

            int numSegments = waypoints.Count - 1;
            float segmentT = t * numSegments;
            int segIndex = Mathf.Clamp(Mathf.FloorToInt(segmentT), 0, numSegments - 1);
            float localT = segmentT - segIndex;
            localT = Mathf.SmoothStep(0f, 1f, localT);

            int i0 = Mathf.Clamp(segIndex - 1, 0, waypoints.Count - 1);
            int i1 = segIndex;
            int i2 = Mathf.Clamp(segIndex + 1, 0, waypoints.Count - 1);
            int i3 = Mathf.Clamp(segIndex + 2, 0, waypoints.Count - 1);

            pos = CatmullRom(waypoints[i0].position, waypoints[i1].position, waypoints[i2].position, waypoints[i3].position, localT);
            look = CatmullRom(waypoints[i0].lookTarget, waypoints[i1].lookTarget, waypoints[i2].lookTarget, waypoints[i3].lookTarget, localT);
            fov = Mathf.Lerp(waypoints[i1].fov, waypoints[i2].fov, localT);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2.0f * p1) +
                (-p0 + p2) * t +
                (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
                (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
            );
        }

        private GameObject CreateSkipCinematicPrompt()
        {
            var canvasGo = new GameObject("Cinematic_Skip_HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;

            var cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.05f);
            rect.anchorMax = new Vector2(0.5f, 0.05f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800, 50);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "NHẤN  [SPACE]  HOẶC  [ENTER]  ĐỂ BỎ QUA CINEMATIC";
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 0.95f, 1.0f, 0.85f);

            return canvasGo;
        }

        private void Exit()
        {
            if (_activeCinematicRoutine != null) StopCoroutine(_activeCinematicRoutine);
            _activeCinematicRoutine = StartCoroutine(PerformCitadelExitCinematicRoutine());
        }

        private System.Collections.IEnumerator PerformCitadelExitCinematicRoutine()
        {
            _state = State.Exiting;
            ClearHover();

            // Dọn dẹp ngàm kẹp bãi đáp nếu có
            foreach (var c in _activeDockingClamps) { if (c != null) Destroy(c); }
            _activeDockingClamps.Clear();

            // Đặt lại đèn bãi đáp về chế độ standby
            if (landingBeacons != null)
            {
                foreach (var b in landingBeacons)
                {
                    if (b != null && b.material != null)
                    {
                        b.material.EnableKeyword("_EMISSION");
                        b.material.SetColor("_EmissionColor", new Color(0.08f, 0.45f, 0.65f) * 1.5f);
                    }
                }
            }

            var autoDoor = FindFirstObjectByType<CitadelAutomaticDoor>();
            if (autoDoor != null) autoDoor.OpenDoor(locked: true);

            Vector3 startPos = viewCamera.transform.position;
            Quaternion startRot = viewCamera.transform.rotation;
            float startFov = viewCamera.fieldOfView;

            // Đích đến sau lưng tàu
            Vector3 targetShipCamPos;
            Quaternion targetShipCamRot;
            if (ship != null && cameraFollow != null)
            {
                Transform target = cameraFollow.target != null ? cameraFollow.target : ship.transform;
                targetShipCamPos = target.position - target.forward * cameraFollow.distance + target.up * cameraFollow.height;
                Vector3 look = target.position + target.forward * 4f;
                targetShipCamRot = Quaternion.LookRotation(look - targetShipCamPos, target.up);
            }
            else
            {
                targetShipCamPos = landingPadCenter != null ? landingPadCenter.position + new Vector3(0f, 3.5f, -8f) : startPos;
                targetShipCamRot = Quaternion.identity;
            }

            // Waypoints thoát: Kiosk -> Hành lang Tây -> Ra ngoài cửa -> Bãi đáp
            List<Vector3> exitWaypoints = new List<Vector3>
            {
                startPos,
                new Vector3(4.0f, 2.5f, 23.0f),
                new Vector3(-6.0f, 2.2f, 23.0f),
                new Vector3(-14.0f, 2.4f, 23.0f),
                targetShipCamPos
            };

            float exitDuration = 2.2f;
            float elapsed = 0f;
            bool doorClosed = false;

            while (elapsed < exitDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / exitDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                int numSegments = exitWaypoints.Count - 1;
                float segT = smoothT * numSegments;
                int segIdx = Mathf.Clamp(Mathf.FloorToInt(segT), 0, numSegments - 1);
                float localT = Mathf.SmoothStep(0f, 1f, segT - segIdx);

                Vector3 curPos = Vector3.Lerp(exitWaypoints[segIdx], exitWaypoints[segIdx + 1], localT);
                viewCamera.transform.position = curPos;
                viewCamera.transform.rotation = Quaternion.Slerp(startRot, targetShipCamRot, smoothT);
                viewCamera.fieldOfView = Mathf.Lerp(startFov, _savedFov, smoothT);

                // Khi camera đã lướt ra ngoài cửa (X < -12m), tự động đóng cửa lại sau lưng!
                if (!doorClosed && curPos.x < -12.0f)
                {
                    doorClosed = true;
                    if (autoDoor != null) autoDoor.CloseDoor(unlock: true);
                }

                yield return null;
            }

            if (autoDoor != null) autoDoor.CloseDoor(unlock: true);

            viewCamera.transform.position = targetShipCamPos;
            viewCamera.transform.rotation = targetShipCamRot;
            viewCamera.fieldOfView = _savedFov;

            _state = State.Idle;
            RestoreControl();
            _activeCinematicRoutine = null;
        }

        private void UpdateTransition(State next)
        {
            float curTransitionDuration = (_state == State.Exiting) ? 0.55f : Mathf.Min(transitionTime, 1.0f);
            _t += Time.deltaTime / Mathf.Max(0.01f, curTransitionDuration);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t));
            viewCamera.transform.position = Vector3.Lerp(_fromPos, _toPos, k);
            viewCamera.transform.rotation = Quaternion.Slerp(_fromRot, _toRot, k);
            viewCamera.fieldOfView = Mathf.Lerp(_fromFov, _toFov, k);

            if (_t >= 1f)
            {
                _state = next;
                if (next == State.Idle) RestoreControl();
            }
        }

        private void RestoreControl()
        {
            if (ship != null)
            {
                // Nhấc nhẹ tàu lên 1.8m để phi thuyền thoát khỏi va chạm mặt sàn và bay lên ngay lập tức
                ship.transform.position += Vector3.up * 1.8f;

                if (_shipRb != null)
                {
                    _shipRb.isKinematic = false; // BẮT BUỘC LÀ FALSE ĐỂ PHI THUYỀN NHẬN LỰC ĐỘNG CƠ BAY ĐƯỢC!
                    _shipRb.linearVelocity = Vector3.up * 4.0f; // Cấp lực nâng ban đầu để cất cánh mượt mà
                    _shipRb.angularVelocity = Vector3.zero;
                }
                ship.enabled = true;
            }
            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
                if (ship != null) cameraFollow.target = ship.transform;
            }
            _isLandingSequence = false;
            _armed = false; // Tắt armed để không bị kích hoạt đáp lại ngay lập tức
            _landedTimer = 0f;
            SetPrompt(false, "");
        }

        // ───────────────────────── Viewing: điều khiển trạm ─────────────────────────
        private void UpdateViewing()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Next();
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Prev();
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Open();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) { Exit(); return; }

            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            Transform hoverNow = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Collide))
            {
                Collider c = hit.collider;
                if (c == prevButton || c == nextButton || c == openButton || c == prevWingCollider || c == nextWingCollider)
                    hoverNow = c.transform;
                else if (pageDots != null)
                {
                    for (int i = 0; i < pageDots.Length; i++)
                        if (pageDots[i] != null && c.transform == pageDots[i].transform) hoverNow = c.transform;
                }

                if (Input.GetMouseButtonDown(0) && hoverNow != null)
                {
                    if (c == prevButton || c == prevWingCollider) Prev();
                    else if (c == nextButton || c == nextWingCollider) Next();
                    else if (c == openButton) Open();
                    else
                    {
                        for (int i = 0; i < pageDots.Length; i++)
                            if (pageDots[i] != null && c.transform == pageDots[i].transform) ShowSlide(i, false);
                    }
                }
            }
            SetHover(hoverNow);
        }

        private void SetHover(Transform t)
        {
            if (t == _hovered) return;
            ClearHover();
            if (t == null) return;
            _hovered = t;
            _hoveredBaseScale = t.localScale;
            t.localScale = _hoveredBaseScale * 1.14f;
        }

        private void ClearHover()
        {
            if (_hovered != null) _hovered.localScale = _hoveredBaseScale;
            _hovered = null;
        }

        public void Next() => ShowSlide((_index + 1) % slides.Length, false);
        public void Prev() => ShowSlide((_index - 1 + slides.Length) % slides.Length, false);

        public void Open()
        {
            var s = slides[_index];
            if (!string.IsNullOrEmpty(s.url)) Application.OpenURL(s.url);
        }

        // ───────────────────────── Hiển thị slide ─────────────────────────
        private void ShowSlide(int i, bool instant)
        {
            if (slides == null || slides.Length == 0) return;
            _index = Mathf.Clamp(i, 0, slides.Length - 1);
            var s = slides[_index];
            var prev = slides[(_index - 1 + slides.Length) % slides.Length];
            var next = slides[(_index + 1) % slides.Length];
            string accentHex = ColorUtility.ToHtmlStringRGB(s.accent);

            _targetTitle = s.title ?? "";
            _targetBody = s.body ?? "";

            if (titleText) titleText.text = _targetTitle;
            if (linkText) linkText.text = s.linkLabel;
            if (bodyText) bodyText.text = _targetBody;
            if (prevLabelText) prevLabelText.text = prev.shortLabel;
            if (nextLabelText) nextLabelText.text = next.shortLabel;
            if (roleText) roleText.text = s.role;
            if (withText) withText.text = s.with;
            if (pageText) pageText.text = $"<color=#{accentHex}>{(_index + 1):00}</color> / {slides.Length:00}";
            if (achievementsText) achievementsText.text = !string.IsNullOrEmpty(s.distinctions) ? s.distinctions : achievements;
            if (titleText) titleText.color = Color.Lerp(Color.white, s.accent, 0.25f);

            if (imageRenderer != null)
            {
                imageRenderer.GetPropertyBlock(_mpb);
                _mpb.SetTexture(BaseMapId, s.image != null ? s.image : _fallbackImage);
                _mpb.SetColor(BaseColorId, Color.white);
                imageRenderer.SetPropertyBlock(_mpb);
            }

            if (pageDots != null)
            {
                for (int d = 0; d < pageDots.Length; d++)
                {
                    if (pageDots[d] == null) continue;
                    bool active = d == _index;
                    pageDots[d].gameObject.SetActive(d < slides.Length);
                    pageDots[d].GetPropertyBlock(_mpb);
                    Color c = active ? s.accent * 2.2f : new Color(0.12f, 0.16f, 0.24f);
                    _mpb.SetColor(BaseColorId, active ? s.accent : new Color(0.15f, 0.2f, 0.3f));
                    _mpb.SetColor(EmissionColorId, c);
                    pageDots[d].SetPropertyBlock(_mpb);
                }
            }

            if (accentRenderers != null)
            {
                foreach (var r in accentRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(BaseColorId, s.accent);
                    _mpb.SetColor(EmissionColorId, s.accent * 2.2f);
                    r.SetPropertyBlock(_mpb);
                }
            }
            if (accentLights != null)
                foreach (var l in accentLights) if (l != null) l.color = s.accent;

            _contentAnim = instant ? 1f : 0f;
        }

        // ───────────────────────── Hoạt họa sáng tạo (Animations) ─────────────────────────
        private void AnimateVisuals()
        {
            float time = Time.time;

            // 1. Hiệu ứng giải mã Cyberpunk (Scramble Decode Title) & Gõ chữ (Typewriter Body) khi chuyển Project
            if (_contentAnim < 1f)
            {
                _contentAnim = Mathf.MoveTowards(_contentAnim, 1f, Time.deltaTime * 2.2f);
                float a = Mathf.SmoothStep(0f, 1f, _contentAnim);

                // Giải mã từng ký tự của tiêu đề
                if (titleText != null && !string.IsNullOrEmpty(_targetTitle))
                {
                    int revealed = Mathf.FloorToInt(_contentAnim * _targetTitle.Length);
                    char[] buf = _targetTitle.ToCharArray();
                    for (int c = revealed; c < buf.Length; c++)
                    {
                        if (buf[c] != ' ')
                            buf[c] = CyberChars[(c + Mathf.FloorToInt(time * 45f)) % CyberChars.Length];
                    }
                    titleText.text = new string(buf);
                }

                // Gõ từng ký tự của nội dung mô tả kèm con trỏ █
                if (bodyText != null && !string.IsNullOrEmpty(_targetBody))
                {
                    int chars = Mathf.Clamp(Mathf.FloorToInt(_contentAnim * _targetBody.Length), 0, _targetBody.Length);
                    bodyText.text = _targetBody.Substring(0, chars) + "<color=#45F0FF>\u2588</color>";
                }

                if (imageRenderer != null)
                {
                    float flicker = _contentAnim < 0.35f ? (Mathf.PerlinNoise(time * 45f, 0f) > 0.45f ? 0.35f : 1f) : 1f;
                    imageRenderer.GetPropertyBlock(_mpb);
                    _mpb.SetColor(BaseColorId, Color.white * (a * flicker));
                    imageRenderer.SetPropertyBlock(_mpb);
                }
            }
            else if (bodyText != null && !string.IsNullOrEmpty(_targetBody))
            {
                // Con trỏ máy tính nhấp nháy ở cuối văn bản khi đã gõ xong
                bool blink = Mathf.Repeat(time * 2.2f, 1f) > 0.45f;
                string cursor = blink ? " <color=#45F0FF>_</color>" : "";
                string desired = _targetBody + cursor;
                if (bodyText.text != desired) bodyText.text = desired;
                if (titleText != null && titleText.text != _targetTitle) titleText.text = _targetTitle;
            }

            // 2. Cuộn sọc ngang CRT (Scrolling Scanlines) trên màn hình chính & các màn hình vệ tinh
            if (scanlineRenderer != null)
            {
                scanlineRenderer.GetPropertyBlock(_mpb);
                _mpb.SetVector("_BaseMap_ST", new Vector4(1f, 9f, 0f, -time * 0.35f));
                scanlineRenderer.SetPropertyBlock(_mpb);
            }
            if (allScanlineRenderers != null)
            {
                for (int i = 0; i < allScanlineRenderers.Length; i++)
                {
                    var sr = allScanlineRenderers[i];
                    if (sr == null) continue;
                    sr.GetPropertyBlock(_mpb);
                    _mpb.SetVector("_BaseMap_ST", new Vector4(1f, 6f, 0f, -time * 0.4f + i * 0.2f));
                    sr.SetPropertyBlock(_mpb);
                }
            }

            // 3. Tia quét Laser ngang (Laser Scan Bar) chạy dọc từ đỉnh xuống đáy màn hình chính
            if (laserScanBar != null)
            {
                float scanT = Mathf.Repeat(time * 0.38f, 1f);
                float scanY = Mathf.Lerp(5.05f, 1.25f, scanT);
                Vector3 lp = laserScanBar.localPosition;
                laserScanBar.localPosition = new Vector3(lp.x, scanY, lp.z);
            }

            // 4. Các màn hình Hologram vệ tinh lơ lửng nhấp nhô (Anti-Gravity Bobbing)
            if (floatingMonitors != null && _floatingBaseY != null)
            {
                for (int i = 0; i < floatingMonitors.Length && i < _floatingBaseY.Length; i++)
                {
                    Transform fm = floatingMonitors[i];
                    if (fm == null) continue;
                    float bob = Mathf.Sin(time * 2.2f + i * 1.15f) * 0.055f;
                    Vector3 p = fm.localPosition;
                    fm.localPosition = new Vector3(p.x, _floatingBaseY[i] + bob, p.z);
                }
            }

            // 5. Khối cầu Gyroscope Hologram 3D trên bàn trung tâm xoay đa trục
            if (gyroRings != null)
            {
                for (int i = 0; i < gyroRings.Length; i++)
                {
                    Transform gr = gyroRings[i];
                    if (gr == null) continue;
                    if (i == 0) gr.Rotate(35f * Time.deltaTime, 55f * Time.deltaTime, 0f, Space.Self);
                    else if (i == 1) gr.Rotate(-50f * Time.deltaTime, 25f * Time.deltaTime, 40f * Time.deltaTime, Space.Self);
                    else gr.Rotate(20f * Time.deltaTime, -65f * Time.deltaTime, 30f * Time.deltaTime, Space.Self);
                }
            }

            // 6. Biểu tượng Hologram dưới sàn xoay tròn & nhịp thở
            if (floorEmblem != null)
            {
                floorEmblem.Rotate(0f, 0f, 18f * Time.deltaTime, Space.Self);
                float s = 2.8f + Mathf.Sin(time * 2.5f) * 0.12f;
                floorEmblem.localScale = new Vector3(s, s, 1f);
            }

            // 7. Các đĩa radar / máy chiếu sao trên đảo quay tự động
            if (islandRotators != null)
            {
                for (int i = 0; i < islandRotators.Length; i++)
                {
                    if (islandRotators[i] != null)
                        islandRotators[i].Rotate(0f, (16f + i * 8f) * Time.deltaTime, 0f, Space.World);
                }
            }

            // 8. Nhịp đèn năng lượng
            if (accentLights != null)
            {
                float pulse = 1f + Mathf.Sin(time * 2.6f) * 0.22f;
                foreach (var l in accentLights) if (l != null) l.intensity = 2.2f * pulse;
            }

            // 9. Prompt nổi lơ lửng hướng về camera
            if (promptText != null && promptText.gameObject.activeSelf && viewCamera != null)
            {
                Transform pt = promptText.transform;
                pt.rotation = Quaternion.LookRotation(pt.position - viewCamera.transform.position, Vector3.up);
            }

            // 10. Các biển phụ luôn xoay mặt chữ hướng thẳng về phía Camera hiện tại
            if (cameraFacingSigns != null && viewCamera != null)
            {
                Vector3 camPos = viewCamera.transform.position;
                for (int i = 0; i < cameraFacingSigns.Length; i++)
                {
                    Transform s = cameraFacingSigns[i];
                    if (s == null) continue;
                    Vector3 awayFromCam = s.position - camPos;
                    awayFromCam.y = 0f;
                    if (awayFromCam.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(awayFromCam.normalized, Vector3.up);
                        s.rotation = Quaternion.Slerp(s.rotation, targetRot, Time.deltaTime * 10f);
                    }
                }
            }
        }

        private void SetPrompt(bool show, string msg)
        {
            if (promptText == null) return;
            if (promptText.gameObject.activeSelf != show) promptText.gameObject.SetActive(show);
            if (show && promptText.text != msg) promptText.text = msg;
        }

        private static Texture2D BuildFallbackImage()
        {
            const int w = 256, h = 160;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (float)y / h;
                Color col = Color.Lerp(new Color(0.03f, 0.05f, 0.12f), new Color(0.10f, 0.06f, 0.22f), v);
                if (x % 16 == 0 || y % 16 == 0) col += new Color(0.05f, 0.12f, 0.18f);
                float r = Vector2.Distance(new Vector2(x, y), c);
                if (Mathf.Abs(r - 52f) < 2.2f || Mathf.Abs(r - 36f) < 1.2f) col = new Color(0.5f, 0.95f, 1f);
                if (r < 20f) col = Color.Lerp(new Color(0.9f, 0.4f, 1f), col, r / 20f);
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return tex;
        }

        private static Slide[] DefaultSlides()
        {
            return new[]
            {
                new Slide
                {
                    title = "3D PORTFOLIO WORLD",
                    linkLabel = "LEETONION.PORTFOLIO.IO",
                    url = "https://github.com/LeeTonion",
                    shortLabel = "3D PORTFOLIO",
                    body = "Thế giới 3D tương tác điều khiển phi thuyền không gian & hệ thống GPS Navigation A*.\n• Tech: Unity 6 • C# • URP • Physics",
                    role = "CREATIVE DEV",
                    with = "CODEDRIVE STUDIO",
                    distinctions = "• Creative Dev Showcase 2026\n• Interactive 3D World\n• Custom Flight & Hover Physics",
                    accent = new Color(0.25f, 0.9f, 1f)
                },
                new Slide
                {
                    title = "3D ACTION RPG",
                    linkLabel = "GITHUB.COM/LEETONION/RPG",
                    url = "https://github.com/LeeTonion",
                    shortLabel = "ACTION RPG",
                    body = "Hệ thống chiến đấu hành động nhịp độ cao, Animation Motion Warping, AI Behavior Trees & VFX.\n• Tech: Unity 3D • C# • URP • AI Nav",
                    role = "LEAD PROGRAMMER",
                    with = "UNITY COMMUNITY",
                    distinctions = "• Best Gameplay Showcase\n• Boss AI Behavior Tree\n• 60 FPS URP Combat VFX",
                    accent = new Color(1f, 0.45f, 0.2f)
                },
                new Slide
                {
                    title = "DRIFT RACER ONLINE",
                    linkLabel = "PLAY.CODE-DRIVE.IO",
                    url = "https://github.com/LeeTonion",
                    shortLabel = "DRIFT RACER",
                    body = "Game đua xe drift nhiều người chơi thời gian thực, vật lý Raycast tùy biến & Client Prediction.\n• Tech: Unity • Photon Fusion • Physics",
                    role = "FULLSTACK GAME DEV",
                    with = "INDIE LABS",
                    distinctions = "• Top 10 Indie Showcase\n• Real-time Multiplayer\n• Custom Raycast Vehicle",
                    accent = new Color(0.95f, 0.3f, 0.7f)
                },
                new Slide
                {
                    title = "THREE.JS JOURNEY",
                    linkLabel = "THREEJS-JOURNEY.COM",
                    url = "https://threejs-journey.com",
                    shortLabel = "THREE.JS WEB",
                    body = "Ứng dụng & không gian 3D Web tương tác thời gian thực với WebGL, Shaders, Physics & GSAP.\n• Tech: Three.js • WebGL • GLSL • Blender",
                    role = "3D WEB DEVELOPER",
                    with = "CREATIVE WEB LAB",
                    distinctions = "• FWA of the Day\n• Awwwards Site of Month\n• Custom GLSL Shaders",
                    accent = new Color(0.55f, 0.45f, 1f)
                },
                new Slide
                {
                    title = "SPACE OUTPOST SIM",
                    linkLabel = "GITHUB.COM/LEETONION/SPACE",
                    url = "https://github.com/LeeTonion",
                    shortLabel = "SPACE SIM",
                    body = "Mô phỏng trạm nghiên cứu không gian Cyberpunk với hệ thống năng lượng, radar & thiên thạch.\n• Tech: Unity 6 • URP • Shader Graph",
                    role = "SYSTEM ARCHITECT",
                    with = "INTERSTELLAR LAB",
                    distinctions = "• Procedural Space Belt\n• Modular Sci-Fi Base\n• WebGL Ready Optimization",
                    accent = new Color(0.25f, 0.95f, 0.55f)
                },
            };
        }
    }
}
