using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Global settings store. Holds every user-facing value, applies it to the
/// relevant systems, and persists to PlayerPrefs. Survives scene changes.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string Prefix = "Settings.";

    // ─── Graphics ───
    public int QualityIndex { get; set; }
    public float RenderScale { get; set; } = 1f;
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
    public bool Fullscreen { get; set; } = true;
    public bool VSync { get; set; }
    public int FpsIndex { get; set; }
    public bool ShowFps { get; set; }
    public bool EnableShadows { get; set; } = true;

    // ─── Camera ───
    public float FieldOfView { get; set; } = 40f;
    public bool ZoomEnabled { get; set; } = true;
    public float ZoomSpeed { get; set; } = 8f;
    public float DefaultDistance { get; set; } = 22f;
    public float LookAhead { get; set; } = 2f;
    public bool CollisionShake { get; set; } = true;

    // ─── HUD ───
    public bool ShowMiniMap { get; set; } = true;
    public bool ShowSpeedHud { get; set; } = true;
    public bool ShowPrompt { get; set; } = true;
    public bool ShowGpsPath { get; set; } = true;
    public float UiScale { get; set; } = 1f;

    // ─── Audio ───
    public float MasterVolume { get; set; } = 1f;
    public float MusicVolume { get; set; } = 1f;
    public float SfxVolume { get; set; } = 1f;
    public float EngineVolume { get; set; } = 1f;

    // ─── Environment ───
    public bool PauseDayNight { get; set; }
    public float DayDuration { get; set; } = 300f;
    public float TimeOfDay { get; set; } = 0.35f;
    public bool StreetLamps { get; set; } = true;

    public bool IsDirty { get; private set; }

    // ─── Defaults captured from live systems ───
    private float _defaultFov, _defaultDistance, _defaultZoomSpeed, _defaultLookAhead, _defaultDayDuration;
    private bool _defaultZoom, _defaultShake, _defaultPauseDayNight, _defaultStreetLamps;
    private int _defaultQuality;
    private float _defaultRenderScale;

    private FpsDisplay _fps;
    private Coroutine _applyRoutine;

    private static readonly int[] FpsOptions = { 0, 30, 60, 120, 144 };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!PlayerPrefs.HasKey(Prefix + "Quality")) CaptureDefaults();
        Load();
        ApplyAll();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(DelayedApply());

    private IEnumerator DelayedApply()
    {
        yield return null;
        ApplyAll();
    }

    private void CaptureDefaults()
    {
        _defaultQuality = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        _defaultRenderScale = 1f;
        try { var a = UniversalRenderPipeline.asset; if (a != null) _defaultRenderScale = a.renderScale; } catch { }

        _defaultFov = 60f; _defaultDistance = 12f; _defaultZoomSpeed = 8f; _defaultLookAhead = 2f;
        _defaultZoom = true; _defaultShake = true; _defaultDayDuration = 300f;
        _defaultPauseDayNight = false; _defaultStreetLamps = true;

        var spaceCam = FindFirstObjectByType<SpaceCyberpunk.SpaceshipCameraFollow>();
        if (spaceCam != null)
        {
            _defaultDistance = spaceCam.distance;
            _defaultFov = spaceCam.normalFOV;
        }
        else if (Camera.main != null)
        {
            _defaultFov = Camera.main.fieldOfView;
        }

        var res = Screen.currentResolution;
        if (res.width > 0 && res.height > 0) { ResolutionWidth = res.width; ResolutionHeight = res.height; }
    }

    public void NotifyChanged()
    {
        IsDirty = true;
        if (_applyRoutine != null) StopCoroutine(_applyRoutine);
        _applyRoutine = StartCoroutine(DebouncedApply());
    }

    private IEnumerator DebouncedApply()
    {
        yield return new WaitForSeconds(0.15f);
        ApplyAll();
        _applyRoutine = null;
    }

    public void Save()
    {
        PlayerPrefs.SetInt(Prefix + "Quality", QualityIndex);
        PlayerPrefs.SetFloat(Prefix + "RenderScale", RenderScale);
        PlayerPrefs.SetInt(Prefix + "ResW", ResolutionWidth);
        PlayerPrefs.SetInt(Prefix + "ResH", ResolutionHeight);
        PlayerPrefs.SetInt(Prefix + "Fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "VSync", VSync ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "FpsIndex", FpsIndex);
        PlayerPrefs.SetInt(Prefix + "ShowFps", ShowFps ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "Shadows", EnableShadows ? 1 : 0);

        PlayerPrefs.SetFloat(Prefix + "Fov", FieldOfView);
        PlayerPrefs.SetInt(Prefix + "ZoomEnabled", ZoomEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(Prefix + "ZoomSpeed", ZoomSpeed);
        PlayerPrefs.SetFloat(Prefix + "Distance", DefaultDistance);
        PlayerPrefs.SetFloat(Prefix + "LookAhead", LookAhead);
        PlayerPrefs.SetInt(Prefix + "CollisionShake", CollisionShake ? 1 : 0);

        PlayerPrefs.SetInt(Prefix + "MiniMap", ShowMiniMap ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "SpeedHud", ShowSpeedHud ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "Prompt", ShowPrompt ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "GpsPath", ShowGpsPath ? 1 : 0);
        PlayerPrefs.SetFloat(Prefix + "UiScale", UiScale);

        PlayerPrefs.SetFloat(Prefix + "Volume", MasterVolume);
        PlayerPrefs.SetFloat(Prefix + "Music", MusicVolume);
        PlayerPrefs.SetFloat(Prefix + "Sfx", SfxVolume);
        PlayerPrefs.SetFloat(Prefix + "Engine", EngineVolume);

        PlayerPrefs.SetInt(Prefix + "PauseDayNight", PauseDayNight ? 1 : 0);
        PlayerPrefs.SetFloat(Prefix + "DayDuration", DayDuration);
        PlayerPrefs.SetFloat(Prefix + "TimeOfDay", TimeOfDay);
        PlayerPrefs.SetInt(Prefix + "StreetLamps", StreetLamps ? 1 : 0);

        PlayerPrefs.Save();
        IsDirty = false;
    }

    public void Load()
    {
        QualityIndex = PlayerPrefs.GetInt(Prefix + "Quality", _defaultQuality);
        RenderScale = PlayerPrefs.GetFloat(Prefix + "RenderScale", _defaultRenderScale);
        ResolutionWidth = PlayerPrefs.GetInt(Prefix + "ResW", ResolutionWidth);
        ResolutionHeight = PlayerPrefs.GetInt(Prefix + "ResH", ResolutionHeight);
        Fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) == 1;
        VSync = PlayerPrefs.GetInt(Prefix + "VSync", 0) == 1;
        FpsIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "FpsIndex", 0), 0, FpsOptions.Length - 1);
        ShowFps = PlayerPrefs.GetInt(Prefix + "ShowFps", 0) == 1;
        EnableShadows = PlayerPrefs.GetInt(Prefix + "Shadows", 1) == 1;

        FieldOfView = PlayerPrefs.GetFloat(Prefix + "Fov", _defaultFov);
        ZoomEnabled = PlayerPrefs.GetInt(Prefix + "ZoomEnabled", _defaultZoom ? 1 : 0) == 1;
        ZoomSpeed = PlayerPrefs.GetFloat(Prefix + "ZoomSpeed", _defaultZoomSpeed);
        DefaultDistance = PlayerPrefs.GetFloat(Prefix + "Distance", _defaultDistance);
        LookAhead = PlayerPrefs.GetFloat(Prefix + "LookAhead", _defaultLookAhead);
        CollisionShake = PlayerPrefs.GetInt(Prefix + "CollisionShake", _defaultShake ? 1 : 0) == 1;

        ShowMiniMap = PlayerPrefs.GetInt(Prefix + "MiniMap", 1) == 1;
        ShowSpeedHud = PlayerPrefs.GetInt(Prefix + "SpeedHud", 1) == 1;
        ShowPrompt = PlayerPrefs.GetInt(Prefix + "Prompt", 1) == 1;
        ShowGpsPath = PlayerPrefs.GetInt(Prefix + "GpsPath", 1) == 1;
        UiScale = PlayerPrefs.GetFloat(Prefix + "UiScale", 1f);

        MasterVolume = PlayerPrefs.GetFloat(Prefix + "Volume", 1f);
        MusicVolume = PlayerPrefs.GetFloat(Prefix + "Music", 1f);
        SfxVolume = PlayerPrefs.GetFloat(Prefix + "Sfx", 1f);
        EngineVolume = PlayerPrefs.GetFloat(Prefix + "Engine", 1f);

        PauseDayNight = PlayerPrefs.GetInt(Prefix + "PauseDayNight", _defaultPauseDayNight ? 1 : 0) == 1;
        DayDuration = PlayerPrefs.GetFloat(Prefix + "DayDuration", _defaultDayDuration);
        TimeOfDay = PlayerPrefs.GetFloat(Prefix + "TimeOfDay", 0.35f);
        StreetLamps = PlayerPrefs.GetInt(Prefix + "StreetLamps", _defaultStreetLamps ? 1 : 0) == 1;
    }

    public void ResetToDefaults()
    {
        QualityIndex = _defaultQuality;
        RenderScale = _defaultRenderScale;
        var res = Screen.currentResolution;
        if (res.width > 0 && res.height > 0) { ResolutionWidth = res.width; ResolutionHeight = res.height; }
        Fullscreen = true; VSync = false; FpsIndex = 0; ShowFps = false; EnableShadows = true;

        FieldOfView = _defaultFov; ZoomEnabled = _defaultZoom; ZoomSpeed = _defaultZoomSpeed;
        DefaultDistance = _defaultDistance; LookAhead = _defaultLookAhead; CollisionShake = _defaultShake;

        ShowMiniMap = true; ShowSpeedHud = true; ShowPrompt = true; ShowGpsPath = true; UiScale = 1f;

        MasterVolume = 1f; MusicVolume = 1f; SfxVolume = 1f; EngineVolume = 1f;

        PauseDayNight = _defaultPauseDayNight; DayDuration = _defaultDayDuration;
        TimeOfDay = 0.35f; StreetLamps = _defaultStreetLamps;

        ApplyAll();
    }

    public void ApplyAll()
    {
        ApplyGraphics(); ApplyCamera(); ApplyHud(); ApplyAudio(); ApplyEnvironment(); ApplyUiScale();
    }

    private void ApplyGraphics()
    {
        if (QualityIndex >= 0 && QualityIndex < QualitySettings.names.Length)
            QualitySettings.SetQualityLevel(QualityIndex, true);
        try { var a = UniversalRenderPipeline.asset; if (a != null) a.renderScale = Mathf.Clamp(RenderScale, 0.5f, 2f); } catch { }

        Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        ApplyResolution();
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = FpsIndex >= 0 && FpsIndex < FpsOptions.Length ? FpsOptions[FpsIndex] : 0;

        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional)
                l.shadows = EnableShadows ? LightShadows.Soft : LightShadows.None;
        }

        ApplyFps();
    }

    private void ApplyResolution()
    {
        var resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0) return;
        Resolution best = resolutions[0];
        float bestScore = float.MaxValue;
        for (int i = 0; i < resolutions.Length; i++)
        {
            var r = resolutions[i];
            float score = Mathf.Abs(r.width - ResolutionWidth) + Mathf.Abs(r.height - ResolutionHeight);
            if (score < bestScore) { bestScore = score; best = r; }
        }
        Screen.SetResolution(best.width, best.height, Fullscreen);
    }

    private void ApplyFps()
    {
        if (ShowFps && _fps == null)
        {
            var go = new GameObject("FpsDisplay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 9999;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.matchWidthOrHeight = 0.5f;
            _fps = go.AddComponent<FpsDisplay>();
        }
        else if (!ShowFps && _fps != null) { Destroy(_fps.gameObject); _fps = null; }
    }

    private void ApplyCamera()
    {
        var spaceCam = FindFirstObjectByType<SpaceCyberpunk.SpaceshipCameraFollow>();
        if (spaceCam != null && !SpaceCyberpunk.AboutInfoStation.AnyStationActive)
        {
            spaceCam.distance = Mathf.Clamp(DefaultDistance, 4f, 40f);
            spaceCam.normalFOV = Mathf.Clamp(FieldOfView, 30f, 100f);
        }
    }

    private void ApplyHud()
    {
        // Space HUD settings
    }

    private void ApplyUiScale()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.scaleFactor = Mathf.Clamp(UiScale, 0.5f, 2f);
    }

    private void ApplyAudio()
    {
        AudioListener.volume = Mathf.Clamp01(MasterVolume);
    }

    private void ApplyEnvironment()
    {
        // Space scene uses cosmic lighting directly
    }
}
