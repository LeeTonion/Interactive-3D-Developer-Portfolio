using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeDrive.Environment;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Simulates realistic lighting on the Showcase Booth billboard panel.
    /// 
    /// Behavior:
    ///  - Daytime  → Panel is bright warm-white with subtle shadow depth
    ///  - Night    → Panel dims to dark (ambient night level)
    ///  - Car headlights nearby → Panel brightens realistically based on
    ///    angle, distance, and spotlight cone — exactly like a real billboard
    ///    lit up at night by a car's headlights.
    /// 
    /// This creates a dramatic cinematic effect: driving up to a dark booth
    /// at night and watching the billboard "light up" as your headlights hit it.
    /// </summary>
    public class ShowcaseBoothLighting : MonoBehaviour
    {
        [Header("Panel References (auto-found if empty)")]
        [Tooltip("The main white background Image of the billboard")]
        [SerializeField] private Image panelBackground;
        [Tooltip("All Text components on the billboard to tint")]
        [SerializeField] private List<Text> panelTexts = new List<Text>();
        [Tooltip("All secondary Image elements (buttons, borders)")]
        [SerializeField] private List<Image> panelImages = new List<Image>();

        [Header("Day Colors (Bright)")]
        [SerializeField] private Color dayPanelColor    = new Color(0.97f, 0.95f, 0.90f, 1f); // Warm cream
        [SerializeField] private Color dayTitleColor    = new Color(0.10f, 0.08f, 0.07f, 1f); // Dark charcoal
        [SerializeField] private Color daySubtitleColor = new Color(0.40f, 0.36f, 0.32f, 1f);
        [SerializeField] private Color dayDescColor     = new Color(0.28f, 0.25f, 0.22f, 1f);
        [SerializeField] private Color dayAccentColor   = new Color(1f, 0.55f, 0.15f, 1f);   // Orange

        [Header("Night Colors (Dark ambient — car headlights add brightness)")]
        [SerializeField] private Color nightPanelColor    = new Color(0.10f, 0.09f, 0.08f, 1f); // Very dark
        [SerializeField] private Color nightTitleColor    = new Color(0.30f, 0.28f, 0.25f, 1f);
        [SerializeField] private Color nightSubtitleColor = new Color(0.22f, 0.20f, 0.18f, 1f);
        [SerializeField] private Color nightDescColor     = new Color(0.20f, 0.18f, 0.16f, 1f);
        [SerializeField] private Color nightAccentColor   = new Color(0.35f, 0.15f, 0.02f, 1f); // Dim orange

        [Header("Headlight Effect Settings")]
        [Tooltip("Maximum illumination radius from car headlights")]
        [SerializeField] private float headlightMaxRange = 35f;
        [Tooltip("How tightly the spotlight cone matters (higher = more directional)")]
        [SerializeField] private float spotlightDirectionPower = 3f;
        [Tooltip("Maximum brightness boost from headlights (0-1 above night level)")]
        [SerializeField] private float maxHeadlightBoost = 1.0f;
        [Tooltip("Smooth speed of light transition")]
        [SerializeField] private float lightSmoothSpeed = 6f;

        [Header("Shadow Depth Effect")]
        [Tooltip("Slight warm shadow vignette at panel edges (artistic depth)")]
        [SerializeField] private bool enableEdgeDarken = true;

        // State
        private CarControl _car;
        private Light[] _carLights;
        private float _currentBrightness = 1f;
        private float _targetBrightness  = 1f;

        // Cache original colors (name-based classification)
        private class TextEntry { public Text text; public string role; }
        private List<TextEntry> _textEntries = new List<TextEntry>();

        private void Start()
        {
            _car = FindObjectOfType<CarControl>();
            if (_car != null)
                _carLights = _car.GetComponentsInChildren<Light>(true);

            AutoFindComponents();
            CacheTextRoles();
        }

        private void AutoFindComponents()
        {
            if (panelBackground == null)
            {
                foreach (var img in GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name.Contains("Screen_BG") ||
                        img.gameObject.name.Contains("Panel_BG") ||
                        img.gameObject.name.Contains("Board"))
                    {
                        panelBackground = img;
                        break;
                    }
                }
            }

            if (panelTexts.Count == 0)
            {
                panelTexts.AddRange(GetComponentsInChildren<Text>(true));
            }

            if (panelImages.Count == 0)
            {
                foreach (var img in GetComponentsInChildren<Image>(true))
                {
                    if (img != panelBackground)
                        panelImages.Add(img);
                }
            }
        }

        private void CacheTextRoles()
        {
            _textEntries.Clear();
            foreach (var txt in panelTexts)
            {
                if (txt == null) continue;
                string name = txt.gameObject.name.ToLower();
                string role;
                if (name.Contains("title") && !name.Contains("sub"))
                    role = "title";
                else if (name.Contains("sub") || name.Contains("role"))
                    role = "subtitle";
                else if (name.Contains("tech") || name.Contains("accent") || name.Contains("dist"))
                    role = "accent";
                else if (name.Contains("page") || name.Contains("indicator"))
                    role = "accent";
                else
                    role = "desc";

                _textEntries.Add(new TextEntry { text = txt, role = role });
            }
        }

        private void Update()
        {
            // 1. Determine base ambient brightness (day vs night)
            float ambientBrightness = GetAmbientBrightness();

            // 2. Calculate headlight contribution
            float headlightBrightness = CalculateHeadlightBrightness();

            // 3. Total brightness = max of ambient + headlight boost
            float rawTarget = Mathf.Clamp01(ambientBrightness + headlightBrightness * (1f - ambientBrightness));

            // 4. Smooth transition
            _targetBrightness = rawTarget;
            _currentBrightness = Mathf.Lerp(_currentBrightness, _targetBrightness, Time.deltaTime * lightSmoothSpeed);

            // 5. Apply colors
            ApplyBrightness(_currentBrightness);
        }

        private float GetAmbientBrightness()
        {
            if (DayNightCycleManager.Instance == null) return 1f;

            float time = DayNightCycleManager.Instance.CurrentTimeOfDay;

            // 0.25 = sunrise, 0.5 = noon, 0.75 = sunset, 0.0/1.0 = midnight
            if (time >= 0.28f && time <= 0.72f)
            {
                // Daytime — full brightness, ease in/out at transitions
                float edgeFade = Mathf.Clamp01(Mathf.InverseLerp(0.28f, 0.33f, time)) *
                                 Mathf.Clamp01(Mathf.InverseLerp(0.72f, 0.67f, time));
                return Mathf.Lerp(0.35f, 1.0f, edgeFade);
            }
            else if (time > 0.72f && time <= 0.80f)
            {
                // Dusk — fading
                return Mathf.Lerp(1.0f, 0.05f, Mathf.InverseLerp(0.72f, 0.80f, time));
            }
            else if (time >= 0.18f && time < 0.28f)
            {
                // Dawn — brightening
                return Mathf.Lerp(0.05f, 1.0f, Mathf.InverseLerp(0.18f, 0.28f, time));
            }
            else
            {
                // Deep night — nearly dark, just tiny ambient
                return 0.04f;
            }
        }

        private float CalculateHeadlightBrightness()
        {
            if (_car == null || _carLights == null) return 0f;

            float totalBoost = 0f;
            Vector3 panelPos = transform.position;
            Vector3 panelNormal = -transform.forward; // Facing direction of billboard

            foreach (var light in _carLights)
            {
                if (light == null || !light.enabled || !light.gameObject.activeInHierarchy) continue;
                if (light.type != LightType.Spot) continue;

                Vector3 lightPos = light.transform.position;
                Vector3 toPanel = (panelPos - lightPos);
                float dist = toPanel.magnitude;
                if (dist > headlightMaxRange) continue;

                // Distance falloff (inverse square)
                float distFactor = Mathf.Clamp01(1f - (dist / headlightMaxRange));
                distFactor = distFactor * distFactor; // Squared falloff

                // Spotlight cone check: is panel within spotlight cone?
                Vector3 lightDir = light.transform.forward;
                float dotLightToPanel = Vector3.Dot(lightDir, toPanel.normalized);
                float halfAngleRad = light.spotAngle * 0.5f * Mathf.Deg2Rad;
                float coneEdge = Mathf.Cos(halfAngleRad);
                if (dotLightToPanel < coneEdge) continue; // Outside cone

                // Cone intensity: center of cone = full, edge = falloff
                float coneFactor = Mathf.Pow(Mathf.InverseLerp(coneEdge, 1f, dotLightToPanel),
                                             spotlightDirectionPower);

                // Panel facing: how directly is the panel facing the light?
                float facingFactor = Mathf.Max(0f, Vector3.Dot(panelNormal, -lightDir));
                facingFactor = Mathf.Lerp(0.3f, 1f, facingFactor); // Some ambient even side-on

                // Combined contribution from this light
                float contribution = distFactor * coneFactor * facingFactor *
                                     (light.intensity / 3f); // Normalize by expected intensity

                totalBoost = Mathf.Max(totalBoost, Mathf.Clamp01(contribution));
            }

            return totalBoost * maxHeadlightBoost;
        }

        private void ApplyBrightness(float t)
        {
            // Lerp between night and day colors based on brightness t
            if (panelBackground != null)
            {
                panelBackground.color = Color.Lerp(nightPanelColor, dayPanelColor, t);
            }

            foreach (var entry in _textEntries)
            {
                if (entry.text == null) continue;
                Color nightC, dayC;
                switch (entry.role)
                {
                    case "title":
                        nightC = nightTitleColor; dayC = dayTitleColor; break;
                    case "subtitle":
                        nightC = nightSubtitleColor; dayC = daySubtitleColor; break;
                    case "accent":
                        nightC = nightAccentColor; dayC = dayAccentColor; break;
                    default:
                        nightC = nightDescColor; dayC = dayDescColor; break;
                }
                entry.text.color = Color.Lerp(nightC, dayC, t);
            }

            // Button images dim proportionally
            foreach (var img in panelImages)
            {
                if (img == null || img == panelBackground) continue;

                string name = img.gameObject.name.ToLower();
                if (name.Contains("btn_next") || name.Contains("openurl"))
                {
                    // Orange accent button: stays more vibrant
                    float btnT = Mathf.Clamp01(t * 1.3f);
                    img.color = Color.Lerp(nightAccentColor, dayAccentColor, btnT);
                }
                else if (name.Contains("close") || name.Contains("prev"))
                {
                    // Dark buttons
                    img.color = Color.Lerp(
                        new Color(0.04f, 0.04f, 0.04f, 1f),
                        new Color(0.10f, 0.10f, 0.10f, 1f),
                        t);
                }
                else
                {
                    img.color = Color.Lerp(nightPanelColor * 0.8f, img.color, t);
                }
            }
        }
    }
}
