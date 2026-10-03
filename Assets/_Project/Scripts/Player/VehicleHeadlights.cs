using CodeDrive.Environment;
using UnityEngine;

namespace CodeDrive.Player
{
    /// <summary>
    /// Vehicle headlights & taillights with realistic soft-shadow ground projection.
    /// Uses pure physical spotlights that create smooth, natural light pools on the road
    /// without any intersecting geometry artifacts.
    /// </summary>
    public class VehicleHeadlights : MonoBehaviour
    {
        [Header("Headlight Settings")]
        [SerializeField] private Color lightColor = new Color(1.0f, 0.97f, 0.90f, 1.0f); // Warm crisp white
        [SerializeField] private float headlightIntensity = 38.0f; // High intensity for vivid ground illumination in URP
        [SerializeField] private float spotAngle = 60f;
        [SerializeField] private float innerSpotAngle = 30f;
        [SerializeField] private float range = 40f;

        [Header("Headlight Offsets")]
        [SerializeField] private Vector3 leftLightOffset = new Vector3(-0.65f, 0.55f, 1.75f);
        [SerializeField] private Vector3 rightLightOffset = new Vector3(0.65f, 0.55f, 1.75f);

        [Header("Taillight Settings")]
        [SerializeField] private Color tailColor = new Color(1.0f, 0.12f, 0.08f, 1.0f);
        [SerializeField] private float tailIntensity = 6.0f;
        [SerializeField] private float tailRange = 5.0f;
        [SerializeField] private Vector3 leftTailOffset = new Vector3(-0.65f, 0.55f, -1.75f);
        [SerializeField] private Vector3 rightTailOffset = new Vector3(0.65f, 0.55f, -1.75f);

        private Light _leftHeadlight;
        private Light _rightHeadlight;
        private Light _leftTaillight;
        private Light _rightTaillight;

        private float _currentHeadlightIntensity = 0f;
        private float _currentTailIntensity = 0f;
        private bool _manualOverride = false;
        private bool _lightsOn = false;

        private void Start()
        {
            CleanupOldLights();

            CreateHeadlights();
            CreateTaillights();

            bool isNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
            _currentHeadlightIntensity = isNight ? headlightIntensity : 0f;
            _currentTailIntensity = isNight ? tailIntensity : 0f;
            ApplyLights();
        }

        private void CleanupOldLights()
        {
            // Destroy any previous light or beam objects to prevent duplicate or buggy meshes
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Headlight_") || child.name.StartsWith("Taillight_") || child.name.StartsWith("Beam_"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void CreateHeadlights()
        {
            _leftHeadlight = CreateSpotlight("Headlight_Left", leftLightOffset, new Vector3(4f, -2f, 0f));
            _rightHeadlight = CreateSpotlight("Headlight_Right", rightLightOffset, new Vector3(4f, 2f, 0f));
        }

        private Light CreateSpotlight(string name, Vector3 offset, Vector3 rotationAngles)
        {
            var lightGo = new GameObject(name);
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = offset;
            lightGo.transform.localRotation = Quaternion.Euler(rotationAngles);

            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = lightColor;
            spot.intensity = 0f;
            spot.range = range;
            spot.spotAngle = spotAngle;
            spot.innerSpotAngle = innerSpotAngle;
            spot.shadows = LightShadows.Soft;
            spot.shadowStrength = 0.85f;
            spot.shadowNearPlane = 0.2f;

            return spot;
        }

        private void CreateTaillights()
        {
            _leftTaillight = CreatePointLight("Taillight_Left", leftTailOffset, tailColor);
            _rightTaillight = CreatePointLight("Taillight_Right", rightTailOffset, tailColor);
        }

        private Light CreatePointLight(string name, Vector3 offset, Color col)
        {
            var lightGo = new GameObject(name);
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = offset;

            var pt = lightGo.AddComponent<Light>();
            pt.type = LightType.Point;
            pt.color = col;
            pt.intensity = 0f;
            pt.range = tailRange;
            pt.shadows = LightShadows.None;

            return pt;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.H))
            {
                _manualOverride = true;
                _lightsOn = !_lightsOn;
            }

            bool isNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
            bool shouldLight = _manualOverride ? _lightsOn : isNight;

            float targetHeadlight = shouldLight ? headlightIntensity : 0f;
            float targetTail = shouldLight ? tailIntensity : 0f;

            bool isBraking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Space);
            if (isBraking) targetTail = Mathf.Max(targetTail, tailIntensity * 2.2f);

            _currentHeadlightIntensity = Mathf.MoveTowards(_currentHeadlightIntensity, targetHeadlight, Time.deltaTime * 35f);
            _currentTailIntensity = Mathf.MoveTowards(_currentTailIntensity, targetTail, Time.deltaTime * 25f);

            ApplyLights();
        }

        private void ApplyLights()
        {
            bool headlightsActive = _currentHeadlightIntensity > 0.05f;

            if (_leftHeadlight != null)
            {
                _leftHeadlight.intensity = _currentHeadlightIntensity;
                _leftHeadlight.enabled = headlightsActive;
            }
            if (_rightHeadlight != null)
            {
                _rightHeadlight.intensity = _currentHeadlightIntensity;
                _rightHeadlight.enabled = headlightsActive;
            }

            if (_leftTaillight != null)
            {
                _leftTaillight.intensity = _currentTailIntensity;
                _leftTaillight.enabled = _currentTailIntensity > 0.05f;
            }
            if (_rightTaillight != null)
            {
                _rightTaillight.intensity = _currentTailIntensity;
                _rightTaillight.enabled = _currentTailIntensity > 0.05f;
            }
        }
    }
}
