using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Zero-GC animator for rotating warning beacons, bobbing hologram consoles,
    /// and breathing HDR emission waves on the circular Landing Pad LED rings (using MaterialPropertyBlock).
    /// </summary>
    public class HangarLightingAnimator : MonoBehaviour
    {
        [Header("Rotating Warning Beacons")]
        public Transform[] rotatingBeacons;
        public float beaconRotationSpeed = 140f;

        [Header("Floating Holograms")]
        public Transform[] hologramScreens;
        public float holoBobAmplitude = 0.08f;
        public float holoBobSpeed = 2.0f;
        public float holoYawSway = 8f;

        [Header("Landing Pad LED Rings (MaterialPropertyBlock)")]
        public Renderer[] outerYellowRing;
        public Renderer[] midCyanRing;
        public Renderer[] innerWhiteRing;
        public float pulseSpeed = 2.2f;

        private Vector3[] _holoBasePos;
        private Vector3[] _holoBaseEuler;
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private static readonly Color BaseYellow = new Color(1.0f, 0.78f, 0.10f);
        private static readonly Color BaseCyan   = new Color(0.0f, 0.90f, 1.0f);
        private static readonly Color BaseWhite  = new Color(0.84f, 0.92f, 1.0f);

        private float _t;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (hologramScreens != null)
            {
                _holoBasePos = new Vector3[hologramScreens.Length];
                _holoBaseEuler = new Vector3[hologramScreens.Length];
                for (int i = 0; i < hologramScreens.Length; i++)
                {
                    if (hologramScreens[i] == null) continue;
                    _holoBasePos[i] = hologramScreens[i].localPosition;
                    _holoBaseEuler[i] = hologramScreens[i].localEulerAngles;
                }
            }
        }

        private void Update()
        {
            float spd = HangarSimulationManager.EffectiveSpeed;
            if (spd <= 0.001f) return;
            float dt = Time.deltaTime * spd;
            _t += dt;

            // 1. Rotate warning beacons 360 deg continuously
            if (rotatingBeacons != null)
            {
                for (int i = 0; i < rotatingBeacons.Length; i++)
                {
                    if (rotatingBeacons[i] != null)
                        rotatingBeacons[i].Rotate(Vector3.up, beaconRotationSpeed * dt, Space.Self);
                }
            }

            // 2. Float & gently sway Hologram screens
            if (hologramScreens != null && _holoBasePos != null)
            {
                for (int i = 0; i < hologramScreens.Length; i++)
                {
                    if (hologramScreens[i] == null) continue;
                    float ph = _t * holoBobSpeed + i * 1.4f;
                    hologramScreens[i].localPosition = _holoBasePos[i] + Vector3.up * (Mathf.Sin(ph) * holoBobAmplitude);
                    hologramScreens[i].localEulerAngles = _holoBaseEuler[i] + new Vector3(0f, Mathf.Sin(ph * 0.6f) * holoYawSway, 0f);
                }
            }

            // 3. Breathing HDR energy wave across concentric Landing Pad LED rings (zero material allocations!)
            ApplyRingPulse(outerYellowRing, BaseYellow, 1.8f, 3.6f, _t * pulseSpeed);
            ApplyRingPulse(midCyanRing,     BaseCyan,   2.0f, 4.0f, _t * pulseSpeed - 0.9f);
            ApplyRingPulse(innerWhiteRing,  BaseWhite,  1.6f, 3.2f, _t * pulseSpeed - 1.8f);
        }

        private void ApplyRingPulse(Renderer[] ring, Color baseColor, float minMult, float maxMult, float phase)
        {
            if (ring == null || ring.Length == 0) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            float wave = (Mathf.Sin(phase) + 1f) * 0.5f;
            float mult = Mathf.Lerp(minMult, maxMult, wave);
            _mpb.SetColor(EmissionColorId, baseColor * mult);
            for (int i = 0; i < ring.Length; i++)
            {
                if (ring[i] != null) ring[i].SetPropertyBlock(_mpb);
            }
        }
    }
}
