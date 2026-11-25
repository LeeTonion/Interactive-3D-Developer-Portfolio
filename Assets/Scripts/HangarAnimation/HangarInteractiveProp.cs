using TMPro;
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
        [Tooltip("0..3 for the 4 individual Landing Pad cables/clamps; -1 toggles all.")]
        public int cableIndex = -1;
        [Tooltip("Optional world-space anchor override (e.g. LineRenderer cable endpoint).")]
        public bool useCustomLocalAnchor = false;
        public Vector3 customLocalAnchor;

        private Vector3 GetInteractWorldPos()
        {
            return useCustomLocalAnchor ? transform.TransformPoint(customLocalAnchor) : transform.position;
        }

        private Transform _player;
        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;
        private Vector3 _baseLocalScale;
        private Light _indicatorLight;
        private ParticleSystem _burstVfx;
        private AudioSource _audio;
        private Renderer _beaconOrbRenderer;
        private Material _beaconMat;
        private Transform _leverPivot;
        private float _leverAngle = -42f; // Starts UP (-42 deg), pulls DOWN (+42 deg) to turn ON
        private Material _leverHandleMat;

        // Detailed Realistic Fuel Valve & Fuel Pump animated parts
        private Transform _valveWheelPivot;
        private Transform _valveNeedlePivot;
        private float _valveWheelAngle;
        private float _valveNeedleAngle = -55f;
        private Material _valveStatusMat;
        private Material _valvePipeGlowMat;

        private Transform _pumpLeverPivot;
        private float _pumpLeverAngle = 35f;
        private Material _pumpStatusMat;
        private TextMeshPro _pumpLevelText;
        private readonly Renderer[] _pumpGaugeSegs = new Renderer[8];
        private Material[] _pumpSegMats;

        // Physical Ship Scan Button Pod
        private Transform _scanButtonCap;
        private Transform _scanRadarDish;
        private Material _scanButtonMat;
        private float _scanPressAnim;

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
            // 1. Bind pre-placed scene objects if they already exist in Hierarchy (no runtime GameObject spawning!)
            _indicatorLight = GetComponentInChildren<Light>(true);
            _burstVfx = GetComponentInChildren<ParticleSystem>(true);

            if (role == HangarPropRole.FuelPipeValve)
            {
                if (transform.Find("Realistic_Valve_Station") != null)
                    BindValveStationRefs();
                else if (!Application.isPlaying)
                    BuildRealisticValveStation();
            }
            else if (role == HangarPropRole.MainFuelTank)
            {
                if (transform.Find("Realistic_FuelPump_Station") != null)
                    BindFuelPumpRefs();
                else if (!Application.isPlaying)
                    BuildRealisticFuelPumpStation();
            }
            else if (role == HangarPropRole.ShipDiagnosticHolo)
            {
                if (transform.Find("Realistic_ScanButton_Pod") != null)
                    BindScanButtonRefs();
                else if (!Application.isPlaying)
                    BuildRealisticScanButtonPod();
            }
            else if (role == HangarPropRole.PowerJunctionBox)
            {
                if (transform.Find("Breaker_Lever_Pivot") != null)
                    BindBreakerLeverRefs();
                else if (!Application.isPlaying)
                    BuildBreakerLever();
            }

            if (_indicatorLight == null && !Application.isPlaying &&
                (role == HangarPropRole.PowerJunctionBox ||
                 role == HangarPropRole.MainFuelTank ||
                 role == HangarPropRole.ShipDiagnosticHolo ||
                 role == HangarPropRole.GasCylinderRack ||
                 role == HangarPropRole.FuelPipeValve))
            {
                GameObject lGo = new GameObject("Prop_StatusLight");
                lGo.transform.SetParent(transform, false);
                lGo.transform.localPosition = useCustomLocalAnchor
                    ? customLocalAnchor + new Vector3(0f, 0.85f, 0f)
                    : new Vector3(0f, 1.1f, 0.35f);
                _indicatorLight = lGo.AddComponent<Light>();
                _indicatorLight.type = LightType.Point;
                _indicatorLight.range = 4.2f;
                _indicatorLight.intensity = 2.0f;
                _indicatorLight.color = GetIdleColor();
            }

            if (_burstVfx == null && !Application.isPlaying &&
                (role == HangarPropRole.GasCylinderRack || role == HangarPropRole.MainFuelTank))
            {
                GameObject vfxGo = new GameObject(role == HangarPropRole.GasCylinderRack ? "Coolant_Purge_VFX" : "Fuel_Flow_VFX");
                vfxGo.transform.SetParent(transform, false);
                vfxGo.transform.localPosition = useCustomLocalAnchor
                    ? customLocalAnchor + new Vector3(0f, 1.25f, 0f)
                    : new Vector3(0f, 1.25f, 0f);
                _burstVfx = vfxGo.AddComponent<ParticleSystem>();
                _burstVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
                if (pShader != null && Application.isPlaying)
                {
                    Material m = new Material(pShader);
                    m.color = main.startColor.color;
                    rend.sharedMaterial = m;
                }
            }
        }

        public void EnsureSceneRigInEditor()
        {
            SetupIndicatorAndVfx();
        }

        private void BindBreakerLeverRefs()
        {
            _leverPivot = transform.Find("Breaker_Lever_Pivot");
            if (_leverPivot != null)
            {
                var grip = _leverPivot.Find("Lever_Grip");
                if (grip != null)
                {
                    var r = grip.GetComponent<Renderer>();
                    if (r != null) _leverHandleMat = Application.isPlaying ? r.material : r.sharedMaterial;
                }
            }
        }

        private void BuildBreakerLever()
        {
            if (transform.Find("Breaker_Lever_Pivot") != null) { BindBreakerLeverRefs(); return; }

            GameObject pivot = new GameObject("Breaker_Lever_Pivot");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(0f, -0.05f, 0.34f);
            pivot.transform.localRotation = Quaternion.Euler(-42f, 0f, 0f);
            _leverPivot = pivot.transform;

            Material mArm = LoadHangarMat("Mat_Hangar_DarkFrame_AAA.mat", new Color(0.22f, 0.26f, 0.32f), 0.8f, 0.5f);
            Material mGrip = LoadHangarMat("Mat_Hangar_Emissive_Red.mat", new Color(1f, 0.25f, 0.18f), 0.2f, 0.5f);

            MakePart(pivot.transform, PrimitiveType.Cube, "Lever_Arm", new Vector3(0f, 0f, 0.24f), new Vector3(0.08f, 0.08f, 0.48f), Vector3.zero, mArm);
            GameObject handle = MakePart(pivot.transform, PrimitiveType.Cylinder, "Lever_Grip", new Vector3(0f, 0f, 0.48f), new Vector3(0.11f, 0.28f, 0.11f), new Vector3(0f, 0f, 90f), mGrip);
            if (handle != null && Application.isPlaying)
                _leverHandleMat = handle.GetComponent<Renderer>().material;
        }

        private void BindValveStationRefs()
        {
            Transform rig = transform.Find("Realistic_Valve_Station");
            if (rig == null) return;
            _valveWheelPivot = rig.Find("Valve_Handwheel_Pivot");
            _valveNeedlePivot = rig.Find("Gauge_Needle_Pivot");
            var led = rig.Find("Status_LED")?.GetComponent<Renderer>();
            if (led != null) _valveStatusMat = Application.isPlaying ? led.material : led.sharedMaterial;
            var ring = rig.Find("Pipe_Glow_Ring1")?.GetComponent<Renderer>();
            if (ring != null) _valvePipeGlowMat = Application.isPlaying ? ring.material : ring.sharedMaterial;
        }

        private void BindFuelPumpRefs()
        {
            Transform rig = transform.Find("Realistic_FuelPump_Station");
            if (rig == null) return;
            _pumpLeverPivot = rig.Find("Fuel_Pump_Lever_Pivot");
            _pumpLevelText = rig.Find("Fuel_Level_TMP")?.GetComponent<TextMeshPro>();
            var bar = rig.Find("Pump_Status_Bar")?.GetComponent<Renderer>();
            if (bar != null) _pumpStatusMat = Application.isPlaying ? bar.material : bar.sharedMaterial;
            _pumpSegMats = new Material[8];
            for (int i = 0; i < 8; i++)
            {
                var seg = rig.Find($"Gauge_Seg_{i}")?.GetComponent<Renderer>();
                _pumpGaugeSegs[i] = seg;
                if (seg != null) _pumpSegMats[i] = Application.isPlaying ? seg.material : seg.sharedMaterial;
            }
        }

        private void BindScanButtonRefs()
        {
            Transform rig = transform.Find("Realistic_ScanButton_Pod");
            if (rig == null) return;
            _scanButtonCap = rig.Find("Scan_Button_Cap");
            _scanRadarDish = rig.Find("Radar_Spinner");
            var capR = _scanButtonCap != null ? _scanButtonCap.GetComponent<Renderer>() : null;
            if (capR != null) _scanButtonMat = Application.isPlaying ? capR.material : capR.sharedMaterial;
        }

        private void BuildRealisticValveStation()
        {
            if (transform.Find("Realistic_Valve_Station") != null) return;

            // AAA Wall-Mounted Pressure Valve Station (like Image 3 Left) bolted to left wall (-17.15, 3.65, -13.90)
            GameObject rig = new GameObject("Realistic_Valve_Station");
            rig.transform.SetParent(transform, false);
            rig.transform.localPosition = useCustomLocalAnchor ? customLocalAnchor : Vector3.zero;

            Material darkMat = CreateMetallicMat(new Color(0.13f, 0.16f, 0.21f), 0.78f, 0.45f);
            Material midSteelMat = CreateMetallicMat(new Color(0.28f, 0.33f, 0.40f), 0.75f, 0.50f);
            Material chromeMat = CreateMetallicMat(new Color(0.68f, 0.73f, 0.78f), 0.90f, 0.70f);
            Material hazardMat = CreateMetallicMat(new Color(0.92f, 0.64f, 0.08f), 0.35f, 0.48f);
            Material redValveMat = CreateMetallicMat(new Color(0.88f, 0.15f, 0.12f), 0.42f, 0.52f);
            Material dialFaceMat = CreateEmissiveMat(new Color(0.85f, 0.94f, 1.0f), 1.3f);
            Material cyanRingMat = CreateEmissiveMat(new Color(0.14f, 0.88f, 1.0f), 2.4f);
            _valveStatusMat = CreateEmissiveMat(new Color(1f, 0.28f, 0.14f), 2.6f);
            _valvePipeGlowMat = CreateEmissiveMat(new Color(1f, 0.58f, 0.12f), 2.0f);

            // Multi-layered wall backplate + corner bolts + hazard trim
            MakePart(rig.transform, PrimitiveType.Cube, "Wall_Backplate", new Vector3(-0.24f, 0.05f, 0f), new Vector3(0.12f, 1.85f, 1.35f), Vector3.zero, darkMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Wall_InnerPlate", new Vector3(-0.15f, 0.05f, 0f), new Vector3(0.08f, 1.48f, 1.04f), Vector3.zero, midSteelMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Wall_Hazard_Top", new Vector3(-0.18f, 0.92f, 0f), new Vector3(0.14f, 0.08f, 1.32f), Vector3.zero, hazardMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Wall_Hazard_Bot", new Vector3(-0.18f, -0.82f, 0f), new Vector3(0.14f, 0.08f, 1.32f), Vector3.zero, hazardMat);

            // Circular Pressure Gauge at the top with rotating needle
            MakePart(rig.transform, PrimitiveType.Cylinder, "Gauge_Bezel", new Vector3(-0.08f, 0.62f, 0f), new Vector3(0.42f, 0.06f, 0.42f), new Vector3(0f, 0f, 90f), cyanRingMat);
            MakePart(rig.transform, PrimitiveType.Cylinder, "Gauge_Face", new Vector3(-0.04f, 0.62f, 0f), new Vector3(0.34f, 0.04f, 0.34f), new Vector3(0f, 0f, 90f), dialFaceMat);
            GameObject needlePiv = new GameObject("Gauge_Needle_Pivot");
            needlePiv.transform.SetParent(rig.transform, false);
            needlePiv.transform.localPosition = new Vector3(-0.01f, 0.62f, 0f);
            _valveNeedlePivot = needlePiv.transform;
            MakePart(needlePiv.transform, PrimitiveType.Cube, "Needle", new Vector3(0f, 0.06f, 0f), new Vector3(0.02f, 0.14f, 0.025f), Vector3.zero, redValveMat);

            // Side switch box + status light
            MakePart(rig.transform, PrimitiveType.Cube, "Side_SwitchBox", new Vector3(-0.07f, 0.06f, -0.44f), new Vector3(0.12f, 0.38f, 0.18f), Vector3.zero, darkMat);
            MakePart(rig.transform, PrimitiveType.Cylinder, "Status_LED", new Vector3(0.0f, 0.12f, -0.44f), new Vector3(0.08f, 0.03f, 0.08f), new Vector3(0f, 0f, 90f), _valveStatusMat);

            // Valve central housing & heavy steel pipe dropping straight down to FloorUmbilical_Port2 (y=2.46)
            MakePart(rig.transform, PrimitiveType.Cube, "Valve_Housing", new Vector3(-0.02f, 0.05f, 0f), new Vector3(0.28f, 0.46f, 0.46f), Vector3.zero, darkMat);
            MakePart(rig.transform, PrimitiveType.Cylinder, "Drop_Pipe", new Vector3(0.04f, -0.62f, 0f), new Vector3(0.24f, 0.58f, 0.24f), Vector3.zero, chromeMat);
            MakePart(rig.transform, PrimitiveType.Cylinder, "Pipe_Glow_Ring1", new Vector3(0.04f, -0.78f, 0f), new Vector3(0.31f, 0.045f, 0.31f), Vector3.zero, _valvePipeGlowMat);
            MakePart(rig.transform, PrimitiveType.Cylinder, "Pipe_Glow_Ring2", new Vector3(0.04f, -0.96f, 0f), new Vector3(0.31f, 0.045f, 0.31f), Vector3.zero, _valvePipeGlowMat);
            MakePart(rig.transform, PrimitiveType.Cylinder, "Floor_Collar", new Vector3(0.04f, -1.14f, 0f), new Vector3(0.42f, 0.07f, 0.42f), Vector3.zero, darkMat);

            // Rotating 4-spoke Red Industrial Handwheel facing +X into the Hangar
            GameObject wheelPivot = new GameObject("Valve_Handwheel_Pivot");
            wheelPivot.transform.SetParent(rig.transform, false);
            wheelPivot.transform.localPosition = new Vector3(0.22f, 0.05f, 0f);
            _valveWheelPivot = wheelPivot.transform;

            MakePart(wheelPivot.transform, PrimitiveType.Cylinder, "Wheel_Stem", new Vector3(-0.08f, 0f, 0f), new Vector3(0.10f, 0.12f, 0.10f), new Vector3(0f, 0f, 90f), chromeMat);
            MakePart(wheelPivot.transform, PrimitiveType.Cylinder, "Wheel_Hub", Vector3.zero, new Vector3(0.16f, 0.05f, 0.16f), new Vector3(0f, 0f, 90f), redValveMat);
            MakePart(wheelPivot.transform, PrimitiveType.Cube, "Spoke_Vert", Vector3.zero, new Vector3(0.045f, 0.62f, 0.055f), Vector3.zero, redValveMat);
            MakePart(wheelPivot.transform, PrimitiveType.Cube, "Spoke_Horiz", Vector3.zero, new Vector3(0.045f, 0.055f, 0.62f), Vector3.zero, redValveMat);
            MakePart(wheelPivot.transform, PrimitiveType.Cylinder, "Rim_Ring", new Vector3(0.02f, 0f, 0f), new Vector3(0.64f, 0.032f, 0.64f), new Vector3(0f, 0f, 90f), redValveMat);
        }

        private void BuildRealisticFuelPumpStation()
        {
            if (transform.Find("Realistic_FuelPump_Station") != null) return;

            // AAA Industrial Fuel Pump Unit (like Image 3 Right)
            GameObject rig = new GameObject("Realistic_FuelPump_Station");
            rig.transform.SetParent(transform, false);
            rig.transform.localPosition = useCustomLocalAnchor ? customLocalAnchor : Vector3.zero;
            // If placed along the left wall (x < 0), rotate 180 deg so front face (+X) looks into the Hangar!
            if (useCustomLocalAnchor && customLocalAnchor.x < 0f)
                rig.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

            Material darkMat = CreateMetallicMat(new Color(0.12f, 0.15f, 0.20f), 0.78f, 0.45f);
            Material bodySteelMat = CreateMetallicMat(new Color(0.26f, 0.31f, 0.38f), 0.72f, 0.50f);
            Material chromeMat = CreateMetallicMat(new Color(0.68f, 0.73f, 0.78f), 0.90f, 0.68f);
            Material hazardMat = CreateMetallicMat(new Color(0.92f, 0.62f, 0.08f), 0.35f, 0.48f);
            Material redGripMat = CreateMetallicMat(new Color(0.88f, 0.16f, 0.14f), 0.40f, 0.52f);
            _pumpStatusMat = CreateEmissiveMat(new Color(1f, 0.65f, 0.12f), 2.4f);

            // Heavy reinforced base plinth + yellow corner bumpers
            MakePart(rig.transform, PrimitiveType.Cube, "Pump_Base", new Vector3(0f, 0.12f, 0f), new Vector3(1.10f, 0.24f, 1.95f), Vector3.zero, darkMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Bumper_Fore", new Vector3(-0.48f, 0.22f, 0.84f), new Vector3(0.28f, 0.34f, 0.28f), Vector3.zero, hazardMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Bumper_Aft", new Vector3(-0.48f, 0.22f, -0.84f), new Vector3(0.28f, 0.34f, 0.28f), Vector3.zero, hazardMat);

            // Main armored machine cabinet
            MakePart(rig.transform, PrimitiveType.Cube, "Main_Cabinet", new Vector3(0.05f, 1.06f, 0f), new Vector3(0.82f, 1.68f, 1.56f), Vector3.zero, bodySteelMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Top_Crown", new Vector3(0.05f, 1.95f, 0f), new Vector3(0.74f, 0.16f, 1.38f), Vector3.zero, darkMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Top_Hazard_Strip", new Vector3(-0.34f, 1.78f, 0f), new Vector3(0.08f, 0.10f, 1.44f), Vector3.zero, hazardMat);

            // Left front (-Z side on -X face): Segmented Glass Fuel Level Gauge + Live TMP "FUEL LEVEL XX%"
            MakePart(rig.transform, PrimitiveType.Cube, "Gauge_Frame", new Vector3(-0.37f, 0.82f, -0.40f), new Vector3(0.12f, 1.02f, 0.44f), Vector3.zero, darkMat);
            _pumpSegMats = new Material[8];
            for (int i = 0; i < 8; i++)
            {
                float segY = 0.42f + i * 0.105f;
                Material sm = CreateEmissiveMat(new Color(0.10f, 0.14f, 0.18f), 0.3f);
                _pumpSegMats[i] = sm;
                GameObject seg = MakePart(rig.transform, PrimitiveType.Cube, $"Gauge_Seg_{i}", new Vector3(-0.42f, segY, -0.40f), new Vector3(0.05f, 0.075f, 0.30f), Vector3.zero, sm);
                _pumpGaugeSegs[i] = seg.GetComponent<Renderer>();
            }

            GameObject txtGo = new GameObject("Fuel_Level_TMP");
            txtGo.transform.SetParent(rig.transform, false);
            txtGo.transform.localPosition = new Vector3(-0.39f, 1.50f, -0.40f);
            txtGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            _pumpLevelText = txtGo.AddComponent<TextMeshPro>();
            _pumpLevelText.font = HangarUITheme.GetUnicodeFont();
            _pumpLevelText.text = "FUEL LEVEL\n25%";
            _pumpLevelText.fontSize = 1.65f;
            _pumpLevelText.fontStyle = FontStyles.Bold;
            _pumpLevelText.alignment = TextAlignmentOptions.Center;
            _pumpLevelText.color = new Color(1f, 0.78f, 0.16f);

            // Right front (+Z side on -X face): Lever Track Housing + Heavy Mechanical 3D Lever with Red T-Handle
            MakePart(rig.transform, PrimitiveType.Cube, "Lever_Housing", new Vector3(-0.37f, 0.92f, 0.36f), new Vector3(0.14f, 0.92f, 0.44f), Vector3.zero, darkMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Lever_Slot", new Vector3(-0.43f, 0.92f, 0.36f), new Vector3(0.04f, 0.64f, 0.10f), Vector3.zero, chromeMat);

            GameObject leverPivot = new GameObject("Fuel_Pump_Lever_Pivot");
            leverPivot.transform.SetParent(rig.transform, false);
            leverPivot.transform.localPosition = new Vector3(-0.45f, 0.92f, 0.36f);
            leverPivot.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            _pumpLeverPivot = leverPivot.transform;

            MakePart(leverPivot.transform, PrimitiveType.Cylinder, "Lever_Hub", Vector3.zero, new Vector3(0.16f, 0.08f, 0.16f), new Vector3(0f, 0f, 90f), chromeMat);
            MakePart(leverPivot.transform, PrimitiveType.Cylinder, "Pump_Lever_Arm", new Vector3(-0.24f, 0f, 0f), new Vector3(0.07f, 0.26f, 0.07f), new Vector3(0f, 0f, 90f), chromeMat);
            MakePart(leverPivot.transform, PrimitiveType.Cylinder, "Pump_Lever_T_Grip", new Vector3(-0.50f, 0f, 0f), new Vector3(0.11f, 0.24f, 0.11f), new Vector3(90f, 0f, 0f), redGripMat);

            // Top status beacon bar
            MakePart(rig.transform, PrimitiveType.Cube, "Pump_Status_Bar", new Vector3(-0.34f, 1.98f, 0f), new Vector3(0.10f, 0.08f, 0.72f), Vector3.zero, _pumpStatusMat);
        }

        private void BuildRealisticScanButtonPod()
        {
            if (transform.Find("Realistic_ScanButton_Pod") != null) return;

            // Wall-mounted Ship Scan Trigger Panel on the aft wall (-3.80, 3.65, -25.35), facing +Z into Hangar
            GameObject rig = new GameObject("Realistic_ScanButton_Pod");
            rig.transform.SetParent(transform, false);
            rig.transform.localPosition = useCustomLocalAnchor ? customLocalAnchor : Vector3.zero;

            Material darkMat = CreateMetallicMat(new Color(0.12f, 0.15f, 0.20f), 0.78f, 0.45f);
            Material steelMat = CreateMetallicMat(new Color(0.55f, 0.60f, 0.66f), 0.85f, 0.60f);
            Material hazardMat = CreateMetallicMat(new Color(0.92f, 0.62f, 0.08f), 0.35f, 0.45f);
            _scanButtonMat = CreateEmissiveMat(new Color(0.14f, 0.90f, 1f), 2.6f);

            // Wall Backplate & Hazard Frame bolted to aft wall
            MakePart(rig.transform, PrimitiveType.Cube, "Wall_Backplate", new Vector3(0f, 0f, -0.14f), new Vector3(0.88f, 1.05f, 0.12f), Vector3.zero, darkMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Wall_Hazard_Top", new Vector3(0f, 0.48f, -0.09f), new Vector3(0.86f, 0.08f, 0.08f), Vector3.zero, hazardMat);
            MakePart(rig.transform, PrimitiveType.Cube, "Wall_Hazard_Bot", new Vector3(0f, -0.48f, -0.09f), new Vector3(0.86f, 0.08f, 0.08f), Vector3.zero, hazardMat);
            MakePart(rig.transform, PrimitiveType.Cylinder, "Button_Collar", new Vector3(0f, -0.04f, -0.02f), new Vector3(0.46f, 0.05f, 0.46f), new Vector3(90f, 0f, 0f), hazardMat);

            // Big Glowing Physical Push-Button Cap protruding +Z from the wall (depresses into wall when pressed!)
            _scanButtonCap = MakePart(rig.transform, PrimitiveType.Cylinder, "Scan_Button_Cap", new Vector3(0f, -0.04f, 0.06f), new Vector3(0.34f, 0.07f, 0.34f), new Vector3(90f, 0f, 0f), _scanButtonMat).transform;

            // Compact Rotating Radar Scanner on top of the wall panel
            MakePart(rig.transform, PrimitiveType.Cylinder, "Radar_Mast", new Vector3(0f, 0.64f, -0.08f), new Vector3(0.07f, 0.18f, 0.07f), Vector3.zero, steelMat);
            GameObject radarSpin = new GameObject("Radar_Spinner");
            radarSpin.transform.SetParent(rig.transform, false);
            radarSpin.transform.localPosition = new Vector3(0f, 0.84f, -0.08f);
            _scanRadarDish = radarSpin.transform;
            MakePart(radarSpin.transform, PrimitiveType.Cube, "Radar_Bar", Vector3.zero, new Vector3(0.52f, 0.07f, 0.10f), Vector3.zero, _scanButtonMat);
        }

        private static GameObject MakePart(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        private static Material LoadHangarMat(string fileName, Color fallbackCol, float metallic, float smoothness)
        {
#if UNITY_EDITOR
            var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Hangar_AAA/" + fileName);
            if (m != null) return m;
#endif
            return CreateMetallicMat(fallbackCol, metallic, smoothness);
        }

        private static Material CreateMetallicMat(Color col, float metallic, float smoothness)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string path = col.r > 0.6f && col.g < 0.3f ? "Assets/_Project/Materials/Hangar_AAA/Mat_Valve_IndustrialRed.mat"
                    : (col.r > 0.6f && col.g > 0.4f ? "Assets/_Project/Materials/Hangar_AAA/Mat_Hangar_HazardStripe_AAA.mat"
                    : (metallic > 0.82f ? "Assets/_Project/Materials/Hangar_AAA/Mat_Pipe_BrushedSteel.mat"
                    : "Assets/_Project/Materials/Hangar_AAA/Mat_Hangar_DarkFrame_AAA.mat"));
                var assetMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (assetMat != null) return assetMat;
            }
#endif
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(sh);
            m.color = col;
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private static Material CreateEmissiveMat(Color col, float intensity)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string path = col.g > 0.7f && col.b > 0.7f ? "Assets/_Project/Materials/Hangar_AAA/Mat_Hangar_Emissive_Cyan.mat"
                    : (col.g > 0.7f ? "Assets/_Project/Materials/Hangar_AAA/Mat_Hangar_Emissive_Grn.mat"
                    : (col.r > 0.7f && col.g < 0.4f ? "Assets/_Project/Materials/Hangar_AAA/Mat_Hangar_Emissive_Red.mat"
                    : "Assets/_Project/Materials/Hangar_AAA/Mat_Hangar_Emissive_Amber.mat"));
                var assetMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (assetMat != null) return assetMat;
            }
#endif
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(sh);
            m.color = col;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", col * intensity);
            return m;
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
                dist = Vector3.Distance(_player.position, GetInteractWorldPos());

            if (dist <= interactRadius && dist < _closestDist && HasLineOfSight())
            {
                _closestDist = dist;
                _closestProp = this;
            }

            if (_toastTimer > 0f) _toastTimer -= dt;
            if (_actionAnimTimer > 0f) _actionAnimTimer = Mathf.Max(0f, _actionAnimTimer - dt);

            UpdateTaskBeaconVisual();
            AnimateProp(simDt);
        }

        private bool HasLineOfSight()
        {
            if (_player == null) return false;

            Vector3 targetPos = GetInteractWorldPos() + Vector3.up * 0.45f;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 toTarget = (targetPos - cam.transform.position).normalized;
                if (Vector3.Dot(cam.transform.forward, toTarget) < 0.25f)
                    return false; // Behind or outside player's view

                Vector3 vp = cam.WorldToViewportPoint(targetPos);
                if (vp.z <= 0.1f || vp.x < 0.04f || vp.x > 0.96f || vp.y < 0.04f || vp.y > 0.96f)
                    return false; // Outside screen viewport
            }

            Vector3 eyePos = cam != null ? cam.transform.position : (_player.position + Vector3.up * 1.45f);
            Vector3 dir = targetPos - eyePos;
            float len = dir.magnitude;
            if (len < 0.2f) return true;

            RaycastHit[] hits = Physics.RaycastAll(eyePos, dir / len, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Transform ht = hits[i].transform;
                if (ht == null) continue;
                if (ht.IsChildOf(_player) || ht.IsChildOf(transform)) continue;
                if (ht.GetComponentInParent<PlayerSpaceshipController>() != null) continue;
                if (hits[i].distance < len - 0.35f)
                    return false;
            }
            return true;
        }

        private void UpdateTaskBeaconVisual()
        {
            if (_beaconOrbRenderer == null || _beaconMat == null) return;
            var mgr = HangarSimulationManager.Instance;
            bool taskDone = false;
            if (mgr != null)
            {
                switch (role)
                {
                    case HangarPropRole.PowerJunctionBox: taskDone = mgr.powerGridPercent >= 75f; break;
                    case HangarPropRole.FuelPipeValve: taskDone = mgr.pipeValvesOpen; break;
                    case HangarPropRole.MainFuelTank: taskDone = mgr.fuelPercent >= 99f; break;
                    case HangarPropRole.CableWinchReel:
                        taskDone = (cableIndex >= 0 && cableIndex < 4)
                            ? (mgr.individualCablesReleased != null && mgr.individualCablesReleased[cableIndex])
                            : mgr.clampsReleased;
                        break;
                    case HangarPropRole.ShipDiagnosticHolo: taskDone = mgr.hullScanComplete; break;
                    case HangarPropRole.GasCylinderRack: taskDone = !mgr.IsOverheating; break;
                }
            }

            Color col = taskDone ? new Color(0.2f, 1f, 0.52f) : GetIdleColor();
            float pulse = taskDone ? 0.9f : (1.8f + 1.1f * Mathf.Abs(Mathf.Sin(Time.time * 3.2f)));
            _beaconMat.color = col;
            _beaconMat.SetColor("_EmissionColor", col * pulse);
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
                    bool isCableOpen = (cableIndex >= 0 && cableIndex < 4)
                        ? (mgr != null && mgr.individualCablesReleased != null && mgr.individualCablesReleased[cableIndex])
                        : (mgr != null && mgr.clampsReleased);

                    // Spin the cable reel smoothly when releasing/retracting and idle-pulse when locked
                    float dir = isCableOpen ? -1f : 1f;
                    float rpm = (_actionAnimTimer > 0f) ? 320f : (isCableOpen ? 75f : 12f);
                    _spinAngle += dir * rpm * dt;
                    transform.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, _spinAngle);

                    // Subtle reel recoil/vibration while unwinding cable
                    float vib = (_actionAnimTimer > 0f) ? Mathf.Sin(t * 45f) * 0.025f * (_actionAnimTimer / 1.4f) : 0f;
                    transform.localPosition = _baseLocalPos + new Vector3(0f, vib, 0f);
                    break;
                }

                case HangarPropRole.FuelPipeValve:
                {
                    bool open = mgr != null && mgr.pipeValvesOpen;
                    float targetSpin = open ? 360f : 0f;
                    _valveWheelAngle = Mathf.MoveTowards(_valveWheelAngle, targetSpin, Time.deltaTime * 280f);
                    if (_valveWheelPivot != null)
                        _valveWheelPivot.localRotation = Quaternion.Euler(_valveWheelAngle, 0f, 0f);

                    float targetNeedle = open ? -115f : 55f;
                    _valveNeedleAngle = Mathf.Lerp(_valveNeedleAngle, targetNeedle, Time.deltaTime * 6f);
                    if (_valveNeedlePivot != null)
                        _valveNeedlePivot.localRotation = Quaternion.Euler(_valveNeedleAngle, 0f, 0f);

                    if (_valveStatusMat != null)
                    {
                        Color vc = open ? new Color(0.20f, 1f, 0.52f) : new Color(1f, 0.30f, 0.16f);
                        float pulse = open ? 1.8f : (1.6f + 1.1f * Mathf.Abs(Mathf.Sin(Time.time * 3.5f)));
                        _valveStatusMat.color = vc;
                        _valveStatusMat.SetColor("_EmissionColor", vc * pulse);
                    }
                    if (_valvePipeGlowMat != null)
                    {
                        Color pc = open ? new Color(0.20f, 1f, 0.52f) : new Color(1f, 0.58f, 0.12f);
                        _valvePipeGlowMat.color = pc;
                        _valvePipeGlowMat.SetColor("_EmissionColor", pc * (open ? 2.4f : 1.2f));
                    }
                    break;
                }

                case HangarPropRole.PowerJunctionBox:
                {
                    if (_leverPivot != null)
                    {
                        // Pull DOWN (+42 deg) when ON, UP (-42 deg) when OFF
                        float targetAngle = _isActiveState ? 42f : -42f;
                        _leverAngle = Mathf.Lerp(_leverAngle, targetAngle, Time.deltaTime * 11f);
                        _leverPivot.localRotation = Quaternion.Euler(_leverAngle, 0f, 0f);
                    }
                    if (_leverHandleMat != null)
                    {
                        Color hCol = _isActiveState ? new Color(0.2f, 1f, 0.52f) : new Color(1f, 0.25f, 0.18f);
                        _leverHandleMat.color = hCol;
                        _leverHandleMat.SetColor("_EmissionColor", hCol * (_isActiveState ? 2.2f : 1.6f));
                    }
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
                    bool full = mgr != null && mgr.fuelPercent >= 99f;
                    float fuelRatio = mgr != null ? Mathf.Clamp01(mgr.fuelPercent / 100f) : 0.25f;

                    // Update 8 segmented glass gauge bars (amber -> bright green as fuel rises 25% -> 100%)
                    int litSegs = Mathf.Clamp(Mathf.CeilToInt(fuelRatio * 8f), 1, 8);
                    Color activeSegCol = full ? new Color(0.18f, 1f, 0.45f) : new Color(1f, 0.72f, 0.12f);
                    if (pumping && !full) activeSegCol = Color.Lerp(new Color(1f, 0.72f, 0.12f), new Color(0.18f, 1f, 0.45f), fuelRatio);
                    if (_pumpSegMats != null)
                    {
                        for (int i = 0; i < _pumpSegMats.Length; i++)
                        {
                            if (_pumpSegMats[i] == null) continue;
                            bool on = i < litSegs;
                            Color c = on ? activeSegCol : new Color(0.08f, 0.12f, 0.16f);
                            _pumpSegMats[i].color = c;
                            _pumpSegMats[i].SetColor("_EmissionColor", on ? c * 2.4f : c * 0.2f);
                        }
                    }

                    if (_pumpLevelText != null)
                    {
                        _pumpLevelText.text = $"FUEL LEVEL\n{Mathf.RoundToInt(fuelRatio * 100f)}%";
                        _pumpLevelText.color = full ? new Color(0.20f, 1f, 0.52f) : new Color(1f, 0.78f, 0.16f);
                    }

                    // Animate mechanical 3D lever pulling DOWN (+40 deg -> -40 deg) when pumping / full
                    if (_pumpLeverPivot != null)
                    {
                        float targetLever = (pumping || full) ? 40f : -35f;
                        _pumpLeverAngle = Mathf.Lerp(_pumpLeverAngle, targetLever, Time.deltaTime * 10f);
                        _pumpLeverPivot.localRotation = Quaternion.Euler(0f, 0f, _pumpLeverAngle);
                    }

                    if (_pumpStatusMat != null)
                    {
                        Color pc = full ? new Color(0.20f, 1f, 0.52f) : (pumping ? new Color(0.14f, 0.92f, 1f) : new Color(1f, 0.65f, 0.12f));
                        float pulse = pumping ? (2.2f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 1.4f) : 1.8f;
                        _pumpStatusMat.color = pc;
                        _pumpStatusMat.SetColor("_EmissionColor", pc * pulse);
                    }

                    if (pumping && _burstVfx != null && Random.value < dt * 8f)
                        _burstVfx.Emit(3);
                    if (_indicatorLight != null)
                    {
                        _indicatorLight.color = full ? new Color(0.2f, 1f, 0.5f) : (pumping ? new Color(0.15f, 0.9f, 1f) : GetIdleColor());
                        _indicatorLight.intensity = pumping ? (3.0f + Mathf.Sin(t * 12f) * 0.9f) : 1.3f;
                    }
                    break;
                }

                case HangarPropRole.ShipDiagnosticHolo:
                {
                    bool scanning = mgr != null && mgr.isHullScanning;
                    bool done = mgr != null && mgr.hullScanComplete;

                    if (_scanPressAnim > 0f) _scanPressAnim = Mathf.Max(0f, _scanPressAnim - Time.deltaTime * 3.5f);
                    if (_scanButtonCap != null)
                    {
                        float pressDrop = Mathf.Sin(_scanPressAnim * Mathf.PI) * 0.055f;
                        _scanButtonCap.localPosition = new Vector3(0f, -0.04f, 0.06f - pressDrop);
                    }
                    if (_scanRadarDish != null)
                    {
                        float rSpd = scanning ? 320f : (done ? 60f : 110f);
                        _scanRadarDish.Rotate(Vector3.up, rSpd * Time.deltaTime, Space.Self);
                    }
                    if (_scanButtonMat != null)
                    {
                        Color sc = done ? new Color(0.20f, 1f, 0.52f) : (scanning ? new Color(1f, 0.78f, 0.15f) : new Color(0.14f, 0.90f, 1f));
                        float pulse = scanning ? (2.2f + Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 1.4f) : 2.0f;
                        _scanButtonMat.color = sc;
                        _scanButtonMat.SetColor("_EmissionColor", sc * pulse);
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
                    if (mgr != null) mgr.PurgeCoolant(25f);
                    PlaySound(_clipHiss);
                    int temp = mgr != null ? Mathf.RoundToInt(mgr.coreTemperatureC) : 75;
                    ShowToast($"XẢ NITƠ LÀM MÁT (-25°C) — NHIỆT ĐỘ HIỆN TẠI: {temp}°C", temp >= 75);
                    break;

                case HangarPropRole.CableWinchReel:
                    if (mgr != null)
                    {
                        if (cableIndex >= 0 && cableIndex < 4)
                        {
                            bool opened = mgr.ToggleSingleCable(cableIndex);
                            PlaySound(_clipHiss);
                            int count = mgr.ReleasedCableCount;
                            ShowToast(mgr.clampsReleased
                                ? "ĐÃ MỞ ĐỦ 4/4 DÂY CÁP BÃI ĐÁP — HOÀN THÀNH NHIỆM VỤ!"
                                : $"DÂY CÁP #{cableIndex + 1} {(opened ? "ĐÃ MỞ" : "ĐÃ KHÓA")} ({count}/4)", !mgr.clampsReleased);
                        }
                        else
                        {
                            mgr.ToggleLandingClamps();
                            PlaySound(_clipHiss);
                            bool rel = mgr.clampsReleased;
                            ShowToast(rel
                                ? "ĐÃ MỞ 4/4 DÂY CÁP BÃI ĐÁP!"
                                : "ĐÃ KHÓA CHẶT 4/4 DÂY CÁP GIỮ TÀU", false);
                        }
                    }
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
                    _scanPressAnim = 1f;
                    if (mgr != null) mgr.StartFullShipScan();
                    PlaySound(_clipScan);
                    ShowToast("ĐÃ NHẤN NÚT QUÉT THÂN VỎ — ĐIỀU ĐỘNG DRONE & ROBOT HÀN!", false);
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
                    int tc = mgr != null ? Mathf.RoundToInt(mgr.coreTemperatureC) : 100;
                    return mgr != null && mgr.IsOverheating ? $"XẢ NITƠ -25°C ({tc}°C!)" : $"HẠ NHIỆT THÊM -25°C ({tc}°C)";
                case HangarPropRole.CableWinchReel:
                    if (mgr != null && cableIndex >= 0 && cableIndex < 4)
                    {
                        bool isRel = mgr.individualCablesReleased != null && mgr.individualCablesReleased[cableIndex];
                        return isRel
                            ? $"KHÓA DÂY CÁP #{cableIndex + 1} ({mgr.ReleasedCableCount}/4)"
                            : $"MỞ DÂY CÁP #{cableIndex + 1} ({mgr.ReleasedCableCount}/4)";
                    }
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
                WorldInteractPromptUI.DrawAtWorldPos(GetInteractWorldPos() + Vector3.up * 0.75f, "E", GetDynamicPromptVerb());
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
