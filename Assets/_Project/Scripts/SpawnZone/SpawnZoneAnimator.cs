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

        [Header("8. Cửa Hầm Chiến Hạm & Quy Trình Phóng Tàu (Capital Hangar Blast Doors)")]
        public Transform blastDoorLeft;
        public Transform blastDoorRight;
        public Transform blastDoorUpper;
        public Vector3 doorLeftClosedPos = new Vector3(-4.6f, 8.0f, 16.5f);
        public Vector3 doorLeftOpenPos = new Vector3(-13.8f, 8.0f, 16.5f);
        public Vector3 doorRightClosedPos = new Vector3(4.6f, 8.0f, 16.5f);
        public Vector3 doorRightOpenPos = new Vector3(13.8f, 8.0f, 16.5f);
        public Vector3 doorUpperClosedPos = new Vector3(0f, 13.0f, 15.9f);
        public Vector3 doorUpperOpenPos = new Vector3(0f, 17.8f, 15.9f);
        public Renderer hangarForcefieldRenderer;
        public Renderer[] doorStatusStripRenderers;
        public Light[] doorWarningLights;
        public float doorPreDelay = 0.25f;
        public float doorOpenDuration = 1.60f;
        public float launchHoldDuration = 1.25f;
        public float launchBoostDuration = 1.85f;
        public float launchExitSpeed = 30f;
        public bool autoOpenForReturningShip = true;
        public float returnAutoOpenDistance = 62f;
        public Vector3 hangarMouthLocalPos = new Vector3(0f, 6f, 16.5f);

        [Header("8B. Hoạt Ảnh Bước Ra Từ Căn Cứ & Leo Lên Tàu (Boarding Intro)")]
        public bool playBoardingIntroOnStart = true;
        public bool playBoardingIntroOnRespawn = false;
        public float walkDuration = 4.6f;
        public float climbDuration = 1.85f;
        public Vector3 airlockStartLocalPos = new Vector3(0f, 4.05f, -23.6f);
        public Vector3 airlockMidLocalPos = new Vector3(-2.1f, 4.05f, -16.2f);
        public Vector3 shipSideBoardLocalPos = new Vector3(-3.35f, 4.05f, -8.8f);
        public Vector3 wingStepLocalPos = new Vector3(-1.45f, 5.55f, -7.0f);
        public Vector3 canopyOverLocalPos = new Vector3(-0.35f, 6.45f, -6.0f);

        public enum HangarOnboardingState
        {
            None = 0,
            WalkingFromBase = 1,
            ClimbingIntoShip = 2,
            AwaitingEnterBriefing = 3,
            DoorsOpening = 4
        }

        [Header("9. Hạm Đội Chiến Hạm & Phi Đội Hộ Tống Bên Cạnh")]
        public Transform[] companionCapitalShips;
        public Transform[] wingEscortFighters;
        public Transform[] flagshipEngineGlows;
        public Transform[] bridgeSensorSpinners;

        public static SpawnZoneAnimator ActiveInstance { get; private set; }
        public static bool IsAnyIntroWalkActive => ActiveInstance != null && ActiveInstance.IsIntroWalkActive;

        public HangarOnboardingState CurrentOnboardingState => _onboardingState;
        public bool IsIntroWalkActive =>
            _onboardingState == HangarOnboardingState.WalkingFromBase ||
            _onboardingState == HangarOnboardingState.ClimbingIntoShip;
        public bool IsAwaitingEnterBriefing => _onboardingState == HangarOnboardingState.AwaitingEnterBriefing;
        public bool IsFlightLockedForOnboarding => IsIntroWalkActive || IsAwaitingEnterBriefing;
        public bool AreHangarDoorsOpen => _doorOpenProgress >= 0.85f;

        private MaterialPropertyBlock _mpb;
        private float[] _crystalBaseY;
        private float[] _gyroBaseY;
        private Vector3[] _companionBasePos;
        private Quaternion[] _companionBaseRot;
        private float[] _wingBaseRadiusX;
        private float[] _wingBaseRadiusZ;
        private float[] _wingBaseY;
        private Vector3[] _engineBaseScale;
        private Collider[] _leftDoorColliders;
        private Collider[] _rightDoorColliders;
        private Collider[] _upperDoorColliders;
        private float _doorOpenProgress = 1f;
        private bool _launchSequenceActive;
        private float _launchTimer;
        private bool _awaitingManualDoorOpen;
        private bool _hasPlayedInitialBoarding;
        private HangarOnboardingState _onboardingState = HangarOnboardingState.None;
        private float _onboardingTimer;

        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            ActiveInstance = this;
            _mpb = new MaterialPropertyBlock();
            CacheBaseHeights();
        }

        private void OnDestroy()
        {
            if (ActiveInstance == this) ActiveInstance = null;
        }

        private void Start()
        {
            CacheBaseHeights();

            // Ẩn drone cũ của trạm nhỏ nếu còn sót
            if (patrolDrones != null)
            {
                for (int i = 0; i < patrolDrones.Length; i++)
                {
                    if (patrolDrones[i] != null)
                    {
                        patrolDrones[i].gameObject.SetActive(false);
                    }
                }
                patrolDrones = new Transform[0];
            }

            if (Application.isPlaying && playerShip == null)
            {
                playerShip = FindFirstObjectByType<PlayerSpaceshipController>();
            }
        }

        [ContextMenu("Preview Hangar Doors Closed")]
        public void PreviewDoorsClosed()
        {
            ApplyDoorOpenProgress(0f);
        }

        [ContextMenu("Preview Hangar Doors Open")]
        public void PreviewDoorsOpen()
        {
            ApplyDoorOpenProgress(1f);
        }

        /// <summary>
        /// Đóng kín cửa hầm ngay lập tức rồi chạy hoạt ảnh bước ra từ căn cứ -> leo lên tàu -> chờ nhấn Enter mở cửa.
        /// Không tự động đẩy tàu bay ra ngoài.
        /// </summary>
        public void BeginHangarOnboardingSequence(PlayerSpaceshipController targetShip = null, bool isInitialSpawn = true)
        {
            ActiveInstance = this;
            if (targetShip != null) playerShip = targetShip;

            _launchSequenceActive = false;
            _launchTimer = 0f;
            _awaitingManualDoorOpen = true;
            _onboardingTimer = 0f;
            ApplyDoorOpenProgress(0f);

            bool shouldWalk = (isInitialSpawn && playBoardingIntroOnStart && !_hasPlayedInitialBoarding)
                              || (!isInitialSpawn && playBoardingIntroOnRespawn);

            if (shouldWalk && Application.isPlaying)
            {
                _hasPlayedInitialBoarding = true;
                _onboardingState = HangarOnboardingState.WalkingFromBase;
            }
            else
            {
                _onboardingState = HangarOnboardingState.AwaitingEnterBriefing;
            }
        }

        /// <summary>
        /// Cho phép bỏ qua đoạn đi bộ/leo tàu để vào ngay buồng lái chờ nhấn Enter (cửa hầm vẫn đóng).
        /// </summary>
        public void SkipIntroWalkToCockpit()
        {
            if (!IsIntroWalkActive) return;
            _onboardingState = HangarOnboardingState.AwaitingEnterBriefing;
            _onboardingTimer = 0f;
        }

        /// <summary>
        /// Được gọi khi người chơi nhấn Enter sau khi xem bảng chỉ dẫn: mở cửa hầm chiến hạm, tàu vẫn đứng yên để tự lái.
        /// </summary>
        public void OpenHangarDoorsOnEnter()
        {
            if (_onboardingState != HangarOnboardingState.AwaitingEnterBriefing) return;
            _awaitingManualDoorOpen = false;
            _onboardingState = HangarOnboardingState.DoorsOpening;
            _launchSequenceActive = true;
            _launchTimer = 0f;
        }

        public void TriggerHangarLaunchSequence(PlayerSpaceshipController targetShip = null)
        {
            BeginHangarOnboardingSequence(targetShip, !_hasPlayedInitialBoarding);
        }

        /// <summary>
        /// Cung cấp vị trí & góc xoay camera trong lúc nhân vật bước ra từ cửa căn cứ và leo lên buồng lái phi thuyền.
        /// </summary>
        public bool TryGetOnboardingCameraPose(
            Vector3 seatWorldPos,
            Quaternion seatWorldRot,
            float walkFov,
            float seatFov,
            out Vector3 camWorldPos,
            out Quaternion camWorldRot,
            out float outFov,
            out bool showCockpitInterior)
        {
            camWorldPos = seatWorldPos;
            camWorldRot = seatWorldRot;
            outFov = seatFov;
            showCockpitInterior = true;

            if (!IsIntroWalkActive) return false;

            Vector3 shipCockpitWorld = playerShip != null
                ? playerShip.transform.TransformPoint(new Vector3(0f, 1.35f, 0.35f))
                : transform.TransformPoint(new Vector3(0f, 5.8f, -6.15f));

            if (_onboardingState == HangarOnboardingState.WalkingFromBase)
            {
                float dur = Mathf.Max(0.5f, walkDuration);
                float u = Mathf.Clamp01(_onboardingTimer / dur);
                float s = u * u * (3f - 2f * u);
                float inv = 1f - s;

                Vector3 localPos = (inv * inv) * airlockStartLocalPos
                                 + (2f * inv * s) * airlockMidLocalPos
                                 + (s * s) * shipSideBoardLocalPos;

                // Nhịp bước chân nhẹ + nghiêng đầu tự nhiên khi bước ra từ khoang căn cứ
                float stepEnvelope = Mathf.Sin(u * Mathf.PI);
                float bobY = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 9f)) * 0.055f * stepEnvelope;
                float swayX = Mathf.Sin(u * Mathf.PI * 4.5f) * 0.025f * stepEnvelope;
                float rollDeg = Mathf.Sin(u * Mathf.PI * 4.5f) * 0.85f * stepEnvelope;
                localPos += new Vector3(swayX, bobY, 0f);

                camWorldPos = transform.TransformPoint(localPos);

                // Ban đầu nhìn dọc hầm chứa & cửa hầm đóng, nửa sau hướng dần về phi thuyền đang đỗ
                float glanceX = Mathf.Sin(u * Mathf.PI * 2.0f) * 3.5f * (1f - u * 0.55f);
                Vector3 hallLookWorld = transform.TransformPoint(new Vector3(glanceX, 5.5f, 10.0f));
                float focusShip = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.32f) / 0.62f));
                Vector3 lookTarget = Vector3.Lerp(hallLookWorld, shipCockpitWorld, focusShip);

                Quaternion baseRot = Quaternion.LookRotation((lookTarget - camWorldPos).normalized, transform.up);
                camWorldRot = baseRot * Quaternion.Euler(0f, 0f, rollDeg);
                outFov = walkFov;
                showCockpitInterior = false;
                return true;
            }
            else
            {
                float dur = Mathf.Max(0.4f, climbDuration);
                float u = Mathf.Clamp01(_onboardingTimer / dur);

                Vector3 worldSide = transform.TransformPoint(shipSideBoardLocalPos);
                Vector3 worldStep = transform.TransformPoint(wingStepLocalPos);
                Vector3 worldCanopy = transform.TransformPoint(canopyOverLocalPos);

                if (u < 0.44f)
                {
                    float sub = Mathf.SmoothStep(0f, 1f, u / 0.44f);
                    camWorldPos = Vector3.Lerp(worldSide, worldStep, sub);
                    camWorldPos.y += Mathf.Sin(sub * Mathf.PI) * 0.14f;
                }
                else
                {
                    float sub = Mathf.SmoothStep(0f, 1f, (u - 0.44f) / 0.56f);
                    float inv = 1f - sub;
                    camWorldPos = (inv * inv) * worldStep
                                + (2f * inv * sub) * worldCanopy
                                + (sub * sub) * seatWorldPos;
                }

                Vector3 climbLookTarget = Vector3.Lerp(shipCockpitWorld, seatWorldPos + seatWorldRot * (Vector3.forward * 22f), Mathf.SmoothStep(0f, 1f, u));
                Quaternion lookRot = Quaternion.LookRotation((climbLookTarget - camWorldPos).normalized, transform.up);
                float rotBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.48f) / 0.52f));
                camWorldRot = Quaternion.Slerp(lookRot, seatWorldRot, rotBlend);

                outFov = Mathf.Lerp(walkFov, seatFov, Mathf.SmoothStep(0f, 1f, u));
                showCockpitInterior = u >= 0.88f;
                return true;
            }
        }

        private void ApplyDoorOpenProgress(float ratio)
        {
            _doorOpenProgress = Mathf.Clamp01(ratio);
            float eased = _doorOpenProgress * _doorOpenProgress * (3f - 2f * _doorOpenProgress);

            if (blastDoorLeft != null)
                blastDoorLeft.localPosition = Vector3.Lerp(doorLeftClosedPos, doorLeftOpenPos, eased);
            if (blastDoorRight != null)
                blastDoorRight.localPosition = Vector3.Lerp(doorRightClosedPos, doorRightOpenPos, eased);
            if (blastDoorUpper != null)
                blastDoorUpper.localPosition = Vector3.Lerp(doorUpperClosedPos, doorUpperOpenPos, eased);

            bool passable = _doorOpenProgress >= 0.48f;
            SetDoorCollidersEnabled(blastDoorLeft, ref _leftDoorColliders, !passable);
            SetDoorCollidersEnabled(blastDoorRight, ref _rightDoorColliders, !passable);
            SetDoorCollidersEnabled(blastDoorUpper, ref _upperDoorColliders, !passable);

            if (hangarForcefieldRenderer != null)
            {
                float fieldVis = Mathf.Clamp01(1f - _doorOpenProgress * 1.45f);
                hangarForcefieldRenderer.enabled = fieldVis > 0.02f;
                if (hangarForcefieldRenderer.enabled)
                {
                    if (_mpb == null) _mpb = new MaterialPropertyBlock();
                    hangarForcefieldRenderer.GetPropertyBlock(_mpb);
                    _mpb.SetColor(BaseColorId, new Color(0.18f, 0.85f, 1f, 0.28f * fieldVis));
                    _mpb.SetColor(EmissionColorId, new Color(0.2f, 0.95f, 1.3f, 1f) * (1.4f * fieldVis));
                    hangarForcefieldRenderer.SetPropertyBlock(_mpb);
                }
            }
        }

        private static void SetDoorCollidersEnabled(Transform root, ref Collider[] cache, bool enabled)
        {
            if (root == null) return;
            if (cache == null) cache = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cache.Length; i++)
            {
                if (cache[i] != null && cache[i].enabled != enabled)
                    cache[i].enabled = enabled;
            }
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
            if (companionCapitalShips != null && companionCapitalShips.Length > 0)
            {
                _companionBasePos = new Vector3[companionCapitalShips.Length];
                _companionBaseRot = new Quaternion[companionCapitalShips.Length];
                for (int i = 0; i < companionCapitalShips.Length; i++)
                {
                    if (companionCapitalShips[i] != null)
                    {
                        _companionBasePos[i] = companionCapitalShips[i].localPosition;
                        _companionBaseRot[i] = companionCapitalShips[i].localRotation;
                    }
                }
            }
            if (wingEscortFighters != null && wingEscortFighters.Length > 0)
            {
                _wingBaseRadiusX = new float[wingEscortFighters.Length];
                _wingBaseRadiusZ = new float[wingEscortFighters.Length];
                _wingBaseY = new float[wingEscortFighters.Length];
                for (int i = 0; i < wingEscortFighters.Length; i++)
                {
                    if (wingEscortFighters[i] != null)
                    {
                        Vector3 p = wingEscortFighters[i].localPosition;
                        _wingBaseRadiusX[i] = Mathf.Max(48f, Mathf.Abs(p.x));
                        _wingBaseRadiusZ[i] = Mathf.Max(42f, Mathf.Abs(p.z));
                        _wingBaseY[i] = p.y;
                    }
                }
            }
            if (flagshipEngineGlows != null && flagshipEngineGlows.Length > 0)
            {
                _engineBaseScale = new Vector3[flagshipEngineGlows.Length];
                for (int i = 0; i < flagshipEngineGlows.Length; i++)
                {
                    if (flagshipEngineGlows[i] != null)
                        _engineBaseScale[i] = flagshipEngineGlows[i].localScale;
                }
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
                            camFollow.SnapToTargetImmediate();
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

            // 10. Tiến trình bước ra từ căn cứ -> leo lên tàu -> chờ Enter mở cửa
            UpdateOnboardingState(dt);

            // 11. Cửa hầm chiến hạm tự động mở theo lệnh Enter & khi tàu bay về gần cảng
            UpdateHangarDoorsAndSignals(dt, t);

            // 12. Hạm đội tàu chiến khổng lồ bên cạnh nhấp nhô trang nghiêm + Phi đội hộ tống bay tuần tra bên ngoài
            UpdateCompanionFleet(dt, t);
        }

        private void UpdateOnboardingState(float dt)
        {
            if (_onboardingState == HangarOnboardingState.WalkingFromBase)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    SkipIntroWalkToCockpit();
                    return;
                }

                _onboardingTimer += dt;
                if (_onboardingTimer >= Mathf.Max(0.5f, walkDuration))
                {
                    _onboardingState = HangarOnboardingState.ClimbingIntoShip;
                    _onboardingTimer = 0f;
                }
            }
            else if (_onboardingState == HangarOnboardingState.ClimbingIntoShip)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    SkipIntroWalkToCockpit();
                    return;
                }

                _onboardingTimer += dt;
                if (_onboardingTimer >= Mathf.Max(0.4f, climbDuration))
                {
                    _onboardingState = HangarOnboardingState.AwaitingEnterBriefing;
                    _onboardingTimer = 0f;
                }
            }
        }

        private void UpdateHangarDoorsAndSignals(float dt, float t)
        {
            if (blastDoorLeft != null || blastDoorRight != null || blastDoorUpper != null)
            {
                if (_awaitingManualDoorOpen)
                {
                    if (_doorOpenProgress > 0.001f) ApplyDoorOpenProgress(0f);
                }
                else if (_launchSequenceActive)
                {
                    _launchTimer += dt;
                    float targetRatio = Mathf.Clamp01((_launchTimer - doorPreDelay) / Mathf.Max(0.25f, doorOpenDuration));
                    ApplyDoorOpenProgress(targetRatio);

                    float totalSeq = doorPreDelay + doorOpenDuration;
                    if (_launchTimer >= totalSeq)
                    {
                        _launchSequenceActive = false;
                        if (_onboardingState == HangarOnboardingState.DoorsOpening)
                        {
                            _onboardingState = HangarOnboardingState.None;
                        }
                    }
                }
                else if (autoOpenForReturningShip && playerShip != null)
                {
                    Vector3 mouthWorld = transform.TransformPoint(hangarMouthLocalPos);
                    float dist = Vector3.Distance(playerShip.transform.position, mouthWorld);
                    float desired = dist < returnAutoOpenDistance ? 1f : (dist > returnAutoOpenDistance + 14f ? 0f : (_doorOpenProgress >= 0.5f ? 1f : 0f));
                    if (!Mathf.Approximately(_doorOpenProgress, desired))
                    {
                        float step = dt / Mathf.Max(0.35f, doorOpenDuration * 0.75f);
                        ApplyDoorOpenProgress(Mathf.MoveTowards(_doorOpenProgress, desired, step));
                    }
                }

                // Đèn cảnh báo đóng/mở cửa hầm (Cam chớp khi đang mở -> Xanh Cyan khi thông thoáng)
                bool isMoving = _doorOpenProgress > 0.02f && _doorOpenProgress < 0.96f;
                bool isOpen = _doorOpenProgress >= 0.85f;
                float flash = 0.45f + 0.55f * Mathf.Sin(t * (isMoving ? 14f : 4f));
                Color stripColor = isOpen
                    ? new Color(0.18f, 0.95f, 1.0f, 1f)
                    : (isMoving
                        ? Color.Lerp(new Color(1f, 0.38f, 0.05f, 1f), new Color(1f, 0.85f, 0.15f, 1f), flash)
                        : new Color(1f, 0.18f, 0.08f, 1f));
                float emissionMult = isOpen ? 2.4f : (1.5f + flash * 2.5f);

                if (doorStatusStripRenderers != null)
                {
                    for (int i = 0; i < doorStatusStripRenderers.Length; i++)
                    {
                        var r = doorStatusStripRenderers[i];
                        if (r == null) continue;
                        r.GetPropertyBlock(_mpb);
                        _mpb.SetColor(BaseColorId, stripColor);
                        _mpb.SetColor(EmissionColorId, stripColor * emissionMult);
                        r.SetPropertyBlock(_mpb);
                    }
                }

                if (doorWarningLights != null)
                {
                    for (int i = 0; i < doorWarningLights.Length; i++)
                    {
                        var l = doorWarningLights[i];
                        if (l == null) continue;
                        l.color = stripColor;
                        l.intensity = isOpen ? 3.2f : (2.2f + flash * 5.0f);
                    }
                }
            }
        }

        private void UpdateCompanionFleet(float dt, float t)
        {
            if (companionCapitalShips != null && companionCapitalShips.Length > 0)
            {
                if (_companionBasePos == null || _companionBasePos.Length != companionCapitalShips.Length) CacheBaseHeights();
                for (int i = 0; i < companionCapitalShips.Length; i++)
                {
                    var ship = companionCapitalShips[i];
                    if (ship == null) continue;
                    float phase = t * 0.48f + i * 1.65f;
                    float hoverY = Mathf.Sin(phase) * 0.85f;
                    float surgeZ = Mathf.Cos(phase * 0.7f) * 1.15f;
                    float rollDeg = Mathf.Sin(phase * 0.65f) * 1.8f;
                    float pitchDeg = Mathf.Cos(phase * 0.55f) * 0.9f;
                    ship.localPosition = _companionBasePos[i] + new Vector3(0f, hoverY, surgeZ);
                    ship.localRotation = _companionBaseRot[i] * Quaternion.Euler(pitchDeg, 0f, rollDeg);
                }
            }

            if (wingEscortFighters != null && wingEscortFighters.Length > 0)
            {
                if (_wingBaseRadiusX == null || _wingBaseRadiusX.Length != wingEscortFighters.Length) CacheBaseHeights();
                for (int i = 0; i < wingEscortFighters.Length; i++)
                {
                    var f = wingEscortFighters[i];
                    if (f == null) continue;
                    float speed = (i % 2 == 0 ? 0.24f : -0.21f);
                    float angle = t * speed + i * (Mathf.PI * 0.5f);
                    float nextAngle = angle + (speed > 0f ? 0.04f : -0.04f);

                    Vector3 cur = GetEscortPoint(angle, _wingBaseRadiusX[i], _wingBaseRadiusZ[i], _wingBaseY[i], i);
                    Vector3 nxt = GetEscortPoint(nextAngle, _wingBaseRadiusX[i], _wingBaseRadiusZ[i], _wingBaseY[i], i);
                    f.localPosition = cur;

                    Vector3 dir = nxt - cur;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        float bank = speed > 0f ? 22f : -22f;
                        f.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
                    }
                }
            }

            if (flagshipEngineGlows != null && flagshipEngineGlows.Length > 0)
            {
                if (_engineBaseScale == null || _engineBaseScale.Length != flagshipEngineGlows.Length) CacheBaseHeights();
                for (int i = 0; i < flagshipEngineGlows.Length; i++)
                {
                    var eg = flagshipEngineGlows[i];
                    if (eg == null) continue;
                    float pulse = 0.92f + Mathf.Sin(t * 6.5f + i * 0.9f) * 0.08f;
                    Vector3 bs = _engineBaseScale[i];
                    eg.localScale = new Vector3(bs.x * pulse, bs.y * pulse, bs.z * (0.95f + Mathf.Sin(t * 9f + i) * 0.09f));
                }
            }

            if (bridgeSensorSpinners != null)
            {
                for (int i = 0; i < bridgeSensorSpinners.Length; i++)
                {
                    if (bridgeSensorSpinners[i] == null) continue;
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    bridgeSensorSpinners[i].Rotate(0f, dir * (28f + i * 9f) * dt, 0f, Space.Self);
                }
            }
        }

        private static Vector3 GetEscortPoint(float angle, float rx, float rz, float baseY, int idx)
        {
            float x = Mathf.Cos(angle) * rx;
            float z = -8f + Mathf.Sin(angle) * rz;
            float y = baseY + Mathf.Sin(angle * 2f + idx) * 2.4f;
            return new Vector3(x, y, z);
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
