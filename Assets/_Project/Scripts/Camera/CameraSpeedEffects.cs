using UnityEngine;

namespace CodeDrive.CameraSystem
{
    /// <summary>
    /// Adds cinematic speed and collision effects to the camera:
    /// - FOV widens slightly at high speed (smooth speed rush feel)
    /// - Punchy camera shake ONLY on vehicle collision impact (no continuous shake)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraSpeedEffects : MonoBehaviour
    {
        public static CameraSpeedEffects Instance { get; private set; }

        [Header("Speed FOV Effect")]
        [Tooltip("Enable dynamic FOV based on car speed")]
        [SerializeField] private bool enableSpeedFOV = true;
        [Tooltip("FOV increase at max speed")]
        [SerializeField] private float maxFOVBoost = 6f;
        [Tooltip("Speed at which FOV boost is fully applied")]
        [SerializeField] private float speedForMaxFOV = 28f;
        [Tooltip("Smoothing speed for FOV transitions")]
        [SerializeField] private float fovSmoothSpeed = 4f;

        [Header("Impact Collision Shake")]
        [Tooltip("Enable punchy shake only on collision impact")]
        [SerializeField] private bool enableCollisionShake = true;

        private Camera _cam;
        private CameraControl _cameraControl;
        private CarControl _car;
        private float _baseFOV;
        private float _currentFOVBoost;

        private float _impactShakeDuration = 0f;
        private float _impactShakeTimer = 0f;
        private float _impactShakeIntensity = 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _cameraControl = GetComponent<CameraControl>();
            _car = FindObjectOfType<CarControl>();

            if (_cam != null)
            {
                _baseFOV = _cam.fieldOfView;
            }
        }

        /// <summary>
        /// Trigger a punchy camera shake on vehicle collision impact with decay.
        /// </summary>
        public void TriggerImpactShake(float intensity = 0.25f, float duration = 0.25f)
        {
            if (!enableCollisionShake) return;
            if (_cameraControl != null && _cameraControl.IsFocusing) return;

            _impactShakeIntensity = intensity;
            _impactShakeDuration = Mathf.Max(duration, 0.05f);
            _impactShakeTimer = _impactShakeDuration;
        }

        private void LateUpdate()
        {
            // --- Impact Shake (Only active during collision timer) ---
            if (_impactShakeTimer > 0f)
            {
                _impactShakeTimer -= Time.deltaTime;
                float progress = Mathf.Clamp01(_impactShakeTimer / _impactShakeDuration);
                float currentIntensity = _impactShakeIntensity * progress * progress; // Quadratic decay

                float time = Time.time * 40f;
                Vector3 shake = new Vector3(
                    (Mathf.PerlinNoise(time, 0f) * 2f - 1f) * currentIntensity,
                    (Mathf.PerlinNoise(0f, time) * 2f - 1f) * currentIntensity * 0.7f,
                    0f
                );

                transform.position += shake;
            }

            if (_car == null || _cam == null) return;

            // Don't modify FOV during showcase focus mode
            if (_cameraControl != null && _cameraControl.IsFocusing) return;

            float speed = _car.CurrentSpeed;

            // --- Smooth Speed FOV ---
            if (enableSpeedFOV)
            {
                float targetBoost = Mathf.Lerp(0f, maxFOVBoost, Mathf.InverseLerp(0f, speedForMaxFOV, speed));
                _currentFOVBoost = Mathf.Lerp(_currentFOVBoost, targetBoost, Time.deltaTime * fovSmoothSpeed);

                float baseFov = _cameraControl != null ? _cameraControl.fieldOfView : _baseFOV;
                _cam.fieldOfView = baseFov + _currentFOVBoost;
            }
        }
    }
}
