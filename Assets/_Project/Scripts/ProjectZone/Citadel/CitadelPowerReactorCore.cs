using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Khu Năng Lượng Trung Tâm Căn Cứ Citadel (Citadel Central Power Reactor Core):
    /// - Đặt tại sảnh trung tâm / tháp năng lượng, phi thuyền có thể ngắm bắn thẳng vào từ ngoài cửa hoặc từ trên không.
    /// - Khi bị bắn trúng: Tạo tia sét hồ quang, báo động an ninh và hao hụt thanh năng lượng.
    /// - Khi bị phá hủy hoàn toàn (Catastrophic Core Meltdown):
    ///   1. Nổ tung hồ quang điện siêu cấp, chấn động shockwave.
    ///   2. Đèn điện toàn bộ căn cứ bị mất điện cục bộ và chớp tắt loạn xạ (Emergency Brownout & Flickering).
    ///   3. Màn hình chính (Main Screen) bốc khói nghi ngút từ khe tản nhiệt và bị nhiễu scanline cyberpunk,
    ///      NHƯNG VẪN PHẢI XEM ĐƯỢC (100% chữ, nút bấm và slide danh mục portfolio vẫn rõ nét, hoạt động bình thường).
    /// </summary>
    public class CitadelPowerReactorCore : MonoBehaviour
    {
        [Header("Reactor Core Stats")]
        public float maxHealth = 160f;
        public float currentHealth = 160f;
        public bool isDestroyed = false;

        public static bool IsBasePowerFailed { get; private set; } = false;
        public static CitadelPowerReactorCore Instance { get; private set; }

        [Header("Visual Elements")]
        public Transform coreSphere;
        public Transform ringA;
        public Transform ringB;
        public Transform ringC;
        public Light coreLight;

        private AudioSource _audioSource;
        private static AudioClip _shieldHitSound;
        private static AudioClip _coreMeltdownSound;
        private static AudioClip _alarmSirenSound;

        private static Material _plasmaCoreMat;
        private static Material _charredSlagMat;
        private static Material _containmentSteelMat;
        private static Material _smokeMat;
        private static Material _sparkMat;

        private void Awake()
        {
            Instance = this;
            currentHealth = maxHealth;
            EnsureAudio();
            EnsureMaterials();
            SetupCoreVisuals();
        }

        private void EnsureAudio()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                _audioSource.minDistance = 8f;
                _audioSource.maxDistance = 90f;
                _audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            if (_shieldHitSound == null) _shieldHitSound = CreateShieldHitClip();
            if (_coreMeltdownSound == null) _coreMeltdownSound = CreateMeltdownExplosionClip();
            if (_alarmSirenSound == null) _alarmSirenSound = CreateEmergencySirenClip();
        }

        private static void EnsureMaterials()
        {
#if UNITY_EDITOR
            if (_plasmaCoreMat == null)
                _plasmaCoreMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_BlueCyan.mat");
            if (_charredSlagMat == null)
                _charredSlagMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_SciFi_DarkTitanium.mat");
            if (_containmentSteelMat == null)
                _containmentSteelMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_SciFi_SteelAccent.mat");
            if (_smokeMat == null)
                _smokeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Cosmic_Smoke.mat");
            if (_sparkMat == null)
                _sparkMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/ModularSciFi/hologram_particle.mat");
#endif
            if (_plasmaCoreMat == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                _plasmaCoreMat = new Material(s);
                _plasmaCoreMat.name = "Mat_Reactor_PlasmaCore";
                _plasmaCoreMat.color = new Color(0.1f, 0.9f, 1.0f, 1f);
            }

            if (_charredSlagMat == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                _charredSlagMat = new Material(s);
                _charredSlagMat.name = "Mat_Reactor_CharredSlag";
                _charredSlagMat.color = new Color(0.04f, 0.04f, 0.05f, 1f);
            }

            if (_containmentSteelMat == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                _containmentSteelMat = new Material(s);
                _containmentSteelMat.name = "Mat_Reactor_Steel";
                _containmentSteelMat.color = new Color(0.2f, 0.25f, 0.3f, 1f);
            }
        }

        public void SetupCoreVisuals()
        {
            EnsureMaterials();

            // Nếu chưa có các chi tiết mesh con, tạo cấu trúc Lò phản ứng lượng tử Sci-Fi
            if (coreSphere == null)
            {
                var sphereTr = transform.Find("Reactor_Plasma_Sphere");
                if (sphereTr != null)
                {
                    coreSphere = sphereTr;
                }
                else
                {
                    var sphereGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphereGo.name = "Reactor_Plasma_Sphere";
                    sphereGo.transform.SetParent(transform, false);
                    sphereGo.transform.localPosition = Vector3.zero;
                    sphereGo.transform.localScale = new Vector3(0.70f, 0.70f, 0.70f);
                    if (_plasmaCoreMat != null) sphereGo.GetComponent<Renderer>().sharedMaterial = _plasmaCoreMat;
                    coreSphere = sphereGo.transform;

                    var col = sphereGo.GetComponent<SphereCollider>();
                    if (col == null) col = sphereGo.AddComponent<SphereCollider>();
                    col.radius = 0.55f;
                }
            }

            if (ringA == null)
            {
                var ringTr = transform.Find("Magnetic_Containment_Ring_Alpha");
                if (ringTr != null)
                {
                    ringA = ringTr;
                }
                else
                {
                    var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ringGo.name = "Magnetic_Containment_Ring_Alpha";
                    ringGo.transform.SetParent(transform, false);
                    ringGo.transform.localPosition = Vector3.zero;
                    ringGo.transform.localScale = new Vector3(1.30f, 0.005f, 1.30f);
                    ringGo.transform.localRotation = Quaternion.identity;
                    DestroyImmediate(ringGo.GetComponent<Collider>());
                    if (_plasmaCoreMat != null) ringGo.GetComponent<Renderer>().sharedMaterial = _plasmaCoreMat;
                    ringA = ringGo.transform;
                }
            }

            if (ringB == null)
            {
                var ringTr = transform.Find("Magnetic_Containment_Ring_Beta");
                if (ringTr != null)
                {
                    ringB = ringTr;
                }
                else
                {
                    var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ringGo.name = "Magnetic_Containment_Ring_Beta";
                    ringGo.transform.SetParent(transform, false);
                    ringGo.transform.localPosition = Vector3.zero;
                    ringGo.transform.localScale = new Vector3(1.75f, 0.005f, 1.75f);
                    ringGo.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                    DestroyImmediate(ringGo.GetComponent<Collider>());
                    if (_plasmaCoreMat != null) ringGo.GetComponent<Renderer>().sharedMaterial = _plasmaCoreMat;
                    ringB = ringGo.transform;
                }
            }

            if (coreLight == null)
            {
                coreLight = GetComponent<Light>() ?? gameObject.AddComponent<Light>();
                coreLight.type = LightType.Point;
                coreLight.range = 22f;
                coreLight.intensity = 4.8f;
                coreLight.color = new Color(0.2f, 0.95f, 1.0f);
            }
        }

        private void Update()
        {
            if (isDestroyed) return;

            // Xoay các vòng từ trường giam hãm plasma
            float dt = Time.deltaTime;
            if (ringA != null) ringA.Rotate(Vector3.up, 38f * dt, Space.Self);
            if (ringB != null) ringB.Rotate(Vector3.forward, -44f * dt, Space.Self);
            if (ringC != null) ringC.Rotate(Vector3.right, 30f * dt, Space.Self);

            // Nhấp nháy nhẹ năng lượng plasma
            if (coreLight != null)
            {
                float pulse = Mathf.Sin(Time.time * 4f) * 0.45f + 4.5f;
                coreLight.intensity = pulse;
            }
        }

        /// <summary>
        /// Nhận sát thương khi bị tàu vũ trụ bắn đạn laser trúng.
        /// </summary>
        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 boltDir)
        {
            if (isDestroyed) return;

            currentHealth -= damage;

            // Kích hoạt báo động hạm đội bảo vệ
            SpaceTrafficShipAI.TriggerFleetHostileAlert(90f);

            // Hiệu ứng tia chớp điện hồ quang
            SpawnPlasmaArcImpact(hitPoint, boltDir);

            // Âm thanh khiên năng lượng bị bắn trúng
            if (_audioSource != null && _shieldHitSound != null)
            {
                _audioSource.pitch = Random.Range(0.9f, 1.15f);
                _audioSource.PlayOneShot(_shieldHitSound, 0.85f);
            }

            // Chớp đỏ cảnh báo trên đèn lõi
            StartCoroutine(DamageFlashRoutine());

            if (currentHealth <= 0f)
            {
                TriggerCoreMeltdown();
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            if (coreLight == null || isDestroyed) yield break;
            Color origColor = new Color(0.2f, 0.95f, 1.0f);
            coreLight.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            if (!isDestroyed && coreLight != null) coreLight.color = origColor;
        }

        /// <summary>
        /// Lõi năng lượng bị phá hủy hoàn toàn -> Kích hoạt chuỗi sự kiện sụp đổ năng lượng căn cứ!
        /// </summary>
        public void TriggerCoreMeltdown()
        {
            if (isDestroyed) return;
            isDestroyed = true;
            IsBasePowerFailed = true;

            // Âm thanh nổ tung lõi năng lượng
            if (_audioSource != null && _coreMeltdownSound != null)
            {
                _audioSource.PlayOneShot(_coreMeltdownSound, 1.0f);
            }

            // 1. Vụ nổ hồ quang điện và shockwave cực mạnh
            SpawnMeltdownShockwaveExplosion();

            // 2. Chuyển mesh lõi thành kim loại cháy đen xỉn màu
            if (coreSphere != null && _charredSlagMat != null)
            {
                var r = coreSphere.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = _charredSlagMat;
            }
            if (coreLight != null)
            {
                coreLight.color = new Color(1.0f, 0.3f, 0.1f);
                coreLight.intensity = 0.8f;
            }

            // 3. Kích hoạt còi báo động khẩn cấp
            if (Application.isPlaying)
            {
                StartCoroutine(EmergencySirenRoutine());
            }

            // 4. Kích hoạt đèn điện chớp tắt toàn căn cứ + màn hình chính bốc khói nhiễu
            ApplyBaseWidePowerFailure();
        }

        private void SpawnPlasmaArcImpact(Vector3 hitPoint, Vector3 boltDir)
        {
            if (!Application.isPlaying) return;
            EnsureMaterials();

            var arcGo = new GameObject("Reactor_Arc_Burst");
            arcGo.transform.position = hitPoint;

            var ps = arcGo.AddComponent<ParticleSystem>();
            var rend = arcGo.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_sparkMat != null) rend.sharedMaterial = _sparkMat;

            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = 0.25f;
            main.startSpeed = Random.Range(6f, 14f);
            main.startSize = 0.18f;
            main.startColor = new Color(0.4f, 0.95f, 1.0f, 1.0f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 22) });

            Destroy(arcGo, 0.6f);
        }

        private void SpawnMeltdownShockwaveExplosion()
        {
            if (!Application.isPlaying) return;
            EnsureMaterials();

            var blastGo = new GameObject("Reactor_Meltdown_Supernova");
            blastGo.transform.position = transform.position;

            var ps = blastGo.AddComponent<ParticleSystem>();
            var rend = blastGo.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_sparkMat != null) rend.sharedMaterial = _sparkMat;

            var main = ps.main;
            main.duration = 1.2f;
            main.loop = false;
            main.startLifetime = 1.0f;
            main.startSpeed = 16f;
            main.startSize = 0.8f;
            main.startColor = new Color(0.3f, 0.95f, 1.0f, 1.0f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 65),
                new ParticleSystem.Burst(0.2f, 40)
            });

            var flashLight = blastGo.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(0.5f, 0.9f, 1.0f);
            flashLight.range = 45f;
            flashLight.intensity = 18f;

            // Tạo cột khói đen đặc bốc lên cao
            var smokeGo = new GameObject("Reactor_Smoke_Plume");
            smokeGo.transform.SetParent(blastGo.transform, false);
            var smokePs = smokeGo.AddComponent<ParticleSystem>();
            var smokeRend = smokeGo.GetComponent<ParticleSystemRenderer>();
            smokeRend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_smokeMat != null) smokeRend.sharedMaterial = _smokeMat;

            var sMain = smokePs.main;
            sMain.duration = 4.0f;
            sMain.loop = true;
            sMain.startLifetime = 5.0f;
            sMain.startSpeed = 2.5f;
            sMain.startSize = 1.8f;
            sMain.startColor = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            sMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var sEmission = smokePs.emission;
            sEmission.rateOverTime = 16f;

            var sShape = smokePs.shape;
            sShape.shapeType = ParticleSystemShapeType.Sphere;
            sShape.radius = 1.2f;

            Destroy(blastGo, 45f);
        }

        private IEnumerator EmergencySirenRoutine()
        {
            int sirens = 4;
            while (sirens > 0)
            {
                if (_audioSource != null && _alarmSirenSound != null)
                {
                    _audioSource.PlayOneShot(_alarmSirenSound, 0.8f);
                }
                yield return new WaitForSeconds(2.2f);
                sirens--;
            }
        }

        /// <summary>
        /// Áp dụng tình trạng mất điện: Đèn điện chớp tắt và Màn hình chính bốc khói nhiễu (vẫn xem được).
        /// </summary>
        public static void ApplyBaseWidePowerFailure()
        {
            IsBasePowerFailed = true;

            // 1. Áp dụng cho toàn bộ Đèn trong Căn Cứ Citadel: Chớp tắt loạn xạ (Emergency Flickering)
            var allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l == null) continue;
                string fullName = (l.name + " " + (l.transform.parent != null ? l.transform.parent.name : "")).ToLower();

                bool isCitadelLight = fullName.Contains("chamber") || fullName.Contains("citadel") ||
                                      fullName.Contains("atrium") || fullName.Contains("interior") ||
                                      fullName.Contains("corridor") || fullName.Contains("spire") ||
                                      fullName.Contains("portal") || fullName.Contains("gate") ||
                                      fullName.Contains("desk") || fullName.Contains("monitor");

                if (isCitadelLight)
                {
                    if (l.GetComponent<CitadelEmergencyLightFlicker>() == null)
                    {
                        l.gameObject.AddComponent<CitadelEmergencyLightFlicker>();
                    }
                }
            }

            // 2. Áp dụng cho Màn hình chính (Main Screen): Bốc khói nghi ngút + Nhiễu scanline Cyberpunk
            // (ĐẢM BẢO 100% VẪN XEM ĐƯỢC CHỮ VÀ BẤM NÚT BÌNH THƯỜNG)
            var aboutStation = Object.FindFirstObjectByType<AboutInfoStation>();
            if (aboutStation != null)
            {
                var mainScreenTransform = aboutStation.transform.Find("MainScreen");
                if (mainScreenTransform != null)
                {
                    if (mainScreenTransform.GetComponent<MainScreenDamageSmokeAndGlitch>() == null)
                    {
                        mainScreenTransform.gameObject.AddComponent<MainScreenDamageSmokeAndGlitch>();
                    }
                }
            }
        }

        #region Audio Synthesis

        private static AudioClip CreateShieldHitClip()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.35f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float zap = Mathf.Sin(2f * Mathf.PI * (1100f - t * 650f) * (float)i / sampleRate);
                float crackle = (Random.value * 2f - 1f) * 0.35f;
                float env = Mathf.Exp(-t * 12f);
                data[i] = (zap * 0.7f + crackle) * env * 0.85f;
            }
            var clip = AudioClip.Create("Core_Shield_Zap", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMeltdownExplosionClip()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 1.8f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float sub = Mathf.Sin(2f * Mathf.PI * (60f + 25f * Mathf.Sin(t * 12f)) * (float)i / sampleRate);
                float hiss = (Random.value * 2f - 1f) * 0.6f;
                float zap = Mathf.Sin(2f * Mathf.PI * 450f * (float)i / sampleRate) * 0.25f;
                float env = Mathf.Exp(-t * 3.8f);
                data[i] = (sub * 0.6f + hiss * 0.3f + zap) * env;
            }
            var clip = AudioClip.Create("Core_Meltdown_Boom", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateEmergencySirenClip()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 2.0f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 480f + 320f * Mathf.Sin(2f * Mathf.PI * 1.25f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = tone * 0.45f;
            }
            var clip = AudioClip.Create("Emergency_Base_Siren", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }

    /// <summary>
    /// Hiệu ứng đèn điện chớp tắt khẩn cấp khi căn cứ bị mất nguồn năng lượng chính (Đèn điện phải chớp tắt).
    /// </summary>
    public class CitadelEmergencyLightFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;
        private Color _baseColor;
        private float _nextFlickerTime;
        private bool _isBlackout = false;

        private void Awake()
        {
            _light = GetComponent<Light>();
            if (_light != null)
            {
                _baseIntensity = _light.intensity;
                _baseColor = _light.color;
            }
        }

        private IEnumerator Start()
        {
            if (_light == null) yield break;

            _light.intensity = 0.05f;
            yield return new WaitForSeconds(Random.Range(0.25f, 0.55f));

            Color emergencyColor = Color.Lerp(_baseColor, new Color(1.0f, 0.35f, 0.1f), 0.75f);
            _light.color = emergencyColor;
        }

        private void Update()
        {
            if (_light == null) return;

            if (Time.time >= _nextFlickerTime)
            {
                _isBlackout = Random.value < 0.32f;
                if (_isBlackout)
                {
                    _light.intensity = Random.Range(0.02f, 0.15f);
                    _nextFlickerTime = Time.time + Random.Range(0.05f, 0.22f);
                }
                else
                {
                    float stutter = Mathf.PerlinNoise(Time.time * 18f, transform.position.x) * 0.85f + 0.15f;
                    _light.intensity = _baseIntensity * Random.Range(0.35f, 1.15f) * stutter;
                    _nextFlickerTime = Time.time + Random.Range(0.08f, 0.35f);
                }
            }
        }
    }

    /// <summary>
    /// Hiệu ứng Màn Hình Chính Bốc Khói & Nhiễu Cyberpunk khi khu năng lượng bị phá hủy:
    /// - Khói điện bốc lên từ các khe tản nhiệt trên đỉnh khung màn hình (MainScreen_Asset_Frame).
    /// - Tia lửa chập điện nhỏ lóe lên ở góc bezel.
    /// - Nhiễu scanline / viền giật cyberpunk.
    /// - QUAN TRỌNG: VẪN PHẢI XEM ĐƯỢC (Chữ, hình ảnh slide và nút bấm 100% rõ ràng, thao tác mượt mà).
    /// </summary>
    public class MainScreenDamageSmokeAndGlitch : MonoBehaviour
    {
        private ParticleSystem _topVentSmokeLeft;
        private ParticleSystem _topVentSmokeRight;
        private ParticleSystem _cornerSparks;
        private float _nextGlitchTime;
        private Vector3 _originalPos;

        private static Material _smokeMat;
        private static Material _sparkMat;

        private void Start()
        {
            _originalPos = transform.localPosition;
            EnsureVfxMaterials();
            CreateScreenSmokeEmitters();
            CreateCornerSparkEmitters();

            _nextGlitchTime = Time.time + Random.Range(1.5f, 3.5f);
        }

        private static void EnsureVfxMaterials()
        {
#if UNITY_EDITOR
            if (_smokeMat == null)
                _smokeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Cosmic_Smoke.mat");
            if (_sparkMat == null)
                _sparkMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/ModularSciFi/hologram_particle.mat");
#endif
        }

        private void CreateScreenSmokeEmitters()
        {
            _topVentSmokeLeft = CreateSmokeVent("MainScreen_Vent_Smoke_Left", new Vector3(-3.2f, 3.65f, 0.15f));
            _topVentSmokeRight = CreateSmokeVent("MainScreen_Vent_Smoke_Right", new Vector3(3.2f, 3.65f, 0.15f));
        }

        private ParticleSystem CreateSmokeVent(string goName, Vector3 localPos)
        {
            var ventGo = new GameObject(goName);
            ventGo.transform.SetParent(transform, false);
            ventGo.transform.localPosition = localPos;

            var ps = ventGo.AddComponent<ParticleSystem>();
            var rend = ventGo.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_smokeMat != null) rend.sharedMaterial = _smokeMat;

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 4.2f;
            main.startSpeed = 1.35f;
            main.startSize = 0.55f;
            main.startColor = new Color(0.25f, 0.25f, 0.28f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.2f;
            ventGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(1f, 2.2f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            return ps;
        }

        private void CreateCornerSparkEmitters()
        {
            var sparkGo = new GameObject("MainScreen_Corner_Sparks");
            sparkGo.transform.SetParent(transform, false);
            sparkGo.transform.localPosition = new Vector3(-3.2f, 3.65f, 0.12f);

            _cornerSparks = sparkGo.AddComponent<ParticleSystem>();
            var rend = sparkGo.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            if (_sparkMat != null) rend.sharedMaterial = _sparkMat;

            var main = _cornerSparks.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = 0.18f;
            main.startSpeed = 3.5f;
            main.startSize = 0.08f;
            main.startColor = new Color(1.0f, 0.8f, 0.3f, 1.0f);

            var emission = _cornerSparks.emission;
            emission.rateOverTime = 3f;

            var shape = _cornerSparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
        }

        private void Update()
        {
            if (Time.time >= _nextGlitchTime)
            {
                StartCoroutine(ScreenGlitchFlickerRoutine());
                _nextGlitchTime = Time.time + Random.Range(2.2f, 5.0f);
            }
        }

        private IEnumerator ScreenGlitchFlickerRoutine()
        {
            Vector3 twitch = new Vector3(Random.Range(-0.015f, 0.015f), Random.Range(-0.008f, 0.008f), 0f);
            transform.localPosition = _originalPos + twitch;
            yield return new WaitForSeconds(0.05f);
            transform.localPosition = _originalPos;
        }
    }
}

