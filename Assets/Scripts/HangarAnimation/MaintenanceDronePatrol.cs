using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Autonomous hovering maintenance drone flying smooth Lissajous patrol curves
    /// around the Landing Pad and Catwalk with realistic velocity-based banking.
    /// </summary>
    public class MaintenanceDronePatrol : MonoBehaviour
    {
        [Header("Lissajous Patrol Volume (Local)")]
        public Vector3 patrolCenter = new Vector3(0f, 7.2f, -6.5f);
        public float radiusX = 11.5f;
        public float radiusZ = 10.5f;
        public float bobHeightY = 1.15f;
        public float freqX = 0.22f;
        public float freqZ = 0.44f;
        public float freqY = 0.55f;
        public float phaseOffset = 0f;

        [Header("Flight Feel")]
        public float bankStrength = 28f;
        public float pitchTilt = 10f;
        public float turnSmooth = 5.5f;

        private float _t;
        private Vector3 _prevPos;
        private Vector3 _prevVel;
        private float _scanFocusTimer;
        private Light _scanSpot;
        private float _baseSpotIntensity = 3f;

        public void TriggerShipScanFocus(float duration = 5.0f)
        {
            _scanFocusTimer = Mathf.Max(_scanFocusTimer, duration);
        }

        private void Start()
        {
            _t = phaseOffset;
            _prevPos = EvaluatePosition(_t);
            transform.localPosition = _prevPos;
            _scanSpot = GetComponentInChildren<Light>();
            if (_scanSpot != null) _baseSpotIntensity = _scanSpot.intensity;
        }

        private Vector3 EvaluatePosition(float t)
        {
            // When scanning the hero spaceship, swoop tightly around the Landing Pad center
            float focusBlend = Mathf.Clamp01(_scanFocusTimer * 0.6f);
            Vector3 center = Vector3.Lerp(patrolCenter, new Vector3(0f, 5.2f, -6.5f), focusBlend);
            float rx = Mathf.Lerp(radiusX, 4.8f, focusBlend);
            float rz = Mathf.Lerp(radiusZ, 4.8f, focusBlend);
            float speedMul = Mathf.Lerp(1f, 2.1f, focusBlend);

            return center + new Vector3(
                Mathf.Sin(t * freqX * speedMul) * rx,
                Mathf.Sin(t * freqY * speedMul) * bobHeightY,
                Mathf.Sin(t * freqZ * speedMul + 0.6f) * rz
            );
        }

        private void Update()
        {
            float spd = HangarSimulationManager.EffectiveSpeed;
            if (spd <= 0.001f) return;
            float dt = Time.deltaTime * spd;
            if (dt <= 0.0001f) return;

            _t += dt;
            if (_scanFocusTimer > 0f)
                _scanFocusTimer = Mathf.Max(0f, _scanFocusTimer - Time.deltaTime);

            if (_scanSpot != null)
            {
                _scanSpot.intensity = _scanFocusTimer > 0f
                    ? _baseSpotIntensity * (2.2f + Mathf.Sin(Time.time * 14f) * 0.6f)
                    : _baseSpotIntensity;
            }

            Vector3 nextPos = EvaluatePosition(_t);
            Vector3 vel = (nextPos - _prevPos) / dt;
            Vector3 accel = (vel - _prevVel) / dt;

            transform.localPosition = nextPos;

            Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
            if (flatVel.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
                Vector3 right = lookRot * Vector3.right;
                float lateralAccel = Vector3.Dot(accel, right);
                float bankRoll = Mathf.Clamp(-lateralAccel * bankStrength, -32f, 32f);
                Quaternion targetRot = lookRot * Quaternion.Euler(pitchTilt, 0f, bankRoll);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, dt * turnSmooth);
            }

            _prevPos = nextPos;
            _prevVel = vel;
        }
    }
}
