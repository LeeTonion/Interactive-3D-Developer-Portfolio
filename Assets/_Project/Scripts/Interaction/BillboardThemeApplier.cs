using UnityEngine;
using UnityEngine.UI;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Applies a sleek, high-contrast, modern dark showcase theme to all billboards.
    /// Eliminates blinding white glare at night, provides razor-sharp typography,
    /// and ensures all navigation & action buttons are crystal clear and readable.
    /// </summary>
    public class BillboardThemeApplier : MonoBehaviour
    {
        [Header("Modern Dark Showcase Palette")]
        [SerializeField] private Color canvasBgColor = new Color(0.10f, 0.11f, 0.15f, 0.97f);    // Deep dark slate
        [SerializeField] private Color titleColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);            // Pure crisp white
        [SerializeField] private Color subtitleColor = new Color(0.35f, 0.82f, 1.0f, 1.0f);        // Electric cyan / gold
        [SerializeField] private Color descColor = new Color(0.90f, 0.92f, 0.96f, 1.0f);            // High contrast off-white
        [SerializeField] private Color techStackColor = new Color(1.0f, 0.65f, 0.20f, 1.0f);       // Glowing warm amber
        [SerializeField] private Color pageIndicatorColor = new Color(1.0f, 0.85f, 0.40f, 1.0f);   // Golden indicator

        [Header("Button Colors")]
        [SerializeField] private Color prevBtnBg = new Color(0.20f, 0.22f, 0.28f, 1.0f);           // Dark slate pill
        [SerializeField] private Color nextBtnBg = new Color(1.0f, 0.42f, 0.08f, 1.0f);           // Vibrant orange
        [SerializeField] private Color openUrlBtnBg = new Color(0.12f, 0.52f, 0.96f, 1.0f);        // Tech blue
        [SerializeField] private Color closeBtnBg = new Color(0.28f, 0.30f, 0.38f, 0.95f);         // Neutral slate
        [SerializeField] private Color btnTextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);          // Bold pure white

        [Header("3D Billboard Frame")]
        [SerializeField] private Color postColor = new Color(0.14f, 0.15f, 0.18f, 1.0f);          // Dark steel posts
        [SerializeField] private Color frameColor = new Color(0.18f, 0.20f, 0.24f, 1.0f);         // Frame border
        [SerializeField] private Color headerAccent = new Color(1.0f, 0.45f, 0.10f, 1.0f);        // Orange accent

        [Header("Marker Colors")]
        [SerializeField] private Color markerGlow = new Color(1.0f, 0.78f, 0.20f, 1.0f);          // Golden marker
        [SerializeField] private Color speechBubbleBg = new Color(0.12f, 0.14f, 0.18f, 0.96f);    // Dark card
        [SerializeField] private Color speechBubbleText = new Color(1.0f, 1.0f, 1.0f, 1.0f);      // White text

        private void Start()
        {
            ApplyThemeToAllBooths();
        }

        public void ApplyThemeToAllBooths()
        {
            var booths = FindObjectsByType<ShowcaseBooth3D>(FindObjectsSortMode.None);

            foreach (var booth in booths)
            {
                ApplyThemeToBooth(booth);
            }

            Debug.Log($"[BillboardTheme] Applied high-contrast dark theme to {booths.Length} booths.");
        }

        public void ApplyThemeToBooth(ShowcaseBooth3D booth)
        {
            var t = booth.transform;

            // 1. Theme Billboard 3D Structure
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                string lower = rend.gameObject.name.ToLower();
                if (lower.Contains("post") || lower.Contains("pole") || lower.Contains("leg"))
                {
                    SetRendererColor(rend, postColor);
                }
                else if (lower.Contains("board") || lower.Contains("panel") || lower.Contains("back"))
                {
                    SetRendererColor(rend, frameColor);
                }
                else if (lower.Contains("header") || lower.Contains("stripe") || lower.Contains("top"))
                {
                    SetRendererColor(rend, headerAccent);
                }
            }

            // 2. Theme ScreenCanvas UI
            var screenCanvas = FindDeep(t, "ScreenCanvas");
            if (screenCanvas != null)
            {
                // Background image
                var bgTransform = screenCanvas.Find("Screen_BG");
                if (bgTransform != null)
                {
                    var bgImg = bgTransform.GetComponent<Image>();
                    if (bgImg != null) bgImg.color = canvasBgColor;
                }

                // Content Texts
                SetTextColor(screenCanvas, "TitleText", titleColor, 44, FontStyle.Bold);
                SetTextColor(screenCanvas, "SubtitleText", subtitleColor, 22, FontStyle.Normal);
                SetTextColor(screenCanvas, "DescriptionText", descColor, 24, FontStyle.Normal);
                SetTextColor(screenCanvas, "TechStackText", techStackColor, 20, FontStyle.Bold);
                SetTextColor(screenCanvas, "PageIndicatorText", pageIndicatorColor, 22, FontStyle.Bold);

                // Buttons with clear contrasting backgrounds & bold white text
                StyleButton(screenCanvas, "Btn_Prev", prevBtnBg, btnTextColor, "◀ PREV", 22);
                StyleButton(screenCanvas, "Btn_Next", nextBtnBg, btnTextColor, "NEXT ▶", 22);
                StyleButton(screenCanvas, "Btn_OpenUrl", openUrlBtnBg, btnTextColor, "VIEW PROJECT ↗", 22);
                StyleButton(screenCanvas, "Btn_Close", closeBtnBg, btnTextColor, "✕", 28);
            }

            // 3. Theme 3D Floating Diamond Marker
            var markers = booth.GetComponentsInChildren<FloatingMarker3D>(true);
            foreach (var marker in markers)
            {
                var diamond = FindDeep(marker.transform, "DiamondMarker");
                if (diamond != null)
                {
                    var diamondRend = diamond.GetComponent<Renderer>();
                    if (diamondRend != null)
                    {
                        SetRendererColor(diamondRend, markerGlow);
                        diamondRend.material.EnableKeyword("_EMISSION");
                        diamondRend.material.SetColor("_EmissionColor", markerGlow * 2.5f);
                    }
                }
            }

            // 4. Theme SpeechBubbleCanvas
            var bubbleCanvas = FindDeep(t, "SpeechBubbleCanvas");
            if (bubbleCanvas != null)
            {
                var bubbleImages = bubbleCanvas.GetComponentsInChildren<Image>(true);
                foreach (var img in bubbleImages)
                {
                    img.color = speechBubbleBg;
                }
                var bubbleTexts = bubbleCanvas.GetComponentsInChildren<Text>(true);
                foreach (var txt in bubbleTexts)
                {
                    txt.color = speechBubbleText;
                    txt.fontStyle = FontStyle.Bold;
                }
            }
        }

        private void SetTextColor(Transform parent, string name, Color color, int minSize, FontStyle style)
        {
            var target = parent.Find(name);
            if (target != null)
            {
                var txt = target.GetComponent<Text>();
                if (txt != null)
                {
                    txt.color = color;
                    txt.fontSize = Mathf.Max(txt.fontSize, minSize);
                    txt.fontStyle = style;
                }
            }
        }

        private void StyleButton(Transform parent, string btnName, Color bgColor, Color textColor, string defaultText, int fontSize)
        {
            var btnTr = parent.Find(btnName);
            if (btnTr == null) return;

            var img = btnTr.GetComponent<Image>();
            if (img != null)
            {
                img.color = bgColor;
            }

            var btn = btnTr.GetComponent<Button>();
            if (btn != null)
            {
                var cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                cb.selectedColor = Color.white;
                btn.colors = cb;
            }

            var txt = btnTr.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.color = textColor;
                txt.fontStyle = FontStyle.Bold;
                txt.fontSize = fontSize;
                if (!string.IsNullOrEmpty(defaultText)) txt.text = defaultText;
            }
        }

        private static void SetRendererColor(Renderer rend, Color color)
        {
            if (rend != null && rend.material != null)
            {
                rend.material.color = color;
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindDeep(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
