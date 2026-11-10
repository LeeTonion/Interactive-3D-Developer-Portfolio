using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Điều khiển toàn bộ trải nghiệm Góc nhìn thứ nhất (First-Person Perspective Walkthrough) của người chơi:
    /// 1. Xuống tàu tại bãi đáp (Disembark).
    /// 2. Đi bộ đến bàn phím an ninh tại Cổng Tây (Walk to Keypad).
    /// 3. Nhập mật mã an ninh tại bàn phím (Passcode Entry) -> Cửa chính mở ra.
    /// 4. Bước vào sảnh giữa, quay đầu sang trái, sang phải để quan sát tìm phòng chính (Atrium Look Around).
    /// 5. Tiến đến cửa phòng điều khiển, nhấn nút mở cửa (Inner Door Button Press) -> Cửa mở.
    /// 6. Bước vào, đi đến ghế chỉ huy và ngồi xuống ghế (Sit Down in Commander Chair).
    /// 7. Chuyển vào góc nhìn chọn các bảng thông tin Projects (Portfolio Interactive Viewing).
    /// 8. Khi thoát ra: Đứng dậy, quay đầu, đi bộ/chạy bộ ra ngoài, thao tác mở cửa và trở lại buồng lái tàu!
    /// </summary>
    public class FirstPersonCitadelWalkthrough : MonoBehaviour
    {
        public static FirstPersonCitadelWalkthrough Instance { get; private set; }

        [Header("Camera & Station References")]
        public Camera playerCamera;
        public AboutInfoStation station;
        public PlayerSpaceshipController ship;
        public SpaceshipCameraFollow cameraFollow;

        [Header("Doors & Terminals")]
        public CitadelAutomaticDoor westGateDoor;
        public CitadelAutomaticDoor eastChamberDoor;
        public CitadelKeypadTerminal keypadTerminal;
        public CitadelDoorButtonPanel innerDoorPanel;
        public Transform commanderChair;

        [Header("Movement Settings (Realistic Human Walking)")]
        [Tooltip("Vận tốc đi bộ ổn định chuẩn người thật (~3.2 m/s)")]
        public float walkSpeed = 3.2f;
        [Tooltip("Vận tốc tiếp cận cẩn thận khi lại gần bệ máy / ghế (~2.0 m/s)")]
        public float approachSpeed = 2.0f;
        [Tooltip("Vận tốc chạy bộ khi ra ngoài (~5.0 m/s)")]
        public float jogSpeed = 5.0f;
        [Tooltip("Độ cao tầm mắt người chơi khi đứng")]
        public float eyeHeight = 1.70f;
        [Tooltip("Độ cao tầm mắt khi ngồi ghế")]
        public float seatedHeight = 1.30f;
        [Tooltip("Độ nhấp nhô cao độ bước chân (Vertical Bobbing)")]
        public float headBobAmplitudeY = 0.035f;
        [Tooltip("Độ lắc lư nhẹ trái-phải tự nhiên khi dồn trọng tâm (Lateral Sway)")]
        public float headBobAmplitudeX = 0.024f;
        [Tooltip("Góc nghiêng đầu tự nhiên theo bước đi (Head Roll)")]
        public float headBobRollAngle = 0.55f;
        [Tooltip("Độ dài 1 sải bước chân người lớn (~1.40m cho 2 bước)")]
        public float stepStrideLength = 1.40f;

        private AudioSource _audioSource;
        private AudioClip _footstepClip1;
        private AudioClip _footstepClip2;
        private AudioClip _doorWhooshClip;
        private AudioClip _cyberUplinkClip;

        private Coroutine _activeRoutine;
        private GameObject _skipHUD;
        private float _strideDistance = 0f;
        private bool _stepAlt = false;
        private float _nextStepDistance = 0.35f;
        private bool _skipRequested = false;

        public bool IsActive => _activeRoutine != null;

        private void Update()
        {
            if (_activeRoutine != null && !_skipRequested)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
                {
                    _skipRequested = true;
                }
            }
        }

        private void Awake()
        {
            Instance = this;
            Application.runInBackground = true;
            BuildProceduralAudio();
            AutoResolveReferences();
        }

        private void Start()
        {
            AutoResolveReferences();
        }

        private void BuildProceduralAudio()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0.0f; // 2D player audio
            _audioSource.playOnAwake = false;

            // Âm thanh bước chân kim loại (Metallic Footsteps)
            _footstepClip1 = CreateStepClip(240f, 0.07f, 0.22f);
            _footstepClip2 = CreateStepClip(290f, 0.07f, 0.24f);

            // Âm thanh khí nén cửa mở (Pneumatic Door Whoosh)
            _doorWhooshClip = CreateWhooshClip(0.45f);

            // Âm thanh kích hoạt kết nối Cyber Uplink hiển thị Hologram
            _cyberUplinkClip = CreateCyberUplinkClip(0.65f);
        }

        private static AudioClip CreateStepClip(float freq, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = 1f - ((float)i / samples);
                float noise = UnityEngine.Random.Range(-0.15f, 0.15f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + noise * 0.3f) * decay * volume;
            }
            var clip = AudioClip.Create("Step_" + freq, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateWhooshClip(float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;
                float env = Mathf.Sin(progress * Mathf.PI);
                float noise = UnityEngine.Random.Range(-0.35f, 0.35f);
                data[i] = noise * env * 0.25f;
            }
            var clip = AudioClip.Create("Door_Whoosh", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCyberUplinkClip(float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;
                float freq = Mathf.Lerp(440f, 1320f, Mathf.Sqrt(progress));
                float env = Mathf.Sin(progress * Mathf.PI);
                float tone1 = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.55f;
                float tone2 = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.30f;
                float noise = UnityEngine.Random.Range(-0.1f, 0.1f) * env;
                data[i] = (tone1 + tone2 + noise) * env * 0.32f;
            }
            var clip = AudioClip.Create("Cyber_Uplink_Chime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayFootstep(bool alternate)
        {
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(alternate ? _footstepClip2 : _footstepClip1);
            }
        }

        public void PlayDoorWhoosh()
        {
            if (_audioSource != null && _doorWhooshClip != null)
            {
                _audioSource.PlayOneShot(_doorWhooshClip);
            }
        }

        public void PlayCyberUplinkSound()
        {
            if (_audioSource != null && _cyberUplinkClip != null)
            {
                _audioSource.PlayOneShot(_cyberUplinkClip);
            }
        }

        public void AutoResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
            }

            if (station == null) station = FindFirstObjectByType<AboutInfoStation>();
            if (ship == null) ship = FindFirstObjectByType<PlayerSpaceshipController>();
            if (cameraFollow == null) cameraFollow = FindFirstObjectByType<SpaceshipCameraFollow>();

            // Cửa cổng Tây
            if (westGateDoor == null)
            {
                var wg = GameObject.Find("West_Gate_Airlock_Portal");
                if (wg != null) westGateDoor = wg.GetComponent<CitadelAutomaticDoor>();
            }

            // Cửa phòng Đông
            if (eastChamberDoor == null)
            {
                var ec = GameObject.Find("East_Chamber_Airlock_Door");
                if (ec != null) eastChamberDoor = ec.GetComponent<CitadelAutomaticDoor>();
            }

            // Bàn phím an ninh Cổng Tây (gắn chuẩn vào Landing_Telemetry_Console)
            if (keypadTerminal == null)
            {
                var kt = GameObject.Find("Citadel_Entrance_Keypad_Terminal");
                if (kt != null) keypadTerminal = kt.GetComponent<CitadelKeypadTerminal>();
            }

            var ltc = GameObject.Find("Landing_Telemetry_Console");
            if (ltc != null)
            {
                if (keypadTerminal == null)
                {
                    var terminalGo = new GameObject("Citadel_Entrance_Keypad_Terminal");
                    keypadTerminal = terminalGo.AddComponent<CitadelKeypadTerminal>();
                }
                keypadTerminal.transform.position = ltc.transform.position;
                keypadTerminal.transform.rotation = ltc.transform.rotation;
                keypadTerminal.EnsureTerminalSetup();
            }
            else if (keypadTerminal == null)
            {
                var terminalGo = new GameObject("Citadel_Entrance_Keypad_Terminal");
                terminalGo.transform.position = new Vector3(-11.840f, 0.090f, 20.810f);
                terminalGo.transform.rotation = Quaternion.Euler(0f, 273.275f, 0f);
                keypadTerminal = terminalGo.AddComponent<CitadelKeypadTerminal>();
            }

            // Nút bấm mở cửa Cánh Đông
            if (innerDoorPanel == null)
            {
                var idp = GameObject.Find("East_Chamber_Door_Panel");
                if (idp != null) innerDoorPanel = idp.GetComponent<CitadelDoorButtonPanel>();
                else
                {
                    var panelGo = new GameObject("East_Chamber_Door_Panel");
                    panelGo.transform.position = new Vector3(29.35f, 1.25f, 21.35f);
                    panelGo.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                    innerDoorPanel = panelGo.AddComponent<CitadelDoorButtonPanel>();
                }
            }

            // Ghế chỉ huy trung tâm
            if (commanderChair == null)
            {
                var chair = GameObject.Find("Commander_Pilot_Chair_Center");
                if (chair != null) commanderChair = chair.transform;
            }
            if (commanderChair != null)
            {
                commanderChair.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // Đảm bảo ánh sáng phòng chính rực rỡ và rõ nét
            EnsureEnhancedChamberLighting();
        }

        /// <summary>
        /// Bổ sung và tăng cường ánh sáng rực rỡ, sắc nét cho phòng chính (Command Room / East Chamber)
        /// </summary>
        public static void EnsureEnhancedChamberLighting()
        {
            var lightGroup = GameObject.Find("Chamber_Atmospheric_Lighting");
            if (lightGroup == null)
            {
                var chamber = GameObject.Find("Chamber_Command_Structure") ?? GameObject.Find("AboutIsland");
                if (chamber != null)
                {
                    lightGroup = new GameObject("Chamber_Atmospheric_Lighting");
                    lightGroup.transform.SetParent(chamber.transform, false);
                }
            }
            if (lightGroup == null) return;

            // 1. Ánh sáng tràn vòm trần trung tâm: Tăng cường độ sáng bao quát toàn bộ gian phòng
            EnsurePointLight(lightGroup.transform, "Chamber_Center_Fill", new Vector3(35.0f, 5.8f, 23.0f), new Color(0.92f, 0.96f, 1.0f), 3.8f, 24.0f);

            // 2. Chiếu sáng nền khu vực dàn màn hình Portfolio phía sau
            EnsurePointLight(lightGroup.transform, "Chamber_VideoWall_Fill", new Vector3(39.0f, 5.5f, 23.0f), new Color(0.90f, 0.96f, 1.0f), 3.6f, 18.0f);

            // 3. Spotlight rọi thẳng xuống bàn ghế chỉ huy
            EnsureSpotLight(lightGroup.transform, "Chamber_Commander_Desk_Spot", new Vector3(37.0f, 5.2f, 23.0f), new Vector3(90f, 0f, 0f), new Color(0.96f, 0.98f, 1.0f), 3.5f, 8.5f, 80f);

            // 4. Spotlight chính rọi trực diện vào cụm 5 màn hình Portfolio
            EnsureSpotLight(lightGroup.transform, "Chamber_Screen_Spotlight", new Vector3(37.5f, 5.50f, 23.0f), new Vector3(50f, 90f, 0f), new Color(0.96f, 0.98f, 1.0f), 4.8f, 12.0f, 75f);

            // 5. Đèn rọi vòm cửa lối vào phòng chỉ huy
            EnsureSpotLight(lightGroup.transform, "Walkway_Entry_Downlight", new Vector3(28.0f, 4.5f, 23.0f), new Vector3(90f, 0f, 0f), new Color(0.90f, 0.96f, 1.0f), 3.0f, 8.0f, 80f);
        }

        private static Light EnsurePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var tr = parent.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name);
            if (tr == null)
            {
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
            }
            var l = go.GetComponent<Light>();
            if (l == null) l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            return l;
        }

        private static Light EnsureSpotLight(Transform parent, string name, Vector3 pos, Vector3 rot, Color color, float intensity, float range, float spotAngle)
        {
            var tr = parent.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name);
            if (tr == null)
            {
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(rot);
            }
            var l = go.GetComponent<Light>();
            if (l == null) l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = spotAngle;
            return l;
        }

        /// <summary>
        /// Bắt đầu hành trình góc nhìn thứ nhất bước vào căn cứ
        /// </summary>
        public void StartEntranceWalkthrough(Action onComplete)
        {
            _skipRequested = false;
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(EntranceWalkthroughRoutine(onComplete));
        }

        /// <summary>
        /// Bắt đầu hành trình góc nhìn thứ nhất bước ra ngoài trở lại phi thuyền
        /// </summary>
        public void StartExitWalkthrough(Action onComplete)
        {
            _skipRequested = false;
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(ExitWalkthroughRoutine(onComplete));
        }

        private IEnumerator EntranceWalkthroughRoutine(Action onComplete)
        {
            AutoResolveReferences();

            // Khóa cửa ban đầu để chỉ mở khi nhập đúng mật khẩu (chỉ khi cổng chưa bị bắn nổ thủng)
            if (westGateDoor != null && !westGateDoor.IsDestroyed)
            {
                westGateDoor.autoProximity = false;
                westGateDoor.SetOpenImmediate(0f);
            }
            if (eastChamberDoor != null)
            {
                eastChamberDoor.autoProximity = false;
                eastChamberDoor.SetOpenImmediate(0f);
            }
            if (keypadTerminal != null)
            {
                keypadTerminal.ResetTerminal();
            }

            // Tắt camera follow để người chơi toàn quyền trải nghiệm FPS
            if (cameraFollow != null) cameraFollow.enabled = false;

            // Khởi tạo HUD Skip
            CreateSkipHUD();

            // Reset nhịp bước chân
            _strideDistance = 0f;
            _nextStepDistance = stepStrideLength * 0.25f;
            _stepAlt = false;

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 1: XUỐNG TÀU (Disembark Ship)
            // Xuất hiện ngay bên cạnh phi thuyền (bên mạn phải/cửa khoang phi thuyền)
            // ═════════════════════════════════════════════════════════════════════════
            Transform pad = (station != null && station.landingPadCenter != null) ? station.landingPadCenter : null;
            if (pad == null)
            {
                var padGo = GameObject.Find("LandingPad_Center");
                if (padGo != null) pad = padGo.transform;
            }
            Vector3 padPos = (pad != null) ? pad.position : new Vector3(-32f, 0.12f, 23f);
            float padFloorY = padPos.y + 0.72f;

            Vector3 shipPos = (ship != null && Vector3.Distance(ship.transform.position, padPos) < 25f)
                ? ship.transform.position
                : padPos;
            Transform shipTr = (ship != null && Vector3.Distance(ship.transform.position, padPos) < 25f)
                ? ship.transform
                : null;

            // Xuất hiện bên cạnh phi thuyền ở cự ly an toàn (cách tâm tàu 2.75m, không chạm vỏ tàu)
            Vector3 besideOffset = (shipTr != null)
                ? (shipTr.right * 2.75f + shipTr.forward * 0.40f)
                : new Vector3(2.75f, 0f, 0.40f);
            Vector3 shipBesidePos = new Vector3(shipPos.x + besideOffset.x, padFloorY + eyeHeight, shipPos.z + besideOffset.z);
            Vector3 runwayDeckPos = new Vector3(-24.0f, padFloorY + eyeHeight, 23.0f);

            // Xuất hiện bên mạn phi thuyền, nhìn về phía buồng lái phi thuyền ở khoảng cách rõ nét
            playerCamera.transform.position = shipBesidePos;
            Vector3 lookShipHatch = shipPos + (shipTr != null ? shipTr.forward * 0.60f : Vector3.zero) + Vector3.up * 1.0f;
            playerCamera.transform.rotation = Quaternion.LookRotation(lookShipHatch - shipBesidePos);
            playerCamera.fieldOfView = 60f;

            yield return new WaitForSeconds(0.40f);

            // Quay người từ mạn tàu hướng về phía đường băng dẫn vào căn cứ Citadel
            Vector3 runwayLook = new Vector3(-10f, padFloorY + eyeHeight, 23f);
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(runwayLook - shipBesidePos), 0.60f));

            // Đi bộ từ cạnh tàu dọc đường băng với vận tốc ổn định
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: shipBesidePos,
                toPos: runwayDeckPos,
                lookTarget: runwayLook,
                speed: walkSpeed,
                easeStart: true,
                easeEnd: false
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            bool isGateBreached = (westGateDoor != null && westGateDoor.IsDestroyed) || CitadelAutomaticDoor.IsWestGateBreached;
            Vector3 doorwayOutsideCenter = new Vector3(-11.50f, eyeHeight, 23.00f);
            Vector3 doorCorridorLook = new Vector3(0f, eyeHeight, 23.00f); // Hướng nhìn thẳng vào tâm hành lang

            if (isGateBreached)
            {
                // ═════════════════════════════════════════════════════════════════════════
                // CỔNG ĐÃ BỊ BẮN THỦNG / PHÁ HỦY: KHÔNG CẦN NHẬP MẬT KHẨU!
                // Đi thẳng từ đường băng qua vòm cửa đã bị toác mở bốc khói vào căn cứ
                // ═════════════════════════════════════════════════════════════════════════
                yield return StartCoroutine(MoveCameraFPS(
                    fromPos: runwayDeckPos,
                    toPos: doorwayOutsideCenter,
                    lookTarget: doorCorridorLook,
                    speed: walkSpeed,
                    easeStart: false,
                    easeEnd: true
                ));
                if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

                yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(doorCorridorLook - doorwayOutsideCenter), 0.35f));
                yield return new WaitForSeconds(0.20f);
            }
            else
            {
                // ═════════════════════════════════════════════════════════════════════════
                // BƯỚC 2: TIẾN ĐẾN ĐÚNG BÀN PHÍM AN NINH TẠI BỆ NGOÀI CỔNG
                // ═════════════════════════════════════════════════════════════════════════
                Vector3 outsideGateApproach = new Vector3(-15.5f, eyeHeight, 22.0f);
                Vector3 inFrontOfKeypad = new Vector3(-12.75f, eyeHeight, 20.81f);
                Vector3 keypadScreenPos = (keypadTerminal != null)
                    ? keypadTerminal.transform.position + new Vector3(0f, 1.25f, 0f)
                    : new Vector3(-11.84f, 1.34f, 20.81f);

                // Đi dọc đường băng dẫn vào tiếp cận cổng
                yield return StartCoroutine(MoveCameraFPS(
                    fromPos: runwayDeckPos,
                    toPos: outsideGateApproach,
                    lookTarget: inFrontOfKeypad,
                    speed: walkSpeed,
                    easeStart: false,
                    easeEnd: false
                ));
                if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

                // Bước chậm lại tiếp cận đứng thẳng trước màn hình console an ninh
                yield return StartCoroutine(MoveCameraFPS(
                    fromPos: outsideGateApproach,
                    toPos: inFrontOfKeypad,
                    lookTarget: keypadScreenPos,
                    speed: approachSpeed,
                    easeStart: false,
                    easeEnd: true
                ));
                if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

                // ═════════════════════════════════════════════════════════════════════════
                // BƯỚC 3: NHẬP MẬT MÃ TẠI BÀN PHÍM (Enter Passcode)
                // Camera cúi nhẹ vào màn hình bàn phím an ninh
                // ═════════════════════════════════════════════════════════════════════════
                Quaternion lookKeypadRot = Quaternion.LookRotation(keypadScreenPos - inFrontOfKeypad);
                yield return StartCoroutine(RotateCameraTo(lookKeypadRot, 0.40f));

                if (keypadTerminal != null)
                {
                    yield return StartCoroutine(keypadTerminal.PerformPasscodeEntrySequence(null));
                }
                else
                {
                    yield return new WaitForSeconds(1.5f);
                }

                yield return new WaitForSeconds(0.25f);

                // ═════════════════════════════════════════════════════════════════════════
                // BƯỚC 4: BƯỚC RA GIỮA VÒM CỬA, CỬA MỞ RA VÀ BƯỚC THẲNG VÀO TRONG
                // (Step to Center of Doorway, Door Opens & Walk Straight Inside)
                // ═════════════════════════════════════════════════════════════════════════
                // Bước từ bệ bàn phím ra ĐỨNG NGAY GIỮA CỬA
                yield return StartCoroutine(MoveCameraFPS(
                    fromPos: inFrontOfKeypad,
                    toPos: doorwayOutsideCenter,
                    lookTarget: doorCorridorLook,
                    speed: walkSpeed,
                    easeStart: true,
                    easeEnd: true
                ));
                if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

                // Đứng ngay chính giữa cửa, hướng mắt nhìn thẳng vào trong
                yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(doorCorridorLook - doorwayOutsideCenter), 0.35f));

                // Kích hoạt cửa trượt mở hoàn toàn từ chính diện + hiệu ứng xả khí nén
                if (westGateDoor != null)
                {
                    PlayDoorWhoosh();
                    westGateDoor.OpenDoor(locked: true);
                }
                yield return new WaitForSeconds(0.65f);
            }

            // Bước thẳng xuyên qua tâm cửa vào trong (tất cả các điểm đều nằm trên trục Z = 23.0f chính giữa cửa!)
            Vector3[] entrancePath = new Vector3[]
            {
                new Vector3(-9.20f, eyeHeight, 23.00f), // Ngay ngưỡng cửa
                new Vector3(-6.50f, eyeHeight, 23.00f), // Hầm thông gió
                new Vector3(14.00f, eyeHeight, 23.00f)  // Tiến vào tâm sảnh lớn Rotunda
            };

            yield return StartCoroutine(MoveCameraPathFPS(
                waypoints: entrancePath,
                lookTarget: new Vector3(30f, eyeHeight, 23.00f),
                speed: walkSpeed
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 5: ĐỨNG TẠI SẢNH GIỮA, QUAY TRÁI QUAY PHẢI ĐỂ TÌM PHÒNG CHÍNH
            // (Central Atrium & Look Left, Look Right)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 atriumCenterPos = new Vector3(14.0f, eyeHeight, 23.0f);
            yield return new WaitForSeconds(0.30f);

            // 5a. Quay đầu sang TRÁI (nhìn về cụm Mission Control / Cánh Bắc)
            Vector3 lookLeftAtrium = atriumCenterPos + new Vector3(8f, 0.6f, 15f);
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(lookLeftAtrium - atriumCenterPos), 0.70f));
            yield return new WaitForSeconds(0.85f);

            // 5b. Quay đầu sang PHẢI (nhìn về Cánh Nam / dàn Server & Máy chiếu tinh thể)
            Vector3 lookRightAtrium = atriumCenterPos + new Vector3(8f, 0.5f, -15f);
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(lookRightAtrium - atriumCenterPos), 1.05f));
            yield return new WaitForSeconds(0.85f);

            // 5c. Quay thẳng về PHÍA TRƯỚC: Phát hiện cửa căn phòng chính Cánh Đông!
            Vector3 eastDoorLook = new Vector3(30f, eyeHeight, 23f);
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(eastDoorLook - atriumCenterPos), 0.75f));
            yield return new WaitForSeconds(0.35f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 6: TIẾN ĐẾN CỬA PHÒNG CHÍNH VÀ NHẤN NÚT MỞ CỬA
            // (Approach & Press Inner Door Button)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 inFrontOfEastDoor = new Vector3(28.2f, eyeHeight, 22.3f);
            Vector3 innerDoorButtonPos = (innerDoorPanel != null) ? innerDoorPanel.transform.position : new Vector3(29.35f, 1.25f, 21.35f);

            // Bước đều chân đến trước cửa phòng chỉ huy
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: atriumCenterPos,
                toPos: inFrontOfEastDoor,
                lookTarget: innerDoorButtonPos,
                speed: walkSpeed,
                easeStart: true,
                easeEnd: true
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // Liếc nhìn nút bấm mở cửa
            Quaternion lookButtonRot = Quaternion.LookRotation(innerDoorButtonPos - inFrontOfEastDoor);
            yield return StartCoroutine(RotateCameraTo(lookButtonRot, 0.40f));

            // Thao tác nhấn nút (âm thanh bấm nút + đèn nút xanh)
            if (innerDoorPanel != null)
            {
                yield return StartCoroutine(innerDoorPanel.PerformPressRoutine(() =>
                {
                    if (eastChamberDoor != null)
                    {
                        PlayDoorWhoosh();
                        eastChamberDoor.OpenDoor(locked: true);
                    }
                }));
            }
            else
            {
                if (eastChamberDoor != null)
                {
                    PlayDoorWhoosh();
                    eastChamberDoor.OpenDoor(locked: true);
                }
                yield return new WaitForSeconds(0.6f);
            }

            yield return new WaitForSeconds(0.35f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 7: BƯỚC VÀO TRONG, XOAY GHẾ LẠI VÀ NGỒI XUỐNG
            // (Enter Command Room, Swivel Chair & Sit Down)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 chairWorldPos = (commanderChair != null) ? commanderChair.position : new Vector3(37.22f, 0.23f, 23.0f);
            Vector3 approachChairStop = new Vector3(chairWorldPos.x - 1.55f, eyeHeight, chairWorldPos.z);
            Vector3 standingAtChair = new Vector3(chairWorldPos.x - 0.40f, eyeHeight, chairWorldPos.z);
            Vector3 seatedInChair = new Vector3(chairWorldPos.x - 0.20f, seatedHeight, chairWorldPos.z);
            Vector3 screensLook = new Vector3(40.35f, 2.25f, 23.0f);

            // Bước qua cửa tiến đến trước khu vực ghế chỉ huy
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: inFrontOfEastDoor,
                toPos: approachChairStop,
                lookTarget: chairWorldPos + Vector3.up * 1.0f,
                speed: walkSpeed,
                easeStart: true,
                easeEnd: true
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // ── XOAY GHẾ 1: Ghế xoay 180 độ quay về phía người chơi đang bước tới! ──
            yield return StartCoroutine(SwivelChairRoutine(commanderChair, 90f, 270f, 0.75f, false));

            // Người chơi bước tới sát ghế đã mở sẵn
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: approachChairStop,
                toPos: standingAtChair,
                lookTarget: seatedInChair + Vector3.up * 0.2f,
                speed: approachSpeed,
                easeStart: false,
                easeEnd: true
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // Hoạt họa ngồi xuống ghế (hạ độ cao từ 1.70m xuống 1.30m với độ nhún đệm ghế êm ái)
            float sitDuration = 0.90f;
            float sitElapsed = 0f;
            while (sitElapsed < sitDuration)
            {
                sitElapsed += Time.deltaTime;
                float st = Mathf.Clamp01(sitElapsed / sitDuration);
                float smoothSit = Mathf.SmoothStep(0f, 1f, st);
                float bounce = (st > 0.8f) ? Mathf.Sin((st - 0.8f) / 0.2f * Mathf.PI) * 0.025f : 0f;

                playerCamera.transform.position = Vector3.Lerp(standingAtChair, seatedInChair, smoothSit) + Vector3.up * bounce;
                playerCamera.transform.rotation = Quaternion.Slerp(
                    Quaternion.LookRotation(seatedInChair - standingAtChair),
                    Quaternion.Euler(0f, 270f, 0f),
                    smoothSit
                );
                yield return null;
            }
            playerCamera.transform.position = seatedInChair;

            yield return new WaitForSeconds(0.20f);

            // ── XOAY GHẾ 2: Người chơi đã ngồi xuống ghế -> Ghế và Camera cùng xoay 180 độ quay về màn hình! ──
            yield return StartCoroutine(SwivelChairRoutine(commanderChair, 270f, 90f, 1.00f, true));

            yield return new WaitForSeconds(0.20f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 8: HIỆU ỨNG CHUYỂN CẢNH CYBER ĐẾN CAMERA XEM BẢNG PORTFOLIO
            // (Sci-Fi Cyber Uplink Cinematic Transition FX)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 kioskViewPos = (station != null && station.cameraViewPoint != null) ? station.cameraViewPoint.position : new Vector3(32.5f, 2.65f, 23.0f);
            Vector3 kioskLookPos = (station != null && station.cameraLookTarget != null) ? station.cameraLookTarget.position : new Vector3(40.35f, 2.25f, 23.0f);
            float targetFov = (station != null) ? station.viewFov : 52f;

            // Kích hoạt hiệu ứng chuyển cảnh Cyber Optical Flare & Scanlines HUD
            yield return StartCoroutine(PerformCyberTransitionFX(onPeakFlash: () =>
            {
                playerCamera.transform.position = kioskViewPos;
                playerCamera.transform.rotation = Quaternion.LookRotation(kioskLookPos - kioskViewPos);
                playerCamera.fieldOfView = targetFov;
            }));

            DestroySkipHUD();
            _activeRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator ExitWalkthroughRoutine(Action onComplete)
        {
            AutoResolveReferences();
            CreateSkipHUD();

            Vector3 chairWorldPos = (commanderChair != null) ? commanderChair.position : new Vector3(37.22f, 0.23f, 23.0f);
            Vector3 seatedInChair = new Vector3(chairWorldPos.x - 0.20f, seatedHeight, chairWorldPos.z);
            Vector3 standingAtChair = new Vector3(chairWorldPos.x - 0.40f, eyeHeight, chairWorldPos.z);
            Vector3 approachChairStop = new Vector3(chairWorldPos.x - 1.55f, eyeHeight, chairWorldPos.z);
            Vector3 westLook = new Vector3(0f, eyeHeight, 23.0f); // Nhìn về hướng Tây (hướng lối ra)

            // Hiệu ứng chuyển cảnh Cyber ngược từ góc nhìn xem bảng về lại góc nhìn ghế ngồi
            yield return StartCoroutine(PerformCyberTransitionFX(onPeakFlash: () =>
            {
                playerCamera.transform.position = seatedInChair;
                playerCamera.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                playerCamera.fieldOfView = 60f;
            }));

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 1: XOAY GHẾ VỀ PHÍA CỬA VÀ ĐỨNG DẬY (Swivel Chair & Stand Up)
            // ═════════════════════════════════════════════════════════════════════════
            // Ghế và camera xoay 180 độ từ hướng màn hình (90) về hướng cửa (270)
            yield return StartCoroutine(SwivelChairRoutine(commanderChair, 90f, 270f, 0.75f, true));

            // Đứng dậy khỏi ghế
            float standDuration = 0.85f;
            float standElapsed = 0f;
            while (standElapsed < standDuration)
            {
                standElapsed += Time.deltaTime;
                float st = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(standElapsed / standDuration));
                playerCamera.transform.position = Vector3.Lerp(seatedInChair, standingAtChair, st);
                yield return null;
            }

            // Ghế xoay lại vị trí ban đầu (90 độ)
            StartCoroutine(SwivelChairRoutine(commanderChair, 270f, 90f, 0.75f, false));

            // Bước lùi ra khỏi bàn ghế chỉ huy
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: standingAtChair,
                toPos: approachChairStop,
                lookTarget: westLook,
                speed: walkSpeed,
                easeStart: true,
                easeEnd: false
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 2: TIẾN ĐẾN CỬA PHÒNG CHỈ HUY & THAO TÁC MỞ CỬA RA NGOÀI
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 innerDoorExitApproach = new Vector3(30.5f, eyeHeight, 23.0f);
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: approachChairStop,
                toPos: innerDoorExitApproach,
                lookTarget: westLook,
                speed: jogSpeed,
                easeStart: false,
                easeEnd: true
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // Thao tác mở cửa
            if (eastChamberDoor != null)
            {
                PlayDoorWhoosh();
                eastChamberDoor.OpenDoor(locked: true);
            }
            yield return new WaitForSeconds(0.40f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 3: CHẠY BỘ / ĐI BỘ NHANH QUA SẢNH GIỮA (Jog Through Central Atrium)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 atriumCenterPos = new Vector3(14.0f, eyeHeight, 23.0f);
            Vector3 insideThresholdExit = new Vector3(-6.5f, eyeHeight, 23.0f);
            Vector3 doorwayOutsideCenter = new Vector3(-11.50f, eyeHeight, 23.00f);

            // Chạy thẳng từ cửa Đông qua tâm sảnh Rotunda đến hầm thông gió
            Vector3[] atriumExitPath = new Vector3[]
            {
                atriumCenterPos,
                insideThresholdExit
            };

            yield return StartCoroutine(MoveCameraPathFPS(
                waypoints: atriumExitPath,
                lookTarget: westLook,
                speed: jogSpeed
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // Thao tác mở cửa Cổng Tây (nếu chưa bị bắn thủng)
            if (westGateDoor != null && !westGateDoor.IsDestroyed)
            {
                PlayDoorWhoosh();
                westGateDoor.OpenDoor(locked: true);
                yield return new WaitForSeconds(0.40f);
            }

            // Bước THẲNG QUA TÂM CỬA CỔNG TÂY ra vị trí chính giữa bên ngoài (Z = 23.0f chuẩn tâm cửa)
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: insideThresholdExit,
                toPos: doorwayOutsideCenter,
                lookTarget: westLook,
                speed: jogSpeed,
                easeStart: false,
                easeEnd: false
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 4: BƯỚC RA NGOÀI BÃI ĐÁP VÀ TRỞ LẠI TÀU (Step Out & Board Ship)
            // ═════════════════════════════════════════════════════════════════════════
            Transform exitPad = (station != null && station.landingPadCenter != null) ? station.landingPadCenter : null;
            if (exitPad == null)
            {
                var padGo = GameObject.Find("LandingPad_Center");
                if (padGo != null) exitPad = padGo.transform;
            }
            Vector3 exitPadPos = (exitPad != null) ? exitPad.position : new Vector3(-32f, 0.12f, 23f);
            float exitPadFloorY = exitPadPos.y + 0.72f;

            Vector3 exitShipPos = (ship != null && Vector3.Distance(ship.transform.position, exitPadPos) < 25f)
                ? ship.transform.position
                : exitPadPos;
            Transform exitShipTr = (ship != null && Vector3.Distance(ship.transform.position, exitPadPos) < 25f)
                ? ship.transform
                : null;

            Vector3 exitBesideOffset = (exitShipTr != null)
                ? (exitShipTr.right * 2.75f + exitShipTr.forward * 0.40f)
                : new Vector3(2.75f, 0f, 0.40f);
            Vector3 exitShipBesidePos = new Vector3(exitShipPos.x + exitBesideOffset.x, exitPadFloorY + eyeHeight, exitShipPos.z + exitBesideOffset.z);

            // Chạy bộ dọc đường băng trở lại cạnh phi thuyền
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: doorwayOutsideCenter,
                toPos: exitShipBesidePos,
                lookTarget: exitShipPos + Vector3.up * 0.8f,
                speed: jogSpeed,
                easeStart: false,
                easeEnd: true
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // Nhìn về phía cửa khoang / buồng lái tàu
            Quaternion lookReturnShipRot = Quaternion.LookRotation((exitShipPos + Vector3.up * 0.8f) - exitShipBesidePos);
            yield return StartCoroutine(RotateCameraTo(lookReturnShipRot, 0.50f));
            yield return new WaitForSeconds(0.30f);

            // Đóng cửa căn cứ lại sau lưng (nếu chưa bị bắn nổ thủng)
            if (westGateDoor != null && !westGateDoor.IsDestroyed)
            {
                westGateDoor.CloseDoor(unlock: true);
            }

            // Chuyển mượt về camera thứ 3 theo dõi tàu
            Vector3 targetShipCamPos;
            Quaternion targetShipCamRot;

            if (ship != null && cameraFollow != null)
            {
                Transform target = cameraFollow.target != null ? cameraFollow.target : ship.transform;
                targetShipCamPos = target.position - target.forward * cameraFollow.distance + target.up * cameraFollow.height;
                Vector3 look = target.position + target.forward * 4f;
                targetShipCamRot = Quaternion.LookRotation(look - targetShipCamPos, target.up);
            }
            else
            {
                targetShipCamPos = new Vector3(-32.0f, 3.5f, 15.0f);
                targetShipCamRot = Quaternion.Euler(15f, 45f, 0f);
            }

            float camReturnDuration = 1.0f;
            float camReturnElapsed = 0f;
            Vector3 startRampCamPos = playerCamera.transform.position;
            Quaternion startRampCamRot = playerCamera.transform.rotation;

            while (camReturnElapsed < camReturnDuration)
            {
                camReturnElapsed += Time.deltaTime;
                float ct = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(camReturnElapsed / camReturnDuration));
                playerCamera.transform.position = Vector3.Lerp(startRampCamPos, targetShipCamPos, ct);
                playerCamera.transform.rotation = Quaternion.Slerp(startRampCamRot, targetShipCamRot, ct);
                yield return null;
            }

            playerCamera.transform.position = targetShipCamPos;
            playerCamera.transform.rotation = targetShipCamRot;

            if (cameraFollow != null) cameraFollow.enabled = true;

            DestroySkipHUD();
            _activeRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator SwivelChairRoutine(Transform chairTr, float fromYaw, float toYaw, float duration, bool syncCamera)
        {
            if (chairTr == null) yield break;

            float elapsed = 0f;
            Quaternion startCamRot = (playerCamera != null) ? playerCamera.transform.rotation : Quaternion.identity;
            Quaternion targetCamRot = Quaternion.Euler(0f, toYaw, 0f);

            while (elapsed < duration)
            {
                if (_skipRequested) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float curYaw = Mathf.Lerp(fromYaw, toYaw, smoothT);

                chairTr.rotation = Quaternion.Euler(0f, curYaw, 0f);

                if (syncCamera && playerCamera != null)
                {
                    playerCamera.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, smoothT);
                }

                yield return null;
            }

            chairTr.rotation = Quaternion.Euler(0f, toYaw, 0f);
            if (syncCamera && playerCamera != null)
            {
                playerCamera.transform.rotation = targetCamRot;
            }
        }

        /// <summary>
        /// Hiệu ứng chuyển cảnh Sci-Fi Cyber Uplink khi ngồi vào ghế chỉ huy:
        /// Lóe sáng Neon Cyan, viền khung Letterbox, HUD trạng thái đồng bộ và tan biến hé lộ 5 màn hình Portfolio.
        /// </summary>
        private IEnumerator PerformCyberTransitionFX(Action onPeakFlash)
        {
            PlayCyberUplinkSound();

            GameObject fxCanvasGo = new GameObject("Cyber_Transition_Overlay_FX");
            var canvas = fxCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var cs = fxCanvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            fxCanvasGo.AddComponent<GraphicRaycaster>();

            // Nền Flash lóe sáng phát quang Neon Cyan
            var flashGo = new GameObject("Cyber_Flash");
            flashGo.transform.SetParent(fxCanvasGo.transform, false);
            var flashRt = flashGo.AddComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.sizeDelta = Vector2.zero;
            var flashImg = flashGo.AddComponent<Image>();
            flashImg.color = new Color(0f, 0.95f, 1f, 0f);

            // Dòng chữ HUD đồng bộ giao diện
            var textGo = new GameObject("Uplink_Status_Text");
            textGo.transform.SetParent(fxCanvasGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(900f, 80f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "<color=#00e5ff><b>◆ SYSTEM UPLINK SYNCHRONIZED ◆</b></color>\n<size=16><color=#ffffffcc>COMMAND DISPLAY ONLINE</color></size>";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 1f, 1f, 0f);

            // Giai đoạn 1: Flash lóe sáng rực rỡ (0.28s)
            float inDuration = 0.28f;
            float inElapsed = 0f;
            while (inElapsed < inDuration)
            {
                inElapsed += Time.deltaTime;
                float p = Mathf.Clamp01(inElapsed / inDuration);
                float alpha = Mathf.SmoothStep(0f, 0.92f, p);
                flashImg.color = new Color(0.04f, 0.85f, 1f, alpha);
                tmp.color = new Color(1f, 1f, 1f, p);
                yield return null;
            }

            // Đỉnh điểm Flash: Thực hiện hành động chuyển đổi (đặt vị trí Camera)
            onPeakFlash?.Invoke();

            yield return new WaitForSeconds(0.06f);

            // Giai đoạn 2: Lóe sáng tan dần hé lộ các bảng Hologram (0.36s)
            float outDuration = 0.36f;
            float outElapsed = 0f;
            while (outElapsed < outDuration)
            {
                outElapsed += Time.deltaTime;
                float p = Mathf.Clamp01(outElapsed / outDuration);
                float alpha = Mathf.Lerp(0.92f, 0f, p * p);
                flashImg.color = new Color(0.04f, 0.85f, 1f, alpha);
                tmp.color = new Color(1f, 1f, 1f, 1f - p);
                yield return null;
            }

            Destroy(fxCanvasGo);
        }

        /// <summary>
        /// Di chuyển Camera mô phỏng bước chân góc nhìn thứ nhất (Head Bobbing & Sway):
        /// Vận tốc ổn định theo mét/giây, lắc lư nhẹ trái-phải (Lateral Sway),
        /// nhấp nhô dọc (Vertical Bob) và nghiêng đầu nhẹ theo hệ trục cục bộ của Camera.
        /// </summary>
        private IEnumerator MoveCameraFPS(Vector3 fromPos, Vector3 toPos, Vector3 lookTarget, float speed, bool easeStart = false, bool easeEnd = false)
        {
            float distance = Vector3.Distance(fromPos, toPos);
            if (distance < 0.01f)
            {
                playerCamera.transform.position = toPos;
                yield break;
            }

            float duration = distance / Mathf.Max(0.5f, speed);
            float elapsed = 0f;
            Vector3 lastPos = fromPos;

            while (elapsed < duration)
            {
                if (_skipRequested) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Tính toán đường cong gia tốc chỉ ở điểm đầu hoặc điểm dừng chân
                float moveT = t;
                if (easeStart && easeEnd) moveT = Mathf.SmoothStep(0f, 1f, t);
                else if (easeStart) moveT = Mathf.Sin(t * Mathf.PI * 0.5f);
                else if (easeEnd) moveT = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

                Vector3 currentBasePos = Vector3.Lerp(fromPos, toPos, moveT);
                float stepDelta = Vector3.Distance(currentBasePos, lastPos);
                lastPos = currentBasePos;
                _strideDistance += stepDelta;

                // 1. Chu kỳ bước chân tính theo quãng đường thực tế di chuyển
                float strideCycle = (_strideDistance / stepStrideLength) * Mathf.PI * 2f;

                // 2. Nhấp nhô trục dọc (2 chu kỳ mỗi sải bước, 1 lần mỗi khi chân chạm đất)
                float vertBob = -Mathf.Abs(Mathf.Sin(strideCycle)) * headBobAmplitudeY;

                // 3. Lắc lư nhẹ trái - phải (1 chu kỳ mỗi sải bước: chuyển trọng tâm từ trái sang phải)
                float horizSway = Mathf.Sin(strideCycle * 0.5f) * headBobAmplitudeX;

                // 4. Nghiêng đầu tự nhiên theo trọng tâm bước đi
                float headRoll = -Mathf.Sin(strideCycle * 0.5f) * headBobRollAngle;

                // Áp dụng trực tiếp theo hệ trục Camera: ngang = Camera.right, dọc = Vector3.up
                Vector3 bobOffset = playerCamera.transform.right * horizSway + Vector3.up * vertBob;
                playerCamera.transform.position = currentBasePos + bobOffset;

                // Hướng mắt nhìn về mục tiêu một cách ổn định, mượt mà
                Vector3 curLookDir = (lookTarget - playerCamera.transform.position).normalized;
                if (curLookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion baseLookRot = Quaternion.LookRotation(curLookDir, Vector3.up);
                    playerCamera.transform.rotation = Quaternion.Slerp(
                        playerCamera.transform.rotation,
                        baseLookRot * Quaternion.Euler(0f, 0f, headRoll),
                        Time.deltaTime * 12f
                    );
                }

                // Phát tiếng bước chân kim loại khi chân chạm sàn
                if (_strideDistance >= _nextStepDistance)
                {
                    PlayFootstep(_stepAlt);
                    _stepAlt = !_stepAlt;
                    _nextStepDistance += stepStrideLength * 0.5f;
                }

                yield return null;
            }

            playerCamera.transform.position = toPos;
        }

        /// <summary>
        /// Di chuyển liên tục qua chuỗi các điểm Waypoint với tốc độ ổn định không khựng giữa đường
        /// </summary>
        private IEnumerator MoveCameraPathFPS(Vector3[] waypoints, Vector3 lookTarget, float speed)
        {
            if (waypoints == null || waypoints.Length == 0) yield break;

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (_skipRequested) yield break;
                Vector3 start = (i == 0) ? playerCamera.transform.position : waypoints[i - 1];
                Vector3 target = waypoints[i];
                bool isFirst = (i == 0);
                bool isLast = (i == waypoints.Length - 1);
                yield return StartCoroutine(MoveCameraFPS(start, target, lookTarget, speed, easeStart: isFirst, easeEnd: isLast));
            }
        }

        private IEnumerator RotateCameraTo(Quaternion targetRot, float duration)
        {
            float elapsed = 0f;
            Quaternion startRot = playerCamera.transform.rotation;

            while (elapsed < duration)
            {
                if (_skipRequested) break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                yield return null;
            }

            playerCamera.transform.rotation = targetRot;
        }

        private void CreateSkipHUD()
        {
            if (_skipHUD != null) Destroy(_skipHUD);

            _skipHUD = new GameObject("Walkthrough_Skip_HUD");
            var canvas = _skipHUD.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;

            var cs = _skipHUD.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            _skipHUD.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("Skip_Prompt_Text");
            textGo.transform.SetParent(_skipHUD.transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.04f);
            rect.anchorMax = new Vector2(0.5f, 0.04f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(750f, 40f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "NHẤN  [SPACE]  HOẶC  [ENTER]  ĐỂ BỎ QUA HOẠT CẢNH (SKIP)";
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 0.95f, 1.0f, 0.85f);
        }

        private void DestroySkipHUD()
        {
            if (_skipHUD != null)
            {
                Destroy(_skipHUD);
                _skipHUD = null;
            }
        }

        public void FastForwardToKiosk(Action onComplete)
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
            _skipRequested = false;
            DestroySkipHUD();

            if (commanderChair != null) commanderChair.rotation = Quaternion.Euler(0f, 90f, 0f);
            if (westGateDoor != null) westGateDoor.OpenDoor(locked: true);
            if (eastChamberDoor != null) eastChamberDoor.OpenDoor(locked: true);

            Vector3 kioskViewPos = (station != null && station.cameraViewPoint != null) ? station.cameraViewPoint.position : new Vector3(32.5f, 2.65f, 23.0f);
            Vector3 kioskLookPos = (station != null && station.cameraLookTarget != null) ? station.cameraLookTarget.position : new Vector3(40.35f, 2.25f, 23.0f);
            float targetFov = (station != null) ? station.viewFov : 52f;

            if (playerCamera != null)
            {
                playerCamera.transform.position = kioskViewPos;
                playerCamera.transform.rotation = Quaternion.LookRotation(kioskLookPos - kioskViewPos);
                playerCamera.fieldOfView = targetFov;
            }

            onComplete?.Invoke();
        }

        public void FastForwardToShip(Action onComplete)
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
            _skipRequested = false;
            DestroySkipHUD();

            if (commanderChair != null) commanderChair.rotation = Quaternion.Euler(0f, 90f, 0f);
            if (westGateDoor != null) westGateDoor.CloseDoor(unlock: true);

            if (ship != null && cameraFollow != null)
            {
                cameraFollow.enabled = true;
                Transform target = cameraFollow.target != null ? cameraFollow.target : ship.transform;
                if (playerCamera != null)
                {
                    playerCamera.transform.position = target.position - target.forward * cameraFollow.distance + target.up * cameraFollow.height;
                    playerCamera.transform.rotation = Quaternion.LookRotation(target.position + target.forward * 4f - playerCamera.transform.position, target.up);
                }
            }

            onComplete?.Invoke();
        }

        public void SkipToInteractiveStation()
        {
            FastForwardToKiosk(null);
        }

        public void SkipToShipCockpit()
        {
            FastForwardToShip(null);
        }
    }
}
