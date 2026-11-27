using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Controls Room 03 - Engineering Room (28m x 24m x 8m) animations and [E] interactions:
    /// - Spins the 4 corner Amber Power Turbine rotors continuously.
    /// - Pulses the 4 Cyan Cryogenic Coolant Tanks (zero-GC MaterialPropertyBlock).
    /// - Slowly traverses the Overhead Yellow Gantry Crane along its ceiling rails.
    /// - [E] at any of the 4 Diagnostic Consoles toggles Turbine Overdrive & Coolant Steam Purge.
    /// - [E] at Left/Right Cargo Elevator terminals toggles freight loading bay beacons.
    /// </summary>
    public class Room03_EngineeringController : MonoBehaviour
    {
        [Header("4 Corner Power Turbines & Coolant Tanks")]
        public Transform[] turbineRotors;
        public Renderer[] coolantRenderers;
        public ParticleSystem[] steamVents;
        public float baseRotorSpeed = 160f;

        [Header("Emergency Disaster Lighting")]
        public Light[] emergencyLights;
        public bool disasterMode = true;

        [Header("Overhead Yellow Gantry Crane")]
        public Transform gantryBridge;
        public float gantryTravelZ = 4.5f;
        public float gantrySpeed = 0.25f;

        [Header("Interactive Consoles & Cargo Elevators")]
        public Transform[] diagnosticConsoles;
        public Transform[] elevatorTerminals;
        public Renderer[] elevatorBeacons;
        public float interactRadius = 3.6f;

        private Vector3 _gantryBasePos;
        private bool _overdriveActive;
        private bool _elevatorsActive = true;
        private float _steamTimer;
        private int _activeTarget; // 0=none, 1=Console, 2=Elevator
        private string _activePrompt;
        private string _toastText;
        private float _toastTimer;

        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private Transform _player;
        private GUIStyle _promptStyle;
        private GUIStyle _toastStyle;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (gantryBridge != null) _gantryBasePos = gantryBridge.localPosition;
            FindPlayer();
        }

        private void FindPlayer()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null) _player = pc.transform;
            else if (Camera.main != null) _player = Camera.main.transform;
        }

        private bool CanSee(Vector3 worldPos, float maxDist)
        {
            Camera cam = Camera.main;
            Transform origin = cam != null ? cam.transform : _player;
            if (origin == null) return false;

            float d = Vector3.Distance(_player != null ? _player.position : origin.position, worldPos);
            if (d > maxDist) return false;

            if (cam != null)
            {
                Vector3 dir = (worldPos - cam.transform.position).normalized;
                if (Vector3.Dot(cam.transform.forward, dir) < 0.20f) return false;
            }
            return true;
        }

        private void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;

            // 1. Spin 4 Turbine Rotors
            float spd = _overdriveActive ? baseRotorSpeed * 2.2f : baseRotorSpeed;
            if (turbineRotors != null)
            {
                for (int i = 0; i < turbineRotors.Length; i++)
                {
                    if (turbineRotors[i] != null)
                        turbineRotors[i].Rotate(Vector3.forward, spd * dt, Space.Self);
                }
            }

            // 2. Pulse / Erratically Flicker Cyan Coolant Tanks
            if (coolantRenderers != null && coolantRenderers.Length > 0)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                for (int i = 0; i < coolantRenderers.Length; i++)
                {
                    Renderer r = coolantRenderers[i];
                    if (r == null) continue;
                    float pulse = 1.35f + Mathf.Sin(t * 2.4f + i * 1.1f) * 0.45f;
                    if (disasterMode && (i % 3 == 1))
                    {
                        float flk = Mathf.PerlinNoise(t * 14f, i * 5.3f);
                        pulse = flk > 0.35f ? (0.8f + flk * 1.4f) : 0.25f;
                    }
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(EmissionColorId, new Color(0f, 0.90f, 1.0f) * pulse);
                    r.SetPropertyBlock(_mpb);
                }
            }

            // 2b. Flicker Emergency Red & Amber Strobe Lights
            if (emergencyLights != null)
            {
                for (int i = 0; i < emergencyLights.Length; i++)
                {
                    Light lt = emergencyLights[i];
                    if (lt == null) continue;
                    float wave = Mathf.Sin(t * 5.2f + i * 1.9f);
                    float noise = Mathf.PerlinNoise(t * 11f, i * 2.7f);
                    lt.intensity = (wave > -0.1f && noise > 0.28f) ? (8.5f * noise + 2.5f) : 0.4f;
                }
            }

            // 3. Overhead Yellow Gantry Crane smooth patrol
            if (gantryBridge != null)
            {
                float zOff = Mathf.Sin(t * gantrySpeed) * gantryTravelZ;
                gantryBridge.localPosition = _gantryBasePos + new Vector3(0f, 0f, zOff);
            }

            // 4. Periodic Steam Venting
            _steamTimer -= dt;
            if (_steamTimer <= 0f && steamVents != null)
            {
                _steamTimer = _overdriveActive ? 2.5f : 5.5f;
                for (int i = 0; i < steamVents.Length; i++)
                {
                    if (steamVents[i] != null) steamVents[i].Play();
                }
            }

            // 5. Elevator Beacon Blink
            if (elevatorBeacons != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                float blink = _elevatorsActive ? (Mathf.Sin(t * 6.0f) > 0f ? 2.4f : 0.25f) : 0.1f;
                for (int i = 0; i < elevatorBeacons.Length; i++)
                {
                    if (elevatorBeacons[i] == null) continue;
                    elevatorBeacons[i].GetPropertyBlock(_mpb);
                    _mpb.SetColor(EmissionColorId, new Color(1.0f, 0.68f, 0.0f) * blink);
                    elevatorBeacons[i].SetPropertyBlock(_mpb);
                }
            }

            // 6. [E] Interaction check
            if (_player == null) FindPlayer();
            _activeTarget = 0;
            _activePrompt = null;
            float bestDist = 999f;

            if (_player != null)
            {
                if (diagnosticConsoles != null)
                {
                    for (int i = 0; i < diagnosticConsoles.Length; i++)
                    {
                        if (diagnosticConsoles[i] == null) continue;
                        Vector3 p = diagnosticConsoles[i].position + Vector3.up * 0.9f;
                        float d = Vector3.Distance(_player.position, p);
                        if (d < bestDist && CanSee(p, interactRadius))
                        {
                            bestDist = d;
                            _activeTarget = 1;
                            _activePrompt = _overdriveActive ? "[E] Giảm Tải Tua-bin & Tắt Xả Áp" : "[E] Kích Hoạt Tăng Tải Tua-bin & Xả Tản Nhiệt";
                        }
                    }
                }

                if (elevatorTerminals != null)
                {
                    for (int i = 0; i < elevatorTerminals.Length; i++)
                    {
                        if (elevatorTerminals[i] == null) continue;
                        Vector3 p = elevatorTerminals[i].position + Vector3.up * 1.1f;
                        float d = Vector3.Distance(_player.position, p);
                        if (d < bestDist && CanSee(p, interactRadius))
                        {
                            bestDist = d;
                            _activeTarget = 2;
                            _activePrompt = _elevatorsActive ? "[E] Khóa Đèn Cảnh Báo Thang Hàng" : "[E] Bật Chế Độ Bốc Dỡ Thang Hàng";
                        }
                    }
                }

                if (_activeTarget != 0 && Input.GetKeyDown(KeyCode.E))
                {
                    if (_activeTarget == 1)
                    {
                        _overdriveActive = !_overdriveActive;
                        _steamTimer = 0.05f;
                        _toastText = _overdriveActive ? "TUA-BIN: TĂNG TẢI + XẢ TẢN NHIỆT" : "TUA-BIN: CHẾ ĐỘ TIÊU CHUẨN";
                        _toastTimer = 2.6f;
                    }
                    else if (_activeTarget == 2)
                    {
                        _elevatorsActive = !_elevatorsActive;
                        _toastText = _elevatorsActive ? "THANG HÀNG: SẴN SÀNG BỐC DỠ" : "THANG HÀNG: ĐÃ KHÓA AN TOÀN";
                        _toastTimer = 2.6f;
                    }
                }
            }

            if (_toastTimer > 0f) _toastTimer -= dt;
        }

        private void OnGUI()
        {
            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _promptStyle.normal.textColor = new Color(0.75f, 0.96f, 1f);
            }
            if (_toastStyle == null)
            {
                _toastStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _toastStyle.normal.textColor = new Color(1f, 0.80f, 0.35f);
            }

            if (!string.IsNullOrEmpty(_activePrompt))
            {
                float w = 440f, h = 38f;
                GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height - 96f, w, h), _activePrompt, _promptStyle);
            }
            if (_toastTimer > 0f && !string.IsNullOrEmpty(_toastText))
            {
                float w = 440f, h = 36f;
                GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height - 140f, w, h), _toastText, _toastStyle);
            }
        }
    }
}
