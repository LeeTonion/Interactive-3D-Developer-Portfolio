using System.Collections;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Cửa trượt tự động cơ điện Airlock Sci-Fi tại Cổng Tây căn cứ (West Gate Airlock Portal).
    /// Tính năng:
    /// - Điều khiển đồng bộ 2 cánh cửa cơ khí (door_1_left, door_1_right) trượt êm ái sang 2 bên.
    /// - Cập nhật đồng bộ Animator parameter "character_nearby".
    /// - Đèn LED tín hiệu trạng thái chuyển từ Đỏ/Hổ phách (Closed) sang Xanh Cyan (Open).
    /// - Hiệu ứng xả áp suất khí nén (Steam Vent Puff) khi cửa kích hoạt mở.
    /// - Hỗ trợ cả kích hoạt tự động theo khoảng cách (Proximity) và điều khiển qua Script Cinematic Flythrough.
    /// </summary>
    [SelectionBase]
    [ExecuteAlways]
    public class CitadelAutomaticDoor : MonoBehaviour
    {
        [Header("Door Leaves")]
        public Transform doorLeft;
        public Transform doorRight;
        public Animator animator;

        [Header("Motion Settings")]
        public float openSpeed = 2.4f;
        public float closeSpeed = 2.6f;

        // Tọa độ đóng mở chính xác: Khi mở hoàn toàn trượt sâu 3.6m vào trong vách để thông thoáng 100%
        private static readonly Vector3 ClosedLeftPos = new Vector3(1.386f, 0.0057f, 0f);
        private static readonly Vector3 ClosedRightPos = new Vector3(-1.409f, 0f, 0f);
        private static readonly Vector3 OpenLeftPos = new Vector3(3.60f, 0.0057f, 0f);
        private static readonly Vector3 OpenRightPos = new Vector3(-3.60f, 0f, 0f);

        [Header("Proximity Auto-Sense")]
        public bool autoProximity = true;
        public float triggerDistance = 7.0f;
        public float closeDistance = 10.0f;

        [Header("Destructible Gate Settings (Bắn thủng cửa)")]
        [Tooltip("Lượng máu tối đa của cửa căn cứ trước khi bị bắn nổ thủng")]
        public float maxHealth = 220f;
        [Tooltip("Máu hiện tại")]
        public float currentHealth = 220f;
        [Tooltip("Trạng thái cổng đã bị bắn thủng / phá hủy hoàn toàn")]
        public bool isBreached = false;
        public static bool IsWestGateBreached = false;

        public bool IsDestroyed => isBreached;

        [Header("Visual Indicators")]
        public Light statusLight;
        public Renderer statusLedLeft;
        public Renderer statusLedRight;
        public Material cyanLedMat;
        public Material amberLedMat;

        private float _currentOpen = 0f; // 0 = Closed, 1 = Fully Open
        private float _targetOpen = 0f;
        private bool _isLocked = false;
        private float _stayOpenTimer = 0f;
        private ParticleSystem _steamLeft;
        private ParticleSystem _steamRight;
        private ParticleSystem _breachSmoke;
        private ParticleSystem _sparkEmitter;
        private AudioSource _audioSource;
        private AudioClip _impactClip;
        private AudioClip _explosionClip;
        private AudioClip _sparkClip;
        private Coroutine _shakeRoutine;

        public bool IsFullyOpen => _currentOpen >= 0.95f;
        public bool IsFullyClosed => _currentOpen <= 0.05f;
        public float OpenNormalized => _currentOpen;

        private void Awake()
        {
            currentHealth = maxHealth;
            InitializeDoorReferences();
            if (Application.isPlaying)
            {
                BuildProceduralAudio();
                CreateStatusIndicators();
                CreateSteamVentEmitters();
            }
            else
            {
                if (!isBreached)
                {
                    SetOpenImmediate(0f);
                }
            }
        }

        private void OnEnable()
        {
            InitializeDoorReferences();
            if (!Application.isPlaying && !isBreached)
            {
                SetOpenImmediate(0f);
            }
        }

        private void OnValidate()
        {
            InitializeDoorReferences();
            if (!Application.isPlaying && !isBreached)
            {
                ApplyDoorPositions();
            }
        }

        private void Start()
        {
            if (isBreached || IsWestGateBreached)
            {
                TriggerCatastrophicBreach(instant: true);
            }
            else
            {
                // Mặc định cửa bắt đầu ở trạng thái đóng kín
                SetOpenImmediate(0f);
            }
        }

        private void BuildProceduralAudio()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0.85f;
            _audioSource.playOnAwake = false;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = 3f;
            _audioSource.maxDistance = 60f;

            _impactClip = CreateImpactTone(220f, 0.18f, 0.45f);
            _explosionClip = CreateBreachExplosionClip(1.4f);
            _sparkClip = CreateElectricSparkClip(0.12f);
        }

        private static AudioClip CreateImpactTone(float freq, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 18f);
                float noise = UnityEngine.Random.Range(-0.4f, 0.4f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = (tone * 0.6f + noise * 0.4f) * decay * volume;
            }
            var clip = AudioClip.Create("Gate_Hit_Clang", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateBreachExplosionClip(float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 3.8f);
                float noise = UnityEngine.Random.Range(-1f, 1f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 32f, t / duration) * t);
                data[i] = (sub * 0.65f + noise * 0.55f) * decay * 0.75f;
            }
            var clip = AudioClip.Create("Gate_Breach_Explosion", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateElectricSparkClip(float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float noise = UnityEngine.Random.Range(-0.8f, 0.8f);
                float decay = 1f - ((float)i / samples);
                data[i] = noise * decay * 0.35f;
            }
            var clip = AudioClip.Create("Gate_Electric_Spark", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void InitializeDoorReferences()
        {
            if (doorLeft == null) doorLeft = transform.Find("door_1_left");
            if (doorRight == null) doorRight = transform.Find("door_1_right");
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false; // Tắt animator để không bị kẹp chết ở vị trí hé mở 2.47m

            if (!Application.isPlaying && !isBreached)
            {
                if (doorLeft != null) doorLeft.localPosition = ClosedLeftPos;
                if (doorRight != null) doorRight.localPosition = ClosedRightPos;
            }

            if (Application.isPlaying)
            {
                // Đảm bảo cả 2 cánh cửa cơ khí đều có Collider để đạn Laser bắn trúng
                EnsureDoorCollider(doorLeft, new Vector3(2.4f, 3.8f, 0.5f), new Vector3(0f, 1.8f, 0f));
                EnsureDoorCollider(doorRight, new Vector3(2.4f, 3.8f, 0.5f), new Vector3(0f, 1.8f, 0f));

                // Collider bao trùm toàn bộ vòm cổng để nhận sát thương đạn pháo
                var portalCol = GetComponent<BoxCollider>();
                if (portalCol == null)
                {
                    portalCol = gameObject.AddComponent<BoxCollider>();
                    portalCol.size = new Vector3(4.8f, 4.4f, 0.9f);
                    portalCol.center = new Vector3(0f, 2.0f, 0f);
                }
            }
        }

        private void EnsureDoorCollider(Transform doorTr, Vector3 size, Vector3 center)
        {
            if (doorTr == null) return;
            var col = doorTr.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = doorTr.gameObject.AddComponent<BoxCollider>();
                col.size = size;
                col.center = center;
            }
        }

        private void CreateStatusIndicators()
        {
            // Đèn rọi vòm cửa
            if (statusLight == null)
            {
                var ltObj = new GameObject("Door_Portal_StatusLight");
                ltObj.transform.SetParent(transform, false);
                ltObj.transform.localPosition = new Vector3(0f, 3.8f, -0.4f);
                statusLight = ltObj.AddComponent<Light>();
                statusLight.type = LightType.Point;
                statusLight.range = 8.0f;
                statusLight.intensity = 1.8f;
                statusLight.color = new Color(1.0f, 0.4f, 0.1f);
            }

            // Đèn LED chỉ báo trạng thái 2 bên khung cửa
            if (amberLedMat == null)
            {
#if UNITY_EDITOR
                amberLedMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_CoreReactor.mat");
#endif
            }
            if (cyanLedMat == null)
            {
#if UNITY_EDITOR
                cyanLedMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_BlueCyan.mat");
#endif
            }

            if (statusLedLeft == null)
            {
                var ledL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledL.name = "Status_LED_Left";
                ledL.transform.SetParent(transform, false);
                ledL.transform.localPosition = new Vector3(1.85f, 2.1f, -0.25f);
                ledL.transform.localScale = new Vector3(0.12f, 0.6f, 0.08f);
                DestroyImmediate(ledL.GetComponent<Collider>());
                statusLedLeft = ledL.GetComponent<Renderer>();
                if (amberLedMat != null) statusLedLeft.sharedMaterial = amberLedMat;
            }

            if (statusLedRight == null)
            {
                var ledR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledR.name = "Status_LED_Right";
                ledR.transform.SetParent(transform, false);
                ledR.transform.localPosition = new Vector3(-1.85f, 2.1f, -0.25f);
                ledR.transform.localScale = new Vector3(0.12f, 0.6f, 0.08f);
                DestroyImmediate(ledR.GetComponent<Collider>());
                statusLedRight = ledR.GetComponent<Renderer>();
                if (amberLedMat != null) statusLedRight.sharedMaterial = amberLedMat;
            }
        }

        private void CreateSteamVentEmitters()
        {
            _steamLeft = CreateSteamEmitter("SteamVent_Left", new Vector3(1.4f, 0.4f, -0.1f));
            _steamRight = CreateSteamEmitter("SteamVent_Right", new Vector3(-1.4f, 0.4f, -0.1f));
        }

        private ParticleSystem CreateSteamEmitter(string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.45f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new Color(0.8f, 0.95f, 1f, 0.45f);

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.1f;

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
#if UNITY_EDITOR
            var smokeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Cosmic_Smoke.mat");
            if (smokeMat != null) rend.sharedMaterial = smokeMat;
#endif

            return ps;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                if (!isBreached && _targetOpen <= 0.01f)
                {
                    ApplyDoorPositions();
                }
                return;
            }

            if (isBreached)
            {
                // Cửa đã bị bắn nổ thủng: Giữ nguyên vị trí biến dạng toác mở vĩnh viễn
                if (doorLeft != null)
                {
                    doorLeft.localPosition = new Vector3(3.90f, 0.0057f, 0.15f);
                    doorLeft.localRotation = Quaternion.Euler(0f, 0f, -4.5f);
                }
                if (doorRight != null)
                {
                    doorRight.localPosition = new Vector3(-3.90f, 0f, 0.15f);
                    doorRight.localRotation = Quaternion.Euler(0f, 0f, 5.2f);
                }
                return;
            }

            // Luôn áp dụng vị trí cửa tại LateUpdate để đảm bảo không bị bất kỳ Animator nào ghi đè
            ApplyDoorPositions();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (isBreached)
            {
                // Đèn báo động khẩn cấp chập chờn màu đỏ sau khi cửa bị phá hủy
                if (statusLight != null)
                {
                    float flicker = (Time.frameCount % 8 < 4) ? 3.5f : 0.8f;
                    statusLight.color = new Color(1.0f, 0.08f, 0.05f);
                    statusLight.intensity = flicker;
                }
                return;
            }

            // Kiểm tra proximity tự động khi không bị khóa điều khiển bởi cinematic
            if (autoProximity && !_isLocked)
            {
                CheckProximitySensors();
            }

            // Chuyển động cơ khí mượt mà
            float speed = (_targetOpen > _currentOpen) ? openSpeed : closeSpeed;
            if (!Mathf.Approximately(_currentOpen, _targetOpen))
            {
                _currentOpen = Mathf.MoveTowards(_currentOpen, _targetOpen, Time.deltaTime * speed);
                ApplyDoorPositions();
                UpdateVisualStatus();
            }
        }

        /// <summary>
        /// Nhận sát thương khi bị tàu vũ trụ của người chơi hoặc các vũ khí bắn trúng
        /// </summary>
        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 shotDir)
        {
            if (isBreached) return;

            currentHealth -= damage;

            // Âm thanh kim loại va chạm
            if (_audioSource != null && _impactClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                _audioSource.PlayOneShot(_impactClip, 0.65f);
            }

            // Rung chấn cửa khi trúng đạn
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(DoImpactShake(shotDir));

            // Hiệu ứng tia lửa xả ra từ khe cửa
            TriggerSteamPuff();

            // Nếu máu giảm dưới 50%: Phát tia lửa điện cảnh báo
            if (currentHealth <= maxHealth * 0.5f)
            {
                if (statusLight != null)
                {
                    statusLight.color = new Color(1.0f, 0.35f, 0.1f);
                    statusLight.intensity = 3.0f;
                }
            }

            // Khi máu cạn kiệt: NỔ TUNG VÀ BẮN THỦNG CỬA HOÀN TOÀN
            if (currentHealth <= 0f)
            {
                TriggerCatastrophicBreach();
            }
        }

        private IEnumerator DoImpactShake(Vector3 shotDir)
        {
            Vector3 localShake = transform.InverseTransformDirection(shotDir).normalized * 0.08f;
            float elapsed = 0f;
            float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                float sin = Mathf.Sin(elapsed * 45f) * damp;
                if (doorLeft != null) doorLeft.localPosition += localShake * sin;
                if (doorRight != null) doorRight.localPosition += localShake * sin;
                yield return null;
            }
            ApplyDoorPositions();
        }

        /// <summary>
        /// Kích hoạt nổ tung, bắn thủng và toác mở vĩnh viễn vòm cửa căn cứ
        /// </summary>
        public void TriggerCatastrophicBreach(bool instant = false)
        {
            if (isBreached && !instant) return;

            isBreached = true;
            IsWestGateBreached = true;
            _isLocked = true;
            autoProximity = false;
            _currentOpen = 1f;
            _targetOpen = 1f;

            // Biến dạng cánh cửa toác sang 2 bên
            if (doorLeft != null)
            {
                doorLeft.localPosition = new Vector3(3.90f, 0.0057f, 0.15f);
                doorLeft.localRotation = Quaternion.Euler(0f, 0f, -4.5f);
            }
            if (doorRight != null)
            {
                doorRight.localPosition = new Vector3(-3.90f, 0f, 0.15f);
                doorRight.localRotation = Quaternion.Euler(0f, 0f, 5.2f);
            }

            // Đèn vòm chuyển sang báo động đỏ
            if (statusLight != null)
            {
                statusLight.color = new Color(1.0f, 0.08f, 0.05f);
                statusLight.intensity = 3.5f;
            }

            // Bật khói và tia lửa điện chập chờn liên tục
            CreateBreachContinuousSmokeAndSparks();

            if (!instant)
            {
                // Âm thanh nổ vang rền
                if (_audioSource != null && _explosionClip != null)
                {
                    _audioSource.pitch = 0.95f;
                    _audioSource.PlayOneShot(_explosionClip, 1.0f);
                }

                // Tạo các mảnh vỡ kim loại nóng chảy văng tung tóe ra đường băng
                SpawnFieryDoorDebris();
            }

            // Ghi đè trạng thái bàn phím mật mã ngoài cửa: Báo Breach Override, không cần nhập mã
            var keypad = CitadelKeypadTerminal.Instance ?? FindFirstObjectByType<CitadelKeypadTerminal>();
            if (keypad != null)
            {
                keypad.TriggerTerminalBreachOverride();
            }
        }

        private void CreateBreachContinuousSmokeAndSparks()
        {
            if (_breachSmoke == null)
            {
                var smokeGo = new GameObject("Gate_Breach_Smoke");
                smokeGo.transform.SetParent(transform, false);
                smokeGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);

                _breachSmoke = smokeGo.AddComponent<ParticleSystem>();
                var main = _breachSmoke.main;
                main.playOnAwake = true;
                main.loop = true;
                main.duration = 1.0f;
                main.startLifetime = 1.8f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
                main.startColor = new Color(0.2f, 0.22f, 0.25f, 0.65f);

                var emission = _breachSmoke.emission;
                emission.rateOverTime = 22f;

                var shape = _breachSmoke.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(3.2f, 2.0f, 0.5f);

                var rend = _breachSmoke.GetComponent<ParticleSystemRenderer>();
                rend.renderMode = ParticleSystemRenderMode.Billboard;
#if UNITY_EDITOR
                var smokeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Cosmic_Smoke.mat");
                if (smokeMat != null) rend.sharedMaterial = smokeMat;
#endif
            }

            if (_sparkEmitter == null)
            {
                StartCoroutine(BreachElectricSparkingRoutine());
            }
        }

        private IEnumerator BreachElectricSparkingRoutine()
        {
            while (isBreached)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.6f, 1.8f));
                if (!isBreached) yield break;

                // Phát tia lửa điện chập giật
                if (_steamLeft != null) _steamLeft.Emit(UnityEngine.Random.Range(4, 10));
                if (_audioSource != null && _sparkClip != null)
                {
                    _audioSource.pitch = UnityEngine.Random.Range(1.1f, 1.6f);
                    _audioSource.PlayOneShot(_sparkClip, 0.35f);
                }
            }
        }

        private void SpawnFieryDoorDebris()
        {
            Vector3 blastCenter = transform.position + Vector3.up * 2.0f;
            for (int i = 0; i < 14; i++)
            {
                var debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.name = "Door_Breached_Shrapnel";
                debris.transform.position = blastCenter + UnityEngine.Random.insideUnitSphere * 0.8f;
                debris.transform.localScale = new Vector3(
                    UnityEngine.Random.Range(0.12f, 0.35f),
                    UnityEngine.Random.Range(0.12f, 0.35f),
                    UnityEngine.Random.Range(0.08f, 0.25f)
                );

                var mr = debris.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.EnableKeyword("_EMISSION");
                    mat.color = new Color(0.2f, 0.2f, 0.2f);
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.35f, 0.05f) * 2.5f);
                    mr.material = mat;
                }

                var rb = debris.AddComponent<Rigidbody>();
                rb.mass = 2.5f;
                Vector3 blastDir = (transform.forward * UnityEngine.Random.Range(-0.8f, 1.2f) +
                                    transform.right * UnityEngine.Random.Range(-1.5f, 1.5f) +
                                    Vector3.up * UnityEngine.Random.Range(0.8f, 2.5f)).normalized;
                rb.linearVelocity = blastDir * UnityEngine.Random.Range(8f, 18f);
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 20f;

                Destroy(debris, UnityEngine.Random.Range(4.0f, 7.0f));
            }
        }

        private void CheckProximitySensors()
        {
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
            float distToCam = cam != null ? Vector3.Distance(transform.position, camPos) : 999f;

            // Tìm tàu người chơi nếu có
            var ship = FindFirstObjectByType<PlayerSpaceshipController>();
            float distToShip = ship != null ? Vector3.Distance(transform.position, ship.transform.position) : 999f;

            float minDist = Mathf.Min(distToCam, distToShip);

            if (minDist <= triggerDistance)
            {
                _stayOpenTimer = 2.5f;
                if (_targetOpen < 0.5f) OpenDoor();
            }
            else if (minDist >= closeDistance)
            {
                if (_stayOpenTimer > 0f)
                {
                    _stayOpenTimer -= Time.deltaTime;
                    if (_stayOpenTimer <= 0f && _targetOpen > 0.5f)
                    {
                        CloseDoor();
                    }
                }
            }
        }

        public void OpenDoor(bool locked = false)
        {
            if (isBreached) return;
            _isLocked = locked;
            if (_targetOpen < 0.5f)
            {
                _targetOpen = 1f;
                TriggerSteamPuff();
                if (animator != null) animator.SetBool("character_nearby", true);
            }
        }

        public void CloseDoor(bool unlock = true)
        {
            if (isBreached) return;
            if (unlock) _isLocked = false;
            if (_targetOpen > 0.5f)
            {
                _targetOpen = 0f;
                if (animator != null) animator.SetBool("character_nearby", false);
            }
        }

        public void SetLock(bool locked)
        {
            if (isBreached) return;
            _isLocked = locked;
        }

        public void SetOpenImmediate(float openNormalized)
        {
            if (isBreached) return;
            _currentOpen = Mathf.Clamp01(openNormalized);
            _targetOpen = _currentOpen;
            ApplyDoorPositions();
            UpdateVisualStatus();
            if (animator != null) animator.SetBool("character_nearby", _currentOpen > 0.5f);
        }

        private void ApplyDoorPositions()
        {
            float t = Mathf.SmoothStep(0f, 1f, _currentOpen);
            if (doorLeft != null)
            {
                doorLeft.localPosition = Vector3.Lerp(ClosedLeftPos, OpenLeftPos, t);
            }
            if (doorRight != null)
            {
                doorRight.localPosition = Vector3.Lerp(ClosedRightPos, OpenRightPos, t);
            }
        }

        private void TriggerSteamPuff()
        {
            if (_steamLeft != null) _steamLeft.Emit(14);
            if (_steamRight != null) _steamRight.Emit(14);
        }

        private void UpdateVisualStatus()
        {
            bool isOpen = _currentOpen > 0.25f;
            Color targetColor = isOpen ? new Color(0.1f, 0.95f, 1.0f) : new Color(1.0f, 0.45f, 0.1f);
            float lightIntensity = isOpen ? 2.4f : 1.2f;

            if (statusLight != null)
            {
                statusLight.color = Color.Lerp(statusLight.color, targetColor, Time.deltaTime * 6f);
                statusLight.intensity = Mathf.Lerp(statusLight.intensity, lightIntensity, Time.deltaTime * 6f);
            }

            // Cập nhật phát quang cho 2 dải LED
            Material activeMat = isOpen ? cyanLedMat : amberLedMat;

            if (statusLedLeft != null && activeMat != null && statusLedLeft.sharedMaterial != activeMat)
            {
                statusLedLeft.sharedMaterial = activeMat;
            }
            if (statusLedRight != null && activeMat != null && statusLedRight.sharedMaterial != activeMat)
            {
                statusLedRight.sharedMaterial = activeMat;
            }
        }
    }
}
