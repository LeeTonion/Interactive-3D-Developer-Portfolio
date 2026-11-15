using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Expands the surrounding Project_Zone space into a cohesive 4-layer Asteroid Colony
    /// without modifying or rebuilding any existing Project_Zone structures or Asteroid_Planetary_Surface.
    ///
    /// Layers:
    ///   - Layer 1 & 2 transitions: Supporting space infrastructure (mining platforms, energy relays,
    ///     observation towers, cargo/docking platforms, research outpost, maintenance station)
    ///   - Layer 3: Large asteroids (natural, mineral-rich, facility-bearing), intentional rock clusters,
    ///     glowing crystal fragments, metallic debris & abandoned spacecraft components, subtle space dust
    ///   - Layer 4: Distant deep-space asteroids & satellites with LODGroup culling (no colliders)
    ///   - Ambient Activities: Reuses GemstoneCargoShuttle, MiningExtractionLaser, HologramPlanetRotator, Enemy_2Legs
    /// </summary>
    public static class AsteroidColonyExpander
    {
        private const string ExpansionRootName = "Asteroid_Colony_Expansion";
        private static readonly Dictionary<string, GameObject> AssetCache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        [MenuItem("Tools/Project Zone/Expand Cohesive Asteroid Colony")]
        public static GameObject ExpandColony()
        {
            AssetCache.Clear();
            var pz = GameObject.Find("Project_Zone");
            if (pz == null)
            {
                Debug.LogError("[AsteroidColonyExpander] Project_Zone not found.");
                return null;
            }

            var prev = pz.transform.Find(ExpansionRootName);
            if (prev != null) Undo.DestroyObjectImmediate(prev.gameObject);

            var root = new GameObject(ExpansionRootName);
            root.transform.SetParent(pz.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Expand Asteroid Colony");

            // Load & enable GPU instancing on shared environment materials
            var matRockDark = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Asteroid_RockDark.mat");
            var matSlate = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Asteroid_Slate.mat");
            var matSteel = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_SciFi_SteelAccent.mat");
            var matGunmetal = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_SciFi_Gunmetal.mat");
            var matDarkTitanium = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_SciFi_DarkTitanium.mat");
            var matFloorGray = LoadMat("Assets/_Project/Materials/City/Floor_gray(Demo).mat");
            var matCyanNeon = LoadMat("Assets/_Project/Materials/BadgesAndVFX/Mat_Badge_CyanNeon.mat");
            var matCrystalCyan = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_BlueCyan.mat");
            var matCrystalPurple = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_PurpleRare.mat");
            var matCrystalMagenta = LoadMat("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_MagentaRare.mat");

            var infraGroup = CreateChild(root.transform, "01_Layer2_Space_Infrastructure");
            var beltGroup = CreateChild(root.transform, "02_Layer3_Asteroid_Belt");
            var decorGroup = CreateChild(root.transform, "03_Layer3_Decorations_And_Debris");
            var deepSpaceGroup = CreateChild(root.transform, "04_Layer4_DeepSpace_Background");
            var activityGroup = CreateChild(root.transform, "05_Ambient_Activities");

            BuildLargeAsteroidsAndClusters(beltGroup.transform, matRockDark, matSlate, matCrystalCyan, matCrystalPurple, matCrystalMagenta);
            BuildSpaceInfrastructure(infraGroup.transform, activityGroup.transform, matRockDark, matSlate, matFloorGray, matSteel, matGunmetal, matDarkTitanium, matCyanNeon, matCrystalCyan);
            BuildDecorationsAndDebris(decorGroup.transform, matRockDark, matSlate, matSteel, matGunmetal, matCrystalCyan, matCrystalPurple, matCrystalMagenta);
            BuildDeepSpaceLayer(deepSpaceGroup.transform, matRockDark, matSlate, matSteel, matCyanNeon, matCrystalCyan, matCrystalPurple);
            BuildAmbientActivities(activityGroup.transform, matSteel, matGunmetal, matCyanNeon);
            AuditAndEquipExistingFloatingRocks(pz.transform, matRockDark, matCrystalCyan, matCrystalPurple, matCrystalMagenta);

            EditorUtility.SetDirty(pz);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pz.scene);
            Debug.Log("[AsteroidColonyExpander] Cohesive 4-layer asteroid colony expansion complete.");
            return root;
        }

        // =========================================================================================
        // LAYER 3: LARGE ASTEROIDS + INTENTIONAL CLUSTERS (NATURAL BOUNDARY, CLEAR FLIGHT ROUTES)
        // =========================================================================================
        private static void BuildLargeAsteroidsAndClusters(
            Transform beltRoot,
            Material matRockDark, Material matSlate,
            Material matCrystalCyan, Material matCrystalPurple, Material matCrystalMagenta)
        {
            var largeRoot = CreateChild(beltRoot, "Large_Asteroids");
            var clusterRoot = CreateChild(beltRoot, "Asteroid_Clusters");

            // 14 Large Asteroids forming an organic, broken ring around the colony (Z=23 West approach stays open)
            // Kind: 0 = natural, 1 = mineral deposit, 2 = facility anchor
            var largeAsteroids = new (string name, string model, Vector3 pos, Vector3 rot, Vector3 scale, int kind, bool purpleCrystal)[]
            {
                ("LargeAst_NW_Sentinel", "rock_largeA", new Vector3(-76f, 16f, 82f), new Vector3(12f, 35f, -8f), new Vector3(28f, 22f, 26f), 1, false),
                ("LargeAst_North_Ridge", "rock_largeB", new Vector3(-8f, 4f, 96f), new Vector3(-8f, 110f, 14f), new Vector3(32f, 24f, 28f), 2, false),
                ("LargeAst_NE_Monolith", "meteor_half", new Vector3(58f, 12f, 82f), new Vector3(18f, -45f, 165f), new Vector3(34f, 26f, 30f), 1, true),
                ("LargeAst_East_Bastion", "rock_largeA", new Vector3(108f, -4f, 36f), new Vector3(-6f, 70f, 10f), new Vector3(30f, 22f, 28f), 0, false),
                ("LargeAst_East_DockAnchor", "rock_largeB", new Vector3(92f, -12f, 22f), new Vector3(4f, -20f, -6f), new Vector3(24f, 18f, 22f), 2, false),
                ("LargeAst_SE_RefineryPeak", "craterLarge", new Vector3(96f, -18f, -34f), new Vector3(10f, 145f, -12f), new Vector3(28f, 20f, 26f), 1, true),
                ("LargeAst_SE_MiningGiant", "rock_largeA", new Vector3(42f, -12f, -54f), new Vector3(-5f, 30f, 7f), new Vector3(24f, 18f, 22f), 2, false),
                ("LargeAst_South_Vault", "rock_largeB", new Vector3(-8f, -16f, -68f), new Vector3(14f, -75f, 9f), new Vector3(32f, 24f, 28f), 1, false),
                ("LargeAst_SW_Crater", "craterLarge", new Vector3(-56f, -12f, -44f), new Vector3(-9f, 50f, -11f), new Vector3(28f, 20f, 26f), 0, false),
                ("LargeAst_SW_DockAnchor", "rock_largeA", new Vector3(-50f, -9f, -6f), new Vector3(5f, 15f, -4f), new Vector3(22f, 16f, 20f), 2, false),
                ("LargeAst_West_MiningAnchor", "rock_largeB", new Vector3(-66f, -4f, 26f), new Vector3(-4f, 88f, 6f), new Vector3(20f, 16f, 20f), 2, false),
                ("LargeAst_West_HighSpire", "meteor_detailed", new Vector3(-98f, 22f, 12f), new Vector3(16f, -115f, 12f), new Vector3(22f, 24f, 20f), 0, false),
                ("LargeAst_FarNorth_Natural", "rock_largeA", new Vector3(26f, 20f, 108f), new Vector3(-14f, 25f, -10f), new Vector3(26f, 20f, 24f), 0, false),
                ("LargeAst_SouthEast_Mineral", "rock_largeB", new Vector3(82f, -6f, -74f), new Vector3(8f, 60f, -12f), new Vector3(26f, 20f, 24f), 1, true),
            };

            for (int i = 0; i < largeAsteroids.Length; i++)
            {
                var a = largeAsteroids[i];
                var m1 = (i % 2 == 0) ? matRockDark : matSlate;
                var m2 = (i % 2 == 0) ? matSlate : matRockDark;
                var go = SpawnAsset(largeRoot.transform, a.name, a.model, a.pos, a.rot, a.scale, m1, m2, true);
                if (go != null && a.kind == 1)
                {
                    var cMat = a.purpleCrystal ? (i % 3 == 0 ? matCrystalMagenta : matCrystalPurple) : matCrystalCyan;
                    SpawnAsset(largeRoot.transform, $"{a.name}_Crystals", (i % 2 == 0) ? "rock_crystalsLargeA" : "rock_crystalsLargeB",
                        a.pos + new Vector3(0f, a.scale.y * 0.38f, 0f), new Vector3(6f, i * 47f, -4f),
                        Vector3.one * 4.5f, matRockDark, cMat, false);
                }
            }

            // 8 Intentional Asteroid Clusters (anchor rock + 4 varied satellite boulders, NO even spacing)
            var clusterSpecs = new (string name, Vector3 center, float radius, int seed)[]
            {
                ("Cluster_Main_NE", new Vector3(44f, 3f, 66f), 14f, 11),
                ("Cluster_Research_West", new Vector3(-58f, 11f, 68f), 13f, 23),
                ("Cluster_Landing_NW_Flank", new Vector3(-58f, 5f, 44f), 12f, 37),
                ("Cluster_Landing_SW_Flank", new Vector3(-52f, -4f, -22f), 14f, 49),
                ("Cluster_Industrial_South", new Vector3(14f, -11f, -46f), 15f, 61),
                ("Cluster_Mining_Corridor_West", new Vector3(16f, -9f, -76f), 14f, 73),
                ("Cluster_Aux_NorthEast", new Vector3(78f, -2f, 44f), 15f, 89),
                ("Cluster_Outer_SouthEast", new Vector3(72f, -15f, -54f), 14f, 103),
            };

            string[] smallRockModels = { "rock", "meteor", "meteor_detailed", "crater", "rocks_smallA", "rocks_smallB" };
            foreach (var cs in clusterSpecs)
            {
                var cNode = CreateChild(clusterRoot.transform, cs.name);
                var rng = new System.Random(cs.seed);

                // Anchor medium boulder (drifting gently in place + collider + glowing crystal)
                var anchor = SpawnAsset(cNode.transform, $"{cs.name}_Anchor", (cs.seed % 2 == 0) ? "rock_largeA" : "meteor_detailed",
                    cs.center,
                    new Vector3(rng.Next(-12, 12), rng.Next(0, 360), rng.Next(-12, 12)),
                    new Vector3(11f, 9f, 10f), matSlate, matRockDark, true);
                if (anchor != null)
                {
                    AttachChildGlowCrystal(anchor.transform, matRockDark, (cs.seed % 2 == 0) ? matCrystalCyan : matCrystalPurple, 2.4f);
                    AttachDriftingPhysics(anchor, 0.7f, 7f, 4.0f);
                }

                // 4 asymmetric satellite rocks at varied radii & elevations (colliders + drifting + glow crystals)
                float[] angles = { 18f, 95f, 205f, 305f };
                float[] distMults = { 0.55f, 0.85f, 0.70f, 1.05f };
                float[] scales = { 7.5f, 5.2f, 6.4f, 4.0f };
                for (int k = 0; k < 4; k++)
                {
                    float rad = (angles[k] + rng.Next(-14, 15)) * Mathf.Deg2Rad;
                    float dist = cs.radius * distMults[k];
                    float yOff = rng.Next(-5, 6);
                    var pos = cs.center + new Vector3(Mathf.Cos(rad) * dist, yOff, Mathf.Sin(rad) * dist);
                    var rot = new Vector3(rng.Next(-25, 26), rng.Next(0, 360), rng.Next(-25, 26));
                    string mdl = smallRockModels[(cs.seed + k) % smallRockModels.Length];
                    var sat = SpawnAsset(cNode.transform, $"{cs.name}_Sat_{k:D2}", mdl, pos, rot, Vector3.one * scales[k],
                        (k % 2 == 0) ? matRockDark : matSlate, matRockDark, true);
                    if (sat != null)
                    {
                        if (k % 2 == 0)
                        {
                            var cMat = ((cs.seed + k) % 3 == 0) ? matCrystalMagenta : ((k == 0) ? matCrystalCyan : matCrystalPurple);
                            AttachChildGlowCrystal(sat.transform, matRockDark, cMat, 1.8f);
                        }
                        AttachDriftingPhysics(sat, 0.9f + (k * 0.15f), 11f + k * 2f, 4.5f);
                    }
                }
            }
        }

        // =========================================================================================
        // LAYER 1 & 2 TRANSITIONS: SPACE INFRASTRUCTURE
        // =========================================================================================
        private static void BuildSpaceInfrastructure(
            Transform infraRoot, Transform activityRoot,
            Material matRockDark, Material matSlate, Material matFloor, Material matSteel,
            Material matGunmetal, Material matDarkTitanium, Material matCyanNeon, Material matCrystalCyan)
        {
            // 1. Small Mining Platform (SE Mining Corridor on LargeAst_SE_MiningGiant)
            BuildPlatformDeck(infraRoot, "Mining_Platform_SouthEast", new Vector3(42f, -4.5f, -52f), 2.4f, matFloor, matSteel, matGunmetal, matCyanNeon, true);
            SpawnAsset(infraRoot, "Mining_SE_Barrels", "barrels_rail", new Vector3(40.2f, -4.1f, -50.5f), new Vector3(0f, 30f, 0f), Vector3.one * 1.4f, matSteel, matGunmetal, false);
            var seCrystal = SpawnAsset(infraRoot, "Mining_SE_Deposit", "rock_crystalsLargeA", new Vector3(45.5f, -4.2f, -54.5f), new Vector3(0f, 15f, 0f), Vector3.one * 3.8f, matRockDark, matCrystalCyan, false);
            var seDrone = SpawnAsset(activityRoot, "MiningDrone_SE_Platform", "Enemy_2Legs", new Vector3(41.5f, -4.1f, -52.5f), new Vector3(0f, 130f, 0f), Vector3.one * 1.1f, null, null, false);
            if (seDrone != null && seCrystal != null) AttachMiningLaser(seDrone.transform, seCrystal.transform, new Color(0.2f, 0.9f, 1f, 1f), 0.09f);

            // 2. Small Mining Platform (West Asteroid on LargeAst_West_MiningAnchor, clear of Z=23)
            BuildPlatformDeck(infraRoot, "Mining_Platform_West", new Vector3(-66f, 2.8f, 26f), 2.0f, matFloor, matSteel, matGunmetal, matCyanNeon, true);
            var wCrystal = SpawnAsset(infraRoot, "Mining_West_Deposit", "rock_crystalsLargeB", new Vector3(-69.5f, 3.0f, 28.5f), new Vector3(0f, -45f, 0f), Vector3.one * 3.5f, matRockDark, matCrystalCyan, false);
            var wDrone = SpawnAsset(activityRoot, "MiningDrone_West_Platform", "Enemy_2Legs", new Vector3(-65.5f, 3.2f, 25.5f), new Vector3(0f, -55f, 0f), Vector3.one * 1.1f, null, null, false);
            if (wDrone != null && wCrystal != null) AttachMiningLaser(wDrone.transform, wCrystal.transform, new Color(0.25f, 0.85f, 1f, 1f), 0.08f);

            // 3. Energy Relay Stations (3) + Subtle Energy Transmission Beams
            var rNW = BuildEnergyRelay(infraRoot, "Energy_Relay_NW", new Vector3(-48f, 6.5f, 58f), matFloor, matSteel, matGunmetal, matCyanNeon);
            var rNE = BuildEnergyRelay(infraRoot, "Energy_Relay_NE", new Vector3(52f, 1.5f, 44f), matFloor, matSteel, matGunmetal, matCyanNeon);
            var rSE = BuildEnergyRelay(infraRoot, "Energy_Relay_SE", new Vector3(68f, -6.5f, -26f), matFloor, matSteel, matGunmetal, matCyanNeon);
            if (rNW != null && rNE != null) AttachMiningLaser(rNW, rNE, new Color(0.18f, 0.82f, 1f, 0.45f), 0.06f);
            if (rNE != null && rSE != null) AttachMiningLaser(rNE, rSE, new Color(0.18f, 0.82f, 1f, 0.45f), 0.06f);

            // 4. Observation Towers (2)
            BuildObservationTower(infraRoot, "Observation_Tower_North", new Vector3(28f, 4.0f, 62f), 190f, matSteel, matDarkTitanium, matCyanNeon);
            BuildObservationTower(infraRoot, "Observation_Tower_SW", new Vector3(-26f, -1.0f, -14f), 35f, matSteel, matDarkTitanium, matCyanNeon);

            // 5. Floating Cargo Platform (East) & Asteroid Docking Structure (SW)
            BuildPlatformDeck(infraRoot, "Floating_Cargo_Platform_East", new Vector3(88f, -4.5f, 22f), 2.8f, matFloor, matSteel, matGunmetal, matCyanNeon, true);
            SpawnAsset(infraRoot, "Cargo_East_Barrels", "barrels_rail", new Vector3(86f, -4.1f, 21f), new Vector3(0f, 45f, 0f), Vector3.one * 1.5f, matSteel, matGunmetal, false);
            SpawnAsset(infraRoot, "Cargo_East_Lamp", "Light_Street_1", new Vector3(90.5f, -4.1f, 24f), new Vector3(0f, -135f, 0f), Vector3.one * 1.4f, matGunmetal, matCyanNeon, false);

            BuildPlatformDeck(infraRoot, "Asteroid_Docking_Structure_SW", new Vector3(-50f, -2.5f, -6f), 2.8f, matFloor, matSteel, matGunmetal, matCyanNeon, true);
            SpawnAsset(infraRoot, "Dock_SW_Sign", "Sign_1", new Vector3(-47.5f, -1.0f, -4f), new Vector3(0f, 40f, 0f), Vector3.one * 1.4f, matSteel, matCyanNeon, false);
            SpawnAsset(infraRoot, "Dock_SW_Lamp", "Light_Street_2", new Vector3(-52.5f, -2.1f, -8f), new Vector3(0f, 45f, 0f), Vector3.one * 1.4f, matGunmetal, matCyanNeon, false);

            // 6. Small Research Outpost (North) & Industrial Maintenance Station (SE)
            BuildPlatformDeck(infraRoot, "Small_Research_Outpost_North", new Vector3(-8f, 13.0f, 96f), 2.6f, matFloor, matSteel, matGunmetal, matCyanNeon, true);
            SpawnAsset(infraRoot, "Research_North_Dish", "satelliteDish_large", new Vector3(-6f, 13.4f, 97f), new Vector3(0f, 170f, 0f), Vector3.one * 1.8f, matSteel, matCyanNeon, false);
            SpawnAsset(infraRoot, "Research_North_Antenna", "Antenna_1", new Vector3(-10.5f, 13.4f, 95f), Vector3.zero, Vector3.one * 1.8f, matGunmetal, matCyanNeon, false);
            SpawnAsset(activityRoot, "ResearchDrone_North", "Enemy_2Legs", new Vector3(-8f, 13.4f, 94.5f), new Vector3(0f, 180f, 0f), Vector3.one * 1.1f, null, null, false);

            BuildPlatformDeck(infraRoot, "Industrial_Maintenance_Station_SE", new Vector3(58f, -10.5f, -42f), 2.6f, matFloor, matSteel, matGunmetal, matCyanNeon, true);
            SpawnAsset(infraRoot, "Maint_SE_Pipe", "Pipe_1", new Vector3(56f, -9.8f, -41f), new Vector3(0f, 35f, 0f), Vector3.one * 1.4f, matGunmetal, matSteel, false);
            SpawnAsset(infraRoot, "Maint_SE_Barrels", "barrels", new Vector3(60f, -10.1f, -43.5f), Vector3.zero, Vector3.one * 1.4f, matSteel, matGunmetal, false);
            SpawnAsset(activityRoot, "MaintRobot_SE", "Enemy_2Legs", new Vector3(58.5f, -10.1f, -40.5f), new Vector3(0f, -120f, 0f), Vector3.one * 1.1f, null, null, false);
        }

        // =========================================================================================
        // LAYER 3 DECORATIONS: CRYSTALS, METALLIC DEBRIS, ABANDONED PARTS, SUBTLE SPACE DUST
        // =========================================================================================
        private static void BuildDecorationsAndDebris(
            Transform decorRoot,
            Material matRockDark, Material matSlate, Material matSteel, Material matGunmetal,
            Material matCrystalCyan, Material matCrystalPurple, Material matCrystalMagenta)
        {
            // Floating mineral fragments (no colliders)
            var mineralPos = new Vector3[]
            {
                new Vector3(-44f, 5f, 48f), new Vector3(-62f, 9f, 56f), new Vector3(38f, 6f, 54f),
                new Vector3(64f, -1f, 30f), new Vector3(48f, -7f, -46f), new Vector3(6f, -8f, -42f),
                new Vector3(-34f, -3f, -20f), new Vector3(-70f, 12f, 38f),
            };
            for (int i = 0; i < mineralPos.Length; i++)
            {
                var cMat = (i % 3 == 0) ? matCrystalPurple : (i % 3 == 1 ? matCrystalMagenta : matCrystalCyan);
                var frag = SpawnAsset(decorRoot, $"Mineral_Fragment_{i:D2}", "rock_crystals", mineralPos[i],
                    new Vector3(i * 22f, i * 53f, i * 17f), Vector3.one * (2.2f + (i % 3) * 0.6f), matRockDark, cMat, true);
                if (frag != null) AttachDriftingPhysics(frag, 1.1f, 16f, 5.0f);
            }

            // Metallic debris & abandoned spacecraft components drifting along outer belt
            var debrisSpecs = new (string name, string model, Vector3 pos, Vector3 rot, float scale)[]
            {
                ("Debris_FuelPod_NW", "rocket_fuelA", new Vector3(-72f, 14f, 62f), new Vector3(32f, 45f, -24f), 2.4f),
                ("Debris_HullWing_SW", "rocket_sidesA", new Vector3(-62f, -5f, -28f), new Vector3(-18f, 110f, 38f), 2.6f),
                ("Debris_HullPod_SE", "rocket_fuelB", new Vector3(78f, -11f, -58f), new Vector3(24f, -65f, 19f), 2.4f),
                ("Debris_Strut_NE", "pipe_straight", new Vector3(62f, 4f, 54f), new Vector3(15f, 35f, -42f), 2.0f),
                ("Debris_Strut_North", "supports_high", new Vector3(8f, 9f, 84f), new Vector3(48f, 20f, -15f), 1.6f),
                ("Debris_HullWing_East", "rocket_sidesB", new Vector3(94f, -2f, 42f), new Vector3(-28f, -140f, 22f), 2.5f),
            };
            foreach (var d in debrisSpecs)
            {
                SpawnAsset(decorRoot, d.name, d.model, d.pos, d.rot, Vector3.one * d.scale, matGunmetal, matSteel, false);
            }

            // Subtle ambient cosmic dust particle volume (1 lightweight ParticleSystem)
            var dustGo = CreateChild(decorRoot, "Ambient_Cosmic_Dust");
            dustGo.transform.position = new Vector3(5f, 6f, 20f);
            var ps = dustGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 140;
            main.startLifetime = 14f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.85f, 1f, 0.25f), new Color(0.65f, 0.45f, 1f, 0.22f));
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(180f, 50f, 180f);
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            var psr = dustGo.GetComponent<ParticleSystemRenderer>();
            psr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        }

        // =========================================================================================
        // LAYER 4: DEEP SPACE BACKGROUND (LOD-CULLED, COLLIDERS + GENTLE DRIFT)
        // =========================================================================================
        private static void BuildDeepSpaceLayer(
            Transform deepRoot, Material matRockDark, Material matSlate, Material matSteel, Material matCyanNeon,
            Material matCrystalCyan, Material matCrystalPurple)
        {
            string[] deepModels = { "rock_largeA", "rock_largeB", "meteor_detailed", "meteor", "craterLarge" };
            var rng = new System.Random(404);

            for (int i = 0; i < 24; i++)
            {
                float angle = (i * 15f + rng.Next(-5, 6)) * Mathf.Deg2Rad;
                // Keep a cleaner gap directly along West landing vector (170..195 deg)
                float deg = angle * Mathf.Rad2Deg;
                if (deg > 168f && deg < 196f) continue;

                float radius = Mathf.Lerp(148f, 235f, (float)rng.NextDouble());
                float y = rng.Next(-42, 52);
                var pos = new Vector3(10f + Mathf.Cos(angle) * radius, y, 20f + Mathf.Sin(angle) * radius);
                var rot = new Vector3(rng.Next(-30, 31), rng.Next(0, 360), rng.Next(-30, 31));
                float sc = Mathf.Lerp(10f, 24f, (float)rng.NextDouble());

                var go = SpawnAsset(deepRoot, $"DeepSpace_Asteroid_{i:D2}", deepModels[i % deepModels.Length],
                    pos, rot, Vector3.one * sc, (i % 2 == 0) ? matRockDark : matSlate, matSlate, true);
                if (go != null)
                {
                    if (i % 3 == 0) AttachChildGlowCrystal(go.transform, matRockDark, (i % 2 == 0) ? matCrystalCyan : matCrystalPurple, 3.2f);
                    AttachDriftingPhysics(go, 1.4f, 8f, 14f);
                    AddSimpleCullLOD(go, 0.015f);
                }
            }

            // 4 Distant communication satellites
            var satPositions = new Vector3[]
            {
                new Vector3(-125f, 34f, 118f),
                new Vector3(132f, 28f, 110f),
                new Vector3(145f, -18f, -92f),
                new Vector3(-110f, -24f, -112f),
            };
            for (int i = 0; i < satPositions.Length; i++)
            {
                var sat = SpawnAsset(deepRoot, $"DeepSpace_Satellite_{i:D2}", "satelliteDish",
                    satPositions[i], new Vector3(12f, i * 85f, 0f), Vector3.one * 2.4f, matSteel, matCyanNeon, false);
                if (sat != null) AddSimpleCullLOD(sat, 0.01f);
            }
        }

        // =========================================================================================
        // AMBIENT ACTIVITIES (REUSING EXISTING COMPONENTS)
        // =========================================================================================
        private static void BuildAmbientActivities(Transform activityRoot, Material matSteel, Material matGunmetal, Material matCyanNeon)
        {
            // 1. Cargo Ships travelling between asteroid facilities using GemstoneCargoShuttle
            var routes = new (string name, string prefab, float speed, Vector3[] pts)[]
            {
                ("Colony_Cargo_Shuttle_EastLoop", "craft_cargoA", 9.5f, new[]
                {
                    new Vector3(88f, 2f, 22f),
                    new Vector3(58f, -4f, -42f),
                    new Vector3(42f, 2f, -52f),
                    new Vector3(55f, 5f, 15f),
                }),
                ("Colony_Cargo_Shuttle_WestLoop", "craft_cargoB", 8.5f, new[]
                {
                    new Vector3(-50f, 4f, -6f),
                    new Vector3(-66f, 8f, 26f),
                    new Vector3(-48f, 14f, 58f),
                    new Vector3(-20f, 10f, -24f),
                }),
                ("Colony_Mining_Hauler_South", "craft_miner", 7.8f, new[]
                {
                    new Vector3(42f, 1f, -52f),
                    new Vector3(48f, 4f, -83f),
                    new Vector3(16f, 3f, -46f),
                    new Vector3(15f, 2f, -24f),
                }),
            };

            foreach (var r in routes)
            {
                var ship = SpawnAsset(activityRoot, r.name, r.prefab, r.pts[0], Vector3.zero, Vector3.one * 1.8f, null, null, false);
                if (ship != null)
                {
                    var shuttle = ship.AddComponent<GemstoneCargoShuttle>();
                    shuttle.cruiseSpeed = r.speed;
                    shuttle.turnSpeed = 2.8f;
                    shuttle.routeWaypoints = new List<Vector3>(r.pts);
                }
            }

            // 2. Orbiting Satellites around 3 large outer asteroids using HologramPlanetRotator
            var orbitAnchors = new (string name, Vector3 center, float radius, float speed)[]
            {
                ("Orbiting_Sat_NW", new Vector3(-76f, 18f, 82f), 20f, 14f),
                ("Orbiting_Sat_NE", new Vector3(58f, 14f, 82f), 22f, -12f),
                ("Orbiting_Sat_SE", new Vector3(82f, -4f, -74f), 20f, 15f),
            };
            foreach (var o in orbitAnchors)
            {
                var pivot = CreateChild(activityRoot, o.name + "_Pivot");
                pivot.transform.position = o.center;
                var ring = CreateChild(pivot.transform, "OrbitRing");
                var sat = SpawnAsset(ring.transform, o.name, "satelliteDish",
                    o.center + new Vector3(o.radius, 0f, 0f), new Vector3(0f, -90f, 0f), Vector3.one * 1.6f, matSteel, matCyanNeon, false);
                if (sat != null)
                {
                    var rotator = pivot.AddComponent<HologramPlanetRotator>();
                    rotator.outerRing = ring.transform;
                    rotator.outerRingSpeed = o.speed;
                    rotator.bobHeight = 0.4f;
                    rotator.bobFrequency = 0.6f;
                }
            }
        }

        private static void AuditAndEquipExistingFloatingRocks(
            Transform pz,
            Material matRockDark, Material matCrystalCyan, Material matCrystalPurple, Material matCrystalMagenta)
        {
            // 1. Audit all rocks in Asteroid_Planetary_Surface (colliders + glow crystals + drift on floating cluster rocks)
            var surface = pz.Find("Asteroid_Planetary_Surface");
            if (surface != null)
            {
                var arch = surface.Find("04_Colony_Archipelago");
                if (arch != null)
                {
                    int idx = 0;
                    for (int i = 0; i < arch.childCount; i++)
                    {
                        var rock = arch.GetChild(i).gameObject;
                        EnsureMeshColliders(rock, true);
                        bool isFloating = rock.name.StartsWith("West_Archipelago_") || rock.name.StartsWith("Colony_Cluster_Rock_");
                        if (isFloating)
                        {
                            AttachDriftingPhysics(rock, 0.85f, 9f, 4.0f);
                            if (idx % 2 == 0)
                            {
                                var cMat = (idx % 3 == 0) ? matCrystalPurple : ((idx % 4 == 0) ? matCrystalMagenta : matCrystalCyan);
                                AttachChildGlowCrystal(rock.transform, matRockDark, cMat, 2.0f);
                            }
                            idx++;
                        }
                    }
                }
            }

            // 2. Audit existing drifting asteroid fields & floating rocks (colliders + SpaceAsteroidPhysics + glow crystals)
            string[] fieldPaths =
            {
                "AboutIsland/Terrain/FloatingRocks",
                "05_Drifting_Asteroid_Field",
                "Project_Zone_Expanded_Outposts/Complex_Asteroid_Field",
            };
            foreach (var path in fieldPaths)
            {
                var field = pz.Find(path);
                if (field == null) continue;
                for (int i = 0; i < field.childCount; i++)
                {
                    var rock = field.GetChild(i).gameObject;
                    EnsureMeshColliders(rock, true);
                    AttachDriftingPhysics(rock, 1.3f, 14f, 10.0f);
                    if (i % 3 == 0)
                    {
                        var cMat = (i % 2 == 0) ? matCrystalCyan : matCrystalPurple;
                        AttachChildGlowCrystal(rock.transform, matRockDark, cMat, 1.6f);
                    }
                }
            }
        }

        private static void AttachChildGlowCrystal(Transform rockParent, Material matRockDark, Material matCrystal, float desiredWorldScale)
        {
            if (rockParent == null || rockParent.Find("Added_Glow_Crystal") != null) return;
            var asset = LoadAsset("rock_crystals");
            if (asset == null) return;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, rockParent) ?? UnityEngine.Object.Instantiate(asset, rockParent);
            go.name = "Added_Glow_Crystal";
            go.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            go.transform.localRotation = Quaternion.Euler(8f, (rockParent.GetInstanceID() * 37) % 360, -6f);

            // Compensate for parent's non-uniform world scale so crystals never blow up
            var pScale = rockParent.lossyScale;
            go.transform.localScale = new Vector3(
                desiredWorldScale / Mathf.Max(0.1f, Mathf.Abs(pScale.x)),
                desiredWorldScale / Mathf.Max(0.1f, Mathf.Abs(pScale.y)),
                desiredWorldScale / Mathf.Max(0.1f, Mathf.Abs(pScale.z)));

            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = mr.sharedMaterials;
                if (mats == null || mats.Length == 0) mr.sharedMaterial = matCrystal;
                else
                {
                    var newMats = new Material[mats.Length];
                    for (int i = 0; i < newMats.Length; i++) newMats[i] = (i % 2 == 1) ? matCrystal : matRockDark;
                    mr.sharedMaterials = newMats;
                }
            }
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(col);
            }
        }

        private static void AttachDriftingPhysics(GameObject rock, float driftSpeed, float tumbleSpeed, float roamRadius)
        {
            if (rock == null) return;
            EnsureMeshColliders(rock, true);

            var rb = rock.GetComponent<Rigidbody>();
            if (rb == null) rb = rock.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;

            var sap = rock.GetComponent<SpaceAsteroidPhysics>();
            if (sap == null) sap = rock.AddComponent<SpaceAsteroidPhysics>();
            sap.driftSpeed = driftSpeed;
            sap.tumbleSpeed = tumbleSpeed;
            sap.roamRadius = roamRadius;
            sap.returnForce = 2.2f;
        }

        private static void EnsureMeshColliders(GameObject go, bool convex)
        {
            if (go == null) return;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var col = mf.GetComponent<Collider>();
                if (col == null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = convex;
                }
                else if (convex && col is MeshCollider existingMc)
                {
                    existingMc.convex = true;
                }
            }
        }

        // =========================================================================================
        // HELPERS
        // =========================================================================================
        private static void BuildPlatformDeck(
            Transform parent, string name, Vector3 pos, float scale,
            Material matFloor, Material matSteel, Material matGunmetal, Material matCyanNeon, bool withSupports)
        {
            var root = CreateChild(parent, name);
            root.transform.position = pos;
            SpawnAsset(root.transform, "Deck", "platform_large", pos, Vector3.zero, Vector3.one * scale, matFloor, matSteel, true);
            if (withSupports)
            {
                SpawnAsset(root.transform, "Support", "supports_high", pos + new Vector3(0f, -4.5f, 0f),
                    Vector3.zero, Vector3.one * (scale * 0.7f), matGunmetal, matSteel, false);
            }
            SpawnAsset(root.transform, "Beacon", "Antenna_1", pos + new Vector3(scale * 0.7f, 0.3f, scale * 0.7f),
                Vector3.zero, Vector3.one * 1.2f, matGunmetal, matCyanNeon, false);
        }

        private static Transform BuildEnergyRelay(
            Transform parent, string name, Vector3 pos,
            Material matFloor, Material matSteel, Material matGunmetal, Material matCyanNeon)
        {
            var root = CreateChild(parent, name);
            root.transform.position = pos;
            SpawnAsset(root.transform, "Base", "platform_small", pos, Vector3.zero, Vector3.one * 1.8f, matFloor, matSteel, true);
            SpawnAsset(root.transform, "RelayRing", "pipe_ringHigh", pos + new Vector3(0f, 0.4f, 0f), Vector3.zero, Vector3.one * 1.4f, matGunmetal, matCyanNeon, false);
            var spire = SpawnAsset(root.transform, "RelaySpire", "Antenna_2", pos + new Vector3(0f, 1.2f, 0f), Vector3.zero, Vector3.one * 1.8f, matSteel, matCyanNeon, false);
            return spire != null ? spire.transform : root.transform;
        }

        private static void BuildObservationTower(
            Transform parent, string name, Vector3 pos, float yaw,
            Material matSteel, Material matDarkTitanium, Material matCyanNeon)
        {
            var root = CreateChild(parent, name);
            root.transform.position = pos;
            SpawnAsset(root.transform, "TowerPylon", "supports_high", pos, new Vector3(0f, yaw, 0f), new Vector3(1.5f, 2.5f, 1.5f), matDarkTitanium, matSteel, true);
            SpawnAsset(root.transform, "TowerDeck", "platform_small", pos + new Vector3(0f, 5.5f, 0f), new Vector3(0f, yaw, 0f), Vector3.one * 1.6f, matSteel, matCyanNeon, true);
            SpawnAsset(root.transform, "TowerDish", "satelliteDish_detailed", pos + new Vector3(0f, 6.2f, 0f), new Vector3(0f, yaw, 0f), Vector3.one * 1.4f, matSteel, matCyanNeon, false);
        }

        private static void AttachMiningLaser(Transform origin, Transform target, Color color, float width)
        {
            var lr = origin.gameObject.GetComponent<LineRenderer>();
            if (lr == null) lr = origin.gameObject.AddComponent<LineRenderer>();
            lr.startWidth = width;
            lr.endWidth = width * 0.7f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            var laserMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            laserMat.color = color;
            lr.sharedMaterial = laserMat;

            var laser = origin.gameObject.AddComponent<MiningExtractionLaser>();
            laser.laserEmitterOrigin = origin;
            laser.crystalTarget = target;
            laser.laserBeam = lr;
            laser.laserColor = color;
            laser.baseWidth = width;
        }

        private static void AddSimpleCullLOD(GameObject go, float cullHeight)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            var lodGroup = go.GetComponent<LODGroup>();
            if (lodGroup == null) lodGroup = go.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[] { new LOD(cullHeight, renderers) });
            lodGroup.RecalculateBounds();
        }

        private static GameObject SpawnAsset(
            Transform parent, string instanceName, string assetName,
            Vector3 worldPos, Vector3 worldEuler, Vector3 scaleMult,
            Material mat1, Material mat2, bool ensureCollider)
        {
            var asset = LoadAsset(assetName);
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
            else
            {
                foreach (var col in go.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.DestroyImmediate(col);
                }
            }
            return go;
        }

        private static GameObject LoadAsset(string assetName)
        {
            if (AssetCache.TryGetValue(assetName, out var cached) && cached != null) return cached;
            foreach (var filter in new[] { " t:Model", " t:Prefab" })
            {
                foreach (var guid in AssetDatabase.FindAssets(assetName + filter))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path).Equals(assetName, StringComparison.OrdinalIgnoreCase))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (asset != null) { AssetCache[assetName] = asset; return asset; }
                    }
                }
            }
            return null;
        }

        private static Material LoadMat(string path)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
