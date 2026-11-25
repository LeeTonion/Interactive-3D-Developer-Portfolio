using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Interactive Sci-Fi button with smooth hover scale, border glow, press feedback,
    /// disabled state styling, and active/selected highlight.
    /// </summary>
    public class HangarSciFiButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Button _button;
        private Image _bg;
        private Outline _outline;
        private TextMeshProUGUI _label;

        private Color _normalBg;
        private Color _hoverBg;
        private Color _normalBorder;
        private Color _normalText;

        private bool _isHovered;
        private bool _isPressed;
        private bool _isSelected;
        private bool _isInteractable = true;

        private float _targetScale = 1f;
        private Color _targetBg;
        private Color _targetBorder;

        public void Initialize(Button btn, Image bg, Outline outline, TextMeshProUGUI label, Color normalBg, Color hoverBg, Color borderCol, Color textCol)
        {
            _button = btn;
            _bg = bg;
            _outline = outline;
            _label = label;
            _normalBg = normalBg;
            _hoverBg = hoverBg;
            _normalBorder = borderCol;
            _normalText = textCol;
            RefreshTargets();
        }

        public void SetText(string text)
        {
            if (_label != null && _label.text != text)
                _label.text = text;
        }

        public void SetPalette(Color normalBg, Color hoverBg, Color borderCol, Color textCol)
        {
            _normalBg = normalBg;
            _hoverBg = hoverBg;
            _normalBorder = borderCol;
            _normalText = textCol;
            RefreshTargets();
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
            if (_button != null) _button.interactable = interactable;
            if (!interactable)
            {
                _isHovered = false;
                _isPressed = false;
            }
            RefreshTargets();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            RefreshTargets();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            _isHovered = true;
            RefreshTargets();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            RefreshTargets();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            _isPressed = true;
            RefreshTargets();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            RefreshTargets();
        }

        private void RefreshTargets()
        {
            if (!_isInteractable)
            {
                _targetScale = 1f;
                _targetBg = HangarUITheme.BtnDisabled;
                _targetBorder = new Color(0.25f, 0.35f, 0.45f, 0.35f);
                if (_label != null) _label.color = HangarUITheme.TextMuted;
                return;
            }

            if (_label != null) _label.color = _normalText;

            if (_isPressed)
            {
                _targetScale = 0.96f;
                _targetBg = _hoverBg;
                _targetBorder = Color.white;
            }
            else if (_isSelected)
            {
                _targetScale = _isHovered ? 1.03f : 1.015f;
                _targetBg = _hoverBg;
                _targetBorder = Color.Lerp(_normalBorder, Color.white, 0.45f);
            }
            else if (_isHovered)
            {
                _targetScale = 1.03f;
                _targetBg = _hoverBg;
                _targetBorder = Color.Lerp(_normalBorder, Color.white, 0.35f);
            }
            else
            {
                _targetScale = 1f;
                _targetBg = _normalBg;
                _targetBorder = _normalBorder;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime * 14f;
            Vector3 s = transform.localScale;
            float ns = Mathf.Lerp(s.x, _targetScale, dt);
            if (Mathf.Abs(ns - s.x) > 0.0005f)
                transform.localScale = new Vector3(ns, ns, 1f);

            if (_bg != null)
                _bg.color = Color.Lerp(_bg.color, _targetBg, dt);
            if (_outline != null)
                _outline.effectColor = Color.Lerp(_outline.effectColor, _targetBorder, dt);
        }
    }

    /// <summary>
    /// Smoothly animated progress bar with color interpolation support.
    /// </summary>
    public class HangarAnimatedBar : MonoBehaviour
    {
        private Image _fill;
        private TextMeshProUGUI _valueText;
        private float _targetFill = 0.5f;
        private Color _targetColor;

        public void Initialize(Image fill, TextMeshProUGUI valueText, Color initialColor)
        {
            _fill = fill;
            _valueText = valueText;
            _targetColor = initialColor;
        }

        public void SetProgress(float normalized01, string labelText, Color barColor)
        {
            _targetFill = Mathf.Clamp01(normalized01);
            _targetColor = barColor;
            if (_valueText != null && _valueText.text != labelText)
            {
                _valueText.text = labelText;
                _valueText.color = barColor;
            }
        }

        private void Update()
        {
            if (_fill == null) return;
            float dt = Time.deltaTime * 9f;
            if (Mathf.Abs(_fill.fillAmount - _targetFill) > 0.001f)
                _fill.fillAmount = Mathf.Lerp(_fill.fillAmount, _targetFill, dt);
            _fill.color = Color.Lerp(_fill.color, _targetColor, dt);
        }
    }

    /// <summary>
    /// Lightweight blinking status indicator dot for normal, warning, and critical states.
    /// </summary>
    public class HangarStatusIndicator : MonoBehaviour
    {
        private Image _img;
        private Color _baseColor = HangarUITheme.NeonCyan;
        private float _blinkSpeed = 0f;

        private void Awake()
        {
            _img = GetComponent<Image>();
        }

        public void SetState(Color color, float blinkHz)
        {
            _baseColor = color;
            _blinkSpeed = blinkHz;
            if (_img != null && blinkHz <= 0.01f)
                _img.color = color;
        }

        private void Update()
        {
            if (_img == null || _blinkSpeed <= 0.01f) return;
            float pulse = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * _blinkSpeed * Mathf.PI));
            _img.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, pulse);
        }
    }

    /// <summary>
    /// Subtle holographic scanning bar sweeping vertically across the monitor glass.
    /// </summary>
    public class HangarScanlineSweep : MonoBehaviour
    {
        private RectTransform _rt;
        private float _halfRange = 170f;
        private float _speed = 55f;
        private float _y;

        public void Configure(float halfHeight, float speed = 52f)
        {
            _rt = GetComponent<RectTransform>();
            _halfRange = halfHeight;
            _speed = speed;
            _y = halfHeight;
        }

        private void Update()
        {
            if (_rt == null) return;
            _y -= Time.deltaTime * _speed;
            if (_y < -_halfRange) _y = _halfRange;
            _rt.anchoredPosition = new Vector2(0f, _y);
        }
    }
}
