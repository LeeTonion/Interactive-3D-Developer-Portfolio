using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Bản Đồ Radar Tác Chiến Sci-Fi (Minimap Compass Radar - Theo Thiết Kế Ảnh 1):
    /// - Khung tròn la bàn với 4 hướng chính (N, E, S, W) và vạch chia tọa độ.
    /// - Quạt quét tầm nhìn (Vision Cone) xoay đồng bộ theo hướng mũi tàu.
    /// - Icon phi thuyền người chơi (Player Arrow) chỉ hướng ở chính giữa tâm radar.
    /// - Thể hiện các hòn đảo bay (Floating Islands) dưới dạng mảng sinh thái xanh lam/cyan hữu cơ.
    /// - Thể hiện các phi thuyền giao thông/địch (Traffic/Enemy Ships) dưới dạng mũi tên đỏ xoay theo hướng bay thực tế.
    /// - Viền đỏ cong (Corner Vignette) ở góc trên bên trái tạo cảm ứng buồng lái chiến đấu chân thực.
    /// </summary>
    [ExecuteAlways]
    public class SciFiRadarMinimap : MonoBehaviour
    {
        [Header("Cấu Hình Bán Kính Radar (Radar Range)")]
        [Tooltip("Bán kính không gian thực tế được quét trên Radar (mét)")]
        public float radarWorldRadius = 450f;
        [Tooltip("Bán kính hiển thị trên giao diện UI (pixel)")]
        public float radarUiRadius = 82f;
        [Tooltip("Tọa độ neo góc trên trái màn hình")]
        public Vector2 radarAnchoredPosition = new Vector2(130f, -130f);
        [Tooltip("Kích thước đường kính radar")]
        public float radarSize = 205f;

        [Header("Màu Sắc Các Thực Thể Trên Radar")]
        public Color islandColor = new Color(0.28f, 0.72f, 0.82f, 0.95f);
        public Color shipBlipColor = new Color(1f, 0.22f, 0.26f, 0.98f);
        public Color playerArrowColor = new Color(0.85f, 0.98f, 1f, 1f);

        private GameObject _radarRoot;
        private RectTransform _radarRt;
        private RectTransform _visionConeRt;
        private RectTransform _playerArrowRt;
        private GameObject _cornerVignetteGo;

        private Transform _islandsContainer;
        private Transform _shipsContainer;

        private readonly List<IslandRadarBlip> _islandBlips = new List<IslandRadarBlip>();
        private readonly List<RectTransform> _shipBlipPool = new List<RectTransform>();
        private const int MaxShipBlips = 16;

        private Sprite _ringSprite;
        private Sprite _visionConeSprite;
        private Sprite _islandBlobSprite;
        private Sprite _shipBlipSprite;
        private Sprite _playerArrowSprite;
        private Sprite _cornerVignetteSprite;

        private class IslandRadarBlip
        {
            public Transform WorldTransform;
            public RectTransform UiRect;
            public float BaseRadius;
        }

        private void Start()
        {
            LoadSprites();
            EnsureRadarUI();
            CacheWorldIslands();
        }

        private void OnEnable()
        {
            LoadSprites();
            EnsureRadarUI();
            CacheWorldIslands();
        }

        private void LoadSprites()
        {
            if (_ringSprite == null) _ringSprite = LoadSprite("Assets/New/Textures/Tex_Radar_Ring.png");
            if (_visionConeSprite == null) _visionConeSprite = LoadSprite("Assets/New/Textures/Tex_Radar_Vision_Cone.png");
            if (_islandBlobSprite == null) _islandBlobSprite = LoadSprite("Assets/New/Textures/Tex_Radar_Island_Blob.png");
            if (_shipBlipSprite == null) _shipBlipSprite = LoadSprite("Assets/New/Textures/Tex_Radar_Ship_Blip.png");
            if (_playerArrowSprite == null) _playerArrowSprite = LoadSprite("Assets/New/Textures/Tex_Radar_Player_Arrow.png");
            if (_cornerVignetteSprite == null) _cornerVignetteSprite = LoadSprite("Assets/New/Textures/Tex_Radar_Corner_Vignette.png");
        }

        private Sprite LoadSprite(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        public void EnsureRadarUI()
        {
            LoadSprites();

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            Transform parent = canvas.transform.Find("PlayerSciFiHUD");
            if (parent == null) parent = canvas.transform;

            if (_radarRoot != null && _radarRoot.transform.parent != parent)
            {
                DestroyImmediate(_radarRoot);
                _radarRoot = null;
            }

            // 1. Viền vệt đỏ mờ ở góc buồng lái (Corner Red Vignette)
            if (_cornerVignetteSprite != null && _cornerVignetteGo == null)
            {
                var existingVign = parent.Find("CornerVignette");
                if (existingVign != null)
                {
                    _cornerVignetteGo = existingVign.gameObject;
                }
                else
                {
                    _cornerVignetteGo = new GameObject("CornerVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    _cornerVignetteGo.transform.SetParent(parent, false);
                    _cornerVignetteGo.transform.SetAsFirstSibling();
                    var vignRt = _cornerVignetteGo.GetComponent<RectTransform>();
                    vignRt.anchorMin = new Vector2(0f, 1f);
                    vignRt.anchorMax = new Vector2(0f, 1f);
                    vignRt.pivot = new Vector2(0f, 1f);
                    vignRt.anchoredPosition = Vector2.zero;
                    vignRt.sizeDelta = new Vector2(320f, 320f);
                    var img = _cornerVignetteGo.GetComponent<Image>();
                    img.sprite = _cornerVignetteSprite;
                    img.color = Color.white;
                    img.raycastTarget = false;
                }
            }

            if (_radarRoot != null && _shipBlipPool.Count > 0 && _islandsContainer != null) return;

            var existing = parent.Find("RadarMinimap_Root");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            // 2. Gốc Root của Radar Minimap (Neo góc trên bên trái)
            _radarRoot = new GameObject("RadarMinimap_Root", typeof(RectTransform));
            _radarRoot.transform.SetParent(parent, false);
            _radarRt = _radarRoot.GetComponent<RectTransform>();
            _radarRt.anchorMin = new Vector2(0f, 1f);
            _radarRt.anchorMax = new Vector2(0f, 1f);
            _radarRt.pivot = new Vector2(0.5f, 0.5f);
            _radarRt.anchoredPosition = radarAnchoredPosition;
            _radarRt.sizeDelta = new Vector2(radarSize, radarSize);

            // 3. Khung đĩa và vòng tròn la bàn (Compass Ring & Scope)
            var ringGo = new GameObject("RadarRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringGo.transform.SetParent(_radarRoot.transform, false);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.sizeDelta = new Vector2(radarSize, radarSize);
            ringRt.anchoredPosition = Vector2.zero;
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = _ringSprite;
            ringImg.color = Color.white;
            ringImg.raycastTarget = false;

            // 4. Nhãn 4 hướng la bàn (N, E, S, W)
            CreateCompassLabel("Label_N", _radarRoot.transform, "N", new Vector2(0f, 85f));
            CreateCompassLabel("Label_E", _radarRoot.transform, "E", new Vector2(85f, 0f));
            CreateCompassLabel("Label_S", _radarRoot.transform, "S", new Vector2(0f, -85f));
            CreateCompassLabel("Label_W", _radarRoot.transform, "W", new Vector2(-85f, 0f));

            // 5. Container cho các hòn đảo bay (Islands)
            var islandsGo = new GameObject("IslandsContainer", typeof(RectTransform));
            islandsGo.transform.SetParent(_radarRoot.transform, false);
            _islandsContainer = islandsGo.transform;

            // 6. Quạt quét tầm nhìn (Vision Cone)
            var coneGo = new GameObject("VisionCone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            coneGo.transform.SetParent(_radarRoot.transform, false);
            _visionConeRt = coneGo.GetComponent<RectTransform>();
            _visionConeRt.sizeDelta = new Vector2(radarSize * 0.95f, radarSize * 0.95f);
            _visionConeRt.anchoredPosition = Vector2.zero;
            var coneImg = coneGo.GetComponent<Image>();
            coneImg.sprite = _visionConeSprite;
            coneImg.color = Color.white;
            coneImg.raycastTarget = false;

            // 7. Container cho các phi thuyền giao thông/địch (Ships)
            var shipsGo = new GameObject("ShipsContainer", typeof(RectTransform));
            shipsGo.transform.SetParent(_radarRoot.transform, false);
            _shipsContainer = shipsGo.transform;

            _shipBlipPool.Clear();
            for (int i = 0; i < MaxShipBlips; i++)
            {
                var blipGo = new GameObject("ShipBlip_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                blipGo.transform.SetParent(_shipsContainer, false);
                var blipRt = blipGo.GetComponent<RectTransform>();
                blipRt.sizeDelta = new Vector2(13f, 13f);
                var blipImg = blipGo.GetComponent<Image>();
                blipImg.sprite = _shipBlipSprite;
                blipImg.color = shipBlipColor;
                blipImg.raycastTarget = false;
                blipGo.SetActive(false);
                _shipBlipPool.Add(blipRt);
            }

            // 8. Mũi tên phi thuyền người chơi ở chính giữa tâm (Player Arrow)
            var arrowGo = new GameObject("PlayerArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            arrowGo.transform.SetParent(_radarRoot.transform, false);
            _playerArrowRt = arrowGo.GetComponent<RectTransform>();
            _playerArrowRt.sizeDelta = new Vector2(18f, 18f);
            _playerArrowRt.anchoredPosition = Vector2.zero;
            var arrowImg = arrowGo.GetComponent<Image>();
            arrowImg.sprite = _playerArrowSprite;
            arrowImg.color = playerArrowColor;
            arrowImg.raycastTarget = false;

            CacheWorldIslands();
        }

        private void CreateCompassLabel(string name, Transform parent, string text, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(24f, 24f);
            rt.anchoredPosition = pos;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.65f, 0.90f, 1f, 0.92f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        public void CacheWorldIslands()
        {
            _islandBlips.Clear();
            if (_islandsContainer == null) return;

            // Xóa các blip cũ nếu có
            for (int i = _islandsContainer.childCount - 1; i >= 0; i--)
            {
                var child = _islandsContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var foundTransforms = new List<(Transform tr, float size)>();

            // 1. About Island
            var aboutIsland = GameObject.Find("AboutIsland");
            if (aboutIsland != null) foundTransforms.Add((aboutIsland.transform, 34f));

            // 2. Cụm đảo hữu cơ 02_Floating_Islands_Organic
            var organicGroup = GameObject.Find("02_Floating_Islands_Organic");
            if (organicGroup != null)
            {
                for (int i = 0; i < organicGroup.transform.childCount; i++)
                {
                    var child = organicGroup.transform.GetChild(i);
                    float sz = 28f;
                    if (child.name.Contains("02")) sz = 34f;
                    else if (child.name.Contains("04")) sz = 26f;
                    else if (child.name.Contains("05")) sz = 30f;
                    else if (child.name.Contains("06")) sz = 32f;
                    else if (child.name.Contains("07")) sz = 30f;
                    foundTransforms.Add((child, sz));
                }
            }

            // 3. Mỏ khoáng thạch GemstoneMiningAsteroid
            var gemstone = GameObject.Find("GemstoneMiningAsteroid");
            if (gemstone != null) foundTransforms.Add((gemstone.transform, 30f));

            // 4. Đảo xuất phát Core_Island_Rock_Main
            var coreRock = GameObject.Find("Core_Island_Rock_Main");
            if (coreRock != null) foundTransforms.Add((coreRock.transform, 36f));

            // 5. Một số tiểu hành tinh chính tạo chiều sâu không gian
            var heavyAsteroids = new string[] { "Heavy_Asteroid_1", "Heavy_Asteroid_3", "Heavy_Asteroid_6", "Heavy_Asteroid_7", "Heavy_Asteroid_8", "Heavy_Asteroid_11" };
            foreach (var hName in heavyAsteroids)
            {
                var hObj = GameObject.Find(hName);
                if (hObj != null) foundTransforms.Add((hObj.transform, 20f));
            }

            // Tạo các icon mảng cyan cho từng đảo
            for (int i = 0; i < foundTransforms.Count; i++)
            {
                var info = foundTransforms[i];
                var blobGo = new GameObject("IslandBlob_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                blobGo.transform.SetParent(_islandsContainer, false);
                var blobRt = blobGo.GetComponent<RectTransform>();
                blobRt.sizeDelta = new Vector2(info.size, info.size * 0.9f);

                var img = blobGo.GetComponent<Image>();
                img.sprite = _islandBlobSprite;
                img.color = islandColor;
                img.raycastTarget = false;

                _islandBlips.Add(new IslandRadarBlip
                {
                    WorldTransform = info.tr,
                    UiRect = blobRt,
                    BaseRadius = info.size
                });
            }
        }

        public void UpdateRadarDisplay()
        {
            if (AboutInfoStation.AnyStationActive)
            {
                if (_radarRoot != null && _radarRoot.activeSelf) _radarRoot.SetActive(false);
                if (_cornerVignetteGo != null && _cornerVignetteGo.activeSelf) _cornerVignetteGo.SetActive(false);
                return;
            }

            EnsureRadarUI();
            if (_islandBlips.Count == 0) CacheWorldIslands();

            if (_radarRoot != null && !_radarRoot.activeSelf) _radarRoot.SetActive(true);
            if (_cornerVignetteGo != null && !_cornerVignetteGo.activeSelf) _cornerVignetteGo.SetActive(true);

            Vector3 playerPos = transform.position;
            float playerYaw = transform.eulerAngles.y;

            // Cập nhật mũi tên người chơi và quạt quét tầm nhìn
            if (_playerArrowRt != null)
            {
                _playerArrowRt.localRotation = Quaternion.Euler(0f, 0f, -playerYaw);
            }
            if (_visionConeRt != null)
            {
                _visionConeRt.localRotation = Quaternion.Euler(0f, 0f, -playerYaw);
            }

            // 1. Cập nhật vị trí các hòn đảo bay trên Radar
            for (int i = 0; i < _islandBlips.Count; i++)
            {
                var blip = _islandBlips[i];
                if (blip.WorldTransform == null) continue;

                Vector3 islandPos = blip.WorldTransform.position;
                float dx = islandPos.x - playerPos.x;
                float dz = islandPos.z - playerPos.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist <= radarWorldRadius * 1.05f)
                {
                    blip.UiRect.gameObject.SetActive(true);
                    float clampedDist = Mathf.Min(dist, radarWorldRadius);
                    float uiDist = (clampedDist / radarWorldRadius) * radarUiRadius;
                    float angle = Mathf.Atan2(dz, dx);
                    blip.UiRect.anchoredPosition = new Vector2(Mathf.Cos(angle) * uiDist, Mathf.Sin(angle) * uiDist);
                }
                else
                {
                    blip.UiRect.gameObject.SetActive(false);
                }
            }

            // 2. Cập nhật vị trí và góc bay của các phi thuyền xung quanh
            IReadOnlyList<SpaceTrafficShipAI> ships = SpaceTrafficShipAI.ActiveShips;
            if (ships == null || ships.Count == 0)
            {
                ships = FindObjectsByType<SpaceTrafficShipAI>(FindObjectsSortMode.None);
            }

            int blipIdx = 0;
            if (ships != null)
            {
                for (int i = 0; i < ships.Count && blipIdx < _shipBlipPool.Count; i++)
                {
                    var ship = ships[i];
                    if (ship == null || !ship.gameObject.activeInHierarchy) continue;

                    Vector3 shipPos = ship.transform.position;
                    float dx = shipPos.x - playerPos.x;
                    float dz = shipPos.z - playerPos.z;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);

                    if (dist <= radarWorldRadius)
                    {
                        var blipRt = _shipBlipPool[blipIdx++];
                        blipRt.gameObject.SetActive(true);

                        float uiX = (dx / radarWorldRadius) * radarUiRadius;
                        float uiY = (dz / radarWorldRadius) * radarUiRadius;
                        blipRt.anchoredPosition = new Vector2(uiX, uiY);

                        float shipHeading = ship.transform.eulerAngles.y;
                        blipRt.localRotation = Quaternion.Euler(0f, 0f, -shipHeading);
                    }
                }
            }

            // Tắt các blip phi thuyền không dùng
            for (int i = blipIdx; i < _shipBlipPool.Count; i++)
            {
                if (_shipBlipPool[i].gameObject.activeSelf)
                {
                    _shipBlipPool[i].gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            UpdateRadarDisplay();
        }
    }
}
