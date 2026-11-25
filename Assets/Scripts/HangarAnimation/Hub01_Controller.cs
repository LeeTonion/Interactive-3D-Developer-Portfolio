using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Controls Central Hub 01 (20m x 12m x 8m) animations and interactions:
    /// - Rotates & bobs the 3D energy schematic hologram on the East-offset octagonal console.
    /// - Sweeps the North Hangar security scanner laser curtain.
    /// - Blinks Server Rack LEDs randomly using MaterialPropertyBlock (zero-GC).
    /// - Press [E] near the North Hangar or South Engineering door to slide open/close.
    /// </summary>
    public class Hub01_Controller : MonoBehaviour
    {
        [Header("3D Energy Hologram")]
        public Transform hologramRoot;
        public Transform hologramRing;
        public float holoSpinSpeed = 24f;
        public float holoBobAmp = 0.06f;
        public float holoBobFreq = 1.8f;

        [Header("Security Scanner Lasers")]
        public Transform[] scannerBeams;

        [Header("North Hangar & South Engineering Doors ([E])")]
        public Transform northDoorLeft;
        public Transform northDoorRight;
        public float northSlideDistance = 2.0f;
        public Transform southDoorLeft;
        public Transform southDoorRight;
        public float southSlideDistance = 2.1f;
        public Transform[] southCrossLocks;
        public float doorInteractRadius = 4.8f;

        [Header("Server Rack LEDs (MaterialPropertyBlock)")]
        public Renderer[] serverLedRenderers;

        private Vector3 _holoBasePos;
        private Vector3[] _beamBasePos;
        private Vector3 _northLeftClosed;
        private Vector3 _northRightClosed;
        private Vector3 _southLeftClosed;
        private Vector3 _southRightClosed;
        private Vector3[] _lockBasePos;

        private bool _northOpen;
        private float _northProgress;
        private bool _southOpen;
        private float _southProgress;
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
            if (hologramRoot != null) _holoBasePos = hologramRoot.localPosition;

            if (scannerBeams != null)
            {
                _beamBasePos = new Vector3[scannerBeams.Length];
                for (int i = 0; i < scannerBeams.Length; i++)
                    if (scannerBeams[i] != null) _beamBasePos[i] = scannerBeams[i].localPosition;
            }

            if (northDoorLeft != null) _northLeftClosed = northDoorLeft.localPosition;
            if (northDoorRight != null) _northRightClosed = northDoorRight.localPosition;
            if (southDoorLeft != null) _southLeftClosed = southDoorLeft.localPosition;
            if (southDoorRight != null) _southRightClosed = southDoorRight.localPosition;

            if (southCrossLocks != null)
            {
                _lockBasePos = new Vector3[southCrossLocks.Length];
                for (int i = 0; i < southCrossLocks.Length; i++)
                    if (southCrossLocks[i] != null) _lockBasePos[i] = southCrossLocks[i].localPosition;
            }

            FindPlayer();
        }

        private void FindPlayer()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null) _player = pc.transform;
            else if (Camera.main != null) _player = Camera.main.transform;
        }

        private void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;

            // 1. Hologram spin & bob
            if (hologramRoot != null)
            {
                hologramRoot.Rotate(Vector3.up, holoSpinSpeed * dt, Space.Self);
                hologramRoot.localPosition = _holoBasePos + Vector3.up * (Mathf.Sin(t * holoBobFreq) * holoBobAmp);
            }
            if (hologramRing != null)
            {
                hologramRing.Rotate(Vector3.up, -holoSpinSpeed * 1.4f * dt, Space.Self);
            }

            // 2. Security scanner laser sweep
            if (scannerBeams != null && _beamBasePos != null)
            {
                for (int i = 0; i < scannerBeams.Length; i++)
                {
                    if (scannerBeams[i] == null) continue;
                    float sweep = Mathf.Sin(t * 2.2f + i * 1.1f) * 0.65f;
                    scannerBeams[i].localPosition = _beamBasePos[i] + new Vector3(0f, sweep, 0f);
                }
            }

            // 3. Door [E] interaction
            if (_player == null) FindPlayer();

            if (northDoorLeft != null && _player != null)
            {
                Vector3 nPos = northDoorLeft.parent != null ? northDoorLeft.parent.position : northDoorLeft.position;
                float dn = Vector3.Distance(_player.position, nPos);
                if (dn <= doorInteractRadius && Input.GetKeyDown(KeyCode.E))
                    _northOpen = !_northOpen;
                else if (dn > doorInteractRadius + 2.5f)
                    _northOpen = false;

                _northProgress = Mathf.MoveTowards(_northProgress, _northOpen ? 1f : 0f, dt * 2.2f);
                float ns = Mathf.SmoothStep(0f, 1f, _northProgress);
                northDoorLeft.localPosition = _northLeftClosed + new Vector3(-northSlideDistance * ns, 0f, 0f);
                if (northDoorRight != null)
                    northDoorRight.localPosition = _northRightClosed + new Vector3(northSlideDistance * ns, 0f, 0f);
            }

            if (southDoorLeft != null && _player != null)
            {
                Vector3 sPos = southDoorLeft.parent != null ? southDoorLeft.parent.position : southDoorLeft.position;
                float ds = Vector3.Distance(_player.position, sPos);
                if (ds <= doorInteractRadius && Input.GetKeyDown(KeyCode.E))
                    _southOpen = !_southOpen;
                else if (ds > doorInteractRadius + 2.5f)
                    _southOpen = false;

                _southProgress = Mathf.MoveTowards(_southProgress, _southOpen ? 2f : 0f, dt * 2.4f);

                float lockT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_southProgress));
                if (southCrossLocks != null && _lockBasePos != null)
                {
                    for (int i = 0; i < southCrossLocks.Length; i++)
                    {
                        if (southCrossLocks[i] == null) continue;
                        float sx = (i % 2 == 0) ? -0.4f : 0.4f;
                        float sy = (i < 2) ? 0.3f : -0.3f;
                        southCrossLocks[i].localPosition = _lockBasePos[i] + new Vector3(sx * lockT, sy * lockT, 0f);
                    }
                }

                float slideT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_southProgress - 1f));
                southDoorLeft.localPosition = _southLeftClosed + new Vector3(-southSlideDistance * slideT, 0f, 0f);
                if (southDoorRight != null)
                    southDoorRight.localPosition = _southRightClosed + new Vector3(southSlideDistance * slideT, 0f, 0f);
            }

            // 4. Random server rack LED blink (MaterialPropertyBlock)
            _ledTimer -= dt;
            if (_ledTimer <= 0f && serverLedRenderers != null && serverLedRenderers.Length > 0)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                _ledTimer = 0.14f;
                for (int i = 0; i < serverLedRenderers.Length; i++)
                {
                    Renderer r = serverLedRenderers[i];
                    if (r == null) continue;
                    float rnd = Mathf.PerlinNoise(t * 7.3f, i * 3.17f);
                    Color col = (i % 3 == 0)
                        ? new Color(0.10f, 0.95f, 0.35f)
                        : (i % 3 == 1 ? new Color(0.00f, 0.90f, 1.00f) : new Color(0.95f, 0.15f, 0.15f));
                    float intensity = rnd > 0.42f ? (1.2f + rnd * 1.4f) : 0.12f;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(EmissionColorId, col * intensity);
                    r.SetPropertyBlock(_mpb);
                }
            }
        }
    }
}
