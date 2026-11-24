using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Lightweight animator for Hangar rotating warning beacons and floating console holograms.
    /// </summary>
    public class HangarBeaconAndFX : MonoBehaviour
    {
        public enum Mode { RotatingBeacon, FloatingHologram }

        public Mode mode = Mode.RotatingBeacon;
        public float speed = 135f;
        public float bobAmplitude = 0.08f;

        private Vector3 _baseLocalPos;
        private float _phase;

        private void Start()
        {
            _baseLocalPos = transform.localPosition;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (mode == Mode.RotatingBeacon)
            {
                transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
            }
            else if (mode == Mode.FloatingHologram)
            {
                float t = Time.time * 1.8f + _phase;
                transform.localPosition = _baseLocalPos + Vector3.up * (Mathf.Sin(t) * bobAmplitude);
            }
        }
    }
}
