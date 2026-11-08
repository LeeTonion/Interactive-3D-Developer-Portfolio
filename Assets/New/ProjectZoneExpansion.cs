using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceCyberpunk
{
    /// <summary>
    /// Bộ Mở Rộng To Toàn Diện Vùng Đất Project Zone (Project Zone Mega Expansion).
    /// - Không sử dụng bất kỳ Primitive Sphere đơn điệu nào.
    /// - Tích hợp trực tiếp vào hệ thống hòn đảo modular đá tự nhiên có sẵn (02_Floating_Islands_Organic & 03_Cyberpunk_Outposts).
    /// - Hệ Thống Chi Tiết Hóa Bề Thế:
    ///   1. Trạm Tiếp Sóng Vệ Tinh Vũ Trụ (Comm-Relay Array + Laser Uplink 350m + Giàn máy chủ dữ liệu viễn thông + Đèn Strobe Beacon).
    ///   2. Trạm Khai Thác Năng Lượng Tinh Thể (Crystal Refinery + Plasma Mining Extraction Laser + Máy phát + Ống dẫn + Thùng chứa tinh thể).
    ///   3. Tuyến Đường Đua Cổng Tăng Tốc Siêu Không Gian (5 Hyper-Drive Speed Rings).
    ///   4. Tàu Đệm Từ Không Gian Tuần Tra (Floating Maglev Monorail 3 Toa + Đèn pha SpotLight + Đèn đuôi).
    ///   5. Trạm Cấp Năng Lượng & Sửa Chữa Phi Thuyền (Spaceship Quantum Recharge Station + Tia nạp năng lượng + Hồi phục khiên máu).
    ///   6. Mạng Lưới Thiên Thạch Phức Tạp (Complex Asteroid Field: Hẻm núi thiên thạch, vành đai tầng cao & vực thẳm tầng đáy).
    /// </summary>
    [ExecuteAlways]
    public class ProjectZoneExpansion : MonoBehaviour
    {
        private const string OutpostRootName = "Project_Zone_Expanded_Outposts";

        private void Awake()
        {
            EnsureExpandedContent();
        }

        private void Start()
        {
            EnsureExpandedContent();
        }

        [ContextMenu("Rebuild Project Zone Expansion")]
        public void EnsureExpandedContent()
        {
            // 1. Nâng cấp toàn bộ các biển quảng cáo hiện có thành CyberpunkVideoBillboard
            UpgradeExistingBillboards();

            // 2. Kiểm tra nếu đã có phân khu mở rộng thì dọn để tạo mới đầy đủ chi tiết
            GameObject existingRoot = GameObject.Find(OutpostRootName);
            if (existingRoot != null)
            {
                if (Application.isPlaying) Destroy(existingRoot);
                else DestroyImmediate(existingRoot);
            }

            Transform pzParent = transform;
            var pzGo = GameObject.Find("Project_Zone");
            if (pzGo != null) pzParent = pzGo.transform;

            GameObject root = new GameObject(OutpostRootName);
            root.transform.SetParent(pzParent, false);

            // Xây dựng các hệ thống chi tiết hoành tráng
            BuildCommRelayStation(root.transform);
            BuildCrystalRefineryStation(root.transform);
            BuildMaglevPatrolTrainSystem(root.transform);
            BuildSpaceshipRechargeStation(root.transform);
            BuildComplexAsteroidField(root.transform);
        }

        public static void UpgradeExistingBillboards()
        {
            var allScreens = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var mr in allScreens)
            {
                if (mr.gameObject.name.IndexOf("big_screen", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var oldDisp = mr.GetComponent<CyberpunkBillboardDisplay>();
                    if (oldDisp != null)
                    {
                        if (Application.isPlaying) Destroy(oldDisp);
                        else DestroyImmediate(oldDisp);
                    }

                    if (mr.GetComponent<CyberpunkVideoBillboard>() == null)
                    {
                        var videoBb = mr.gameObject.AddComponent<CyberpunkVideoBillboard>();
                        videoBb.billboardNetworkName = "MEGA_SCREEN // NEO_CYBER_REEL";
                        videoBb.clipDuration = 8.0f;
                        videoBb.BuildBillboardFaces();
                    }
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHÂN KHU 1: TRẠM TIẾP SÓNG VỆ TINH VŨ TRỤ (Chi Tiết Hóa Cao Cấp)
        // ═════════════════════════════════════════════════════════════════════════
        private void BuildCommRelayStation(Transform parent)
        {
            GameObject outpostGo = new GameObject("Outpost_01_Comm_Relay");
            outpostGo.transform.SetParent(parent, false);
            outpostGo.transform.position = new Vector3(70.0f, 26.0f, 45.0f);

            // Chảo Vệ Tinh Khổng Lồ xoay 360 độ
            var existingDish = GameObject.Find("satelliteDish_large(Clone)");
            Transform dishTransform = null;
            if (existingDish != null)
            {
                dishTransform = existingDish.transform;
            }
            else
            {
                var dishPrefab = LoadModel("satelliteDish_large.fbx");
                if (dishPrefab != null)
                {
                    var dish = Instantiate(dishPrefab, outpostGo.transform, false);
                    dish.name = "Giant_Satellite_Dish";
                    dish.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                    dish.transform.localScale = Vector3.one * 1.6f;
                    dishTransform = dish.transform;
                }
            }

            if (dishTransform != null)
            {
                var rotator = dishTransform.GetComponent<CommDishRotator>();
                if (rotator == null) rotator = dishTransform.gameObject.AddComponent<CommDishRotator>();
                rotator.rotateSpeed = 15f;
            }

            // Cột Laser Viễn Thông Cao Áp 350m
            GameObject laserGo = new GameObject("Deep_Space_Laser_Uplink");
            laserGo.transform.SetParent(outpostGo.transform, false);
            laserGo.transform.localPosition = new Vector3(0f, 4.0f, 0f);

            var line = laserGo.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, new Vector3(0f, 350f, 0f));
            line.startWidth = 0.65f;
            line.endWidth = 2.0f;

            var laserMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            laserMat.color = new Color(0f, 0.95f, 1f, 0.95f);
            laserMat.EnableKeyword("_EMISSION");
            laserMat.SetColor("_EmissionColor", new Color(0f, 4.0f, 6.0f));
            line.material = laserMat;

            // Đèn chớp cảnh báo hàng không Strobe
            var beaconGo = new GameObject("Strobe_Aviation_Beacon");
            beaconGo.transform.SetParent(outpostGo.transform, false);
            beaconGo.transform.localPosition = new Vector3(0f, 6.5f, 0f);
            var beaconLight = beaconGo.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.range = 40f;
            beaconLight.color = new Color(0f, 0.95f, 1f);
            beaconLight.intensity = 4.5f;
            if (dishTransform != null)
            {
                var rot = dishTransform.GetComponent<CommDishRotator>();
                if (rot != null) rot.beaconLight = beaconLight;
            }

            // Chi tiết bổ sung: Máy chủ viễn thông & Thùng pin dữ liệu
            var wirelessPrefab = LoadModel("machine_wireless.fbx");
            var cablePrefab = LoadModel("machine_wirelessCable.fbx");
            var barrelPrefab = LoadModel("machine_barrel.fbx");

            if (wirelessPrefab != null)
            {
                var server1 = Instantiate(wirelessPrefab, outpostGo.transform, false);
                server1.name = "Comm_Server_Rack_A";
                server1.transform.localPosition = new Vector3(2.5f, 0.8f, -2.5f);
                server1.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);

                var server2 = Instantiate(wirelessPrefab, outpostGo.transform, false);
                server2.name = "Comm_Server_Rack_B";
                server2.transform.localPosition = new Vector3(-2.8f, 0.8f, 2.5f);
                server2.transform.localRotation = Quaternion.Euler(0f, 135f, 0f);
            }

            if (cablePrefab != null)
            {
                var cable = Instantiate(cablePrefab, outpostGo.transform, false);
                cable.name = "Data_Transmission_Conduit";
                cable.transform.localPosition = new Vector3(1.2f, 0.6f, -1.2f);
                cable.transform.localScale = Vector3.one * 1.2f;
            }

            if (barrelPrefab != null)
            {
                var barrel1 = Instantiate(barrelPrefab, outpostGo.transform, false);
                barrel1.name = "Coolant_Cell_1";
                barrel1.transform.localPosition = new Vector3(3.8f, 0.6f, -1.8f);

                var barrel2 = Instantiate(barrelPrefab, outpostGo.transform, false);
                barrel2.name = "Coolant_Cell_2";
                barrel2.transform.localPosition = new Vector3(4.3f, 0.6f, -1.3f);
            }

            // Mega Billboard #2
            SpawnSecondaryBillboard(outpostGo.transform, new Vector3(-5.5f, 1.5f, -3.5f), Quaternion.Euler(0f, 45f, 0f), "COMM_RELAY // DEEP_SPACE");
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHÂN KHU 2: TRẠM KHAI THÁC TINH THỂ PHA LÊ (Chi Tiết Hóa Bề Thế)
        // ═════════════════════════════════════════════════════════════════════════
        private void BuildCrystalRefineryStation(Transform parent)
        {
            GameObject outpostGo = new GameObject("Outpost_02_Crystal_Refinery");
            outpostGo.transform.SetParent(parent, false);
            outpostGo.transform.position = new Vector3(55.0f, -15.0f, -20.0f);

            var crystalPrefabA = LoadModel("rock_crystalsLargeA.fbx");
            var crystalPrefabB = LoadModel("rock_crystalsLargeB.fbx");
            var generatorPrefab = LoadModel("machine_generatorLarge.fbx");
            var barrelLargePrefab = LoadModel("machine_barrelLarge.fbx");
            var pipeStraightPrefab = LoadModel("pipe_straight.fbx");

            GameObject mainCrystal = null;
            if (crystalPrefabA != null)
            {
                mainCrystal = Instantiate(crystalPrefabA, outpostGo.transform, false);
                mainCrystal.name = "PhaLe_Extraction_Core_A";
                mainCrystal.transform.localPosition = new Vector3(0f, 0.5f, 3.0f);
                mainCrystal.transform.localScale = Vector3.one * 1.6f;
                ApplyCrystalEmission(mainCrystal, new Color(0.15f, 0.85f, 1f));
            }

            if (crystalPrefabB != null)
            {
                var crystalB = Instantiate(crystalPrefabB, outpostGo.transform, false);
                crystalB.name = "PhaLe_Cluster_B";
                crystalB.transform.localPosition = new Vector3(3.5f, 0.2f, 1.5f);
                crystalB.transform.localScale = Vector3.one * 1.3f;
                ApplyCrystalEmission(crystalB, new Color(0.95f, 0.2f, 0.75f));
            }

            if (generatorPrefab != null)
            {
                var gen = Instantiate(generatorPrefab, outpostGo.transform, false);
                gen.name = "Industrial_Power_Generator";
                gen.transform.localPosition = new Vector3(-4.0f, 0.2f, -1.0f);
                gen.transform.localRotation = Quaternion.Euler(0f, 65f, 0f);
                gen.transform.localScale = Vector3.one * 1.3f;
            }

            // Chi tiết bổ sung: Hệ thống ống năng lượng plasma nối máy phát vào mỏ pha lê
            if (pipeStraightPrefab != null)
            {
                var pipe1 = Instantiate(pipeStraightPrefab, outpostGo.transform, false);
                pipe1.name = "Plasma_Conduit_Pipe_1";
                pipe1.transform.localPosition = new Vector3(-1.8f, 0.5f, 0.8f);
                pipe1.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
                pipe1.transform.localScale = new Vector3(0.8f, 0.8f, 2.2f);
            }

            // Thùng chứa năng lượng tinh thể đã tinh chế
            if (barrelLargePrefab != null)
            {
                var tank1 = Instantiate(barrelLargePrefab, outpostGo.transform, false);
                tank1.name = "Refined_Crystal_Tank_1";
                tank1.transform.localPosition = new Vector3(-4.2f, 0.2f, 2.5f);

                var tank2 = Instantiate(barrelLargePrefab, outpostGo.transform, false);
                tank2.name = "Refined_Crystal_Tank_2";
                tank2.transform.localPosition = new Vector3(-3.2f, 0.2f, 3.2f);
            }

            // Tia Laser Khai Khoáng
            if (mainCrystal != null)
            {
                GameObject laserRig = new GameObject("Mining_Extraction_Laser_Rig");
                laserRig.transform.SetParent(outpostGo.transform, false);
                laserRig.transform.localPosition = new Vector3(-3.5f, 2.2f, -0.8f);

                var line = laserRig.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.18f;
                line.endWidth = 0.09f;

                var plasmaMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                plasmaMat.color = new Color(1f, 0.55f, 0.1f, 0.95f);
                plasmaMat.EnableKeyword("_EMISSION");
                plasmaMat.SetColor("_EmissionColor", new Color(5.0f, 2.0f, 0.25f));
                line.material = plasmaMat;

                var contactLightGo = new GameObject("Plasma_Impact_Point_Light");
                contactLightGo.transform.SetParent(mainCrystal.transform, false);
                contactLightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                var contactLight = contactLightGo.AddComponent<Light>();
                contactLight.type = LightType.Point;
                contactLight.range = 12f;
                contactLight.color = new Color(1f, 0.6f, 0.15f);
                contactLight.intensity = 4.5f;

                var miningScript = laserRig.AddComponent<MiningExtractionLaser>();
                miningScript.laserEmitterOrigin = laserRig.transform;
                miningScript.crystalTarget = contactLightGo.transform;
                miningScript.laserBeam = line;
                miningScript.contactPointLight = contactLight;
            }

            // Mega Billboard #3
            SpawnSecondaryBillboard(outpostGo.transform, new Vector3(-4.5f, 1.2f, 4.5f), Quaternion.Euler(0f, 135f, 0f), "CRYSTAL_REFINERY // ENERGY");
        }

        private void ApplyCrystalEmission(GameObject crystalObj, Color emissionCol)
        {
            Material rockDark = LoadMaterialAsset("Assets/New/Materials/Mat_Asteroid_RockDark.mat");
            Material rockSlate = LoadMaterialAsset("Assets/New/Materials/Mat_Asteroid_Slate.mat");
            var renderers = crystalObj.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = emissionCol;
                mat.SetColor("_BaseColor", emissionCol);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionCol * 2.2f);
                mat.SetFloat("_Smoothness", 0.9f);

                var shared = r.sharedMaterials;
                if (shared != null && shared.Length >= 3)
                {
                    if (rockDark != null) shared[0] = rockDark;
                    if (rockSlate != null) shared[1] = rockSlate;
                    shared[2] = mat;
                    r.sharedMaterials = shared;
                }
                else
                {
                    r.sharedMaterial = mat;
                }
            }
        }

        private static Material LoadMaterialAsset(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
            return null;
#endif
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHÂN KHU 3: TÀU ĐỆM TỪ KHÔNG GIAN TUẦN TRA (MAGLEV MONORAIL TRAIN)
        // ═════════════════════════════════════════════════════════════════════════
        private void BuildMaglevPatrolTrainSystem(Transform parent)
        {
            GameObject trainSystemRoot = new GameObject("Maglev_Monorail_Patrol_Train");
            trainSystemRoot.transform.SetParent(parent, false);

            var trainFrontPrefab = LoadModel("monorail_trainFront.fbx");
            var trainCargoPrefab = LoadModel("monorail_trainCargo.fbx");
            var trainEndPrefab = LoadModel("monorail_trainEnd.fbx");

            if (trainFrontPrefab == null || trainCargoPrefab == null || trainEndPrefab == null) return;

            // 1. Đầu tàu
            var head = Instantiate(trainFrontPrefab, trainSystemRoot.transform, false);
            head.name = "Maglev_Train_Head";
            head.transform.position = new Vector3(-60f, -18f, -10f);
            head.transform.localScale = Vector3.one * 1.4f;

            // Đèn pha SpotLight quét đường ray
            var headlightGo = new GameObject("Train_Headlight");
            headlightGo.transform.SetParent(head.transform, false);
            headlightGo.transform.localPosition = new Vector3(0f, 0.6f, 1.8f);
            var hl = headlightGo.AddComponent<Light>();
            hl.type = LightType.Spot;
            hl.range = 30f;
            hl.spotAngle = 60f;
            hl.color = new Color(0.2f, 0.95f, 1f);
            hl.intensity = 5.5f;

            // 2. Toa hàng giữa
            var cargo = Instantiate(trainCargoPrefab, trainSystemRoot.transform, false);
            cargo.name = "Maglev_Train_Cargo";
            cargo.transform.position = new Vector3(-63.5f, -18f, -10f);
            cargo.transform.localScale = Vector3.one * 1.4f;

            // 3. Toa đuôi
            var end = Instantiate(trainEndPrefab, trainSystemRoot.transform, false);
            end.name = "Maglev_Train_End";
            end.transform.position = new Vector3(-67.0f, -18f, -10f);
            end.transform.localScale = Vector3.one * 1.4f;

            // Đèn đuôi đỏ rực
            var tailLightGo = new GameObject("Train_TailLight");
            tailLightGo.transform.SetParent(end.transform, false);
            tailLightGo.transform.localPosition = new Vector3(0f, 0.5f, -1.8f);
            var tl = tailLightGo.AddComponent<Light>();
            tl.type = LightType.Point;
            tl.range = 14f;
            tl.color = new Color(1f, 0.08f, 0.12f);
            tl.intensity = 4.0f;

            var trainScript = trainSystemRoot.AddComponent<MaglevPatrolTrain>();
            trainScript.engineHead = head.transform;
            trainScript.cargoCar = cargo.transform;
            trainScript.cabooseCar = end.transform;
            trainScript.moveSpeed = 9.0f;
            trainScript.carSpacing = 1.85f;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHÂN KHU 5: TRẠM CẤP NĂNG LƯỢNG CHO TÀU (SPACESHIP RECHARGE DOCK)
        // ═════════════════════════════════════════════════════════════════════════
        private void BuildSpaceshipRechargeStation(Transform parent)
        {
            GameObject stationGo = new GameObject("Outpost_05_Spaceship_Recharge_Dock");
            stationGo.transform.SetParent(parent, false);
            stationGo.transform.position = new Vector3(-135.0f, 24.0f, 85.0f); // Tọa độ chiến lược giữa hành lang bay

            // 1. Vòng cảm ứng từ trường nạp năng lượng (Quantum Induction Ring)
            var ringPrefab = LoadModel("pipe_ringHigh.fbx");
            if (ringPrefab == null) ringPrefab = LoadModel("pipe_ring.fbx");
            if (ringPrefab != null)
            {
                var ring = Instantiate(ringPrefab, stationGo.transform, false);
                ring.name = "Quantum_Induction_Ring";
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                ring.transform.localScale = Vector3.one * 2.8f;

                ApplyCrystalEmission(ring, new Color(0.1f, 1f, 0.5f));
            }

            // 2. Hai Trụ Năng Lượng Plasma Hai Bên (Recharge Pylons)
            var genPrefab = LoadModel("machine_generator.fbx");
            var crystalPrefab = LoadModel("rock_crystals.fbx");

            GameObject pylonLeft = new GameObject("Recharge_Pylon_Left");
            pylonLeft.transform.SetParent(stationGo.transform, false);
            pylonLeft.transform.localPosition = new Vector3(-6.5f, -1.0f, 0f);

            GameObject pylonRight = new GameObject("Recharge_Pylon_Right");
            pylonRight.transform.SetParent(stationGo.transform, false);
            pylonRight.transform.localPosition = new Vector3(6.5f, -1.0f, 0f);

            if (genPrefab != null)
            {
                var genL = Instantiate(genPrefab, pylonLeft.transform, false);
                genL.transform.localScale = Vector3.one * 1.5f;

                var genR = Instantiate(genPrefab, pylonRight.transform, false);
                genR.transform.localScale = Vector3.one * 1.5f;
            }

            if (crystalPrefab != null)
            {
                var cryL = Instantiate(crystalPrefab, pylonLeft.transform, false);
                cryL.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                cryL.transform.localScale = Vector3.one * 1.3f;
                ApplyCrystalEmission(cryL, new Color(0.2f, 1f, 0.6f));

                var cryR = Instantiate(crystalPrefab, pylonRight.transform, false);
                cryR.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                cryR.transform.localScale = Vector3.one * 1.3f;
                ApplyCrystalEmission(cryR, new Color(0.2f, 1f, 0.6f));
            }

            // 3. Hai Chùm Tia Laser Sạc Năng Lượng (Energy Tether Beams)
            var beamMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            beamMat.color = new Color(0.1f, 1f, 0.5f, 0.95f);
            beamMat.EnableKeyword("_EMISSION");
            beamMat.SetColor("_EmissionColor", new Color(0.5f, 4.5f, 2.0f));

            var beamLeftGo = new GameObject("Left_Tether_Beam");
            beamLeftGo.transform.SetParent(pylonLeft.transform, false);
            beamLeftGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var lineL = beamLeftGo.AddComponent<LineRenderer>();
            lineL.useWorldSpace = true;
            lineL.material = beamMat;
            lineL.enabled = false;

            var beamRightGo = new GameObject("Right_Tether_Beam");
            beamRightGo.transform.SetParent(pylonRight.transform, false);
            beamRightGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var lineR = beamRightGo.AddComponent<LineRenderer>();
            lineR.useWorldSpace = true;
            lineR.material = beamMat;
            lineR.enabled = false;

            // 4. Đèn Lõi Trung Tâm Sạc (Core Dock Light)
            var dockLightGo = new GameObject("Dock_Core_Light");
            dockLightGo.transform.SetParent(stationGo.transform, false);
            dockLightGo.transform.localPosition = new Vector3(0f, 0f, 0f);
            var dLight = dockLightGo.AddComponent<Light>();
            dLight.type = LightType.Point;
            dLight.range = 25f;
            dLight.color = new Color(0f, 0.85f, 1f);
            dLight.intensity = 4.0f;

            // 5. Bảng Hologram Trạng Thái Sạc Pin
            var holoGo = new GameObject("Recharge_Status_Hologram");
            holoGo.transform.SetParent(stationGo.transform, false);
            holoGo.transform.localPosition = new Vector3(0f, 5.5f, 0f);
            var tmp = holoGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 7f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.1f, 1f, 0.5f);
            tmp.text = "⚡ QUANTUM RECHARGE DOCK\n[ STANDBY // ENTER TO RECHARGE ]";

            // 6. Trigger Collider để đón phi thuyền
            var triggerCol = stationGo.AddComponent<SphereCollider>();
            triggerCol.isTrigger = true;
            triggerCol.radius = 8.5f;

            // Gắn Script Quản Lý Trạm Sạc
            var stationScript = stationGo.AddComponent<SpaceshipRechargeStation>();
            stationScript.rechargeRate = 80f;
            stationScript.leftPylonEmitter = beamLeftGo.transform;
            stationScript.rightPylonEmitter = beamRightGo.transform;
            stationScript.leftBeam = lineL;
            stationScript.rightBeam = lineR;
            stationScript.coreDockLight = dLight;
            stationScript.statusHoloText = tmp;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PHÂN KHU 6: MẠNG LƯỚI THIÊN THẠCH PHỨC TẠP (COMPLEX ASTEROID FIELD)
        // ═════════════════════════════════════════════════════════════════════════
        private void BuildComplexAsteroidField(Transform parent)
        {
            GameObject fieldRoot = new GameObject("Complex_Asteroid_Field");
            fieldRoot.transform.SetParent(parent, false);

            var rockLargeA = LoadModel("rock_largeA.fbx");
            var rockLargeB = LoadModel("rock_largeB.fbx");
            var meteorDetailed = LoadModel("meteor_detailed.fbx");
            var meteorHalf = LoadModel("meteor_half.fbx");
            var rockCrystals = LoadModel("rock_crystals.fbx");

            var models = new List<GameObject>();
            if (rockLargeA != null) models.Add(rockLargeA);
            if (rockLargeB != null) models.Add(rockLargeB);
            if (meteorDetailed != null) models.Add(meteorDetailed);
            if (meteorHalf != null) models.Add(meteorHalf);

            if (models.Count == 0) return;

            // Tọa độ các thiên thạch lớn tạo thành khe núi không gian và chướng ngại vật hiểm trở
            (Vector3 pos, Vector3 rot, float scale, bool hasCrystal)[] asteroidData = new[]
            {
                // Cụm 1: Hẻm núi không gian bao quanh đường đua tốc độ (Trench Run)
                (new Vector3(-185f, 36f, 125f), new Vector3(25f, 40f, 15f), 3.2f, true),
                (new Vector3(-180f, 15f, 110f), new Vector3(-15f, 85f, 35f), 2.8f, false),
                (new Vector3(-135f, 38f, 95f),  new Vector3(45f, -20f, 60f), 3.5f, true),
                (new Vector3(-140f, 12f, 75f),  new Vector3(10f, 115f, -40f), 2.6f, false),
                (new Vector3(-88f, 32f, 55f),   new Vector3(-35f, 60f, 20f), 3.0f, false),
                (new Vector3(-92f, 10f, 38f),   new Vector3(20f, -75f, 50f), 2.7f, true),

                // Cụm 2: Tầng thượng không gian che chở Radar và bầu trời (Y = 48 -> 70)
                (new Vector3(45f, 55f, 35f),    new Vector3(15f, 30f, 80f), 4.2f, true),
                (new Vector3(85f, 60f, 65f),    new Vector3(-40f, 105f, 10f), 3.8f, false),
                (new Vector3(20f, 62f, -15f),   new Vector3(50f, -45f, 30f), 3.4f, false),
                (new Vector3(-45f, 58f, -25f),  new Vector3(25f, 90f, -65f), 3.6f, true),

                // Cụm 3: Vực thẳm tầng đáy không gian sâu (Y = -35 -> -55)
                (new Vector3(-35f, -38f, -40f), new Vector3(-20f, 15f, 45f), 4.5f, true),
                (new Vector3(15f, -45f, -35f),  new Vector3(35f, 70f, -15f), 4.0f, false),
                (new Vector3(65f, -38f, 10f),   new Vector3(-10f, -60f, 85f), 3.5f, false),
                (new Vector3(-85f, -42f, 15f),  new Vector3(40f, 120f, 25f), 4.2f, true),

                // Cụm 4: Các đảo đá hộ vệ xung quanh bãi đáp AboutIsland
                (new Vector3(-35f, 8f, 42f),    new Vector3(15f, 65f, -30f), 2.5f, false),
                (new Vector3(32f, 10f, 48f),    new Vector3(-25f, 140f, 15f), 2.8f, true),
                (new Vector3(-10f, -15f, -25f), new Vector3(30f, -50f, 70f), 3.2f, false)
            };

            for (int i = 0; i < asteroidData.Length; i++)
            {
                var data = asteroidData[i];
                var prefab = models[i % models.Count];

                var rock = Instantiate(prefab, fieldRoot.transform, false);
                rock.name = $"Heavy_Asteroid_{i + 1}";
                rock.transform.position = data.pos;
                rock.transform.rotation = Quaternion.Euler(data.rot);
                rock.transform.localScale = Vector3.one * (data.scale * 2.4f);

                Material rockDark = LoadMaterialAsset("Assets/New/Materials/Mat_Asteroid_RockDark.mat");
                Material rockSlate = LoadMaterialAsset("Assets/New/Materials/Mat_Asteroid_Slate.mat");
                var mr = rock.GetComponentInChildren<MeshRenderer>();
                if (mr != null && rockDark != null)
                {
                    var mats = mr.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        mats[m] = (m % 2 == 0 || rockSlate == null) ? rockDark : rockSlate;
                    }
                    mr.sharedMaterials = mats;
                }

                // Đảm bảo Collider chắc chắn cho va chạm vật lý và bắn súng
                var col = rock.GetComponent<Collider>();
                if (col == null)
                {
                    var mc = rock.AddComponent<MeshCollider>();
                    mc.convex = true;
                }

                // Găm tinh thể phát quang lên các thiên thạch lớn
                if (data.hasCrystal && rockCrystals != null)
                {
                    var cry = Instantiate(rockCrystals, rock.transform, false);
                    cry.name = "Grown_Energy_Crystal";
                    cry.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                    cry.transform.localRotation = Quaternion.Euler(-15f, 35f, 20f);
                    cry.transform.localScale = Vector3.one * 0.7f;
                    ApplyCrystalEmission(cry, (i % 2 == 0) ? new Color(0.2f, 0.9f, 1f) : new Color(1f, 0.3f, 0.8f));
                }
            }
        }

        private void SpawnSecondaryBillboard(Transform parent, Vector3 localPos, Quaternion localRot, string netTitle)
        {
            var screenPrefab = LoadPrefab("big_screen.prefab");
            if (screenPrefab == null) return;

            var screen = Instantiate(screenPrefab, parent, false);
            screen.name = "Secondary_Video_Billboard";
            screen.transform.localPosition = localPos;
            screen.transform.localRotation = localRot;
            screen.transform.localScale = Vector3.one * 1.1f;

            var videoBb = screen.AddComponent<CyberpunkVideoBillboard>();
            videoBb.billboardNetworkName = netTitle;
            videoBb.clipDuration = 7.0f;
            videoBb.BuildBillboardFaces();
        }

        private static GameObject LoadModel(string filename)
        {
#if UNITY_EDITOR
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

        private static GameObject LoadPrefab(string filename)
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:Prefab " + System.IO.Path.GetFileNameWithoutExtension(filename));
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

    /// <summary>
    /// Xoay chảo vệ tinh viễn thông 360 độ và kích hoạt đèn nhấp nháy cảnh báo
    /// </summary>
    public class CommDishRotator : MonoBehaviour
    {
        public float rotateSpeed = 12f;
        public Light beaconLight;
        private float _strobeTimer = 0f;

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.Rotate(Vector3.up * (rotateSpeed * dt), Space.Self);

            if (beaconLight != null)
            {
                _strobeTimer += dt;
                if (_strobeTimer >= 0.75f)
                {
                    _strobeTimer = 0f;
                    beaconLight.enabled = !beaconLight.enabled;
                }
            }
        }
    }
}
