using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// On-screen HUD attached to HangarSimulationManager:
    /// - Triggers a mission-accepted banner when the player walks through the gate into Room01_Spaceship_Hangar.
    /// - Displays the 5 Launch Prep tasks cleanly in the top-left corner with live progress & status dots.
    /// - Automatically opens 04_Animated_Hangar_Blast_Doors as soon as all 5 tasks are completed!
    /// </summary>
    public class HangarMissionHUD : MonoBehaviour
    {
        private Canvas _hudCanvas;
        private CanvasGroup _trackerGroup;
        private CanvasGroup _bannerGroup;
        private TextMeshProUGUI _bannerTitleText;
        private TextMeshProUGUI _bannerSubText;

        private readonly HangarStatusIndicator[] _dots = new HangarStatusIndicator[5];
        private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[5];
        private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[5];
        private TextMeshProUGUI _headerProgressText;

        private Transform _player;
        private PlayerCharacterController _playerController;
        private HangarSimulationManager _boundMgr;
        private BlastDoorController _blastDoor;
        private Transform _hangarRoot;

        private bool _missionAccepted;
        private bool _autoOpenedDoors;
        private float _bannerTimer;
        private float _trackerAlpha;

        private AudioSource _audio;
        private AudioClip _acceptClip;
        private AudioClip _completeClip;

        private void Awake()
        {
            _hangarRoot = transform.parent != null ? transform.parent : transform;
            EnsureAudio();
            BuildHudCanvas();
        }

        private void Start()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null)
            {
                _player = pc.transform;
                _playerController = pc;
            }
            _blastDoor = FindFirstObjectByType<BlastDoorController>();
            BindManager();
            SyncMissionUI();
        }

        private void OnEnable()
        {
            BindManager();
        }

        private void OnDisable()
        {
            if (_boundMgr != null)
            {
                _boundMgr.OnEngineeringStateChanged -= SyncMissionUI;
                _boundMgr = null;
            }
        }

        private void BindManager()
        {
            if (_boundMgr != null) return;
            _boundMgr = HangarSimulationManager.Instance;
            if (_boundMgr != null)
                _boundMgr.OnEngineeringStateChanged += SyncMissionUI;
        }

        private void EnsureAudio()
        {
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake = false;

            _acceptClip = MakeTone(580f, 880f, 0.25f, 0.18f);
            _completeClip = MakeTone(660f, 1120f, 0.38f, 0.22f);
        }

        private void BuildHudCanvas()
        {
            if (_hudCanvas != null) return;

            GameObject cGo = new GameObject("Hangar_PlayerMissionHUD");
            cGo.transform.SetParent(transform, false);
            _hudCanvas = cGo.AddComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 25;

            var scaler = cGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // 1. Top-Left 5-Task Tracker Panel
            GameObject tracker = HangarUITheme.CreateGlassPanel(
                cGo.transform, "MissionTrackerPanel",
                new Vector2(24f, -24f), new Vector2(430f, 224f),
                new Color(0.020f, 0.048f, 0.090f, 0.88f),
                new Color(HangarUITheme.NeonCyan.r, HangarUITheme.NeonCyan.g, HangarUITheme.NeonCyan.b, 0.65f));

            RectTransform tRt = tracker.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(0f, 1f);
            tRt.pivot = new Vector2(0f, 1f);
            tRt.anchoredPosition = new Vector2(24f, -24f);

            _trackerGroup = tracker.AddComponent<CanvasGroup>();
            _trackerGroup.alpha = 0f;
            _trackerGroup.blocksRaycasts = false;
            _trackerGroup.interactable = false;

            // Header strip
            GameObject hdr = HangarUITheme.CreateGlassPanel(
                tracker.transform, "Header", new Vector2(0f, 88f), new Vector2(414f, 32f),
                HangarUITheme.BgHeaderStrip, new Color(HangarUITheme.NeonCyan.r, HangarUITheme.NeonCyan.g, HangarUITheme.NeonCyan.b, 0.35f));

            HangarUITheme.CreateTMP(hdr.transform, "Title", "NHIỆM VỤ HANGAR BAY 01", 15f, HangarUITheme.NeonCyan, new Vector2(-58f, 0f), new Vector2(280f, 26f), TextAlignmentOptions.Left);
            _headerProgressText = HangarUITheme.CreateTMP(hdr.transform, "Count", "0/5", 15f, HangarUITheme.StatusAmber, new Vector2(150f, 0f), new Vector2(90f, 26f), TextAlignmentOptions.Right);

            string[] defaultNames =
            {
                "1. Đóng cầu dao điện (≥ 75%)",
                "2. Mở van trái & bơm nhiên liệu (phải)",
                "3. Mở đủ 4 cuộn dây cáp vàng",
                "4. Quét thân vỏ tàu (Hologram)",
                "5. Giữ nhiệt độ an toàn (< 85°C)"
            };

            for (int i = 0; i < 5; i++)
            {
                float y = 44f - i * 31f;
                _dots[i] = HangarUITheme.CreateStatusDot(tracker.transform, $"Dot_{i}", new Vector2(-190f, y), 10f);
                _labels[i] = HangarUITheme.CreateTMP(tracker.transform, $"Lbl_{i}", defaultNames[i], 13.5f, HangarUITheme.TextPrimary, new Vector2(-24f, y), new Vector2(298f, 26f), TextAlignmentOptions.Left, FontStyles.Normal);
                _values[i] = HangarUITheme.CreateTMP(tracker.transform, $"Val_{i}", "--", 13.5f, HangarUITheme.StatusAmber, new Vector2(146f, y), new Vector2(104f, 26f), TextAlignmentOptions.Right);
            }

            // 2. Top-Center Notification Banner (when entering garage / completing all 5)
            GameObject banner = HangarUITheme.CreateGlassPanel(
                cGo.transform, "MissionBanner",
                new Vector2(0f, -36f), new Vector2(640f, 82f),
                new Color(0.025f, 0.065f, 0.125f, 0.94f),
                HangarUITheme.NeonCyan);

            RectTransform bRt = banner.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 1f);
            bRt.anchorMax = new Vector2(0.5f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.anchoredPosition = new Vector2(0f, -36f);

            _bannerGroup = banner.AddComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable = false;

            _bannerTitleText = HangarUITheme.CreateTMP(banner.transform, "BannerTitle", "NHẬN NHIỆM VỤ MỚI: CHUẨN BỊ CẤT CÁNH", 20f, HangarUITheme.NeonCyan, new Vector2(0f, 14f), new Vector2(610f, 30f), TextAlignmentOptions.Center);
            _bannerSubText = HangarUITheme.CreateTMP(banner.transform, "BannerSub", "Hoàn thành 5/5 nhiệm vụ trong Hangar để tự động mở Cửa Lớn!", 14.5f, HangarUITheme.TextPrimary, new Vector2(0f, -18f), new Vector2(610f, 24f), TextAlignmentOptions.Center, FontStyles.Normal);
        }

        private void Update()
        {
            if (_boundMgr == null) BindManager();

            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerCharacterController>();
                if (pc != null)
                {
                    _player = pc.transform;
                    _playerController = pc;
                }
            }
            if (_blastDoor == null)
                _blastDoor = FindFirstObjectByType<BlastDoorController>();

            bool inShip = _playerController != null && _playerController.IsInShip;
            bool insideGarage = IsPlayerInsideGarage();

            if (!_missionAccepted && insideGarage && !inShip)
            {
                _missionAccepted = true;
                ShowBanner(
                    "NHẬN NHIỆM VỤ: CHUẨN BỊ CẤT CÁNH (5 MỤC)",
                    "Hoàn thành đủ 5 nhiệm vụ trong Hangar để tự động mở Cửa Lớn!",
                    HangarUITheme.NeonCyan,
                    4.5f);
                if (_audio != null && _acceptClip != null) _audio.PlayOneShot(_acceptClip);
                SyncMissionUI();
            }

            // Smoothly show tracker when mission accepted & player is on foot (hide when flying in space)
            float targetTrackerAlpha = (_missionAccepted && !inShip && !PlayerCharacterController.AnyUiPanelOpen) ? 1f : 0f;
            _trackerAlpha = Mathf.MoveTowards(_trackerAlpha, targetTrackerAlpha, Time.deltaTime * 4f);
            if (_trackerGroup != null)
                _trackerGroup.alpha = _trackerAlpha;

            // Smooth banner fade
            if (_bannerGroup != null)
            {
                if (_bannerTimer > 0f)
                {
                    _bannerTimer -= Time.deltaTime;
                    _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, 1f, Time.deltaTime * 5f);
                }
                else
                {
                    _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, 0f, Time.deltaTime * 3.5f);
                }
            }

            // Auto-open Hangar Blast Doors when all 5 tasks are completed!
            if (_missionAccepted && !_autoOpenedDoors && _boundMgr != null && _boundMgr.IsLaunchReady)
            {
                _autoOpenedDoors = true;
                if (_blastDoor != null)
                    _blastDoor.OpenDoors();
                ShowBanner(
                    "HOÀN THÀNH 5/5 NHIỆM VỤ — CỬA LỚN ĐANG TỰ ĐỘNG MỞ!",
                    "Tất cả hệ thống đã sẵn sàng. Lên tàu [F] và cất cánh!",
                    HangarUITheme.StatusGreen,
                    5.5f);
                if (_audio != null && _completeClip != null) _audio.PlayOneShot(_completeClip);
                SyncMissionUI();
            }
        }

        private bool IsPlayerInsideGarage()
        {
            if (_player == null || _hangarRoot == null) return false;
            Vector3 lp = _hangarRoot.InverseTransformPoint(_player.position);
            // Room01_Spaceship_Hangar interior bounds (HUB_01 door is at z = -25.35, Blast Door at z = +16)
            return lp.z >= -24.5f && lp.z <= 16.5f && Mathf.Abs(lp.x) <= 18.5f && lp.y >= 1.2f && lp.y <= 14.5f;
        }

        private void ShowBanner(string title, string sub, Color titleColor, float duration)
        {
            if (_bannerTitleText != null)
            {
                _bannerTitleText.text = title;
                _bannerTitleText.color = titleColor;
            }
            if (_bannerSubText != null)
                _bannerSubText.text = sub;
            _bannerTimer = duration;
        }

        private void SyncMissionUI()
        {
            var mgr = _boundMgr != null ? _boundMgr : HangarSimulationManager.Instance;
            if (mgr == null) return;

            bool pwrOk = mgr.powerGridPercent >= 75f;
            bool fuelOk = mgr.fuelPercent >= 99f;
            bool cableOk = mgr.clampsReleased;
            bool scanOk = mgr.hullScanComplete;
            bool tempOk = !mgr.IsOverheating;

            int doneCount = (pwrOk ? 1 : 0) + (fuelOk ? 1 : 0) + (cableOk ? 1 : 0) + (scanOk ? 1 : 0) + (tempOk ? 1 : 0);
            if (_headerProgressText != null)
            {
                _headerProgressText.text = _autoOpenedDoors ? "5/5 (ĐÃ MỞ)" : $"{doneCount}/5";
                _headerProgressText.color = doneCount == 5 ? HangarUITheme.StatusGreen : HangarUITheme.StatusAmber;
            }

            SetRow(0, pwrOk, false, "1. Đóng cầu dao điện (≥ 75%)", $"{Mathf.RoundToInt(mgr.powerGridPercent)}%");

            string fuelLabel = fuelOk
                ? "2. Bơm nhiên liệu tàu"
                : (!mgr.pipeValvesOpen ? "2. Mở van ống (bên trái bãi đáp)" : "2. Bơm nhiên liệu (bên phải bãi đáp)");
            string fuelVal = fuelOk ? "100% (OK)" : (mgr.isFuelPumping ? $"BƠM {Mathf.RoundToInt(mgr.fuelPercent)}%" : $"{Mathf.RoundToInt(mgr.fuelPercent)}%");
            SetRow(1, fuelOk, mgr.isFuelPumping, fuelLabel, fuelVal);

            SetRow(2, cableOk, false, "3. Mở 4 cuộn dây cáp vàng", $"{mgr.ReleasedCableCount}/4");

            string scanVal = scanOk ? "HOÀN TẤT" : (mgr.isHullScanning ? $"{Mathf.RoundToInt(mgr.hullScanProgress)}%" : "CHƯA");
            SetRow(3, scanOk, mgr.isHullScanning, "4. Quét thân vỏ (Hologram)", scanVal);

            SetRow(4, tempOk, !tempOk, "5. Giữ nhiệt độ (< 75°C)", $"{Mathf.RoundToInt(mgr.coreTemperatureC)}°C");
        }

        private void SetRow(int idx, bool ok, bool activeBlink, string label, string val)
        {
            Color col = ok ? HangarUITheme.StatusGreen : (activeBlink ? HangarUITheme.NeonCyan : HangarUITheme.StatusAmber);
            if (idx == 4 && !ok) col = HangarUITheme.StatusRed;

            if (_dots[idx] != null)
                _dots[idx].SetState(col, activeBlink || (idx == 4 && !ok) ? 3.2f : 0f);
            if (_labels[idx] != null)
            {
                _labels[idx].text = label;
                _labels[idx].color = ok ? new Color(0.65f, 0.95f, 0.78f, 0.9f) : HangarUITheme.TextPrimary;
            }
            if (_values[idx] != null)
            {
                _values[idx].text = val;
                _values[idx].color = col;
            }
        }

        private static AudioClip MakeTone(float f1, float f2, float duration, float vol)
        {
            int sr = 44100;
            int n = Mathf.Max(64, (int)(sr * duration));
            float[] data = new float[n];
            int half = n / 2;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float f = (i < half) ? f1 : f2;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * (1f - (float)i / n) * vol;
            }
            AudioClip clip = AudioClip.Create("MissionTone", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
