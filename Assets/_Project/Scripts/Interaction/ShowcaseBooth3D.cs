using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using CodeDrive.CameraSystem;
using CodeDrive.Portfolio;

namespace CodeDrive.Interaction
{
    [System.Serializable]
    public class ShowcaseSlide
    {
        public string title = "PROJECT TITLE";
        public string subtitleUrl = "project-link.com";
        public string role = "DEVELOPER";
        public string withCollab = "STUDIO / INDIE";
        public string distinctions = "FWA OF THE DAY • AWWWARDS";
        [TextArea(2, 5)] public string description = "Project detailed description.";
        public string techStack = "UNITY 3D • C# • SHADERS";
        public string externalLinkUrl = "https://github.com";
        public Color themeColor = new Color(0.2f, 0.55f, 1f);
        public Sprite previewSprite;
    }

    /// <summary>
    /// 1:1 Bruno Simon Three.js Journey-style Showcase Booth.
    /// Features:
    /// - Center wooden billboard with large title, link pill badge, project preview artwork, and dots pagination.
    /// - Left wing: Hanging wooden sign for previous project + arrow button.
    /// - Left floor: Standing A-frame Chalkboard guide with keyboard controls.
    /// - Right wing: Hanging wooden sign for next project + arrow button.
    /// - Right column: Signpost wooden planks for ROLE and WITH / TECH.
    /// - Front bench: Distinctions & awards plaque with trophy badge.
    /// </summary>
    public class ShowcaseBooth3D : MonoBehaviour
    {
        private static ShowcaseBooth3D _activeFocusedBooth;
        public static bool AnyBoothFocused => _activeFocusedBooth != null;
        public static void CloseActiveBooth()
        {
            if (_activeFocusedBooth != null)
            {
                _activeFocusedBooth.ExitFocusView();
            }
        }

        [Header("Area Configuration")]
        [SerializeField] private PortfolioAreaType areaType;
        [SerializeField] private string areaLabel;

        [Header("Showcase Content Slides")]
        [SerializeField] private List<ShowcaseSlide> slides = new List<ShowcaseSlide>();
        [SerializeField] private int currentSlideIndex = 0;

        [Header("Visual Roots")]
        [SerializeField] private GameObject displayRoot;
        [SerializeField] private GameObject pointMarkerRoot;

        [Header("Camera & Parking")]
        [SerializeField] private Transform cameraFocusAnchor;
        [SerializeField] private Transform parkingSpotAnchor;
        [SerializeField] private float focusFOV = 50f;
        [SerializeField] private float focusSpeed = 4.5f;

        [Header("Center Screen UI References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Button openUrlButton;
        [SerializeField] private Text urlBadgeText;
        [SerializeField] private Image projectImage;
        [SerializeField] private Text pageIndicatorText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;

        [Header("Left Wing References")]
        [SerializeField] private Text leftPrevSignText;
        [SerializeField] private Button leftPrevButton;
        [SerializeField] private GameObject chalkboardGuide;
        [SerializeField] private Text chalkboardGuideText;

        [Header("Right Wing References")]
        [SerializeField] private Text rightNextSignText;
        [SerializeField] private Button rightNextButton;
        [SerializeField] private Text roleSignText;
        [SerializeField] private Text withSignText;

        [Header("Front Bench References")]
        [SerializeField] private Text distinctionsText;

        [Header("Marker UI References")]
        [SerializeField] private Text markerTitleText;
        [SerializeField] private Text markerSubtitleText;

        private bool _isPlayerInsideZone;
        private bool _isFocusedOnBooth;
        private CarControl _cachedCar;
        private Coroutine _scaleCoroutine;
        private Vector3 _preFocusCarPosition;
        private Quaternion _preFocusCarRotation;

        [Header("Display Animation")]
        [SerializeField] private float displayScaleSpeed = 6f;

        public bool IsFocused => _isFocusedOnBooth;

        private void Awake()
        {
            SetupDefaultSlidesIfEmpty();

            // Center nav buttons
            if (prevButton != null) prevButton.onClick.AddListener(PrevSlide);
            if (nextButton != null) nextButton.onClick.AddListener(NextSlide);
            if (closeButton != null) closeButton.onClick.AddListener(ExitFocusView);
            if (openUrlButton != null) openUrlButton.onClick.AddListener(OpenCurrentProjectUrl);

            // Left & right wing buttons
            if (leftPrevButton != null) leftPrevButton.onClick.AddListener(PrevSlide);
            if (rightNextButton != null) rightNextButton.onClick.AddListener(NextSlide);
        }

        private void Start()
        {
            EnsurePreviewSprites();
            UpdateSlideUI();
            UpdateMarkerUI();

            // Always display the 3D booth structure in the world
            if (displayRoot != null)
            {
                displayRoot.SetActive(true);
                displayRoot.transform.localScale = Vector3.one;
            }
            if (pointMarkerRoot != null) pointMarkerRoot.SetActive(true);

            // Ensure world cameras are set for UI raycasting
            var canvases = GetComponentsInChildren<Canvas>(true);
            var mainCam = Camera.main;
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.WorldSpace && c.worldCamera == null)
                {
                    c.worldCamera = mainCam;
                }
            }
        }

        public void ConfigureArea(PortfolioAreaType type, string label, List<ShowcaseSlide> customSlides = null)
        {
            areaType = type;
            areaLabel = label;
            if (customSlides != null && customSlides.Count > 0)
            {
                slides = customSlides;
            }
            else
            {
                slides = GenerateDefaultSlidesForType(type, label);
            }
            currentSlideIndex = 0;
            EnsurePreviewSprites();
            UpdateSlideUI();
            UpdateMarkerUI();
        }

        private void SetupDefaultSlidesIfEmpty()
        {
            if (slides != null && slides.Count > 0) return;
            slides = GenerateDefaultSlidesForType(areaType, string.IsNullOrEmpty(areaLabel) ? areaType.ToString() : areaLabel);
        }

        public void ForceRegenerateSprites()
        {
            if (slides == null) return;
            for (int i = 0; i < slides.Count; i++)
            {
                slides[i].previewSprite = CreateProceduralArtwork(slides[i].title, slides[i].themeColor, i);
            }
        }

        public void EnsurePreviewSprites()
        {
            if (slides == null) return;
            for (int i = 0; i < slides.Count; i++)
            {
                if (slides[i].previewSprite == null)
                {
                    slides[i].previewSprite = CreateProceduralArtwork(slides[i].title, slides[i].themeColor, i);
                }
            }
        }

        private Sprite CreateProceduralArtwork(string title, Color themeColor, int index)
        {
            int w = 640;
            int h = 380;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color bgDark = new Color(0.09f, 0.10f, 0.14f, 1f);
            Color cardBg = new Color(0.14f, 0.16f, 0.22f, 1f);
            Color roomWall = new Color(0.88f, 0.52f, 0.58f, 1f); // Soft pastel room wall like Bruno Simon's
            Color roomFloor = new Color(0.94f, 0.85f, 0.80f, 1f);
            Color roomDesk = new Color(0.98f, 0.96f, 0.92f, 1f);
            Color roomAccent = themeColor;
            Color warmGold = new Color(1f, 0.78f, 0.25f, 1f);

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;

                    // Base dark background
                    Color col = bgDark;

                    // Left Side: UI Card Mockups (x: 0.08 to 0.46, y: 0.15 to 0.85)
                    if (u >= 0.06f && u <= 0.44f && v >= 0.14f && v <= 0.86f)
                    {
                        col = cardBg;
                        // Card header bar
                        if (v >= 0.76f && v <= 0.84f && u <= 0.38f) col = Color.Lerp(cardBg, themeColor, 0.75f);
                        // Sub-card text lines (horizontal skeleton bars)
                        if (v >= 0.62f && v <= 0.68f && u <= 0.40f) col = new Color(0.35f, 0.38f, 0.48f, 1f);
                        if (v >= 0.52f && v <= 0.56f && u <= 0.35f) col = new Color(0.25f, 0.28f, 0.36f, 1f);
                        if (v >= 0.44f && v <= 0.48f && u <= 0.32f) col = new Color(0.25f, 0.28f, 0.36f, 1f);
                        // Mini price / badge tag
                        if (u >= 0.10f && u <= 0.26f && v >= 0.22f && v <= 0.34f) col = new Color(0.20f, 0.24f, 0.35f, 1f);
                        if (u >= 0.28f && u <= 0.40f && v >= 0.22f && v <= 0.34f) col = warmGold;
                    }

                    // Right Side: 3D Isometric Mini Room (x: 0.50 to 0.92, y: 0.12 to 0.88)
                    if (u >= 0.50f && u <= 0.92f && v >= 0.14f && v <= 0.86f)
                    {
                        // 3D Isometric room background box
                        float rx = (u - 0.71f) / 0.21f; // -1 to 1
                        float ry = (v - 0.50f) / 0.36f; // -1 to 1

                        // Isometric Floor
                        if (ry < 0f && Mathf.Abs(rx) < 0.90f - ry * 0.4f)
                        {
                            col = roomFloor;
                            // Isometric grid pattern on floor
                            int fx = (int)((rx + ry) * 35f);
                            int fy = (int)((rx - ry) * 35f);
                            if (fx % 5 == 0 || fy % 5 == 0) col = Color.Lerp(roomFloor, Color.white, 0.35f);

                            // Desk in room
                            if (rx >= -0.35f && rx <= 0.35f && ry >= -0.55f && ry <= -0.15f)
                            {
                                col = roomDesk;
                            }
                            // Chair / Arcade box
                            if (rx >= -0.75f && rx <= -0.45f && ry >= -0.45f && ry <= 0.10f)
                            {
                                col = themeColor;
                            }
                        }
                        // Isometric Back Left Wall
                        else if (rx < 0f && ry >= 0f && ry < 0.95f)
                        {
                            col = roomWall;
                            // Poster on wall
                            if (rx >= -0.70f && rx <= -0.30f && ry >= 0.30f && ry <= 0.75f)
                            {
                                col = Color.Lerp(roomWall, Color.white, 0.6f);
                            }
                        }
                        // Isometric Back Right Wall
                        else if (rx >= 0f && ry >= 0f && ry < 0.95f)
                        {
                            col = Color.Lerp(roomWall, Color.black, 0.15f); // Shadowed wall
                            // Neon sign on right wall
                            if (rx >= 0.30f && rx <= 0.70f && ry >= 0.40f && ry <= 0.70f)
                            {
                                col = warmGold;
                            }
                        }
                    }

                    // Border Frame
                    if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3)
                    {
                        col = new Color(0.22f, 0.25f, 0.32f, 1f);
                    }

                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        public static List<ShowcaseSlide> GenerateDefaultSlidesForType(PortfolioAreaType type, string label)
        {
            switch (type)
            {
                case PortfolioAreaType.Projects:
                    return new List<ShowcaseSlide>
                    {
                        new ShowcaseSlide
                        {
                            title = "THREE.JS JOURNEY",
                            subtitleUrl = "THREEJS-JOURNEY.COM",
                            role = "DEVELOPER\nFORMATER",
                            withCollab = "HERVÉ STUDIO\nBONHOMME PARIS",
                            distinctions = "★ FWA OF THE DAY • AWWWARDS SITE OF THE MONTH ★",
                            description = "Trang web học 3D Web tương tác hàng đầu thế giới với WebGL, Shaders, Physics & Three.js.",
                            techStack = "THREE.JS • WEBGL • SHADERS • BLENDER • GSAP",
                            externalLinkUrl = "https://threejs-journey.com",
                            themeColor = new Color(0.2f, 0.55f, 1f)
                        },
                        new ShowcaseSlide
                        {
                            title = "3D ACTION RPG COMBAT",
                            subtitleUrl = "GITHUB.COM/LEETONION/RPG",
                            role = "LEAD GAMEPLAY\nPROGRAMMER",
                            withCollab = "UNITY ASSET STORE\nCOMMUNITY 2025",
                            distinctions = "★ UNITY SHOWCASE BEST GAMEPLAY 2025 ★",
                            description = "Hệ thống chiến đấu hành động nhịp độ cao, Animation Motion Warping, AI Behavior Trees và VFX URP.",
                            techStack = "UNITY 3D • C# • URP • STATE MACHINE • AI NAV",
                            externalLinkUrl = "https://github.com",
                            themeColor = new Color(1f, 0.45f, 0.15f)
                        },
                        new ShowcaseSlide
                        {
                            title = "MULTIPLAYER DRIFT RACER",
                            subtitleUrl = "PLAY.CODE-DRIVE.IO",
                            role = "FULLSTACK GAME\nDEVELOPER",
                            withCollab = "PHOTON ENGINE\nINDIE LABS",
                            distinctions = "★ GLOBAL TOP 10 INDIE SHOWCASE ★",
                            description = "Game đua xe drift mạng thời gian thực, vật lý Raycast tùy biến mượt mà và dự đoán Client-side Prediction.",
                            techStack = "UNITY • PHOTON FUSION • VEHICLE PHYSICS",
                            externalLinkUrl = "https://github.com",
                            themeColor = new Color(0.95f, 0.2f, 0.55f)
                        },
                        new ShowcaseSlide
                        {
                            title = "3D PORTFOLIO WORLD",
                            subtitleUrl = "LEETONION.PORTFOLIO.IO",
                            role = "CREATIVE\nTECHNOLOGIST",
                            withCollab = "BRUNO SIMON\nINSPIRED DESIGN",
                            distinctions = "★ CREATIVE DEV SHOWCASE 2026 ★",
                            description = "Thành phố 3D tương tác với hệ thống GPS Road Navigation A*, điều khiển xe isometric và bảng Showcase thời gian thực.",
                            techStack = "UNITY 6 / 2022 • C# • LIGHTWEIGHT A* • URP",
                            externalLinkUrl = "https://github.com",
                            themeColor = new Color(0.2f, 0.85f, 0.45f)
                        }
                    };

                case PortfolioAreaType.About:
                    return new List<ShowcaseSlide>
                    {
                        new ShowcaseSlide
                        {
                            title = "ABOUT ME",
                            subtitleUrl = "LEETONION.PORTFOLIO.IO",
                            role = "UNITY GAME &\nSIMULATION DEV",
                            withCollab = "5+ YEARS\nPROFESSIONAL EXP",
                            distinctions = "★ CREATIVE TECHNICAL ARCHITECT ★",
                            description = "Đam mê sáng tạo trải nghiệm 3D tương tác sống động, tối ưu hóa hiệu năng cao và phát triển hệ thống gameplay chuyên sâu.",
                            techStack = "C# • UNITY 3D • HLSL • SYSTEM ARCHITECTURE",
                            externalLinkUrl = "https://github.com",
                            themeColor = new Color(1f, 0.7f, 0.2f)
                        }
                    };

                case PortfolioAreaType.Skills:
                    return new List<ShowcaseSlide>
                    {
                        new ShowcaseSlide
                        {
                            title = "TECHNICAL SKILLS",
                            subtitleUrl = "CORE COMPETENCIES",
                            role = "GAME ENGINE &\nGRAPHICS",
                            withCollab = "C# • HLSL • C++\nPYTHON",
                            distinctions = "★ ADVANCED ARCHITECTURE & OPTIMIZATION ★",
                            description = "• Ngôn ngữ: C#, C++, Python, HLSL / ShaderLab\n• Kỹ thuật: A* Pathfinding, Vehicle Physics, Object Pooling, State Machines\n• Tools: URP/HDRP, Profiler, Git, CI/CD",
                            techStack = "UNITY • C# • URP • SHADERS • PHYSICS",
                            externalLinkUrl = "",
                            themeColor = new Color(0.2f, 0.85f, 1f)
                        }
                    };

                case PortfolioAreaType.Experience:
                    return new List<ShowcaseSlide>
                    {
                        new ShowcaseSlide
                        {
                            title = "WORK EXPERIENCE",
                            subtitleUrl = "2021 - PRESENT",
                            role = "SENIOR GAME\nDEVELOPER",
                            withCollab = "STUDIO &\nINDIE LEAD",
                            distinctions = "★ 5+ YEARS SHIPPING SUCCESSFUL TITLES ★",
                            description = "• Senior Unity Developer (2023 - Hiện tại): Thiết kế kiến trúc core gameplay, tối ưu hóa cảnh 3D quy mô lớn.\n• Gameplay Programmer (2021 - 2023): Phát triển cơ chế điều khiển xe, camera isometric & UI/UX.",
                            techStack = "C# • ARCHITECTURE • OPTIMIZATION",
                            externalLinkUrl = "",
                            themeColor = new Color(0.85f, 0.4f, 1f)
                        }
                    };

                case PortfolioAreaType.Education:
                    return new List<ShowcaseSlide>
                    {
                        new ShowcaseSlide
                        {
                            title = "EDUCATION & CERTS",
                            subtitleUrl = "ACADEMIC BACKGROUND",
                            role = "SOFTWARE\nENGINEER",
                            withCollab = "UNITY CERTIFIED\nPROFESSIONAL",
                            distinctions = "★ BACHELOR OF SOFTWARE ENGINEERING ★",
                            description = "• Cử nhân Công nghệ Thông tin / Kỹ thuật Phần mềm\n• Unity Certified Professional: Programmer\n• Advanced 3D Graphics & Game Engine Architecture",
                            techStack = "COMPUTER SCIENCE • SOFTWARE ENGINEERING",
                            externalLinkUrl = "",
                            themeColor = new Color(0.3f, 1f, 0.6f)
                        }
                    };

                case PortfolioAreaType.Contact:
                    return new List<ShowcaseSlide>
                    {
                        new ShowcaseSlide
                        {
                            title = "GET IN TOUCH",
                            subtitleUrl = "LEETONIOM.DEV@GMAIL.COM",
                            role = "OPEN FOR HIRE &\nCOLLABORATION",
                            withCollab = "REMOTE /\nRELOCATION",
                            distinctions = "★ AVAILABLE FOR FREELANCE & FULLTIME ★",
                            description = "• Email: leetoniom.dev@gmail.com\n• GitHub: github.com/LeeTonion\n• LinkedIn: linkedin.com/in/leetoniom\n• Location: Vietnam (Open for Remote / Relocation)",
                            techStack = "EMAIL • GITHUB • LINKEDIN",
                            externalLinkUrl = "https://github.com",
                            themeColor = new Color(1f, 0.4f, 0.5f)
                        }
                    };

                default:
                    return new List<ShowcaseSlide>
                    {
                        new ShowcaseSlide
                        {
                            title = label.ToUpper(),
                            subtitleUrl = "PORTFOLIO AREA",
                            role = "DEVELOPER",
                            withCollab = "CREATIVE DEV",
                            distinctions = "★ FEATURED AREA ★",
                            description = $"Nội dung chi tiết khu vực {label}.",
                            techStack = "UNITY 3D • C#",
                            externalLinkUrl = "",
                            themeColor = Color.cyan
                        }
                    };
            }
        }

        private void Update()
        {
            HandleMouseClickRaycast();

            if (!_isPlayerInsideZone && !_isFocusedOnBooth) return;

            HandleKeyboardInput();
        }

        private void HandleMouseClickRaycast()
        {
            if (_isFocusedOnBooth) return;

            bool clicked = false;
            Vector2 mousePos = Vector2.zero;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                clicked = true;
                mousePos = mouse.position.ReadValue();
            }
            if (!clicked && UnityEngine.Input.GetMouseButtonDown(0))
            {
                clicked = true;
                mousePos = UnityEngine.Input.mousePosition;
            }

            if (clicked)
            {
                // If clicking on screen HUD UI, don't trigger 3D booth focus
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                var cam = Camera.main;
                if (cam == null) return;

                Ray ray = cam.ScreenPointToRay(mousePos);
                var hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform == transform || hits[i].transform.IsChildOf(transform))
                    {
                        EnterFocusView();
                        break;
                    }
                }
            }
        }

        private void OnMouseDown()
        {
            if (!_isFocusedOnBooth)
            {
                EnterFocusView();
            }
        }

        private void HandleKeyboardInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            {
                if (!_isFocusedOnBooth)
                {
                    EnterFocusView();
                }
                else
                {
                    OpenCurrentProjectUrl();
                }
            }

            if (_isFocusedOnBooth)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
                {
                    PrevSlide();
                }
                else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
                {
                    NextSlide();
                }
                else if (kb.escapeKey.wasPressedThisFrame)
                {
                    ExitFocusView();
                }
            }
        }

        public void NextSlide()
        {
            if (slides.Count <= 1) return;
            currentSlideIndex = (currentSlideIndex + 1) % slides.Count;
            UpdateSlideUI();
        }

        public void PrevSlide()
        {
            if (slides.Count <= 1) return;
            currentSlideIndex = (currentSlideIndex - 1 + slides.Count) % slides.Count;
            UpdateSlideUI();
        }

        public void UpdateSlideUI()
        {
            if (slides.Count == 0) return;

            var currentSlide = slides[currentSlideIndex];
            int prevIdx = (currentSlideIndex - 1 + slides.Count) % slides.Count;
            int nextIdx = (currentSlideIndex + 1) % slides.Count;

            // 1. Center Screen
            if (titleText != null) titleText.text = currentSlide.title;
            if (urlBadgeText != null) urlBadgeText.text = currentSlide.subtitleUrl.ToUpper();
            if (projectImage != null && currentSlide.previewSprite != null)
            {
                projectImage.sprite = currentSlide.previewSprite;
                projectImage.color = Color.white;
            }

            if (openUrlButton != null)
            {
                bool hasUrl = !string.IsNullOrEmpty(currentSlide.externalLinkUrl);
                openUrlButton.gameObject.SetActive(hasUrl);
            }

            // Center dots indicator: ■ □ □ □
            if (pageIndicatorText != null)
            {
                if (slides.Count > 1)
                {
                    string dots = "";
                    for (int i = 0; i < slides.Count; i++)
                    {
                        dots += (i == currentSlideIndex) ? " ■ " : " □ ";
                    }
                    pageIndicatorText.text = dots;
                }
                else
                {
                    pageIndicatorText.text = "■";
                }
            }

            // 2. Left Wing Hanging Sign
            if (leftPrevSignText != null)
            {
                leftPrevSignText.text = slides.Count > 1 ? slides[prevIdx].title : "";
            }
            if (leftPrevButton != null) leftPrevButton.gameObject.SetActive(slides.Count > 1);

            // 3. Right Wing Hanging Sign
            if (rightNextSignText != null)
            {
                rightNextSignText.text = slides.Count > 1 ? slides[nextIdx].title : "";
            }
            if (rightNextButton != null) rightNextButton.gameObject.SetActive(slides.Count > 1);

            // 4. Right Signpost Planks (ROLE & WITH)
            if (roleSignText != null) roleSignText.text = currentSlide.role;
            if (withSignText != null) withSignText.text = currentSlide.withCollab;

            // 5. Front Bench Distinctions
            if (distinctionsText != null) distinctionsText.text = currentSlide.distinctions;

            // 6. Chalkboard Guide
            if (chalkboardGuideText != null)
            {
                chalkboardGuideText.text = "NEXT  →\nPREV  ←\nOPEN  ↵\nEXIT  [ESC]";
            }
        }

        private void UpdateMarkerUI()
        {
            string title = !string.IsNullOrEmpty(areaLabel) ? areaLabel.ToUpper() : areaType.ToString().ToUpper();
            if (markerTitleText != null) markerTitleText.text = title;
            if (markerSubtitleText != null) markerSubtitleText.text = "HELLO :D";
        }

        public void OpenCurrentProjectUrl()
        {
            if (slides.Count == 0) return;
            string url = slides[currentSlideIndex].externalLinkUrl;
            if (!string.IsNullOrEmpty(url))
            {
                Debug.Log($"[ShowcaseBooth3D] Opening URL: {url}");
                Application.OpenURL(url);
            }
        }

        public void EnterFocusView()
        {
            _isFocusedOnBooth = true;
            _activeFocusedBooth = this;

            // Ensure display is active and at scale
            if (displayRoot != null)
            {
                displayRoot.SetActive(true);
                displayRoot.transform.localScale = Vector3.one;
            }
            if (pointMarkerRoot != null) pointMarkerRoot.SetActive(false);

            // Save original car position and park vehicle out of view
            if (_cachedCar == null) _cachedCar = FindObjectOfType<CarControl>();
            if (_cachedCar != null)
            {
                _preFocusCarPosition = _cachedCar.transform.position;
                _preFocusCarRotation = _cachedCar.transform.rotation;
                _cachedCar.IsInputBlocked = true;

                var rb = _cachedCar.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    // Park vehicle well behind camera so it's not visible in booth view
                    Vector3 sideParkingPos = transform.position + transform.right * 14f + transform.forward * 18f;
                    rb.position = sideParkingPos;
                    rb.rotation = Quaternion.Euler(0f, transform.eulerAngles.y - 150f, 0f);
                    _cachedCar.transform.position = sideParkingPos;
                }
            }

            // Camera Focus
            if (cameraFocusAnchor != null && CameraControl.Instance != null)
            {
                CameraControl.Instance.FocusOnTransform(cameraFocusAnchor, focusFOV, focusSpeed);
            }

            Core.UIManager.Instance?.ShowInteractionPrompt(false);
        }

        public void ExitFocusView()
        {
            if (!_isFocusedOnBooth) return;

            _isFocusedOnBooth = false;
            if (_activeFocusedBooth == this) _activeFocusedBooth = null;

            // Always keep displayRoot visible in world!
            if (displayRoot != null)
            {
                displayRoot.SetActive(true);
                displayRoot.transform.localScale = Vector3.one;
            }
            if (pointMarkerRoot != null) pointMarkerRoot.SetActive(true);

            // Restore vehicle to its pre-focus position in front of the booth
            if (_cachedCar != null)
            {
                var rb = _cachedCar.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = _preFocusCarPosition;
                    rb.rotation = _preFocusCarRotation;
                }
                _cachedCar.transform.position = _preFocusCarPosition;
                _cachedCar.transform.rotation = _preFocusCarRotation;
                _cachedCar.IsInputBlocked = false;
            }

            if (CameraControl.Instance != null)
            {
                CameraControl.Instance.ClearFocus();
            }

            if (_isPlayerInsideZone)
            {
                Core.UIManager.Instance?.ShowInteractionPrompt(true, $"Click chuột vào điểm sáng để xem {areaLabel}");
            }
        }

        public void OnZoneEntered()
        {
            _isPlayerInsideZone = true;
            if (!_isFocusedOnBooth && pointMarkerRoot != null)
            {
                pointMarkerRoot.SetActive(true);
            }
        }

        public void OnZoneExited()
        {
            _isPlayerInsideZone = false;
            // Note: Do NOT call ExitFocusView() here because parking the vehicle moves it outside the trigger zone.
        }
    }
}
