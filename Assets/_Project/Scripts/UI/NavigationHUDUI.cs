using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CodeDrive.Navigation;
using CodeDrive.Portfolio;
using CodeDrive.Core;

namespace CodeDrive.UI
{
    /// <summary>
    /// UI Controller for GPS Road Navigation HUD and Destination Selection Menu.
    /// Features EventSystem auto-creation, Raycast target setup, and robust button events.
    /// </summary>
    public class NavigationHUDUI : MonoBehaviour
    {
        [Header("UI Panel Containers")]
        [SerializeField] private GameObject gpsHudPanel;
        [SerializeField] private GameObject gpsMenuPanel;

        [Header("HUD Elements")]
        [SerializeField] private Text destinationText;
        [SerializeField] private Text distanceText;
        [SerializeField] private RectTransform compassArrow;
        [SerializeField] private GameObject toastNotificationObj;
        [SerializeField] private Text toastNotificationText;

        [Header("Menu Buttons")]
        [SerializeField] private Transform buttonsContainer;
        [SerializeField] private Button clearGpsButton;
        [SerializeField] private Button toggleMenuButton;

        [Header("Keybindings")]
        [SerializeField] private KeyCode toggleGpsKey = KeyCode.G;

        private Dictionary<PortfolioAreaType, Button> _areaButtons = new Dictionary<PortfolioAreaType, Button>();
        private Coroutine _toastCoroutine;

        private void Awake()
        {
            EnsureEventSystem();
            AutoBindReferences();
        }

        public void EnsureEventSystem()
        {
            var es = FindObjectOfType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
                
                // Check for New Input System module
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
        }

        public void AutoBindReferences()
        {
            Transform canvasTrans = transform.parent != null ? transform.parent : transform;
            if (canvasTrans.name != "Canvas")
            {
                var canvasObj = GameObject.Find("Canvas");
                if (canvasObj != null) canvasTrans = canvasObj.transform;
            }

            if (gpsHudPanel == null)
            {
                var hud = canvasTrans.Find("GPSHUD");
                if (hud != null) gpsHudPanel = hud.gameObject;
            }

            if (gpsMenuPanel == null)
            {
                var menu = canvasTrans.Find("GPSMenu");
                if (menu != null) gpsMenuPanel = menu.gameObject;
            }

            if (gpsHudPanel != null)
            {
                if (destinationText == null)
                    destinationText = gpsHudPanel.transform.Find("DestinationText")?.GetComponent<Text>();

                if (distanceText == null)
                    distanceText = gpsHudPanel.transform.Find("DistanceText")?.GetComponent<Text>();

                if (compassArrow == null)
                    compassArrow = gpsHudPanel.transform.Find("CompassArrow")?.GetComponent<RectTransform>();
            }

            if (gpsMenuPanel != null)
            {
                if (buttonsContainer == null)
                {
                    // Deep search: ButtonsContainer may be nested inside ScrollRect > Viewport
                    var allTransforms = gpsMenuPanel.GetComponentsInChildren<Transform>(true);
                    foreach (var t in allTransforms)
                    {
                        if (t.name == "ButtonsContainer")
                        {
                            buttonsContainer = t;
                            break;
                        }
                    }
                }

                if (clearGpsButton == null)
                    clearGpsButton = gpsMenuPanel.transform.Find("Btn_ClearGPS")?.GetComponent<Button>();
            }

            if (toastNotificationObj == null)
            {
                var toast = canvasTrans.Find("GPSToast");
                if (toast != null)
                {
                    toastNotificationObj = toast.gameObject;
                    toastNotificationText = toast.transform.Find("Text")?.GetComponent<Text>();
                }
            }
        }

        private void Start()
        {
            EnsureEventSystem();
            AutoBindReferences();
            SetupUIEvents();

            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnTargetChanged += UpdateHudState;
                RoadNavigationManager.Instance.OnPathCleared += HandlePathCleared;
                RoadNavigationManager.Instance.OnDestinationReached += HandleDestinationReached;
                RoadNavigationManager.Instance.OnAreasListUpdated += PopulateMenuButtons;
            }

            PopulateMenuButtons();
            UpdateHudState(RoadNavigationManager.Instance?.CurrentTargetArea);
            if (toastNotificationObj != null) toastNotificationObj.SetActive(false);
        }

        private void OnDestroy()
        {
            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnTargetChanged -= UpdateHudState;
                RoadNavigationManager.Instance.OnPathCleared -= HandlePathCleared;
                RoadNavigationManager.Instance.OnDestinationReached -= HandleDestinationReached;
                RoadNavigationManager.Instance.OnAreasListUpdated -= PopulateMenuButtons;
            }
        }

        private void SetupUIEvents()
        {
            if (clearGpsButton != null)
            {
                clearGpsButton.onClick.RemoveAllListeners();
                clearGpsButton.onClick.AddListener(() =>
                {
                    RoadNavigationManager.Instance?.ClearNavigation();
                });
            }

            if (toggleMenuButton != null)
            {
                toggleMenuButton.onClick.RemoveAllListeners();
                toggleMenuButton.onClick.AddListener(() =>
                {
                    ToggleMenuPanel();
                });
            }
        }

        public void PopulateMenuButtons()
        {
            AutoBindReferences();
            if (buttonsContainer == null || RoadNavigationManager.Instance == null) return;

            var areas = RoadNavigationManager.Instance.AllPortfolioAreas;
            if (areas == null || areas.Count == 0) return;

            // Remove existing children
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in buttonsContainer)
            {
                children.Add(child.gameObject);
            }
            foreach (var child in children)
            {
                Destroy(child);
            }

            _areaButtons.Clear();

            foreach (var area in areas)
            {
                PortfolioArea areaRef = area; // Closure copy
                GameObject btnObj = CreateButtonObject(area.AreaLabel, buttonsContainer);

                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    RoadNavigationManager.Instance.SetTargetArea(areaRef);
                });

                _areaButtons[area.AreaType] = btn;
            }
        }

        private GameObject CreateButtonObject(string labelText, Transform parent)
        {
            GameObject btnObj = new GameObject("Btn_" + labelText, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 32f);

            var img = btnObj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.20f, 0.32f, 0.95f);
            img.raycastTarget = true; // Ensure Raycast Target enabled on button image

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var txt = textObj.GetComponent<Text>();
            txt.text = labelText.ToUpper();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.9f, 0.95f, 1f);
            txt.raycastTarget = false; // Disable text raycast target so parent button gets click directly

            return btnObj;
        }

        private void Update()
        {
            // Hotkey toggle
            if (Input.GetKeyDown(toggleGpsKey))
            {
                ToggleMenuPanel();
            }

            // Update GPS HUD values
            if (RoadNavigationManager.Instance != null && RoadNavigationManager.Instance.HasActivePath)
            {
                if (distanceText != null)
                {
                    float dist = RoadNavigationManager.Instance.RemainingDistance;
                    distanceText.text = $"{dist:F0}m";
                }

                // Update Compass / Arrow rotation
                Transform playerCar = RoadNavigationManager.Instance.PlayerVehicle;
                if (compassArrow != null && playerCar != null)
                {
                    Vector3 targetDir = RoadNavigationManager.Instance.GetNextWaypointDirection();
                    Vector3 carForward = playerCar.forward;
                    carForward.y = 0;

                    float angle = Vector3.SignedAngle(carForward, targetDir, Vector3.up);
                    compassArrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
                }
            }
        }

        private void UpdateHudState(PortfolioArea targetArea)
        {
            AutoBindReferences();
            bool hasTarget = targetArea != null;

            if (gpsHudPanel != null)
            {
                gpsHudPanel.SetActive(hasTarget);
            }

            if (hasTarget && destinationText != null)
            {
                destinationText.text = $"GPS: {targetArea.AreaLabel.ToUpper()}";
            }
        }

        private void HandlePathCleared()
        {
            UpdateHudState(null);
        }

        private void HandleDestinationReached(PortfolioArea area)
        {
            ShowToast($"ARRIVED AT {area.AreaLabel.ToUpper()}!");
        }

        public void ToggleMenuPanel()
        {
            AutoBindReferences();
            if (gpsMenuPanel != null)
            {
                bool active = !gpsMenuPanel.activeSelf;
                gpsMenuPanel.SetActive(active);
                if (active && _areaButtons.Count == 0)
                {
                    PopulateMenuButtons();
                }
            }
        }

        public void ShowToast(string message)
        {
            AutoBindReferences();
            if (toastNotificationObj == null || toastNotificationText == null) return;

            toastNotificationText.text = message;
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            if (toastNotificationObj != null)
            {
                toastNotificationObj.SetActive(true);
                yield return new WaitForSeconds(3.5f);
                toastNotificationObj.SetActive(false);
            }
        }
    }
}
