using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Bộ điều khiển phi thuyền không gian Cyberpunk (Flight, Hover, Twin Plasma Laser & Sci-Fi Combat HUD):
    /// - Đệm nâng phản trọng lực + Trọng lực thực tế.
    /// - Pháo kép Laser Plasma (Chuột Trái / Phím J).
    /// - Khi chết sẽ nổ tung và Hồi Sinh chính xác tại Điểm Spawn ban đầu (PlayerSpawnPoint).
    /// - Thanh máu Cyberpunk dạng 12 ô năng lượng phát sáng trên Canvas (chỉ hiện khi bị truy đuổi) + Nháy đỏ màn hình khi sắp hết máu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class PlayerSpaceshipController : MonoBehaviour
    {
        [Header("Khu Vực Xuất Phát Động (Spawn_Zone + Offset)")]
        [Tooltip("Object gốc của vùng Spawn (Spawn_Zone) — khi di chuyển vùng này, vị trí tàu spawn sẽ tự động đi theo")]
        public Transform spawnZone;
        [Tooltip("Tọa độ tương đối (Local Offset) của tàu tính từ tâm Spawn_Zone")]
        public Vector3 spawnPositionOffset = new Vector3(0f, 3.85f, -1.5f);
        [Tooltip("Góc xoay tương đối (Local Euler) của tàu so với hướng của Spawn_Zone")]
        public Vector3 spawnRotationOffset = Vector3.zero;
        [Tooltip("Tự động đặt tàu vào (Spawn_Zone + Offset) ngay khi bấm Play")]
        public bool snapToSpawnZoneOnStart = true;
        [Tooltip("Điểm neo phụ (tùy chọn)")]
        public Transform spawnPoint;

        [Header("Trọng Lực & Đệm Nâng Lơ Lửng (Hover & Gravity)")]
        public float gravity = 12f;
        public float targetHoverHeight = 2.2f;
        public float hoverSpring = 28f;
        public float hoverDamping = 8f;
        public float bobAmplitude = 0.15f;
        public float bobFrequency = 2.2f;
        public LayerMask groundLayer = ~0;

        [Header("Tốc Độ Động Cơ (Engine Propulsion)")]
        public float cruiseSpeed = 30f;
        public float boostSpeed = 60f;
        public float reverseSpeed = 14f;
        public float verticalSpeed = 16f;
        public float acceleration = 6f;

        [Header("Cơ Chế Lái & Khí Động Học Không Gian")]
        public float turnSpeed = 70f;
        public float pitchSpeed = 45f;
        public float maxBankAngle = 32f;
        public float bankSmoothing = 5f;

        [Header("Hệ Thống Tự Cân Bằng Sau Va Chạm (Gyro Auto-Stabilizer)")]
        [Tooltip("Tốc độ con quay hồi chuyển tự lấy lại thăng bằng phương ngang sau va chạm")]
        public float horizonStabilizeSpeed = 10f;
        [Tooltip("Tốc độ tự trả mũi tàu (Pitch) về mặt phẳng ngang")]
        public float pitchAutoLevelSpeed = 5.5f;
        [Tooltip("Độ triệt tiêu lực xoay văng vật lý sau khi va đập")]
        public float gyroAngularDamping = 14f;
        [Tooltip("Góc ngóc/chúi tối đa cho phép (độ)")]
        public float maxPitchAngle = 26f;

        [Header("Hệ Thống Vũ Khí & Giáp Chiến Đấu (Space Combat)")]
        public float fireInterval = 0.16f;
        public float laserDamage = 35f;
        public float maxHealth = 200f;
        public float shieldRegenRate = 12f;

        [Header("Hiệu Ứng Động Cơ Phản Lực (VFX)")]
        public Transform visualModel;
        public Light engineLight;
        public ParticleSystem leftThrusterParticle;
        public ParticleSystem rightThrusterParticle;

        private Rigidbody _rb;
        private float _currentForwardSpeed;
        private float _targetForwardSpeed;
        private float _currentVerticalSpeed;
        private float _targetVerticalSpeed;
        private float _currentBank;
        private float _currentYaw;
        private float _currentPitch;
        private float _impactRollJolt;
        private float _impactPitchJolt;
        private bool _isBoosting;
        private bool _isHoveringOverGround;
        private float _groundDistance;

        private float _currentHealth;
        private float _displayedHealthRatio = 1f;
        private float _nextFireTime;
        private float _lastDamageTime = -99f;
        private float _collisionRecoilTimer;
        private Vector3 _contactNormalSum;
        private bool _isRespawning;
        private Vector3 _spawnPos = new Vector3(-65f, 28.7f, 45f);
        private Quaternion _spawnRot = Quaternion.Euler(0f, 115f, 0f);

        // UI Canvas HUD Elements
        private GameObject _combatHudRoot;
        private Image _redScreenOverlay;
        private Image[] _redBorders;
        private Image _hudOuterBorder;
        private Image _damageLagBar;
        private Image[] _healthCells;
        private TMP_Text _healthValueText;
        private TMP_Text _healthStatusText;
        private Image _alertBannerBg;
        private TMP_Text _alertSubText;

        public bool IsBoosting => _isBoosting;
        public bool IsHovering => _isHoveringOverGround;
        public float CurrentSpeedRatio => Mathf.Abs(_currentForwardSpeed) / boostSpeed;
        public float HealthRatio => Mathf.Clamp01(_currentHealth / maxHealth);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.linearDamping = 1.8f;
            _rb.angularDamping = 3.5f;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CaptureSpawnPose();
            _currentYaw = transform.eulerAngles.y;
            _currentPitch = 0f;
            _currentHealth = maxHealth;
            _displayedHealthRatio = 1f;

            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = false;
                if (col.size == Vector3.one)
                {
                    col.size = new Vector3(3.2f, 1.4f, 4.2f);
                    col.center = new Vector3(0f, 0.5f, 0f);
                }
            }

            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
            }
        }

        private void OnEnable()
        {
            if (_currentHealth <= 0f) _currentHealth = maxHealth;
            CaptureSpawnPose();
            _currentYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            if (snapToSpawnZoneOnStart)
            {
                RespawnImmediate();
            }
        }

        /// <summary>
        /// Tính toán vị trí & góc xoay xuất phát động theo công thức: Vị trí Spawn_Zone hiện tại + Local Offset.
        /// Nhờ đó khi kéo vùng Spawn_Zone đi bất cứ đâu trong Scene, điểm hồi sinh luôn đi theo.
        /// </summary>
        public void GetDynamicSpawnPose(out Vector3 pos, out Quaternion rot)
        {
            if (spawnZone == null)
            {
                var sz = GameObject.Find("Spawn_Zone");
                if (sz != null) spawnZone = sz.transform;
            }

            if (spawnZone != null)
            {
                pos = spawnZone.TransformPoint(spawnPositionOffset);
                rot = spawnZone.rotation * Quaternion.Euler(spawnRotationOffset);
                if (spawnPoint != null)
                {
                    spawnPoint.SetPositionAndRotation(pos, rot);
                }
                return;
            }

            if (spawnPoint == null)
            {
                var spGo = GameObject.Find("PlayerSpawnPoint");
                if (spGo != null) spawnPoint = spGo.transform;
            }

            if (spawnPoint != null)
            {
                pos = spawnPoint.position;
                rot = spawnPoint.rotation;
                return;
            }

            pos = _spawnPos;
            rot = _spawnRot;
        }

        private void CaptureSpawnPose()
        {
            GetDynamicSpawnPose(out _spawnPos, out _spawnRot);
        }

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                if (_combatHudRoot != null) _combatHudRoot.SetActive(false);
                return;
            }

            UpdateCombatHUD();

            if (_isRespawning) return;

            ReadPlayerInput();
            HandleWeaponFire();
            UpdateShieldRegen();
            UpdateVisualEffects();
        }

        private void FixedUpdate()
        {
            if (_isRespawning) return;
            if (_rb != null && _rb.isKinematic) return;

            if (_collisionRecoilTimer > 0f)
            {
                _collisionRecoilTimer -= Time.fixedDeltaTime;
            }

            ApplyHoverAndGravityPhysics();
            ApplyPropulsionPhysics();
            ApplySteeringPhysics();
        }

        private void ReadPlayerInput()
        {
            _isBoosting = Input.GetKey(KeyCode.LeftShift);

            float forwardInput = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) forwardInput = 1f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) forwardInput = -1f;

            if (forwardInput > 0f)
                _targetForwardSpeed = _isBoosting ? boostSpeed : cruiseSpeed;
            else if (forwardInput < 0f)
                _targetForwardSpeed = -reverseSpeed;
            else
                _targetForwardSpeed = 0f;

            _currentForwardSpeed = Mathf.Lerp(_currentForwardSpeed, _targetForwardSpeed, Time.deltaTime * acceleration);

            float vertInput = 0f;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E)) vertInput = 1f;
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.Q)) vertInput = -1f;

            _targetVerticalSpeed = vertInput * verticalSpeed;
            _currentVerticalSpeed = Mathf.Lerp(_currentVerticalSpeed, _targetVerticalSpeed, Time.deltaTime * acceleration);
        }

        private void HandleWeaponFire()
        {
            if (AboutInfoStation.AnyStationActive) return;

            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool firePressed = (!pointerOverUI && Input.GetMouseButton(0)) || Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.LeftAlt);

            if (firePressed && Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + fireInterval;
                FireTwinCannons();
            }
        }

        private void FireTwinCannons()
        {
            Vector3 convergePoint = transform.position + transform.forward * 85f;

            Vector3 leftMuzzle = transform.position + transform.forward * 2.8f - transform.right * 1.35f + transform.up * 0.25f;
            Vector3 rightMuzzle = transform.position + transform.forward * 2.8f + transform.right * 1.35f + transform.up * 0.25f;

            Vector3 leftDir = (convergePoint - leftMuzzle).normalized;
            Vector3 rightDir = (convergePoint - rightMuzzle).normalized;

            SpaceLaserProjectile.SpawnBolt(leftMuzzle, leftDir, true, gameObject, 125f, laserDamage);
            SpaceLaserProjectile.SpawnBolt(rightMuzzle, rightDir, true, gameObject, 125f, laserDamage);
        }

        private void UpdateShieldRegen()
        {
            if (_currentHealth < maxHealth && Time.time - _lastDamageTime > 3.5f)
            {
                _currentHealth = Mathf.Min(maxHealth, _currentHealth + shieldRegenRate * Time.deltaTime);
            }
        }

        public void RechargeEnergy(float rate, bool instantFull = false)
        {
            if (_isRespawning) return;
            if (instantFull)
            {
                _currentHealth = maxHealth;
            }
            else
            {
                _currentHealth = Mathf.Min(maxHealth, _currentHealth + rate * Time.deltaTime);
            }
            _lastDamageTime = -99f;
        }

        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (_isRespawning || AboutInfoStation.AnyStationActive) return;

            _lastDamageTime = Time.time;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);

            if (_currentHealth <= 0f)
            {
                StartCoroutine(ExplodeAndRespawnRoutine());
            }
        }

        public void RespawnImmediate()
        {
            CaptureSpawnPose();
            SpaceTrafficShipAI.ClearFleetHostileAlert();

            _currentForwardSpeed = 0f;
            _targetForwardSpeed = 0f;
            _currentVerticalSpeed = 0f;
            _targetVerticalSpeed = 0f;
            _currentBank = 0f;
            _currentPitch = 0f;
            _impactRollJolt = 0f;
            _impactPitchJolt = 0f;
            _currentYaw = _spawnRot.eulerAngles.y;
            _currentHealth = maxHealth;
            _displayedHealthRatio = 1f;

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.position = _spawnPos;
                _rb.rotation = _spawnRot;
            }
            transform.SetPositionAndRotation(_spawnPos, _spawnRot);
            Physics.SyncTransforms();

            if (visualModel != null)
            {
                visualModel.gameObject.SetActive(true);
                visualModel.localRotation = Quaternion.identity;
            }

            var camFollow = FindFirstObjectByType<SpaceshipCameraFollow>();
            if (camFollow != null)
            {
                Vector3 camPos = _spawnPos - (_spawnRot * Vector3.forward) * camFollow.distance + Vector3.up * camFollow.height;
                camFollow.transform.position = camPos;
                camFollow.transform.LookAt(_spawnPos + (_spawnRot * Vector3.forward) * 4f);
            }
        }

        private IEnumerator ExplodeAndRespawnRoutine()
        {
            _isRespawning = true;
            SpaceExplosionVFX.SpawnExplosion(transform.position, 5.2f, false, null);
            SpaceTrafficShipAI.ClearFleetHostileAlert();

            if (visualModel != null) visualModel.gameObject.SetActive(false);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            yield return new WaitForSeconds(1.5f);

            RespawnImmediate();
            _isRespawning = false;
        }

        private void ApplyHoverAndGravityPhysics()
        {
            Vector3 rayOrigin = transform.position - (Vector3.up * 0.35f);
            float checkDist = targetHoverHeight + 4.5f;

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, checkDist, groundLayer, QueryTriggerInteraction.Ignore);
            bool hitGround = false;
            float closestDist = 999f;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.transform.root != transform.root)
                {
                    if (hits[i].distance < closestDist)
                    {
                        closestDist = hits[i].distance;
                        hitGround = true;
                    }
                }
            }

            _isHoveringOverGround = hitGround;
            _groundDistance = hitGround ? (closestDist + 0.35f) : 999f;

            if (hitGround)
            {
                // Khi người chơi chủ động giữ phím hạ cánh (Ctrl / C / Q): hạ đệm nâng xuống 0.35m để tiếp đất thay vì giữ cố định 2.2m
                float baseHeight = (_targetVerticalSpeed < -0.1f) ? 0.35f : targetHoverHeight;
                float bobOffset = (_targetVerticalSpeed < -0.1f && _groundDistance < 0.6f) ? 0f : (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
                float desiredHeight = baseHeight + bobOffset;

                float error = desiredHeight - _groundDistance;
                float upwardVel = _rb.linearVelocity.y;
                float springForce = (error * hoverSpring) - (upwardVel * hoverDamping);

                if (_targetVerticalSpeed >= 0f)
                {
                    _rb.AddForce(Vector3.up * springForce, ForceMode.Acceleration);
                }
                else
                {
                    if (error > 0f)
                    {
                        _rb.AddForce(Vector3.up * (springForce * 0.4f), ForceMode.Acceleration);
                    }
                }

                // Khi tàu đã tiếp đất ổn định (đáp thành công): triệt tiêu trượt ngang để tàu đỗ vững chắc
                if (_groundDistance <= 0.45f && Mathf.Abs(_currentForwardSpeed) < 3f)
                {
                    Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                    _rb.AddForce(-flatVel * 6f, ForceMode.Acceleration);
                }
            }
            else
            {
                _rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
            }
        }

        private void ApplyPropulsionPhysics()
        {
            if (_collisionRecoilTimer > 0f)
            {
                _contactNormalSum = Vector3.zero;
                return;
            }

            Vector3 forwardForce = transform.forward * _currentForwardSpeed;
            Vector3 verticalVelocity = Vector3.up * _currentVerticalSpeed;

            Vector3 desiredVel;
            if (Mathf.Abs(_targetVerticalSpeed) < 0.1f)
            {
                Vector3 currentVel = _rb.linearVelocity;
                desiredVel = new Vector3(forwardForce.x, currentVel.y, forwardForce.z);
            }
            else
            {
                desiredVel = forwardForce + verticalVelocity;
            }

            // 1. Chặn ép xuyên vật thể khi đang tiếp xúc trực tiếp (OnCollisionStay)
            if (_contactNormalSum.sqrMagnitude > 0.0001f)
            {
                Vector3 contactNormal = _contactNormalSum.normalized;
                if (Vector3.Dot(desiredVel, contactNormal) < 0f)
                {
                    desiredVel = Vector3.ProjectOnPlane(desiredVel, contactNormal) + contactNormal * 1.5f;
                    _currentForwardSpeed *= 0.65f;
                }
                _contactNormalSum = Vector3.zero;
            }

            // 2. Quét SphereCast phía trước theo hướng bay để chống xuyên tường ở tốc độ Boost cao (Continuous Anti-Tunneling)
            float speedMag = desiredVel.magnitude;
            if (speedMag > 0.5f)
            {
                Vector3 moveDir = desiredVel / speedMag;
                float sweepDist = Mathf.Max(2.2f, speedMag * Time.fixedDeltaTime * 2.2f + 1.5f);
                RaycastHit[] hits = Physics.SphereCastAll(transform.position, 1.15f, moveDir, sweepDist, ~0, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider col = hits[i].collider;
                    if (col == null || col.isTrigger || col.transform.root == transform.root) continue;
                    if (hits[i].distance <= 0.001f) continue;

                    Vector3 hitNormal = hits[i].normal;
                    float intoWall = Vector3.Dot(desiredVel, hitNormal);
                    if (intoWall < 0f && hits[i].distance < 2.1f)
                    {
                        desiredVel = Vector3.ProjectOnPlane(desiredVel, hitNormal) + hitNormal * 2.0f;
                        _currentForwardSpeed *= 0.5f;
                    }
                }
            }

            if (_rb != null && !_rb.isKinematic)
            {
                _rb.linearVelocity = desiredVel;
            }
        }

        private void ApplySteeringPhysics()
        {
            // 1. Triệt tiêu lực xoáy văng vật lý (Angular Velocity) sau va chạm bằng Con Quay Hồi Chuyển
            if (_rb != null && !_rb.isKinematic && _rb.angularVelocity.sqrMagnitude > 0.0001f)
            {
                _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * gyroAngularDamping);
            }

            // Đồng bộ góc hướng mũi tàu thực tế trên mặt phẳng ngang để tránh lệch góc sau khi va đập mạnh
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude > 0.01f && _collisionRecoilTimer > 0f)
            {
                _currentYaw = Mathf.LerpAngle(_currentYaw, Quaternion.LookRotation(flatForward, Vector3.up).eulerAngles.y, Time.fixedDeltaTime * 8f);
            }

            float turnInput = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turnInput = 1f;
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turnInput = -1f;

            _currentYaw += turnInput * turnSpeed * Time.fixedDeltaTime;

            // 2. Điều khiển ngóc/chúi mũi tàu (Pitch) & Tự động trả về thăng bằng ngang (0 độ)
            bool manualPitch = false;
            if (Input.GetKey(KeyCode.I))
            {
                _currentPitch -= pitchSpeed * Time.fixedDeltaTime;
                manualPitch = true;
            }
            else if (Input.GetKey(KeyCode.K))
            {
                _currentPitch += pitchSpeed * Time.fixedDeltaTime;
                manualPitch = true;
            }

            if (!manualPitch)
            {
                float targetAutoPitch = 0f;
                if (Mathf.Abs(_currentVerticalSpeed) > 0.5f && _currentForwardSpeed > 3f)
                {
                    targetAutoPitch = Mathf.Clamp(-(_currentVerticalSpeed / verticalSpeed) * 14f, -14f, 14f);
                }
                _currentPitch = Mathf.Lerp(_currentPitch, targetAutoPitch, Time.fixedDeltaTime * pitchAutoLevelSpeed);
            }
            _currentPitch = Mathf.Clamp(_currentPitch, -maxPitchAngle, maxPitchAngle);

            // 3. Tự động lấy lại thăng bằng đường chân trời (Khử hoàn toàn góc nghiêng Roll bị lệch do va chạm)
            Quaternion targetBalancedRot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Quaternion stabilizedRot = Quaternion.Slerp(_rb.rotation, targetBalancedRot, Time.fixedDeltaTime * horizonStabilizeSpeed);
            _rb.MoveRotation(stabilizedRot);

            // 4. Giảm chấn rung lắc thân tàu sau va chạm & Nghiêng cánh khí động học (Visual Banking)
            _impactRollJolt = Mathf.Lerp(_impactRollJolt, 0f, Time.fixedDeltaTime * 7.5f);
            _impactPitchJolt = Mathf.Lerp(_impactPitchJolt, 0f, Time.fixedDeltaTime * 7.5f);

            float targetBank = -turnInput * maxBankAngle;
            float swayOffset = Mathf.Sin(Time.time * (bobFrequency * 0.8f)) * 1.5f;
            _currentBank = Mathf.Lerp(_currentBank, targetBank + swayOffset, Time.fixedDeltaTime * bankSmoothing);

            if (visualModel != null)
            {
                visualModel.localRotation = Quaternion.Euler(_impactPitchJolt, 0f, _currentBank + _impactRollJolt);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_rb == null || _isRespawning) return;

            if (collision.relativeVelocity.sqrMagnitude > 2.2f)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 normal = contact.normal;
                _contactNormalSum += normal;

                if (Vector3.Dot(normal, Vector3.up) < 0.65f)
                {
                    _collisionRecoilTimer = 0.24f;
                    _currentForwardSpeed *= -0.35f;
                    _rb.AddForce(normal * Mathf.Clamp(collision.relativeVelocity.magnitude * 0.5f, 4.5f, 18f), ForceMode.VelocityChange);

                    // Tạo hiệu ứng nghiêng chấn động ngắn trên thân tàu rồi tự cân bằng lại mượt mà
                    Vector3 localHit = transform.InverseTransformPoint(contact.point);
                    float impactStrength = Mathf.Clamp01(collision.relativeVelocity.magnitude / 30f);
                    _impactRollJolt = Mathf.Clamp(-localHit.x * 18f * impactStrength, -25f, 25f);
                    _impactPitchJolt = Mathf.Clamp(localHit.z * 12f * impactStrength, -18f, 18f);
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (_rb == null || _isRespawning) return;

            int count = collision.contactCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 n = collision.GetContact(i).normal;
                if (Vector3.Dot(n, Vector3.up) < 0.75f)
                {
                    _contactNormalSum += n;
                }
            }
        }

        private void UpdateVisualEffects()
        {
            if (engineLight != null)
            {
                float baseIntensity = _isBoosting ? 4.5f : (_currentForwardSpeed > 1f ? 2.2f : 0.8f);
                float pulse = Mathf.Sin(Time.time * 15f) * 0.15f;
                engineLight.intensity = Mathf.Lerp(engineLight.intensity, baseIntensity + pulse, Time.deltaTime * 10f);
                engineLight.color = _isBoosting ? new Color(1f, 0.4f, 0.1f) : new Color(0.2f, 0.8f, 1f);
            }

            UpdateThrusterParticle(leftThrusterParticle);
            UpdateThrusterParticle(rightThrusterParticle);
        }

        private void UpdateThrusterParticle(ParticleSystem ps)
        {
            if (ps == null) return;

            var emission = ps.emission;
            var main = ps.main;

            if (_isBoosting)
            {
                emission.rateOverTime = 80f;
                main.startSpeed = 8f;
                main.startSize = 0.55f;
            }
            else if (_currentForwardSpeed > 1f)
            {
                emission.rateOverTime = 40f;
                main.startSpeed = 4f;
                main.startSize = 0.35f;
            }
            else
            {
                emission.rateOverTime = 15f;
                main.startSpeed = 1.8f;
                main.startSize = 0.22f;
            }
        }

        // ─────────────────── SCI-FI CYBERPUNK CANVAS COMBAT HUD ───────────────────
        private void EnsureCombatHUD()
        {
            if (_combatHudRoot != null) return;

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            var existing = canvas.transform.Find("CombatHUD_Root");
            if (existing != null) Destroy(existing.gameObject);

            _combatHudRoot = new GameObject("CombatHUD_Root", typeof(RectTransform));
            _combatHudRoot.transform.SetParent(canvas.transform, false);
            _combatHudRoot.transform.SetAsFirstSibling();

            var rootRt = _combatHudRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // 1. Màn hình nháy đỏ & 4 viền cảnh báo khi sắp hết máu (Low Health Red Vignette)
            _redScreenOverlay = CreateHudImage("RedScreenFlash", _combatHudRoot.transform, new Color(0.95f, 0.04f, 0.06f, 0f));
            var overlayRt = _redScreenOverlay.rectTransform;
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            _redBorders = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                _redBorders[i] = CreateHudImage("RedEdge_" + i, _combatHudRoot.transform, new Color(1f, 0.05f, 0.08f, 0f));
                var brt = _redBorders[i].rectTransform;
                if (i == 0) { brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0.5f, 1f); brt.sizeDelta = new Vector2(0, 22f); }
                if (i == 1) { brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0.5f, 0f); brt.sizeDelta = new Vector2(0, 22f); }
                if (i == 2) { brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(0, 1); brt.pivot = new Vector2(0f, 0.5f); brt.sizeDelta = new Vector2(22f, 0); }
                if (i == 3) { brt.anchorMin = new Vector2(1, 0); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(1f, 0.5f); brt.sizeDelta = new Vector2(22f, 0); }
            }

            // 2. Bảng Cảnh báo Truy Sát Cyberpunk ở phía trên màn hình
            var alertCard = CreateHudImage("WantedAlertBanner", _combatHudRoot.transform, new Color(0.05f, 0.07f, 0.12f, 0.92f));
            _alertBannerBg = alertCard;
            var alertRt = alertCard.rectTransform;
            alertRt.anchorMin = new Vector2(0.5f, 1f);
            alertRt.anchorMax = new Vector2(0.5f, 1f);
            alertRt.pivot = new Vector2(0.5f, 1f);
            alertRt.anchoredPosition = new Vector2(0f, -20f);
            alertRt.sizeDelta = new Vector2(540f, 64f);

            var alertTopTrim = CreateHudImage("TopTrim", alertCard.transform, new Color(1f, 0.18f, 0.24f, 1f));
            alertTopTrim.rectTransform.anchorMin = new Vector2(0f, 1f);
            alertTopTrim.rectTransform.anchorMax = new Vector2(1f, 1f);
            alertTopTrim.rectTransform.pivot = new Vector2(0.5f, 1f);
            alertTopTrim.rectTransform.sizeDelta = new Vector2(0f, 3.5f);

            var alertBottomTrim = CreateHudImage("BottomTrim", alertCard.transform, new Color(1f, 0.18f, 0.24f, 0.7f));
            alertBottomTrim.rectTransform.anchorMin = new Vector2(0.08f, 0f);
            alertBottomTrim.rectTransform.anchorMax = new Vector2(0.92f, 0f);
            alertBottomTrim.rectTransform.pivot = new Vector2(0.5f, 0f);
            alertBottomTrim.rectTransform.sizeDelta = new Vector2(0f, 2f);

            var alertTitle = CreateHudText("AlertTitle", alertCard.transform, "[ ! ] BÁO ĐỘNG AN NINH - VÙNG ĐẤT PROJECT [ ! ]", 17f, new Color(1f, 0.28f, 0.32f, 1f), TextAlignmentOptions.Center);
            alertTitle.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            alertTitle.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            alertTitle.rectTransform.offsetMin = Vector2.zero;
            alertTitle.rectTransform.offsetMax = Vector2.zero;

            _alertSubText = CreateHudText("AlertSub", alertCard.transform, "HẠM ĐỘI ĐANG VÂY BẮT  |  RỜI KHỎI VÙNG ĐẤT PROJECT ĐỂ CẮT ĐUÔI", 12.5f, new Color(0.85f, 0.93f, 1f, 0.9f), TextAlignmentOptions.Center);
            _alertSubText.rectTransform.anchorMin = new Vector2(0f, 0.06f);
            _alertSubText.rectTransform.anchorMax = new Vector2(1f, 0.48f);
            _alertSubText.rectTransform.offsetMin = Vector2.zero;
            _alertSubText.rectTransform.offsetMax = Vector2.zero;

            // 3. Thanh Máu Sci-Fi 12 Ô Năng Lượng ở góc dưới trái (Hull / Shield HUD Card)
            var shieldCard = CreateHudImage("ShipShieldCard", _combatHudRoot.transform, new Color(0.03f, 0.07f, 0.13f, 0.92f));
            var cardRt = shieldCard.rectTransform;
            cardRt.anchorMin = new Vector2(0f, 0f);
            cardRt.anchorMax = new Vector2(0f, 0f);
            cardRt.pivot = new Vector2(0f, 0f);
            cardRt.anchoredPosition = new Vector2(28f, 28f);
            cardRt.sizeDelta = new Vector2(360f, 82f);

            // Thanh neon trang trí cạnh trái & viền trên
            _hudOuterBorder = CreateHudImage("LeftAccentBar", shieldCard.transform, new Color(0.15f, 0.95f, 1f, 1f));
            _hudOuterBorder.rectTransform.anchorMin = new Vector2(0f, 0f);
            _hudOuterBorder.rectTransform.anchorMax = new Vector2(0f, 1f);
            _hudOuterBorder.rectTransform.pivot = new Vector2(0f, 0.5f);
            _hudOuterBorder.rectTransform.sizeDelta = new Vector2(5f, 0f);

            var topLine = CreateHudImage("TopLine", shieldCard.transform, new Color(0.15f, 0.95f, 1f, 0.45f));
            topLine.rectTransform.anchorMin = new Vector2(0f, 1f);
            topLine.rectTransform.anchorMax = new Vector2(1f, 1f);
            topLine.rectTransform.pivot = new Vector2(0.5f, 1f);
            topLine.rectTransform.sizeDelta = new Vector2(0f, 2f);

            _healthStatusText = CreateHudText("StatusLabel", shieldCard.transform, "HULL & SHIELD INTEGRITY", 13.5f, new Color(0.55f, 0.92f, 1f, 0.95f), TextAlignmentOptions.MidlineLeft);
            _healthStatusText.rectTransform.anchorMin = new Vector2(0f, 0.54f);
            _healthStatusText.rectTransform.anchorMax = new Vector2(0.72f, 0.94f);
            _healthStatusText.rectTransform.offsetMin = new Vector2(18f, 0f);
            _healthStatusText.rectTransform.offsetMax = Vector2.zero;

            _healthValueText = CreateHudText("PercentValue", shieldCard.transform, "100%", 21f, new Color(0.2f, 0.98f, 1f, 1f), TextAlignmentOptions.MidlineRight);
            _healthValueText.rectTransform.anchorMin = new Vector2(0.68f, 0.52f);
            _healthValueText.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            _healthValueText.rectTransform.offsetMin = Vector2.zero;
            _healthValueText.rectTransform.offsetMax = new Vector2(-16f, 0f);

            // Rãnh chứa thanh máu
            var barTrack = CreateHudImage("BarTrack", shieldCard.transform, new Color(0.08f, 0.13f, 0.2f, 0.95f));
            var trackRt = barTrack.rectTransform;
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(1f, 0f);
            trackRt.pivot = new Vector2(0.5f, 0f);
            trackRt.offsetMin = new Vector2(18f, 14f);
            trackRt.offsetMax = new Vector2(-16f, 38f);

            // Thanh tụt máu trễ màu cam/đỏ phía dưới (Damage Lag Fill)
            _damageLagBar = CreateHudImage("DamageLagFill", barTrack.transform, new Color(1f, 0.35f, 0.15f, 0.55f));
            _damageLagBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            _damageLagBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            _damageLagBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            _damageLagBar.rectTransform.offsetMin = new Vector2(2f, 2f);
            _damageLagBar.rectTransform.offsetMax = new Vector2(-2f, -2f);

            // 12 ô năng lượng phát sáng (Segmented Energy Cells)
            int cellCount = 12;
            _healthCells = new Image[cellCount];
            float gap = 0.008f;
            float cellW = (1f - gap * (cellCount + 1)) / cellCount;
            for (int i = 0; i < cellCount; i++)
            {
                float xMin = gap + i * (cellW + gap);
                float xMax = xMin + cellW;
                var cell = CreateHudImage("Cell_" + i, barTrack.transform, new Color(0.15f, 0.95f, 1f, 1f));
                cell.rectTransform.anchorMin = new Vector2(xMin, 0.12f);
                cell.rectTransform.anchorMax = new Vector2(xMax, 0.88f);
                cell.rectTransform.offsetMin = Vector2.zero;
                cell.rectTransform.offsetMax = Vector2.zero;
                _healthCells[i] = cell;
            }
        }

        private static Image CreateHudImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text CreateHudText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void UpdateCombatHUD()
        {
            EnsureCombatHUD();
            if (_combatHudRoot == null) return;

            bool showHud = SpaceTrafficShipAI.IsFleetHostile && !AboutInfoStation.AnyStationActive;
            if (_combatHudRoot.activeSelf != showHud)
            {
                _combatHudRoot.SetActive(showHud);
            }
            if (!showHud) return;

            float ratio = HealthRatio;
            _displayedHealthRatio = Mathf.Lerp(_displayedHealthRatio, ratio, Time.deltaTime * 4.5f);

            // Màu theo mức máu: Xanh Cyan (>60%) -> Vàng Cam (35%-60%) -> Đỏ Nguy Kịch (<=35%)
            Color themeColor;
            if (ratio > 0.6f)
                themeColor = new Color(0.15f, 0.95f, 1f, 1f);
            else if (ratio > 0.35f)
                themeColor = new Color(1f, 0.72f, 0.15f, 1f);
            else
                themeColor = new Color(1f, 0.16f, 0.22f, 1f);

            if (_hudOuterBorder != null) _hudOuterBorder.color = themeColor;
            if (_healthValueText != null)
            {
                _healthValueText.text = $"{Mathf.CeilToInt(ratio * 100f)}%";
                _healthValueText.color = themeColor;
            }
            if (_healthStatusText != null)
            {
                _healthStatusText.text = ratio <= 0.35f ? "[ ! ] CRITICAL HULL DAMAGE" : "HULL & SHIELD INTEGRITY";
                _healthStatusText.color = ratio <= 0.35f ? themeColor : new Color(0.65f, 0.92f, 1f, 0.95f);
            }

            if (_damageLagBar != null)
            {
                _damageLagBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_displayedHealthRatio), 1f);
            }

            if (_healthCells != null)
            {
                int n = _healthCells.Length;
                for (int i = 0; i < n; i++)
                {
                    float cellStart = (float)i / n;
                    float cellEnd = (float)(i + 1) / n;
                    float fill = Mathf.Clamp01((ratio - cellStart) / (cellEnd - cellStart));
                    if (fill > 0.02f)
                    {
                        _healthCells[i].color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.35f + 0.65f * fill);
                    }
                    else
                    {
                        _healthCells[i].color = new Color(0.15f, 0.22f, 0.3f, 0.25f);
                    }
                }
            }

            // Cập nhật thông báo khoảng cách rút lui khỏi Vùng Đất Project
            if (_alertSubText != null)
            {
                float dist = SpaceTrafficShipAI.GetPlayerDistanceFromProjectBase(transform);
                float remain = Mathf.Max(0f, SpaceTrafficShipAI.ProjectZoneRadius - dist);
                _alertSubText.text = $"HẠM ĐỘI ĐANG VÂY BẮT  |  CÁCH BIÊN GIỚI VÙNG PROJECT: {Mathf.CeilToInt(remain)}M";
            }

            // Nháy đỏ màn hình khi gần hết máu (<= 35%) hoặc vừa trúng đạn
            float hitFlash = Mathf.Clamp01(1f - (Time.time - _lastDamageTime) / 0.25f) * 0.22f;
            float lowHealthFlash = 0f;
            if (ratio <= 0.35f && !_isRespawning)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 11f);
                lowHealthFlash = 0.08f + pulse * 0.28f;
            }
            float redAlpha = Mathf.Max(hitFlash, lowHealthFlash);
            if (_redScreenOverlay != null)
                _redScreenOverlay.color = new Color(0.95f, 0.03f, 0.06f, redAlpha);

            if (_redBorders != null)
            {
                float borderAlpha = Mathf.Clamp01(redAlpha * 2.1f);
                for (int i = 0; i < _redBorders.Length; i++)
                {
                    if (_redBorders[i] != null)
                        _redBorders[i].color = new Color(1f, 0.04f, 0.08f, borderAlpha);
                }
            }
        }
    }
}
