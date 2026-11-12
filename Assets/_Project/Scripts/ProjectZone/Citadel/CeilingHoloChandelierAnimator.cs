using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Điều khiển hoạt họa cho Cụm Đèn Trần / Máy Chiếu Hologram Trung Tâm (Ceiling Holo-Chandelier).
    /// - Turbine cơ khí quay êm ái tạo chiều sâu cơ điện tử.
    /// - Vành màng trập Iris bên trong tự xoay ngược chiều.
    /// - Nhịp thở ánh sáng Projector đồng bộ nhịp điệu trạm chỉ huy.
    /// </summary>
    [ExecuteAlways]
    public class CeilingHoloChandelierAnimator : MonoBehaviour
    {
        [Header("Tua-bin cơ khí quay")]
        public Transform turbineRotor;
        public float turbineRotorSpeed = 16f;

        [Header("Vành Iris xoay ngược chiều")]
        public Transform innerIrisRing;
        public float innerIrisSpeed = -24f;

        [Header("Đèn chiếu nhịp thở")]
        public Light centralProjectorLight;
        public float baseIntensity = 5.0f;
        public float pulseAmplitude = 0.5f;
        public float pulseSpeed = 2.8f;

        private void Update()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;

            if (turbineRotor != null)
            {
                turbineRotor.Rotate(Vector3.up, turbineRotorSpeed * dt, Space.Self);
            }

            if (innerIrisRing != null)
            {
                innerIrisRing.Rotate(Vector3.up, innerIrisSpeed * dt, Space.Self);
            }

            if (centralProjectorLight != null && Application.isPlaying)
            {
                centralProjectorLight.intensity = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            }
        }
    }
}

