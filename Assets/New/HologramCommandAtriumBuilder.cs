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
            Vector3 landingPadPos = new Vector3(-32.00f, 0.12f, 23.00f);

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
                    b.position = new Vector3(bx, 0.32f, bz);
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
            Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Cosmic_Smoke.mat");

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
                        autoDoor.cyanLedMat = cyanEmissiveMat;
                        autoDoor.amberLedMat = coreGlowMat;
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
                    autoDoorEast.autoProximity = true;
                    autoDoorEast.triggerDistance = 8.0f;
                    autoDoorEast.closeDistance = 12.0f;

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
                projectorPrefab, compStation, pilotSeat, darkTitaniumMat, steelAccentMat, cyanEmissiveMat, coreGlowMat);

            // ── ZONE 2: SERVER RACKS & CURVED TERMINAL DATA BAYS ──
            BuildServerAndTerminalBays(propsGroup.transform, cabinetL, consolePrefab, consoleScreenPrefab,
                consoleCeiling, pilotSeat, cableThick, cyanEmissiveMat, coreGlowMat);

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

            // NORTH WING: PANORAMIC MISSION CONTROL VIDEO WALL
            if (bigScreen != null)
            {
                spawnProp(bigScreen, propsGroup.transform, new Vector3(14.0f, 2.20f, 48.0f), Quaternion.Euler(0f, 180f, 0f));
                spawnProp(bigScreen, propsGroup.transform, new Vector3(10.0f, 2.20f, 48.0f), Quaternion.Euler(0f, 180f, 0f));
                spawnProp(bigScreen, propsGroup.transform, new Vector3(18.0f, 2.20f, 48.0f), Quaternion.Euler(0f, 180f, 0f));
            }
            if (consoleScreenPrefab != null)
            {
                spawnProp(consoleScreenPrefab, propsGroup.transform, new Vector3(10.0f, 0.10f, 46.8f), Quaternion.Euler(0f, 180f, 0f));
                spawnProp(consoleScreenPrefab, propsGroup.transform, new Vector3(14.0f, 0.10f, 46.8f), Quaternion.Euler(0f, 180f, 0f));
                spawnProp(consoleScreenPrefab, propsGroup.transform, new Vector3(18.0f, 0.10f, 46.8f), Quaternion.Euler(0f, 180f, 0f));
            }
            if (pilotSeat != null)
            {
                spawnProp(pilotSeat, propsGroup.transform, new Vector3(10.0f, 0.10f, 45.6f), Quaternion.identity);
                spawnProp(pilotSeat, propsGroup.transform, new Vector3(14.0f, 0.10f, 45.6f), Quaternion.identity);
                spawnProp(pilotSeat, propsGroup.transform, new Vector3(18.0f, 0.10f, 45.6f), Quaternion.identity);
            }
            var headBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headBar.name = "Mission_Control_Header_Bar";
            headBar.transform.SetParent(propsGroup.transform, false);
            headBar.transform.position = new Vector3(14.0f, 3.80f, 47.8f);
            headBar.transform.localScale = new Vector3(12.0f, 0.25f, 0.40f);
            Object.DestroyImmediate(headBar.GetComponent<Collider>());
            if (cyanEmissiveMat != null) headBar.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // ── DEDICATED PORTFOLIO MASTER COMMAND CHAMBER (EAST WING - EXACT IMAGE DESIGN) ──
            GameObject deskPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/desk.prefab");
            GameObject deskNoCompPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/desk_no_computer.prefab");
            GameObject bigScreenPrefab = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab");
            GameObject cabinet = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab") ?? cabinetL;
            GameObject ceilingLight2 = LoadPrefab("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_celing_2.prefab") ?? ceilingLight;
            Material radarScopeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Radar_Scope.mat");
            Material centralHoloMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Central_Holo_Ring.mat");
            Material telemetryLMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Telemetry_Left.mat");
            Material telemetryRMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_Telemetry_Right.mat");
            Material deskTelemetryMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Desk_Monitors_Telemetry.mat");
            Material deskDiagnosticsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Desk_Monitors_Diagnostics.mat");

            DesignPortfolioMasterChamber(stationGo, propsGroup.transform, deskPrefab, deskNoCompPrefab, bigScreenPrefab,
                pipeStraight, cableThick, pilotSeat, cabinet, ceilingLight2, columnPillar, wallShelf,
                darkTitaniumMat, steelAccentMat, cyanEmissiveMat, smokeMat,
                radarScopeMat, centralHoloMat, telemetryLMat, telemetryRMat,
                deskTelemetryMat, deskDiagnosticsMat);

            // OPEN MONUMENTAL SCI-FI PORTAL ARCHES (Replaces solid blocking corridors!)
            if (columnPillar != null)
            {
                // North Wing Entrance Portal
                var nP1 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                nP1.transform.position = new Vector3(10.0f, 0.10f, 36.5f);
                var nP2 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                nP2.transform.position = new Vector3(18.0f, 0.10f, 36.5f);
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
            // A. SIDE WALL HORIZONTAL INDUSTRIAL PIPES & ACCENTS (FROM ASSET PACK)
            // ─────────────────────────────────────────────────────────────────
            if (pipeStraight != null)
            {
                // North Wall (Z = 28.7m, X: 31 to 41)
                for (float x = 31.0f; x <= 41.0f; x += 3.5f)
                {
                    var pUpper = Object.Instantiate(pipeStraight, chamberGo.transform, false);
                    pUpper.name = $"EastChamber_NorthPipe_Upper_X{x}";
                    pUpper.transform.position = new Vector3(x, 2.20f, 28.70f);
                    pUpper.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    pUpper.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

                    var pLower = Object.Instantiate(pipeStraight, chamberGo.transform, false);
                    pLower.name = $"EastChamber_NorthPipe_Lower_X{x}";
                    pLower.transform.position = new Vector3(x, 1.40f, 28.70f);
                    pLower.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    pLower.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                }

                // South Wall (Z = 17.3m, X: 31 to 41)
                for (float x = 31.0f; x <= 41.0f; x += 3.5f)
                {
                    var pUpper = Object.Instantiate(pipeStraight, chamberGo.transform, false);
                    pUpper.name = $"EastChamber_SouthPipe_Upper_X{x}";
                    pUpper.transform.position = new Vector3(x, 2.20f, 17.30f);
                    pUpper.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    pUpper.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

                    var pLower = Object.Instantiate(pipeStraight, chamberGo.transform, false);
                    pLower.name = $"EastChamber_SouthPipe_Lower_X{x}";
                    pLower.transform.position = new Vector3(x, 1.40f, 17.30f);
                    pLower.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    pLower.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                }
            }

            // ─────────────────────────────────────────────────────────────────
            // A2. SIDE WALL HEXAGONAL SHELVING UNITS (100% MODULAR shelf.prefab)
            // ─────────────────────────────────────────────────────────────────
            if (wallShelf != null)
            {
                var sL1 = Object.Instantiate(wallShelf, chamberGo.transform, false);
                sL1.name = "Chamber_Hex_Shelf_North_1";
                sL1.transform.position = new Vector3(34.2f, 2.10f, 28.80f);
                sL1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                var sL2 = Object.Instantiate(wallShelf, chamberGo.transform, false);
                sL2.name = "Chamber_Hex_Shelf_North_2";
                sL2.transform.position = new Vector3(36.8f, 2.10f, 28.80f);
                sL2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                var sR1 = Object.Instantiate(wallShelf, chamberGo.transform, false);
                sR1.name = "Chamber_Hex_Shelf_South_1";
                sR1.transform.position = new Vector3(34.2f, 2.10f, 17.20f);
                sR1.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                var sR2 = Object.Instantiate(wallShelf, chamberGo.transform, false);
                sR2.name = "Chamber_Hex_Shelf_South_2";
                sR2.transform.position = new Vector3(36.8f, 2.10f, 17.20f);
                sR2.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

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

                var oldDeskChildren = new List<GameObject>();
                foreach (Transform c in centerDesk) oldDeskChildren.Add(c.gameObject);
                foreach (var g in oldDeskChildren)
                {
                    if (Application.isPlaying) Object.Destroy(g);
                    else Object.DestroyImmediate(g);
                }

                // Center Command Desk: Real asset desk_no_computer
                if (deskNoCompPrefab != null)
                {
                    var cDesk = Object.Instantiate(deskNoCompPrefab, centerDesk, false);
                    cDesk.name = "Center_Bridge_Asset_Desk";
                    cDesk.transform.localPosition = new Vector3(0f, -0.35f, 0f);
                    cDesk.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }

                // Commander Pilot Chair (Center)
                if (pilotSeat != null)
                {
                    var cmdChair = Object.Instantiate(pilotSeat, centerDesk, false);
                    cmdChair.name = "Commander_Pilot_Chair_Center";
                    cmdChair.transform.localPosition = new Vector3(0f, 0.05f, -0.80f);
                    cmdChair.transform.localRotation = Quaternion.identity;
                    cmdChair.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
                }
            }

            // Left & Right Console Wings
            Transform existingWings = stationGo.transform.Find("Curved_Console_Wing_Plinths");
            if (existingWings != null)
            {
                if (Application.isPlaying) Object.Destroy(existingWings.gameObject);
                else Object.DestroyImmediate(existingWings.gameObject);
            }

            var wingsRoot = new GameObject("Curved_Console_Wing_Plinths");
            wingsRoot.transform.SetParent(stationGo.transform, false);

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

            if (deskPrefab != null)
            {
                // Left Wing Background Desk (Aft)
                var leftDeskAft = Object.Instantiate(deskPrefab, wingsRoot.transform, false);
                leftDeskAft.name = "Left_Wing_Asset_Desk";
                leftDeskAft.transform.localPosition = new Vector3(-3.85f, -0.35f, -1.85f);
                leftDeskAft.transform.localRotation = Quaternion.Euler(0f, 145f, 0f);
                leftDeskAft.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(leftDeskAft, deskTelemetryMat);

                // Left Wing Foreground Desk (Fore)
                var leftDeskFore = Object.Instantiate(deskPrefab, wingsRoot.transform, false);
                leftDeskFore.name = "Left_Wing_Asset_Desk_Fore";
                leftDeskFore.transform.localPosition = new Vector3(-6.58f, -0.22f, -5.83f);
                leftDeskFore.transform.localRotation = Quaternion.Euler(0f, 150f, 0f);
                leftDeskFore.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(leftDeskFore, deskTelemetryMat);

                // Right Wing Background Desk (Aft)
                var rightDeskAft = Object.Instantiate(deskPrefab, wingsRoot.transform, false);
                rightDeskAft.name = "Right_Wing_Asset_Desk";
                rightDeskAft.transform.localPosition = new Vector3(3.85f, -0.35f, -1.85f);
                rightDeskAft.transform.localRotation = Quaternion.Euler(0f, 215f, 0f);
                rightDeskAft.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(rightDeskAft, deskDiagnosticsMat);

                // Right Wing Foreground Desk (Fore)
                var rightDeskFore = Object.Instantiate(deskPrefab, wingsRoot.transform, false);
                rightDeskFore.name = "Right_Wing_Asset_Desk_Fore";
                rightDeskFore.transform.localPosition = new Vector3(6.93f, -0.05f, -5.29f);
                rightDeskFore.transform.localRotation = Quaternion.Euler(0f, 210f, 0f);
                rightDeskFore.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                assignDeskMonitorMat(rightDeskFore, deskDiagnosticsMat);
            }

            if (pilotSeat != null)
            {
                var chairLAft = Object.Instantiate(pilotSeat, wingsRoot.transform, false);
                chairLAft.name = "Officer_Chair_Left_Aft";
                chairLAft.transform.localPosition = new Vector3(-2.85f, 0.05f, -1.85f);
                chairLAft.transform.localRotation = Quaternion.Euler(0f, 65f, 0f);
                chairLAft.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

                var chairLFore = Object.Instantiate(pilotSeat, wingsRoot.transform, false);
                chairLFore.name = "Officer_Chair_Left_Fore";
                chairLFore.transform.localPosition = new Vector3(-5.20f, 0.05f, -5.60f);
                chairLFore.transform.localRotation = Quaternion.Euler(0f, 70f, 0f);
                chairLFore.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

                var chairRAft = Object.Instantiate(pilotSeat, wingsRoot.transform, false);
                chairRAft.name = "Officer_Chair_Right_Aft";
                chairRAft.transform.localPosition = new Vector3(2.85f, 0.05f, -1.85f);
                chairRAft.transform.localRotation = Quaternion.Euler(0f, 115f, 0f);
                chairRAft.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

                var chairRFore = Object.Instantiate(pilotSeat, wingsRoot.transform, false);
                chairRFore.name = "Officer_Chair_Right_Fore";
                chairRFore.transform.localPosition = new Vector3(5.50f, 0.05f, -5.30f);
                chairRFore.transform.localRotation = Quaternion.Euler(0f, 110f, 0f);
                chairRFore.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // ─────────────────────────────────────────────────────────────────
            // E. THE MULTI-SCREEN VIDEO WALL (MAIN DISPLAY & ANGLED FLANKING WINGS)
            // ─────────────────────────────────────────────────────────────────
            Transform mainScreen = stationGo.transform.Find("MainScreen");
            if (mainScreen != null)
            {
                mainScreen.localPosition = new Vector3(0f, 0.35f, 0.85f);
                mainScreen.localRotation = Quaternion.identity;

                string[] clutterNames = new string[] {
                    "Frame_Bottom", "Frame_BottomStrip", "Frame_Top", "Frame_TopStrip",
                    "Screen_Pillar", "Screen_PillarStrip", "Screen_PillarFoot",
                    "MainScreen_Bezel_Bottom", "MainScreen_Bezel_Top", "Command_Viewscreen_Arch",
                    "Screen_Frame_BigScreen", "Laser_Scan_Bar", "MainScreen_Backing_Frame", "MainScreen_Corner_Brackets"
                };
                foreach (Transform c in mainScreen)
                {
                    foreach (var name in clutterNames)
                    {
                        if (c.name.StartsWith(name))
                        {
                            c.gameObject.SetActive(false);
                            break;
                        }
                    }
                }

                // Clean all old mock backing frames
                var oldBackings = new List<GameObject>();
                foreach (Transform c in mainScreen)
                {
                    if (c.name.StartsWith("MainScreen_Bezel") || c.name.StartsWith("MainScreen_Backing") || c.name.StartsWith("MainScreen_Corner") || c.name == "MainScreen_Asset_Frame")
                        oldBackings.Add(c.gameObject);
                }
                foreach (var g in oldBackings)
                {
                    if (Application.isPlaying) Object.Destroy(g);
                    else Object.DestroyImmediate(g);
                }

                // Real big_screen.prefab asset frame
                if (bigScreenPrefab != null)
                {
                    var bs = Object.Instantiate(bigScreenPrefab, mainScreen, false);
                    bs.name = "MainScreen_Asset_Frame";
                    bs.transform.localPosition = new Vector3(0f, 2.75f, 0.15f);
                    bs.transform.localRotation = Quaternion.identity;
                    bs.transform.localScale = new Vector3(1.45f, 1.30f, 1.0f);
                }
            }

            // Left Multi-Screen Wing (Clean backing from Image 4)
            Transform existingLeftWing = stationGo.transform.Find("Chamber_Left_MultiScreen_Wing");
            if (existingLeftWing != null)
            {
                if (Application.isPlaying) Object.Destroy(existingLeftWing.gameObject);
                else Object.DestroyImmediate(existingLeftWing.gameObject);
            }

            var leftWingScreenGo = new GameObject("Chamber_Left_MultiScreen_Wing");
            leftWingScreenGo.transform.SetParent(stationGo.transform, false);
            leftWingScreenGo.transform.localPosition = new Vector3(-3.85f, 2.30f, 0.65f);
            leftWingScreenGo.transform.localRotation = Quaternion.Euler(0f, -24f, 0f);

            if (bigScreenPrefab != null)
            {
                var bsL = Object.Instantiate(bigScreenPrefab, leftWingScreenGo.transform, false);
                bsL.name = "LeftWing_Asset_Frame";
                bsL.transform.localPosition = new Vector3(0f, 0.15f, 0.15f);
                bsL.transform.localRotation = Quaternion.identity;
                bsL.transform.localScale = new Vector3(0.80f, 1.05f, 1.0f);
            }

            // Middle Screen on Left Wing: Sign_Prev
            Transform signPrev = stationGo.transform.Find("Sign_Prev");
            if (signPrev != null)
            {
                signPrev.localPosition = new Vector3(-3.55f, 2.45f, 0.52f);
                signPrev.localRotation = Quaternion.Euler(0f, -24f, 0f);
                signPrev.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            }

            // Bottom Screen on Left Wing: Controls_Board
            Transform controlsBoard = stationGo.transform.Find("Controls_Board");
            if (controlsBoard != null)
            {
                controlsBoard.localPosition = new Vector3(-3.55f, -1.03f, 0.52f);
                controlsBoard.localRotation = Quaternion.Euler(0f, -24f, 0f);
                controlsBoard.localScale = new Vector3(0.95f, 0.95f, 0.95f);

                Transform existingBase = controlsBoard.Find("Controls_Board_ConsoleBase");
                if (existingBase != null) existingBase.gameObject.SetActive(false);
            }

            // Right Multi-Screen Wing (Clean backing from Image 4)
            Transform existingRightWing = stationGo.transform.Find("Chamber_Right_MultiScreen_Wing");
            if (existingRightWing != null)
            {
                if (Application.isPlaying) Object.Destroy(existingRightWing.gameObject);
                else Object.DestroyImmediate(existingRightWing.gameObject);
            }

            var rightWingScreenGo = new GameObject("Chamber_Right_MultiScreen_Wing");
            rightWingScreenGo.transform.SetParent(stationGo.transform, false);
            rightWingScreenGo.transform.localPosition = new Vector3(3.85f, 2.30f, 0.65f);
            rightWingScreenGo.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);

            if (bigScreenPrefab != null)
            {
                var bsR = Object.Instantiate(bigScreenPrefab, rightWingScreenGo.transform, false);
                bsR.name = "RightWing_Asset_Frame";
                bsR.transform.localPosition = new Vector3(0f, 0.15f, 0.15f);
                bsR.transform.localRotation = Quaternion.identity;
                bsR.transform.localScale = new Vector3(0.80f, 1.05f, 1.0f);
            }

            // Middle Screen on Right Wing: Sign_Next
            Transform signNext = stationGo.transform.Find("Sign_Next");
            if (signNext != null)
            {
                signNext.localPosition = new Vector3(3.55f, 2.45f, 0.52f);
                signNext.localRotation = Quaternion.Euler(0f, 24f, 0f);
                signNext.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            }

            // Bottom Screen on Right Wing: Achievements_Board
            Transform achieveBoard = stationGo.transform.Find("Achievements_Board");
            if (achieveBoard != null)
            {
                achieveBoard.localPosition = new Vector3(3.55f, -0.60f, 0.52f);
                achieveBoard.localRotation = Quaternion.Euler(0f, 24f, 0f);
                achieveBoard.localScale = new Vector3(0.95f, 0.95f, 0.95f);

                Transform existingBase = achieveBoard.Find("Achievements_Board_ConsoleBase");
                if (existingBase != null) existingBase.gameObject.SetActive(false);
            }

            // Hide unused redundant monitors
            Transform signRole = stationGo.transform.Find("Sign_RoleWith");
            if (signRole != null) signRole.gameObject.SetActive(false);

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

            // 1. High-Bay Central Ambient Fill (Soft, clear ice-white/blue)
            spawnPointLight("Chamber_Center_Fill", new Vector3(35.0f, 5.8f, 23.0f), new Color(0.88f, 0.94f, 1.0f), 2.0f, 22.0f);

            // 2. North Wall Architectural Sconces (Vibrant cyan wash over hexagonal pipes & wall panels)
            spawnPointLight("NorthWall_Accent_A", new Vector3(31.5f, 3.6f, 28.2f), new Color(0.15f, 0.85f, 1.0f), 2.8f, 8.5f);
            spawnPointLight("NorthWall_Accent_B", new Vector3(37.0f, 3.6f, 28.2f), new Color(0.15f, 0.85f, 1.0f), 2.8f, 8.5f);

            // 3. South Wall Architectural Sconces (Warm amber complementary contrast glow)
            spawnPointLight("SouthWall_Accent_A", new Vector3(31.5f, 3.6f, 17.8f), new Color(1.0f, 0.68f, 0.22f), 2.8f, 8.5f);
            spawnPointLight("SouthWall_Accent_B", new Vector3(37.0f, 3.6f, 17.8f), new Color(1.0f, 0.68f, 0.22f), 2.8f, 8.5f);

            // 4. Console Workstation Downlights (Focused task illumination for desks & keyboards)
            spawnSpotLight("TaskLight_North_Fore", new Vector3(33.0f, 4.4f, 28.0f), new Vector3(85f, 0f, 0f), new Color(0.92f, 0.96f, 1.0f), 2.6f, 6.5f, 70f);
            spawnSpotLight("TaskLight_North_Aft", new Vector3(37.2f, 4.4f, 26.5f), new Vector3(85f, 0f, 0f), new Color(0.92f, 0.96f, 1.0f), 2.6f, 6.5f, 70f);
            spawnSpotLight("TaskLight_South_Fore", new Vector3(33.0f, 4.4f, 18.0f), new Vector3(85f, 0f, 0f), new Color(0.92f, 0.96f, 1.0f), 2.6f, 6.5f, 70f);
            spawnSpotLight("TaskLight_South_Aft", new Vector3(37.2f, 4.4f, 19.5f), new Vector3(85f, 0f, 0f), new Color(0.92f, 0.96f, 1.0f), 2.6f, 6.5f, 70f);

            // 5. Entrance Walkway Runner Downlight
            spawnSpotLight("Walkway_Entry_Downlight", new Vector3(28.0f, 4.5f, 23.0f), new Vector3(90f, 0f, 0f), new Color(0.85f, 0.95f, 1.0f), 2.2f, 7.0f, 75f);

            // 6. Video Wall Backing Halo Glow & Key Spotlight on Main Screen
            spawnPointLight("VideoWall_Backing_Halo", new Vector3(40.8f, 3.8f, 23.0f), new Color(0.1f, 0.7f, 1.0f), 2.0f, 7.0f);

            var screenSpotGo = new GameObject("Chamber_Screen_Spotlight");
            screenSpotGo.transform.SetParent(lightGroup.transform, false);
            screenSpotGo.transform.position = new Vector3(37.5f, 5.50f, 23.0f);
            screenSpotGo.transform.rotation = Quaternion.Euler(50f, 90f, 0f);
            var ss = screenSpotGo.AddComponent<Light>();
            ss.type = LightType.Spot;
            ss.range = 10f;
            ss.spotAngle = 65f;
            ss.intensity = 3.2f;
            ss.color = new Color(0.95f, 0.98f, 1.0f);

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

        private static void BuildCommandBridgeHoloTable(
            Transform parent, Vector3 centerPos,
            GameObject projectorPrefab, GameObject compStation, GameObject pilotSeat,
            Material titaniumMat, Material steelMat, Material cyanEmissiveMat, Material coreGlowMat)
        {
            var holoTableRoot = new GameObject("Zone1_CommandBridge_HoloTable");
            holoTableRoot.transform.SetParent(parent, false);
            holoTableRoot.transform.position = centerPos;

            // 1. Real Sci-Fi Hexagonal Command Table Asset (table_2.prefab)
            var table2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/table_2.prefab");
            if (table2Prefab != null)
            {
                var t2 = Object.Instantiate(table2Prefab, holoTableRoot.transform, false);
                t2.name = "Table2_Asset_Base";
                t2.transform.localPosition = Vector3.zero;
            }

            // Projector Base at center
            if (projectorPrefab != null)
            {
                var proj = Object.Instantiate(projectorPrefab, holoTableRoot.transform, false);
                proj.name = "Holo_Projector_Emitter";
                proj.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                proj.transform.localRotation = Quaternion.identity;
                proj.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            }

            // 2. 3D Floating Planetary Hologram Core
            var holoPlanetGo = new GameObject("HoloTable_Planetary_Hologram");
            holoPlanetGo.transform.SetParent(holoTableRoot.transform, false);
            holoPlanetGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);

            // Planet Core Sphere
            var planetCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetCore.name = "Planet_Core";
            planetCore.transform.SetParent(holoPlanetGo.transform, false);
            planetCore.transform.localPosition = Vector3.zero;
            planetCore.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            Object.DestroyImmediate(planetCore.GetComponent<Collider>());
            if (cyanEmissiveMat != null) planetCore.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // Inner Orbit Ring A
            var ringA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringA.name = "Orbit_Ring_Inner";
            ringA.transform.SetParent(holoPlanetGo.transform, false);
            ringA.transform.localPosition = Vector3.zero;
            ringA.transform.localRotation = Quaternion.Euler(22f, 0f, 15f);
            ringA.transform.localScale = new Vector3(1.30f, 0.012f, 1.30f);
            Object.DestroyImmediate(ringA.GetComponent<Collider>());
            if (cyanEmissiveMat != null) ringA.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // Outer Orbit Ring B
            var ringB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringB.name = "Orbit_Ring_Outer";
            ringB.transform.SetParent(holoPlanetGo.transform, false);
            ringB.transform.localPosition = Vector3.zero;
            ringB.transform.localRotation = Quaternion.Euler(-30f, 40f, -10f);
            ringB.transform.localScale = new Vector3(1.85f, 0.010f, 1.85f);
            Object.DestroyImmediate(ringB.GetComponent<Collider>());
            if (coreGlowMat != null) ringB.GetComponent<Renderer>().sharedMaterial = coreGlowMat;
            else if (cyanEmissiveMat != null) ringB.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // Vertical Scan Beam
            var scanBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scanBeam.name = "Laser_Scan_Beam";
            scanBeam.transform.SetParent(holoPlanetGo.transform, false);
            scanBeam.transform.localPosition = Vector3.zero;
            scanBeam.transform.localScale = new Vector3(0.035f, 0.55f, 0.035f);
            Object.DestroyImmediate(scanBeam.GetComponent<Collider>());
            if (cyanEmissiveMat != null) scanBeam.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

            // Hologram Point Light
            var holoLight = holoPlanetGo.AddComponent<Light>();
            holoLight.type = LightType.Point;
            holoLight.range = 3.5f;
            holoLight.intensity = 1.6f;
            holoLight.color = new Color(0.2f, 0.85f, 1.0f);

            // Attach HologramPlanetRotator component
            var rotator = holoPlanetGo.AddComponent<HologramPlanetRotator>();
            rotator.planetCore = planetCore.transform;
            rotator.innerRing = ringA.transform;
            rotator.outerRing = ringB.transform;
            rotator.scanBeam = scanBeam.transform;
            rotator.holoSpotLight = holoLight;
            rotator.planetSpinSpeed = 25f;
            rotator.innerRingSpeed = -34f;
            rotator.outerRingSpeed = 26f;

            // 3. Ergonomic Commander Seats
            if (pilotSeat != null)
            {
                var seatLeft = Object.Instantiate(pilotSeat, holoTableRoot.transform, false);
                seatLeft.name = "Commander_Seat_South";
                seatLeft.transform.localPosition = new Vector3(-1.6f, 0f, -1.8f);
                seatLeft.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);

                var seatRight = Object.Instantiate(pilotSeat, holoTableRoot.transform, false);
                seatRight.name = "Commander_Seat_North";
                seatRight.transform.localPosition = new Vector3(-1.6f, 0f, 1.8f);
                seatRight.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
            }

            // 4. Tactical Status Lecterns
            if (compStation != null)
            {
                var lecternSouth = Object.Instantiate(compStation, holoTableRoot.transform, false);
                lecternSouth.name = "Holo_Tactical_Console_South";
                lecternSouth.transform.localPosition = new Vector3(0f, 0f, -2.4f);
                lecternSouth.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

                var lecternNorth = Object.Instantiate(compStation, holoTableRoot.transform, false);
                lecternNorth.name = "Holo_Tactical_Console_North";
                lecternNorth.transform.localPosition = new Vector3(0f, 0f, 2.4f);
                lecternNorth.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Overhead Cyan Spotlight pool
            var spotGo = new GameObject("HoloTable_Overhead_Spotlight");
            spotGo.transform.SetParent(holoTableRoot.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 6.0f, 0f);
            spotGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 8f;
            spot.spotAngle = 45f;
            spot.intensity = 2.8f;
            spot.color = new Color(0.2f, 0.85f, 1.0f);
        }

        private static void BuildServerAndTerminalBays(
            Transform parent, GameObject cabinetL, GameObject consolePrefab, GameObject consoleScreenPrefab,
            GameObject consoleCeiling, GameObject pilotSeat, GameObject cableThick, Material cyanEmissiveMat, Material coreGlowMat)
        {
            var serverBayRoot = new GameObject("Zone2_Server_And_Terminal_Bays");
            serverBayRoot.transform.SetParent(parent, false);

            // 1. South Wall Server Racks (Z = 9.8m)
            float[] rackXPositions = new float[] { 6.5f, 10.0f, 13.5f, 17.0f, 20.5f };
            for (int i = 0; i < rackXPositions.Length; i++)
            {
                float x = rackXPositions[i];
                var rackContainer = new GameObject($"Server_Rack_Bay_{i + 1}");
                rackContainer.transform.SetParent(serverBayRoot.transform, false);
                rackContainer.transform.position = new Vector3(x, 0.10f, 9.8f);
                rackContainer.transform.rotation = Quaternion.identity;

                if (cabinetL != null)
                {
                    var model = Object.Instantiate(cabinetL, rackContainer.transform, false);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                }

                // Procedural Blinking LED indicators on rack front panel
                List<Renderer> ledRends = new List<Renderer>();
                float[] ledY = new float[] { 0.7f, 1.1f, 1.5f, 1.9f };
                for (int j = 0; j < ledY.Length; j++)
                {
                    var led = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    led.name = $"LED_Status_{j}";
                    led.transform.SetParent(rackContainer.transform, false);
                    led.transform.localPosition = new Vector3(0f, ledY[j], 0.38f);
                    led.transform.localScale = new Vector3(0.55f, 0.05f, 0.04f);
                    Object.DestroyImmediate(led.GetComponent<Collider>());
                    var rend = led.GetComponent<Renderer>();
                    if (cyanEmissiveMat != null) rend.sharedMaterial = cyanEmissiveMat;
                    ledRends.Add(rend);
                }

                // Add ServerBlinkingLights script
                var bl = rackContainer.AddComponent<ServerBlinkingLights>();
                bl.ledRenderers = ledRends.ToArray();
                bl.minBlinkInterval = 0.06f;
                bl.maxBlinkInterval = 0.22f;
            }

            // 2. Curved Consoles & Operator Chairs (Z ≈ 13.0m)
            Vector3[] consolePos = new Vector3[]
            {
                new Vector3(10.5f, 0.10f, 13.3f),
                new Vector3(13.5f, 0.10f, 13.0f),
                new Vector3(16.5f, 0.10f, 13.3f)
            };
            float[] consoleAngles = new float[] { 18f, 0f, -18f };

            for (int i = 0; i < consolePos.Length; i++)
            {
                if (consolePrefab != null)
                {
                    var c = Object.Instantiate(consolePrefab, serverBayRoot.transform, false);
                    c.name = $"Terminal_Console_{i + 1}";
                    c.transform.position = consolePos[i];
                    c.transform.rotation = Quaternion.Euler(0f, consoleAngles[i], 0f);
                }
                if (consoleScreenPrefab != null)
                {
                    var cs = Object.Instantiate(consoleScreenPrefab, serverBayRoot.transform, false);
                    cs.name = $"Terminal_Screen_{i + 1}";
                    cs.transform.position = consolePos[i] + new Vector3(0f, 0.85f, 0.15f);
                    cs.transform.rotation = Quaternion.Euler(0f, consoleAngles[i], 0f);
                }
            }

            // Operator chairs facing the terminals (rotated 180)
            if (pilotSeat != null)
            {
                var chairL = Object.Instantiate(pilotSeat, serverBayRoot.transform, false);
                chairL.name = "Server_Operator_Chair_L";
                chairL.transform.position = new Vector3(11.5f, 0.10f, 14.5f);
                chairL.transform.rotation = Quaternion.Euler(0f, 195f, 0f);

                var chairR = Object.Instantiate(pilotSeat, serverBayRoot.transform, false);
                chairR.name = "Server_Operator_Chair_R";
                chairR.transform.position = new Vector3(15.5f, 0.10f, 14.5f);
                chairR.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
            }

            // 3. Suspended Overhead Terminal
            if (consoleCeiling != null)
            {
                var cCeil = Object.Instantiate(consoleCeiling, serverBayRoot.transform, false);
                cCeil.name = "ServerBay_Overhead_Terminal";
                cCeil.transform.position = new Vector3(13.5f, 3.8f, 12.0f);
                cCeil.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // 4. Heavy Baseboard Power Cables
            if (cableThick != null)
            {
                for (float x = 7.0f; x <= 20.0f; x += 4.0f)
                {
                    var cb = Object.Instantiate(cableThick, serverBayRoot.transform, false);
                    cb.name = $"Server_Cable_Trunk_X{x}";
                    cb.transform.position = new Vector3(x, 0.10f, 9.4f);
                    cb.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }

            // 5. Warm Amber Accent Wash Light
            var amberLightGo = new GameObject("ServerBay_Amber_Accent_Light");
            amberLightGo.transform.SetParent(serverBayRoot.transform, false);
            amberLightGo.transform.position = new Vector3(13.5f, 3.2f, 10.5f);
            var ambLight = amberLightGo.AddComponent<Light>();
            ambLight.type = LightType.Point;
            ambLight.range = 10f;
            ambLight.intensity = 2.2f;
            ambLight.color = new Color(1.0f, 0.60f, 0.15f);
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

            // 1. Floor Hazard Warning Decal Stripes at Door Thresholds
            if (hazardStripeMat != null)
            {
                // Inner sliding blast door threshold (X = -2.2m, across Z: 21 to 25)
                var stripeInner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripeInner.name = "Hazard_Stripe_Inner_Threshold";
                stripeInner.transform.SetParent(cargoRoot.transform, false);
                stripeInner.transform.position = new Vector3(-2.2f, 0.11f, 23.0f);
                stripeInner.transform.localScale = new Vector3(0.75f, 0.015f, 4.4f);
                Object.DestroyImmediate(stripeInner.GetComponent<Collider>());
                stripeInner.GetComponent<Renderer>().sharedMaterial = hazardStripeMat;

                // Outer airlock threshold (X = -9.2m, across Z: 21 to 25)
                var stripeOuter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripeOuter.name = "Hazard_Stripe_Outer_Threshold";
                stripeOuter.transform.SetParent(cargoRoot.transform, false);
                stripeOuter.transform.position = new Vector3(-9.2f, 0.11f, 23.0f);
                stripeOuter.transform.localScale = new Vector3(0.75f, 0.015f, 4.4f);
                Object.DestroyImmediate(stripeOuter.GetComponent<Collider>());
                stripeOuter.GetComponent<Renderer>().sharedMaterial = hazardStripeMat;
            }

            // 2. Biometric Security / Retina Scanner Wall Terminals flanking door frame
            if (compStation != null)
            {
                var scanSouth = Object.Instantiate(compStation, cargoRoot.transform, false);
                scanSouth.name = "Biometric_Door_Terminal_South";
                scanSouth.transform.position = new Vector3(-2.2f, 0.10f, 20.8f);
                scanSouth.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                scanSouth.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);

                var scanNorth = Object.Instantiate(compStation, cargoRoot.transform, false);
                scanNorth.name = "Biometric_Door_Terminal_North";
                scanNorth.transform.position = new Vector3(-2.2f, 0.10f, 25.2f);
                scanNorth.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                scanNorth.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);
            }

            // 3. South Cargo Staging Bay (X: -7.5 to -3.5, Z: 18.2 to 20.5)
            if (containerBig != null)
            {
                var cb = Object.Instantiate(containerBig, cargoRoot.transform, false);
                cb.name = "Cargo_Heavy_Shipping_Container";
                cb.transform.position = new Vector3(-5.2f, 0.10f, 19.5f);
                cb.transform.rotation = Quaternion.Euler(0f, 5f, 0f);
            }
            if (containerSmall != null)
            {
                var cs1 = Object.Instantiate(containerSmall, cargoRoot.transform, false);
                cs1.name = "Cargo_Stacked_Small_Crate";
                cs1.transform.position = new Vector3(-5.0f, 1.45f, 19.6f);
                cs1.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            }
            if (storageContainerSmall != null)
            {
                var scs = Object.Instantiate(storageContainerSmall, cargoRoot.transform, false);
                scs.name = "Cargo_Modular_Storage_Unit";
                scs.transform.position = new Vector3(-7.2f, 0.10f, 19.2f);
                scs.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
            }
            if (barrelsCluster != null)
            {
                var bc = Object.Instantiate(barrelsCluster, cargoRoot.transform, false);
                bc.name = "Cargo_Coolant_Barrels";
                bc.transform.position = new Vector3(-3.2f, 0.10f, 19.5f);
            }

            // 4. North Equipment & EVA Bay (X: -7.5 to -3.5, Z: 25.5 to 27.8)
            if (torpedoRack != null)
            {
                var tr = Object.Instantiate(torpedoRack, cargoRoot.transform, false);
                tr.name = "EVA_Equipment_Rack";
                tr.transform.position = new Vector3(-5.5f, 0.10f, 27.2f);
                tr.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (wallShelf != null)
            {
                var ws = Object.Instantiate(wallShelf, cargoRoot.transform, false);
                ws.name = "Airlock_Wall_Gear_Shelf";
                ws.transform.position = new Vector3(-3.4f, 1.20f, 27.4f);
                ws.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Automated Hover-Lifter Pallet
            var hoverGo = new GameObject("Automated_Hover_Lifter_Pallet");
            hoverGo.transform.SetParent(cargoRoot.transform, false);
            hoverGo.transform.position = new Vector3(-4.5f, 0.22f, 25.5f);

            var palletDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palletDeck.name = "Pallet_Platform";
            palletDeck.transform.SetParent(hoverGo.transform, false);
            palletDeck.transform.localPosition = Vector3.zero;
            palletDeck.transform.localScale = new Vector3(1.8f, 0.10f, 1.4f);
            Object.DestroyImmediate(palletDeck.GetComponent<Collider>());
            if (darkTitaniumMat != null) palletDeck.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

            // 4 Cyan anti-gravity repulsor pads underneath
            Vector3[] padOffsets = new Vector3[]
            {
                new Vector3(-0.7f, -0.06f, -0.5f),
                new Vector3(0.7f, -0.06f, -0.5f),
                new Vector3(-0.7f, -0.06f, 0.5f),
                new Vector3(0.7f, -0.06f, 0.5f)
            };
            foreach (var off in padOffsets)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "AntiGrav_Thruster_Pad";
                pad.transform.SetParent(hoverGo.transform, false);
                pad.transform.localPosition = off;
                pad.transform.localScale = new Vector3(0.30f, 0.04f, 0.30f);
                Object.DestroyImmediate(pad.GetComponent<Collider>());
                if (cyanEmissiveMat != null) pad.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;
            }

            // Loaded cargo on hover pallet
            if (storageContainerSmall != null)
            {
                var hc = Object.Instantiate(storageContainerSmall, hoverGo.transform, false);
                hc.name = "Hover_Loaded_Alloy_Crate";
                hc.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                hc.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            }

            // Warm Task Spotlights for Cargo Staging
            var cargoLightGo = new GameObject("Airlock_Cargo_Floodlight");
            cargoLightGo.transform.SetParent(cargoRoot.transform, false);
            cargoLightGo.transform.position = new Vector3(-5.0f, 3.6f, 23.0f);
            var cl = cargoLightGo.AddComponent<Light>();
            cl.type = LightType.Spot;
            cl.range = 8f;
            cl.spotAngle = 75f;
            cl.intensity = 2.4f;
            cl.color = new Color(1.0f, 0.95f, 0.88f);
            cargoLightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
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

                // 3. Portico Airlock Ceiling Ducts at Y = 3.8m
                for (float z = 20.0f; z <= 26.0f; z += 3.0f)
                {
                    var p = Object.Instantiate(pipeStraight, conduitRoot.transform, false);
                    p.name = $"Airlock_Ceiling_Duct_Z{z}";
                    p.transform.position = new Vector3(-6.0f, 3.80f, z);
                    p.transform.rotation = Quaternion.identity;
                    p.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
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

            // 4. Floor Baseboard Perimeter Ornament Pipes
            if (floorCornerPipes != null)
            {
                for (float x = 8.0f; x <= 20.0f; x += 4.0f)
                {
                    var fcp = Object.Instantiate(floorCornerPipes, conduitRoot.transform, false);
                    fcp.name = $"Baseboard_Pipes_South_X{x}";
                    fcp.transform.position = new Vector3(x, 0.10f, 9.15f);
                    fcp.transform.rotation = Quaternion.identity;
                }
            }
        }

        private static void BuildInteriorAtmosphereAndLighting(
            Transform parent, GameObject ceilingLight, Material cyanEmissiveMat, Material smokeMat)
        {
            var lightingGroup = new GameObject("Citadel_Interior_Lighting");
            lightingGroup.transform.SetParent(parent, false);

            // 1. Balanced Ambient Fill Light (Contrast Lighting: reduced from 2.2f to 1.15f)
            var ambientFillGo = new GameObject("Interior_Ambient_Fill_Light");
            ambientFillGo.transform.SetParent(lightingGroup.transform, false);
            ambientFillGo.transform.position = new Vector3(14.0f, 5.0f, 23.0f);
            var fillLight = ambientFillGo.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 38.0f;
            fillLight.intensity = 1.15f; // Gives shadows depth while keeping base clear
            fillLight.color = new Color(0.85f, 0.95f, 1.0f);

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

            // 3. High-Bay Overhead Spotlights
            Vector3[] floodlightPos = new Vector3[]
            {
                new Vector3(14.0f, 11.2f, 23.0f),
                new Vector3(8.0f, 7.6f, 19.0f),
                new Vector3(20.0f, 7.6f, 19.0f),
                new Vector3(8.0f, 7.6f, 27.0f),
                new Vector3(20.0f, 7.6f, 27.0f),
                new Vector3(-6.0f, 3.8f, 23.0f),
                new Vector3(14.0f, 3.8f, 43.0f)
            };

            foreach (var pos in floodlightPos)
            {
                var lightGo = new GameObject("Ceiling_HighBay_Light");
                lightGo.transform.SetParent(lightingGroup.transform, false);
                lightGo.transform.position = pos;

                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 18f;
                l.spotAngle = 80f;
                l.intensity = 2.8f;
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

        private static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
