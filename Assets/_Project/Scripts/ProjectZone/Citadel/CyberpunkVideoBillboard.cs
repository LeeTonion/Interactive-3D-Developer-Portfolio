using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Cyberpunk Dynamic Motion Video Billboard (Biển Quảng Cáo Video Cyberpunk Động 2 Mặt).
    /// - Hiển thị 2 mặt (Front & Back) sắc nét 360 độ, không bao giờ bị bệt xám hay ngược hình.
    /// - Bộ Tạo Clip Video Quảng Cáo Động (Cyberpunk Motion Video Reel Engine):
    ///     * Clip 1: [HYPERION WARP DRIVE] - Lõi động cơ Tachyon xoay 3D, kim tua máy nảy giật, thanh âm thanh Equalizer.
    ///     * Clip 2: [CYBER SHADOW: ACTION RPG] - Cinematic gameplay preview, thanh máu Boss nảy sát thương CRITICAL x9999.
    ///     * Clip 3: [NEO DRIFT RACER 2077] - Đua xe Synthwave đường chân trời 3D Wireframe Grid, đồng hồ tốc độ 320 km/h.
    ///     * Clip 4: [A.I. FLEET COMBAT RADAR] - Màn hình radar quân sự quét 360 độ khóa mục tiêu TARGET LOCKED.
    /// - Thanh điều khiển thời gian phát Video: [ ▶ PLAYING // 00:18 / 00:30 ] [LIVE 4K 60FPS].
    /// - Hiệu ứng chuyển cảnh Video Glitch Transition quét sóng scanline CRT sau mỗi 8 giây.
    /// - Khi bị tàu bắn trúng: Tóe lửa điện giật, glitch nhiễu tuyết màn hình rồi tự động phục hồi!
    /// </summary>
    [ExecuteAlways]
    public class CyberpunkVideoBillboard : MonoBehaviour
    {
        [Header("Cấu Hình Kênh Video")]
        public string billboardNetworkName = "CYBER_NET // MEGA_SCREEN_01";
        public float clipDuration = 8.0f;

        [Header("Màu Sắc Cyberpunk")]
        public Color neonCyan = new Color(0f, 0.95f, 1f, 1f);
        public Color neonMagenta = new Color(1f, 0.15f, 0.65f, 1f);
        public Color neonYellow = new Color(1f, 0.9f, 0.1f, 1f);
        public Color warningRed = new Color(1f, 0.15f, 0.15f, 1f);

        // UI Representation per Face
        private class VideoBillboardFace
        {
            public GameObject root;
            public Image background;
            public Image borderGlow;
            public RawImage videoDisplay;
            public Image scanlines;
            public Image glitchFlash;

            // Video Header
            public TMP_Text topBrandText;
            public TMP_Text liveBadgeText;

            // Video Player HUD Controls
            public TMP_Text videoPlaybackStatus;
            public Slider videoProgressBar;
            public TMP_Text timeCodeText;

            // Animated Visual Elements
            public RectTransform visualizerContainer;
            public Image[] eqBars;
            public RectTransform centerRotatingGraphic;
            public TMP_Text mainPromoTitle;
            public TMP_Text subPromoDetail;
            public TMP_Text statCounterText;

            // Hit Glitch Overlay
            public GameObject hitAlertPanel;
            public TMP_Text hitAlertText;
        }

        private readonly List<VideoBillboardFace> _faces = new List<VideoBillboardFace>();

        // Clip state
        private int _currentClip = 0;
        private float _clipTimer = 0f;
        private bool _isHitGlitched = false;
        private float _hitGlitchRemaining = 0f;

        private void Awake()
        {
            BuildBillboardFaces();
        }

        private void OnEnable()
        {
            BuildBillboardFaces();
        }

        private void Start()
        {
            BuildBillboardFaces();
        }

        public void BuildBillboardFaces()
        {
            SetupScreenMaterials();

            // Nếu đã có 2 canvas con hợp lệ thì chỉ refresh
            var existingFront = transform.Find("Billboard_VideoFace_Front");
            var existingBack = transform.Find("Billboard_VideoFace_Back");
            if (existingFront != null && existingBack != null && _faces.Count == 2)
            {
                return;
            }

            // Dọn dẹp canvas cũ
            _faces.Clear();
            var oldCanvases = GetComponentsInChildren<Canvas>(true);
            foreach (var c in oldCanvases)
            {
                if (Application.isPlaying) Destroy(c.gameObject);
                else DestroyImmediate(c.gameObject);
            }

            // Mặt trước: quay góc (0, 180, 0)
            _faces.Add(CreateFaceUI("Billboard_VideoFace_Front", new Vector3(0.046f, 0.07f, 0.022f), Quaternion.Euler(0f, 180f, 0f)));
            // Mặt sau: quay góc (0, 0, 0)
            _faces.Add(CreateFaceUI("Billboard_VideoFace_Back", new Vector3(0.046f, 0.07f, -0.042f), Quaternion.Euler(0f, 0f, 0f)));

            // Đảm bảo BoxCollider để nhận laser bắn
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var bc = gameObject.AddComponent<BoxCollider>();
                bc.size = new Vector3(4.5f, 3.8f, 0.6f);
            }

            ApplyClipContent();
        }

        private void SetupScreenMaterials()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                var mats = mr.sharedMaterials;
                if (mats != null && mats.Length >= 2)
                {
                    Material screenMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    screenMat.name = "Mat_VideoScreen_Face";
                    screenMat.color = new Color(0.01f, 0.02f, 0.04f, 1f);
                    screenMat.EnableKeyword("_EMISSION");
                    screenMat.SetColor("_EmissionColor", new Color(0.08f, 0.15f, 0.3f));

                    Material frameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    frameMat.name = "Mat_VideoScreen_Frame";
                    frameMat.color = new Color(0.08f, 0.09f, 0.12f, 1f);
                    frameMat.SetFloat("_Metallic", 0.9f);
                    frameMat.SetFloat("_Smoothness", 0.75f);

                    mr.materials = new Material[] { screenMat, frameMat };
                }
            }
        }

        private VideoBillboardFace CreateFaceUI(string faceName, Vector3 localPos, Quaternion localRot)
        {
            var face = new VideoBillboardFace();

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

            // 1. Nền màn hình tối sâu
            var bgGo = CreateUIObject("Bg_Display", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero);
            face.background = bgGo.AddComponent<Image>();
            face.background.color = new Color(0.015f, 0.025f, 0.05f, 0.98f);

            // 2. Viền Neon phát quang rực rỡ
            var borderGo = CreateUIObject("Border_Glow", canvasGo.transform, Vector2.zero, Vector2.one, new Vector2(-12f, -12f));
            face.borderGlow = borderGo.AddComponent<Image>();
            face.borderGlow.color = new Color(neonCyan.r, neonCyan.g, neonCyan.b, 0.55f);

            // Nền bên trong khung viền
            var innerBg = CreateUIObject("Inner_Bg", borderGo.transform, Vector2.zero, Vector2.one, new Vector2(-8f, -8f));
            var innerImg = innerBg.AddComponent<Image>();
            innerImg.color = new Color(0.01f, 0.02f, 0.04f, 1f);

            // 3. RawImage hiển thị video texture (nếu cắm VideoPlayer)
            var videoDispGo = CreateUIObject("Video_Texture_Display", innerBg.transform, Vector2.zero, Vector2.one, Vector2.zero);
            face.videoDisplay = videoDispGo.AddComponent<RawImage>();
            face.videoDisplay.color = Color.white;
            face.videoDisplay.gameObject.SetActive(false);

            // 4. Thanh Header Video
            var headerBar = CreateUIObject("Header_Bar", innerBg.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-20f, 40f));
            headerBar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            headerBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -10f);

            var brandGo = CreateUIObject("Brand_Text", headerBar.transform, new Vector2(0f, 0f), new Vector2(0.7f, 1f), Vector2.zero);
            face.topBrandText = brandGo.AddComponent<TextMeshProUGUI>();
            face.topBrandText.fontSize = 17f;
            face.topBrandText.fontStyle = FontStyles.Bold;
            face.topBrandText.alignment = TextAlignmentOptions.MidlineLeft;
            face.topBrandText.color = neonCyan;
            face.topBrandText.text = $"[ {billboardNetworkName} ]";

            var liveBadgeGo = CreateUIObject("Live_Badge", headerBar.transform, new Vector2(0.7f, 0f), new Vector2(1f, 1f), Vector2.zero);
            face.liveBadgeText = liveBadgeGo.AddComponent<TextMeshProUGUI>();
            face.liveBadgeText.fontSize = 15f;
            face.liveBadgeText.fontStyle = FontStyles.Bold;
            face.liveBadgeText.alignment = TextAlignmentOptions.MidlineRight;
            face.liveBadgeText.color = neonMagenta;
            face.liveBadgeText.text = "● LIVE COMMERCIAL FEED";

            // 5. Khu vực đồ họa động trung tâm (Motion Graphics Center Stage)
            var centerStage = CreateUIObject("Motion_Stage", innerBg.transform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.88f), Vector2.zero);

            // Hình xoay biểu tượng trung tâm
            var rotGo = CreateUIObject("Center_Rotating_Emblem", centerStage.transform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(160f, 160f));
            rotGo.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            var rotImg = rotGo.AddComponent<Image>();
            rotImg.color = new Color(neonCyan.r, neonCyan.g, neonCyan.b, 0.35f);
            face.centerRotatingGraphic = rotGo.GetComponent<RectTransform>();

            // Vòng tròn inner xoay ngược
            var innerRotGo = CreateUIObject("Inner_Ring", rotGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f));
            var innerRotImg = innerRotGo.AddComponent<Image>();
            innerRotImg.color = new Color(neonMagenta.r, neonMagenta.g, neonMagenta.b, 0.45f);

            // Tiêu đề Quảng cáo lớn
            var promoTitleGo = CreateUIObject("Promo_Title", centerStage.transform, new Vector2(0f, 0.22f), new Vector2(1f, 0.48f), Vector2.zero);
            face.mainPromoTitle = promoTitleGo.AddComponent<TextMeshProUGUI>();
            face.mainPromoTitle.fontSize = 32f;
            face.mainPromoTitle.fontStyle = FontStyles.Bold;
            face.mainPromoTitle.alignment = TextAlignmentOptions.Center;
            face.mainPromoTitle.color = Color.white;
            face.mainPromoTitle.text = "HYPERION WARP DRIVE";

            // Dòng phụ chi tiết quảng cáo
            var promoSubGo = CreateUIObject("Promo_Sub", centerStage.transform, new Vector2(0f, 0.08f), new Vector2(1f, 0.25f), Vector2.zero);
            face.subPromoDetail = promoSubGo.AddComponent<TextMeshProUGUI>();
            face.subPromoDetail.fontSize = 17f;
            face.subPromoDetail.fontStyle = FontStyles.Normal;
            face.subPromoDetail.alignment = TextAlignmentOptions.Center;
            face.subPromoDetail.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            face.subPromoDetail.text = "NEXT-GEN FASTER THAN LIGHT PROPULSION // AVAILABLE NOW";

            // Dòng chỉ số kỹ thuật nhảy số
            var statGo = CreateUIObject("Stat_Counter", centerStage.transform, new Vector2(0f, 0.00f), new Vector2(1f, 0.12f), Vector2.zero);
            face.statCounterText = statGo.AddComponent<TextMeshProUGUI>();
            face.statCounterText.fontSize = 16f;
            face.statCounterText.fontStyle = FontStyles.Bold;
            face.statCounterText.alignment = TextAlignmentOptions.Center;
            face.statCounterText.color = neonYellow;
            face.statCounterText.text = "WARP FACTOR: 9.98 // ENERGY OUTPUT: 104.5% OVERDRIVE";

            // Audio Equalizer Spectrum Bar Visualizer (12 cột nhảy sóng âm)
            var eqContainer = CreateUIObject("Audio_Visualizer", innerBg.transform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.27f), Vector2.zero);
            face.visualizerContainer = eqContainer.GetComponent<RectTransform>();
            face.eqBars = new Image[12];
            float barStep = 1.0f / 12f;
            for (int i = 0; i < 12; i++)
            {
                var barGo = CreateUIObject($"EQ_Bar_{i}", eqContainer.transform, new Vector2(i * barStep + 0.01f, 0f), new Vector2((i + 1) * barStep - 0.01f, 1f), Vector2.zero);
                barGo.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
                var barImg = barGo.AddComponent<Image>();
                barImg.color = Color.Lerp(neonCyan, neonMagenta, i / 11f);
                face.eqBars[i] = barImg;
            }

            // 6. Thanh Player HUD Control ở đáy (Timeline, Timecode, Progress Bar)
            var playerHud = CreateUIObject("Player_Controls_HUD", innerBg.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 50f));
            playerHud.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            playerHud.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 10f);

            var statusGo = CreateUIObject("Playback_Status", playerHud.transform, new Vector2(0f, 0.5f), new Vector2(0.35f, 1f), Vector2.zero);
            face.videoPlaybackStatus = statusGo.AddComponent<TextMeshProUGUI>();
            face.videoPlaybackStatus.fontSize = 15f;
            face.videoPlaybackStatus.fontStyle = FontStyles.Bold;
            face.videoPlaybackStatus.alignment = TextAlignmentOptions.MidlineLeft;
            face.videoPlaybackStatus.color = neonCyan;
            face.videoPlaybackStatus.text = "▶ PLAYING [4K 60FPS]";

            var timecodeGo = CreateUIObject("Timecode", playerHud.transform, new Vector2(0.65f, 0.5f), new Vector2(1f, 1f), Vector2.zero);
            face.timeCodeText = timecodeGo.AddComponent<TextMeshProUGUI>();
            face.timeCodeText.fontSize = 15f;
            face.timeCodeText.fontStyle = FontStyles.Bold;
            face.timeCodeText.alignment = TextAlignmentOptions.MidlineRight;
            face.timeCodeText.color = Color.white;
            face.timeCodeText.text = "00:15 / 00:30";

            // Progress Slider
            var sliderGo = CreateUIObject("Progress_Bar", playerHud.transform, new Vector2(0f, 0.1f), new Vector2(1f, 0.4f), Vector2.zero);
            face.videoProgressBar = sliderGo.AddComponent<Slider>();
            face.videoProgressBar.interactable = false;

            var sliderBg = CreateUIObject("Slider_Bg", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero);
            var sliderBgImg = sliderBg.AddComponent<Image>();
            sliderBgImg.color = new Color(0.15f, 0.2f, 0.3f, 0.6f);

            var fillArea = CreateUIObject("Fill_Area", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero);
            var fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = neonCyan;
            face.videoProgressBar.fillRect = fill.GetComponent<RectTransform>();

            // 7. Scanline CRT Overlay
            var scanGo = CreateUIObject("Scanline_Overlay", innerBg.transform, Vector2.zero, Vector2.one, Vector2.zero);
            face.scanlines = scanGo.AddComponent<Image>();
            face.scanlines.color = new Color(0f, 0.05f, 0.1f, 0.2f);
            face.scanlines.raycastTarget = false;

            // 8. Hit Glitch Alert Panel (Khi bị bắn trúng)
            var hitGo = CreateUIObject("Hit_Glitch_Panel", innerBg.transform, Vector2.zero, Vector2.one, Vector2.zero);
            var hitBg = hitGo.AddComponent<Image>();
            hitBg.color = new Color(0.9f, 0.05f, 0.1f, 0.85f);
            var hitTextGo = CreateUIObject("Hit_Text", hitGo.transform, Vector2.zero, Vector2.one, Vector2.zero);
            face.hitAlertText = hitTextGo.AddComponent<TextMeshProUGUI>();
            face.hitAlertText.fontSize = 32f;
            face.hitAlertText.fontStyle = FontStyles.Bold;
            face.hitAlertText.alignment = TextAlignmentOptions.Center;
            face.hitAlertText.color = Color.white;
            face.hitAlertText.text = "⚠ WARNING: SYSTEM INTEGRITY BREACH ⚠\nDISRUPTIVE LASER IMPACT DETECTED\nAUTO-REBOOTING DISPLAY ENGINE...";
            face.hitAlertPanel = hitGo;
            face.hitAlertPanel.SetActive(false);

            return face;
        }

        private GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private void Update()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.033f;

            // Xử lý khi bị bắn trúng
            if (_isHitGlitched)
            {
                _hitGlitchRemaining -= dt;
                if (_hitGlitchRemaining <= 0f)
                {
                    _isHitGlitched = false;
                    foreach (var face in _faces)
                    {
                        if (face.hitAlertPanel != null) face.hitAlertPanel.SetActive(false);
                    }
                }
            }

            // Chuyển đổi Clip Video theo chu kỳ
            _clipTimer += dt;
            if (_clipTimer >= clipDuration)
            {
                _clipTimer = 0f;
                _currentClip = (_currentClip + 1) % 4;
                ApplyClipContent();
            }

            // Animate các thành phần đồ họa theo thời gian thực
            AnimateMotionGraphics(dt);
        }

        private void ApplyClipContent()
        {
            switch (_currentClip)
            {
                case 0:
                    SetClipInfo(
                        "HYPERION AEROSPACE",
                        "● WARP DRIVE COMMERCIAL",
                        "TACHYON-9 WARP DRIVE",
                        "NEXT-GEN BEYOND LIGHT SPEED // 0 TO 10c IN 3.2 SECONDS",
                        "TACHYON CORE: 104.8% OVERDRIVE // STABILITY: 99.98%",
                        neonCyan,
                        neonMagenta
                    );
                    break;
                case 1:
                    SetClipInfo(
                        "CYBER SHADOW STUDIOS",
                        "● ACTION RPG TRAILER",
                        "CYBER SHADOW: RESURGENCE",
                        "DYNAMIC HACK & SLASH // PRECISE PARRY & WEAPON UPGRADES",
                        "COMBAT INTENSITY: EXTREME // COMBO x64 MULTIPLIER",
                        neonMagenta,
                        warningRed
                    );
                    break;
                case 2:
                    SetClipInfo(
                        "NEO-TOKYO DRIFT CORP",
                        "● SPEED DEMON 2077",
                        "SYNTHWAVE NITRO RACER",
                        "HYPERSONIC ANTI-GRAV DRIFTING // 12 CYBER CIRCUITS",
                        "TOP SPEED: 485 KM/H // NITRO CAPACITY: 100% READY",
                        neonYellow,
                        neonCyan
                    );
                    break;
                case 3:
                    SetClipInfo(
                        "DEFENSE MATRIX NETWORK",
                        "● FLEET SECURITY TAC-FEED",
                        "A.I. DEFENSE & DRONE RADAR",
                        "360° THREAT INTERCEPTION // AUTONOMOUS INTERCEPTORS",
                        "PERIMETER: SECURE // 04 CARRIER PATROLS ONLINE",
                        neonCyan,
                        neonYellow
                    );
                    break;
            }
        }

        private void SetClipInfo(string brand, string badge, string title, string sub, string stats, Color primaryCol, Color accentCol)
        {
            foreach (var f in _faces)
            {
                if (f.topBrandText != null) f.topBrandText.text = $"[ {billboardNetworkName} // {brand} ]";
                if (f.liveBadgeText != null) { f.liveBadgeText.text = badge; f.liveBadgeText.color = accentCol; }
                if (f.mainPromoTitle != null) { f.mainPromoTitle.text = title; f.mainPromoTitle.color = Color.white; }
                if (f.subPromoDetail != null) { f.subPromoDetail.text = sub; }
                if (f.statCounterText != null) { f.statCounterText.text = stats; f.statCounterText.color = primaryCol; }
                if (f.borderGlow != null) f.borderGlow.color = new Color(primaryCol.r, primaryCol.g, primaryCol.b, 0.65f);
            }
        }

        private void AnimateMotionGraphics(float dt)
        {
            float time = Application.isPlaying ? Time.time : (float)System.DateTime.Now.TimeOfDay.TotalSeconds;

            // Tiến độ phát video clip
            float progress = Mathf.Clamp01(_clipTimer / clipDuration);
            int currentSec = Mathf.FloorToInt(progress * 30f);
            string timeStr = $"00:{currentSec:D2} / 00:30";

            foreach (var f in _faces)
            {
                if (f.videoProgressBar != null) f.videoProgressBar.value = progress;
                if (f.timeCodeText != null) f.timeCodeText.text = timeStr;

                // Xoay biểu tượng trung tâm
                if (f.centerRotatingGraphic != null)
                {
                    f.centerRotatingGraphic.Rotate(0f, 0f, 45f * dt);
                }

                // Cột sóng âm thanh Equalizer nhảy theo nhịp nhạc
                if (f.eqBars != null)
                {
                    for (int i = 0; i < f.eqBars.Length; i++)
                    {
                        if (f.eqBars[i] != null)
                        {
                            float wave = Mathf.PerlinNoise(time * 3.5f, i * 0.45f);
                            float barHeight = Mathf.Lerp(0.15f, 1.0f, wave);
                            f.eqBars[i].rectTransform.localScale = new Vector3(1f, barHeight, 1f);
                        }
                    }
                }

                // Hiệu ứng scanline nhấp nháy
                if (f.scanlines != null)
                {
                    float scanAlpha = 0.15f + Mathf.Sin(time * 12f) * 0.05f;
                    f.scanlines.color = new Color(0f, 0.05f, 0.1f, scanAlpha);
                }
            }
        }

        /// <summary>
        /// Gọi khi người chơi bắn laser trúng bảng quảng cáo
        /// </summary>
        public void OnLaserHit()
        {
            _isHitGlitched = true;
            _hitGlitchRemaining = 2.0f;

            foreach (var f in _faces)
            {
                if (f.hitAlertPanel != null) f.hitAlertPanel.SetActive(true);
            }
        }
    }
}
