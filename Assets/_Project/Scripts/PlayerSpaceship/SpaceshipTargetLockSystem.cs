using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Hệ Thống Khóa Mục Tiêu Nhanh 1 Giây & HUD Lục Giác Tập Trung Độc Quyền (Single-Target Hexagon Lock-On HUD):
    /// - Hiển thị bộ tâm ngắm lục giác vector khử răng cưa phát sáng (Anti-Aliased Hexagon Targeting Reticle).
    /// - Chỉ khóa và hiển thị DUY NHẤT 1 mục tiêu được ngắm chuẩn xác (không hiện rác nhiều marker trên màn hình).
    /// - Điều kiện khóa:
    ///   + Mục tiêu phải nằm trọn bên trong khung lục giác (Hexagon Aperture).
    ///   + Nằm trong tầm ngắm 200m và không bị che khuất tầm nhìn trực tiếp (Physics Collider + Opaque Visual Geometry).
    ///   + Khóa nhanh thần tốc trong vòng đúng 1.0 giây (1s countdown).
    /// - Khi đã khóa cứng (TARGET LOCKED):
    ///   + Toàn bộ khung lục giác và tâm ngắm chuyển sang màu vàng-cam rực sáng (Luminous White-Amber Glow).
    ///   + Tiêu đề hiển thị TARGET LOCKED, thanh đáy hiển thị WEAPON SYS: ACTIVE.
    ///   + Pháo laser kép tự động hội tụ và bám theo mục tiêu chính xác tuyệt đối.
    ///   + Nếu mục tiêu bị phá hủy hoặc bị bất kỳ đảo/bảng/công trình/tàu nào che khuất -> Lập tức hủy khóa.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SpaceshipTargetLockSystem : MonoBehaviour
    {
        [Header("Cấu Hình Khóa Mục Tiêu (Lock-On Configuration)")]
        [Tooltip("Khoảng cách tối đa để có thể khóa mục tiêu (mặc định: 200m)")]
        public float maxLockDistance = 200f;

        [Tooltip("Thời gian giữ tâm ngắm liên tục trong lục giác để khóa cứng (mặc định: đúng 1.0 giây theo yêu cầu)")]
        public float lockHoldDuration = 1.0f;

        [Header("Màu Sắc HUD Khóa Mục Tiêu")]
        public Color searchColor = new Color(0.28f, 0.88f, 1.0f, 0.72f);        // Cyan vi tính tinh tế khi dò tìm
        public Color lockingColor = new Color(1.0f, 0.80f, 0.22f, 0.95f);       // Hổ phách vàng khi đang đếm 1s
        public Color lockedColor = new Color(1.0f, 0.60f, 0.14f, 1.0f);         // Cam hổ phách hào quang khi khóa cứng
        public Color lockedHeaderColor = new Color(1.0f, 0.95f, 0.86f, 1.0f);   // Lõi sáng vàng kem nổi bật (chuẩn ảnh mẫu)
        public Color activeCyanColor = new Color(0.28f, 1.0f, 0.94f, 1.0f);      // Chữ xanh ngọc WEAPON SYS: ACTIVE.

        public static SpaceshipTargetLockSystem Instance { get; private set; }

        private Camera _mainCam;
        private Canvas _canvas;
        private RectTransform _hudRootRt;
        private GameObject _trackingUiRoot;
        private Transform _currentLockedTarget;
        private Transform _potentialLockCandidate;
        private float _lockHoldTimer = 0f;
        private Vector2 _reticleSmoothVel;

        private HexagonLockReticleUI _reticleUI;
        private static Sprite _circleReticleSprite;
        private static Sprite _dotReticleSprite;

        // Bộ nhớ đệm các khối hình học hiển thị (Opaque MeshRenderer không có Collider - vd: bảng SPAWN HUB, tinh thể, tường)
        private struct MeshTriData
        {
            public Vector3[] Vertices;
            public int[] Triangles;
            public Bounds PaddedLocalBounds;
        }

        private class OpaqueVisualOccluder
        {
            public MeshRenderer Renderer;
            public GameObject GameObject;
            public MeshTriData MeshData;
            public bool HasExactMesh;
        }

        private class OccluderGroup
        {
            public Bounds WorldBounds;
            public List<OpaqueVisualOccluder> Items = new List<OpaqueVisualOccluder>();
        }

        private readonly List<OccluderGroup> _occluderGroups = new List<OccluderGroup>();
        private readonly Dictionary<Mesh, MeshTriData> _meshTriCache = new Dictionary<Mesh, MeshTriData>();
        private bool _visualOccludersBuilt = false;

        public Transform CurrentLockedTarget => _currentLockedTarget;
        public bool HasLockedTarget => _currentLockedTarget != null;
        public float LockProgress => Mathf.Clamp01(_lockHoldTimer / lockHoldDuration);

        public void ClearLockImmediate()
        {
            _currentLockedTarget = null;
            _potentialLockCandidate = null;
            _lockHoldTimer = 0f;
        }

        public void ClearLockIfTarget(Transform t)
        {
            if (t == null) return;
            if (_currentLockedTarget == t || (_currentLockedTarget != null && _currentLockedTarget.IsChildOf(t)))
            {
                _currentLockedTarget = null;
            }
            if (_potentialLockCandidate == t || (_potentialLockCandidate != null && _potentialLockCandidate.IsChildOf(t)))
            {
                _potentialLockCandidate = null;
                _lockHoldTimer = 0f;
            }
        }

        private class HexagonLockReticleUI
        {
            public GameObject Root;
            public RectTransform RootRt;

            // Khung lục giác vector khử răng cưa + hào quang neon mượt mà (Outer Hexagon Border Only)
            public HexagonVectorGraphic HexGraphic;

            // Tâm ngắm tròn chính giữa sắc nét (Center Circular Reticle: Vòng tròn mảnh + chấm tròn phát sáng)
            public Image CenterRing;
            public RectTransform CenterRingRt;
            public Image CenterDot;

            // Văn bản trạng thái sắc nét không hộp đen thô (Drop shadow + Letter spacing)
            public TMP_Text HeaderText;   // TARGET LOCKED / LOCKING 0.Xs
            public TMP_Text FooterText;   // WEAPON SYS: ACTIVE.
            public TMP_Text DistText;     // [ 85M ]
        }

        /// <summary>
        /// Custom Vector MaskableGraphic vẽ viền lục giác khử răng cưa (sub-pixel AA feather)
        /// với góc nối mitered liền mạch 100% và hào quang neon mềm mại (không vỡ pixel ở cạnh chéo).
        /// </summary>
        private class HexagonVectorGraphic : MaskableGraphic
        {
            public float HalfWidth = 220f;
            public float HalfHeight = 172f;
            public float CornerY = 74f;
            public float TopGap = 98f;

            public float CoreHalfWidth = 1.55f;
            public float FeatherWidth = 1.35f;
            public float GlowHalfWidth = 9.0f;

            public Color CoreColor = new Color(0.28f, 0.88f, 1.0f, 0.72f);
            public Color GlowColor = new Color(0.10f, 0.76f, 1.0f, 0.22f);

            public void SetStyle(Color coreCol, Color glowCol, float coreHalfW)
            {
                if (CoreColor == coreCol && GlowColor == glowCol && Mathf.Abs(CoreHalfWidth - coreHalfW) < 0.01f)
                    return;
                CoreColor = coreCol;
                GlowColor = glowCol;
                CoreHalfWidth = coreHalfW;
                SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                Vector2[] leftPts = new Vector2[]
                {
                    new Vector2(-TopGap, HalfHeight),
                    new Vector2(-HalfWidth, CornerY),
                    new Vector2(-HalfWidth, -CornerY),
                    new Vector2(-TopGap, -HalfHeight)
                };

                Vector2[] rightPts = new Vector2[]
                {
                    new Vector2(TopGap, HalfHeight),
                    new Vector2(HalfWidth, CornerY),
                    new Vector2(HalfWidth, -CornerY),
                    new Vector2(TopGap, -HalfHeight)
                };

                // Pass 1: Hào quang neon mềm bên ngoài (Glow Halo)
                EmitPolylinePass(vh, leftPts, 0f, GlowHalfWidth, GlowColor);
                EmitPolylinePass(vh, rightPts, 0f, GlowHalfWidth, GlowColor);

                // Pass 2: Lõi nét sắc khử răng cưa mượt mà (Crisp AA Core)
                EmitPolylinePass(vh, leftPts, CoreHalfWidth, FeatherWidth, CoreColor);
                EmitPolylinePass(vh, rightPts, CoreHalfWidth, FeatherWidth, CoreColor);
            }

            private static void EmitPolylinePass(VertexHelper vh, Vector2[] pts, float innerHalfW, float featherW, Color col)
            {
                int count = pts.Length;
                if (count < 2 || col.a <= 0.002f) return;

                Vector2[] miters = new Vector2[count];
                float[] miterScales = new float[count];

                for (int i = 0; i < count; i++)
                {
                    if (i == 0)
                    {
                        Vector2 t = (pts[1] - pts[0]).normalized;
                        miters[i] = new Vector2(-t.y, t.x);
                        miterScales[i] = 1f;
                    }
                    else if (i == count - 1)
                    {
                        Vector2 t = (pts[i] - pts[i - 1]).normalized;
                        miters[i] = new Vector2(-t.y, t.x);
                        miterScales[i] = 1f;
                    }
                    else
                    {
                        Vector2 t0 = (pts[i] - pts[i - 1]).normalized;
                        Vector2 t1 = (pts[i + 1] - pts[i]).normalized;
                        Vector2 n0 = new Vector2(-t0.y, t0.x);
                        Vector2 n1 = new Vector2(-t1.y, t1.x);
                        Vector2 m = (n0 + n1).normalized;
                        float dot = Vector2.Dot(m, n0);
                        miters[i] = m;
                        miterScales[i] = dot > 0.35f ? (1f / dot) : 1f;
                    }
                }

                float outerHalfW = innerHalfW + featherW;
                Color32 cSolid = col;
                Color32 cEdge = new Color32(cSolid.r, cSolid.g, cSolid.b, 0);
                // Đầu mút mở hở được vuốt mờ nhẹ để trông hiện đại, không cụt lủn
                Color32 cTip = new Color32(cSolid.r, cSolid.g, cSolid.b, (byte)(cSolid.a * 0.55f));

                int baseIdx = vh.currentVertCount;
                for (int i = 0; i < count; i++)
                {
                    Vector2 p = pts[i];
                    Vector2 n = miters[i] * miterScales[i];
                    Color32 centerCol = (i == 0 || i == count - 1) ? cTip : cSolid;

                    if (innerHalfW > 0.01f)
                    {
                        vh.AddVert(p + n * outerHalfW, cEdge, Vector2.zero);
                        vh.AddVert(p + n * innerHalfW, centerCol, Vector2.zero);
                        vh.AddVert(p - n * innerHalfW, centerCol, Vector2.zero);
                        vh.AddVert(p - n * outerHalfW, cEdge, Vector2.zero);
                    }
                    else
                    {
                        vh.AddVert(p + n * outerHalfW, cEdge, Vector2.zero);
                        vh.AddVert(p, centerCol, Vector2.zero);
                        vh.AddVert(p - n * outerHalfW, cEdge, Vector2.zero);
                    }
                }

                int stride = innerHalfW > 0.01f ? 4 : 3;
                for (int i = 0; i < count - 1; i++)
                {
                    int rowA = baseIdx + i * stride;
                    int rowB = baseIdx + (i + 1) * stride;
                    for (int s = 0; s < stride - 1; s++)
                    {
                        vh.AddTriangle(rowA + s, rowB + s, rowB + s + 1);
                        vh.AddTriangle(rowA + s, rowB + s + 1, rowA + s + 1);
                    }
                }
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _mainCam = Camera.main;
            EnsureTrackingUI();
            BuildVisualOccluderCache();
        }

        private void LateUpdate()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            bool hideForUiOrIntro = AboutInfoStation.AnyStationActive
                                    || AboutInfoStation.AnyLandingSequenceActive
                                    || SpawnZoneAnimator.IsAnyIntroWalkActive
                                    || (SpawnZoneAnimator.ActiveInstance != null && SpawnZoneAnimator.ActiveInstance.IsAwaitingEnterBriefing);
            if (hideForUiOrIntro)
            {
                if (_trackingUiRoot != null && _trackingUiRoot.activeSelf)
                    _trackingUiRoot.SetActive(false);
                return;
            }

            if (_reticleUI == null || _reticleUI.Root == null)
            {
                EnsureTrackingUI();
            }
            if (_reticleUI == null) return;

            if (_trackingUiRoot != null && !_trackingUiRoot.activeSelf)
                _trackingUiRoot.SetActive(true);

            UpdateTargetLockingAndTracking();
        }

        private void EnsureTrackingUI()
        {
            if (_trackingUiRoot != null && _reticleUI != null && _reticleUI.Root != null) return;

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas targetCanvas = null;
            foreach (var c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.name == "Canvas")
                {
                    targetCanvas = c;
                    break;
                }
            }
            if (targetCanvas == null)
            {
                foreach (var c in allCanvases)
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        targetCanvas = c;
                        break;
                    }
                }
            }
            if (targetCanvas == null) return;

            _canvas = targetCanvas;

            // Xóa UI cũ nếu có
            var existing = targetCanvas.transform.Find("TargetTracking_HUD_Root");
            while (existing != null)
            {
                DestroyImmediate(existing.gameObject);
                existing = targetCanvas.transform.Find("TargetTracking_HUD_Root");
            }

            // Tắt Crosshair cũ của PlayerController nếu đang mở để tránh chồng chéo
            var oldCrosshair = targetCanvas.transform.Find("CrosshairReticle");
            if (oldCrosshair != null) oldCrosshair.gameObject.SetActive(false);
            foreach (var hudName in new[] { "PlayerCombatHUD", "PlayerSciFiHUD" })
            {
                var hudRoot = targetCanvas.transform.Find(hudName);
                if (hudRoot != null)
                {
                    var ch = hudRoot.Find("CrosshairReticle");
                    if (ch != null) ch.gameObject.SetActive(false);
                }
            }

            _trackingUiRoot = new GameObject("TargetTracking_HUD_Root", typeof(RectTransform));
            _trackingUiRoot.transform.SetParent(targetCanvas.transform, false);

            _hudRootRt = _trackingUiRoot.GetComponent<RectTransform>();
            _hudRootRt.anchorMin = Vector2.zero;
            _hudRootRt.anchorMax = Vector2.one;
            _hudRootRt.pivot = new Vector2(0.5f, 0.5f);
            _hudRootRt.offsetMin = Vector2.zero;
            _hudRootRt.offsetMax = Vector2.zero;

            _reticleUI = BuildHexagonReticleUI(_trackingUiRoot.transform);
        }

        private HexagonLockReticleUI BuildHexagonReticleUI(Transform parent)
        {
            var ui = new HexagonLockReticleUI();

            var rootGo = new GameObject("HexagonLockReticle", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);

            var rt = rootGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(540f, 430f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            ui.Root = rootGo;
            ui.RootRt = rt;

            // Khung lục giác cân đối hài hòa (~gấp 3x bản gọn, 400x296px) và thoáng mép dưới không đè đuôi tàu
            float hexW = 200f;
            float hexH = 148f;
            float topGap = 90f;
            float cornerY = 64f;

            // 1. VIỀN NGOÀI LỤC GIÁC VECTOR KHỬ RĂNG CƯA + HÀO QUANG NEON MỀM (KHÔNG RĂNG CƯA, KHÔNG MẤU GÓC)
            var hexGo = new GameObject("HexVectorFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(HexagonVectorGraphic));
            hexGo.transform.SetParent(rootGo.transform, false);
            var hexRt = hexGo.GetComponent<RectTransform>();
            hexRt.anchorMin = new Vector2(0.5f, 0.5f);
            hexRt.anchorMax = new Vector2(0.5f, 0.5f);
            hexRt.sizeDelta = new Vector2(480f, 360f);
            hexRt.anchoredPosition = Vector2.zero;

            ui.HexGraphic = hexGo.GetComponent<HexagonVectorGraphic>();
            ui.HexGraphic.raycastTarget = false;
            ui.HexGraphic.HalfWidth = hexW;
            ui.HexGraphic.HalfHeight = hexH;
            ui.HexGraphic.CornerY = cornerY;
            ui.HexGraphic.TopGap = topGap;
            ui.HexGraphic.CoreColor = searchColor;
            ui.HexGraphic.GlowColor = new Color(searchColor.r, searchColor.g, searchColor.b, 0.22f);

            // 2. TÂM TRÒN CHÍNH GIỮA MẢNH SẮC NÉT + CHẤM TRÒN PHÁT SÁNG (NO CHUNKY PIXEL)
            var ringGo = new GameObject("CenterRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringGo.transform.SetParent(rootGo.transform, false);
            ui.CenterRing = ringGo.GetComponent<Image>();
            ui.CenterRing.sprite = GetOrCreateCircleSprite();
            ui.CenterRing.color = searchColor;
            ui.CenterRing.raycastTarget = false;
            ui.CenterRingRt = ui.CenterRing.rectTransform;
            ui.CenterRingRt.sizeDelta = new Vector2(44f, 44f);
            ui.CenterRingRt.anchoredPosition = Vector2.zero;

            var dotGo = new GameObject("CenterDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dotGo.transform.SetParent(rootGo.transform, false);
            ui.CenterDot = dotGo.GetComponent<Image>();
            ui.CenterDot.sprite = GetOrCreateDotSprite();
            ui.CenterDot.color = searchColor;
            ui.CenterDot.raycastTarget = false;
            var dotRt = ui.CenterDot.rectTransform;
            dotRt.sizeDelta = new Vector2(8f, 8f);
            dotRt.anchoredPosition = Vector2.zero;

            // 3. NHÃN TRẠNG THÁI CHỮ NỔI VIỀN ĐẬM RÕ NÉT TRÊN MỌI NỀN (KHÔNG CẦN HỘP ĐEN THÔ)
            ui.HeaderText = CreateText("HeaderText", rootGo.transform, "", 15.5f, lockedHeaderColor, TextAlignmentOptions.Center, 5.5f);
            var hRt = ui.HeaderText.rectTransform;
            hRt.anchoredPosition = new Vector2(0f, hexH);
            hRt.sizeDelta = new Vector2(240f, 26f);

            ui.DistText = CreateText("DistText", rootGo.transform, "", 12f, lockedColor, TextAlignmentOptions.Center, 3.5f);
            var dRt = ui.DistText.rectTransform;
            dRt.anchoredPosition = new Vector2(0f, hexH - 21f);
            dRt.sizeDelta = new Vector2(160f, 20f);

            ui.FooterText = CreateText("FooterText", rootGo.transform, "", 12.5f, activeCyanColor, TextAlignmentOptions.Center, 4.5f);
            var fRt = ui.FooterText.rectTransform;
            fRt.anchoredPosition = new Vector2(0f, -hexH);
            fRt.sizeDelta = new Vector2(240f, 22f);

            return ui;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, float charSpacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = charSpacing;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.outlineWidth = 0.24f;
            tmp.outlineColor = new Color32(4, 10, 18, 240);

            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.03f, 0.06f, 0.92f);
            shadow.effectDistance = new Vector2(1.4f, -1.4f);
            return tmp;
        }

        private static Sprite GetOrCreateCircleSprite()
        {
            if (_circleReticleSprite != null) return _circleReticleSprite;

            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float center = size * 0.5f;
            float ringR = size * 0.39f;
            float halfCore = size * 0.018f;   // Vòng tròn mảnh tinh tế ~4.5% đường kính
            float aaEdge = 2.0f;
            float glowSigma = size * 0.055f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float radialErr = Mathf.Abs(dist - ringR);

                    float coreAlpha = 1f - Mathf.Clamp01((radialErr - halfCore) / aaEdge);
                    float glowAlpha = 0.34f * Mathf.Exp(-(radialErr * radialErr) / (2f * glowSigma * glowSigma));
                    float alpha = Mathf.Clamp01(Mathf.Max(coreAlpha, glowAlpha));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _circleReticleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _circleReticleSprite;
        }

        private static Sprite GetOrCreateDotSprite()
        {
            if (_dotReticleSprite != null) return _dotReticleSprite;

            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float center = size * 0.5f;
            float coreR = size * 0.16f;
            float glowSigma = size * 0.22f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float core = 1f - Mathf.Clamp01((dist - coreR) / 1.5f);
                    float glow = 0.45f * Mathf.Exp(-(dist * dist) / (2f * glowSigma * glowSigma));
                    float alpha = Mathf.Clamp01(Mathf.Max(core, glow));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _dotReticleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _dotReticleSprite;
        }

        private void UpdateTargetLockingAndTracking()
        {
            if (_reticleUI == null || _reticleUI.Root == null) return;

            Vector3 camPos = _mainCam.transform.position;
            Vector3 camForward = _mainCam.transform.forward;
            Vector3 shipPos = transform.position;

            Camera cam2D = (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
                ? _canvas.worldCamera : null;

            // 1. TÍNH TOÁN ĐIỂM NGẮM BORE-SIGHT CỦA PHÁO MẶC ĐỊNH
            Vector3 gunAimPoint = shipPos + transform.forward * 90f;
            Vector3 gunSc = _mainCam.WorldToScreenPoint(gunAimPoint);
            Vector2 boreSightLocal = Vector2.zero;
            if (gunSc.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(_hudRootRt, gunSc, cam2D, out Vector2 bPt))
            {
                boreSightLocal = bPt;
            }

            // 2. KIỂM TRA MỤC TIÊU ĐANG KHÓA CỨNG: nếu bị phá hủy, che khuất (LOS), hoặc quá 300m -> HỦY NGAY
            if (_currentLockedTarget != null)
            {
                bool lost = false;
                if (!_currentLockedTarget.gameObject.activeInHierarchy) lost = true;
                else
                {
                    var targetShip = _currentLockedTarget.GetComponent<SpaceTrafficShipAI>();
                    if (targetShip != null && targetShip.IsDestroyed) lost = true;
                    else
                    {
                        float dist = Vector3.Distance(shipPos, _currentLockedTarget.position);
                        Vector3 toTarget = _currentLockedTarget.position - camPos;
                        float angle = Vector3.Angle(camForward, toTarget.normalized);

                        // Mất khóa ngay nếu vượt tầm 200m, lệch quá 45 độ, hoặc bị che khuất tầm nhìn (LOS)
                        if (dist > maxLockDistance || angle > 45f || !HasLineOfSight(camPos, _currentLockedTarget))
                        {
                            lost = true;
                        }
                    }
                }

                if (lost)
                {
                    if (_potentialLockCandidate == _currentLockedTarget)
                    {
                        _potentialLockCandidate = null;
                        _lockHoldTimer = 0f;
                    }
                    _currentLockedTarget = null;
                }
            }

            // 3 & 4. CHỈ KHI CHƯA KHÓA MỤC TIÊU: TÌM DUY NHẤT 1 MỤC TIÊU TRONG VÙNG LỤC GIÁC VÀ KHÓA TRONG 1.0 GIÂY
            Vector2 bestCandidateLocalPt = Vector2.zero;
            float bestCandidateDistShip = 999f;

            if (_currentLockedTarget == null)
            {
                IReadOnlyList<SpaceTrafficShipAI> allShips = SpaceTrafficShipAI.ActiveShips;
                if (allShips == null || allShips.Count == 0)
                {
                    allShips = FindObjectsByType<SpaceTrafficShipAI>(FindObjectsSortMode.None);
                }

                Transform bestCandidateInHex = null;
                float closestDistToReticleSqr = float.MaxValue;

                // Vùng lục giác: Rộng 200px, Cao 148px tính quanh tâm ngắm bore-sight (khớp viền ngoài cân đối)
                float hexW = 200f;
                float hexH = 148f;
                float acquireRange = Mathf.Max(10f, maxLockDistance - 2f); // Chừa đệm 2m tránh nhấp nháy ở mép 200m

                foreach (var ship in allShips)
                {
                    if (ship == null || !ship.gameObject.activeInHierarchy || ship.IsDestroyed) continue;

                    Transform tr = ship.transform;
                    Vector3 toTarget = tr.position - camPos;
                    float distCam = toTarget.magnitude;
                    if (distCam < 3.5f) continue;

                    // Phải ở phía trước Camera
                    if (Vector3.Dot(camForward, toTarget) <= 0f) continue;

                    float distShip = Vector3.Distance(shipPos, tr.position);
                    if (distShip > acquireRange) continue; // Giới hạn 200m theo yêu cầu

                    // KIỂM TRA TẦM NHÌN TRỰC TIẾP (Line of Sight): Không bị bất kỳ đảo/vật cản nào che chắn
                    if (!HasLineOfSight(camPos, tr)) continue;

                    Vector3 screenPt = _mainCam.WorldToScreenPoint(tr.position);
                    if (screenPt.z <= 0f) continue;

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _hudRootRt, screenPt, cam2D, out Vector2 localPt))
                    {
                        // KIỂM TRA MỤC TIÊU NẰM TRỌN TRONG VÙNG LỤC GIÁC (HEXAGON APERTURE):
                        Vector2 delta = localPt - boreSightLocal;
                        float dx = Mathf.Abs(delta.x);
                        float dy = Mathf.Abs(delta.y);

                        bool insideHex = (dx <= hexW && dy <= hexH) &&
                                         ((dx / hexW) + (dy / hexH) <= 1.45f);

                        if (insideHex)
                        {
                            float distSqr = delta.sqrMagnitude;
                            if (distSqr < closestDistToReticleSqr)
                            {
                                closestDistToReticleSqr = distSqr;
                                bestCandidateInHex = tr;
                                bestCandidateLocalPt = localPt;
                                bestCandidateDistShip = distShip;
                            }
                        }
                    }
                }

                // XỬ LÝ KHÓA NHANH TRONG VÒNG ĐÚNG 1 GIÂY (1.0s Hold Lock-On)
                if (bestCandidateInHex != null)
                {
                    if (_potentialLockCandidate == bestCandidateInHex)
                    {
                        _lockHoldTimer += Time.deltaTime;
                    }
                    else
                    {
                        _potentialLockCandidate = bestCandidateInHex;
                        _lockHoldTimer = Time.deltaTime;
                    }

                    if (_lockHoldTimer >= lockHoldDuration)
                    {
                        _currentLockedTarget = _potentialLockCandidate;
                    }
                }
                else
                {
                    // Nếu mục tiêu rời khỏi lục giác trước khi khóa cứng: reset bộ đếm ngay
                    _lockHoldTimer = 0f;
                    _potentialLockCandidate = null;
                }
            }

            // 5. ĐIỀU KHIỂN VỊ TRÍ TÂM LỤC GIÁC (SMOOTH TARGET TRACKING)
            Vector2 targetReticlePos = boreSightLocal;
            float targetDistance = 0f;

            if (_currentLockedTarget != null)
            {
                Vector3 sc = _mainCam.WorldToScreenPoint(_currentLockedTarget.position);
                if (sc.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(_hudRootRt, sc, cam2D, out Vector2 pt))
                {
                    targetReticlePos = pt;
                    targetDistance = Vector3.Distance(shipPos, _currentLockedTarget.position);
                }
            }
            else if (_potentialLockCandidate != null && _lockHoldTimer > 0f)
            {
                targetReticlePos = bestCandidateLocalPt;
                targetDistance = bestCandidateDistShip;
            }

            // Đảm bảo bộ nhớ đệm vật cản thị giác đã bao gồm toàn bộ object sinh ra sau Start()
            if (!_visualOccludersBuilt || (_occludersPostStartPending && Time.timeSinceLevelLoad > 0.4f))
            {
                if (_visualOccludersBuilt) _occludersPostStartPending = false;
                BuildVisualOccluderCache();
            }

            // Di chuyển tâm lục giác cực mượt bám theo mũi ngắm hoặc mục tiêu (SmoothDamp triệt tiêu giật khung hình)
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            _reticleUI.RootRt.anchoredPosition = Vector2.SmoothDamp(
                _reticleUI.RootRt.anchoredPosition,
                targetReticlePos,
                ref _reticleSmoothVel,
                0.036f,
                Mathf.Infinity,
                dt);

            // 6. CẬP NHẬT GIAO DIỆN & MÀU SẮC THEO 3 TRẠNG THÁI (LOCKED, LOCKING 1S, SEARCHING)
            bool isLocked = _currentLockedTarget != null;
            bool isLocking = !isLocked && _potentialLockCandidate != null && _lockHoldTimer > 0f;

            Color activeFrameColor;
            Color coreColor;
            Color glowColor;
            float coreHalfW;

            if (isLocked)
            {
                // ĐÃ KHÓA CỨNG: Lõi sáng trắng-vàng ấm + hào quang cam hổ phách mượt mà
                activeFrameColor = lockedColor;
                coreColor = Color.Lerp(lockedColor, lockedHeaderColor, 0.62f);
                glowColor = new Color(lockedColor.r, lockedColor.g * 0.88f, lockedColor.b * 0.65f, 0.50f);
                coreHalfW = 1.80f;

                float pulse = 1f + 0.025f * Mathf.Sin(Time.time * 11f);
                _reticleUI.RootRt.localScale = new Vector3(pulse, pulse, 1f);

                _reticleUI.HeaderText.text = "TARGET LOCKED";
                _reticleUI.HeaderText.color = lockedHeaderColor;

                _reticleUI.FooterText.text = "WEAPON SYS: ACTIVE.";
                _reticleUI.FooterText.color = activeCyanColor;

                _reticleUI.DistText.text = targetDistance < 1000f ? $"[ {targetDistance:F0}M ]" : $"[ {(targetDistance / 1000f):F2}KM ]";
                _reticleUI.DistText.color = Color.Lerp(lockedColor, lockedHeaderColor, 0.35f);
            }
            else if (isLocking)
            {
                // ĐANG KHÓA NHANH 1 GIÂY (LOCKING COUNTDOWN - co nhẹ khung vào mục tiêu)
                float progress = Mathf.Clamp01(_lockHoldTimer / lockHoldDuration);
                float timeLeft = Mathf.Max(0f, lockHoldDuration - _lockHoldTimer);

                activeFrameColor = Color.Lerp(lockingColor, lockedColor, progress);
                coreColor = Color.Lerp(activeFrameColor, lockedHeaderColor, 0.25f + progress * 0.30f);
                glowColor = new Color(activeFrameColor.r, activeFrameColor.g, activeFrameColor.b, 0.32f + progress * 0.14f);
                coreHalfW = Mathf.Lerp(1.50f, 1.75f, progress);

                float shrink = Mathf.Lerp(1.06f, 1.00f, progress);
                _reticleUI.RootRt.localScale = new Vector3(shrink, shrink, 1f);

                _reticleUI.HeaderText.text = $"LOCKING  {timeLeft:F1}s";
                _reticleUI.HeaderText.color = lockingColor;

                _reticleUI.FooterText.text = "ACQUIRING...";
                _reticleUI.FooterText.color = Color.Lerp(lockingColor, activeCyanColor, progress * 0.4f);

                _reticleUI.DistText.text = targetDistance < 1000f ? $"[ {targetDistance:F0}M ]" : $"[ {(targetDistance / 1000f):F2}KM ]";
                _reticleUI.DistText.color = activeFrameColor;
            }
            else
            {
                // TRẠNG THÁI TỰ DO: Tối giản tuyệt đối - chỉ hiển thị viền ngoài lục giác thanh mảnh + tâm tròn
                activeFrameColor = searchColor;
                coreColor = searchColor;
                glowColor = new Color(searchColor.r, searchColor.g, searchColor.b, 0.22f);
                coreHalfW = 1.40f;
                _reticleUI.RootRt.localScale = Vector3.one;

                _reticleUI.HeaderText.text = "";
                _reticleUI.FooterText.text = "";
                _reticleUI.DistText.text = "";
            }

            if (_reticleUI.HexGraphic != null)
            {
                _reticleUI.HexGraphic.SetStyle(coreColor, glowColor, coreHalfW);
            }

            _reticleUI.CenterRing.color = coreColor;
            _reticleUI.CenterDot.color = coreColor;
        }

        private bool _occludersPostStartPending = true;

        /// <summary>
        /// Thu thập toàn bộ khối hình học mờ đục (Opaque MeshRenderer không có Collider)
        /// như bảng hiệu STARPORT ALPHA // SPAWN HUB, tinh thể đảo, tường công trình... để chặn khóa xuyên vật thể.
        /// </summary>
        public void BuildVisualOccluderCache()
        {
            _occluderGroups.Clear();
            var groupMap = new Dictionary<Transform, OccluderGroup>();

            MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                MeshRenderer mr = allRenderers[i];
                if (mr == null || !mr.enabled || !mr.gameObject.activeInHierarchy) continue;

                // Bỏ qua tàu người chơi, camera và tàu AI di chuyển (đã có collider vật lý)
                if (mr.transform == transform || mr.transform.IsChildOf(transform)) continue;
                if (_mainCam != null && (mr.transform == _mainCam.transform || mr.transform.IsChildOf(_mainCam.transform))) continue;
                if (mr.GetComponentInParent<SpaceTrafficShipAI>() != null) continue;
                if (mr.GetComponent<TMP_Text>() != null) continue;

                // Bỏ qua vật thể đã có Collider vật lý solid (đã được Physics.RaycastAll kiểm tra)
                Collider existingCol = mr.GetComponent<Collider>();
                if (existingCol != null && existingCol.enabled && !existingCol.isTrigger) continue;

                // Chỉ xét vật liệu đặc (Opaque renderQueue < 2500), bỏ qua kính trong suốt / scanlines / FX
                Material mat = mr.sharedMaterial;
                if (mat == null || mat.renderQueue >= 2500) continue;
                if (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") > 0.5f) continue;

                Vector3 worldSize = mr.bounds.size;
                float maxDim = Mathf.Max(worldSize.x, Mathf.Max(worldSize.y, worldSize.z));
                if (maxDim < 1.5f) continue;

                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                Mesh mesh = mf.sharedMesh;
                MeshTriData triData;
                bool hasExactMesh = false;

                if (!_meshTriCache.TryGetValue(mesh, out triData))
                {
                    Bounds b = mesh.bounds;
                    Vector3 ext = b.extents;
                    // Đệm tối thiểu cho các mặt phẳng mỏng như Quad để local bounds luôn cắt được tia
                    ext.x = Mathf.Max(ext.x, 0.03f);
                    ext.y = Mathf.Max(ext.y, 0.03f);
                    ext.z = Mathf.Max(ext.z, 0.03f);

                    triData = new MeshTriData
                    {
                        PaddedLocalBounds = new Bounds(b.center, ext * 2f)
                    };

                    if (mesh.isReadable && mesh.triangles.Length <= 2400) // <= 800 tam giác mỗi mesh
                    {
                        triData.Vertices = mesh.vertices;
                        triData.Triangles = mesh.triangles;
                    }
                    _meshTriCache[mesh] = triData;
                }

                hasExactMesh = triData.Vertices != null && triData.Triangles != null;

                Transform root = mr.transform.root;
                if (!groupMap.TryGetValue(root, out OccluderGroup grp))
                {
                    grp = new OccluderGroup { WorldBounds = mr.bounds };
                    groupMap[root] = grp;
                    _occluderGroups.Add(grp);
                }
                else
                {
                    grp.WorldBounds.Encapsulate(mr.bounds);
                }

                grp.Items.Add(new OpaqueVisualOccluder
                {
                    Renderer = mr,
                    GameObject = mr.gameObject,
                    MeshData = triData,
                    HasExactMesh = hasExactMesh
                });
            }

            // Mở rộng nhẹ bao giới hạn của từng cụm phòng trường hợp có bộ phận xoay nhẹ (turret/door)
            for (int g = 0; g < _occluderGroups.Count; g++)
            {
                _occluderGroups[g].WorldBounds.Expand(4f);
            }

            _visualOccludersBuilt = true;
        }

        private bool IsBlockedByVisualOccluders(Vector3 start, Vector3 dir, float maxDist)
        {
            if (!_visualOccludersBuilt || maxDist <= 0.1f) return false;

            Ray worldRay = new Ray(start, dir);
            Vector3 segDelta = dir * maxDist;

            for (int g = 0; g < _occluderGroups.Count; g++)
            {
                OccluderGroup grp = _occluderGroups[g];
                if (!grp.WorldBounds.IntersectRay(worldRay, out float grpDist) || grpDist > maxDist)
                    continue;

                List<OpaqueVisualOccluder> items = grp.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    OpaqueVisualOccluder occ = items[i];
                    if (occ.Renderer == null || !occ.Renderer.enabled || !occ.GameObject.activeInHierarchy)
                        continue;

                    if (!occ.Renderer.bounds.IntersectRay(worldRay, out float wDist) || wDist > maxDist)
                        continue;

                    Matrix4x4 invMat = occ.Renderer.transform.worldToLocalMatrix;
                    Vector3 lOrig = invMat.MultiplyPoint3x4(start);
                    Vector3 lDelta = invMat.MultiplyVector(segDelta);
                    float lMaxDist = lDelta.magnitude;
                    if (lMaxDist < 1e-5f) continue;

                    Vector3 lDir = lDelta / lMaxDist;
                    Ray localRay = new Ray(lOrig, lDir);

                    if (!occ.MeshData.PaddedLocalBounds.IntersectRay(localRay, out float bDist) || bDist > lMaxDist)
                        continue;

                    if (!occ.HasExactMesh)
                    {
                        return true;
                    }

                    // Kiểm tra va chạm tia-tam giác chính xác tuyệt đối (Möller–Trumbore hai mặt)
                    Vector3[] verts = occ.MeshData.Vertices;
                    int[] tris = occ.MeshData.Triangles;
                    for (int t = 0; t + 2 < tris.Length; t += 3)
                    {
                        Vector3 v0 = verts[tris[t]];
                        Vector3 v1 = verts[tris[t + 1]];
                        Vector3 v2 = verts[tris[t + 2]];

                        Vector3 e1 = v1 - v0;
                        Vector3 e2 = v2 - v0;
                        Vector3 h = Vector3.Cross(lDir, e2);
                        float a = Vector3.Dot(e1, h);
                        if (a > -1e-6f && a < 1e-6f) continue;

                        float f = 1f / a;
                        Vector3 s = lOrig - v0;
                        float u = f * Vector3.Dot(s, h);
                        if (u < -0.001f || u > 1.001f) continue;

                        Vector3 q = Vector3.Cross(s, e1);
                        float v = f * Vector3.Dot(lDir, q);
                        if (v < -0.001f || u + v > 1.001f) continue;

                        float hitT = f * Vector3.Dot(e2, q);
                        if (hitT > 0.001f && hitT <= lMaxDist)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra tầm nhìn trực tiếp (Line of Sight) từ Camera và từ Tàu người chơi đến tàu địch.
        /// Trả về false nếu mục tiêu bị đảo, đất đá, bảng hiệu SPAWN HUB, công trình, tàu mẹ hay tàu khác che khuất,
        /// hoặc tàu đã bị tiêu diệt / ẩn.
        /// </summary>
        public bool HasLineOfSight(Vector3 camPos, Transform target)
        {
            if (target == null) return false;
            if (!target.gameObject.activeInHierarchy) return false;

            var shipAi = target.GetComponent<SpaceTrafficShipAI>();
            if (shipAi != null && shipAi.IsDestroyed) return false;

            Vector3 targetPos = target.position;
            Vector3 toTargetCam = targetPos - camPos;
            float distCam = toTargetCam.magnitude;
            if (distCam < 1.5f) return true;

            Vector3 dirCam = toTargetCam / distCam;
            float checkDist = Mathf.Max(0.5f, distCam - 1.2f);

            // 1. Raycast tất cả Physics Collider từ Camera tới target
            RaycastHit[] hits = Physics.RaycastAll(camPos, dirCam, checkDist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.isTrigger) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (col.transform == target || col.transform.IsChildOf(target)) continue;
                if (_mainCam != null && (col.transform == _mainCam.transform || col.transform.IsChildOf(_mainCam.transform))) continue;

                return false;
            }

            // 2. SphereCastAll bán kính 0.25m từ Camera (không bị dừng bởi chính tàu người chơi)
            RaycastHit[] sphereHits = Physics.SphereCastAll(camPos, 0.25f, dirCam, checkDist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < sphereHits.Length; i++)
            {
                Collider sc = sphereHits[i].collider;
                if (sc == null || sc.isTrigger) continue;
                if (sc.transform == transform || sc.transform.IsChildOf(transform)) continue;
                if (sc.transform == target || sc.transform.IsChildOf(target)) continue;
                if (_mainCam != null && (sc.transform == _mainCam.transform || sc.transform.IsChildOf(_mainCam.transform))) continue;

                return false;
            }

            // 3. Kiểm tra các khối hình học đặc không có Collider (bảng Welcome SPAWN HUB, đá, tinh thể, công trình)
            if (IsBlockedByVisualOccluders(camPos, dirCam, checkDist))
            {
                return false;
            }

            // 4. Kiểm tra bổ sung từ tàu người chơi đến mục tiêu (ngăn góc nhìn camera lệch thấy tàu sau tường)
            Vector3 shipPos = transform.position;
            Vector3 toTargetShip = targetPos - shipPos;
            float distShip = toTargetShip.magnitude;
            if (distShip > 2.5f)
            {
                Vector3 dirShip = toTargetShip / distShip;
                float checkShipDist = Mathf.Max(0.5f, distShip - 1.2f);
                RaycastHit[] shipHits = Physics.RaycastAll(shipPos, dirShip, checkShipDist, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < shipHits.Length; i++)
                {
                    Collider sc = shipHits[i].collider;
                    if (sc == null || sc.isTrigger) continue;
                    if (sc.transform == transform || sc.transform.IsChildOf(transform)) continue;
                    if (sc.transform == target || sc.transform.IsChildOf(target)) continue;
                    if (_mainCam != null && (sc.transform == _mainCam.transform || sc.transform.IsChildOf(_mainCam.transform))) continue;

                    return false;
                }

                if (IsBlockedByVisualOccluders(shipPos, dirShip, checkShipDist))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Lấy tọa độ mục tiêu đang bị khóa cứng để pháo laser tự động hội tụ bắn trúng
        /// </summary>
        public bool TryGetLockAimPoint(out Vector3 aimPoint)
        {
            if (_currentLockedTarget != null)
            {
                if (!_currentLockedTarget.gameObject.activeInHierarchy)
                {
                    _currentLockedTarget = null;
                    aimPoint = Vector3.zero;
                    return false;
                }

                var shipAi = _currentLockedTarget.GetComponent<SpaceTrafficShipAI>();
                if (shipAi != null && shipAi.IsDestroyed)
                {
                    _currentLockedTarget = null;
                    aimPoint = Vector3.zero;
                    return false;
                }

                var rb = _currentLockedTarget.GetComponent<Rigidbody>();
                Vector3 lead = Vector3.zero;
                if (rb != null)
                {
                    float dist = Vector3.Distance(transform.position, _currentLockedTarget.position);
                    float travelTime = dist / 125f;
                    lead = rb.linearVelocity * (travelTime * 0.7f);
                }
                aimPoint = _currentLockedTarget.position + lead;
                return true;
            }
            aimPoint = Vector3.zero;
            return false;
        }
    }
}
