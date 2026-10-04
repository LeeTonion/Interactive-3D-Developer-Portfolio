#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using CodeDrive.Portfolio;
using UnityEditor;

namespace CodeDrive.Interaction.Editor
{
    /// <summary>
    /// Constructs a 1:1 authentic replica of Bruno Simon's Three.js Journey Showcase Booth.
    /// Camera is at +Z facing -Z:
    /// - Viewer's LEFT is local +X
    /// - Viewer's RIGHT is local -X
    /// </summary>
    public static class ShowcaseBoothBuilder
    {
        [MenuItem("Portfolio/Rebuild All Showcase Booths (1:1 Bruno Simon)")]
        public static void RebuildAllBoothsInScene()
        {
            var booths = Object.FindObjectsByType<ShowcaseBooth3D>(FindObjectsSortMode.None);
            foreach (var booth in booths)
            {
                RebuildBooth(booth);
            }
            Debug.Log($"[ShowcaseBoothBuilder] Successfully rebuilt {booths.Length} booths with 1:1 Bruno Simon styling.");
        }

        public static void RebuildBooth(ShowcaseBooth3D booth)
        {
            var t = booth.transform;

            // --- Stylized Materials matching Bruno Simon's warm low-poly palette ---
            var warmWoodMat = GetOrCreateMaterial("Mat_Booth_WarmWood", new Color(0.88f, 0.54f, 0.28f));
            var darkWoodMat = GetOrCreateMaterial("Mat_Booth_DarkWood", new Color(0.48f, 0.25f, 0.12f));
            var lightPlankMat = GetOrCreateMaterial("Mat_Booth_LightPlank", new Color(0.92f, 0.62f, 0.35f));
            var screenFrameMat = GetOrCreateMaterial("Mat_Booth_ScreenFrame", new Color(0.12f, 0.13f, 0.16f));
            var purplePillMat = GetOrCreateMaterial("Mat_Booth_PurplePill", new Color(0.60f, 0.26f, 0.88f));
            var orangeBadgeMat = GetOrCreateMaterial("Mat_Booth_OrangeBadge", new Color(0.94f, 0.52f, 0.12f));
            var goldMat = GetOrCreateMaterial("Mat_Booth_Gold", new Color(1.0f, 0.75f, 0.18f));
            var chalkSlateMat = GetOrCreateMaterial("Mat_Booth_ChalkSlate", new Color(0.14f, 0.16f, 0.18f));
            var bookPaperMat = GetOrCreateMaterial("Mat_Booth_BookPaper", new Color(0.95f, 0.92f, 0.85f));
            var fireGlowMat = GetOrCreateMaterial("Mat_Booth_FireGlow", new Color(1.0f, 0.45f, 0.08f), isEmissive: true);

            // Clean previous Display_Root
            var oldDisplay = t.Find("Display_Root");
            if (oldDisplay != null)
            {
                Object.DestroyImmediate(oldDisplay.gameObject);
            }

            var displayRootGo = new GameObject("Display_Root");
            displayRootGo.transform.SetParent(t, false);
            var displayRoot = displayRootGo.transform;

            // Add large trigger BoxCollider so clicking anywhere on the booth in 3D world focuses into it
            var boothCollider = displayRootGo.AddComponent<BoxCollider>();
            boothCollider.center = new Vector3(0f, 3.5f, 0.5f);
            boothCollider.size = new Vector3(18f, 8f, 6f);
            boothCollider.isTrigger = true;

            // 1. Center Billboard Wooden Structure (with solid physics collider)
            var billboard = new GameObject("Structure_Billboard").transform;
            billboard.SetParent(displayRoot, false);
            var billboardCol = billboard.gameObject.AddComponent<BoxCollider>();
            billboardCol.center = new Vector3(0f, 3.3f, 0f);
            billboardCol.size = new Vector3(9.8f, 6.8f, 0.8f);
            billboardCol.isTrigger = false;

            // Left and Right main upright pillars (warm wood)
            CreateCube(billboard, "Post_Left", new Vector3(4.5f, 3.3f, 0f), new Vector3(0.55f, 6.8f, 0.55f), warmWoodMat);
            CreateCube(billboard, "Post_Right", new Vector3(-4.5f, 3.3f, 0f), new Vector3(0.55f, 6.8f, 0.55f), warmWoodMat);
            // Top cross beam
            CreateCube(billboard, "Header_Beam", new Vector3(0f, 6.55f, 0.08f), new Vector3(9.8f, 0.75f, 0.5f), warmWoodMat);
            // Bottom cross beam
            CreateCube(billboard, "Bottom_Beam", new Vector3(0f, 0.45f, 0.08f), new Vector3(9.8f, 0.6f, 0.5f), warmWoodMat);
            // Back dark slate screen backing board
            CreateCube(billboard, "Backing_Board", new Vector3(0f, 3.45f, -0.08f), new Vector3(8.9f, 5.75f, 0.25f), screenFrameMat);

            // 2. Center ScreenCanvas (Faces towards +Z camera)
            var screenCanvas = SetupScreenCanvas(displayRoot, purplePillMat, orangeBadgeMat);

            // 3. Left Wing (Viewer's Left = +X): Hanging Sign & Purple Arrow Button
            var leftWing = SetupLeftWing(displayRoot, darkWoodMat, lightPlankMat, purplePillMat);

            // 4. Left Floor (Viewer's Left = +X): Standing A-Frame Chalkboard Guide + Lantern
            var chalkboard = SetupChalkboard(displayRoot, darkWoodMat, chalkSlateMat, fireGlowMat);

            // 5. Right Wing (Viewer's Right = -X): Hanging Sign & Purple Arrow Button
            var rightWing = SetupRightWing(displayRoot, darkWoodMat, lightPlankMat, purplePillMat);

            // 6. Right Column (Viewer's Right = -X): Wooden Signpost with ROLE and WITH planks
            var rightSignpost = SetupRightSignpost(displayRoot, darkWoodMat, lightPlankMat, orangeBadgeMat);

            // 7. Front Bench: Distinctions, 3D Book & Trophy
            var frontBench = SetupFrontBench(displayRoot, warmWoodMat, darkWoodMat, orangeBadgeMat, goldMat, bookPaperMat);

            // 8. Overhead Booth Spotlight
            var lightGo = new GameObject("Booth_OverheadLight");
            lightGo.transform.SetParent(displayRoot, false);
            lightGo.transform.localPosition = new Vector3(0f, 7.0f, 3.8f);
            lightGo.transform.localRotation = Quaternion.Euler(42f, 180f, 0f);
            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.96f, 0.90f);
            spot.intensity = 20f;
            spot.range = 18f;
            spot.spotAngle = 88f;
            spot.shadows = LightShadows.None;

            // 9. Camera Focus Anchor (Cinematic framing matching Bruno Simon)
            var anchor = t.Find("CameraFocusAnchor");
            if (anchor == null)
            {
                var go = new GameObject("CameraFocusAnchor");
                go.transform.SetParent(t, false);
                anchor = go.transform;
            }
            anchor.localPosition = new Vector3(0f, 3.85f, 12.25f);
            anchor.localRotation = Quaternion.Euler(7.8f, 180f, 0f);

            // 10. Position PointMarker_Root cleanly in front of the board
            var pointMarker = t.Find("PointMarker_Root");
            if (pointMarker != null)
            {
                pointMarker.localPosition = new Vector3(0f, 1.30f, 3.20f);
            }

            // Wire up SerializedFields on ShowcaseBooth3D
            var so = new SerializedObject(booth);
            so.FindProperty("displayRoot").objectReferenceValue = displayRoot.gameObject;
            so.FindProperty("cameraFocusAnchor").objectReferenceValue = anchor;
            so.FindProperty("focusFOV").floatValue = 50f;

            // Center Screen
            so.FindProperty("titleText").objectReferenceValue = screenCanvas.titleText;
            so.FindProperty("openUrlButton").objectReferenceValue = screenCanvas.openUrlBtn;
            so.FindProperty("urlBadgeText").objectReferenceValue = screenCanvas.urlBadgeText;
            so.FindProperty("projectImage").objectReferenceValue = screenCanvas.projectImage;
            so.FindProperty("pageIndicatorText").objectReferenceValue = screenCanvas.pageIndicatorText;
            so.FindProperty("prevButton").objectReferenceValue = screenCanvas.prevBtn;
            so.FindProperty("nextButton").objectReferenceValue = screenCanvas.nextBtn;
            so.FindProperty("closeButton").objectReferenceValue = screenCanvas.closeBtn;

            // Left Wing
            so.FindProperty("leftPrevSignText").objectReferenceValue = leftWing.signText;
            so.FindProperty("leftPrevButton").objectReferenceValue = leftWing.arrowBtn;
            so.FindProperty("chalkboardGuide").objectReferenceValue = chalkboard.root;
            so.FindProperty("chalkboardGuideText").objectReferenceValue = chalkboard.text;

            // Right Wing
            so.FindProperty("rightNextSignText").objectReferenceValue = rightWing.signText;
            so.FindProperty("rightNextButton").objectReferenceValue = rightWing.arrowBtn;
            so.FindProperty("roleSignText").objectReferenceValue = rightSignpost.roleText;
            so.FindProperty("withSignText").objectReferenceValue = rightSignpost.withText;

            // Front Bench
            so.FindProperty("distinctionsText").objectReferenceValue = frontBench.text;

            so.ApplyModifiedProperties();

            // Refresh UI content
            booth.ForceRegenerateSprites();
            booth.UpdateSlideUI();
            EditorUtility.SetDirty(booth);
        }

        private struct ScreenCanvasResult
        {
            public Text titleText;
            public Button openUrlBtn;
            public Text urlBadgeText;
            public Image projectImage;
            public Text pageIndicatorText;
            public Button prevBtn;
            public Button nextBtn;
            public Button closeBtn;
        }

        private static ScreenCanvasResult SetupScreenCanvas(Transform root, Material purpleMat, Material orangeMat)
        {
            var res = new ScreenCanvasResult();
            var canvasGo = new GameObject("ScreenCanvas");
            canvasGo.transform.SetParent(root, false);
            canvasGo.transform.localPosition = new Vector3(0f, 3.45f, 0.08f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = new Vector3(0.009f, 0.009f, 0.009f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<GraphicRaycaster>();

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(960f, 580f);

            // Dark inner background
            var bg = CreateUIElement<Image>(canvasGo.transform, "Screen_BG");
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;
            bg.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);

            // Title Text (Top centered, large bold white typography)
            res.titleText = CreateUIElement<Text>(canvasGo.transform, "TitleText");
            res.titleText.rectTransform.anchorMin = new Vector2(0.05f, 0.86f);
            res.titleText.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            res.titleText.rectTransform.offsetMin = Vector2.zero;
            res.titleText.rectTransform.offsetMax = Vector2.zero;
            res.titleText.alignment = TextAnchor.MiddleCenter;
            res.titleText.fontSize = 44;
            res.titleText.fontStyle = FontStyle.Bold;
            res.titleText.color = Color.white;
            res.titleText.text = "THREE.JS JOURNEY";

            var outline = res.titleText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            // URL Pill Badge Button (Right below title, vibrant purple)
            var pillImg = CreateUIElement<Image>(canvasGo.transform, "Btn_OpenUrl");
            pillImg.rectTransform.anchorMin = new Vector2(0.30f, 0.75f);
            pillImg.rectTransform.anchorMax = new Vector2(0.70f, 0.84f);
            pillImg.rectTransform.offsetMin = Vector2.zero;
            pillImg.rectTransform.offsetMax = Vector2.zero;
            pillImg.color = new Color(0.60f, 0.26f, 0.88f, 1f); // Bruno Simon signature purple
            res.openUrlBtn = pillImg.gameObject.AddComponent<Button>();

            res.urlBadgeText = CreateUIElement<Text>(pillImg.transform, "UrlText");
            res.urlBadgeText.rectTransform.anchorMin = Vector2.zero;
            res.urlBadgeText.rectTransform.anchorMax = Vector2.one;
            res.urlBadgeText.rectTransform.offsetMin = Vector2.zero;
            res.urlBadgeText.rectTransform.offsetMax = Vector2.zero;
            res.urlBadgeText.alignment = TextAnchor.MiddleCenter;
            res.urlBadgeText.fontSize = 20;
            res.urlBadgeText.fontStyle = FontStyle.Bold;
            res.urlBadgeText.color = Color.white;
            res.urlBadgeText.text = "THREEJS-JOURNEY.COM";

            // Main Project Artwork Image (Large center stage preview)
            res.projectImage = CreateUIElement<Image>(canvasGo.transform, "ProjectPreviewImage");
            res.projectImage.rectTransform.anchorMin = new Vector2(0.06f, 0.14f);
            res.projectImage.rectTransform.anchorMax = new Vector2(0.94f, 0.72f);
            res.projectImage.rectTransform.offsetMin = Vector2.zero;
            res.projectImage.rectTransform.offsetMax = Vector2.zero;

            // Bottom Navigation Pill Bar: [ ◀ ]   [ ■ □ □ □ ]   [ ▶ ]
            var bottomBar = new GameObject("BottomNav_Bar").AddComponent<RectTransform>();
            bottomBar.SetParent(canvasGo.transform, false);
            bottomBar.anchorMin = new Vector2(0.32f, 0.03f);
            bottomBar.anchorMax = new Vector2(0.68f, 0.11f);
            bottomBar.offsetMin = Vector2.zero;
            bottomBar.offsetMax = Vector2.zero;

            // Prev Button
            var prevImg = CreateUIElement<Image>(bottomBar, "Btn_Prev");
            prevImg.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            prevImg.rectTransform.anchorMax = new Vector2(0.24f, 1.0f);
            prevImg.rectTransform.offsetMin = Vector2.zero;
            prevImg.rectTransform.offsetMax = Vector2.zero;
            prevImg.color = new Color(0.60f, 0.26f, 0.88f, 1f);
            res.prevBtn = prevImg.gameObject.AddComponent<Button>();
            var prevTxt = CreateUIElement<Text>(prevImg.transform, "Text");
            prevTxt.rectTransform.anchorMin = Vector2.zero;
            prevTxt.rectTransform.anchorMax = Vector2.one;
            prevTxt.alignment = TextAnchor.MiddleCenter;
            prevTxt.fontSize = 24;
            prevTxt.fontStyle = FontStyle.Bold;
            prevTxt.color = Color.white;
            prevTxt.text = "◀";

            // Page Indicator Dots
            res.pageIndicatorText = CreateUIElement<Text>(bottomBar, "PageIndicatorText");
            res.pageIndicatorText.rectTransform.anchorMin = new Vector2(0.26f, 0.0f);
            res.pageIndicatorText.rectTransform.anchorMax = new Vector2(0.74f, 1.0f);
            res.pageIndicatorText.rectTransform.offsetMin = Vector2.zero;
            res.pageIndicatorText.rectTransform.offsetMax = Vector2.zero;
            res.pageIndicatorText.alignment = TextAnchor.MiddleCenter;
            res.pageIndicatorText.fontSize = 22;
            res.pageIndicatorText.fontStyle = FontStyle.Bold;
            res.pageIndicatorText.color = new Color(0.85f, 0.50f, 1.0f, 1f);
            res.pageIndicatorText.text = "■ □ □ □";

            // Next Button
            var nextImg = CreateUIElement<Image>(bottomBar, "Btn_Next");
            nextImg.rectTransform.anchorMin = new Vector2(0.76f, 0.0f);
            nextImg.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            nextImg.rectTransform.offsetMin = Vector2.zero;
            nextImg.rectTransform.offsetMax = Vector2.zero;
            nextImg.color = new Color(0.60f, 0.26f, 0.88f, 1f);
            res.nextBtn = nextImg.gameObject.AddComponent<Button>();
            var nextTxt = CreateUIElement<Text>(nextImg.transform, "Text");
            nextTxt.rectTransform.anchorMin = Vector2.zero;
            nextTxt.rectTransform.anchorMax = Vector2.one;
            nextTxt.alignment = TextAnchor.MiddleCenter;
            nextTxt.fontSize = 24;
            nextTxt.fontStyle = FontStyle.Bold;
            nextTxt.color = Color.white;
            nextTxt.text = "▶";

            // Close button top-right (✕)
            var closeImg = CreateUIElement<Image>(canvasGo.transform, "Btn_Close");
            closeImg.rectTransform.anchorMin = new Vector2(0.93f, 0.89f);
            closeImg.rectTransform.anchorMax = new Vector2(0.98f, 0.97f);
            closeImg.rectTransform.offsetMin = Vector2.zero;
            closeImg.rectTransform.offsetMax = Vector2.zero;
            closeImg.color = new Color(0.25f, 0.28f, 0.35f, 0.95f);
            res.closeBtn = closeImg.gameObject.AddComponent<Button>();
            var closeTxt = CreateUIElement<Text>(closeImg.transform, "Text");
            closeTxt.rectTransform.anchorMin = Vector2.zero;
            closeTxt.rectTransform.anchorMax = Vector2.one;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.fontSize = 22;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.color = Color.white;
            closeTxt.text = "✕";

            return res;
        }

        private struct WingResult
        {
            public Text signText;
            public Button arrowBtn;
        }

        private static WingResult SetupLeftWing(Transform root, Material darkWood, Material lightPlank, Material purpleMat)
        {
            var res = new WingResult();
            var wing = new GameObject("Left_Wing").transform;
            wing.SetParent(root, false);
            // Viewer's LEFT is +X
            wing.localPosition = new Vector3(6.6f, 4.4f, 0f);

            // Overhead arm & chains
            CreateCube(wing, "Arm_Beam", new Vector3(0f, 1.15f, 0f), new Vector3(2.8f, 0.3f, 0.3f), darkWood);
            CreateCube(wing, "Chain_L", new Vector3(-0.85f, 0.55f, 0f), new Vector3(0.08f, 0.9f, 0.08f), darkWood);
            CreateCube(wing, "Chain_R", new Vector3(0.85f, 0.55f, 0f), new Vector3(0.08f, 0.9f, 0.08f), darkWood);
            // Wooden hanging sign plank
            CreateCube(wing, "Sign_Plank", new Vector3(0f, -0.05f, 0f), new Vector3(2.6f, 1.25f, 0.18f), lightPlank);

            var canvasGo = new GameObject("SignCanvas");
            canvasGo.transform.SetParent(wing, false);
            canvasGo.transform.localPosition = new Vector3(0f, -0.05f, 0.12f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = new Vector3(0.009f, 0.009f, 0.009f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<GraphicRaycaster>();

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280f, 130f);

            res.signText = CreateUIElement<Text>(canvasGo.transform, "SignText");
            res.signText.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
            res.signText.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            res.signText.rectTransform.offsetMin = Vector2.zero;
            res.signText.rectTransform.offsetMax = Vector2.zero;
            res.signText.alignment = TextAnchor.MiddleCenter;
            res.signText.fontSize = 19;
            res.signText.fontStyle = FontStyle.Bold;
            res.signText.lineSpacing = 1.15f;
            res.signText.color = new Color(0.98f, 0.98f, 0.98f);
            res.signText.text = "CITRIX\nREDBULL";

            // Purple Arrow Button below hanging sign
            var btnImg = CreateUIElement<Image>(canvasGo.transform, "Btn_LeftArrow");
            btnImg.rectTransform.anchorMin = new Vector2(0.28f, -0.76f);
            btnImg.rectTransform.anchorMax = new Vector2(0.72f, -0.14f);
            btnImg.rectTransform.offsetMin = Vector2.zero;
            btnImg.rectTransform.offsetMax = Vector2.zero;
            btnImg.color = new Color(0.60f, 0.26f, 0.88f, 1f);
            res.arrowBtn = btnImg.gameObject.AddComponent<Button>();

            var btnTxt = CreateUIElement<Text>(btnImg.transform, "Text");
            btnTxt.rectTransform.anchorMin = Vector2.zero;
            btnTxt.rectTransform.anchorMax = Vector2.one;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.fontSize = 28;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color = Color.white;
            btnTxt.text = "◀";

            return res;
        }

        private static WingResult SetupRightWing(Transform root, Material darkWood, Material lightPlank, Material purpleMat)
        {
            var res = new WingResult();
            var wing = new GameObject("Right_Wing").transform;
            wing.SetParent(root, false);
            // Viewer's RIGHT is -X
            wing.localPosition = new Vector3(-6.6f, 4.4f, 0f);

            // Overhead arm & chains
            CreateCube(wing, "Arm_Beam", new Vector3(0f, 1.15f, 0f), new Vector3(2.8f, 0.3f, 0.3f), darkWood);
            CreateCube(wing, "Chain_L", new Vector3(-0.85f, 0.55f, 0f), new Vector3(0.08f, 0.9f, 0.08f), darkWood);
            CreateCube(wing, "Chain_R", new Vector3(0.85f, 0.55f, 0f), new Vector3(0.08f, 0.9f, 0.08f), darkWood);
            // Wooden hanging sign plank
            CreateCube(wing, "Sign_Plank", new Vector3(0f, -0.05f, 0f), new Vector3(2.6f, 1.25f, 0.18f), lightPlank);

            var canvasGo = new GameObject("SignCanvas");
            canvasGo.transform.SetParent(wing, false);
            canvasGo.transform.localPosition = new Vector3(0f, -0.05f, 0.12f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = new Vector3(0.009f, 0.009f, 0.009f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<GraphicRaycaster>();

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280f, 130f);

            res.signText = CreateUIElement<Text>(canvasGo.transform, "SignText");
            res.signText.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
            res.signText.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            res.signText.rectTransform.offsetMin = Vector2.zero;
            res.signText.rectTransform.offsetMax = Vector2.zero;
            res.signText.alignment = TextAnchor.MiddleCenter;
            res.signText.fontSize = 19;
            res.signText.fontStyle = FontStyle.Bold;
            res.signText.lineSpacing = 1.15f;
            res.signText.color = new Color(0.98f, 0.98f, 0.98f);
            res.signText.text = "BONHOMME\n10 ANS";

            // Purple Arrow Button below hanging sign
            var btnImg = CreateUIElement<Image>(canvasGo.transform, "Btn_RightArrow");
            btnImg.rectTransform.anchorMin = new Vector2(0.28f, -0.76f);
            btnImg.rectTransform.anchorMax = new Vector2(0.72f, -0.14f);
            btnImg.rectTransform.offsetMin = Vector2.zero;
            btnImg.rectTransform.offsetMax = Vector2.zero;
            btnImg.color = new Color(0.60f, 0.26f, 0.88f, 1f);
            res.arrowBtn = btnImg.gameObject.AddComponent<Button>();

            var btnTxt = CreateUIElement<Text>(btnImg.transform, "Text");
            btnTxt.rectTransform.anchorMin = Vector2.zero;
            btnTxt.rectTransform.anchorMax = Vector2.one;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.fontSize = 28;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color = Color.white;
            btnTxt.text = "▶";

            return res;
        }

        private struct ChalkboardResult
        {
            public GameObject root;
            public Text text;
        }

        private static ChalkboardResult SetupChalkboard(Transform root, Material darkWood, Material slateMat, Material fireGlow)
        {
            var res = new ChalkboardResult();
            var chalk = new GameObject("Chalkboard_Guide").transform;
            chalk.SetParent(root, false);
            res.root = chalk.gameObject;

            // Solid physics collider for chalkboard
            var chalkCol = chalk.gameObject.AddComponent<BoxCollider>();
            chalkCol.center = new Vector3(0f, 1.25f, 0f);
            chalkCol.size = new Vector3(2.1f, 2.5f, 0.8f);
            chalkCol.isTrigger = false;

            // Standing A-Frame Chalkboard on viewer's LEFT floor (+X): x = +5.2f, z = 1.4f
            chalk.localPosition = new Vector3(5.2f, 0f, 1.4f);
            // Angled 18 degrees towards the camera
            chalk.localRotation = Quaternion.Euler(0f, -18f, 0f);

            CreateCube(chalk, "Frame_Front", new Vector3(0f, 1.25f, 0.05f), new Vector3(2.1f, 2.5f, 0.15f), darkWood);
            CreateCube(chalk, "Frame_BackLeg", new Vector3(0f, 1.15f, -0.55f), new Vector3(1.8f, 2.3f, 0.12f), darkWood);
            CreateCube(chalk, "Slate_Surface", new Vector3(0f, 1.25f, 0.14f), new Vector3(1.85f, 2.15f, 0.05f), slateMat);

            var canvasGo = new GameObject("ChalkCanvas");
            canvasGo.transform.SetParent(chalk, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.25f, 0.24f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 210f);

            res.text = CreateUIElement<Text>(canvasGo.transform, "GuideText");
            res.text.rectTransform.anchorMin = Vector2.zero;
            res.text.rectTransform.anchorMax = Vector2.one;
            res.text.alignment = TextAnchor.MiddleCenter;
            res.text.fontSize = 24;
            res.text.fontStyle = FontStyle.Bold;
            res.text.lineSpacing = 1.35f;
            res.text.color = new Color(0.95f, 0.98f, 1.0f);
            res.text.text = "NEXT  ➔\nPREV  ⬅\nOPEN  ↵\nEXIT  [ESC]";

            // Cozy Lantern / Brazier next to the chalkboard
            var lantern = new GameObject("Lantern_Cozy").transform;
            lantern.SetParent(chalk, false);
            lantern.localPosition = new Vector3(1.4f, 0.35f, 0.2f);
            CreateCube(lantern, "Base", new Vector3(0f, 0f, 0f), new Vector3(0.6f, 0.7f, 0.6f), darkWood);
            CreateCube(lantern, "FlameCore", new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.45f, 0.35f), fireGlow);

            var warmPoint = lantern.gameObject.AddComponent<Light>();
            warmPoint.type = LightType.Point;
            warmPoint.color = new Color(1.0f, 0.65f, 0.25f);
            warmPoint.intensity = 3.5f;
            warmPoint.range = 5.0f;
            warmPoint.shadows = LightShadows.None;

            return res;
        }

        private struct RightSignpostResult
        {
            public Text roleText;
            public Text withText;
        }

        private static RightSignpostResult SetupRightSignpost(Transform root, Material darkWood, Material lightPlank, Material orangeBadge)
        {
            var res = new RightSignpostResult();
            var post = new GameObject("Right_Signpost").transform;
            post.SetParent(root, false);
            // Signpost on viewer's RIGHT floor (-X): x = -5.2f, z = 1.4f
            post.localPosition = new Vector3(-5.2f, 0f, 1.4f);
            // Angled 18 degrees towards camera
            post.localRotation = Quaternion.Euler(0f, 18f, 0f);

            // Solid physics collider for signpost
            var postCol = post.gameObject.AddComponent<BoxCollider>();
            postCol.center = new Vector3(0f, 1.4f, 0.1f);
            postCol.size = new Vector3(2.8f, 2.8f, 0.6f);
            postCol.isTrigger = false;

            // Upright Pole holding the signpost
            CreateCube(post, "Post_Pole", new Vector3(-1.0f, 1.4f, 0.05f), new Vector3(0.35f, 2.8f, 0.35f), darkWood);

            // Top: ROLE Badge & Plank (Positioned on ground level so it NEVER blocks the wing arrow button above it)
            CreateCube(post, "Role_Badge_3D", new Vector3(-0.65f, 2.20f, 0.20f), new Vector3(1.1f, 0.46f, 0.12f), orangeBadge);
            CreateCube(post, "Role_Plank_3D", new Vector3(0.0f, 1.65f, 0.15f), new Vector3(2.8f, 0.75f, 0.12f), lightPlank);

            // Bottom: WITH Badge & Plank
            CreateCube(post, "With_Badge_3D", new Vector3(-0.65f, 0.95f, 0.20f), new Vector3(1.1f, 0.46f, 0.12f), orangeBadge);
            CreateCube(post, "With_Plank_3D", new Vector3(0.0f, 0.40f, 0.15f), new Vector3(2.8f, 0.75f, 0.12f), lightPlank);

            // Canvas placed at Z=0.32f to completely prevent Z-fighting with 3D wood cubes
            var canvasGo = new GameObject("SignpostCanvas");
            canvasGo.transform.SetParent(post, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.05f, 0.32f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 320f);

            // Canvas coordinate mapping (Y=180 rotation flips X, so +65 in canvas is -0.65 in 3D):
            var roleBadgeTxt = CreateUIElement<Text>(canvasGo.transform, "RoleBadgeText");
            roleBadgeTxt.rectTransform.anchoredPosition = new Vector2(65f, 115f);
            roleBadgeTxt.rectTransform.sizeDelta = new Vector2(100f, 40f);
            roleBadgeTxt.alignment = TextAnchor.MiddleCenter;
            roleBadgeTxt.fontSize = 17;
            roleBadgeTxt.fontStyle = FontStyle.Bold;
            roleBadgeTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            roleBadgeTxt.color = Color.white;
            roleBadgeTxt.text = "ROLE";

            res.roleText = CreateUIElement<Text>(canvasGo.transform, "RoleText");
            res.roleText.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            res.roleText.rectTransform.sizeDelta = new Vector2(260f, 70f);
            res.roleText.alignment = TextAnchor.MiddleCenter;
            res.roleText.fontSize = 15;
            res.roleText.fontStyle = FontStyle.Bold;
            res.roleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            res.roleText.verticalOverflow = VerticalWrapMode.Truncate;
            res.roleText.lineSpacing = 1.1f;
            res.roleText.color = Color.white;
            res.roleText.text = "DEVELOPER\nFORMATER";

            var withBadgeTxt = CreateUIElement<Text>(canvasGo.transform, "WithBadgeText");
            withBadgeTxt.rectTransform.anchoredPosition = new Vector2(65f, -10f);
            withBadgeTxt.rectTransform.sizeDelta = new Vector2(100f, 40f);
            withBadgeTxt.alignment = TextAnchor.MiddleCenter;
            withBadgeTxt.fontSize = 17;
            withBadgeTxt.fontStyle = FontStyle.Bold;
            withBadgeTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            withBadgeTxt.color = Color.white;
            withBadgeTxt.text = "WITH";

            res.withText = CreateUIElement<Text>(canvasGo.transform, "WithText");
            res.withText.rectTransform.anchoredPosition = new Vector2(0f, -65f);
            res.withText.rectTransform.sizeDelta = new Vector2(260f, 70f);
            res.withText.alignment = TextAnchor.MiddleCenter;
            res.withText.fontSize = 15;
            res.withText.fontStyle = FontStyle.Bold;
            res.withText.horizontalOverflow = HorizontalWrapMode.Wrap;
            res.withText.verticalOverflow = VerticalWrapMode.Truncate;
            res.withText.lineSpacing = 1.1f;
            res.withText.color = Color.white;
            res.withText.text = "HERVÉ STUDIO\nBONHOMME PARIS";

            return res;
        }

        private struct FrontBenchResult
        {
            public Text text;
        }

        private static FrontBenchResult SetupFrontBench(Transform root, Material warmWood, Material darkWood, Material orangeBadge, Material goldMat, Material bookPaper)
        {
            var res = new FrontBenchResult();
            var bench = new GameObject("Front_Bench").transform;
            bench.SetParent(root, false);

            // Solid physics collider for front bench
            var benchCol = bench.gameObject.AddComponent<BoxCollider>();
            benchCol.center = new Vector3(0f, 0.5f, 0f);
            benchCol.size = new Vector3(3.8f, 1.0f, 1.2f);
            benchCol.isTrigger = false;

            // Wooden Bench on front center: x = 0f, z = 2.4f
            bench.localPosition = new Vector3(0f, 0f, 2.4f);

            // Table top and legs (warm caramel wood)
            CreateCube(bench, "Bench_Top", new Vector3(0f, 0.70f, 0f), new Vector3(3.8f, 0.22f, 1.2f), warmWood);
            CreateCube(bench, "Leg_L", new Vector3(1.5f, 0.35f, 0f), new Vector3(0.28f, 0.70f, 0.95f), darkWood);
            CreateCube(bench, "Leg_R", new Vector3(-1.5f, 0.35f, 0f), new Vector3(0.28f, 0.70f, 0.95f), darkWood);

            // 3D Low-Poly Open Book on viewer's left side of table (+X)
            var book = new GameObject("3D_OpenBook").transform;
            book.SetParent(bench, false);
            book.localPosition = new Vector3(0.65f, 0.84f, 0f);
            CreateCube(book, "Page_Left", new Vector3(0.32f, 0.04f, 0f), new Vector3(0.60f, 0.08f, 0.80f), bookPaper);
            CreateCube(book, "Page_Right", new Vector3(-0.32f, 0.04f, 0f), new Vector3(0.60f, 0.08f, 0.80f), bookPaper);
            CreateCube(book, "Cover_Spine", new Vector3(0f, 0.01f, 0f), new Vector3(1.30f, 0.04f, 0.85f), darkWood);

            // 3D Trophy / Award on viewer's right side of table (-X)
            CreateCube(bench, "Award_Trophy", new Vector3(-0.95f, 1.12f, 0f), new Vector3(0.95f, 0.60f, 0.25f), goldMat);

            // Distinctions Plaque on front edge of bench
            CreateCube(bench, "Distinctions_Plank", new Vector3(0f, 0.50f, 0.62f), new Vector3(3.6f, 0.44f, 0.10f), orangeBadge);

            var canvasGo = new GameObject("BenchCanvas");
            canvasGo.transform.SetParent(bench, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.50f, 0.74f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = new Vector3(0.009f, 0.009f, 0.009f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 48f);

            res.text = CreateUIElement<Text>(canvasGo.transform, "DistinctionsText");
            res.text.rectTransform.anchorMin = Vector2.zero;
            res.text.rectTransform.anchorMax = Vector2.one;
            res.text.alignment = TextAnchor.MiddleCenter;
            res.text.fontSize = 18;
            res.text.fontStyle = FontStyle.Bold;
            res.text.color = Color.white;
            res.text.text = "DISTINCTIONS • FWA OF THE DAY";

            return res;
        }

        private static GameObject CreateCube(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.identity;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
            }

            return go;
        }

        private static T CreateUIElement<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var comp = go.AddComponent<T>();
            if (comp is Text txt)
            {
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.font");
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
            }
            return comp;
        }

        private static Material GetOrCreateMaterial(string name, Color color, bool isEmissive = false)
        {
            string path = $"Assets/_Project/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                if (isEmissive)
                {
                    existing.EnableKeyword("_EMISSION");
                    if (existing.HasProperty("_EmissionColor")) existing.SetColor("_EmissionColor", color * 2.5f);
                }
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

            if (isEmissive)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2.5f);
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
#endif
