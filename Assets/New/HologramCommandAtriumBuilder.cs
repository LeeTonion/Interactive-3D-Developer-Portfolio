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

            // Clean legacy Walkways (eliminating old conflicting tiles & knee buttresses)
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
            }

            // ── 6.5 TRUE MATHEMATICAL CORNER COLUMNS (SEALS ALL 16 VERTICAL CORNERS!) ──
            Vector3[] perimeterCorners = new Vector3[]
            {
                new Vector3(-10.0f, 0.10f, 17.0f), // West Gate South
                new Vector3(-10.0f, 0.10f, 29.0f), // West Gate North
                new Vector3(-2.0f, 0.10f, 17.0f),  // Portico meets Rotunda West South
                new Vector3(-2.0f, 0.10f, 29.0f),  // Portico meets Rotunda West North
                new Vector3(-2.0f, 0.10f, 9.0f),   // Rotunda South-West
                new Vector3(30.0f, 0.10f, 9.0f),   // Rotunda South-East
                new Vector3(30.0f, 0.10f, 17.0f),  // Rotunda meets East Wing South
                new Vector3(42.0f, 0.10f, 17.0f),  // East Wing South-East
                new Vector3(42.0f, 0.10f, 29.0f),  // East Wing North-East
                new Vector3(30.0f, 0.10f, 29.0f),  // East Wing North meets Rotunda
                new Vector3(30.0f, 0.10f, 37.0f),  // Rotunda North-East
                new Vector3(26.0f, 0.10f, 37.0f),  // North Hall meets North Wing East
                new Vector3(26.0f, 0.10f, 49.0f),  // North Wing North-East
                new Vector3(2.0f, 0.10f, 49.0f),   // North Wing North-West
                new Vector3(2.0f, 0.10f, 37.0f),   // North Hall meets North Wing West
                new Vector3(-2.0f, 0.10f, 37.0f)   // Rotunda North-West
            };

            foreach (var cp in perimeterCorners)
            {
                // Solid Titanium Structural Core (Seals both inside and outside 100%)
                var col = GameObject.CreatePrimitive(PrimitiveType.Cube);
                col.name = $"Corner_Seal_Pillar_{cp.x}_{cp.z}";
                col.transform.SetParent(wallsGroup.transform, false);
                col.transform.position = cp + Vector3.up * 4.10f;
                col.transform.localScale = new Vector3(1.20f, 8.20f, 1.20f);
                Object.DestroyImmediate(col.GetComponent<Collider>());
                if (darkTitaniumMat != null) col.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;

                // Vertical Glowing Neon Energy Conduit Accent
                var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rod.name = "Corner_Neon_Conduit";
                rod.transform.SetParent(col.transform, false);
                rod.transform.localPosition = Vector3.zero;
                rod.transform.localScale = new Vector3(0.18f, 1.0f, 0.18f);
                Object.DestroyImmediate(rod.GetComponent<Collider>());
                if (cyanEmissiveMat != null) rod.GetComponent<Renderer>().sharedMaterial = cyanEmissiveMat;

                // Architectural Modular Finish at Tier 0 and Tier 1
                if (columnPillar != null)
                {
                    var colT0 = Object.Instantiate(columnPillar, wallsGroup.transform, false);
                    colT0.name = $"Column_Finish_{cp.x}_{cp.z}_T0";
                    colT0.transform.position = cp;

                    var colT1 = Object.Instantiate(columnPillar, wallsGroup.transform, false);
                    colT1.name = $"Column_Finish_{cp.x}_{cp.z}_T1";
                    colT1.transform.position = cp + Vector3.up * 4.0f;
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
            // 4 Crown Tower Corner Seal Columns
            Vector3[] crownCorners = new Vector3[]
            {
                new Vector3(4f, roofY, 13f),
                new Vector3(24f, roofY, 13f),
                new Vector3(24f, roofY, 33f),
                new Vector3(4f, roofY, 33f)
            };
            foreach (var cc in crownCorners)
            {
                var cCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cCol.name = $"Crown_Corner_{cc.x}_{cc.z}";
                cCol.transform.SetParent(crownTower.transform, false);
                cCol.transform.position = cc + Vector3.up * 2.0f;
                cCol.transform.localScale = new Vector3(1.10f, 4.10f, 1.10f);
                Object.DestroyImmediate(cCol.GetComponent<Collider>());
                if (darkTitaniumMat != null) cCol.GetComponent<Renderer>().sharedMaterial = darkTitaniumMat;
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

            // ─────────────────────────────────────────────────────────────────
            // 9. INTERIOR COMMAND SET DRESSING (Grand Open Rotunda - Zero Corridors!)
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

            UpgradeStationBridge(stationGo, projectorPrefab, consolePrefab, compStation, cabinetL, pilotSeat, darkTitaniumMat, steelAccentMat);

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

            // EAST WING: CREW LOUNGE & STARFIELD DECK
            if (sofa != null)
            {
                spawnProp(sofa, propsGroup.transform, new Vector3(36.0f, 0.10f, 26.0f), Quaternion.Euler(0f, 180f, 0f));
                spawnProp(sofa, propsGroup.transform, new Vector3(36.0f, 0.10f, 20.0f), Quaternion.identity);
            }
            if (tableGlass != null) spawnProp(tableGlass, propsGroup.transform, new Vector3(36.0f, 0.10f, 23.0f), Quaternion.identity);
            if (chair != null)
            {
                spawnProp(chair, propsGroup.transform, new Vector3(33.5f, 0.10f, 23.0f), Quaternion.Euler(0f, 90f, 0f));
                spawnProp(chair, propsGroup.transform, new Vector3(38.5f, 0.10f, 23.0f), Quaternion.Euler(0f, -90f, 0f));
            }

            // OPEN MONUMENTAL SCI-FI PORTAL ARCHES (Replaces solid blocking corridors!)
            if (columnPillar != null)
            {
                // North Wing Entrance Portal
                var nP1 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                nP1.transform.position = new Vector3(10.0f, 0.10f, 36.5f);
                var nP2 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                nP2.transform.position = new Vector3(18.0f, 0.10f, 36.5f);

                // East Wing Entrance Portal
                var eP1 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                eP1.transform.position = new Vector3(29.5f, 0.10f, 19.5f);
                var eP2 = Object.Instantiate(columnPillar, propsGroup.transform, false);
                eP2.transform.position = new Vector3(29.5f, 0.10f, 26.5f);
            }

            // ─────────────────────────────────────────────────────────────────
            // 10. INTERIOR AMBIENT & HIGH-BAY ILLUMINATION (Eliminates Pitch-Black Shadows!)
            // ─────────────────────────────────────────────────────────────────
            GameObject lightingGroup = new GameObject("Citadel_Interior_Lighting");
            lightingGroup.transform.SetParent(mainBuilding, false);

            // Broad Ambient Fill Light (Eliminates space-darkness inside the command deck)
            var ambientFillGo = new GameObject("Interior_Ambient_Fill_Light");
            ambientFillGo.transform.SetParent(lightingGroup.transform, false);
            ambientFillGo.transform.position = new Vector3(14.0f, 5.0f, 23.0f);
            var fillLight = ambientFillGo.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 35.0f;
            fillLight.intensity = 2.2f;
            fillLight.color = new Color(0.85f, 0.95f, 1.0f);

            // Downward High-Bay Spotlights
            Vector3[] floodlightPos = new Vector3[]
            {
                new Vector3(14.0f, 11.2f, 23.0f),
                new Vector3(8.0f, 7.6f, 19.0f),
                new Vector3(20.0f, 7.6f, 19.0f),
                new Vector3(8.0f, 7.6f, 27.0f),
                new Vector3(20.0f, 7.6f, 27.0f),
                new Vector3(-6.0f, 3.8f, 23.0f),
                new Vector3(14.0f, 3.8f, 43.0f),
                new Vector3(36.0f, 3.8f, 23.0f)
            };

            foreach (var pos in floodlightPos)
            {
                var lightGo = new GameObject("Ceiling_HighBay_Light");
                lightGo.transform.SetParent(lightingGroup.transform, false);
                lightGo.transform.position = pos;

                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 18f;
                l.spotAngle = 85f;
                l.intensity = 3.5f;
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

        private static void UpgradeStationBridge(GameObject stationGo, GameObject projectorPrefab, GameObject consolePrefab,
            GameObject compStation, GameObject cabinetL, GameObject pilotSeat, Material titaniumMat, Material steelMat)
        {
            if (stationGo == null) return;

            stationGo.transform.position = new Vector3(14.00f, 0.19f, 23.00f);
            stationGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Transform centerDesk = stationGo.transform.Find("Center_Desk");
            if (centerDesk != null)
            {
                Transform glassTable = centerDesk.Find("decorative_table_glass");
                if (glassTable != null)
                {
                    if (Application.isPlaying) Object.Destroy(glassTable.gameObject);
                    else Object.DestroyImmediate(glassTable.gameObject);
                }
                Transform comp15 = centerDesk.Find("Cosmic_Retro_Computer_15");
                if (comp15 != null)
                {
                    if (Application.isPlaying) Object.Destroy(comp15.gameObject);
                    else Object.DestroyImmediate(comp15.gameObject);
                }
                Transform comp14 = centerDesk.Find("Cosmic_Retro_Computer_14");
                if (comp14 != null)
                {
                    if (Application.isPlaying) Object.Destroy(comp14.gameObject);
                    else Object.DestroyImmediate(comp14.gameObject);
                }

                Transform existingHoloConsole = centerDesk.Find("Tactical_Holo_Podium");
                if (existingHoloConsole == null)
                {
                    var podiumGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    podiumGo.name = "Tactical_Holo_Podium";
                    podiumGo.transform.SetParent(centerDesk, false);
                    podiumGo.transform.localPosition = new Vector3(0.08f, 0.45f, -0.18f);
                    podiumGo.transform.localScale = new Vector3(2.2f, 0.45f, 2.2f);
                    Object.DestroyImmediate(podiumGo.GetComponent<Collider>());
                    if (steelMat != null) podiumGo.GetComponent<Renderer>().sharedMaterial = steelMat;

                    if (consolePrefab != null)
                    {
                        var cL = Object.Instantiate(consolePrefab, centerDesk, false);
                        cL.name = "Bridge_Tactical_Console_Left";
                        cL.transform.localPosition = new Vector3(-1.4f, 0f, 0f);
                        cL.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
                        cL.transform.localScale = new Vector3(1.2f, 1.0f, 1.2f);

                        var cR = Object.Instantiate(consolePrefab, centerDesk, false);
                        cR.name = "Bridge_Tactical_Console_Right";
                        cR.transform.localPosition = new Vector3(1.5f, 0f, 0f);
                        cR.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
                        cR.transform.localScale = new Vector3(1.2f, 1.0f, 1.2f);
                    }
                }
            }

            Transform mainScreen = stationGo.transform.Find("MainScreen");
            if (mainScreen != null)
            {
                Transform existingArch = mainScreen.Find("Command_Viewscreen_Arch");
                if (existingArch != null)
                {
                    if (Application.isPlaying) Object.Destroy(existingArch.gameObject);
                    else Object.DestroyImmediate(existingArch.gameObject);
                }

                var archGo = new GameObject("Command_Viewscreen_Arch");
                archGo.transform.SetParent(mainScreen, false);
                archGo.transform.localPosition = new Vector3(0f, 0f, 0.35f);

                if (cabinetL != null)
                {
                    var sL = Object.Instantiate(cabinetL, archGo.transform, false);
                    sL.name = "Viewscreen_Rear_Server_L";
                    sL.transform.localPosition = new Vector3(-2.2f, 0f, 0.65f);
                    sL.transform.localRotation = Quaternion.identity;

                    var sR = Object.Instantiate(cabinetL, archGo.transform, false);
                    sR.name = "Viewscreen_Rear_Server_R";
                    sR.transform.localPosition = new Vector3(2.2f, 0f, 0.65f);
                    sR.transform.localRotation = Quaternion.identity;
                }

                if (compStation != null)
                {
                    var diag = Object.Instantiate(compStation, archGo.transform, false);
                    diag.name = "Viewscreen_Rear_Diagnostic_Console";
                    diag.transform.localPosition = new Vector3(0f, 0f, 0.85f);
                    diag.transform.localRotation = Quaternion.identity;
                }

                if (pilotSeat != null)
                {
                    var seat = Object.Instantiate(pilotSeat, archGo.transform, false);
                    seat.name = "Viewscreen_Rear_Operator_Seat";
                    seat.transform.localPosition = new Vector3(0f, 0f, 1.85f);
                    seat.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            Transform camView = stationGo.transform.Find("Camera_ViewPoint");
            if (camView != null) camView.localPosition = new Vector3(0f, 4.20f, -12.50f);

            Transform camLook = stationGo.transform.Find("Camera_LookTarget");
            if (camLook != null) camLook.localPosition = new Vector3(0f, 2.70f, 0.80f);

            var infoComp = stationGo.GetComponent<AboutInfoStation>();
            if (infoComp != null)
            {
                infoComp.viewFov = 64f;
                if (camView != null) infoComp.cameraViewPoint = camView;
                if (camLook != null) infoComp.cameraLookTarget = camLook;

                var lp = GameObject.Find("LandingPad_Center");
                if (lp != null) infoComp.landingPadCenter = lp.transform;

                var beaconsGo = GameObject.Find("LandingLights");
                if (beaconsGo != null) infoComp.landingBeacons = beaconsGo.GetComponentsInChildren<Renderer>();
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
