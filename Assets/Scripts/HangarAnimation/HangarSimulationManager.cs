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
        [Range(20f, 120f)] public float coreTemperatureC = 42f;
        public bool pipeValvesOpen = false;
        public bool isFuelPumping = false;
        public bool clampsReleased = false;
        public bool hullScanComplete = false;
        public float hullScanProgress = 0f;
        public bool isHullScanning = false;

        [Header("Auto-Wiring")]
        public bool autoBootstrapOnStart = true;

        public bool IsOverheating => coreTemperatureC >= 85f;
        public bool IsLaunchReady => powerGridPercent >= 75f && fuelPercent >= 99f && clampsReleased && hullScanComplete && !IsOverheating;

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
        private readonly List<Light> _hangarLights = new List<Light>();
        private readonly List<float> _hangarLightBaseIntensities = new List<float>();
        private float _clampOpenBlend = 0f;

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
                }
            }

            // 3. Core Temperature simulation (rises on Overdrive/High Power, slowly cools when idle)
            float targetTemp = 38f + (powerGridPercent * 0.22f) + (globalSpeed > 1.3f ? 38f : 0f);
            float tempRate = (targetTemp > coreTemperatureC) ? 2.2f : 1.4f;
            coreTemperatureC = Mathf.MoveTowards(coreTemperatureC, targetTemp, dt * tempRate);

            // 4. Physically animate the 4 Landing Pad Hydraulic Clamp Arms (Locked <-> Released)
            float targetClamp = clampsReleased ? 1f : 0f;
            _clampOpenBlend = Mathf.MoveTowards(_clampOpenBlend, targetClamp, dt * 1.4f);
            float easedClamp = Mathf.SmoothStep(0f, 1f, _clampOpenBlend);
            for (int i = 0; i < _clampArms.Count; i++)
            {
                if (_clampArms[i] == null) continue;
                // Tilt outward 42 degrees when released
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
        }

        // === Public Engineering Actions called by Interactive Props ===

        public void AdjustPowerGrid(float delta)
        {
            powerGridPercent = Mathf.Clamp(powerGridPercent + delta, 20f, 100f);
            if (powerGridPercent >= 90f)
                coreTemperatureC = Mathf.Min(110f, coreTemperatureC + 8f);
        }

        public void PurgeCoolant(float coolDegrees = 32f)
        {
            coreTemperatureC = Mathf.Max(26f, coreTemperatureC - coolDegrees);
        }

        public void SetPipeValvesOpen(bool open)
        {
            pipeValvesOpen = open;
            var lighting = FindFirstObjectByType<HangarLightingAnimator>();
            if (lighting != null)
                lighting.pulseSpeed = open ? 4.4f : 2.2f;
        }

        public bool ToggleFuelPump()
        {
            if (!pipeValvesOpen)
            {
                coreTemperatureC = Mathf.Min(110f, coreTemperatureC + 6f);
                return false; // Blocked! Must open Pipe Valve first
            }
            isFuelPumping = !isFuelPumping;
            return true;
        }

        public void ToggleLandingClamps()
        {
            clampsReleased = !clampsReleased;
            // Sync steam vent burst on clamp release
            Transform hangarRoot = transform.parent != null ? transform.parent : transform;
            Transform vfxGroup = hangarRoot.Find("Detail_VFX_Steam");
            if (vfxGroup != null)
            {
                foreach (var ps in vfxGroup.GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.name.StartsWith("SteamVent_"))
                        ps.Emit(18);
                }
            }
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

            // 1. Remove any legacy standalone BlastDoor_Control_Terminal (Image 1 vertical panels)
            Transform legacyTerm = hangarRoot.Find("BlastDoor_Control_Terminal");
            if (legacyTerm != null)
            {
                if (Application.isPlaying) Destroy(legacyTerm.gameObject);
                else DestroyImmediate(legacyTerm.gameObject);
            }

            // Bind HangarBlastDoorTerminal directly onto the existing 3-monitor Desk + Chair in the Hangar (Image 2!)
            Transform existingDesk = FindDeepByName(hangarRoot, "desk");
            Transform existingChair = FindDeepByName(hangarRoot, "decorative_chair");
            if (existingDesk == null)
            {
                GameObject grandBayObj = GameObject.Find("02_Grand_Hangar_Bay");
                if (grandBayObj != null)
                {
                    existingDesk = FindDeepByName(grandBayObj.transform, "desk");
                    existingChair = FindDeepByName(grandBayObj.transform, "decorative_chair");
                }
            }
            if (existingDesk == null)
            {
                // Fallback: search active scene for the Hangar desk near (15.2, 2.25, -21)
                foreach (var tr in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if (tr.name == "desk" && tr.position.y < 8f) existingDesk = tr;
                    if (tr.name == "decorative_chair" && tr.position.y < 8f) existingChair = tr;
                }
            }

            if (existingDesk != null)
            {
                // Remove any generic EquipmentUnit prop component from the desk/chair so they act as the Command Station
                var oldProp = existingDesk.GetComponent<HangarInteractiveProp>();
                if (oldProp != null) Destroy(oldProp);

                HangarBlastDoorTerminal terminal = existingDesk.GetComponent<HangarBlastDoorTerminal>();
                if (terminal == null) terminal = existingDesk.gameObject.AddComponent<HangarBlastDoorTerminal>();
                terminal.existingChair = existingChair;
                terminal.EnsureVisualRig();

                BlastDoorController blastDoor = FindFirstObjectByType<BlastDoorController>();
                if (blastDoor != null)
                {
                    terminal.blastDoor = blastDoor;
                    blastDoor.allowDebugHotkeyO = false;
                }
            }

            // 2. Wire EVERY prop inside Room01_Spaceship_Hangar so zero useless decorations remain
            WirePropsUnder(hangarRoot.Find("Detail_ClutterProps"));
            WirePropsUnder(hangarRoot.Find("Props_Maintenance"));
            WirePropsUnder(hangarRoot.Find("Detail_OverheadCrane"));
            WirePropsUnder(hangarRoot.Find("Detail_CablesAndPipes"));

            GameObject grandBay = GameObject.Find("02_Grand_Hangar_Bay");
            if (grandBay != null)
            {
                foreach (Transform child in grandBay.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.StartsWith("Fuel_Tank_"))
                    {
                        AssignProp(child.gameObject, HangarPropRole.MainFuelTank,
                            child.name.Contains("Port") ? "Bồn Nhiên Liệu Mạn Trái" : "Bồn Nhiên Liệu Mạn Phải", 4.2f);
                    }
                }
            }

            // 3. Wire overhead cable & pendant lamp airflow sway
            WireCableAndLampSway(hangarRoot);
        }

        private void WirePropsUnder(Transform group)
        {
            if (group == null) return;

            for (int i = 0; i < group.childCount; i++)
            {
                Transform child = group.GetChild(i);
                string n = child.name;
                string nl = n.ToLowerInvariant();

                if (n.StartsWith("JunctionBox_"))
                    AssignProp(child.gameObject, HangarPropRole.PowerJunctionBox, $"Cầu Dao Điện #{ExtractSuffix(n)}", 3.2f);
                else if (n.StartsWith("GasCylinderRack_"))
                    AssignProp(child.gameObject, HangarPropRole.GasCylinderRack, $"Bình Nitơ Làm Mát #{ExtractSuffix(n)}", 3.2f);
                else if (n.StartsWith("CableReel_"))
                    AssignProp(child.gameObject, HangarPropRole.CableWinchReel, $"Tời Kẹp Bãi Đáp #{ExtractSuffix(n)}", 3.2f);
                else if (n.StartsWith("PipeValve_"))
                    AssignProp(child.gameObject, HangarPropRole.FuelPipeValve, "Van Đường Ống Nhiên Liệu", 3.5f);
                else if (n.StartsWith("Hologram_Screen_"))
                    AssignProp(child.gameObject, HangarPropRole.ShipDiagnosticHolo, $"Trạm Quét Tàu #{ExtractSuffix(n)}", 3.4f);
                else if (nl.Contains("crate") || nl.Contains("box") || nl.Contains("container") || nl.Contains("barrel"))
                    AssignProp(child.gameObject, HangarPropRole.SupplyCrate, "Kho Linh Kiện Bảo Trì", 3.0f);
                else if (nl.Contains("console") || nl.Contains("terminal") || nl.Contains("bench") || nl.Contains("table") || nl.Contains("locker") || nl.Contains("rack") || nl.Contains("cart") || nl.Contains("forklift"))
                    AssignProp(child.gameObject, HangarPropRole.EquipmentUnit, "Thiết Bị Bảo Trì", 3.2f);
            }
        }

        private static void AssignProp(GameObject go, HangarPropRole role, string title, float radius)
        {
            HangarInteractiveProp prop = go.GetComponent<HangarInteractiveProp>();
            if (prop == null) prop = go.AddComponent<HangarInteractiveProp>();
            prop.role = role;
            prop.propTitle = title;
            prop.interactRadius = radius;
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
