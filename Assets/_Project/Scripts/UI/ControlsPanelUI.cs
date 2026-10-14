using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using CodeDrive.UI;

namespace CodeDrive.UI
{
    /// <summary>
    /// Displays the control scheme for Keyboard & Mouse.
    /// Attach to the ControlsPanel GameObject under PauseMenuPanel.
    /// </summary>
    public class ControlsPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform controlListContainer; // Parent for control entries
        [SerializeField] private GameObject controlEntryPrefab; // Prefab with Label + Key display
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text titleText;

        [Header("Theme")]
        [SerializeField] private UITheme uiTheme;

        [Header("Input Actions Asset")]
        [SerializeField] private InputActionAsset inputActionsAsset;

        private readonly System.Collections.Generic.List<GameObject> _spawnedEntries = new();

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            // Build control display from Input Actions
            BuildControlDisplay();

            // Apply theme
            if (uiTheme != null)
                ApplyTheme();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
            
            ClearEntries();
        }

        private void BuildControlDisplay()
        {
            if (controlListContainer == null || controlEntryPrefab == null)
            {
                Debug.LogWarning("[ControlsPanelUI] Missing container or prefab reference.");
                return;
            }

            ClearEntries();

            // Use provided asset or try to find the default one
            var asset = inputActionsAsset;
            if (asset == null)
            {
                // Try to find in Resources or scene
                asset = Resources.Load<InputActionAsset>("CodeDriveInputActions");
            }

            if (asset == null)
            {
                // Fallback: hardcoded controls for CodeDrive
                AddHardcodedControls();
                return;
            }

            // Find the CodeDrive action map
            var map = asset.FindActionMap("CodeDrive", throwIfNotFound: false);
            if (map == null)
            {
                AddHardcodedControls();
                return;
            }

            // Define display order and friendly names
            var displayOrder = new System.Collections.Generic.Dictionary<string, (string label, string description)>
            {
                { "Move", ("Di chuyển", "WASD hoặc Mũi tên") },
                { "Look", ("Nhìn quanh", "Di chuyển chuột") },
                { "Brake", ("Phanh / Lùi", "Space") },
                { "Interact", ("Tương tác", "E") },
                { "Pause", ("Tạm dừng / Menu", "ESC") },
                { "ResetVehicle", ("Reset Xe", "R") }
            };

            foreach (var action in map.actions)
            {
                if (displayOrder.TryGetValue(action.name, out var info))
                {
                    AddControlEntry(info.label, info.description, action);
                }
                else
                {
                    // Fallback for unknown actions
                    string bindingDisplay = GetBindingDisplayString(action);
                    AddControlEntry(action.name, bindingDisplay, action);
                }
            }
        }

        private void AddHardcodedControls()
        {
            var controls = new (string label, string key, string description)[]
            {
                ("Di chuyển", "W / A / S / D", "Hoặc phím mũi tên"),
                ("Nhìn quanh", "Di chuyển chuột", "Xoay camera"),
                ("Phanh / Lùi", "Space", "Giữ để phanh"),
                ("Tương tác", "E", "Mở bảng thông tin, v.v."),
                ("Tạm dừng / Menu", "ESC", "Mở/đóng menu tạm dừng"),
                ("Reset Xe", "R", "Đặt lại vị trí xe")
            };

            foreach (var (label, key, desc) in controls)
            {
                AddControlEntry(label, $"{key} - {desc}", null);
            }
        }

        private void AddControlEntry(string label, string keyDisplay, InputAction action)
        {
            var entry = Instantiate(controlEntryPrefab, controlListContainer);
            _spawnedEntries.Add(entry);

            // Find label and key text components
            var labelText = entry.transform.Find("LabelText")?.GetComponent<TMP_Text>();
            var keyText = entry.transform.Find("KeyText")?.GetComponent<TMP_Text>();

            // If not found by name, try getting all TMP_Text children
            if (labelText == null || keyText == null)
            {
                var texts = entry.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 2)
                {
                    labelText = texts[0];
                    keyText = texts[1];
                }
            }

            if (labelText != null)
            {
                labelText.text = label;
                if (uiTheme != null)
                    uiTheme.ApplyToText(labelText, UITheme.TextStyle.Body);
            }

            if (keyText != null)
            {
                keyText.text = keyDisplay;
                if (uiTheme != null)
                    uiTheme.ApplyToText(keyText, UITheme.TextStyle.Caption);
            }

            // Apply background styling to entry
            var entryImage = entry.GetComponent<Image>();
            if (entryImage != null && uiTheme != null)
            {
                entryImage.color = uiTheme.PanelBackground;
            }
        }

        private string GetBindingDisplayString(InputAction action)
        {
            var bindings = action.bindings;
            var displays = new System.Collections.Generic.List<string>();

            foreach (var binding in bindings)
            {
                if (binding.isComposite || binding.isPartOfComposite) continue;
                
                string display = InputControlPath.ToHumanReadableString(
                    binding.effectivePath, 
                    InputControlPath.HumanReadableStringOptions.OmitDevice
                );
                
                if (!string.IsNullOrEmpty(display))
                    displays.Add(display);
            }

            return string.Join(" / ", displays);
        }

        private void ClearEntries()
        {
            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry);
            }
            _spawnedEntries.Clear();
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        private void ApplyTheme()
        {
            if (uiTheme == null) return;

            if (titleText != null)
                uiTheme.ApplyToText(titleText, UITheme.TextStyle.Heading);

            if (closeButton != null)
                uiTheme.ApplyToButton(closeButton, closeButton.GetComponentInChildren<TMP_Text>());

            // Apply panel background
            var panelImage = GetComponent<Image>();
            var rectTransform = GetComponent<RectTransform>();
            if (panelImage != null && rectTransform != null)
                uiTheme.ApplyToPanel(panelImage, rectTransform);

            // Apply to container background if it has an Image
            if (controlListContainer != null)
            {
                var containerImage = controlListContainer.GetComponent<Image>();
                if (containerImage != null)
                    containerImage.color = uiTheme.Background;
            }
        }
    }
}