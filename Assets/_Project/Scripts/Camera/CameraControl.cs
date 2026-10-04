using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeDrive.CameraSystem
{
    public class CameraControl : MonoBehaviour
    {
        public static CameraControl Instance { get; private set; }

        [Header("Target Tracking")]
        [Tooltip("Target transform to follow (usually the player car)")]
        public Transform target;

        [Header("Isometric Camera Angle (Bruno Simon Style)")]
        [Tooltip("Pitch angle - how steeply the camera looks down (50-55 for Bruno Simon style)")]
        [Range(20f, 75f)]
        public float pitch = 52f;
        [Tooltip("Yaw angle - horizontal rotation around the target (45 = classic diagonal)")]
        [Range(-180f, 180f)]
        public float yaw = 45f;

        [Header("Follow Settings")]
        [Tooltip("Smoothing speed for camera position follow")]
        public float followSpeed = 6f;
        [Tooltip("Height offset for the look-at target point")]
        public float lookAtHeightOffset = 0.5f;
        [Tooltip("Look-ahead distance in the car's forward direction")]
        [Range(0f, 5f)]
        public float lookAheadDistance = 2f;

        [Header("Distance")]
        [Tooltip("Camera distance from target (controls how much world is visible)")]
        public float distance = 22f;

        [Header("Zoom Settings (Mouse Scroll Wheel)")]
        [Tooltip("Enable zooming with mouse scroll wheel")]
        public bool enableZoom = true;
        [Tooltip("Minimum zoom distance")]
        public float minDistance = 12f;
        [Tooltip("Maximum zoom distance")]
        public float maxDistance = 40f;
        [Tooltip("Zoom step size per scroll tick")]
        public float zoomStep = 2f;
        [Tooltip("Zoom smoothing speed")]
        public float zoomSmoothness = 8f;

        [Header("Field of View")]
        [Tooltip("Camera field of view (lower = more telephoto/flat, higher = wider)")]
        [Range(20f, 80f)]
        public float fieldOfView = 40f;

        [Header("Showcase Focus Mode (Bruno Simon Cinematic View)")]
        [SerializeField] private bool isFocusing;
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Vector3 focusCameraPosition;
        [SerializeField] private Quaternion focusCameraRotation;
        [SerializeField] private float focusFOV = 34f;
        [SerializeField] private float focusTransitionSpeed = 4f;

        public bool IsFocusing => isFocusing;

        private Camera _cam;
        private float _targetDistance;
        private float _currentDistance;
        private Vector3 _currentLookAhead;
        private float _defaultFOV;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cam = GetComponent<Camera>();
            _currentDistance = distance;
            _targetDistance = _currentDistance;
            _defaultFOV = fieldOfView;
        }

        private void OnValidate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam != null && !isFocusing)
            {
                _cam.fieldOfView = fieldOfView;
            }
            if (!Application.isPlaying)
            {
                _currentDistance = distance;
                _targetDistance = distance;
            }
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_cam != null)
            {
                _cam.fieldOfView = fieldOfView;
            }

            if (target != null && !isFocusing)
            {
                SnapToTarget();
            }
        }

        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 lookTarget = GetLookTarget();
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = lookTarget + rotation * (Vector3.back * _currentDistance);

            transform.position = desiredPosition;
            transform.LookAt(lookTarget);
        }

        private Vector3 GetLookTarget()
        {
            if (target == null) return Vector3.zero;

            Vector3 lookTarget = target.position + Vector3.up * lookAtHeightOffset;

            if (lookAheadDistance > 0.01f)
            {
                Vector3 forwardOffset = target.forward * lookAheadDistance;
                forwardOffset.y = 0f;
                _currentLookAhead = Application.isPlaying
                    ? Vector3.Lerp(_currentLookAhead, forwardOffset, Time.deltaTime * 3f)
                    : forwardOffset;
                lookTarget += _currentLookAhead;
            }

            return lookTarget;
        }

        /// <summary>
        /// Smoothly focus camera onto a 3D Showcase Booth / Billboard in front of the vehicle.
        /// </summary>
        public void FocusOnTransform(Transform cameraFocusAnchor, float targetFOV = 35f, float speed = 4f)
        {
            if (cameraFocusAnchor == null) return;

            isFocusing = true;
            focusTarget = cameraFocusAnchor;
            focusCameraPosition = cameraFocusAnchor.position;
            focusCameraRotation = cameraFocusAnchor.rotation;
            focusFOV = targetFOV;
            focusTransitionSpeed = speed;
        }

        /// <summary>
        /// Exit focus mode and smoothly return to third-person driving camera.
        /// </summary>
        public void ClearFocus()
        {
            isFocusing = false;
            focusTarget = null;
        }

        void Update()
        {
            if (enableZoom && !isFocusing)
            {
                HandleZoomInput();
            }
        }

        private void HandleZoomInput()
        {
            float scroll = 0f;

            if (Mouse.current != null)
            {
                scroll = Mouse.current.scroll.y.ReadValue();
            }

            if (Mathf.Abs(scroll) > 0.01f)
            {
                float zoomDelta = Mathf.Sign(scroll) * zoomStep;
                _targetDistance = Mathf.Clamp(_targetDistance - zoomDelta, minDistance, maxDistance);
            }

            float zoomT = 1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime);
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, zoomT);
        }

        void LateUpdate()
        {
            if (isFocusing && focusTarget != null)
            {
                // Cinematic Showcase Focus Lerp
                float t = 1f - Mathf.Exp(-focusTransitionSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, focusTarget.position, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, focusTarget.rotation, t);

                if (_cam != null)
                {
                    _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, focusFOV, t);
                }
                return;
            }

            if (target == null) return;

            Vector3 lookTarget = GetLookTarget();
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = lookTarget + rotation * (Vector3.back * _currentDistance);

            float posT = 1f - Mathf.Exp(followSpeed * -Time.deltaTime * -1f);
            float followT = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
            transform.LookAt(lookTarget);

            if (_cam != null)
            {
                float fovT = 1f - Mathf.Exp(-focusTransitionSpeed * Time.deltaTime);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _defaultFOV, fovT);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }
    }
}



