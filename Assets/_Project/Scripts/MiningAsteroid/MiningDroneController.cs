using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Section 4: Autonomous Mining Drones (Drone Khai Khoáng Tự Hành).
    /// - Di chuyển mượt mà giữa các cụm tinh thể được chỉ định và bệ khai khoáng.
    /// - Dừng ở khoảng cách an toàn trước tinh thể, bật tia laser khai khoáng ngắn & phát hạt tia lửa nhỏ.
    /// - Quay trở về bệ khai thác lân cận sau khi chiết xuất xong.
    /// - Phản ứng với xung năng lượng từ Lõi Trung Tâm.
    /// </summary>
    public class MiningDroneController : MonoBehaviour
    {
        [Header("Inspector Settings (Thông Số Điều Chỉnh)")]
        public float droneMovementSpeed = 7.5f;
        public float droneMiningDuration = 3.5f;
        public float turnSmoothSpeed = 5.0f;
        public float standoffDistance = 4.2f;
        public float standoffHeight = 2.4f;

        [Header("Tương Tác Quét Người Chơi (Feature E)")]
        public float playerDetectionRadius = 15f;
        [Range(1.2f, 5f)] public float scanDuration = 2.6f;
        public float scanCooldown = 9.0f;
        public Color scanBeamColor = new Color(0.08f, 0.95f, 1f, 1f);
        public LineRenderer scanBeam;

        [Header("Lộ Trình Khai Khoáng")]
        public List<Transform> targetCrystals = new List<Transform>();
        public Transform homeMiningPlatform;
        public int startingCrystalIndex = 0;

        [Header("Thiết Bị Laser & VFX")]
        public Transform laserNozzle;
        public LineRenderer miningLaser;
        public Light laserTipLight;
        public ParticleSystem extractionSparks;
        public Light navigationLight;

        private enum State { ToCrystal, Extracting, ToPlatform, Unloading, ScanningPlayer }
        private State _state = State.ToCrystal;
        private State _resumeState = State.ToCrystal;
        private float _resumeTimer;
        private int _crystalIndex;
        private float _stateTimer;
        private float _scanCooldownTimer;
        private float _bobSeed;
        private float _pulseSpeedBoost = 1f;
        private Transform _playerTransform;

        private void Start()
        {
            _crystalIndex = startingCrystalIndex;
            _bobSeed = Random.Range(0f, 10f);
            SetMiningActive(false, Vector3.zero);
            EnsureScanBeam();
            SetScanBeamActive(false, Vector3.zero);
        }

        private void EnsureScanBeam()
        {
            if (scanBeam != null) return;
            var go = new GameObject("PlayerScanBeam");
            go.transform.SetParent(transform, false);
            scanBeam = go.AddComponent<LineRenderer>();
            scanBeam.useWorldSpace = true;
            scanBeam.positionCount = 2;
            scanBeam.startWidth = 0.09f;
            scanBeam.endWidth = 0.24f;
            Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(sh);
            mat.color = new Color(scanBeamColor.r, scanBeamColor.g, scanBeamColor.b, 0.72f);
            scanBeam.sharedMaterial = mat;
            scanBeam.startColor = scanBeamColor;
            scanBeam.endColor = new Color(scanBeamColor.r, scanBeamColor.g, scanBeamColor.b, 0.15f);
            scanBeam.enabled = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;

            if (_pulseSpeedBoost > 1f)
            {
                _pulseSpeedBoost = Mathf.MoveTowards(_pulseSpeedBoost, 1f, dt * 0.5f);
            }
            if (_scanCooldownTimer > 0f)
            {
                _scanCooldownTimer -= dt;
            }
            if (_playerTransform == null)
            {
                _playerTransform = AsteroidInteractionHUD.GetPlayerTransform();
            }

            // Feature E: Player detection & scan check
            if (_state != State.ScanningPlayer && _scanCooldownTimer <= 0f && _playerTransform != null)
            {
                bool busyUI = AboutInfoStation.AnyStationActive
                              || AboutInfoStation.AnyLandingSequenceActive
                              || SpawnZoneAnimator.IsAnyIntroWalkActive;
                if (!busyUI)
                {
                    float pDist = Vector3.Distance(transform.position, _playerTransform.position);
                    if (pDist <= playerDetectionRadius)
                    {
                        BeginPlayerScan();
                        return;
                    }
                }
            }

            if (_state == State.ScanningPlayer)
            {
                UpdatePlayerScanState(dt, time);
                return;
            }

            if (targetCrystals == null || targetCrystals.Count == 0) return;
            _crystalIndex = Mathf.Clamp(_crystalIndex, 0, targetCrystals.Count - 1);
            Transform crystal = targetCrystals[_crystalIndex];
            if (crystal == null) return;

            // Điểm dừng lơ lửng phía trước và cao hơn tinh thể để không xuyên vào đá
            Vector3 fromCenterDir = (crystal.position - (homeMiningPlatform != null ? homeMiningPlatform.position : transform.position));
            fromCenterDir.y = 0f;
            if (fromCenterDir.sqrMagnitude < 0.01f) fromCenterDir = Vector3.forward;
            fromCenterDir.Normalize();

            Vector3 hoverPos = crystal.position - fromCenterDir * standoffDistance + Vector3.up * standoffHeight;
            hoverPos.y += Mathf.Sin(time * 2.6f + _bobSeed) * 0.28f;

            Vector3 crystalAimPoint = crystal.position + Vector3.up * 1.1f;

            switch (_state)
            {
                case State.ToCrystal:
                    if (MoveSmoothlyTo(hoverPos, crystalAimPoint, dt))
                    {
                        _state = State.Extracting;
                        _stateTimer = droneMiningDuration;
                        SetMiningActive(true, crystalAimPoint);
                    }
                    break;

                case State.Extracting:
                    _stateTimer -= dt;
                    // Giữ vị trí lơ lửng nhẹ nhàng và hướng mũi vào tinh thể
                    transform.position = Vector3.Lerp(transform.position, hoverPos, 4f * dt);
                    LookSmoothlyAt(crystalAimPoint, dt * 1.5f);
                    UpdateMiningLaserBeam(crystalAimPoint, time);

                    if (_stateTimer <= 0f)
                    {
                        SetMiningActive(false, Vector3.zero);
                        _state = State.ToPlatform;
                    }
                    break;

                case State.ToPlatform:
                    Vector3 depotPos = homeMiningPlatform != null
                        ? homeMiningPlatform.position + Vector3.up * 3.2f
                        : transform.position + Vector3.up * 4f;
                    depotPos.y += Mathf.Sin(time * 2.6f + _bobSeed) * 0.22f;

                    if (MoveSmoothlyTo(depotPos, depotPos + transform.forward, dt))
                    {
                        _state = State.Unloading;
                        _stateTimer = 1.8f;
                    }
                    break;

                case State.Unloading:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f)
                    {
                        _crystalIndex = (_crystalIndex + 1) % targetCrystals.Count;
                        _state = State.ToCrystal;
                    }
                    break;
            }
        }

        private bool MoveSmoothlyTo(Vector3 destination, Vector3 lookTargetOnArrive, float dt)
        {
            Vector3 toDest = destination - transform.position;
            float dist = toDest.magnitude;
            if (dist <= 0.85f)
            {
                return true;
            }

            Vector3 dir = toDest / dist;
            float speed = droneMovementSpeed * _pulseSpeedBoost * Mathf.Clamp01(dist / 3.5f + 0.25f);
            transform.position += dir * (speed * dt);
            LookSmoothlyAt(destination, dt);
            return false;
        }

        private void LookSmoothlyAt(Vector3 worldPoint, float dt)
        {
            Vector3 lookDir = worldPoint - transform.position;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSmoothSpeed * dt);
            }
        }

        private void SetMiningActive(bool active, Vector3 hitPoint)
        {
            if (miningLaser != null) miningLaser.enabled = active;
            if (laserTipLight != null)
            {
                laserTipLight.enabled = active;
                if (active) laserTipLight.transform.position = hitPoint;
            }
            if (extractionSparks != null)
            {
                if (active)
                {
                    extractionSparks.transform.position = hitPoint;
                    extractionSparks.Play();
                }
                else
                {
                    extractionSparks.Stop();
                }
            }
        }

        private void UpdateMiningLaserBeam(Vector3 hitPoint, float time)
        {
            Vector3 origin = laserNozzle != null ? laserNozzle.position : (transform.position + transform.forward * 1.1f);
            if (miningLaser != null)
            {
                miningLaser.SetPosition(0, origin);
                miningLaser.SetPosition(1, hitPoint);
                float w = 0.11f + 0.03f * Mathf.Sin(time * 28f);
                miningLaser.startWidth = w;
                miningLaser.endWidth = w * 0.65f;
            }
            if (laserTipLight != null)
            {
                laserTipLight.transform.position = hitPoint;
                laserTipLight.intensity = 2.6f + 0.8f * Mathf.Sin(time * 22f);
            }
            if (extractionSparks != null)
            {
                extractionSparks.transform.position = hitPoint;
            }
        }

        public bool IsScanningPlayer => _state == State.ScanningPlayer;

        public void BeginPlayerScan()
        {
            _resumeState = _state;
            _resumeTimer = _stateTimer;
            SetMiningActive(false, Vector3.zero);
            _state = State.ScanningPlayer;
            _stateTimer = scanDuration;
            AsteroidInteractionHUD.PlayTerminalClick();
        }

        private void UpdatePlayerScanState(float dt, float time)
        {
            _stateTimer -= dt;

            // Gentle hover bob while scanning
            Vector3 hover = transform.position + Vector3.up * (Mathf.Sin(time * 3f + _bobSeed) * 0.18f * dt);
            transform.position = hover;

            if (_playerTransform != null)
            {
                LookSmoothlyAt(_playerTransform.position, dt * 1.45f);
                Vector3 sweepOffset = Vector3.up * (Mathf.Sin(time * 7.5f) * 0.55f);
                SetScanBeamActive(true, _playerTransform.position + sweepOffset);
            }

            if (_stateTimer <= 0f)
            {
                SetScanBeamActive(false, Vector3.zero);
                _scanCooldownTimer = scanCooldown;
                _state = _resumeState;
                _stateTimer = _resumeTimer;

                if (_state == State.Extracting && targetCrystals != null && targetCrystals.Count > 0)
                {
                    Transform crystal = targetCrystals[Mathf.Clamp(_crystalIndex, 0, targetCrystals.Count - 1)];
                    if (crystal != null)
                    {
                        SetMiningActive(true, crystal.position + Vector3.up * 1.1f);
                    }
                }
            }
        }

        private void SetScanBeamActive(bool active, Vector3 targetPoint)
        {
            if (scanBeam == null) return;
            scanBeam.enabled = active;
            if (active)
            {
                Vector3 origin = laserNozzle != null ? laserNozzle.position : (transform.position + transform.forward * 1.1f);
                scanBeam.SetPosition(0, origin);
                scanBeam.SetPosition(1, targetPoint);
            }
            if (navigationLight != null)
            {
                navigationLight.intensity = active ? 3.4f : 1.6f;
            }
        }

        public void ReactToCorePulse()
        {
            if (!gameObject.activeInHierarchy) return;
            _pulseSpeedBoost = 1.65f;
            StartCoroutine(PulseNavLightRoutine());
        }

        private IEnumerator PulseNavLightRoutine()
        {
            if (navigationLight == null) yield break;
            float baseInt = navigationLight.intensity;
            navigationLight.intensity = baseInt * 2.8f;
            yield return new WaitForSeconds(1.2f);
            if (navigationLight != null) navigationLight.intensity = baseInt;
        }
    }
}
