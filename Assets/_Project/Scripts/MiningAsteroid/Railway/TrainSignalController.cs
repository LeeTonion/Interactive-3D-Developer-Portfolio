using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Feature D: Interactive Railway Control Terminal allowing the player to switch
    /// track signals (RED/GREEN), call an approaching train to the controlled station,
    /// and toggle railway navigation beacon lights without disrupting autonomous train routes.
    /// </summary>
    public class TrainSignalController : MonoBehaviour
    {
        public enum SignalState
        {
            Green,
            Red
        }

        [Header("Railway References")]
        public MiningRailwaySystem railwaySystem;
        public MiningStationDock controlledStation;
        [Tooltip("Distance along MiningRailwayCircuit where trains brake cleanly for a RED signal.")]
        public float signalStopDistance = 53.8f;
        public float incomingThresholdDistance = 42f;

        [Header("Interaction Settings")]
        public float interactionRadius = 18f;
        public KeyCode switchSignalKey = KeyCode.Alpha1;
        public KeyCode callTrainKey = KeyCode.Alpha2;
        public KeyCode toggleLightsKey = KeyCode.Alpha3;

        [Header("Terminal Visuals & Track Signal Beacon")]
        public Renderer terminalScreenRenderer;
        public Light terminalStatusLight;
        public Light trackSignalLight;
        public Renderer trackSignalBulbRenderer;
        public TextMeshPro worldScreenText;

        [Header("Signal Colors")]
        public Color greenSignalColor = new Color(0.15f, 1f, 0.45f, 1f);
        public Color redSignalColor = new Color(1f, 0.22f, 0.25f, 1f);
        public Color hudAccentCyan = new Color(0.15f, 0.88f, 1f, 1f);

        private SignalState _currentSignal = SignalState.Green;
        private Transform _playerTransform;
        private bool _isPanelOpen;
        private bool _isPlayerInside;
        private Material _screenMatInst;
        private Material _bulbMatInst;

        // Compact Corner UI Panel
        private GameObject _panelRoot;
        private TextMeshProUGUI _signalValueText;
        private TextMeshProUGUI _nextTrainValueText;
        private TextMeshProUGUI _stationValueText;
        private TextMeshProUGUI _lightsValueText;
        private Image _signalBadgeBg;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public SignalState CurrentSignal => _currentSignal;
        public bool IsSignalRed => _currentSignal == SignalState.Red;
        public bool IsPanelOpen => _isPanelOpen;

        private void Start()
        {
            EnsureVisualMaterialInstances();
            ApplySignalVisuals();
        }

        private void OnDisable()
        {
            SetPanelOpen(false);
        }

        private void EnsureVisualMaterialInstances()
        {
            if (terminalScreenRenderer != null && _screenMatInst == null)
            {
                _screenMatInst = new Material(terminalScreenRenderer.sharedMaterial);
                _screenMatInst.EnableKeyword("_EMISSION");
                terminalScreenRenderer.sharedMaterial = _screenMatInst;
            }

            if (trackSignalBulbRenderer != null && _bulbMatInst == null)
            {
                _bulbMatInst = new Material(trackSignalBulbRenderer.sharedMaterial);
                _bulbMatInst.EnableKeyword("_EMISSION");
                trackSignalBulbRenderer.sharedMaterial = _bulbMatInst;
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                _playerTransform = AsteroidInteractionHUD.GetPlayerTransform();
                if (_playerTransform == null) return;
            }

            bool suppress = AboutInfoStation.AnyStationActive
                            || AboutInfoStation.AnyLandingSequenceActive
                            || SpawnZoneAnimator.IsAnyIntroWalkActive;

            if (suppress)
            {
                if (_isPanelOpen) SetPanelOpen(false);
                return;
            }

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            if (dist <= interactionRadius)
            {
                if (!_isPlayerInside)
                {
                    _isPlayerInside = true;
                    UpdateTerminalScreenGlow(true);
                }

                string prompt = _isPanelOpen
                    ? "[F] CLOSE RAILWAY CONTROL  |  [1] SIGNAL  [2] CALL  [3] LIGHTS"
                    : "[F] OPEN RAILWAY CONTROL TERMINAL";
                Color promptColor = _currentSignal == SignalState.Red ? redSignalColor : hudAccentCyan;
                AsteroidInteractionHUD.ReportCandidate(this, dist, prompt, promptColor);

                if (AsteroidInteractionHUD.ConsumeInteractPress(this))
                {
                    SetPanelOpen(!_isPanelOpen);
                }
                else if (_isPanelOpen)
                {
                    if (Input.GetKeyDown(switchSignalKey)) SwitchSignal();
                    else if (Input.GetKeyDown(callTrainKey)) CallNextTrain();
                    else if (Input.GetKeyDown(toggleLightsKey)) ToggleRailwayLights();
                    else if (Input.GetKeyDown(KeyCode.Escape)) SetPanelOpen(false);
                }
            }
            else if (dist > interactionRadius + 4.5f)
            {
                if (_isPlayerInside)
                {
                    _isPlayerInside = false;
                    UpdateTerminalScreenGlow(false);
                }
                if (_isPanelOpen) SetPanelOpen(false);
            }

            if (_isPanelOpen)
            {
                RefreshPanelStatus();
            }
        }

        public void SetPanelOpen(bool open)
        {
            if (_isPanelOpen == open) return;
            _isPanelOpen = open;
            AsteroidInteractionHUD.SetRailwayPanelOpen(open);

            if (open)
            {
                EnsurePanelBuilt();
                RefreshPanelStatus();
                AsteroidInteractionHUD.PlayTerminalClick();
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(open);
            }
        }

        public void SwitchSignal()
        {
            _currentSignal = _currentSignal == SignalState.Green ? SignalState.Red : SignalState.Green;
            ApplySignalVisuals();
            RefreshPanelStatus();
            AsteroidInteractionHUD.PlayTerminalClick();
            Color col = _currentSignal == SignalState.Red ? redSignalColor : greenSignalColor;
            AsteroidInteractionHUD.ShowNotification($"RAILWAY SIGNAL SET TO {_currentSignal.ToString().ToUpper()}", col, 2.2f);
        }

        public void SetSignalState(SignalState state)
        {
            _currentSignal = state;
            ApplySignalVisuals();
            RefreshPanelStatus();
        }

        public void CallNextTrain()
        {
            if (railwaySystem == null || railwaySystem.activeTrains == null || railwaySystem.circuit == null || controlledStation == null)
                return;

            float loopLen = railwaySystem.circuit.TotalLength;
            float stationDist = Mathf.Repeat(controlledStation.circuitDistance, loopLen);

            OrbitalMiningTrain bestTrain = null;
            float bestApproach = float.MaxValue;

            for (int i = 0; i < railwaySystem.activeTrains.Count; i++)
            {
                var tr = railwaySystem.activeTrains[i];
                if (tr == null) continue;
                if (tr.DockedStation == controlledStation)
                {
                    if (_currentSignal == SignalState.Red)
                    {
                        _currentSignal = SignalState.Green;
                        ApplySignalVisuals();
                        RefreshPanelStatus();
                    }
                    AsteroidInteractionHUD.ShowNotification("TRAIN ALREADY AT STATION", greenSignalColor, 2.0f);
                    return;
                }

                float trDist = Mathf.Repeat(tr.CurrentDistance, loopLen);
                float distToStation = stationDist - trDist;
                if (distToStation < 0f) distToStation += loopLen;

                if (distToStation < bestApproach)
                {
                    bestApproach = distToStation;
                    bestTrain = tr;
                }
            }

            if (bestTrain != null)
            {
                bestTrain.CallToStation(controlledStation);
                if (_currentSignal == SignalState.Red)
                {
                    _currentSignal = SignalState.Green;
                    ApplySignalVisuals();
                }
                RefreshPanelStatus();
                AsteroidInteractionHUD.PlayTerminalClick();
                AsteroidInteractionHUD.ShowNotification($"DISPATCHED {bestTrain.trainID.ToUpper()} TO STATION", hudAccentCyan, 2.6f);
            }
        }

        public void ToggleRailwayLights()
        {
            if (railwaySystem == null) return;
            bool nextState = !railwaySystem.AreLightsEnabled;
            railwaySystem.SetNavigationLights(nextState);
            RefreshPanelStatus();
            AsteroidInteractionHUD.PlayTerminalClick();
            AsteroidInteractionHUD.ShowNotification(
                nextState ? "RAILWAY LIGHTS ACTIVATED" : "RAILWAY LIGHTS DIMMED",
                hudAccentCyan,
                2.0f
            );
        }

        private bool IsAnyTrainIncoming()
        {
            if (railwaySystem == null || railwaySystem.activeTrains == null || railwaySystem.circuit == null || controlledStation == null)
                return false;

            float loopLen = railwaySystem.circuit.TotalLength;
            float stationDist = Mathf.Repeat(controlledStation.circuitDistance, loopLen);

            for (int i = 0; i < railwaySystem.activeTrains.Count; i++)
            {
                var tr = railwaySystem.activeTrains[i];
                if (tr == null) continue;
                if (tr.CalledTargetStation == controlledStation) return true;

                float trDist = Mathf.Repeat(tr.CurrentDistance, loopLen);
                float ahead = stationDist - trDist;
                if (ahead < 0f) ahead += loopLen;
                if (ahead > 0f && ahead <= incomingThresholdDistance) return true;
            }
            return false;
        }

        private void ApplySignalVisuals()
        {
            EnsureVisualMaterialInstances();
            Color col = _currentSignal == SignalState.Red ? redSignalColor : greenSignalColor;

            if (trackSignalLight != null)
            {
                trackSignalLight.color = col;
                trackSignalLight.intensity = 3.2f;
            }

            if (terminalStatusLight != null)
            {
                terminalStatusLight.color = col;
                terminalStatusLight.intensity = _isPlayerInside ? 2.8f : 1.6f;
            }

            if (_bulbMatInst != null)
            {
                _bulbMatInst.SetColor("_BaseColor", col);
                _bulbMatInst.SetColor(EmissionColorId, col * 3.5f);
            }

            if (worldScreenText != null)
            {
                string sigStr = _currentSignal == SignalState.Red ? "<color=#FF4A55>RED</color>" : "<color=#33FF77>GREEN</color>";
                worldScreenText.text = $"RAILWAY CONTROL\nSIGNAL: {sigStr}";
            }
        }

        private void UpdateTerminalScreenGlow(bool active)
        {
            EnsureVisualMaterialInstances();
            if (_screenMatInst != null)
            {
                Color c = active ? hudAccentCyan : (hudAccentCyan * 0.45f);
                _screenMatInst.SetColor(EmissionColorId, c * (active ? 2.2f : 0.9f));
            }
            if (terminalStatusLight != null)
            {
                terminalStatusLight.intensity = active ? 3.0f : 1.5f;
            }
        }

        private void RefreshPanelStatus()
        {
            if (_panelRoot == null) return;

            bool isRed = _currentSignal == SignalState.Red;
            _signalValueText.text = isRed ? "RED" : "GREEN";
            _signalValueText.color = isRed ? redSignalColor : greenSignalColor;
            if (_signalBadgeBg != null)
            {
                _signalBadgeBg.color = isRed
                    ? new Color(0.35f, 0.06f, 0.08f, 0.92f)
                    : new Color(0.05f, 0.28f, 0.12f, 0.92f);
            }

            bool incoming = IsAnyTrainIncoming();
            _nextTrainValueText.text = incoming ? "INCOMING" : "NO TRAIN";
            _nextTrainValueText.color = incoming ? hudAccentCyan : new Color(0.65f, 0.72f, 0.80f, 1f);

            bool stationBusy = controlledStation != null && (controlledStation.IsOccupied || controlledStation.IsLoadingActive);
            bool stationActive = stationBusy || incoming || _isPlayerInside;
            _stationValueText.text = stationActive ? "ACTIVE" : "INACTIVE";
            _stationValueText.color = stationBusy ? new Color(1f, 0.84f, 0.25f, 1f)
                                    : (stationActive ? greenSignalColor : new Color(0.65f, 0.72f, 0.80f, 1f));

            if (_lightsValueText != null && railwaySystem != null)
            {
                _lightsValueText.text = railwaySystem.AreLightsEnabled ? "LIGHTS: ON" : "LIGHTS: OFF";
            }
        }

        private void EnsurePanelBuilt()
        {
            if (_panelRoot != null) return;
            var hud = AsteroidInteractionHUD.Instance;
            if (hud == null || hud.HudRootTransform == null) return;

            _panelRoot = new GameObject("RailwayControlPanel");
            _panelRoot.transform.SetParent(hud.HudRootTransform, false);
            var rt = _panelRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-26f, 0f);
            rt.sizeDelta = new Vector2(310f, 250f);

            var borderImg = _panelRoot.AddComponent<Image>();
            borderImg.color = new Color(0.15f, 0.85f, 1f, 0.85f);

            var bgGo = new GameObject("InnerPanel");
            bgGo.transform.SetParent(rt, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f);
            bgRt.offsetMax = new Vector2(-2f, -2f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.06f, 0.12f, 0.94f);

            // Title
            var title = CreateText(bgRt, "Title", "RAILWAY CONTROL", new Vector2(0f, -14f), new Vector2(280f, 26f), 16f, FontStyles.Bold, hudAccentCyan, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);

            // Status Rows
            CreateStatusRow(bgRt, "SignalRow", "Signal:", -48f, out _signalValueText, out _signalBadgeBg);
            CreateStatusRow(bgRt, "TrainRow", "Next Train:", -78f, out _nextTrainValueText, out _);
            CreateStatusRow(bgRt, "StationRow", "Station:", -108f, out _stationValueText, out _);

            // Compact Action Buttons
            CreateButton(bgRt, "BtnSwitch", "[1] SWITCH SIGNAL", new Vector2(0f, -146f), new Vector2(276f, 28f), SwitchSignal, out _);
            CreateButton(bgRt, "BtnCall", "[2] CALL TRAIN", new Vector2(0f, -180f), new Vector2(276f, 28f), CallNextTrain, out _);
            CreateButton(bgRt, "BtnLights", "[3] LIGHTS: ON", new Vector2(0f, -214f), new Vector2(276f, 28f), ToggleRailwayLights, out _lightsValueText);

            _panelRoot.SetActive(false);
        }

        private void CreateStatusRow(RectTransform parent, string rowName, string keyText, float yOffset, out TextMeshProUGUI valueText, out Image badgeBg)
        {
            var rowGo = new GameObject(rowName);
            rowGo.transform.SetParent(parent, false);
            var rRt = rowGo.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 1f);
            rRt.anchorMax = new Vector2(0.5f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(0f, yOffset);
            rRt.sizeDelta = new Vector2(276f, 24f);

            badgeBg = rowGo.AddComponent<Image>();
            badgeBg.color = new Color(0.06f, 0.11f, 0.20f, 0.8f);

            var keyTmp = CreateText(rRt, "Key", keyText, new Vector2(8f, 0f), new Vector2(130f, 24f), 14f, FontStyles.Normal, new Color(0.75f, 0.85f, 0.95f, 1f), TextAlignmentOptions.Left);
            keyTmp.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            keyTmp.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            keyTmp.rectTransform.pivot = new Vector2(0f, 0.5f);

            valueText = CreateText(rRt, "Val", "--", new Vector2(-8f, 0f), new Vector2(130f, 24f), 14f, FontStyles.Bold, Color.white, TextAlignmentOptions.Right);
            valueText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            valueText.rectTransform.pivot = new Vector2(1f, 0.5f);
        }

        private void CreateButton(RectTransform parent, string btnName, string text, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI labelOut)
        {
            var go = new GameObject(btnName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.07f, 0.20f, 0.34f, 0.96f);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            labelOut = CreateText(rt, "Label", text, Vector2.zero, size, 13.5f, FontStyles.Bold, new Color(0.88f, 0.97f, 1f, 1f), TextAlignmentOptions.Center);
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string objName, string text, Vector2 anchoredPos, Vector2 size, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }
    }
}
