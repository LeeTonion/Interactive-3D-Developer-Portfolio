using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Central interconnected engineering simulation for Room01_Spaceship_Hangar:
    /// - Power Grid (driven by JunctionBoxes): controls Hangar light intensity & machinery speed.
    /// - Fuel Pipeline (PipeValves -> MainFuelTanks): pumps fuel 0% -> 100% into the hero spaceship.
    /// - Landing Pad Hydraulic Clamps (driven by CableReels): physically opens/locks the 4 pad clamps.
    /// - Thermal System (GasCylinderRacks): heat builds under load/overdrive; purging coolant cools the bay.
    /// - Ship Diagnostic Scan (HologramScreens): summons Maintenance Drones + triggers Robotic Arm weld checks.
    /// </summary>
    public class HangarSimulationManager : MonoBehaviour
    {
        public static HangarSimulationManager Instance { get; private set; }

        [Header("Machinery Tempo")]
        [Range(0f, 3f)]
        public float globalSpeed = 1.0f;
        public bool simulationActive = true;

        [Header("Interconnected Hangar Engineering State")]
        [Range(0f, 100f)] public float powerGridPercent = 50f;
        [Range(0f, 100f)] public float fuelPercent = 25f;
        [Range(20f, 120f)] public float coreTemperatureC = 100f;
        public bool pipeValvesOpen = false;
        public bool isFuelPumping = false;
        public bool clampsReleased = false;
        public bool[] individualCablesReleased = new bool[4];
        public int ReleasedCableCount
        {
            get
            {
                int count = 0;
                if (individualCablesReleased != null)
                {
                    for (int i = 0; i < individualCablesReleased.Length; i++)
                        if (individualCablesReleased[i]) count++;
                }
                return count;
            }
        }
        public bool hullScanComplete = false;
        public float hullScanProgress = 0f;
        public bool isHullScanning = false;

        [Header("Auto-Wiring")]
        [Tooltip("Disabled by default: all Hangar props, rigs, and HUD are pre-placed directly in the scene.")]
        public bool autoBootstrapOnStart = false;

        public bool IsOverheating => coreTemperatureC >= 75f;
        public bool IsLaunchReady => powerGridPercent >= 75f && fuelPercent >= 99f && clampsReleased && hullScanComplete && !IsOverheating;

        public event Action OnEngineeringStateChanged;
        public event Action<string, string> OnSystemLog;

        private bool _wasPowerOk;
        private bool _wasFuelOk;
        private bool _wasOverheating;
        private float _lastBroadcastFuel = -1f;
        private float _lastBroadcastTemp = -1f;
        private float _lastBroadcastScan = -1f;
        private bool _lastBroadcastActive = true;
        private float _lastBroadcastSpeed = 1f;

        public static float EffectiveSpeed
        {
            get
            {
                if (Instance == null) return 1f;
                if (!Instance.simulationActive) return 0f;
                float powerFactor = Mathf.Lerp(0.25f, 1.35f, Instance.powerGridPercent / 100f);
                return Instance.globalSpeed * powerFactor;
            }
        }

        // Cached physical Hangar elements controlled by engineering state
        private readonly List<Transform> _clampArms = new List<Transform>();
        private readonly List<Quaternion> _clampClosedRots = new List<Quaternion>();
        private readonly float[] _clampOpenBlends = new float[4];
        private readonly List<Light> _hangarLights = new List<Light>();
        private readonly List<float> _hangarLightBaseIntensities = new List<float>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void Start()
        {
            if (autoBootstrapOnStart)
            {
                BootstrapInteractiveHangar();
            }
            CachePhysicalHangarSystems();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Live Fuel Pumping (requires open Pipe Valve!)
            if (isFuelPumping)
            {
                if (!pipeValvesOpen)
                {
                    isFuelPumping = false;
                }
                else
                {
                    fuelPercent = Mathf.MoveTowards(fuelPercent, 100f, dt * 16f);
                    coreTemperatureC = Mathf.MoveTowards(coreTemperatureC, 96f, dt * 3.8f);
                    if (fuelPercent >= 100f)
                    {
                        isFuelPumping = false;
                        // Top up the actual PlayerSpaceship shield & boost if present
                        var ship = FindFirstObjectByType<PlayerSpaceshipController>();
                        if (ship != null)
                        {
                            ship.SendMessage("TryManualShipEnergyRecharge", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
            }

            // 2. Live Hull Scan progress (summons drones & robots)
            if (isHullScanning)
            {
                hullScanProgress = Mathf.MoveTowards(hullScanProgress, 100f, dt * 28f);
                if (hullScanProgress >= 100f)
                {
                    isHullScanning = false;
                    hullScanComplete = true;
                    EmitLog("DRONE", "Quét thân vỏ hoàn tất 100%.");
                }
            }

            // 3. Core Temperature simulation: continuously rises toward 100°C so player must keep it < 75°C
            float riseRate = (globalSpeed > 1.3f || isFuelPumping) ? 2.6f : 1.6f;
            coreTemperatureC = Mathf.MoveTowards(coreTemperatureC, 100f, dt * riseRate);

            // 4. Physically animate each of the 4 Landing Pad Hydraulic Clamp / Cable Arms individually
            for (int i = 0; i < _clampArms.Count && i < 4; i++)
            {
                if (_clampArms[i] == null) continue;
                bool isOpen = (individualCablesReleased != null && i < individualCablesReleased.Length && individualCablesReleased[i]);
                _clampOpenBlends[i] = Mathf.MoveTowards(_clampOpenBlends[i], isOpen ? 1f : 0f, dt * 1.6f);
                float easedClamp = Mathf.SmoothStep(0f, 1f, _clampOpenBlends[i]);
                _clampArms[i].localRotation = _clampClosedRots[i] * Quaternion.Euler(-42f * easedClamp, 0f, 0f);
            }

            // 5. Physically modulate Hangar Lighting based on Power Grid & Overheat alarm
            float powerScale = Mathf.Lerp(0.30f, 1.25f, powerGridPercent / 100f);
            float overheatPulse = IsOverheating ? (0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 7f))) : 1f;
            for (int i = 0; i < _hangarLights.Count; i++)
            {
                if (_hangarLights[i] == null) continue;
                _hangarLights[i].intensity = _hangarLightBaseIntensities[i] * powerScale * overheatPulse;
            }

            // 6. Event-driven threshold & progress notifications
            bool powerOk = powerGridPercent >= 75f;
            if (powerOk != _wasPowerOk)
            {
                _wasPowerOk = powerOk;
                EmitLog("SYSTEM", powerOk ? "Điện lưới đạt ngưỡng an toàn (>= 75%)." : "Điện lưới xuống dưới ngưỡng an toàn.");
            }

            bool fuelOk = fuelPercent >= 99f;
            if (fuelOk && !_wasFuelOk)
            {
                _wasFuelOk = true;
                EmitLog("SYSTEM", "Bơm nhiên liệu hoàn tất (100%).");
            }
            else if (!fuelOk)
            {
                _wasFuelOk = false;
            }

            bool hot = IsOverheating;
            if (hot != _wasOverheating)
            {
                _wasOverheating = hot;
                if (hot) EmitLog("WARNING", "Nhiệt độ xưởng vượt ngưỡng an toàn (>= 75°C)!");
                else EmitLog("SYSTEM", "Nhiệt độ xưởng đã hạ xuống mức an toàn (< 75°C).");
            }

            if (Mathf.Abs(fuelPercent - _lastBroadcastFuel) >= 0.5f ||
                Mathf.Abs(coreTemperatureC - _lastBroadcastTemp) >= 0.4f ||
                Mathf.Abs(hullScanProgress - _lastBroadcastScan) >= 1f ||
                simulationActive != _lastBroadcastActive ||
                Mathf.Abs(globalSpeed - _lastBroadcastSpeed) >= 0.01f)
            {
                _lastBroadcastFuel = fuelPercent;
                _lastBroadcastTemp = coreTemperatureC;
                _lastBroadcastScan = hullScanProgress;
                _lastBroadcastActive = simulationActive;
                _lastBroadcastSpeed = globalSpeed;
                OnEngineeringStateChanged?.Invoke();
            }
        }

        public void EmitLog(string tag, string message)
        {
            OnSystemLog?.Invoke(tag, message);
        }

        public void SetSimulationSpeed(float speed, bool active)
        {
            globalSpeed = Mathf.Clamp(speed, 0f, 3f);
            simulationActive = active;
            EmitLog("OPS", !active ? "Đã tạm dừng dây chuyền máy móc (0.0x)." : $"Dây chuyền máy móc chuyển sang {globalSpeed:0.0}x.");
            OnEngineeringStateChanged?.Invoke();
        }

        // === Public Engineering Actions called by Interactive Props ===

        public void AdjustPowerGrid(float delta)
        {
            powerGridPercent = Mathf.Clamp(powerGridPercent + delta, 20f, 100f);
            if (powerGridPercent >= 90f)
                coreTemperatureC = Mathf.Min(110f, coreTemperatureC + 8f);
            EmitLog("SYSTEM", $"Điện lưới Hangar điều chỉnh thành {Mathf.RoundToInt(powerGridPercent)}%.");
            OnEngineeringStateChanged?.Invoke();
        }

        public void PurgeCoolant(float coolDegrees = 25f)
        {
            coreTemperatureC = Mathf.Max(25f, coreTemperatureC - coolDegrees);
            EmitSteamAndCoolantVfx(24);
            EmitLog("SYSTEM", $"Đã xả nitơ làm mát (-{Mathf.RoundToInt(coolDegrees)}°C) -> {Mathf.RoundToInt(coreTemperatureC)}°C.");
            OnEngineeringStateChanged?.Invoke();
        }

        public void SetPipeValvesOpen(bool open)
        {
            pipeValvesOpen = open;
            var lighting = FindFirstObjectByType<HangarLightingAnimator>();
            if (lighting != null)
                lighting.pulseSpeed = open ? 4.4f : 2.2f;
            EmitLog("SYSTEM", open ? "Đã mở van đường ống nhiên liệu." : "Đã khóa van đường ống nhiên liệu.");
            OnEngineeringStateChanged?.Invoke();
        }

        public bool ToggleFuelPump()
        {
            if (!pipeValvesOpen)
            {
                coreTemperatureC = Mathf.Min(110f, coreTemperatureC + 6f);
                EmitLog("WARNING", "Nghẽn áp suất: Cần mở van đường ống trước khi bơm!");
                OnEngineeringStateChanged?.Invoke();
                return false; // Blocked! Must open Pipe Valve first
            }
            isFuelPumping = !isFuelPumping;
            EmitLog("SYSTEM", isFuelPumping ? "Bắt đầu bơm nhiên liệu vào tàu chính." : "Đã tạm dừng bơm nhiên liệu.");
            OnEngineeringStateChanged?.Invoke();
            return true;
        }

        public void ToggleLandingClamps()
        {
            bool target = !clampsReleased;
            if (individualCablesReleased == null || individualCablesReleased.Length != 4)
                individualCablesReleased = new bool[4];
            for (int i = 0; i < 4; i++)
                individualCablesReleased[i] = target;
            clampsReleased = target;
            EmitSteamAndCoolantVfx(18);
            EmitLog("HANGAR", clampsReleased ? "Đã mở toàn bộ 4/4 dây cáp & kẹp bãi đáp." : "Đã khóa lại 4/4 dây cáp & kẹp bãi đáp.");
            OnEngineeringStateChanged?.Invoke();
        }

        public bool ToggleSingleCable(int index)
        {
            if (individualCablesReleased == null || individualCablesReleased.Length != 4)
                individualCablesReleased = new bool[4];
            if (index < 0 || index >= 4) return false;

            individualCablesReleased[index] = !individualCablesReleased[index];
            int count = ReleasedCableCount;
            clampsReleased = (count == 4);

            EmitSteamAndCoolantVfx(10);
            if (clampsReleased)
                EmitLog("HANGAR", "Hoàn thành nhiệm vụ dây cáp: Đã mở đủ 4/4 dây cáp!");
            else
                EmitLog("HANGAR", $"Dây cáp #{index + 1} {(individualCablesReleased[index] ? "đã mở" : "đã khóa")} ({count}/4).");

            OnEngineeringStateChanged?.Invoke();
            return individualCablesReleased[index];
        }

        public void StartFullShipScan()
        {
            isHullScanning = true;
            hullScanProgress = 0f;
            hullScanComplete = false;

            // Command Robotic Arms to weld/inspect immediately
            var arms = FindObjectsByType<RoboticArmController>(FindObjectsSortMode.None);
            foreach (var arm in arms) arm.TriggerWeldNow(3.2f);

            // Summon Maintenance Drones to swoop in and scan the spaceship
            var drones = FindObjectsByType<MaintenanceDronePatrol>(FindObjectsSortMode.None);
            foreach (var drone in drones) drone.TriggerShipScanFocus(5.0f);

            EmitLog("DRONE", "Đã kích hoạt 2 drone & robot kiểm tra thân vỏ tàu.");
            OnEngineeringStateChanged?.Invoke();
        }

        private void EmitSteamAndCoolantVfx(int count)
        {
            Transform hangarRoot = transform.parent != null ? transform.parent : transform;
            Transform vfxGroup = hangarRoot.Find("Detail_VFX_Steam");
            if (vfxGroup != null)
            {
                foreach (var ps in vfxGroup.GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.name.StartsWith("SteamVent_"))
                        ps.Emit(count);
                }
            }
            Transform clutterGroup = hangarRoot.Find("Detail_ClutterProps");
            if (clutterGroup != null)
            {
                foreach (var ps in clutterGroup.GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.name == "Coolant_Purge_VFX")
                        ps.Emit(count);
                }
            }
        }

        private void CachePhysicalHangarSystems()
        {
            _clampArms.Clear();
            _clampClosedRots.Clear();
            _hangarLights.Clear();
            _hangarLightBaseIntensities.Clear();

            Transform hangarRoot = transform.parent != null ? transform.parent : transform;

            Transform pad = hangarRoot.Find("LandingPad_Hero");
            if (pad != null)
            {
                for (int i = 0; i < pad.childCount; i++)
                {
                    Transform c = pad.GetChild(i);
                    if (c.name.StartsWith("Hydraulic_Clamp_Arm_"))
                    {
                        _clampArms.Add(c);
                        _clampClosedRots.Add(c.localRotation);
                    }
                }
            }

            Transform lightingGroup = hangarRoot.Find("Lighting");
            if (lightingGroup != null)
            {
                foreach (Light lt in lightingGroup.GetComponentsInChildren<Light>(true))
                {
                    if (lt.name.StartsWith("Spot_"))
                    {
                        _hangarLights.Add(lt);
                        _hangarLightBaseIntensities.Add(lt.intensity);
                    }
                }
            }
        }

        /// <summary>
        /// Spawns/binds the Blast Door Computer Terminal, wires every decorative Hangar prop
        /// with a real interconnected engineering role + animation, and binds overhead cable/lamp airflow sway.
        /// </summary>
        public void BootstrapInteractiveHangar()
        {
            Transform hangarRoot = transform.parent != null ? transform.parent : transform;

            // 1. Clean up any legacy computer terminal / 3-monitor desk UI (player uses HUD instead)
            Transform legacyTerm = hangarRoot.Find("BlastDoor_Control_Terminal");
            if (legacyTerm != null)
            {
                if (Application.isPlaying) Destroy(legacyTerm.gameObject);
                else DestroyImmediate(legacyTerm.gameObject);
            }

            foreach (var oldTerm in FindObjectsByType<HangarBlastDoorTerminal>(FindObjectsSortMode.None))
            {
                Transform uiRoot = oldTerm.transform.Find("Desk_3Monitor_UI_Root");
                if (uiRoot != null)
                {
                    if (Application.isPlaying) Destroy(uiRoot.gameObject);
                    else DestroyImmediate(uiRoot.gameObject);
                }
                if (Application.isPlaying) Destroy(oldTerm);
                else DestroyImmediate(oldTerm);
            }

            // Ensure Player On-Screen Mission HUD is active on Hangar_SimulationManager
            if (GetComponent<HangarMissionHUD>() == null)
                gameObject.AddComponent<HangarMissionHUD>();

            // 2. Wire ONLY real functional props (reuse existing CableReels 0..3 & FloorUmbilical fuel lines!)
            EnsureFourthCableReel(hangarRoot.Find("Detail_ClutterProps"));
            WirePropsUnder(hangarRoot.Find("Detail_ClutterProps"));
            WirePropsUnder(hangarRoot.Find("Props_Maintenance"));
            WirePropsUnder(hangarRoot.Find("Detail_OverheadCrane"));
            WirePropsUnder(hangarRoot.Find("Detail_CablesAndPipes"));

            // Clean up any temporary floor fuel primitives if present from earlier test
            foreach (string tmpName in new[] { "Fuel_Valve_Floor", "Fuel_Pump_Floor" })
            {
                Transform tmpTr = hangarRoot.Find(tmpName);
                if (tmpTr != null)
                {
                    if (Application.isPlaying) Destroy(tmpTr.gameObject);
                    else DestroyImmediate(tmpTr.gameObject);
                }
            }

            // 3. Wire overhead cable & pendant lamp airflow sway
            WireCableAndLampSway(hangarRoot);
        }

        private static void EnsureFourthCableReel(Transform clutterGroup)
        {
            if (clutterGroup == null || clutterGroup.Find("CableReel_3") != null) return;
            Transform template = clutterGroup.Find("CableReel_0");
            if (template == null) return;

            GameObject reel3 = Instantiate(template.gameObject, clutterGroup);
            reel3.name = "CableReel_3";
            reel3.transform.localPosition = new Vector3(13.80f, 3.05f, -2.00f);
            reel3.transform.localRotation = template.localRotation;
        }

        private void WirePropsUnder(Transform group)
        {
            if (group == null) return;

            for (int i = 0; i < group.childCount; i++)
            {
                Transform child = group.GetChild(i);
                string n = child.name;

                if (n.StartsWith("JunctionBox_"))
                    AssignProp(child.gameObject, HangarPropRole.PowerJunctionBox, $"Cầu Dao Điện #{ExtractSuffix(n)}", 3.2f);
                else if (n.StartsWith("GasCylinderRack_"))
                    AssignProp(child.gameObject, HangarPropRole.GasCylinderRack, $"Bình Nitơ Làm Mát #{ExtractSuffix(n)}", 3.2f);
                else if (n.StartsWith("CableReel_"))
                {
                    int idx = 0;
                    int.TryParse(ExtractSuffix(n), out idx);
                    idx = Mathf.Clamp(idx, 0, 3);
                    var p = AssignProp(child.gameObject, HangarPropRole.CableWinchReel, $"Cuộn Dây Cáp #{idx + 1}", 3.6f);
                    p.cableIndex = idx;
                }
                else if (n.StartsWith("PipeValve_"))
                    AssignProp(child.gameObject, HangarPropRole.FuelPipeValve, "Van Đường Ống Nhiên Liệu", 3.5f);
                else if (n == "console" && group.name == "Props_Maintenance")
                {
                    // Hide unnecessary tilted console pedestals (Image 1)
                    child.gameObject.SetActive(false);
                }
                else if (n == "Hologram_Screen_0")
                {
                    // Mount the physical Ship Scan Button on the aft wall (-3.80, 3.65, -25.35)
                    var meshRend = child.GetComponent<Renderer>();
                    if (meshRend != null) meshRend.enabled = false;
                    var boxCol = child.GetComponent<Collider>();
                    if (boxCol != null) boxCol.enabled = false;
                    var p = AssignProp(child.gameObject, HangarPropRole.ShipDiagnosticHolo, "Nút Bấm Quét Thân Tàu", 3.6f);
                    p.useCustomLocalAnchor = true;
                    p.customLocalAnchor = new Vector3(0f, -0.62f, -0.65f);
                }
                else if (n == "Hologram_Screen_1" || n == "Hologram_Screen_2")
                {
                    // Remove unnecessary extra terminal screens
                    child.gameObject.SetActive(false);
                }
                else if (n == "FloorUmbilical_Port2")
                {
                    var p = AssignProp(child.gameObject, HangarPropRole.FuelPipeValve, "Van Đường Ống (Trên Tường)", 3.8f);
                    // Place valve on the left wall right where FloorUmbilical_Port2 exits the wall
                    p.useCustomLocalAnchor = true;
                    p.customLocalAnchor = new Vector3(-17.15f, 3.65f, -13.90f);
                }
                else if (n == "FloorUmbilical_Stbd2")
                {
                    var p = AssignProp(child.gameObject, HangarPropRole.MainFuelTank, "Cần Gạt Bơm Nhiên Liệu", 3.8f);
                    // Place Fuel Pump station in the open bay right beside the Wall Valve on the left wall
                    p.useCustomLocalAnchor = true;
                    p.customLocalAnchor = new Vector3(-16.95f, 2.46f, -11.20f);
                }
            }
        }

        private static void AttachLineEndAnchor(GameObject go, HangarInteractiveProp prop)
        {
            var lr = go.GetComponent<LineRenderer>();
            if (lr != null && lr.positionCount > 0)
            {
                prop.useCustomLocalAnchor = true;
                prop.customLocalAnchor = lr.GetPosition(lr.positionCount - 1);
            }
        }

        private static HangarInteractiveProp AssignProp(GameObject go, HangarPropRole role, string title, float radius)
        {
            HangarInteractiveProp prop = go.GetComponent<HangarInteractiveProp>();
            if (prop == null) prop = go.AddComponent<HangarInteractiveProp>();
            prop.role = role;
            prop.propTitle = title;
            prop.interactRadius = radius;
            return prop;
        }

        private void WireCableAndLampSway(Transform hangarRoot)
        {
            List<Transform> swayList = new List<Transform>();

            Transform craneGroup = hangarRoot.Find("Detail_OverheadCrane");
            if (craneGroup != null)
            {
                for (int i = 0; i < craneGroup.childCount; i++)
                {
                    Transform c = craneGroup.GetChild(i);
                    if (c.name.StartsWith("Pendant_Hood_"))
                        swayList.Add(c);
                }
            }

            Transform cablesGroup = hangarRoot.Find("Detail_CablesAndPipes");
            if (cablesGroup != null)
            {
                for (int i = 0; i < cablesGroup.childCount; i++)
                {
                    Transform c = cablesGroup.GetChild(i);
                    if (c.name.StartsWith("RoofDrop_") || c.name.StartsWith("CatwalkDrop_") || c.name.StartsWith("BlastDoor_Cable_"))
                        swayList.Add(c);
                }
            }

            if (swayList.Count > 0)
            {
                HangarCableAnimator anim = GetComponent<HangarCableAnimator>();
                if (anim == null) anim = gameObject.AddComponent<HangarCableAnimator>();
                anim.swayingElements = swayList.ToArray();
                anim.swayDegrees = 1.5f;
                anim.swaySpeed = 1.3f;
                anim.CacheBaseRotations();
            }
        }

        private static string ExtractSuffix(string name)
        {
            int idx = name.LastIndexOf('_');
            return (idx >= 0 && idx < name.Length - 1) ? name.Substring(idx + 1) : "01";
        }

        private static Transform FindDeepByName(Transform parent, string targetName)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (string.Equals(c.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                    return c;
                Transform found = FindDeepByName(c, targetName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
