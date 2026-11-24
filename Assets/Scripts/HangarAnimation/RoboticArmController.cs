using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Multi-joint articulated robotic maintenance arm with heavy-inertia SmoothStep motion
    /// and periodic welding spark bursts + flickering arc light.
    /// </summary>
    public class RoboticArmController : MonoBehaviour
    {
        [Header("Articulated Joints")]
        public Transform baseYawPivot;
        public Transform elbowPivot;
        public Transform wristPivot;

        [Header("Welding VFX & Light")]
        public ParticleSystem weldingSparks;
        public Light weldingLight;

        [Header("Motion Parameters")]
        public float yawRangeDegrees = 35f;
        public float elbowRangeDegrees = 18f;
        public float wristRangeDegrees = 22f;
        public float cycleSpeed = 0.45f;
        public float phaseOffset = 0f;

        [Header("Welding Cycle")]
        public float weldIntervalMin = 3.0f;
        public float weldIntervalMax = 5.0f;
        public float weldDuration = 1.35f;

        private float _animTime;
        private float _weldTimer;
        private float _weldActiveRemaining;
        private float _baseInitYaw;
        private Vector3 _elbowInitEuler;
        private Vector3 _wristInitEuler;
        private float _flickerSeed;

        public void TriggerWeldNow(float duration = 2.0f)
        {
            _weldActiveRemaining = Mathf.Max(_weldActiveRemaining, duration);
            if (weldingSparks != null && !weldingSparks.isPlaying)
                weldingSparks.Play();
        }

        private void Start()
        {
            if (baseYawPivot != null) _baseInitYaw = baseYawPivot.localEulerAngles.y;
            if (elbowPivot != null) _elbowInitEuler = elbowPivot.localEulerAngles;
            if (wristPivot != null) _wristInitEuler = wristPivot.localEulerAngles;
            _weldTimer = Random.Range(1.0f, weldIntervalMax);
            _flickerSeed = Random.Range(0f, 100f);

            if (weldingSparks != null) weldingSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (weldingLight != null) weldingLight.intensity = 0f;
        }

        private void Update()
        {
            float spd = HangarSimulationManager.EffectiveSpeed;
            if (spd <= 0.001f) return;
            float dt = Time.deltaTime * spd;

            // Handle welding pause cycle
            if (_weldActiveRemaining > 0f)
            {
                _weldActiveRemaining -= dt;
                if (weldingLight != null)
                {
                    float flicker = Mathf.PerlinNoise(Time.time * 42f + _flickerSeed, 0.5f);
                    weldingLight.intensity = (flicker > 0.35f) ? Mathf.Lerp(4f, 18f, flicker) : 0.8f;
                }
                if (_weldActiveRemaining <= 0f)
                {
                    if (weldingSparks != null) weldingSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    if (weldingLight != null) weldingLight.intensity = 0f;
                    _weldTimer = Random.Range(weldIntervalMin, weldIntervalMax);
                }
                return; // Hold arm steady while welding!
            }

            _weldTimer -= dt;
            if (_weldTimer <= 0f)
            {
                _weldActiveRemaining = weldDuration;
                if (weldingSparks != null) weldingSparks.Play();
                return;
            }

            // Smooth heavy-inertia multi-joint movement (Sin + SmoothStep)
            _animTime += dt * cycleSpeed;
            float t = _animTime + phaseOffset;

            float sYaw = Mathf.SmoothStep(-1f, 1f, (Mathf.Sin(t) + 1f) * 0.5f);
            float sElbow = Mathf.SmoothStep(-1f, 1f, (Mathf.Sin(t * 1.35f + 0.8f) + 1f) * 0.5f);
            float sWrist = Mathf.SmoothStep(-1f, 1f, (Mathf.Cos(t * 1.7f) + 1f) * 0.5f);

            if (baseYawPivot != null)
            {
                Vector3 e = baseYawPivot.localEulerAngles;
                e.y = _baseInitYaw + sYaw * yawRangeDegrees;
                baseYawPivot.localEulerAngles = e;
            }
            if (elbowPivot != null)
            {
                elbowPivot.localEulerAngles = _elbowInitEuler + new Vector3(0f, 0f, sElbow * elbowRangeDegrees);
            }
            if (wristPivot != null)
            {
                wristPivot.localEulerAngles = _wristInitEuler + new Vector3(0f, 0f, sWrist * wristRangeDegrees);
            }
        }
    }
}
