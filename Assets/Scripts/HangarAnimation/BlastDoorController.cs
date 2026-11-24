using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Multi-stage heavy industrial blast door controller for 04_Animated_Hangar_Blast_Doors.
    /// Press [O] in Play Mode to toggle Open/Close (hydraulic locks retract -> warning lights flash -> doors slide with rumble).
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

        public bool IsTargetOpen => _targetOpen;
        public bool IsFullyOpen => _stateProgress >= 1.99f;
        public bool IsFullyClosed => _stateProgress <= 0.01f;
        public bool IsTransitioning => Mathf.Abs(_stateProgress - (_targetOpen ? 2f : 0f)) > 0.005f;
        public float Progress01 => Mathf.Clamp01(_stateProgress * 0.5f);
        public bool IsForcefieldActive => forcefieldMembrane != null && forcefieldMembrane.activeSelf;

        public string StatusLabel
        {
            get
            {
                if (IsTransitioning)
                {
                    if (_targetOpen)
                        return _stateProgress < 1f ? "ĐANG MỞ CHỐT THỦY LỰC..." : "ĐANG MỞ CỬA HANGAR...";
                    else
                        return _stateProgress > 1f ? "ĐANG ĐÓNG CỬA HANGAR..." : "ĐANG KHÓA CHỐT AN NINH...";
                }
                return _targetOpen ? "CỬA ĐÃ MỞ // SẴN SÀNG CẤT CÁNH" : "CỬA ĐANG ĐÓNG // ÁP SUẤT ỔN ĐỊNH";
            }
        }

        public void OpenDoors()
        {
            _targetOpen = true;
            _autoTimer = autoCycleInterval;
        }

        public void CloseDoors()
        {
            _targetOpen = false;
            _autoTimer = autoCycleInterval;
        }

        public void ToggleDoors()
        {
            _targetOpen = !_targetOpen;
            _autoTimer = autoCycleInterval;
        }

        public void ToggleForcefieldManual()
        {
            _manualForcefieldOverride = true;
            _manualForcefieldState = !IsForcefieldActive;
            if (forcefieldMembrane != null)
                forcefieldMembrane.SetActive(_manualForcefieldState);
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
        }
    }
}
