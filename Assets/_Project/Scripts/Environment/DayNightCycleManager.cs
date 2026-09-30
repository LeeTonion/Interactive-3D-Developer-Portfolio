using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodeDrive.Environment
{
    /// <summary>
    /// Clean, crystal-clear Day/Night cycle.
    /// Completely removes any red/dusty haze artifacts.
    /// Transitions smoothly between Clean Sky Blue (Day) and Deep Midnight Navy (Night).
    /// Keeps nearby fog far from the player (45m - 300m) for maximum visual clarity.
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; private set; }

        [Header("Time & Speed")]
        [Tooltip("1 full 24h day = 300s (5 minutes)")]
        [SerializeField] private float dayDurationInSeconds = 300f;

        [Range(0f, 1f)]
        [Tooltip("0.0 = Midnight, 0.25 = Sunrise 06:00, 0.5 = Noon 12:00, 0.75 = Sunset 18:00")]
        [SerializeField] private float currentTimeOfDay = 0.30f; // Start in clear morning

        [SerializeField] private bool pauseCycle = false;

        [Header("Sun Reference")]
        [SerializeField] private Light sunLight;

        // Clean, pure colors (NO RED/BROWN DUST)
        private static readonly Color DaySkyColor   = new Color(0.55f, 0.75f, 0.95f); // Clean clear sky blue
        private static readonly Color NightSkyColor = new Color(0.012f, 0.022f, 0.045f); // Deep clean midnight dark

        private static readonly Color DaySunColor   = new Color(1.00f, 0.98f, 0.92f); // Warm sunlight
        private static readonly Color DuskSunColor  = new Color(1.00f, 0.88f, 0.70f); // Soft warm gold (gentle, not red)

        [Header("Fog Distance Settings")]
        [SerializeField] private float dayFogStart = 60f;
        [SerializeField] private float dayFogEnd = 320f;
        [SerializeField] private float nightFogStart = 45f;
        [SerializeField] private float nightFogEnd = 240f;

        public bool IsNight { get; private set; }
        public float CurrentTimeOfDay => currentTimeOfDay;

        public event Action<bool> OnDayNightToggled;
        public event Action<float> OnTimeUpdated;

        private Material _runtimeSkyboxMat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (sunLight == null)
            {
                var sunGo = GameObject.Find("Directional Light");
                if (sunGo != null) sunLight = sunGo.GetComponent<Light>();
            }

            if (RenderSettings.skybox != null)
            {
                _runtimeSkyboxMat = new Material(RenderSettings.skybox);
                RenderSettings.skybox = _runtimeSkyboxMat;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.ambientMode = AmbientMode.Trilight;

            ApplyEnvironment(currentTimeOfDay);
        }

        private void Update()
        {
            if (!pauseCycle && dayDurationInSeconds > 0f)
            {
                currentTimeOfDay += (Time.deltaTime / dayDurationInSeconds);
                if (currentTimeOfDay >= 1f) currentTimeOfDay -= 1f;
            }

            // Night is between 19:15 (0.80) and 05:45 (0.24)
            bool nightNow = currentTimeOfDay < 0.24f || currentTimeOfDay > 0.80f;
            if (nightNow != IsNight)
            {
                IsNight = nightNow;
                OnDayNightToggled?.Invoke(IsNight);
            }

            ApplyEnvironment(currentTimeOfDay);
            OnTimeUpdated?.Invoke(currentTimeOfDay);
        }

        public void ApplyEnvironment(float t)
        {
            // 1. Calculate continuous smooth day factor [0 = night, 1 = midday]
            // Sun rises at 0.25 (06:00), peaks at 0.50 (12:00), sets at 0.75 (18:00)
            float sunAngle = (t * 360f) - 90f;
            float sunElevation = Mathf.Sin((t - 0.25f) * Mathf.PI * 2f);

            // Smooth cosine blend between Day and Night (no piecewise jumps)
            float dayWeight = Mathf.Clamp01(sunElevation * 2.2f);
            dayWeight = Mathf.SmoothStep(0f, 1f, dayWeight);

            // 2. Pure Clean Sky & Fog Color (Interpolates strictly between Day Blue and Deep Night Navy)
            Color currentSkyColor = Color.Lerp(NightSkyColor, DaySkyColor, dayWeight);

            // Fog is ALWAYS identical to Sky color, so the world blends cleanly into horizon with ZERO red tint
            RenderSettings.fogColor = currentSkyColor;
            RenderSettings.fogStartDistance = Mathf.Lerp(nightFogStart, dayFogStart, dayWeight);
            RenderSettings.fogEndDistance = Mathf.Lerp(nightFogEnd, dayFogEnd, dayWeight);

            // 3. Sun Directional Light
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
                sunLight.color = Color.Lerp(DuskSunColor, DaySunColor, dayWeight);
                sunLight.intensity = dayWeight * 1.30f;
                sunLight.enabled = dayWeight > 0.005f;
            }

            // 4. Runtime Skybox properties
            if (_runtimeSkyboxMat != null)
            {
                if (_runtimeSkyboxMat.HasProperty("_SkyTint"))
                    _runtimeSkyboxMat.SetColor("_SkyTint", currentSkyColor);
                if (_runtimeSkyboxMat.HasProperty("_GroundColor"))
                    _runtimeSkyboxMat.SetColor("_GroundColor", currentSkyColor);
                if (_runtimeSkyboxMat.HasProperty("_Exposure"))
                {
                    float exposure = Mathf.Lerp(0.06f, 1.25f, dayWeight);
                    _runtimeSkyboxMat.SetFloat("_Exposure", exposure);
                }
            }

            // 5. Clean Ambient Trilight
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.015f, 0.025f, 0.05f), // Midnight Navy
                new Color(0.72f, 0.82f, 0.96f),   // Day Sky
                dayWeight
            );

            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(0.01f, 0.018f, 0.035f),
                new Color(0.55f, 0.60f, 0.68f),
                dayWeight
            );

            RenderSettings.ambientGroundColor = Color.Lerp(
                new Color(0.005f, 0.01f, 0.02f),
                new Color(0.35f, 0.35f, 0.38f),
                dayWeight
            );
        }

        public void SetTimeOfDay(float time01)
        {
            currentTimeOfDay = Mathf.Repeat(time01, 1f);
            ApplyEnvironment(currentTimeOfDay);
        }

        private void OnDestroy()
        {
            if (_runtimeSkyboxMat != null) Destroy(_runtimeSkyboxMat);
        }
    }
}
