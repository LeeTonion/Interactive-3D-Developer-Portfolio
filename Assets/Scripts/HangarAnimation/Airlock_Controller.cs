using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Full 4-stage Interlock Controller for the 4x4x4m Radiation Decontamination Airlock (Z = -62 to -66):
    /// 0. Idle: Opens ONLY the entry hatch when player approaches from outside.
    /// 1. ClosingDoors: Immediately closes & locks BOTH hatches when player steps inside.
    /// 2. Scanning (1.8s): Sweeps a cyan laser scanner beam across the chamber + spins amber beacons.
    /// 3. Spraying (3.0s): Fires all 4 ceiling decontamination mist nozzles + amber floor LEDs.
    /// 4. ExitOpen: Switches floor LEDs to Green and opens ONLY the opposite hatch.
    /// Never allows both hatches open at the same time.
    /// </summary>
    public class Airlock_Controller : MonoBehaviour
    {
        [Header("Airlock Hatches (Z = -62 North, Z = -66 South)")]
        public Transform northDoorLeft;
        public Transform northDoorRight;
        public Transform southDoorLeft;
        public Transform southDoorRight;
        public float slideDistance = 2.05f;

        [Header("Floor LEDs, Laser Scanner & Rotating Amber Beacon")]
        public Renderer[] floorLedRenderers;
        public Transform scannerBeam;
        public Transform[] rotatingBeacons;
        public Light[] beaconLights;
        public float beaconSpinSpeed = 220f;

        [Header("4 Ceiling Decontamination Nozzles")]
        public ParticleSystem[] deconNozzles;
        public float scanDuration = 1.8f;
        public float sprayDuration = 3.0f;

        private enum AirlockState { Idle, ClosingDoors, Scanning, Spraying, ExitOpen }
        private AirlockState _state = AirlockState.Idle;
        private bool _targetSouth; // true = exit to Reactor (-66), false = exit to Engineering (-62)
        private float _stateTimer;
        private float _northOpenT;
        private float _southOpenT;

        private Vector3 _nLeftClosed, _nRightClosed;
        private Vector3 _sLeftClosed, _sRightClosed;
        private Transform _player;

        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private string _statusText;
        private GUIStyle _statusStyle;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (northDoorLeft != null) _nLeftClosed = northDoorLeft.localPosition;
            if (northDoorRight != null) _nRightClosed = northDoorRight.localPosition;
            if (southDoorLeft != null) _sLeftClosed = southDoorLeft.localPosition;
            if (southDoorRight != null) _sRightClosed = southDoorRight.localPosition;

            if (scannerBeam != null) scannerBeam.gameObject.SetActive(false);
            SetFloorLedColor(new Color(0.0f, 0.90f, 1.0f), 1.6f);
            SetBeaconsActive(false);
            SetNozzles(false);
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
            float dt = Time.deltaTime;
            if (_player == null) FindPlayer();

            bool insideCenter = false;
            bool nearNorthOutside = false;
            bool nearSouthOutside = false;
            bool farOutside = true;
            float localZ = -64f;

            if (_player != null)
            {
                Vector3 lp = transform.InverseTransformPoint(_player.position);
                localZ = lp.z;
                bool inY = lp.y >= 1.8f && lp.y <= 5.4f; // Deck 1 only! Ignore Deck 2 (Y >= 6.25)
                bool inX = Mathf.Abs(lp.x) <= 2.2f;
                insideCenter = inY && Mathf.Abs(lp.x) <= 1.65f && lp.z >= -64.7f && lp.z <= -63.3f;
                nearNorthOutside = inY && inX && lp.z > -63.3f && lp.z <= -58.0f;
                nearSouthOutside = inY && inX && lp.z < -64.7f && lp.z >= -70.0f;
                farOutside = !inY || Mathf.Abs(lp.x) > 2.4f || lp.z > -61.0f || lp.z < -67.0f;
            }

            bool openNorth = false;
            bool openSouth = false;

            switch (_state)
            {
                case AirlockState.Idle:
                    _statusText = null;
                    // Open ONLY the entry hatch the player approaches from outside
                    if (nearNorthOutside) openNorth = true;
                    else if (nearSouthOutside) openSouth = true;

                    if (insideCenter)
                    {
                        _targetSouth = localZ > -64.0f;
                        _state = AirlockState.ClosingDoors;
                        _stateTimer = 0.8f;
                        SetBeaconsActive(true);
                        SetFloorLedColor(new Color(1.0f, 0.48f, 0.0f), 2.0f);
                        _statusText = "ĐANG ĐÓNG & KHÓA CỬA ÁP SUẤT...";
                    }
                    break;

                case AirlockState.ClosingDoors:
                    // Force both doors closed & wait until completely shut
                    SpinBeacons(dt);
                    _stateTimer -= dt;
                    _statusText = "ĐANG ĐÓNG & KHÓA CỬA ÁP SUẤT...";
                    if (_stateTimer <= 0f && _northOpenT <= 0.02f && _southOpenT <= 0.02f)
                    {
                        _state = AirlockState.Scanning;
                        _stateTimer = scanDuration;
                        if (scannerBeam != null) scannerBeam.gameObject.SetActive(true);
                        SetFloorLedColor(new Color(0.0f, 0.90f, 1.0f), 2.2f);
                    }
                    break;

                case AirlockState.Scanning:
                    SpinBeacons(dt);
                    _stateTimer -= dt;
                    _statusText = $"ĐANG QUÉT BỨC XẠ... ({Mathf.Max(0f, _stateTimer):0.0}s)";
                    if (scannerBeam != null)
                    {
                        float p = Mathf.PingPong((scanDuration - _stateTimer) * 1.4f, 1f);
                        Vector3 sp = scannerBeam.localPosition;
                        sp.z = Mathf.Lerp(-62.5f, -65.5f, p);
                        scannerBeam.localPosition = sp;
                    }
                    if (_stateTimer <= 0f)
                    {
                        if (scannerBeam != null) scannerBeam.gameObject.SetActive(false);
                        _state = AirlockState.Spraying;
                        _stateTimer = sprayDuration;
                        SetNozzles(true);
                        SetFloorLedColor(new Color(1.0f, 0.48f, 0.0f), 2.2f);
                    }
                    break;

                case AirlockState.Spraying:
                    SpinBeacons(dt);
                    _stateTimer -= dt;
                    _statusText = $"ĐANG XỊT KHÓI KHỬ TRÙNG... ({Mathf.Max(0f, _stateTimer):0.0}s)";
                    if (_stateTimer <= 0f)
                    {
                        SetBeaconsActive(false);
                        SetFloorLedColor(new Color(0.10f, 0.95f, 0.35f), 2.0f);
                        SetMistSpillDirection(_targetSouth ? -3.0f : 3.0f);
                        _state = AirlockState.ExitOpen;
                        _stateTimer = 1.1f; // keep spilling mist out as door opens
                    }
                    break;

                case AirlockState.ExitOpen:
                    openNorth = !_targetSouth;
                    openSouth = _targetSouth;
                    _statusText = "KHỬ TRÙNG HOÀN TẤT — MỞ CỬA";
                    if (_stateTimer > 0f)
                    {
                        _stateTimer -= dt;
                        if (_stateTimer <= 0f) SetNozzles(false); // StopEmitting lets mist linger & drift out
                    }
                    if (farOutside)
                    {
                        SetNozzles(false);
                        SetMistSpillDirection(0f);
                        _state = AirlockState.Idle;
                        SetFloorLedColor(new Color(0.0f, 0.90f, 1.0f), 1.6f);
                        _statusText = null;
                    }
                    break;
            }

            _northOpenT = Mathf.MoveTowards(_northOpenT, openNorth ? 1f : 0f, dt * 2.6f);
            _southOpenT = Mathf.MoveTowards(_southOpenT, openSouth ? 1f : 0f, dt * 2.6f);

            float nS = Mathf.SmoothStep(0f, 1f, _northOpenT);
            float sS = Mathf.SmoothStep(0f, 1f, _southOpenT);

            // Slide outward away from center (positive Sign(x) moves +X, negative Sign(x) moves -X)
            float nDirL = _nLeftClosed.x >= 0f ? 1f : -1f;
            float nDirR = _nRightClosed.x >= 0f ? 1f : -1f;
            float sDirL = _sLeftClosed.x >= 0f ? 1f : -1f;
            float sDirR = _sRightClosed.x >= 0f ? 1f : -1f;

            if (northDoorLeft != null)  northDoorLeft.localPosition  = _nLeftClosed  + new Vector3(nDirL * slideDistance * nS, 0f, 0f);
            if (northDoorRight != null) northDoorRight.localPosition = _nRightClosed + new Vector3(nDirR * slideDistance * nS, 0f, 0f);
            if (southDoorLeft != null)  southDoorLeft.localPosition  = _sLeftClosed  + new Vector3(sDirL * slideDistance * sS, 0f, 0f);
            if (southDoorRight != null) southDoorRight.localPosition = _sRightClosed + new Vector3(sDirR * slideDistance * sS, 0f, 0f);
        }

        private void SpinBeacons(float dt)
        {
            if (rotatingBeacons == null) return;
            for (int i = 0; i < rotatingBeacons.Length; i++)
                if (rotatingBeacons[i] != null)
                    rotatingBeacons[i].Rotate(Vector3.up, beaconSpinSpeed * dt, Space.Self);
        }

        private void SetNozzles(bool active)
        {
            if (deconNozzles == null) return;
            for (int i = 0; i < deconNozzles.Length; i++)
            {
                if (deconNozzles[i] == null) continue;
                if (active) { if (!deconNozzles[i].isPlaying) deconNozzles[i].Play(); }
                else { if (deconNozzles[i].isPlaying) deconNozzles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting); }
            }
        }

        private void SetMistSpillDirection(float zVel)
        {
            if (deconNozzles == null) return;
            Vector3 worldVel = transform.TransformDirection(new Vector3(0f, 0f, zVel));
            for (int i = 0; i < deconNozzles.Length; i++)
            {
                if (deconNozzles[i] == null) continue;
                var vel = deconNozzles[i].velocityOverLifetime;
                vel.enabled = Mathf.Abs(zVel) > 0.01f;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = worldVel.x;
                vel.y = worldVel.y;
                vel.z = worldVel.z;
            }
        }

        private void SetBeaconsActive(bool active)
        {
            if (beaconLights == null) return;
            for (int i = 0; i < beaconLights.Length; i++)
                if (beaconLights[i] != null) beaconLights[i].enabled = active;
        }

        private void SetFloorLedColor(Color col, float intensity)
        {
            if (floorLedRenderers == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            for (int i = 0; i < floorLedRenderers.Length; i++)
            {
                if (floorLedRenderers[i] == null) continue;
                floorLedRenderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, col);
                _mpb.SetColor(EmissionColorId, col * intensity);
                floorLedRenderers[i].SetPropertyBlock(_mpb);
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_statusText)) return;
            if (_statusStyle == null)
            {
                _statusStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _statusStyle.normal.textColor = new Color(1f, 0.85f, 0.45f);
            }
            float w = 420f, h = 36f;
            GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height - 96f, w, h), _statusText, _statusStyle);
        }
    }
}
