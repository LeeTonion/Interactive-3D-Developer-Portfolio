using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceCyberpunk
{
    /// <summary>
    /// Builder utility dedicated to generating the continuous Orbital Surface Mining Railway System
    /// around the Gemstone Mining Asteroid using existing Kenney monorail track and train prefabs.
    /// </summary>
    public static class MiningRailwayBuilder
    {
        public static void BuildRailwaySystem(GameObject asteroidRoot)
        {
            if (asteroidRoot == null) return;

            Transform existingRail = asteroidRoot.transform.Find("MiningRailwaySystem");
            if (existingRail != null)
            {
                if (Application.isPlaying) Object.Destroy(existingRail.gameObject);
                else Object.DestroyImmediate(existingRail.gameObject);
            }

            GameObject railwayRoot = new GameObject("MiningRailwaySystem");
            railwayRoot.transform.SetParent(asteroidRoot.transform, false);
            railwayRoot.transform.localPosition = Vector3.zero;

            var railwaySys = railwayRoot.AddComponent<MiningRailwaySystem>();

            // Materials
            Material darkTitaniumMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_DarkTitanium.mat");
            Material steelAccentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_SteelAccent.mat");
            Material trackCyanGlowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_BlueCyan.mat");
            Material loadedPurpleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_PurpleRare.mat");
            Material emptyCargoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Asteroid_Slate.mat");

            // Load Existing Kenney Monorail models
            GameObject trackStraight = LoadModel("monorail_trackStraight.fbx");
            GameObject trackSupport = LoadModel("monorail_trackSupport.fbx");
            GameObject trainFront = LoadModel("monorail_trainFront.fbx");
            GameObject trainCargo = LoadModel("monorail_trainCargo.fbx");
            GameObject trainBox = LoadModel("monorail_trainBox.fbx");
            GameObject trainPassenger = LoadModel("monorail_trainPassenger.fbx");
            GameObject trainFlat = LoadModel("monorail_trainFlat.fbx");
            GameObject trainEnd = LoadModel("monorail_trainEnd.fbx");
            GameObject barrelLarge = LoadModel("machine_barrelLarge.fbx");

            // ─────────────────────────────────────────────────────────────────
            // 1. RAILWAY CIRCUIT WAYPOINTS (Following Asteroid Surface & Facilities)
            // ─────────────────────────────────────────────────────────────────
            GameObject circuitGo = new GameObject("RailwayCircuit");
            circuitGo.transform.SetParent(railwayRoot.transform, false);
            var circuit = circuitGo.AddComponent<MiningRailwayCircuit>();
            railwaySys.circuit = circuit;

            // Contour curve skirting safely OUTSIDE all industrial facilities while traversing asteroid terrain
            circuit.waypoints = new List<Vector3>()
            {
                new Vector3(-19.5f, 1.8f, -2.0f),  // WP0: Station 1 - West Extraction Depot (Apron outside generator)
                new Vector3(-18.5f, 2.2f, 6.5f),   // WP1: West Ridge curve
                new Vector3(-11.5f, 3.6f, 13.5f),  // WP2: Ascending North Mountain Ridge
                new Vector3(-2.0f, 4.2f, 16.5f),   // WP3: Station 2 - North Observation Viaduct (Outer North ledge)
                new Vector3(6.5f, 3.5f, 15.0f),    // WP4: North East curve bridge
                new Vector3(14.5f, 2.0f, 7.5f),    // WP5: East Descent slope
                new Vector3(19.5f, 0.4f, -2.0f),   // WP6: Station 3 - East Cargo Terminal (Outer gantry dock track)
                new Vector3(16.5f, -0.2f, -10.5f), // WP7: South East cliff pass
                new Vector3(8.5f, -0.2f, -16.5f),  // WP8: South Viaduct curve
                new Vector3(2.0f, -0.2f, -17.0f),  // WP9: Station 4 - South Substation Deck (Outer southern edge)
                new Vector3(-7.5f, 0.2f, -15.5f),  // WP10: South West elevated bridge
                new Vector3(-15.5f, 1.0f, -9.5f)   // WP11: West Ascent ramp
            };

            circuit.BakeSpline();

            // ─────────────────────────────────────────────────────────────────
            // 2. PHYSICAL RAILWAY TRACKS & STRUCTURAL TRESTLE SUPPORTS
            // ─────────────────────────────────────────────────────────────────
            GameObject trackVisuals = new GameObject("RailwayPhysicalTracks");
            trackVisuals.transform.SetParent(railwayRoot.transform, false);

            float totalCircuitLen = circuit.TotalLength;
            float trackSegmentLen = 1.6f; // spacing between track segments
            int numTrackPieces = Mathf.Max(12, Mathf.RoundToInt(totalCircuitLen / trackSegmentLen));
            float actualStep = totalCircuitLen / numTrackPieces;

            for (int i = 0; i < numTrackPieces; i++)
            {
                float dist = i * actualStep;
                circuit.EvaluateDistance(dist, out Vector3 worldPos, out Quaternion worldRot);

                if (trackStraight != null)
                {
                    var seg = Object.Instantiate(trackStraight, trackVisuals.transform, false);
                    seg.name = $"Track_Seg_{i:D2}";
                    seg.transform.position = worldPos;
                    seg.transform.rotation = worldRot;
                    seg.transform.localScale = new Vector3(1.6f, 1.4f, actualStep * 1.05f);

                    // Track navigation glow light strip
                    if (i % 4 == 0)
                    {
                        var navDot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        navDot.name = "Track_NavLight_Dot";
                        navDot.transform.SetParent(seg.transform, false);
                        navDot.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                        navDot.transform.localScale = new Vector3(0.06f, 0.04f, 0.15f);
                        Object.DestroyImmediate(navDot.GetComponent<Collider>());
                        if (trackCyanGlowMat != null) navDot.GetComponent<Renderer>().sharedMaterial = trackCyanGlowMat;
                    }
                }

                // Periodic vertical trestle supports grounding elevated railway sections
                if (i % 3 == 0 && trackSupport != null)
                {
                    // Raycast or estimate distance to rock beneath
                    Vector3 localP = asteroidRoot.transform.InverseTransformPoint(worldPos);
                    float supportHeight = Mathf.Max(1.2f, localP.y - (-5.0f)); // extend down into massif
                    if (supportHeight > 1.5f)
                    {
                        var supp = Object.Instantiate(trackSupport, trackVisuals.transform, false);
                        supp.name = $"Track_TrestleSupport_{i:D2}";
                        supp.transform.position = worldPos + Vector3.down * (supportHeight * 0.5f);
                        supp.transform.rotation = Quaternion.Euler(0f, worldRot.eulerAngles.y, 0f);
                        supp.transform.localScale = new Vector3(1.4f, supportHeight * 2.0f, 1.4f);
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 3. MINING STATIONS (Loading & Unloading Platforms Along Route)
            // ─────────────────────────────────────────────────────────────────
            GameObject stationsRoot = new GameObject("MiningStations");
            stationsRoot.transform.SetParent(railwayRoot.transform, false);

            (string name, float dist, Vector3 localPos, Color col)[] stationData = new[]
            {
                ("West_Extraction_Depot", 0f, new Vector3(-19.5f, 1.8f, -2.0f), new Color(0f, 0.85f, 1f)),
                ("North_Ridge_Outpost", totalCircuitLen * 0.25f, new Vector3(-2.0f, 4.2f, 16.5f), new Color(0.75f, 0.25f, 1f)),
                ("East_Cargo_Freight_Terminal", totalCircuitLen * 0.52f, new Vector3(19.5f, 0.4f, -2.0f), new Color(0.1f, 0.9f, 1f)),
                ("South_Power_Refueling_Dock", totalCircuitLen * 0.78f, new Vector3(2.0f, -0.2f, -17.0f), new Color(0.95f, 0.6f, 0.1f))
            };

            List<MiningStationDock> stationDocks = new List<MiningStationDock>();

            foreach (var sd in stationData)
            {
                GameObject stGo = new GameObject(sd.name);
                stGo.transform.SetParent(stationsRoot.transform, false);
                stGo.transform.localPosition = sd.localPos;

                var dock = stGo.AddComponent<MiningStationDock>();
                dock.stationName = sd.name;
                dock.circuitDistance = sd.dist;
                dock.dockingDuration = 4.2f;
                dock.loadingAnimationDuration = 2.6f;

                // Indicator beacon light
                var beaconGo = new GameObject("Station_Indicator_Light");
                beaconGo.transform.SetParent(stGo.transform, false);
                beaconGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                var bLight = beaconGo.AddComponent<Light>();
                bLight.type = LightType.Point;
                bLight.range = 10f;
                bLight.intensity = 2.2f;
                bLight.color = sd.col;
                dock.stationIndicatorLight = bLight;

                // Gemstone loading particle emitter
                var psGo = new GameObject("Station_Gemstone_Loading_VFX");
                psGo.transform.SetParent(stGo.transform, false);
                psGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                var ps = psGo.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false;
                main.startLifetime = 1.1f;
                main.startSpeed = 2.2f;
                main.startSize = 0.16f;
                main.startColor = sd.col;
                main.maxParticles = 40;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var em = ps.emission;
                em.rateOverTime = 20f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 25f;
                shape.radius = 0.4f;

                dock.gemstoneLoadingParticles = ps;

                // Small loading gantry mechanical arm
                var armGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                armGo.name = "Gantry_Loader_Arm";
                armGo.transform.SetParent(stGo.transform, false);
                armGo.transform.localPosition = new Vector3(1.2f, 1.6f, 0f);
                armGo.transform.localScale = new Vector3(1.6f, 0.22f, 0.22f);
                Object.DestroyImmediate(armGo.GetComponent<Collider>());
                if (steelAccentMat != null) armGo.GetComponent<Renderer>().sharedMaterial = steelAccentMat;
                dock.loaderArm = armGo.transform;

                stationDocks.Add(dock);
            }

            railwaySys.stations = stationDocks;

            // ─────────────────────────────────────────────────────────────────
            // 4. AUTONOMOUS MINING TRAINS (Reusing Existing Prefabs)
            // ─────────────────────────────────────────────────────────────────
            GameObject trainsRoot = new GameObject("ActiveMiningTrains");
            trainsRoot.transform.SetParent(railwayRoot.transform, false);

            (string name, float startDist, float cruiseSpeed, GameObject[] wagons)[] trainConfigs = new[]
            {
                // Train 1: Heavy Gemstone Cargo Hauler (Front + Cargo + Cargo + End)
                ("Mineral_Ore_Express_01", 0f, 7.2f, new GameObject[] { trainCargo, trainCargo }),
                // Train 2: Tech Supply & Specimen Transport (Front + Box + Passenger + Flat + End)
                ("Colony_Supply_Train_02", totalCircuitLen * 0.50f, 6.8f, new GameObject[] { trainBox, trainPassenger, trainFlat })
            };

            List<OrbitalMiningTrain> createdTrains = new List<OrbitalMiningTrain>();

            for (int t = 0; t < trainConfigs.Length; t++)
            {
                var tc = trainConfigs[t];
                GameObject trainGo = new GameObject(tc.name);
                trainGo.transform.SetParent(trainsRoot.transform, false);

                var trainCtrl = trainGo.AddComponent<OrbitalMiningTrain>();
                trainCtrl.trainID = tc.name;
                trainCtrl.railwayCircuit = circuit;
                trainCtrl.startingOffset = tc.startDist;
                trainCtrl.movementSpeed = tc.cruiseSpeed;
                trainCtrl.acceleration = 2.8f;
                trainCtrl.deceleration = 3.6f;
                trainCtrl.wagonSpacing = 1.65f;
                trainCtrl.stations = stationDocks;
                trainCtrl.emptyCargoMat = emptyCargoMat;
                trainCtrl.loadedGemstoneMat = loadedPurpleMat;

                // Locomotive Head
                if (trainFront != null)
                {
                    var head = Object.Instantiate(trainFront, trainGo.transform, false);
                    head.name = $"{tc.name}_Locomotive";
                    head.transform.localScale = Vector3.one * 1.4f;
                    trainCtrl.locomotiveHead = head.transform;

                    // Headlight
                    var hlGo = new GameObject("Locomotive_Headlight");
                    hlGo.transform.SetParent(head.transform, false);
                    hlGo.transform.localPosition = new Vector3(0f, 0.45f, 0.55f);
                    var hl = hlGo.AddComponent<Light>();
                    hl.type = LightType.Spot;
                    hl.range = 16f;
                    hl.spotAngle = 55f;
                    hl.intensity = 2.8f;
                    hl.color = new Color(0.85f, 0.95f, 1f);
                    trainCtrl.engineHeadlight = hl;
                }

                // Articulated Wagons
                for (int w = 0; w < tc.wagons.Length; w++)
                {
                    GameObject wPrefab = tc.wagons[w] ?? trainCargo;
                    if (wPrefab != null)
                    {
                        var wagon = Object.Instantiate(wPrefab, trainGo.transform, false);
                        wagon.name = $"{tc.name}_Wagon_{w + 1}";
                        wagon.transform.localScale = Vector3.one * 1.4f;
                        trainCtrl.cargoWagons.Add(wagon.transform);

                        // If it's a cargo car, register its container renderer
                        var rend = wagon.GetComponentInChildren<Renderer>();
                        if (rend != null) trainCtrl.cargoContainers.Add(rend);

                        // Attach glowing gemstone chunk inside open wagons
                        if (wPrefab == trainCargo && barrelLarge != null)
                        {
                            var gemChunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            gemChunk.name = "Loaded_Gemstone_Payload";
                            gemChunk.transform.SetParent(wagon.transform, false);
                            gemChunk.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                            gemChunk.transform.localRotation = Quaternion.Euler(15f, 30f, 10f);
                            gemChunk.transform.localScale = new Vector3(0.32f, 0.26f, 0.45f);
                            Object.DestroyImmediate(gemChunk.GetComponent<Collider>());
                            if (loadedPurpleMat != null) gemChunk.GetComponent<Renderer>().sharedMaterial = loadedPurpleMat;
                        }
                    }
                }

                // Caboose Car
                if (trainEnd != null)
                {
                    var caboose = Object.Instantiate(trainEnd, trainGo.transform, false);
                    caboose.name = $"{tc.name}_Caboose";
                    caboose.transform.localScale = Vector3.one * 1.4f;
                    trainCtrl.cabooseCar = caboose.transform;

                    // Tail warning beacon
                    var tailGo = new GameObject("Caboose_TailLight");
                    tailGo.transform.SetParent(caboose.transform, false);
                    tailGo.transform.localPosition = new Vector3(0f, 0.45f, -0.55f);
                    var tl = tailGo.AddComponent<Light>();
                    tl.type = LightType.Point;
                    tl.range = 6f;
                    tl.intensity = 2.0f;
                    tl.color = Color.red;
                    trainCtrl.tailWarningLight = tl;
                }

                createdTrains.Add(trainCtrl);
                trainCtrl.AlignImmediate();
            }

            railwaySys.activeTrains = createdTrains;
        }

        private static GameObject LoadModel(string filename)
        {
#if UNITY_EDITOR
            string directPath = "Assets/_Project/kenney_space-kit/Models/FBX format/" + filename;
            var direct = AssetDatabase.LoadAssetAtPath<GameObject>(directPath);
            if (direct != null) return direct;

            string[] guids = AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(filename));
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (path.EndsWith(filename, System.StringComparison.OrdinalIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
#endif
            return null;
        }
    }
}
