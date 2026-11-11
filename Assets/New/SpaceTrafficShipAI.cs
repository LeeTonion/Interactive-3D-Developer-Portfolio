using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// AI điều khiển đội tàu bảo vệ Vùng Đất Project (Project Zone Guardian & Tactical Fighter AI):
    /// - Gắn liền với Vùng Đất Project (ProjectBaseCenter, bán kính ProjectZoneRadius = 115m).
    /// - Nếu người chơi bay ra xa khỏi Vùng Đất Project (> 115m), lệnh cảnh báo truy sát tự động tắt ngay lập tức.
    /// - Di chuyển chiến thuật thông minh (Tactical Flanking & Orbit):
    ///   + Không bay túm tụm hay đâm đuôi người chơi.
    ///   + Mỗi tàu giữ 1 góc vây quanh riêng biệt (8 hướng), lượn vòng chiến thuật ở cự ly 34m - 52m,
    ///     tự giữ khoảng cách với các tàu đồng đội (Fleet Separation), thay phiên nhau bổ nhào bắn đón đầu rồi lượn tách ra.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceTrafficShipAI : MonoBehaviour
    {
        public static bool IsFleetHostile { get; private set; }
        public static float HostileTimeRemaining { get; private set; }

        [Tooltip("Tâm của Vùng Đất Project (AboutIsland)")]
        public static Vector3 ProjectBaseCenter = new Vector3(0f, 10f, 15f);

        [Tooltip("Khoảng cách tối đa tính từ Căn cứ Project — ra khỏi vùng này sẽ hết cảnh báo truy sát")]
        public static float ProjectZoneRadius = 270f;   

        public static readonly List<SpaceTrafficShipAI> ActiveShips = new List<SpaceTrafficShipAI>();

        [Header("Thông Số Bay Tuần Tra & Chiến Thuật")]
        public int tacticalSlotIndex = 0;
        public float cruiseSpeed = 18f;
        public float hostileSpeedMultiplier = 1.4f;
        public float turnSpeed = 2.6f;
        public float hostileTurnSpeed = 4.2f;
        public float maxBankAngle = 34f;
        public float waypointReachDistance = 12f;

        [Header("Máu & Chiến Đấu (Combat & Aggro)")]
        public bool isReinforcementShip = false;
        public float maxHealth = 105f;
        public float fireInterval = 1.45f;
        public float attackRange = 115f;
        public float laserDamage = 12f;
        public float respawnDelay = 20f;

        [Header("Giữ Khoảng Cách An Toàn Với Tàu Ta (Player Standoff)")]
        [Tooltip("Khoảng cách tối thiểu tàu địch luôn giữ với tàu người chơi (m) - không bao giờ áp sát hay đứng kề bên")]
        public float minPlayerStandoffDistance = 52f;
        [Tooltip("Bán kính lượn vòng chiến đấu tiêu chuẩn quanh tàu người chơi (m)")]
        public float preferredCombatRadius = 78f;

        [Header("Quỹ Đạo & Điểm Tuần Tra (Patrol Route)")]
        public Vector3[] patrolPoints;
        public float orbitRadius = 65f;
        public float verticalWaveAmplitude = 6f;

        [Header("Né Tránh Vật Cản (Obstacle Avoidance)")]
        public float avoidanceDistance = 22f;
        public float avoidanceSphereRadius = 3f;
        public float avoidanceStrength = 5f;

        [Header("Hiệu Ứng Động Cơ (Thruster Light & Trail)")]
        public Light thrusterLight;
        public TrailRenderer thrusterTrail;

        private float _currentHealth;
        private bool _isDestroyed;
        private Rigidbody _rb;
        private Collider[] _colliders;
        private Renderer[] _renderers;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Vector3 _initialScale;

        public bool IsDestroyed => _isDestroyed;
        public float CurrentHealth => _currentHealth;
        public Vector3 InitialScale => _initialScale.sqrMagnitude > 0.01f ? _initialScale : Vector3.one * 2.2f;

        private int _currentIndex;
        private float _currentBank;
        private float _baseLightIntensity = 3.5f;
        private float _timeOffset;
        private float _nextFireTime;
        private PlayerSpaceshipController _player;

        private static readonly Color PeacefulColor = new Color(0.2f, 0.9f, 1f, 1f);
        private static readonly Color HostileColor = new Color(1f, 0.16f, 0.12f, 1f);

        public static void TriggerFleetHostileAlert(float duration = 60f)
        {
            IsFleetHostile = true;
            HostileTimeRemaining = Mathf.Max(HostileTimeRemaining, duration);
        }

        public static void ClearFleetHostileAlert()
        {
            IsFleetHostile = false;
            HostileTimeRemaining = 0f;

            // Thu hồi các tàu tiếp viện sinh thêm khi hết lệnh báo động truy sát
            for (int i = ActiveShips.Count - 1; i >= 0; i--)
            {
                var ship = ActiveShips[i];
                if (ship != null && ship.isReinforcementShip)
                {
                    ActiveShips.RemoveAt(i);
                    Destroy(ship.gameObject);
                }
            }
        }

        public static float GetPlayerDistanceFromProjectBase(Transform playerTransform)
        {
            if (playerTransform == null) return 0f;
            Vector2 p = new Vector2(playerTransform.position.x, playerTransform.position.z);
            Vector2 c = new Vector2(ProjectBaseCenter.x, ProjectBaseCenter.z);
            return Vector2.Distance(p, c);
        }

        private void OnEnable()
        {
            if (!ActiveShips.Contains(this))
            {
                tacticalSlotIndex = ActiveShips.Count;
                ActiveShips.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveShips.Remove(this);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _initialScale = transform.localScale;
            _currentHealth = maxHealth;

            var meshCol = GetComponent<MeshCollider>();
            if (meshCol == null) meshCol = gameObject.AddComponent<MeshCollider>();
            if (meshCol.sharedMesh == null)
            {
                var mf = GetComponentInChildren<MeshFilter>();
                if (mf != null) meshCol.sharedMesh = mf.sharedMesh;
            }
            meshCol.convex = true;

            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);

            _rb.useGravity = false;
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.linearDamping = 1.2f;
            _rb.angularDamping = 4f;
            _rb.mass = 15f;

            if (thrusterLight == null)
                thrusterLight = GetComponentInChildren<Light>();
            if (thrusterLight != null)
                _baseLightIntensity = thrusterLight.intensity;
            if (thrusterTrail == null)
                thrusterTrail = GetComponentInChildren<TrailRenderer>();
        }

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerSpaceshipController>();
            attackRange = Mathf.Max(attackRange, 115f);
            minPlayerStandoffDistance = Mathf.Max(minPlayerStandoffDistance, 48f);
            preferredCombatRadius = Mathf.Max(preferredCombatRadius, minPlayerStandoffDistance + 22f);
            _timeOffset = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.53f) + tacticalSlotIndex * 1.9f;
            _nextFireTime = Time.time + Random.Range(0.5f, 1.8f);

            var station = FindFirstObjectByType<AboutInfoStation>();
            if (station != null)
            {
                ProjectBaseCenter = station.transform.position;
            }

            if (patrolPoints == null || patrolPoints.Length < 3)
            {
                GenerateOrbitRoute();
            }

            float bestDist = float.MaxValue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                float d = Vector3.Distance(transform.position, patrolPoints[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    _currentIndex = (i + 1) % patrolPoints.Length;
                }
            }
        }

        private void GenerateOrbitRoute()
        {
            Vector3 startPos = transform.position;
            float baseY = startPos.y;
            float startAngle = Mathf.Atan2(startPos.z - ProjectBaseCenter.z, startPos.x - ProjectBaseCenter.x);
            float radius = Mathf.Clamp(new Vector2(startPos.x - ProjectBaseCenter.x, startPos.z - ProjectBaseCenter.z).magnitude, 42f, 88f);
            orbitRadius = radius;

            float dir = (tacticalSlotIndex % 2 == 0) ? 1f : -1f;

            int count = 6;
            patrolPoints = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + dir * (i * Mathf.PI * 2f / count);
                float r = radius * (i % 2 == 0 ? 1.05f : 0.86f);
                float yOffset = Mathf.Sin(i * 1.7f + _timeOffset) * verticalWaveAmplitude;
                patrolPoints[i] = new Vector3(
                    ProjectBaseCenter.x + Mathf.Cos(angle) * r,
                    baseY + yOffset,
                    ProjectBaseCenter.z + Mathf.Sin(angle) * r
                );
            }
        }

        private void FixedUpdate()
        {
            if (_isDestroyed || _rb == null || _rb.isKinematic) return;
            if (_player == null) _player = FindFirstObjectByType<PlayerSpaceshipController>();

            // Kiểm tra nếu người chơi đã bay ra xa khỏi Vùng Đất Project -> Tắt cảnh báo truy sát ngay
            if (IsFleetHostile && _player != null)
            {
                float distFromBase = GetPlayerDistanceFromProjectBase(_player.transform);
                if (distFromBase > ProjectZoneRadius)
                {
                    ClearFleetHostileAlert();
                }
            }

            bool hunting = IsFleetHostile && _player != null && !AboutInfoStation.AnyStationActive;
            Vector3 targetPos;

            if (hunting)
            {
                targetPos = ComputeSmartTacticalPosition();
            }
            else
            {
                if (patrolPoints == null || patrolPoints.Length == 0) return;
                targetPos = patrolPoints[_currentIndex];
                bool nearWaypoint = Vector3.Distance(transform.position, targetPos) < waypointReachDistance;
                bool playerOccupyingWaypoint = _player != null && Vector3.Distance(_player.transform.position, targetPos) < minPlayerStandoffDistance;
                if (nearWaypoint || playerOccupyingWaypoint)
                {
                    _currentIndex = (_currentIndex + 1) % patrolPoints.Length;
                    targetPos = patrolPoints[_currentIndex];
                }
            }

            Vector3 desiredDir = (targetPos - transform.position).normalized;

            // Lực tách bầy (Fleet Separation) để các tàu địch không bao giờ bay chồng hay túm tụm vào nhau
            Vector3 separation = ComputeFleetSeparation();
            if (separation.sqrMagnitude > 0.001f)
            {
                desiredDir = (desiredDir + separation * 0.65f).normalized;
            }

            // Lực giữ khoảng cách an toàn với tàu người chơi (áp dụng cả khi tuần tra lẫn khi truy sát):
            // Ngăn tàu địch đứng yên hoặc bay lượn quá sát tàu ta (< minPlayerStandoffDistance)
            float distToPlayer = float.MaxValue;
            Vector3 radialFromPlayer = Vector3.zero;
            if (_player != null)
            {
                Vector3 fromPlayer = transform.position - _player.transform.position;
                distToPlayer = fromPlayer.magnitude;
                radialFromPlayer = (distToPlayer > 0.05f) ? (fromPlayer / distToPlayer) : transform.forward;

                float softBuffer = minPlayerStandoffDistance + 16f;
                if (distToPlayer < softBuffer)
                {
                    float urgency = 1f - Mathf.Clamp01((distToPlayer - minPlayerStandoffDistance * 0.7f) / (softBuffer - minPlayerStandoffDistance * 0.7f));
                    float dirSign = (tacticalSlotIndex % 2 == 0) ? 1f : -1f;
                    Vector3 tangent = Vector3.Cross(Vector3.up, radialFromPlayer);
                    if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(Vector3.right, radialFromPlayer);
                    tangent = tangent.normalized * dirSign;

                    if (distToPlayer < minPlayerStandoffDistance)
                    {
                        // Bẻ lái dứt khoát tách ra xa khỏi tàu người chơi
                        desiredDir = (radialFromPlayer * (1.8f + urgency * 1.8f) + tangent * 0.85f + Vector3.up * 0.25f).normalized;
                    }
                    else
                    {
                        desiredDir = (desiredDir + (radialFromPlayer * 1.4f + tangent * 0.65f) * urgency).normalized;
                    }
                }
            }

            // Cảm biến né tránh đảo nổi, thiên thạch và chính tàu người chơi
            if (Physics.SphereCast(transform.position, avoidanceSphereRadius, transform.forward,
                out RaycastHit hit, avoidanceDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.rigidbody != _rb)
                {
                    Vector3 avoidDir = (hit.normal + Vector3.up * 0.45f).normalized;
                    float urgency = 1f - Mathf.Clamp01(hit.distance / avoidanceDistance);
                    desiredDir = (desiredDir + avoidDir * (avoidanceStrength * urgency)).normalized;
                }
            }

            float activeTurnSpeed = hunting ? hostileTurnSpeed : turnSpeed;
            if (distToPlayer < minPlayerStandoffDistance)
            {
                activeTurnSpeed *= 1.6f; // Xoay mũi tách ra nhanh hơn nếu lỡ tiến sát
            }

            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Vector3 localDir = transform.InverseTransformDirection(desiredDir);
                float targetBank = Mathf.Clamp(-localDir.x * maxBankAngle * 1.8f, -maxBankAngle, maxBankAngle);
                _currentBank = Mathf.Lerp(_currentBank, targetBank, Time.fixedDeltaTime * 4.5f);

                Quaternion lookRot = Quaternion.LookRotation(desiredDir, Vector3.up);
                Quaternion bankedRot = lookRot * Quaternion.Euler(0f, 0f, _currentBank);
                Quaternion nextRot = Quaternion.Slerp(_rb.rotation, bankedRot, Time.fixedDeltaTime * activeTurnSpeed);

                _rb.MoveRotation(nextRot);
            }

            float activeSpeed = hunting ? (cruiseSpeed * hostileSpeedMultiplier) : cruiseSpeed;
            Vector3 targetVel = transform.forward * activeSpeed;
            if (_player != null && distToPlayer < minPlayerStandoffDistance)
            {
                // Triệt tiêu thành phần vận tốc lao về phía tàu ta và đẩy tách ra giữ cự ly an toàn
                float pushBoost = Mathf.Clamp((minPlayerStandoffDistance - distToPlayer) * 1.35f, 8f, 34f);
                float towardPlayerSpeed = Vector3.Dot(targetVel, -radialFromPlayer);
                if (towardPlayerSpeed > 0f)
                {
                    targetVel += radialFromPlayer * towardPlayerSpeed;
                }
                targetVel += radialFromPlayer * pushBoost;
            }

            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVel, Time.fixedDeltaTime * 4.5f);
            _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        }

        /// <summary>
        /// Tính toán vị trí bay chiến thuật thông minh (Tactical Flanking & Standoff Orbit):
        /// - Giữ khoảng cách tối thiểu >= minPlayerStandoffDistance (52m), bay lượn ở vòng 78m - 102m.
        /// - Luân phiên cắt góc bắn từ xa rồi lượn vòng rộng, không bao giờ lao sát hay đỗ kề tàu người chơi.
        /// </summary>
        private Vector3 ComputeSmartTacticalPosition()
        {
            Vector3 playerPos = _player.transform.position;
            Rigidbody playerRb = _player.GetComponent<Rigidbody>();
            Vector3 playerVel = playerRb != null ? playerRb.linearVelocity : Vector3.zero;

            // Chu kỳ chiến thuật riêng cho từng tàu: chủ yếu lượn vòng giữ cự ly, xen kẽ đợt cắt mặt ở khoảng cách an toàn
            float phase = Mathf.Sin(Time.time * 0.65f + tacticalSlotIndex * 1.3f);
            bool inAttackPass = phase > 0.35f;

            float orbitDir = (tacticalSlotIndex % 2 == 0) ? 1f : -1f;
            float baseAngle = (tacticalSlotIndex * (Mathf.PI * 2f / Mathf.Max(1, ActiveShips.Count)))
                              + Time.time * 0.55f * orbitDir;

            float distToPlayer = Vector3.Distance(transform.position, playerPos);

            Vector3 tacticalTarget;
            if (inAttackPass && distToPlayer > (minPlayerStandoffDistance + 18f))
            {
                // Bay cắt chéo ở cự ly an toàn (>= 66m) bên sườn, tuyệt đối không áp sát
                float passDist = minPlayerStandoffDistance + 16f;
                Vector3 sideOffset = new Vector3(Mathf.Cos(baseAngle), 0.18f, Mathf.Sin(baseAngle)) * passDist;
                tacticalTarget = playerPos + playerVel * 0.35f + sideOffset;
            }
            else
            {
                // Lượn vòng vây quanh người chơi ở bán kính 78m - 102m và biến thiên cao độ
                float ringRadius = preferredCombatRadius + (tacticalSlotIndex % 3) * 12f;
                float heightWave = 8f + Mathf.Sin(Time.time * 1.2f + _timeOffset) * 10f;
                tacticalTarget = new Vector3(
                    playerPos.x + Mathf.Cos(baseAngle) * ringRadius,
                    playerPos.y + heightWave,
                    playerPos.z + Mathf.Sin(baseAngle) * ringRadius
                );
            }

            // Đảm bảo điểm mục tiêu luôn cách tàu người chơi ít nhất minPlayerStandoffDistance
            Vector3 toTarget = tacticalTarget - playerPos;
            if (toTarget.magnitude < minPlayerStandoffDistance)
            {
                Vector3 dir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : new Vector3(Mathf.Cos(baseAngle), 0.2f, Mathf.Sin(baseAngle)).normalized;
                tacticalTarget = playerPos + dir * minPlayerStandoffDistance;
            }

            // Giữ các tàu luôn nằm trong phạm vi Vùng Đất Project
            Vector2 flatOffset = new Vector2(tacticalTarget.x - ProjectBaseCenter.x, tacticalTarget.z - ProjectBaseCenter.z);
            if (flatOffset.magnitude > ProjectZoneRadius)
            {
                flatOffset = flatOffset.normalized * ProjectZoneRadius;
                tacticalTarget.x = ProjectBaseCenter.x + flatOffset.x;
                tacticalTarget.z = ProjectBaseCenter.z + flatOffset.y;
            }

            return tacticalTarget;
        }

        private Vector3 ComputeFleetSeparation()
        {
            Vector3 push = Vector3.zero;
            float sepRadius = 20f;
            for (int i = 0; i < ActiveShips.Count; i++)
            {
                var other = ActiveShips[i];
                if (other == null || other == this || other._isDestroyed) continue;
                Vector3 diff = transform.position - other.transform.position;
                float d = diff.magnitude;
                if (d < sepRadius && d > 0.01f)
                {
                    push += diff.normalized * ((sepRadius - d) / sepRadius);
                }
            }
            return push;
        }

        private void Update()
        {
            if (_isDestroyed) return;

            if (IsFleetHostile)
            {
                HostileTimeRemaining -= Time.deltaTime * 0.12f;
                if (HostileTimeRemaining <= 0f) ClearFleetHostileAlert();
            }

            Color activeColor = IsFleetHostile ? HostileColor : PeacefulColor;
            if (thrusterLight != null)
            {
                float pulse = 0.85f + 0.3f * Mathf.Sin((Time.time + _timeOffset) * (IsFleetHostile ? 22f : 14f));
                thrusterLight.intensity = _baseLightIntensity * (IsFleetHostile ? 1.5f : 1f) * pulse;
                thrusterLight.color = activeColor;
            }
            if (thrusterTrail != null)
            {
                thrusterTrail.sharedMaterial = SpaceExplosionVFX.GetEmissiveMaterial(!IsFleetHostile);
            }

            // Bắn đạn Laser đỏ đón đầu người chơi khi nằm trong góc bắn hợp lệ
            if (IsFleetHostile && _player != null && !AboutInfoStation.AnyStationActive && Time.time >= _nextFireTime)
            {
                Vector3 toPlayer = _player.transform.position - transform.position;
                float dist = toPlayer.magnitude;
                if (dist <= attackRange && dist > 14f)
                {
                    float angle = Vector3.Angle(transform.forward, toPlayer);
                    if (angle < 42f)
                    {
                        _nextFireTime = Time.time + fireInterval + Random.Range(-0.15f, 0.4f);
                        Rigidbody playerRb = _player.GetComponent<Rigidbody>();
                        Vector3 leadPos = _player.transform.position + (playerRb != null ? playerRb.linearVelocity * (dist / 95f) : Vector3.zero);
                        Vector3 muzzlePos = transform.position + transform.forward * 3.8f;
                        Vector3 aimDir = (leadPos - muzzlePos).normalized;
                        SpaceLaserProjectile.SpawnBolt(muzzlePos, aimDir, false, gameObject, 95f, laserDamage);
                    }
                }
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint, bool fromPlayer)
        {
            if (_isDestroyed) return;

            if (fromPlayer)
            {
                TriggerFleetHostileAlert(60f);
            }

            _currentHealth -= damage;
            if (_currentHealth <= 0f)
            {
                ExplodeShip();
            }
        }

        public void InitializeAsReinforcement(Vector3 targetScale, Vector3 launchVelocity)
        {
            isReinforcementShip = true;
            _isDestroyed = false;
            _currentHealth = maxHealth;
            _initialScale = targetScale;

            SetShipVisible(true);
            transform.localScale = targetScale;

            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = launchVelocity;
                _rb.angularVelocity = Vector3.zero;
            }
            if (thrusterTrail != null) thrusterTrail.Clear();

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                GenerateOrbitRoute();
            }
        }

        private IEnumerator WarpInScaleRoutine(Vector3 targetScale)
        {
            transform.localScale = targetScale * 0.15f;
            float t = 0f;
            while (t < 0.45f)
            {
                if (_isDestroyed) yield break;
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(targetScale * 0.15f, targetScale, t / 0.45f);
                yield return null;
            }
            if (!_isDestroyed) transform.localScale = targetScale;
        }

        private void ExplodeShip()
        {
            _isDestroyed = true;
            if (SpaceshipTargetLockSystem.Instance != null)
            {
                SpaceshipTargetLockSystem.Instance.ClearLockIfTarget(transform);
            }
            Material mat = (_renderers != null && _renderers.Length > 0) ? _renderers[0].sharedMaterial : null;
            SpaceExplosionVFX.SpawnExplosion(transform.position, 4.2f, false, mat);

            if (isReinforcementShip)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            SetShipVisible(false);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }
            if (thrusterTrail != null) thrusterTrail.Clear();

            yield return new WaitForSeconds(respawnDelay);

            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            transform.localScale = Vector3.zero;
            _currentHealth = maxHealth;

            if (_rb != null) _rb.isKinematic = false;
            SetShipVisible(true);
            if (thrusterTrail != null) thrusterTrail.Clear();

            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(Vector3.zero, _initialScale, t / 0.6f);
                yield return null;
            }
            transform.localScale = _initialScale;
            _isDestroyed = false;
        }

        private void SetShipVisible(bool visible)
        {
            if (_renderers != null)
                foreach (var r in _renderers) if (r != null) r.enabled = visible;
            if (_colliders != null)
                foreach (var c in _colliders) if (c != null) c.enabled = visible;
            if (thrusterLight != null)
                thrusterLight.enabled = visible;
        }
    }
}
