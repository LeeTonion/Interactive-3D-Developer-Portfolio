using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Interaction A: Crystal Resonance (Cộng Hưởng Tinh Thể Năng Lượng).
    /// - Chỉ điều khiển phát sáng trên đúng Submesh Tinh Thể (Submesh 2 của rock_crystals), giữ nguyên bệ đá tối màu.
    /// - Khi người chơi bay lại gần:
    ///   * Tinh thể sáng rực lên mượt mà.
    ///   * Hạt năng lượng lơ lửng bay quanh tinh thể.
    ///   * Các mỏ khoáng sản nhỏ (Small Mineral Deposits) lân cận sáng hưởng ứng.
    /// - Hỗ trợ kích hoạt phát sáng dây chuyền từ Lõi Năng Lượng (Energy Core Chain Reaction).
    /// </summary>
    public class CrystalResonance : MonoBehaviour
    {
        public enum CrystalCategory { BlueEnergy, PurpleRare, SmallDeposit }

        [Header("Phân Loại & Màu Sắc Tinh Thể")]
        public CrystalCategory category = CrystalCategory.BlueEnergy;
        public Color crystalGlowColor = new Color(0f, 0.9f, 1f, 1f);
        [Range(0.2f, 5f)] public float crystalGlowIntensity = 1.4f;
        [Range(1f, 8f)] public float resonanceGlowMultiplier = 2.4f;

        [Header("Bán Kính Tương Tác (Inspector Settings)")]
        public float crystalInteractionRadius = 16f;
        public float particleEmissionRate = 14f;

        [Header("Tham Chiếu Hiển Thị & VFX")]
        public Light crystalLight;
        public ParticleSystem crystalParticles;
        public List<CrystalResonance> nearbySmallDeposits = new List<CrystalResonance>();

        private Material _crystalMatInstance;
        private Transform _playerTransform;
        private float _currentGlow;
        private float _baseLightIntensity = 2.0f;
        private bool _isChainTriggered;
        private float _chainTimer;
        private bool _wasPlayerInside;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Start()
        {
            InitializeCrystalMaterial();
            _currentGlow = crystalGlowIntensity;
            if (crystalLight != null) _baseLightIntensity = crystalLight.intensity;
            FindPlayer();
        }

        public void InitializeCrystalMaterial()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = GetComponentInChildren<MeshRenderer>();
            if (mr == null) return;

            var mats = mr.sharedMaterials;
            // Kenney rock_crystals models have 3 submeshes: [0] rockTrack, [1] rock, [2] crystal
            int crystalSubmeshIndex = mats.Length >= 3 ? 2 : (mats.Length - 1);
            if (crystalSubmeshIndex >= 0 && crystalSubmeshIndex < mats.Length && mats[crystalSubmeshIndex] != null)
            {
                _crystalMatInstance = new Material(mats[crystalSubmeshIndex]);
                _crystalMatInstance.EnableKeyword("_EMISSION");
                _crystalMatInstance.SetColor(BaseColorId, crystalGlowColor);
                _crystalMatInstance.color = crystalGlowColor;
                _crystalMatInstance.SetColor(EmissionColorId, crystalGlowColor * crystalGlowIntensity);
                mats[crystalSubmeshIndex] = _crystalMatInstance;
                mr.sharedMaterials = mats;
            }
        }

        private void FindPlayer()
        {
            var p = GameObject.Find("PlayerSpaceship");
            if (p != null) _playerTransform = p.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_playerTransform == null) FindPlayer();

            float shimmer = 1f + 0.12f * Mathf.Sin(Time.time * 2.4f + transform.position.x * 0.5f);
            float targetGlow = crystalGlowIntensity * shimmer;
            bool playerNear = false;

            if (_isChainTriggered)
            {
                _chainTimer -= dt;
                if (_chainTimer <= 0f) _isChainTriggered = false;
                targetGlow = crystalGlowIntensity * resonanceGlowMultiplier * 1.15f;
                playerNear = true;
            }
            else if (_playerTransform != null && category != CrystalCategory.SmallDeposit)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                if (dist <= crystalInteractionRadius)
                {
                    playerNear = true;
                    float t = Mathf.Clamp01(1f - (dist / crystalInteractionRadius));
                    targetGlow = Mathf.Lerp(crystalGlowIntensity, crystalGlowIntensity * resonanceGlowMultiplier, Mathf.SmoothStep(0f, 1f, t));

                    if (!_wasPlayerInside)
                    {
                        _wasPlayerInside = true;
                        IlluminateNearbySmallDeposits();
                    }
                }
                else
                {
                    _wasPlayerInside = false;
                }
            }

            _currentGlow = Mathf.Lerp(_currentGlow, targetGlow, 4.5f * dt);

            if (_crystalMatInstance != null)
            {
                _crystalMatInstance.SetColor(EmissionColorId, crystalGlowColor * _currentGlow);
            }

            if (crystalLight != null)
            {
                float ratio = _currentGlow / Mathf.Max(0.1f, crystalGlowIntensity);
                crystalLight.intensity = _baseLightIntensity * ratio;
            }

            if (crystalParticles != null)
            {
                var em = crystalParticles.emission;
                em.rateOverTime = playerNear ? particleEmissionRate : (particleEmissionRate * 0.25f);
            }
        }

        private void IlluminateNearbySmallDeposits()
        {
            if (nearbySmallDeposits == null) return;
            for (int i = 0; i < nearbySmallDeposits.Count; i++)
            {
                if (nearbySmallDeposits[i] != null)
                {
                    nearbySmallDeposits[i].TriggerChainReaction(0.12f * (i + 1));
                }
            }
        }

        public void TriggerChainReaction(float delay)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(ChainReactionRoutine(delay));
        }

        private IEnumerator ChainReactionRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            _isChainTriggered = true;
            _chainTimer = 2.8f;
            if (crystalParticles != null)
            {
                crystalParticles.Emit(12);
            }
        }
    }
}
