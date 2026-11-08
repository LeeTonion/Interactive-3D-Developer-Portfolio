using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Biển Quảng Cáo & Màn Hình Công Nghệ Cyberpunk 2 Mặt (Dual-Sided Cyberpunk Billboard Display).
    /// - Hiển thị 2 mặt (Front & Back) sắc nét 360 độ, không bao giờ bị ngược chữ hay bệt xám.
    /// - Tự động chạy trong cả Edit Mode lẫn Play Mode ([ExecuteAlways]).
    /// - 4 Kênh nội dung Sci-Fi: Dự án nổi bật (Project Spotlight), Megacorp Ads, Hacker Terminal Matrix.
    /// - Tự động chuyển màu đỏ rực báo động chiến sự (Fleet Combat Alert) khi có tiếng súng.
    /// - Khi bị người chơi bắn laser: Chập điện tóe lửa, màn hình glitch sọc và tự động Reboot!
    /// </summary>
    [ExecuteAlways]
    public class CyberpunkBillboardDisplay : MonoBehaviour
    {
        [Header("Cấu Hình Biển Quảng Cáo")]
        public string billboardTitle = "PROJECT_ZONE // SECTOR_01";
        public float channelDuration = 7.0f;

        [Header("Màu Sắc Cyberpunk")]
        public Color normalThemeColor = new Color(0f, 0.95f, 1f, 1f);      // Cyan
        public Color accentThemeColor = new Color(1f, 0.2f, 0.6f, 1f);      // Neon Magenta
        public Color alertThemeColor  = new Color(1f, 0.08f, 0.12f, 1f);    // Blood Red

        private class BillboardFaceUI
        {
            public GameObject root;
            public Image bgImage;
            public Image borderFrame;
            public Image scanlinesImage;
            public TMP_Text headerText;
            public TMP_Text titleText;
            public TMP_Text bodyText;
            public TMP_Text badgeText;
            public TMP_Text footerText;
            public TMP_Text matrixTerminalText;
            public GameObject rebootPanel;
            public TMP_Text rebootText;
            public Image rebootProgressBar;
        }

        private readonly List<BillboardFaceUI> _faces = new List<BillboardFaceUI>();

        // Trạng thái hiển thị
        private int _currentChannel = 0;
        private float _channelTimer = 0f;
        private bool _isRebooting = false;
        private int _projectIndex = 0;
        private float _matrixTimer = 0f;
        private readonly StringBuilder _matrixSb = new StringBuilder();

        // Danh sách dự án trong Portfolio
        private static readonly (string title, string tags, string desc)[] Projects = new[]
        {
            ("3D INTERACTIVE PORTFOLIO", "[UNITY 6 // URP // HLSL]", "Kham pha the gioi khong gian Cyberpunk 3D day song dong voi vat ly phi thuyen, AI ham doi & tram ket noi docking."),
            ("CYBER ACTION RPG", "[C# // COMBAT FSM // HITBOX]", "He thong chien dau hanh dong chat che: Combo da don, ne don the luc & phan ung va cham chan thuc."),
            ("DRIFT RACER 3D", "[RAYCAST PHYSICS // TIRE SMOKE]", "Co che lai xe arcade dinh cao voi tinh diem drift, khoi bui phan luc & vat ly ma sat lop xe."),
            ("THREE.JS WEBGL SIM", "[WEBGL // GLSL // AUDIO-REACT]", "Mo phong do hoa trinh duyet thoi gian thuc voi Shader procedural, bloom & phan xa am thanh song dong."),
            ("SPACE COMBAT SIMULATOR", "[FLOCKING AI // VOID LASERS]", "Khong chien ham doi quy mo lon, pha huy loi nang luong Carrier & chien dau vu tru toc do cao.")
        };

        // Quảng cáo Megacorp Sci-Fi
        private static readonly (string brand, string slogan, string details, string stat)[] MegacorpAds = new[]
        {
            ("HYPERION AEROSPACE", "TACHYON-9 WARP DRIVE", "Dong co be cong khong thoi gian the he moi. Vuot qua van toc anh sang trong 3.2 giay.", "CORE OUTPUT: 104.8% [OVERDRIVE]"),
            ("CYBERSHIELD DEFENSE", "TACTICAL DEFLECTOR MATRIX", "Mang chan truong luc hap thu 99.9% tia laser & va cham thien thach.", "SHIELD INTEGRITY: 100% MAXIMUM"),
            ("NEO-TOKYO CYBERNETICS", "NEURAL AVIONICS COCKPIT", "Ket noi truc tiep song nao vao he thong dieu khien tau chien khong gian.", "NEURAL LATENCY: 0.12 ms [ULTRA-FAST]")
        };

        private void Awake()
        {
            EnsureFacesBuilt();
        }

        private void Start()
        {
            EnsureFacesBuilt();
        }

        private void OnEnable()
        {
            EnsureFacesBuilt();
        }

        public void EnsureFacesBuilt()
        {
            SetupMaterials();

            // Nếu đã tạo 2 mặt rồi thì chỉ cập nhật nội dung
            if (_faces.Count == 2 && _faces[0].root != null && _faces[1].root != null)
            {
                UpdateActiveChannel();
                return;
            }

            // Dọn dẹp canvas cũ nếu có
            _faces.Clear();
            var oldCanvases = GetComponentsInChildren<Canvas>(true);
            foreach (var c in oldCanvases)
            {
                if (Application.isPlaying) Destroy(c.gameObject);
                else DestroyImmediate(c.gameObject);
            }

            // Mặt trước (+Z): localRotation = Euler(0, 180, 0)
            _faces.Add(CreateFace("Billboard_Canvas_Front", new Vector3(0.046f, 0.07f, 0.022f), Quaternion.Euler(0f, 180f, 0f)));
            // Mặt sau (-Z): localRotation = Euler(0, 0, 0)
            _faces.Add(CreateFace("Billboard_Canvas_Back", new Vector3(0.046f, 0.07f, -0.042f), Quaternion.Euler(0f, 0f, 0f)));

            // Đảm bảo có BoxCollider trên Billboard để nhận đạn của người chơi
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var bc = gameObject.AddComponent<BoxCollider>();
                bc.size = new Vector3(4.5f, 3.8f, 0.5f);
            }

            UpdateActiveChannel();
        }

        private void SetupMaterials()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                var mats = mr.sharedMaterials;
                if (mats != null && mats.Length >= 2)
                {
                    // Submesh 0: Screen face
                    Material screenFaceMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    screenFaceMat.name = "Mat_Billboard_ScreenFace_Dynamic";
                    screenFaceMat.color = new Color(0.02f, 0.04f, 0.08f, 1f);
                    screenFaceMat.EnableKeyword("_EMISSION");
                    screenFaceMat.SetColor("_EmissionColor", new Color(0.05f, 0.15f, 0.25f));

                    // Submesh 1: Casing Frame -> Kim loại xám đen cao cấp
                    Material frameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    frameMat.name = "Mat_Billboard_Frame_DarkMetal";
                    frameMat.color = new Color(0.12f, 0.14f, 0.18f, 1f);
                    frameMat.SetFloat("_Metallic", 0.85f);
                    frameMat.SetFloat("_Smoothness", 0.65f);

                    mr.materials = new Material[] { screenFaceMat, frameMat };
                }
            }
        }

        private BillboardFaceUI CreateFace(string faceName, Vector3 localPos, Quaternion localRot)
        {
            var face = new BillboardFaceUI();

            GameObject canvasGo = new GameObject(faceName);
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = localPos;
            canvasGo.transform.localRotation = localRot;
            canvasGo.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(780f, 600f);

            face.root = canvasGo;

            // 1. Background Panel
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            face.bgImage = bgGo.AddComponent<Image>();
            face.bgImage.color = new Color(0.02f, 0.04f, 0.08f, 0.96f);

            // 2. Glowing Neon Border Frame
            GameObject borderGo = new GameObject("Border_Frame");
            borderGo.transform.SetParent(canvasGo.transform, false);
            var borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one; borderRt.sizeDelta = new Vector2(-16f, -16f);
            face.borderFrame = borderGo.AddComponent<Image>();
            face.borderFrame.color = new Color(normalThemeColor.r, normalThemeColor.g, normalThemeColor.b, 0.45f);

            // 3. Header Bar
            GameObject headerGo = new GameObject("Header_Text");
            headerGo.transform.SetParent(canvasGo.transform, false);
            var headerRt = headerGo.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f); headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f); headerRt.anchoredPosition = new Vector2(0f, -18f);
            headerRt.sizeDelta = new Vector2(730f, 32f);
            face.headerText = headerGo.AddComponent<TextMeshProUGUI>();
            face.headerText.fontSize = 17f; face.headerText.fontStyle = FontStyles.Bold;
            face.headerText.alignment = TextAlignmentOptions.MidlineLeft;
            face.headerText.color = normalThemeColor;
            face.headerText.text = $"[ {billboardTitle} // BROADCAST NETWORK ]";

            // 4. Badge / Category Tag
            GameObject badgeGo = new GameObject("Badge_Text");
            badgeGo.transform.SetParent(canvasGo.transform, false);
            var badgeRt = badgeGo.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 1f); badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f); badgeRt.anchoredPosition = new Vector2(0f, -56f);
            badgeRt.sizeDelta = new Vector2(730f, 26f);
            face.badgeText = badgeGo.AddComponent<TextMeshProUGUI>();
            face.badgeText.fontSize = 15f; face.badgeText.fontStyle = FontStyles.Bold;
            face.badgeText.color = accentThemeColor;
            face.badgeText.text = ">> PROJECT HIGHLIGHT // SYSTEM SHOWCASE <<";

            // 5. Main Title Text
            GameObject titleGo = new GameObject("Title_Text");
            titleGo.transform.SetParent(canvasGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 75f);
            titleRt.sizeDelta = new Vector2(730f, 55f);
            face.titleText = titleGo.AddComponent<TextMeshProUGUI>();
            face.titleText.fontSize = 30f; face.titleText.fontStyle = FontStyles.Bold;
            face.titleText.alignment = TextAlignmentOptions.TopLeft;
            face.titleText.color = Color.white;
            face.titleText.text = "3D INTERACTIVE PORTFOLIO";

            // 6. Body Text
            GameObject bodyGo = new GameObject("Body_Text");
            bodyGo.transform.SetParent(canvasGo.transform, false);
            var bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchoredPosition = new Vector2(0f, -25f);
            bodyRt.sizeDelta = new Vector2(730f, 130f);
            face.bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            face.bodyText.fontSize = 17f;
            face.bodyText.color = new Color(0.85f, 0.92f, 1f, 0.95f);
            face.bodyText.text = "Kham pha the gioi khong gian Cyberpunk 3D.";

            // 7. Matrix Hacker Terminal Feed
            GameObject matrixGo = new GameObject("Matrix_Terminal_Text");
            matrixGo.transform.SetParent(canvasGo.transform, false);
            var matrixRt = matrixGo.AddComponent<RectTransform>();
            matrixRt.anchoredPosition = new Vector2(0f, -25f);
            matrixRt.sizeDelta = new Vector2(730f, 170f);
            face.matrixTerminalText = matrixGo.AddComponent<TextMeshProUGUI>();
            face.matrixTerminalText.fontSize = 16f;
            face.matrixTerminalText.color = new Color(0.2f, 1f, 0.5f, 0.95f);
            face.matrixTerminalText.gameObject.SetActive(false);

            // 8. Footer Bar
            GameObject footerGo = new GameObject("Footer_Text");
            footerGo.transform.SetParent(canvasGo.transform, false);
            var footerRt = footerGo.AddComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f); footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f); footerRt.anchoredPosition = new Vector2(0f, 20f);
            footerRt.sizeDelta = new Vector2(730f, 30f);
            face.footerText = footerGo.AddComponent<TextMeshProUGUI>();
            face.footerText.fontSize = 15f; face.footerText.fontStyle = FontStyles.Bold;
            face.footerText.alignment = TextAlignmentOptions.Center;
            face.footerText.color = normalThemeColor;
            face.footerText.text = ">>> DOCK AT ABOUT STATION (CARRIER SECTOR) TO LAUNCH DEMOS <<<";

            // 9. Scanline Overlay Effect
            GameObject scanlineGo = new GameObject("Scanline_Overlay");
            scanlineGo.transform.SetParent(canvasGo.transform, false);
            var scanlineRt = scanlineGo.AddComponent<RectTransform>();
            scanlineRt.anchorMin = Vector2.zero; scanlineRt.anchorMax = Vector2.one; scanlineRt.sizeDelta = Vector2.zero;
            face.scanlinesImage = scanlineGo.AddComponent<Image>();
            face.scanlinesImage.color = new Color(0f, 0.95f, 1f, 0.06f);

            // 10. Reboot Overlay Panel
            face.rebootPanel = new GameObject("Reboot_Panel");
            face.rebootPanel.transform.SetParent(canvasGo.transform, false);
            var rebootRt = face.rebootPanel.AddComponent<RectTransform>();
            rebootRt.anchorMin = Vector2.zero; rebootRt.anchorMax = Vector2.one; rebootRt.sizeDelta = Vector2.zero;
            var rebootBg = face.rebootPanel.AddComponent<Image>();
            rebootBg.color = new Color(0.08f, 0.02f, 0.03f, 0.96f);

            GameObject rebootTxtGo = new GameObject("Reboot_Text");
            rebootTxtGo.transform.SetParent(face.rebootPanel.transform, false);
            var rtTxt = rebootTxtGo.AddComponent<RectTransform>();
            rtTxt.anchoredPosition = new Vector2(0f, 35f);
            rtTxt.sizeDelta = new Vector2(700f, 90f);
            face.rebootText = rebootTxtGo.AddComponent<TextMeshProUGUI>();
            face.rebootText.fontSize = 24f; face.rebootText.fontStyle = FontStyles.Bold;
            face.rebootText.alignment = TextAlignmentOptions.Center;
            face.rebootText.color = alertThemeColor;
            face.rebootText.text = "⚠ HARDWARE IMPACT DETECTED ⚠\nSIGNAL INTERRUPTED • REBOOTING SYSTEM...";

            GameObject progGo = new GameObject("Progress_Bar");
            progGo.transform.SetParent(face.rebootPanel.transform, false);
            var progRt = progGo.AddComponent<RectTransform>();
            progRt.anchoredPosition = new Vector2(0f, -35f);
            progRt.sizeDelta = new Vector2(460f, 16f);
            face.rebootProgressBar = progGo.AddComponent<Image>();
            face.rebootProgressBar.color = normalThemeColor;

            face.rebootPanel.SetActive(false);

            return face;
        }

        private void Update()
        {
            if (_faces.Count == 0 || _faces[0].root == null)
            {
                EnsureFacesBuilt();
                return;
            }

            if (_isRebooting) return;

            // 1. Kiểm tra Báo động Chiến sự toàn hạm đội
            bool isAlert = SpaceTrafficShipAI.IsFleetHostile;
            if (isAlert)
            {
                DisplayAlertState();
                return;
            }

            // 2. Chu kỳ chuyển kênh hiển thị bình thường
            if (Application.isPlaying)
            {
                _channelTimer += Time.deltaTime;
                if (_channelTimer >= channelDuration)
                {
                    _channelTimer = 0f;
                    _currentChannel = (_currentChannel + 1) % 3;
                    StartCoroutine(GlitchTransitionRoutine());
                }
            }

            // Cập nhật nội dung kênh hiện tại
            UpdateActiveChannel();

            // Hiệu ứng thở nhẹ của khung viền neon
            float pulse = 0.40f + Mathf.Sin(Time.time * 3f) * 0.15f;
            foreach (var f in _faces)
            {
                if (f.borderFrame != null)
                {
                    f.borderFrame.color = new Color(normalThemeColor.r, normalThemeColor.g, normalThemeColor.b, pulse);
                }
            }
        }

        private void UpdateActiveChannel()
        {
            float stationTime = Time.time;
            string timeStr = $"{(int)(stationTime / 60):00}:{(int)(stationTime % 60):00}";

            foreach (var f in _faces)
            {
                if (f == null || f.root == null) continue;

                if (_currentChannel == 0)
                {
                    // KÊNH 1: DỰ ÁN NỔI BẬT (PROJECT SPOTLIGHT)
                    f.titleText.gameObject.SetActive(true);
                    f.bodyText.gameObject.SetActive(true);
                    f.badgeText.gameObject.SetActive(true);
                    f.matrixTerminalText.gameObject.SetActive(false);

                    var p = Projects[_projectIndex];
                    f.headerText.text = $"[ {billboardTitle} // CH-01: PROJECT SPOTLIGHT ]  <color=#00e5ff>● LIVE {timeStr}</color>";
                    f.badgeText.text = $">> {p.tags} <<";
                    f.titleText.color = normalThemeColor;
                    f.titleText.text = p.title;
                    f.bodyText.text = $"{p.desc}\n\n<color=#ff0077>TECH:</color> Real-time Physics • Custom Shaders • Dynamic Camera Rig";
                    f.footerText.text = ">>> DOCK AT ABOUT STATION (CARRIER SECTOR) TO LAUNCH DEMOS <<<";
                    f.footerText.color = normalThemeColor;
                }
                else if (_currentChannel == 1)
                {
                    // KÊNH 2: QUẢNG CÁO MEGACORP SCI-FI
                    f.titleText.gameObject.SetActive(true);
                    f.bodyText.gameObject.SetActive(true);
                    f.badgeText.gameObject.SetActive(true);
                    f.matrixTerminalText.gameObject.SetActive(false);

                    int adIdx = Mathf.FloorToInt(Time.time * 0.25f) % MegacorpAds.Length;
                    var ad = MegacorpAds[adIdx];

                    f.headerText.text = $"[ {billboardTitle} // CH-02: MEGACORP BROADCAST ]  <color=#ff0077>● AD {timeStr}</color>";
                    f.badgeText.text = $">> SPONSORED: {ad.brand} <<";
                    f.titleText.color = accentThemeColor;
                    f.titleText.text = ad.slogan;
                    f.bodyText.text = $"{ad.details}\n\n<color=#00e5ff>{ad.stat}</color>";
                    f.footerText.text = ">>> POWERED BY CODE-DRIVE AUTONOMOUS PROPULSION NETWORK <<<";
                    f.footerText.color = accentThemeColor;
                }
                else
                {
                    // KÊNH 3: HACKER TERMINAL & CARRIER TELEMETRY MATRIX
                    f.titleText.gameObject.SetActive(false);
                    f.bodyText.gameObject.SetActive(false);
                    f.badgeText.gameObject.SetActive(false);
                    f.matrixTerminalText.gameObject.SetActive(true);

                    f.headerText.text = $"[ {billboardTitle} // CH-03: CARRIER TELEMETRY ]  <color=#00ff88>● ROOT {timeStr}</color>";
                    UpdateMatrixTerminalText(f.matrixTerminalText);
                    f.footerText.text = ">>> REAL-TIME VOID TELEMETRY FEED • 360° RADAR SWEEP ACTIVE <<<";
                    f.footerText.color = new Color(0.2f, 1f, 0.5f);
                }
            }
        }

        private void UpdateMatrixTerminalText(TMP_Text targetText)
        {
            _matrixTimer += Time.deltaTime;
            if (_matrixTimer > 0.12f)
            {
                _matrixTimer = 0f;
                _matrixSb.Clear();
                _matrixSb.AppendLine("<color=#00ff88>CARRIER_ROOT@SECTOR-01:~$ netstat -void</color>");
                _matrixSb.AppendLine($"0x7FFD0420: {Random.Range(10, 99):X2} {Random.Range(10, 99):X2} {Random.Range(10, 99):X2} [TELEMETRY_LINK_OK]");
                _matrixSb.AppendLine($"0x7FFD0430: {Random.Range(10, 99):X2} {Random.Range(10, 99):X2} {Random.Range(10, 99):X2} [CARRIER_CORE: 4,820 K]");
                _matrixSb.AppendLine($"0x7FFD0440: {Random.Range(10, 99):X2} {Random.Range(10, 99):X2} {Random.Range(10, 99):X2} [DEFENSE_DRONES: READY]");
                _matrixSb.AppendLine($"RADAR SWEEP: 360° SCANNING // HOSTILE SHIPS: 0 // CARRIER WINGMEN: 2");
                _matrixSb.AppendLine($"FIREWALL PROTOCOL: AES-4096 QUANTUM ENCRYPTION [SECURED]");
            }
            if (targetText != null && _matrixSb.Length > 0)
            {
                targetText.text = _matrixSb.ToString();
            }
        }

        private void DisplayAlertState()
        {
            bool flash = (Mathf.FloorToInt(Time.time * 4f) % 2 == 0);
            Color alertCol = flash ? alertThemeColor : new Color(1f, 0.45f, 0.1f);

            foreach (var f in _faces)
            {
                if (f == null || f.root == null) continue;

                f.titleText.gameObject.SetActive(true);
                f.bodyText.gameObject.SetActive(true);
                f.badgeText.gameObject.SetActive(true);
                f.matrixTerminalText.gameObject.SetActive(false);

                f.headerText.text = $"[ {billboardTitle} // FLEET EMERGENCY PROTOCOL ]  <color=#ff0000>⚠ COMBAT LOCKDOWN</color>";
                f.badgeText.text = "⚠⚠⚠ CRITICAL WARNING: HOSTILE SHIP ENGAGED ⚠⚠⚠";
                f.badgeText.color = alertCol;

                f.titleText.color = alertCol;
                f.titleText.text = "DEFENSE PROTOCOL ACTIVE";

                f.bodyText.color = Color.white;
                f.bodyText.text = "Phat hien tau xam phap no sung vao can cu!\n" +
                                 "Toan bo tram phong thu va ham doi phi thuyen chuyen sang che do nghinh chien.\n" +
                                 "<color=#ff0000>YEU CAU TOAN BO PHI THUYEN DAN SU SO TAN KHOI KHU VUC NGAY!</color>";

                f.footerText.text = ">>> DEFENSE TURRETS ONLINE // CARRIER ALERT ACTIVE <<<";
                f.footerText.color = alertCol;

                if (f.borderFrame != null) f.borderFrame.color = alertCol;
            }
        }

        private IEnumerator GlitchTransitionRoutine()
        {
            _projectIndex = (_projectIndex + 1) % Projects.Length;

            for (int i = 0; i < 4; i++)
            {
                foreach (var f in _faces)
                {
                    if (f.scanlinesImage != null) f.scanlinesImage.color = new Color(1f, 0f, 0.4f, 0.35f);
                }
                yield return new WaitForSeconds(0.04f);
                foreach (var f in _faces)
                {
                    if (f.scanlinesImage != null) f.scanlinesImage.color = new Color(0f, 0.95f, 1f, 0.06f);
                }
                yield return new WaitForSeconds(0.04f);
            }
        }

        /// <summary>
        /// Xử lý khi người chơi bắn laser trúng biển quảng cáo:
        /// Tạo tia lửa điện, glitch sọc màn hình cực mạnh và kích hoạt quá trình tự động Reboot!
        /// </summary>
        public void OnHitByLaser(Vector3 hitPoint, Vector3 boltDir)
        {
            if (_isRebooting) return;

            SpaceExplosionVFX.SpawnHitSpark(hitPoint, true);
            StartCoroutine(HardwareGlitchAndRebootRoutine());
        }

        private IEnumerator HardwareGlitchAndRebootRoutine()
        {
            _isRebooting = true;
            foreach (var f in _faces) f.rebootPanel.SetActive(true);

            // Giai đoạn 1: Chập mạch giật cục (0.5s)
            float glitchTimer = 0f;
            while (glitchTimer < 0.5f)
            {
                glitchTimer += Time.deltaTime;
                string glitchMsg = $"█▓▒░ SYSTEM FAULT 0x{Random.Range(100, 999)} ░▒▓█\n" +
                                   $"HARDWARE IMPACT DETECTED AT COORD\n" +
                                   $"SIGNAL CORRUPTED // REBOOTING...";
                Color c = (Random.value > 0.5f) ? alertThemeColor : Color.white;

                foreach (var f in _faces)
                {
                    f.rebootText.text = glitchMsg;
                    f.rebootText.color = c;
                }
                yield return new WaitForSeconds(0.05f);
            }

            // Giai đoạn 2: Thanh tiến trình khởi động lại 0% -> 100% (1.2s)
            float rebootDuration = 1.2f;
            float elapsed = 0f;
            while (elapsed < rebootDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / rebootDuration);
                string rebootMsg = $"REBOOTING FIRMWARE MATRIX... {Mathf.RoundToInt(progress * 100)}%\n" +
                                   "RESTRICTING VOLTAGE • CALIBRATING HOLOGRAPHIC TILES";

                foreach (var f in _faces)
                {
                    f.rebootText.text = rebootMsg;
                    f.rebootText.color = normalThemeColor;
                    if (f.rebootProgressBar != null)
                    {
                        f.rebootProgressBar.rectTransform.sizeDelta = new Vector2(460f * progress, 16f);
                    }
                }

                yield return null;
            }

            foreach (var f in _faces)
            {
                f.rebootText.text = "✓ REBOOT COMPLETE // SIGNAL RESTORED";
                f.rebootText.color = new Color(0.2f, 1f, 0.4f);
            }
            yield return new WaitForSeconds(0.35f);

            foreach (var f in _faces) f.rebootPanel.SetActive(false);
            _isRebooting = false;
        }
    }
}
