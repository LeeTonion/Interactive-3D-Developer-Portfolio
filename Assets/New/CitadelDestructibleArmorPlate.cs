using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Điều khiển các mảng vỏ bọc ngoài (Exterior Armor Plates / Cladding Modules) của Căn cứ Citadel:
    /// - Khi tàu vũ trụ bắn trúng: Tạo tia lửa nổ rực lửa (Fiery explosive impacts & sparks).
    /// - Khi chịu đủ sát thương (2 phát đạn laser): Mảng vỏ bọc ngoài bị hỏng lóc, bung khớp nối
    ///   và rụng rơi xuống đất với vật lý 3D Rigidbody (Peel off and fall to ground).
    /// - Để lộ khung xương chịu lực cháy xém đen (Charred structural bulkhead) với khói bốc nghi ngút.
    /// </summary>
    public class CitadelDestructibleArmorPlate : MonoBehaviour
    {
        [Header("Armor Stats")]
        [Tooltip("Lượng máu của mảng giáp trước khi bung rơi")]
        public float maxHealth = 50f;
        public float currentHealth = 50f;
        public bool isDetached = false;

        [Header("Visual References")]
        public MeshRenderer plateRenderer;
        public Collider plateCollider;

        private Vector3 _originalLocalPos;
        private Quaternion _originalLocalRot;
        private Transform _originalParent;
        private GameObject _charredBacking;
        private ParticleSystem _smolderSmoke;
        private AudioSource _audioSource;
        private static AudioClip _impactSound;
        private static AudioClip _fractureSound;

        private static Material _charredTitaniumMat;
        private static Material _smokeMat;
        private static Material _sparkMat;
        private static readonly List<CitadelDestructibleArmorPlate> AllPlates = new List<CitadelDestructibleArmorPlate>();

        public static IReadOnlyList<CitadelDestructibleArmorPlate> ActivePlates => AllPlates;

        private void Awake()
        {
            currentHealth = maxHealth;
            _originalLocalPos = transform.localPosition;
            _originalLocalRot = transform.localRotation;
            _originalParent = transform.parent;

            if (plateRenderer == null) plateRenderer = GetComponent<MeshRenderer>();
            if (plateCollider == null) plateCollider = GetComponent<Collider>();

            EnsureAudio();
            EnsureMaterials();
        }

        private void OnEnable()
        {
            if (!AllPlates.Contains(this)) AllPlates.Add(this);
        }

        private void OnDisable()
        {
            AllPlates.Remove(this);
        }

        private void EnsureAudio()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                _audioSource.minDistance = 6f;
                _audioSource.maxDistance = 75f;
                _audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (_impactSound == null) _impactSound = CreateMetalImpactClip();
            if (_fractureSound == null) _fractureSound = CreateArmorFractureClip();
        }

        private static void EnsureMaterials()
        {
#if UNITY_EDITOR
            if (_charredTitaniumMat == null)
                _charredTitaniumMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_SciFi_DarkTitanium.mat");
            if (_smokeMat == null)
                _smokeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Cosmic_Smoke.mat");
            if (_sparkMat == null)
                _sparkMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Sci-Fi Styled Modular Pack/Materials/hologram_particle.mat");
#endif
            if (_charredTitaniumMat == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                _charredTitaniumMat = new Material(s);
                _charredTitaniumMat.name = "Mat_Armor_CharredTitanium";
                _charredTitaniumMat.color = new Color(0.08f, 0.08f, 0.09f, 1f);
            }
        }

        /// <summary>
        /// Nhận sát thương từ phát bắn Laser của Phi thuyền.
        /// </summary>
        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 boltDir)
        {
            if (isDetached) return;

            currentHealth -= damage;

            // Kích hoạt báo động hạm đội bảo vệ căn cứ
            SpaceTrafficShipAI.TriggerFleetHostileAlert(60f);

            // Hiệu ứng tia lửa nổ rực cháy trên bề mặt giáp
            SpawnImpactSparksAndFire(hitPoint, boltDir);

            // Âm thanh kim loại va đập nặng
            if (_audioSource != null && _impactSound != null)
            {
                _audioSource.pitch = Random.Range(0.85f, 1.15f);
                _audioSource.PlayOneShot(_impactSound, 0.75f);
            }

            // Rung lắc nhẹ tấm giáp
            StartCoroutine(ImpactJoltRoutine());

            if (currentHealth <= 0f)
            {
                DetachAndPeelOff(hitPoint, boltDir);
            }
        }

        private IEnumerator ImpactJoltRoutine()
        {
            if (isDetached) yield break;
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;
            float duration = 0.12f;

            while (elapsed < duration)
            {
                if (isDetached) yield break;
                elapsed += Time.deltaTime;
                Vector3 jitter = Random.insideUnitSphere * 0.04f;
                transform.localPosition = originalPos + jitter;
                yield return null;
            }
            if (!isDetached) transform.localPosition = originalPos;
        }

        /// <summary>
        /// Mảng vỏ ngoài bị bung nẹp hàn, văng rụng ra khỏi tòa nhà và rơi tự do xuống đất với Rigidbody.
        /// </summary>
        public void DetachAndPeelOff(Vector3 hitPoint, Vector3 boltDir)
        {
            if (isDetached) return;
            isDetached = true;

            // Âm thanh đứt gãy kết cấu hợp kim dữ dội
            if (_audioSource != null && _fractureSound != null)
            {
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.PlayOneShot(_fractureSound, 1.0f);
            }

            // 1. Tạo vụ nổ bốc lửa rực sáng điểm gãy
            SpawnDetachmentExplosion(transform.position);

            // 2. Tạo khung xương chịu lực cháy xém đen (Charred Backing) để lại trên vách tường
            SpawnCharredBackingFrame();

            // 3. Tách mảng vỏ ngoài ra khỏi vách tường và gắn Rigidbody vật lý thực tế
            transform.SetParent(null, true);

            // Đảm bảo có Collider hợp lệ để rơi chạm đất
            if (plateCollider == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1.8f, 1.8f, 0.25f);
                plateCollider = box;
            }
            plateCollider.isTrigger = false;

            // Thêm Rigidbody để rơi tự do theo trọng lực
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 35f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Lực đẩy văng ra ngoài từ bề mặt tường + văng theo hướng đạn
            Vector3 wallNormal = (hitPoint - _originalParent.position).normalized;
            if (wallNormal == Vector3.zero) wallNormal = -boltDir;

            Vector3 blastImpulse = (wallNormal * Random.Range(3.5f, 6.0f))
                                 + (boltDir * Random.Range(1.5f, 3.0f))
                                 + (Vector3.up * Random.Range(1.2f, 3.0f))
                                 + (Random.insideUnitSphere * 1.0f);

            rb.linearVelocity = blastImpulse;
            rb.angularVelocity = Random.insideUnitSphere * Random.Range(12f, 24f);

            // Làm xỉn màu mảng giáp đã bị cháy xém
            if (plateRenderer != null && _charredTitaniumMat != null)
            {
                var mats = plateRenderer.materials;
                if (mats != null && mats.Length > 0)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = _charredTitaniumMat;
                    }
                    plateRenderer.materials = mats;
                }
            }

            // Tự động dọn dẹp mảng vỏ vỡ sau 35s để tối ưu hiệu năng
            Destroy(gameObject, 35f);
        }

        private void SpawnCharredBackingFrame()
        {
            if (_originalParent == null) return;
            EnsureMaterials();

            _charredBacking = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _charredBacking.name = "Charred_Bulkhead_" + name;
            _charredBacking.transform.SetParent(_originalParent, false);
            _charredBacking.transform.localPosition = _originalLocalPos - Vector3.forward * 0.05f;
            _charredBacking.transform.localRotation = _originalLocalRot;
            _charredBacking.transform.localScale = new Vector3(1.85f, 1.85f, 0.12f);

            if (_charredTitaniumMat != null)
            {
                _charredBacking.GetComponent<Renderer>().sharedMaterial = _charredTitaniumMat;
            }

            if (!Application.isPlaying) return;

            // Tạo cột khói âm ỉ bốc lên từ khung vách bị toác
            var smokeGo = new GameObject("Smoldering_Embers_Smoke");
            smokeGo.transform.SetParent(_charredBacking.transform, false);
            smokeGo.transform.localPosition = Vector3.zero;

            _smolderSmoke = smokeGo.AddComponent<ParticleSystem>();
            var rend = smokeGo.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_smokeMat != null) rend.sharedMaterial = _smokeMat;

            var main = _smolderSmoke.main;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.6f;
            main.startSize = 0.6f;
            main.startColor = new Color(0.2f, 0.2f, 0.2f, 0.65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            var emission = _smolderSmoke.emission;
            emission.rateOverTime = 8f;

            var shape = _smolderSmoke.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.2f, 0.2f, 0.2f);
        }

        private void SpawnImpactSparksAndFire(Vector3 hitPoint, Vector3 boltDir)
        {
            if (!Application.isPlaying) return;
            EnsureMaterials();

            var sparkGo = new GameObject("Armor_Impact_SparkBurst");
            sparkGo.transform.position = hitPoint;

            var ps = sparkGo.AddComponent<ParticleSystem>();
            var rend = sparkGo.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_sparkMat != null) rend.sharedMaterial = _sparkMat;

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = Random.Range(6f, 12f);
            main.startSize = 0.15f;
            main.startColor = new Color(1.0f, 0.65f, 0.2f, 1.0f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            sparkGo.transform.rotation = Quaternion.LookRotation(-boltDir);

            Destroy(sparkGo, 0.8f);
        }

        private void SpawnDetachmentExplosion(Vector3 pos)
        {
            if (!Application.isPlaying) return;
            EnsureMaterials();

            var blastGo = new GameObject("Armor_Detachment_Blast");
            blastGo.transform.position = pos;

            var ps = blastGo.AddComponent<ParticleSystem>();
            var rend = blastGo.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_sparkMat != null) rend.sharedMaterial = _sparkMat;

            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = 0.6f;
            main.startSpeed = 8f;
            main.startSize = 0.4f;
            main.startColor = new Color(1.0f, 0.45f, 0.1f, 1.0f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var light = blastGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.5f, 0.15f);
            light.range = 10f;
            light.intensity = 5f;

            Destroy(blastGo, 1.0f);
        }

        #region Procedural Audio Synthesis

        private static AudioClip CreateMetalImpactClip()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.22f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float metalRing = Mathf.Sin(2f * Mathf.PI * 520f * (float)i / sampleRate);
                float sub = Mathf.Sin(2f * Mathf.PI * 180f * (float)i / sampleRate);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                float env = Mathf.Exp(-t * 18f);
                data[i] = (metalRing * 0.45f + sub * 0.35f + noise) * env * 0.8f;
            }
            var clip = AudioClip.Create("Armor_Impact_Tone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateArmorFractureClip()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.55f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float crunch = (Random.value * 2f - 1f) * 0.7f;
                float bassBoom = Mathf.Sin(2f * Mathf.PI * 90f * (float)i / sampleRate);
                float tear = Mathf.Sin(2f * Mathf.PI * (350f - t * 200f) * (float)i / sampleRate);
                float env = Mathf.Exp(-t * 7f);
                data[i] = (bassBoom * 0.5f + crunch * 0.35f + tear * 0.25f) * env * 0.95f;
            }
            var clip = AudioClip.Create("Armor_Fracture_Boom", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion

        /// <summary>
        /// Khởi tạo và trang bị hệ thống mảng giáp phá hủy được cho toàn bộ mặt tiền và tháp Cổng Tây Citadel.
        /// </summary>
        public static void SetupCitadelExteriorArmorPlates(GameObject aboutIsland)
        {
            if (aboutIsland == null) return;

            Transform citadelWalls = aboutIsland.transform.Find("Citadel_Main_Atrium_Superstructure/Citadel_Exterior_Walls");
            if (citadelWalls == null)
            {
                foreach (var t in aboutIsland.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Citadel_Exterior_Walls")
                    {
                        citadelWalls = t;
                        break;
                    }
                }
            }
            if (citadelWalls == null) return;

            // Duyệt qua các cụm tường mặt tiền và cổng Tây
            string[] facadeKeywords = new string[]
            {
                "West_Gate", "Portico", "Portal_Column", "Center_West", "West_Transom",
                "South_X", "North_X"
            };

            foreach (Transform wallChild in citadelWalls)
            {
                bool matches = false;
                foreach (var kw in facadeKeywords)
                {
                    if (wallChild.name.Contains(kw))
                    {
                        matches = true;
                        break;
                    }
                }
                if (!matches) continue;

                // Nếu đã có mảng giáp thì bỏ qua
                if (wallChild.GetComponent<CitadelDestructibleArmorPlate>() != null) continue;

                // Trang bị giáp có thể bị bắn rơi cho tường này
                var plate = wallChild.gameObject.AddComponent<CitadelDestructibleArmorPlate>();
                plate.maxHealth = 45f;
                plate.currentHealth = 45f;
                plate.plateRenderer = wallChild.GetComponentInChildren<MeshRenderer>();
                plate.plateCollider = wallChild.GetComponentInChildren<Collider>();
            }
        }
    }
}

