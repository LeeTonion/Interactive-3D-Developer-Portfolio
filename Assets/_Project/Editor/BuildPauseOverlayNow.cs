using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using CodeDrive.UI;
using CodeDrive.Core;

public class BuildPauseOverlayNow

{
    [MenuItem("CodeDrive/Build Pause Overlay Now")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("No Canvas"); return; }

        var existing = canvas.transform.Find("PauseOverlay");
        if (existing != null) DestroyImmediate(existing.gameObject);

        UITheme uiTheme = null;
        var themes = Resources.FindObjectsOfTypeAll<UITheme>();
        if (themes.Length > 0) uiTheme = themes[0];

        Color primary = uiTheme != null ? uiTheme.Primary : new Color(1f, 0.65f, 0.2f);
        Color primaryLight = uiTheme != null ? uiTheme.PrimaryLight : new Color(1f, 0.75f, 0.35f);
        Color primaryDark = uiTheme != null ? uiTheme.PrimaryDark : new Color(0.85f, 0.55f, 0.15f);
        Color panelBg = uiTheme != null ? uiTheme.PanelBackground : new Color(0.1f, 0.1f, 0.12f, 0.98f);
        Color panelBgDark = uiTheme != null ? uiTheme.Background : new Color(0.06f, 0.06f, 0.08f, 0.95f);
        Color dividerCol = uiTheme != null ? uiTheme.Divider : new Color(0.3f, 0.3f, 0.35f, 0.3f);
        Color textPrimary = uiTheme != null ? uiTheme.TextPrimary : Color.white;
        Color textSecondary = uiTheme != null ? uiTheme.TextSecondary : new Color(0.7f, 0.7f, 0.75f);

        var overlayObj = new GameObject("PauseOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        overlayObj.SetActive(false);

        var cg = overlayObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;

        var bgImg = overlayObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.04f, 0.06f, 0.85f);
        bgImg.raycastTarget = true;

        var rect = overlayObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var pauseMenuUI = overlayObj.AddComponent<PauseMenuUI>();

        var menuCard = new GameObject("MenuCard");
        menuCard.transform.SetParent(overlayObj.transform, false);
        var mcImg = menuCard.AddComponent<Image>();
        mcImg.color = panelBg; mcImg.type = Image.Type.Sliced;

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

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(menuCard.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "PAUSED";
        titleTmp.color = primary;
        titleTmp.fontSize = 36;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.textWrappingMode = TextWrappingModes.Normal;
        titleTmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 60;

        var dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(menuCard.transform, false);
        var dividerImg = dividerObj.AddComponent<Image>();
        dividerImg.color = dividerCol;
        var dividerRect = dividerObj.GetComponent<RectTransform>();
        dividerRect.sizeDelta = new Vector2(0, 1);
        var dividerLayout = dividerObj.AddComponent<LayoutElement>();
        dividerLayout.preferredHeight = 1;
        dividerLayout.flexibleWidth = 1;

        Button resumeBtn = CreateButton("ResumeButton", "RESUME", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
        Button settingsBtn = CreateButton("SettingsButton", "SETTINGS", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
        Button controlsBtn = CreateButton("ControlsButton", "CONTROLS", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
        Button mainMenuBtn = CreateButton("MainMenuButton", "MAIN MENU", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);
        Button quitBtn = CreateButton("QuitButton", "QUIT", menuCard.transform, primary, primaryLight, primaryDark, 0.1f);

        var settingsPanel = CreateSubPanel("SettingsPanel", "SETTINGS", panelBgDark, dividerCol, primary, textSecondary, primary, primaryLight, primaryDark, 0.1f);
        settingsPanel.transform.SetParent(overlayObj.transform, false);
        settingsPanel.SetActive(false);

        var controlsPanel = CreateSubPanel("ControlsPanel", "CONTROLS", panelBgDark, dividerCol, primary, textSecondary, primary, primaryLight, primaryDark, 0.1f);
        controlsPanel.transform.SetParent(overlayObj.transform, false);
        controlsPanel.SetActive(false);

        AssignField(pauseMenuUI, "resumeButton", resumeBtn);
        AssignField(pauseMenuUI, "settingsButton", settingsBtn);
        AssignField(pauseMenuUI, "controlsButton", controlsBtn);
        AssignField(pauseMenuUI, "mainMenuButton", mainMenuBtn);
        AssignField(pauseMenuUI, "quitButton", quitBtn);
        AssignField(pauseMenuUI, "settingsPanel", settingsPanel);
        AssignField(pauseMenuUI, "controlsPanel", controlsPanel);
        AssignField(pauseMenuUI, "uiTheme", uiTheme);

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
            Debug.LogWarning("UIManager.Instance not found");
        }

        var gm = GameManager.Instance;
        if (gm != null)
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Project/Settings/CodeDriveInputActions.inputactions");
            if (inputActions != null)
            {
                var field = typeof(GameManager).GetField("inputActions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(gm, inputActions);
                Debug.Log("InputActions assigned to GameManager");
            }
        }

        EditorUtility.SetDirty(overlayObj);
        EditorUtility.SetDirty(pauseMenuUI);
        if (uiManager != null) EditorUtility.SetDirty(uiManager);
        if (gm != null) EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("✅ PauseOverlay COMPLETE - Press Escape in Play Mode!");
    }

    static Button CreateButton(string name, string label, Transform parent, Color pri, Color hi, Color pr, float fade)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btn = btnObj.AddComponent<Button>();
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = pri; btnImg.type = Image.Type.Sliced;
        var colors = btn.colors;
        colors.normalColor = pri; colors.highlightedColor = hi; colors.pressedColor = pr;
        colors.selectedColor = pri; colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); colors.fadeDuration = fade;
        btn.colors = colors;
        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(300, 50);
        var layout = btnObj.AddComponent<LayoutElement>();
        layout.preferredWidth = 300; layout.preferredHeight = 50; layout.flexibleWidth = 1;
        var textObj = new GameObject(name + "Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.color = Color.white; tmp.fontSize = 16; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        return btn;
    }

    static GameObject CreateSubPanel(string name, string title, Color panelBg, Color divCol, Color pri, Color txtSec, Color btnPri, Color btnHi, Color btnPr, float fade)
    {
        var panelObj = new GameObject(name);
        var panelImg = panelObj.AddComponent<Image>();
        panelImg.color = panelBg; panelImg.type = Image.Type.Sliced;
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
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title; titleTmp.color = pri; titleTmp.fontSize = 28; titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.textWrappingMode = TextWrappingModes.Normal;
        titleTmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 50;
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(panelObj.transform, false);
        var contentTmp = contentObj.AddComponent<TextMeshProUGUI>();
        contentTmp.text = title + " panel content goes here"; contentTmp.color = txtSec; contentTmp.fontSize = 16; contentTmp.fontStyle = FontStyles.Normal;
        contentTmp.alignment = TextAlignmentOptions.Center; contentTmp.textWrappingMode = TextWrappingModes.Normal;
        contentTmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var contentLayout = contentObj.AddComponent<LayoutElement>();
        contentLayout.preferredHeight = 200; contentLayout.flexibleWidth = 1;
        var backBtn = CreateButton("BackButton", "BACK", panelObj.transform, btnPri, btnHi, btnPr, fade);
        var backBtnRect = backBtn.GetComponent<RectTransform>();
        backBtnRect.sizeDelta = new Vector2(200, 40);
        return panelObj;
    }

    static void AssignField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null) field.SetValue(target, value);
        else Debug.LogWarning("Field '" + fieldName + "' not found on " + target.GetType().Name);
    }
}