using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeDrive.Navigation;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// 2D Schematic Mini Map — draws roads, active navigation paths & portfolio markers as UI elements.
    /// Pure UI Canvas implementation: fast, crisp, reliable, zero camera/RenderTexture overhead.
    /// Press M to toggle. Click any destination marker to navigate.
    /// </summary>
    public class MiniMapController : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private RectTransform roadsContainer;
        [SerializeField] private RectTransform pathContainer;
        [SerializeField] private RectTransform markersContainer;
        [SerializeField] private RectTransform playerMarker;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.M;

        // Dynamic Map Bounds
        private Vector2 _worldCenter = Vector2.zero;
        private Vector2 _worldSize = new Vector2(200f, 200f);

        // State
        private bool _isVisible = false;
        private Transform _playerTransform;
        private bool _mapDrawn = false;

        private void Awake()
        {
            EnsureContainers();
        }

        private void Start()
        {
            EnsureContainers();

            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnAreasListUpdated += RebuildMap;
                RoadNavigationManager.Instance.OnPathUpdated += OnPathUpdated;
                RoadNavigationManager.Instance.OnPathCleared += OnPathCleared;
                _playerTransform = RoadNavigationManager.Instance.PlayerVehicle;
            }

            RebuildMap();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnAreasListUpdated -= RebuildMap;
                RoadNavigationManager.Instance.OnPathUpdated -= OnPathUpdated;
                RoadNavigationManager.Instance.OnPathCleared -= OnPathCleared;
            }
        }

        public void EnsureContainers()
        {
            if (panelRoot == null)
            {
                var canvas = GameObject.Find("Canvas");
                if (canvas != null)
                {
                    var panel = canvas.transform.Find("MiniMapPanel");
                    if (panel != null) panelRoot = panel.gameObject;
                }
            }

            if (panelRoot != null)
            {
                if (mapContainer == null)
                {
                    var mapArea = panelRoot.transform.Find("MapArea");
                    if (mapArea != null) mapContainer = mapArea.GetComponent<RectTransform>();
                }

                if (mapContainer != null)
                {
                    roadsContainer = GetOrCreateContainer("RoadsContainer", mapContainer);
                    pathContainer = GetOrCreateContainer("PathContainer", mapContainer);
                    markersContainer = GetOrCreateContainer("MarkersContainer", mapContainer);

                    if (playerMarker == null)
                    {
                        var pm = mapContainer.Find("PlayerMarker");
                        if (pm != null) playerMarker = pm.GetComponent<RectTransform>();
                    }

                    // Ensure stacking order
                    if (roadsContainer != null) roadsContainer.SetSiblingIndex(0);
                    if (pathContainer != null) pathContainer.SetSiblingIndex(1);
                    if (markersContainer != null) markersContainer.SetSiblingIndex(2);
                    if (playerMarker != null) playerMarker.SetAsLastSibling();
                }
            }
        }

        private RectTransform GetOrCreateContainer(string name, RectTransform parent)
        {
            var found = parent.Find(name);
            if (found != null) return found.GetComponent<RectTransform>();

            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetVisible(!_isVisible);
            }

            if (!_isVisible) return;

            if (_playerTransform == null && RoadNavigationManager.Instance != null)
                _playerTransform = RoadNavigationManager.Instance.PlayerVehicle;

            UpdatePlayerMarker();
        }

        // ─────────────────────────────────────────
        //  World → Map UV mapping
        // ─────────────────────────────────────────
        private void CalculateBounds()
        {
            var nodes = FindObjectsOfType<RoadNode>();
            var areas = FindObjectsOfType<PortfolioArea>();

            if ((nodes == null || nodes.Length == 0) && (areas == null || areas.Length == 0))
            {
                _worldCenter = Vector2.zero;
                _worldSize = new Vector2(250f, 250f);
                return;
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n == null) continue;
                    Vector3 p = n.Position;
                    if (p.x < minX) minX = p.x;
                    if (p.x > maxX) maxX = p.x;
                    if (p.z < minZ) minZ = p.z;
                    if (p.z > maxZ) maxZ = p.z;
                }
            }

            if (areas != null)
            {
                foreach (var a in areas)
                {
                    if (a == null) continue;
                    Vector3 p = a.transform.position;
                    if (p.x < minX) minX = p.x;
                    if (p.x > maxX) maxX = p.x;
                    if (p.z < minZ) minZ = p.z;
                    if (p.z > maxZ) maxZ = p.z;
                }
            }

            // Margin
            float padding = 25f;
            minX -= padding; maxX += padding;
            minZ -= padding; maxZ += padding;

            _worldCenter = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
            float spanX = Mathf.Max(maxX - minX, 50f);
            float spanZ = Mathf.Max(maxZ - minZ, 50f);
            float maxSpan = Mathf.Max(spanX, spanZ);
            _worldSize = new Vector2(maxSpan, maxSpan);
        }

        private Vector2 WorldToMap(Vector3 worldPos)
        {
            if (mapContainer == null || _worldSize.x < 1f) return Vector2.zero;

            float mapW = mapContainer.rect.width;
            float mapH = mapContainer.rect.height;

            // Preserve 1:1 aspect ratio so full-screen doesn't stretch coordinates
            float uiScale = Mathf.Min(mapW, mapH) / _worldSize.x;

            float dx = worldPos.x - _worldCenter.x;
            float dz = worldPos.z - _worldCenter.y;

            return new Vector2(dx * uiScale, dz * uiScale);
        }

        // ─────────────────────────────────────────
        //  Build Map Structure
        // ─────────────────────────────────────────
        public void RebuildMap()
        {
            EnsureContainers();
            if (mapContainer == null) return;

            CalculateBounds();

            // Clear road container
            if (roadsContainer != null)
            {
                for (int i = roadsContainer.childCount - 1; i >= 0; i--)
                {
                    var child = roadsContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            // Clear markers container
            if (markersContainer != null)
            {
                for (int i = markersContainer.childCount - 1; i >= 0; i--)
                {
                    var child = markersContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            DrawRoads();
            DrawAreaMarkers();

            // Bring player marker to front
            if (playerMarker != null)
            {
                playerMarker.SetAsLastSibling();
            }

            // If active path exists, draw it
            if (RoadNavigationManager.Instance != null && RoadNavigationManager.Instance.HasActivePath)
            {
                DrawActivePath(RoadNavigationManager.Instance.CurrentPathWaypoints);
            }

            _mapDrawn = true;
        }

        private void DrawRoads()
        {
            if (roadsContainer == null) return;

            var nodes = FindObjectsOfType<RoadNode>();
            if (nodes == null || nodes.Length == 0) return;

            HashSet<string> drawnPairs = new HashSet<string>();

            foreach (var node in nodes)
            {
                if (node == null || node.neighbors == null) continue;

                foreach (var nb in node.neighbors)
                {
                    if (nb == null) continue;
                    string key = GetPairKey(node, nb);
                    if (drawnPairs.Contains(key)) continue;
                    drawnPairs.Add(key);

                    // Road background (dark thick asphalt line)
                    DrawLine(node.Position, nb.Position,
                        new Color(0.20f, 0.26f, 0.32f, 1f), 10f, roadsContainer, "Road_Base");

                    // Road center line (clean modern green lane)
                    DrawLine(node.Position, nb.Position,
                        new Color(0.35f, 0.68f, 0.48f, 0.9f), 3f, roadsContainer, "Road_Lane");
                }
            }
        }

        private string GetPairKey(RoadNode a, RoadNode b)
        {
            int idA = a.GetInstanceID();
            int idB = b.GetInstanceID();
            return idA < idB ? $"{idA}_{idB}" : $"{idB}_{idA}";
        }

        private GameObject DrawLine(Vector3 from, Vector3 to, Color color, float thickness, RectTransform parent, string name)
        {
            Vector2 a = WorldToMap(from);
            Vector2 b = WorldToMap(to);

            Vector2 dir = b - a;
            float length = dir.magnitude;
            if (length < 0.5f) return null;

            GameObject lineObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(parent, false);

            var rect = lineObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(length, thickness);
            rect.anchoredPosition = (a + b) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var img = lineObj.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            return lineObj;
        }

        // ─────────────────────────────────────────
        //  Portfolio Area Markers
        // ─────────────────────────────────────────
        private void DrawAreaMarkers()
        {
            if (markersContainer == null) return;

            var areas = RoadNavigationManager.Instance != null && RoadNavigationManager.Instance.AllPortfolioAreas.Count > 0 ?
                RoadNavigationManager.Instance.AllPortfolioAreas :
                (IReadOnlyList<PortfolioArea>)FindObjectsOfType<PortfolioArea>();

            if (areas == null) return;

            HashSet<PortfolioAreaType> addedTypes = new HashSet<PortfolioAreaType>();
            foreach (var area in areas)
            {
                if (area == null || addedTypes.Contains(area.AreaType)) continue;
                addedTypes.Add(area.AreaType);
                CreateAreaMarker(area, markersContainer);
            }
        }

        private void CreateAreaMarker(PortfolioArea area, RectTransform parent)
        {
            // Button Container
            GameObject markerObj = new GameObject($"Marker_{area.AreaLabel}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            markerObj.transform.SetParent(parent, false);

            var rect = markerObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(24f, 24f);
            rect.anchoredPosition = WorldToMap(area.transform.position);

            var img = markerObj.GetComponent<Image>();
            img.color = new Color(0.1f, 0.85f, 1f, 1f); // Vibrant Cyan
            img.raycastTarget = true;

            // Glow Ring
            GameObject ringObj = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringObj.transform.SetParent(markerObj.transform, false);
            var ringRect = ringObj.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(34f, 34f);
            ringRect.anchoredPosition = Vector2.zero;
            var ringImg = ringObj.GetComponent<Image>();
            ringImg.color = new Color(0.1f, 0.85f, 1f, 0.35f);
            ringImg.raycastTarget = false;

            // Label Background Pill
            GameObject pillObj = new GameObject("LabelPill", typeof(RectTransform), typeof(Image));
            pillObj.transform.SetParent(markerObj.transform, false);
            var pillRect = pillObj.GetComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(0.5f, 0.5f);
            pillRect.anchorMax = new Vector2(0.5f, 0.5f);
            pillRect.pivot = new Vector2(0.5f, 0f);
            pillRect.anchoredPosition = new Vector2(0f, 18f);
            pillRect.sizeDelta = new Vector2(100f, 20f);
            var pillImg = pillObj.GetComponent<Image>();
            pillImg.color = new Color(0.02f, 0.08f, 0.14f, 0.92f);
            pillImg.raycastTarget = false;

            // Label Text
            GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lblObj.transform.SetParent(pillObj.transform, false);
            var lblRect = lblObj.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.sizeDelta = Vector2.zero;
            var lbl = lblObj.GetComponent<Text>();
            lbl.text = area.AreaLabel.ToUpper();
            lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.fontSize = 10;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = new Color(0.9f, 0.98f, 1f);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.raycastTarget = false;

            // Button onClick -> Navigate & Close Map
            var btn = markerObj.GetComponent<Button>();
            var capturedArea = area;
            btn.onClick.AddListener(() =>
            {
                if (RoadNavigationManager.Instance != null)
                {
                    RoadNavigationManager.Instance.SetTargetArea(capturedArea);
                }
                SetVisible(false);
            });
        }

        // ─────────────────────────────────────────
        //  Active Path Overlay (Glowing Path on Map)
        // ─────────────────────────────────────────
        private void OnPathUpdated(List<Vector3> waypoints)
        {
            DrawActivePath(waypoints);
        }

        private void OnPathCleared()
        {
            ClearActivePath();
        }

        private void ClearActivePath()
        {
            if (pathContainer != null)
            {
                for (int i = pathContainer.childCount - 1; i >= 0; i--)
                {
                    var child = pathContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }
        }

        private void DrawActivePath(List<Vector3> waypoints)
        {
            ClearActivePath();
            if (waypoints == null || waypoints.Count < 2 || pathContainer == null) return;

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                // Glowing route line on top of roads
                DrawLine(waypoints[i], waypoints[i + 1],
                    new Color(0f, 1f, 0.9f, 0.95f), 6f, pathContainer, "GPS_ActiveRoute");
            }

            if (playerMarker != null)
            {
                playerMarker.SetAsLastSibling();
            }
        }

        // ─────────────────────────────────────────
        //  Player Marker Real-Time Update
        // ─────────────────────────────────────────
        private void UpdatePlayerMarker()
        {
            if (playerMarker == null || _playerTransform == null || mapContainer == null) return;

            playerMarker.anchoredPosition = WorldToMap(_playerTransform.position);

            Vector3 fwd = _playerTransform.forward;
            float angle = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // ─────────────────────────────────────────
        //  Show / Hide Toggle
        // ─────────────────────────────────────────
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }

            if (visible)
            {
                if (!_mapDrawn)
                {
                    RebuildMap();
                }
                else
                {
                    if (RoadNavigationManager.Instance != null && RoadNavigationManager.Instance.HasActivePath)
                    {
                        DrawActivePath(RoadNavigationManager.Instance.CurrentPathWaypoints);
                    }
                    else
                    {
                        ClearActivePath();
                    }
                }
            }
        }

        public void SetReferences(GameObject root, RectTransform mapCont, RectTransform markerCont, RectTransform player)
        {
            panelRoot = root;
            mapContainer = mapCont;
            playerMarker = player;
            EnsureContainers();
        }
    }
}
