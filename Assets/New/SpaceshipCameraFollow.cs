using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Camera bám theo tàu bay không gian 3D.
    /// Tự động xoay mượt mà theo góc bay, lùi xa và mở rộng FOV khi tăng tốc phản lực.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SpaceshipCameraFollow : MonoBehaviour
    {
        [Header("Mục tiêu theo dõi")]
        public Transform target;
        public PlayerSpaceshipController spaceship;

        [Header("Chế độ Góc Nhìn Thứ Nhất (1st-Person Cockpit)")]
        [Tooltip("Bật góc nhìn buồng lái phi công ngay từ đầu")]
        public bool firstPersonMode = true;
        [Tooltip("Vị trí mắt phi công trong hệ tọa độ local của phi thuyền")]
        public Vector3 firstPersonEyeOffset = new Vector3(0f, 1.48f, 0.90f);
        [Tooltip("Khoảng cách điểm ngắm thẳng phía trước mũi tàu (m)")]
        public float firstPersonLookAhead = 95f;
        public float firstPersonNormalFOV = 64f;
        public float firstPersonBoostFOV = 78f;
        [Tooltip("Cho phép nhấn phím V để đổi qua lại giữa góc nhìn 1P và 3P")]
        public bool allowViewToggleKey = true;
        public KeyCode toggleViewKey = KeyCode.V;

        [Header("Khoảng cách & Góc nhìn (3rd-Person)")]
        [Tooltip("Khoảng cách phía sau tàu")]
        public float distance = 12.5f;
        [Tooltip("Độ cao phía trên tàu")]
        public float height = 3.8f;
        [Tooltip("Tốc độ bám theo vị trí")]
        public float followSpeed = 10f;
        [Tooltip("Tốc độ xoay bám theo góc nhìn")]
        public float rotationSpeed = 8f;

        [Header("Hiệu ứng tốc độ cao (Speed Dynamic FOV)")]
        public float normalFOV = 52f;
        public float boostFOV = 70f;
        public float fovSmoothSpeed = 4f;

        private Camera _cam;
        private SpaceshipCockpit1P _cockpit;
        private Vector3 _smoothedHeadEuler;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            ResolveReferences();
            EnsureCockpit();
        }

        private void Start()
        {
            ResolveReferences();
            EnsureCockpit();
            ApplyViewModeState(snapImmediately: true);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            ResolveReferences();
            EnsureCockpit();
            SyncThirdPersonVisualsAndNearClip();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            if (_cockpit != null) _cockpit.SetVisible(false);
            if (spaceship != null) spaceship.SetThirdPersonShipVisualsVisible(true);
        }

        public void SetFirstPersonMode(bool enable)
        {
            firstPersonMode = enable;
            ApplyViewModeState(snapImmediately: true);
        }

        public void GetDesiredPose(out Vector3 pos, out Quaternion rot)
        {
            if (target == null)
            {
                pos = transform.position;
                rot = transform.rotation;
                return;
            }

            if (firstPersonMode)
            {
                pos = target.TransformPoint(firstPersonEyeOffset);
                Vector3 lookTarget = target.position + (target.forward * firstPersonLookAhead) + (target.up * 0.25f);
                float bankRoll = spaceship != null ? spaceship.CurrentBankAngle * 0.35f : 0f;
                Vector3 upDir = Quaternion.AngleAxis(bankRoll, target.forward) * target.up;
                rot = Quaternion.LookRotation(lookTarget - pos, upDir);
            }
            else
            {
                pos = target.position - (target.forward * distance) + (target.up * height);
                Vector3 lookTarget = target.position + (target.forward * 5.5f) + (target.up * 0.4f);
                rot = Quaternion.LookRotation(lookTarget - pos, target.up);
            }
        }

        public void SnapToTargetImmediate()
        {
            ResolveReferences();
            if (target == null) return;
            GetDesiredPose(out Vector3 pos, out Quaternion rot);
            transform.SetPositionAndRotation(pos, rot);
        }

        private void ResolveReferences()
        {
            if (spaceship == null && target != null)
            {
                spaceship = target.GetComponent<PlayerSpaceshipController>();
            }
            if (spaceship == null)
            {
                spaceship = FindFirstObjectByType<PlayerSpaceshipController>();
            }
            if (target == null && spaceship != null)
            {
                target = spaceship.transform;
            }
        }

        private void EnsureCockpit()
        {
            if (_cockpit == null)
            {
                _cockpit = GetComponent<SpaceshipCockpit1P>();
                if (_cockpit == null) _cockpit = gameObject.AddComponent<SpaceshipCockpit1P>();
            }
            _cockpit.cameraFollow = this;
            _cockpit.spaceship = spaceship;
        }

        private void SyncThirdPersonVisualsAndNearClip()
        {
            if (_cam != null)
            {
                _cam.nearClipPlane = firstPersonMode ? 0.08f : 0.3f;
            }
            if (spaceship != null)
            {
                spaceship.SetThirdPersonShipVisualsVisible(!firstPersonMode);
            }
            if (_cockpit != null)
            {
                _cockpit.SetVisible(firstPersonMode);
            }
        }

        private void ApplyViewModeState(bool snapImmediately)
        {
            SyncThirdPersonVisualsAndNearClip();

            if (_cam != null)
            {
                bool boosting = spaceship != null && spaceship.IsBoosting;
                _cam.fieldOfView = firstPersonMode
                    ? (boosting ? firstPersonBoostFOV : firstPersonNormalFOV)
                    : (boosting ? boostFOV : normalFOV);
            }

            if (snapImmediately)
            {
                SnapToTargetImmediate();
            }

            if (_cockpit != null)
            {
                _cockpit.UpdateCockpit(0.016f);
            }
        }

        private void LateUpdate()
        {
            ResolveReferences();
            if (target == null) return;

            if (allowViewToggleKey
                && Time.timeScale > 0f
                && Input.GetKeyDown(toggleViewKey)
                && !AboutInfoStation.AnyStationActive
                && (spaceship == null || !spaceship.IsRespawning))
            {
                SetFirstPersonMode(!firstPersonMode);
            }

            GetDesiredPose(out Vector3 targetPos, out Quaternion targetRot);

            if (firstPersonMode)
            {
                // Khóa cứng vào buồng lái nội suy của tàu + độ trễ G-force tự nhiên của đầu phi công & rung phản lực Boost
                Vector3 finalPos = targetPos;
                Quaternion finalRot = targetRot;

                if (spaceship != null && Time.deltaTime > 0f)
                {
                    float pitchLag = Mathf.Clamp(-spaceship.CurrentPitchRate * 0.0085f, -0.75f, 0.75f);
                    float yawLag = Mathf.Clamp(-spaceship.CurrentYawRate * 0.0085f, -0.85f, 0.85f);
                    float rollLag = Mathf.Clamp(-spaceship.CurrentRollRate * 0.012f, -1.6f, 1.6f);
                    float headBlend = 1f - Mathf.Exp(-7.5f * Time.deltaTime);
                    _smoothedHeadEuler = Vector3.Lerp(_smoothedHeadEuler, new Vector3(pitchLag, yawLag, rollLag), headBlend);
                    finalRot = targetRot * Quaternion.Euler(_smoothedHeadEuler);

                    float shakeAmp = spaceship.IsBoosting ? 0.0034f : (Mathf.Clamp01(spaceship.CurrentSpeedRatio - 0.55f) * 0.0012f);
                    float fireElapsed = Time.time - spaceship.LastFireTime;
                    if (fireElapsed >= 0f && fireElapsed < 0.09f)
                    {
                        float kick = (1f - fireElapsed / 0.09f) * 0.0045f;
                        finalPos -= (finalRot * Vector3.forward) * kick;
                    }

                    if (shakeAmp > 0.0001f)
                    {
                        float nx = (Mathf.PerlinNoise(Time.time * 28f, 1.7f) - 0.5f) * 2f;
                        float ny = (Mathf.PerlinNoise(3.4f, Time.time * 28f) - 0.5f) * 2f;
                        finalPos += finalRot * new Vector3(nx * shakeAmp, ny * shakeAmp, 0f);
                    }
                }

                transform.SetPositionAndRotation(finalPos, finalRot);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }

            if (_cam != null)
            {
                float desiredNear = firstPersonMode ? 0.08f : 0.3f;
                if (!Mathf.Approximately(_cam.nearClipPlane, desiredNear))
                {
                    _cam.nearClipPlane = desiredNear;
                }

                float baseNormal = firstPersonMode ? firstPersonNormalFOV : normalFOV;
                float baseBoost = firstPersonMode ? firstPersonBoostFOV : boostFOV;
                bool boosting = spaceship != null && spaceship.IsBoosting;
                float targetFOV = boosting ? baseBoost : baseNormal;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
            }

            EnsureCockpit();
            if (_cockpit != null)
            {
                _cockpit.UpdateCockpit(Time.deltaTime);
            }
        }
    }
}

