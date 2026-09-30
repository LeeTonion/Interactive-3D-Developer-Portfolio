using System.Collections.Generic;
using UnityEngine;

namespace CodeDrive.Environment
{
    /// <summary>
    /// Controls the Point Lights that are already embedded inside each Street Lamp Prefab.
    /// At night: fades lights ON. During day: fades lights OFF.
    /// Does NOT create any new light objects - works with the existing "PointLight" prefab child.
    /// </summary>
    public class StreetLampManager : MonoBehaviour
    {
        public static StreetLampManager Instance { get; private set; }

        [Header("Lamp Root")]
        [SerializeField] private string streetLampsRootPath = "World/Street lamps";

        [Header("Night Intensity")]
        [Tooltip("Target intensity at night. Matches what's set in each Prefab's PointLight.")]
        [SerializeField] private float targetNightIntensity = 7.5f;

        [Tooltip("Speed of fade-in / fade-out during dusk and dawn transitions.")]
        [SerializeField] private float transitionSpeed = 3.5f;

        private readonly List<Light> _lampLights = new List<Light>();
        private float _currentIntensity = 0f;

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
            DiscoverLampLights();

            // Initial state
            bool startNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
            _currentIntensity = startNight ? targetNightIntensity : 0f;
            ApplyIntensity(_currentIntensity);
        }

        private void DiscoverLampLights()
        {
            _lampLights.Clear();

            // Try exact path first, then simple name search
            GameObject root = GameObject.Find(streetLampsRootPath);
            if (root == null) root = GameObject.Find("Street lamps");

            if (root == null)
            {
                Debug.LogWarning("[StreetLampManager] Could not find 'Street lamps' root in scene.");
                return;
            }

            int found = 0;
            foreach (Transform lampTr in root.transform)
            {
                // Find the "PointLight" child that was embedded directly in the prefab
                Transform lightTr = lampTr.Find("PointLight");
                if (lightTr != null)
                {
                    Light light = lightTr.GetComponent<Light>();
                    if (light != null)
                    {
                        _lampLights.Add(light);
                        found++;
                    }
                }
            }

            Debug.Log($"[StreetLampManager] Found {found} embedded PointLights in street lamps.");
        }

        private void Update()
        {
            bool isNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
            float desired = isNight ? targetNightIntensity : 0f;

            if (!Mathf.Approximately(_currentIntensity, desired))
            {
                _currentIntensity = Mathf.MoveTowards(_currentIntensity, desired, Time.deltaTime * transitionSpeed);
                ApplyIntensity(_currentIntensity);
            }
        }

        private void ApplyIntensity(float intensity)
        {
            bool enable = intensity > 0.01f;
            for (int i = 0; i < _lampLights.Count; i++)
            {
                if (_lampLights[i] == null) continue;
                _lampLights[i].intensity = intensity;
                _lampLights[i].enabled = enable;
            }
        }
    }
}
