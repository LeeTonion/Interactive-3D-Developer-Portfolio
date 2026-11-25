using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Lightweight 60+ FPS ambient animator for all office, operations, data center,
    /// command bridge, and observation deck props inside HOME_MOTHERSHIP/INTERIOR.
    /// All references are pre-wired in the scene (zero runtime GameObject spawning, zero GC).
    /// </summary>
    public class MothershipOfficeAnimator : MonoBehaviour
    {
        [Header("Rotating Holograms & Mainframe Cores")]
        public Transform[] rotatingObjects;
        public float[] rotateSpeeds;

        [Header("Office Botanical Plants (Airflow Sway)")]
        public Transform[] officePlants;
        public float plantSwayDegrees = 2.0f;
        public float plantSwaySpeed = 1.4f;

        [Header("Office Swivel Chairs (Subtle Idle Motion)")]
        public Transform[] officeChairs;
        public float chairSwivelDegrees = 5.5f;

        [Header("Server Racks & Computers (Subtle Activity Pulse)")]
        public Transform[] serverUnits;
        public Light[] officeLights;

        private Quaternion[] _plantBaseRots;
        private Quaternion[] _chairBaseRots;
        private Vector3[] _serverBasePos;
        private float[] _lightBaseIntensity;

        private void Start()
        {
            if (officePlants != null)
            {
                _plantBaseRots = new Quaternion[officePlants.Length];
                for (int i = 0; i < officePlants.Length; i++)
                    if (officePlants[i] != null) _plantBaseRots[i] = officePlants[i].localRotation;
            }

            if (officeChairs != null)
            {
                _chairBaseRots = new Quaternion[officeChairs.Length];
                for (int i = 0; i < officeChairs.Length; i++)
                    if (officeChairs[i] != null) _chairBaseRots[i] = officeChairs[i].localRotation;
            }

            if (serverUnits != null)
            {
                _serverBasePos = new Vector3[serverUnits.Length];
                for (int i = 0; i < serverUnits.Length; i++)
                    if (serverUnits[i] != null) _serverBasePos[i] = serverUnits[i].localPosition;
            }

            if (officeLights != null)
            {
                _lightBaseIntensity = new float[officeLights.Length];
                for (int i = 0; i < officeLights.Length; i++)
                    if (officeLights[i] != null) _lightBaseIntensity[i] = officeLights[i].intensity;
            }
        }

        private void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;

            // 1. Rotate Holo StarSectors, OrbitRings & Data Core Mainframe
            if (rotatingObjects != null)
            {
                for (int i = 0; i < rotatingObjects.Length; i++)
                {
                    Transform tr = rotatingObjects[i];
                    if (tr == null) continue;
                    float spd = (rotateSpeeds != null && i < rotateSpeeds.Length) ? rotateSpeeds[i] : 24f;
                    tr.Rotate(Vector3.up, spd * dt, Space.Self);
                }
            }

            // 2. Gentle HVAC airflow sway on all office botanical plants
            if (officePlants != null && _plantBaseRots != null)
            {
                for (int i = 0; i < officePlants.Length; i++)
                {
                    Transform tr = officePlants[i];
                    if (tr == null) continue;
                    float ph = i * 1.15f;
                    float sx = Mathf.Sin(t * plantSwaySpeed + ph) * plantSwayDegrees;
                    float sz = Mathf.Cos(t * plantSwaySpeed * 0.82f + ph) * (plantSwayDegrees * 0.75f);
                    tr.localRotation = _plantBaseRots[i] * Quaternion.Euler(sx, 0f, sz);
                }
            }

            // 3. Subtle idle swivel on office operator chairs
            if (officeChairs != null && _chairBaseRots != null)
            {
                for (int i = 0; i < officeChairs.Length; i++)
                {
                    Transform tr = officeChairs[i];
                    if (tr == null) continue;
                    float ph = i * 1.73f;
                    float yaw = Mathf.Sin(t * 0.65f + ph) * chairSwivelDegrees;
                    tr.localRotation = _chairBaseRots[i] * Quaternion.Euler(0f, yaw, 0f);
                }
            }

            // 4. Micro-vibration on Data Center servers & kiosks
            if (serverUnits != null && _serverBasePos != null)
            {
                for (int i = 0; i < serverUnits.Length; i++)
                {
                    Transform tr = serverUnits[i];
                    if (tr == null) continue;
                    float vib = Mathf.Sin(t * 22f + i * 0.9f) * 0.0018f;
                    tr.localPosition = _serverBasePos[i] + new Vector3(0f, vib, 0f);
                }
            }

            // 5. Breathing data-center / command lights
            if (officeLights != null && _lightBaseIntensity != null)
            {
                for (int i = 0; i < officeLights.Length; i++)
                {
                    Light lt = officeLights[i];
                    if (lt == null) continue;
                    float pulse = 0.88f + 0.12f * Mathf.Sin(t * 2.1f + i * 1.3f);
                    lt.intensity = _lightBaseIntensity[i] * pulse;
                }
            }
        }
    }
}
