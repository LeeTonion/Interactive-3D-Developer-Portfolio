using UnityEngine;
using TMPro;

/// <summary>
/// Lightweight FPS counter overlay. Created/destroyed by SettingsManager
/// when the "Hiển thị FPS" setting toggles.
/// </summary>
public class FpsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private float _accum;
    private int _frames;
    private float _timeLeft;

    private void Awake()
    {
        if (text == null)
        {
            var go = new GameObject("FpsText");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -16f);
            rt.sizeDelta = new Vector2(220f, 60f);
            text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 26;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = new Color(1f, 0.65f, 0.2f);
            text.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        _timeLeft = 0.2f;
        _accum = 0f;
        _frames = 0;
    }

    private void Update()
    {
        _timeLeft -= Time.unscaledDeltaTime;
        _accum += Time.unscaledDeltaTime;
        _frames++;

        if (_timeLeft <= 0f)
        {
            float fps = _frames / _accum;
            if (text != null) text.text = $"FPS {Mathf.RoundToInt(fps)}";
            _timeLeft = 0.2f;
            _accum = 0f;
            _frames = 0;
        }
    }
}
