using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Smooth 4-Deck Sci-Fi Elevator controller with automatic safety shaft doors.
    /// Safety doors stay CLOSED on every deck where the cabin is not parked so the player can never fall down an empty shaft.
    /// </summary>
    public class MothershipElevator : MonoBehaviour
    {
        [Header("Deck Local Elevations (relative to HOME_MOTHERSHIP)")]
        public float[] deckLocalYs = new float[] { 2.25f, 10.25f, 16.25f, 20.25f };
        public string[] deckNames = new string[]
        {
            "DECK 01 - FLIGHT & POWER",
            "DECK 02 - OPERATIONS & RESEARCH",
            "DECK 03 - CREW & OBSERVATION",
            "DECK 04 - COMMAND"
        };

        [Header("Safety Doors (one per deck, in order Deck 1..4)")]
        public GameObject[] deckDoors = new GameObject[4];

        [Header("Motion Settings")]
        public float moveSpeed = 7.5f;
        public float cabinHalfWidth = 1.9f;
        public float callDistance = 4.8f;
        public bool autoCallOnApproach = true;

        private int _currentDeckIndex = 0;
        private int _targetDeckIndex = 0;
        private bool _isMoving = false;
        private Transform _playerTr;
        private CharacterController _playerCc;
        private bool _playerInside = false;
        private bool _playerNearOutside = false;
        private int _playerNearestDeck = 0;

        private void Start()
        {
            FindPlayer();
            EnsureSolidCabinFloor();
            SnapToDeck(_currentDeckIndex);
            UpdateSafetyDoors();
        }

        private void EnsureSolidCabinFloor()
        {
            // Guarantee a thick solid BoxCollider right on the cabin floor so player never slips through
            var col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.05f, 0f);
            col.size = new Vector3(3.9f, 0.35f, 3.9f);
        }

        private void FindPlayer()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null)
            {
                _playerTr = pc.transform;
                _playerCc = pc.GetComponent<CharacterController>();
            }
        }

        private void SnapToDeck(int idx)
        {
            if (deckLocalYs == null || deckLocalYs.Length == 0) return;
            idx = Mathf.Clamp(idx, 0, deckLocalYs.Length - 1);
            Vector3 lp = transform.localPosition;
            lp.y = deckLocalYs[idx];
            transform.localPosition = lp;
            _currentDeckIndex = idx;
            _targetDeckIndex = idx;
            _isMoving = false;
        }

        private void UpdateSafetyDoors()
        {
            if (deckDoors == null) return;
            for (int i = 0; i < deckDoors.Length; i++)
            {
                if (deckDoors[i] == null) continue;
                // Door is OPEN (inactive) ONLY when cabin is stopped at that exact deck!
                bool shouldBeClosed = _isMoving || (i != _currentDeckIndex);
                if (deckDoors[i].activeSelf != shouldBeClosed)
                    deckDoors[i].SetActive(shouldBeClosed);
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0.01f) return;
            if (_playerTr == null) { FindPlayer(); if (_playerTr == null) return; }

            Vector3 playerInShaft = transform.parent != null
                ? transform.parent.InverseTransformPoint(_playerTr.position)
                : _playerTr.position;
            Vector3 cabinLocal = transform.localPosition;

            float dx = Mathf.Abs(playerInShaft.x - cabinLocal.x);
            float dz = Mathf.Abs(playerInShaft.z - cabinLocal.z);
            float dyCabin = playerInShaft.y - cabinLocal.y;

            _playerInside = (dx < cabinHalfWidth && dz < cabinHalfWidth && dyCabin >= -0.5f && dyCabin <= 3.3f);

            _playerNearestDeck = 0;
            float bestDy = float.MaxValue;
            for (int i = 0; i < deckLocalYs.Length; i++)
            {
                float d = Mathf.Abs(playerInShaft.y - deckLocalYs[i]);
                if (d < bestDy) { bestDy = d; _playerNearestDeck = i; }
            }

            float horizDist = Mathf.Sqrt(dx * dx + dz * dz);
            _playerNearOutside = !_playerInside && horizDist < callDistance && bestDy < 2.2f;

            // Inside cabin controls
            if (_playerInside)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) GoToDeck(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) GoToDeck(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) GoToDeck(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) GoToDeck(3);
                else if (Input.GetKeyDown(KeyCode.E)) GoToDeck((_targetDeckIndex + 1) % deckLocalYs.Length);
            }
            else if (_playerNearOutside)
            {
                if (Input.GetKeyDown(KeyCode.E) || (autoCallOnApproach && !_isMoving && _currentDeckIndex != _playerNearestDeck && horizDist < 3.6f))
                {
                    GoToDeck(_playerNearestDeck);
                }
            }

            // Smooth vertical motion + carry CharacterController
            if (_isMoving)
            {
                float targetY = deckLocalYs[_targetDeckIndex];
                Vector3 prevWorldPos = transform.position;

                Vector3 lp = transform.localPosition;
                lp.y = Mathf.MoveTowards(lp.y, targetY, moveSpeed * Time.deltaTime);
                transform.localPosition = lp;

                Vector3 worldDelta = transform.position - prevWorldPos;
                if (_playerInside && _playerCc != null && _playerCc.enabled && worldDelta.sqrMagnitude > 0.000001f)
                {
                    _playerCc.Move(worldDelta);
                }

                if (Mathf.Abs(lp.y - targetY) < 0.005f)
                {
                    lp.y = targetY;
                    transform.localPosition = lp;
                    _currentDeckIndex = _targetDeckIndex;
                    _isMoving = false;
                }
            }

            UpdateSafetyDoors();
        }

        public void GoToDeck(int deckIndex)
        {
            if (deckLocalYs == null || deckIndex < 0 || deckIndex >= deckLocalYs.Length) return;
            _targetDeckIndex = deckIndex;
            _isMoving = Mathf.Abs(transform.localPosition.y - deckLocalYs[deckIndex]) > 0.02f;
            if (!_isMoving) _currentDeckIndex = deckIndex;
            UpdateSafetyDoors();
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0.01f) return;
            if (!_playerInside && !_playerNearOutside) return;

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(0.35f, 0.95f, 1f, 1f);

            if (_playerInside)
            {
                string status = _isMoving
                    ? $"MOVING TO {deckNames[_targetDeckIndex]}..."
                    : $"ELEVATOR  |  {deckNames[_currentDeckIndex]}";
                string hint = "[1] Deck 1   [2] Deck 2   [3] Deck 3   [4] Deck 4   |   [E] Next Deck";
                GUI.Box(new Rect(Screen.width * 0.5f - 280f, Screen.height - 105f, 560f, 54f), status + "\n" + hint, style);
            }
            else if (_playerNearOutside && (_isMoving || _currentDeckIndex != _playerNearestDeck))
            {
                if (_isMoving)
                {
                    WorldInteractPromptUI.DrawAtWorldPos(transform.position + Vector3.up * 1.4f, "●", $"THANG MÁY ĐANG TỚI TẦNG {_targetDeckIndex + 1}");
                }
                else
                {
                    WorldInteractPromptUI.DrawAtWorldPos(transform.position + Vector3.up * 1.4f, "E", $"GỌI THANG MÁY (TẦNG {_playerNearestDeck + 1})");
                }
            }
        }
    }
}
