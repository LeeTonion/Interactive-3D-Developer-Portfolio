using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceCyberpunk
{
    /// <summary>
    /// Builder chuẩn kiến trúc Modular cho Tiểu Hành Tinh Khai Thác Đá Quý (Gemstone Mining Asteroid).
    /// Tuân thủ nghiêm ngặt Art Direction & Technical Implementation:
    /// - Thân thiên thạch gồ ghề đa tầng liền khối (~58m x 38m x 50m), sử dụng vật liệu đá tối Mat_Asteroid_RockDark & Mat_Asteroid_Slate.
    /// - Chỉ phát sáng đúng Submesh tinh thể (Submesh 2), giữ nguyên chân đá tối màu tạo độ tương phản cao.
    /// - Lõi năng lượng trung tâm (EnergyCore) với 3 vòng xoay cơ khí đa trục, 4 đường ống dẫn năng lượng sáng tuần tự và hạt năng lượng bay quanh lõi.
    /// - 4 Trạm công nghiệp tích hợp trực tiếp trên bề mặt đá (không lơ lửng).
    /// - 3 Drone khai khoáng tự hành, 3 Tàu chở hàng bay quỹ đạo, và đầy đủ 4 khám phá tương tác (Interactions A, B, C, D).
    /// </summary>
    public class GemstoneMiningAsteroidBuilder : MonoBehaviour
    {
        public const string AsteroidRootName = "GemstoneMiningAsteroid";
        public static readonly Vector3 DefaultWorldPosition = new Vector3(95f, 16f, -65f);

        [ContextMenu("Build Gemstone Mining Asteroid")]
        public static GameObject BuildEnvironment()
        {
            var oldRoot = GameObject.Find(AsteroidRootName);
            if (oldRoot != null)
            {
                if (Application.isPlaying) Destroy(oldRoot);
                else DestroyImmediate(oldRoot);
            }

            // Gỡ bỏ bản cũ nếu từng đặt trong Project_Zone
            var pz = GameObject.Find("Project_Zone");
            if (pz != null)
            {
                var oldInPz = pz.transform.Find(AsteroidRootName);
                if (oldInPz != null)
                {
                    if (Application.isPlaying) Destroy(oldInPz.gameObject);
                    else DestroyImmediate(oldInPz.gameObject);
                }
            }

            GameObject root = new GameObject(AsteroidRootName);
            root.transform.position = DefaultWorldPosition;

            // ─────────────────────────────────────────────────────────────────
            // Chuẩn bị Vật Liệu URP Chuẩn (Lưu thành Asset để bền vững 100%)
            // ─────────────────────────────────────────────────────────────────
            Material rockDarkMat = LoadOrCreateRockMaterial("Assets/New/Materials/Mat_Asteroid_RockDark.mat", new Color(0.10f, 0.11f, 0.14f, 1f), 0.25f, 0.18f);
            Material rockSlateMat = LoadOrCreateRockMaterial("Assets/New/Materials/Mat_Asteroid_Slate.mat", new Color(0.14f, 0.16f, 0.20f, 1f), 0.35f, 0.24f);
            Material darkTitaniumMat = LoadOrCreateRockMaterial("Assets/New/Materials/Mat_SciFi_DarkTitanium.mat", new Color(0.13f, 0.15f, 0.19f, 1f), 0.78f, 0.58f);
            Material steelAccentMat = LoadOrCreateRockMaterial("Assets/New/Materials/Mat_SciFi_SteelAccent.mat", new Color(0.22f, 0.26f, 0.32f, 1f), 0.85f, 0.68f);

            Color blueCyanColor = new Color(0f, 0.88f, 1f, 1f);
            Color purpleRareColor = new Color(0.72f, 0.16f, 1f, 1f);
            Color magentaRareColor = new Color(0.95f, 0.15f, 0.72f, 1f);
            Color smallDepositColor = new Color(0.12f, 0.75f, 0.95f, 1f);
            Color hiddenMineralColor = new Color(1f, 0.28f, 0.82f, 1f);

            Material blueCrystalMat = LoadOrCreateEmissiveMaterial("Assets/New/Materials/Mat_Gemstone_BlueCyan.mat", blueCyanColor, 1.8f);
            Material purpleCrystalMat = LoadOrCreateEmissiveMaterial("Assets/New/Materials/Mat_Gemstone_PurpleRare.mat", purpleRareColor, 2.0f);
            Material magentaCrystalMat = LoadOrCreateEmissiveMaterial("Assets/New/Materials/Mat_Gemstone_MagentaRare.mat", magentaRareColor, 1.9f);
            Material smallDepositMat = LoadOrCreateEmissiveMaterial("Assets/New/Materials/Mat_Gemstone_SmallMineral.mat", smallDepositColor, 1.0f);
            Material hiddenMineralMat = LoadOrCreateEmissiveMaterial("Assets/New/Materials/Mat_Gemstone_HiddenRare.mat", hiddenMineralColor, 1.8f);
            Material coreGlowMat = LoadOrCreateEmissiveMaterial("Assets/New/Materials/Mat_Gemstone_CoreReactor.mat", blueCyanColor, 2.8f);

            // Load FBX Models từ Kenney Space Kit
            GameObject rockLargeA = LoadModel("rock_largeA.fbx");
            GameObject rockLargeB = LoadModel("rock_largeB.fbx");
            GameObject meteorHalf = LoadModel("meteor_half.fbx");
            GameObject meteorDetailed = LoadModel("meteor_detailed.fbx");
            GameObject meteor = LoadModel("meteor.fbx");
            GameObject rocksSmallA = LoadModel("rocks_smallA.fbx");
            GameObject rocksSmallB = LoadModel("rocks_smallB.fbx");

            GameObject crystalLargeA = LoadModel("rock_crystalsLargeA.fbx");
            GameObject crystalLargeB = LoadModel("rock_crystalsLargeB.fbx");
            GameObject crystalSmall = LoadModel("rock_crystals.fbx");

            GameObject platformLarge = LoadModel("platform_large.fbx");
            GameObject platformLong = LoadModel("platform_long.fbx");
            GameObject supportsHigh = LoadModel("supports_high.fbx");
            GameObject supportsLow = LoadModel("supports_low.fbx");
            GameObject structureDetailed = LoadModel("structure_detailed.fbx");
            GameObject structureClosed = LoadModel("structure_closed.fbx");
            GameObject hangarRoundGlass = LoadModel("hangar_roundGlass.fbx");
            GameObject satelliteDishLarge = LoadModel("satelliteDish_large.fbx");
            GameObject generatorLarge = LoadModel("machine_generatorLarge.fbx");
            GameObject generatorSmall = LoadModel("machine_generator.fbx");
            GameObject barrelLarge = LoadModel("machine_barrelLarge.fbx");
            GameObject barrelsRail = LoadModel("barrels_rail.fbx");
            GameObject wirelessMachine = LoadModel("machine_wireless.fbx");
            GameObject chimneyDetailed = LoadModel("chimney_detailed.fbx");
            GameObject pipeStraight = LoadModel("pipe_straight.fbx");
            GameObject pipeRing = LoadModel("pipe_ring.fbx");
            GameObject pipeRingHigh = LoadModel("pipe_ringHigh.fbx");

            GameObject craftMiner = LoadModel("craft_miner.fbx");
            GameObject craftCargoA = LoadModel("craft_cargoA.fbx");
            GameObject craftSpeederB = LoadModel("craft_speederB.fbx");

            // ═════════════════════════════════════════════════════════════════
            // 1. ASTEROID BODY (Khối Thiên Thạch Đá Tối Liền Khối Đa Tầng)
            // ═════════════════════════════════════════════════════════════════
            GameObject asteroidBody = new GameObject("AsteroidBody");
            asteroidBody.transform.SetParent(root.transform, false);

            // Lưu ý: rock_largeA & B có size Y = 0.50 (minY=0, maxY=0.5).
            // Đặt chồng khít lên nhau tạo địa hình đa tầng liền mạch 100%, không bao giờ rời rạc!
            (string name, GameObject prefab, Vector3 pos, Vector3 rot, Vector3 scale, bool addCollider)[] bodyRocks = new[]
            {
                // Thềm trung tâm chính (Đỉnh ở Y = 0.0m, rộng ~42m x 36m)
                ("Central_Basin_Massif", rockLargeA, new Vector3(0f, -6.5f, 0f), new Vector3(0f, 15f, 0f), new Vector3(46f, 13f, 42f), true),
                // Dãy núi cao phía Bắc (Đỉnh ở Y = +3.5m)
                ("North_High_Ridge_Massif", rockLargeB, new Vector3(-2f, -4.0f, 12f), new Vector3(0f, 70f, 0f), new Vector3(32f, 15f, 24f), true),
                // Thềm khai thác phía Tây (Đỉnh ở Y = +1.5m)
                ("West_Extraction_Plateau", rockLargeA, new Vector3(-14f, -5.0f, -2f), new Vector3(0f, 140f, 0f), new Vector3(28f, 13f, 28f), true),
                // Thềm bến cảng phía Đông (Đỉnh ở Y = 0.0m)
                ("East_Cargo_Terrace", rockLargeB, new Vector3(14f, -5.5f, -2f), new Vector3(0f, 310f, 0f), new Vector3(28f, 11f, 26f), true),
                // Thềm trạm truyền tải phía Nam (Đỉnh ở Y = -0.5m)
                ("South_Substation_Ledge", rockLargeA, new Vector3(2f, -6.0f, -13f), new Vector3(0f, 215f, 0f), new Vector3(26f, 11f, 22f), true),

                // Các đỉnh núi đá gồ ghề bao quanh (Craggy Peaks & Shoulders)
                ("Peak_NorthWest_Crag", meteorDetailed, new Vector3(-11f, -0.5f, 10f), new Vector3(12f, 35f, -10f), new Vector3(15f, 13f, 15f), true),
                ("Peak_NorthEast_Crag", meteorDetailed, new Vector3(10f, -0.5f, 11f), new Vector3(-10f, 125f, 14f), new Vector3(14f, 11.5f, 14f), true),
                ("Shoulder_SouthWest_Outcrop", meteorDetailed, new Vector3(-13f, -2.5f, -11f), new Vector3(8f, 240f, -12f), new Vector3(13f, 9.5f, 13f), true),
                ("Shoulder_SouthEast_Outcrop", meteorDetailed, new Vector3(13f, -2.8f, -11f), new Vector3(-12f, 300f, 8f), new Vector3(12f, 8.5f, 12f), true),

                // Phần đáy nhọn của thiên thạch (Underbelly Inverted Cones - xoay 180 độ trục Z)
                ("Underbelly_Primary_Mass", meteorHalf, new Vector3(0f, -4.0f, 0f), new Vector3(0f, 195f, 180f), new Vector3(44f, 52f, 40f), true),
                ("Underbelly_West_Mass", meteorHalf, new Vector3(-14f, -3.5f, -2f), new Vector3(4f, 285f, 182f), new Vector3(26f, 38f, 26f), true),
                ("Underbelly_East_Mass", meteorHalf, new Vector3(14f, -3.8f, -2f), new Vector3(-4f, 115f, 178f), new Vector3(26f, 40f, 24f), true),
                ("Underbelly_North_Mass", meteorHalf, new Vector3(-2f, -2.5f, 12f), new Vector3(5f, 45f, 180f), new Vector3(28f, 42f, 22f), true),
                ("Underbelly_South_Mass", meteorHalf, new Vector3(2f, -3.8f, -12f), new Vector3(-5f, 160f, 180f), new Vector3(24f, 36f, 20f), true),
                ("Abyssal_Keel_Spire", meteorDetailed, new Vector3(0f, -19.5f, 0f), new Vector3(14f, 55f, 180f), new Vector3(16f, 22f, 16f), true),

                // Vòm đá và thềm hang ngầm phía Tây Nam cho Hidden Mineral Deposit (Đỉnh thềm ở Y = -8.5m)
                ("Hidden_Grotto_Ledge", rockLargeB, new Vector3(-12f, -12.5f, -10f), new Vector3(0f, 50f, 0f), new Vector3(16f, 8f, 16f), true),
                ("Hidden_Grotto_Overhang", meteorDetailed, new Vector3(-15f, -7.5f, -12f), new Vector3(25f, 110f, -20f), new Vector3(11f, 8f, 11f), true)
            };

            foreach (var br in bodyRocks)
            {
                if (br.prefab == null) continue;
                var go = Instantiate(br.prefab, asteroidBody.transform, false);
                go.name = br.name;
                go.transform.localPosition = br.pos;
                go.transform.localRotation = Quaternion.Euler(br.rot);
                go.transform.localScale = br.scale;
                ApplyRockMaterials(go, rockDarkMat, rockSlateMat);

                if (br.addCollider && go.GetComponent<Collider>() == null)
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.convex = true;
                }
            }

            // ═════════════════════════════════════════════════════════════════
            // 2. CRYSTAL FORMATIONS (3 Phân Nhóm Đá Quý: Blue, Purple, Deposits)
            // ═════════════════════════════════════════════════════════════════
            GameObject crystalFormations = new GameObject("CrystalFormations");
            crystalFormations.transform.SetParent(root.transform, false);

            GameObject blueGroup = new GameObject("BlueEnergyCrystals");
            blueGroup.transform.SetParent(crystalFormations.transform, false);

            GameObject purpleGroup = new GameObject("PurpleRareCrystals");
            purpleGroup.transform.SetParent(crystalFormations.transform, false);

            GameObject depositGroup = new GameObject("MineralDeposits");
            depositGroup.transform.SetParent(crystalFormations.transform, false);

            List<CrystalResonance> allLargeCrystals = new List<CrystalResonance>();
            List<CrystalResonance> allSmallDeposits = new List<CrystalResonance>();

            // 2.1 Small Mineral Deposits (30 cụm nhỏ rải rác tự nhiên trên các triền đá và sườn dốc)
            (Vector3 pos, Vector3 rot, float scale)[] smallDepositsData = new[]
            {
                // Thềm trung tâm
                (new Vector3(-7.5f, -0.1f, 5.5f), new Vector3(8f, 25f, -5f), 2.4f),
                (new Vector3(7.8f, -0.1f, 5.2f), new Vector3(-6f, 140f, 8f), 2.2f),
                (new Vector3(-8.2f, -0.1f, -6.5f), new Vector3(10f, 210f, -6f), 2.6f),
                (new Vector3(8.5f, -0.1f, -6.8f), new Vector3(-8f, 310f, 10f), 2.3f),
                // Triền đá Tây
                (new Vector3(-16.5f, 1.3f, 3.5f), new Vector3(12f, 85f, -8f), 2.5f),
                (new Vector3(-18.5f, 0.8f, -7.2f), new Vector3(14f, 120f, -6f), 2.3f),
                (new Vector3(-21.0f, -0.5f, -1.5f), new Vector3(16f, 45f, -10f), 2.7f),
                (new Vector3(-13.5f, 0.9f, -10.5f), new Vector3(10f, 230f, 5f), 2.2f),
                // Triền đá Đông
                (new Vector3(16.5f, -0.1f, 4.0f), new Vector3(-10f, 195f, 6f), 2.4f),
                (new Vector3(19.2f, -0.5f, -2.5f), new Vector3(-12f, 280f, 8f), 2.6f),
                (new Vector3(15.5f, -0.8f, -8.5f), new Vector3(-14f, 335f, 12f), 2.3f),
                (new Vector3(18.0f, 0.5f, 7.5f), new Vector3(-8f, 110f, 6f), 2.5f),
                // Vách núi Bắc
                (new Vector3(-4.5f, 3.3f, 14.5f), new Vector3(6f, 45f, -12f), 2.8f),
                (new Vector3(3.5f, 3.2f, 14.0f), new Vector3(-12f, 165f, 8f), 2.5f),
                (new Vector3(-8.5f, 4.2f, 16.5f), new Vector3(14f, 30f, -10f), 2.6f),
                (new Vector3(7.5f, 3.8f, 15.5f), new Vector3(-16f, 190f, 14f), 2.4f),
                (new Vector3(0.0f, 4.8f, 18.0f), new Vector3(20f, 90f, 0f), 2.8f),
                // Sườn dốc Nam
                (new Vector3(-2.5f, -0.6f, -15.5f), new Vector3(14f, 275f, 5f), 2.4f),
                (new Vector3(5.5f, -0.6f, -14.5f), new Vector3(-8f, 60f, -10f), 2.2f),
                (new Vector3(-7.0f, -1.2f, -17.5f), new Vector3(16f, 220f, -8f), 2.5f),
                (new Vector3(8.5f, -1.4f, -16.0f), new Vector3(-15f, 305f, 10f), 2.4f),
                (new Vector3(0.5f, -1.0f, -18.5f), new Vector3(22f, 180f, 0f), 2.6f),
                // Các hốc đá & vách ngầm
                (new Vector3(-9.5f, -4.5f, -8.0f), new Vector3(25f, 140f, -15f), 2.4f),
                (new Vector3(10.0f, -4.2f, -6.5f), new Vector3(-20f, 240f, 10f), 2.3f),
                (new Vector3(-6.0f, -5.5f, 8.5f), new Vector3(15f, 75f, -20f), 2.5f),
                (new Vector3(7.2f, -5.0f, 9.0f), new Vector3(-18f, 150f, 15f), 2.2f),
                (new Vector3(-14.0f, -9.5f, -8.5f), new Vector3(30f, 50f, -10f), 2.7f),
                (new Vector3(12.0f, -8.8f, 6.0f), new Vector3(-25f, 200f, 18f), 2.4f),
                (new Vector3(-3.0f, -12.0f, -4.0f), new Vector3(35f, 120f, -30f), 2.8f),
                (new Vector3(4.0f, -11.5f, 5.0f), new Vector3(-30f, 290f, 25f), 2.6f)
            };

            for (int i = 0; i < smallDepositsData.Length; i++)
            {
                var sd = smallDepositsData[i];
                GameObject prefab = (i % 2 == 0 && crystalSmall != null) ? crystalSmall : crystalLargeA;
                if (prefab == null) continue;

                var depObj = Instantiate(prefab, depositGroup.transform, false);
                depObj.name = $"Mineral_Deposit_{i + 1:D2}";
                depObj.transform.localPosition = sd.pos;
                depObj.transform.localRotation = Quaternion.Euler(sd.rot);
                depObj.transform.localScale = Vector3.one * sd.scale;

                ApplyGemstoneMaterials(depObj, rockDarkMat, rockSlateMat, smallDepositMat);

                var cr = depObj.AddComponent<CrystalResonance>();
                cr.category = CrystalResonance.CrystalCategory.SmallDeposit;
                cr.crystalGlowColor = smallDepositColor;
                cr.crystalGlowIntensity = 1.0f;
                cr.resonanceGlowMultiplier = 2.2f;
                allSmallDeposits.Add(cr);
            }

            // 2.2 Blue Energy Crystals (12 cụm lớn bao quanh Lõi Năng Lượng trung tâm và các thềm đá)
            (Vector3 pos, Vector3 rot, float scale)[] blueCrystalsData = new[]
            {
                (new Vector3(-5.8f, -0.2f, 3.8f), new Vector3(12f, 35f, -8f), 4.4f),
                (new Vector3(5.8f, -0.2f, 3.6f), new Vector3(-10f, 145f, 12f), 4.2f),
                (new Vector3(-6.2f, -0.2f, -3.8f), new Vector3(14f, 225f, -10f), 4.6f),
                (new Vector3(6.0f, -0.2f, -4.0f), new Vector3(-12f, 315f, 8f), 4.3f),
                (new Vector3(0.0f, -0.1f, 6.8f), new Vector3(8f, 90f, 6f), 4.8f),
                (new Vector3(0.0f, -0.1f, -7.2f), new Vector3(8f, 270f, 6f), 4.6f),
                (new Vector3(-10.5f, 1.3f, 2.2f), new Vector3(10f, 15f, -14f), 4.5f),
                (new Vector3(10.5f, 0.8f, 1.5f), new Vector3(-10f, 195f, 14f), 4.4f),
                (new Vector3(-8.5f, 2.6f, 10.0f), new Vector3(12f, 55f, -8f), 4.6f),
                (new Vector3(8.2f, 2.4f, 9.5f), new Vector3(-14f, 135f, 10f), 4.3f),
                (new Vector3(-8.5f, 0.4f, -9.5f), new Vector3(15f, 235f, -12f), 4.5f),
                (new Vector3(8.5f, 0.2f, -10.0f), new Vector3(-12f, 300f, 10f), 4.4f)
            };

            for (int i = 0; i < blueCrystalsData.Length; i++)
            {
                var bd = blueCrystalsData[i];
                GameObject prefab = (i % 2 == 0) ? crystalLargeA : crystalLargeB;
                if (prefab == null) continue;

                var cObj = Instantiate(prefab, blueGroup.transform, false);
                cObj.name = $"Blue_Energy_Crystal_{i + 1:D2}";
                cObj.transform.localPosition = bd.pos;
                cObj.transform.localRotation = Quaternion.Euler(bd.rot);
                cObj.transform.localScale = Vector3.one * bd.scale;

                ApplyGemstoneMaterials(cObj, rockDarkMat, rockSlateMat, blueCrystalMat);

                var ptLightGo = new GameObject("Blue_Crystal_Light");
                ptLightGo.transform.SetParent(cObj.transform, false);
                ptLightGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                var ptLight = ptLightGo.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.color = blueCyanColor;
                ptLight.range = 14f;
                ptLight.intensity = 2.2f;

                var shimmerPs = CreateCrystalShimmerParticles(cObj.transform, blueCyanColor);

                var cr = cObj.AddComponent<CrystalResonance>();
                cr.category = CrystalResonance.CrystalCategory.BlueEnergy;
                cr.crystalGlowColor = blueCyanColor;
                cr.crystalGlowIntensity = 1.6f;
                cr.resonanceGlowMultiplier = 2.4f;
                cr.crystalInteractionRadius = 16f;
                cr.crystalLight = ptLight;
                cr.crystalParticles = shimmerPs;
                AssignClosestDeposits(cr, allSmallDeposits, 2);
                allLargeCrystals.Add(cr);
            }

            // 2.3 Purple Rare Crystals (10 cụm khổng lồ trên các đỉnh núi đá làm Landmark thị giác)
            (Vector3 pos, Vector3 rot, float scale, bool useMagenta)[] purpleCrystalsData = new[]
            {
                // Trên đỉnh Peak_NorthWest_Crag
                (new Vector3(-11.5f, 6.2f, 9.5f), new Vector3(-14f, 40f, 12f), 6.8f, false),
                // Trên đỉnh Peak_NorthEast_Crag
                (new Vector3(10.2f, 5.5f, 10.5f), new Vector3(12f, 155f, -14f), 6.4f, true),
                // Trên vách đá phía Tây
                (new Vector3(-19.5f, 1.2f, -3.5f), new Vector3(8f, 260f, -18f), 6.2f, false),
                // Trên mỏm đá Tây Nam
                (new Vector3(-12.5f, 2.8f, -10.5f), new Vector3(16f, 210f, 10f), 5.8f, true),
                // Trên mỏm đá Đông Nam
                (new Vector3(12.5f, 2.2f, -10.5f), new Vector3(-12f, 320f, 14f), 5.6f, false),
                // Sườn dưới Bắc
                (new Vector3(-1.5f, 3.2f, 17.5f), new Vector3(18f, 10f, -8f), 6.0f, true),
                // Vách núi nhô cao Đông Bắc
                (new Vector3(15.2f, 4.2f, 8.5f), new Vector3(-16f, 130f, 20f), 5.5f, false),
                // Mỏm nhô Tây Bắc
                (new Vector3(-16.0f, 3.8f, 12.0f), new Vector3(14f, 60f, -15f), 5.8f, true),
                // Mỏm nhô Nam gồ ghề
                (new Vector3(0.0f, 1.5f, -16.5f), new Vector3(25f, 180f, 0f), 6.0f, false),
                // Đỉnh vách ngầm phía Nam
                (new Vector3(-6.5f, -3.2f, -14.5f), new Vector3(28f, 210f, -15f), 5.2f, true)
            };

            for (int i = 0; i < purpleCrystalsData.Length; i++)
            {
                var pd = purpleCrystalsData[i];
                GameObject prefab = (i % 2 == 0) ? crystalLargeB : crystalLargeA;
                if (prefab == null) continue;

                var pObj = Instantiate(prefab, purpleGroup.transform, false);
                pObj.name = $"Purple_Rare_Crystal_{i + 1:D2}";
                pObj.transform.localPosition = pd.pos;
                pObj.transform.localRotation = Quaternion.Euler(pd.rot);
                pObj.transform.localScale = Vector3.one * pd.scale;

                Color col = pd.useMagenta ? magentaRareColor : purpleRareColor;
                Material mat = pd.useMagenta ? magentaCrystalMat : purpleCrystalMat;
                ApplyGemstoneMaterials(pObj, rockDarkMat, rockSlateMat, mat);

                var ptLightGo = new GameObject("Purple_Landmark_Light");
                ptLightGo.transform.SetParent(pObj.transform, false);
                ptLightGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                var ptLight = ptLightGo.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.color = col;
                ptLight.range = 20f;
                ptLight.intensity = 3.0f;

                var shimmerPs = CreateCrystalShimmerParticles(pObj.transform, col);

                var cr = pObj.AddComponent<CrystalResonance>();
                cr.category = CrystalResonance.CrystalCategory.PurpleRare;
                cr.crystalGlowColor = col;
                cr.crystalGlowIntensity = 1.8f;
                cr.resonanceGlowMultiplier = 2.5f;
                cr.crystalInteractionRadius = 18f;
                cr.crystalLight = ptLight;
                cr.crystalParticles = shimmerPs;
                AssignClosestDeposits(cr, allSmallDeposits, 2);
                allLargeCrystals.Add(cr);
            }

            // ═════════════════════════════════════════════════════════════════
            // 3. CENTRAL ENERGY CORE (Lò Phản Ứng Năng Lượng Trung Tâm)
            // ═════════════════════════════════════════════════════════════════
            GameObject energyCore = new GameObject("EnergyCore");
            energyCore.transform.SetParent(root.transform, false);
            energyCore.transform.localPosition = Vector3.zero;

            // 3.1 CoreCrystal (Lõi tinh thể năng lượng & Quả cầu plasma trung tâm)
            GameObject coreCrystalGroup = new GameObject("CoreCrystal");
            coreCrystalGroup.transform.SetParent(energyCore.transform, false);
            coreCrystalGroup.transform.localPosition = Vector3.zero;

            // Bệ khung bát giác bảo vệ lò phản ứng gắn chặt xuống nền đá Y = 0
            if (structureDetailed != null)
            {
                for (int k = 0; k < 8; k++)
                {
                    float ang = k * 45f;
                    Vector3 rimPos = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * 4.2f, -0.2f, Mathf.Sin(ang * Mathf.Deg2Rad) * 4.2f);
                    var pillar = Instantiate(structureDetailed, coreCrystalGroup.transform, false);
                    pillar.name = $"Reactor_Collar_Pylon_{k + 1}";
                    pillar.transform.localPosition = rimPos;
                    pillar.transform.localRotation = Quaternion.Euler(0f, -ang + 90f, 0f);
                    pillar.transform.localScale = new Vector3(1.6f, 2.2f, 1.4f);
                    ApplyMetalMaterials(pillar, darkTitaniumMat, steelAccentMat);
                }
            }

            // Khối tinh thể lõi khổng lồ nhô lên từ tâm lò
            Renderer mainCoreCrystalRenderer = null;
            if (crystalLargeA != null)
            {
                var mainCore = Instantiate(crystalLargeA, coreCrystalGroup.transform, false);
                mainCore.name = "Primary_Reactor_Crystal";
                mainCore.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                mainCore.transform.localScale = new Vector3(6.2f, 9.5f, 6.2f);
                ApplyGemstoneMaterials(mainCore, rockDarkMat, rockSlateMat, coreGlowMat);
                mainCoreCrystalRenderer = mainCore.GetComponentInChildren<Renderer>();
            }

            // Quả cầu năng lượng Cyan lơ lửng tại tâm vòng xoay (Y = 5.2m)
            GameObject coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreSphere.name = "Floating_Plasma_Core_Sphere";
            coreSphere.transform.SetParent(coreCrystalGroup.transform, false);
            coreSphere.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            coreSphere.transform.localScale = Vector3.one * 2.6f;
            DestroyImmediate(coreSphere.GetComponent<Collider>());
            var sphereRenderer = coreSphere.GetComponent<Renderer>();
            sphereRenderer.sharedMaterial = coreGlowMat;

            // Đèn chiếu sáng trung tâm Lõi Năng Lượng
            GameObject coreLightGo = new GameObject("Core_Reactor_OmniLight");
            coreLightGo.transform.SetParent(coreCrystalGroup.transform, false);
            coreLightGo.transform.localPosition = new Vector3(0f, 5.5f, 0f);
            var coreLight = coreLightGo.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = blueCyanColor;
            coreLight.range = 42f;
            coreLight.intensity = 6.0f;

            // 3.2 RotatingRings (3 Vòng cơ khí mỏng xoay đa trục quanh quả cầu tâm Y = 5.2m)
            GameObject rotatingRingsGroup = new GameObject("RotatingRings");
            rotatingRingsGroup.transform.SetParent(energyCore.transform, false);
            rotatingRingsGroup.transform.localPosition = new Vector3(0f, 5.2f, 0f);

            List<Transform> ringTransforms = new List<Transform>();
            float[] ringRadii = new float[] { 4.2f, 5.4f, 6.6f };
            for (int r = 0; r < 3; r++)
            {
                var ringPivot = new GameObject($"Mechanical_Gimbal_Ring_{r + 1}");
                ringPivot.transform.SetParent(rotatingRingsGroup.transform, false);
                ringPivot.transform.localPosition = Vector3.zero;
                ringPivot.transform.localRotation = Quaternion.Euler(r * 32f + 15f, r * 60f, r * 20f);

                BuildThinMechanicalGimbalRing(ringPivot.transform, ringRadii[r], pipeStraight, steelAccentMat, coreGlowMat);
                ringTransforms.Add(ringPivot.transform);
            }

            // 3.3 EnergyConduits (4 Tuyến ống dẫn năng lượng nối từ Lõi tới 4 Trạm Khai Khoáng)
            GameObject conduitsGroup = new GameObject("EnergyConduits");
            conduitsGroup.transform.SetParent(energyCore.transform, false);
            conduitsGroup.transform.localPosition = Vector3.zero;

            List<Renderer> conduitStripRenderers = new List<Renderer>();
            (Vector3 start, Vector3 end, string label)[] conduitLines = new[]
            {
                (new Vector3(-4.0f, 0.4f, -0.5f), new Vector3(-12.5f, 1.5f, -1.8f), "Conduit_West_Extraction"),
                (new Vector3(4.0f, 0.4f, -0.5f), new Vector3(12.5f, 0.2f, -2.0f), "Conduit_East_CargoDock"),
                (new Vector3(-0.8f, 0.5f, 4.0f), new Vector3(-1.8f, 3.4f, 10.5f), "Conduit_North_Tower"),
                (new Vector3(0.8f, 0.3f, -4.0f), new Vector3(1.8f, -0.3f, -11.5f), "Conduit_South_Substation")
            };

            foreach (var cl in conduitLines)
            {
                var cRoot = new GameObject(cl.label);
                cRoot.transform.SetParent(conduitsGroup.transform, false);

                Vector3 diff = cl.end - cl.start;
                float len = diff.magnitude;
                Vector3 mid = (cl.start + cl.end) * 0.5f;
                Quaternion rot = Quaternion.LookRotation(diff.normalized, Vector3.up);

                if (pipeStraight != null)
                {
                    int segs = Mathf.Max(2, Mathf.RoundToInt(len / 2.2f));
                    for (int s = 0; s < segs; s++)
                    {
                        float t = (s + 0.5f) / segs;
                        var pSeg = Instantiate(pipeStraight, cRoot.transform, false);
                        pSeg.name = $"Pipe_Seg_{s + 1}";
                        pSeg.transform.localPosition = Vector3.Lerp(cl.start, cl.end, t);
                        pSeg.transform.localRotation = rot;
                        pSeg.transform.localScale = new Vector3(1.1f, 1.1f, (len / segs) * 0.98f);
                        ApplyMetalMaterials(pSeg, darkTitaniumMat, steelAccentMat);
                    }
                }

                // Thanh lõi dẫn truyền năng lượng phát sáng tuần tự
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = "Conduit_Energy_Strip";
                strip.transform.SetParent(cRoot.transform, false);
                strip.transform.localPosition = mid + Vector3.up * 0.42f;
                strip.transform.localRotation = rot;
                strip.transform.localScale = new Vector3(0.22f, 0.14f, len * 0.92f);
                DestroyImmediate(strip.GetComponent<Collider>());
                var stripRend = strip.GetComponent<Renderer>();
                stripRend.sharedMaterial = blueCrystalMat;
                conduitStripRenderers.Add(stripRend);
            }

            // 3.4 CoreParticles & Pulse Ring
            GameObject coreParticlesGo = new GameObject("CoreParticles");
            coreParticlesGo.transform.SetParent(energyCore.transform, false);
            coreParticlesGo.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            ParticleSystem coreOrbitPs = BuildOrbitingCoreParticles(coreParticlesGo, blueCyanColor);

            GameObject pulseWaveGo = new GameObject("EnergyPulseWaveRing");
            pulseWaveGo.transform.SetParent(energyCore.transform, false);
            pulseWaveGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var pulseLine = pulseWaveGo.AddComponent<LineRenderer>();
            pulseLine.sharedMaterial = coreGlowMat;

            var coreController = energyCore.AddComponent<AsteroidEnergyCore>();
            coreController.coreCrystalRenderer = mainCoreCrystalRenderer;
            coreController.innerSphereRenderer = sphereRenderer;
            coreController.coreLight = coreLight;
            coreController.rotatingRings = ringTransforms.ToArray();
            coreController.conduitStripRenderers = conduitStripRenderers.ToArray();
            coreController.coreOrbitParticles = coreOrbitPs;
            coreController.pulseWaveRing = pulseLine;
            coreController.connectedCrystals = allLargeCrystals.ToArray();

            // ═════════════════════════════════════════════════════════════════
            // 4. MINING FACILITIES (Các Cơ Sở Công Nghiệp Tích Hợp Vào Đá)
            // ═════════════════════════════════════════════════════════════════
            GameObject miningFacilities = new GameObject("MiningFacilities");
            miningFacilities.transform.SetParent(root.transform, false);

            List<Light> facilityLights = new List<Light>();

            // 4.1 Mineral Extraction Platform West (Đặt trên West_Extraction_Plateau ở Y = 1.5m)
            GameObject westFacility = new GameObject("West_Extraction_Platform");
            westFacility.transform.SetParent(miningFacilities.transform, false);
            westFacility.transform.localPosition = new Vector3(-14.5f, 1.5f, -2.0f);
            westFacility.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            if (platformLarge != null)
            {
                var pad = Instantiate(platformLarge, westFacility.transform, false);
                pad.name = "Extraction_Deck";
                pad.transform.localPosition = Vector3.zero;
                pad.transform.localScale = new Vector3(3.6f, 2.0f, 3.6f);
                ApplyMetalMaterials(pad, darkTitaniumMat, steelAccentMat);
            }
            if (supportsHigh != null)
            {
                var sup1 = Instantiate(supportsHigh, westFacility.transform, false);
                sup1.transform.localPosition = new Vector3(-2.5f, -3.2f, -2.5f);
                sup1.transform.localScale = new Vector3(2.2f, 3.4f, 2.2f);
                var sup2 = Instantiate(supportsHigh, westFacility.transform, false);
                sup2.transform.localPosition = new Vector3(2.5f, -3.2f, 2.5f);
                sup2.transform.localScale = new Vector3(2.2f, 3.4f, 2.2f);
            }
            if (generatorLarge != null)
            {
                var gen = Instantiate(generatorLarge, westFacility.transform, false);
                gen.name = "Heavy_Mineral_Processor";
                gen.transform.localPosition = new Vector3(-1.2f, 0.2f, -0.8f);
                gen.transform.localScale = Vector3.one * 2.2f;
            }
            if (chimneyDetailed != null)
            {
                var stack1 = Instantiate(chimneyDetailed, westFacility.transform, false);
                stack1.transform.localPosition = new Vector3(-2.4f, 0.2f, 1.6f);
                stack1.transform.localScale = Vector3.one * 2.0f;
                var stack2 = Instantiate(chimneyDetailed, westFacility.transform, false);
                stack2.transform.localPosition = new Vector3(-1.0f, 0.2f, 1.6f);
                stack2.transform.localScale = Vector3.one * 2.0f;
            }
            facilityLights.Add(CreateFacilityBeaconLight(westFacility.transform, new Vector3(0f, 3.5f, 0f), blueCyanColor));

            // 4.2 Cargo Loading Station East (Đặt trên East_Cargo_Terrace ở Y = 0.0m)
            GameObject eastCargoStation = new GameObject("East_Cargo_Loading_Station");
            eastCargoStation.transform.SetParent(miningFacilities.transform, false);
            eastCargoStation.transform.localPosition = new Vector3(15.0f, 0.0f, -2.0f);
            eastCargoStation.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            if (platformLarge != null)
            {
                var mainPad = Instantiate(platformLarge, eastCargoStation.transform, false);
                mainPad.name = "Cargo_Dock_Deck";
                mainPad.transform.localPosition = Vector3.zero;
                mainPad.transform.localScale = new Vector3(4.5f, 2.0f, 4.5f);
                ApplyMetalMaterials(mainPad, darkTitaniumMat, steelAccentMat);
            }
            if (supportsHigh != null)
            {
                var gantryL = Instantiate(supportsHigh, eastCargoStation.transform, false);
                gantryL.name = "Cargo_Gantry_Left";
                gantryL.transform.localPosition = new Vector3(-3.2f, 0.2f, -1.8f);
                gantryL.transform.localScale = new Vector3(2.0f, 4.2f, 2.0f);

                var gantryR = Instantiate(supportsHigh, eastCargoStation.transform, false);
                gantryR.name = "Cargo_Gantry_Right";
                gantryR.transform.localPosition = new Vector3(3.2f, 0.2f, -1.8f);
                gantryR.transform.localScale = new Vector3(2.0f, 4.2f, 2.0f);
            }
            if (barrelLarge != null)
            {
                for (int b = 0; b < 3; b++)
                {
                    var container = Instantiate(barrelLarge, eastCargoStation.transform, false);
                    container.name = $"Mineral_Container_{b + 1}";
                    container.transform.localPosition = new Vector3(-2.6f + b * 1.3f, 0.2f, 2.0f);
                    container.transform.localScale = Vector3.one * 1.8f;
                }
            }

            List<Light> dockLoadingLights = new List<Light>();
            for (int l = 0; l < 4; l++)
            {
                float lx = (l < 2) ? -3.4f : 3.4f;
                float lz = (l % 2 == 0) ? -2.8f : 2.8f;
                var dLight = CreateFacilityBeaconLight(eastCargoStation.transform, new Vector3(lx, 1.2f, lz), blueCyanColor);
                dockLoadingLights.Add(dLight);
                facilityLights.Add(dLight);
            }

            // Thùng quặng bay lơ lửng phục vụ hoạt cảnh Interaction D (Cargo Transfer Animation)
            GameObject transferContainer = null;
            if (barrelLarge != null)
            {
                transferContainer = Instantiate(barrelLarge, eastCargoStation.transform, false);
                transferContainer.name = "Active_Transfer_Mineral_Pod";
                transferContainer.transform.localPosition = new Vector3(-2.2f, 1.4f, -1.0f);
                transferContainer.transform.localScale = Vector3.one * 1.6f;
            }
            GameObject transferBeamGo = new GameObject("Cargo_Gantry_Transfer_Beam");
            transferBeamGo.transform.SetParent(eastCargoStation.transform, false);
            transferBeamGo.transform.localPosition = new Vector3(0f, 4.2f, -1.8f);
            var transferLine = transferBeamGo.AddComponent<LineRenderer>();
            transferLine.useWorldSpace = true;
            transferLine.startWidth = 0.14f;
            transferLine.endWidth = 0.08f;
            transferLine.sharedMaterial = coreGlowMat;
            transferLine.enabled = false;

            // 4.3 Observation Tower North (Đặt trên North_High_Ridge_Massif ở Y = 3.5m)
            GameObject northTower = new GameObject("North_Observation_Tower");
            northTower.transform.SetParent(miningFacilities.transform, false);
            northTower.transform.localPosition = new Vector3(-2.0f, 3.5f, 12.0f);

            if (supportsHigh != null)
            {
                var baseTruss = Instantiate(supportsHigh, northTower.transform, false);
                baseTruss.name = "Tower_Truss_Base";
                baseTruss.transform.localPosition = Vector3.zero;
                baseTruss.transform.localScale = new Vector3(2.8f, 4.8f, 2.8f);
            }
            if (hangarRoundGlass != null)
            {
                var dome = Instantiate(hangarRoundGlass, northTower.transform, false);
                dome.name = "Observation_Command_Pod";
                dome.transform.localPosition = new Vector3(0f, 4.8f, 0f);
                dome.transform.localScale = Vector3.one * 1.4f;
            }
            if (satelliteDishLarge != null)
            {
                var dish = Instantiate(satelliteDishLarge, northTower.transform, false);
                dish.name = "Colony_Telemetry_Dish";
                dish.transform.localPosition = new Vector3(0f, 6.8f, 0f);
                dish.transform.localScale = Vector3.one * 1.8f;
                var rot = dish.AddComponent<CommDishRotator>();
                rot.rotateSpeed = 18f;
            }
            facilityLights.Add(CreateFacilityBeaconLight(northTower.transform, new Vector3(0f, 8.2f, 0f), purpleRareColor));

            // 4.4 Energy Transfer Substation South (Đặt trên South_Substation_Ledge ở Y = -0.5m)
            GameObject southSubstation = new GameObject("South_Energy_Transfer_Station");
            southSubstation.transform.SetParent(miningFacilities.transform, false);
            southSubstation.transform.localPosition = new Vector3(2.0f, -0.5f, -13.0f);

            if (platformLong != null)
            {
                var subPad = Instantiate(platformLong, southSubstation.transform, false);
                subPad.transform.localScale = new Vector3(3.2f, 2.0f, 3.2f);
                ApplyMetalMaterials(subPad, darkTitaniumMat, steelAccentMat);
            }
            if (generatorSmall != null)
            {
                var g1 = Instantiate(generatorSmall, southSubstation.transform, false);
                g1.transform.localPosition = new Vector3(-1.8f, 0.2f, 0f);
                g1.transform.localScale = Vector3.one * 2.0f;
                var g2 = Instantiate(generatorSmall, southSubstation.transform, false);
                g2.transform.localPosition = new Vector3(1.8f, 0.2f, 0f);
                g2.transform.localScale = Vector3.one * 2.0f;
            }
            if (wirelessMachine != null)
            {
                var relay = Instantiate(wirelessMachine, southSubstation.transform, false);
                relay.transform.localPosition = new Vector3(0f, 0.2f, -1.2f);
                relay.transform.localScale = Vector3.one * 2.2f;
            }
            facilityLights.Add(CreateFacilityBeaconLight(southSubstation.transform, new Vector3(0f, 3.0f, 0f), blueCyanColor));

            coreController.facilityStatusLights = facilityLights.ToArray();

            // ═════════════════════════════════════════════════════════════════
            // 5. MINING DRONES (3 Drone Khai Khoáng Tự Hành)
            // ═════════════════════════════════════════════════════════════════
            GameObject miningDronesGroup = new GameObject("MiningDrones");
            miningDronesGroup.transform.SetParent(root.transform, false);

            List<MiningDroneController> droneControllers = new List<MiningDroneController>();
            (string name, Vector3 startLocalPos, Transform homePlatform, int startCrystalIdx, float speed)[] droneConfigs = new[]
            {
                ("Mining_Drone_Alpha", new Vector3(-9f, 5.5f, 3f), westFacility.transform, 0, 7.5f),
                ("Mining_Drone_Beta", new Vector3(9f, 5.0f, 2f), eastCargoStation.transform, 2, 6.8f),
                ("Mining_Drone_Gamma", new Vector3(0f, 7.5f, -8f), southSubstation.transform, 4, 7.2f)
            };

            if (craftMiner != null)
            {
                for (int d = 0; d < droneConfigs.Length; d++)
                {
                    var cfg = droneConfigs[d];
                    var droneGo = Instantiate(craftMiner, miningDronesGroup.transform, false);
                    droneGo.name = cfg.name;
                    droneGo.transform.localPosition = cfg.startLocalPos;
                    droneGo.transform.localScale = Vector3.one * 0.9f;

                    // Tắt collider trên drone để không va chạm với người chơi
                    foreach (var c in droneGo.GetComponentsInChildren<Collider>(true)) c.enabled = false;

                    // Đèn định vị Cyan
                    var navLightGo = new GameObject("Drone_NavLight");
                    navLightGo.transform.SetParent(droneGo.transform, false);
                    navLightGo.transform.localPosition = new Vector3(0f, 0.6f, 0.5f);
                    var navLight = navLightGo.AddComponent<Light>();
                    navLight.type = LightType.Point;
                    navLight.color = blueCyanColor;
                    navLight.range = 8f;
                    navLight.intensity = 2.0f;

                    // Đầu bắn tia laser khai khoáng
                    var nozzleGo = new GameObject("Laser_Emitter_Nozzle");
                    nozzleGo.transform.SetParent(droneGo.transform, false);
                    nozzleGo.transform.localPosition = new Vector3(0f, 0.2f, 1.35f);

                    var laserLine = nozzleGo.AddComponent<LineRenderer>();
                    laserLine.useWorldSpace = true;
                    laserLine.positionCount = 2;
                    laserLine.sharedMaterial = coreGlowMat;
                    laserLine.enabled = false;

                    var tipLightGo = new GameObject("Laser_Contact_Light");
                    tipLightGo.transform.SetParent(droneGo.transform, false);
                    var tipLight = tipLightGo.AddComponent<Light>();
                    tipLight.type = LightType.Point;
                    tipLight.color = blueCyanColor;
                    tipLight.range = 6f;
                    tipLight.intensity = 2.5f;
                    tipLight.enabled = false;

                    var sparksPs = CreateExtractionSparks(droneGo.transform, blueCyanColor);

                    var dCtrl = droneGo.AddComponent<MiningDroneController>();
                    dCtrl.droneMovementSpeed = cfg.speed;
                    dCtrl.droneMiningDuration = 3.6f;
                    dCtrl.homeMiningPlatform = cfg.homePlatform;
                    dCtrl.startingCrystalIndex = cfg.startCrystalIdx;
                    dCtrl.laserNozzle = nozzleGo.transform;
                    dCtrl.miningLaser = laserLine;
                    dCtrl.laserTipLight = tipLight;
                    dCtrl.extractionSparks = sparksPs;
                    dCtrl.navigationLight = navLight;

                    foreach (var cr in allLargeCrystals)
                    {
                        dCtrl.targetCrystals.Add(cr.transform);
                    }

                    droneControllers.Add(dCtrl);
                }
            }

            coreController.nearbyDrones = droneControllers.ToArray();

            // ═════════════════════════════════════════════════════════════════
            // 6. CARGO TRAFFIC (3 Tàu Chở Hàng Bay Quỹ Đạo Quanh Thiên Thạch)
            // ═════════════════════════════════════════════════════════════════
            GameObject cargoTrafficGroup = new GameObject("CargoTraffic");
            cargoTrafficGroup.transform.SetParent(root.transform, false);

            List<CargoShipOrbitController> cargoControllers = new List<CargoShipOrbitController>();
            (string name, GameObject prefab, float radius, float ecc, float alt, float tilt, float speed, float phase, bool rev)[] cargoConfigs = new[]
            {
                ("Cargo_Freighter_01", craftCargoA, 42f, 1.22f, 6.5f, 12f, 13.5f, 0f, false),
                ("Cargo_Freighter_02", craftCargoA, 52f, 1.15f, -4.5f, -16f, 11.0f, 2.1f, true),
                ("Mineral_Transport_03", craftSpeederB != null ? craftSpeederB : craftCargoA, 46f, 1.28f, 14.0f, 22f, 16.0f, 4.2f, false)
            };

            for (int c = 0; c < cargoConfigs.Length; c++)
            {
                var cc = cargoConfigs[c];
                if (cc.prefab == null) continue;

                var shipGo = Instantiate(cc.prefab, cargoTrafficGroup.transform, false);
                shipGo.name = cc.name;
                shipGo.transform.localScale = Vector3.one * 1.45f;

                // Gắn thùng hàng khoáng sản nhìn thấy rõ trên tàu
                if (barrelLarge != null)
                {
                    var cargoPod = Instantiate(barrelLarge, shipGo.transform, false);
                    cargoPod.name = "Mineral_Cargo_Pod";
                    cargoPod.transform.localPosition = new Vector3(0f, 0.35f, -0.3f);
                    cargoPod.transform.localScale = Vector3.one * 0.65f;
                }

                // Đèn phản lực động cơ & đèn định vị
                var exhaustGo = new GameObject("Engine_Exhaust_Glow");
                exhaustGo.transform.SetParent(shipGo.transform, false);
                exhaustGo.transform.localPosition = new Vector3(0f, 0.35f, -1.4f);
                var exLight = exhaustGo.AddComponent<Light>();
                exLight.type = LightType.Point;
                exLight.color = blueCyanColor;
                exLight.range = 10f;
                exLight.intensity = 2.8f;

                var navGo = new GameObject("Ship_Nav_Beacon");
                navGo.transform.SetParent(shipGo.transform, false);
                navGo.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
                var navLight = navGo.AddComponent<Light>();
                navLight.type = LightType.Point;
                navLight.color = purpleRareColor;
                navLight.range = 8f;
                navLight.intensity = 1.8f;

                var orbitCtrl = shipGo.AddComponent<CargoShipOrbitController>();
                orbitCtrl.orbitCenterTransform = root.transform;
                orbitCtrl.cargoOrbitRadius = cc.radius;
                orbitCtrl.orbitEccentricity = cc.ecc;
                orbitCtrl.altitudeOffset = cc.alt;
                orbitCtrl.orbitTiltDegrees = cc.tilt;
                orbitCtrl.cargoShipMovementSpeed = cc.speed;
                orbitCtrl.startingPhaseRadians = cc.phase;
                orbitCtrl.reverseDirection = cc.rev;
                orbitCtrl.engineExhaustLight = exLight;
                orbitCtrl.navBeaconLight = navLight;

                cargoControllers.Add(orbitCtrl);
            }

            // ═════════════════════════════════════════════════════════════════
            // 7. FLOATING DEBRIS (Các Tảng Đá Nhỏ Trôi Nổi Quanh Thiên Thạch)
            // ═════════════════════════════════════════════════════════════════
            GameObject floatingDebris = new GameObject("FloatingDebris");
            floatingDebris.transform.SetParent(root.transform, false);

            (Vector3 pos, Vector3 rot, float scale)[] debrisList = new[]
            {
                (new Vector3(-28f, 8f, 14f), new Vector3(15f, 40f, 25f), 4.2f),
                (new Vector3(-32f, -6f, -12f), new Vector3(-25f, 110f, 15f), 3.6f),
                (new Vector3(29f, 10f, 12f), new Vector3(30f, 215f, -20f), 4.5f),
                (new Vector3(33f, -8f, -14f), new Vector3(-18f, 305f, 35f), 3.8f),
                (new Vector3(-16f, 18f, 24f), new Vector3(40f, 80f, -15f), 3.2f),
                (new Vector3(18f, 16f, -24f), new Vector3(-30f, 165f, 20f), 3.5f),
                (new Vector3(0f, -24f, 18f), new Vector3(22f, 250f, -30f), 4.8f),
                (new Vector3(-20f, -20f, -18f), new Vector3(-15f, 35f, 45f), 4.0f),
                (new Vector3(24f, 4f, 24f), new Vector3(12f, 140f, -25f), 3.0f),
                (new Vector3(-25f, 12f, -22f), new Vector3(-20f, 290f, 10f), 3.4f),
                (new Vector3(8f, 22f, 18f), new Vector3(35f, 60f, 15f), 2.8f),
                (new Vector3(-8f, -26f, -15f), new Vector3(-28f, 190f, -35f), 3.6f)
            };

            for (int i = 0; i < debrisList.Length; i++)
            {
                var dd = debrisList[i];
                GameObject dPrefab = (i % 3 == 0) ? meteorDetailed : ((i % 3 == 1) ? meteor : rockLargeB);
                if (dPrefab == null) continue;

                var dGo = Instantiate(dPrefab, floatingDebris.transform, false);
                dGo.name = $"Floating_Asteroid_Debris_{i + 1:D2}";
                dGo.transform.localPosition = dd.pos;
                dGo.transform.localRotation = Quaternion.Euler(dd.rot);
                dGo.transform.localScale = Vector3.one * dd.scale;
                ApplyRockMaterials(dGo, rockDarkMat, rockSlateMat);

                // Gắn thêm mầm tinh thể phát quang trên một nửa số đá thiên thạch trôi nổi
                if (i % 2 == 0 && crystalSmall != null)
                {
                    var shard = Instantiate(crystalSmall, dGo.transform, false);
                    shard.name = "Embedded_Crystal_Facet";
                    shard.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                    shard.transform.localRotation = Quaternion.Euler(20f, (i * 37) % 360, 15f);
                    shard.transform.localScale = Vector3.one * 0.45f;
                    ApplyGemstoneMaterials(shard, rockDarkMat, rockSlateMat, (i % 4 == 0) ? blueCrystalMat : purpleCrystalMat);
                }
            }

            // ═════════════════════════════════════════════════════════════════
            // 8. INTERACTIVE ELEMENTS (Hidden Mineral Deposit & Cargo Observation)
            // ═════════════════════════════════════════════════════════════════
            GameObject interactiveElements = new GameObject("InteractiveElements");
            interactiveElements.transform.SetParent(root.transform, false);

            // Interaction C: Hidden Mineral Deposit (Đặt trên thềm hang ngầm Hidden_Grotto_Ledge ở (-12, -8.5, -10))
            GameObject hiddenDepositRoot = new GameObject("HiddenMineralDeposit_InteractionC");
            hiddenDepositRoot.transform.SetParent(interactiveElements.transform, false);
            hiddenDepositRoot.transform.localPosition = new Vector3(-12.0f, -8.5f, -10.0f);

            Renderer hiddenCrystalRend = null;
            if (crystalLargeB != null)
            {
                var rareCluster = Instantiate(crystalLargeB, hiddenDepositRoot.transform, false);
                rareCluster.name = "Hidden_Rare_Crystal_Cluster";
                rareCluster.transform.localPosition = Vector3.zero;
                rareCluster.transform.localRotation = Quaternion.Euler(8f, 45f, -6f);
                rareCluster.transform.localScale = Vector3.one * 4.2f;
                ApplyGemstoneMaterials(rareCluster, rockDarkMat, rockSlateMat, hiddenMineralMat);
                hiddenCrystalRend = rareCluster.GetComponentInChildren<Renderer>();
            }

            var hiddenLightGo = new GameObject("Hidden_Mineral_Light");
            hiddenLightGo.transform.SetParent(hiddenDepositRoot.transform, false);
            hiddenLightGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var hiddenLight = hiddenLightGo.AddComponent<Light>();
            hiddenLight.type = LightType.Point;
            hiddenLight.color = hiddenMineralColor;
            hiddenLight.range = 10f;
            hiddenLight.intensity = 1.6f;

            var hiddenSparks = CreateCrystalShimmerParticles(hiddenDepositRoot.transform, hiddenMineralColor);

            // Bảng thông báo Hologram nhỏ gọn: "RARE MINERAL DISCOVERED"
            var holoBannerRoot = new GameObject("Rare_Mineral_Hologram_Notification");
            holoBannerRoot.transform.SetParent(hiddenDepositRoot.transform, false);
            holoBannerRoot.transform.localPosition = new Vector3(0f, 3.6f, 0f);

            var holoTextGo = new GameObject("Notification_Text");
            holoTextGo.transform.SetParent(holoBannerRoot.transform, false);
            holoTextGo.transform.localPosition = Vector3.zero;
            var tmpHolo = holoTextGo.AddComponent<TextMeshPro>();
            tmpHolo.fontSize = 4.2f;
            tmpHolo.fontStyle = FontStyles.Bold;
            tmpHolo.alignment = TextAlignmentOptions.Center;
            tmpHolo.color = new Color(0.25f, 0.95f, 1f, 1f);
            tmpHolo.text = "RARE MINERAL DISCOVERED";

            var hiddenDiscovery = hiddenDepositRoot.AddComponent<HiddenMineralDiscovery>();
            hiddenDiscovery.playerInteractionRadius = 16f;
            hiddenDiscovery.mineralRenderer = hiddenCrystalRend;
            hiddenDiscovery.mineralLight = hiddenLight;
            hiddenDiscovery.hologramRoot = holoBannerRoot.transform;
            hiddenDiscovery.discoveryHoloText = tmpHolo;
            hiddenDiscovery.sparkleParticles = hiddenSparks;
            hiddenDiscovery.rareMineralColor = hiddenMineralColor;

            // Interaction D: Cargo Ship Observation (Gắn tại vị trí East_Cargo_Loading_Station)
            GameObject cargoObsRoot = new GameObject("CargoObservation_InteractionD");
            cargoObsRoot.transform.SetParent(interactiveElements.transform, false);
            cargoObsRoot.transform.localPosition = eastCargoStation.transform.localPosition;

            var cargoInteraction = cargoObsRoot.AddComponent<AsteroidInteraction>();
            cargoInteraction.playerInteractionRadius = 24f;
            cargoInteraction.loadingPlatformLights = dockLoadingLights.ToArray();
            cargoInteraction.nearbyCargoShips = cargoControllers.ToArray();
            if (transferContainer != null) cargoInteraction.cargoContainershuttle = transferContainer.transform;
            cargoInteraction.cargoTransferBeam = transferLine;
            cargoInteraction.transferBeamAnchor = transferBeamGo.transform;

            // ═════════════════════════════════════════════════════════════════
            // 9. ENVIRONMENT VFX (Hiệu Ứng Khí Quyển & Bụi Tinh Thể Tinh Tế)
            // ═════════════════════════════════════════════════════════════════
            GameObject environmentVfx = new GameObject("EnvironmentVFX");
            environmentVfx.transform.SetParent(root.transform, false);
            BuildAmbientCrystalDustVFX(environmentVfx.transform, blueCyanColor, purpleRareColor);

            // ═════════════════════════════════════════════════════════════════
            // 10. ORBITAL SURFACE MINING RAILWAY SYSTEM (Hệ Thống Đường Sắt Mỏ Tinh Thể)
            // ═════════════════════════════════════════════════════════════════
            MiningRailwayBuilder.BuildRailwaySystem(root);

            return root;
        }

        private static void AssignClosestDeposits(CrystalResonance parentCrystal, List<CrystalResonance> deposits, int count)
        {
            if (deposits == null || deposits.Count == 0) return;
            deposits.Sort((a, b) =>
                Vector3.SqrMagnitude(a.transform.position - parentCrystal.transform.position)
                .CompareTo(Vector3.SqrMagnitude(b.transform.position - parentCrystal.transform.position)));
            for (int i = 0; i < Mathf.Min(count, deposits.Count); i++)
            {
                parentCrystal.nearbySmallDeposits.Add(deposits[i]);
            }
        }

        private static void BuildThinMechanicalGimbalRing(Transform ringParent, float radius, GameObject segmentPrefab, Material metalMat, Material glowMat)
        {
            int segments = 12;
            float arcLength = (2f * Mathf.PI * radius) / segments;
            for (int i = 0; i < segments; i++)
            {
                float angleRad = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(angleRad) * radius, 0f, Mathf.Sin(angleRad) * radius);
                Vector3 tangent = new Vector3(-Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));

                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"Ring_Segment_{i + 1}";
                seg.transform.SetParent(ringParent, false);
                seg.transform.localPosition = pos;
                seg.transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up);
                seg.transform.localScale = new Vector3(0.24f, 0.16f, arcLength * 0.96f);
                DestroyImmediate(seg.GetComponent<Collider>());

                var rend = seg.GetComponent<Renderer>();
                rend.sharedMaterial = (i % 3 == 0) ? glowMat : metalMat;
            }
        }

        private static Light CreateFacilityBeaconLight(Transform parent, Vector3 localPos, Color col)
        {
            var go = new GameObject("Facility_Beacon_Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = 14f;
            l.intensity = 2.2f;
            return l;
        }

        private static ParticleSystem CreateCrystalShimmerParticles(Transform parent, Color color)
        {
            var go = new GameObject("Crystal_Shimmer_VFX");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.8f;
            main.startSpeed = 0.45f;
            main.startSize = 0.12f;
            main.startColor = new Color(color.r, color.g, color.b, 0.85f);
            main.maxParticles = 35;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 4f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;

            AssignDefaultParticleMaterial(ps, color);
            return ps;
        }

        private static ParticleSystem CreateExtractionSparks(Transform parent, Color color)
        {
            var go = new GameObject("Mining_Extraction_Sparks");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = 0.55f;
            main.startSpeed = 2.8f;
            main.startSize = 0.14f;
            main.startColor = color;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            AssignDefaultParticleMaterial(ps, color);
            return ps;
        }

        private static ParticleSystem BuildOrbitingCoreParticles(GameObject go, Color color)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.8f;
            main.startSize = 0.22f;
            main.startColor = color;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var em = ps.emission;
            em.rateOverTime = 22f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 4.8f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalY = 1.2f;

            AssignDefaultParticleMaterial(ps, color);
            return ps;
        }

        private static void BuildAmbientCrystalDustVFX(Transform parent, Color cyan, Color purple)
        {
            var go = new GameObject("Ambient_Colony_Spore_Dust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 4f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 6.0f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(cyan, purple);
            main.maxParticles = 90;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 14f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 18f, 40f);

            AssignDefaultParticleMaterial(ps, cyan);
        }

        private static void AssignDefaultParticleMaterial(ParticleSystem ps, Color col)
        {
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr == null) return;
#if UNITY_EDITOR
            Material existingTrailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Mat_Traffic_Trail.mat");
            if (existingTrailMat != null)
            {
                psr.sharedMaterial = existingTrailMat;
                return;
            }
#endif
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (sh != null)
            {
                var mat = new Material(sh);
                mat.color = col;
                psr.sharedMaterial = mat;
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

        /// <summary>
        /// Gán vật liệu chuẩn cho mô hình tinh thể Kenney (3 submeshes: [0] rockTrack, [1] rock, [2] crystal).
        /// Tuyệt đối giữ bệ đá [0], [1] màu đá đen tối và chỉ phát sáng trên đúng submesh tinh thể [2]!
        /// </summary>
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

        private static Material LoadOrCreateRockMaterial(string assetPath, Color color, float metallic, float smoothness)
        {
#if UNITY_EDITOR
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;
#endif
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(mat, assetPath);
#endif
            return mat;
        }

        private static Material LoadOrCreateEmissiveMaterial(string assetPath, Color glowColor, float emissionMultiplier)
        {
#if UNITY_EDITOR
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                existing.color = glowColor;
                existing.SetColor("_BaseColor", glowColor);
                existing.EnableKeyword("_EMISSION");
                existing.SetColor("_EmissionColor", glowColor * emissionMultiplier);
                existing.SetFloat("_Smoothness", 0.92f);
                existing.SetFloat("_Metallic", 0.15f);
                EditorUtility.SetDirty(existing);
                return existing;
            }
#endif
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = glowColor;
            mat.SetColor("_BaseColor", glowColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * emissionMultiplier);
            mat.SetFloat("_Smoothness", 0.92f);
            mat.SetFloat("_Metallic", 0.15f);
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(mat, assetPath);
#endif
            return mat;
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
