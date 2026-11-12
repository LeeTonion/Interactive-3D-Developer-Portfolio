using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Tia Laser Khai Khoáng Năng Lượng Tinh Thể (Mining Extraction Laser).
    /// - Chiếu chùm tia nhiệt năng lượng plasma liên tục vào khối tinh thể pha lê.
    /// - LineRenderer chùm tia sáng động với biến thiên độ dày & cường độ ánh sáng.
    /// - Đèn PointLight tại điểm tiếp xúc nhấp nháy tạo hiệu ứng nhiệt độ siêu cao (Plasma Melt Point).
    /// </summary>
    public class MiningExtractionLaser : MonoBehaviour
    {
        [Header("Mục Tiêu & Điểm Bắn")]
        public Transform laserEmitterOrigin;
        public Transform crystalTarget;

        [Header("Chùm Tia Laser")]
        public LineRenderer laserBeam;
        public Color laserColor = new Color(1f, 0.45f, 0.1f, 1f); // Plasma Gold/Orange
        public float baseWidth = 0.12f;

        [Header("Điểm Tác Động Plasma")]
        public Light contactPointLight;

        private void Start()
        {
            if (laserBeam == null) laserBeam = GetComponent<LineRenderer>();
            if (laserBeam != null)
            {
                laserBeam.positionCount = 2;
                laserBeam.useWorldSpace = true;
            }
        }

        private void Update()
        {
            if (laserBeam == null || laserEmitterOrigin == null || crystalTarget == null) return;

            float time = Time.time;
            Vector3 startPos = laserEmitterOrigin.position;
            Vector3 endPos = crystalTarget.position;

            laserBeam.SetPosition(0, startPos);
            laserBeam.SetPosition(1, endPos);

            // Xung nhịp biến thiên độ dày tia
            float pulseWidth = baseWidth * (1f + 0.35f * Mathf.Sin(time * 25f));
            laserBeam.startWidth = pulseWidth;
            laserBeam.endWidth = pulseWidth * 0.75f;

            if (contactPointLight != null)
            {
                contactPointLight.transform.position = endPos;
                contactPointLight.intensity = 3.5f + 1.8f * Mathf.PerlinNoise(time * 20f, 0f);
            }
        }
    }
}

