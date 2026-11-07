using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Căn cứ sản xuất & phóng tàu chiến kẻ địch (06_Enemy_Defense_Spawner):
    /// - Có 2 chỗ spawn: Bệ Trái (Pad 0) & Bệ Phải (Pad 1).
    /// - Mỗi lượt xuất xưởng ĐÚNG 2 TÀU (mỗi bên 1 tàu, không bị spawn trùng 4 tàu).
    /// - CHỈ tạo tàu khi hạm đội đang ở trạng thái BỊ TẤN CÔNG (Hostile).
    /// - Quá trình chế tạo 2 tàu kéo dài 5 giây trên 2 bệ (in 3D / tia lửa hàn điện quang).
    /// - NẾU ĐANG TẠO DỞ MÀ HẾT BỊ TẤN CÔNG (người chơi đáp xuống Project):
    ///   -> Tiếp tục tạo nốt cho đủ 5 giây rồi CHO 2 TÀU ĐẬU LẠI NGAY TRÊN 2 BỆ (Standby Parked Ships).
    ///   -> Khi nào bị tấn công tiếp, 2 chiếc tàu đậu sẵn này cất cánh ngay lập tức nghênh chiến!
    /// - Khi lõi năng lượng bị phá hủy: Toàn bộ căn cứ (Hangar, 04_Defense_Turrets_And_Squadron, bệ phóng, tháp pháo, radar, nền đá)
    ///   NỔ TUNG LIÊN HOÀN, mảnh vỡ vật lý 3D bay tung tóe khắp vũ trụ!
    /// </summary>
    public class ProjectZoneEnemySpawner : MonoBehaviour
    {
        [Header("Cấu Hình Sản Xuất Tàu (Ship Construction & Launch)")]
        [Tooltip("Khoảng cách thời gian giữa các đợt bắt đầu chế tạo (4 giây)")]
        public float buildCooldown = 4.0f;

        [Tooltip("Thời gian quá trình chế tạo 2 tàu trên 2 bệ (5 giây)")]
        public float constructionDuration = 5.0f;

        [Tooltip("Số lượng tàu tiếp viện tối đa cùng lúc để đảm bảo hiệu năng")]
        public int maxReinforcementShips = 18;

        [Tooltip("Tốc độ phóng ban đầu khi tàu cất cánh")]
        public float launchSpeed = 28f;

        [Header("Vị Trí 2 Bệ Phóng & Tàu Mẫu (Wing Pads)")]
        public Transform leftPadAnchor;
        public Transform rightPadAnchor;
        public Transform shipsContainer;

        [Header("Hiệu Ứng Báo Động & Đèn (Visuals)")]
        public Transform outerWarpRing;
        public Transform midWarpRing;
        public Transform innerWarpRing;
        public Renderer portalCoreRenderer;
        public Light portalLight;
        public Light leftBeaconLight;
        public Light rightBeaconLight;
        public TextMeshPro statusText;
        public Renderer statusBorderRenderer;
        public Renderer statusArrowRenderer;

        private float _cooldownTimer;
        private bool _isConstructing;
        private bool _isExploding;
        private int _totalSpawnCount;

        private readonly List<SpaceTrafficShipAI> _templatePrefabs = new List<SpaceTrafficShipAI>();
        private readonly List<SpaceTrafficShipAI> _activeReinforcements = new List<SpaceTrafficShipAI>();

        // Tàu đang đỗ sẵn trên 2 bệ (nếu lần trước tạo dở khi hết bị tấn công)
        private GameObject _parkedShipLeft;
        private GameObject _parkedShipRight;

        // 2 Tàu đang trong quá trình chế tạo (mỗi bên 1 tàu)
        private GameObject _buildingShipLeft;
        private GameObject _buildingShipRight;

        private Material _portalRuntimeMat;
        private Material _borderRuntimeMat;
        private Material _arrowRuntimeMat;

        private static readonly Color StandbyGateColor = new Color(0.15f, 0.85f, 1f, 1f);
        private static readonly Color AlertGateColor = new Color(1f, 0.16f, 0.1f, 1f);

        private void Start()
        {
            CachePadAnchors();
            RefreshShipTemplates();
            EnsureTurretsAndCore();
            SpawnInitialParkedShips();

            if (portalCoreRenderer != null && portalCoreRenderer.sharedMaterial != null)
            {
                _portalRuntimeMat = new Material(portalCoreRenderer.sharedMaterial);
                portalCoreRenderer.sharedMaterial = _portalRuntimeMat;
            }
            if (statusBorderRenderer != null && statusBorderRenderer.sharedMaterial != null)
            {
                _borderRuntimeMat = new Material(statusBorderRenderer.sharedMaterial);
                statusBorderRenderer.sharedMaterial = _borderRuntimeMat;
            }
            if (statusArrowRenderer != null && statusArrowRenderer.sharedMaterial != null)
            {
                _arrowRuntimeMat = new Material(statusArrowRenderer.sharedMaterial);
                statusArrowRenderer.sharedMaterial = _arrowRuntimeMat;
            }
        }

        /// <summary>
        /// Khởi tạo 2 chiếc tàu thật đậu sẵn trên 2 bệ (Wing Decks) ngay từ đầu game
        /// </summary>
        public void SpawnInitialParkedShips()
        {
            RefreshShipTemplates();
            if (_templatePrefabs.Count == 0) return;

            Transform parentContainer = shipsContainer != null ? shipsContainer : transform.parent;

            // 1. Tàu đậu bệ Trái
            if (_parkedShipLeft == null)
            {
                SpaceTrafficShipAI templateLeft = _templatePrefabs[0];
                Vector3 leftPos = GetPadPosition(0);
                Quaternion leftRot = GetPadRotation(0);
                _parkedShipLeft = Instantiate(templateLeft.gameObject, leftPos, leftRot, parentContainer);
                _parkedShipLeft.name = "Carrier_Docked_Ship_Left";
                _parkedShipLeft.transform.localScale = templateLeft.InitialScale;
                _parkedShipLeft.SetActive(true);

                var ai = _parkedShipLeft.GetComponent<SpaceTrafficShipAI>();
                var rb = _parkedShipLeft.GetComponent<Rigidbody>();
                if (ai != null) ai.enabled = false;
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }

            // 2. Tàu đậu bệ Phải
            if (_parkedShipRight == null)
            {
                SpaceTrafficShipAI templateRight = _templatePrefabs.Count > 1 ? _templatePrefabs[1] : _templatePrefabs[0];
                Vector3 rightPos = GetPadPosition(1);
                Quaternion rightRot = GetPadRotation(1);
                _parkedShipRight = Instantiate(templateRight.gameObject, rightPos, rightRot, parentContainer);
                _parkedShipRight.name = "Carrier_Docked_Ship_Right";
                _parkedShipRight.transform.localScale = templateRight.InitialScale;
                _parkedShipRight.SetActive(true);

                var ai = _parkedShipRight.GetComponent<SpaceTrafficShipAI>();
                var rb = _parkedShipRight.GetComponent<Rigidbody>();
                if (ai != null) ai.enabled = false;
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }
        }

        /// <summary>
        /// Đảm bảo đúng 4 tháp pháo đều có AI & Collider bắn trả người chơi, và Lõi Năng Lượng có thể bị bắn nổ
        /// </summary>
        public void EnsureTurretsAndCore()
        {
            string[] turretNames = new string[]
            {
                "Gantry_Defense_Turret_Left",
                "Gantry_Defense_Turret_Right",
                "Runway_Cannon_Left",
                "Runway_Cannon_Right"
            };

            var d4 = transform.Find("04_Defense_Turrets_And_Squadron");
            if (d4 != null)
            {
                foreach (var tName in turretNames)
                {
                    var t = d4.Find(tName);
                    if (t != null)
                    {
                        if (t.GetComponent<CarrierDefenseTurretAI>() == null)
                        {
                            t.gameObject.AddComponent<CarrierDefenseTurretAI>();
                        }
                        if (t.GetComponent<Collider>() == null)
                        {
                            t.gameObject.AddComponent<BoxCollider>();
                        }
                    }
                }
            }

            // Gắn CarrierEnergyCore vào lõi năng lượng
            var coreTr = transform.Find("03_Launch_Catapult_Gate/Warp_Portal_Core_Emitter/Warp_Ring_Inner_Pivot");
            if (coreTr == null) coreTr = transform.Find("03_Launch_Catapult_Gate/Warp_Portal_Core_Emitter");
            if (coreTr != null)
            {
                if (coreTr.GetComponent<CarrierEnergyCore>() == null)
                {
                    var coreComp = coreTr.gameObject.AddComponent<CarrierEnergyCore>();
                    coreComp.parentCarrierSpawner = this;
                }
                if (coreTr.GetComponent<Collider>() == null)
                {
                    coreTr.gameObject.AddComponent<SphereCollider>();
                }
            }
        }

        public void CachePadAnchors()
        {
            if (leftPadAnchor == null)
            {
                var l = transform.Find("02_Military_Hangar_Complex/Carrier_Wing_Deck_Left");
                if (l != null) leftPadAnchor = l;
            }
            if (rightPadAnchor == null)
            {
                var r = transform.Find("02_Military_Hangar_Complex/Carrier_Wing_Deck_Right");
                if (r != null) rightPadAnchor = r;
            }
        }

        public void RefreshShipTemplates()
        {
            if (_templatePrefabs.Count > 0) return;

            if (shipsContainer == null)
            {
                var found = GameObject.Find("04_Traffic_Spaceships");
                if (found != null) shipsContainer = found.transform;
            }

            if (shipsContainer != null)
            {
                var ships = shipsContainer.GetComponentsInChildren<SpaceTrafficShipAI>(true);
                for (int i = 0; i < ships.Length; i++)
                {
                    if (ships[i] != null && !ships[i].isReinforcementShip && !_templatePrefabs.Contains(ships[i]))
                    {
                        _templatePrefabs.Add(ships[i]);
                    }
                }
            }
        }

        public Vector3 GetPadPosition(int padIndex)
        {
            CachePadAnchors();
            if (padIndex == 0 && leftPadAnchor != null)
            {
                return leftPadAnchor.position + Vector3.up * 0.45f;
            }
            if (padIndex == 1 && rightPadAnchor != null)
            {
                return rightPadAnchor.position + Vector3.up * 0.45f;
            }
            Vector3 local = (padIndex == 0) ? new Vector3(-12.92f, 0.05f, -1.0f) : new Vector3(12.51f, 0.05f, -1.0f);
            return transform.TransformPoint(local);
        }

        public Quaternion GetPadRotation(int padIndex)
        {
            return transform.rotation;
        }

        private void Update()
        {
            if (_isExploding) return;

            // Dọn dẹp danh sách tàu tiếp viện đã nổ
            for (int i = _activeReinforcements.Count - 1; i >= 0; i--)
            {
                if (_activeReinforcements[i] == null) _activeReinforcements.RemoveAt(i);
            }

            bool hostile = SpaceTrafficShipAI.IsFleetHostile && !AboutInfoStation.AnyStationActive;

            // CHỈ CHẾ TẠO TÀU KHI ĐANG Ở TRẠNG THÁI BỊ TẤN CÔNG (Hostile):
            if (hostile)
            {
                // 1. Nếu trên 2 bệ đang có tàu đậu sẵn (từ lần trước tạo dở) -> Cho cất cánh nghênh chiến ngay!
                if (_parkedShipLeft != null) LaunchParkedShip(0, ref _parkedShipLeft);
                if (_parkedShipRight != null) LaunchParkedShip(1, ref _parkedShipRight);

                // 2. Chế tạo 1 đợt 2 tàu (mỗi bên 1 tàu) sau mỗi đợt cooldown 4 giây:
                if (!_isConstructing && _activeReinforcements.Count < maxReinforcementShips && _parkedShipLeft == null && _parkedShipRight == null)
                {
                    _cooldownTimer += Time.deltaTime;
                    if (_cooldownTimer >= buildCooldown)
                    {
                        _cooldownTimer = 0f;
                        StartCoroutine(ConstructPairSquadronRoutine());
                    }
                }
            }
            else
            {
                // Khi không bị tấn công: Căn cứ dừng chờ
                _cooldownTimer = 0f;
            }

            UpdateSpawnerVisuals(hostile);
        }

        /// <summary>
        /// Phóng chiếc tàu đang đậu sẵn trên bệ xuất kích nghênh chiến
        /// </summary>
        private void LaunchParkedShip(int padIndex, ref GameObject parkedShip)
        {
            var ship = parkedShip;
            parkedShip = null;
            if (ship == null) return;

            var player = FindFirstObjectByType<PlayerSpaceshipController>();
            Vector3 padPos = GetPadPosition(padIndex);
            Vector3 toPlayer = player != null ? (player.transform.position - padPos).normalized : transform.forward;
            Vector3 launchDir = (toPlayer * 0.75f + Vector3.up * 0.6f + (padIndex == 0 ? -transform.right : transform.right) * 0.3f).normalized;

            ship.transform.rotation = Quaternion.LookRotation(launchDir, Vector3.up);
            var rb = ship.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = launchDir * launchSpeed;
            }
            var ai = ship.GetComponent<SpaceTrafficShipAI>();
            if (ai != null)
            {
                ai.enabled = true;
                ai.InitializeAsReinforcement(ai.InitialScale, launchDir * launchSpeed);
                _activeReinforcements.Add(ai);
            }
            SpaceExplosionVFX.SpawnHitSpark(padPos, false);
        }

        /// <summary>
        /// Quá trình chế tạo 2 TÀU (mỗi bên 1 tàu) kéo dài đúng 5 giây trên 2 bệ.
        /// NẾU ĐANG TẠO DỞ MÀ HẾT BỊ TẤN CÔNG: Vẫn tạo nốt cho đủ 5 giây rồi CHO 2 TÀU ĐẬU LẠI NGAY TRÊN 2 BỆ!
        /// </summary>
        private IEnumerator ConstructPairSquadronRoutine()
        {
            _isConstructing = true;
            RefreshShipTemplates();

            if (_templatePrefabs.Count == 0)
            {
                _isConstructing = false;
                yield break;
            }

            // Chọn 2 mẫu tàu từ danh sách mẫu trong 04_Traffic_Spaceships
            SpaceTrafficShipAI templateLeft = _templatePrefabs[_totalSpawnCount % _templatePrefabs.Count];
            SpaceTrafficShipAI templateRight = _templatePrefabs[(_totalSpawnCount + 1) % _templatePrefabs.Count];
            _totalSpawnCount += 2;

            Vector3 leftPos = GetPadPosition(0);
            Quaternion leftRot = GetPadRotation(0);
            Vector3 rightPos = GetPadPosition(1);
            Quaternion rightRot = GetPadRotation(1);

            Transform parentContainer = shipsContainer != null ? shipsContainer : transform.parent;

            // 1. Tạo ĐÚNG 1 tàu trên Bệ Trái
            _buildingShipLeft = Instantiate(templateLeft.gameObject, leftPos, leftRot, parentContainer);
            _buildingShipLeft.name = $"Factory_Ship_Left_{_totalSpawnCount}";
            _buildingShipLeft.SetActive(true);
            var leftAi = _buildingShipLeft.GetComponent<SpaceTrafficShipAI>();
            var leftRb = _buildingShipLeft.GetComponent<Rigidbody>();
            if (leftAi != null) leftAi.enabled = false;
            if (leftRb != null)
            {
                if (!leftRb.isKinematic) leftRb.linearVelocity = Vector3.zero;
                leftRb.isKinematic = true;
            }

            // 2. Tạo ĐÚNG 1 tàu trên Bệ Phải
            _buildingShipRight = Instantiate(templateRight.gameObject, rightPos, rightRot, parentContainer);
            _buildingShipRight.name = $"Factory_Ship_Right_{_totalSpawnCount}";
            _buildingShipRight.SetActive(true);
            var rightAi = _buildingShipRight.GetComponent<SpaceTrafficShipAI>();
            var rightRb = _buildingShipRight.GetComponent<Rigidbody>();
            if (rightAi != null) rightAi.enabled = false;
            if (rightRb != null)
            {
                if (!rightRb.isKinematic) rightRb.linearVelocity = Vector3.zero;
                rightRb.isKinematic = true;
            }

            Vector3 leftFullScale = templateLeft.InitialScale;
            Vector3 rightFullScale = templateRight.InitialScale;
            _buildingShipLeft.transform.localScale = Vector3.zero;
            _buildingShipRight.transform.localScale = Vector3.zero;

            // QUÁ TRÌNH CHẾ TẠO KÉO DÀI 5 GIÂY (Dù hết bị tấn công giữa chừng vẫn tạo nốt)
            float elapsed = 0f;
            float sparkTimer = 0f;
            while (elapsed < constructionDuration)
            {
                if (_isExploding) yield break;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / constructionDuration);

                if (_buildingShipLeft != null)
                {
                    _buildingShipLeft.transform.localScale = Vector3.Lerp(Vector3.zero, leftFullScale, progress);
                    _buildingShipLeft.transform.position = leftPos + Vector3.up * (Mathf.Sin(progress * Mathf.PI * 0.5f) * 0.35f);
                }
                if (_buildingShipRight != null)
                {
                    _buildingShipRight.transform.localScale = Vector3.Lerp(Vector3.zero, rightFullScale, progress);
                    _buildingShipRight.transform.position = rightPos + Vector3.up * (Mathf.Sin(progress * Mathf.PI * 0.5f) * 0.35f);
                }

                // Bắn tia lửa hàn điện quang
                sparkTimer += Time.deltaTime;
                if (sparkTimer >= 0.22f)
                {
                    sparkTimer = 0f;
                    if (_buildingShipLeft != null) SpaceExplosionVFX.SpawnHitSpark(leftPos + Random.insideUnitSphere * 1.5f + Vector3.up * 0.5f, false);
                    if (_buildingShipRight != null) SpaceExplosionVFX.SpawnHitSpark(rightPos + Random.insideUnitSphere * 1.5f + Vector3.up * 0.5f, false);
                }

                yield return null;
            }

            if (_isExploding) yield break;

            // ĐÃ HOÀN TẤT CHẾ TẠO 5 GIÂY: KIỂM TRA TRẠNG THÁI HIỆN TẠI
            bool stillHostile = SpaceTrafficShipAI.IsFleetHostile && !AboutInfoStation.AnyStationActive;

            if (stillHostile)
            {
                // 1. VẪN ĐANG BỊ TẤN CÔNG: GẦM RÚ ĐỘNG CƠ VÀ BỐC ĐẦU CẤT CÁNH BAY LÊN TRUY SÁT!
                var player = FindFirstObjectByType<PlayerSpaceshipController>();
                Vector3 playerPos = player != null ? player.transform.position : (transform.position + Vector3.up * 35f);

                // Phóng tàu Trái
                if (_buildingShipLeft != null)
                {
                    Vector3 toPlayer = (playerPos - leftPos).normalized;
                    Vector3 launchDir = (toPlayer * 0.75f + Vector3.up * 0.6f - transform.right * 0.3f).normalized;
                    _buildingShipLeft.transform.rotation = Quaternion.LookRotation(launchDir, Vector3.up);
                    if (leftRb != null)
                    {
                        leftRb.isKinematic = false;
                        leftRb.linearVelocity = launchDir * launchSpeed;
                    }
                    if (leftAi != null)
                    {
                        leftAi.enabled = true;
                        leftAi.InitializeAsReinforcement(leftFullScale, launchDir * launchSpeed);
                        _activeReinforcements.Add(leftAi);
                    }
                    SpaceExplosionVFX.SpawnHitSpark(leftPos, false);
                }

                // Phóng tàu Phải
                if (_buildingShipRight != null)
                {
                    Vector3 toPlayer = (playerPos - rightPos).normalized;
                    Vector3 launchDir = (toPlayer * 0.75f + Vector3.up * 0.6f + transform.right * 0.3f).normalized;
                    _buildingShipRight.transform.rotation = Quaternion.LookRotation(launchDir, Vector3.up);
                    if (rightRb != null)
                    {
                        rightRb.isKinematic = false;
                        rightRb.linearVelocity = launchDir * launchSpeed;
                    }
                    if (rightAi != null)
                    {
                        rightAi.enabled = true;
                        rightAi.InitializeAsReinforcement(rightFullScale, launchDir * launchSpeed);
                        _activeReinforcements.Add(rightAi);
                    }
                    SpaceExplosionVFX.SpawnHitSpark(rightPos, false);
                }
            }
            else
            {
                // 2. ĐÃ HẾT BỊ TẤN CÔNG (Người chơi đã đáp xuống căn cứ / Project):
                // -> TẠO NỐT CHO XONG RỒI CHO 2 TÀU ĐẬU LẠI NGAY TRÊN 2 BỆ!
                if (_buildingShipLeft != null)
                {
                    _buildingShipLeft.transform.position = leftPos;
                    _buildingShipLeft.transform.rotation = leftRot;
                    _buildingShipLeft.transform.localScale = leftFullScale;
                    if (leftRb != null)
                    {
                        if (!leftRb.isKinematic) leftRb.linearVelocity = Vector3.zero;
                        leftRb.isKinematic = true;
                    }
                    if (leftAi != null) leftAi.enabled = false;
                    var trail = _buildingShipLeft.GetComponentInChildren<TrailRenderer>();
                    if (trail != null) trail.Clear();
                    _parkedShipLeft = _buildingShipLeft;
                }

                if (_buildingShipRight != null)
                {
                    _buildingShipRight.transform.position = rightPos;
                    _buildingShipRight.transform.rotation = rightRot;
                    _buildingShipRight.transform.localScale = rightFullScale;
                    if (rightRb != null)
                    {
                        if (!rightRb.isKinematic) rightRb.linearVelocity = Vector3.zero;
                        rightRb.isKinematic = true;
                    }
                    if (rightAi != null) rightAi.enabled = false;
                    var trail = _buildingShipRight.GetComponentInChildren<TrailRenderer>();
                    if (trail != null) trail.Clear();
                    _parkedShipRight = _buildingShipRight;
                }
            }

            if (portalLight != null) portalLight.intensity = 18f;

            _buildingShipLeft = null;
            _buildingShipRight = null;
            _isConstructing = false;
        }

        /// <summary>
        /// Chuỗi nổ tung hủy diệt căn cứ khi Lõi Năng Lượng bị phá hủy:
        /// - Phá hủy TOÀN BỘ CĂN CỨ (Hangar, 04_Defense_Turrets_And_Squadron, đài quan sát, bệ phóng, tháp pháo, đá nền).
        /// - Biến toàn bộ thành hàng chục mảnh vụn vật lý 3D NỔ TUNG TÓE khắp không gian!
        /// </summary>
        public IEnumerator CatastrophicExplosionSequence()
        {
            if (_isExploding) yield break;
            _isExploding = true;
            this.enabled = false;

            SpaceTrafficShipAI.TriggerFleetHostileAlert(90f);

            Vector3 basePos = transform.position;

            // Hủy tàu đang chế tạo dở (nếu có)
            if (_buildingShipLeft != null)
            {
                SpaceExplosionVFX.SpawnExplosion(_buildingShipLeft.transform.position, 6.0f, false, null);
                Destroy(_buildingShipLeft);
            }
            if (_buildingShipRight != null)
            {
                SpaceExplosionVFX.SpawnExplosion(_buildingShipRight.transform.position, 6.0f, false, null);
                Destroy(_buildingShipRight);
            }

            // Hủy 2 tàu đang đậu trên bệ (nếu có)
            if (_parkedShipLeft != null)
            {
                SpaceExplosionVFX.SpawnExplosion(_parkedShipLeft.transform.position, 6.5f, false, null);
                Destroy(_parkedShipLeft);
            }
            if (_parkedShipRight != null)
            {
                SpaceExplosionVFX.SpawnExplosion(_parkedShipRight.transform.position, 6.5f, false, null);
                Destroy(_parkedShipRight);
            }

            // 1. Nổ trung tâm Lõi Năng Lượng liên tiếp 2 đợt
            Vector3 corePos = basePos + transform.forward * -6.5f + Vector3.up * 4.2f;
            SpaceExplosionVFX.SpawnExplosion(corePos, 12f, false, null);
            yield return new WaitForSeconds(0.2f);
            SpaceExplosionVFX.SpawnExplosion(corePos + Vector3.up * 2f, 15f, false, null);
            yield return new WaitForSeconds(0.2f);

            // 2. Nổ 4 Tháp pháo và Radar trên 04_Defense_Turrets_And_Squadron
            var turrets = GetComponentsInChildren<CarrierDefenseTurretAI>(true);
            for (int i = 0; i < turrets.Length; i++)
            {
                if (turrets[i] != null)
                {
                    SpaceExplosionVFX.SpawnExplosion(turrets[i].transform.position, 8.0f, false, null);
                }
            }
            yield return new WaitForSeconds(0.25f);

            // 3. Nổ 2 cánh bệ phóng và đường băng phía trước
            SpaceExplosionVFX.SpawnExplosion(GetPadPosition(0), 11.0f, false, null);
            yield return new WaitForSeconds(0.15f);
            SpaceExplosionVFX.SpawnExplosion(GetPadPosition(1), 11.0f, false, null);
            yield return new WaitForSeconds(0.2f);
            SpaceExplosionVFX.SpawnExplosion(basePos + transform.forward * -10.5f, 14.0f, false, null);
            yield return new WaitForSeconds(0.25f);

            // 4. Vụ nổ siêu hạt nhân 28m xé toạc toàn bộ căn cứ
            SpaceExplosionVFX.SpawnExplosion(basePos + Vector3.up * 4.5f, 28.0f, false, null);

            // 5. MẢNH VỠ BAY TUNG TÓE (PHYSICAL 3D DEBRIS SHATTER):
            var allRenderers = GetComponentsInChildren<MeshRenderer>(true);
            List<Transform> debrisTargets = new List<Transform>();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].transform != transform)
                {
                    debrisTargets.Add(allRenderers[i].transform);
                }
            }

            foreach (var tr in debrisTargets)
            {
                if (tr == null) continue;

                tr.SetParent(null);

                var turretComp = tr.GetComponent<CarrierDefenseTurretAI>();
                if (turretComp != null) Destroy(turretComp);
                var coreComp = tr.GetComponent<CarrierEnergyCore>();
                if (coreComp != null) Destroy(coreComp);

                var col = tr.GetComponent<Collider>();
                if (col == null)
                {
                    tr.gameObject.AddComponent<BoxCollider>();
                }
                else if (col is MeshCollider mc)
                {
                    mc.convex = true;
                }

                var rb = tr.GetComponent<Rigidbody>();
                if (rb == null) rb = tr.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.mass = Random.Range(4f, 18f);
                rb.linearDamping = 0.15f;
                rb.angularDamping = 0.3f;

                Vector3 outwardDir = (tr.position - (basePos + Vector3.up * 3.5f) + Random.insideUnitSphere * 2.5f).normalized;
                rb.linearVelocity = outwardDir * Random.Range(22f, 52f);
                rb.angularVelocity = Random.onUnitSphere * Random.Range(10f, 32f);

                Destroy(tr.gameObject, Random.Range(6.0f, 8.5f));
            }

            // 6. Hủy hoàn toàn GameObject căn cứ
            Destroy(gameObject);
        }

        private void UpdateSpawnerVisuals(bool hostile)
        {
            float speedMult = hostile ? 3.8f : 1.0f;
            float dt = Time.deltaTime;

            if (outerWarpRing != null)
                outerWarpRing.Rotate(0f, 32f * speedMult * dt, 0f, Space.Self);
            if (midWarpRing != null)
                midWarpRing.Rotate(65f * speedMult * dt, 0f, 0f, Space.Self);
            if (innerWarpRing != null)
                innerWarpRing.Rotate(-95f * speedMult * dt, 0f, 0f, Space.Self);

            Color targetCol = hostile ? AlertGateColor : StandbyGateColor;
            float pulse = hostile
                ? (0.7f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 10f)))
                : (0.85f + 0.15f * Mathf.Sin(Time.time * 2.5f));

            if (_portalRuntimeMat != null)
            {
                Color c = targetCol * (hostile ? 1.35f : 0.95f);
                c.a = 1f;
                _portalRuntimeMat.SetColor("_BaseColor", c);
                _portalRuntimeMat.color = c;
                if (_portalRuntimeMat.HasProperty("_EmissionColor"))
                {
                    _portalRuntimeMat.EnableKeyword("_EMISSION");
                    _portalRuntimeMat.SetColor("_EmissionColor", targetCol * (hostile ? 2.8f : 2.0f) * pulse);
                }
            }

            if (portalLight != null)
            {
                portalLight.color = targetCol;
                float targetIntensity = (hostile ? 8.5f : 3.2f) * pulse;
                portalLight.intensity = Mathf.Lerp(portalLight.intensity, targetIntensity, dt * 8f);
            }

            if (leftBeaconLight != null)
            {
                leftBeaconLight.color = targetCol;
                leftBeaconLight.intensity = hostile ? (Mathf.Sin(Time.time * 14f) > 0f ? 7f : 0.5f) : 2f;
            }

            if (rightBeaconLight != null)
            {
                rightBeaconLight.color = targetCol;
                rightBeaconLight.intensity = hostile ? (Mathf.Sin(Time.time * 14f + Mathf.PI) > 0f ? 7f : 0.5f) : 2f;
            }

            if (_arrowRuntimeMat != null)
            {
                Color c = targetCol * (hostile ? 1.4f : 1.0f);
                c.a = 1f;
                _arrowRuntimeMat.SetColor("_BaseColor", c);
                _arrowRuntimeMat.color = c;
            }
            if (_borderRuntimeMat != null)
            {
                Color c = targetCol * (hostile ? 1.4f : 1.0f);
                c.a = 1f;
                _borderRuntimeMat.SetColor("_BaseColor", c);
                _borderRuntimeMat.color = c;
            }

            if (statusText != null)
            {
                statusText.color = hostile ? AlertGateColor : new Color(0.92f, 0.99f, 1f, 1f);
                statusText.text = hostile ? "ALERT" : "CARRIER";
            }
        }
    }
}
