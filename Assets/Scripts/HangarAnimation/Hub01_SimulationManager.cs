using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// 60+ FPS simulation controller for DECK_01_FLIGHT_POWER/Hub_01:
    /// - Rotates & bobs the central 3D Holographic Starship schematic.
    /// - Sweeps security scanner lasers at the North Hangar Bay 01 gate.
    /// - Animates South Engineering hydraulic cross-locks on [E] or proximity.
    /// - Spins wall ventilation fans, blinks Server Rack LEDs (MaterialPropertyBlock, zero-GC),
    ///   and emits steam bursts every 6 seconds above the Engineering bulkhead.
    /// </summary>
    public class Hub01_SimulationManager : MonoBehaviour
    {
        [Header("Hero 3D Hologram")]
        public Transform hologramShipRoot;
        public Transform hologramRingOuter;
        public float holoSpinSpeed = 22f;
        public float holoBobAmp = 0.08f;
        public float holoBobFreq = 1.8f;

        [Header("Security Scanners & South Hydraulic Locks")]
        public Transform[] scannerBeams;
        public Transform[] southCrossLocks;
        public Transform southDoorLeft;
        public Transform southDoorRight;
        public float doorInteractRadius = 4.0f;

        [Header("Server LEDs (MaterialPropertyBlock)")]
        public Renderer[] serverLedRenderers;

        [Header("Ventilation Fans & Steam Vents")]
        public Transform[] wallFans;
        public ParticleSystem[] engineeringSteamVents;
        public float steamIntervalSeconds = 6.0f;

        private Vector3 _holoBasePos;
        private Vector3[] _beamBasePos;
        private Vector3[] _lockBasePos;
        private Vector3 _southLeftClosed;
        private Vector3 _southRightClosed;
        private float _southDoorProgress;
        private bool _southDoorOpen;
        private float _steamTimer;
        private float _ledTimer;

        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private Transform _player;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (hologramShipRoot != null) _holoBasePos = hologramShipRoot.localPosition;

            if (scannerBeams != null)
            {
                _beamBasePos = new Vector3[scannerBeams.Length];
                for (int i = 0; i < scannerBeams.Length; i++)
                    if (scannerBeams[i] != null) _beamBasePos[i] = scannerBeams[i].localPosition;
            }

            if (southCrossLocks != null)
            {
                _lockBasePos = new Vector3[southCrossLocks.Length];
                for (int i = 0; i < southCrossLocks.Length; i++)
                    if (southCrossLocks[i] != null) _lockBasePos[i] = southCrossLocks[i].localPosition;
            }

            if (southDoorLeft != null) _southLeftClosed = southDoorLeft.localPosition;
            if (southDoorRight != null) _southRightClosed = southDoorRight.localPosition;

            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null) _player = pc.transform;

            _steamTimer = 1.5f;
        }

        private void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;

            // 1. 3D Starship Hologram spin + bob + outer ring counter-spin
            if (hologramShipRoot != null)
            {
                hologramShipRoot.Rotate(Vector3.up, holoSpinSpeed * dt, Space.Self);
                hologramShipRoot.localPosition = _holoBasePos + Vector3.up * (Mathf.Sin(t * holoBobFreq) * holoBobAmp);
            }
            if (hologramRingOuter != null)
            {
                hologramRingOuter.Rotate(Vector3.up, -holoSpinSpeed * 1.4f * dt, Space.Self);
            }

            // 2. North Gate Security Scanner Laser Sweep
            if (scannerBeams != null && _beamBasePos != null)
            {
                for (int i = 0; i < scannerBeams.Length; i++)
                {
                    if (scannerBeams[i] == null) continue;
                    float sweep = Mathf.Sin(t * 2.4f + i * 1.2f) * 0.85f;
                    scannerBeams[i].localPosition = _beamBasePos[i] + new Vector3(0f, sweep, 0f);
                }
            }

            // 3. South Engineering Door & Hydraulic Cross-Locks (auto-proximity or [E])
            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerCharacterController>();
                if (pc != null) _player = pc.transform;
            }

            if (southDoorLeft != null && _player != null)
            {
                float d = Vector3.Distance(_player.position, southDoorLeft.parent != null ? southDoorLeft.parent.position : southDoorLeft.position);
                bool nearSouth = d <= doorInteractRadius;
                if (nearSouth && Input.GetKeyDown(KeyCode.E))
                    _southDoorOpen = !_southDoorOpen;
                else if (nearSouth)
                    _southDoorOpen = true;
                else if (d > doorInteractRadius + 1.5f)
                    _southDoorOpen = false;

                float target = _southDoorOpen ? 2f : 0f;
                _southDoorProgress = Mathf.MoveTowards(_southDoorProgress, target, dt * 2.2f);

                float lockT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_southDoorProgress));
                if (southCrossLocks != null && _lockBasePos != null)
                {
                    for (int i = 0; i < southCrossLocks.Length; i++)
                    {
                        if (southCrossLocks[i] == null) continue;
                        float sx = (i % 2 == 0) ? -0.45f : 0.45f;
                        float sy = (i < 2) ? 0.35f : -0.35f;
                        southCrossLocks[i].localPosition = _lockBasePos[i] + new Vector3(sx * lockT, sy * lockT, 0f);
                    }
                }

                float slideT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_southDoorProgress - 1f));
                if (southDoorLeft != null)
                    southDoorLeft.localPosition = _southLeftClosed + new Vector3(-1.35f * slideT, 0f, 0f);
                if (southDoorRight != null)
                    southDoorRight.localPosition = _southRightClosed + new Vector3(1.35f * slideT, 0f, 0f);
            }

            // 4. Wall Ventilation Fans
            if (wallFans != null)
            {
                for (int i = 0; i < wallFans.Length; i++)
                {
                    if (wallFans[i] != null)
                        wallFans[i].Rotate(Vector3.forward, (210f + i * 45f) * dt, Space.Self);
                }
            }

            // 5. Server Rack LED Random Blinking (zero-GC MaterialPropertyBlock)
            _ledTimer -= dt;
            if (_ledTimer <= 0f && serverLedRenderers != null && serverLedRenderers.Length > 0)
            {
                _ledTimer = 0.14f;
                for (int i = 0; i < serverLedRenderers.Length; i++)
                {
                    Renderer r = serverLedRenderers[i];
                    if (r == null) continue;
                    float rnd = Mathf.PerlinNoise(t * 7.3f, i * 3.17f);
                    Color baseCol = (i % 5 == 0) ? new Color(1f, 0.22f, 0.16f)
                        : ((i % 2 == 0) ? new Color(0.12f, 1f, 0.48f) : new Color(0.14f, 0.88f, 1f));
                    float mult = (rnd > 0.42f) ? 2.6f : 0.25f;
                    _mpb.SetColor(EmissionColorId, baseCol * mult);
                    r.SetPropertyBlock(_mpb);
                }
            }

            // 6. Periodic Engineering Steam Vents (every 6 seconds)
            _steamTimer -= dt;
            if (_steamTimer <= 0f)
            {
                _steamTimer = steamIntervalSeconds;
                if (engineeringSteamVents != null)
                {
                    for (int i = 0; i < engineeringSteamVents.Length; i++)
                    {
                        if (engineeringSteamVents[i] != null)
                            engineeringSteamVents[i].Emit(16);
                    }
                }
            }
        }
    }
}
