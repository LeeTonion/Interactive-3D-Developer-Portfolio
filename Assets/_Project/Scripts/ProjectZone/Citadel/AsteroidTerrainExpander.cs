using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Multi-island Asteroid Colony restructure for Project_Zone:
    ///   - Main Island (y=0): Citadel (dominant landmark), Central Forecourt Plaza, North Geothermal Terrace, NW Cave
    ///   - Landing & Transport Island (y=0, west): Starport apron, LandingPad (-48, 23), FlightLine gear, Cargo, Billboard
    ///   - Industrial Island (y=-4, south) + Refinery Tier (y=-15, SE): Gemstone Terminal, IndustrialArea, S. Machinery, Outpost_02
    ///   - Research Outpost Island (y=+8, NW): Merged research peak (Island_02/04/05 + Comm Relay + Dish)
    ///   - Aux Operations Island (y=-6, east): East Logistics & Freight yard
    ///   - Connectivity: Exactly 5 purposeful bridges (B1 Grand, B2 Industrial, B3 Aux, B4 Research, B5 Mining + Refinery spur)
    /// Preserves 100% of original assets, components, scripts, and serialized references.
    /// </summary>
    public static class AsteroidTerrainExpander
    {
        private const string RootName = "Asteroid_Planetary_Surface";
        private static readonly Dictionary<string, GameObject> ModelCache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        [MenuItem("Tools/Project Zone/Restructure Multi-Island Asteroid Colony")]
        public static GameObject BuildPlanetarySurface()
        {
            ModelCache.Clear();
            var pz = GameObject.Find("Project_Zone");
            if (pz == null)
            {
                Debug.LogError("[AsteroidTerrainExpander] Project_Zone not found.");
                return null;
            }

            // Stage 1: Clean up earlier generated clutter only (never touch original assets)
            CleanupEarlierGenerated(pz.transform);

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(pz.transform, false);
            Undo.RegisterCreatedObjectUndo(rootGo, "Restructure Project Zone Colony");

            var matRockDark = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Asteroid_RockDark.mat");
            var matSlate = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Asteroid_Slate.mat");
            var matSteel = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_SciFi_SteelAccent.mat");
            var matGunmetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_SciFi_Gunmetal.mat");
            var matCyanNeon = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/BadgesAndVFX/Mat_Badge_CyanNeon.mat");
            var matFloorGray = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/City/Floor_gray(Demo).mat");

            var terrainGroup = CreateChild(rootGo.transform, "01_MultiIsland_Terrain");
            var bridgesGroup = CreateChild(rootGo.transform, "02_Colony_Bridges");
            var lightingGroup = CreateChild(rootGo.transform, "03_District_Lighting");
            var archipelagoGroup = CreateChild(rootGo.transform, "04_Colony_Archipelago");
            var propsGroup = CreateChild(rootGo.transform, "05_Colony_Props");

            // Stage 2: Natural multi-island asteroid terrain
            BuildTerrain(pz.transform, terrainGroup.transform, matRockDark, matSlate);

            // Stage 3: Reorganize existing buildings & props into clear districts
            ReorganizeDistricts(pz.transform);

            // Stage 4: Build purposeful bridges
            BuildBridges(pz.transform, bridgesGroup.transform, matFloorGray, matSteel, matGunmetal, matCyanNeon);

            // Stage 5: Regroup asteroid fields, build cohesive archipelago & decorative props, set district lighting
            CurateAsteroidArcs(pz.transform);
            BuildColonyArchipelagoAndProps(archipelagoGroup.transform, propsGroup.transform, lightingGroup.transform, matRockDark, matSlate, matSteel, matGunmetal, matCyanNeon);
            SetupDistrictLighting(lightingGroup.transform);

            EditorUtility.SetDirty(pz);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pz.scene);
            Debug.Log("[AsteroidTerrainExpander] Multi-island restructure complete.");
            return rootGo;
        }

        private static void CleanupEarlierGenerated(Transform pz)
        {
            var old = pz.Find(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            ClearByPrefix(pz.Find("AboutIsland/ObservatoryComplex/SecondaryBuildings"), "Citadel_Wing_");
            ClearByPrefix(pz.Find("AboutIsland/IndustrialArea/Cables"), "Ind_Conduit_");
        }

        // =========================================================================================
        // STAGE 2: TERRAIN (5 DISTINCT ROCKY ISLANDS + NW RECESS CAVE, NO FLAT RECTANGLES)
        // =========================================================================================
        private static void BuildTerrain(Transform pz, Transform terrainRoot, Material matRockDark, Material matSlate)
        {
            // 1. Main Island (Y = 0, irregular ~80x66m around X=14, Z=26)
            var mainRock = pz.Find("AboutIsland/Terrain/MainRockIsland");
            if (mainRock != null)
            {
                SetTf(mainRock, "Main_Asteroid_Shelf_Center", new Vector3(14f, -10.0f, 26f), new Vector3(0f, 16f, 0f), new Vector3(66f, 20f, 52f));
                SetTf(mainRock, "West_Landing_Apron_Massif", new Vector3(-16f, -8.5f, 23f), new Vector3(0f, -14f, 0f), new Vector3(28f, 17f, 40f));
                SetTf(mainRock, "South_Refinery_Support_Shelf", new Vector3(14f, -8.0f, 6f), new Vector3(0f, 8f, 0f), new Vector3(44f, 16f, 20f));
                SetTf(mainRock, "North_Plateau_Promontory", new Vector3(14f, -8.0f, 54f), new Vector3(0f, 102f, 0f), new Vector3(42f, 16f, 22f));
                SetTf(mainRock, "Underbelly_Primary_Cone", new Vector3(12f, -3.5f, 24f), new Vector3(0f, 35f, 180f), new Vector3(84f, 42f, 68f));
                SetTf(mainRock, "Underbelly_West_Keel_Cone", new Vector3(-12f, -4.0f, 23f), new Vector3(0f, 115f, 180f), new Vector3(38f, 26f, 38f));
                SetTf(mainRock, "Underbelly_South_Spur_Cone", new Vector3(16f, -4.5f, 6f), new Vector3(0f, -40f, 180f), new Vector3(40f, 24f, 30f));
            }

            // Cliffs + NW Recessed Cave under Main Island's north-west rim (-8, -2, 46)
            var cliffs = pz.Find("AboutIsland/Terrain/RockCliffs");
            if (cliffs != null)
            {
                SetTf(cliffs, "Perimeter_Ridge_NorthEast", new Vector3(40f, -4.5f, 48f), new Vector3(-8f, 28f, 10f), new Vector3(24f, 16f, 22f));
                SetTf(cliffs, "Perimeter_Ridge_NorthWest", new Vector3(-10f, -3.5f, 49f), new Vector3(6f, -38f, -8f), new Vector3(22f, 16f, 20f));
                SetTf(cliffs, "Perimeter_Ridge_SouthEast", new Vector3(42f, -5.5f, 4f), new Vector3(5f, 118f, -6f), new Vector3(22f, 14f, 20f));
            }
            var floatingRocks = pz.Find("AboutIsland/Terrain/FloatingRocks");
            if (floatingRocks != null)
            {
                for (int i = 0; i < floatingRocks.childCount; i++)
                {
                    var r = floatingRocks.GetChild(i);
                    Undo.RecordObject(r, "Group Main Island Rim Rocks");
                    float ang = (30f + i * 55f) * Mathf.Deg2Rad;
                    r.position = new Vector3(14f + Mathf.Cos(ang) * 44f, -3.5f - (i % 3) * 1.5f, 26f + Mathf.Sin(ang) * 35f);
                }
            }
            var crystals = pz.Find("AboutIsland/Terrain/Crystals");
            if (crystals != null)
            {
                Undo.RecordObject(crystals, "Place Crystals in NW Cave Recess");
                crystals.position = new Vector3(-9f, -3.5f, 46f);
                crystals.localScale = Vector3.one * 0.45f;
            }
            // Cave overhang slab on Main Island NW cliff
            SpawnModel(terrainRoot, "MainIsland_NW_Cave_Overhang", "rock_largeB",
                new Vector3(-11f, -1.2f, 46f), new Vector3(14f, 45f, -18f), new Vector3(14f, 10f, 12f), matSlate, matRockDark, true);

            // 2. Landing & Transport Apron (attached seamlessly to West Forecourt around X=-32, Z=23, Y=0)
            var landingMassif = CreateChild(terrainRoot, "Landing_Island_Massif");
            SpawnRockShelf(landingMassif.transform, "Landing_Shelf_Main", "rock_largeB", new Vector3(-32f, 0.0f, 23f), 16f, new Vector3(32f, 16f, 40f), new Vector3(0f, 22f, 0f), matRockDark, matSlate);
            SpawnRockShelf(landingMassif.transform, "Landing_Shelf_NorthSpur", "rock_largeA", new Vector3(-36f, -0.2f, 36f), 14f, new Vector3(22f, 14f, 20f), new Vector3(0f, -35f, 0f), matSlate, matRockDark);
            SpawnModel(landingMassif.transform, "Landing_Underbelly_Keel", "meteor_half", new Vector3(-33f, -3.0f, 23f), new Vector3(0f, 70f, 180f), new Vector3(38f, 28f, 42f), matRockDark, matSlate, true);

            // 3. Industrial Island (South, X=16, Z=-24, Y=-4) + Refinery Tier (X=55, Z=-20, Y=-15)
            var indMassif = CreateChild(terrainRoot, "Industrial_Island_Massif");
            SpawnRockShelf(indMassif.transform, "Industrial_Shelf_West", "rock_largeA", new Vector3(5f, -4.0f, -24f), 15f, new Vector3(30f, 15f, 26f), new Vector3(0f, 15f, 0f), matRockDark, matSlate);
            SpawnRockShelf(indMassif.transform, "Industrial_Shelf_East", "rock_largeB", new Vector3(24f, -4.0f, -24f), 15f, new Vector3(32f, 15f, 26f), new Vector3(0f, -25f, 0f), matSlate, matRockDark);
            SpawnModel(indMassif.transform, "Industrial_Underbelly_Keel", "meteor_half", new Vector3(15f, -6.5f, -24f), new Vector3(0f, -20f, 180f), new Vector3(56f, 26f, 32f), matRockDark, matSlate, true);
            // Refinery tier (Outpost_02 / Island_07)
            SpawnRockShelf(indMassif.transform, "Refinery_Tier_Shelf", "rock_largeB", new Vector3(55f, -15.0f, -20f), 14f, new Vector3(24f, 14f, 22f), new Vector3(0f, 40f, 0f), matRockDark, matSlate);

            // 4. Research Outpost Island (North-West, X=-32, Z=63, Y=+8) — clear of Maglev loop
            var resMassif = CreateChild(terrainRoot, "Research_Island_Massif");
            SpawnRockShelf(resMassif.transform, "Research_Peak_Shelf", "rock_largeA", new Vector3(-32f, 8.0f, 63f), 14f, new Vector3(24f, 14f, 22f), new Vector3(0f, 35f, 0f), matSlate, matRockDark);
            SpawnModel(resMassif.transform, "Research_Peak_Keel", "meteor_half", new Vector3(-32f, 5.5f, 63f), new Vector3(0f, 110f, 180f), new Vector3(26f, 22f, 24f), matRockDark, matSlate, true);

            // 5. Aux Operations Island (East, X=71, Z=9, Y=-6) — clear of Maglev loop
            var auxMassif = CreateChild(terrainRoot, "Aux_Island_Massif");
            SpawnRockShelf(auxMassif.transform, "Aux_Shelf_Main", "rock_largeB", new Vector3(71f, -6.0f, 9f), 14f, new Vector3(24f, 14f, 26f), new Vector3(0f, 65f, 0f), matRockDark, matSlate);
            SpawnModel(auxMassif.transform, "Aux_Underbelly_Keel", "meteor_half", new Vector3(71f, -8.5f, 9f), new Vector3(0f, -55f, 180f), new Vector3(26f, 22f, 28f), matSlate, matRockDark, true);

            // Outer Recharge Dock rock base (-135, 24, 85)
            SpawnRockShelf(terrainRoot, "Outpost05_Recharge_Rock", "rock_largeA", new Vector3(-135f, 23.8f, 85f), 16f, new Vector3(26f, 16f, 26f), new Vector3(0f, 25f, 0f), matSlate, matRockDark);
        }

        // =========================================================================================
        // STAGE 3: REORGANIZE EXISTING OBJECTS INTO DISTRICTS
        // =========================================================================================
        private static void ReorganizeDistricts(Transform pz)
        {
            // --- A. SPLIT OBSERVATORY COURTYARD PLAZA (East half = Main Forecourt, West half = Landing Apron) ---
            var plaza = pz.Find("AboutIsland/Observatory_Courtyard_Plaza");
            if (plaza != null)
            {
                for (int i = 0; i < plaza.childCount; i++)
                {
                    var c = plaza.GetChild(i);
                    Undo.RecordObject(c, "Reorganize Plaza & Landing Apron");
                    if (c.name.StartsWith("Courtyard_Tile_"))
                    {
                        // Keep original grid X (-40..0) so Landing Apron attaches seamlessly to Central Forecourt
                        var parts = c.name.Split('_');
                        if (parts.Length >= 4 && float.TryParse(parts[2], out float origX))
                        {
                            c.position = new Vector3(origX, 0.1f, c.position.z);
                        }
                    }
                    else if (c.name.StartsWith("Balustrade_West_"))
                    {
                        c.position = new Vector3(-42f, 0.1f, c.position.z);
                    }
                    else if (c.name.StartsWith("Balustrade_North_X") || c.name.StartsWith("Balustrade_South_X"))
                    {
                        int idx = c.name.LastIndexOf('X');
                        if (idx >= 0 && float.TryParse(c.name.Substring(idx + 1), out float bx))
                        {
                            c.position = new Vector3(bx, 0.1f, c.position.z);
                        }
                    }
                    else if (c.name == "Plaza_Monument_Of_Light")
                    {
                        c.position = new Vector3(-18f, 0.1f, 32f);
                    }
                    else if (c.name == "Rover_Charging_Berth")
                    {
                        c.position = new Vector3(-18f, 0.1f, 14f);
                    }
                }
            }

            // --- B. LANDING & TRANSPORT APRON (attached seamlessly at X=-32, Z=23, Y=0) ---
            var landingZone = pz.Find("AboutIsland/LandingZone");
            if (landingZone != null)
            {
                Undo.RecordObject(landingZone, "Attach LandingZone to West Forecourt");
                landingZone.position = new Vector3(-1.6f, 0f, 15f);
            }

            var cargoMain = pz.Find("AboutIsland/Props/Cargo_Container_Main");
            if (cargoMain != null)
            {
                Undo.RecordObject(cargoMain, "Place Cargo Container on Landing Apron");
                cargoMain.position = new Vector3(-36f, 0.1f, 11f);
            }

            var header = pz.Find("Zone_Header_Title");
            if (header != null)
            {
                Undo.RecordObject(header, "Move Zone Header over West Gate");
                header.position = new Vector3(-24f, 15.5f, 23.3f);
            }

            // --- C. CITADEL SURROUNDING MACHINERY NETWORK (Split cleanly by district!) ---
            var net = pz.Find("AboutIsland/Citadel_Surrounding_Machinery_Network");
            if (net != null)
            {
                // Move Flight_* onto Landing Apron north edge (clear of Monument & Pad)
                MoveIfFound(net, "Flight_FlightLine_Charger", new Vector3(-36f, 0.1f, 35f));
                MoveIfFound(net, "Flight_Tool_Vault_Alpha", new Vector3(-38f, 0.1f, 31f));
                MoveIfFound(net, "Flight_Tool_Vault_Beta", new Vector3(-38f, 0.1f, 37f));

                // Move South_* onto Industrial Island (Y=-3.9, Z=-30..-18)
                MoveIfFound(net, "South_Aux_Turbine_A", new Vector3(-2f, -3.9f, -22f));
                MoveIfFound(net, "South_Aux_Turbine_B", new Vector3(-2f, -3.9f, -18f));
                MoveIfFound(net, "South_Grid_Capacitor_Alpha", new Vector3(11f, -3.9f, -18f));
                MoveIfFound(net, "South_Grid_Capacitor_Beta", new Vector3(15f, -3.9f, -18f));
                MoveIfFound(net, "South_Heavy_Refinery_Freight_1", new Vector3(0f, -3.9f, -30f));
                MoveIfFound(net, "South_Heavy_Refinery_Freight_2", new Vector3(5f, -3.9f, -30f));

                // Move East_* onto Aux Operations Island (X=68..74, Y=-5.9, Z=6..12)
                MoveIfFound(net, "East_Logistics_Container_1", new Vector3(68f, -5.9f, 6f));
                MoveIfFound(net, "East_Logistics_Container_2", new Vector3(68f, -5.9f, 12f));
                MoveIfFound(net, "East_Heavy_Cargo_Freight", new Vector3(74f, -5.9f, 9f));
            }

            // --- D. INDUSTRIAL ISLAND: IndustrialArea + Gemstone_Import_Terminal + Shuttles ---
            var indArea = pz.Find("AboutIsland/IndustrialArea");
            if (indArea != null)
            {
                Undo.RecordObject(indArea, "Move IndustrialArea to Industrial Island");
                // Original pos was (0, 0, 15) with children around X=2..9, Z=-8..2.
                // Shift by (0, -4, -20) -> world Y=-3.9, Z=-28..-18!
                indArea.position = new Vector3(0f, -4f, -5f);
            }

            var gemTerminal = pz.Find("AboutIsland/Gemstone_Import_Terminal");
            if (gemTerminal != null)
            {
                var oldPos = gemTerminal.position;
                var newPos = new Vector3(20f, -4f, -22f);
                var delta = newPos - oldPos;

                Undo.RecordObject(gemTerminal, "Move Gemstone Import Terminal to Industrial Island");
                gemTerminal.position = newPos;

                // Tidy its Foundation_Rock_Shelf so it sits neatly inside Industrial Island
                var rockShelf = gemTerminal.Find("Foundation_Rock_Shelf");
                if (rockShelf != null)
                {
                    Undo.RecordObject(rockShelf, "Scale Terminal Rock Shelf");
                    rockShelf.localScale = new Vector3(0.65f, 0.55f, 0.65f);
                }

                // Shift Gemstone_Cargo_Shuttle_Fleet terminal-end waypoints by the exact same delta!
                var fleet = pz.Find("04_Traffic_Spaceships/Gemstone_Cargo_Shuttle_Fleet");
                if (fleet != null)
                {
                    foreach (Transform shuttle in fleet)
                    {
                        var ai = shuttle.GetComponent<GemstoneCargoShuttle>();
                        if (ai == null) continue;
                        Undo.RecordObject(shuttle, "Update Shuttle Terminal Waypoints");
                        Undo.RecordObject(ai, "Update Shuttle Route Waypoints");
                        if (shuttle.name.Contains("Courier"))
                        {
                            shuttle.position += delta;
                        }
                        for (int w = 0; w < ai.routeWaypoints.Count; w++)
                        {
                            // Only shift waypoints near the old terminal (Z > -25 and X < 60)
                            var wp = ai.routeWaypoints[w];
                            if (wp.x < 60f && wp.z > -25f)
                            {
                                ai.routeWaypoints[w] = wp + delta;
                            }
                        }
                    }
                }
            }

            // --- E. FLOATING ISLANDS & CYBERPUNK OUTPOSTS -> RESEARCH, AUX & REFINERY ---
            var islands = pz.Find("02_Floating_Islands_Organic");
            if (islands != null)
            {
                MoveIfFound(islands, "Island_02_ChimneySmoker_TopMidLeft", new Vector3(-34f, 7.5f, 65f));
                MoveIfFound(islands, "Island_04_HologramOutpost_BottomLeft", new Vector3(-37f, 5.5f, 58f));
                MoveIfFound(islands, "Island_05_UpperRelay_TopMidRight", new Vector3(-29f, 7.5f, 61f));
                MoveIfFound(islands, "Island_06_RadarSpire_TopRight", new Vector3(71f, -6.5f, 9f));
                MoveIfFound(islands, "Island_07_TwinSmokestacks_BottomRight", new Vector3(55f, -16f, -20f));
            }

            var expanded = pz.Find("Project_Zone_Expanded_Outposts");
            if (expanded != null)
            {
                var out1 = expanded.Find("Outpost_01_Comm_Relay");
                if (out1 != null)
                {
                    Undo.RecordObject(out1, "Move Outpost_01 to Research Island");
                    out1.position = new Vector3(-32f, 8.1f, 63f);
                    // Shorten the 350m laser beam to a clean 18m comm beam
                    var laser = out1.Find("Deep_Space_Laser_Uplink");
                    if (laser != null)
                    {
                        var lr = laser.GetComponent<LineRenderer>();
                        if (lr != null && lr.positionCount >= 2)
                        {
                            Undo.RecordObject(lr, "Shorten Comm Laser Beam");
                            lr.SetPosition(1, new Vector3(0f, 18f, 0f));
                        }
                    }
                }
                MoveIfFound(expanded, "Outpost_02_Crystal_Refinery", new Vector3(55f, -15f, -20f));
            }

            var cyber = pz.Find("03_Cyberpunk_Outposts");
            if (cyber != null)
            {
                int chimneyIdx = 0;
                Vector3[] chimneySpots =
                {
                    new Vector3(-36f, 8.1f, 64f),  // Research Island
                    new Vector3(-34f, 8.1f, 60f),  // Research Island
                    new Vector3(74f, -5.9f, 13f),  // Aux Island
                    new Vector3(52f, -14.5f, -18f),// Refinery Tier
                    new Vector3(58f, -14.5f, -20f) // Refinery Tier
                };
                for (int i = 0; i < cyber.childCount; i++)
                {
                    var c = cyber.GetChild(i);
                    Undo.RecordObject(c, "Organize Cyberpunk Outpost Props");
                    if (c.name.StartsWith("satelliteDish_large"))
                    {
                        c.position = new Vector3(-28f, 8.2f, 66f);
                        c.eulerAngles = new Vector3(0f, -135f, 0f);
                        c.localScale = Vector3.one * 3.2f;
                    }
                    else if (c.name.StartsWith("chimney_detailed") && chimneyIdx < chimneySpots.Length)
                    {
                        c.position = chimneySpots[chimneyIdx++];
                        c.eulerAngles = Vector3.zero;
                        c.localScale = new Vector3(2.4f, 3.2f, 2.4f);
                    }
                    else if (c.name.StartsWith("Cosmic_Retro_Computer"))
                    {
                        c.position = new Vector3(55f, -14.5f, -23f);
                    }
                }
            }
        }

        // =========================================================================================
        // STAGE 4: CONNECTIVITY — EXACTLY 5 PURPOSEFUL BRIDGES
        // =========================================================================================
        private static void BuildBridges(
            Transform pz, Transform bridgesRoot,
            Material matFloor, Material matSteel, Material matGunmetal, Material matCyanNeon)
        {
            // B1 removed because Landing Apron is now seamlessly attached to West Forecourt at X=-40..0

            // B2: Industrial Walkway + Pipeline (Citadel South -> Industrial Island)
            BuildBridge(bridgesRoot, "B2_Industrial_Walkway_And_Pipeline",
                new Vector3(14f, 0.1f, 2f), new Vector3(14f, -3.9f, -14f), 4, 2.2f,
                matFloor, matSteel, matCyanNeon, true);
            BuildPipeLine(bridgesRoot, "B2_Industrial_Energy_Conduit",
                new Vector3(17f, 0.3f, 2f), new Vector3(17f, -3.6f, -14f), 4, matGunmetal, matSteel);

            // B3: Aux Causeway (East Chamber -> Aux Operations Island)
            BuildBridge(bridgesRoot, "B3_Aux_Causeway_East",
                new Vector3(44f, 0.1f, 18f), new Vector3(62f, -5.9f, 10f), 5, 2.2f,
                matFloor, matSteel, matCyanNeon, true);

            // B4: Research Stair-Bridge (Central Plaza NW -> Research Outpost Island +8m)
            BuildBridge(bridgesRoot, "B4_Research_Stair_Bridge",
                new Vector3(-20f, 0.1f, 41f), new Vector3(-28f, 8.1f, 54f), 5, 2.2f,
                matFloor, matSteel, matCyanNeon, true);

            // B5: Mining Viaduct (Industrial Island -> GemstoneMiningAsteroid) + Refinery Tier spur
            var viaStart = new Vector3(28f, -3.9f, -30f);
            var viaEnd = new Vector3(47.6f, 0.4f, -83.2f);
            BuildBridge(bridgesRoot, "B5_Mining_Viaduct_To_Asteroid",
                viaStart, viaEnd, 11, 2.2f, matFloor, matSteel, matCyanNeon, true);

            // Reuse the 3 monorail_trackStraight(Clone) in 03_Cyberpunk_Outposts along B5
            var cyber = pz.Find("03_Cyberpunk_Outposts");
            if (cyber != null)
            {
                var rot = Quaternion.LookRotation((viaEnd - viaStart).normalized, Vector3.up).eulerAngles;
                int tIdx = 0;
                float[] ts = { 0.25f, 0.50f, 0.75f };
                for (int i = 0; i < cyber.childCount && tIdx < ts.Length; i++)
                {
                    var c = cyber.GetChild(i);
                    if (c.name.StartsWith("monorail_trackStraight"))
                    {
                        Undo.RecordObject(c, "Align Monorail Track on B5 Viaduct");
                        c.position = Vector3.Lerp(viaStart, viaEnd, ts[tIdx++]) + new Vector3(2.8f, 0.2f, 0f);
                        c.eulerAngles = rot;
                        c.localScale = new Vector3(2.0f, 2.0f, 6.0f);
                    }
                }
            }

            // Refinery Tier stepped spur (Industrial East -> Outpost_02)
            BuildBridge(bridgesRoot, "B5_Refinery_Tier_Spur",
                new Vector3(36f, -3.9f, -24f), new Vector3(48f, -14.9f, -20f), 4, 1.8f,
                matFloor, matSteel, matCyanNeon, true);
        }

        // =========================================================================================
        // STAGE 5: ASTEROID ARCS & DISTRICT LIGHTING
        // =========================================================================================
        private static void CurateAsteroidArcs(Transform pz)
        {
            CurateFieldToTwoArcs(pz.Find("05_Drifting_Asteroid_Field"), 108f, 136f);
            CurateFieldToTwoArcs(pz.Find("Project_Zone_Expanded_Outposts/Complex_Asteroid_Field"), 112f, 142f);
        }

        private static void CurateFieldToTwoArcs(Transform field, float rMin, float rMax)
        {
            if (field == null) return;
            for (int i = 0; i < field.childCount; i++)
            {
                var rock = field.GetChild(i);
                Undo.RecordObject(rock, "Curate Asteroid Arc");
                // Half to NW arc (115..175 deg), half to SE arc (285..345 deg), outside Maglev loop
                bool nw = (i % 2 == 0);
                float t = (float)(i / 2) / Mathf.Max(1, field.childCount / 2);
                float deg = nw ? Mathf.Lerp(115f, 175f, t) : Mathf.Lerp(285f, 345f, t);
                float rad = deg * Mathf.Deg2Rad;
                float r = Mathf.Lerp(rMin, rMax, (i % 5) / 4f);
                float y = -12f + (i % 6) * 6f;
                rock.position = new Vector3(5f + Mathf.Cos(rad) * r, y, 20f + Mathf.Sin(rad) * r);
            }
        }

        private static void BuildColonyArchipelagoAndProps(
            Transform archipelagoRoot, Transform propsRoot, Transform lightRoot,
            Material matRockDark, Material matSlate, Material matSteel, Material matGunmetal, Material matCyanNeon)
        {
            var matCrystalCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_BlueCyan.mat");
            var matCrystalPurple = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_PurpleRare.mat");

            // 1. Multi-tier rock buttresses around secondary islands so they feel anchored, not tiny pebbles
            var buttresses = new (string name, string model, Vector3 pos, Vector3 rot, Vector3 scale, bool slateFirst)[]
            {
                // Research Outpost Island (-32, 8, 63) flanking spurs
                ("Res_Buttress_NW", "rock_largeB", new Vector3(-42f, 2.0f, 67f), new Vector3(6f, 48f, -8f), new Vector3(18f, 13f, 16f), true),
                ("Res_Buttress_NE", "rock_largeA", new Vector3(-20f, 1.5f, 66f), new Vector3(-4f, -35f, 6f), new Vector3(16f, 12f, 15f), false),
                ("Res_Lower_Spur", "meteor_half", new Vector3(-34f, -3.5f, 54f), new Vector3(0f, 145f, 180f), new Vector3(22f, 16f, 20f), true),

                // Aux Operations Island (71, -6, 9) flanking spurs
                ("Aux_Buttress_East", "rock_largeA", new Vector3(81f, -10.0f, 14f), new Vector3(5f, 25f, 10f), new Vector3(18f, 13f, 16f), false),
                ("Aux_Buttress_South", "rock_largeB", new Vector3(67f, -11.5f, -3f), new Vector3(-8f, -65f, 5f), new Vector3(19f, 12f, 17f), true),

                // Industrial Island (15, -4, -24) & Refinery Tier (55, -15, -20) flanking spurs
                ("Ind_Buttress_SW", "rock_largeB", new Vector3(-6f, -9.0f, -31f), new Vector3(4f, 55f, -7f), new Vector3(22f, 13f, 18f), true),
                ("Ind_Buttress_SE", "rock_largeA", new Vector3(36f, -9.5f, -32f), new Vector3(-5f, -40f, 8f), new Vector3(20f, 12f, 18f), false),
                ("Refinery_Buttress", "meteor_half", new Vector3(62f, -18.5f, -24f), new Vector3(0f, 80f, 180f), new Vector3(22f, 14f, 20f), true),
            };
            foreach (var b in buttresses)
            {
                SpawnModel(archipelagoRoot, b.name, b.model, b.pos, b.rot, b.scale,
                    b.slateFirst ? matSlate : matRockDark, b.slateFirst ? matRockDark : matSlate, true);
            }

            // 2. Western Archipelago Chain: bridges Main/Landing Island (-40, 23) to SW Relay Rock (-135, 85)
            var westChain = new (string model, Vector3 pos, Vector3 rot, float scale)[]
            {
                ("rock_largeA", new Vector3(-56f, -2.5f, 31f), new Vector3(8f, 25f, -5f), 14f),
                ("meteor_detailed", new Vector3(-68f, 1.0f, 38f), new Vector3(-12f, 65f, 14f), 11f),
                ("rock_largeB", new Vector3(-79f, 4.5f, 46f), new Vector3(5f, 110f, -8f), 16f),
                ("craterLarge", new Vector3(-92f, 8.5f, 54f), new Vector3(-6f, 15f, 9f), 12f),
                ("meteor", new Vector3(-103f, 12.0f, 63f), new Vector3(14f, -45f, 10f), 10f),
                ("rock_largeA", new Vector3(-115f, 16.5f, 72f), new Vector3(-4f, 85f, -6f), 15f),
                ("rocks_smallA", new Vector3(-62f, -1.0f, 24f), new Vector3(0f, 40f, 0f), 8f),
                ("rocks_smallB", new Vector3(-86f, 6.0f, 42f), new Vector3(10f, 120f, 5f), 9f),
            };
            for (int i = 0; i < westChain.Length; i++)
            {
                var w = westChain[i];
                SpawnModel(archipelagoRoot, $"West_Archipelago_{i:D2}", w.model, w.pos, w.rot, Vector3.one * w.scale,
                    (i % 2 == 0) ? matRockDark : matSlate, (i % 2 == 0) ? matSlate : matRockDark, true);
            }

            // 3. SW Bay, NE Rim, & B5 Mining Corridor Asteroids (fills empty black void around colony)
            var clusterRocks = new (string model, Vector3 pos, Vector3 rot, Vector3 scale)[]
            {
                // SW Bay (between Landing Apron and Industrial Island)
                ("rock_largeB", new Vector3(-24f, -4.5f, -6f), new Vector3(6f, 32f, -10f), new Vector3(16f, 12f, 15f)),
                ("meteor_detailed", new Vector3(-38f, -6.0f, -12f), new Vector3(-10f, 78f, 12f), new Vector3(12f, 11f, 12f)),
                ("rock_largeA", new Vector3(-14f, -7.5f, -18f), new Vector3(4f, -25f, 8f), new Vector3(13f, 10f, 12f)),
                ("rocks_smallA", new Vector3(-30f, -2.5f, 2f), new Vector3(0f, 55f, 0f), new Vector3(8f, 8f, 8f)),

                // North & North-East Rim (framing Citadel & Research Outpost)
                ("rock_largeA", new Vector3(48f, -3.5f, 48f), new Vector3(-5f, 40f, 8f), new Vector3(18f, 13f, 16f)),
                ("meteor_detailed", new Vector3(60f, -5.0f, 35f), new Vector3(10f, 115f, -12f), new Vector3(13f, 12f, 13f)),
                ("craterLarge", new Vector3(36f, 1.5f, 62f), new Vector3(8f, -30f, 5f), new Vector3(15f, 11f, 15f)),
                ("rock_largeB", new Vector3(12f, -3.0f, 72f), new Vector3(-7f, 80f, 6f), new Vector3(18f, 13f, 16f)),
                ("meteor", new Vector3(-8f, 3.0f, 74f), new Vector3(12f, 15f, -9f), new Vector3(11f, 11f, 11f)),
                ("rocks_smallB", new Vector3(26f, -1.5f, 66f), new Vector3(0f, 95f, 0f), new Vector3(9f, 9f, 9f)),

                // East & South-East (framing Aux Island & Refinery)
                ("rock_largeB", new Vector3(86f, -9.0f, 26f), new Vector3(9f, -50f, 7f), new Vector3(16f, 12f, 15f)),
                ("meteor_detailed", new Vector3(82f, -12.0f, -10f), new Vector3(-8f, 25f, 14f), new Vector3(13f, 12f, 13f)),
                ("rocks_smallA", new Vector3(56f, -4.0f, 28f), new Vector3(5f, 70f, -4f), new Vector3(9f, 9f, 9f)),

                // B5 Mining Viaduct Corridor (Industrial -> GemstoneMiningAsteroid)
                ("rock_largeA", new Vector3(22f, -6.5f, -48f), new Vector3(7f, 42f, -9f), new Vector3(16f, 12f, 15f)),
                ("meteor_detailed", new Vector3(56f, -5.0f, -54f), new Vector3(-11f, 105f, 8f), new Vector3(14f, 13f, 14f)),
                ("rock_largeB", new Vector3(66f, -8.5f, -40f), new Vector3(5f, -65f, 11f), new Vector3(17f, 12f, 16f)),
                ("craterLarge", new Vector3(34f, -8.0f, -68f), new Vector3(-6f, 20f, -8f), new Vector3(14f, 11f, 14f)),
                ("meteor", new Vector3(64f, -2.0f, -72f), new Vector3(10f, 140f, 6f), new Vector3(12f, 12f, 12f)),
                ("rocks_smallB", new Vector3(44f, -6.5f, -58f), new Vector3(0f, 35f, 0f), new Vector3(9f, 9f, 9f)),
            };
            for (int i = 0; i < clusterRocks.Length; i++)
            {
                var c = clusterRocks[i];
                SpawnModel(archipelagoRoot, $"Colony_Cluster_Rock_{i:D2}", c.model, c.pos, c.rot, c.scale,
                    (i % 2 == 0) ? matSlate : matRockDark, (i % 2 == 0) ? matRockDark : matSlate, true);
            }

            // 4. Crystal Outcrops on key perimeter asteroids (gives visual rhythm & color unity)
            var crystalSpots = new (string model, Vector3 pos, Vector3 rot, float scale, bool purple)[]
            {
                ("rock_crystalsLargeA", new Vector3(-56f, 3.0f, 31f), new Vector3(0f, 25f, 0f), 4.5f, false),
                ("rock_crystalsLargeB", new Vector3(-79f, 10.5f, 46f), new Vector3(0f, 110f, 0f), 5.0f, true),
                ("rock_crystalsLargeA", new Vector3(-24f, 0.5f, -6f), new Vector3(0f, 45f, 0f), 4.2f, false),
                ("rock_crystalsLargeB", new Vector3(48f, 2.0f, 48f), new Vector3(0f, -35f, 0f), 4.8f, false),
                ("rock_crystalsLargeA", new Vector3(81f, -4.5f, 14f), new Vector3(0f, 75f, 0f), 4.5f, true),
                ("rock_crystalsLargeB", new Vector3(22f, -1.5f, -48f), new Vector3(0f, 15f, 0f), 4.8f, true),
                ("rock_crystalsLargeA", new Vector3(56f, 0.5f, -54f), new Vector3(0f, -80f, 0f), 5.0f, true),
                ("rock_crystals", new Vector3(-42f, 7.5f, 67f), new Vector3(0f, 50f, 0f), 4.0f, false),
            };
            for (int i = 0; i < crystalSpots.Length; i++)
            {
                var cs = crystalSpots[i];
                var mat = cs.purple ? matCrystalPurple : matCrystalCyan;
                SpawnModel(propsRoot, $"Colony_Crystal_Outcrop_{i:D2}", cs.model, cs.pos, cs.rot, Vector3.one * cs.scale,
                    matRockDark, mat, false);
                AddPointLight(lightRoot, $"Light_Crystal_{i:D2}", cs.pos + Vector3.up * 2.5f,
                    cs.purple ? new Color(0.75f, 0.35f, 1.0f) : new Color(0.20f, 0.90f, 1.0f), 2.4f, 18f);
            }

            // 5. Sci-Fi Colony Decorative Props (dishes, antennas, street lights, cargo barrels)
            var sciFiProps = new (string name, string model, Vector3 pos, Vector3 rot, float scale, Material m1, Material m2)[]
            {
                // Comm dishes & relay antennas on elevated rock promontories
                ("Prop_Relay_Dish_NW", "satelliteDish_large", new Vector3(-42f, 7.5f, 67f), new Vector3(0f, 145f, 0f), 2.2f, matSteel, matCyanNeon),
                ("Prop_Relay_Dish_NE", "satelliteDish_detailed", new Vector3(36f, 6.0f, 62f), new Vector3(0f, -120f, 0f), 2.4f, matSteel, matCyanNeon),
                ("Prop_Relay_Antenna_West", "Antenna_2", new Vector3(-56f, 3.2f, 32f), new Vector3(0f, 40f, 0f), 2.2f, matGunmetal, matCyanNeon),
                ("Prop_Relay_Antenna_SW", "Antenna_1", new Vector3(-24f, 0.8f, -5f), new Vector3(0f, 15f, 0f), 2.2f, matGunmetal, matCyanNeon),
                ("Prop_Relay_Antenna_East", "Antenna_2", new Vector3(81f, -4.2f, 13f), new Vector3(0f, -60f, 0f), 2.2f, matGunmetal, matCyanNeon),

                // Landing Apron perimeter lights & cargo barrels (clear of Z=23 tour path)
                ("Prop_Apron_Lamp_South", "Light_Street_1", new Vector3(-41f, 0.1f, 14f), new Vector3(0f, 90f, 0f), 1.6f, matGunmetal, matCyanNeon),
                ("Prop_Apron_Lamp_North", "Light_Street_1", new Vector3(-41f, 0.1f, 32f), new Vector3(0f, 90f, 0f), 1.6f, matGunmetal, matCyanNeon),
                ("Prop_Plaza_Lamp_SW", "Light_Street_2", new Vector3(-22f, 0.1f, 12f), new Vector3(0f, 0f, 0f), 1.5f, matGunmetal, matCyanNeon),
                ("Prop_Plaza_Lamp_NW", "Light_Street_2", new Vector3(-22f, 0.1f, 34f), new Vector3(0f, 180f, 0f), 1.5f, matGunmetal, matCyanNeon),
                ("Prop_Apron_Barrels_01", "barrels_rail", new Vector3(-34.5f, 0.1f, 9.5f), new Vector3(0f, 25f, 0f), 1.6f, matSteel, matGunmetal),
                ("Prop_Apron_Barrels_02", "barrels", new Vector3(-39.5f, 0.1f, 12.5f), new Vector3(0f, -40f, 0f), 1.6f, matSteel, matGunmetal),
                ("Prop_Industrial_Barrels", "barrels_rail", new Vector3(8f, -3.9f, -17f), new Vector3(0f, 15f, 0f), 1.6f, matSteel, matGunmetal),
            };
            foreach (var p in sciFiProps)
            {
                SpawnModel(propsRoot, p.name, p.model, p.pos, p.rot, Vector3.one * p.scale, p.m1, p.m2, false);
            }
        }

        private static void SetupDistrictLighting(Transform lightRoot)
        {
            AddPointLight(lightRoot, "Light_Landing_Island", new Vector3(-48f, 6f, 23f), new Color(0.25f, 0.95f, 0.85f), 3.5f, 32f);
            AddPointLight(lightRoot, "Light_Central_Forecourt", new Vector3(-18f, 6f, 23f), new Color(0.30f, 0.88f, 1.0f), 3.5f, 28f);
            AddPointLight(lightRoot, "Light_Industrial_Island", new Vector3(14f, 2f, -22f), new Color(1.0f, 0.58f, 0.18f), 4.0f, 34f);
            AddPointLight(lightRoot, "Light_Research_Island", new Vector3(-32f, 14f, 63f), new Color(0.65f, 0.55f, 1.0f), 3.8f, 28f);
            AddPointLight(lightRoot, "Light_Aux_Island", new Vector3(71f, 0f, 9f), new Color(0.25f, 0.90f, 0.78f), 3.5f, 26f);
            AddPointLight(lightRoot, "Light_Main_NW_Cave", new Vector3(-9f, -1.5f, 46f), new Color(0.30f, 0.92f, 1.0f), 3.2f, 16f);
        }

        // =========================================================================================
        // HELPERS
        // =========================================================================================
        private static void BuildBridge(
            Transform parent, string name, Vector3 startPos, Vector3 endPos, int segments, float halfWidth,
            Material matFloor, Material matSteel, Material matCyanNeon, bool withPylons)
        {
            var root = CreateChild(parent, name);
            var delta = endPos - startPos;
            var dir = delta.normalized;
            var flatDir = new Vector3(dir.x, 0f, dir.z).normalized;
            if (flatDir.sqrMagnitude < 0.001f) flatDir = Vector3.forward;

            var rot = Quaternion.LookRotation(dir, Vector3.up);
            var yawRot = Quaternion.LookRotation(flatDir, Vector3.up);
            var right = yawRot * Vector3.right;
            float segLen = delta.magnitude / segments;
            float zScale = (segLen / 2.0f) * 1.06f;

            for (int i = 0; i <= segments; i++)
            {
                var pos = Vector3.Lerp(startPos, endPos, (float)i / segments);
                SpawnModel(root.transform, $"Deck_{i:D2}", "platform_large",
                    pos, rot.eulerAngles, new Vector3(halfWidth, 1.2f, zScale), matFloor, matSteel, true);

                var railRot = (rot * Quaternion.Euler(0f, 90f, 0f)).eulerAngles;
                SpawnModel(root.transform, $"RailL_{i:D2}", "rail_middle",
                    pos - right * (halfWidth * 0.92f) + Vector3.up * 0.15f, railRot, new Vector3(segLen * 0.95f, 1.2f, 1.2f), matCyanNeon, matSteel, true);
                SpawnModel(root.transform, $"RailR_{i:D2}", "rail_middle",
                    pos + right * (halfWidth * 0.92f) + Vector3.up * 0.15f, railRot, new Vector3(segLen * 0.95f, 1.2f, 1.2f), matCyanNeon, matSteel, true);

                if (withPylons && (i == segments / 2))
                {
                    SpawnModel(root.transform, $"Pylon_{i:D2}", "supports_high",
                        pos + new Vector3(0f, -7.0f, 0f), yawRot.eulerAngles, new Vector3(2.0f, 7.0f, 2.0f), null, null, true);
                }
            }
        }

        private static void BuildPipeLine(Transform parent, string name, Vector3 startPos, Vector3 endPos, int count, Material matPipe, Material matAccent)
        {
            var root = CreateChild(parent, name);
            var rot = Quaternion.LookRotation((endPos - startPos).normalized, Vector3.up).eulerAngles;
            for (int i = 0; i <= count; i++)
            {
                var pos = Vector3.Lerp(startPos, endPos, (float)i / count);
                SpawnModel(root.transform, $"Pipe_{i:D2}", "Pipe_1", pos, rot, new Vector3(1.1f, 1.1f, 1.7f), matPipe, matAccent, false);
            }
        }

        private static GameObject SpawnRockShelf(
            Transform parent, string name, string modelName,
            Vector3 topPos, float heightScale, Vector3 fullScale, Vector3 rotEuler,
            Material mat1, Material mat2)
        {
            fullScale.y = heightScale;
            var centerPos = new Vector3(topPos.x, topPos.y - 0.50f * heightScale, topPos.z);
            return SpawnModel(parent, name, modelName, centerPos, rotEuler, fullScale, mat1, mat2, true);
        }

        private static GameObject SpawnModel(
            Transform parent, string instanceName, string modelName,
            Vector3 worldPos, Vector3 worldEuler, Vector3 scaleMult,
            Material mat1, Material mat2, bool ensureCollider)
        {
            var asset = LoadModelAsset(modelName);
            if (asset == null) return null;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent) ?? UnityEngine.Object.Instantiate(asset, parent);
            go.name = instanceName;
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(worldEuler);
            go.transform.localScale = Vector3.Scale(asset.transform.localScale, scaleMult);

            if (mat1 != null)
            {
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = mr.sharedMaterials;
                    if (mats == null || mats.Length == 0) mr.sharedMaterial = mat1;
                    else
                    {
                        var newMats = new Material[mats.Length];
                        for (int i = 0; i < newMats.Length; i++) newMats[i] = (i % 2 == 1 && mat2 != null) ? mat2 : mat1;
                        mr.sharedMaterials = newMats;
                    }
                }
            }
            if (ensureCollider)
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                    {
                        mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    }
                }
            }
            return go;
        }

        private static GameObject LoadModelAsset(string modelName)
        {
            if (ModelCache.TryGetValue(modelName, out var cached) && cached != null) return cached;
            foreach (var filter in new[] { " t:Model", " t:Prefab" })
            {
                foreach (var guid in AssetDatabase.FindAssets(modelName + filter))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path).Equals(modelName, StringComparison.OrdinalIgnoreCase))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (asset != null) { ModelCache[modelName] = asset; return asset; }
                    }
                }
            }
            return null;
        }

        private static void SetTf(Transform parent, string childName, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
        {
            var c = parent.Find(childName);
            if (c == null) return;
            Undo.RecordObject(c, "Set Terrain Transform");
            c.localPosition = localPos;
            c.localEulerAngles = localEuler;
            c.localScale = localScale;
        }

        private static void MoveIfFound(Transform parent, string childName, Vector3 worldPos)
        {
            var c = parent.Find(childName);
            if (c == null) return;
            Undo.RecordObject(c, "Reposition District Object");
            c.position = worldPos;
        }

        private static void ClearByPrefix(Transform parent, string prefix)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (c.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) Undo.DestroyObjectImmediate(c.gameObject);
            }
        }

        private static void AddPointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = CreateChild(parent, name);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
