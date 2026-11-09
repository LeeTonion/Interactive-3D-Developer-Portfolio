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
        public float triggerDistance = 16.0f;
        public float closeDistance = 22.0f;

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

        public bool IsFullyOpen => _currentOpen >= 0.95f;
        public bool IsFullyClosed => _currentOpen <= 0.05f;
        public float OpenNormalized => _currentOpen;

        private void Awake()
        {
            InitializeDoorReferences();
            CreateStatusIndicators();
            CreateSteamVentEmitters();
        }

        private void Start()
        {
            // Mặc định cửa bắt đầu ở trạng thái đóng kín
            SetOpenImmediate(0f);
        }

        private void InitializeDoorReferences()
        {
            if (doorLeft == null) doorLeft = transform.Find("door_1_left");
            if (doorRight == null) doorRight = transform.Find("door_1_right");
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false; // Tắt animator để không bị kẹp chết ở vị trí hé mở 2.47m
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
            // Luôn áp dụng vị trí cửa tại LateUpdate để đảm bảo không bị bất kỳ Animator nào ghi đè
            ApplyDoorPositions();
        }

        private void Update()
        {
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
            if (unlock) _isLocked = false;
            if (_targetOpen > 0.5f)
            {
                _targetOpen = 0f;
                if (animator != null) animator.SetBool("character_nearby", false);
            }
        }

        public void SetLock(bool locked)
        {
            _isLocked = locked;
        }

        public void SetOpenImmediate(float openNormalized)
        {
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
