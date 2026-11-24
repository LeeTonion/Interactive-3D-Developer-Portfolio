using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    public enum HangarPropRole
    {
        PowerJunctionBox,   // Real Power Grid breaker (+25% / -25% Hangar power, modulates lights & machinery speed)
        GasCylinderRack,    // Real Coolant Purge (-32°C Core Temp, giant cryogenic steam burst + recoil)
        CableWinchReel,     // Real Landing Pad Clamp Winch (physically opens/locks all 4 Hydraulic Clamps on the pad)
        FuelPipeValve,      // Real Fuel Pipeline Valve (must be opened BEFORE Fuel Tanks can pump fuel!)
        ShipDiagnosticHolo, // Real Ship Diagnostic Scanner (summons both Drones to swoop in + triggers Robotic Arm weld!)
        MainFuelTank,       // Real Refueling Pump (pumps 0% -> 100% into PlayerSpaceship once Pipe Valve is open)
        SupplyCrate,        // Real Maintenance Supply Crate (boosts Hangar repair readiness + mechanical lid animation)
        EquipmentUnit       // Real Workshop Diagnostic Unit
    }

    /// <summary>
    /// Interconnected functional Hangar prop with procedural 3D audio, continuous mechanical animation,
    /// and real cause-and-effect impact on HangarSimulationManager's Pre-Flight Launch System.
    /// </summary>
    public class HangarInteractiveProp : MonoBehaviour
    {
        public HangarPropRole role = HangarPropRole.EquipmentUnit;
        public string propTitle = "Thiết Bị Xưởng Bảo Trì";
        public float interactRadius = 3.2f;
        public KeyCode interactKey = KeyCode.E;

        private Transform _player;
        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;
        private Vector3 _baseLocalScale;
        private Light _indicatorLight;
        private ParticleSystem _burstVfx;
        private AudioSource _audio;

        private bool _isActiveState;
        private float _actionAnimTimer;
        private float _spinAngle;
        private float _phaseSeed;
        private float _toastTimer;
        private string _toastMsg;
        private bool _toastIsWarning;

        private static AudioClip _clipEngage;
        private static AudioClip _clipHiss;
        private static AudioClip _clipError;
        private static AudioClip _clipScan;

        private static HangarInteractiveProp _closestProp;
        private static float _closestDist = 999f;
        private static int _lastFrame = -1;

        private void Awake()
        {
            _baseLocalPos = transform.localPosition;
            _baseLocalRot = transform.localRotation;
            _baseLocalScale = transform.localScale;
            _phaseSeed = Mathf.Abs(transform.position.x * 13.7f + transform.position.z * 7.3f);
            EnsureSharedAudioClips();
        }

        private void Start()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null) _player = pc.transform;

            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0.85f;
            _audio.maxDistance = 24f;
            _audio.playOnAwake = false;

            SetupIndicatorAndVfx();
        }

        private static void EnsureSharedAudioClips()
        {
            if (_clipEngage != null) return;
            _clipEngage = CreateSynthClip(240f, 520f, 0.22f, false);
            _clipHiss = CreateSynthClip(0f, 0f, 0.55f, true);
            _clipError = CreateSynthClip(320f, 180f, 0.28f, false);
            _clipScan = CreateSynthClip(660f, 1180f, 0.32f, false);
        }

        private static AudioClip CreateSynthClip(float fStart, float fEnd, float duration, bool whiteNoise)
        {
            int sr = 22050;
            int n = Mathf.Max(64, (int)(sr * duration));
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float env = (1f - u) * Mathf.Clamp01(u * 18f);
                if (whiteNoise)
                {
                    data[i] = (Random.value * 2f - 1f) * env * 0.28f;
                }
                else
                {
                    float freq = Mathf.Lerp(fStart, fEnd, u);
                    data[i] = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / sr)) * env * 0.26f;
                }
            }
            var clip = AudioClip.Create(whiteNoise ? "HangarHiss" : "HangarTone", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void SetupIndicatorAndVfx()
        {
            if (role == HangarPropRole.PowerJunctionBox ||
                role == HangarPropRole.MainFuelTank ||
                role == HangarPropRole.ShipDiagnosticHolo ||
                role == HangarPropRole.GasCylinderRack)
            {
                _indicatorLight = GetComponentInChildren<Light>();
                if (_indicatorLight == null)
                {
                    GameObject lGo = new GameObject("Prop_StatusLight");
                    lGo.transform.SetParent(transform, false);
                    lGo.transform.localPosition = new Vector3(0f, 0.7f, 0.35f);
                    _indicatorLight = lGo.AddComponent<Light>();
                    _indicatorLight.type = LightType.Point;
                    _indicatorLight.range = 3.0f;
                    _indicatorLight.intensity = 1.3f;
                    _indicatorLight.color = GetIdleColor();
                }
            }

            if (role == HangarPropRole.GasCylinderRack || role == HangarPropRole.MainFuelTank)
            {
                GameObject vfxGo = new GameObject(role == HangarPropRole.GasCylinderRack ? "Coolant_Purge_VFX" : "Fuel_Flow_VFX");
                vfxGo.transform.SetParent(transform, false);
                vfxGo.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                _burstVfx = vfxGo.AddComponent<ParticleSystem>();
                var main = _burstVfx.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 1.4f;
                main.startLifetime = 1.3f;
                main.startSpeed = role == HangarPropRole.GasCylinderRack ? 4.5f : 2.6f;
                main.startSize = role == HangarPropRole.GasCylinderRack ? 0.65f : 0.32f;
                main.startColor = role == HangarPropRole.GasCylinderRack
                    ? new Color(0.75f, 0.94f, 1f, 0.65f)
                    : new Color(0.2f, 0.95f, 1f, 0.75f);
                var em = _burstVfx.emission;
                em.rateOverTime = 0f;
                var shape = _burstVfx.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 22f;
                shape.radius = 0.2f;
                var rend = vfxGo.GetComponent<ParticleSystemRenderer>();
                Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                if (pShader != null)
                {
                    Material m = new Material(pShader);
                    m.color = main.startColor.color;
                    rend.sharedMaterial = m;
                }
            }
        }

        private Color GetIdleColor()
        {
            switch (role)
            {
                case HangarPropRole.PowerJunctionBox: return new Color(0.15f, 0.85f, 1f);
                case HangarPropRole.GasCylinderRack: return new Color(0.35f, 0.95f, 0.85f);
                case HangarPropRole.MainFuelTank: return new Color(1f, 0.65f, 0.12f);
                case HangarPropRole.ShipDiagnosticHolo: return new Color(0.2f, 1f, 0.65f);
                default: return new Color(0.25f, 0.9f, 1f);
            }
        }

        private void Update()
        {
            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerCharacterController>();
                if (pc != null) _player = pc.transform;
            }

            float spd = HangarSimulationManager.EffectiveSpeed;
            float dt = Time.deltaTime;
            float simDt = dt * Mathf.Max(0.15f, spd);

            if (_lastFrame != Time.frameCount)
            {
                _lastFrame = Time.frameCount;
                _closestProp = null;
                _closestDist = 999f;
            }

            float dist = 999f;
            if (_player != null && _player.gameObject.activeInHierarchy)
                dist = Vector3.Distance(_player.position, transform.position);

            if (dist <= interactRadius && dist < _closestDist)
            {
                _closestDist = dist;
                _closestProp = this;
            }

            if (_toastTimer > 0f) _toastTimer -= dt;
            if (_actionAnimTimer > 0f) _actionAnimTimer = Mathf.Max(0f, _actionAnimTimer - dt);

            AnimateProp(simDt);
        }

        private void LateUpdate()
        {
            if (_closestProp == this && !PlayerCharacterController.AnyUiPanelOpen && Input.GetKeyDown(interactKey))
            {
                ExecuteFunction();
            }
        }

        private void AnimateProp(float dt)
        {
            float t = Time.time + _phaseSeed;
            var mgr = HangarSimulationManager.Instance;

            switch (role)
            {
                case HangarPropRole.CableWinchReel:
                {
                    bool released = mgr != null && mgr.clampsReleased;
                    float dir = released ? -1f : 1f;
                    float rpm = (_actionAnimTimer > 0f) ? 180f : (released ? 45f : 18f);
                    _spinAngle += dir * rpm * dt;
                    transform.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, _spinAngle);
                    break;
                }

                case HangarPropRole.FuelPipeValve:
                {
                    bool open = mgr != null && mgr.pipeValvesOpen;
                    float targetSpin = open ? 360f : 0f;
                    _spinAngle = Mathf.MoveTowards(_spinAngle, targetSpin, dt * 260f);
                    transform.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, _spinAngle);
                    break;
                }

                case HangarPropRole.PowerJunctionBox:
                {
                    float vib = (_isActiveState ? 0.008f : 0.002f) * Mathf.Sin(t * 28f);
                    transform.localPosition = _baseLocalPos + new Vector3(0f, vib, 0f);
                    if (_indicatorLight != null)
                    {
                        bool hot = mgr != null && mgr.IsOverheating;
                        _indicatorLight.color = hot ? new Color(1f, 0.2f, 0.15f) : (_isActiveState ? new Color(0.2f, 1f, 0.55f) : GetIdleColor());
                        _indicatorLight.intensity = _isActiveState ? (2.4f + Mathf.Sin(t * 10f) * 0.7f) : 1.1f;
                    }
                    break;
                }

                case HangarPropRole.GasCylinderRack:
                {
                    bool hot = mgr != null && mgr.IsOverheating;
                    float shake = (_actionAnimTimer > 0f)
                        ? Mathf.Sin(t * 58f) * 0.022f * (_actionAnimTimer / 1.4f)
                        : (hot ? Mathf.Sin(t * 32f) * 0.008f : Mathf.Sin(t * 5f) * 0.002f);
                    transform.localPosition = _baseLocalPos + new Vector3(shake, 0f, 0f);
                    if (_indicatorLight != null)
                    {
                        _indicatorLight.color = hot ? new Color(1f, 0.25f, 0.15f) : GetIdleColor();
                        _indicatorLight.intensity = hot ? (2.2f + Mathf.Abs(Mathf.Sin(t * 12f)) * 1.8f) : 1.2f;
                    }
                    break;
                }

                case HangarPropRole.MainFuelTank:
                {
                    bool pumping = mgr != null && mgr.isFuelPumping;
                    float pumpFreq = pumping ? 11f : 2.6f;
                    float pumpAmp = pumping ? 0.018f : 0.004f;
                    float s = 1f + Mathf.Sin(t * pumpFreq) * pumpAmp;
                    transform.localScale = new Vector3(_baseLocalScale.x * s, _baseLocalScale.y, _baseLocalScale.z * s);
                    if (pumping && _burstVfx != null && Random.value < dt * 8f)
                        _burstVfx.Emit(3);
                    if (_indicatorLight != null)
                    {
                        bool full = mgr != null && mgr.fuelPercent >= 99f;
                        _indicatorLight.color = full ? new Color(0.2f, 1f, 0.5f) : (pumping ? new Color(0.15f, 0.9f, 1f) : GetIdleColor());
                        _indicatorLight.intensity = pumping ? (3.0f + Mathf.Sin(t * 12f) * 0.9f) : 1.3f;
                    }
                    break;
                }

                case HangarPropRole.ShipDiagnosticHolo:
                {
                    bool scanning = mgr != null && mgr.isHullScanning;
                    float pulse = scanning ? (1.10f + Mathf.Sin(t * 16f) * 0.05f) : 1f;
                    transform.localScale = Vector3.Lerp(transform.localScale, _baseLocalScale * pulse, dt * 8f);
                    if (_indicatorLight != null)
                    {
                        bool done = mgr != null && mgr.hullScanComplete;
                        _indicatorLight.color = done ? new Color(0.2f, 1f, 0.5f) : (scanning ? new Color(1f, 0.78f, 0.15f) : GetIdleColor());
                        _indicatorLight.intensity = scanning ? 3.5f : 1.5f;
                    }
                    break;
                }

                default:
                {
                    float lift = _isActiveState ? 0.08f : 0f;
                    float bounce = (_actionAnimTimer > 0f) ? Mathf.Sin(_actionAnimTimer * 18f) * 0.03f : 0f;
                    transform.localPosition = Vector3.Lerp(transform.localPosition, _baseLocalPos + new Vector3(0f, lift + bounce, 0f), dt * 7f);
                    break;
                }
            }
        }

        private void ExecuteFunction()
        {
            _isActiveState = !_isActiveState;
            _actionAnimTimer = 1.4f;
            var mgr = HangarSimulationManager.Instance;

            switch (role)
            {
                case HangarPropRole.PowerJunctionBox:
                    if (mgr != null) mgr.AdjustPowerGrid(_isActiveState ? 25f : -25f);
                    PlaySound(_clipEngage);
                    int pwr = mgr != null ? Mathf.RoundToInt(mgr.powerGridPercent) : 100;
                    ShowToast(_isActiveState
                        ? $"ĐÃ ĐÓNG CẦU DAO — ĐIỆN LƯỚI HANGAR: {pwr}%"
                        : $"ĐÃ NGẮT CẦU DAO — ĐIỆN LƯỚI HANGAR: {pwr}%", pwr < 50);
                    break;

                case HangarPropRole.GasCylinderRack:
                    if (_burstVfx != null) _burstVfx.Emit(36);
                    if (mgr != null) mgr.PurgeCoolant(32f);
                    PlaySound(_clipHiss);
                    int temp = mgr != null ? Mathf.RoundToInt(mgr.coreTemperatureC) : 35;
                    ShowToast($"XẢ NITƠ LÀM MÁT THÀNH CÔNG — NHIỆT ĐỘ XƯỞNG: {temp}°C", false);
                    break;

                case HangarPropRole.CableWinchReel:
                    if (mgr != null) mgr.ToggleLandingClamps();
                    PlaySound(_clipHiss);
                    bool rel = mgr != null && mgr.clampsReleased;
                    ShowToast(rel
                        ? "ĐÃ MỞ 4 KẸP THỦY LỰC BÃI ĐÁP — TÀU SẴN SÀNG RỜI BỆ!"
                        : "ĐÃ KHÓA CHẶT 4 KẸP THỦY LỰC GIỮ TÀU", false);
                    break;

                case HangarPropRole.FuelPipeValve:
                    bool openValve = mgr != null ? !mgr.pipeValvesOpen : _isActiveState;
                    if (mgr != null) mgr.SetPipeValvesOpen(openValve);
                    PlaySound(_clipHiss);
                    ShowToast(openValve
                        ? "ĐÃ MỞ VAN ĐƯỜNG ỐNG — CÓ THỂ BƠM NHIÊN LIỆU TỪ BỒN!"
                        : "ĐÃ KHÓA VAN ĐƯỜNG ỐNG NHIÊN LIỆU", false);
                    break;

                case HangarPropRole.MainFuelTank:
                    if (mgr != null)
                    {
                        if (mgr.fuelPercent >= 99.5f)
                        {
                            PlaySound(_clipEngage);
                            ShowToast("BÌNH NHIÊN LIỆU TÀU ĐÃ ĐẦY 100%!", false);
                        }
                        else if (!mgr.ToggleFuelPump())
                        {
                            PlaySound(_clipError);
                            ShowToast("LỖI NGHẼN ÁP SUẤT! HÃY MỞ VAN ĐƯỜNG ỐNG (PIPE VALVE) TRƯỚC!", true);
                        }
                        else
                        {
                            PlaySound(_clipEngage);
                            ShowToast(mgr.isFuelPumping
                                ? $"ĐANG BƠM NHIÊN LIỆU VÀO TÀU ({Mathf.RoundToInt(mgr.fuelPercent)}% -> 100%)..."
                                : "ĐÃ TẠM DỪNG BƠM NHIÊN LIỆU", false);
                        }
                    }
                    break;

                case HangarPropRole.ShipDiagnosticHolo:
                    if (mgr != null) mgr.StartFullShipScan();
                    PlaySound(_clipScan);
                    ShowToast("ĐANG QUÉT THÂN VỎ — ĐIỀU ĐỘNG DRONE & ROBOT HÀN KIỂM TRA!", false);
                    break;

                default:
                    PlaySound(_clipEngage);
                    if (mgr != null && mgr.powerGridPercent < 100f) mgr.AdjustPowerGrid(10f);
                    ShowToast($"{propTitle}: ĐÃ KIỂM TRA & BỔ SUNG NGUỒN DỰ PHÒNG", false);
                    break;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (_audio != null && clip != null)
                _audio.PlayOneShot(clip);
        }

        private void ShowToast(string msg, bool warning)
        {
            _toastMsg = msg;
            _toastIsWarning = warning;
            _toastTimer = 3.4f;
        }

        private string GetDynamicPromptVerb()
        {
            var mgr = HangarSimulationManager.Instance;
            switch (role)
            {
                case HangarPropRole.PowerJunctionBox:
                    int p = mgr != null ? Mathf.RoundToInt(mgr.powerGridPercent) : 50;
                    return _isActiveState ? $"NGẮT CẦU DAO ({p}%)" : $"ĐÓNG CẦU DAO ({p}%)";
                case HangarPropRole.GasCylinderRack:
                    int tc = mgr != null ? Mathf.RoundToInt(mgr.coreTemperatureC) : 42;
                    return mgr != null && mgr.IsOverheating ? $"XẢ NITƠ KHẨN ({tc}°C!)" : $"XẢ NITƠ LÀM MÁT ({tc}°C)";
                case HangarPropRole.CableWinchReel:
                    return (mgr != null && mgr.clampsReleased) ? "KHÓA KẸP BÃI ĐÁP" : "MỞ KẸP BÃI ĐÁP";
                case HangarPropRole.FuelPipeValve:
                    return (mgr != null && mgr.pipeValvesOpen) ? "KHÓA VAN ỐNG" : "MỞ VAN ỐNG NHIÊN LIỆU";
                case HangarPropRole.MainFuelTank:
                    if (mgr == null) return "BƠM NHIÊN LIỆU";
                    if (mgr.fuelPercent >= 99.5f) return "NHIÊN LIỆU 100%";
                    if (!mgr.pipeValvesOpen) return "CẦN MỞ VAN ỐNG TRƯỚC";
                    return mgr.isFuelPumping ? $"ĐANG BƠM ({Mathf.RoundToInt(mgr.fuelPercent)}%)" : $"BƠM NHIÊN LIỆU ({Mathf.RoundToInt(mgr.fuelPercent)}%)";
                case HangarPropRole.ShipDiagnosticHolo:
                    if (mgr != null && mgr.isHullScanning) return $"ĐANG QUÉT ({Mathf.RoundToInt(mgr.hullScanProgress)}%)";
                    return (mgr != null && mgr.hullScanComplete) ? "QUÉT LẠI THÂN VỎ" : "QUÉT TÀU & GỌI DRONE";
                default:
                    return "KIỂM TRA THIẾT BỊ";
            }
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0.01f || PlayerCharacterController.AnyUiPanelOpen) return;

            if (_closestProp == this)
            {
                WorldInteractPromptUI.DrawAtWorldPos(transform.position + Vector3.up * 0.75f, "E", GetDynamicPromptVerb());
            }

            if (_toastTimer > 0f && !string.IsNullOrEmpty(_toastMsg))
            {
                GUIStyle tStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                tStyle.normal.textColor = _toastIsWarning ? new Color(1f, 0.35f, 0.25f) : new Color(0.35f, 1f, 0.65f);
                GUI.Box(new Rect(Screen.width * 0.5f - 290f, Screen.height - 95f, 580f, 38f), _toastMsg, tStyle);
            }
        }
    }
}
