using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodeDrive.Environment
{
    /// <summary>
    /// Realistic Day/Night cycle with dynamic isometric sun shadows,
    /// high-contrast night atmosphere, smooth ambient transitions,
    /// and keyboard shortcut 'N' to toggle Day/Night.
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; private set; }

        [Header("Time & Speed")]
        [Tooltip("1 full 24h day = 300s (5 minutes)")]
        [SerializeField] private float dayDurationInSeconds = 300f;

        [Range(0f, 1f)]
        [Tooltip("0.0 = Midnight, 0.25 = Sunrise 06:00, 0.5 = Noon 12:00, 0.75 = Sunset 18:00")]
        [SerializeField] private float currentTimeOfDay = 0.35f; // Start in beautiful morning sunlight

        [SerializeField] private bool pauseCycle = false;

        [Header("Sun Reference")]
        [SerializeField] private Light sunLight;

        // Visual Palette (Clean Bruno Simon daytime & Deep Midnight nighttime)
        private static readonly Color DaySkyColor   = new Color(0.70f, 0.82f, 0.95f); // Soft clear sky
        private static readonly Color NightSkyColor = new Color(0.015f, 0.022f, 0.040f); // Deep clean midnight

        private static readonly Color DaySunColor   = new Color(1.00f, 0.97f, 0.90f); // Warm crisp sunlight
        private static readonly Color DuskSunColor  = new Color(1.00f, 0.78f, 0.55f); // Golden sunset

        [Header("Fog Distance Settings")]
        [SerializeField] private float dayFogStart = 70f;
        [SerializeField] private float dayFogEnd = 350f;
        [SerializeField] private float nightFogStart = 45f;
        [SerializeField] private float nightFogEnd = 260f;

        [Header("Night & Street Lamp Thresholds")]
        [Range(0.5f, 1f)]
        [SerializeField] private float duskTurnOnTime = 0.72f;

        [Range(0f, 0.5f)]
        [SerializeField] private float dawnTurnOffTime = 0.26f;

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

            if (sunLight != null)
            {
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = 0.85f;
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
            // Keyboard shortcut 'N' to toggle between Noon and Midnight
            if (Input.GetKeyDown(KeyCode.N))
            {
                ToggleDayNightQuick();
            }

            if (!pauseCycle && dayDurationInSeconds > 0f)
            {
                currentTimeOfDay += (Time.deltaTime / dayDurationInSeconds);
                if (currentTimeOfDay >= 1f) currentTimeOfDay -= 1f;
            }

            // Lights turn ON at dusk and turn OFF at dawn
            bool nightNow = currentTimeOfDay < dawnTurnOffTime || currentTimeOfDay > duskTurnOnTime;
            if (nightNow != IsNight)
            {
                IsNight = nightNow;
                OnDayNightToggled?.Invoke(IsNight);
            }

            ApplyEnvironment(currentTimeOfDay);
            OnTimeUpdated?.Invoke(currentTimeOfDay);
        }

        public void ToggleDayNightQuick()
        {
            if (IsNight)
            {
                // Switch to sunny daytime (10:00 AM)
                SetTimeOfDay(0.42f);
            }
            else
            {
                // Switch to midnight
                SetTimeOfDay(0.05f);
            }
        }

        public void ApplyEnvironment(float t)
        {
            // 1. Calculate continuous smooth day factor [0 = night, 1 = midday]
            // Sun rises at 0.25 (06:00), peaks at 0.50 (12:00), sets at 0.75 (18:00)
            float sunElevation = Mathf.Sin((t - 0.25f) * Mathf.PI * 2f);
            float dayWeight = Mathf.Clamp01(sunElevation * 2.2f);
            dayWeight = Mathf.SmoothStep(0f, 1f, dayWeight);

            // 2. Pure Clean Sky & Fog Color
            Color currentSkyColor = Color.Lerp(NightSkyColor, DaySkyColor, dayWeight);

            RenderSettings.fogColor = currentSkyColor;
            RenderSettings.fogStartDistance = Mathf.Lerp(nightFogStart, dayFogStart, dayWeight);
            RenderSettings.fogEndDistance = Mathf.Lerp(nightFogEnd, dayFogEnd, dayWeight);

            // 3. Sun Directional Light with optimal angled shadows
            if (sunLight != null)
            {
                // Angle the sun dynamically between 35° and 60° pitch for distinct cast shadows
                float pitch = Mathf.Lerp(25f, 52f, dayWeight);
                float yaw = Mathf.Lerp(70f, 140f, dayWeight);
                sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                sunLight.color = Color.Lerp(DuskSunColor, DaySunColor, dayWeight);
                sunLight.intensity = Mathf.Lerp(0f, 1.25f, dayWeight);
                sunLight.enabled = dayWeight > 0.005f;
            }

            // 4. Skybox
            if (_runtimeSkyboxMat != null)
            {
                if (_runtimeSkyboxMat.HasProperty("_SkyTint"))
                    _runtimeSkyboxMat.SetColor("_SkyTint", currentSkyColor);
                if (_runtimeSkyboxMat.HasProperty("_GroundColor"))
                    _runtimeSkyboxMat.SetColor("_GroundColor", currentSkyColor);
                if (_runtimeSkyboxMat.HasProperty("_Exposure"))
                {
                    float exposure = Mathf.Lerp(0.08f, 1.20f, dayWeight);
                    _runtimeSkyboxMat.SetFloat("_Exposure", exposure);
                }
            }

            // 5. Ambient Trilight (Dark enough at night so headlights pop, vibrant in day)
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.012f, 0.018f, 0.035f), // Night sky ambient
                new Color(0.70f, 0.78f, 0.90f),    // Day sky ambient
                dayWeight
            );

            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(0.008f, 0.012f, 0.025f),
                new Color(0.55f, 0.58f, 0.65f),
                dayWeight
            );

            RenderSettings.ambientGroundColor = Color.Lerp(
                new Color(0.004f, 0.006f, 0.012f),
                new Color(0.32f, 0.33f, 0.36f),
                dayWeight
            );
        }

        public void SetTimeOfDay(float time01)
        {
            currentTimeOfDay = Mathf.Repeat(time01, 1f);
            bool nightNow = currentTimeOfDay < dawnTurnOffTime || currentTimeOfDay > duskTurnOnTime;
            if (nightNow != IsNight)
            {
                IsNight = nightNow;
                OnDayNightToggled?.Invoke(IsNight);
            }
            ApplyEnvironment(currentTimeOfDay);
        }

        private void OnDestroy()
        {
            if (_runtimeSkyboxMat != null) Destroy(_runtimeSkyboxMat);
        }
    }
}
