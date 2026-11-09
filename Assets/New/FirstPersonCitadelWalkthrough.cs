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

        [Header("Movement Settings")]
        public float walkSpeed = 3.6f;
        public float runSpeed = 5.2f;
        public float eyeHeight = 1.70f;
        public float seatedHeight = 1.30f;
        public float headBobFrequency = 8.5f;
        public float headBobAmplitude = 0.045f;

        private AudioSource _audioSource;
        private AudioClip _footstepClip1;
        private AudioClip _footstepClip2;
        private AudioClip _doorWhooshClip;

        private Coroutine _activeRoutine;
        private GameObject _skipHUD;
        private float _bobTimer = 0f;
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

            // Bàn phím an ninh Cổng Tây
            if (keypadTerminal == null)
            {
                var kt = GameObject.Find("Citadel_Entrance_Keypad_Terminal");
                if (kt != null) keypadTerminal = kt.GetComponent<CitadelKeypadTerminal>();
                else
                {
                    // Tạo terminal mới cạnh cổng Tây nếu chưa có
                    var terminalGo = new GameObject("Citadel_Entrance_Keypad_Terminal");
                    terminalGo.transform.position = new Vector3(-10.65f, 0.10f, 21.25f);
                    terminalGo.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
                    keypadTerminal = terminalGo.AddComponent<CitadelKeypadTerminal>();
                }
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

            // Khóa cửa ban đầu để chỉ mở khi nhập đúng mật khẩu
            if (westGateDoor != null)
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

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 1: XUỐNG TÀU (Disembark Ship)
            // Bắt đầu tại cửa khoang phi thuyền trên bãi đáp, bước xuống sàn
            // ═════════════════════════════════════════════════════════════════════════
            Transform pad = (station != null && station.landingPadCenter != null) ? station.landingPadCenter : null;
            if (pad == null)
            {
                var padGo = GameObject.Find("LandingPad_Center");
                if (padGo != null) pad = padGo.transform;
            }
            Vector3 padPos = (pad != null) ? pad.position : new Vector3(-32f, 0.12f, 23f);
            float padFloorY = padPos.y + 0.72f;
            Vector3 shipDoorPos = (ship != null && Vector3.Distance(ship.transform.position, padPos) < 15f)
                ? ship.transform.position + new Vector3(2.2f, eyeHeight, 0f)
                : padPos + new Vector3(2.5f, padFloorY + eyeHeight, 0f);
            Vector3 landingDeckPos = new Vector3(-27.5f, padFloorY + eyeHeight, 23.0f);

            playerCamera.transform.position = shipDoorPos;
            playerCamera.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Nhìn thẳng về hướng cổng Đông (+X)
            playerCamera.fieldOfView = 60f;

            yield return new WaitForSeconds(0.45f);

            // Bước xuống bãi đáp
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: shipDoorPos,
                toPos: landingDeckPos,
                lookTarget: new Vector3(-10f, eyeHeight + 0.10f, 23f),
                duration: 1.4f,
                speedBob: 1.0f
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 2: TIẾN ĐẾN BÀN PHÍM TẠI CỔNG TÂY (Walk to Keypad)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 outsideGateApproach = new Vector3(-15.0f, eyeHeight + 0.10f, 22.4f);
            Vector3 inFrontOfKeypad = new Vector3(-11.5f, eyeHeight + 0.10f, 21.6f);
            Vector3 keypadScreenPos = (keypadTerminal != null) ? keypadTerminal.transform.position + Vector3.up * 1.15f : new Vector3(-10.65f, 1.25f, 21.25f);

            // Đi dọc đường băng dẫn vào cổng
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: landingDeckPos,
                toPos: outsideGateApproach,
                lookTarget: new Vector3(-10f, eyeHeight + 0.10f, 23f),
                duration: 2.8f,
                speedBob: 1.0f
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // Tiến sát lại trước bàn phím an ninh
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: outsideGateApproach,
                toPos: inFrontOfKeypad,
                lookTarget: keypadScreenPos,
                duration: 1.6f,
                speedBob: 0.85f
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 3: NHẬP MẬT MÃ TẠI BÀN PHÍM (Enter Passcode)
            // Camera hơi nghiêng về phía màn hình bàn phím, các nút bấm nhấp nháy bíp bíp
            // ═════════════════════════════════════════════════════════════════════════
            // Cúi nhẹ góc nhìn vào màn hình console
            Quaternion lookKeypadRot = Quaternion.LookRotation(keypadScreenPos - inFrontOfKeypad);
            yield return StartCoroutine(RotateCameraTo(lookKeypadRot, 0.45f));

            if (keypadTerminal != null)
            {
                yield return StartCoroutine(keypadTerminal.PerformPasscodeEntrySequence(null));
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            yield return new WaitForSeconds(0.3f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 4: CỬA MỞ RA VÀ BƯỚC VÀO TRONG (Outer Door Open & Step Inside)
            // ═════════════════════════════════════════════════════════════════════════
            // Quay mặt trở lại chính giữa vòm cửa
            Vector3 doorCenterLook = new Vector3(-4f, eyeHeight, 23f);
            Quaternion lookDoorCenter = Quaternion.LookRotation(doorCenterLook - inFrontOfKeypad);
            yield return StartCoroutine(RotateCameraTo(lookDoorCenter, 0.55f));

            // Kích hoạt cửa trượt mở hoàn toàn + hiệu ứng xả khí nén
            if (westGateDoor != null)
            {
                PlayDoorWhoosh();
                westGateDoor.OpenDoor(locked: true);
            }
            yield return new WaitForSeconds(0.75f);

            // Bước qua cửa vào hầm thông gió
            Vector3 insideThresholdPos = new Vector3(-6.5f, eyeHeight, 23.0f);
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: inFrontOfKeypad,
                toPos: insideThresholdPos,
                lookTarget: new Vector3(10f, eyeHeight, 23f),
                duration: 1.6f,
                speedBob: 1.0f
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 5: ĐI VÀO SẢNH GIỮA, QUAY TRÁI QUAY PHẢI ĐỂ TÌM PHÒNG CHÍNH
            // (Walk to Central Atrium & Look Left, Look Right)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 atriumCenterPos = new Vector3(14.0f, eyeHeight, 23.0f);

            // Đi dọc hành lang tiến vào trung tâm sảnh lớn Rotunda
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: insideThresholdPos,
                toPos: atriumCenterPos,
                lookTarget: new Vector3(30f, eyeHeight, 23f),
                duration: 3.2f,
                speedBob: 1.0f
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // Dừng chân ở tâm sảnh lớn
            yield return new WaitForSeconds(0.35f);

            // 5a. Quay đầu sang TRÁI (nhìn về cụm Mission Control / Cánh Bắc)
            Vector3 lookLeftAtrium = atriumCenterPos + new Vector3(8f, 0.6f, 15f);
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(lookLeftAtrium - atriumCenterPos), 0.75f));
            yield return new WaitForSeconds(0.95f); // Dừng lại nhìn quan sát

            // 5b. Quay đầu sang PHẢI (nhìn về Cánh Nam / dàn Server & Máy chiếu tinh thể)
            Vector3 lookRightAtrium = atriumCenterPos + new Vector3(8f, 0.5f, -15f);
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(lookRightAtrium - atriumCenterPos), 1.15f));
            yield return new WaitForSeconds(0.95f); // Dừng lại nhìn quan sát

            // 5c. Quay thẳng về PHÍA TRƯỚC: Phát hiện cửa căn phòng chính Cánh Đông!
            Vector3 eastDoorLook = new Vector3(30f, eyeHeight, 23f);
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(eastDoorLook - atriumCenterPos), 0.85f));
            yield return new WaitForSeconds(0.45f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 6: TIẾN ĐẾN CỬA PHÒNG CHÍNH VÀ NHẤN NÚT MỞ CỬA (Approach & Press Inner Door Button)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 inFrontOfEastDoor = new Vector3(28.2f, eyeHeight, 22.3f);
            Vector3 innerDoorButtonPos = (innerDoorPanel != null) ? innerDoorPanel.transform.position : new Vector3(29.35f, 1.25f, 21.35f);

            // Bước đến trước cửa phòng chỉ huy
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: atriumCenterPos,
                toPos: inFrontOfEastDoor,
                lookTarget: innerDoorButtonPos,
                duration: 2.6f,
                speedBob: 1.0f
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // Liếc nhìn nút bấm mở cửa
            Quaternion lookButtonRot = Quaternion.LookRotation(innerDoorButtonPos - inFrontOfEastDoor);
            yield return StartCoroutine(RotateCameraTo(lookButtonRot, 0.45f));

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

            yield return new WaitForSeconds(0.4f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 7: BƯỚC VÀO TRONG, ĐI ĐẾN GHẾ CHỈ HUY VÀ NGỒI XUỐNG
            // (Enter Command Room, Approach Chair & Sit Down)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 chairWorldPos = (commanderChair != null) ? commanderChair.position : new Vector3(37.22f, 0.23f, 23.0f);
            Vector3 standingInFrontOfChair = new Vector3(chairWorldPos.x - 0.45f, eyeHeight, chairWorldPos.z);
            Vector3 seatedInChair = new Vector3(chairWorldPos.x - 0.25f, seatedHeight, chairWorldPos.z);
            Vector3 screensLook = new Vector3(40.35f, 2.25f, 23.0f);

            // Bước qua cửa tiến vào bàn chỉ huy
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: inFrontOfEastDoor,
                toPos: standingInFrontOfChair,
                lookTarget: screensLook,
                duration: 2.0f,
                speedBob: 0.9f
            ));
            if (_skipRequested) { FastForwardToKiosk(onComplete); yield break; }

            // Đứng đối diện màn hình, chuẩn bị ngồi xuống
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(screensLook - standingInFrontOfChair), 0.4f));

            // Hoạt họa ngồi xuống ghế (hạ độ cao từ 1.70m xuống 1.30m với độ nhún êm ái)
            float sitDuration = 1.1f;
            float sitElapsed = 0f;
            while (sitElapsed < sitDuration)
            {
                sitElapsed += Time.deltaTime;
                float st = Mathf.Clamp01(sitElapsed / sitDuration);
                float smoothSit = Mathf.SmoothStep(0f, 1f, st);

                // Thêm độ nảy nhẹ khi cơ thể chạm đệm ghế
                float bounce = (st > 0.8f) ? Mathf.Sin((st - 0.8f) / 0.2f * Mathf.PI) * 0.025f : 0f;

                playerCamera.transform.position = Vector3.Lerp(standingInFrontOfChair, seatedInChair, smoothSit) + Vector3.up * bounce;
                playerCamera.transform.rotation = Quaternion.LookRotation(screensLook - playerCamera.transform.position);
                yield return null;
            }

            yield return new WaitForSeconds(0.35f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 8: CHUYỂN VÀO GÓC NHÌN CHỌN BẢNG PORTFOLIO (Interactive Slide Viewing)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 kioskViewPos = (station != null && station.cameraViewPoint != null) ? station.cameraViewPoint.position : new Vector3(32.5f, 2.65f, 23.0f);
            Vector3 kioskLookPos = (station != null && station.cameraLookTarget != null) ? station.cameraLookTarget.position : new Vector3(40.35f, 2.25f, 23.0f);
            float targetFov = (station != null) ? station.viewFov : 52f;

            // Camera nhẹ nhàng hòa nhập vào góc nhìn toàn cảnh tương tác 5 bảng
            float transDuration = 0.85f;
            float transElapsed = 0f;
            Vector3 startTransPos = playerCamera.transform.position;
            Quaternion startTransRot = playerCamera.transform.rotation;
            float startFov = playerCamera.fieldOfView;

            while (transElapsed < transDuration)
            {
                transElapsed += Time.deltaTime;
                float tt = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transElapsed / transDuration));
                playerCamera.transform.position = Vector3.Lerp(startTransPos, kioskViewPos, tt);
                playerCamera.transform.rotation = Quaternion.Slerp(startTransRot, Quaternion.LookRotation(kioskLookPos - kioskViewPos), tt);
                playerCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, tt);
                yield return null;
            }

            playerCamera.transform.position = kioskViewPos;
            playerCamera.transform.rotation = Quaternion.LookRotation(kioskLookPos - kioskViewPos);
            playerCamera.fieldOfView = targetFov;

            DestroySkipHUD();
            _activeRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator ExitWalkthroughRoutine(Action onComplete)
        {
            AutoResolveReferences();
            CreateSkipHUD();

            Vector3 chairWorldPos = (commanderChair != null) ? commanderChair.position : new Vector3(37.22f, 0.23f, 23.0f);
            Vector3 seatedInChair = new Vector3(chairWorldPos.x - 0.25f, seatedHeight, chairWorldPos.z);
            Vector3 standingInFrontOfChair = new Vector3(chairWorldPos.x - 0.45f, eyeHeight, chairWorldPos.z);
            Vector3 westLook = new Vector3(0f, eyeHeight, 23.0f); // Nhìn về hướng Tây (hướng lối ra)

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 1: ĐỨNG DẬY KHỎI GHẾ CHỈ HUY (Stand Up & Turn Around)
            // ═════════════════════════════════════════════════════════════════════════
            float standDuration = 0.85f;
            float standElapsed = 0f;
            Vector3 startPos = playerCamera.transform.position;

            while (standElapsed < standDuration)
            {
                standElapsed += Time.deltaTime;
                float st = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(standElapsed / standDuration));
                playerCamera.transform.position = Vector3.Lerp(startPos, standingInFrontOfChair, st);
                yield return null;
            }

            // Quay người 180 độ hướng ra cửa phòng
            yield return StartCoroutine(RotateCameraTo(Quaternion.LookRotation(westLook - standingInFrontOfChair), 0.6f));

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 2: TIẾN ĐẾN CỬA PHÒNG CHỈ HUY & THAO TÁC MỞ CỬA RA NGOÀI
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 innerDoorExitApproach = new Vector3(31.5f, eyeHeight, 23.0f);
            yield return StartCoroutine(MoveCameraFPS(
                fromPos: standingInFrontOfChair,
                toPos: innerDoorExitApproach,
                lookTarget: westLook,
                duration: 1.5f,
                speedBob: 1.1f
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // Thao tác mở cửa
            if (eastChamberDoor != null)
            {
                PlayDoorWhoosh();
                eastChamberDoor.OpenDoor(locked: true);
            }
            yield return new WaitForSeconds(0.5f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 3: CHẠY BỘ / ĐI BỘ NHANH QUA SẢNH GIỮA (Jog Through Central Atrium)
            // ═════════════════════════════════════════════════════════════════════════
            Vector3 atriumCenterPos = new Vector3(14.0f, eyeHeight, 23.0f);
            Vector3 corridorExitPos = new Vector3(-6.5f, eyeHeight, 23.0f);
            Vector3 outerDoorExitApproach = new Vector3(-8.5f, eyeHeight, 23.0f);

            yield return StartCoroutine(MoveCameraFPS(
                fromPos: innerDoorExitApproach,
                toPos: atriumCenterPos,
                lookTarget: westLook,
                duration: 2.2f,
                speedBob: 1.25f
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            yield return StartCoroutine(MoveCameraFPS(
                fromPos: atriumCenterPos,
                toPos: outerDoorExitApproach,
                lookTarget: westLook,
                duration: 2.4f,
                speedBob: 1.25f
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // Thao tác mở cửa Cổng Tây
            if (westGateDoor != null)
            {
                PlayDoorWhoosh();
                westGateDoor.OpenDoor(locked: true);
            }
            yield return new WaitForSeconds(0.5f);

            // ═════════════════════════════════════════════════════════════════════════
            // BƯỚC 4: BƯỚC RA NGOÀI BÃI ĐÁP VÀ TRỞ LẠI TÀU (Step Out & Board Ship)
            // ═════════════════════════════════════════════════════════════════════════
            Transform pad = (station != null && station.landingPadCenter != null) ? station.landingPadCenter : null;
            if (pad == null)
            {
                var padGo = GameObject.Find("LandingPad_Center");
                if (padGo != null) pad = padGo.transform;
            }
            Vector3 padPos = (pad != null) ? pad.position : new Vector3(-32f, 0.12f, 23f);
            Vector3 landingPadRamp = (ship != null && Vector3.Distance(ship.transform.position, padPos) < 15f)
                ? ship.transform.position + new Vector3(2.2f, eyeHeight, 0f)
                : padPos + new Vector3(2.5f, eyeHeight, 0f);
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

            yield return StartCoroutine(MoveCameraFPS(
                fromPos: outerDoorExitApproach,
                toPos: landingPadRamp,
                lookTarget: landingPadRamp + Vector3.left * 5f,
                duration: 2.6f,
                speedBob: 1.25f
            ));
            if (_skipRequested) { FastForwardToShip(onComplete); yield break; }

            // Đóng cửa căn cứ lại sau lưng
            if (westGateDoor != null)
            {
                westGateDoor.CloseDoor(unlock: true);
            }

            // Chuyển mượt về camera thứ 3 theo dõi tàu
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

        /// <summary>
        /// Di chuyển Camera mô phỏng bước chân góc nhìn thứ nhất (Head Bobbing & Footsteps)
        /// </summary>
        private IEnumerator MoveCameraFPS(Vector3 fromPos, Vector3 toPos, Vector3 lookTarget, float duration, float speedBob)
        {
            float elapsed = 0f;
            float stepInterval = 0.42f / speedBob;
            float nextStepTime = stepInterval * 0.5f;
            bool stepAlt = false;

            while (elapsed < duration)
            {
                if (_skipRequested) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                _bobTimer += Time.deltaTime * headBobFrequency * speedBob;
                float bobY = Mathf.Sin(_bobTimer) * headBobAmplitude * speedBob;
                float bobX = Mathf.Cos(_bobTimer * 0.5f) * headBobAmplitude * 0.4f * speedBob;

                Vector3 currentBasePos = Vector3.Lerp(fromPos, toPos, smoothT);
                playerCamera.transform.position = currentBasePos + new Vector3(bobX, bobY, 0f);

                Vector3 curLookDir = (lookTarget - playerCamera.transform.position).normalized;
                if (curLookDir.sqrMagnitude > 0.001f)
                {
                    playerCamera.transform.rotation = Quaternion.Slerp(
                        playerCamera.transform.rotation,
                        Quaternion.LookRotation(curLookDir, Vector3.up),
                        Time.deltaTime * 10f
                    );
                }

                // Phát tiếng bước chân
                if (elapsed >= nextStepTime)
                {
                    PlayFootstep(stepAlt);
                    stepAlt = !stepAlt;
                    nextStepTime += stepInterval;
                }

                yield return null;
            }

            playerCamera.transform.position = toPos;
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
