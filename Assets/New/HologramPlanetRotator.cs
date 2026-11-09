using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Điều khiển hoạt họa xoay 3D Hologram bản đồ sao / hành tinh tại Bàn chỉ huy Holo-Table trung tâm.
    /// Tính năng:
    /// - Quả cầu hành tinh nghiêng trục tự xoay quanh trục Y.
    /// - 2 vòng quỹ đạo (Orbital Rings) xoay đa trục độc lập với vận tốc khác nhau.
    /// - Tia quét phân tích Hologram (Scan Beam) di chuyển lên xuống tuần hoàn.
    /// - Hiệu ứng nhấp nhô lơ lửng (Anti-Gravity Bobbing) và đổi độ sáng nhẹ.
    /// </summary>
    public class HologramPlanetRotator : MonoBehaviour
    {
        [Header("Tốc độ xoay (Rotation Speeds)")]
        public float planetSpinSpeed = 18f;
        public float outerRingSpeed = 26f;
        public float innerRingSpeed = -34f;

        [Header("Tham chiếu đối tượng con")]
        public Transform planetCore;
        public Transform outerRing;
        public Transform innerRing;
        public Transform scanBeam;
        public Light holoSpotLight;

        [Header("Hiệu ứng nhấp nhô (Floating Bobbing)")]
        public float bobHeight = 0.08f;
        public float bobFrequency = 1.2f;

        private Vector3 _initialLocalPos;
        private float _timeOffset;
        private float _scanBeamBaseY;

        private void Start()
        {
            _initialLocalPos = transform.localPosition;
            _timeOffset = Random.Range(0f, 10f);
            if (scanBeam != null) _scanBeamBaseY = scanBeam.localPosition.y;
        }

        private void Update()
        {
            float t = Time.time + _timeOffset;

            // 1. Quả cầu hành tinh tự xoay quanh trục nghiêng
            if (planetCore != null)
            {
                planetCore.Rotate(Vector3.up, planetSpinSpeed * Time.deltaTime, Space.Self);
            }

            // 2. Vòng quỹ đạo ngoài & trong xoay đa hướng
            if (outerRing != null)
            {
                outerRing.Rotate(Vector3.forward, outerRingSpeed * Time.deltaTime, Space.Self);
            }
            if (innerRing != null)
            {
                innerRing.Rotate(Vector3.right, innerRingSpeed * Time.deltaTime, Space.Self);
            }

            // 3. Tia quét Laser phân tích di chuyển lên xuống dọc quả cầu
            if (scanBeam != null)
            {
                float scanY = _scanBeamBaseY + Mathf.Sin(t * 2.5f) * 0.45f;
                Vector3 p = scanBeam.localPosition;
                p.y = scanY;
                scanBeam.localPosition = p;
            }

            // 4. Toàn bộ hologram nhấp nhô nhẹ nhàng
            float bob = Mathf.Sin(t * bobFrequency) * bobHeight;
            transform.localPosition = _initialLocalPos + Vector3.up * bob;

            // 5. Ánh sáng hologram nhấp nháy tần số vi mô
            if (holoSpotLight != null)
            {
                holoSpotLight.intensity = 2.4f + Mathf.Sin(t * 6f) * 0.25f;
            }
        }
    }
}

