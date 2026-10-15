using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using CodeDrive.UI;
using CodeDrive.Core;

namespace CodeDrive.Editor
{
    public class BuildPauseOverlay : EditorWindow
    {
        [MenuItem("CodeDrive/Build Pause Overlay")]
        public static void Build()
        {
            // Find Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas found in scene!");
                return;
            }

            // Remove existing PauseOverlay if present
            var existing = canvas.transform.Find("PauseOverlay");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            // Find UITheme
            UITheme uiTheme = null;
            var themes = Resources.FindObjectsOfTypeAll<UITheme>();
            if (themes.Length > 0) uiTheme = themes[0];

            Color primary = uiTheme != null ? uiTheme.Primary : new Color(1f, 0.65f, 0.2f);
            Color primaryLight = uiTheme != null ? uiTheme.PrimaryLight : new Color(1f, 0.75f, 0.35f);
            Color primaryDark = uiTheme != null ? uiTheme.PrimaryDark : new Color(0.85f, 0.55f, 0.15f);
            Color panelBg = uiTheme != null ? uiTheme.PanelBackground : new Color(0.1f, 0.1f, 0.12f, 0.98f);
            Color bgOverlay = new Color(0.04f, 0.04f, 0.06f, 0.85f);
            Color panelBgDark = uiTheme != null ? uiTheme.Background : new Color(0.06f, 0.06f, 0.08f, 0.95f);
            Color dividerCol = uiTheme != null ? uiTheme.Divider : new Color(0.3f, 0.3f, 0.35f, 0.3f);
            Color textPrimary = uiTheme != null ? uiTheme.TextPrimary : Color.white;
            Color textSecondary = uiTheme != null ? uiTheme.TextSecondary : new Color(0.7f, 0.7f, 0.75f);

            // ─── Create PauseOverlay root ───
            var overlayObj = new GameObject("PauseOverlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.SetActive(false);

            var cg = overlayObj.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            var bgImg = overlayObj.AddComponent<Image>();
            bgImg.color = bgOverlay;
            bgImg.raycastTarget = true;

            var rect = overlayObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var pauseMenuUI = overlayObj.AddComponent<PauseMenuUI>();

            // ─── Create MenuCard ───
            var menuCard = new GameObject("MenuCard");
            menuCard.transform.SetParent(overlayObj.transform, false);
            var mcImg = menuCard.AddComponent<Image>();
            mcImg.color = panelBg;
            mcImg.type = Image.Type.Sliced;

            var mcRect = menuCard.GetComponent<RectTransform>();
            mcRect.anchorMin = new Vector2(0.5f, 0.5f);
            mcRect.anchorMax = new Vector2(0.5f, 0.5f);
            mcRect.anchoredPosition = Vector2.zero;
            mcRect.sizeDelta = new Vector2(400, 500);

            var vLayout = menuCard.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(24, 24, 24, 24);
            vLayout.spacing = 16;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            var csf = menuCard.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            var titleObj = CreateText("Title", "PAUSED", primary, 36, FontStyles.Bold);
            titleObj.transform.SetParent(menuCard.transform, false);
            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 60;

            // Divider
            var dividerObj = new GameObject("Divider");
            dividerObj.transform.SetParent(menuCard.transform, false);
            var dividerImg = dividerObj.AddComponent<Image>();
            dividerImg.color = dividerCol;
            var dividerRect = dividerObj.GetComponent<RectTransform>();
            dividerRect.sizeDelta = new Vector2(0, 1);
            var dividerLayout = dividerObj.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 1;
            dividerLayout.flexibleWidth = 1;

            // Buttons
            Button resumeBtn = CreateButton("ResumeButton", "RESUME", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
            Button settingsBtn = CreateButton("SettingsButton", "SETTINGS", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
            Button controlsBtn = CreateButton("ControlsButton", "CONTROLS", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
            Button mainMenuBtn = CreateButton("MainMenuButton", "MAIN MENU", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
            Button quitBtn = CreateButton("QuitButton", "QUIT", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);

            // ─── Create Settings Panel ───
            var settingsPanel = CreateSubPanel("SettingsPanel", "SETTINGS", panelBgDark, dividerCol, primary, textSecondary, primary, primaryLight, primaryDark, 0.1f);
            settingsPanel.transform.SetParent(overlayObj.transform, false);
            settingsPanel.SetActive(false);

            // ─── Create Controls Panel ───
            var controlsPanel = CreateSubPanel("ControlsPanel", "CONTROLS", panelBgDark, dividerCol, primary, textSecondary, primary, primaryLight, primaryDark, 0.1f);
            controlsPanel.transform.SetParent(overlayObj.transform, false);
            controlsPanel.SetActive(false);

            // ─── Assign fields via reflection ───
            AssignField(pauseMenuUI, "resumeButton", resumeBtn);
            AssignField(pauseMenuUI, "settingsButton", settingsBtn);
            AssignField(pauseMenuUI, "controlsButton", controlsBtn);
            AssignField(pauseMenuUI, "mainMenuButton", mainMenuBtn);
            AssignField(pauseMenuUI, "quitButton", quitBtn);
            AssignField(pauseMenuUI, "settingsPanel", settingsPanel);
            AssignField(pauseMenuUI, "controlsPanel", controlsPanel);
            AssignField(pauseMenuUI, "uiTheme", uiTheme);

            // ─── Assign to UIManager ───
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                AssignField(uiManager, "pauseOverlay", overlayObj);
                AssignField(uiManager, "pauseMenuUI", pauseMenuUI);
                AssignField(uiManager, "settingsPanel", settingsPanel);
                AssignField(uiManager, "controlsPanel", controlsPanel);
                Debug.Log("PauseOverlay built and assigned to UIManager!");
            }
            else
            {
                Debug.LogWarning("UIManager.Instance not found - assign manually in Inspector");
            }

            // Mark dirty
            EditorUtility.SetDirty(overlayObj);
            EditorUtility.SetDirty(pauseMenuUI);
            if (uiManager != null) EditorUtility.SetDirty(uiManager);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("✅ PauseOverlay built successfully! Press Escape in Play Mode to test.");
        }

        static Button CreateButton(string name, string label, Transform parent, Color primary, Color highlight, Color pressed, float fadeDur)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var btn = btnObj.AddComponent<Button>();
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = primary;
            btnImg.type = Image.Type.Sliced;

            var colors = btn.colors;
            colors.normalColor = primary;
            colors.highlightedColor = highlight;
            colors.pressedColor = pressed;
            colors.selectedColor = primary;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = fadeDur;
            btn.colors = colors;

            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(300, 50);

            var layout = btnObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 300;
            layout.preferredHeight = 50;
            layout.flexibleWidth = 1;

            // Button text
            var textObj = CreateText(name + "Text", label, Color.white, 16, FontStyles.Bold);
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btn;
        }

        static GameObject CreateSubPanel(string name, string title, Color panelBg, Color dividerCol, Color primary, Color textSec, Color btnPrimary, Color btnHighlight, Color btnPressed, float fadeDur)
        {
            var panelObj = new GameObject(name);
            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = panelBg;
            panelImg.type = Image.Type.Sliced;

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500, 400);

            var vLayout = panelObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(24, 24, 24, 24);
            vLayout.spacing = 16;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            var csf = panelObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            var titleObj = CreateText("Title", title, primary, 28, FontStyles.Bold);
            titleObj.transform.SetParent(panelObj.transform, false);
            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 50;

            // Placeholder content
            var contentObj = CreateText("Content", $"{title} panel content goes here", textSec, 16, FontStyles.Normal);
            contentObj.transform.SetParent(panelObj.transform, false);
            var contentLayout = contentObj.AddComponent<LayoutElement>();
            contentLayout.preferredHeight = 200;
            contentLayout.flexibleWidth = 1;

            // Back button
            var backBtn = CreateButton("BackButton", "BACK", panelObj.transform, btnPrimary, btnHighlight, btnPressed, fadeDur);
            var backBtnRect = backBtn.GetComponent<RectTransform>();
            backBtnRect.sizeDelta = new Vector2(200, 40);

            return panelObj;
        }

        static GameObject CreateText(string name, string text, Color color, int fontSize, FontStyles style)
        {
            var obj = new GameObject(name);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return obj;
        }

        static void AssignField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}