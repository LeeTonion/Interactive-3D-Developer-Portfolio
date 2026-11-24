using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Animates the overhead gantry crane along Z, trolley carriage along X,
    /// and applies inertial pendulum sway to the suspended hook block.
    /// </summary>
    public class GantryCraneController : MonoBehaviour
    {
        [Header("Crane Hierarchy")]
        public Transform craneBridge;
        public Transform trolleyCarriage;
        public Transform hookPendulumPivot;

        [Header("Travel Bounds (Local)")]
        public float zMin = -17.5f;
        public float zMax = 8.5f;
        public float bridgeSpeed = 0.09f;

        public float xMin = -9.5f;
        public float xMax = 9.5f;
        public float trolleySpeed = 0.16f;

        [Header("Hook Inertial Sway")]
        public float swayAmplitude = 6.5f;
        public float swayDamping = 3.5f;

        private float _tBridge;
        private float _tTrolley;
        private float _prevZ;
        private float _prevX;
        private float _prevVz;
        private float _prevVx;
        private float _swayX;
        private float _swayZ;

        private void Start()
        {
            if (craneBridge != null) _prevZ = craneBridge.localPosition.z;
            if (trolleyCarriage != null) _prevX = trolleyCarriage.localPosition.x;
        }

        private void Update()
        {
            float spd = HangarSimulationManager.EffectiveSpeed;
            if (spd <= 0.001f) return;
            float dt = Time.deltaTime * spd;
            if (dt <= 0.0001f) return;

            _tBridge += dt * bridgeSpeed;
            _tTrolley += dt * trolleySpeed;

            // Smooth cosine ease-in/ease-out at rail endpoints
            float zNorm = (1f - Mathf.Cos(_tBridge * Mathf.PI)) * 0.5f;
            float xNorm = (1f - Mathf.Cos(_tTrolley * Mathf.PI + 0.7f)) * 0.5f;

            float newZ = Mathf.Lerp(zMin, zMax, zNorm);
            float newX = Mathf.Lerp(xMin, xMax, xNorm);

            if (craneBridge != null)
            {
                Vector3 lp = craneBridge.localPosition;
                lp.z = newZ;
                craneBridge.localPosition = lp;
            }
            if (trolleyCarriage != null)
            {
                Vector3 lp = trolleyCarriage.localPosition;
                lp.x = newX;
                trolleyCarriage.localPosition = lp;
            }

            // Compute acceleration-driven pendulum sway for suspended hook block
            float vz = (newZ - _prevZ) / dt;
            float vx = (newX - _prevX) / dt;
            float az = (vz - _prevVz) / dt;
            float ax = (vx - _prevVx) / dt;

            float targetSwayPitch = Mathf.Clamp(-az * swayAmplitude, -12f, 12f);
            float targetSwayRoll = Mathf.Clamp(ax * swayAmplitude, -12f, 12f);

            _swayX = Mathf.Lerp(_swayX, targetSwayPitch, dt * swayDamping);
            _swayZ = Mathf.Lerp(_swayZ, targetSwayRoll, dt * swayDamping);

            if (hookPendulumPivot != null)
            {
                hookPendulumPivot.localEulerAngles = new Vector3(_swayX, 0f, _swayZ);
            }

            _prevZ = newZ;
            _prevX = newX;
            _prevVz = vz;
            _prevVx = vx;
        }
    }
}
