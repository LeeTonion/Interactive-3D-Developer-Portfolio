using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Core;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Recreates the iconic Bruno Simon 3D Portfolio UI Modal:
    /// - Split view: Left Preview Image + Right Dark Content Panel.
    /// - Top Tab Bar: Home, Options, Controls, Achievements, Projects, and Close button.
    /// - Sub-panel switching with synchronized preview textures.
    /// - Fully interactive Options (Audio mute, Quality, Respawn, Reset).
    /// </summary>
    public class BrunoPortfolioModalUI : MonoBehaviour
    {
        public static BrunoPortfolioModalUI Instance { get; private set; }

        [Header("Preview View (Left Side)")]
        [SerializeField] private Image previewImage;
        [SerializeField] private Sprite homePreviewSprite;
        [SerializeField] private Sprite optionsPreviewSprite;
        [SerializeField] private Sprite controlsPreviewSprite;
        [SerializeField] private Sprite achievementsPreviewSprite;
        [SerializeField] private Sprite projectsPreviewSprite;

        [Header("Top Tab Bar (Icons)")]
        [SerializeField] private Button homeTabButton;
        [SerializeField] private Button optionsTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private Button achievementsTabButton;
        [SerializeField] private Button projectsTabButton;
        [SerializeField] private Button closeButton;

        [Header("Tab Pages (Right Side)")]
        [SerializeField] private GameObject homePage;
        [SerializeField] private GameObject optionsPage;
        [SerializeField] private GameObject controlsPage;
        [SerializeField] private GameObject achievementsPage;
        [SerializeField] private GameObject projectsPage;

        [Header("Options Controls")]
        [SerializeField] private Button audioToggleButton;
        [SerializeField] private TMP_Text audioToggleText;
        [SerializeField] private Button qualityToggleButton;
        [SerializeField] private TMP_Text qualityToggleText;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text rendererText;

        [Header("Home Page Text")]
        [SerializeField] private TMP_Text homeTitleText;
        [SerializeField] private TMP_Text homeBodyText;

        [Header("Projects Page Text")]
        [SerializeField] private TMP_Text projectsTitleText;
        [SerializeField] private TMP_Text projectsBodyText;

        [Header("Achievements Data")]
        [SerializeField] private TMP_Text achievementCounterText;

        private int _currentTabIndex = 0;
        private bool _isAudioMuted = false;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private GameObject _playerCar;

        private readonly Color TabActiveColor = new Color(0.25f, 0.22f, 0.32f, 1f);
        private readonly Color TabInactiveColor = new Color(0.12f, 0.10f, 0.15f, 0.95f);

        private void Awake()
        {
            Instance = this;

            // Hook tab buttons
            if (homeTabButton != null) homeTabButton.onClick.AddListener(OnHomeClicked);
            if (optionsTabButton != null) optionsTabButton.onClick.AddListener(OnOptionsClicked);
            if (controlsTabButton != null) controlsTabButton.onClick.AddListener(OnControlsClicked);
            if (achievementsTabButton != null) achievementsTabButton.onClick.AddListener(OnAchievementsClicked);
            if (projectsTabButton != null) projectsTabButton.onClick.AddListener(OnProjectsClicked);
            if (closeButton != null) closeButton.onClick.AddListener(CloseModal);

            // Options listeners
            if (audioToggleButton != null) audioToggleButton.onClick.AddListener(ToggleAudio);
            if (qualityToggleButton != null) qualityToggleButton.onClick.AddListener(CycleQuality);
            if (respawnButton != null) respawnButton.onClick.AddListener(RespawnPlayer);
            if (resetButton != null) resetButton.onClick.AddListener(ResetPlayerToSpawn);

            CachePlayerSpawn();
            UpdateOptionsUI();
        }

        private void Start()
        {
            PopulateStaticContent();
            SwitchTab(0);
        }

        private void PopulateStaticContent()
        {
            // Home page
            if (homeTitleText != null && string.IsNullOrEmpty(homeTitleText.text) || 
                (homeTitleText != null && homeTitleText.text.Contains("BRUNO")))
            {
                homeTitleText.text = "WELCOME HOME";
            }
            if (homeBodyText != null && (string.IsNullOrEmpty(homeBodyText.text) ||
                homeBodyText.text.Contains("Bruno Simon")))
            {
                homeBodyText.text = 
                    "Welcome to my Interactive 3D Portfolio!\n\n" +
                    "Drive around to explore the world and discover different areas \u2014 " +
                    "each showcasing a chapter of my journey as a developer.\n\n" +
                    "<b>Interact with booths</b> using [E] to open panels. " +
                    "Press [ESC] to pause at any time.\n\n" +
                    "Enjoy the ride!";
            }

            // Projects page
            if (projectsTitleText != null && (string.IsNullOrEmpty(projectsTitleText.text) ||
                projectsTitleText.text.Contains("BRUNO")))
            {
                projectsTitleText.text = "FEATURED PROJECTS";
            }
        }

        private void OnHomeClicked() { SwitchTab(0); }
        private void OnOptionsClicked() { SwitchTab(1); }
        private void OnControlsClicked() { SwitchTab(2); }
        private void OnAchievementsClicked() { SwitchTab(3); }
        private void OnProjectsClicked() { SwitchTab(4); }

        private void CachePlayerSpawn()
        {
            _playerCar = GameObject.FindWithTag("Player");
            if (_playerCar == null)
            {
                var carControl = FindObjectOfType<CarControl>();
                if (carControl != null) _playerCar = carControl.gameObject;
            }

            if (_playerCar != null)
            {
                _spawnPosition = _playerCar.transform.position;
                _spawnRotation = _playerCar.transform.rotation;
            }
        }

        public void SwitchTab(int tabIndex)
        {
            _currentTabIndex = tabIndex;

            // Toggle pages
            if (homePage != null) homePage.SetActive(tabIndex == 0);
            if (optionsPage != null) optionsPage.SetActive(tabIndex == 1);
            if (controlsPage != null) controlsPage.SetActive(tabIndex == 2);
            if (achievementsPage != null) achievementsPage.SetActive(tabIndex == 3);
            if (projectsPage != null) projectsPage.SetActive(tabIndex == 4);

            // Update Left Preview Image
            if (previewImage != null)
            {
                Sprite targetSprite = homePreviewSprite;
                switch (tabIndex)
                {
                    case 0: targetSprite = homePreviewSprite; break;
                    case 1: targetSprite = optionsPreviewSprite; break;
                    case 2: targetSprite = controlsPreviewSprite; break;
                    case 3: targetSprite = achievementsPreviewSprite; break;
                    case 4: targetSprite = projectsPreviewSprite; break;
                }
                if (targetSprite != null) previewImage.sprite = targetSprite;
            }

            // Update Tab Button Highlights
            HighlightTab(homeTabButton, tabIndex == 0);
            HighlightTab(optionsTabButton, tabIndex == 1);
            HighlightTab(controlsTabButton, tabIndex == 2);
            HighlightTab(achievementsTabButton, tabIndex == 3);
            HighlightTab(projectsTabButton, tabIndex == 4);
        }

        private void HighlightTab(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? TabActiveColor : TabInactiveColor;
            }
        }

        public void CloseModal()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPaused(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // ─── Options Actions ───
        private void ToggleAudio()
        {
            _isAudioMuted = !_isAudioMuted;
            AudioListener.pause = _isAudioMuted;
            if (audioToggleText != null)
            {
                audioToggleText.text = _isAudioMuted ? "Muted" : "On";
            }
        }

        private void CycleQuality()
        {
            int current = QualitySettings.GetQualityLevel();
            int total = QualitySettings.names.Length;
            int next = (current + 1) % total;
            QualitySettings.SetQualityLevel(next, true);

            if (qualityToggleText != null)
            {
                qualityToggleText.text = QualitySettings.names[next];
            }
        }

        private void RespawnPlayer()
        {
            if (_playerCar == null) CachePlayerSpawn();
            if (_playerCar != null)
            {
                var rb = _playerCar.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                // Flip upright if upside down, lift up 1.5m
                _playerCar.transform.position += Vector3.up * 1.5f;
                _playerCar.transform.rotation = Quaternion.Euler(0f, _playerCar.transform.eulerAngles.y, 0f);
            }
            CloseModal();
        }

        private void ResetPlayerToSpawn()
        {
            if (_playerCar == null) CachePlayerSpawn();
            if (_playerCar != null)
            {
                var rb = _playerCar.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                _playerCar.transform.position = _spawnPosition;
                _playerCar.transform.rotation = _spawnRotation;
            }
            CloseModal();
        }

        private void UpdateOptionsUI()
        {
            if (audioToggleText != null)
            {
                audioToggleText.text = AudioListener.pause ? "Muted" : "On";
            }
            if (qualityToggleText != null)
            {
                qualityToggleText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
            }
            if (rendererText != null)
            {
                rendererText.text = SystemInfo.graphicsDeviceType.ToString();
            }
        }
    }
}
