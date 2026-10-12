using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CodeDrive.UI
{
    /// <summary>
    /// Centralized UI Design System - colors, fonts, spacing, panel styles.
    /// Assign to all portfolio panels for consistent look.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "CodeDrive/UI/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Color Palette")]
        [SerializeField] private Color primary = new Color(1f, 0.65f, 0.2f);      // Bruno orange
        [SerializeField] private Color primaryDark = new Color(0.85f, 0.55f, 0.15f);
        [SerializeField] private Color primaryLight = new Color(1f, 0.75f, 0.35f);
        [SerializeField] private Color background = new Color(0.06f, 0.06f, 0.08f, 0.95f);
        [SerializeField] private Color panelBackground = new Color(0.1f, 0.1f, 0.12f, 0.98f);
        [SerializeField] private Color textPrimary = new Color(0.95f, 0.95f, 0.95f);
        [SerializeField] private Color textSecondary = new Color(0.7f, 0.7f, 0.75f);
        [SerializeField] private Color textMuted = new Color(0.55f, 0.55f, 0.6f);
        [SerializeField] private Color accent = new Color(0.2f, 0.85f, 0.5f);
        [SerializeField] private Color border = new Color(0.25f, 0.25f, 0.3f, 0.5f);
        [SerializeField] private Color divider = new Color(0.3f, 0.3f, 0.35f, 0.3f);

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset headingFont;
        [SerializeField] private TMP_FontAsset bodyFont;
        [SerializeField] private int headingSize = 32;
        [SerializeField] private int subheadingSize = 20;
        [SerializeField] private int bodySize = 16;
        [SerializeField] private int captionSize = 14;
        [SerializeField] private float lineHeight = 1.4f;
        [SerializeField] private float letterSpacing = 0.5f;

        [Header("Spacing (px)")]
        [SerializeField] private int spacingXS = 4;
        [SerializeField] private int spacingSM = 8;
        [SerializeField] private int spacingMD = 16;
        [SerializeField] private int spacingLG = 24;
        [SerializeField] private int spacingXL = 32;
        [SerializeField] private int spacingXXL = 48;

        [Header("Panel Style")]
        [SerializeField] private float panelCornerRadius = 12f;
        [SerializeField] private Vector4 panelPadding = new Vector4(24, 24, 24, 24); // L, T, R, B
        [SerializeField] private float panelBorderWidth = 1f;
        [SerializeField] private bool panelShadow = true;
        [SerializeField] private Color panelShadowColor = new Color(0, 0, 0, 0.3f);
        [SerializeField] private Vector2 panelShadowOffset = new Vector2(0, 4);
        [SerializeField] private float panelShadowBlur = 16f;

        [Header("Button Style")]
        [SerializeField] private float buttonCornerRadius = 8f;
        [SerializeField] private Vector2 buttonPadding = new Vector2(24, 12);
        [SerializeField] private int buttonFontSize = 16;
        [SerializeField] private float buttonTransitionDuration = 0.1f;

        [Header("Input Field Style")]
        [SerializeField] private float inputCornerRadius = 6f;
        [SerializeField] private Vector2 inputPadding = new Vector2(12, 10);
        [SerializeField] private int inputFontSize = 16;

        // ─── Properties ───
        public Color Primary => primary;
        public Color PrimaryDark => primaryDark;
        public Color PrimaryLight => primaryLight;
        public Color Background => background;
        public Color PanelBackground => panelBackground;
        public Color TextPrimary => textPrimary;
        public Color TextSecondary => textSecondary;
        public Color TextMuted => textMuted;
        public Color Accent => accent;
        public Color Border => border;
        public Color Divider => divider;

        public TMP_FontAsset HeadingFont => headingFont;
        public TMP_FontAsset BodyFont => bodyFont;
        public int HeadingSize => headingSize;
        public int SubheadingSize => subheadingSize;
        public int BodySize => bodySize;
        public int CaptionSize => captionSize;
        public float LineHeight => lineHeight;
        public float LetterSpacing => letterSpacing;

        public int SpacingXS => spacingXS;
        public int SpacingSM => spacingSM;
        public int SpacingMD => spacingMD;
        public int SpacingLG => spacingLG;
        public int SpacingXL => spacingXL;
        public int SpacingXXL => spacingXXL;

        public float PanelCornerRadius => panelCornerRadius;
        public Vector4 PanelPadding => panelPadding;
        public float PanelBorderWidth => panelBorderWidth;
        public bool PanelShadow => panelShadow;
        public Color PanelShadowColor => panelShadowColor;
        public Vector2 PanelShadowOffset => panelShadowOffset;
        public float PanelShadowBlur => panelShadowBlur;

        public float ButtonCornerRadius => buttonCornerRadius;
        public Vector2 ButtonPadding => buttonPadding;
        public int ButtonFontSize => buttonFontSize;
        public float ButtonTransitionDuration => buttonTransitionDuration;

        public float InputCornerRadius => inputCornerRadius;
        public Vector2 InputPadding => inputPadding;
        public int InputFontSize => inputFontSize;

        // ─── Helper Methods ───
        public void ApplyToText(TMP_Text text, TextStyle style = TextStyle.Body)
        {
            if (text == null) return;
            text.font = style == TextStyle.Heading ? headingFont : bodyFont;
            text.fontSize = style switch
            {
                TextStyle.Heading => headingSize,
                TextStyle.Subheading => subheadingSize,
                TextStyle.Body => bodySize,
                TextStyle.Caption => captionSize,
                _ => bodySize
            };
            text.lineSpacing = (lineHeight - 1f) * 100f; // TMP uses percentage
            text.characterSpacing = letterSpacing;
            text.color = style == TextStyle.Caption ? textMuted : textPrimary;
        }

        public void ApplyToPanel(Image panelImage, RectTransform rectTransform)
        {
            if (panelImage != null)
            {
                panelImage.color = panelBackground;
                panelImage.type = Image.Type.Sliced;
            }
            // Corner radius handled by Image's sprite (9-slice) or separate RoundRect
        }

        public void ApplyToButton(Button btn, TMP_Text btnText)
        {
            if (btn != null)
            {
                var colors = btn.colors;
                colors.normalColor = primary;
                colors.highlightedColor = primaryLight;
                colors.pressedColor = primaryDark;
                colors.selectedColor = primary;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                colors.fadeDuration = buttonTransitionDuration;
                btn.colors = colors;
            }
            if (btnText != null)
            {
                btnText.font = bodyFont;
                btnText.fontSize = buttonFontSize;
                btnText.color = Color.white;
                btnText.fontStyle = FontStyles.Bold;
            }
        }

        /// <summary>
        /// Text style presets for consistent typography.
        /// </summary>
        public enum TextStyle
        {
            Heading,
            Subheading,
            Body,
            Caption
        }
    }
}
