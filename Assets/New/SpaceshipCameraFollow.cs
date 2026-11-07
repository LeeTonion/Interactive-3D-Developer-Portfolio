using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Camera bám theo tàu bay không gian 3D.
    /// Tự động xoay mượt mà theo góc bay, lùi xa và mở rộng FOV khi tăng tốc phản lực.
    /// </summary>
    public class SpaceshipCameraFollow : MonoBehaviour
    {
        [Header("Mục tiêu theo dõi")]
        public Transform target;
        public PlayerSpaceshipController spaceship;

        [Header("Khoảng cách & Góc nhìn")]
        [Tooltip("Khoảng cách phía sau tàu")]
        public float distance = 8.5f;
        [Tooltip("Độ cao phía trên tàu")]
        public float height = 2.8f;
        [Tooltip("Tốc độ bám theo vị trí")]
        public float followSpeed = 10f;
        [Tooltip("Tốc độ xoay bám theo góc nhìn")]
        public float rotationSpeed = 8f;

        [Header("Hiệu ứng tốc độ cao (Speed Dynamic FOV)")]
        public float normalFOV = 50f;
        public float boostFOV = 68f;
        public float fovSmoothSpeed = 4f;

        private Camera _cam;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null) _cam.fieldOfView = normalFOV;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. Tính toán vị trí camera phía sau tàu
            Vector3 targetPos = target.position - (target.forward * distance) + (target.up * height);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            // 2. Hướng góc nhìn về phía trước đầu tàu một chút
            Vector3 lookTarget = target.position + (target.forward * 4f);
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position, target.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

            // 3. Hiệu ứng mở rộng góc nhìn khi Boost
            if (_cam != null && spaceship != null)
            {
                float targetFOV = spaceship.IsBoosting ? boostFOV : normalFOV;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
            }
        }
    }
}

