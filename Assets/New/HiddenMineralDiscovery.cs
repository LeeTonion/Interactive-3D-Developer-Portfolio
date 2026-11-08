using System.Collections;
using UnityEngine;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Section 7 (Interaction C): Hidden Mineral Deposit (Khám Phá Mỏ Khoáng Sản Hiếm Ẩn Giấu).
    /// - Khi phi thuyền người chơi bay vào gần hốc đá bí mật:
    ///   * Cụm tinh thể quý hiếm kích hoạt phát sáng rực rỡ.
    ///   * Hạt năng lượng lấp lánh bay lên.
    ///   * Thông báo Hologram nhỏ gọn xuất hiện: "RARE MINERAL DISCOVERED".
    /// </summary>
    public class HiddenMineralDiscovery : MonoBehaviour
    {
        [Header("Inspector Settings (Bán Kính Khám Phá)")]
        public float playerInteractionRadius = 15f;

        [Header("Thành Phần Hiển Thị")]
        public Renderer mineralRenderer;
        public Light mineralLight;
        public Transform hologramRoot;
        public TMP_Text discoveryHoloText;
        public ParticleSystem sparkleParticles;
        public Color rareMineralColor = new Color(1f, 0.25f, 0.85f, 1f);

        private bool _isDiscovered;
        private Transform _playerTransform;
        private Camera _mainCam;
        private Material _crystalMat;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            if (mineralRenderer != null)
            {
                var mats = mineralRenderer.sharedMaterials;
                int idx = mats.Length >= 3 ? 2 : (mats.Length - 1);
                if (idx >= 0 && idx < mats.Length && mats[idx] != null)
                {
                    _crystalMat = new Material(mats[idx]);
                    _crystalMat.EnableKeyword("_EMISSION");
                    _crystalMat.SetColor(EmissionColorId, rareMineralColor * 0.8f);
                    mats[idx] = _crystalMat;
                    mineralRenderer.sharedMaterials = mats;
                }
            }

            if (hologramRoot != null) hologramRoot.gameObject.SetActive(false);
            else if (discoveryHoloText != null) discoveryHoloText.gameObject.SetActive(false);

            FindPlayer();
        }

        private void FindPlayer()
        {
            var p = GameObject.Find("PlayerSpaceship");
            if (p != null) _playerTransform = p.transform;
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (_playerTransform == null || _mainCam == null) FindPlayer();

            if (!_isDiscovered && _playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                if (dist <= playerInteractionRadius)
                {
                    TriggerDiscovery();
                }
            }

            // Giữ bảng thông báo hologram nhỏ luôn xoay hướng về Camera người chơi
            Transform targetHolo = hologramRoot != null ? hologramRoot : (discoveryHoloText != null ? discoveryHoloText.transform : null);
            if (_isDiscovered && targetHolo != null && targetHolo.gameObject.activeSelf && _mainCam != null)
            {
                Vector3 toCam = targetHolo.position - _mainCam.transform.position;
                if (toCam.sqrMagnitude > 0.01f)
                {
                    targetHolo.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                }
            }
        }

        private void TriggerDiscovery()
        {
            _isDiscovered = true;

            if (_crystalMat != null)
            {
                _crystalMat.SetColor(EmissionColorId, rareMineralColor * 3.8f);
            }

            if (mineralLight != null)
            {
                mineralLight.color = rareMineralColor;
                mineralLight.intensity = 4.8f;
                mineralLight.range = 16f;
            }

            if (sparkleParticles != null)
            {
                sparkleParticles.Play();
            }

            if (discoveryHoloText != null)
            {
                discoveryHoloText.text = "RARE MINERAL DISCOVERED";
            }

            if (hologramRoot != null) hologramRoot.gameObject.SetActive(true);
            else if (discoveryHoloText != null) discoveryHoloText.gameObject.SetActive(true);
        }
    }
}
