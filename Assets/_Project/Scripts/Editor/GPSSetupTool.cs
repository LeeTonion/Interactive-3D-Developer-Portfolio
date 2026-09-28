#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using CodeDrive.Navigation;
using CodeDrive.Portfolio;

namespace CodeDrive.EditorTools
{
    public static class GPSSetupTool
    {
        [MenuItem("Tools/Setup Node-Based Road Navigation System")]
        public static void SetupGPSNavigation()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[GPSSetupTool] Cannot run setup tool during Play Mode. Exit Play Mode first.");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.isLoaded) return;

            // 0. Ensure EventSystem exists in Scene
            var es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    esObj.AddComponent(inputModuleType);
                }
                else
                {
                    esObj.AddComponent<StandaloneInputModule>();
                }
            }

            // 1. Setup the Road Node Network from World/Roads
            var roadsParent = GameObject.Find("World/Roads");
            if (roadsParent == null) roadsParent = GameObject.Find("Roads");

            if (roadsParent != null)
            {
                int roadNodesAdded = 0;
                int collidersAdded = 0;

                foreach (Transform roadSegment in roadsParent.transform)
                {
                    // Clean legacy nodes on road segment
                    var oldNodes = roadSegment.GetComponentsInChildren<RoadNode>();
                    foreach (var on in oldNodes) Object.DestroyImmediate(on.gameObject);

                    // Add MeshCollider
                    var mf = roadSegment.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        if (roadSegment.GetComponent<MeshCollider>() == null)
                        {
                            var mc = roadSegment.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                            collidersAdded++;
                        }
                    }

                    // Calculate bounding box of the road piece
                    var renderers = roadSegment.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) continue;

                    Bounds bounds = renderers[0].bounds;
                    for (int k = 1; k < renderers.Length; k++)
                    {
                        bounds.Encapsulate(renderers[k].bounds);
                    }

                    Vector3 center = bounds.center;
                    center.y = roadSegment.position.y;

                    float sizeX = bounds.size.x;
                    float sizeZ = bounds.size.z;

                    // Place nodes centered on each 18m grid tile
                    if (sizeX > 25f) // Road along X axis (Medium=54m, Long=90m)
                    {
                        int tileCount = Mathf.RoundToInt(sizeX / 18f);
                        float startX = center.x - (tileCount - 1) * 9f;
                        for (int t = 0; t < tileCount; t++)
                        {
                            GameObject nObj = new GameObject("Node_" + t);
                            nObj.transform.SetParent(roadSegment, false);
                            nObj.transform.position = new Vector3(startX + t * 18f, center.y, center.z);
                            nObj.AddComponent<RoadNode>();
                            roadNodesAdded++;
                        }
                    }
                    else if (sizeZ > 25f) // Road along Z axis (Medium=54m, Long=90m)
                    {
                        int tileCount = Mathf.RoundToInt(sizeZ / 18f);
                        float startZ = center.z - (tileCount - 1) * 9f;
                        for (int t = 0; t < tileCount; t++)
                        {
                            GameObject nObj = new GameObject("Node_" + t);
                            nObj.transform.SetParent(roadSegment, false);
                            nObj.transform.position = new Vector3(center.x, center.y, startZ + t * 18f);
                            nObj.AddComponent<RoadNode>();
                            roadNodesAdded++;
                        }
                    }
                    else // Single 18m x 18m tile (Short, Turn, Cross)
                    {
                        GameObject nObj = new GameObject("Node");
                        nObj.transform.SetParent(roadSegment, false);
                        nObj.transform.position = center;
                        nObj.AddComponent<RoadNode>();
                        roadNodesAdded++;
                    }
                }

                Debug.Log($"[GPSSetupTool] Road network: {roadNodesAdded} 18m tile RoadNodes centered, {collidersAdded} MeshColliders added.");
            }

            // 2. Setup NavigationManager & RoadGraph in _Systems
            var systems = GameObject.Find("_Systems");
            if (systems == null)
            {
                systems = new GameObject("_Systems");
            }

            var navManagerObj = systems.transform.Find("NavigationManager")?.gameObject;
            if (navManagerObj == null)
            {
                navManagerObj = new GameObject("NavigationManager");
                navManagerObj.transform.SetParent(systems.transform, false);
            }

            // RoadGraph Component
            var roadGraph = navManagerObj.GetComponent<RoadGraph>();
            if (roadGraph == null) roadGraph = navManagerObj.AddComponent<RoadGraph>();

            // Set autoConnectMaxDistance to 26f (adjacent 18m tiles)
            var distField = typeof(RoadGraph).GetField("autoConnectMaxDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (distField != null)
            {
                distField.SetValue(roadGraph, 26f);
            }

            roadGraph.RefreshAndConnectNodes();

            // Navigation Manager
            var roadNavManager = navManagerObj.GetComponent<RoadNavigationManager>();
            if (roadNavManager == null) roadNavManager = navManagerObj.AddComponent<RoadNavigationManager>();

            // Link player car directly in Inspector so no runtime Find is needed
            var playerCar = GameObject.FindWithTag("Player");
            if (playerCar != null)
            {
                roadNavManager.SetPlayerVehicle(playerCar.transform);
            }

            var lineRenderer = navManagerObj.GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = navManagerObj.AddComponent<LineRenderer>();

            var pathRenderer = navManagerObj.GetComponent<NavigationPathRenderer>();
            if (pathRenderer == null) pathRenderer = navManagerObj.AddComponent<NavigationPathRenderer>();

            var beacon = navManagerObj.GetComponent<NavigationBeacon>();
            if (beacon == null) beacon = navManagerObj.AddComponent<NavigationBeacon>();

            var vehicleArrow = navManagerObj.GetComponent<VehicleNavigationArrow>();
            if (vehicleArrow == null) vehicleArrow = navManagerObj.AddComponent<VehicleNavigationArrow>();

            // 3. Create UI in Canvas
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogWarning("[GPSSetupTool] Canvas not found in scene.");
                return;
            }

            // Clean existing GPS UI if present (activate before DestroyImmediate to avoid assertion errors)
            string[] oldUINames = { "GPSHUD", "GPSMenu", "GPSToast", "MiniMapPanel" };
            foreach (var uiName in oldUINames)
            {
                var oldUI = canvas.transform.Find(uiName);
                if (oldUI != null)
                {
                    oldUI.gameObject.SetActive(true);
                    Object.DestroyImmediate(oldUI.gameObject);
                }
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- GPS HUD Panel (Top Right HUD) ---
            GameObject hudObj = new GameObject("GPSHUD", typeof(RectTransform), typeof(Image));
            hudObj.transform.SetParent(canvas.transform, false);
            var hudRect = hudObj.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(1f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(1f, 1f);
            hudRect.anchoredPosition = new Vector2(-20f, -20f);
            hudRect.sizeDelta = new Vector2(240f, 60f);

            var hudImg = hudObj.GetComponent<Image>();
            hudImg.color = new Color(0.08f, 0.12f, 0.20f, 0.88f);

            // Destination Text
            GameObject destTextObj = new GameObject("DestinationText", typeof(RectTransform), typeof(Text));
            destTextObj.transform.SetParent(hudObj.transform, false);
            var destRect = destTextObj.GetComponent<RectTransform>();
            destRect.anchorMin = new Vector2(0f, 0.5f);
            destRect.anchorMax = new Vector2(0.7f, 1f);
            destRect.offsetMin = new Vector2(12f, 0f);
            destRect.offsetMax = new Vector2(0f, -6f);

            var destTxt = destTextObj.GetComponent<Text>();
            destTxt.text = "GPS: PROJECTS";
            destTxt.font = defaultFont;
            destTxt.fontSize = 15;
            destTxt.fontStyle = FontStyle.Bold;
            destTxt.color = new Color(0.2f, 0.9f, 1f);
            destTxt.alignment = TextAnchor.MiddleLeft;

            // Distance Text
            GameObject distTextObj = new GameObject("DistanceText", typeof(RectTransform), typeof(Text));
            distTextObj.transform.SetParent(hudObj.transform, false);
            var distRect = distTextObj.GetComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0f, 0f);
            distRect.anchorMax = new Vector2(0.7f, 0.5f);
            distRect.offsetMin = new Vector2(12f, 6f);
            distRect.offsetMax = new Vector2(0f, 0f);

            var distTxt = distTextObj.GetComponent<Text>();
            distTxt.text = "120m";
            distTxt.font = defaultFont;
            distTxt.fontSize = 13;
            distTxt.color = new Color(0.9f, 0.95f, 1f, 0.9f);
            distTxt.alignment = TextAnchor.MiddleLeft;

            // Compass Arrow Icon
            GameObject arrowObj = new GameObject("CompassArrow", typeof(RectTransform), typeof(Image));
            arrowObj.transform.SetParent(hudObj.transform, false);
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-30f, 0f);
            arrowRect.sizeDelta = new Vector2(28f, 28f);

            var arrowImg = arrowObj.GetComponent<Image>();
            arrowImg.color = new Color(0.2f, 0.9f, 1f);


            // --- MINI MAP PANEL (2D Schematic, press M to toggle) ---
            // Remove any leftover MiniMapCamera objects
            var oldMapCam = systems.transform.Find("MiniMapCamera");
            if (oldMapCam != null) { oldMapCam.gameObject.SetActive(true); Object.DestroyImmediate(oldMapCam.gameObject); }

            // --- MiniMap Root Panel (Full Screen) ---
            GameObject miniMapPanel = new GameObject("MiniMapPanel", typeof(RectTransform), typeof(Image));
            miniMapPanel.transform.SetParent(canvas.transform, false);
            var mmRect = miniMapPanel.GetComponent<RectTransform>();
            mmRect.anchorMin = Vector2.zero;
            mmRect.anchorMax = Vector2.one;
            mmRect.offsetMin = Vector2.zero;
            mmRect.offsetMax = Vector2.zero;
            var mmBg = miniMapPanel.GetComponent<Image>();
            mmBg.color = new Color(0.02f, 0.05f, 0.08f, 0.95f); // Deep dark tech background

            // --- Title Bar ---
            GameObject titleObj = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleObj.transform.SetParent(miniMapPanel.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(0f, 48f);
            titleObj.GetComponent<Image>().color = new Color(0.04f, 0.18f, 0.22f, 0.85f);

            GameObject titleTxtObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleTxtObj.transform.SetParent(titleObj.transform, false);
            var titleTxtRect = titleTxtObj.GetComponent<RectTransform>();
            titleTxtRect.anchorMin = Vector2.zero;
            titleTxtRect.anchorMax = Vector2.one;
            titleTxtRect.sizeDelta = Vector2.zero;
            var titleTxt = titleTxtObj.GetComponent<Text>();
            titleTxt.text = "🗺  PORTFOLIO WORLD MAP  [M]  —  CLICK ANY DESTINATION TO NAVIGATE";
            titleTxt.font = defaultFont;
            titleTxt.fontSize = 15;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.3f, 0.95f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.raycastTarget = false;

            // --- Map Drawing Area (fills panel below title) ---
            GameObject mapAreaObj = new GameObject("MapArea", typeof(RectTransform), typeof(Image));
            mapAreaObj.transform.SetParent(miniMapPanel.transform, false);
            var mapAreaRect = mapAreaObj.GetComponent<RectTransform>();
            mapAreaRect.anchorMin = new Vector2(0f, 0f);
            mapAreaRect.anchorMax = new Vector2(1f, 1f);
            mapAreaRect.offsetMin = new Vector2(24f, 24f);
            mapAreaRect.offsetMax = new Vector2(-24f, -56f); // leave room for title
            var mapAreaImg = mapAreaObj.GetComponent<Image>();
            mapAreaImg.color = new Color(0.04f, 0.08f, 0.12f, 0.9f);
            mapAreaImg.raycastTarget = false;

            // Grid lines (light decorative grid)
            for (int i = 1; i < 4; i++)
            {
                float t = i / 4f;
                // Horizontal
                GameObject hLine = new GameObject("GridH" + i, typeof(RectTransform), typeof(Image));
                hLine.transform.SetParent(mapAreaObj.transform, false);
                var hRect = hLine.GetComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0f, t);
                hRect.anchorMax = new Vector2(1f, t);
                hRect.sizeDelta = new Vector2(0f, 1f);
                hLine.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
                hLine.GetComponent<Image>().raycastTarget = false;
                // Vertical
                GameObject vLine = new GameObject("GridV" + i, typeof(RectTransform), typeof(Image));
                vLine.transform.SetParent(mapAreaObj.transform, false);
                var vRect = vLine.GetComponent<RectTransform>();
                vRect.anchorMin = new Vector2(t, 0f);
                vRect.anchorMax = new Vector2(t, 1f);
                vRect.sizeDelta = new Vector2(1f, 0f);
                vLine.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
                vLine.GetComponent<Image>().raycastTarget = false;
            }

            // --- Player Marker (gold triangle) ---
            GameObject playerMarkerObj = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
            playerMarkerObj.transform.SetParent(mapAreaObj.transform, false);
            var playerMarkerRect = playerMarkerObj.GetComponent<RectTransform>();
            playerMarkerRect.sizeDelta = new Vector2(16f, 16f);
            playerMarkerRect.anchoredPosition = Vector2.zero;
            var playerImg = playerMarkerObj.GetComponent<Image>();
            playerImg.color = new Color(1f, 0.9f, 0.1f, 1f); // Gold = you
            playerImg.raycastTarget = false;

            // --- Close Button in title bar ---
            GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(titleObj.transform, false);
            var closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1f, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1f, 0.5f);
            closeBtnRect.pivot = new Vector2(1f, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-8f, 0f);
            closeBtnRect.sizeDelta = new Vector2(52f, 24f);
            closeBtnObj.GetComponent<Image>().color = new Color(0.7f, 0.15f, 0.15f, 0.95f);
            GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.sizeDelta = Vector2.zero;
            var closeTxt = closeTxtObj.GetComponent<Text>();
            closeTxt.text = "✕ CLOSE";
            closeTxt.font = defaultFont;
            closeTxt.fontSize = 10;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.raycastTarget = false;

            // --- GPSToast (Arrival Banner) ---
            GameObject toastObj = new GameObject("GPSToast", typeof(RectTransform), typeof(Image));
            toastObj.transform.SetParent(canvas.transform, false);
            var toastRect = toastObj.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.anchoredPosition = new Vector2(0f, -80f);
            toastRect.sizeDelta = new Vector2(380f, 44f);
            toastObj.GetComponent<Image>().color = new Color(0.05f, 0.8f, 0.4f, 0.92f);

            GameObject toastTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            toastTxtObj.transform.SetParent(toastObj.transform, false);
            var toastTxtRect = toastTxtObj.GetComponent<RectTransform>();
            toastTxtRect.anchorMin = Vector2.zero;
            toastTxtRect.anchorMax = Vector2.one;
            var toastTxt = toastTxtObj.GetComponent<Text>();
            toastTxt.text = "DESTINATION REACHED!";
            toastTxt.font = defaultFont;
            toastTxt.fontSize = 16;
            toastTxt.fontStyle = FontStyle.Bold;
            toastTxt.color = Color.white;
            toastTxt.alignment = TextAnchor.MiddleCenter;
            toastObj.SetActive(false);

            // Hide mini map by default
            miniMapPanel.SetActive(false);

            // --- Attach MiniMapController ---
            var miniMapControllerType = System.Type.GetType("CodeDrive.UI.MiniMapController, Assembly-CSharp");
            if (miniMapControllerType == null)
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    miniMapControllerType = asm.GetType("CodeDrive.UI.MiniMapController");
                    if (miniMapControllerType != null) break;
                }
            }

            if (miniMapControllerType != null)
            {
                var mapCtrl = navManagerObj.GetComponent(miniMapControllerType);
                if (mapCtrl == null) mapCtrl = navManagerObj.AddComponent(miniMapControllerType);

                var setRefMethod = miniMapControllerType.GetMethod("SetReferences");
                setRefMethod?.Invoke(mapCtrl, new object[] { miniMapPanel, mapAreaRect, mapAreaRect, playerMarkerRect });

                // Wire close button
                var closeBtnComp = closeBtnObj.GetComponent<Button>();
                closeBtnComp.onClick.RemoveAllListeners();
                closeBtnComp.onClick.AddListener(() => {
                    var ctrl = navManagerObj.GetComponent(miniMapControllerType);
                    miniMapControllerType.GetMethod("SetVisible")?.Invoke(ctrl, new object[] { false });
                });
            }

            // --- Attach NavigationHUDUI for GPS HUD bar only ---
            var hudType = System.Type.GetType("CodeDrive.UI.NavigationHUDUI, Assembly-CSharp");
            if (hudType == null)
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    hudType = asm.GetType("CodeDrive.UI.NavigationHUDUI");
                    if (hudType != null) break;
                }
            }

            if (hudType != null)
            {
                var hudScript = navManagerObj.GetComponent(hudType);
                if (hudScript == null) hudScript = navManagerObj.AddComponent(hudType);

                hudType.GetField("gpsHudPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudScript, hudObj);
                hudType.GetField("gpsMenuPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudScript, miniMapPanel);
                hudType.GetField("destinationText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudScript, destTxt);
                hudType.GetField("distanceText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudScript, distTxt);
                hudType.GetField("compassArrow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudScript, arrowRect);
                hudType.GetField("toastNotificationObj", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudScript, toastObj);
                hudType.GetField("toastNotificationText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudScript, toastTxt);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[GPSSetupTool] Mini Map + GPS HUD setup completed. Press M to open Mini Map in Play Mode.");
        }
    }
}
#endif

