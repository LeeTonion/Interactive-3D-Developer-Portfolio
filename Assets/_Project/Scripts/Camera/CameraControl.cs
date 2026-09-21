using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    [Header("Target Tracking")]
    [Tooltip("Target transform to follow (usually the player car)")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("Smoothing speed for camera position follow")]
    public float followSpeed = 8f;
    [Tooltip("Smoothing speed for camera rotation")]
    public float rotationSpeed = 6f;
    [Tooltip("Height offset for the look-at target point")]
    public float lookAtHeightOffset = 1.2f;

    [Header("Distance & Height")]
    [Tooltip("Base distance behind the car")]
    public float distance = 18f;
    [Tooltip("Base height above the car")]
    public float height = 6.5f;

    [Header("Zoom Settings (Mouse Scroll Wheel)")]
    [Tooltip("Enable zooming with mouse scroll wheel")]
    public bool enableZoom = true;
    [Tooltip("Minimum zoom distance")]
    public float minDistance = 6f;
    [Tooltip("Maximum zoom distance")]
    public float maxDistance = 24f;
    [Tooltip("Zoom step size per scroll tick")]
    public float zoomStep = 1.5f;
    [Tooltip("Zoom smoothing speed")]
    public float zoomSmoothness = 10f;

    private float _targetDistance;
    private float _currentDistance;
    private float _heightToDistanceRatio;

    void Awake()
    {
        _currentDistance = maxDistance;
        _targetDistance = _currentDistance;
        UpdateRatio();
    }

    private void OnValidate()
    {
        UpdateRatio();
    }

    private void UpdateRatio()
    {
        if (distance > 0.001f)
        {
            _heightToDistanceRatio = height / distance;
        }
        else
        {
            _heightToDistanceRatio = 0.4f;
        }
    }

    void Start()
    {
        // Keep cursor unlocked and visible for UI and portfolio interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (target != null)
        {
            SnapToTarget();
        }
    }

    public void SnapToTarget()
    {
        if (target == null) return;

        float currentH = _currentDistance * _heightToDistanceRatio;
        Vector3 desiredPosition = target.TransformPoint(new Vector3(0f, currentH, -_currentDistance));
        transform.position = desiredPosition;

        Vector3 lookTarget = target.position + Vector3.up * lookAtHeightOffset;
        Vector3 direction = lookTarget - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void Update()
    {
        if (enableZoom)
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
            // Scrolling up (positive) -> Zoom in (closer)
            // Scrolling down (negative) -> Zoom out (farther)
            float zoomDelta = Mathf.Sign(scroll) * zoomStep;
            _targetDistance = Mathf.Clamp(_targetDistance - zoomDelta, minDistance, maxDistance);
        }

        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * zoomSmoothness);
    }

    void LateUpdate()
    {
        if (target == null) return;

        float currentH = _currentDistance * _heightToDistanceRatio;
        Vector3 desiredPosition = target.TransformPoint(new Vector3(0f, currentH, -_currentDistance));
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        Vector3 lookTarget = target.position + Vector3.up * lookAtHeightOffset;
        Vector3 direction = lookTarget - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }
}
