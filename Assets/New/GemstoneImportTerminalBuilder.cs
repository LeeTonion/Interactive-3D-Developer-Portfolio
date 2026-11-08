using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceCyberpunk
{
    /// <summary>
    /// Builder dedicated to creating a Rich, Highly-Detailed Sci-Fi Heavy Industrial
    /// Gemstone Import & Energy Refinery Terminal expanding AboutIsland.
    /// Features:
    /// - Heavy unloading gantry crane arch with overhead mechanical rails & extraction arm.
    /// - High-tech Multi-stage Energy Refinery Facility (Generators, Battery banks, Plasma converters, Storage silos, Chimney exhaust).
    /// - Dedicated pipeline routed logically into the Main Island Industrial Area / Central Plasma Shield Core.
    /// - Clean-cut VTOL pad with industrial guidance lights and safety barriers.
    /// </summary>
    public static class GemstoneImportTerminalBuilder
    {
        public static void BuildImportTerminal()
        {
            var mainIsland = GameObject.Find("AboutIsland");
            if (mainIsland == null)
            {
                Debug.LogWarning("AboutIsland not found in scene!");
                return;
            }

            Transform existing = mainIsland.transform.Find("Gemstone_Import_Terminal");
            if (existing != null)
            {
                if (Application.isPlaying) Object.Destroy(existing.gameObject);
                else Object.DestroyImmediate(existing.gameObject);
            }

            // Materials
            Material rockDarkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Asteroid_RockDark.mat");
            Material rockSlateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Asteroid_Slate.mat");
            Material cyanCrystalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_BlueCyan.mat");
            Material purpleCrystalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_PurpleRare.mat");
            Material coreGlowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_CoreReactor.mat");
            Material darkTitaniumMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_DarkTitanium.mat");
            Material steelAccentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_SteelAccent.mat");

            // Models
            GameObject rockLargeA = LoadKenneyModel("rock_largeA.fbx");
            GameObject rockLargeB = LoadKenneyModel("rock_largeB.fbx");
            GameObject meteorHalf = LoadKenneyModel("meteor_half.fbx");
            GameObject crystalLargeA = LoadKenneyModel("rock_crystalsLargeA.fbx");
            GameObject crystalLargeB = LoadKenneyModel("rock_crystalsLargeB.fbx");
            GameObject crystalSmall = LoadKenneyModel("rock_crystals.fbx");
            GameObject pipeStraight = LoadKenneyModel("pipe_straight.fbx");
            GameObject pipeCorner = LoadKenneyModel("pipe_corner.fbx");
            GameObject pipeRing = LoadKenneyModel("pipe_ring.fbx");
            GameObject pipeSupportHigh = LoadKenneyModel("pipe_supportHigh.fbx");
            GameObject supportsHigh = LoadKenneyModel("supports_high.fbx");
            GameObject machineBarrelLarge = LoadKenneyModel("machine_barrelLarge.fbx");
            GameObject machineGeneratorLarge = LoadKenneyModel("machine_generatorLarge.fbx");
            GameObject machineGeneratorSmall = LoadKenneyModel("machine_generator.fbx");
            GameObject chimneyDetailed = LoadKenneyModel("chimney_detailed.fbx");
            GameObject barrelsRail = LoadKenneyModel("barrels_rail.fbx");
            GameObject structureDetailed = LoadKenneyModel("structure_detailed.fbx");
            GameObject satelliteDish = LoadKenneyModel("satelliteDish.fbx");

            GameObject craftCargoA = LoadKenneyModel("craft_cargoA.fbx");
            GameObject craftMiner = LoadKenneyModel("craft_miner.fbx");

            // Sci-Fi Prefabs
            GameObject scifiFloor1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Floors/floor_1.prefab");
            GameObject shieldGenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/shield_generator.prefab");
            GameObject batteryBigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_big.prefab");
            GameObject capacitorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Capacitor.prefab");
            GameObject containerBigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/container_big.prefab");
            GameObject columnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab");

            // ─────────────────────────────────────────────────────────────────
            // TERMINAL ROOT: Local (22, 0, -21) -> World (22, 0, -6)
            // ─────────────────────────────────────────────────────────────────
            GameObject terminalRoot = new GameObject("Gemstone_Import_Terminal");
            terminalRoot.transform.SetParent(mainIsland.transform, false);
            terminalRoot.transform.localPosition = new Vector3(22.0f, 0.0f, -21.0f);

            // ─────────────────────────────────────────────────────────────────
            // 1. EXPANSIVE ROCK FOUNDATION
            // ─────────────────────────────────────────────────────────────────
            GameObject foundationGo = new GameObject("Foundation_Rock_Shelf");
            foundationGo.transform.SetParent(terminalRoot.transform, false);

            if (rockLargeA != null)
            {
                var mainTerrace = Object.Instantiate(rockLargeA, foundationGo.transform, false);
                mainTerrace.name = "Terminal_Terrace_Rock";
                mainTerrace.transform.localPosition = new Vector3(0f, -5.2f, 0f);
                mainTerrace.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
                mainTerrace.transform.localScale = new Vector3(36f, 10.5f, 32f);
                ApplyRockMaterials(mainTerrace, rockDarkMat, rockSlateMat);

                if (meteorHalf != null)
                {
                    var under = Object.Instantiate(meteorHalf, foundationGo.transform, false);
                    under.name = "Terminal_Underbelly_Mass";
                    under.transform.localPosition = new Vector3(0f, -4.5f, 0f);
                    under.transform.localRotation = Quaternion.Euler(0f, 205f, 180f);
                    under.transform.localScale = new Vector3(32f, 40f, 28f);
                    ApplyRockMaterials(under, rockDarkMat, rockSlateMat);
                }

                var connector = Object.Instantiate(rockLargeB, foundationGo.transform, false);
                connector.name = "Industrial_Ledge_Connector_Rock";
                connector.transform.localPosition = new Vector3(-11.5f, -5.0f, 6.5f);
                connector.transform.localRotation = Quaternion.Euler(0f, 95f, 0f);
                connector.transform.localScale = new Vector3(24f, 10.0f, 22f);
                ApplyRockMaterials(connector, rockDarkMat, rockSlateMat);
            }

            // ─────────────────────────────────────────────────────────────────
            // 2. VTOL LANDING PAD & HEAVY OVERHEAD UNLOADING GANTRY
            // ─────────────────────────────────────────────────────────────────
            GameObject landingDeck = new GameObject("VTOL_Shuttle_Landing_Deck");
            landingDeck.transform.SetParent(terminalRoot.transform, false);
            landingDeck.transform.localPosition = new Vector3(4.5f, 0.1f, 0f);

            // Modular Floor (3x3 tiles)
            GameObject floorGroup = new GameObject("Modular_Floor_Tiles");
            floorGroup.transform.SetParent(landingDeck.transform, false);
            if (scifiFloor1 != null)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var fl = Object.Instantiate(scifiFloor1, floorGroup.transform, false);
                        fl.name = $"Pad_Tile_{x}_{z}";
                        fl.transform.localPosition = new Vector3(x * 4.0f, 0f, z * 4.0f);
                        fl.transform.localScale = new Vector3(0.98f, 1f, 0.98f);
                    }
                }
            }

            // 4 Corner VTOL Beacons
            for (int i = 0; i < 4; i++)
            {
                float px = (i < 2) ? -5.2f : 5.2f;
                float pz = (i % 2 == 0) ? -5.2f : 5.2f;

                var beaconGo = new GameObject($"VTOL_Touchdown_Beacon_{i + 1}");
                beaconGo.transform.SetParent(landingDeck.transform, false);
                beaconGo.transform.localPosition = new Vector3(px, 0.35f, pz);

                var bl = beaconGo.AddComponent<Light>();
                bl.type = LightType.Point;
                bl.range = 10f;
                bl.intensity = 2.8f;
                bl.color = new Color(0f, 0.9f, 1f);

                if (columnPrefab != null)
                {
                    var col = Object.Instantiate(columnPrefab, beaconGo.transform, false);
                    col.transform.localPosition = Vector3.down * 0.3f;
                    col.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
                }
            }

            // Heavy Industrial Gantry Crane Arch spanning the landing pad
            GameObject gantryArch = new GameObject("Gantry_Unloading_Arch");
            gantryArch.transform.SetParent(landingDeck.transform, false);
            gantryArch.transform.localPosition = new Vector3(0f, 0f, 0f);

            if (supportsHigh != null)
            {
                // Left Column Tower
                var colL = Object.Instantiate(supportsHigh, gantryArch.transform, false);
                colL.name = "Gantry_Pylon_Left";
                colL.transform.localPosition = new Vector3(-5.4f, 0.2f, 0f);
                colL.transform.localScale = new Vector3(2.4f, 5.5f, 2.4f);

                // Right Column Tower
                var colR = Object.Instantiate(supportsHigh, gantryArch.transform, false);
                colR.name = "Gantry_Pylon_Right";
                colR.transform.localPosition = new Vector3(5.4f, 0.2f, 0f);
                colR.transform.localScale = new Vector3(2.4f, 5.5f, 2.4f);
            }

            // Overhead Dual Heavy Industrial Conduit Gantry Truss (Replacing primitive cube with high-tech pipes)
            GameObject conduitTruss = new GameObject("Overhead_Crane_Conduit_Truss");
            conduitTruss.transform.SetParent(gantryArch.transform, false);
            conduitTruss.transform.localPosition = new Vector3(0f, 5.4f, 0f);

            // 1. Primary Top Conduit Pipe spanning left to right (along X)
            if (pipeStraight != null)
            {
                var topPipe = Object.Instantiate(pipeStraight, conduitTruss.transform, false);
                topPipe.name = "Primary_Energy_Conduit_Pipe";
                topPipe.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                topPipe.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                topPipe.transform.localScale = new Vector3(1.8f, 1.8f, 11.2f);

                // 2. Secondary Bottom Mechanical Rail Conduit
                var botPipe = Object.Instantiate(pipeStraight, conduitTruss.transform, false);
                botPipe.name = "Secondary_Support_Conduit_Pipe";
                botPipe.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                botPipe.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                botPipe.transform.localScale = new Vector3(1.4f, 1.4f, 11.2f);
            }

            // 3. Reinforcement Flange Rings along the overhead gantry pipes
            if (pipeRing != null)
            {
                float[] ringX = new float[] { -4.2f, -2.1f, 0f, 2.1f, 4.2f };
                for (int r = 0; r < ringX.Length; r++)
                {
                    var ring = Object.Instantiate(pipeRing, conduitTruss.transform, false);
                    ring.name = $"Conduit_Flange_Ring_{r + 1}";
                    ring.transform.localPosition = new Vector3(ringX[r], 0.25f, 0f);
                    ring.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    ring.transform.localScale = new Vector3(1.9f, 1.9f, 1.9f);
                }
            }

            // 4. Downpipe Elbows on Gantry Pylons (Routing energy down into the Refinery pipeline)
            if (pipeCorner != null)
            {
                var elbowL = Object.Instantiate(pipeCorner, gantryArch.transform, false);
                elbowL.name = "Gantry_Elbow_To_Refinery";
                elbowL.transform.localPosition = new Vector3(-5.4f, 5.4f, 0f);
                elbowL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                elbowL.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

                var elbowR = Object.Instantiate(pipeCorner, gantryArch.transform, false);
                elbowR.name = "Gantry_Elbow_Right_Cap";
                elbowR.transform.localPosition = new Vector3(5.4f, 5.4f, 0f);
                elbowR.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);
                elbowR.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            }

            // 5. Vertical Downward Conduit running down Left Pylon into Refinery Ground Pipe
            if (pipeStraight != null)
            {
                var verticalDownpipe = Object.Instantiate(pipeStraight, gantryArch.transform, false);
                verticalDownpipe.name = "Pylon_Vertical_Energy_Downpipe";
                verticalDownpipe.transform.localPosition = new Vector3(-5.4f, 2.8f, 0f);
                verticalDownpipe.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                verticalDownpipe.transform.localScale = new Vector3(1.8f, 1.8f, 5.0f);

                // Horizontal ground pipe feeding directly into the refinery complex
                var groundFeedPipe = Object.Instantiate(pipeStraight, gantryArch.transform, false);
                groundFeedPipe.name = "Gantry_To_Refinery_Ground_Feed";
                groundFeedPipe.transform.localPosition = new Vector3(-7.5f, 0.4f, 0f);
                groundFeedPipe.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                groundFeedPipe.transform.localScale = new Vector3(1.8f, 1.8f, 4.5f);
            }

            // Overhead Unloading Laser Carriage & Telescopic Suction Pipe Assembly
            var carriageGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            carriageGo.name = "Suction_Carriage_Collar";
            carriageGo.transform.SetParent(gantryArch.transform, false);
            carriageGo.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            carriageGo.transform.localScale = new Vector3(1.8f, 0.4f, 1.8f);
            Object.DestroyImmediate(carriageGo.GetComponent<Collider>());
            if (darkTitaniumMat != null) carriageGo.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

            // Telescoping Pipe (Telescopic Energy Suction Column)
            var pipeColumnGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipeColumnGo.name = "Telescopic_Suction_Pipe";
            pipeColumnGo.transform.SetParent(carriageGo.transform, false);
            pipeColumnGo.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            pipeColumnGo.transform.localScale = new Vector3(0.75f, 1.8f, 0.75f);
            Object.DestroyImmediate(pipeColumnGo.GetComponent<Collider>());
            if (steelAccentMat != null) pipeColumnGo.GetComponent<Renderer>().sharedMaterial = steelAccentMat;

            // Magnetic Suction Coupling Bell (Vành phễu hút năng lượng phát sáng)
            var suctionBellGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            suctionBellGo.name = "Magnetic_Suction_Bell";
            suctionBellGo.transform.SetParent(pipeColumnGo.transform, false);
            suctionBellGo.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            suctionBellGo.transform.localScale = new Vector3(1.6f, 0.22f, 1.6f);
            Object.DestroyImmediate(suctionBellGo.GetComponent<Collider>());
            if (coreGlowMat != null) suctionBellGo.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

            // High-intensity Suction Plasma Light
            var suctionLightGo = new GameObject("Suction_Plasma_Light");
            suctionLightGo.transform.SetParent(suctionBellGo.transform, false);
            suctionLightGo.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            var sLight = suctionLightGo.AddComponent<Light>();
            sLight.type = LightType.Point;
            sLight.range = 10f;
            sLight.intensity = 4.5f;
            sLight.color = new Color(0f, 0.95f, 1f);
            sLight.enabled = false;

            // Suction Beam LineRenderer
            var beamGo = new GameObject("Energy_Suction_Beam");
            beamGo.transform.SetParent(suctionBellGo.transform, false);
            var lrBeam = beamGo.AddComponent<LineRenderer>();
            lrBeam.useWorldSpace = true;
            lrBeam.startWidth = 0.25f;
            lrBeam.endWidth = 0.12f;
            lrBeam.sharedMaterial = coreGlowMat;
            lrBeam.enabled = false;

            // Attach Timed Gantry Crane Pulley Animation Controller
            var pulleyAnim = gantryArch.AddComponent<GantryCranePulleyAnimation>();
            pulleyAnim.trolleyCarriage = carriageGo.transform;
            pulleyAnim.suctionNozzle = pipeColumnGo.transform;
            pulleyAnim.suctionHeadBell = suctionBellGo.transform;
            pulleyAnim.suctionPlasmaLight = sLight;
            pulleyAnim.extractionBeamLine = lrBeam;
            pulleyAnim.retractedLocalY = -0.5f;
            pulleyAnim.extendedDockedLocalY = -2.6f;

            // ─────────────────────────────────────────────────────────────────
            // 3. MULTI-STAGE REFINERY COMPLEX (Dày đặc máy móc công nghiệp)
            // ─────────────────────────────────────────────────────────────────
            GameObject refineryComplex = new GameObject("Crystal_Energy_Refinery_Complex");
            refineryComplex.transform.SetParent(terminalRoot.transform, false);
            refineryComplex.transform.localPosition = new Vector3(-8.5f, 0.1f, 0f);

            // 3.1 Quantum Plasma Shield Generator (Lò biến đổi năng lượng chính)
            if (shieldGenPrefab != null)
            {
                var shieldGen = Object.Instantiate(shieldGenPrefab, refineryComplex.transform, false);
                shieldGen.name = "Quantum_Plasma_Converter_Reactor";
                shieldGen.transform.localPosition = new Vector3(0f, 0f, 0f);
                shieldGen.transform.localScale = Vector3.one * 1.4f;

                // ─────────────────────────────────────────────────────────────
                // LÕI NĂNG LƯỢNG RỰC SÁNG BÊN TRONG LÒ PHẢN ỨNG (Ảnh 2)
                // ─────────────────────────────────────────────────────────────
                var coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coreSphere.name = "Plasma_Reactor_Energy_Core";
                coreSphere.transform.SetParent(shieldGen.transform, false);
                coreSphere.transform.localPosition = new Vector3(0f, 1.95f, 0f); // Center of reactor chamber
                coreSphere.transform.localScale = Vector3.one * 1.45f;
                Object.DestroyImmediate(coreSphere.GetComponent<Collider>());
                if (coreGlowMat != null) coreSphere.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

                var coreLightGo = new GameObject("Plasma_Core_PointLight");
                coreLightGo.transform.SetParent(coreSphere.transform, false);
                var cLight = coreLightGo.AddComponent<Light>();
                cLight.type = LightType.Point;
                cLight.range = 16f;
                cLight.intensity = 4.2f;
                cLight.color = new Color(0f, 0.95f, 1f);

                // 3 Gyroscope Rings rotating around the core
                var outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                outerRing.name = "Core_Gimbal_Ring_Outer";
                outerRing.transform.SetParent(coreSphere.transform, false);
                outerRing.transform.localScale = new Vector3(1.85f, 0.04f, 1.85f);
                Object.DestroyImmediate(outerRing.GetComponent<Collider>());
                if (steelAccentMat != null) outerRing.GetComponent<Renderer>().sharedMaterial = steelAccentMat;

                var middleRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                middleRing.name = "Core_Gimbal_Ring_Middle";
                middleRing.transform.SetParent(coreSphere.transform, false);
                middleRing.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                middleRing.transform.localScale = new Vector3(1.6f, 0.04f, 1.6f);
                Object.DestroyImmediate(middleRing.GetComponent<Collider>());
                if (darkTitaniumMat != null) middleRing.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

                var innerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                innerRing.name = "Core_Gimbal_Ring_Inner";
                innerRing.transform.SetParent(coreSphere.transform, false);
                innerRing.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
                innerRing.transform.localScale = new Vector3(1.35f, 0.04f, 1.35f);
                Object.DestroyImmediate(innerRing.GetComponent<Collider>());
                if (coreGlowMat != null) innerRing.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

                // Add Quantum Reactor Core Controller Animation
                var coreCtrl = shieldGen.AddComponent<QuantumReactorPlasmaCore>();
                coreCtrl.plasmaCoreSphere = coreSphere.transform;
                coreCtrl.coreLight = cLight;
                coreCtrl.outerRing = outerRing.transform;
                coreCtrl.middleRing = middleRing.transform;
                coreCtrl.innerRing = innerRing.transform;
                coreCtrl.outerSpeed = 30f;
                coreCtrl.middleSpeed = -45f;
                coreCtrl.innerSpeed = 60f;
            }

            // 3.2 Heavy Industrial Turbine Generator
            if (machineGeneratorLarge != null)
            {
                var turbine = Object.Instantiate(machineGeneratorLarge, refineryComplex.transform, false);
                turbine.name = "Industrial_Turbine_Gen";
                turbine.transform.localPosition = new Vector3(-3.8f, 0f, -4.5f);
                turbine.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                turbine.transform.localScale = Vector3.one * 2.2f;
                ApplyMetalMaterials(turbine, darkTitaniumMat, steelAccentMat);
            }

            // 3.3 Twin Industrial Exhaust Chimneys (Ống khói xả nhiệt công nghiệp)
            if (chimneyDetailed != null)
            {
                var ch1 = Object.Instantiate(chimneyDetailed, refineryComplex.transform, false);
                ch1.name = "Thermal_Exhaust_Chimney_1";
                ch1.transform.localPosition = new Vector3(-5.2f, 0f, 3.2f);
                ch1.transform.localScale = Vector3.one * 2.2f;

                var ch2 = Object.Instantiate(chimneyDetailed, refineryComplex.transform, false);
                ch2.name = "Thermal_Exhaust_Chimney_2";
                ch2.transform.localPosition = new Vector3(-5.2f, 0f, 5.2f);
                ch2.transform.localScale = Vector3.one * 2.2f;
            }

            // 3.4 High-Capacity Battery Bank & Pulse Capacitor
            if (batteryBigPrefab != null)
            {
                var bat = Object.Instantiate(batteryBigPrefab, refineryComplex.transform, false);
                bat.name = "Refinery_Battery_Vault";
                bat.transform.localPosition = new Vector3(1.5f, 0f, 5.5f);
                bat.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bat.transform.localScale = Vector3.one * 1.25f;
            }

            if (capacitorPrefab != null)
            {
                var cap = Object.Instantiate(capacitorPrefab, refineryComplex.transform, false);
                cap.name = "Flux_Capacitor_Bank";
                cap.transform.localPosition = new Vector3(-1.8f, 0f, -5.2f);
                cap.transform.localScale = Vector3.one * 1.35f;
            }

            // 3.5 Heavy Mineral Cargo Vault
            if (containerBigPrefab != null)
            {
                var cont = Object.Instantiate(containerBigPrefab, refineryComplex.transform, false);
                cont.name = "Secure_Mineral_Vault";
                cont.transform.localPosition = new Vector3(2.4f, 0f, -5.0f);
                cont.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
                cont.transform.localScale = Vector3.one * 1.2f;
            }

            // 3.6 Crystal Storage Pods (Hàng dãy thùng nén quặng)
            if (machineBarrelLarge != null)
            {
                for (int b = 0; b < 3; b++)
                {
                    var pod = Object.Instantiate(machineBarrelLarge, refineryComplex.transform, false);
                    pod.name = $"Refined_Energy_Pod_{b + 1}";
                    pod.transform.localPosition = new Vector3(4.8f, 0.2f, 4.0f + b * 1.4f);
                    pod.transform.localScale = Vector3.one * 1.6f;
                }
            }

            // 3.7 Raw Crystal Receiving Hopper (Bồn chứa quặng thô phát quang rực rỡ)
            if (crystalLargeA != null)
            {
                var hopper = Object.Instantiate(crystalLargeA, refineryComplex.transform, false);
                hopper.name = "Raw_Energy_Crystal_Hopper";
                hopper.transform.localPosition = new Vector3(4.2f, 0f, -2.5f);
                hopper.transform.localRotation = Quaternion.Euler(10f, 55f, -8f);
                hopper.transform.localScale = Vector3.one * 4.0f;
                ApplyGemstoneMaterials(hopper, rockDarkMat, rockSlateMat, cyanCrystalMat);

                var hlGo = new GameObject("Hopper_Glow_Light");
                hlGo.transform.SetParent(hopper.transform, false);
                hlGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                var hl = hlGo.AddComponent<Light>();
                hl.type = LightType.Point;
                hl.range = 14f;
                hl.intensity = 3.2f;
                hl.color = new Color(0f, 0.9f, 1f);
            }

            // 3.8 Telemetry Dish Antenna
            if (satelliteDish != null)
            {
                var dish = Object.Instantiate(satelliteDish, refineryComplex.transform, false);
                dish.name = "Terminal_Telemetry_Antenna";
                dish.transform.localPosition = new Vector3(-4.5f, 2.8f, 0f);
                dish.transform.localRotation = Quaternion.Euler(15f, 45f, 0f);
                dish.transform.localScale = Vector3.one * 1.8f;
            }

            // ─────────────────────────────────────────────────────────────────
            // 4. CONTINUOUS MULTI-SEGMENT PIPELINE (Dẫn thẳng vào Central Shield Core)
            // ─────────────────────────────────────────────────────────────────
            // Từ Lò tinh luyện trạm (X=13.5, Y=0.5, Z=-6) -> Nối vào Trạm năng lượng đảo chính (X=14, Y=0.5, Z=6.5)
            GameObject pipelineGroup = new GameObject("Energy_Grid_Interconnect_Pipeline");
            pipelineGroup.transform.SetParent(terminalRoot.transform, false);

            Vector3[] pipeSegments = new Vector3[]
            {
                new Vector3(-8.5f, 0.4f, 2.5f),   // Segment 1 (From refinery at world Z=-3.5)
                new Vector3(-8.5f, 0.4f, 8.5f),   // Segment 2 (Connecting to South Power Substation at world Z=2.5)
                new Vector3(-8.5f, 0.4f, 14.5f)   // Segment 3 (Terminating at Citadel South Wall Manifold at world Z=8.5)
            };

            for (int p = 0; p < pipeSegments.Length; p++)
            {
                if (pipeStraight != null)
                {
                    var seg = Object.Instantiate(pipeStraight, pipelineGroup.transform, false);
                    seg.name = $"MainGrid_Pipe_Seg_{p + 1}";
                    seg.transform.localPosition = pipeSegments[p];
                    seg.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // Running North-South
                    seg.transform.localScale = new Vector3(2.2f, 2.2f, 6.2f);

                    // Add pipe supports at joints
                    if (pipeSupportHigh != null && p % 2 == 0)
                    {
                        var supp = Object.Instantiate(pipeSupportHigh, pipelineGroup.transform, false);
                        supp.name = $"Pipe_Pylon_Support_{p + 1}";
                        supp.transform.localPosition = pipeSegments[p] + Vector3.down * 0.4f;
                        supp.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                    }
                }
            }

            // Cyberpunk Holographic Terminal Billboard
            var signGo = new GameObject("Import_Status_Hologram");
            signGo.transform.SetParent(terminalRoot.transform, false);
            signGo.transform.localPosition = new Vector3(4.5f, 3.2f, -6.8f);
            var tmp = signGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 3.8f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0f, 0.95f, 1f, 1f);
            tmp.text = "GEMSTONE IMPORT REFINERY // TERMINAL 01\n<size=2.2><color=#FFD700>STATUS: ONLINE // PIPELINE FEEDING MAIN CORE</color></size>";

            // ─────────────────────────────────────────────────────────────────
            // 5. AUTONOMOUS CARGO SHUTTLE FLEET WITH VTOL FLIGHT PATH
            // ─────────────────────────────────────────────────────────────────
            Transform trafficGroup = GameObject.Find("Project_Zone/04_Traffic_Spaceships")?.transform ?? terminalRoot.transform;
            Transform existingShuttles = trafficGroup.Find("Gemstone_Cargo_Shuttle_Fleet");
            if (existingShuttles != null)
            {
                if (Application.isPlaying) Object.Destroy(existingShuttles.gameObject);
                else Object.DestroyImmediate(existingShuttles.gameObject);
            }

            GameObject fleetRoot = new GameObject("Gemstone_Cargo_Shuttle_Fleet");
            fleetRoot.transform.SetParent(trafficGroup, true);

            Vector3 asteroidDockPos = new Vector3(110f, 16.5f, -67f);
            Vector3 mainPadPos = landingDeck.transform.position; // World: (26.5, 0.1, -6.0)

            List<Vector3> routeA = new List<Vector3>()
            {
                asteroidDockPos,
                new Vector3(92f, 24f, -50f),
                new Vector3(68f, 26f, -32f),
                new Vector3(45f, 18f, -18f),
                new Vector3(32f, 10f, -10f),
                mainPadPos + Vector3.up * 7.5f, // Overhead hover hold
                mainPadPos                      // VTOL Touchdown pad
            };

            List<Vector3> routeB = new List<Vector3>()
            {
                asteroidDockPos,
                new Vector3(96f, 22f, -44f),
                new Vector3(72f, 25f, -26f),
                new Vector3(48f, 19f, -14f),
                new Vector3(34f, 11f, -8f),
                mainPadPos + Vector3.up * 7.5f,
                mainPadPos
            };

            // Spawn Shuttle 1 (Heavy Freighter)
            SpawnVTOLShuttle("Gemstone_Freighter_01", craftCargoA ?? craftMiner, fleetRoot.transform, routeA, machineBarrelLarge, cyanCrystalMat, rockSlateMat, 15f, false);

            // Spawn Shuttle 2 (Courier)
            SpawnVTOLShuttle("Gemstone_Courier_02", craftMiner ?? craftCargoA, fleetRoot.transform, routeB, machineBarrelLarge, purpleCrystalMat, rockSlateMat, 18f, true);
        }

        private static void SpawnVTOLShuttle(string name, GameObject prefab, Transform parent, List<Vector3> route, GameObject barrelModel, Material loadedMat, Material emptyMat, float speed, bool startAtMainPad)
        {
            if (prefab == null) return;

            var shipGo = Object.Instantiate(prefab, parent, false);
            shipGo.name = name;
            shipGo.transform.localScale = Vector3.one * 1.55f;

            foreach (var c in shipGo.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            GameObject podObj = null;
            Renderer podRend = null;
            if (barrelModel != null)
            {
                podObj = Object.Instantiate(barrelModel, shipGo.transform, false);
                podObj.name = "Gemstone_Payload_Pod";
                podObj.transform.localPosition = new Vector3(0f, 0.4f, -0.2f);
                podObj.transform.localScale = Vector3.one * 0.9f;
                podRend = podObj.GetComponentInChildren<Renderer>();
            }

            var exGo = new GameObject("Engine_Exhaust_Light");
            exGo.transform.SetParent(shipGo.transform, false);
            exGo.transform.localPosition = new Vector3(0f, 0.3f, -1.3f);
            var el = exGo.AddComponent<Light>();
            el.type = LightType.Point;
            el.range = 10f;
            el.intensity = 2.8f;
            el.color = new Color(0f, 0.85f, 1f);

            var bellyGo = new GameObject("VTOL_Belly_Downblast_Light");
            bellyGo.transform.SetParent(shipGo.transform, false);
            bellyGo.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            bellyGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var bLight = bellyGo.AddComponent<Light>();
            bLight.type = LightType.Spot;
            bLight.range = 12f;
            bLight.spotAngle = 65f;
            bLight.intensity = 4.0f;
            bLight.color = new Color(0.1f, 0.95f, 1f);
            bLight.enabled = false;

            var psGo = new GameObject("VTOL_Downblast_VFX");
            psGo.transform.SetParent(shipGo.transform, false);
            psGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            psGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = 0.6f;
            main.startSpeed = 4.5f;
            main.startSize = 0.22f;
            main.startColor = new Color(0.2f, 0.9f, 1f, 0.75f);
            main.maxParticles = 35;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.35f;

            var shuttle = shipGo.AddComponent<GemstoneCargoShuttle>();
            shuttle.routeWaypoints = new List<Vector3>(route);
            shuttle.cruiseSpeed = speed;
            shuttle.cargoPodPayload = podObj != null ? podObj.transform : null;
            shuttle.cargoPodRenderer = podRend;
            shuttle.loadedCrystalMat = loadedMat;
            shuttle.emptyContainerMat = emptyMat;
            shuttle.engineGlowLight = el;
            shuttle.vtolBellyLight = bLight;
            shuttle.vtolDownblastParticles = ps;
            shuttle.padTouchdownOffset = 0.45f;
            shuttle.vtolHoverAltitude = 7.5f;

            if (startAtMainPad)
            {
                shuttle.currentState = GemstoneCargoShuttle.ShuttleState.UnloadingAtMainTerminal;
                shipGo.transform.position = route[route.Count - 1] + Vector3.up * 0.45f;
                shipGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            else
            {
                shuttle.currentState = GemstoneCargoShuttle.ShuttleState.LoadingAtMiningAsteroid;
                shipGo.transform.position = route[0] + Vector3.up * 0.45f;
                shipGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        private static void ApplyRockMaterials(GameObject obj, Material darkMat, Material slateMat)
        {
            if (obj == null || darkMat == null) return;
            var rends = obj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in rends)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = (i % 2 == 0 || slateMat == null) ? darkMat : slateMat;
                }
                r.sharedMaterials = mats;
            }
        }

        private static void ApplyGemstoneMaterials(GameObject obj, Material rockDark, Material rockSlate, Material crystalMat)
        {
            if (obj == null || crystalMat == null) return;
            var rends = obj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in rends)
            {
                var mats = r.sharedMaterials;
                if (mats.Length >= 3)
                {
                    if (rockDark != null) mats[0] = rockDark;
                    if (rockSlate != null) mats[1] = rockSlate;
                    mats[2] = crystalMat;
                }
                else if (mats.Length == 2)
                {
                    if (rockDark != null) mats[0] = rockDark;
                    mats[1] = crystalMat;
                }
                else if (mats.Length == 1)
                {
                    mats[0] = crystalMat;
                }
                r.sharedMaterials = mats;
            }
        }

        private static void ApplyMetalMaterials(GameObject obj, Material primaryMetal, Material accentMetal)
        {
            if (obj == null || primaryMetal == null) return;
            var rends = obj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in rends)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = (i % 2 == 0 || accentMetal == null) ? primaryMetal : accentMetal;
                }
                r.sharedMaterials = mats;
            }
        }

        private static GameObject LoadKenneyModel(string filename)
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
