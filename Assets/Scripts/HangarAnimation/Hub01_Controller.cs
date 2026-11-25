using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Controls Central Hub 01 (20m x 12m x 8m) animations, Security Laser Gate, and [E] interactions:
    /// - Security Laser Gate blocks passage to Hangar Bay 01 unless cleared (currently always denied)
    ///   OR until the player cuts power at the West HV Breaker (which also blacks out the Hangar bay).
    /// - North Hangar Door auto-opens on proximity once past the barrier.
    /// - Octagonal Hologram Console ([E] to cycle schematic mode).
    /// - East EVA Lockers ([E] to toggle EVA inspection lighting).
    /// </summary>
    public class Hub01_Controller : MonoBehaviour
    {
        [Header("3D Energy Hologram")]
        public Transform hologramRoot;
        public Transform hologramRing;
        public Renderer[] hologramRenderers;
        public float holoSpinSpeed = 24f;
        public float holoBobAmp = 0.06f;
        public float holoBobFreq = 1.8f;

        [Header("Security Laser Gate")]
        public Transform[] scannerBeams;
        public Renderer[] scannerBeamRenderers;
        public Collider securityBarrierCollider;
        public Transform securityScannerAnchor;
        public bool securityCleared = false;

        [Header("North Hangar Door (Auto)")]
        public Transform northDoorLeft;
        public Transform northDoorRight;
        public float northSlideDistance = 2.0f;
        public float doorInteractRadius = 4.8f;

        [Header("Interactive Stations (HV Breaker & EVA Lockers)")]
        public Transform hvBreakerAnchor;
        public Transform hvLeverPivot;
        public Renderer hvStatusRenderer;
        public Transform evaConsoleAnchor;
        public Renderer[] evaLedRenderers;
        public Light[] hubSpotLights;
        public float stationInteractRadius = 3.8f;

        [Header("Server Rack LEDs (MaterialPropertyBlock)")]
        public Renderer[] serverLedRenderers;

        // Unused legacy fields kept null-safe for Inspector compatibility
        [HideInInspector] public Transform southDoorLeft;
        [HideInInspector] public Transform southDoorRight;
        [HideInInspector] public float southSlideDistance = 2.1f;
        [HideInInspector] public Transform[] southCrossLocks;

        private Vector3 _holoBasePos;
        private Vector3 _holoBaseScale = Vector3.one;
        private Vector3[] _beamBasePos;
        private Vector3 _northLeftClosed;
        private Vector3 _northRightClosed;
        private float[] _lightBaseIntensities;

        private Light[] _hangarLights;
        private float[] _hangarBaseIntensities;
        private HangarLightingAnimator _hangarAnim;

        private bool _northOpen;
        private float _northProgress;
        private int _holoMode;
        private bool _auxPowerOn = true;
        private bool _evaLightsOn = true;
        private float _ledTimer;
        private float _denyFlashTimer;

        private string _activePrompt;
        private int _activeTarget; // 0=none, 3=Holo, 4=HV, 5=EVA, 6=SecurityScan
        private string _toastText;
        private float _toastTimer;

        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private Transform _player;
        private GUIStyle _promptStyle;
        private GUIStyle _toastStyle;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (hologramRoot != null)
            {
                _holoBasePos = hologramRoot.localPosition;
                _holoBaseScale = hologramRoot.localScale;
            }

            if (scannerBeams != null)
            {
                _beamBasePos = new Vector3[scannerBeams.Length];
                for (int i = 0; i < scannerBeams.Length; i++)
                    if (scannerBeams[i] != null) _beamBasePos[i] = scannerBeams[i].localPosition;
            }

            if (northDoorLeft != null) _northLeftClosed = northDoorLeft.localPosition;
            if (northDoorRight != null) _northRightClosed = northDoorRight.localPosition;

            if (hubSpotLights != null)
            {
                _lightBaseIntensities = new float[hubSpotLights.Length];
                for (int i = 0; i < hubSpotLights.Length; i++)
                    if (hubSpotLights[i] != null) _lightBaseIntensities[i] = hubSpotLights[i].intensity;
            }

            // Cache Hangar bay lights so cutting Hub 01 HV power also cuts power to the ship hall
            GameObject hangarObj = GameObject.Find("Room01_Spaceship_Hangar");
            if (hangarObj != null)
            {
                _hangarLights = hangarObj.GetComponentsInChildren<Light>(true);
                _hangarBaseIntensities = new float[_hangarLights.Length];
                for (int i = 0; i < _hangarLights.Length; i++)
                    _hangarBaseIntensities[i] = _hangarLights[i].intensity;
                _hangarAnim = hangarObj.GetComponentInChildren<HangarLightingAnimator>(true);
            }

            UpdateSecurityBarrierState();
            FindPlayer();
        }

        private void FindPlayer()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null) _player = pc.transform;
            else if (Camera.main != null) _player = Camera.main.transform;
        }

        private bool CanSeeTarget(Vector3 worldPos, float maxDist)
        {
            Camera cam = Camera.main;
            Transform originTf = cam != null ? cam.transform : _player;
            if (originTf == null) return false;

            float d = Vector3.Distance(_player != null ? _player.position : originTf.position, worldPos);
            if (d > maxDist) return false;

            if (cam != null)
            {
                Vector3 toTarget = (worldPos - cam.transform.position).normalized;
                if (Vector3.Dot(cam.transform.forward, toTarget) < 0.25f) return false;
            }

            Vector3 eye = originTf.position;
            if (Physics.Linecast(eye, worldPos, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                if (_player != null && hit.transform.IsChildOf(_player)) return true;
                if (Vector3.Distance(hit.point, worldPos) > 1.25f) return false;
            }
            return true;
        }

        private void UpdateSecurityBarrierState()
        {
            bool gateLocked = _auxPowerOn && !securityCleared;
            if (securityBarrierCollider != null)
                securityBarrierCollider.enabled = gateLocked;
        }

        private void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;

            // 1. Hologram spin, bob & mode tint
            if (hologramRoot != null)
            {
                float speedMult = _holoMode == 1 ? 2.8f : (_holoMode == 2 ? 1.6f : 1.0f);
                hologramRoot.Rotate(Vector3.up, holoSpinSpeed * speedMult * dt, Space.Self);
                hologramRoot.localPosition = _holoBasePos + Vector3.up * (Mathf.Sin(t * holoBobFreq) * holoBobAmp);
                float pulse = _holoMode == 1 ? (1f + Mathf.Sin(t * 5f) * 0.08f) : 1f;
                hologramRoot.localScale = _holoBaseScale * pulse;
            }
            if (hologramRing != null)
            {
                hologramRing.Rotate(Vector3.up, -holoSpinSpeed * 1.4f * dt, Space.Self);
            }

            // 2. Security scanner laser sweep & deny flash
            if (_denyFlashTimer > 0f) _denyFlashTimer -= dt;
            if (scannerBeams != null && _beamBasePos != null)
            {
                for (int i = 0; i < scannerBeams.Length; i++)
                {
                    if (scannerBeams[i] == null) continue;
                    scannerBeams[i].gameObject.SetActive(_auxPowerOn && !securityCleared);
                    if (_auxPowerOn && !securityCleared)
                    {
                        float sweep = Mathf.Sin(t * 2.2f + i * 1.1f) * 0.65f;
                        scannerBeams[i].localPosition = _beamBasePos[i] + new Vector3(0f, sweep, 0f);
                    }
                }
            }
            if (scannerBeamRenderers != null && _auxPowerOn && !securityCleared)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                bool flashingRed = _denyFlashTimer > 0f && Mathf.Sin(t * 28f) > 0f;
                Color bc = flashingRed ? new Color(1.0f, 0.10f, 0.10f, 0.75f) : new Color(0.0f, 0.90f, 1.0f, 0.36f);
                Color ec = flashingRed ? new Color(2.8f, 0.15f, 0.15f) : new Color(0.0f, 1.65f, 2.20f);
                for (int i = 0; i < scannerBeamRenderers.Length; i++)
                {
                    if (scannerBeamRenderers[i] == null) continue;
                    scannerBeamRenderers[i].GetPropertyBlock(_mpb);
                    _mpb.SetColor(BaseColorId, bc);
                    _mpb.SetColor(EmissionColorId, ec);
                    scannerBeamRenderers[i].SetPropertyBlock(_mpb);
                }
            }

            // 3. Find closest visible interactive target & auto North door
            if (_player == null) FindPlayer();
            _activeTarget = 0;
            _activePrompt = null;
            float bestDist = 999f;

            if (_player != null)
            {
                if (northDoorLeft != null)
                {
                    Vector3 p = (northDoorLeft.parent != null ? northDoorLeft.parent.position : northDoorLeft.position) + Vector3.up * 1.6f;
                    float d = Vector3.Distance(_player.position, p);
                    _northOpen = d <= doorInteractRadius && (!_auxPowerOn || securityCleared);
                }

                if (securityScannerAnchor != null && _auxPowerOn && !securityCleared)
                {
                    Vector3 p = securityScannerAnchor.position + Vector3.up * 1.2f;
                    float d = Vector3.Distance(_player.position, p);
                    if (d < bestDist && CanSeeTarget(p, 4.2f))
                    {
                        bestDist = d;
                        _activeTarget = 6;
                        _activePrompt = "[E] Quét An Ninh";
                    }
                }

                if (hologramRoot != null)
                {
                    Vector3 p = hologramRoot.position;
                    float d = Vector3.Distance(_player.position, p);
                    if (d < bestDist && CanSeeTarget(p, stationInteractRadius))
                    {
                        bestDist = d;
                        _activeTarget = 3;
                        _activePrompt = "[E] Chuyển Chế Độ Hologram Chiến Thuật";
                    }
                }

                if (hvBreakerAnchor != null)
                {
                    Vector3 p = hvBreakerAnchor.position;
                    float d = Vector3.Distance(_player.position, p);
                    if (d < bestDist && CanSeeTarget(p, stationInteractRadius))
                    {
                        bestDist = d;
                        _activeTarget = 4;
                        _activePrompt = _auxPowerOn ? "[E] Ngắt Cầu Dao Điện" : "[E] Đóng Cầu Dao Điện";
                    }
                }

                if (evaConsoleAnchor != null)
                {
                    Vector3 p = evaConsoleAnchor.position;
                    float d = Vector3.Distance(_player.position, p);
                    if (d < bestDist && CanSeeTarget(p, stationInteractRadius))
                    {
                        bestDist = d;
                        _activeTarget = 5;
                        _activePrompt = _evaLightsOn ? "[E] Tắt Đèn Tủ Bảo Hộ EVA" : "[E] Bật Đèn Kiểm Tra Tủ EVA";
                    }
                }

                if (_activeTarget != 0 && Input.GetKeyDown(KeyCode.E))
                {
                    ExecuteInteraction(_activeTarget);
                }
            }

            // 4. Animate North Hangar door
            if (northDoorLeft != null)
            {
                _northProgress = Mathf.MoveTowards(_northProgress, _northOpen ? 1f : 0f, dt * 2.2f);
                float ns = Mathf.SmoothStep(0f, 1f, _northProgress);
                northDoorLeft.localPosition = _northLeftClosed + new Vector3(-northSlideDistance * ns, 0f, 0f);
                if (northDoorRight != null)
                    northDoorRight.localPosition = _northRightClosed + new Vector3(northSlideDistance * ns, 0f, 0f);
            }

            // 5. Random server rack LED blink (MaterialPropertyBlock)
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
                    float intensity = _auxPowerOn ? (rnd > 0.42f ? (1.2f + rnd * 1.4f) : 0.12f) : 0.02f;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(EmissionColorId, col * intensity);
                    r.SetPropertyBlock(_mpb);
                }
            }

            if (hvLeverPivot != null)
            {
                float targetAng = _auxPowerOn ? -35f : 42f;
                Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAng);
                hvLeverPivot.localRotation = Quaternion.Slerp(hvLeverPivot.localRotation, targetRot, dt * 10f);
            }

            if (_toastTimer > 0f) _toastTimer -= dt;
        }

        private void ExecuteInteraction(int target)
        {
            switch (target)
            {
                case 3:
                    _holoMode = (_holoMode + 1) % 3;
                    ApplyHologramMode();
                    string modeName = _holoMode == 0 ? "Sơ đồ Năng lượng (Cyan)" : (_holoMode == 1 ? "Cảnh báo Lõi Nhiệt (Amber)" : "Trạng thái Sẵn sàng (Emerald)");
                    ShowToast("Hologram: " + modeName);
                    break;
                case 4:
                    _auxPowerOn = !_auxPowerOn;
                    ApplyAuxPower();
                    UpdateSecurityBarrierState();
                    ShowToast(_auxPowerOn ? "NGUỒN ĐIỆN: BẬT" : "NGUỒN ĐIỆN: NGẮT");
                    break;
                case 5:
                    _evaLightsOn = !_evaLightsOn;
                    ApplyEvaLights();
                    ShowToast(_evaLightsOn ? "Đèn tủ EVA: BẬT" : "Đèn tủ EVA: TẮT");
                    break;
                case 6:
                    _denyFlashTimer = 1.4f;
                    ShowToast("TRUY CẬP BỊ TỪ CHỐI");
                    break;
            }
        }

        private void ApplyHologramMode()
        {
            if (hologramRenderers == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Color baseC = _holoMode == 0 ? new Color(0f, 0.9f, 1f, 0.36f)
                        : (_holoMode == 1 ? new Color(1f, 0.48f, 0f, 0.42f) : new Color(0.15f, 0.95f, 0.45f, 0.40f));
            Color emitC = _holoMode == 0 ? new Color(0f, 1.65f, 2.2f)
                        : (_holoMode == 1 ? new Color(2.2f, 0.9f, 0f) : new Color(0.2f, 1.85f, 0.7f));
            for (int i = 0; i < hologramRenderers.Length; i++)
            {
                if (hologramRenderers[i] == null) continue;
                hologramRenderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, baseC);
                _mpb.SetColor(EmissionColorId, emitC);
                hologramRenderers[i].SetPropertyBlock(_mpb);
            }
        }

        private void ApplyAuxPower()
        {
            if (hubSpotLights != null && _lightBaseIntensities != null)
            {
                for (int i = 0; i < hubSpotLights.Length; i++)
                {
                    if (hubSpotLights[i] == null) continue;
                    hubSpotLights[i].intensity = _auxPowerOn ? _lightBaseIntensities[i] : _lightBaseIntensities[i] * 0.12f;
                }
            }

            if (_hangarLights != null && _hangarBaseIntensities != null)
            {
                for (int i = 0; i < _hangarLights.Length; i++)
                {
                    if (_hangarLights[i] == null) continue;
                    _hangarLights[i].intensity = _auxPowerOn ? _hangarBaseIntensities[i] : 0.05f;
                }
            }
            if (_hangarAnim != null)
                _hangarAnim.enabled = _auxPowerOn;

            if (hvStatusRenderer != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                Color c = _auxPowerOn ? new Color(0.15f, 1.9f, 0.45f) : new Color(2.2f, 0.18f, 0.15f);
                hvStatusRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, c);
                hvStatusRenderer.SetPropertyBlock(_mpb);
            }
        }

        private void ApplyEvaLights()
        {
            if (evaLedRenderers == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Color c = _evaLightsOn ? new Color(1.25f, 1.40f, 1.55f) : new Color(0.06f, 0.08f, 0.10f);
            for (int i = 0; i < evaLedRenderers.Length; i++)
            {
                if (evaLedRenderers[i] == null) continue;
                evaLedRenderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, c);
                evaLedRenderers[i].SetPropertyBlock(_mpb);
            }
        }

        private void ShowToast(string msg)
        {
            _toastText = msg;
            _toastTimer = 2.8f;
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
                _toastStyle.normal.textColor = new Color(1f, 0.78f, 0.35f);
            }

            if (!string.IsNullOrEmpty(_activePrompt))
            {
                float w = 440f, h = 38f;
                GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height - 96f, w, h), _activePrompt, _promptStyle);
            }
            if (_toastTimer > 0f && !string.IsNullOrEmpty(_toastText))
            {
                float w = 540f, h = 36f;
                GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height - 140f, w, h), _toastText, _toastStyle);
            }
        }
    }
}
