using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Hoạt họa sinh động cho Trạm Xuất Phát Trung Tâm (Spawn_Zone — Starport Launch Hub):
    /// 1. Vòng Hologram dưới bệ phóng xoay ngược chiều + nhịp thở phát sáng.
    /// 2. Đèn đường băng (Runway Chasing Chevrons) chạy đuổi liên tục hướng ra Cổng Phóng.
    /// 3. Sọc ngang màn hình CRT (Scrolling Scanlines) + Tia quét Laser chạy dọc trên bảng STARPORT ALPHA.
    /// 4. Hai khối Cầu Hologram Đa Trục (3D Gyroscope Orbs) tự xoay 3 chiều và lơ lửng trên máy chiếu hai bên sườn.
    /// 5. Tia quét Năng lượng (Gate Laser Scanners) trượt dọc theo 2 cột Cổng Phóng.
    /// 6. Hai Drone tuần tra tự hành (Starport Patrol Drones) bay lượn vòng số 8 quanh trạm xuất phát.
    /// 7. Các cụm Đá Tinh Thể Năng Lượng lơ lửng nhấp nhô (Anti-Gravity Floating Crystals) quanh rìa trạm.
    /// </summary>
    [ExecuteAlways]
    public class SpawnZoneAnimator : MonoBehaviour
    {
        [Header("0. Đồng Bộ Vị Trí Tàu Theo Spawn_Zone (Dynamic Offset)")]
        [Tooltip("Tự động di chuyển tàu và Camera theo Spawn_Zone khi bạn kéo Spawn_Zone trong Scene")]
        public bool syncShipWithZoneInEditor = true;
        public PlayerSpaceshipController playerShip;
        [Header("1. Vòng Bệ Phóng & Radar")]
        public Transform[] rotatingRings;
        public Transform[] rotatingRadars;

        [Header("2. Đèn Đường Băng & Đèn Nhịp Thở")]
        public Renderer[] runwayLightBars;
        public Light[] pulsingLights;

        [Header("3. Màn Hình CRT Scanlines & Tia Quét Bảng Chào Mừng")]
        public Renderer welcomeScanlineRenderer;
        public Transform welcomeLaserBar;
        public float welcomeScreenHeight = 2.0f;

        [Header("4. Khối Cầu Hologram 3D Đa Trục Hai Bên Sườn")]
        public Transform[] gyroCores;
        public Transform[] gyroRingsX;
        public Transform[] gyroRingsY;
        public Transform[] gyroRingsZ;

        [Header("5. Tia Quét Cổng Phóng (Gate Scanners)")]
        public Transform[] gateScannerBars;
        public float gateScanBaseY = 5.6f;
        public float gateScanAmplitude = 2.8f;

        [Header("6. Drone Tuần Tra Cảng Vũ Trụ (Figure-8 Patrol Drones)")]
        public Transform[] patrolDrones;
        public float droneOrbitRadiusX = 13.5f;
        public float droneOrbitRadiusZ = 9.5f;
        public float droneBaseHeight = 7.2f;

        [Header("7. Tinh Thể Lơ Lửng Quanh Trạm (Floating Crystals)")]
        public Transform[] floatingCrystals;

        private MaterialPropertyBlock _mpb;
        private float[] _crystalBaseY;
        private float[] _gyroBaseY;

        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            CacheBaseHeights();
        }

        private void Start()
        {
            CacheBaseHeights();
        }

        private void CacheBaseHeights()
        {
            if (floatingCrystals != null && floatingCrystals.Length > 0)
            {
                _crystalBaseY = new float[floatingCrystals.Length];
                for (int i = 0; i < floatingCrystals.Length; i++)
                    if (floatingCrystals[i] != null) _crystalBaseY[i] = floatingCrystals[i].localPosition.y;
            }
            if (gyroCores != null && gyroCores.Length > 0)
            {
                _gyroBaseY = new float[gyroCores.Length];
                for (int i = 0; i < gyroCores.Length; i++)
                    if (gyroCores[i] != null) _gyroBaseY[i] = gyroCores[i].localPosition.y;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (syncShipWithZoneInEditor && transform.hasChanged)
                {
                    transform.hasChanged = false;
                    if (playerShip == null) playerShip = FindFirstObjectByType<PlayerSpaceshipController>();
                    if (playerShip != null)
                    {
                        playerShip.spawnZone = transform;
                        playerShip.GetDynamicSpawnPose(out Vector3 pos, out Quaternion rot);
                        playerShip.transform.SetPositionAndRotation(pos, rot);

                        var camFollow = FindFirstObjectByType<SpaceshipCameraFollow>();
                        if (camFollow != null)
                        {
                            camFollow.transform.position = pos - (rot * Vector3.forward) * camFollow.distance + Vector3.up * camFollow.height;
                            camFollow.transform.LookAt(pos + (rot * Vector3.forward) * 4f);
                        }
                    }
                }
                return;
            }

            float dt = Time.deltaTime;
            float t = Time.time;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            // 1. Vòng Hologram dưới bệ phóng xoay ngược chiều & nhấp nhô nhẹ
            if (rotatingRings != null)
            {
                for (int i = 0; i < rotatingRings.Length; i++)
                {
                    if (rotatingRings[i] == null) continue;
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    rotatingRings[i].Rotate(0f, dir * (28f + i * 12f) * dt, 0f, Space.Self);
                }
            }

            // 2. Chảo Radar xoay
            if (rotatingRadars != null)
            {
                for (int i = 0; i < rotatingRadars.Length; i++)
                {
                    if (rotatingRadars[i] == null) continue;
                    rotatingRadars[i].Rotate(0f, (32f + i * 10f) * dt, 0f, Space.World);
                }
            }

            // 3. Đèn đường băng chạy đuổi liên tục ra Cổng Phóng
            if (runwayLightBars != null && runwayLightBars.Length > 0)
            {
                int pairCount = runwayLightBars.Length / 2;
                if (pairCount > 0)
                {
                    float cycle = (t * 5.5f) % pairCount;
                    for (int i = 0; i < runwayLightBars.Length; i++)
                    {
                        var r = runwayLightBars[i];
                        if (r == null) continue;
                        int pairIdx = i / 2;
                        float dist = Mathf.Abs(pairIdx - cycle);
                        if (dist > pairCount * 0.5f) dist = pairCount - dist;
                        float glow = Mathf.Clamp01(1f - dist * 0.7f);

                        Color col = Color.Lerp(new Color(0.05f, 0.35f, 0.55f, 0.8f), new Color(0.3f, 1f, 1f, 1f), glow);
                        r.GetPropertyBlock(_mpb);
                        _mpb.SetColor(BaseColorId, col);
                        _mpb.SetColor(EmissionColorId, col * (1.2f + glow * 3.2f));
                        r.SetPropertyBlock(_mpb);
                    }
                }
            }

            // 4. Sọc ngang màn hình CRT & Tia quét Laser trên bảng STARPORT ALPHA
            if (welcomeScanlineRenderer != null)
            {
                float scrollY = -t * 0.65f;
                Vector4 st = new Vector4(1f, 1.8f, 0f, scrollY);
                welcomeScanlineRenderer.GetPropertyBlock(_mpb);
                _mpb.SetVector(BaseMapStId, st);
                _mpb.SetVector(MainTexStId, st);
                welcomeScanlineRenderer.SetPropertyBlock(_mpb);
            }

            if (welcomeLaserBar != null)
            {
                float scanT = Mathf.PingPong(t * 0.85f, 1f);
                float yPos = Mathf.Lerp(welcomeScreenHeight * 0.45f, -welcomeScreenHeight * 0.45f, scanT);
                Vector3 lp = welcomeLaserBar.localPosition;
                welcomeLaserBar.localPosition = new Vector3(lp.x, yPos, lp.z);
            }

            // 5. Khối cầu Gyroscope 3D đa trục trên 2 máy chiếu sườn
            if (gyroCores != null)
            {
                if (_gyroBaseY == null || _gyroBaseY.Length != gyroCores.Length) CacheBaseHeights();
                for (int i = 0; i < gyroCores.Length; i++)
                {
                    if (gyroCores[i] == null) continue;
                    Vector3 p = gyroCores[i].localPosition;
                    float bob = Mathf.Sin(t * 2.4f + i * 1.8f) * 0.22f;
                    gyroCores[i].localPosition = new Vector3(p.x, _gyroBaseY[i] + bob, p.z);
                    gyroCores[i].Rotate(0f, 45f * dt, 0f, Space.Self);
                }
            }
            if (gyroRingsX != null)
                for (int i = 0; i < gyroRingsX.Length; i++)
                    if (gyroRingsX[i] != null) gyroRingsX[i].Rotate(75f * dt, 25f * dt, 0f, Space.Self);
            if (gyroRingsY != null)
                for (int i = 0; i < gyroRingsY.Length; i++)
                    if (gyroRingsY[i] != null) gyroRingsY[i].Rotate(0f, -95f * dt, 40f * dt, Space.Self);
            if (gyroRingsZ != null)
                for (int i = 0; i < gyroRingsZ.Length; i++)
                    if (gyroRingsZ[i] != null) gyroRingsZ[i].Rotate(35f * dt, 0f, 110f * dt, Space.Self);

            // 6. Tia quét Năng lượng trượt lên xuống trên 2 cột Cổng Phóng
            if (gateScannerBars != null)
            {
                for (int i = 0; i < gateScannerBars.Length; i++)
                {
                    if (gateScannerBars[i] == null) continue;
                    Vector3 p = gateScannerBars[i].localPosition;
                    float y = gateScanBaseY + Mathf.Sin(t * 2.2f + i * Mathf.PI) * gateScanAmplitude;
                    gateScannerBars[i].localPosition = new Vector3(p.x, y, p.z);
                }
            }

            // 7. Drone tuần tra bay lượn quỹ đạo số 8 (Lemniscate / Orbit) phía trên cảng xuất phát
            if (patrolDrones != null)
            {
                for (int i = 0; i < patrolDrones.Length; i++)
                {
                    var d = patrolDrones[i];
                    if (d == null) continue;

                    float phase = t * 0.55f + i * Mathf.PI;
                    Vector3 curPos = GetDronePoint(phase, i);
                    Vector3 nextPos = GetDronePoint(phase + 0.05f, i);
                    d.localPosition = curPos;

                    Vector3 dir = nextPos - curPos;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        float bank = Mathf.Cos(phase * 2f) * -24f;
                        d.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
                    }
                }
            }

            // 8. Các cụm Tinh thể & Đá lơ lửng quanh rìa cảng
            if (floatingCrystals != null)
            {
                if (_crystalBaseY == null || _crystalBaseY.Length != floatingCrystals.Length) CacheBaseHeights();
                for (int i = 0; i < floatingCrystals.Length; i++)
                {
                    var c = floatingCrystals[i];
                    if (c == null) continue;
                    Vector3 p = c.localPosition;
                    float bob = Mathf.Sin(t * 1.8f + i * 1.3f) * 0.45f;
                    c.localPosition = new Vector3(p.x, _crystalBaseY[i] + bob, p.z);
                    c.Rotate(12f * dt, (22f + (i % 3) * 8f) * dt, 8f * dt, Space.Self);
                }
            }

            // 9. Đèn nhịp thở
            if (pulsingLights != null)
            {
                for (int i = 0; i < pulsingLights.Length; i++)
                {
                    if (pulsingLights[i] == null) continue;
                    pulsingLights[i].intensity = 2.6f + Mathf.Sin(t * 3.5f + i * 1.4f) * 0.85f;
                }
            }
        }

        private Vector3 GetDronePoint(float phase, int idx)
        {
            float x = Mathf.Sin(phase) * droneOrbitRadiusX;
            float z = 2f + Mathf.Sin(phase * 2f) * (droneOrbitRadiusZ * 0.65f);
            float y = droneBaseHeight + idx * 1.4f + Mathf.Cos(phase * 2f) * 1.1f;
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Dòng chữ Hologram lơ lửng trên đỉnh mỗi Khu vực (Zone Header Billboard):
    /// - Không cần bảng vật lý cồng kềnh, hiển thị trực tiếp tên khu vực trên bầu trời phía trên đỉnh vùng đó.
    /// - Luôn tự động xoay hướng thẳng về tầm mắt (Camera) của người chơi từ mọi góc nhìn.
    /// </summary>
    [ExecuteAlways]
    public class ZoneHeaderBillboard : MonoBehaviour
    {
        public Vector3 baseLocalPosition = new Vector3(0f, 22f, 0f);
        public float bobAmplitude = 0.55f;
        public float bobFrequency = 1.6f;
        public bool lockVerticalPitch = true;
        public Transform downArrowTransform;
        public float arrowBaseLocalY = -3.8f;

        [Header("Khoảng Cách Ẩn Khi Tàu Đến Gần")]
        public float fadeStartDistance = 220f;
        public float fadeEndDistance = 110f;
        public Transform targetPlayerTransform;

        private Camera _cachedCam;
        private Vector3 _initialScale = Vector3.one;

        private void OnEnable()
        {
            if (baseLocalPosition == Vector3.zero)
            {
                baseLocalPosition = transform.localPosition;
            }
            if (_initialScale == Vector3.one && transform.localScale != Vector3.zero)
            {
                _initialScale = transform.localScale;
            }
        }

        private void LateUpdate()
        {
            if (Application.isPlaying)
            {
                if (bobAmplitude > 0.001f)
                {
                    float yOffset = Mathf.Sin(Time.time * bobFrequency + baseLocalPosition.x * 0.2f) * bobAmplitude;
                    transform.localPosition = baseLocalPosition + new Vector3(0f, yOffset, 0f);
                }

                if (downArrowTransform != null)
                {
                    Vector3 ap = downArrowTransform.localPosition;
                    float arrowBob = Mathf.Sin(Time.time * 3.4f) * 0.35f;
                    downArrowTransform.localPosition = new Vector3(ap.x, arrowBaseLocalY + arrowBob, ap.z);
                }

                // Tự động thu nhỏ biến mất khi tàu người chơi đến gần
                if (targetPlayerTransform == null)
                {
                    var player = FindFirstObjectByType<PlayerSpaceshipController>();
                    if (player != null) targetPlayerTransform = player.transform;
                }

                if (targetPlayerTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, targetPlayerTransform.position);
                    float alpha = Mathf.Clamp01((dist - fadeEndDistance) / (fadeStartDistance - fadeEndDistance));
                    transform.localScale = _initialScale * alpha;
                }
            }

            if (_cachedCam == null || !_cachedCam.isActiveAndEnabled)
            {
                _cachedCam = Camera.main;
                if (_cachedCam == null)
                {
                    _cachedCam = FindFirstObjectByType<Camera>();
                }
            }

            if (_cachedCam != null)
            {
                Vector3 dirFromCam = transform.position - _cachedCam.transform.position;
                if (lockVerticalPitch)
                {
                    dirFromCam.y = 0f;
                }

                if (dirFromCam.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(dirFromCam.normalized, Vector3.up);
                }
            }
        }

        /// <summary>
        /// Tạo bộ 3 Mesh chuẩn Sci-Fi Hologram Beacon giống hệt thiết kế mẫu:
        /// 1) Nền vát 8 góc (Chamfered Octagon Fill)
        /// 2) Viền Neon Cyan bo 2 đầu (Chamfered Corner Brackets)
        /// 3) Mũi tên chỉ xuống ▼ vát chữ V (Notched Downward Pointer)
        /// </summary>
        public static void BuildChamferedBadgeMeshes(float width, float height, float chamfer, out Mesh fillMesh, out Mesh borderMesh, out Mesh arrowMesh)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float c = Mathf.Min(chamfer, Mathf.Min(hw * 0.45f, hh * 0.65f));

            // 1. Fill Mesh (8-vertex Chamfered Octagon, double-sided)
            Vector3[] boundary = new Vector3[8]
            {
                new Vector3(-hw + c,  hh, 0f),
                new Vector3( hw - c,  hh, 0f),
                new Vector3( hw,  hh - c, 0f),
                new Vector3( hw, -hh + c, 0f),
                new Vector3( hw - c, -hh, 0f),
                new Vector3(-hw + c, -hh, 0f),
                new Vector3(-hw, -hh + c, 0f),
                new Vector3(-hw,  hh - c, 0f)
            };

            fillMesh = new Mesh { name = "Mesh_SciFi_Badge_Fill" };
            Vector3[] fillVerts = new Vector3[9];
            fillVerts[0] = Vector3.zero;
            for (int i = 0; i < 8; i++) fillVerts[i + 1] = boundary[i];

            int[] fillTris = new int[8 * 3 * 2];
            int ti = 0;
            for (int i = 0; i < 8; i++)
            {
                int cur = i + 1;
                int next = ((i + 1) % 8) + 1;
                // Front face
                fillTris[ti++] = 0; fillTris[ti++] = cur; fillTris[ti++] = next;
                // Back face
                fillTris[ti++] = 0; fillTris[ti++] = next; fillTris[ti++] = cur;
            }
            fillMesh.vertices = fillVerts;
            fillMesh.triangles = fillTris;
            fillMesh.RecalculateNormals();
            fillMesh.RecalculateBounds();

            // 2. Border Mesh (Thick glowing cyan corner brackets + thin top/bottom frame lines)
            borderMesh = new Mesh { name = "Mesh_SciFi_Badge_Border" };
            var bVerts = new System.Collections.Generic.List<Vector3>();
            var bTris = new System.Collections.Generic.List<int>();

            void AddQuadStrip(Vector3 p0, Vector3 p1, float thickness, float zOffset)
            {
                Vector3 dir = (p1 - p0).normalized;
                Vector3 n = new Vector3(-dir.y, dir.x, 0f) * (thickness * 0.5f);
                Vector3 z = new Vector3(0f, 0f, zOffset);
                int baseIdx = bVerts.Count;
                bVerts.Add(p0 - n + z);
                bVerts.Add(p0 + n + z);
                bVerts.Add(p1 + n + z);
                bVerts.Add(p1 - n + z);

                bTris.Add(baseIdx + 0); bTris.Add(baseIdx + 1); bTris.Add(baseIdx + 2);
                bTris.Add(baseIdx + 0); bTris.Add(baseIdx + 2); bTris.Add(baseIdx + 3);
                bTris.Add(baseIdx + 0); bTris.Add(baseIdx + 2); bTris.Add(baseIdx + 1);
                bTris.Add(baseIdx + 0); bTris.Add(baseIdx + 3); bTris.Add(baseIdx + 2);
            }

            float thickBracket = 0.28f;
            float thinLine = 0.08f;
            float bracketSpan = (hw - c) * 0.42f;

            // Left C-Bracket (Top-left horizontal stub -> Top-left chamfer -> Left vertical -> Bottom-left chamfer -> Bottom-left horizontal stub)
            AddQuadStrip(new Vector3(-hw + c + bracketSpan, hh, 0f), boundary[0], thickBracket, -0.02f);
            AddQuadStrip(boundary[0], boundary[7], thickBracket, -0.02f);
            AddQuadStrip(boundary[7], boundary[6], thickBracket, -0.02f);
            AddQuadStrip(boundary[6], boundary[5], thickBracket, -0.02f);
            AddQuadStrip(boundary[5], new Vector3(-hw + c + bracketSpan, -hh, 0f), thickBracket, -0.02f);

            // Right C-Bracket
            AddQuadStrip(new Vector3(hw - c - bracketSpan, hh, 0f), boundary[1], thickBracket, -0.02f);
            AddQuadStrip(boundary[1], boundary[2], thickBracket, -0.02f);
            AddQuadStrip(boundary[2], boundary[3], thickBracket, -0.02f);
            AddQuadStrip(boundary[3], boundary[4], thickBracket, -0.02f);
            AddQuadStrip(boundary[4], new Vector3(hw - c - bracketSpan, -hh, 0f), thickBracket, -0.02f);

            // Thin connecting top & bottom frame lines with subtle center gap
            float gap = (hw - c) * 0.16f;
            AddQuadStrip(new Vector3(-hw + c + bracketSpan, hh, 0f), new Vector3(-gap, hh, 0f), thinLine, -0.01f);
            AddQuadStrip(new Vector3(gap, hh, 0f), new Vector3(hw - c - bracketSpan, hh, 0f), thinLine, -0.01f);
            AddQuadStrip(new Vector3(-hw + c + bracketSpan, -hh, 0f), new Vector3(-gap, -hh, 0f), thinLine, -0.01f);
            AddQuadStrip(new Vector3(gap, -hh, 0f), new Vector3(hw - c - bracketSpan, -hh, 0f), thinLine, -0.01f);

            borderMesh.SetVertices(bVerts);
            borderMesh.SetTriangles(bTris, 0);
            borderMesh.RecalculateNormals();
            borderMesh.RecalculateBounds();

            // 3. Notched Downward Pointer Arrow Mesh (▼)
            arrowMesh = new Mesh { name = "Mesh_SciFi_Badge_Arrow" };
            float aw = width * 0.11f;
            float ah = height * 0.48f;
            Vector3[] aVerts = new Vector3[4]
            {
                new Vector3(-aw, 0f, 0f),
                new Vector3( 0f, -ah * 0.22f, 0f),
                new Vector3( aw, 0f, 0f),
                new Vector3( 0f, -ah, 0f)
            };
            int[] aTris = new int[]
            {
                0, 1, 3,
                1, 2, 3,
                0, 3, 1,
                1, 3, 2
            };
            arrowMesh.vertices = aVerts;
            arrowMesh.triangles = aTris;
            arrowMesh.RecalculateNormals();
            arrowMesh.RecalculateBounds();
        }
    }
}
