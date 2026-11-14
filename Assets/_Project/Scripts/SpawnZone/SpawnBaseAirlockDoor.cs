using System.Collections;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Cánh cửa trượt tự động cơ điện Airlock Sci-Fi tại cửa căn cứ xuất phát (Hangar Aft Door - Spawn Zone).
    /// Thiết kế chuẩn cơ khí tương đồng 100% với West Gate Airlock Portal của Citadel:
    /// - Điều khiển đồng bộ 2 cánh cửa cơ khí (door_1_left, door_1_right) trượt êm ái sang 2 bên vào trong khung vách.
    /// - Đèn LED tín hiệu trạng thái chuyển từ Đỏ/Hổ phách (Closed) sang Xanh Cyan rực rỡ (Open).
    /// - Hiệu ứng xả áp suất khí nén (Steam Vent Puff) và đèn rọi vòm cửa khi kích hoạt mở.
    /// - Âm thanh thủ tục cơ điện Sci-Fi (Pneumatic Hiss & Servo Whoosh).
    /// - Tích hợp chặt chẽ với SpawnZoneAnimator trong hoạt cảnh bước chân ra khỏi căn cứ (WalkingFromBase).
    /// - Hỗ trợ cả tự động nhận diện khoảng cách (Proximity Sensing) khi người chơi hoặc camera lại gần.
    /// </summary>
    [SelectionBase]
    [ExecuteAlways]
    public class SpawnBaseAirlockDoor : MonoBehaviour
    {
        [Header("Door Leaves")]
        public Transform doorLeft;
        public Transform doorRight;
        public Animator animator;

        [Header("Motion Speeds")]
        public float openSpeed = 2.5f;
        public float closeSpeed = 2.2f;

        // Tọa độ đóng mở chính xác theo mesh FBX frame_door_1
        private static readonly Vector3 ClosedLeftPos = new Vector3(1.386f, 0.0057f, 0f);
        private static readonly Vector3 ClosedRightPos = new Vector3(-1.409f, 0f, 0f);
        private static readonly Vector3 OpenLeftPos = new Vector3(3.60f, 0.0057f, 0f);
        private static readonly Vector3 OpenRightPos = new Vector3(-3.60f, 0f, 0f);

        [Header("Proximity Auto-Sense")]
        public bool autoProximity = true;
        public float triggerDistance = 8.5f;
        public float closeDistance = 12.0f;

        [Header("Visual Indicators")]
        public Light statusLight;
        public Renderer statusLedLeft;
        public Renderer statusLedRight;
        public Material cyanLedMat;
        public Material amberLedMat;

        [Header("Current State")]
        [Range(0f, 1f)]
        public float openRatio = 0f; // 0 = Closed, 1 = Fully Open

        private float _currentOpen = 0f;
        private float _targetOpen = 0f;
        private bool _isLocked = false;
        private float _stayOpenTimer = 0f;

        private ParticleSystem _steamLeft;
        private ParticleSystem _steamRight;
        private AudioSource _audioSource;
        private AudioClip _doorOpenClip;
        private AudioClip _doorCloseClip;

        public bool IsFullyOpen => _currentOpen >= 0.95f;
        public bool IsFullyClosed => _currentOpen <= 0.05f;
        public float OpenNormalized => _currentOpen;

        private void Awake()
        {
            InitializeDoorReferences();
            if (Application.isPlaying)
            {
                BuildProceduralAudio();
                CreateStatusIndicators();
                CreateSteamVentEmitters();
                SetOpenImmediate(0f);
            }
            else
            {
                SetOpenImmediate(0f);
            }
        }

        private void OnEnable()
        {
            InitializeDoorReferences();
            if (!Application.isPlaying)
            {
                SetOpenImmediate(0f);
            }
        }

        private void OnValidate()
        {
            InitializeDoorReferences();
            if (!Application.isPlaying)
            {
                ApplyDoorPositions();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                SetOpenImmediate(0f);
            }
        }

        private void InitializeDoorReferences()
        {
            if (doorLeft == null) doorLeft = transform.Find("door_1_left");
            if (doorRight == null) doorRight = transform.Find("door_1_right");
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false; // Tắt animator để không bị kẹt cứng ở vị trí 2.47m

            if (!Application.isPlaying)
            {
                if (doorLeft != null) doorLeft.localPosition = Vector3.Lerp(ClosedLeftPos, OpenLeftPos, openRatio);
                if (doorRight != null) doorRight.localPosition = Vector3.Lerp(ClosedRightPos, OpenRightPos, openRatio);
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
            // 1. Đèn rọi vòm cửa
            if (statusLight == null)
            {
                var ltObj = new GameObject("AftDoor_StatusLight");
                ltObj.transform.SetParent(transform, false);
                ltObj.transform.localPosition = new Vector3(0f, 3.8f, -0.4f);
                statusLight = ltObj.AddComponent<Light>();
                statusLight.type = LightType.Point;
                statusLight.range = 9.0f;
                statusLight.intensity = 1.9f;
                statusLight.color = new Color(1.0f, 0.42f, 0.1f);
            }

            // 2. Load materials LED
            if (amberLedMat == null)
            {
#if UNITY_EDITOR
                amberLedMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_CoreReactor.mat");
#endif
            }
            if (cyanLedMat == null)
            {
#if UNITY_EDITOR
                cyanLedMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Gemstone_BlueCyan.mat");
#endif
            }

            // 3. LED chỉ báo trạng thái 2 bên khung cửa
            if (statusLedLeft == null)
            {
                var ledL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledL.name = "AftDoor_LED_Left";
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
                ledR.name = "AftDoor_LED_Right";
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
            _steamLeft = CreateSteamEmitter("AftSteamVent_Left", new Vector3(1.4f, 0.4f, -0.1f));
            _steamRight = CreateSteamEmitter("AftSteamVent_Right", new Vector3(-1.4f, 0.4f, -0.1f));
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
            var smokeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/EnvironmentAndSpace/Mat_Cosmic_Smoke.mat");
            if (smokeMat != null) rend.sharedMaterial = smokeMat;
#endif
            return ps;
        }

        private void BuildProceduralAudio()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0.85f;
            _audioSource.playOnAwake = false;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = 3f;
            _audioSource.maxDistance = 55f;

            _doorOpenClip = CreatePneumaticHissClip(1.4f, 0.55f);
            _doorCloseClip = CreatePneumaticCloseClip(1.2f, 0.50f);
        }

        private static AudioClip CreatePneumaticHissClip(float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 2.8f);
                float noise = UnityEngine.Random.Range(-0.5f, 0.5f);
                float servo = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 340f, t / duration) * t) * 0.35f;
                data[i] = (noise * 0.65f + servo) * decay * volume;
            }
            var clip = AudioClip.Create("AftDoor_Pneumatic_Open", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePneumaticCloseClip(float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 3.5f);
                float noise = UnityEngine.Random.Range(-0.4f, 0.4f);
                float latch = (t > duration * 0.75f) ? (Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.8f) : 0f;
                data[i] = (noise * 0.5f + latch * 0.5f) * decay * volume;
            }
            var clip = AudioClip.Create("AftDoor_Pneumatic_Close", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                ApplyDoorPositions();
                return;
            }

            // 1. Proximity Auto-Sense (nếu không bị khóa từ kịch bản Onboarding)
            if (autoProximity && !_isLocked)
            {
                CheckProximitySensors();
            }

            // 2. Stay open timer
            if (_stayOpenTimer > 0f)
            {
                _stayOpenTimer -= Time.deltaTime;
                if (_stayOpenTimer <= 0f && !_isLocked)
                {
                    _targetOpen = 0f;
                }
            }

            // 3. Interpolation mở/đóng cửa
            float speed = (_targetOpen > _currentOpen) ? openSpeed : closeSpeed;
            if (Mathf.Abs(_currentOpen - _targetOpen) > 0.001f)
            {
                _currentOpen = Mathf.MoveTowards(_currentOpen, _targetOpen, speed * Time.deltaTime);
                openRatio = _currentOpen;
                ApplyDoorPositions();
                UpdateStatusVisuals();
            }
        }

        private void CheckProximitySensors()
        {
            Vector3 checkPos = Vector3.zero;
            bool hasTarget = false;

            var cam = Camera.main;
            if (cam != null)
            {
                checkPos = cam.transform.position;
                hasTarget = true;
            }

            var ship = FindFirstObjectByType<PlayerSpaceshipController>();
            if (ship != null)
            {
                float distShip = Vector3.Distance(transform.position, ship.transform.position);
                if (!hasTarget || distShip < Vector3.Distance(transform.position, checkPos))
                {
                    checkPos = ship.transform.position;
                    hasTarget = true;
                }
            }

            if (!hasTarget) return;

            float dist = Vector3.Distance(transform.position, checkPos);
            if (dist <= triggerDistance)
            {
                OpenDoor(holdSeconds: 3.5f);
            }
            else if (dist > closeDistance && _stayOpenTimer <= 0f)
            {
                CloseDoor();
            }
        }

        public void OpenDoor(float holdSeconds = 4.0f)
        {
            if (_targetOpen < 0.99f)
            {
                _targetOpen = 1.0f;
                PlayOpenEffects();
            }
            _stayOpenTimer = Mathf.Max(_stayOpenTimer, holdSeconds);
        }

        public void CloseDoor()
        {
            if (_targetOpen > 0.01f)
            {
                _targetOpen = 0f;
                _stayOpenTimer = 0f;
                PlayCloseEffects();
            }
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        public void SetOpenImmediate(float ratio)
        {
            _currentOpen = Mathf.Clamp01(ratio);
            _targetOpen = _currentOpen;
            openRatio = _currentOpen;
            ApplyDoorPositions();
            UpdateStatusVisuals();
        }

        private void PlayOpenEffects()
        {
            if (_steamLeft != null) _steamLeft.Play();
            if (_steamRight != null) _steamRight.Play();
            if (_audioSource != null && _doorOpenClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                _audioSource.PlayOneShot(_doorOpenClip, 0.75f);
            }
        }

        private void PlayCloseEffects()
        {
            if (_audioSource != null && _doorCloseClip != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.02f);
                _audioSource.PlayOneShot(_doorCloseClip, 0.65f);
            }
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

        private void UpdateStatusVisuals()
        {
            bool isOpen = _currentOpen > 0.35f;

            if (statusLight != null)
            {
                Color targetCol = isOpen ? new Color(0.15f, 0.95f, 1.0f) : new Color(1.0f, 0.42f, 0.1f);
                statusLight.color = Color.Lerp(statusLight.color, targetCol, Time.deltaTime * 6f);
            }

            Material targetMat = isOpen ? cyanLedMat : amberLedMat;
            if (targetMat != null)
            {
                if (statusLedLeft != null && statusLedLeft.sharedMaterial != targetMat)
                    statusLedLeft.sharedMaterial = targetMat;
                if (statusLedRight != null && statusLedRight.sharedMaterial != targetMat)
                    statusLedRight.sharedMaterial = targetMat;
            }
        }
    }
}
