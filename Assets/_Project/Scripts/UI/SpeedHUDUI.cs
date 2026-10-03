using UnityEngine;
using UnityEngine.UI;

namespace CodeDrive.UI
{
    /// <summary>
    /// Bruno Simon-inspired Speed HUD.
    /// Shows the current vehicle speed as a minimal, clean overlay in km/h.
    /// Style: bottom-center, large white number, subtle warm background.
    /// </summary>
    public class SpeedHUDUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CarControl car;

        [Header("Display")]
        [SerializeField] private string unit = "km/h";
        [SerializeField] private bool smoothDisplay = true;
        [SerializeField] private float smoothSpeed = 5f;

        // Runtime
        private Canvas _canvas;
        private Text _speedText;
        private Text _unitText;
        private Image _bgImage;
        private float _displayedSpeed;

        // Bruno Simon palette
        private static readonly Color BgColor    = new Color(0.08f, 0.07f, 0.06f, 0.75f);
        private static readonly Color SpeedColor = new Color(0.98f, 0.95f, 0.90f);
        private static readonly Color UnitColor  = new Color(1f, 0.60f, 0.15f);

        private void Start()
        {
            if (car == null) car = FindObjectOfType<CarControl>();
            CreateHUD();
        }

        private void CreateHUD()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Outer container: bottom-left, minimal pill
            var container = new GameObject("SpeedContainer");
            container.transform.SetParent(transform, false);

            var bgRect = container.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(0f, 0f);
            bgRect.pivot     = new Vector2(0f, 0f);
            bgRect.anchoredPosition = new Vector2(30f, 30f);
            bgRect.sizeDelta = new Vector2(130f, 70f);

            // Background rounded-ish card
            _bgImage = container.AddComponent<Image>();
            _bgImage.color = BgColor;

            // Speed number text
            var speedObj = new GameObject("SpeedNumber");
            speedObj.transform.SetParent(container.transform, false);

            var speedRect = speedObj.AddComponent<RectTransform>();
            speedRect.anchorMin = new Vector2(0f, 0.35f);
            speedRect.anchorMax = new Vector2(1f, 1f);
            speedRect.offsetMin = new Vector2(8f, 0f);
            speedRect.offsetMax = new Vector2(-8f, -4f);

            _speedText = speedObj.AddComponent<Text>();
            _speedText.text = "0";
            _speedText.color = SpeedColor;
            _speedText.fontSize = 42;
            _speedText.fontStyle = FontStyle.Bold;
            _speedText.alignment = TextAnchor.MiddleCenter;
            _speedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Unit text
            var unitObj = new GameObject("SpeedUnit");
            unitObj.transform.SetParent(container.transform, false);

            var unitRect = unitObj.AddComponent<RectTransform>();
            unitRect.anchorMin = new Vector2(0f, 0f);
            unitRect.anchorMax = new Vector2(1f, 0.38f);
            unitRect.offsetMin = new Vector2(4f, 2f);
            unitRect.offsetMax = new Vector2(-4f, 0f);

            _unitText = unitObj.AddComponent<Text>();
            _unitText.text = unit;
            _unitText.color = UnitColor;
            _unitText.fontSize = 15;
            _unitText.fontStyle = FontStyle.Bold;
            _unitText.alignment = TextAnchor.MiddleCenter;
            _unitText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Thin orange accent line above unit text
            var lineObj = new GameObject("AccentLine");
            lineObj.transform.SetParent(container.transform, false);

            var lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.1f, 0.36f);
            lineRect.anchorMax = new Vector2(0.9f, 0.36f);
            lineRect.sizeDelta = new Vector2(0f, 2f);

            var lineImg = lineObj.AddComponent<Image>();
            lineImg.color = UnitColor;
        }

        private void Update()
        {
            if (car == null || _speedText == null) return;

            // Convert m/s to km/h
            float speedKmh = car.CurrentSpeed * 3.6f;

            if (smoothDisplay)
            {
                _displayedSpeed = Mathf.Lerp(_displayedSpeed, speedKmh, Time.deltaTime * smoothSpeed);
            }
            else
            {
                _displayedSpeed = speedKmh;
            }

            _speedText.text = Mathf.RoundToInt(_displayedSpeed).ToString();

            // Color-shift speed text: white → warm orange as speed increases
            float t = Mathf.InverseLerp(0f, 80f, _displayedSpeed);
            _speedText.color = Color.Lerp(SpeedColor, UnitColor, t * 0.5f);
        }
    }
}
