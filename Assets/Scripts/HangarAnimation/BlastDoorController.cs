using System;
using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Multi-stage heavy industrial blast door controller for 04_Animated_Hangar_Blast_Doors.
    /// </summary>
    public class BlastDoorController : MonoBehaviour
    {
        [Header("Door Parts")]
        public Transform leftDoorPanel;
        public Transform rightDoorPanel;
        public Transform[] hydraulicLocks;
        public Light[] warningLights;
        public GameObject forcefieldMembrane;

        [Header("Controls & Timing")]
        [Tooltip("Disabled by default: use the 3D Blast Door Computer Terminal next to the gate.")]
        public bool allowDebugHotkeyO = false;
        public KeyCode toggleKey = KeyCode.O;
        public bool autoTestCycle = false;
        public float autoCycleInterval = 30f;
        public float slideDistance = 7.2f;
        public float lockRetractDuration = 0.9f;
        public float doorSlideDuration = 3.2f;
        public float doorRumbleAmount = 0.035f;

        private bool _targetOpen = false;
        private bool _manualForcefieldOverride = false;
        private bool _manualForcefieldState = true;
        private float _stateProgress = 0f; // 0 = closed, 1 = locks retracted, 2 = fully open
        private Vector3 _leftClosedPos;
        private Vector3 _rightClosedPos;
        private Vector3[] _lockClosedPos;
        private float[] _baseLightIntensities;
        private float _autoTimer;
        private string _lastBroadcastState;
        private float _lastBroadcastProgress = -1f;
        private bool _lastForcefieldActive;

        public event Action OnDoorStateChanged;
        public event Action<string, string> OnDoorLogEvent;

        public bool IsTargetOpen => _targetOpen;
        public bool IsFullyOpen => _stateProgress >= 1.99f;
        public bool IsFullyClosed => _stateProgress <= 0.01f;
        public bool IsTransitioning => Mathf.Abs(_stateProgress - (_targetOpen ? 2f : 0f)) > 0.005f;
        public float Progress01 => Mathf.Clamp01(_stateProgress * 0.5f);
        public bool IsForcefieldActive => forcefieldMembrane != null && forcefieldMembrane.activeSelf;

        public string HydraulicLockStatusLabel
        {
            get
            {
                if (_stateProgress <= 0.01f) return "ĐANG KHÓA CHẶT";
                if (_stateProgress < 0.99f) return _targetOpen ? "ĐANG RÚT CHỐT" : "ĐANG KHÓA CHỐT";
                return "ĐÃ RÚT CHỐT";
            }
        }

        public string StatusLabel
        {
            get
            {
                if (IsTransitioning)
                {
                    if (_targetOpen)
                        return _stateProgress < 1f ? "ĐANG MỞ CHỐT THỦY LỰC" : "ĐANG MỞ CỬA HANGAR";
                    else
                        return _stateProgress > 1f ? "ĐANG ĐÓNG CỬA HANGAR" : "ĐANG MỞ CHỐT THỦY LỰC";
                }
                return _targetOpen ? "CỬA ĐÃ MỞ" : "CỬA ĐANG ĐÓNG";
            }
        }

        public void OpenDoors()
        {
            if (!_targetOpen)
            {
                _targetOpen = true;
                OnDoorLogEvent?.Invoke("HANGAR", "Đang rút chốt thủy lực và mở cửa lớn.");
                OnDoorStateChanged?.Invoke();
            }
            _autoTimer = autoCycleInterval;
        }

        public void CloseDoors()
        {
            if (_targetOpen)
            {
                _targetOpen = false;
                OnDoorLogEvent?.Invoke("HANGAR", "Đang đóng cửa lớn và kích hoạt chốt khóa thủy lực.");
                OnDoorStateChanged?.Invoke();
            }
            _autoTimer = autoCycleInterval;
        }

        public void ToggleDoors()
        {
            if (_targetOpen) CloseDoors();
            else OpenDoors();
        }

        public void ToggleForcefieldManual()
        {
            _manualForcefieldOverride = true;
            _manualForcefieldState = !IsForcefieldActive;
            if (forcefieldMembrane != null)
                forcefieldMembrane.SetActive(_manualForcefieldState);
            OnDoorLogEvent?.Invoke("HANGAR", _manualForcefieldState ? "Đã BẬT màn chắn năng lượng." : "Đã TẮT màn chắn năng lượng.");
            OnDoorStateChanged?.Invoke();
        }

        private void Start()
        {
            if (leftDoorPanel != null) _leftClosedPos = leftDoorPanel.localPosition;
            if (rightDoorPanel != null) _rightClosedPos = rightDoorPanel.localPosition;

            if (hydraulicLocks != null)
            {
                _lockClosedPos = new Vector3[hydraulicLocks.Length];
                for (int i = 0; i < hydraulicLocks.Length; i++)
                    if (hydraulicLocks[i] != null) _lockClosedPos[i] = hydraulicLocks[i].localPosition;
            }
            if (warningLights != null)
            {
                _baseLightIntensities = new float[warningLights.Length];
                for (int i = 0; i < warningLights.Length; i++)
                    if (warningLights[i] != null) _baseLightIntensities[i] = warningLights[i].intensity;
            }
            _autoTimer = autoCycleInterval;
        }

        private void Update()
        {
            if (allowDebugHotkeyO && Input.GetKeyDown(toggleKey))
            {
                ToggleDoors();
            }

            float spd = HangarSimulationManager.EffectiveSpeed;
            if (spd <= 0.001f) return;
            float dt = Time.deltaTime * spd;

            if (autoTestCycle)
            {
                _autoTimer -= dt;
                if (_autoTimer <= 0f)
                {
                    _targetOpen = !_targetOpen;
                    _autoTimer = autoCycleInterval;
                }
            }

            float targetState = _targetOpen ? 2f : 0f;
            bool isTransitioning = Mathf.Abs(_stateProgress - targetState) > 0.001f;

            if (isTransitioning)
            {
                float stepRate = (_stateProgress < 1f)
                    ? (1f / Mathf.Max(0.1f, lockRetractDuration))
                    : (1f / Mathf.Max(0.1f, doorSlideDuration));
                _stateProgress = Mathf.MoveTowards(_stateProgress, targetState, dt * stepRate);
            }

            // Stage 1 (0..1): Hydraulic locks retract
            float lockT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_stateProgress));
            if (hydraulicLocks != null && _lockClosedPos != null)
            {
                for (int i = 0; i < hydraulicLocks.Length; i++)
                {
                    if (hydraulicLocks[i] == null) continue;
                    float dir = (i % 2 == 0) ? -1f : 1f;
                    hydraulicLocks[i].localPosition = _lockClosedPos[i] + new Vector3(dir * 0.65f * lockT, 0f, 0f);
                }
            }

            // Stage 2 (1..2): Heavy steel doors slide apart with mechanical rumble
            float doorT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_stateProgress - 1f));
            bool doorsMoving = isTransitioning && _stateProgress > 1f && _stateProgress < 2f;
            Vector3 rumble = doorsMoving
                ? new Vector3(0f, (Mathf.PerlinNoise(Time.time * 35f, 0f) - 0.5f) * doorRumbleAmount, 0f)
                : Vector3.zero;

            if (leftDoorPanel != null)
                leftDoorPanel.localPosition = _leftClosedPos + new Vector3(-slideDistance * doorT, 0f, 0f) + rumble;
            if (rightDoorPanel != null)
                rightDoorPanel.localPosition = _rightClosedPos + new Vector3(slideDistance * doorT, 0f, 0f) + rumble;

            if (forcefieldMembrane != null)
            {
                bool desired = _manualForcefieldOverride ? _manualForcefieldState : (doorT > 0.05f);
                if (forcefieldMembrane.activeSelf != desired)
                    forcefieldMembrane.SetActive(desired);
            }

            // Flash warning lights rapidly during transition
            if (warningLights != null && _baseLightIntensities != null)
            {
                float flash = isTransitioning ? (0.4f + 1.8f * Mathf.Abs(Mathf.Sin(Time.time * 10f))) : 1f;
                for (int i = 0; i < warningLights.Length; i++)
                {
                    if (warningLights[i] != null)
                        warningLights[i].intensity = _baseLightIntensities[i] * flash;
                }
            }

            // Event-driven UI sync (fires only when state/progress/forcefield meaningfully changes)
            string curState = StatusLabel;
            bool curFf = IsForcefieldActive;
            if (curState != _lastBroadcastState || curFf != _lastForcefieldActive || Mathf.Abs(_stateProgress - _lastBroadcastProgress) >= 0.015f)
            {
                if (_lastBroadcastState != null && curState != _lastBroadcastState)
                {
                    if (curState == "CỬA ĐÃ MỞ")
                        OnDoorLogEvent?.Invoke("HANGAR", "Cửa lớn Hangar đã mở hoàn toàn.");
                    else if (curState == "CỬA ĐANG ĐÓNG")
                        OnDoorLogEvent?.Invoke("HANGAR", "Cửa lớn Hangar đã đóng & khóa chốt an toàn.");
                    else if (curState == "ĐANG MỞ CỬA HANGAR")
                        OnDoorLogEvent?.Invoke("HANGAR", "Chốt thủy lực đã rút — cửa lớn đang trượt mở.");
                }
                _lastBroadcastState = curState;
                _lastBroadcastProgress = _stateProgress;
                _lastForcefieldActive = curFf;
                OnDoorStateChanged?.Invoke();
            }
        }
    }
}
