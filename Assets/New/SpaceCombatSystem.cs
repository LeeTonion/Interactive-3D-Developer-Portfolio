using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Hệ thống đạn Laser Plasma & Hiệu ứng Nổ Vỡ 3D + Vết Cháy / Hố Biến Dạng Bề Mặt (Space Combat, Scorch Decal & Deformation VFX):
    /// - Đạn Laser Plasma có lõi phát sáng, TrailRenderer và đèn điểm.
    /// - Khi bắn trúng Thiên thạch / Tàu NPC: Gây sát thương và nổ tung thành mảnh vỡ 3D khi hết máu.
    /// - Khi bắn trúng Vật thể không thể nổ (Đảo đá, Tòa nhà, Sàn cảng, Máy móc, Ống dẫn...):
    ///   + Tạo vụ nổ Plasma bề mặt + 8 mảnh xỉ nóng chảy văng phản xạ theo pháp tuyến bề mặt.
    ///   + Để lại Vết Cháy Đen (Scorch Mark) + Hố Nóng Chảy Lồi Lõm 3D (Deformed Crater Rim) phát sáng đỏ rực rồi nguội dần.
    ///   + Tạo chấn động biến dạng đàn hồi (Elastic Impact Dent / Shudder) lên vật thể vừa trúng đạn.
    /// </summary>
    public class SpaceLaserProjectile : MonoBehaviour
    {
        public bool isPlayerShot = true;
        public float speed = 115f;
        public float damage = 35f;
        public float lifeTime = 2.8f;
        public GameObject owner;

        private float _spawnTime;
        private Vector3 _prevPos;

        public static void SpawnBolt(Vector3 pos, Vector3 dir, bool fromPlayer, GameObject shooter, float boltSpeed = 115f, float boltDamage = 35f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = fromPlayer ? "Player_LaserBolt" : "Enemy_LaserBolt";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(0.22f, 1.35f, 0.22f);

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Color boltColor = fromPlayer ? new Color(0.15f, 0.95f, 1f, 1f) : new Color(1f, 0.18f, 0.15f, 1f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = SpaceExplosionVFX.GetEmissiveMaterial(fromPlayer);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var lightGo = new GameObject("BoltLight");
            lightGo.transform.SetParent(go.transform, false);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = boltColor;
            l.range = 8f;
            l.intensity = 3.5f;
            l.shadows = LightShadows.None;

            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.14f;
            tr.startWidth = 0.28f;
            tr.endWidth = 0.0f;
            tr.sharedMaterial = SpaceExplosionVFX.GetEmissiveMaterial(fromPlayer);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var proj = go.AddComponent<SpaceLaserProjectile>();
            proj.isPlayerShot = fromPlayer;
            proj.speed = boltSpeed;
            proj.damage = boltDamage;
            proj.owner = shooter;
        }

        private void Start()
        {
            _spawnTime = Time.time;
            _prevPos = transform.position;
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 moveDir = transform.up;
            float step = speed * Time.deltaTime;
            Vector3 nextPos = transform.position + moveDir * step;

            if (Physics.SphereCast(_prevPos, 0.42f, moveDir, out RaycastHit hit, step + 0.25f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (owner != null && (hit.collider.gameObject == owner || hit.collider.transform.IsChildOf(owner.transform)))
                {
                    // Bỏ qua chính tàu bắn ra
                }
                else
                {
                    OnHit(hit, moveDir);
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position = nextPos;
            _prevPos = nextPos;
        }

        private void OnHit(RaycastHit hit, Vector3 boltDir)
        {
            Vector3 hitPoint = hit.point;

            // 1. Kiểm tra trúng thiên thạch
            var asteroid = hit.collider.GetComponentInParent<SpaceAsteroidPhysics>();
            if (asteroid != null)
            {
                asteroid.TakeDamage(damage, hitPoint, boltDir);
                SpaceExplosionVFX.SpawnSurfaceImpact(hit, boltDir, isPlayerShot, true);
                return;
            }

            // 2. Kiểm tra trúng tàu vũ trụ NPC
            var npcShip = hit.collider.GetComponentInParent<SpaceTrafficShipAI>();
            if (npcShip != null)
            {
                if (isPlayerShot)
                {
                    npcShip.TakeDamage(damage, hitPoint, true);
                    SpaceExplosionVFX.SpawnSurfaceImpact(hit, boltDir, true, false);
                }
                return;
            }

            // 3. Kiểm tra trúng tàu người chơi
            var playerShip = hit.collider.GetComponentInParent<PlayerSpaceshipController>();
            if (playerShip != null)
            {
                if (!isPlayerShot)
                {
                    playerShip.TakeDamage(damage, hitPoint);
                    SpaceExplosionVFX.SpawnHitSpark(hitPoint, false);
                }
                return;
            }

            // 4. Kiểm tra trúng Pháo đài Phòng thủ (Carrier Defense Turret)
            var turretAI = hit.collider.GetComponentInParent<CarrierDefenseTurretAI>();
            if (turretAI != null)
            {
                if (isPlayerShot)
                {
                    turretAI.TakeDamage(damage, hitPoint);
                    SpaceExplosionVFX.SpawnSurfaceImpact(hit, boltDir, true, false);
                }
                return;
            }

            // 5. Kiểm tra trúng Lõi Năng Lượng Căn Cứ Hạm Đội (Carrier Power Core)
            var carrierCore = hit.collider.GetComponentInParent<CarrierEnergyCore>();
            if (carrierCore == null)
            {
                var spawner = hit.collider.GetComponentInParent<ProjectZoneEnemySpawner>();
                if (spawner != null)
                {
                    string colName = hit.collider.name;
                    Transform colParent = hit.collider.transform.parent;
                    string parentName = colParent != null ? colParent.name : "";
                    if (colName.Contains("Warp") || colName.Contains("Portal") || colName.Contains("Catapult") ||
                        colName.Contains("Core") || parentName.Contains("Catapult") || parentName.Contains("Gate") ||
                        parentName.Contains("Warp"))
                    {
                        carrierCore = spawner.GetComponentInChildren<CarrierEnergyCore>();
                    }
                }
            }

            if (carrierCore != null)
            {
                if (isPlayerShot)
                {
                    carrierCore.TakeDamage(damage, hitPoint);
                    SpaceExplosionVFX.SpawnSurfaceImpact(hit, boltDir, true, false);
                }
                return;
            }

            // 5.5 Kiểm tra trúng Biển Quảng Cáo Cyberpunk Video hoặc Billboard Display
            var videoBillboard = hit.collider.GetComponentInParent<CyberpunkVideoBillboard>();
            if (videoBillboard != null)
            {
                videoBillboard.OnLaserHit();
                SpaceExplosionVFX.SpawnSurfaceImpact(hit, boltDir, isPlayerShot, false);
                return;
            }
            var billboard = hit.collider.GetComponentInParent<CyberpunkBillboardDisplay>();
            if (billboard != null)
            {
                billboard.OnHitByLaser(hitPoint, boltDir);
                SpaceExplosionVFX.SpawnSurfaceImpact(hit, boltDir, isPlayerShot, false);
                return;
            }

            // 6. Kiểm tra bắn vào Nhà Chính (Project / AboutIsland) hoặc các trụ / căn cứ / công trình -> Kích hoạt báo động hạm đội ngay
            if (isPlayerShot)
            {
                var hitTr = hit.collider.transform;
                bool isBaseOrStructure = hitTr.GetComponentInParent<ProjectZoneEnemySpawner>() != null ||
                    hitTr.GetComponentInParent<CarrierDefenseTurretAI>() != null ||
                    hitTr.GetComponentInParent<CarrierEnergyCore>() != null ||
                    hitTr.root.name.Contains("Project") || hitTr.root.name.Contains("About") ||
                    hitTr.name.ToLower().Contains("pillar") || hitTr.name.ToLower().Contains("tower") ||
                    hitTr.name.ToLower().Contains("project") || hitTr.name.ToLower().Contains("station") ||
                    hitTr.name.ToLower().Contains("building") || hitTr.name.ToLower().Contains("observatory") ||
                    hitTr.name.ToLower().Contains("gantry") || hitTr.name.ToLower().Contains("cannon") ||
                    hitTr.name.ToLower().Contains("radar") || hitTr.name.ToLower().Contains("spawner") ||
                    hitTr.name.ToLower().Contains("hangar") || hitTr.name.ToLower().Contains("carrier") ||
                    hitTr.name.ToLower().Contains("deck") || hitTr.name.ToLower().Contains("core");

                if (isBaseOrStructure)
                {
                    SpaceTrafficShipAI.TriggerFleetHostileAlert(60f);
                }
            }

            // 7. Trúng các vật thể tĩnh / công trình / đá đảo không thể nổ:
            // Tạo vụ nổ bề mặt + vết cháy đen + hố nóng chảy lồi lõm + rung chấn biến dạng vật thể!
            SpaceExplosionVFX.SpawnSurfaceImpact(hit, boltDir, isPlayerShot, true);
        }
    }

    /// <summary>
    /// Bộ tạo hiệu ứng nổ tung 3D, Vết Cháy (Scorch Mark) & Hố Biến Dạng Bề Mặt (Surface Deformation Crater).
    /// </summary>
    public class SpaceExplosionVFX : MonoBehaviour
    {
        private static Material _cyanMat;
        private static Material _redMat;
        private static Material _fireMat;
        private static Material _rockDebrisMat;
        private static Material _scorchCharcoalMat;
        private static Material _moltenSlagMat;

        private static readonly Queue<GameObject> ActiveScorchMarks = new Queue<GameObject>();
        private static readonly HashSet<int> ClonedDeformableMeshes = new HashSet<int>();
        private const int MaxScorchMarks = 45;

        public static Material GetEmissiveMaterial(bool cyan)
        {
            if (cyan)
            {
                if (_cyanMat == null)
                    _cyanMat = CreateUnlitMat(new Color(0.15f, 0.95f, 1f, 1f));
                return _cyanMat;
            }
            else
            {
                if (_redMat == null)
                    _redMat = CreateUnlitMat(new Color(1f, 0.2f, 0.15f, 1f));
                return _redMat;
            }
        }

        private static Material CreateUnlitMat(Color col)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var m = new Material(sh);
            m.SetColor("_BaseColor", col);
            m.color = col;
            return m;
        }

        private static void EnsureImpactMaterials()
        {
            if (_fireMat == null)
                _fireMat = CreateUnlitMat(new Color(1f, 0.55f, 0.12f, 1f));
            if (_rockDebrisMat == null)
                _rockDebrisMat = CreateUnlitMat(new Color(0.32f, 0.35f, 0.42f, 1f));
            if (_scorchCharcoalMat == null)
                _scorchCharcoalMat = CreateUnlitMat(new Color(0.06f, 0.05f, 0.05f, 1f));
            if (_moltenSlagMat == null)
                _moltenSlagMat = CreateUnlitMat(new Color(1f, 0.32f, 0.08f, 1f));
        }

        public static void SpawnHitSpark(Vector3 pos, bool cyan)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "LaserHitSpark";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.85f;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = GetEmissiveMaterial(cyan);

            var vfx = go.AddComponent<SpaceExplosionVFX>();
            vfx.StartCoroutine(vfx.AnimateSpark());
        }

        /// <summary>
        /// Tạo hiệu ứng nổ va chạm bề mặt + Làm LÕM trực tiếp lưới 3D (Mesh Denting) + VỠ bắn mảnh vật lý (Shatter Debris) + Hố nứt cháy đen.
        /// Hoàn toàn không dùng hiệu ứng co giãn đàn hồi kiểu thạch (Jelly).
        /// </summary>
        public static void SpawnSurfaceImpact(RaycastHit hit, Vector3 boltDir, bool cyan, bool leaveScorchCrater)
        {
            EnsureImpactMaterials();

            Vector3 pos = hit.point;
            Vector3 normal = hit.normal.sqrMagnitude > 0.01f ? hit.normal.normalized : -boltDir.normalized;

            // 1. Vụ nổ tia lửa Plasma & mảnh xỉ bắn tung tóe khỏi bề mặt
            var burstGo = new GameObject("Surface_Impact_Burst");
            burstGo.transform.position = pos;
            var burstVfx = burstGo.AddComponent<SpaceExplosionVFX>();
            burstVfx.StartCoroutine(burstVfx.RunSurfaceBurstSequence(pos, normal, boltDir, cyan));

            if (leaveScorchCrater && hit.collider != null)
            {
                Transform targetTr = hit.collider.transform;
                Material surfaceMat = GetTargetSurfaceMaterial(targetTr);

                // 2. Làm LÕM thực sự lưới Mesh 3D (Real Vertex Mesh Cratering) ngay tại vị trí trúng đạn
                DentMeshVerticesAtHit(hit.collider, pos, normal, boltDir);

                // 3. Làm VỠ bắn các mảnh đá/kim loại (Physical Shattered Chunks) có Rigidbody văng & nảy trên mặt đất
                burstVfx.StartCoroutine(burstVfx.SpawnShatteredSurfaceChunks(pos, normal, boltDir, surfaceMat));

                // 4. Tạo Hố Vỡ Nứt (Fractured Crater Pit + Vết nứt chânchim + Mảnh gờ vỡ) trên bề mặt
                SpawnScorchCraterOnSurface(targetTr, pos, normal, surfaceMat);
            }
        }

        private static Material GetTargetSurfaceMaterial(Transform targetTransform)
        {
            if (targetTransform == null) return _rockDebrisMat;
            var mr = targetTransform.GetComponent<MeshRenderer>();
            if (mr == null) mr = targetTransform.GetComponentInChildren<MeshRenderer>();
            if (mr == null) mr = targetTransform.GetComponentInParent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                return mr.sharedMaterial;
            }
            return _rockDebrisMat;
        }

        private static void SpawnScorchCraterOnSurface(Transform targetTransform, Vector3 hitPoint, Vector3 hitNormal, Material surfaceMat)
        {
            while (ActiveScorchMarks.Count >= MaxScorchMarks)
            {
                var oldest = ActiveScorchMarks.Dequeue();
                if (oldest != null) Destroy(oldest);
            }

            var craterRoot = new GameObject("Fractured_Impact_Crater");
            craterRoot.transform.position = hitPoint + hitNormal * 0.02f;
            craterRoot.transform.rotation = Quaternion.LookRotation(hitNormal) * Quaternion.Euler(90f, 0f, Random.Range(0f, 360f));

            float craterSize = Random.Range(0.85f, 1.25f);

            // Lớp 1: Vết cháy sém đen bên ngoài (Charred Blast Ring)
            var scorchDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scorchDisc.name = "Charred_Scorch_Ring";
            scorchDisc.transform.SetParent(craterRoot.transform, false);
            scorchDisc.transform.localPosition = Vector3.zero;
            scorchDisc.transform.localScale = new Vector3(craterSize * 1.55f, 0.012f, craterSize * 1.55f);
            Destroy(scorchDisc.GetComponent<Collider>());
            scorchDisc.GetComponent<MeshRenderer>().sharedMaterial = _scorchCharcoalMat;

            // Lớp 2: Hốc lõm sâu tối đen ở tâm vết vỡ (Deep Fracture Pit)
            var deepPit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deepPit.name = "Deep_Fracture_Pit";
            deepPit.transform.SetParent(craterRoot.transform, false);
            deepPit.transform.localPosition = new Vector3(0f, 0.008f, 0f);
            deepPit.transform.localScale = new Vector3(craterSize * 0.95f, 0.018f, craterSize * 0.95f);
            Destroy(deepPit.GetComponent<Collider>());
            deepPit.GetComponent<MeshRenderer>().sharedMaterial = _scorchCharcoalMat;

            // Lớp 3: Lõi hố lõm nóng chảy âm ỉ bên dưới vết nứt (Molten Glowing Core)
            var moltenCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            moltenCore.name = "Molten_Crater_Core";
            moltenCore.transform.SetParent(craterRoot.transform, false);
            moltenCore.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            moltenCore.transform.localScale = new Vector3(craterSize * 0.58f, 0.022f, craterSize * 0.58f);
            Destroy(moltenCore.GetComponent<Collider>());
            var coreMatInstance = new Material(_moltenSlagMat);
            moltenCore.GetComponent<MeshRenderer>().sharedMaterial = coreMatInstance;

            // Lớp 4: 6 Đường nứt gãy tỏa ra xung quanh miệng hố (Radial Fracture Cracks)
            int crackCount = 6;
            for (int i = 0; i < crackCount; i++)
            {
                float angDeg = i * (360f / crackCount) + Random.Range(-18f, 18f);
                float crackLen = craterSize * Random.Range(0.75f, 1.35f);
                var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crack.name = "Fracture_Crack_" + i;
                crack.transform.SetParent(craterRoot.transform, false);
                crack.transform.localRotation = Quaternion.Euler(0f, angDeg, 0f);
                crack.transform.localPosition = crack.transform.localRotation * new Vector3(0f, 0.014f, craterSize * 0.35f + crackLen * 0.4f);
                crack.transform.localScale = new Vector3(Random.Range(0.035f, 0.065f), 0.016f, crackLen);
                Destroy(crack.GetComponent<Collider>());
                crack.GetComponent<MeshRenderer>().sharedMaterial = _scorchCharcoalMat;
            }

            // Lớp 5: 7 Mảnh gờ đá/kim loại bị toác vỡ dựng ngược quanh miệng hố (Shattered Rim Shards)
            int rimCount = 7;
            Material shardMat = surfaceMat != null ? surfaceMat : _rockDebrisMat;
            for (int i = 0; i < rimCount; i++)
            {
                float ang = i * (Mathf.PI * 2f / rimCount) + Random.Range(-0.22f, 0.22f);
                float r = craterSize * Random.Range(0.42f, 0.58f);
                var rimChunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rimChunk.name = "Shattered_Rim_Shard_" + i;
                rimChunk.transform.SetParent(craterRoot.transform, false);
                rimChunk.transform.localPosition = new Vector3(Mathf.Cos(ang) * r, 0.05f, Mathf.Sin(ang) * r);
                rimChunk.transform.localRotation = Quaternion.Euler(Random.Range(25f, 62f), Random.Range(0f, 360f), Random.Range(20f, 55f));
                float s = craterSize * Random.Range(0.18f, 0.32f);
                rimChunk.transform.localScale = new Vector3(s * 1.4f, s * 0.45f, s * 0.9f);
                Destroy(rimChunk.GetComponent<Collider>());
                rimChunk.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? shardMat : _scorchCharcoalMat;
            }

            craterRoot.transform.SetParent(targetTransform, true);
            ActiveScorchMarks.Enqueue(craterRoot);

            var vfx = craterRoot.AddComponent<SpaceExplosionVFX>();
            vfx.StartCoroutine(vfx.CoolDownAndFadeCrater(coreMatInstance));
        }

        /// <summary>
        /// Biến dạng LÕM thực tế trên các đỉnh (Vertices) của Mesh 3D tại điểm trúng đạn.
        /// Tự động chia nhỏ lưới cục bộ (Local Red-Green Tessellation) nếu bề mặt ít đa giác (như tường Cube) rồi đẩy lõm các đỉnh vào trong.
        /// </summary>
        private static void DentMeshVerticesAtHit(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal, Vector3 boltDir)
        {
            if (hitCollider == null) return;

            MeshFilter mf = hitCollider.GetComponent<MeshFilter>();
            if (mf == null) mf = hitCollider.GetComponentInChildren<MeshFilter>();
            if (mf == null) mf = hitCollider.GetComponentInParent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return;

            string goName = mf.gameObject.name;
            if (goName.Contains("Text") || goName.Contains("Screen") || goName.Contains("Quad")) return;

            Transform tr = mf.transform;
            int mfId = mf.GetInstanceID();

            Mesh mesh;
            if (!ClonedDeformableMeshes.Contains(mfId))
            {
                mesh = Instantiate(mf.sharedMesh);
                mesh.name = mf.sharedMesh.name + "_Deformed";
                mesh.MarkDynamic();
                mf.sharedMesh = mesh;
                ClonedDeformableMeshes.Add(mfId);
            }
            else
            {
                mesh = mf.sharedMesh;
            }

            float dentRadius = 1.45f;
            float dentDepth = 0.42f;

            // Nếu vùng trúng đạn có tam giác quá thưa (như mặt phẳng tường/khối hộp), tự động chia nhỏ tam giác quanh vết bắn
            if (mesh.vertexCount < 3800)
            {
                SubdivideMeshNearHit(mesh, tr, hitPoint, dentRadius * 1.35f, 0.52f);
                if (mesh.vertexCount < 3800)
                {
                    SubdivideMeshNearHit(mesh, tr, hitPoint, dentRadius * 1.05f, 0.35f);
                }
            }

            Vector3[] verts = mesh.vertices;
            int vertCount = verts.Length;
            Vector3 inwardDirWorld = (-hitNormal * 0.75f + boltDir.normalized * 0.25f).normalized;
            bool modified = false;

            for (int i = 0; i < vertCount; i++)
            {
                Vector3 worldV = tr.TransformPoint(verts[i]);
                float dist = Vector3.Distance(worldV, hitPoint);
                if (dist < dentRadius)
                {
                    float normDist = dist / dentRadius;
                    // Hàm lõm lòng chảo sắc nét ở tâm + viền gồ ghề như bị bắn vỡ
                    float craterProfile = (1f - normDist * normDist);
                    craterProfile *= craterProfile;

                    // Nhiễu gãy góc định sẵn theo tọa độ 3D để các đỉnh trùng vị trí (Hard Edges) luôn di chuyển khớp khít 100%
                    float jagNoise = 0.78f + 0.44f * Mathf.Sin(worldV.x * 17.3f + worldV.y * 23.1f + worldV.z * 19.7f);
                    float pushDist = dentDepth * craterProfile * jagNoise;

                    worldV += inwardDirWorld * pushDist;
                    verts[i] = tr.InverseTransformPoint(worldV);
                    modified = true;
                }
            }

            if (modified)
            {
                mesh.vertices = verts;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
        }

        private static void SubdivideMeshNearHit(Mesh mesh, Transform tr, Vector3 hitPoint, float radius, float minEdgeWorldLen)
        {
            Vector3[] oldVerts = mesh.vertices;
            Vector3[] oldNormals = mesh.normals;
            Vector2[] oldUvs = mesh.uv;
            bool hasNormals = oldNormals != null && oldNormals.Length == oldVerts.Length;
            bool hasUvs = oldUvs != null && oldUvs.Length == oldVerts.Length;

            List<Vector3> verts = new List<Vector3>(oldVerts);
            List<Vector3> normals = hasNormals ? new List<Vector3>(oldNormals) : null;
            List<Vector2> uvs = hasUvs ? new List<Vector2>(oldUvs) : null;

            int subMeshCount = mesh.subMeshCount;
            Dictionary<long, int> edgeMidpointCache = new Dictionary<long, int>();

            int GetOrCreateMidpoint(int a, int b)
            {
                long min = a < b ? a : b;
                long max = a < b ? b : a;
                long key = (min << 32) | (uint)max;
                if (edgeMidpointCache.TryGetValue(key, out int midIdx))
                {
                    return midIdx;
                }

                midIdx = verts.Count;
                verts.Add((verts[a] + verts[b]) * 0.5f);
                if (hasNormals) normals.Add((normals[a] + normals[b]).normalized);
                if (hasUvs) uvs.Add((uvs[a] + uvs[b]) * 0.5f);
                edgeMidpointCache[key] = midIdx;
                return midIdx;
            }

            bool ShouldSplitEdge(int a, int b)
            {
                Vector3 wa = tr.TransformPoint(oldVerts[a]);
                Vector3 wb = tr.TransformPoint(oldVerts[b]);
                float edgeLen = Vector3.Distance(wa, wb);
                if (edgeLen < minEdgeWorldLen) return false;

                float distToEdge = DistancePointToSegment(hitPoint, wa, wb);
                return distToEdge < radius;
            }

            List<int>[] newSubMeshTris = new List<int>[subMeshCount];
            bool anySplit = false;

            for (int s = 0; s < subMeshCount; s++)
            {
                int[] tris = mesh.GetTriangles(s);
                List<int> outTris = new List<int>(tris.Length * 2);
                newSubMeshTris[s] = outTris;

                for (int t = 0; t < tris.Length; t += 3)
                {
                    int i0 = tris[t];
                    int i1 = tris[t + 1];
                    int i2 = tris[t + 2];

                    bool s01 = ShouldSplitEdge(i0, i1);
                    bool s12 = ShouldSplitEdge(i1, i2);
                    bool s20 = ShouldSplitEdge(i2, i0);

                    int splitCount = (s01 ? 1 : 0) + (s12 ? 1 : 0) + (s20 ? 1 : 0);
                    if (splitCount == 0)
                    {
                        outTris.Add(i0); outTris.Add(i1); outTris.Add(i2);
                    }
                    else
                    {
                        anySplit = true;
                        if (splitCount == 3)
                        {
                            int m01 = GetOrCreateMidpoint(i0, i1);
                            int m12 = GetOrCreateMidpoint(i1, i2);
                            int m20 = GetOrCreateMidpoint(i2, i0);

                            outTris.Add(i0); outTris.Add(m01); outTris.Add(m20);
                            outTris.Add(m01); outTris.Add(i1); outTris.Add(m12);
                            outTris.Add(m20); outTris.Add(m12); outTris.Add(i2);
                            outTris.Add(m01); outTris.Add(m12); outTris.Add(m20);
                        }
                        else if (splitCount == 2)
                        {
                            // Xoay thứ tự đỉnh để 2 cạnh bị chia luôn là (a-b) và (b-c)
                            int a = i0, b = i1, c = i2;
                            if (!s01) { a = i1; b = i2; c = i0; }
                            else if (!s12) { a = i2; b = i0; c = i1; }

                            int mab = GetOrCreateMidpoint(a, b);
                            int mbc = GetOrCreateMidpoint(b, c);

                            outTris.Add(a); outTris.Add(mab); outTris.Add(c);
                            outTris.Add(mab); outTris.Add(b); outTris.Add(mbc);
                            outTris.Add(mab); outTris.Add(mbc); outTris.Add(c);
                        }
                        else // splitCount == 1
                        {
                            int a = i0, b = i1, c = i2;
                            if (s12) { a = i1; b = i2; c = i0; }
                            else if (s20) { a = i2; b = i0; c = i1; }

                            int mab = GetOrCreateMidpoint(a, b);
                            outTris.Add(a); outTris.Add(mab); outTris.Add(c);
                            outTris.Add(mab); outTris.Add(b); outTris.Add(c);
                        }
                    }
                }
            }

            if (!anySplit) return;

            mesh.SetVertices(verts);
            if (hasNormals) mesh.SetNormals(normals);
            if (hasUvs) mesh.SetUVs(0, uvs);

            for (int s = 0; s < subMeshCount; s++)
            {
                mesh.SetTriangles(newSubMeshTris[s], s, false);
            }
        }

        private static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.00001f) return Vector3.Distance(p, a);
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / lenSq);
            return Vector3.Distance(p, a + ab * t);
        }

        /// <summary>
        /// Sinh ra 8 mảnh vỡ vật lý 3D (Shattered Debris Chunks) có Rigidbody & BoxCollider văng ra từ vết bắn và nảy trên sàn.
        /// </summary>
        private IEnumerator SpawnShatteredSurfaceChunks(Vector3 pos, Vector3 normal, Vector3 boltDir, Material surfaceMat)
        {
            int shardCount = 8;
            GameObject[] shards = new GameObject[shardCount];
            Vector3 reflectDir = Vector3.Reflect(boltDir.normalized, normal);
            Material primaryMat = surfaceMat != null ? surfaceMat : _rockDebrisMat;

            for (int i = 0; i < shardCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Shattered_Surface_Chunk_" + i;
                shard.transform.position = pos + normal * Random.Range(0.12f, 0.28f) + Random.insideUnitSphere * 0.2f;
                shard.transform.rotation = Random.rotation;

                float sx = Random.Range(0.14f, 0.34f);
                float sy = Random.Range(0.08f, 0.22f);
                float sz = Random.Range(0.14f, 0.36f);
                shard.transform.localScale = new Vector3(sx, sy, sz);

                var mr = shard.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = (i % 3 == 0) ? _scorchCharcoalMat : primaryMat;
                }

                var rb = shard.AddComponent<Rigidbody>();
                rb.mass = 0.25f;
                rb.useGravity = true;
                rb.linearDamping = 0.3f;
                rb.angularDamping = 0.4f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                Vector3 launchDir = (normal * 0.85f + reflectDir * 0.45f + Random.insideUnitSphere * 0.65f).normalized;
                rb.linearVelocity = launchDir * Random.Range(5.5f, 14.5f);
                rb.angularVelocity = Random.onUnitSphere * Random.Range(8f, 22f);

                shards[i] = shard;
            }

            // Cho các mảnh vỡ rơi, va đập và nảy trên địa hình trong 2.8 giây rồi thu nhỏ biến mất
            yield return new WaitForSeconds(2.8f);

            float fadeT = 0f;
            while (fadeT < 0.6f)
            {
                fadeT += Time.deltaTime;
                float k = 1f - (fadeT / 0.6f);
                for (int i = 0; i < shardCount; i++)
                {
                    if (shards[i] != null)
                    {
                        shards[i].transform.localScale *= Mathf.Clamp01(0.92f + k * 0.05f);
                    }
                }
                yield return null;
            }

            for (int i = 0; i < shardCount; i++)
            {
                if (shards[i] != null) Destroy(shards[i]);
            }
        }

        private IEnumerator CoolDownAndFadeCrater(Material coreMat)
        {
            // Lõi nóng chảy chuyển từ Cam Đỏ rực -> Đỏ sẫm -> Than đen trong 4.5 giây
            Color hotColor = new Color(1f, 0.42f, 0.08f, 1f);
            Color warmColor = new Color(0.65f, 0.12f, 0.03f, 1f);
            Color coldColor = new Color(0.08f, 0.07f, 0.07f, 1f);

            float elapsed = 0f;
            while (elapsed < 4.5f)
            {
                elapsed += Time.deltaTime;
                float u = elapsed / 4.5f;
                if (coreMat != null)
                {
                    Color c = u < 0.5f
                        ? Color.Lerp(hotColor, warmColor, u * 2f)
                        : Color.Lerp(warmColor, coldColor, (u - 0.5f) * 2f);
                    coreMat.SetColor("_BaseColor", c);
                    coreMat.color = c;
                }
                yield return null;
            }

            // Giữ vết cháy trên bề mặt thêm 18 giây rồi thu nhỏ biến mất
            yield return new WaitForSeconds(18f);

            Vector3 startScale = transform.localScale;
            float fadeT = 0f;
            while (fadeT < 1.0f)
            {
                fadeT += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, fadeT);
                yield return null;
            }

            if (coreMat != null) Destroy(coreMat);
            Destroy(gameObject);
        }

        private IEnumerator RunSurfaceBurstSequence(Vector3 pos, Vector3 normal, Vector3 boltDir, bool cyan)
        {
            // 1. Cầu lửa va chạm bề mặt
            var flashBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flashBall.transform.SetParent(transform, false);
            flashBall.transform.localPosition = Vector3.zero;
            Destroy(flashBall.GetComponent<Collider>());
            flashBall.GetComponent<MeshRenderer>().sharedMaterial = _fireMat;

            // 2. Vòng sóng xung kích ép sát bề mặt
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = normal * 0.04f;
            ring.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(90f, 0f, 0f);
            Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<MeshRenderer>().sharedMaterial = GetEmissiveMaterial(cyan);

            // 3. Đèn chớp va chạm
            var lightGo = new GameObject("ImpactLight");
            lightGo.transform.SetParent(transform, false);
            var impLight = lightGo.AddComponent<Light>();
            impLight.type = LightType.Point;
            impLight.color = new Color(1f, 0.55f, 0.2f);
            impLight.range = 10f;
            impLight.intensity = 8f;

            // 4. 7 mảnh xỉ nóng chảy văng bật ngược ra từ bề mặt
            int sparkCount = 7;
            GameObject[] sparks = new GameObject[sparkCount];
            Vector3[] sparkVel = new Vector3[sparkCount];
            Vector3 reflectDir = Vector3.Reflect(boltDir.normalized, normal);

            for (int i = 0; i < sparkCount; i++)
            {
                var sp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sp.transform.SetParent(transform, false);
                sp.transform.localPosition = normal * 0.1f;
                sp.transform.localRotation = Random.rotation;
                float s = Random.Range(0.12f, 0.26f);
                sp.transform.localScale = new Vector3(s, s, s * 1.8f);
                Destroy(sp.GetComponent<Collider>());
                sp.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? _fireMat : GetEmissiveMaterial(cyan);

                sparks[i] = sp;
                Vector3 spread = (reflectDir + normal * 0.6f + Random.insideUnitSphere * 0.75f).normalized;
                sparkVel[i] = spread * Random.Range(7f, 18f);
            }

            float duration = 0.45f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = elapsed / duration;

                if (flashBall != null)
                {
                    if (u < 0.45f)
                    {
                        float k = u / 0.45f;
                        flashBall.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 0.1f, k);
                    }
                    else Destroy(flashBall);
                }

                if (ring != null)
                {
                    float rSize = Mathf.Lerp(0.4f, 2.4f, u);
                    ring.transform.localScale = new Vector3(rSize, 0.03f * (1f - u), rSize);
                }

                if (impLight != null)
                {
                    impLight.intensity = Mathf.Lerp(8f, 0f, u * 2f);
                }

                for (int i = 0; i < sparkCount; i++)
                {
                    if (sparks[i] == null) continue;
                    sparks[i].transform.localPosition += sparkVel[i] * Time.deltaTime;
                    sparks[i].transform.Rotate(240f * Time.deltaTime, 180f * Time.deltaTime, 0f);
                    sparks[i].transform.localScale *= 0.94f;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        public static void SpawnExplosion(Vector3 pos, float scale, bool isAsteroid, Material chunkMaterial = null)
        {
            var root = new GameObject(isAsteroid ? "Asteroid_Explosion_VFX" : "Spaceship_Explosion_VFX");
            root.transform.position = pos;
            var vfx = root.AddComponent<SpaceExplosionVFX>();
            vfx.StartCoroutine(vfx.RunExplosionSequence(pos, scale, isAsteroid, chunkMaterial));
        }

        private IEnumerator AnimateSpark()
        {
            float t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float k = 1f - (t / 0.18f);
                transform.localScale = startScale * (1f + (1f - k) * 1.6f) * k;
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator RunExplosionSequence(Vector3 pos, float scale, bool isAsteroid, Material chunkMat)
        {
            EnsureImpactMaterials();

            var fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireball.transform.SetParent(transform, false);
            fireball.transform.localPosition = Vector3.zero;
            var fbCol = fireball.GetComponent<Collider>();
            if (fbCol != null) Destroy(fbCol);
            fireball.GetComponent<MeshRenderer>().sharedMaterial = _fireMat;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.zero;
            var rCol = ring.GetComponent<Collider>();
            if (rCol != null) Destroy(rCol);
            ring.GetComponent<MeshRenderer>().sharedMaterial = GetEmissiveMaterial(!isAsteroid);

            var lightGo = new GameObject("ExplosionLight");
            lightGo.transform.SetParent(transform, false);
            var expLight = lightGo.AddComponent<Light>();
            expLight.type = LightType.Point;
            expLight.color = new Color(1f, 0.6f, 0.2f);
            expLight.range = scale * 8f;
            expLight.intensity = 14f;

            int chunkCount = isAsteroid ? 10 : 12;
            GameObject[] chunks = new GameObject[chunkCount];
            Vector3[] chunkVel = new Vector3[chunkCount];
            Vector3[] chunkRot = new Vector3[chunkCount];

            for (int i = 0; i < chunkCount; i++)
            {
                PrimitiveType pt = (i % 2 == 0) ? PrimitiveType.Cube : PrimitiveType.Sphere;
                var c = GameObject.CreatePrimitive(pt);
                c.name = "DebrisChunk_" + i;
                c.transform.SetParent(transform, false);
                c.transform.localPosition = Random.insideUnitSphere * (scale * 0.35f);
                c.transform.localRotation = Random.rotation;

                float s = Random.Range(0.25f, 0.65f) * scale * 0.45f;
                c.transform.localScale = new Vector3(s, s * Random.Range(0.6f, 1.4f), s * Random.Range(0.6f, 1.4f));

                var cc = c.GetComponent<Collider>();
                if (cc != null) Destroy(cc);

                var cmr = c.GetComponent<MeshRenderer>();
                if (cmr != null)
                {
                    if (i % 3 == 0) cmr.sharedMaterial = _fireMat;
                    else cmr.sharedMaterial = chunkMat != null ? chunkMat : _rockDebrisMat;
                }

                chunks[i] = c;
                chunkVel[i] = Random.onUnitSphere * Random.Range(8f, 24f) * Mathf.Clamp(scale * 0.35f, 0.8f, 2.5f);
                chunkRot[i] = Random.onUnitSphere * Random.Range(120f, 360f);
            }

            float duration = 1.6f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = elapsed / duration;

                if (fireball != null)
                {
                    if (u < 0.35f)
                    {
                        float k = u / 0.35f;
                        fireball.transform.localScale = Vector3.one * (scale * 1.8f * (1f - k * 0.5f));
                    }
                    else
                    {
                        Destroy(fireball);
                    }
                }

                if (ring != null)
                {
                    if (u < 0.45f)
                    {
                        float rk = u / 0.45f;
                        float ringRadius = scale * (1f + rk * 4.2f);
                        ring.transform.localScale = new Vector3(ringRadius, 0.08f * (1f - rk), ringRadius);
                    }
                    else
                    {
                        Destroy(ring);
                    }
                }

                if (expLight != null)
                {
                    expLight.intensity = Mathf.Lerp(14f, 0f, u * 2.2f);
                }

                for (int i = 0; i < chunkCount; i++)
                {
                    if (chunks[i] == null) continue;
                    chunks[i].transform.localPosition += chunkVel[i] * Time.deltaTime;
                    chunks[i].transform.Rotate(chunkRot[i] * Time.deltaTime, Space.Self);
                    if (u > 0.5f)
                    {
                        float shrink = 1f - ((u - 0.5f) / 0.5f);
                        chunks[i].transform.localScale *= Mathf.Clamp01(0.97f + shrink * 0.02f);
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Tháp pháo phòng không trên căn cứ tàu sân bay địch (Carrier Defense Turret AI):
    /// - Có Collider & Máu riêng, có thể bị người chơi bắn nổ tung.
    /// - Khi căn cứ ở trạng thái báo động, các tháp pháo sẽ tự động xoay nòng ngắm và bắn đạn laser đỏ vào tàu người chơi.
    /// </summary>
    public class CarrierDefenseTurretAI : MonoBehaviour
    {
        public float maxHealth = 75f;
        public float fireInterval = 1.8f;
        public float attackRange = 95f;
        public float bulletDamage = 14f;
        public Transform swivelHead;
        public Transform muzzlePoint;

        private float _currentHealth;
        private float _nextFireTime;
        private bool _isDestroyed;
        private PlayerSpaceshipController _player;

        private void Start()
        {
            _currentHealth = maxHealth;
            _nextFireTime = Time.time + Random.Range(0.4f, 1.8f);
            if (swivelHead == null)
            {
                if (transform.childCount > 0) swivelHead = transform.GetChild(0);
                else swivelHead = transform;
            }
        }

        private void Update()
        {
            if (_isDestroyed) return;
            if (!SpaceTrafficShipAI.IsFleetHostile || AboutInfoStation.AnyStationActive) return;

            if (_player == null) _player = FindFirstObjectByType<PlayerSpaceshipController>();
            if (_player == null) return;

            Vector3 toPlayer = _player.transform.position - transform.position;
            float dist = toPlayer.magnitude;

            if (dist <= attackRange)
            {
                // Xoay nòng ngắm về phía người chơi
                if (swivelHead != null)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    swivelHead.rotation = Quaternion.Slerp(swivelHead.rotation, targetRot, Time.deltaTime * 5.5f);
                }

                if (Time.time >= _nextFireTime)
                {
                    _nextFireTime = Time.time + fireInterval + Random.Range(-0.2f, 0.3f);
                    Vector3 muzzle = muzzlePoint != null ? muzzlePoint.position : (transform.position + toPlayer.normalized * 1.5f);
                    SpaceLaserProjectile.SpawnBolt(muzzle, toPlayer.normalized, false, gameObject, 100f, bulletDamage);
                }
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (_isDestroyed) return;
            SpaceTrafficShipAI.TriggerFleetHostileAlert(60f);

            _currentHealth -= damage;
            if (_currentHealth <= 0f)
            {
                _isDestroyed = true;
                SpaceExplosionVFX.SpawnExplosion(transform.position, 3.8f, false, null);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Lõi năng lượng phát sáng ở mặt tiền Hangar Căn cứ địch (Carrier Power Core):
    /// - Khi bị bắn sẽ làm căn cứ kích hoạt báo động ngay lập tức.
    /// - Khi hết máu (bị phá hủy), toàn bộ tổ hợp căn cứ 06_Enemy_Defense_Spawner sẽ NỔ TUNG liên hoàn!
    /// </summary>
    public class CarrierEnergyCore : MonoBehaviour
    {
        public float maxHealth = 150f;
        public ProjectZoneEnemySpawner parentCarrierSpawner;

        private float _currentHealth;
        private bool _isDestroyed;

        private void Start()
        {
            _currentHealth = maxHealth;
            if (parentCarrierSpawner == null)
            {
                parentCarrierSpawner = GetComponentInParent<ProjectZoneEnemySpawner>();
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (_isDestroyed) return;
            SpaceTrafficShipAI.TriggerFleetHostileAlert(60f);

            _currentHealth -= damage;
            if (_currentHealth <= 0f)
            {
                _isDestroyed = true;
                if (parentCarrierSpawner != null)
                {
                    parentCarrierSpawner.StartCoroutine(parentCarrierSpawner.CatastrophicExplosionSequence());
                }
                else
                {
                    SpaceExplosionVFX.SpawnExplosion(transform.position, 8.5f, false, null);
                    Destroy(gameObject);
                }
            }
        }
    }
}
