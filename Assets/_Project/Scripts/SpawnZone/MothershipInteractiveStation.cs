using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceCyberpunk
{
    public enum MothershipStationType
    {
        SlidingDoor,
        CaptainHelm,
        HoloStarMap,
        ReactorCore,
        CrewTerminal,
        Seat,
        Bed,
        Container,
        NPC,
        ElevatorPad
    }

    /// <summary>
    /// Universal interactive prop, station, automatic door, elevator, and NPC system inside the HOME Mothership:
    /// - SlidingDoor: Automatically slides door_1_left / door_1_right open when PlayerCharacter approaches.
    /// - CaptainHelm / Seat: Sit down on any chair/sofa/pilot seat; press [E] or WASD to stand up.
    /// - Bed: Lie down on beds; press [E] or WASD to stand up.
    /// - HoloStarMap: Interactive 3D Galaxy Navigation & Mission Selection for all 6 Portfolio Destinations.
    /// - ReactorCore: Toggle Main Reactor Overdrive (faster ring spin + plasma light surge).
    /// - CrewTerminal / Container: Interact with Engineering, Data Center, Captain Quarters & Comm terminals.
    /// - ElevatorPad: Instant or smooth vertical transfer between Main Deck (Y=2.25) and Observation Deck (Y=7.25).
    /// - NPC: Procedural breathing/working/salute animation + multi-line dialogue on [E].
    /// Shows only a minimal "[E]" key badge for the closest interactable object in range.
    /// </summary>
    public class MothershipInteractiveStation : MonoBehaviour
    {
        public MothershipStationType stationType = MothershipStationType.CrewTerminal;
        public string stationTitle = "Hệ Thống Tàu Mẹ";
        public string promptText = "[E]";
        public string statusFeedback = "Đã kích hoạt hệ thống.";
        public string[] npcDialogues = new string[]
        {
            "Chào Chỉ huy! Mọi hệ thống trên Tàu Mẹ HOME đều đang hoạt động ổn định."
        };
        public float interactRadius = 2.8f;
        public KeyCode interactKey = KeyCode.E;

        [Header("Sliding Door References")]
        public Transform doorLeft;
        public Transform doorRight;
        public Vector3 closedLeftPos = new Vector3(1.666f, 0.0057f, 0.00f);
        public Vector3 closedRightPos = new Vector3(-1.409f, 0.00f, 0.00f);
        public Vector3 openLeftPos = new Vector3(3.15f, 0.0057f, 0.00f);
        public Vector3 openRightPos = new Vector3(-2.85f, 0.00f, 0.00f);
        public float doorSpeed = 7.0f;

        [Header("Station, Elevator & Prop References")]
        public Transform targetSeat;
        public Transform rotatingPart;
        public Light stationLight;
        public Vector3 elevatorTargetLocalPos;

        public static string SelectedDestinationName = "PROJECTS – Asteroid Citadel";

        private static readonly string[] DestinationNames =
        {
            "1. ABOUT – Personal Info & Bio Sector (1,450m)",
            "2. SKILLS – Tech Arsenal & Core Matrix (1,820m)",
            "3. EDUCATION – Academic Star Academy (2,100m)",
            "4. EXPERIENCE – Career Fleet Outpost (2,350m)",
            "5. PROJECTS – Asteroid Colony Citadel (1,950m)",
            "6. CONTACT – Deep-Space Comm Relay (1,680m)"
        };

        private Transform _npcHead;
        private Transform _npcBody;
        private Transform _npcArmL;
        private Transform _npcArmR;
        private Vector3 _npcBodyBasePos;
        private int _dialogueIndex;

        private Transform _player;
        private Animator _doorAnimator;
        private float _currentOpen;
        private bool _doorInitialized;
        private bool _isActivated;
        private bool _showPanel;
        private bool _isOccupiedPose;
        private float _toastTimer;
        private string _activeToastText;
        private Vector3 _savedPlayerPos;
        private Quaternion _savedPlayerRot;
        private Vector3 _baseLocalScale;
        private float _pulseAnimTimer;

        private static MothershipInteractiveStation _closestActiveStation;
        private static float _closestActiveDist = 999f;
        private static int _lastTrackedFrame = -1;

        private void Awake()
        {
            _baseLocalScale = transform.localScale;
            if (stationType == MothershipStationType.SlidingDoor)
            {
                InitDoorLeaves();
            }
            else if (stationType == MothershipStationType.NPC)
            {
                ResolveNpcLimbs();
            }
        }

        private void Start()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null) _player = pc.transform;

            if (stationType == MothershipStationType.SlidingDoor)
            {
                InitDoorLeaves();
                ApplyDoorLeaves(0f);
            }
            else if (stationType == MothershipStationType.NPC)
            {
                ResolveNpcLimbs();
            }
        }

        private void ResolveNpcLimbs()
        {
            if (_npcHead == null) _npcHead = FindDeepChild(transform, "head");
            if (_npcBody == null) _npcBody = FindDeepChild(transform, "body");
            if (_npcArmL == null) _npcArmL = FindDeepChild(transform, "armLeft");
            if (_npcArmR == null) _npcArmR = FindDeepChild(transform, "armRight");
            if (_npcBody != null) _npcBodyBasePos = _npcBody.localPosition;
        }

        private void InitDoorLeaves()
        {
            foreach (var anim in GetComponentsInChildren<Animator>(true))
            {
                if (anim != null && anim.enabled) anim.enabled = false;
            }

            if (_doorInitialized && doorLeft != null && doorRight != null) return;

            if (doorLeft == null) doorLeft = FindDeepChild(transform, "door_1_left");
            if (doorRight == null) doorRight = FindDeepChild(transform, "door_1_right");

            if (doorLeft != null)
            {
                doorLeft.localScale = new Vector3(1.20f, 1.0f, 0.8f);
                var bc = doorLeft.GetComponent<BoxCollider>();
                if (bc == null) bc = doorLeft.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(-0.72f, 2.0f, 0f);
                bc.size = new Vector3(1.35f, 3.9f, 0.35f);
                bc.enabled = true;
            }
            if (doorRight != null)
            {
                doorRight.localScale = new Vector3(1.0f, 1.0f, 0.8f);
                var bc = doorRight.GetComponent<BoxCollider>();
                if (bc == null) bc = doorRight.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0.86f, 2.0f, 0f);
                bc.size = new Vector3(1.62f, 3.9f, 0.35f);
                bc.enabled = true;
            }

            _doorInitialized = true;
        }

        private void Update()
        {
            if (_player == null)
            {
                var pc = FindFirstObjectByType<PlayerCharacterController>();
                if (pc != null) _player = pc.transform;
            }

            float dist = 999f;
            if (_player != null && _player.gameObject.activeInHierarchy)
            {
                dist = Vector3.Distance(_player.position, transform.position);
            }
            if (Camera.main != null && stationType == MothershipStationType.SlidingDoor)
            {
                dist = Mathf.Min(dist, Vector3.Distance(Camera.main.transform.position, transform.position));
            }

            if (stationType == MothershipStationType.SlidingDoor)
            {
                InitDoorLeaves();
                float targetOpen = dist <= interactRadius ? 1f : 0f;
                _currentOpen = Mathf.MoveTowards(_currentOpen, targetOpen, Time.deltaTime * doorSpeed);
                return;
            }

            if (_lastTrackedFrame != Time.frameCount)
            {
                _lastTrackedFrame = Time.frameCount;
                _closestActiveStation = null;
                _closestActiveDist = 999f;
            }

            bool inRange = dist <= interactRadius;
            if (inRange && dist < _closestActiveDist)
            {
                _closestActiveDist = dist;
                _closestActiveStation = this;
            }

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
            }

            if (_pulseAnimTimer > 0f)
            {
                _pulseAnimTimer -= Time.deltaTime;
                float s = 1f + Mathf.Sin(_pulseAnimTimer * 24f) * 0.045f;
                transform.localScale = _baseLocalScale * s;
                if (_pulseAnimTimer <= 0f) transform.localScale = _baseLocalScale;
            }

            if (stationType == MothershipStationType.ReactorCore && rotatingPart != null)
            {
                float spd = _isActivated ? 160f : 35f;
                rotatingPart.Rotate(Vector3.up, spd * Time.deltaTime, Space.Self);
            }

            if (stationType == MothershipStationType.NPC)
            {
                UpdateNpcAnimation(inRange, Time.deltaTime);
            }

            if (_isOccupiedPose && (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.2f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.2f || Input.GetKeyDown(KeyCode.Space)))
            {
                ReleaseOccupiedPose();
                return;
            }

            if (!inRange && _showPanel && !_isOccupiedPose)
            {
                _showPanel = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (stationType == MothershipStationType.SlidingDoor)
            {
                if (_doorAnimator != null && _doorAnimator.enabled)
                {
                    _doorAnimator.enabled = false;
                }
                ApplyDoorLeaves(_currentOpen);
                return;
            }

            if (_closestActiveStation == this && Input.GetKeyDown(interactKey))
            {
                TriggerInteraction();
            }
        }

        private void UpdateNpcAnimation(bool playerNear, float dt)
        {
            float t = Time.time;
            if (_npcBody != null)
            {
                float breath = Mathf.Sin(t * 2.4f) * 0.008f;
                _npcBody.localPosition = _npcBodyBasePos + new Vector3(0f, breath, 0f);
            }

            if (playerNear && _player != null)
            {
                Vector3 toPlayer = _player.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.05f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, dt * 5f);
                }

                if (_npcArmR != null)
                {
                    float wave = _showPanel ? Mathf.Sin(t * 8f) * 14f : Mathf.Sin(t * 3f) * 5f;
                    float pitch = _showPanel ? -115f : -35f;
                    _npcArmR.localRotation = Quaternion.Slerp(_npcArmR.localRotation, Quaternion.Euler(pitch, 0f, 15f + wave), dt * 6f);
                }
                if (_npcArmL != null)
                {
                    _npcArmL.localRotation = Quaternion.Slerp(_npcArmL.localRotation, Quaternion.Euler(-15f, 0f, 0f), dt * 6f);
                }
            }
            else
            {
                if (_npcArmL != null)
                {
                    float tapL = Mathf.Sin(t * 5.5f) * 8f;
                    _npcArmL.localRotation = Quaternion.Slerp(_npcArmL.localRotation, Quaternion.Euler(-48f + tapL, -6f, 0f), dt * 5f);
                }
                if (_npcArmR != null)
                {
                    float tapR = Mathf.Cos(t * 5.5f) * 8f;
                    _npcArmR.localRotation = Quaternion.Slerp(_npcArmR.localRotation, Quaternion.Euler(-48f + tapR, 6f, 0f), dt * 5f);
                }
            }
        }

        private void ApplyDoorLeaves(float openNormalized)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(openNormalized));
            if (doorLeft != null)
            {
                doorLeft.localPosition = Vector3.Lerp(closedLeftPos, openLeftPos, t);
            }
            if (doorRight != null)
            {
                doorRight.localPosition = Vector3.Lerp(closedRightPos, openRightPos, t);
            }
        }

        private void TriggerInteraction()
        {
            switch (stationType)
            {
                case MothershipStationType.CaptainHelm:
                case MothershipStationType.Seat:
                    if (_isOccupiedPose)
                    {
                        ReleaseOccupiedPose();
                    }
                    else
                    {
                        EnterOccupiedPose(isBed: false);
                        ShowToast(stationTitle + ": Đang ngồi");
                    }
                    break;

                case MothershipStationType.Bed:
                    if (_isOccupiedPose)
                    {
                        ReleaseOccupiedPose();
                    }
                    else
                    {
                        EnterOccupiedPose(isBed: true);
                        ShowToast(stationTitle + ": Đang nằm nghỉ");
                    }
                    break;

                case MothershipStationType.ElevatorPad:
                    if (_player != null)
                    {
                        var sz = GameObject.Find("Spawn_Zone");
                        Vector3 worldDest = sz != null
                            ? sz.transform.TransformPoint(elevatorTargetLocalPos)
                            : transform.position + new Vector3(0f, 5.0f, 0f);
                        var cc = _player.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        _player.position = worldDest + new Vector3(0f, 0.15f, 0f);
                        if (cc != null) cc.enabled = true;
                        ShowToast(stationTitle + ": " + statusFeedback);
                    }
                    break;

                case MothershipStationType.HoloStarMap:
                    _showPanel = !_showPanel;
                    Cursor.lockState = _showPanel ? CursorLockMode.None : CursorLockMode.Locked;
                    Cursor.visible = _showPanel;
                    break;

                case MothershipStationType.ReactorCore:
                    _isActivated = !_isActivated;
                    _pulseAnimTimer = 0.35f;
                    if (stationLight != null)
                    {
                        stationLight.intensity = _isActivated ? 8.0f : 3.8f;
                        stationLight.color = _isActivated ? new Color(0.6f, 0.35f, 1f) : new Color(0.15f, 0.92f, 1f);
                    }
                    ShowToast(stationTitle + (_isActivated ? ": OVERDRIVE 160%" : ": ỔN ĐỊNH 100%"));
                    break;

                case MothershipStationType.CrewTerminal:
                    _isActivated = !_isActivated;
                    _pulseAnimTimer = 0.3f;
                    ToggleStationGlowLight(_isActivated, new Color(0.2f, 0.95f, 1f));
                    ShowToast(stationTitle + ": " + statusFeedback);
                    break;

                case MothershipStationType.Container:
                    _isActivated = !_isActivated;
                    _pulseAnimTimer = 0.4f;
                    ToggleStationGlowLight(_isActivated, new Color(0.3f, 1f, 0.55f));
                    ShowToast(stationTitle + ": " + statusFeedback);
                    break;

                case MothershipStationType.NPC:
                    if (!_showPanel)
                    {
                        _showPanel = true;
                        _dialogueIndex = 0;
                    }
                    else
                    {
                        _dialogueIndex++;
                        if (npcDialogues == null || _dialogueIndex >= npcDialogues.Length)
                        {
                            _showPanel = false;
                            _dialogueIndex = 0;
                        }
                    }
                    break;
            }
        }

        private void EnterOccupiedPose(bool isBed)
        {
            if (_player == null) return;
            var cc = _player.GetComponent<CharacterController>();
            _savedPlayerPos = _player.position;
            _savedPlayerRot = _player.rotation;
            _isOccupiedPose = true;

            Transform anchor = targetSeat != null ? targetSeat : transform;
            if (cc != null) cc.enabled = false;
            _player.position = anchor.position + (isBed ? new Vector3(0f, 0.55f, 0f) : new Vector3(0f, 0.15f, 0f));
            _player.rotation = anchor.rotation;
            if (cc != null) cc.enabled = true;
        }

        private void ReleaseOccupiedPose()
        {
            if (!_isOccupiedPose || _player == null) return;
            var cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            _player.position = _savedPlayerPos;
            _player.rotation = _savedPlayerRot;
            if (cc != null) cc.enabled = true;
            _isOccupiedPose = false;
            _showPanel = false;
        }

        private void ToggleStationGlowLight(bool active, Color col)
        {
            if (stationLight == null)
            {
                var ltGo = new GameObject("Interactive_Glow");
                ltGo.transform.SetParent(transform, false);
                ltGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                stationLight = ltGo.AddComponent<Light>();
                stationLight.type = LightType.Point;
                stationLight.range = 4.5f;
            }
            stationLight.color = col;
            stationLight.intensity = active ? 3.2f : 0.8f;
        }

        private void ShowToast(string msg)
        {
            _activeToastText = msg;
            _toastTimer = 2.8f;
        }

        private void OnGUI()
        {
            if (stationType == MothershipStationType.SlidingDoor || Time.timeScale <= 0.01f) return;

            if (_closestActiveStation == this)
            {
                var keyStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                keyStyle.normal.textColor = new Color(0.45f, 1f, 0.9f, 1f);
                GUI.Box(new Rect(Screen.width * 0.5f - 28f, Screen.height - 140f, 56f, 34f), "[E]", keyStyle);
            }

            if (_toastTimer > 0f && !string.IsNullOrEmpty(_activeToastText))
            {
                var toastStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                toastStyle.normal.textColor = new Color(0.4f, 1f, 0.85f, 1f);
                GUI.Box(new Rect(Screen.width * 0.5f - 240f, Screen.height - 190f, 480f, 36f), _activeToastText, toastStyle);
            }

            if (!_showPanel || _closestActiveStation != this) return;

            var boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            boxStyle.normal.textColor = new Color(0.4f, 1f, 0.9f, 1f);

            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            bodyStyle.normal.textColor = Color.white;

            if (stationType == MothershipStationType.NPC)
            {
                Rect dlgRect = new Rect(Screen.width * 0.5f - 260f, Screen.height - 245f, 520f, 95f);
                GUI.Box(dlgRect, stationTitle, boxStyle);
                string line = (npcDialogues != null && _dialogueIndex < npcDialogues.Length)
                    ? npcDialogues[_dialogueIndex]
                    : "Chào Chỉ huy!";
                GUI.Label(new Rect(dlgRect.x + 18f, dlgRect.y + 28f, dlgRect.width - 36f, 58f), line, bodyStyle);
            }
            else if (stationType == MothershipStationType.HoloStarMap)
            {
                Rect panelRect = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 175f, 460f, 350f);
                GUI.Box(panelRect, stationTitle + " | Đích: " + SelectedDestinationName, boxStyle);

                for (int i = 0; i < DestinationNames.Length; i++)
                {
                    if (GUI.Button(new Rect(panelRect.x + 25f, panelRect.y + 38f + i * 38f, 410f, 32f), DestinationNames[i]))
                    {
                        SelectedDestinationName = DestinationNames[i];
                        ShowToast("Đã khóa mục tiêu hành trình: " + SelectedDestinationName);
                    }
                }

                if (GUI.Button(new Rect(panelRect.x + 25f, panelRect.y + 278f, 260f, 42f), "KHỞI HÀNH TỚI VÙNG ĐÃ CHỌN"))
                {
                    SceneManager.LoadScene("New Scene");
                }
                if (GUI.Button(new Rect(panelRect.x + 300f, panelRect.y + 278f, 135f, 42f), "Đóng"))
                {
                    _showPanel = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (string.Equals(c.name, childName, System.StringComparison.OrdinalIgnoreCase))
                    return c;
                var found = FindDeepChild(c, childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
