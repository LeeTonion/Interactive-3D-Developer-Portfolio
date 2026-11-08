using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Section 3 & Interaction B: Central Energy Core (Lò Phản Ứng Năng Lượng Trung Tâm).
    /// - Lõi năng lượng Cyan phát sáng theo nhịp thở liên tục.
    /// - Các vòng cơ khí mỏng xoay quanh lõi với tốc độ khác nhau.
    /// - Hạt năng lượng bay quỹ đạo quanh lò phản ứng.
    /// - Các ống dẫn năng lượng (Energy Conduits) phát sáng tuần tự dẫn ra các trạm khai thác.
    /// - Khi người chơi tiến lại gần (Player Interaction):
    ///   1. Lõi tăng độ sáng mạnh mẽ.
    ///   2. Các vòng xoay tăng tốc trong chốc lát.
    ///   3. Vòng xung năng lượng (Controlled Energy Pulse) lan tỏa ra ngoài.
    ///   4. Các cụm tinh thể kết nối sáng lên tuần tự.
    ///   5. Các trạm khai thác & Mining Drones gần đó phản ứng với xung năng lượng.
    ///   6. Không kích hoạt lặp lại liên tục khi người chơi vẫn đứng trong vùng bán kính.
    /// </summary>
    public class AsteroidEnergyCore : MonoBehaviour
    {
        [Header("Inspector Settings (Thông Số Điều Chỉnh)")]
        public float corePulseIntensity = 5.5f;
        public float corePulseDuration = 3.6f;
        public float ringRotationSpeed = 28f;
        public float playerInteractionRadius = 26f;
        public float particleEmissionRate = 24f;

        [Header("Thành Phần Lõi Năng Lượng")]
        public Renderer coreCrystalRenderer;
        public Renderer innerSphereRenderer;
        public Light coreLight;
        public Transform[] rotatingRings;
        public float[] ringSpeedMultipliers = new float[] { 1.0f, -0.72f, 1.35f };
        public Renderer[] conduitStripRenderers;
        public ParticleSystem coreOrbitParticles;
        public LineRenderer pulseWaveRing;

        [Header("Kết Nối Hệ Sinh Thái Quanh Thiên Thạch")]
        public CrystalResonance[] connectedCrystals;
        public MiningDroneController[] nearbyDrones;
        public Light[] facilityStatusLights;

        public Color coreCyanColor = new Color(0f, 0.92f, 1f, 1f);

        private Material _coreCrystalMat;
        private Material _innerSphereMat;
        private Material[] _conduitMats;
        private Transform _playerTransform;
        private bool _isPlayerInsideRadius;
        private bool _isPulseActive;
        private float _pulseElapsed;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            InitializeMaterials();
            SetupPulseRing();
            FindPlayer();
        }

        private void InitializeMaterials()
        {
            if (coreCrystalRenderer != null)
            {
                var mats = coreCrystalRenderer.sharedMaterials;
                int idx = mats.Length >= 3 ? 2 : (mats.Length - 1);
                if (idx >= 0 && idx < mats.Length && mats[idx] != null)
                {
                    _coreCrystalMat = new Material(mats[idx]);
                    _coreCrystalMat.EnableKeyword("_EMISSION");
                    mats[idx] = _coreCrystalMat;
                    coreCrystalRenderer.sharedMaterials = mats;
                }
            }

            if (innerSphereRenderer != null && innerSphereRenderer.sharedMaterial != null)
            {
                _innerSphereMat = new Material(innerSphereRenderer.sharedMaterial);
                _innerSphereMat.EnableKeyword("_EMISSION");
                innerSphereRenderer.sharedMaterial = _innerSphereMat;
            }

            if (conduitStripRenderers != null && conduitStripRenderers.Length > 0)
            {
                _conduitMats = new Material[conduitStripRenderers.Length];
                for (int i = 0; i < conduitStripRenderers.Length; i++)
                {
                    if (conduitStripRenderers[i] != null && conduitStripRenderers[i].sharedMaterial != null)
                    {
                        _conduitMats[i] = new Material(conduitStripRenderers[i].sharedMaterial);
                        _conduitMats[i].EnableKeyword("_EMISSION");
                        conduitStripRenderers[i].sharedMaterial = _conduitMats[i];
                    }
                }
            }
        }

        private void SetupPulseRing()
        {
            if (pulseWaveRing == null) return;
            pulseWaveRing.useWorldSpace = false;
            pulseWaveRing.loop = true;
            pulseWaveRing.positionCount = 48;
            pulseWaveRing.startWidth = 0.22f;
            pulseWaveRing.endWidth = 0.22f;
            pulseWaveRing.enabled = false;
        }

        private void FindPlayer()
        {
            var p = GameObject.Find("PlayerSpaceship");
            if (p != null) _playerTransform = p.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;

            if (_playerTransform == null) FindPlayer();

            // 1. Kiểm tra khoảng cách người chơi (không kích hoạt lặp lại khi đang đứng bên trong)
            if (_playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                if (dist <= playerInteractionRadius)
                {
                    if (!_isPlayerInsideRadius)
                    {
                        _isPlayerInsideRadius = true;
                        ActivateCorePulseInteraction();
                    }
                }
                else if (dist > playerInteractionRadius + 4f)
                {
                    _isPlayerInsideRadius = false;
                }
            }

            // 2. Xử lý tiến trình xung năng lượng khi kích hoạt
            float interactionBoost = 0f;
            if (_isPulseActive)
            {
                _pulseElapsed += dt;
                float norm = Mathf.Clamp01(_pulseElapsed / Mathf.Max(0.2f, corePulseDuration));
                interactionBoost = Mathf.Sin(norm * Mathf.PI);

                UpdateExpandingPulseRing(norm);

                if (_pulseElapsed >= corePulseDuration)
                {
                    _isPulseActive = false;
                    if (pulseWaveRing != null) pulseWaveRing.enabled = false;
                }
            }

            // 3. Xoay các vòng cơ khí đa trục ở tốc độ khác nhau (tăng tốc khi tương tác)
            float speedFactor = 1f + interactionBoost * 2.2f;
            if (rotatingRings != null)
            {
                for (int i = 0; i < rotatingRings.Length; i++)
                {
                    if (rotatingRings[i] == null) continue;
                    float mult = (ringSpeedMultipliers != null && i < ringSpeedMultipliers.Length) ? ringSpeedMultipliers[i] : 1f;
                    float spd = ringRotationSpeed * mult * speedFactor;
                    Vector3 axis = (i % 3 == 0) ? new Vector3(0.25f, 1f, 0.1f).normalized :
                                   (i % 3 == 1) ? new Vector3(1f, 0.2f, 0.3f).normalized :
                                                  new Vector3(0.15f, 0.3f, 1f).normalized;
                    rotatingRings[i].Rotate(axis * (spd * dt), Space.Self);
                }
            }

            // 4. Nhịp đập ánh sáng Lõi Năng Lượng
            float idlePulse = 0.85f + 0.25f * Mathf.Sin(time * 2.2f);
            float totalGlow = (corePulseIntensity * idlePulse) + (interactionBoost * corePulseIntensity * 1.4f);

            if (coreLight != null)
            {
                coreLight.intensity = totalGlow * 1.3f;
                coreLight.range = 36f + interactionBoost * 18f;
            }

            if (_coreCrystalMat != null)
            {
                _coreCrystalMat.SetColor(EmissionColorId, coreCyanColor * (totalGlow * 0.55f));
            }

            if (_innerSphereMat != null)
            {
                _innerSphereMat.SetColor(EmissionColorId, coreCyanColor * (totalGlow * 0.75f));
            }

            if (coreOrbitParticles != null)
            {
                var em = coreOrbitParticles.emission;
                em.rateOverTime = particleEmissionRate * (1f + interactionBoost * 1.8f);
            }

            // 5. Ống dẫn năng lượng phát sáng tuần tự (Sequential Conduit Illumination)
            if (_conduitMats != null && _conduitMats.Length > 0)
            {
                float waveSpeed = _isPulseActive ? 6.0f : 2.0f;
                for (int i = 0; i < _conduitMats.Length; i++)
                {
                    if (_conduitMats[i] == null) continue;
                    float phase = Mathf.Sin(time * waveSpeed - i * 0.85f) * 0.5f + 0.5f;
                    float intensity = Mathf.Lerp(0.6f, 2.8f, phase) * (1f + interactionBoost * 1.2f);
                    _conduitMats[i].SetColor(EmissionColorId, coreCyanColor * intensity);
                }
            }

            // 6. Đèn trạng thái các cơ sở khai thác xung quanh
            if (facilityStatusLights != null)
            {
                for (int i = 0; i < facilityStatusLights.Length; i++)
                {
                    if (facilityStatusLights[i] == null) continue;
                    facilityStatusLights[i].intensity = 2.2f + interactionBoost * 3.5f;
                }
            }
        }

        private void UpdateExpandingPulseRing(float normalizedTime)
        {
            if (pulseWaveRing == null) return;
            pulseWaveRing.enabled = true;

            float radius = Mathf.Lerp(2.5f, playerInteractionRadius * 1.15f, Mathf.SmoothStep(0f, 1f, normalizedTime));
            int count = pulseWaveRing.positionCount;
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * Mathf.PI * 2f;
                pulseWaveRing.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, 0.5f, Mathf.Sin(ang) * radius));
            }

            float width = Mathf.Lerp(0.32f, 0.04f, normalizedTime);
            pulseWaveRing.startWidth = width;
            pulseWaveRing.endWidth = width;
        }

        public void ActivateCorePulseInteraction()
        {
            _isPulseActive = true;
            _pulseElapsed = 0f;

            if (coreOrbitParticles != null)
            {
                coreOrbitParticles.Emit(28);
            }

            // Kích hoạt các tinh thể sáng tuần tự theo khoảng cách
            if (connectedCrystals != null)
            {
                for (int i = 0; i < connectedCrystals.Length; i++)
                {
                    if (connectedCrystals[i] != null)
                    {
                        connectedCrystals[i].TriggerChainReaction(i * 0.16f);
                    }
                }
            }

            // Các Mining Drones gần đó phản ứng với xung năng lượng
            if (nearbyDrones != null)
            {
                for (int i = 0; i < nearbyDrones.Length; i++)
                {
                    if (nearbyDrones[i] != null)
                    {
                        nearbyDrones[i].ReactToCorePulse();
                    }
                }
            }
        }
    }
}
