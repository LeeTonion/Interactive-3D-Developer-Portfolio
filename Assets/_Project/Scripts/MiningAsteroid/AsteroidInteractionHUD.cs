using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Centralized, lightweight HUD, prompt arbitrator, player cache, and subtle procedural audio
    /// system for all Gemstone Mining Asteroid interactive features.
    /// Prevents overlapping prompts, ensures [F] triggers only the closest valid target,
    /// and avoids creating duplicate Canvases or AudioSources.
    /// </summary>
    public class AsteroidInteractionHUD : MonoBehaviour
    {
        private static AsteroidInteractionHUD _instance;
        private static Transform _cachedPlayerTransform;

        /// <summary>
        /// True while an asteroid [F] interaction prompt is active or the Railway Control panel is open.
        /// </summary>
        public static bool AnyPromptActive { get; private set; }

        public static AsteroidInteractionHUD Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AsteroidInteractionHUD>();
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("AsteroidInteractionHUD_Auto");
                        _instance = go.AddComponent<AsteroidInteractionHUD>();
                    }
                }
                return _instance;
            }
        }

        public static Transform GetPlayerTransform()
        {
            if (_cachedPlayerTransform != null) return _cachedPlayerTransform;

            var psc = FindFirstObjectByType<PlayerSpaceshipController>();
            if (psc != null)
            {
                _cachedPlayerTransform = psc.transform;
                return _cachedPlayerTransform;
            }

            var go = GameObject.Find("PlayerSpaceship");
            if (go != null)
            {
                _cachedPlayerTransform = go.transform;
            }
            return _cachedPlayerTransform;
        }

        private struct PromptCandidate
        {
            public MonoBehaviour owner;
            public float distance;
            public string promptText;
            public Color accentColor;
        }

        private readonly List<PromptCandidate> _frameCandidates = new List<PromptCandidate>(16);
        private MonoBehaviour _activeWinner;
        private string _activePromptText;
        private Color _activeAccentColor;
        private bool _consumedThisFrame;
        private bool _railwayPanelOpen;
        private bool _isInitialized;

        // UI Elements
        private Canvas _hudCanvas;
        private GameObject _promptRoot;
        private CanvasGroup _promptCanvasGroup;
        private Image _promptBorder;
        private TextMeshProUGUI _promptLabel;

        private GameObject _toastRoot;
        private CanvasGroup _toastCanvasGroup;
        private Image _toastBorder;
        private TextMeshProUGUI _toastLabel;
        private float _toastTimer;
        private float _toastDuration;

        // Subtle procedural audio
        private AudioSource _audioSource;
        private AudioClip _chimeClip;
        private AudioClip _coreClip;
        private AudioClip _beepClip;
        private AudioClip _discoveryClip;

        public RectTransform HudRootTransform { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            InitializeUI();
            InitializeProceduralAudio();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                AnyPromptActive = false;
                _cachedPlayerTransform = null;
            }
        }

        public static void SetRailwayPanelOpen(bool open)
        {
            if (_instance != null)
            {
                _instance._railwayPanelOpen = open;
            }
            if (open) AnyPromptActive = true;
        }

        /// <summary>
        /// Called in Update() by interactable components while the player is inside their prompt radius.
        /// </summary>
        public static void ReportCandidate(MonoBehaviour owner, float distance, string promptText, Color accentColor)
        {
            if (owner == null || !Application.isPlaying) return;
            var hud = Instance;
            if (hud == null) return;

            hud._frameCandidates.Add(new PromptCandidate
            {
                owner = owner,
                distance = distance,
                promptText = promptText,
                accentColor = accentColor
            });
        }

        /// <summary>
        /// Checks whether this component is currently the winning closest prompt target and consumes the key press.
        /// </summary>
        public static bool ConsumeInteractPress(MonoBehaviour owner, KeyCode key = KeyCode.F)
        {
            if (owner == null || _instance == null || _instance._consumedThisFrame) return false;
            if (AboutInfoStation.AnyStationActive || AboutInfoStation.AnyLandingSequenceActive || SpawnZoneAnimator.IsAnyIntroWalkActive)
                return false;

            if (_instance._activeWinner == owner && Input.GetKeyDown(key))
            {
                _instance._consumedThisFrame = true;
                return true;
            }
            return false;
        }

        public static bool IsCurrentTarget(MonoBehaviour owner)
        {
            return _instance != null && _instance._activeWinner == owner;
        }

        public static void ShowNotification(string message, Color accentColor, float duration = 3.2f)
        {
            if (!Application.isPlaying) return;
            var hud = Instance;
            if (hud == null) return;
            hud.DisplayToast(message, accentColor, duration);
        }

        public static void PlayCrystalChime(float pitch = 1.0f)
        {
            if (_instance == null || _instance._audioSource == null || _instance._chimeClip == null) return;
            _instance._audioSource.pitch = Mathf.Clamp(pitch, 0.7f, 1.5f);
            _instance._audioSource.PlayOneShot(_instance._chimeClip, 0.22f);
        }

        public static void PlayCoreActivationSound()
        {
            if (_instance == null || _instance._audioSource == null || _instance._coreClip == null) return;
            _instance._audioSource.pitch = 1.0f;
            _instance._audioSource.PlayOneShot(_instance._coreClip, 0.26f);
        }

        public static void PlayTerminalClick()
        {
            if (_instance == null || _instance._audioSource == null || _instance._beepClip == null) return;
            _instance._audioSource.pitch = 1.0f;
            _instance._audioSource.PlayOneShot(_instance._beepClip, 0.20f);
        }

        public static void PlayChamberDiscoverySound()
        {
            if (_instance == null || _instance._audioSource == null || _instance._discoveryClip == null) return;
            _instance._audioSource.pitch = 1.0f;
            _instance._audioSource.PlayOneShot(_instance._discoveryClip, 0.28f);
        }

        private void LateUpdate()
        {
            if (!_isInitialized) InitializeUI();

            _consumedThisFrame = false;

            bool suppressHud = AboutInfoStation.AnyStationActive
                               || AboutInfoStation.AnyLandingSequenceActive
                               || SpawnZoneAnimator.IsAnyIntroWalkActive;

            PromptCandidate? best = null;
            if (!suppressHud)
            {
                for (int i = 0; i < _frameCandidates.Count; i++)
                {
                    var c = _frameCandidates[i];
                    if (c.owner == null) continue;
                    if (!best.HasValue || c.distance < best.Value.distance)
                    {
                        best = c;
                    }
                }
            }
            _frameCandidates.Clear();

            if (best.HasValue)
            {
                _activeWinner = best.Value.owner;
                _activePromptText = best.Value.promptText;
                _activeAccentColor = best.Value.accentColor;
            }
            else
            {
                _activeWinner = null;
            }

            AnyPromptActive = !suppressHud && (_activeWinner != null || _railwayPanelOpen);

            // Update prompt pill UI
            if (_promptRoot != null)
            {
                bool showPrompt = !suppressHud && _activeWinner != null;
                if (_promptRoot.activeSelf != showPrompt) _promptRoot.SetActive(showPrompt);
                if (showPrompt)
                {
                    _promptLabel.text = _activePromptText;
                    if (_promptBorder != null) _promptBorder.color = new Color(_activeAccentColor.r, _activeAccentColor.g, _activeAccentColor.b, 0.78f);
                    if (_promptCanvasGroup != null) _promptCanvasGroup.alpha = Mathf.MoveTowards(_promptCanvasGroup.alpha, 1f, Time.deltaTime * 10f);
                }
                else if (_promptCanvasGroup != null)
                {
                    _promptCanvasGroup.alpha = 0f;
                }
            }

            // Update toast UI
            if (_toastRoot != null && _toastRoot.activeSelf)
            {
                _toastTimer += Time.deltaTime;
                if (_toastTimer >= _toastDuration)
                {
                    _toastRoot.SetActive(false);
                }
                else if (_toastCanvasGroup != null)
                {
                    float fadeIn = Mathf.Clamp01(_toastTimer / 0.22f);
                    float fadeOut = Mathf.Clamp01((_toastDuration - _toastTimer) / 0.45f);
                    _toastCanvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);
                }
            }
        }

        private void DisplayToast(string message, Color accentColor, float duration)
        {
            if (!_isInitialized) InitializeUI();
            if (_toastRoot == null) return;

            _toastLabel.text = message;
            _toastBorder.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.88f);
            _toastDuration = Mathf.Max(1.2f, duration);
            _toastTimer = 0f;
            _toastRoot.SetActive(true);
            _toastCanvasGroup.alpha = 0.05f;
        }

        private void InitializeUI()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            var canvasGo = new GameObject("AsteroidHUDCanvas");
            canvasGo.transform.SetParent(transform, false);
            _hudCanvas = canvasGo.AddComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 25;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            HudRootTransform = canvasGo.GetComponent<RectTransform>();

            // 1. Compact Bottom-Center Interaction Prompt
            _promptRoot = CreatePillPanel(
                HudRootTransform,
                "InteractionPrompt",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 112f),
                new Vector2(430f, 44f),
                out _promptBorder,
                out _promptLabel,
                out _promptCanvasGroup,
                16f
            );
            _promptRoot.SetActive(false);

            // 2. Compact Top-Center Notification Toast
            _toastRoot = CreatePillPanel(
                HudRootTransform,
                "NotificationToast",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -66f),
                new Vector2(490f, 46f),
                out _toastBorder,
                out _toastLabel,
                out _toastCanvasGroup,
                16.5f
            );
            _toastLabel.fontStyle = FontStyles.Bold;
            _toastRoot.SetActive(false);
        }

        private GameObject CreatePillPanel(
            RectTransform parent,
            string objName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            out Image borderImage,
            out TextMeshProUGUI label,
            out CanvasGroup group,
            float fontSize)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // Outer border
            borderImage = go.AddComponent<Image>();
            borderImage.color = new Color(0.15f, 0.85f, 1f, 0.82f);

            // Dark fill inset
            var inner = new GameObject("Fill");
            inner.transform.SetParent(rt, false);
            var innerRt = inner.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2f, 2f);
            innerRt.offsetMax = new Vector2(-2f, -2f);
            var fillImg = inner.AddComponent<Image>();
            fillImg.color = new Color(0.03f, 0.06f, 0.12f, 0.92f);

            // Text
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(innerRt, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(14f, 4f);
            txtRt.offsetMax = new Vector2(-14f, -4f);

            label = txtGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.90f, 0.97f, 1f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            return go;
        }

        private void InitializeProceduralAudio()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _chimeClip = CreateHarmonicTone("CrystalResonanceChime", 0.55f, 660f, 990f, 0.35f);
            _coreClip = CreateHarmonicTone("CorePulseActivation", 0.85f, 220f, 440f, 0.45f);
            _beepClip = CreateHarmonicTone("TerminalBeep", 0.14f, 880f, 1180f, 0.30f);
            _discoveryClip = CreateHarmonicTone("AncientChamberDiscovery", 1.15f, 330f, 660f, 0.45f);
        }

        private static AudioClip CreateHarmonicTone(string clipName, float durationSec, float freqA, float freqB, float amp)
        {
            const int sampleRate = 22050;
            int samples = Mathf.Max(256, Mathf.RoundToInt(durationSec * sampleRate));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float u = (float)i / samples;
                float env = Mathf.Sin(Mathf.Min(1f, u * 12f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - u, 1.8f);
                float freq = Mathf.Lerp(freqA, freqB, u * 0.6f);
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.35f;
                data[i] = s * env * amp;
            }

            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

