using System.Collections;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Cổng Tăng Tốc Siêu Không Gian (Hyper-Drive Speed Boost Ring)
    /// Khi phi thuyền bay xuyên qua tâm cổng: Kích hoạt xung lực phóng tàu về phía trước,
    /// tạo sóng xung kích warp quang học và chớp sáng neon bùng nổ.
    /// </summary>
    public class SpeedBoostRing : MonoBehaviour
    {
        [Header("Thông Số Tăng Tốc")]
        public float boostImpulse = 28f;
        public float cooldown = 0.8f;

        [Header("Hiệu Ứng Hình Ảnh")]
        public Color ringColor = new Color(0f, 0.95f, 1f, 1f);
        public Light ringLight;

        private float _lastTriggerTime = -99f;
        private Renderer _ringRenderer;
        private Material _glowMat;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _ringRenderer = GetComponent<Renderer>();
            if (_ringRenderer == null) _ringRenderer = GetComponentInChildren<Renderer>();

            if (_ringRenderer != null)
            {
                _glowMat = new Material(_ringRenderer.sharedMaterial != null 
                    ? _ringRenderer.sharedMaterial 
                    : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")));
                _glowMat.EnableKeyword("_EMISSION");
                _glowMat.SetColor(EmissionColorId, ringColor * 2.5f);
                _ringRenderer.material = _glowMat;
            }

            if (ringLight == null)
            {
                ringLight = GetComponentInChildren<Light>();
            }

            // Đảm bảo có Trigger Collider để đón tàu
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var bc = gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(5.5f, 5.5f, 1.8f);
            }
            else
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastTriggerTime < cooldown) return;

            var player = other.GetComponentInParent<PlayerSpaceshipController>();
            if (player != null)
            {
                _lastTriggerTime = Time.time;
                TriggerBoost(player);
            }
        }

        private void TriggerBoost(PlayerSpaceshipController player)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                // Đẩy mạnh tàu theo hướng của cổng
                Vector3 boostDir = transform.forward;
                rb.AddForce(boostDir * boostImpulse, ForceMode.VelocityChange);
            }

            // Hiệu ứng sóng năng lượng & chớp sáng
            StartCoroutine(BoostFlashRoutine());
            SpaceExplosionVFX.SpawnHitSpark(transform.position, true);
        }

        private IEnumerator BoostFlashRoutine()
        {
            float elapsed = 0f;
            float duration = 0.45f;

            if (ringLight != null) ringLight.intensity = 6.0f;
            if (_glowMat != null) _glowMat.SetColor(EmissionColorId, Color.white * 6.0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = 1f - (elapsed / duration);

                if (ringLight != null)
                {
                    ringLight.intensity = Mathf.Lerp(1.5f, 6.0f, k);
                }

                if (_glowMat != null)
                {
                    Color c = Color.Lerp(ringColor * 2.5f, Color.white * 5.0f, k);
                    _glowMat.SetColor(EmissionColorId, c);
                }

                yield return null;
            }

            if (_glowMat != null) _glowMat.SetColor(EmissionColorId, ringColor * 2.5f);
            if (ringLight != null) ringLight.intensity = 1.5f;
        }

        private void OnDestroy()
        {
            if (_glowMat != null) Destroy(_glowMat);
        }
    }
}

