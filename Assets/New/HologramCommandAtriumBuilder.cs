using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceCyberpunk
{
    /// <summary>
    /// Master Planetary Citadel, Grand Starport Esplanade & Exterior Industrial Infrastructure Builder.
    /// Addresses all user feedback:
    /// 1. FIXES ROCK CLIPPING ("lại bị lỗi mặt đất rồi"):
    ///    - Removed/lowered all protruding cliff rocks (NorthWest_Cliff_Outcrop, NorthEast, SouthEast).
    ///    - Automated clamp: ensures every single asteroid rock in Terrain has bounds.max.y <= -0.20m (ZERO floor clipping!).
    /// 2. ILLUMINATED SCI-FI GLASS BALUSTRADES ("hàng rào thì đen xì"):
    ///    - Replaced the tall solid opaque black walls with sleek 1.15m waist-height Sci-Fi Glass Balustrades:
    ///      * Titanium plinth and stanchion posts.
    ///      * Translucent cyan unlit glass panel (Mat_Station_HoloGlass: NEVER dark in shadow!).
    ///      * Active glowing cyan HDR neon top handrail (Mat_Gemstone_BlueCyan) and soft perimeter lighting.
    /// 3. GAP-FREE SEAMLESS DECKS ("căn cứ thì bị lỗ một số chỗ bị thiếu mặt sàn"):
    ///    - Courtyard & Landing Apron: 100% full continuous deck from X = -40 to 0 and Z = 7 to 39 (Zero 8m void!).
    ///    - Citadel Interior: Unified gap-free 4m floor grid across all wings (Rotunda, North, West, East).
    ///    - Wall Seam Corner Pillars: 16 heavy structural columns sealing all 90-degree corner slits (Zero vertical holes!).
    /// 4. DEDICATED SEPARATED LANDING PAD at (-32, 0.12, 23) with 18m apron and 8 perimeter beacon lights.
    /// 5. EXCAVATED ENERGY TERMINAL & REFINERY with 15m open boulevard, zero building overlap.
    /// 6. PANORAMIC MISSION CONTROL VIDEO WALL at North Wall (Image 3) with multi-monitor command screens and consoles.
    /// 7. EXTENSIVE EXTERIOR MACHINERY YARDS (Image 4) around all 4 flanks, zero heavy machines indoors.
    /// </summary>
    public static class HologramCommandAtriumBuilder
    {
        [MenuItem("Tools/CodeDrive/Build Hologram Command Atrium")]
        public static void BuildAtrium()
        {
            var aboutIsland = GameObject.Find("AboutIsland");
            if (aboutIsland == null)
            {
                Debug.LogWarning("AboutIsland not found in scene!");
                return;
            }

            // Clean obsolete standalone atrium if present
            Transform oldStandalone = aboutIsland.transform.Find("Hologram_Command_Atrium");
            if (oldStandalone != null)
            {
                if (Application.isPlaying) Object.Destroy(oldStandalone.gameObject);
                else Object.DestroyImmediate(oldStandalone.gameObject);
            }

            // Clean temporary objects if present
            string[] tempNames = new string[] { "Exterior_Courtyard_And_Machinery_Yard", "Citadel_Main_Atrium", "Dedicated_Hologram_Command_Chamber" };
            foreach (var tn in tempNames)
            {
                Transform tObj = aboutIsland.transform.Find(tn);
                if (tObj != null)
                {
                    if (Application.isPlaying) Object.Destroy(tObj.gameObject);
                    else Object.DestroyImmediate(tObj.gameObject);
                }
            }
            Transform indArea = aboutIsland.transform.Find("IndustrialArea");
            if (indArea != null) indArea.gameObject.SetActive(true);
            Transform oldWalkways = aboutIsland.transform.Find("Walkways");
            if (oldWalkways != null)
            {
                var wwList = new List<GameObject>();
                foreach (Transform t in oldWalkways) wwList.Add(t.gameObject);
                foreach (var g in wwList)
                {
                    if (Application.isPlaying) Object.Destroy(g);
                    else Object.DestroyImmediate(g);
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 1. REPOSITION & SEPARATE LANDING PAD BASTION (X = -32.0m, Z = 23.0m)
            // ─────────────────────────────────────────────────────────────────
            Vector3 landingPadPos = new Vector3(-32.00f, 0.32f, 23.00f);

            var stationGo = GameObject.Find("About_Info_Station");
            Transform landingZone = aboutIsland.transform.Find("LandingZone");
            if (landingZone == null)
            {
                var lzGo = new GameObject("LandingZone");
                lzGo.transform.SetParent(aboutIsland.transform, false);
                landingZone = lzGo.transform;
            }

            Transform lpCenter = landingZone.Find("LandingPad_Center");
            if (lpCenter == null && stationGo != null) lpCenter = stationGo.transform.Find("LandingPad_Center");
            if (lpCenter != null)
            {
                lpCenter.SetParent(landingZone, true);
                lpCenter.position = landingPadPos;
            }

            Transform landingPadParent = landingZone.Find("LandingPad");
            if (landingPadParent != null)
            {
                Transform platform = landingPadParent.Find("About_Spaceship_Landing_Platform");
                if (platform != null) platform.position = landingPadPos;
                Transform substruct = landingPadParent.Find("LandingPad_Armored_Substructure");
                if (substruct != null) substruct.position = new Vector3(landingPadPos.x, -2.10f, landingPadPos.z);
            }

            Transform landingLights = landingZone.Find("LandingLights");
            if (landingLights != null)
            {
                for (int i = 0; i < landingLights.childCount; i++)
                {
                    var b = landingLights.GetChild(i);
                    float angle = (i * Mathf.PI * 2f) / landingLights.childCount;
                    float bx = landingPadPos.x + Mathf.Cos(angle) * 8.5f;
                    float bz = landingPadPos.z + Mathf.Sin(angle) * 8.5f;
                    b.position = new Vector3(bx, 0.52f, bz);
                }
            }

            Transform ctrlEquip = landingZone.Find("ControlEquipment");
            if (ctrlEquip != null)
            {
                Transform fcs = ctrlEquip.Find("Flight_Control_Station");
                if (fcs != null) fcs.position = new Vector3(-23.0f, 0.60f, 26.5f);
                Transform ard = ctrlEquip.Find("Approach_Radar_Dish");
                if (ard != null) ard.position = new Vector3(-23.0f, 0.60f, 19.5f);
                Transform ltc = ctrlEquip.Find("Landing_Telemetry_Console");
                if (ltc != null) ltc.position = new Vector3(-21.5f, 0.60f, 26.5f);
            }

            // ─────────────────────────────────────────────────────────────────
            // 2. ASTEROID TERRAIN & ZERO-ROCK-CLIPPING ENFORCEMENT
            // Fixes "lại bị lỗi mặt đất rồi": Lowers all rocks strictly <= -0.20m!
            // ─────────────────────────────────────────────────────────────────
            Transform terrain = aboutIsland.transform.Find("Terrain");
            if (terrain != null)
            {
                Transform mainRock = terrain.Find("MainRockIsland");
                if (mainRock != null)
                {
                    // 2.1 Central Planetary Shelf (175m x 150m)
                    Transform shelfRock = mainRock.Find("Main_Asteroid_Shelf_Center");
                    if (shelfRock != null)
                    {
                        shelfRock.position = new Vector3(6.00f, -15.00f, 21.00f);
                        shelfRock.localScale = new Vector3(175.00f, 18.00f, 150.00f);
                    }

                    // 2.2 West Landing Apron Massif (75m x 75m)
                    Transform westBastion = mainRock.Find("West_Landing_Apron_Massif");
                    if (westBastion == null)
                    {
                        var wbGo = new GameObject("West_Landing_Apron_Massif");
                        wbGo.transform.SetParent(mainRock, false);
                        wbGo.transform.position = new Vector3(-32.00f, -11.00f, 23.00f);
                        wbGo.transform.localScale = new Vector3(75.00f, 16.00f, 75.00f);

                        var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/kenney_space-kit/Models/FBX format/rock_largeA.fbx");
                        var rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Asteroid_RockDark.mat");
                        if (rockPrefab != null)
                        {
                            var mf = wbGo.AddComponent<MeshFilter>();
                            mf.sharedMesh = rockPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
                            var mr = wbGo.AddComponent<MeshRenderer>();
                            if (rockMat != null) mr.sharedMaterial = rockMat;
                        }
                    }
                    else
                    {
                        westBastion.position = new Vector3(-32.00f, -11.00f, 23.00f);
                        westBastion.localScale = new Vector3(75.00f, 16.00f, 75.00f);
                    }

                    // 2.3 East Observatory Massif (95m x 95m)
                    Transform massifRock = mainRock.Find("East_Observatory_Massif");
                    if (massifRock != null)
                    {
                        massifRock.position = new Vector3(38.00f, -11.00f, 23.00f);
                        massifRock.localScale = new Vector3(95.00f, 16.00f, 95.00f);
                    }

                    // 2.4 North Research & Machinery Plateau (110m x 95m)
                    Transform northPlateau = mainRock.Find("North_Plateau_Promontory");
                    if (northPlateau != null)
                    {
                        northPlateau.position = new Vector3(14.00f, -11.00f, 56.00f);
                        northPlateau.localScale = new Vector3(110.00f, 16.00f, 95.00f);
                    }

                    // 2.5 South Refinery & Power Substation Shelf (110m x 95m)
                    Transform southShelf = mainRock.Find("South_Refinery_Support_Shelf");
                    if (southShelf != null)
                    {
                        southShelf.position = new Vector3(18.00f, -11.00f, -14.00f);
                        southShelf.localScale = new Vector3(110.00f, 16.00f, 95.00f);
                    }

                    // 2.6 ELIMINATE PROTRUDING OLD CLIFF ROCKS
                    string[] oldCliffs = new string[] { "NorthEast_Cliff_Outcrop", "NorthWest_Cliff_Outcrop", "SouthEast_Cliff_Shoulder", "South_Industrial_Ledge", "West_Landing_Promontory_Rock" };
                    foreach (var cn in oldCliffs)
                    {
                        Transform ct = mainRock.Find(cn);
                        if (ct != null) ct.position = new Vector3(ct.position.x, -12.0f, ct.position.z);
                    }
                }

                // Expand Underbelly Cones
                Transform cliffs = terrain.Find("RockCliffs");
                if (cliffs != null)
                {
                    Transform primaryCone = cliffs.Find("Underbelly_Primary_Cone");
                    if (primaryCone != null) primaryCone.localScale = new Vector3(150.0f, 44.0f, 130.0f);
                    Transform eastCone = cliffs.Find("Underbelly_East_Observatory_Cone");
                    if (eastCone != null) eastCone.localScale = new Vector3(110.0f, 36.0f, 105.0f);
                }

                // AUTOMATED SAFETY ENFORCEMENT: Clamp all rock surfaces in Terrain to strictly <= -0.20m!
                foreach (var r in terrain.GetComponentsInChildren<Renderer>())
                {
                    if (r.bounds.max.y > -0.20f)
                    {
                        float excess = r.bounds.max.y - (-0.20f);
                        r.transform.position += Vector3.down * (excess + 0.05f);
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 3. LOAD SCI-FI ASSETS & MATERIALS
            // ─────────────────────────────────────────────────────────────────
            GameObject windowBigPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Windows/Complete Windows/window_big.prefab")
                                      ?? LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Windows/window_big.prefab");
            GameObject cornerFarPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Windows/Complete Windows/window_big_corner_far.prefab")
                                      ?? LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Windows/window_big_corner_far.prefab");
            GameObject doorBlockerPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Windows/window_big_blocker_3.prefab");
            GameObject wallBigPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Walls/wall_big.prefab");
            GameObject columnPillar = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_end.prefab");
            GameObject doorPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Doors/door_1.prefab")
                                 ?? LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Doors/door_2.prefab");
            GameObject floorTile = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Floors/floor_2.prefab");
            GameObject floorEmissiveTile = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Floors/floor_1.prefab");
            GameObject stairsEmissive = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Stairs/stairs_big_with_emmision.prefab")
                                     ?? LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Stairs/stairs_small_with_emmision.prefab");

            Material darkTitaniumMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_DarkTitanium.mat");
            Material steelAccentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_SteelAccent.mat");
            Material coreGlowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_CoreReactor.mat");
            Material cyanEmissiveMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_BlueCyan.mat");
            Material holoGlassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/AboutStation/Materials/Mat_Station_HoloGlass.mat");
            Material beamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Hologram_Projection_Beam.mat");
            Material oculusMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Ceiling_Holo_Oculus.mat");
            Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Cosmic_Smoke.mat");
            Material baseColorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Sci-Fi Styled Modular Pack/Materials/base color.mat");

            // Hardware & Props
            GameObject satelliteLarge = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/satelliteDish_large.fbx");
            GameObject satelliteDish = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/satelliteDish.fbx");
            GameObject satelliteDishDetailed = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/satelliteDish_detailed.fbx");
            GameObject turretDouble = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/turret_double.fbx");
            GameObject turretSingle = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/turret_single.fbx");
            GameObject rooftopSpeeder = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/craft_speederA.fbx");
            GameObject commMast = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/machine_wirelessCable.fbx");
            GameObject pipeStraight = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/pipe_straight.fbx");
            GameObject pipeCorner = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/pipe_corner.fbx");
            GameObject generatorLarge = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/machine_generatorLarge.fbx");
            GameObject chimneyDetailed = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/chimney_detailed.fbx");
            GameObject roverModel = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/rover.fbx");
            GameObject crystalLarge = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/rock_crystalsLargeA.fbx");

            GameObject projectorPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/projector_2.prefab")
                                      ?? LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/projector.prefab");
            GameObject consolePrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console.prefab");
            GameObject consoleScreenPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console_screen.prefab");
            GameObject compStation = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab");
            GameObject bigScreen = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab");
            GameObject pilotSeat = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/pilot_seat.prefab");
            GameObject sofa = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_sofa.prefab");
            GameObject chair = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab");
            GameObject tableGlass = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_table_glass.prefab");
            GameObject cabinetL = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet_L.prefab");
            GameObject ceilingLight = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_celing_1.prefab");
            GameObject corridorPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Corridors/Corridor_1.prefab");

            // Exterior Heavy Machinery Prefabs
            GameObject batteryBig = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_big.prefab");
            GameObject batteryMedium = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_medium.prefab");
            GameObject capacitorPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Capacitor.prefab");
            GameObject generatorPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab");
            GameObject containerBig = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/container_big.prefab");
            GameObject containerSmall = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/container_small.prefab");
            GameObject storageContainerBig = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab");
            GameObject storageContainerSmall = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab");
            GameObject floorCornerPipes = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes.prefab");
            GameObject floorCornerPipesTurn = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes_corner.prefab");
            GameObject torpedoRack = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/torpedo_docking_station_1_side.prefab");
            GameObject wallShelf = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/shelf.prefab");
            GameObject consoleCeiling = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console_celing.prefab");
            GameObject machineBarrelLarge = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/machine_barrelLarge.fbx");
            GameObject barrelsCluster = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/barrels.fbx");
            GameObject pipeRoundLarge = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/pipe_cornerRoundLarge.fbx");
            GameObject pipeCross = LoadPrefab("Assets/_Project/kenney_space-kit/Models/FBX format/pipe_cross.fbx");
            GameObject cableThick = LoadPrefab("Assets/_Project/Platforms/Cable_Thick.fbx")
                                 ?? LoadPrefab("Assets/_Project/Blends/Cable_Thick.blend")
                                 ?? LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Models/FBX/Interior/Cable_Thick.fbx");
            Material hazardStripeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_HazardStripe.mat");

            // ─────────────────────────────────────────────────────────────────
            // 4. CLEAN & RESTRUCTURE OBSERVATORY COMPLEX & MAIN BUILDING
            // ─────────────────────────────────────────────────────────────────
            Transform obsComplex = aboutIsland.transform.Find("ObservatoryComplex");
            if (obsComplex == null)
            {
                var ocGo = new GameObject("ObservatoryComplex");
                ocGo.transform.SetParent(aboutIsland.transform, false);
                obsComplex = ocGo.transform;
            }
            obsComplex.localPosition = Vector3.zero;

            Transform mainBuilding = obsComplex.Find("MainBuilding");
            if (mainBuilding == null)
            {
                var mbGo = new GameObject("MainBuilding");
                mbGo.transform.SetParent(obsComplex, false);
                mainBuilding = mbGo.transform;
            }
            mainBuilding.localPosition = Vector3.zero;

            var oldChildren = new List<GameObject>();
            foreach (Transform c in mainBuilding) oldChildren.Add(c.gameObject);
            foreach (var go in oldChildren)
            {
                if (Application.isPlaying) Object.Destroy(go);
                else Object.DestroyImmediate(go);
            }

            // Relocate Industrial Area Outdoors (Zero machines indoors!)
            RelocateIndustrialAreaOutdoors(aboutIsland);

            // ─────────────────────────────────────────────────────────────────
            // 5. CITADEL INTERIOR FLOORS & CONTINUOUS ARMORED SUB-FLOOR
            // Fixes "thiếu mặt sàn" & "dùng một chất liệu đồng đều"
            // ─────────────────────────────────────────────────────────────────
            GameObject floorsGroup = new GameObject("Citadel_Interior_Floors");
            floorsGroup.transform.SetParent(mainBuilding, false);

            HashSet<Vector2Int> floorGrid = new HashSet<Vector2Int>();

            // 5.1 Central Command Rotunda (X: 0 to 28, Z: 11 to 35)
            for (int x = 0; x <= 28; x += 4)
                for (int z = 11; z <= 35; z += 4)
                    floorGrid.Add(new Vector2Int(x, z));

            // 5.2 West Gatehouse Portico (X: -10 to -2, Z: 19 to 27)
            for (int x = -10; x <= -2; x += 4)
                for (int z = 19; z <= 27; z += 4)
                    floorGrid.Add(new Vector2Int(x, z));

            // 5.3 North Mission Control & Science Wing (X: 2 to 26, Z: 37 to 49)
            for (int x = 2; x <= 26; x += 4)
                for (int z = 37; z <= 49; z += 4)
                    floorGrid.Add(new Vector2Int(x, z));

            // 5.4 East Observation Wing (X: 30 to 42, Z: 19 to 27)
            for (int x = 30; x <= 42; x += 4)
                for (int z = 19; z <= 27; z += 4)
                    floorGrid.Add(new Vector2Int(x, z));

            if (floorTile != null)
            {
                foreach (var pos in floorGrid)
                {
                    bool isGrandAvenue = (pos.y >= 21 && pos.y <= 25 && pos.x <= 16);
                    GameObject tilePrefab = (isGrandAvenue && floorEmissiveTile != null) ? floorEmissiveTile : floorTile;
                    var fl = Object.Instantiate(tilePrefab, floorsGroup.transform, false);
                    fl.name = $"Floor_{pos.x}_{pos.y}";
                    fl.transform.position = new Vector3(pos.x, 0.09f, pos.y);
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 6. CITADEL EXTERIOR WALLS & TRUE STRUCTURAL CORNER COLUMNS
            // Fixes Image 2 & Image 10 (Zero Corner Slits!) & Image 3 (Aligned Columns)
            // ─────────────────────────────────────────────────────────────────
            GameObject wallsGroup = new GameObject("Citadel_Exterior_Walls");
            wallsGroup.transform.SetParent(mainBuilding, false);

            float[] tierY = new float[] { 0.10f, 4.10f };

            System.Action<GameObject, Vector3, float, string> spawnWall = delegate(GameObject prefab, Vector3 pos, float rotY, string name)
            {
                if (prefab == null) return;
                var w = Object.Instantiate(prefab, wallsGroup.transform, false);
                w.name = name;
                w.transform.position = pos;
                w.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            };

            for (int t = 0; t < 2; t++)
            {
                float y = tierY[t];

                // ── 6.1 WEST GATEHOUSE PORTICO (Entrance at X = -10.0m) ──
                if (t == 0)
                {
                    // Grand Sci-Fi Airlock Portal with Automatic Door System
                    if (doorPrefab != null)
                    {
                        var doorGo = Object.Instantiate(doorPrefab, wallsGroup.transform, false);
                        doorGo.name = "West_Gate_Airlock_Portal";
                        doorGo.transform.position = new Vector3(-10.0f, y, 23.0f);
                        doorGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        var autoDoor = doorGo.GetComponent<CitadelAutomaticDoor>() ?? doorGo.AddComponent<CitadelAutomaticDoor>();
                        autoDoor.doorLeft = doorGo.transform.Find("door_1_left");
                        autoDoor.doorRight = doorGo.transform.Find("door_1_right");
                        autoDoor.animator = doorGo.GetComponent<Animator>();
                        if (autoDoor.animator != null) autoDoor.animator.enabled = false;
                        autoDoor.cyanLedMat = cyanEmissiveMat;
                        autoDoor.amberLedMat = coreGlowMat;
                        autoDoor.triggerDistance = 7.0f;
                        autoDoor.closeDistance = 10.0f;
                        if (autoDoor.doorLeft != null) autoDoor.doorLeft.localPosition = new Vector3(1.385954f, 0.005685329f, 0f);
                        if (autoDoor.doorRight != null) autoDoor.doorRight.localPosition = new Vector3(-1.409172f, 0f, 0f);
                        autoDoor.SetOpenImmediate(0f);

                        // Architectural Portal Sealing Casing - closes 1.15m exterior wall gap completely!
                        BuildAirlockPortalCasing(wallsGroup.transform, new Vector3(-10.0f, y, 23.0f), 90f, baseColorMat, darkTitaniumMat);
                    }
                    spawnWall(windowBigPrefab, new Vector3(-10f, y, 19f), 90f, $"West_Gate_South_T{t}");
                    spawnWall(windowBigPrefab, new Vector3(-10f, y, 27f), 90f, $"West_Gate_North_T{t}");

                    // Monumental Portal Frame Columns
                    if (columnPillar != null)
                    {
                        var colS = Object.Instantiate(columnPillar, wallsGroup.transform, false);
                        colS.name = "Portal_Column_South";
                        colS.transform.position = new Vector3(-10.0f, y, 21.0f);

                        var colN = Object.Instantiate(columnPillar, wallsGroup.transform, false);
                        colN.name = "Portal_Column_North";
                        colN.transform.position = new Vector3(-10.0f, y, 25.0f);
                    }
                }
                else
                {
                    spawnWall(windowBigPrefab, new Vector3(-10f, y, 19f), 90f, $"West_Transom_South_T{t}");
                    spawnWall(windowBigPrefab, new Vector3(-10f, y, 23f), 90f, $"West_Transom_Center_T{t}");
                    spawnWall(windowBigPrefab, new Vector3(-10f, y, 27f), 90f, $"West_Transom_North_T{t}");
                }

                // Portico Flank Walls (X: -10 to -2 at Z = 17 and Z = 29)
                spawnWall(windowBigPrefab, new Vector3(-8f, y, 17f), 0f, $"Portico_South_X-8_T{t}");
                spawnWall(windowBigPrefab, new Vector3(-4f, y, 17f), 0f, $"Portico_South_X-4_T{t}");
                spawnWall(windowBigPrefab, new Vector3(-8f, y, 29f), 180f, $"Portico_North_X-8_T{t}");
                spawnWall(windowBigPrefab, new Vector3(-4f, y, 29f), 180f, $"Portico_North_X-4_T{t}");

                // Central Hall West Facade Returns (at X = -2.0m)
                spawnWall(windowBigPrefab, new Vector3(-2f, y, 11f), 90f, $"Center_West_Z11_T{t}");
                spawnWall(windowBigPrefab, new Vector3(-2f, y, 15f), 90f, $"Center_West_Z15_T{t}");
                spawnWall(windowBigPrefab, new Vector3(-2f, y, 31f), 90f, $"Center_West_Z31_T{t}");
                spawnWall(windowBigPrefab, new Vector3(-2f, y, 35f), 90f, $"Center_West_Z35_T{t}");

                // ── 6.2 SOUTH CITADEL FACADE (Z = 9.0m, X: -2 to 30) ──
                for (int x = 0; x <= 28; x += 4)
                {
                    spawnWall(windowBigPrefab, new Vector3(x, y, 9.0f), 0f, $"South_Facade_X{x}_T{t}");
                }

                // ── 6.3 NORTH MISSION CONTROL & SCIENCE WING ──
                // North Hall Returns at Z = 37.0m
                spawnWall(windowBigPrefab, new Vector3(0f, y, 37f), 180f, $"North_Hall_Return_W_T{t}");
                spawnWall(windowBigPrefab, new Vector3(28f, y, 37f), 180f, $"North_Hall_Return_E_T{t}");

                // North Wing West Wall (X = 2.0m, Z: 37 to 49)
                spawnWall(windowBigPrefab, new Vector3(2f, y, 39f), 90f, $"North_Wing_West_Z39_T{t}");
                spawnWall(windowBigPrefab, new Vector3(2f, y, 43f), 90f, $"North_Wing_West_Z43_T{t}");
                spawnWall(windowBigPrefab, new Vector3(2f, y, 47f), 90f, $"North_Wing_West_Z47_T{t}");

                // North Wing End Wall (Z = 49.0m, X: 2 to 26)
                for (int x = 4; x <= 24; x += 4)
                {
                    spawnWall(windowBigPrefab, new Vector3(x, y, 49.0f), 180f, $"North_Wing_End_X{x}_T{t}");
                }

                // North Wing East Wall (X = 26.0m, Z: 37 to 49)
                spawnWall(windowBigPrefab, new Vector3(26f, y, 47f), 270f, $"North_Wing_East_Z47_T{t}");
                spawnWall(windowBigPrefab, new Vector3(26f, y, 43f), 270f, $"North_Wing_East_Z43_T{t}");
                spawnWall(windowBigPrefab, new Vector3(26f, y, 39f), 270f, $"North_Wing_East_Z39_T{t}");

                // ── 6.4 EAST OBSERVATION WING ──
                // Central Hall East Facade Returns (at X = 30.0m)
                spawnWall(windowBigPrefab, new Vector3(30f, y, 11f), 270f, $"East_Hall_Return_S11_T{t}");
                spawnWall(windowBigPrefab, new Vector3(30f, y, 15f), 270f, $"East_Hall_Return_S15_T{t}");
                spawnWall(windowBigPrefab, new Vector3(30f, y, 31f), 270f, $"East_Hall_Return_N31_T{t}");
                spawnWall(windowBigPrefab, new Vector3(30f, y, 35f), 270f, $"East_Hall_Return_N35_T{t}");

                // East Wing South Wall (Z = 17.0m, X: 30 to 42)
                spawnWall(windowBigPrefab, new Vector3(32f, y, 17f), 0f, $"East_Wing_South_X32_T{t}");
                spawnWall(windowBigPrefab, new Vector3(36f, y, 17f), 0f, $"East_Wing_South_X36_T{t}");
                spawnWall(windowBigPrefab, new Vector3(40f, y, 17f), 0f, $"East_Wing_South_X40_T{t}");

                // East Wing End Wall (X = 42.0m, Z: 17 to 29)
                spawnWall(windowBigPrefab, new Vector3(42f, y, 19f), 270f, $"East_Wing_End_Z19_T{t}");
                spawnWall(windowBigPrefab, new Vector3(42f, y, 23f), 270f, $"East_Wing_End_Z23_T{t}");
                spawnWall(windowBigPrefab, new Vector3(42f, y, 27f), 270f, $"East_Wing_End_Z27_T{t}");

                // East Wing North Wall (Z = 29.0m, X: 30 to 42)
                spawnWall(windowBigPrefab, new Vector3(40f, y, 29f), 180f, $"East_Wing_North_X40_T{t}");
                spawnWall(windowBigPrefab, new Vector3(36f, y, 29f), 180f, $"East_Wing_North_X36_T{t}");
                spawnWall(windowBigPrefab, new Vector3(32f, y, 29f), 180f, $"East_Wing_North_X32_T{t}");

                // East Chamber Entrance Facade & Door Threshold (at X = 30.0m)
                spawnWall(wallBigPrefab, new Vector3(30f, y, 19f), 270f, $"East_Chamber_Wall_S19_T{t}");
                spawnWall(wallBigPrefab, new Vector3(30f, y, 27f), 270f, $"East_Chamber_Wall_N27_T{t}");

                if (t == 1)
                {
                    spawnWall(wallBigPrefab, new Vector3(30f, y, 23f), 270f, "East_Chamber_Entrance_Transom");
                }
                else if (t == 0 && doorPrefab != null)
                {
                    var doorEastGo = Object.Instantiate(doorPrefab, wallsGroup.transform, false);
                    doorEastGo.name = "East_Chamber_Airlock_Door";
                    doorEastGo.transform.position = new Vector3(30.0f, 0.10f, 23.0f);
                    doorEastGo.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

                    var autoDoorEast = doorEastGo.GetComponent<CitadelAutomaticDoor>() ?? doorEastGo.AddComponent<CitadelAutomaticDoor>();
                    autoDoorEast.doorLeft = doorEastGo.transform.Find("door_1_left");
                    autoDoorEast.doorRight = doorEastGo.transform.Find("door_1_right");
                    autoDoorEast.animator = doorEastGo.GetComponent<Animator>();
                    if (autoDoorEast.animator != null) autoDoorEast.animator.enabled = false;
                    autoDoorEast.cyanLedMat = cyanEmissiveMat;
                    autoDoorEast.amberLedMat = coreGlowMat;
                    autoDoorEast.autoProximity = true;
                    autoDoorEast.triggerDistance = 7.0f;
                    autoDoorEast.closeDistance = 10.0f;
                    if (autoDoorEast.doorLeft != null) autoDoorEast.doorLeft.localPosition = new Vector3(1.385954f, 0.005685329f, 0f);
                    if (autoDoorEast.doorRight != null) autoDoorEast.doorRight.localPosition = new Vector3(-1.409172f, 0f, 0f);
                    autoDoorEast.SetOpenImmediate(0f);

                    // Architectural Portal Sealing Casing - closes 1.15m exterior wall gap completely!
                    BuildAirlockPortalCasing(wallsGroup.transform, new Vector3(30.0f, 0.10f, 23.0f), 270f, baseColorMat, darkTitaniumMat);

                    if (columnPillar != null)
                    {
                        for (int tPortal = 0; tPortal <= 1; tPortal++)
                        {
                            float yPortal = 0.10f + tPortal * 4.0f;
                            var colES = Object.Instantiate(columnPillar, wallsGroup.transform, false);
                            colES.name = $"East_Portal_Col_South_T{tPortal}";
                            colES.transform.position = new Vector3(30.0f, yPortal, 21.0f);

                            var colEN = Object.Instantiate(columnPillar, wallsGroup.transform, false);
                            colEN.name = $"East_Portal_Col_North_T{tPortal}";
                            colEN.transform.position = new Vector3(30.0f, yPortal, 25.0f);
                        }
                    }
                }
            }

            // ── 6.5 MODULAR SEAMLESS CORNERS (100% ASSET PACK CORNER PREFABS - NO GAPS, NO CLIPPING) ──
            var cornerPositions = new Vector3[] {
                new Vector3(2.0f, 0.10f, 49.0f),
                new Vector3(26.0f, 0.10f, 49.0f),
                new Vector3(42.0f, 0.10f, 29.0f),
                new Vector3(42.0f, 0.10f, 17.0f),
                new Vector3(30.0f, 0.10f, 9.0f),
                new Vector3(-2.0f, 0.10f, 9.0f),
                new Vector3(-10.0f, 0.10f, 17.0f),
                new Vector3(-10.0f, 0.10f, 29.0f),
                new Vector3(30.0f, 0.10f, 37.0f),
                new Vector3(-2.0f, 0.10f, 37.0f)
            };

            var cornerRotations = new float[] {
                90f,
                180f,
                180f,
                270f,
                270f,
                0f,
                0f,
                90f,
                180f,
                90f
            };

            var cornerNames = new string[] {
                "NorthWing_NW",
                "NorthWing_NE",
                "EastWing_NE",
                "EastWing_SE",
                "Rotunda_SE",
                "Rotunda_SW",
                "WestGate_SW",
                "WestGate_NW",
                "Rotunda_NE",
                "Rotunda_NW"
            };

            if (cornerFarPrefab != null)
            {
                for (int i = 0; i < cornerPositions.Length; i++)
                {
                    var pos = cornerPositions[i];
                    var rot = cornerRotations[i];
                    var name = cornerNames[i];
                    for (int t = 0; t <= 1; t++)
                    {
                        float y = pos.y + t * 4.0f;
                        var cGo = Object.Instantiate(cornerFarPrefab, wallsGroup.transform, false);
                        cGo.name = $"Corner_Far_{name}_T{t}";
                        cGo.transform.position = new Vector3(pos.x, y, pos.z);
                        cGo.transform.rotation = Quaternion.Euler(0f, rot, 0f);
                    }
                }
            }

            // ── 6.6 INSIDE CORNER FINISH COLUMNS (SEAMLESS 90-DEGREE JUNCTIONS - ZERO CLIPPING) ──
            if (columnPillar != null)
            {
                var insideCorners = new (float x, float z, string name)[] {
                    (2f, 37f, "NorthWing_West_Return"),
                    (26f, 37f, "NorthWing_East_Return"),
                    (-2f, 17f, "Portico_South_Return"),
                    (-2f, 29f, "Portico_North_Return"),
                    (30f, 17f, "EastWing_South_Return"),
                    (30f, 29f, "EastWing_North_Return")
                };
                foreach (var ic in insideCorners)
                {
                    for (int t = 0; t <= 1; t++)
                    {
                        float y = 0.10f + t * 4.0f;
                        var col = Object.Instantiate(columnPillar, wallsGroup.transform, false);
                        col.name = $"Inside_Corner_Col_{ic.name}_T{t}";
                        col.transform.position = new Vector3(ic.x, y, ic.z);
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 7. STRUCTURAL ROOF DECK & CENTRAL COMMAND CROWN TOWER
            // ─────────────────────────────────────────────────────────────────
            GameObject roofGroup = new GameObject("Citadel_Upper_RoofDeck");
            roofGroup.transform.SetParent(mainBuilding, false);

            float roofY = 8.10f;
            if (floorTile != null)
            {
                foreach (var pos in floorGrid)
                {
                    var rTile = Object.Instantiate(floorTile, roofGroup.transform, false);
                    rTile.name = $"Roof_{pos.x}_{pos.y}";
                    rTile.transform.position = new Vector3(pos.x, roofY, pos.y);
                }
            }

            GameObject crownTower = new GameObject("Central_Command_Upper_Tower");
            crownTower.transform.SetParent(mainBuilding, false);

            float crownRoofY = 12.10f;
            for (int x = 6; x <= 22; x += 4)
            {
                spawnWall(windowBigPrefab, new Vector3(x, roofY, 13f), 0f, $"Crown_South_X{x}");
                spawnWall(windowBigPrefab, new Vector3(x, roofY, 33f), 180f, $"Crown_North_X{x}");
            }
            for (int z = 15; z <= 31; z += 4)
            {
                spawnWall(windowBigPrefab, new Vector3(4f, roofY, z), 90f, $"Crown_West_Z{z}");
                spawnWall(windowBigPrefab, new Vector3(24f, roofY, z), 270f, $"Crown_East_Z{z}");
            }
            // 4 Crown Tower Corner Seal Modules (100% Asset Pack window_big_corner_far - ZERO primitive cubes)
            Vector3[] crownCorners = new Vector3[]
            {
                new Vector3(4f, roofY, 13f),
                new Vector3(24f, roofY, 13f),
                new Vector3(24f, roofY, 33f),
                new Vector3(4f, roofY, 33f)
            };
            float[] crownCornerRots = new float[] { 0f, 270f, 180f, 90f };
            if (cornerFarPrefab != null)
            {
                for (int i = 0; i < crownCorners.Length; i++)
                {
                    var cGo = Object.Instantiate(cornerFarPrefab, crownTower.transform, false);
                    cGo.name = $"Crown_Corner_Far_{i}";
                    cGo.transform.position = crownCorners[i];
                    cGo.transform.rotation = Quaternion.Euler(0f, crownCornerRots[i], 0f);
                }
            }

            if (floorTile != null)
            {
                for (int x = 6; x <= 22; x += 4)
                    for (int z = 15; z <= 31; z += 4)
                    {
                        var tFloor = Object.Instantiate(floorTile, crownTower.transform, false);
                        tFloor.name = $"Crown_Roof_{x}_{z}";
                        tFloor.transform.position = new Vector3(x, crownRoofY, z);
                    }
            }

            // ─────────────────────────────────────────────────────────────────
            // 8. ROOFTOP SUPERSTRUCTURE, VTOL SPEEDER PAD & HEAVY DEFENSE ARRAYS
            // Fixes Image 2 ("ở trên đang còn khá trống hãy trang trí thêm")
            // ─────────────────────────────────────────────────────────────────
            GameObject rooftopSuperstructure = new GameObject("Rooftop_Superstructure_Systems");
            rooftopSuperstructure.transform.SetParent(mainBuilding, false);

            // Universal Illuminated Sci-Fi Balustrade Helper (Used for Rooftop & Courtyard)
            System.Action<Transform, Vector3, float, string, bool> spawnBalustrade = delegate(Transform parent, Vector3 pos, float rotY, string name, bool withLight)
            {
                var balRoot = new GameObject(name);
                balRoot.transform.SetParent(parent, false);
                balRoot.transform.position = pos;
                balRoot.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

                // Titanium Base Plinth
                var basePlinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                basePlinth.name = "Plinth";
                basePlinth.transform.SetParent(balRoot.transform, false);
                basePlinth.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                basePlinth.transform.localScale = new Vector3(4.0f, 0.16f, 0.20f);
                Object.DestroyImmediate(basePlinth.GetComponent<Collider>());
                if (darkTitaniumMat != null) basePlinth.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

                // Translucent Cyan Unlit Glass Panel
                var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glass.name = "Glass_Panel";
                glass.transform.SetParent(balRoot.transform, false);
                glass.transform.localPosition = new Vector3(0f, 0.58f, 0f);
                glass.transform.localScale = new Vector3(3.8f, 0.84f, 0.06f);
                Object.DestroyImmediate(glass.GetComponent<Collider>());
                if (holoGlassMat != null) glass.GetComponent<Renderer>().sharedMaterial = holoGlassMat;

                // Glowing Neon Cyan Top Handrail
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rail.name = "Neon_Handrail";
                rail.transform.SetParent(balRoot.transform, false);
                rail.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                rail.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                rail.transform.localScale = new Vector3(0.12f, 2.0f, 0.12f);
                Object.DestroyImmediate(rail.GetComponent<Collider>());
                if (cyanEmissiveMat != null) rail.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                // End Stanchions
                Vector3[] postX = new Vector3[] { new Vector3(-1.95f, 0.55f, 0f), new Vector3(1.95f, 0.55f, 0f) };
                foreach (var px in postX)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "Post";
                    post.transform.SetParent(balRoot.transform, false);
                    post.transform.localPosition = px;
                    post.transform.localScale = new Vector3(0.14f, 1.10f, 0.18f);
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                    if (darkTitaniumMat != null) post.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;
                }

                if (withLight)
                {
                    var lt = balRoot.AddComponent<Light>();
                    lt.type = LightType.Point;
                    lt.range = 5.0f;
                    lt.intensity = 1.6f;
                    lt.color = new Color(0.2f, 0.95f, 1.0f);
                }
            };

            // ── 8.1 CENTRAL COMMAND CROWN TOWER ROOFTOP (Y = 12.10m) ──
            Transform glassDome = obsComplex.Find("GlassDome");
            if (glassDome != null) glassDome.position = new Vector3(14.0f, 12.15f, 23.0f);

            var spireRoot = new GameObject("Citadel_Power_Spire_Tower");
            spireRoot.transform.SetParent(rooftopSuperstructure.transform, false);
            spireRoot.transform.position = new Vector3(14.0f, 14.50f, 23.0f);

            var spireCol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spireCol.name = "Central_Spire_Core";
            spireCol.transform.SetParent(spireRoot.transform, false);
            spireCol.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            spireCol.transform.localScale = new Vector3(0.6f, 2.5f, 0.6f);
            Object.DestroyImmediate(spireCol.GetComponent<Collider>());
            if (darkTitaniumMat != null) spireCol.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

            Vector3[] rodOffsets = new Vector3[]
            {
                new Vector3(-0.75f, 2.5f, -0.75f),
                new Vector3(0.75f, 2.5f, -0.75f),
                new Vector3(-0.75f, 2.5f, 0.75f),
                new Vector3(0.75f, 2.5f, 0.75f)
            };
            foreach (var ro in rodOffsets)
            {
                var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rod.name = "Energy_Conduit_Rod";
                rod.transform.SetParent(spireRoot.transform, false);
                rod.transform.localPosition = ro;
                rod.transform.localScale = new Vector3(0.18f, 2.2f, 0.18f);
                Object.DestroyImmediate(rod.GetComponent<Collider>());
                if (cyanEmissiveMat != null) rod.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            var spireLight = spireRoot.AddComponent<Light>();
            spireLight.type = LightType.Point;
            spireLight.range = 18f;
            spireLight.intensity = 4.5f;
            spireLight.color = new Color(0.15f, 0.95f, 1.0f);

            var spireColComp = spireRoot.AddComponent<CapsuleCollider>();
            spireColComp.center = new Vector3(0f, 2.5f, 0f);
            spireColComp.radius = 1.6f;
            spireColComp.height = 5.5f;
            var spireCore = spireRoot.AddComponent<CitadelPowerReactorCore>();
            spireCore.maxHealth = 160f;
            spireCore.currentHealth = 160f;
            spireCore.coreSphere = spireCol.transform;
            spireCore.coreLight = spireLight;

            // Crown Tactical Scanner Dish
            if (satelliteDishDetailed != null)
            {
                var crownDish = Object.Instantiate(satelliteDishDetailed, rooftopSuperstructure.transform, false);
                crownDish.name = "Crown_Tactical_Scanner";
                crownDish.transform.position = new Vector3(8.0f, crownRoofY, 27.0f);
                crownDish.transform.rotation = Quaternion.Euler(-20f, 120f, 0f);
                crownDish.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            }

            // Crown Comms Relay Mast
            if (commMast != null)
            {
                var cMast = Object.Instantiate(commMast, rooftopSuperstructure.transform, false);
                cMast.name = "Crown_Comm_Array";
                cMast.transform.position = new Vector3(20.0f, crownRoofY, 19.0f);
                cMast.transform.rotation = Quaternion.identity;
                cMast.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            }

            // Crown 4 Corner Aviation Warning Beacons
            Vector3[] crownBeaconPos = new Vector3[]
            {
                new Vector3(6f, crownRoofY, 15f),
                new Vector3(22f, crownRoofY, 15f),
                new Vector3(22f, crownRoofY, 31f),
                new Vector3(6f, crownRoofY, 31f)
            };
            foreach (var bp in crownBeaconPos)
            {
                var bPylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bPylon.name = "Aviation_Beacon_Pylon";
                bPylon.transform.SetParent(rooftopSuperstructure.transform, false);
                bPylon.transform.position = bp + Vector3.up * 0.4f;
                bPylon.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);
                Object.DestroyImmediate(bPylon.GetComponent<Collider>());
                if (darkTitaniumMat != null) bPylon.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

                var bLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bLight.name = "Aviation_Strobe";
                bLight.transform.SetParent(rooftopSuperstructure.transform, false);
                bLight.transform.position = bp + Vector3.up * 0.85f;
                bLight.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                Object.DestroyImmediate(bLight.GetComponent<Collider>());
                if (coreGlowMat != null) bLight.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

                var lt = bLight.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 6.0f;
                lt.intensity = 2.0f;
                lt.color = new Color(1.0f, 0.55f, 0.1f);
            }

            // Crown Deck Balustrades
            for (int x = 6; x <= 22; x += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(x, crownRoofY, 13.0f), 0f, $"Crown_Bal_South_X{x}", false);
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(x, crownRoofY, 33.0f), 180f, $"Crown_Bal_North_X{x}", false);
            }
            for (int z = 15; z <= 31; z += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(4.0f, crownRoofY, z), 90f, $"Crown_Bal_West_Z{z}", false);
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(24.0f, crownRoofY, z), 270f, $"Crown_Bal_East_Z{z}", false);
            }

            // ── 8.2 EAST WING ROOFTOP VTOL / RECON SPEEDER LANDING PAD (X = 36m, Y = 8.10m, Z = 23m) ──
            var padPlinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padPlinth.name = "Rooftop_Helipad_Plinth";
            padPlinth.transform.SetParent(rooftopSuperstructure.transform, false);
            padPlinth.transform.position = new Vector3(36.0f, roofY + 0.04f, 23.0f);
            padPlinth.transform.localScale = new Vector3(7.5f, 0.08f, 7.5f);
            Object.DestroyImmediate(padPlinth.GetComponent<Collider>());
            if (darkTitaniumMat != null) padPlinth.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

            var padRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padRing.name = "Rooftop_Helipad_Ring";
            padRing.transform.SetParent(rooftopSuperstructure.transform, false);
            padRing.transform.position = new Vector3(36.0f, roofY + 0.06f, 23.0f);
            padRing.transform.localScale = new Vector3(6.5f, 0.02f, 6.5f);
            Object.DestroyImmediate(padRing.GetComponent<Collider>());
            if (cyanEmissiveMat != null) padRing.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var padCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padCore.name = "Rooftop_Helipad_Core";
            padCore.transform.SetParent(rooftopSuperstructure.transform, false);
            padCore.transform.position = new Vector3(36.0f, roofY + 0.07f, 23.0f);
            padCore.transform.localScale = new Vector3(5.7f, 0.02f, 5.7f);
            Object.DestroyImmediate(padCore.GetComponent<Collider>());
            if (darkTitaniumMat != null) padCore.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

            if (rooftopSpeeder != null)
            {
                var speeder = Object.Instantiate(rooftopSpeeder, rooftopSuperstructure.transform, false);
                speeder.name = "Rooftop_Recon_Speeder";
                speeder.transform.position = new Vector3(36.0f, roofY + 0.12f, 23.0f);
                speeder.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                speeder.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            }

            if (commMast != null)
            {
                var rechg = Object.Instantiate(commMast, rooftopSuperstructure.transform, false);
                rechg.name = "Speeder_Recharging_Pylon";
                rechg.transform.position = new Vector3(40.5f, roofY, 26.5f);
                rechg.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                rechg.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            }

            // East Wing Balustrades (Z = 17 for south, Z = 29 for north, X = 42 for east end)
            for (int x = 32; x <= 40; x += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(x, roofY, 17.0f), 0f, $"East_Roof_Bal_South_X{x}", false);
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(x, roofY, 29.0f), 180f, $"East_Roof_Bal_North_X{x}", false);
            }
            for (int z = 19; z <= 27; z += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(42.0f, roofY, z), 270f, $"East_Roof_Bal_East_Z{z}", false);
            }

            // ── 8.3 NORTH WING ROOFTOP SCIENTIFIC ARRAY & DEFENSE BATTERY (Y = 8.10m) ──
            if (satelliteLarge != null)
            {
                var dish = Object.Instantiate(satelliteLarge, rooftopSuperstructure.transform, false);
                dish.name = "DeepSpace_HighGain_Array";
                dish.transform.position = new Vector3(14.0f, roofY, 47.0f);
                dish.transform.rotation = Quaternion.Euler(-25f, 0f, 0f);
                dish.transform.localScale = new Vector3(2.8f, 2.8f, 2.8f);
            }
            if (satelliteDishDetailed != null)
            {
                var dishDetail = Object.Instantiate(satelliteDishDetailed, rooftopSuperstructure.transform, false);
                dishDetail.name = "North_Tactical_Microwave_Dish";
                dishDetail.transform.position = new Vector3(6.0f, roofY, 41.0f);
                dishDetail.transform.rotation = Quaternion.Euler(-15f, 45f, 0f);
                dishDetail.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            }
            if (turretDouble != null)
            {
                var tBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tBase.name = "Turret_Armored_Plinth";
                tBase.transform.SetParent(rooftopSuperstructure.transform, false);
                tBase.transform.position = new Vector3(22.0f, roofY + 0.15f, 46.0f);
                tBase.transform.localScale = new Vector3(3.2f, 0.30f, 3.2f);
                Object.DestroyImmediate(tBase.GetComponent<Collider>());
                if (darkTitaniumMat != null) tBase.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

                var tN = Object.Instantiate(turretDouble, rooftopSuperstructure.transform, false);
                tN.name = "Defense_Turret_North";
                tN.transform.position = new Vector3(22.0f, roofY + 0.35f, 46.0f);
                tN.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                tN.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            }
            if (generatorLarge != null)
            {
                var gen = Object.Instantiate(generatorLarge, rooftopSuperstructure.transform, false);
                gen.name = "Atmospheric_HVAC_Generator";
                gen.transform.position = new Vector3(8.0f, roofY, 44.0f);
                gen.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                gen.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            }
            if (chimneyDetailed != null)
            {
                var ch1 = Object.Instantiate(chimneyDetailed, rooftopSuperstructure.transform, false);
                ch1.name = "Thermal_Exhaust_Stack_A";
                ch1.transform.position = new Vector3(6.5f, roofY, 47.5f);
                ch1.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

                var ch2 = Object.Instantiate(chimneyDetailed, rooftopSuperstructure.transform, false);
                ch2.name = "Thermal_Exhaust_Stack_B";
                ch2.transform.position = new Vector3(9.5f, roofY, 47.5f);
                ch2.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            }
            if (pipeStraight != null)
            {
                var pipe1 = Object.Instantiate(pipeStraight, rooftopSuperstructure.transform, false);
                pipe1.name = "Coolant_Pipeline_A";
                pipe1.transform.position = new Vector3(11.0f, roofY, 44.0f);
                pipe1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                pipe1.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }

            // North Wing Roof Balustrades
            for (int z = 39; z <= 47; z += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(2.0f, roofY, z), 90f, $"North_Roof_Bal_West_Z{z}", false);
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(26.0f, roofY, z), 270f, $"North_Roof_Bal_East_Z{z}", false);
            }
            for (int x = 4; x <= 24; x += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(x, roofY, 49.0f), 180f, $"North_Roof_Bal_North_X{x}", false);
            }

            // ── 8.4 SOUTH TERRACE SUBSTATION & HEAVY POWER ARRAY (Y = 8.10m) ──
            if (batteryBig != null)
            {
                var bBig = Object.Instantiate(batteryBig, rooftopSuperstructure.transform, false);
                bBig.name = "South_Roof_Battery_Bank";
                bBig.transform.position = new Vector3(10.0f, roofY, 11.0f);
                bBig.transform.rotation = Quaternion.identity;
            }
            if (batteryMedium != null)
            {
                var bMed = Object.Instantiate(batteryMedium, rooftopSuperstructure.transform, false);
                bMed.name = "South_Roof_Battery_Aux";
                bMed.transform.position = new Vector3(14.0f, roofY, 11.0f);
                bMed.transform.rotation = Quaternion.identity;
            }
            if (capacitorPrefab != null)
            {
                var cap = Object.Instantiate(capacitorPrefab, rooftopSuperstructure.transform, false);
                cap.name = "South_Roof_Capacitor_Vault";
                cap.transform.position = new Vector3(18.0f, roofY, 11.0f);
                cap.transform.rotation = Quaternion.identity;
            }
            if (turretSingle != null)
            {
                var tS1 = Object.Instantiate(turretSingle, rooftopSuperstructure.transform, false);
                tS1.name = "Defense_Turret_South_West";
                tS1.transform.position = new Vector3(6.0f, roofY + 0.2f, 11.0f);
                tS1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                tS1.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

                var tS2 = Object.Instantiate(turretSingle, rooftopSuperstructure.transform, false);
                tS2.name = "Defense_Turret_South_East";
                tS2.transform.position = new Vector3(22.0f, roofY + 0.2f, 11.0f);
                tS2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                tS2.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            }
            for (int x = 0; x <= 28; x += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(x, roofY, 9.0f), 0f, $"South_Roof_Bal_X{x}", false);
            }

            // ── 8.5 WEST PORTICO ROOF TERRACE (Y = 8.10m) ──
            if (commMast != null)
            {
                var pMast = Object.Instantiate(commMast, rooftopSuperstructure.transform, false);
                pMast.name = "Portico_Wireless_Relay";
                pMast.transform.position = new Vector3(-8.0f, roofY, 23.0f);
                pMast.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                pMast.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }
            if (satelliteDish != null)
            {
                var pDish = Object.Instantiate(satelliteDish, rooftopSuperstructure.transform, false);
                pDish.name = "Portico_Forward_Radar";
                pDish.transform.position = new Vector3(-8.0f, roofY, 19.0f);
                pDish.transform.rotation = Quaternion.Euler(-10f, -90f, 0f);
                pDish.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            }
            Vector3[] searchlightPos = new Vector3[] { new Vector3(-9.0f, roofY + 0.8f, 18.0f), new Vector3(-9.0f, roofY + 0.8f, 28.0f) };
            foreach (var slp in searchlightPos)
            {
                var sObj = new GameObject("Courtyard_Overlook_Searchlight");
                sObj.transform.SetParent(rooftopSuperstructure.transform, false);
                sObj.transform.position = slp;
                sObj.transform.rotation = Quaternion.Euler(30f, -90f, 0f);
                var sLight = sObj.AddComponent<Light>();
                sLight.type = LightType.Spot;
                sLight.spotAngle = 65f;
                sLight.range = 35f;
                sLight.intensity = 3.5f;
                sLight.color = new Color(0.85f, 0.95f, 1.0f);
            }
            for (int z = 19; z <= 27; z += 4)
            {
                spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-10.0f, roofY, z), 90f, $"Portico_Roof_Bal_West_Z{z}", false);
            }
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-8.0f, roofY, 17.0f), 0f, "Portico_Roof_Bal_S-8", false);
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-4.0f, roofY, 17.0f), 0f, "Portico_Roof_Bal_S-4", false);
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-8.0f, roofY, 29.0f), 180f, "Portico_Roof_Bal_N-8", false);
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-4.0f, roofY, 29.0f), 180f, "Portico_Roof_Bal_N-4", false);

            // Roof West Alcove Balustrades (returns at X = -2.0m)
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-2.0f, roofY, 11.0f), 90f, "Roof_Bal_Alcove_S11", false);
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-2.0f, roofY, 15.0f), 90f, "Roof_Bal_Alcove_S15", false);
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-2.0f, roofY, 31.0f), 90f, "Roof_Bal_Alcove_N31", false);
            spawnBalustrade(rooftopSuperstructure.transform, new Vector3(-2.0f, roofY, 35.0f), 90f, "Roof_Bal_Alcove_N35", false);

            // Balcony Outer Corner Posts (Caps gaps between perpendicular balustrades - 100% modular column_end)
            if (columnPillar != null)
            {
                var roofCorners = new Vector3[] {
                    new Vector3(42.0f, roofY, 17.0f),
                    new Vector3(42.0f, roofY, 29.0f),
                    new Vector3(30.0f, roofY, 9.0f),
                    new Vector3(-2.0f, roofY, 9.0f),
                    new Vector3(-10.0f, roofY, 17.0f),
                    new Vector3(-10.0f, roofY, 29.0f),
                    new Vector3(2.0f, roofY, 49.0f),
                    new Vector3(26.0f, roofY, 49.0f),
                    new Vector3(30.0f, roofY, 37.0f),
                    new Vector3(-2.0f, roofY, 37.0f)
                };
                for (int i = 0; i < roofCorners.Length; i++)
                {
                    var col = Object.Instantiate(columnPillar, rooftopSuperstructure.transform, false);
                    col.name = $"Roof_Balcony_Corner_Finish_{i}";
                    col.transform.position = roofCorners[i];
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 9. INTERIOR COMMAND SET DRESSING & 4 SCI-FI OUTPOST FUNCTIONAL ZONES
            // ─────────────────────────────────────────────────────────────────
            GameObject propsGroup = new GameObject("Citadel_Interior_Decorations");
            propsGroup.transform.SetParent(mainBuilding, false);

            System.Action<GameObject, Transform, Vector3, Quaternion> spawnProp = delegate(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot)
            {
                if (prefab == null) return;
                var p = Object.Instantiate(prefab, parent, false);
                p.transform.position = pos;
                p.transform.rotation = rot;
            };

            // Flush Illuminated Command Ring (No high step casting strange diagonal shadows!)
            var ringDeco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringDeco.name = "Command_Rotunda_Circuit_Ring";
            ringDeco.transform.SetParent(propsGroup.transform, false);
            ringDeco.transform.position = new Vector3(14.0f, 0.10f, 23.0f);
            ringDeco.transform.localScale = new Vector3(12.0f, 0.02f, 12.0f);
            Object.DestroyImmediate(ringDeco.GetComponent<Collider>());
            if (steelAccentMat != null) ringDeco.GetComponent<Renderer>().sharedMaterial = steelAccentMat;

            // ── ZONE 1: COMMAND BRIDGE & 3D PLANETARY HOLO-TABLE ──
            BuildCommandBridgeHoloTable(propsGroup.transform, new Vector3(14.0f, 0.10f, 23.0f),
                projectorPrefab, compStation, pilotSeat,
                darkTitaniumMat, steelAccentMat, cyanEmissiveMat, coreGlowMat, holoGlassMat, beamMat, oculusMat);

            // ── ZONE 3: CRYO STASIS & LIFE SUPPORT PODS ──
            BuildCryoLifeSupportBay(propsGroup.transform, machineBarrelLarge, barrelsCluster,
                pipeStraight, pipeCorner, consoleScreenPrefab, darkTitaniumMat, holoGlassMat, cyanEmissiveMat, smokeMat);

            // ── ZONE 4: AIRLOCK & CARGO LOGISTICS BAY ──
            BuildAirlockAndCargoBay(propsGroup.transform, containerBig, containerSmall, storageContainerSmall,
                torpedoRack, wallShelf, barrelsCluster, floorCornerPipes, compStation,
                hazardStripeMat, darkTitaniumMat, cyanEmissiveMat);

            // ── SCATTERING: OVERHEAD VENTILATION DUCTS & BASEBOARD CONDUITS ──
            BuildOverheadConduitsAndPipes(propsGroup.transform, pipeStraight, pipeCorner, pipeRoundLarge, pipeCross,
                floorCornerPipes, floorCornerPipesTurn);

            // ── DEDICATED PREFABS & SCREEN MATERIALS FOR NORTH WING & EAST WING ──
            GameObject deskPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/desk.prefab");
            GameObject deskNoCompPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/desk_no_computer.prefab");
            GameObject bigScreenPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab");
            GameObject cabinet = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab") ?? cabinetL;
            GameObject ceilingLight2 = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_celing_2.prefab") ?? ceilingLight;
            GameObject plantPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_plant.prefab");
            GameObject plantDeskPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_plant_desk.prefab");
            GameObject plantSmallPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_plant_small.prefab");
            GameObject hydroponicPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/hydroponic.prefab");
            GameObject art1Prefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_1.prefab");
            GameObject art2Prefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_2.prefab");
            GameObject tableGlassPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/decorative_table_glass.prefab");
            GameObject decoChairPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab");
            GameObject sofaPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_sofa.prefab");
            GameObject pillowPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_pilow.prefab");

            // ── ZONE 2: SOUTH OBSERVATION TERRACE & BIO-HYDRO ENERGY LOUNGE ──
            BuildSouthObservationTerraceAndBioLounge(propsGroup.transform,
                tableGlassPrefab, sofaPrefab, pillowPrefab,
                hydroponicPrefab, plantPrefab, plantDeskPrefab, plantSmallPrefab,
                cabinet, compStation, consoleScreenPrefab, pilotSeat,
                art1Prefab, art2Prefab, ceilingLight2,
                cyanEmissiveMat, steelAccentMat, coreGlowMat);

            Material radarScopeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Radar_Scope.mat");
            Material centralHoloMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Central_Holo_Ring.mat");
            Material telemetryLMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Telemetry_Left.mat");
            Material telemetryRMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Telemetry_Right.mat");
            Material deskTelemetryMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Desk_Monitors_Telemetry.mat");
            Material deskDiagnosticsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Desk_Monitors_Diagnostics.mat");

            // Configure procedural animated screen shaders (Radar sweep, rotating holo reticles, jumping telemetry blocks)
            Material overheadRadarMat;
            Material overheadDiagMat;
            ConfigureAnimatedScreenMaterials(radarScopeMat, centralHoloMat, telemetryRMat, telemetryLMat, out overheadRadarMat, out overheadDiagMat);

            // ── ZONE 1B: CENTRAL PROMENADE, GANTRY TRUSSES & ATRIUM ARCHITECTURAL ENHANCEMENTS ──
            BuildCitadelAtriumArchitecturalEnhancements(propsGroup.transform,
                compStation, deskPrefab, pilotSeat, consoleScreenPrefab, consoleCeiling,
                plantPrefab, hydroponicPrefab, crystalLarge,
                darkTitaniumMat, steelAccentMat, cyanEmissiveMat, coreGlowMat, holoGlassMat,
                radarScopeMat, centralHoloMat, telemetryLMat, deskTelemetryMat, deskDiagnosticsMat,
                overheadRadarMat, overheadDiagMat);

            // ── ZONE 5: NORTH WING MISSION CONTROL VIDEO WALL, OPERATOR WORKSTATIONS & RIGHT-SIDE DECORATIVE LOUNGE ──
            BuildNorthMissionControlWing(propsGroup.transform,
                bigScreen, consoleScreenPrefab, consoleCeiling,
                deskPrefab, pilotSeat,
                cabinet, cabinetL, wallShelf,
                plantPrefab, plantDeskPrefab, plantSmallPrefab, hydroponicPrefab,
                art1Prefab, art2Prefab,
                tableGlassPrefab, decoChairPrefab,
                cyanEmissiveMat, steelAccentMat, darkTitaniumMat,
                radarScopeMat, centralHoloMat, telemetryLMat, telemetryRMat,
                deskTelemetryMat, deskDiagnosticsMat);

            // ── DEDICATED PORTFOLIO MASTER COMMAND CHAMBER (EAST WING - EXACT IMAGE DESIGN) ──
            DesignPortfolioMasterChamber(stationGo, propsGroup.transform, deskPrefab, deskNoCompPrefab, bigScreenPrefab,
                pipeStraight, cableThick, pilotSeat, cabinet, ceilingLight2, columnPillar, wallShelf,
                darkTitaniumMat, steelAccentMat, cyanEmissiveMat, smokeMat,
                radarScopeMat, centralHoloMat, telemetryLMat, telemetryRMat,
                deskTelemetryMat, deskDiagnosticsMat);

            // OPEN MONUMENTAL SCI-FI PORTAL ARCHES (Replaces solid blocking corridors!)
            if (columnPillar != null)
            {
                // North Wing Entrance Portal - Widened to X=4m and X=24m for 100% open sightlines!
                var nP1 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                nP1.transform.position = new Vector3(4.0f, 0.10f, 36.5f);
                var nP2 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                nP2.transform.position = new Vector3(24.0f, 0.10f, 36.5f);
            }

            // ─────────────────────────────────────────────────────────────────
            // 10. INTERIOR CONTRAST LIGHTING, NEON BASEBOARD TRIMS & DUST MOTES VFX
            // ─────────────────────────────────────────────────────────────────
            BuildInteriorAtmosphereAndLighting(mainBuilding, ceilingLight, cyanEmissiveMat, smokeMat);

            // ─────────────────────────────────────────────────────────────────
            // 11. CONTINUOUS SOLID COURTYARD PLAZA & ILLUMINATED SCI-FI GLASS BALUSTRADES
            // 100% Solid Deck at Y = 0.09m (Zero height steps, zero gaps, zero black walls!)
            // ─────────────────────────────────────────────────────────────────
            Transform existingCourtyard = aboutIsland.transform.Find("Observatory_Courtyard_Plaza");
            if (existingCourtyard != null)
            {
                if (Application.isPlaying) Object.Destroy(existingCourtyard.gameObject);
                else Object.DestroyImmediate(existingCourtyard.gameObject);
            }

            GameObject courtyardRoot = new GameObject("Observatory_Courtyard_Plaza");
            courtyardRoot.transform.SetParent(aboutIsland.transform, false);
            courtyardRoot.transform.localPosition = Vector3.zero;

            // 11.1 FULL CONTINUOUS COURTYARD DECK (X: -40 to -4, Z: 7 to 39 at Y = 0.09m)
            // Fully covers South Alcove & North Alcove with 0 missing floor spots
            if (floorTile != null)
            {
                for (int x = -40; x <= -4; x += 4)
                {
                    for (int z = 7; z <= 39; z += 4)
                    {
                        // Skip interior of West Gatehouse Portico which is already paved with interior tiles
                        if (x >= -10 && z >= 19 && z <= 27) continue;

                        bool isCenterRunway = (z >= 21 && z <= 25);
                        GameObject tilePrefab = (isCenterRunway && floorEmissiveTile != null) ? floorEmissiveTile : floorTile;
                        var cTile = Object.Instantiate(tilePrefab, courtyardRoot.transform, false);
                        cTile.name = $"Courtyard_Tile_{x}_{z}";
                        cTile.transform.position = new Vector3(x, 0.09f, z);
                    }
                }

                // Seal the North Wing West notch at (X = 0, Z = 39)
                var cornerTileN = Object.Instantiate(floorTile, courtyardRoot.transform, false);
                cornerTileN.name = "Courtyard_Tile_0_39";
                cornerTileN.transform.position = new Vector3(0f, 0.09f, 39f);
            }

            // 11.2 ILLUMINATED SCI-FI GLASS BALUSTRADES (Never black! Glowing neon handrail & unlit cyan glass!)
            // North Perimeter Balustrades (Z = 41.0m, X: -40 to 0)
            for (int x = -40; x <= 0; x += 4)
            {
                spawnBalustrade(courtyardRoot.transform, new Vector3(x, 0.09f, 41.0f), 180f, $"Balustrade_North_X{x}", x % 8 == 0);
            }
            // South Perimeter Balustrades (Z = 5.0m, X: -40 to -4)
            for (int x = -40; x <= -4; x += 4)
            {
                spawnBalustrade(courtyardRoot.transform, new Vector3(x, 0.09f, 5.0f), 0f, $"Balustrade_South_X{x}", x % 8 == 0);
            }
            // South Return Balustrade sealing to building south facade at (-2, 9)
            spawnBalustrade(courtyardRoot.transform, new Vector3(-2.0f, 0.09f, 7.0f), 270f, "Balustrade_South_Return", true);

            // West Outer Edge Balustrades (X = -42.0m, Z: 7 to 39)
            for (int z = 7; z <= 39; z += 4)
            {
                spawnBalustrade(courtyardRoot.transform, new Vector3(-42.0f, 0.09f, z), 90f, $"Balustrade_West_Z{z}", z % 8 == 7);
            }

            // 11.3 Side Monument of Light (Placed on North Flank at Z = 33.0m - Leaves central runway clear!)
            var monumentGo = new GameObject("Plaza_Monument_Of_Light");
            monumentGo.transform.SetParent(courtyardRoot.transform, false);
            monumentGo.transform.position = new Vector3(-20.0f, 0.09f, 33.0f);

            var monPedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            monPedestal.name = "Monument_Pedestal";
            monPedestal.transform.SetParent(monumentGo.transform, false);
            monPedestal.transform.localPosition = Vector3.zero;
            monPedestal.transform.localScale = new Vector3(3.2f, 0.35f, 3.2f);
            Object.DestroyImmediate(monPedestal.GetComponent<Collider>());
            if (darkTitaniumMat != null) monPedestal.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

            if (crystalLarge != null)
            {
                var crys = Object.Instantiate(crystalLarge, monumentGo.transform, false);
                crys.name = "Levitating_Power_Crystal";
                crys.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                crys.transform.localScale = new Vector3(1.4f, 1.8f, 1.4f);
                if (coreGlowMat != null)
                {
                    foreach (var r in crys.GetComponentsInChildren<Renderer>()) r.sharedMaterial = coreGlowMat;
                }

                var monLight = monumentGo.AddComponent<Light>();
                monLight.type = LightType.Point;
                monLight.range = 12f;
                monLight.intensity = 3.6f;
                monLight.color = new Color(0.1f, 0.95f, 1.0f);
            }

            // 11.4 Designated Exploration Rover Service Bay (Placed on South Flank at Z = 13.0m)
            if (roverModel != null)
            {
                var roverBay = new GameObject("Rover_Charging_Berth");
                roverBay.transform.SetParent(courtyardRoot.transform, false);
                roverBay.transform.position = new Vector3(-18.0f, 0.09f, 13.0f);

                var rover = Object.Instantiate(roverModel, roverBay.transform, false);
                rover.name = "Command_Exploration_Rover";
                rover.transform.localPosition = Vector3.zero;
                rover.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                rover.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

                if (columnPillar != null)
                {
                    var chgPillar = Object.Instantiate(columnPillar, roverBay.transform, false);
                    chgPillar.name = "Power_Recharge_Pylon";
                    chgPillar.transform.localPosition = new Vector3(-2.2f, 0f, 1.5f);
                    chgPillar.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 12. EXTERIOR INDUSTRIAL MACHINERY YARDS ("xung quanh căn cứ phải thật nhiều máy móc")
            // ─────────────────────────────────────────────────────────────────
            BuildSurroundingMachineryYards(aboutIsland, generatorLarge, chimneyDetailed, batteryBig, batteryMedium, capacitorPrefab, generatorPrefab, containerBig, containerSmall, storageContainerBig, darkTitaniumMat);

            // 13. CITADEL DESTRUCTIBLE EXTERIOR ARMOR PLATES (Tàu bắn vào căn cứ mảng vỏ ngoài hỏng lóc rụng ra)
            CitadelDestructibleArmorPlate.SetupCitadelExteriorArmorPlates(aboutIsland);

            Debug.Log("Master Planetary Citadel, Dedicated Landing Bastion, Operations Display Wall & Exterior Machinery Yards built successfully!");
        }

        private static void RelocateIndustrialAreaOutdoors(GameObject aboutIsland)
        {
            Transform indArea = aboutIsland.transform.Find("IndustrialArea");
            if (indArea == null) return;

            Transform generators = indArea.Find("Generators");
            if (generators != null)
            {
                Transform shieldCore = generators.Find("Central_Plasma_Shield_Core");
                if (shieldCore != null) shieldCore.position = new Vector3(8.0f, 0.10f, 2.0f);

                Transform turbine = generators.Find("Primary_Turbine_Generator");
                if (turbine != null) turbine.position = new Vector3(3.0f, 0.10f, 2.0f);

                Transform battBank = generators.Find("Heavy_Battery_Bank");
                if (battBank != null) battBank.position = new Vector3(3.0f, 0.10f, -2.0f);

                Transform fluxCap = generators.Find("Flux_Capacitor_Array");
                if (fluxCap != null) fluxCap.position = new Vector3(8.0f, 0.10f, -2.0f);
            }

            Transform tanks = indArea.Find("Tanks");
            if (tanks != null)
            {
                Transform cryoA = tanks.Find("Cryo_Tank_Alpha");
                if (cryoA != null) cryoA.position = new Vector3(8.0f, 0.10f, -6.0f);

                Transform cryoB = tanks.Find("Cryo_Tank_Beta");
                if (cryoB != null) cryoB.position = new Vector3(8.0f, 0.10f, -8.5f);
            }

            Transform machinery = indArea.Find("Machinery");
            if (machinery != null)
            {
                Transform stack0 = machinery.Find("Cooling_Stack_0");
                if (stack0 != null) stack0.position = new Vector3(3.0f, 0.10f, -6.0f);

                Transform stack1 = machinery.Find("Cooling_Stack_1");
                if (stack1 != null) stack1.position = new Vector3(3.0f, 0.10f, -8.5f);
            }

            Transform indPipes = indArea.Find("Pipes");
            if (indPipes != null)
            {
                Object.DestroyImmediate(indPipes.gameObject);
            }
        }

        private static void BuildSurroundingMachineryYards(GameObject aboutIsland,
            GameObject genLarge, GameObject chimney, GameObject battBig, GameObject battMed,
            GameObject capacitor, GameObject generator, GameObject contBig, GameObject contSmall,
            GameObject storageBig, Material titaniumMat)
        {
            Transform existingYards = aboutIsland.transform.Find("Citadel_Surrounding_Machinery_Network");
            if (existingYards != null)
            {
                if (Application.isPlaying) Object.Destroy(existingYards.gameObject);
                else Object.DestroyImmediate(existingYards.gameObject);
            }

            GameObject yardsRoot = new GameObject("Citadel_Surrounding_Machinery_Network");
            yardsRoot.transform.SetParent(aboutIsland.transform, false);

            System.Action<GameObject, Vector3, Quaternion, Vector3, string> spawnMachine = delegate(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, string name)
            {
                if (prefab == null) return;
                var go = Object.Instantiate(prefab, yardsRoot.transform, false);
                go.name = name;
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.transform.localScale = scale;
            };

            // 1. NORTH EXTERIOR GEOTHERMAL & AUXILIARY YARD (Z: 51m to 62m | X: 6m to 22m)
            if (genLarge != null)
            {
                spawnMachine(genLarge, new Vector3(10.0f, 0.10f, 54.0f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.8f, 1.8f, 1.8f), "North_Geothermal_Generator_Alpha");
                spawnMachine(genLarge, new Vector3(18.0f, 0.10f, 54.0f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.8f, 1.8f, 1.8f), "North_Geothermal_Generator_Beta");
            }
            if (chimney != null)
            {
                spawnMachine(chimney, new Vector3(6.0f, 0.10f, 56.0f), Quaternion.identity, new Vector3(1.5f, 2.5f, 1.5f), "North_Cooling_Exhaust_1");
                spawnMachine(chimney, new Vector3(22.0f, 0.10f, 56.0f), Quaternion.identity, new Vector3(1.5f, 2.5f, 1.5f), "North_Cooling_Exhaust_2");
            }
            if (battBig != null) spawnMachine(battBig, new Vector3(14.0f, 0.10f, 58.0f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), "North_Backup_Battery_Vault");
            if (storageBig != null) spawnMachine(storageBig, new Vector3(8.0f, 0.10f, 60.0f), Quaternion.Euler(0f, 35f, 0f), new Vector3(1.2f, 1.2f, 1.2f), "North_Parts_Storage_Container");

            // 2. SOUTH EXTERIOR INDUSTRIAL POWER SUBSTATION (Z: -12m to 5m | X: 0m to 26m)
            if (generator != null)
            {
                spawnMachine(generator, new Vector3(1.0f, 0.10f, -4.0f), Quaternion.identity, new Vector3(1.5f, 1.5f, 1.5f), "South_Aux_Turbine_A");
                spawnMachine(generator, new Vector3(1.0f, 0.10f, 0.0f), Quaternion.identity, new Vector3(1.5f, 1.5f, 1.5f), "South_Aux_Turbine_B");
            }
            if (capacitor != null)
            {
                spawnMachine(capacitor, new Vector3(12.0f, 0.10f, 4.0f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), "South_Grid_Capacitor_Alpha");
                spawnMachine(capacitor, new Vector3(16.0f, 0.10f, 4.0f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), "South_Grid_Capacitor_Beta");
            }
            if (storageBig != null)
            {
                spawnMachine(storageBig, new Vector3(0.0f, 0.10f, -10.0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.3f, 1.3f, 1.3f), "South_Heavy_Refinery_Freight_1");
                spawnMachine(storageBig, new Vector3(5.0f, 0.10f, -10.0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.3f, 1.3f, 1.3f), "South_Heavy_Refinery_Freight_2");
            }

            // 3. EAST EXTERIOR LOGISTICS DEPOT (X: 42m to 52m | Z: 17m to 29m)
            if (contBig != null)
            {
                spawnMachine(contBig, new Vector3(45.0f, 0.10f, 17.0f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), "East_Logistics_Container_1");
                spawnMachine(contBig, new Vector3(45.0f, 0.10f, 29.0f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), "East_Logistics_Container_2");
            }
            if (storageBig != null) spawnMachine(storageBig, new Vector3(48.0f, 0.10f, 23.0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.5f, 1.5f, 1.5f), "East_Heavy_Cargo_Freight");

            // 4. WEST FLIGHT MAINTENANCE DEPOT (X: -26m to -16m | Z: 32m to 40m)
            if (battMed != null) spawnMachine(battMed, new Vector3(-20.0f, 0.10f, 35.0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(1.3f, 1.3f, 1.3f), "Flight_FlightLine_Charger");
            if (contSmall != null)
            {
                spawnMachine(contSmall, new Vector3(-24.0f, 0.10f, 33.0f), Quaternion.identity, new Vector3(1.2f, 1.2f, 1.2f), "Flight_Tool_Vault_Alpha");
                spawnMachine(contSmall, new Vector3(-24.0f, 0.10f, 37.0f), Quaternion.identity, new Vector3(1.2f, 1.2f, 1.2f), "Flight_Tool_Vault_Beta");
            }
        }

        private static void DesignPortfolioMasterChamber(
            GameObject stationGo, Transform parent, GameObject deskPrefab, GameObject deskNoCompPrefab, GameObject bigScreenPrefab,
            GameObject pipeStraight, GameObject cableThick, GameObject pilotSeat, GameObject cabinet, GameObject ceilingLight, GameObject columnPillar,
            GameObject wallShelf,
            Material titaniumMat, Material steelMat, Material cyanEmissiveMat, Material smokeMat,
            Material radarScopeMat, Material centralHoloMat, Material telemetryLMat, Material telemetryRMat,
            Material deskTelemetryMat, Material deskDiagnosticsMat)
        {
            if (stationGo == null) return;

            // 1. Relocate About_Info_Station into the East Wing Chamber (Back wall X = 42.0m, Z = 23.0m)
            stationGo.transform.position = new Vector3(39.50f, 0.10f, 23.00f);
            stationGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Container for chamber architectural elements
            Transform existingChamber = parent.Find("EastWing_Portfolio_Command_Chamber");
            if (existingChamber != null)
            {
                if (Application.isPlaying) Object.Destroy(existingChamber.gameObject);
                else Object.DestroyImmediate(existingChamber.gameObject);
            }

            var chamberGo = new GameObject("EastWing_Portfolio_Command_Chamber");
            chamberGo.transform.SetParent(parent, false);

            // ─────────────────────────────────────────────────────────────────
            // A. SIDE WALLS: Clean, unobstructed sci-fi architecture (Wall pipes removed per user request)
            // ─────────────────────────────────────────────────────────────────

            // ─────────────────────────────────────────────────────────────────
            // A2. SIDE WALLS: Clean, open sci-fi architecture (Wall shelves removed per user request)
            // ─────────────────────────────────────────────────────────────────

            // ─────────────────────────────────────────────────────────────────
            // B. REMOVE ANY FOREGROUND COLUMNS / MOCK BLOCKS
            // ─────────────────────────────────────────────────────────────────
            Transform existingFg = stationGo.transform.Find("Chamber_Foreground_Gateway");
            if (existingFg != null)
            {
                if (Application.isPlaying) Object.Destroy(existingFg.gameObject);
                else Object.DestroyImmediate(existingFg.gameObject);
            }

            // Clean old floor cables that were primitive cylinders
            var oldCables = new List<GameObject>();
            foreach (Transform c in stationGo.transform)
            {
                if (c.name.StartsWith("Chamber_FloorCable_")) oldCables.Add(c.gameObject);
            }
            foreach (var g in oldCables)
            {
                if (Application.isPlaying) Object.Destroy(g);
                else Object.DestroyImmediate(g);
            }

            // ─────────────────────────────────────────────────────────────────
            // C. CENTRAL HOLOGRAPHIC PROJECTION RING ON MODULAR FLOOR
            // ─────────────────────────────────────────────────────────────────
            Transform existingDais = stationGo.transform.Find("Chamber_Central_Hologram_Dais");
            if (existingDais != null)
            {
                if (Application.isPlaying) Object.Destroy(existingDais.gameObject);
                else Object.DestroyImmediate(existingDais.gameObject);
            }

            var daisRoot = new GameObject("Chamber_Central_Hologram_Dais");
            daisRoot.transform.SetParent(stationGo.transform, false);
            daisRoot.transform.localPosition = new Vector3(0f, 0f, -3.10f);

            var holoDisc = GameObject.CreatePrimitive(PrimitiveType.Quad);
            holoDisc.name = "Dais_Hologram_Projection_Disc";
            holoDisc.transform.SetParent(daisRoot.transform, false);
            holoDisc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            holoDisc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            holoDisc.transform.localScale = new Vector3(2.15f, 2.15f, 1.0f);
            Object.DestroyImmediate(holoDisc.GetComponent<Collider>());
            if (centralHoloMat != null) holoDisc.GetComponent<Renderer>().sharedMaterial = centralHoloMat;
            else if (cyanEmissiveMat != null) holoDisc.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var daisLight = daisRoot.AddComponent<Light>();
            daisLight.type = LightType.Point;
            daisLight.range = 4.2f;
            daisLight.intensity = 2.8f;
            daisLight.color = new Color(0.15f, 0.95f, 1.0f);

            // ─────────────────────────────────────────────────────────────────
            // D. COMMAND BRIDGE DESKS (100% REAL MODULAR ASSETS - ZERO PRIMITIVE CUBES)
            // ─────────────────────────────────────────────────────────────────
            Transform centerDesk = stationGo.transform.Find("Center_Desk");
            if (centerDesk != null)
            {
                centerDesk.localPosition = new Vector3(0f, 0f, -0.65f);
                centerDesk.localRotation = Quaternion.identity;

                Transform cDesk = centerDesk.Find("Center_Bridge_Asset_Desk");
                if (cDesk == null && deskNoCompPrefab != null)
                {
                    var newDesk = Object.Instantiate(deskNoCompPrefab, centerDesk, false);
                    newDesk.name = "Center_Bridge_Asset_Desk";
                    cDesk = newDesk.transform;
                }
                if (cDesk != null)
                {
                    cDesk.localPosition = new Vector3(0f, 0.06f, 0f);
                    cDesk.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }

                // Commander Pilot Chair (Center)
                Transform cmdChair = centerDesk.Find("Commander_Pilot_Chair_Center");
                if (cmdChair == null && pilotSeat != null)
                {
                    var newChair = Object.Instantiate(pilotSeat, centerDesk, false);
                    newChair.name = "Commander_Pilot_Chair_Center";
                    cmdChair = newChair.transform;
                }
                if (cmdChair != null)
                {
                    cmdChair.localPosition = new Vector3(0f, 0.13f, -1.63f);
                    cmdChair.localRotation = Quaternion.identity;
                    cmdChair.localScale = new Vector3(0.72f, 0.72f, 0.72f);
                }
            }

            // Left & Right Console Wings (Semi-circle neat workstation layout from New Scene 1)
            Transform wingsRoot = stationGo.transform.Find("Curved_Console_Wing_Plinths");
            if (wingsRoot == null)
            {
                var newWings = new GameObject("Curved_Console_Wing_Plinths");
                newWings.transform.SetParent(stationGo.transform, false);
                wingsRoot = newWings.transform;
            }

            System.Action<GameObject, Material> assignDeskMonitorMat = (deskGo, screenMat) =>
            {
                if (screenMat == null) return;
                var mr = deskGo.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterials.Length >= 2)
                {
                    var mats = mr.sharedMaterials;
                    mats[1] = screenMat;
                    mr.sharedMaterials = mats;
                }
            };

            // Left Wing Background Desk (Aft)
            Transform leftDeskAft = wingsRoot.Find("Left_Wing_Asset_Desk");
            if (leftDeskAft == null && deskPrefab != null)
            {
                var d = Object.Instantiate(deskPrefab, wingsRoot, false);
                d.name = "Left_Wing_Asset_Desk";
                leftDeskAft = d.transform;
            }
            if (leftDeskAft != null)
            {
                leftDeskAft.localPosition = new Vector3(-4.31f, 0.06f, -2.17f);
                leftDeskAft.localRotation = Quaternion.Euler(0f, 145f, 0f);
                leftDeskAft.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(leftDeskAft.gameObject, deskTelemetryMat);
            }

            // Left Wing Foreground Desk (Fore)
            Transform leftDeskFore = wingsRoot.Find("Left_Wing_Asset_Desk_Fore");
            if (leftDeskFore == null && deskPrefab != null)
            {
                var d = Object.Instantiate(deskPrefab, wingsRoot, false);
                d.name = "Left_Wing_Asset_Desk_Fore";
                leftDeskFore = d.transform;
            }
            if (leftDeskFore != null)
            {
                leftDeskFore.localPosition = new Vector3(-6.47f, -0.22f, -6.20f);
                leftDeskFore.localRotation = Quaternion.Euler(0f, 90f, 0f);
                leftDeskFore.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(leftDeskFore.gameObject, deskTelemetryMat);
            }

            // Right Wing Background Desk (Aft)
            Transform rightDeskAft = wingsRoot.Find("Right_Wing_Asset_Desk");
            if (rightDeskAft == null && deskPrefab != null)
            {
                var d = Object.Instantiate(deskPrefab, wingsRoot, false);
                d.name = "Right_Wing_Asset_Desk";
                rightDeskAft = d.transform;
            }
            if (rightDeskAft != null)
            {
                rightDeskAft.localPosition = new Vector3(4.48f, 0.37f, -2.29f);
                rightDeskAft.localRotation = Quaternion.Euler(0f, 215f, 0f);
                rightDeskAft.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(rightDeskAft.gameObject, deskDiagnosticsMat);
            }

            // Right Wing Foreground Desk (Fore)
            Transform rightDeskFore = wingsRoot.Find("Right_Wing_Asset_Desk_Fore");
            if (rightDeskFore == null && deskPrefab != null)
            {
                var d = Object.Instantiate(deskPrefab, wingsRoot, false);
                d.name = "Right_Wing_Asset_Desk_Fore";
                rightDeskFore = d.transform;
            }
            if (rightDeskFore != null)
            {
                rightDeskFore.localPosition = new Vector3(6.62f, -0.05f, -6.19f);
                rightDeskFore.localRotation = Quaternion.Euler(0f, 270f, 0f);
                rightDeskFore.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(rightDeskFore.gameObject, deskDiagnosticsMat);
            }

            // Officer Chairs
            Transform chairLAft = wingsRoot.Find("Officer_Chair_Left_Aft");
            if (chairLAft == null && pilotSeat != null)
            {
                var c = Object.Instantiate(pilotSeat, wingsRoot, false);
                c.name = "Officer_Chair_Left_Aft";
                chairLAft = c.transform;
            }
            if (chairLAft != null)
            {
                chairLAft.localPosition = new Vector3(-3.34f, 0.05f, -3.63f);
                chairLAft.localRotation = Quaternion.Euler(0f, 317.47f, 0f);
                chairLAft.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // Officer Chair Left Fore
            Transform chairLFore = wingsRoot.Find("Officer_Chair_Left_Fore");
            if (chairLFore == null && pilotSeat != null)
            {
                var c = Object.Instantiate(pilotSeat, wingsRoot, false);
                c.name = "Officer_Chair_Left_Fore";
                chairLFore = c.transform;
            }
            if (chairLFore != null)
            {
                chairLFore.localPosition = new Vector3(-4.63f, 0.05f, -6.62f);
                chairLFore.localRotation = Quaternion.Euler(0f, 271.78f, 0f);
                chairLFore.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // Officer Chair Right Aft
            Transform chairRAft = wingsRoot.Find("Officer_Chair_Right_Aft");
            if (chairRAft == null && pilotSeat != null)
            {
                var c = Object.Instantiate(pilotSeat, wingsRoot, false);
                c.name = "Officer_Chair_Right_Aft";
                chairRAft = c.transform;
            }
            if (chairRAft != null)
            {
                chairRAft.localPosition = new Vector3(3.50f, 0.05f, -3.73f);
                chairRAft.localRotation = Quaternion.Euler(0f, 37.82f, 0f);
                chairRAft.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // Officer Chair Right Fore
            Transform chairRFore = wingsRoot.Find("Officer_Chair_Right_Fore");
            if (chairRFore == null && pilotSeat != null)
            {
                var c = Object.Instantiate(pilotSeat, wingsRoot, false);
                c.name = "Officer_Chair_Right_Fore";
                chairRFore = c.transform;
            }
            if (chairRFore != null)
            {
                chairRFore.localPosition = new Vector3(5.12f, 0.05f, -6.33f);
                chairRFore.localRotation = Quaternion.Euler(0f, 85.10f, 0f);
                chairRFore.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // ─────────────────────────────────────────────────────────────────
            // E. THE MULTI-SCREEN VIDEO WALL (MAIN DISPLAY & ANGLED FLANKING WINGS)
            // ─────────────────────────────────────────────────────────────────
            Transform mainScreen = stationGo.transform.Find("MainScreen");
            if (mainScreen != null)
            {
                mainScreen.localPosition = new Vector3(0f, 0.35f, 0.85f);
                mainScreen.localRotation = Quaternion.identity;

                Transform msFrame = mainScreen.Find("MainScreen_Asset_Frame");
                if (msFrame == null && bigScreenPrefab != null)
                {
                    var bs = Object.Instantiate(bigScreenPrefab, mainScreen, false);
                    bs.name = "MainScreen_Asset_Frame";
                    msFrame = bs.transform;
                }
                if (msFrame != null)
                {
                    msFrame.localPosition = new Vector3(0f, 3.03f, 0.15f);
                    msFrame.localRotation = Quaternion.identity;
                    msFrame.localScale = new Vector3(1.45f, 1.30f, 1.0f);
                }
            }

            // Left Multi-Screen Wing
            Transform leftWingScreenGo = stationGo.transform.Find("Chamber_Left_MultiScreen_Wing");
            if (leftWingScreenGo == null)
            {
                var lw = new GameObject("Chamber_Left_MultiScreen_Wing");
                lw.transform.SetParent(stationGo.transform, false);
                leftWingScreenGo = lw.transform;
            }
            leftWingScreenGo.localPosition = new Vector3(-3.85f, 2.30f, 0.65f);
            leftWingScreenGo.localRotation = Quaternion.Euler(0f, -24f, 0f);

            Transform leftWingAsset = leftWingScreenGo.Find("LeftWing_Asset_Frame");
            if (leftWingAsset == null && bigScreenPrefab != null)
            {
                var bsL = Object.Instantiate(bigScreenPrefab, leftWingScreenGo, false);
                bsL.name = "LeftWing_Asset_Frame";
                leftWingAsset = bsL.transform;
            }
            if (leftWingAsset != null)
            {
                leftWingAsset.localPosition = new Vector3(-0.56f, 1.36f, 0.15f);
                leftWingAsset.localRotation = Quaternion.identity;
                leftWingAsset.localScale = new Vector3(0.80f, 1.05f, 1.0f);
            }

            // Middle Screen on Left Wing: Sign_Prev
            Transform signPrev = stationGo.transform.Find("Sign_Prev");
            if (signPrev != null)
            {
                signPrev.localPosition = new Vector3(-4.24f, 4.33f, 0.22f);
                signPrev.localRotation = Quaternion.Euler(0f, -24f, 0f);
                signPrev.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            }

            // Bottom Screen on Left Wing: Controls_Board
            Transform controlsBoard = stationGo.transform.Find("Controls_Board");
            if (controlsBoard != null)
            {
                controlsBoard.localPosition = new Vector3(-4.19f, 0.78f, 0.23f);
                controlsBoard.localRotation = Quaternion.Euler(0f, -24f, 0f);
                controlsBoard.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            }

            // Right Multi-Screen Wing
            Transform rightWingScreenGo = stationGo.transform.Find("Chamber_Right_MultiScreen_Wing");
            if (rightWingScreenGo == null)
            {
                var rw = new GameObject("Chamber_Right_MultiScreen_Wing");
                rw.transform.SetParent(stationGo.transform, false);
                rightWingScreenGo = rw.transform;
            }
            rightWingScreenGo.localPosition = new Vector3(3.85f, 2.30f, 0.65f);
            rightWingScreenGo.localRotation = Quaternion.Euler(0f, 24f, 0f);

            Transform rightWingAsset = rightWingScreenGo.Find("RightWing_Asset_Frame");
            if (rightWingAsset == null && bigScreenPrefab != null)
            {
                var bsR = Object.Instantiate(bigScreenPrefab, rightWingScreenGo, false);
                bsR.name = "RightWing_Asset_Frame";
                rightWingAsset = bsR.transform;
            }
            if (rightWingAsset != null)
            {
                rightWingAsset.localPosition = new Vector3(0.51f, 1.38f, 0.15f);
                rightWingAsset.localRotation = Quaternion.identity;
                rightWingAsset.localScale = new Vector3(0.80f, 1.05f, 1.0f);
            }

            // Middle Screen on Right Wing: Sign_Next
            Transform signNext = stationGo.transform.Find("Sign_Next");
            if (signNext != null)
            {
                signNext.localPosition = new Vector3(4.30f, 4.40f, 0.19f);
                signNext.localRotation = Quaternion.Euler(0f, 24f, 0f);
                signNext.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            }

            // Bottom Screen on Right Wing: Achievements_Board
            Transform achieveBoard = stationGo.transform.Find("Achievements_Board");
            if (achieveBoard != null)
            {
                achieveBoard.localPosition = new Vector3(4.29f, 1.29f, 0.19f);
                achieveBoard.localRotation = Quaternion.Euler(0f, 24f, 0f);
                achieveBoard.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            }

            // Role / With Sign
            Transform signRole = stationGo.transform.Find("Sign_RoleWith");
            if (signRole != null)
            {
                signRole.gameObject.SetActive(true);
                signRole.localPosition = new Vector3(5.15f, 0.00f, 0.25f);
                signRole.localRotation = Quaternion.Euler(0f, 30.20f, 0f);
            }

            Transform retroTerm = stationGo.transform.Find("Retro_Terminal_Left");
            if (retroTerm != null) retroTerm.gameObject.SetActive(false);

            Transform serverTower = stationGo.transform.Find("Server_Tower_Right");
            if (serverTower != null) serverTower.gameObject.SetActive(false);

            // ─────────────────────────────────────────────────────────────────
            // F. ATMOSPHERIC ARCHITECTURAL & WORKSTATION LIGHTING
            // ─────────────────────────────────────────────────────────────────
            Transform existingPendants = stationGo.transform.Find("Chamber_Overhead_Pendant_Lights");
            if (existingPendants != null)
            {
                if (Application.isPlaying) Object.Destroy(existingPendants.gameObject);
                else Object.DestroyImmediate(existingPendants.gameObject);
            }

            // Only spawn atmospheric lights if About_Info_Station does not already have its own lighting setup
            if (stationGo.transform.Find("Chamber_Atmospheric_Lighting") == null)
            {
                Transform existingAtmosphere = chamberGo.transform.Find("Chamber_Atmospheric_Lighting");
                if (existingAtmosphere != null)
                {
                    if (Application.isPlaying) Object.Destroy(existingAtmosphere.gameObject);
                    else Object.DestroyImmediate(existingAtmosphere.gameObject);
                }

                var lightGroup = new GameObject("Chamber_Atmospheric_Lighting");
                lightGroup.transform.SetParent(chamberGo.transform, false);

                System.Action<string, Vector3, Color, float, float> spawnPointLight = (lName, lPos, lCol, lIntensity, lRange) =>
                {
                    var go = new GameObject(lName);
                    go.transform.SetParent(lightGroup.transform, false);
                    go.transform.position = lPos;
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = lCol;
                    l.intensity = lIntensity;
                    l.range = lRange;
                };

                System.Action<string, Vector3, Vector3, Color, float, float, float> spawnSpotLight = (lName, lPos, lRot, lCol, lIntensity, lRange, lAngle) =>
                {
                    var go = new GameObject(lName);
                    go.transform.SetParent(lightGroup.transform, false);
                    go.transform.position = lPos;
                    go.transform.rotation = Quaternion.Euler(lRot);
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Spot;
                    l.color = lCol;
                    l.intensity = lIntensity;
                    l.range = lRange;
                    l.spotAngle = lAngle;
                };

                // 1. High-Bay Central Ambient Fill
                spawnPointLight("Chamber_Center_Fill", new Vector3(35.0f, 5.8f, 23.0f), new Color(0.92f, 0.96f, 1.0f), 3.8f, 24.0f);

                // 1b. Video Wall Fill
                spawnPointLight("Chamber_VideoWall_Fill", new Vector3(39.0f, 5.5f, 23.0f), new Color(0.90f, 0.96f, 1.0f), 3.6f, 18.0f);

                // 2. North Wall Architectural Sconces
                spawnPointLight("NorthWall_Accent_A", new Vector3(31.5f, 3.6f, 28.2f), new Color(0.20f, 0.90f, 1.0f), 3.2f, 9.5f);
                spawnPointLight("NorthWall_Accent_B", new Vector3(37.0f, 3.6f, 28.2f), new Color(0.20f, 0.90f, 1.0f), 3.2f, 9.5f);

                // 3. South Wall Architectural Sconces
                spawnPointLight("SouthWall_Accent_A", new Vector3(31.5f, 3.6f, 17.8f), new Color(1.0f, 0.72f, 0.28f), 3.2f, 9.5f);
                spawnPointLight("SouthWall_Accent_B", new Vector3(37.0f, 3.6f, 17.8f), new Color(1.0f, 0.72f, 0.28f), 3.2f, 9.5f);

                // 4. Console Workstation Downlights
                spawnSpotLight("TaskLight_North_Fore", new Vector3(33.0f, 4.4f, 28.0f), new Vector3(85f, 0f, 0f), new Color(0.95f, 0.98f, 1.0f), 3.0f, 7.5f, 75f);
                spawnSpotLight("TaskLight_North_Aft", new Vector3(37.2f, 4.4f, 26.5f), new Vector3(85f, 0f, 0f), new Color(0.95f, 0.98f, 1.0f), 3.0f, 7.5f, 75f);
                spawnSpotLight("TaskLight_South_Fore", new Vector3(33.0f, 4.4f, 18.0f), new Vector3(85f, 0f, 0f), new Color(0.95f, 0.98f, 1.0f), 3.0f, 7.5f, 75f);
                spawnSpotLight("TaskLight_South_Aft", new Vector3(37.2f, 4.4f, 19.5f), new Vector3(85f, 0f, 0f), new Color(0.95f, 0.98f, 1.0f), 3.0f, 7.5f, 75f);

                // 4b. Overhead Downlight for Commander Chair & Center Console
                spawnSpotLight("Chamber_Commander_Desk_Spot", new Vector3(37.0f, 5.2f, 23.0f), new Vector3(90f, 0f, 0f), new Color(0.96f, 0.98f, 1.0f), 3.5f, 8.5f, 80f);

                // 5. Entrance Walkway Runner Downlight
                spawnSpotLight("Walkway_Entry_Downlight", new Vector3(28.0f, 4.5f, 23.0f), new Vector3(90f, 0f, 0f), new Color(0.90f, 0.96f, 1.0f), 3.0f, 8.0f, 80f);

                // 6. Video Wall Backing Halo Glow & Key Spotlight on Main Screen
                spawnPointLight("VideoWall_Backing_Halo", new Vector3(40.8f, 3.8f, 23.0f), new Color(0.15f, 0.85f, 1.0f), 2.8f, 9.0f);

                var screenSpotGo = new GameObject("Chamber_Screen_Spotlight");
                screenSpotGo.transform.SetParent(lightGroup.transform, false);
                screenSpotGo.transform.position = new Vector3(37.5f, 5.50f, 23.0f);
                screenSpotGo.transform.rotation = Quaternion.Euler(50f, 90f, 0f);
                var ss = screenSpotGo.AddComponent<Light>();
                ss.type = LightType.Spot;
                ss.range = 12f;
                ss.spotAngle = 75f;
                ss.intensity = 4.8f;
                ss.color = new Color(0.96f, 0.98f, 1.0f);
            }

            // ─────────────────────────────────────────────────────────────────
            // G. EXACT CAMERA VIEWPOINT & LOOK TARGET (LOCKED & PRESERVED)
            // G. EXACT CAMERA VIEWPOINT & LOOK TARGET (LOCKED TO USER APPROVED STATE)
            // ─────────────────────────────────────────────────────────────────
            Transform camView = stationGo.transform.Find("Camera_ViewPoint");
            if (camView != null) camView.localPosition = new Vector3(0f, 2.40f, -7.00f);

            Transform camLook = stationGo.transform.Find("Camera_LookTarget");
            if (camLook != null) camLook.localPosition = new Vector3(0f, 2.25f, 0.85f);

            var infoComp = stationGo.GetComponent<AboutInfoStation>();
            if (infoComp != null)
            {
                infoComp.viewFov = 52f;
                if (camView != null) infoComp.cameraViewPoint = camView;
                if (camLook != null) infoComp.cameraLookTarget = camLook;

                var lp = GameObject.Find("LandingPad_Center");
                if (lp != null) infoComp.landingPadCenter = lp.transform;

                var beaconsGo = GameObject.Find("LandingLights");
                if (beaconsGo != null) infoComp.landingBeacons = beaconsGo.GetComponentsInChildren<Renderer>();
            }
        }

        private static Mesh CreateProjectionConeMesh(float bottomRadius, float topRadius, float height, int segments = 32)
        {
            var mesh = new Mesh();
            mesh.name = "Hologram_Projection_Cone_Mesh";

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            float yBottom = 0f;
            float yTop = height;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                int idxB = i * 2;
                vertices[idxB] = new Vector3(cos * bottomRadius, yBottom, sin * bottomRadius);
                normals[idxB] = new Vector3(cos, 0.2f, sin).normalized;
                uvs[idxB] = new Vector2(t, 0f);

                int idxT = i * 2 + 1;
                vertices[idxT] = new Vector3(cos * topRadius, yTop, sin * topRadius);
                normals[idxT] = new Vector3(cos, 0.2f, sin).normalized;
                uvs[idxT] = new Vector2(t, 1f);
            }

            int triIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int b0 = i * 2;
                int t0 = i * 2 + 1;
                int b1 = (i + 1) * 2;
                int t1 = (i + 1) * 2 + 1;

                triangles[triIdx++] = b0;
                triangles[triIdx++] = t0;
                triangles[triIdx++] = b1;

                triangles[triIdx++] = b1;
                triangles[triIdx++] = t0;
                triangles[triIdx++] = t1;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateHologramRingMesh(float innerRadius, float outerRadius, int segments = 64)
        {
            var mesh = new Mesh();
            mesh.name = "Hologram_Ring_Mesh";

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Inner vertex (open hole in center)
                int idxIn = i * 2;
                vertices[idxIn] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
                normals[idxIn] = Vector3.up;
                uvs[idxIn] = new Vector2(t, 0f);

                // Outer vertex
                int idxOut = i * 2 + 1;
                vertices[idxOut] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
                normals[idxOut] = Vector3.up;
                uvs[idxOut] = new Vector2(t, 1f);
            }

            int triIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int in0 = i * 2;
                int out0 = i * 2 + 1;
                int in1 = (i + 1) * 2;
                int out1 = (i + 1) * 2 + 1;

                triangles[triIdx++] = in0;
                triangles[triIdx++] = out0;
                triangles[triIdx++] = in1;

                triangles[triIdx++] = in1;
                triangles[triIdx++] = out0;
                triangles[triIdx++] = out1;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateDownFacingDiscMesh(float radius, int segments = 48)
        {
            var m = new Mesh();
            m.name = "CeilingOculusDisc";
            var verts = new Vector3[segments + 1];
            var uvs = new Vector2[segments + 1];
            var tris = new int[segments * 3];

            verts[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float rad = (i * angleStep) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                verts[i + 1] = new Vector3(cos * radius, 0f, sin * radius);
                uvs[i + 1] = new Vector2(0.5f + 0.44f * cos, 0.5f + 0.44f * sin);

                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % segments + 1;
            }
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateNormals();
            return m;
        }

        private static Mesh CreateCeilingProjectionConeMesh(float rTop, float rBot, float height, int segments = 32)
        {
            var coneMesh = new Mesh();
            coneMesh.name = "HoloCeilingBeamCone";
            var cVerts = new Vector3[(segments + 1) * 2];
            var cUvs = new Vector2[(segments + 1) * 2];
            var cTris = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments;
                float rad = u * Mathf.PI * 2f;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                cVerts[i * 2] = new Vector3(cos * rTop, 0f, sin * rTop);
                cUvs[i * 2] = new Vector2(u, 0f);

                cVerts[i * 2 + 1] = new Vector3(cos * rBot, -height, sin * rBot);
                cUvs[i * 2 + 1] = new Vector2(u, 1f);
            }
            int cTri = 0;
            for (int i = 0; i < segments; i++)
            {
                int v0 = i * 2;
                int v1 = i * 2 + 1;
                int v2 = (i + 1) * 2;
                int v3 = (i + 1) * 2 + 1;

                cTris[cTri++] = v0; cTris[cTri++] = v1; cTris[cTri++] = v2;
                cTris[cTri++] = v2; cTris[cTri++] = v1; cTris[cTri++] = v3;
            }
            coneMesh.vertices = cVerts;
            coneMesh.uv = cUvs;
            coneMesh.triangles = cTris;
            coneMesh.RecalculateNormals();
            return coneMesh;
        }

        private static void BuildCommandBridgeHoloTable(
            Transform parent, Vector3 centerPos,
            GameObject projectorPrefab, GameObject compStation, GameObject pilotSeat,
            Material titaniumMat, Material steelMat, Material cyanEmissiveMat, Material coreGlowMat,
            Material holoGlassMat, Material beamMat, Material oculusMat)
        {
            var holoTableRoot = new GameObject("Zone1_CommandBridge_HoloTable");
            holoTableRoot.transform.SetParent(parent, false);
            holoTableRoot.transform.position = centerPos;

            // ─────────────────────────────────────────────────────────────────
            // 0. GRAND ELEVATED COMMAND PODIUM / DAIS (Đài chỉ huy nâng cao 8.6m)
            // ─────────────────────────────────────────────────────────────────
            var dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dais.name = "Command_Dais_Base";
            dais.transform.SetParent(holoTableRoot.transform, false);
            dais.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            dais.transform.localScale = new Vector3(8.60f, 0.07f, 8.60f);
            if (titaniumMat != null) dais.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            var daisRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            daisRim.name = "Command_Dais_Neon_Rim";
            daisRim.transform.SetParent(holoTableRoot.transform, false);
            daisRim.transform.localPosition = new Vector3(0f, 0.142f, 0f);
            daisRim.transform.localScale = new Vector3(8.75f, 0.005f, 8.75f);
            Object.DestroyImmediate(daisRim.GetComponent<Collider>());
            if (cyanEmissiveMat != null) daisRim.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 4 Symmetrical Beveled Steps (West, East, North, South)
            Vector3[] stepOffsets = new Vector3[] {
                new Vector3(-4.30f, 0.04f, 0f),
                new Vector3(4.30f, 0.04f, 0f),
                new Vector3(0f, 0.04f, -4.30f),
                new Vector3(0f, 0.04f, 4.30f)
            };
            float[] stepAngles = new float[] { 90f, 90f, 0f, 0f };
            for (int s = 0; s < stepOffsets.Length; s++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Dais_Step_{s}";
                step.transform.SetParent(holoTableRoot.transform, false);
                step.transform.localPosition = stepOffsets[s];
                step.transform.localRotation = Quaternion.Euler(0f, stepAngles[s], 0f);
                step.transform.localScale = new Vector3(2.40f, 0.08f, 0.50f);
                if (steelMat != null) step.GetComponent<Renderer>().sharedMaterial = steelMat;
            }

            // 8 Perimeter Stanchion Beacons around Dais
            for (int b = 0; b < 8; b++)
            {
                float bAngle = b * 45f + 22.5f;
                var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beacon.name = $"Dais_Perimeter_Beacon_{b}";
                beacon.transform.SetParent(holoTableRoot.transform, false);
                beacon.transform.localPosition = Quaternion.Euler(0f, bAngle, 0f) * new Vector3(0f, 0.15f, 4.45f);
                beacon.transform.localScale = new Vector3(0.24f, 0.15f, 0.24f);
                Object.DestroyImmediate(beacon.GetComponent<Collider>());
                if (titaniumMat != null) beacon.GetComponent<Renderer>().sharedMaterial = titaniumMat;

                var beaconLight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                beaconLight.name = "Beacon_LED";
                beaconLight.transform.SetParent(beacon.transform, false);
                beaconLight.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                beaconLight.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
                Object.DestroyImmediate(beaconLight.GetComponent<Collider>());
                if (cyanEmissiveMat != null) beaconLight.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // ─────────────────────────────────────────────────────────────────
            // 1. GRAND 8-SEAT SCI-FI CIRCULAR COMMAND TABLE & BASIN
            // ─────────────────────────────────────────────────────────────────
            var tableModelRoot = new GameObject("Command_Bridge_Round_Table");
            tableModelRoot.transform.SetParent(holoTableRoot.transform, false);
            tableModelRoot.transform.localPosition = new Vector3(0f, 0.14f, 0f);

            // 1.1 Heavy Foundation Plinth (Đế tròn titan mở rộng 4.8m)
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Table_Plinth_Base";
            plinth.transform.SetParent(tableModelRoot.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            plinth.transform.localScale = new Vector3(4.80f, 0.04f, 4.80f); // 4.8m diameter, 0.08m height
            if (titaniumMat != null) plinth.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            // 1.2 Perimeter Neon Cyan Glow Ring (Vành LED neon sàn 4.95m)
            var plinthGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinthGlow.name = "Table_Plinth_Neon_Rim";
            plinthGlow.transform.SetParent(tableModelRoot.transform, false);
            plinthGlow.transform.localPosition = new Vector3(0f, 0.082f, 0f);
            plinthGlow.transform.localScale = new Vector3(4.94f, 0.008f, 4.94f);
            Object.DestroyImmediate(plinthGlow.GetComponent<Collider>());
            if (cyanEmissiveMat != null) plinthGlow.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 1.3 Central Pedestal Column (Trụ đỡ bàn tròn 3.4m)
            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Table_Center_Pedestal";
            column.transform.SetParent(tableModelRoot.transform, false);
            column.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            column.transform.localScale = new Vector3(3.40f, 0.33f, 3.40f); // height 0.66m
            if (steelMat != null) column.GetComponent<Renderer>().sharedMaterial = steelMat;

            // 1.4 Radial Reinforcement Ribs & Neon Conduits (8 gân trợ lực phát quang)
            for (int r = 0; r < 8; r++)
            {
                float angle = r * 45f;
                var rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rib.name = $"Table_Pedestal_Rib_{r}";
                rib.transform.SetParent(tableModelRoot.transform, false);
                rib.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(1.75f, 0.42f, 0f);
                rib.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                rib.transform.localScale = new Vector3(0.12f, 0.65f, 0.35f);
                if (titaniumMat != null) rib.GetComponent<Renderer>().sharedMaterial = titaniumMat;

                var ribNeon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ribNeon.name = $"Table_Pedestal_Neon_{r}";
                ribNeon.transform.SetParent(tableModelRoot.transform, false);
                ribNeon.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(1.82f, 0.42f, 0f);
                ribNeon.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                ribNeon.transform.localScale = new Vector3(0.04f, 0.60f, 0.08f);
                Object.DestroyImmediate(ribNeon.GetComponent<Collider>());
                if (cyanEmissiveMat != null) ribNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // 1.5 Main Circular Console Deck (Mặt bàn tròn chính 4.5m)
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deck.name = "Table_Round_Deck";
            deck.transform.SetParent(tableModelRoot.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0.77f, 0f);
            deck.transform.localScale = new Vector3(4.50f, 0.04f, 4.50f); // diameter 4.5m, height 0.08m
            if (titaniumMat != null) deck.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            // 1.6 Outer Chamfered Steel Ring (Vành viền bo ngoài kim loại 4.62m)
            var deckRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deckRim.name = "Table_Outer_Bevel_Rim";
            deckRim.transform.SetParent(tableModelRoot.transform, false);
            deckRim.transform.localPosition = new Vector3(0f, 0.805f, 0f);
            deckRim.transform.localScale = new Vector3(4.62f, 0.015f, 4.62f);
            Object.DestroyImmediate(deckRim.GetComponent<Collider>());
            if (steelMat != null) deckRim.GetComponent<Renderer>().sharedMaterial = steelMat;

            // 1.7 Deck Perimeter Cyan Neon Ring (Đường viền neon mặt bàn)
            var deckGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deckGlow.name = "Table_Deck_Perimeter_Neon";
            deckGlow.transform.SetParent(tableModelRoot.transform, false);
            deckGlow.transform.localPosition = new Vector3(0f, 0.812f, 0f);
            deckGlow.transform.localScale = new Vector3(4.48f, 0.005f, 4.48f);
            Object.DestroyImmediate(deckGlow.GetComponent<Collider>());
            if (cyanEmissiveMat != null) deckGlow.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 1.8 Recessed Central Hologram Projector Basin / Well (BỒN TRÒN TRUNG TÂM 2.3m)
            var basinOuterLip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basinOuterLip.name = "Table_Basin_Outer_Lip";
            basinOuterLip.transform.SetParent(tableModelRoot.transform, false);
            basinOuterLip.transform.localPosition = new Vector3(0f, 0.818f, 0f);
            basinOuterLip.transform.localScale = new Vector3(2.30f, 0.012f, 2.30f);
            Object.DestroyImmediate(basinOuterLip.GetComponent<Collider>());
            if (steelMat != null) basinOuterLip.GetComponent<Renderer>().sharedMaterial = steelMat;

            var basinGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basinGlow.name = "Table_Basin_Neon_Rim";
            basinGlow.transform.SetParent(tableModelRoot.transform, false);
            basinGlow.transform.localPosition = new Vector3(0f, 0.822f, 0f);
            basinGlow.transform.localScale = new Vector3(2.20f, 0.006f, 2.20f);
            Object.DestroyImmediate(basinGlow.GetComponent<Collider>());
            if (cyanEmissiveMat != null) basinGlow.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var basinSunkenFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basinSunkenFloor.name = "Table_Basin_Sunken_Floor";
            basinSunkenFloor.transform.SetParent(tableModelRoot.transform, false);
            basinSunkenFloor.transform.localPosition = new Vector3(0f, 0.795f, 0f);
            basinSunkenFloor.transform.localScale = new Vector3(2.05f, 0.015f, 2.05f);
            Object.DestroyImmediate(basinSunkenFloor.GetComponent<Collider>());
            if (titaniumMat != null) basinSunkenFloor.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            // 1.9 Center Projector Optical Lens Aperture
            var emitterBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitterBase.name = "Holo_Projector_Lens_Mount";
            emitterBase.transform.SetParent(tableModelRoot.transform, false);
            emitterBase.transform.localPosition = new Vector3(0f, 0.815f, 0f);
            emitterBase.transform.localScale = new Vector3(0.85f, 0.020f, 0.85f);
            Object.DestroyImmediate(emitterBase.GetComponent<Collider>());
            if (steelMat != null) emitterBase.GetComponent<Renderer>().sharedMaterial = steelMat;

            var emitterGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitterGlow.name = "Holo_Projector_Core_Aperture";
            emitterGlow.transform.SetParent(tableModelRoot.transform, false);
            emitterGlow.transform.localPosition = new Vector3(0f, 0.825f, 0f);
            emitterGlow.transform.localScale = new Vector3(0.65f, 0.010f, 0.65f);
            Object.DestroyImmediate(emitterGlow.GetComponent<Collider>());
            if (coreGlowMat != null) emitterGlow.GetComponent<Renderer>().sharedMaterial = coreGlowMat;
            else if (cyanEmissiveMat != null) emitterGlow.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // ─────────────────────────────────────────────────────────────────
            // 2. TRUE 3D VOLUMETRIC HOLOGRAPHIC PLANET & CITADEL POWER REACTOR
            // (Unity Shader Graph - Hologram Tutorial Style)
            // ─────────────────────────────────────────────────────────────────
            var sgPlanetMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Hologram_Planet_ShaderGraph.mat");
            if (beamMat == null) beamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Hologram_Projection_Beam.mat");
            var orbitRingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Hologram_Orbit_Ring.mat");

            var holoPlanetGo = new GameObject("HoloTable_Planetary_Hologram");
            holoPlanetGo.transform.SetParent(holoTableRoot.transform, false);
            holoPlanetGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);

            // 2.0 Holographic Upward Projection Cone Beam (Tia chiếu 3D từ bồn tròn)
            var beamGo = new GameObject("Holo_Projector_Light_Cone");
            beamGo.transform.SetParent(tableModelRoot.transform, false);
            beamGo.transform.localPosition = new Vector3(0f, 0.83f, 0f);
            var beamMf = beamGo.AddComponent<MeshFilter>();
            beamMf.sharedMesh = CreateProjectionConeMesh(0.30f, 0.90f, 0.82f, 32);
            var beamMr = beamGo.AddComponent<MeshRenderer>();
            if (beamMat != null) beamMr.sharedMaterial = beamMat;
            else if (cyanEmissiveMat != null) beamMr.sharedMaterial = cyanEmissiveMat;

            // 2.1 3D Shader Graph Hologram Planet Globe
            var planetCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetCore.name = "Planet_Core";
            planetCore.transform.SetParent(holoPlanetGo.transform, false);
            planetCore.transform.localPosition = Vector3.zero;
            planetCore.transform.localScale = new Vector3(1.30f, 1.30f, 1.30f); // Translucent hologram globe
            Object.DestroyImmediate(planetCore.GetComponent<Collider>());
            if (sgPlanetMat != null) planetCore.GetComponent<Renderer>().sharedMaterial = sgPlanetMat;
            else if (cyanEmissiveMat != null) planetCore.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 2.2 Inner Equatorial Holographic Data Ring (Horizontal Annulus Hoop)
            var ringA = new GameObject("Orbit_Ring_Inner");
            ringA.transform.SetParent(holoPlanetGo.transform, false);
            ringA.transform.localPosition = Vector3.zero;
            ringA.transform.localRotation = Quaternion.identity;
            var mfA = ringA.AddComponent<MeshFilter>();
            mfA.sharedMesh = CreateHologramRingMesh(0.85f, 0.98f, 64); // Open ring: planet sits inside hole!
            var mrA = ringA.AddComponent<MeshRenderer>();
            if (orbitRingMat != null) mrA.sharedMaterial = orbitRingMat;
            else if (cyanEmissiveMat != null) mrA.sharedMaterial = cyanEmissiveMat;

            // 2.3 Outer Tilted Holographic Orbit Ring (Thin Annulus Hoop)
            var ringB = new GameObject("Orbit_Ring_Outer");
            ringB.transform.SetParent(holoPlanetGo.transform, false);
            ringB.transform.localPosition = Vector3.zero;
            ringB.transform.localRotation = Quaternion.Euler(20f, 35f, 0f);
            var mfB = ringB.AddComponent<MeshFilter>();
            mfB.sharedMesh = CreateHologramRingMesh(1.22f, 1.28f, 64); // Delicate 6cm wide orbital tracking ring!
            var mrB = ringB.AddComponent<MeshRenderer>();
            if (orbitRingMat != null) mrB.sharedMaterial = orbitRingMat;
            else if (cyanEmissiveMat != null) mrB.sharedMaterial = cyanEmissiveMat;

            // 2.3b Orbiting Satellite Node on Ring B
            var satNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
            satNode.name = "Holo_Satellite_Node";
            satNode.transform.SetParent(ringB.transform, false);
            satNode.transform.localPosition = new Vector3(1.25f, 0f, 0f); // Sits right on the thin outer ring!
            satNode.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            satNode.transform.localScale = new Vector3(0.045f, 0.045f, 0.045f);
            Object.DestroyImmediate(satNode.GetComponent<Collider>());
            if (cyanEmissiveMat != null) satNode.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 2.4 Vertical Laser Scan Beam / Axis Needle
            var scanBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scanBeam.name = "Laser_Scan_Beam";
            scanBeam.transform.SetParent(holoPlanetGo.transform, false);
            scanBeam.transform.localPosition = Vector3.zero;
            scanBeam.transform.localScale = new Vector3(0.015f, 0.85f, 0.015f);
            Object.DestroyImmediate(scanBeam.GetComponent<Collider>());
            if (cyanEmissiveMat != null) scanBeam.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 2.5 Ambient Hologram Light
            var holoLight = holoPlanetGo.AddComponent<Light>();
            holoLight.type = LightType.Point;
            holoLight.range = 6.5f;
            holoLight.intensity = 3.0f;
            holoLight.color = new Color(0.2f, 0.90f, 1.0f);

            // 2.6 Rotator Script
            var rotator = holoPlanetGo.AddComponent<HologramPlanetRotator>();
            rotator.planetCore = planetCore.transform;
            rotator.innerRing = ringA.transform;
            rotator.outerRing = ringB.transform;
            rotator.scanBeam = scanBeam.transform;
            rotator.holoSpotLight = holoLight;
            rotator.planetSpinSpeed = 16f;
            rotator.innerRingSpeed = -22f;
            rotator.outerRingSpeed = 18f;

            // 2.7 Citadel Central Power Reactor Core (Destructible from spaceship laser)
            var sphereCol = planetCore.AddComponent<SphereCollider>();
            sphereCol.radius = 0.65f;

            var reactorComp = holoPlanetGo.AddComponent<CitadelPowerReactorCore>();
            reactorComp.maxHealth = 180f;
            reactorComp.currentHealth = 180f;
            reactorComp.coreSphere = planetCore.transform;
            reactorComp.ringA = ringA.transform;
            reactorComp.ringB = ringB.transform;
            reactorComp.coreLight = holoLight;

            // ─────────────────────────────────────────────────────────────────
            // 3. ERGONOMIC COMMANDER SEATS (8 RADIAL POSITIONS - AISLES OPEN)
            // ─────────────────────────────────────────────────────────────────
            // 8 seats offset by 22.5° so the main corridors (West-East & North-South) stay 100% unobstructed
            float[] seatAngles = new float[] { 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f, 337.5f };
            float seatRadius = 2.85f;

            if (pilotSeat != null)
            {
                for (int s = 0; s < seatAngles.Length; s++)
                {
                    float angle = seatAngles[s];
                    Vector3 seatPos = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, seatRadius);
                    seatPos.y = 0.14f; // Elevated onto the Grand Dais
                    Quaternion seatRot = Quaternion.LookRotation(-new Vector3(seatPos.x, 0f, seatPos.z).normalized, Vector3.up);

                    var seatGo = Object.Instantiate(pilotSeat, holoTableRoot.transform, false);
                    seatGo.name = $"Commander_Seat_{s + 1}";
                    seatGo.transform.localPosition = seatPos;
                    seatGo.transform.localRotation = seatRot;
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 4. INTEGRATED TACTICAL TOUCH CONSOLES ON ROUND TABLE RIM (8 STATIONS)
            // ─────────────────────────────────────────────────────────────────
            float consoleRadius = 1.82f;
            for (int c = 0; c < seatAngles.Length; c++)
            {
                float angle = seatAngles[c];
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, consoleRadius);
                var consolePos = offset + new Vector3(0f, 0.825f, 0f);

                var cGo = new GameObject($"Table_Tactical_Console_{c + 1}");
                cGo.transform.SetParent(tableModelRoot.transform, false);
                cGo.transform.localPosition = consolePos;
                cGo.transform.localRotation = Quaternion.Euler(0f, angle + 180f, 0f);

                // Console bevel wedge housing
                var panelHousing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panelHousing.name = "Panel_Housing";
                panelHousing.transform.SetParent(cGo.transform, false);
                panelHousing.transform.localPosition = Vector3.zero;
                panelHousing.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                panelHousing.transform.localScale = new Vector3(0.46f, 0.025f, 0.28f);
                if (steelMat != null) panelHousing.GetComponent<Renderer>().sharedMaterial = steelMat;

                // Glowing touch screen surface
                var touchScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                touchScreen.name = "Touch_Screen";
                touchScreen.transform.SetParent(cGo.transform, false);
                touchScreen.transform.localPosition = panelHousing.transform.localRotation * new Vector3(0f, 0.015f, 0f);
                touchScreen.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                touchScreen.transform.localScale = new Vector3(0.40f, 0.005f, 0.23f);
                Object.DestroyImmediate(touchScreen.GetComponent<Collider>());
                if (cyanEmissiveMat != null) touchScreen.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // ─────────────────────────────────────────────────────────────────
            // 5. MASTER CITADEL QUANTUM-HOLO PROJECTOR & HIGH-BAY CHANDELIER (Y = 7.5m)
            // ─────────────────────────────────────────────────────────────────
            var ceilRingRoot = new GameObject("Overhead_Holo_Scanner_Ceiling_Ring");
            ceilRingRoot.transform.SetParent(holoTableRoot.transform, false);
            ceilRingRoot.transform.localPosition = new Vector3(0f, 7.50f, 0f);

            // 5.1 Top Ceiling Mounting Baseplate & Neon Rim
            var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlate.name = "Ceiling_Mounting_Baseplate";
            basePlate.transform.SetParent(ceilRingRoot.transform, false);
            basePlate.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            basePlate.transform.localScale = new Vector3(5.80f, 0.05f, 5.80f);
            Object.DestroyImmediate(basePlate.GetComponent<Collider>());
            if (titaniumMat != null) basePlate.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            var baseNeon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseNeon.name = "Baseplate_Neon_Rim";
            baseNeon.transform.SetParent(ceilRingRoot.transform, false);
            baseNeon.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            baseNeon.transform.localScale = new Vector3(5.90f, 0.02f, 5.90f);
            Object.DestroyImmediate(baseNeon.GetComponent<Collider>());
            if (cyanEmissiveMat != null) baseNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 5.2 8 Diagonal Hydraulic Suspension Cantilever Arms
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                float rad = ang * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = $"Suspension_Cantilever_Arm_{i}";
                arm.transform.SetParent(ceilRingRoot.transform, false);
                arm.transform.localPosition = new Vector3(cos * 2.65f, 0.18f, sin * 2.65f);
                arm.transform.localRotation = Quaternion.Euler(-18f * cos, ang, 18f * sin);
                arm.transform.localScale = new Vector3(0.18f, 0.28f, 0.20f);
                Object.DestroyImmediate(arm.GetComponent<Collider>());
                if (steelMat != null) arm.GetComponent<Renderer>().sharedMaterial = steelMat;

                var piston = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                piston.name = $"Hydraulic_Piston_{i}";
                piston.transform.SetParent(arm.transform, false);
                piston.transform.localPosition = new Vector3(0f, 0f, 0.6f);
                piston.transform.localScale = new Vector3(0.35f, 0.55f, 0.35f);
                Object.DestroyImmediate(piston.GetComponent<Collider>());
                if (coreGlowMat != null) piston.GetComponent<Renderer>().sharedMaterial = coreGlowMat;
            }

            // 5.3 Outer Armored Ring Chassis & Dual Stacked Perimeter Neons
            var outerChassis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerChassis.name = "Outer_Armored_Chassis";
            outerChassis.transform.SetParent(ceilRingRoot.transform, false);
            outerChassis.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            outerChassis.transform.localScale = new Vector3(5.30f, 0.18f, 5.30f);
            Object.DestroyImmediate(outerChassis.GetComponent<Collider>());
            if (titaniumMat != null) outerChassis.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            var outerNeonUpper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerNeonUpper.name = "Outer_Neon_Upper";
            outerNeonUpper.transform.SetParent(ceilRingRoot.transform, false);
            outerNeonUpper.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            outerNeonUpper.transform.localScale = new Vector3(5.38f, 0.02f, 5.38f);
            Object.DestroyImmediate(outerNeonUpper.GetComponent<Collider>());
            if (cyanEmissiveMat != null) outerNeonUpper.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var outerNeonLower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerNeonLower.name = "Outer_Neon_Lower";
            outerNeonLower.transform.SetParent(ceilRingRoot.transform, false);
            outerNeonLower.transform.localPosition = new Vector3(0f, -0.07f, 0f);
            outerNeonLower.transform.localScale = new Vector3(5.38f, 0.02f, 5.38f);
            Object.DestroyImmediate(outerNeonLower.GetComponent<Collider>());
            if (cyanEmissiveMat != null) outerNeonLower.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 8 Exterior Heavy-Duty Locking Clamps with Status LEDs
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                float rad = ang * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                var clamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clamp.name = $"Outer_Clamp_{i}";
                clamp.transform.SetParent(ceilRingRoot.transform, false);
                clamp.transform.localPosition = new Vector3(cos * 2.70f, 0.02f, sin * 2.70f);
                clamp.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                clamp.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f);
                Object.DestroyImmediate(clamp.GetComponent<Collider>());
                if (steelMat != null) clamp.GetComponent<Renderer>().sharedMaterial = steelMat;

                var led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                led.name = "Clamp_LED";
                led.transform.SetParent(clamp.transform, false);
                led.transform.localPosition = new Vector3(0f, 0f, 0.52f);
                led.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                Object.DestroyImmediate(led.GetComponent<Collider>());
                if (cyanEmissiveMat != null) led.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // 5.4 Underside Stepped Concentric Neons & Energy Ribs
            var outerBottomNeon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerBottomNeon.name = "Outer_Bottom_Neon_Ring";
            outerBottomNeon.transform.SetParent(ceilRingRoot.transform, false);
            outerBottomNeon.transform.localPosition = new Vector3(0f, -0.065f, 0f);
            outerBottomNeon.transform.localScale = new Vector3(4.90f, 0.015f, 4.90f);
            Object.DestroyImmediate(outerBottomNeon.GetComponent<Collider>());
            if (cyanEmissiveMat != null) outerBottomNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var midCollar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            midCollar.name = "Intermediate_Recessed_Collar";
            midCollar.transform.SetParent(ceilRingRoot.transform, false);
            midCollar.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            midCollar.transform.localScale = new Vector3(4.20f, 0.08f, 4.20f);
            Object.DestroyImmediate(midCollar.GetComponent<Collider>());
            if (titaniumMat != null) midCollar.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            var midBottomNeon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            midBottomNeon.name = "Middle_Bottom_Neon_Ring";
            midBottomNeon.transform.SetParent(ceilRingRoot.transform, false);
            midBottomNeon.transform.localPosition = new Vector3(0f, -0.115f, 0f);
            midBottomNeon.transform.localScale = new Vector3(3.90f, 0.015f, 3.90f);
            Object.DestroyImmediate(midBottomNeon.GetComponent<Collider>());
            if (cyanEmissiveMat != null) midBottomNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var midNeon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            midNeon.name = "Intermediate_Neon_Ring";
            midNeon.transform.SetParent(ceilRingRoot.transform, false);
            midNeon.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            midNeon.transform.localScale = new Vector3(4.26f, 0.02f, 4.26f);
            Object.DestroyImmediate(midNeon.GetComponent<Collider>());
            if (coreGlowMat != null) midNeon.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

            // 8 Radial Glowing Energy Ribs bridging outer to middle collar
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                float rad = ang * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                var rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rib.name = $"Underside_Energy_Rib_{i}";
                rib.transform.SetParent(ceilRingRoot.transform, false);
                rib.transform.localPosition = new Vector3(cos * 2.20f, -0.068f, sin * 2.20f);
                rib.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                rib.transform.localScale = new Vector3(0.08f, 0.02f, 0.55f);
                Object.DestroyImmediate(rib.GetComponent<Collider>());
                if (coreGlowMat != null) rib.GetComponent<Renderer>().sharedMaterial = coreGlowMat;
            }

            // 5.5 8 Radial Tactical Holographic Projector Pods (Facing Down-Inward at 25 deg!)
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f + 22.5f;
                float rad = ang * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                var podGo = new GameObject($"Tactical_Holo_Projector_Pod_{i}");
                podGo.transform.SetParent(ceilRingRoot.transform, false);
                podGo.transform.localPosition = new Vector3(cos * 2.05f, -0.08f, sin * 2.05f);
                podGo.transform.localRotation = Quaternion.Euler(25f, -ang - 90f, 0f);

                var podBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                podBody.name = "Pod_Body";
                podBody.transform.SetParent(podGo.transform, false);
                podBody.transform.localPosition = Vector3.zero;
                podBody.transform.localScale = new Vector3(0.20f, 0.18f, 0.36f);
                Object.DestroyImmediate(podBody.GetComponent<Collider>());
                if (steelMat != null) podBody.GetComponent<Renderer>().sharedMaterial = steelMat;

                var podLens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                podLens.name = "Pod_Optical_Lens";
                podLens.transform.SetParent(podGo.transform, false);
                podLens.transform.localPosition = new Vector3(0f, 0f, 0.19f);
                podLens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                podLens.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
                Object.DestroyImmediate(podLens.GetComponent<Collider>());
                if (coreGlowMat != null) podLens.GetComponent<Renderer>().sharedMaterial = coreGlowMat;
            }

            // 5.6 Rotating Mechanical Turbine Blades Array
            var turbineGroup = new GameObject("Turbine_Mechanical_Rotor");
            turbineGroup.transform.SetParent(ceilRingRoot.transform, false);
            turbineGroup.transform.localPosition = new Vector3(0f, -0.09f, 0f);

            for (int b = 0; b < 12; b++)
            {
                float ang = b * 30f;
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Turbine_Blade_{b}";
                blade.transform.SetParent(turbineGroup.transform, false);
                blade.transform.localPosition = Quaternion.Euler(0f, ang, 0f) * new Vector3(1.68f, 0f, 0f);
                blade.transform.localRotation = Quaternion.Euler(35f, ang, 0f);
                blade.transform.localScale = new Vector3(0.10f, 0.03f, 0.36f);
                Object.DestroyImmediate(blade.GetComponent<Collider>());
                if (steelMat != null) blade.GetComponent<Renderer>().sharedMaterial = steelMat;

                var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.name = "Blade_Tip_Glow";
                tip.transform.SetParent(blade.transform, false);
                tip.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                tip.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                Object.DestroyImmediate(tip.GetComponent<Collider>());
                if (cyanEmissiveMat != null) tip.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // 5.7 Inner Projector Aperture Rim & Bezel
            var innerRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerRim.name = "Inner_Projector_Aperture_Rim";
            innerRim.transform.SetParent(ceilRingRoot.transform, false);
            innerRim.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            innerRim.transform.localScale = new Vector3(3.20f, 0.04f, 3.20f);
            Object.DestroyImmediate(innerRim.GetComponent<Collider>());
            if (titaniumMat != null) innerRim.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            var innerNeon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerNeon.name = "Inner_Aperture_Neon_Ring";
            innerNeon.transform.SetParent(ceilRingRoot.transform, false);
            innerNeon.transform.localPosition = new Vector3(0f, -0.138f, 0f);
            innerNeon.transform.localScale = new Vector3(3.24f, 0.02f, 3.24f);
            Object.DestroyImmediate(innerNeon.GetComponent<Collider>());
            if (coreGlowMat != null) innerNeon.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

            // 5.8 MASTER LUMINOUS ANIMATED HOLOGRAPHIC OCULUS DISC (3.0m diameter)
            var oculusGo = new GameObject("Master_Holo_Oculus_Disc");
            oculusGo.transform.SetParent(ceilRingRoot.transform, false);
            oculusGo.transform.localPosition = new Vector3(0f, -0.165f, 0f);
            var oculusMf = oculusGo.AddComponent<MeshFilter>();
            oculusMf.sharedMesh = CreateDownFacingDiscMesh(1.50f, 48);
            var oculusMr = oculusGo.AddComponent<MeshRenderer>();
            if (oculusMat != null) oculusMr.sharedMaterial = oculusMat;

            // 5.9 Downward Volumetric Holographic Projection Cone
            if (beamMat != null)
            {
                var ceilBeamGo = new GameObject("Ceiling_Downward_Holo_Beam_Cone");
                ceilBeamGo.transform.SetParent(ceilRingRoot.transform, false);
                ceilBeamGo.transform.localPosition = new Vector3(0f, -0.170f, 0f);
                var ceilBeamMf = ceilBeamGo.AddComponent<MeshFilter>();
                ceilBeamMf.sharedMesh = CreateCeilingProjectionConeMesh(1.35f, 0.70f, 5.25f, 32);
                var ceilBeamMr = ceilBeamGo.AddComponent<MeshRenderer>();
                ceilBeamMr.sharedMaterial = beamMat;
            }

            // 5.11 Comprehensive High-Bay Architectural Lighting Suite
            var spotGo = new GameObject("HoloTable_Overhead_Spotlight");
            spotGo.transform.SetParent(ceilRingRoot.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            spotGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 8.5f;
            spot.spotAngle = 48f;
            spot.intensity = 5.2f;
            spot.color = new Color(0.20f, 0.88f, 1.0f);

            for (int i = 0; i < 4; i++)
            {
                float ang = i * 90f;
                float rad = ang * Mathf.Deg2Rad;
                var dGo = new GameObject($"Perimeter_Accent_Downlight_{i}");
                dGo.transform.SetParent(ceilRingRoot.transform, false);
                dGo.transform.localPosition = new Vector3(Mathf.Cos(rad) * 1.9f, -0.15f, Mathf.Sin(rad) * 1.9f);
                dGo.transform.localRotation = Quaternion.Euler(75f * Mathf.Sin(rad) + 90f * (1f - Mathf.Abs(Mathf.Sin(rad))), ang, 0f);
                var dLight = dGo.AddComponent<Light>();
                dLight.type = LightType.Spot;
                dLight.range = 7.5f;
                dLight.spotAngle = 36f;
                dLight.intensity = 2.4f;
                dLight.color = new Color(0.70f, 0.95f, 1.0f);
            }

            var upGo = new GameObject("Ceiling_Upwash_Ambient_Light");
            upGo.transform.SetParent(ceilRingRoot.transform, false);
            upGo.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            var upLight = upGo.AddComponent<Light>();
            upLight.type = LightType.Point;
            upLight.range = 6.5f;
            upLight.intensity = 2.0f;
            upLight.color = new Color(0.12f, 0.70f, 0.95f);

            // 5.12 Dynamic Animation Component
            var anim = ceilRingRoot.AddComponent<SpaceCyberpunk.CeilingHoloChandelierAnimator>();
            anim.turbineRotor = turbineGroup.transform;
            anim.turbineRotorSpeed = 16f;
            anim.centralProjectorLight = spot;
        }

        private static void BuildSouthObservationTerraceAndBioLounge(
            Transform parent,
            GameObject tableGlassPrefab, GameObject sofaPrefab, GameObject pillowPrefab,
            GameObject hydroponicPrefab, GameObject plantPrefab, GameObject plantDeskPrefab, GameObject plantSmallPrefab,
            GameObject cabinetPrefab, GameObject compStationPrefab, GameObject consoleScreenPrefab, GameObject pilotSeatPrefab,
            GameObject art1Prefab, GameObject art2Prefab, GameObject ceilingLightPrefab,
            Material cyanEmissiveMat, Material steelAccentMat, Material coreGlowMat)
        {
            var southRoot = new GameObject("Zone2_South_Observation_Terrace_And_Bio_Lounge");
            southRoot.transform.SetParent(parent, false);

            float furnitureZ = 10.80f; // Single strictly aligned axis along panoramic windows

            // ── 1. CENTERPIECE: CELESTIAL OBSERVATION & MINI-HOLOGRAM TABLE (X = 14.0m) ──
            if (tableGlassPrefab != null)
            {
                var centerTable = Object.Instantiate(tableGlassPrefab, southRoot.transform, false);
                centerTable.name = "Celestial_Observation_Glass_Table";
                centerTable.transform.position = new Vector3(14.0f, 0.10f, furnitureZ);
                centerTable.transform.rotation = Quaternion.identity;

                // Center Miniature Hologram Plinth & Star Sphere on the table
                var holoPlinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                holoPlinth.name = "Mini_Holo_Plinth";
                holoPlinth.transform.SetParent(southRoot.transform, false);
                holoPlinth.transform.position = new Vector3(14.0f, 1.12f, furnitureZ);
                holoPlinth.transform.localScale = new Vector3(0.40f, 0.03f, 0.40f);
                Object.DestroyImmediate(holoPlinth.GetComponent<Collider>());
                if (steelAccentMat != null) holoPlinth.GetComponent<Renderer>().sharedMaterial = steelAccentMat;

                var holoSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                holoSphere.name = "Mini_Holo_Sphere";
                holoSphere.transform.SetParent(southRoot.transform, false);
                holoSphere.transform.position = new Vector3(14.0f, 1.40f, furnitureZ);
                holoSphere.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                Object.DestroyImmediate(holoSphere.GetComponent<Collider>());
                if (cyanEmissiveMat != null) holoSphere.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                var holoLightGo = new GameObject("Mini_Holo_Glow_Light");
                holoLightGo.transform.SetParent(southRoot.transform, false);
                holoLightGo.transform.position = new Vector3(14.0f, 1.40f, furnitureZ);
                var hl = holoLightGo.AddComponent<Light>();
                hl.type = LightType.Point;
                hl.range = 4.0f;
                hl.intensity = 1.8f;
                hl.color = new Color(0.2f, 0.85f, 1.0f);
            }

            // High-tech flat monitor screens flush on observation table
            if (consoleScreenPrefab != null)
            {
                var scrL = Object.Instantiate(consoleScreenPrefab, southRoot.transform, false);
                scrL.name = "Observation_Desk_Screen_L";
                scrL.transform.position = new Vector3(13.1f, 1.10f, furnitureZ);
                scrL.transform.rotation = Quaternion.Euler(0f, 15f, 0f);

                var scrR = Object.Instantiate(consoleScreenPrefab, southRoot.transform, false);
                scrR.name = "Observation_Desk_Screen_R";
                scrR.transform.position = new Vector3(14.9f, 1.10f, furnitureZ);
                scrR.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            }

            // ── 2. SYMMETRICAL BOTANICAL FICUS TREES (X = 11.0m and X = 17.0m) ──
            if (plantPrefab != null)
            {
                var plantL = Object.Instantiate(plantPrefab, southRoot.transform, false);
                plantL.name = "Botanical_Plant_Inner_West";
                plantL.transform.position = new Vector3(11.0f, 0.10f, furnitureZ);
                plantL.transform.rotation = Quaternion.identity;

                var plantR = Object.Instantiate(plantPrefab, southRoot.transform, false);
                plantR.name = "Botanical_Plant_Inner_East";
                plantR.transform.position = new Vector3(17.0f, 0.10f, furnitureZ);
                plantR.transform.rotation = Quaternion.identity;
            }

            // ── 3. SYMMETRICAL BIO-HYDROPONIC GREENHOUSE CHAMBERS (X = 8.0m and X = 20.0m) ──
            if (hydroponicPrefab != null)
            {
                var hydroL = Object.Instantiate(hydroponicPrefab, southRoot.transform, false);
                hydroL.name = "Bio_Hydroponic_Chamber_West";
                hydroL.transform.position = new Vector3(8.0f, 0.10f, furnitureZ);
                hydroL.transform.rotation = Quaternion.identity;

                var hydroR = Object.Instantiate(hydroponicPrefab, southRoot.transform, false);
                hydroR.name = "Bio_Hydroponic_Chamber_East";
                hydroR.transform.position = new Vector3(20.0f, 0.10f, furnitureZ);
                hydroR.transform.rotation = Quaternion.identity;

                // Glowing emerald bioluminescent underglow lights
                var bioLightLGo = new GameObject("Hydroponic_BioGlow_L");
                bioLightLGo.transform.SetParent(southRoot.transform, false);
                bioLightLGo.transform.position = new Vector3(8.0f, 1.2f, furnitureZ);
                var blL = bioLightLGo.AddComponent<Light>();
                blL.type = LightType.Point;
                blL.range = 5.5f;
                blL.intensity = 2.0f;
                blL.color = new Color(0.12f, 0.95f, 0.58f);

                var bioLightRGo = new GameObject("Hydroponic_BioGlow_R");
                bioLightRGo.transform.SetParent(southRoot.transform, false);
                bioLightRGo.transform.position = new Vector3(20.0f, 1.2f, furnitureZ);
                var blR = bioLightRGo.AddComponent<Light>();
                blR.type = LightType.Point;
                blR.range = 5.5f;
                blR.intensity = 2.0f;
                blR.color = new Color(0.12f, 0.95f, 0.58f);
            }

            // ── 4. SYMMETRICAL EXECUTIVE OBSERVATION SOFAS (X = 4.0m and X = 24.0m) ──
            if (sofaPrefab != null)
            {
                var sofaL = Object.Instantiate(sofaPrefab, southRoot.transform, false);
                sofaL.name = "Executive_Lounge_Sofa_West";
                sofaL.transform.position = new Vector3(4.0f, 0.10f, furnitureZ);
                sofaL.transform.rotation = Quaternion.identity;

                var sofaR = Object.Instantiate(sofaPrefab, southRoot.transform, false);
                sofaR.name = "Executive_Lounge_Sofa_East";
                sofaR.transform.position = new Vector3(24.0f, 0.10f, furnitureZ);
                sofaR.transform.rotation = Quaternion.identity;

                if (pillowPrefab != null)
                {
                    var pilL = Object.Instantiate(pillowPrefab, sofaL.transform, false);
                    pilL.name = "Lounge_Pillow";
                    pilL.transform.localPosition = new Vector3(0.5f, 0.65f, 0f);

                    var pilR = Object.Instantiate(pillowPrefab, sofaR.transform, false);
                    pilR.name = "Lounge_Pillow";
                    pilR.transform.localPosition = new Vector3(-0.5f, 0.65f, 0f);
                }
            }

            // Framed Sci-Fi Art mounted on wall directly centered above each sofa
            if (art1Prefab != null)
            {
                var artL = Object.Instantiate(art1Prefab, southRoot.transform, false);
                artL.name = "Lounge_Wall_Art_West";
                artL.transform.position = new Vector3(4.0f, 2.5f, 9.15f);
                artL.transform.rotation = Quaternion.identity;
            }

            if (art2Prefab != null)
            {
                var artR = Object.Instantiate(art2Prefab, southRoot.transform, false);
                artR.name = "Lounge_Wall_Art_East";
                artR.transform.position = new Vector3(24.0f, 2.5f, 9.15f);
                artR.transform.rotation = Quaternion.identity;
            }

            // ── 5. INTERMEDIATE GALLERY: GLASS ARTIFACT VITRINES (X = 8.0m & 20.0m, Z = 14.2m) ──
            if (tableGlassPrefab != null)
            {
                var vitrineL = Object.Instantiate(tableGlassPrefab, southRoot.transform, false);
                vitrineL.name = "Artifact_Vitrine_West";
                vitrineL.transform.position = new Vector3(8.0f, 0.10f, 14.20f);
                vitrineL.transform.rotation = Quaternion.identity;
                vitrineL.transform.localScale = new Vector3(0.60f, 0.80f, 0.60f);

                var coreL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coreL.name = "Vitrine_Artifact_Core_L";
                coreL.transform.SetParent(vitrineL.transform, false);
                coreL.transform.localPosition = new Vector3(0f, 0.90f, 0f);
                coreL.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                Object.DestroyImmediate(coreL.GetComponent<Collider>());
                if (coreGlowMat != null) coreL.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

                var coreLightLGo = new GameObject("Core_Light_L");
                coreLightLGo.transform.SetParent(vitrineL.transform, false);
                coreLightLGo.transform.localPosition = new Vector3(0f, 0.90f, 0f);
                var clL = coreLightLGo.AddComponent<Light>();
                clL.type = LightType.Point;
                clL.range = 3.5f;
                clL.intensity = 1.5f;
                clL.color = new Color(1.0f, 0.70f, 0.20f);

                var vitrineR = Object.Instantiate(tableGlassPrefab, southRoot.transform, false);
                vitrineR.name = "Artifact_Vitrine_East";
                vitrineR.transform.position = new Vector3(20.0f, 0.10f, 14.20f);
                vitrineR.transform.rotation = Quaternion.identity;
                vitrineR.transform.localScale = new Vector3(0.60f, 0.80f, 0.60f);

                var coreR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coreR.name = "Vitrine_Artifact_Core_R";
                coreR.transform.SetParent(vitrineR.transform, false);
                coreR.transform.localPosition = new Vector3(0f, 0.90f, 0f);
                coreR.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                Object.DestroyImmediate(coreR.GetComponent<Collider>());
                if (coreGlowMat != null) coreR.GetComponent<Renderer>().sharedMaterial = coreGlowMat;

                var coreLightRGo = new GameObject("Core_Light_R");
                coreLightRGo.transform.SetParent(vitrineR.transform, false);
                coreLightRGo.transform.localPosition = new Vector3(0f, 0.90f, 0f);
                var clR = coreLightRGo.AddComponent<Light>();
                clR.type = LightType.Point;
                clR.range = 3.5f;
                clR.intensity = 1.5f;
                clR.color = new Color(1.0f, 0.70f, 0.20f);
            }

            // ── 6. FLOOR NEON DEMARCATION GUIDE LINE ──
            if (cyanEmissiveMat != null)
            {
                var neonGuide = GameObject.CreatePrimitive(PrimitiveType.Cube);
                neonGuide.name = "South_Lounge_Floor_Neon_Guide";
                neonGuide.transform.SetParent(southRoot.transform, false);
                neonGuide.transform.position = new Vector3(14.0f, 0.105f, 15.60f);
                neonGuide.transform.localScale = new Vector3(24.0f, 0.01f, 0.08f);
                Object.DestroyImmediate(neonGuide.GetComponent<Collider>());
                neonGuide.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // ── 7. HIGH-BAY OVERHEAD SPOTLIGHTS (Balanced illumination across the terrace) ──
            Vector3[] ceilLightPos = new Vector3[]
            {
                new Vector3(8.0f, 7.5f, 11.0f),
                new Vector3(14.0f, 7.5f, 11.0f),
                new Vector3(20.0f, 7.5f, 11.0f)
            };

            for (int i = 0; i < ceilLightPos.Length; i++)
            {
                var pos = ceilLightPos[i];
                var clGo = new GameObject($"South_Lounge_Ceiling_Light_{i + 1}");
                clGo.transform.SetParent(southRoot.transform, false);
                clGo.transform.position = pos;
                var cl = clGo.AddComponent<Light>();
                cl.type = LightType.Point;
                cl.range = 18.0f;
                cl.intensity = 2.4f;
                cl.color = new Color(0.94f, 0.96f, 1.0f);

                if (ceilingLightPrefab != null)
                {
                    var fix = Object.Instantiate(ceilingLightPrefab, southRoot.transform, false);
                    fix.name = $"South_Lounge_Ceiling_Fixture_{i + 1}";
                    fix.transform.position = pos;
                    fix.transform.rotation = Quaternion.identity;
                }
            }
        }

        private static void ConfigureAnimatedScreenMaterials(
            Material mRadar, Material mHolo, Material mTelR, Material mTelL,
            out Material overheadRadarMat, out Material overheadDiagMat)
        {
            Shader animShader = Shader.Find("SpaceCyberpunk/SciFiBigScreenAnimated");
            overheadRadarMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Overhead_Tactical_Radar.mat");
            overheadDiagMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Overhead_System_Diagnostics.mat");

            if (animShader != null)
            {
                if (overheadRadarMat == null)
                {
                    overheadRadarMat = new Material(animShader);
                    AssetDatabase.CreateAsset(overheadRadarMat, "Assets/New/Materials/Mat_Overhead_Tactical_Radar.mat");
                }
                if (overheadDiagMat == null)
                {
                    overheadDiagMat = new Material(animShader);
                    AssetDatabase.CreateAsset(overheadDiagMat, "Assets/New/Materials/Mat_Overhead_System_Diagnostics.mat");
                }
            }

            if (animShader == null) return;

            if (mRadar != null)
            {
                mRadar.shader = animShader;
                mRadar.EnableKeyword("_MODE_RADARSCOPE");
                mRadar.DisableKeyword("_MODE_CENTRALHOLO");
                mRadar.DisableKeyword("_MODE_TELEMETRYBARS");
                mRadar.DisableKeyword("_MODE_HOLOHUD");
                mRadar.SetFloat("_Mode", 0f);
                mRadar.SetColor("_PrimaryColor", new Color(0.1f, 1.0f, 0.6f, 1.0f));
                mRadar.SetColor("_SecondaryColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
                mRadar.SetFloat("_AnimSpeed", 1.0f);
                mRadar.SetFloat("_GlowIntensity", 2.6f);
                mRadar.SetFloat("_ScreenVMax", 0.634f);
                mRadar.SetFloat("_FlipV", 0.0f);
                mRadar.SetFloat("_AspectCorrection", 1.25f);
                mRadar.SetFloat("_UseBaseTex", 0.0f);
                EditorUtility.SetDirty(mRadar);
            }

            if (mHolo != null)
            {
                mHolo.shader = animShader;
                mHolo.DisableKeyword("_MODE_RADARSCOPE");
                mHolo.EnableKeyword("_MODE_CENTRALHOLO");
                mHolo.DisableKeyword("_MODE_TELEMETRYBARS");
                mHolo.DisableKeyword("_MODE_HOLOHUD");
                mHolo.SetFloat("_Mode", 1f);
                mHolo.SetColor("_PrimaryColor", new Color(0.1f, 0.95f, 1.0f, 1.0f));
                mHolo.SetColor("_SecondaryColor", new Color(0.2f, 0.6f, 1.0f, 1.0f));
                mHolo.SetFloat("_AnimSpeed", 1.0f);
                mHolo.SetFloat("_GlowIntensity", 2.8f);
                mHolo.SetFloat("_ScreenVMax", 0.634f);
                mHolo.SetFloat("_FlipV", 0.0f);
                mHolo.SetFloat("_AspectCorrection", 1.25f);
                mHolo.SetFloat("_UseBaseTex", 0.0f);
                EditorUtility.SetDirty(mHolo);
            }

            if (mTelR != null)
            {
                mTelR.shader = animShader;
                mTelR.DisableKeyword("_MODE_RADARSCOPE");
                mTelR.DisableKeyword("_MODE_CENTRALHOLO");
                mTelR.EnableKeyword("_MODE_TELEMETRYBARS");
                mTelR.DisableKeyword("_MODE_HOLOHUD");
                mTelR.SetFloat("_Mode", 2f);
                mTelR.SetColor("_PrimaryColor", new Color(0.2f, 1.0f, 0.85f, 1.0f));
                mTelR.SetColor("_SecondaryColor", new Color(1.0f, 0.35f, 0.7f, 1.0f));
                mTelR.SetFloat("_AnimSpeed", 1.2f);
                mTelR.SetFloat("_GlowIntensity", 2.6f);
                mTelR.SetFloat("_ScreenVMax", 0.634f);
                mTelR.SetFloat("_FlipV", 0.0f);
                mTelR.SetFloat("_AspectCorrection", 1.25f);
                mTelR.SetFloat("_UseBaseTex", 0.0f);
                EditorUtility.SetDirty(mTelR);
            }

            if (mTelL != null)
            {
                mTelL.shader = animShader;
                mTelL.DisableKeyword("_MODE_RADARSCOPE");
                mTelL.DisableKeyword("_MODE_CENTRALHOLO");
                mTelL.DisableKeyword("_MODE_TELEMETRYBARS");
                mTelL.EnableKeyword("_MODE_HOLOHUD");
                mTelL.SetFloat("_Mode", 3f);
                mTelL.SetColor("_PrimaryColor", new Color(0.15f, 0.95f, 0.85f, 1.0f));
                mTelL.SetColor("_SecondaryColor", new Color(1.0f, 0.55f, 0.2f, 1.0f));
                mTelL.SetFloat("_AnimSpeed", 1.0f);
                mTelL.SetFloat("_GlowIntensity", 2.6f);
                mTelL.SetFloat("_ScreenVMax", 1.0f);
                mTelL.SetFloat("_FlipV", 0.0f);
                mTelL.SetFloat("_AspectCorrection", 1.0f);
                mTelL.SetFloat("_UseBaseTex", 0.0f);
                EditorUtility.SetDirty(mTelL);
            }

            // Overhead Ceiling Consoles (Screen UV.y spans [0..0.634] inverted top-to-bottom, Aspect Ratio = 1.875)
            if (overheadRadarMat != null)
            {
                overheadRadarMat.shader = animShader;
                overheadRadarMat.EnableKeyword("_MODE_RADARSCOPE");
                overheadRadarMat.DisableKeyword("_MODE_CENTRALHOLO");
                overheadRadarMat.DisableKeyword("_MODE_TELEMETRYBARS");
                overheadRadarMat.DisableKeyword("_MODE_HOLOHUD");
                overheadRadarMat.SetFloat("_Mode", 0f);
                overheadRadarMat.SetColor("_PrimaryColor", new Color(0.1f, 1.0f, 0.65f, 1.0f));
                overheadRadarMat.SetColor("_SecondaryColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
                overheadRadarMat.SetFloat("_AnimSpeed", 1.0f);
                overheadRadarMat.SetFloat("_GlowIntensity", 2.8f);
                overheadRadarMat.SetFloat("_ScreenVMax", 0.634f);
                overheadRadarMat.SetFloat("_FlipV", 1.0f);
                overheadRadarMat.SetFloat("_AspectCorrection", 1.875f);
                overheadRadarMat.SetFloat("_UseBaseTex", 0.0f);
                EditorUtility.SetDirty(overheadRadarMat);
            }

            if (overheadDiagMat != null)
            {
                overheadDiagMat.shader = animShader;
                overheadDiagMat.DisableKeyword("_MODE_RADARSCOPE");
                overheadDiagMat.DisableKeyword("_MODE_CENTRALHOLO");
                overheadDiagMat.EnableKeyword("_MODE_TELEMETRYBARS");
                overheadDiagMat.DisableKeyword("_MODE_HOLOHUD");
                overheadDiagMat.SetFloat("_Mode", 2f);
                overheadDiagMat.SetColor("_PrimaryColor", new Color(0.15f, 0.95f, 1.0f, 1.0f));
                overheadDiagMat.SetColor("_SecondaryColor", new Color(1.0f, 0.4f, 0.2f, 1.0f));
                overheadDiagMat.SetFloat("_AnimSpeed", 1.3f);
                overheadDiagMat.SetFloat("_GlowIntensity", 2.8f);
                overheadDiagMat.SetFloat("_ScreenVMax", 0.634f);
                overheadDiagMat.SetFloat("_FlipV", 1.0f);
                overheadDiagMat.SetFloat("_AspectCorrection", 1.875f);
                overheadDiagMat.SetFloat("_UseBaseTex", 0.0f);
                EditorUtility.SetDirty(overheadDiagMat);
            }

            // Ceiling Holo-Chandelier Central Oculus Disc
            Material oculusMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Ceiling_Holo_Oculus.mat");
            if (oculusMat == null && animShader != null)
            {
                oculusMat = new Material(animShader);
                AssetDatabase.CreateAsset(oculusMat, "Assets/New/Materials/Mat_Ceiling_Holo_Oculus.mat");
            }
            if (oculusMat != null && animShader != null)
            {
                oculusMat.shader = animShader;
                oculusMat.DisableKeyword("_MODE_RADARSCOPE");
                oculusMat.EnableKeyword("_MODE_CENTRALHOLO");
                oculusMat.DisableKeyword("_MODE_TELEMETRYBARS");
                oculusMat.DisableKeyword("_MODE_HOLOHUD");
                oculusMat.SetFloat("_Mode", 1f);
                oculusMat.SetColor("_PrimaryColor", new Color(0.12f, 0.95f, 1.0f, 1.0f));
                oculusMat.SetColor("_SecondaryColor", new Color(0.18f, 0.55f, 1.0f, 1.0f));
                oculusMat.SetFloat("_AnimSpeed", 1.1f);
                oculusMat.SetFloat("_GlowIntensity", 3.8f);
                oculusMat.SetFloat("_AspectCorrection", 1.0f);
                oculusMat.SetFloat("_ScreenVMax", 1.0f);
                oculusMat.SetFloat("_FlipV", 0.0f);
                oculusMat.SetFloat("_UseBaseTex", 0.0f);
                EditorUtility.SetDirty(oculusMat);
            }

            AssetDatabase.SaveAssets();
        }

        private static void BuildCitadelAtriumArchitecturalEnhancements(
            Transform parent,
            GameObject compStation, GameObject deskPrefab, GameObject pilotSeatPrefab,
            GameObject consoleScreenPrefab, GameObject consoleCeilingPrefab,
            GameObject plantPrefab, GameObject hydroponicPrefab, GameObject crystalLarge,
            Material titaniumMat, Material steelMat, Material cyanEmissiveMat,
            Material coreGlowMat, Material holoGlassMat,
            Material radarScopeMat, Material centralHoloMat, Material telemetryLMat,
            Material deskTelemetryMat, Material deskDiagnosticsMat,
            Material overheadRadarMat, Material overheadDiagMat)
        {
            var archRoot = new GameObject("Citadel_Atrium_Architectural_Enhancements");
            archRoot.transform.SetParent(parent, false);

            // ─────────────────────────────────────────────────────────────────
            // 1. DUAL EMBEDDED NEON FLOOR RUNWAYS & DIRECTIONAL CHEVRONS
            // Paves the long barren promenade with crisp glowing perspective lines
            // ─────────────────────────────────────────────────────────────────
            var runwayGroup = new GameObject("Atrium_Guidance_Runways");
            runwayGroup.transform.SetParent(archRoot.transform, false);

            float[] trackZ = new float[] { 21.20f, 24.80f };
            // 1.1 Entrance to Dais Guideway Tracks (X: -2.0m to 9.7m)
            foreach (float z in trackZ)
            {
                var trackNeon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trackNeon.name = string.Format("Promenade_Floor_Neon_Z{0:F1}", z);
                trackNeon.transform.SetParent(runwayGroup.transform, false);
                trackNeon.transform.position = new Vector3(3.85f, 0.105f, z);
                trackNeon.transform.localScale = new Vector3(11.70f, 0.012f, 0.08f);
                Object.DestroyImmediate(trackNeon.GetComponent<Collider>());
                if (cyanEmissiveMat != null) trackNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                var trackBezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trackBezel.name = string.Format("Promenade_Floor_Bezel_Z{0:F1}", z);
                trackBezel.transform.SetParent(runwayGroup.transform, false);
                trackBezel.transform.position = new Vector3(3.85f, 0.100f, z);
                trackBezel.transform.localScale = new Vector3(11.70f, 0.010f, 0.22f);
                Object.DestroyImmediate(trackBezel.GetComponent<Collider>());
                if (titaniumMat != null) trackBezel.GetComponent<Renderer>().sharedMaterial = titaniumMat;
            }

            // 1.2 Dais to East Master Chamber Guideway Tracks (X: 18.3m to 26.0m)
            foreach (float z in trackZ)
            {
                var eastNeon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eastNeon.name = string.Format("East_Floor_Neon_Z{0:F1}", z);
                eastNeon.transform.SetParent(runwayGroup.transform, false);
                eastNeon.transform.position = new Vector3(22.15f, 0.105f, z);
                eastNeon.transform.localScale = new Vector3(7.70f, 0.012f, 0.08f);
                Object.DestroyImmediate(eastNeon.GetComponent<Collider>());
                if (cyanEmissiveMat != null) eastNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                var eastBezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eastBezel.name = string.Format("East_Floor_Bezel_Z{0:F1}", z);
                eastBezel.transform.SetParent(runwayGroup.transform, false);
                eastBezel.transform.position = new Vector3(22.15f, 0.100f, z);
                eastBezel.transform.localScale = new Vector3(7.70f, 0.010f, 0.22f);
                Object.DestroyImmediate(eastBezel.GetComponent<Collider>());
                if (titaniumMat != null) eastBezel.GetComponent<Renderer>().sharedMaterial = titaniumMat;
            }

            // 1.3 Floor Wayfinding Directional Chevrons (X = 1.0, 3.5, 6.0, 8.5 at Z = 23.0m)
            float[] chevronX = new float[] { 1.0f, 3.5f, 6.0f, 8.5f };
            for (int i = 0; i < chevronX.Length; i++)
            {
                float cx = chevronX[i];
                var chev = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chev.name = string.Format("Floor_Chevron_{0}", i + 1);
                chev.transform.SetParent(runwayGroup.transform, false);
                chev.transform.position = new Vector3(cx, 0.104f, 23.0f);
                chev.transform.localScale = new Vector3(0.18f, 0.010f, 0.70f);
                chev.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                Object.DestroyImmediate(chev.GetComponent<Collider>());
                if (cyanEmissiveMat != null) chev.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // ─────────────────────────────────────────────────────────────────
            // 2. ENTRANCE GATEWAY THRESHOLD ARCHITECTURE
            // Sits right at X = 0.6m (framing the airlock, non-blocking sightlines)
            // ─────────────────────────────────────────────────────────────────
            var gateGroup = new GameObject("Entrance_Gateway_Threshold");
            gateGroup.transform.SetParent(archRoot.transform, false);

            for (int i = 0; i < 2; i++)
            {
                float z = (i == 0) ? 20.0f : 26.0f;
                var pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pylon.name = string.Format("Threshold_Portal_Pylon_{0}", i + 1);
                pylon.transform.SetParent(gateGroup.transform, false);
                pylon.transform.position = new Vector3(0.60f, 1.80f, z);
                pylon.transform.localScale = new Vector3(0.35f, 1.80f, 0.35f);
                Object.DestroyImmediate(pylon.GetComponent<Collider>());
                if (titaniumMat != null) pylon.GetComponent<Renderer>().sharedMaterial = titaniumMat;

                var neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                neon.name = "Pylon_Front_Neon_Strip";
                neon.transform.SetParent(pylon.transform, false);
                neon.transform.localPosition = new Vector3(0.48f, 0f, 0f); // Facing +X into room!
                neon.transform.localScale = new Vector3(0.10f, 0.92f, 0.20f);
                Object.DestroyImmediate(neon.GetComponent<Collider>());
                if (cyanEmissiveMat != null) neon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                var pLightGo = new GameObject("Pylon_Portal_Light");
                pLightGo.transform.SetParent(pylon.transform, false);
                pLightGo.transform.localPosition = new Vector3(0.50f, 0.2f, 0f);
                var pl = pLightGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.range = 7.0f;
                pl.intensity = 2.2f;
                pl.color = new Color(0.2f, 0.85f, 1.0f);
            }

            // ─────────────────────────────────────────────────────────────────
            // 3. PROMENADE HOLOGRAPHIC COMPASS ROSE PROJECTOR (X = 7.0m, Z = 23.0m)
            // Low-profile ground-level projection keeping eye level 100% clear for the Grand Dais!
            // ─────────────────────────────────────────────────────────────────
            var waypointGroup = new GameObject("Promenade_Holographic_Waypoint");
            waypointGroup.transform.SetParent(archRoot.transform, false);

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Promenade_Holo_Floor_Disc";
            disc.transform.SetParent(waypointGroup.transform, false);
            disc.transform.position = new Vector3(7.0f, 0.106f, 23.0f);
            disc.transform.localScale = new Vector3(2.40f, 0.006f, 2.40f);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            if (cyanEmissiveMat != null) disc.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var discRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            discRing.name = "Promenade_Holo_Floor_Outer_Ring";
            discRing.transform.SetParent(waypointGroup.transform, false);
            discRing.transform.position = new Vector3(7.0f, 0.102f, 23.0f);
            discRing.transform.localScale = new Vector3(2.80f, 0.004f, 2.80f);
            Object.DestroyImmediate(discRing.GetComponent<Collider>());
            if (titaniumMat != null) discRing.GetComponent<Renderer>().sharedMaterial = titaniumMat;

            // Low-profile floating Holographic Compass Radar Rose (Y = 0.35m - Zero sightline blockage!)
            var holoRose = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            holoRose.name = "Promenade_Floating_Holo_Compass";
            holoRose.transform.SetParent(waypointGroup.transform, false);
            holoRose.transform.position = new Vector3(7.0f, 0.35f, 23.0f);
            holoRose.transform.localScale = new Vector3(1.60f, 0.004f, 1.60f);
            Object.DestroyImmediate(holoRose.GetComponent<Collider>());
            if (centralHoloMat != null) holoRose.GetComponent<Renderer>().sharedMaterial = centralHoloMat;
            else if (cyanEmissiveMat != null) holoRose.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var hlGo = new GameObject("Promenade_Holo_Light");
            hlGo.transform.SetParent(waypointGroup.transform, false);
            hlGo.transform.position = new Vector3(7.0f, 0.45f, 23.0f);
            var hl = hlGo.AddComponent<Light>();
            hl.type = LightType.Point;
            hl.range = 5.0f;
            hl.intensity = 2.0f;
            hl.color = new Color(0.2f, 0.85f, 1.0f);

            // Symmetrical Planters framing waypoint
            if (plantPrefab != null)
            {
                var pL = Object.Instantiate(plantPrefab, waypointGroup.transform, false);
                pL.name = "Promenade_Planter_North";
                pL.transform.position = new Vector3(7.0f, 0.10f, 26.5f);
                pL.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

                var pR = Object.Instantiate(plantPrefab, waypointGroup.transform, false);
                pR.name = "Promenade_Planter_South";
                pR.transform.position = new Vector3(7.0f, 0.10f, 19.5f);
                pR.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            }

            // ─────────────────────────────────────────────────────────────────
            // 4. MONUMENTAL CEILING GANTRY TRUSSES & OVERHEAD TACTICAL MONITORS
            // Breaks the vast cavernous ceiling void into layered architectural depth
            // ─────────────────────────────────────────────────────────────────
            var trussGroup = new GameObject("Ceiling_Gantry_Truss_Network");
            trussGroup.transform.SetParent(archRoot.transform, false);

            float[] trussX = new float[] { 7.0f, 21.0f };
            foreach (float tx in trussX)
            {
                var tRoot = new GameObject(string.Format("Gantry_Truss_X{0:F0}", tx));
                tRoot.transform.SetParent(trussGroup.transform, false);

                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "Truss_Cross_Beam";
                beam.transform.SetParent(tRoot.transform, false);
                beam.transform.position = new Vector3(tx, 7.60f, 23.0f);
                beam.transform.localScale = new Vector3(0.45f, 0.55f, 22.0f);
                Object.DestroyImmediate(beam.GetComponent<Collider>());
                if (titaniumMat != null) beam.GetComponent<Renderer>().sharedMaterial = titaniumMat;

                var trussNeon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trussNeon.name = "Truss_Underside_Neon";
                trussNeon.transform.SetParent(tRoot.transform, false);
                trussNeon.transform.position = new Vector3(tx, 7.30f, 23.0f);
                trussNeon.transform.localScale = new Vector3(0.12f, 0.04f, 21.6f);
                Object.DestroyImmediate(trussNeon.GetComponent<Collider>());
                if (cyanEmissiveMat != null) trussNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                float[] hangerZ = new float[] { 12.0f, 34.0f };
                foreach (float hz in hangerZ)
                {
                    var hanger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    hanger.name = string.Format("Truss_Drop_Hanger_Z{0:F0}", hz);
                    hanger.transform.SetParent(tRoot.transform, false);
                    hanger.transform.position = new Vector3(tx, 7.85f, hz);
                    hanger.transform.localScale = new Vector3(0.55f, 0.60f, 0.55f);
                    Object.DestroyImmediate(hanger.GetComponent<Collider>());
                    if (steelMat != null) hanger.GetComponent<Renderer>().sharedMaterial = steelMat;
                }

                float[] consoleZ = new float[] { 21.5f, 24.5f };
                Material[] consoleMats = new Material[] { overheadRadarMat, overheadDiagMat };
                for (int ci = 0; ci < consoleZ.Length; ci++)
                {
                    float cz = consoleZ[ci];
                    if (consoleCeilingPrefab != null)
                    {
                        var cc = Object.Instantiate(consoleCeilingPrefab, tRoot.transform, false);
                        cc.name = string.Format("Overhead_Console_{0}", ci + 1);
                        cc.transform.position = new Vector3(tx, 7.35f, cz);
                        cc.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                        float tiltYaw = (tx < 14f) ? 270f : 90f;
                        cc.transform.rotation = Quaternion.Euler(0f, tiltYaw, 0f);

                        var mr = cc.GetComponentInChildren<MeshRenderer>();
                        if (mr != null && mr.sharedMaterials.Length > 1)
                        {
                            var mats = mr.sharedMaterials;
                            if (titaniumMat != null) mats[0] = titaniumMat;
                            mats[1] = (consoleMats[ci % consoleMats.Length] != null) ? consoleMats[ci % consoleMats.Length] : cyanEmissiveMat;
                            mr.sharedMaterials = mats;
                        }
                    }
                }

                float[] spotZ = new float[] { 17.0f, 23.0f, 29.0f };
                foreach (float sz in spotZ)
                {
                    var sGo = new GameObject("Gantry_Downlight");
                    sGo.transform.SetParent(tRoot.transform, false);
                    sGo.transform.position = new Vector3(tx, 7.30f, sz);
                    var spot = sGo.AddComponent<Light>();
                    spot.type = LightType.Spot;
                    spot.range = 16.0f;
                    spot.spotAngle = 70.0f;
                    spot.intensity = 3.2f;
                    spot.color = new Color(0.92f, 0.96f, 1.0f);
                    sGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 7. SUSPENDED TACTICAL HOLO-BANNERS (FLOATING HUD DISPLAYS)
            // Floating transparent telemetry panels suspended in mid-air
            // ─────────────────────────────────────────────────────────────────
            var bannerGroup = new GameObject("Suspended_Tactical_Holo_Banners");
            bannerGroup.transform.SetParent(archRoot.transform, false);

            float[] bannerX = new float[] { 10.5f, 17.5f };
            for (int bi = 0; bi < bannerX.Length; bi++)
            {
                float bx = bannerX[bi];
                var bannerGo = new GameObject(string.Format("Tactical_Holo_Banner_{0}", bi + 1));
                bannerGo.transform.SetParent(bannerGroup.transform, false);
                bannerGo.transform.position = new Vector3(bx, 5.40f, 23.0f);

                var hudQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                hudQuad.name = "Holo_HUD_Screen";
                hudQuad.transform.SetParent(bannerGo.transform, false);
                hudQuad.transform.localPosition = Vector3.zero;
                hudQuad.transform.localScale = new Vector3(3.20f, 1.20f, 1.0f);
                float tiltRotY = (bx < 14f) ? 270f : 90f;
                hudQuad.transform.localRotation = Quaternion.Euler(15f, tiltRotY, 0f);
                Object.DestroyImmediate(hudQuad.GetComponent<Collider>());
                if (telemetryLMat != null) hudQuad.GetComponent<Renderer>().sharedMaterial = telemetryLMat;
                else if (cyanEmissiveMat != null) hudQuad.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                var bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bracket.name = "HUD_Mount_Bracket";
                bracket.transform.SetParent(bannerGo.transform, false);
                bracket.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                bracket.transform.localScale = new Vector3(0.08f, 0.08f, 3.30f);
                Object.DestroyImmediate(bracket.GetComponent<Collider>());
                if (titaniumMat != null) bracket.GetComponent<Renderer>().sharedMaterial = titaniumMat;

                for (int ci = 0; ci < 2; ci++)
                {
                    float cz = (ci == 0) ? -1.4f : 1.4f;
                    var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cable.name = string.Format("HUD_Suspension_Cable_{0}", ci + 1);
                    cable.transform.SetParent(bannerGo.transform, false);
                    cable.transform.localPosition = new Vector3(0f, 1.60f, cz);
                    cable.transform.localScale = new Vector3(0.02f, 0.95f, 0.02f);
                    Object.DestroyImmediate(cable.GetComponent<Collider>());
                    if (steelMat != null) cable.GetComponent<Renderer>().sharedMaterial = steelMat;
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 8. EAST CHAMBER APPROACH SECURITY CHECKPOINT
            // Frames the pathway approaching the Master Chamber portal (X = 22.0m)
            // ─────────────────────────────────────────────────────────────────
            var eastSecGroup = new GameObject("East_Security_Checkpoint");
            eastSecGroup.transform.SetParent(archRoot.transform, false);

            float[] secZ = new float[] { 20.80f, 25.20f };
            for (int si = 0; si < secZ.Length; si++)
            {
                float z = secZ[si];
                var pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pylon.name = string.Format("East_Security_Pylon_{0}", si + 1);
                pylon.transform.SetParent(eastSecGroup.transform, false);
                pylon.transform.position = new Vector3(22.0f, 1.25f, z);
                pylon.transform.localScale = new Vector3(0.32f, 1.25f, 0.32f);
                Object.DestroyImmediate(pylon.GetComponent<Collider>());
                if (titaniumMat != null) pylon.GetComponent<Renderer>().sharedMaterial = titaniumMat;

                var neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                neon.name = "Security_Scanner_Neon";
                neon.transform.SetParent(pylon.transform, false);
                neon.transform.localPosition = new Vector3(-0.45f, 0f, 0f);
                neon.transform.localScale = new Vector3(0.10f, 0.85f, 0.16f);
                Object.DestroyImmediate(neon.GetComponent<Collider>());
                if (cyanEmissiveMat != null) neon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }
        }

        private static void BuildCryoLifeSupportBay(
            Transform parent, GameObject machineBarrelLarge, GameObject barrelsCluster,
            GameObject pipeStraight, GameObject pipeCorner, GameObject consoleScreenPrefab,
            Material darkTitaniumMat, Material holoGlassMat, Material cyanEmissiveMat, Material smokeMat)
        {
            var cryoRoot = new GameObject("Zone3_Cryo_Life_Support_Bay");
            cryoRoot.transform.SetParent(parent, false);

            // 1. 3 Upright Cryo Stasis Pods (along west wall of north wing: X = 4.0m, Z = 38.5, 40.8, 43.1)
            float[] podZ = new float[] { 38.5f, 40.8f, 43.1f };
            for (int i = 0; i < podZ.Length; i++)
            {
                float z = podZ[i];
                var podGo = new GameObject($"Cryo_Stasis_Pod_{i + 1}");
                podGo.transform.SetParent(cryoRoot.transform, false);
                podGo.transform.position = new Vector3(4.0f, 0.10f, z);

                // Octagonal Plinth Base
                var basePlinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basePlinth.name = "Pod_Base_Plinth";
                basePlinth.transform.SetParent(podGo.transform, false);
                basePlinth.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                basePlinth.transform.localScale = new Vector3(1.35f, 0.24f, 1.35f);
                Object.DestroyImmediate(basePlinth.GetComponent<Collider>());
                if (darkTitaniumMat != null) basePlinth.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

                // Glass Stasis Capsule
                var glassCapsule = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                glassCapsule.name = "Pod_Glass_Capsule";
                glassCapsule.transform.SetParent(podGo.transform, false);
                glassCapsule.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                glassCapsule.transform.localScale = new Vector3(1.10f, 1.0f, 1.10f);
                Object.DestroyImmediate(glassCapsule.GetComponent<Collider>());
                if (holoGlassMat != null) glassCapsule.GetComponent<Renderer>().sharedMaterial = holoGlassMat;
                else if (cyanEmissiveMat != null) glassCapsule.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                // Internal Glowing Stasis Core Rod
                var stasisCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stasisCore.name = "Pod_Stasis_Core";
                stasisCore.transform.SetParent(podGo.transform, false);
                stasisCore.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                stasisCore.transform.localScale = new Vector3(0.18f, 0.85f, 0.18f);
                Object.DestroyImmediate(stasisCore.GetComponent<Collider>());
                if (cyanEmissiveMat != null) stasisCore.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                // Pressurized Top Cap
                var topCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                topCap.name = "Pod_Top_Cap";
                topCap.transform.SetParent(podGo.transform, false);
                topCap.transform.localPosition = new Vector3(0f, 2.45f, 0f);
                topCap.transform.localScale = new Vector3(1.25f, 0.18f, 1.25f);
                Object.DestroyImmediate(topCap.GetComponent<Collider>());
                if (darkTitaniumMat != null) topCap.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

                // Ice-Cyan Stasis Glow Light
                var glowLightGo = new GameObject("Stasis_Glow_Light");
                glowLightGo.transform.SetParent(podGo.transform, false);
                glowLightGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                var glowLight = glowLightGo.AddComponent<Light>();
                glowLight.type = LightType.Point;
                glowLight.range = 3.2f;
                glowLight.intensity = 1.8f;
                glowLight.color = new Color(0.15f, 0.85f, 1.0f);

                // Procedural Cryo Frost Vapor Vent Emitter
                var ventGo = new GameObject("Cryo_Vapor_Emitter");
                ventGo.transform.SetParent(podGo.transform, false);
                ventGo.transform.localPosition = new Vector3(0.55f, 0.20f, 0f);
                ventGo.transform.localRotation = Quaternion.Euler(-15f, 90f, 0f);

                var ps = ventGo.AddComponent<ParticleSystem>();
                var psRend = ventGo.GetComponent<ParticleSystemRenderer>();
                if (psRend != null && smokeMat != null) psRend.sharedMaterial = smokeMat;

                var main = ps.main;
                main.startLifetime = 1.6f;
                main.startSpeed = 1.5f;
                main.startSize = 0.4f;
                main.startColor = new Color(0.8f, 0.95f, 1.0f, 0.5f);
                main.maxParticles = 50;
                main.playOnAwake = false;

                var emission = ps.emission;
                emission.rateOverTime = 0; // Triggered via burst in CryoVaporVent

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 25f;
                shape.radius = 0.08f;

                var cryoVent = ventGo.AddComponent<CryoVaporVent>();
                cryoVent.vaporParticle = ps;
                cryoVent.ventGlowLight = glowLight;
                cryoVent.minInterval = 4.0f + i * 1.5f;
                cryoVent.maxInterval = 7.5f + i * 1.5f;
                cryoVent.burstCount = 18;
            }

            // 2. Oxygen & Nitrogen Gas Buffer Tanks (X = 6.8m, Z = 39.5, 42.0)
            if (machineBarrelLarge != null)
            {
                var t1 = Object.Instantiate(machineBarrelLarge, cryoRoot.transform, false);
                t1.name = "Cryo_Buffer_Tank_O2";
                t1.transform.position = new Vector3(6.8f, 0.10f, 39.5f);
                t1.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

                var t2 = Object.Instantiate(machineBarrelLarge, cryoRoot.transform, false);
                t2.name = "Cryo_Buffer_Tank_N2";
                t2.transform.position = new Vector3(6.8f, 0.10f, 42.2f);
                t2.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            }
            if (barrelsCluster != null)
            {
                var bc = Object.Instantiate(barrelsCluster, cryoRoot.transform, false);
                bc.name = "Cryo_Chemical_Barrels";
                bc.transform.position = new Vector3(6.8f, 0.10f, 44.5f);
            }

            // 3. Manifold Pipes connecting buffer tanks to pods
            if (pipeStraight != null)
            {
                var p1 = Object.Instantiate(pipeStraight, cryoRoot.transform, false);
                p1.name = "Cryo_Manifold_Pipe_1";
                p1.transform.position = new Vector3(5.4f, 0.35f, 39.5f);
                p1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                var p2 = Object.Instantiate(pipeStraight, cryoRoot.transform, false);
                p2.name = "Cryo_Manifold_Pipe_2";
                p2.transform.position = new Vector3(5.4f, 0.35f, 42.2f);
                p2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // 4. Cryo Vitals Monitor Console
            if (consoleScreenPrefab != null)
            {
                var mon = Object.Instantiate(consoleScreenPrefab, cryoRoot.transform, false);
                mon.name = "Cryo_Vitals_Telemetry_Screen";
                mon.transform.position = new Vector3(3.8f, 0.10f, 36.8f);
                mon.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            }
        }

        private static void BuildAirlockAndCargoBay(
            Transform parent, GameObject containerBig, GameObject containerSmall, GameObject storageContainerSmall,
            GameObject torpedoRack, GameObject wallShelf, GameObject barrelsCluster, GameObject floorCornerPipes,
            GameObject compStation, Material hazardStripeMat, Material darkTitaniumMat, Material cyanEmissiveMat)
        {
            var cargoRoot = new GameObject("Zone4_Airlock_And_Cargo_Bay");
            cargoRoot.transform.SetParent(parent, false);

            // 1. Sleek Outer Threshold at airlock door (X = -9.2m)
            if (hazardStripeMat != null)
            {
                var stripeOuter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripeOuter.name = "Hazard_Stripe_Outer_Threshold";
                stripeOuter.transform.SetParent(cargoRoot.transform, false);
                stripeOuter.transform.position = new Vector3(-9.2f, 0.105f, 23.0f);
                stripeOuter.transform.localScale = new Vector3(0.50f, 0.01f, 3.6f);
                Object.DestroyImmediate(stripeOuter.GetComponent<Collider>());
                stripeOuter.GetComponent<Renderer>().sharedMaterial = hazardStripeMat;
            }

            // 2. Sleek North & South Baseboard Floor Neon Guides (Embedded flush into floor)
            if (cyanEmissiveMat != null)
            {
                var guideN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                guideN.name = "Airlock_Floor_Guide_North";
                guideN.transform.SetParent(cargoRoot.transform, false);
                guideN.transform.position = new Vector3(-5.8f, 0.105f, 25.10f);
                guideN.transform.localScale = new Vector3(7.0f, 0.01f, 0.08f);
                Object.DestroyImmediate(guideN.GetComponent<Collider>());
                guideN.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                var guideS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                guideS.name = "Airlock_Floor_Guide_South";
                guideS.transform.SetParent(cargoRoot.transform, false);
                guideS.transform.position = new Vector3(-5.8f, 0.105f, 20.90f);
                guideS.transform.localScale = new Vector3(7.0f, 0.01f, 0.08f);
                Object.DestroyImmediate(guideS.GetComponent<Collider>());
                guideS.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // 3. Overhead Airlock Corridor Ceiling Fixture Light
            var ceilLight = new GameObject("Airlock_Corridor_Ceiling_Light");
            ceilLight.transform.SetParent(cargoRoot.transform, false);
            ceilLight.transform.position = new Vector3(-6.0f, 7.80f, 23.0f);
            var l = ceilLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 14.0f;
            l.intensity = 2.4f;
            l.color = new Color(0.92f, 0.96f, 1.0f);
        }

        private static void BuildOverheadConduitsAndPipes(
            Transform parent, GameObject pipeStraight, GameObject pipeCorner, GameObject pipeRoundLarge, GameObject pipeCross,
            GameObject floorCornerPipes, GameObject floorCornerPipesTurn)
        {
            var conduitRoot = new GameObject("Scattering_Overhead_Conduits_And_Pipes");
            conduitRoot.transform.SetParent(parent, false);

            float ductY = 7.60f; // High-bay truss elevation

            // 1. South High-Bay Duct Run along Z = 17.0m (X: 4 to 24)
            if (pipeStraight != null)
            {
                for (float x = 6.0f; x <= 22.0f; x += 4.0f)
                {
                    var p = Object.Instantiate(pipeStraight, conduitRoot.transform, false);
                    p.name = $"Overhead_Duct_South_X{x}";
                    p.transform.position = new Vector3(x, ductY, 17.0f);
                    p.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    p.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                }

                // 2. North High-Bay Duct Run along Z = 29.0m (X: 4 to 24)
                for (float x = 6.0f; x <= 22.0f; x += 4.0f)
                {
                    var p = Object.Instantiate(pipeStraight, conduitRoot.transform, false);
                    p.name = $"Overhead_Duct_North_X{x}";
                    p.transform.position = new Vector3(x, ductY, 29.0f);
                    p.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    p.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                }
            }

            // Cross junctions
            if (pipeCross != null)
            {
                var pc1 = Object.Instantiate(pipeCross, conduitRoot.transform, false);
                pc1.name = "Duct_Cross_South";
                pc1.transform.position = new Vector3(14.0f, ductY, 17.0f);
                pc1.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

                var pc2 = Object.Instantiate(pipeCross, conduitRoot.transform, false);
                pc2.name = "Duct_Cross_North";
                pc2.transform.position = new Vector3(14.0f, ductY, 29.0f);
                pc2.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            }


        }

        private static void BuildInteriorAtmosphereAndLighting(
            Transform parent, GameObject ceilingLight, Material cyanEmissiveMat, Material smokeMat)
        {
            // 0. Ambient Environment Lighting Lift (Brightens the whole interior facility, eliminating pitch-black shadows)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.40f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.24f, 0.30f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.30f);

            var lightingGroup = new GameObject("Citadel_Interior_Lighting");
            lightingGroup.transform.SetParent(parent, false);

            // 1. Central Hall Ambient Fill Light
            var ambientFillGo = new GameObject("Interior_Ambient_Fill_Light");
            ambientFillGo.transform.SetParent(lightingGroup.transform, false);
            ambientFillGo.transform.position = new Vector3(14.0f, 5.0f, 23.0f);
            var fillLight = ambientFillGo.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 45.0f;
            fillLight.intensity = 2.2f;
            fillLight.color = new Color(0.88f, 0.95f, 1.0f);

            // 1b. North Wing Dedicated Ambient Light
            var northFillGo = new GameObject("North_Wing_Ambient_Fill_Light");
            northFillGo.transform.SetParent(lightingGroup.transform, false);
            northFillGo.transform.position = new Vector3(14.0f, 5.2f, 43.0f);
            var northLight = northFillGo.AddComponent<Light>();
            northLight.type = LightType.Point;
            northLight.range = 26.0f;
            northLight.intensity = 2.4f;
            northLight.color = new Color(0.92f, 0.96f, 1.0f);

            // 2. Floor Baseboard Neon Trims (Cyan / Neon Blue ambient glow lines)
            if (cyanEmissiveMat != null)
            {
                var neonSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                neonSouth.name = "Baseboard_Neon_South";
                neonSouth.transform.SetParent(lightingGroup.transform, false);
                neonSouth.transform.position = new Vector3(14.0f, 0.14f, 9.10f);
                neonSouth.transform.localScale = new Vector3(28.0f, 0.06f, 0.06f);
                Object.DestroyImmediate(neonSouth.GetComponent<Collider>());
                neonSouth.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                var neonNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                neonNorth.name = "Baseboard_Neon_North_Hall";
                neonNorth.transform.SetParent(lightingGroup.transform, false);
                neonNorth.transform.position = new Vector3(14.0f, 0.14f, 48.90f);
                neonNorth.transform.localScale = new Vector3(22.0f, 0.06f, 0.06f);
                Object.DestroyImmediate(neonNorth.GetComponent<Collider>());
                neonNorth.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // 3. High-Bay Overhead Spotlights (Complete balanced coverage across atrium & wings)
            Vector3[] floodlightPos = new Vector3[]
            {
                new Vector3(14.0f, 11.2f, 23.0f),
                new Vector3(8.0f, 7.6f, 19.0f),
                new Vector3(20.0f, 7.6f, 19.0f),
                new Vector3(8.0f, 7.6f, 27.0f),
                new Vector3(20.0f, 7.6f, 27.0f),
                new Vector3(-6.0f, 7.80f, 23.0f),
                new Vector3(8.0f, 7.5f, 43.0f),
                new Vector3(14.0f, 7.5f, 43.0f),
                new Vector3(20.0f, 7.5f, 43.0f),
                new Vector3(14.0f, 7.5f, 37.0f)
            };

            foreach (var pos in floodlightPos)
            {
                var lightGo = new GameObject("Ceiling_HighBay_Light");
                lightGo.transform.SetParent(lightingGroup.transform, false);
                lightGo.transform.position = pos;

                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 20f;
                l.spotAngle = 85f;
                l.intensity = 3.2f;
                l.color = new Color(0.92f, 0.97f, 1.0f);
                lightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                if (ceilingLight != null)
                {
                    var fix = Object.Instantiate(ceilingLight, lightGo.transform, false);
                    fix.name = "Fixture_Model";
                    fix.transform.localPosition = Vector3.zero;
                    fix.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                }
            }

            // 4. Volumetric Space Dust Motes Particle System
            var dustGo = new GameObject("Citadel_Atmospheric_Dust_Motes");
            dustGo.transform.SetParent(lightingGroup.transform, false);
            dustGo.transform.position = new Vector3(14.0f, 4.0f, 25.0f);

            var ps = dustGo.AddComponent<ParticleSystem>();
            var psRend = dustGo.GetComponent<ParticleSystemRenderer>();
            if (psRend != null && smokeMat != null) psRend.sharedMaterial = smokeMat;

            var main = ps.main;
            main.maxParticles = 120;
            main.startLifetime = 14f;
            main.startSpeed = 0.12f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.85f, 0.95f, 1.0f, 0.40f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28.0f, 7.0f, 26.0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.15f;
        }

        private static void BuildNorthMissionControlWing(
            Transform parent,
            GameObject bigScreenPrefab,
            GameObject consoleScreenPrefab,
            GameObject consoleCeilingPrefab,
            GameObject deskPrefab,
            GameObject pilotSeatPrefab,
            GameObject cabinetPrefab,
            GameObject cabinetLPrefab,
            GameObject shelfPrefab,
            GameObject plantPrefab,
            GameObject plantDeskPrefab,
            GameObject plantSmallPrefab,
            GameObject hydroponicPrefab,
            GameObject art1Prefab,
            GameObject art2Prefab,
            GameObject tableGlassPrefab,
            GameObject decoChairPrefab,
            Material cyanEmissiveMat,
            Material steelAccentMat,
            Material darkTitaniumMat,
            Material radarScopeMat,
            Material centralHoloMat,
            Material telemetryLMat,
            Material telemetryRMat,
            Material deskTelemetryMat,
            Material deskDiagnosticsMat)
        {
            Transform existing = parent.Find("North_Mission_Control_Wing");
            if (existing != null)
            {
                if (Application.isPlaying) Object.Destroy(existing.gameObject);
                else Object.DestroyImmediate(existing.gameObject);
            }

            var wingRoot = new GameObject("North_Mission_Control_Wing");
            wingRoot.transform.SetParent(parent, false);

            System.Action<GameObject, int, Material> setMat = (go, idx, mat) =>
            {
                if (go == null || mat == null) return;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr == null) mr = go.GetComponentInChildren<MeshRenderer>();
                if (mr != null && mr.sharedMaterials.Length > idx)
                {
                    var mats = mr.sharedMaterials;
                    mats[idx] = mat;
                    mr.sharedMaterials = mats;
                }
            };

            // ─────────────────────────────────────────────────────────────────
            // 1. ELEVATED COMMAND VIDEO WALL (Bảng điện tử để cao lên trên tường)
            // ─────────────────────────────────────────────────────────────────
            var videoWallGroup = new GameObject("Mission_Control_Panoramic_Wall");
            videoWallGroup.transform.SetParent(wingRoot.transform, false);

            // 1.1 Elevated Wall Brackets & Neon Trims
            var bottomPlinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomPlinth.name = "VideoWall_Bottom_Plinth";
            bottomPlinth.transform.SetParent(videoWallGroup.transform, false);
            bottomPlinth.transform.position = new Vector3(14.0f, 1.85f, 48.70f);
            bottomPlinth.transform.localScale = new Vector3(18.0f, 0.25f, 0.30f);
            Object.DestroyImmediate(bottomPlinth.GetComponent<Collider>());
            if (darkTitaniumMat != null) bottomPlinth.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

            var bottomNeon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomNeon.name = "VideoWall_Bottom_Neon";
            bottomNeon.transform.SetParent(videoWallGroup.transform, false);
            bottomNeon.transform.position = new Vector3(14.0f, 2.00f, 48.55f);
            bottomNeon.transform.localScale = new Vector3(18.0f, 0.05f, 0.05f);
            Object.DestroyImmediate(bottomNeon.GetComponent<Collider>());
            if (cyanEmissiveMat != null) bottomNeon.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            var topHeader = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topHeader.name = "VideoWall_Top_Header";
            topHeader.transform.SetParent(videoWallGroup.transform, false);
            topHeader.transform.position = new Vector3(14.0f, 5.35f, 48.65f);
            topHeader.transform.localScale = new Vector3(18.0f, 0.10f, 0.20f);
            Object.DestroyImmediate(topHeader.GetComponent<Collider>());
            if (cyanEmissiveMat != null) topHeader.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // 1.2 3 Grand Panoramic Command Displays (Elevated High at Y ≈ 3.65m with 7 deg downward tilt!)
            if (bigScreenPrefab != null)
            {
                // Left Tactical Radar Screen
                var scrL = Object.Instantiate(bigScreenPrefab, videoWallGroup.transform, false);
                scrL.name = "Command_Screen_Tactical_Left";
                scrL.transform.position = new Vector3(8.5f, 3.60f, 48.60f);
                scrL.transform.rotation = Quaternion.Euler(7f, 186f, 0f);
                scrL.transform.localScale = new Vector3(1.25f, 1.25f, 1.0f);
                setMat(scrL, 0, radarScopeMat);
                if (steelAccentMat != null) setMat(scrL, 1, steelAccentMat);

                // Center Master Planetary Holo Screen
                var scrC = Object.Instantiate(bigScreenPrefab, videoWallGroup.transform, false);
                scrC.name = "Command_Screen_Master_Center";
                scrC.transform.position = new Vector3(14.0f, 3.65f, 48.70f);
                scrC.transform.rotation = Quaternion.Euler(7f, 180f, 0f);
                scrC.transform.localScale = new Vector3(1.40f, 1.40f, 1.0f);
                setMat(scrC, 0, centralHoloMat);
                if (steelAccentMat != null) setMat(scrC, 1, steelAccentMat);

                // Right Telemetry & Systems Screen
                var scrR = Object.Instantiate(bigScreenPrefab, videoWallGroup.transform, false);
                scrR.name = "Command_Screen_Telemetry_Right";
                scrR.transform.position = new Vector3(19.5f, 3.60f, 48.60f);
                scrR.transform.rotation = Quaternion.Euler(7f, 174f, 0f);
                scrR.transform.localScale = new Vector3(1.25f, 1.25f, 1.0f);
                setMat(scrR, 0, telemetryRMat);
                if (steelAccentMat != null) setMat(scrR, 1, steelAccentMat);
            }

            // ─────────────────────────────────────────────────────────────────
            // 2. OPERATOR WORKSTATIONS: DESKS & CHAIRS (Bàn máy tính để sát vào)
            // Desks pushed in to Z = 46.20m and paired closely at X = 11.8m and X = 16.2m!
            // ─────────────────────────────────────────────────────────────────
            var workstationsGroup = new GameObject("Operator_Workstations");
            workstationsGroup.transform.SetParent(wingRoot.transform, false);

            Vector3[] deskPositions = new Vector3[]
            {
                new Vector3(11.8f, 0.10f, 46.20f),  // Station 1: Tactical Operator
                new Vector3(16.2f, 0.10f, 46.20f)   // Station 2: Systems Operator
            };
            Vector3[] chairPositions = new Vector3[]
            {
                new Vector3(11.8f, 0.10f, 45.25f),
                new Vector3(16.2f, 0.10f, 45.25f)
            };

            for (int s = 0; s < deskPositions.Length; s++)
            {
                // Desk with dual monitors facing operator
                if (deskPrefab != null)
                {
                    var d = Object.Instantiate(deskPrefab, workstationsGroup.transform, false);
                    d.name = $"Operator_Desk_{s + 1}";
                    d.transform.position = deskPositions[s];
                    d.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    d.transform.localScale = new Vector3(0.90f, 0.90f, 0.90f);

                    setMat(d, 1, (s == 0) ? deskTelemetryMat : deskDiagnosticsMat);
                }

                // Operator Pilot Chair
                if (pilotSeatPrefab != null)
                {
                    var ch = Object.Instantiate(pilotSeatPrefab, workstationsGroup.transform, false);
                    ch.name = $"Operator_Seat_{s + 1}";
                    ch.transform.position = chairPositions[s];
                    ch.transform.rotation = Quaternion.identity;
                    ch.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                }

                // Workstation Task Spotlight overhead
                var spotGo = new GameObject($"Workstation_Spot_{s + 1}");
                spotGo.transform.SetParent(workstationsGroup.transform, false);
                spotGo.transform.position = deskPositions[s] + new Vector3(0f, 4.2f, -0.2f);
                spotGo.transform.rotation = Quaternion.Euler(85f, 0f, 0f);
                var spot = spotGo.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.range = 7.5f;
                spot.spotAngle = 55f;
                spot.intensity = 3.2f;
                spot.color = new Color(0.92f, 0.97f, 1.0f);
            }

            // Wall Wash Accent Lights for North Command Screens
            var wash1 = new GameObject("Command_Wall_Wash_Left");
            wash1.transform.SetParent(wingRoot.transform, false);
            wash1.transform.position = new Vector3(8.5f, 2.2f, 47.2f);
            var lw1 = wash1.AddComponent<Light>();
            lw1.type = LightType.Point;
            lw1.range = 10.0f;
            lw1.intensity = 2.0f;
            lw1.color = new Color(0.75f, 0.90f, 1.0f);

            var wash2 = new GameObject("Command_Wall_Wash_Right");
            wash2.transform.SetParent(wingRoot.transform, false);
            wash2.transform.position = new Vector3(19.5f, 2.2f, 47.2f);
            var lw2 = wash2.AddComponent<Light>();
            lw2.type = LightType.Point;
            lw2.range = 10.0f;
            lw2.intensity = 2.0f;
            lw2.color = new Color(0.75f, 0.90f, 1.0f);

            // 2.5 INTERMEDIATE TIER: FLIGHT & SYSTEMS TACTICAL CONSOLE BANK (Z = 39.5m)
            var frontRowGroup = new GameObject("Front_Tier_Workstations");
            frontRowGroup.transform.SetParent(wingRoot.transform, false);

            Vector3[] frontDeskPos = new Vector3[]
            {
                new Vector3(10.2f, 0.10f, 39.50f),
                new Vector3(17.8f, 0.10f, 39.50f)
            };
            for (int f = 0; f < frontDeskPos.Length; f++)
            {
                if (deskPrefab != null)
                {
                    var fd = Object.Instantiate(deskPrefab, frontRowGroup.transform, false);
                    fd.name = $"FrontTier_Desk_{f + 1}";
                    fd.transform.position = frontDeskPos[f];
                    fd.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    fd.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                    setMat(fd, 1, (f == 0) ? deskTelemetryMat : deskDiagnosticsMat);
                }
                if (pilotSeatPrefab != null)
                {
                    var fch = Object.Instantiate(pilotSeatPrefab, frontRowGroup.transform, false);
                    fch.name = $"FrontTier_Seat_{f + 1}";
                    fch.transform.position = frontDeskPos[f] + new Vector3(0f, 0f, -0.90f);
                    fch.transform.rotation = Quaternion.identity;
                    fch.transform.localScale = new Vector3(0.80f, 0.80f, 0.80f);
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // 3. RIGHT-SIDE MODULAR CREDENZA CONSOLE & DECORATIONS (Xếp có hệ thống, liền mạch 6m)
            // 3 tủ thẳng nối liền thành dãy console sang trọng dọc tường Đông, trang trí đối xứng
            // ─────────────────────────────────────────────────────────────────
            var rightWingGroup = new GameObject("Right_Decorative_Lounge_And_Cabinets");
            rightWingGroup.transform.SetParent(wingRoot.transform, false);

            if (cabinetPrefab != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    float zPos = 41.5f + i * 2.0f; // 41.5, 43.5, 45.5
                    var cab = Object.Instantiate(cabinetPrefab, rightWingGroup.transform, false);
                    cab.name = $"East_Credenza_Unit_{i + 1}";
                    cab.transform.position = new Vector3(25.20f, 0.10f, zPos);
                    cab.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

                    // Symmetrical decorations on the credenza top (Y = 1.90m)
                    if (i == 0 || i == 2)
                    {
                        if (plantDeskPrefab != null)
                        {
                            var p = Object.Instantiate(plantDeskPrefab, rightWingGroup.transform, false);
                            p.name = $"Credenza_Plant_{i + 1}";
                            p.transform.position = new Vector3(24.85f, 1.90f, zPos + 1.0f);
                            p.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                        }
                    }
                    else
                    {
                        var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        core.name = "Credenza_Data_Core";
                        core.transform.SetParent(rightWingGroup.transform, false);
                        core.transform.position = new Vector3(24.85f, 1.90f, zPos + 1.0f);
                        core.transform.localScale = new Vector3(0.22f, 0.12f, 0.22f);
                        Object.DestroyImmediate(core.GetComponent<Collider>());
                        if (cyanEmissiveMat != null) core.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
                    }
                }
            }

            // 3.2 Framed Sci-Fi Wall Art centered above credenza
            if (art1Prefab != null)
            {
                var art1 = Object.Instantiate(art1Prefab, rightWingGroup.transform, false);
                art1.name = "East_Wall_Art_Master";
                art1.transform.position = new Vector3(25.45f, 2.75f, 44.50f);
                art1.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                art1.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
            }

            // 3.3 Single Elegant Tall Floor Plant in North-East Corner
            if (plantPrefab != null)
            {
                var cornerPlant = Object.Instantiate(plantPrefab, rightWingGroup.transform, false);
                cornerPlant.name = "NorthEast_Corner_Floor_Plant";
                cornerPlant.transform.position = new Vector3(23.20f, 0.10f, 48.00f);
                cornerPlant.transform.localScale = new Vector3(1.20f, 1.20f, 1.20f);
            }

            // 3.4 Warm Accent Lighting for the Decorative Display Area
            var displaySpotGo = new GameObject("East_Cabinet_Display_Spot");
            displaySpotGo.transform.SetParent(rightWingGroup.transform, false);
            displaySpotGo.transform.position = new Vector3(23.50f, 4.20f, 44.50f);
            displaySpotGo.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
            var displaySpot = displaySpotGo.AddComponent<Light>();
            displaySpot.type = LightType.Spot;
            displaySpot.range = 7.5f;
            displaySpot.spotAngle = 60f;
            displaySpot.intensity = 2.4f;
            displaySpot.color = new Color(1.0f, 0.92f, 0.80f);
        }

        private static void BuildAirlockPortalCasing(Transform parent, Vector3 pos, float rotY, Material baseColorMat, Material darkTitaniumMat)
        {
            string rootName = pos.x > 0 ? "East_Chamber_Airlock_Sealing_Casing" : "West_Gate_Airlock_Sealing_Casing";
            var root = new GameObject(rootName);
            if (parent != null) root.transform.SetParent(parent, false);

            System.Func<Transform, string, Vector3, Vector3, Material, GameObject> createBox = (p, name, bPos, size, mat) => {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(p, false);
                go.transform.position = bPos;
                go.transform.localScale = size;
                var mr = go.GetComponent<Renderer>();
                if (mr != null && mat != null) mr.sharedMaterial = mat;
                return go;
            };

            float xJamb = pos.x > 0 ? 30.55f : -10.60f;
            float xDoor = pos.x > 0 ? 30.00f : -10.00f;
            float xTrim1 = pos.x > 0 ? 29.48f : -11.68f;
            float xTrim2 = pos.x > 0 ? 31.62f : -9.48f;
            float jambXSpan = pos.x > 0 ? 2.10f : 2.15f;

            // 1. South & North Jambs
            createBox(root.transform, "Portal_Jamb_South", new Vector3(xJamb, 2.10f, 21.00f), new Vector3(jambXSpan, 4.02f, 0.85f), baseColorMat);
            createBox(root.transform, "Portal_Jamb_North", new Vector3(xJamb, 2.10f, 25.00f), new Vector3(jambXSpan, 4.02f, 0.85f), baseColorMat);

            // 2. Lintel & Threshold Sill
            createBox(root.transform, "Portal_Lintel_Top", new Vector3(xJamb, 4.12f, 23.00f), new Vector3(jambXSpan, 0.28f, 4.85f), baseColorMat);
            createBox(root.transform, "Portal_Threshold_Sill", new Vector3(xJamb, 0.08f, 23.00f), new Vector3(jambXSpan, 0.15f, 4.85f), baseColorMat);

            // 3. Door Sealing Collars
            createBox(root.transform, "Portal_Door_Sealing_Collar_South", new Vector3(xDoor, 2.10f, 21.25f), new Vector3(1.00f, 4.02f, 0.40f), baseColorMat);
            createBox(root.transform, "Portal_Door_Sealing_Collar_North", new Vector3(xDoor, 2.10f, 24.75f), new Vector3(1.00f, 4.02f, 0.40f), baseColorMat);

            // 4. Trims (Exterior & Interior)
            createBox(root.transform, "Portal_Trim_Face1_South", new Vector3(xTrim1, 2.10f, 21.00f), new Vector3(0.08f, 4.04f, 0.88f), darkTitaniumMat ?? baseColorMat);
            createBox(root.transform, "Portal_Trim_Face1_North", new Vector3(xTrim1, 2.10f, 25.00f), new Vector3(0.08f, 4.04f, 0.88f), darkTitaniumMat ?? baseColorMat);
            createBox(root.transform, "Portal_Trim_Face1_Top", new Vector3(xTrim1, 4.14f, 23.00f), new Vector3(0.08f, 0.28f, 4.88f), darkTitaniumMat ?? baseColorMat);

            createBox(root.transform, "Portal_Trim_Face2_South", new Vector3(xTrim2, 2.10f, 21.00f), new Vector3(0.08f, 4.04f, 0.88f), darkTitaniumMat ?? baseColorMat);
            createBox(root.transform, "Portal_Trim_Face2_North", new Vector3(xTrim2, 2.10f, 25.00f), new Vector3(0.08f, 4.04f, 0.88f), darkTitaniumMat ?? baseColorMat);
            createBox(root.transform, "Portal_Trim_Face2_Top", new Vector3(xTrim2, 4.14f, 23.00f), new Vector3(0.08f, 0.28f, 4.88f), darkTitaniumMat ?? baseColorMat);
        }

        private static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
