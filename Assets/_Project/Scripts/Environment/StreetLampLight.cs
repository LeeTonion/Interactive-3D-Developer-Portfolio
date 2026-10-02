using UnityEngine;

namespace CodeDrive.Environment
{
    /// <summary>
    /// Attach this to a downward street lamp bulb or light fixture prefab.
    /// Automatically manages downward spotlight & emissive bulb glow,
    /// turning ON exclusively at Night and fading OFF during Daytime
    /// via the Day/Night cycle system.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class StreetLampLight : MonoBehaviour
    {
        [Header("Mode & Preview")]
        [Tooltip("When enabled, light previews in the Editor Scene view even when not playing.")]
        [SerializeField] private bool previewInEditor = false;

        [Tooltip("Force light to stay ON 24/7 (even in daytime).")]
        [SerializeField] private bool forceAlwaysOn = false;

        [Header("Light Intensity (URP High Brightness)")]
        [Tooltip("Main downward spotlight intensity (URP requires 50 - 500 for visible road illumination).")]
        [Range(10f, 1000f)]
        [SerializeField] private float targetSpotIntensity = 250f;

        [Tooltip("Wide area point light intensity around the street lamp.")]
        [Range(0f, 300f)]
        [SerializeField] private float targetPointIntensity = 60f;

        [Tooltip("Light reach distance in meters.")]
        [Range(5f, 50f)]
        [SerializeField] private float lightRange = 22f;

        [Tooltip("Spotlight cone angle (degrees).")]
        [Range(30f, 120f)]
        [SerializeField] private float spotAngle = 85f;

        [Header("Light Color & Bulb Glow")]
        [SerializeField] private Color lightColor = new Color(1.0f, 0.85f, 0.55f, 1.0f); // Warm Golden Streetlight

        [ColorUsage(true, true)]
        [SerializeField] private Color emissionColor = new Color(1.0f, 0.85f, 0.55f, 1.0f);

        [Range(1f, 20f)]
        [SerializeField] private float emissionIntensity = 6f;

        [Header("Transitions")]
        [SerializeField] private float transitionSpeed = 3.5f;

        [Header("References (Auto-found if empty)")]
        [SerializeField] private Light spotLight;
        [SerializeField] private Light pointLight;
        [SerializeField] private Renderer bulbRenderer;

        private float _currentWeight = 0f;
        private MaterialPropertyBlock _propBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Reset()
        {
            FindReferences();
            ApplySettings();
        }

        private void OnValidate()
        {
            FindReferences();
            ApplySettings();
            if (!Application.isPlaying)
            {
                ApplyLightWeight(previewInEditor || forceAlwaysOn ? 1f : 0f);
            }
        }

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            FindReferences();
        }

        private void Start()
        {
            FindReferences();
            ApplySettings();

            if (Application.isPlaying)
            {
                if (forceAlwaysOn)
                {
                    _currentWeight = 1f;
                }
                else
                {
                    bool isNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
                    _currentWeight = isNight ? 1f : 0f;
                }
                ApplyLightWeight(_currentWeight);
            }
            else
            {
                ApplyLightWeight(previewInEditor || forceAlwaysOn ? 1f : 0f);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                float editWeight = (previewInEditor || forceAlwaysOn) ? 1f : 0f;
                ApplyLightWeight(editWeight);
                return;
            }

            // In Play Mode
            if (forceAlwaysOn)
            {
                if (_currentWeight < 0.99f)
                {
                    _currentWeight = Mathf.MoveTowards(_currentWeight, 1f, Time.deltaTime * transitionSpeed);
                    ApplyLightWeight(_currentWeight);
                }
                return;
            }

            // Strictly check Day/Night system
            bool isNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
            float targetWeight = isNight ? 1f : 0f;

            if (!Mathf.Approximately(_currentWeight, targetWeight))
            {
                _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, Time.deltaTime * transitionSpeed);
                ApplyLightWeight(_currentWeight);
            }
        }

        public void FindReferences()
        {
            if (spotLight == null)
            {
                Transform spotTr = transform.Find("SpotLight_Down");
                if (spotTr != null) spotLight = spotTr.GetComponent<Light>();
                if (spotLight == null)
                {
                    var lights = GetComponentsInChildren<Light>(true);
                    foreach (var l in lights)
                    {
                        if (l.type == LightType.Spot)
                        {
                            spotLight = l;
                            break;
                        }
                    }
                }
            }

            if (pointLight == null)
            {
                Transform ptTr = transform.Find("PointLight_Wide");
                if (ptTr != null) pointLight = ptTr.GetComponent<Light>();
                if (pointLight == null)
                {
                    var lights = GetComponentsInChildren<Light>(true);
                    foreach (var l in lights)
                    {
                        if (l.type == LightType.Point && l != spotLight)
                        {
                            pointLight = l;
                            break;
                        }
                    }
                }
            }

            if (bulbRenderer == null)
            {
                Transform bTr = transform.Find("Bulb_Mesh");
                if (bTr != null) bulbRenderer = bTr.GetComponent<Renderer>();
                if (bulbRenderer == null) bulbRenderer = GetComponentInChildren<Renderer>(true);
            }
        }

        public void ApplySettings()
        {
            // Configure Downward Spotlight
            if (spotLight != null)
            {
                spotLight.type = LightType.Spot;
                spotLight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                spotLight.range = lightRange;
                spotLight.spotAngle = spotAngle;
                spotLight.innerSpotAngle = spotAngle * 0.5f;
                spotLight.color = lightColor;
                spotLight.shadows = LightShadows.Soft;
                spotLight.cullingMask = ~0; // Everything
                spotLight.renderingLayerMask = ~0; // All light layers
            }

            // Configure Wide Ambient Point Light
            if (pointLight != null)
            {
                pointLight.type = LightType.Point;
                pointLight.range = lightRange * 0.75f;
                pointLight.color = lightColor;
                pointLight.shadows = LightShadows.None;
                pointLight.cullingMask = ~0;
                pointLight.renderingLayerMask = ~0;
            }
        }

        public void ApplyLightWeight(float weight)
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            bool enable = weight > 0.005f;

            if (spotLight != null)
            {
                spotLight.enabled = enable;
                spotLight.intensity = weight * targetSpotIntensity;
                spotLight.color = lightColor;
                spotLight.range = lightRange;
                spotLight.spotAngle = spotAngle;
            }

            if (pointLight != null)
            {
                pointLight.enabled = enable;
                pointLight.intensity = weight * targetPointIntensity;
                pointLight.color = lightColor;
                pointLight.range = lightRange * 0.75f;
            }

            if (bulbRenderer != null)
            {
                bulbRenderer.GetPropertyBlock(_propBlock);
                Color currentEmission = emissionColor * (weight * emissionIntensity);
                _propBlock.SetColor(EmissionColorId, currentEmission);
                bulbRenderer.SetPropertyBlock(_propBlock);
            }
        }

        [ContextMenu("Toggle Force Always On")]
        public void ToggleForceAlwaysOn()
        {
            forceAlwaysOn = !forceAlwaysOn;
            ApplyLightWeight(forceAlwaysOn ? 1f : 0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = lightColor;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawRay(transform.position, Vector3.down * lightRange);
        }
    }
}
