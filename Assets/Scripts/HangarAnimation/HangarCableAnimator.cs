using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Lightweight airflow sway animator for hanging cables and ceiling pendant lamps in Room01_Spaceship_Hangar.
    /// Keeps overhead rigging subtly alive at 60+ FPS with zero allocations.
    /// </summary>
    public class HangarCableAnimator : MonoBehaviour
    {
        public Transform[] swayingElements;
        public float swayDegrees = 1.6f;
        public float swaySpeed = 1.35f;

        private Quaternion[] _baseRots;
        private float[] _phases;

        private void Start()
        {
            CacheBaseRotations();
        }

        public void CacheBaseRotations()
        {
            if (swayingElements == null) return;
            _baseRots = new Quaternion[swayingElements.Length];
            _phases = new float[swayingElements.Length];
            for (int i = 0; i < swayingElements.Length; i++)
            {
                if (swayingElements[i] != null)
                {
                    _baseRots[i] = swayingElements[i].localRotation;
                    _phases[i] = i * 0.93f;
                }
            }
        }

        private void Update()
        {
            if (swayingElements == null || _baseRots == null) return;
            float spd = HangarSimulationManager.EffectiveSpeed;
            if (spd <= 0.001f) return;

            float t = Time.time * swaySpeed * Mathf.Clamp(spd, 0.3f, 2.5f);
            for (int i = 0; i < swayingElements.Length; i++)
            {
                Transform tr = swayingElements[i];
                if (tr == null) continue;
                float ph = _phases[i];
                float pitch = Mathf.Sin(t + ph) * swayDegrees;
                float roll = Mathf.Cos(t * 0.78f + ph * 1.3f) * (swayDegrees * 0.7f);
                tr.localRotation = _baseRots[i] * Quaternion.Euler(pitch, 0f, roll);
            }
        }
    }
}
