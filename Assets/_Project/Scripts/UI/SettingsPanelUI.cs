using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using CodeDrive.UI;

namespace CodeDrive.UI
{
    /// <summary>
    /// Handles the Settings Panel UI: Audio, Graphics, Gameplay settings.
    /// Attach to the SettingsPanel GameObject under PauseMenuPanel.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [Header("UI References - Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeValueText;
        [SerializeField] private TMP_Text musicVolumeValueText;
        [SerializeField] private TMP_Text sfxVolumeValueText;

        [Header("UI References - Graphics")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;

        [Header("UI References - Gameplay")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TMP_Text mouseSensitivityValueText;
        [SerializeField] private Toggle invertYAxisToggle;

        [Header("Theme")]
        [SerializeField] private UITheme uiTheme;

        [Header("Audio Mixer (Optional)")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        private Resolution[] _resolutions;
        private float _defaultMouseSensitivity = 1f;

        private void Awake()
        {
            // Force compilation timestamp
            System.Diagnostics.Debug.WriteLine($"SettingsPanelUI compiled at {System.DateTime.Now}");
            
            // Load saved settings
            LoadSettings();

            // Setup listeners
            SetupAudioListeners();
            SetupGraphicsListeners();
            SetupGameplayListeners();

            // Initialize resolution dropdown
            InitializeResolutionDropdown();

            // Apply theme
            if (uiTheme != null)
                ApplyTheme();
        }

        private void SetupAudioListeners()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                UpdateVolumeText(masterVolumeSlider, masterVolumeValueText);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                UpdateVolumeText(musicVolumeSlider, musicVolumeValueText);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
                UpdateVolumeText(sfxVolumeSlider, sfxVolumeValueText);
            }
        }

        private void SetupGraphicsListeners()
        {
            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
                fullscreenToggle.isOn = Screen.fullScreen;
            }
            if (vsyncToggle != null)
            {
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
                vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            }
        }

        private void SetupGameplayListeners()
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
                UpdateSliderValueText(mouseSensitivitySlider, mouseSensitivityValueText);
            }
            if (invertYAxisToggle != null)
                invertYAxisToggle.onValueChanged.AddListener(OnInvertYAxisChanged);
        }

        private void InitializeResolutionDropdown()
        {
            if (resolutionDropdown == null) return;

            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = $"{_resolutions[i].width} x {_resolutions[i].height} @ {(float)_resolutions[i].refreshRateRatio.value:F1}Hz";
                options.Add(option);

                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height &&
                    Mathf.Approximately((float)_resolutions[i].refreshRateRatio.value, (float)Screen.currentResolution.refreshRateRatio.value))
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        private void LoadSettings()
        {
            // Audio
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

            if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
            if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;

            ApplyAudioSettings(masterVol, musicVol, sfxVol);

            // Graphics
            int qualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            QualitySettings.SetQualityLevel(qualityLevel);
            if (qualityDropdown != null) qualityDropdown.value = qualityLevel;

            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            Screen.fullScreen = fullscreen;
            if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;

            int vsync = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount > 0 ? 1 : 0);
            QualitySettings.vSyncCount = vsync;
            if (vsyncToggle != null) vsyncToggle.isOn = vsync > 0;

            // Gameplay
            float mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", _defaultMouseSensitivity);
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = mouseSens;

            bool invertY = PlayerPrefs.GetInt("InvertYAxis", 0) == 1;
            if (invertYAxisToggle != null) invertYAxisToggle.isOn = invertY;
        }

        // Audio callbacks
        private void OnMasterVolumeChanged(float value)
        {
            UpdateVolumeText(masterVolumeSlider, masterVolumeValueText);
            PlayerPrefs.SetFloat("MasterVolume", value);
            ApplyAudioSettings(value, PlayerPrefs.GetFloat("MusicVolume", 1f), PlayerPrefs.GetFloat("SFXVolume", 1f));
        }

        private void OnMusicVolumeChanged(float value)
        {
            UpdateVolumeText(musicVolumeSlider, musicVolumeValueText);
            PlayerPrefs.SetFloat("MusicVolume", value);
            ApplyAudioSettings(PlayerPrefs.GetFloat("MasterVolume", 1f), value, PlayerPrefs.GetFloat("SFXVolume", 1f));
        }

        private void OnSfxVolumeChanged(float value)
        {
            UpdateVolumeText(sfxVolumeSlider, sfxVolumeValueText);
            PlayerPrefs.SetFloat("SFXVolume", value);
            ApplyAudioSettings(PlayerPrefs.GetFloat("MasterVolume", 1f), PlayerPrefs.GetFloat("MusicVolume", 1f), value);
        }

        private void ApplyAudioSettings(float master, float music, float sfx)
        {
            if (audioMixer != null)
            {
                // Convert 0-1 to dB (-80 to 0) - Mathf.Log10 returns double, cast to float
                float masterDb = master > 0 ? (float)(Mathf.Log10(master) * 20.0) : -80f;
                float musicDb = music > 0 ? (float)(Mathf.Log10(music) * 20.0) : -80f;
                float sfxDb = sfx > 0 ? (float)(Mathf.Log10(sfx) * 20.0) : -80f;

                audioMixer.SetFloat(masterVolumeParam, masterDb);
                audioMixer.SetFloat(musicVolumeParam, musicDb);
                audioMixer.SetFloat(sfxVolumeParam, sfxDb);
            }
            else
            {
                // Fallback: use AudioListener volume for master
                AudioListener.volume = master;
            }
        }

        // Graphics callbacks
        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index);
            PlayerPrefs.SetInt("QualityLevel", index);
        }

        private void OnResolutionChanged(int index)
        {
            if (index >= 0 && index < _resolutions.Length)
            {
                Resolution res = _resolutions[index];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
                PlayerPrefs.SetInt("ResolutionWidth", res.width);
                PlayerPrefs.SetInt("ResolutionHeight", res.height);
            }
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        }

        private void OnVSyncChanged(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            PlayerPrefs.SetInt("VSync", enabled ? 1 : 0);
        }

        // Gameplay callbacks
        private void OnMouseSensitivityChanged(float value)
        {
            UpdateSliderValueText(mouseSensitivitySlider, mouseSensitivityValueText);
            PlayerPrefs.SetFloat("MouseSensitivity", value);
            
            // Apply to camera control if available
            var cameraControl = FindObjectOfType<CameraSystem.CameraControl>();
            if (cameraControl != null)
            {
                // CameraControl would need a public sensitivity property
                // cameraControl.MouseSensitivity = value;
            }
        }

        private void OnInvertYAxisChanged(bool invert)
        {
            PlayerPrefs.SetInt("InvertYAxis", invert ? 1 : 0);
            
            // Apply to camera control if available
            var cameraControl = FindObjectOfType<CameraSystem.CameraControl>();
            if (cameraControl != null)
            {
                // cameraControl.InvertYAxis = invert;
            }
        }

        // Helpers
        private void UpdateVolumeText(Slider slider, TMP_Text text)
        {
            if (text != null && slider != null)
                text.text = $"{Mathf.RoundToInt(slider.value * 100)}%";
        }

        private void UpdateSliderValueText(Slider slider, TMP_Text text)
        {
            if (text != null && slider != null)
                text.text = slider.value.ToString("F1");
        }

        private void ApplyTheme()
        {
            if (uiTheme == null) return;

            // Apply to dropdowns
            ApplyToDropdown(qualityDropdown);
            ApplyToDropdown(resolutionDropdown);

            // Apply to toggles
            ApplyToToggle(fullscreenToggle);
            ApplyToToggle(vsyncToggle);
            ApplyToToggle(invertYAxisToggle);

            // Apply to sliders (visual only)
            ApplyToSlider(masterVolumeSlider);
            ApplyToSlider(musicVolumeSlider);
            ApplyToSlider(sfxVolumeSlider);
            ApplyToSlider(mouseSensitivitySlider);

            // Apply panel background
            var panelImage = GetComponent<Image>();
            var rectTransform = GetComponent<RectTransform>();
            if (panelImage != null && rectTransform != null)
                uiTheme.ApplyToPanel(panelImage, rectTransform);
        }

        private void ApplyToDropdown(TMP_Dropdown dropdown)
        {
            if (dropdown == null || uiTheme == null) return;
            var text = dropdown.GetComponentInChildren<TMP_Text>();
            if (text != null)
                uiTheme.ApplyToText(text, UITheme.TextStyle.Body);
            
            var template = dropdown.template;
            if (template != null)
            {
                var itemText = template.GetComponentInChildren<TMP_Text>();
                if (itemText != null)
                    uiTheme.ApplyToText(itemText, UITheme.TextStyle.Body);
            }
        }

        private void ApplyToToggle(Toggle toggle)
        {
            if (toggle == null || uiTheme == null) return;
            var text = toggle.GetComponentInChildren<TMP_Text>();
            if (text != null)
                uiTheme.ApplyToText(text, UITheme.TextStyle.Body);
        }

        private void ApplyToSlider(Slider slider)
        {
            if (slider == null || uiTheme == null) return;
            var handleText = slider.GetComponentInChildren<TMP_Text>();
            if (handleText != null)
                uiTheme.ApplyToText(handleText, UITheme.TextStyle.Caption);
        }
    }
}