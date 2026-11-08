using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Section 5: Cargo Spacecraft Traffic (Tàu Chở Quặng Tuần Hoàn Quỹ Đạo).
    /// - Di chuyển mượt mà theo quỹ đạo elip hoặc chuỗi Waypoint quanh Tiểu Hành Tinh Khai Khoáng.
    /// - Một số tàu tiếp cận gần Bến Bốc Dỡ Hàng (Cargo Loading Station) rồi rời bến mang theo quặng.
    /// - Phản ứng tăng cường hoạt động khi người chơi quan sát bến hàng (Interaction D: Cargo Ship Observation).
    /// - Hoàn toàn là yếu tố môi trường nền, không va chạm hay cản trở tàu người chơi.
    /// </summary>
    public class CargoShipOrbitController : MonoBehaviour
    {
        [Header("Inspector Settings (Thông Số Điều Chỉnh)")]
        public float cargoShipMovementSpeed = 14f;
        public float cargoOrbitRadius = 46f;
        public float orbitEccentricity = 1.25f;
        public float altitudeOffset = 8f;
        public float orbitTiltDegrees = 14f;
        public float startingPhaseRadians = 0f;
        public bool reverseDirection = false;

        [Header("Hiệu Ứng Động Cơ & Đèn Định Vị")]
        public Transform orbitCenterTransform;
        public Vector3 fallbackOrbitCenter = new Vector3(130f, 20f, -75f);
        public Light engineExhaustLight;
        public Light navBeaconLight;

        private float _phase;
        private float _activityBoost = 1f;

        private void Start()
        {
            _phase = startingPhaseRadians;
            // Đảm bảo tắt mọi Collider để không bao giờ cản trở tàu người chơi hoặc camera
            var cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 center = orbitCenterTransform != null ? orbitCenterTransform.position : fallbackOrbitCenter;

            float dirSign = reverseDirection ? -1f : 1f;
            float angularSpeed = (cargoShipMovementSpeed * _activityBoost) / Mathf.Max(15f, cargoOrbitRadius);
            _phase += dirSign * angularSpeed * dt;
            if (_phase > Mathf.PI * 2f) _phase -= Mathf.PI * 2f;
            else if (_phase < 0f) _phase += Mathf.PI * 2f;

            Vector3 currentPos = EvaluateOrbitPoint(center, _phase);
            Vector3 aheadPos = EvaluateOrbitPoint(center, _phase + dirSign * 0.06f);
            Vector3 moveDir = (aheadPos - currentPos);

            transform.position = currentPos;
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                // Nghiêng nhẹ thân tàu (bank) theo quỹ đạo để bay tự nhiên
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, -dirSign * 12f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * dt);
            }

            if (engineExhaustLight != null)
            {
                engineExhaustLight.intensity = (2.8f + 0.5f * Mathf.Sin(Time.time * 12f)) * _activityBoost;
            }
        }

        private Vector3 EvaluateOrbitPoint(Vector3 center, float angle)
        {
            float rx = cargoOrbitRadius;
            float rz = cargoOrbitRadius * orbitEccentricity;
            float x = Mathf.Cos(angle) * rx;
            float z = Mathf.Sin(angle) * rz;
            float y = Mathf.Sin(angle * 2f) * 3.2f + altitudeOffset;

            Quaternion tilt = Quaternion.Euler(orbitTiltDegrees, 20f, 0f);
            return center + tilt * new Vector3(x, y, z);
        }

        public void SetObservationBoost(bool active)
        {
            _activityBoost = active ? 1.45f : 1.0f;
            if (navBeaconLight != null)
            {
                navBeaconLight.intensity = active ? 4.2f : 2.0f;
            }
        }
    }
}
