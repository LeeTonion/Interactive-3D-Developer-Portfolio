using CodeDrive.Environment;
using UnityEngine;

namespace CodeDrive.Player
{
    /// <summary>
    /// Adds dual forward headlights to the player car that automatically turn ON at night.
    /// </summary>
    public class VehicleHeadlights : MonoBehaviour
    {
        [Header("Headlight Settings")]
        [SerializeField] private Color lightColor = new Color(1.0f, 0.96f, 0.88f, 1.0f); // Warm crisp white
        [SerializeField] private float intensity = 3.0f;
        [SerializeField] private float spotAngle = 55f;
        [SerializeField] private float range = 35f;

        [SerializeField] private Vector3 leftLightOffset = new Vector3(-0.7f, 0.6f, 1.8f);
        [SerializeField] private Vector3 rightLightOffset = new Vector3(0.7f, 0.6f, 1.8f);

        private Light _leftSpot;
        private Light _rightSpot;
        private float _currentIntensity = 0f;

        private void Start()
        {
            _leftSpot = CreateHeadlight("Headlight_Left", leftLightOffset);
            _rightSpot = CreateHeadlight("Headlight_Right", rightLightOffset);
        }

        private Light CreateHeadlight(string name, Vector3 offset)
        {
            var lightGo = new GameObject(name);
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = offset;
            lightGo.transform.localRotation = Quaternion.Euler(6f, 0f, 0f); // Angled slightly downwards towards road

            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = lightColor;
            spot.intensity = 0f;
            spot.range = range;
            spot.spotAngle = spotAngle;
            spot.innerSpotAngle = spotAngle * 0.6f;
            spot.shadows = LightShadows.None;

            return spot;
        }

        private void Update()
        {
            bool isNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
            float desiredIntensity = isNight ? intensity : 0f;

            if (!Mathf.Approximately(_currentIntensity, desiredIntensity))
            {
                _currentIntensity = Mathf.MoveTowards(_currentIntensity, desiredIntensity, Time.deltaTime * 4f);
                if (_leftSpot != null)
                {
                    _leftSpot.intensity = _currentIntensity;
                    _leftSpot.enabled = _currentIntensity > 0.01f;
                }
                if (_rightSpot != null)
                {
                    _rightSpot.intensity = _currentIntensity;
                    _rightSpot.enabled = _currentIntensity > 0.01f;
                }
            }
        }
    }
}
