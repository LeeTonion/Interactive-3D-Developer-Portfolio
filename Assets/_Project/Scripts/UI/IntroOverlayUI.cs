using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CodeDrive.UI
{
    /// <summary>
    /// Bruno Simon-inspired intro welcome overlay.
    /// Shows the developer name and tagline with a smooth fade-in animation,
    /// then fades away after a few seconds to reveal the 3D world.
    /// </summary>
    public class IntroOverlayUI : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float showDuration = 3.5f;
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float fadeOutDuration = 1.5f;

        [Header("Content")]
        [SerializeField] private string titleText = "LEE TONIOM";
        [SerializeField] private string subtitleText = "INTERACTIVE 3D DEVELOPER PORTFOLIO";
        [SerializeField] private string hintText = "USE WASD OR ARROW KEYS TO DRIVE  •  CLICK GLOWING MARKERS TO EXPLORE";

        [Header("Colors")]
        [SerializeField] private Color bgColor = new Color(0.06f, 0.06f, 0.08f, 0.95f);
        [SerializeField] private Color titleColor = new Color(1f, 0.65f, 0.2f);           // Bruno orange
        [SerializeField] private Color subtitleColor = new Color(0.85f, 0.85f, 0.85f);
        [SerializeField] private Color hintColor = new Color(0.55f, 0.55f, 0.6f);

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            CreateOverlay();
            StartCoroutine(RunIntroSequence());
        }

        private void CreateOverlay()
        {
            // Create Canvas
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999; // On top of everything

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;

            // Background panel
            var bgObj = new GameObject("IntroBg");
            bgObj.transform.SetParent(transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = bgColor;
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Title
            var titleObj = CreateTextElement("IntroTitle", titleText, titleColor, 72, FontStyle.Bold);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.55f);
            titleRect.anchorMax = new Vector2(0.5f, 0.55f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(1200, 120);

            // Subtitle
            var subObj = CreateTextElement("IntroSubtitle", subtitleText, subtitleColor, 22, FontStyle.Normal);
            var subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.45f);
            subRect.anchorMax = new Vector2(0.5f, 0.45f);
            subRect.anchoredPosition = Vector2.zero;
            subRect.sizeDelta = new Vector2(1000, 50);

            // Decorative line under subtitle
            var lineObj = new GameObject("IntroLine");
            lineObj.transform.SetParent(transform, false);
            var lineImg = lineObj.AddComponent<Image>();
            lineImg.color = titleColor;
            var lineRect = lineObj.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.43f);
            lineRect.anchorMax = new Vector2(0.5f, 0.43f);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(200, 3);

            // Hint text at bottom
            var hintObj = CreateTextElement("IntroHint", hintText, hintColor, 16, FontStyle.Italic);
            var hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0.15f);
            hintRect.anchorMax = new Vector2(0.5f, 0.15f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(1000, 40);
        }

        private GameObject CreateTextElement(string name, string text, Color color, int fontSize, FontStyle style)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.color = color;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return obj;
        }

        private IEnumerator RunIntroSequence()
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(showDuration);

            // Fade out
            elapsed = 0f;
            _canvasGroup.blocksRaycasts = false;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;

            // Cleanup
            Destroy(gameObject, 0.5f);
        }
    }
}
