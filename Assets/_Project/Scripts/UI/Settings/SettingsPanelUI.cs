using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Binds manually-built Settings UI (5 tabs) to SettingsManager.
/// Tự động tìm panel theo thứ tự con (không phụ thuộc tên), gán listener cho
/// Slider / Toggle / Dropdown. Tab nút chuyển panel; Footer: Reset + Apply.
/// </summary>
[DisallowMultipleComponent]
public class SettingsPanelUI : MonoBehaviour
{
    private SettingsManager _mgr;
    private RectTransform[] _panels = new RectTransform[5];
    private Button[] _tabButtons;
    private int _activeTab;
    private ScrollRect _scrollRect;

    private void Awake()
    {
        _mgr = SettingsManager.Instance;
        if (_mgr == null)
        {
            var go = new GameObject("SettingsManager");
            _mgr = go.AddComponent<SettingsManager>();
        }

        var sv = transform.Find("Content/Scroll View");
        if (sv != null) _scrollRect = sv.GetComponent<ScrollRect>();

        var tabBar = transform.Find("TabBar");
        if (tabBar == null)
        {
            Debug.LogError("[SettingsPanelUI] Không tìm thấy TabBar");
            return;
        }
        _tabButtons = new Button[tabBar.childCount];
        for (int i = 0; i < _tabButtons.Length; i++)
            _tabButtons[i] = tabBar.GetChild(i).GetComponent<Button>();

        var footer = transform.Find("Footer");
        if (footer != null)
        {
            footer.GetChild(0).GetComponent<Button>().onClick.AddListener(OnReset);
            footer.GetChild(1).GetComponent<Button>().onClick.AddListener(OnApply);
        }
        var closeBtn = transform.Find("Header/Button");
        if (closeBtn != null) closeBtn.GetComponent<Button>().onClick.AddListener(OnClose);

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            int idx = i;
            _tabButtons[i].onClick.AddListener(() => SwitchTab(idx));
        }
    }

    private void OnEnable()
    {
        BindAll();
        SwitchTab(_activeTab);
    }

    /// <summary>
    /// Tìm panel theo thứ tự con trong Content/Scroll View/Viewport.
    /// Mục lục: 0=Hình ảnh, 1=Camera, 2=HUD, 3=Âm thanh, 4=Môi trường.
    /// </summary>
    private RectTransform FindPanelByIndex(int index)
    {
        var container = GetPanelContainer();
        if (container == null)
        {
            Debug.LogError("[SettingsPanelUI] Không tìm thấy Content/Scroll View/Viewport");
            return null;
        }
        if (index >= 0 && index < container.childCount)
            return container.GetChild(index) as RectTransform;
        Debug.LogErrorFormat("[SettingsPanelUI] Không tìm thấy panel tại index {0}", index);
        return null;
    }

    /// <summary>
    /// Các panel settings nằm trực tiếp dưới Viewport của Scroll View.
    /// Hỗ trợ cả layout cũ có thêm 1 lớp "Content" bên trong Viewport.
    /// </summary>
    private Transform GetPanelContainer()
    {
        var legacy = transform.Find("Content/Scroll View/Viewport/Content");
        if (legacy != null) return legacy;
        return transform.Find("Content/Scroll View/Viewport");
    }

    private T GetControl<T>(RectTransform panel, int row) where T : Component
    {
        if (panel == null || row < 0 || row >= panel.childCount) return null;
        var r = panel.GetChild(row);
        var ctrl = r.Find("Control");
        return ctrl != null ? ctrl.GetComponentInChildren<T>(true) : null;
    }

    private void SwitchTab(int index)
    {
        _activeTab = index;
        for (int i = 0; i < _panels.Length; i++)
            if (_panels[i] != null) _panels[i].gameObject.SetActive(i == index);

        // Cập nhật content của ScrollRect sang panel đang active để thanh cuộn/trượt hoạt động
        if (_scrollRect != null && index >= 0 && index < _panels.Length && _panels[index] != null)
        {
            _scrollRect.content = _panels[index];
            _scrollRect.verticalNormalizedPosition = 1f;
            _scrollRect.velocity = Vector2.zero;
        }

        if (_tabButtons != null)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                var c = _tabButtons[i].colors;
                c.normalColor = i == index ? new Color(1f, 0.65f, 0.2f) : Color.white;
                _tabButtons[i].colors = c;
            }
        }
    }

    private void OnReset() { _mgr?.ResetToDefaults(); BindAll(); }
    private void OnApply() { _mgr?.Save(); }
    private void OnClose() { gameObject.SetActive(false); }

    // ─────────────────────────────────────────
    //  Binding (gọi cho TẤT CẢ panel, không chỉ tab active)
    // ─────────────────────────────────────────
    private void BindAll()
    {
        var container = GetPanelContainer();
        int panelCount = container == null ? 0 : container.childCount;

        // Khởi tạo _panels theo số lượng panel thực tế trong hierarchy
        // (không hardcode 5 nữa → tránh lỗi "Panel index x không tìm thấy"
        //  khi thiết kế lại content settings)
        _panels = new RectTransform[panelCount];
        for (int i = 0; i < panelCount; i++)
            _panels[i] = FindPanelByIndex(i);

        // Chỉ bind các panel thực sự tồn tại (ánh xạ index theo thứ tự con)
        if (_panels.Length > 0) BindPanel(0, BindGraphics);
        if (_panels.Length > 1) BindPanel(1, BindCamera);
        if (_panels.Length > 2) BindPanel(2, BindHud);
        if (_panels.Length > 3) BindPanel(3, BindAudio);
        if (_panels.Length > 4) BindPanel(4, BindEnv);
    }

    private void BindPanel(int index, System.Action<RectTransform> bindAction)
    {
        var p = _panels[index];
        if (p == null)
        {
            Debug.LogErrorFormat("[SettingsPanelUI] Panel index {0} không tìm thấy", index);
            return;
        }
        bindAction(p);
        Debug.LogFormat("[SettingsPanelUI] Đã bind panel {0} ({1} hàng)", index, p.childCount);
    }

    private void BindGraphics(RectTransform p)
    {
        // 0 Chất lượng (Dropdown)
        var quality = GetControl<TMP_Dropdown>(p, 0);
        if (quality != null)
        {
            quality.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Mobile"),
                new TMP_Dropdown.OptionData("PC")
            };
            quality.value = _mgr.QualityIndex;
            quality.onValueChanged.RemoveAllListeners();
            quality.onValueChanged.AddListener(v => { _mgr.QualityIndex = v; _mgr.NotifyChanged(); });
        }

        // 1 Render Scale (Slider)
        var rs = GetControl<Slider>(p, 1);
        if (rs != null)
        {
            rs.minValue = 0.5f; rs.maxValue = 2f; rs.value = _mgr.RenderScale;
            rs.onValueChanged.RemoveAllListeners();
            rs.onValueChanged.AddListener(v => { _mgr.RenderScale = v; _mgr.NotifyChanged(); });
        }

        // 2 Độ phân giải (Dropdown)
        var res = GetControl<TMP_Dropdown>(p, 2);
        if (res != null)
        {
            res.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("1920 × 1080"),
                new TMP_Dropdown.OptionData("1280 × 720"),
                new TMP_Dropdown.OptionData("2560 × 1440"),
                new TMP_Dropdown.OptionData("3840 × 2160")
            };
            res.value = ResToIndex(_mgr.ResolutionWidth, _mgr.ResolutionHeight);
            res.onValueChanged.RemoveAllListeners();
            res.onValueChanged.AddListener(v =>
            {
                var r = ResFromIndex(v);
                _mgr.ResolutionWidth = r.x; _mgr.ResolutionHeight = r.y; _mgr.NotifyChanged();
            });
        }

        // 3 Toàn màn hình (Toggle)
        var full = GetControl<Toggle>(p, 3);
        if (full != null)
        {
            full.isOn = _mgr.Fullscreen;
            full.onValueChanged.RemoveAllListeners();
            full.onValueChanged.AddListener(v => { _mgr.Fullscreen = v; _mgr.NotifyChanged(); });
        }

        // 4 VSync (Toggle)
        var vsync = GetControl<Toggle>(p, 4);
        if (vsync != null)
        {
            vsync.isOn = _mgr.VSync;
            vsync.onValueChanged.RemoveAllListeners();
            vsync.onValueChanged.AddListener(v => { _mgr.VSync = v; _mgr.NotifyChanged(); });
        }

        // 5 Giới hạn FPS (Dropdown)
        var fps = GetControl<TMP_Dropdown>(p, 5);
        if (fps != null)
        {
            fps.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Unlimited"),
                new TMP_Dropdown.OptionData("30"),
                new TMP_Dropdown.OptionData("60"),
                new TMP_Dropdown.OptionData("120"),
                new TMP_Dropdown.OptionData("144")
            };
            fps.value = _mgr.FpsIndex;
            fps.onValueChanged.RemoveAllListeners();
            fps.onValueChanged.AddListener(v => { _mgr.FpsIndex = v; _mgr.NotifyChanged(); });
        }

        // 6 Hiển thị FPS (Toggle)
        var showFps = GetControl<Toggle>(p, 6);
        if (showFps != null)
        {
            showFps.isOn = _mgr.ShowFps;
            showFps.onValueChanged.RemoveAllListeners();
            showFps.onValueChanged.AddListener(v => { _mgr.ShowFps = v; _mgr.NotifyChanged(); });
        }

        // 7 Bóng đổ (Toggle)
        var shadows = GetControl<Toggle>(p, 7);
        if (shadows != null)
        {
            shadows.isOn = _mgr.EnableShadows;
            shadows.onValueChanged.RemoveAllListeners();
            shadows.onValueChanged.AddListener(v => { _mgr.EnableShadows = v; _mgr.NotifyChanged(); });
        }
    }

    private void BindCamera(RectTransform p)
    {
        // 0 Khoảng cách camera (Slider)
        var dist = GetControl<Slider>(p, 0);
        if (dist != null)
        {
            dist.minValue = 10f; dist.maxValue = 45f; dist.value = _mgr.DefaultDistance;
            dist.onValueChanged.RemoveAllListeners();
            dist.onValueChanged.AddListener(v => { _mgr.DefaultDistance = v; _mgr.NotifyChanged(); });
        }

        // 1 Field of View (Slider)
        var fov = GetControl<Slider>(p, 1);
        if (fov != null)
        {
            fov.minValue = 20f; fov.maxValue = 80f; fov.value = _mgr.FieldOfView;
            fov.onValueChanged.RemoveAllListeners();
            fov.onValueChanged.AddListener(v => { _mgr.FieldOfView = v; _mgr.NotifyChanged(); });
        }

        // 2 Tốc độ zoom (Slider)
        var zs = GetControl<Slider>(p, 2);
        if (zs != null)
        {
            zs.minValue = 2f; zs.maxValue = 20f; zs.value = _mgr.ZoomSpeed;
            zs.onValueChanged.RemoveAllListeners();
            zs.onValueChanged.AddListener(v => { _mgr.ZoomSpeed = v; _mgr.NotifyChanged(); });
        }

        // 3 Zoom bằng cuộn chuột (Toggle)
        var zoomEn = GetControl<Toggle>(p, 3);
        if (zoomEn != null)
        {
            zoomEn.isOn = _mgr.ZoomEnabled;
            zoomEn.onValueChanged.RemoveAllListeners();
            zoomEn.onValueChanged.AddListener(v => { _mgr.ZoomEnabled = v; _mgr.NotifyChanged(); });
        }

        // 4 Look-ahead (Slider)
        var la = GetControl<Slider>(p, 4);
        if (la != null)
        {
            la.minValue = 0f; la.maxValue = 5f; la.value = _mgr.LookAhead;
            la.onValueChanged.RemoveAllListeners();
            la.onValueChanged.AddListener(v => { _mgr.LookAhead = v; _mgr.NotifyChanged(); });
        }

        // 5 Rung camera khi va chạm (Toggle)
        var shake = GetControl<Toggle>(p, 5);
        if (shake != null)
        {
            shake.isOn = _mgr.CollisionShake;
            shake.onValueChanged.RemoveAllListeners();
            shake.onValueChanged.AddListener(v => { _mgr.CollisionShake = v; _mgr.NotifyChanged(); });
        }
    }

    private void BindHud(RectTransform p)
    {
        // 0 Mini Map (Toggle)
        var mm = GetControl<Toggle>(p, 0);
        if (mm != null)
        {
            mm.isOn = _mgr.ShowMiniMap;
            mm.onValueChanged.RemoveAllListeners();
            mm.onValueChanged.AddListener(v => { _mgr.ShowMiniMap = v; _mgr.NotifyChanged(); });
        }

        // 1 Speed HUD (Toggle)
        var sh = GetControl<Toggle>(p, 1);
        if (sh != null)
        {
            sh.isOn = _mgr.ShowSpeedHud;
            sh.onValueChanged.RemoveAllListeners();
            sh.onValueChanged.AddListener(v => { _mgr.ShowSpeedHud = v; _mgr.NotifyChanged(); });
        }

        // 2 Interaction prompt (Toggle)
        var pr = GetControl<Toggle>(p, 2);
        if (pr != null)
        {
            pr.isOn = _mgr.ShowPrompt;
            pr.onValueChanged.RemoveAllListeners();
            pr.onValueChanged.AddListener(v => { _mgr.ShowPrompt = v; _mgr.NotifyChanged(); });
        }

        // 3 Đường dẫn GPS (Toggle)
        var gps = GetControl<Toggle>(p, 3);
        if (gps != null)
        {
            gps.isOn = _mgr.ShowGpsPath;
            gps.onValueChanged.RemoveAllListeners();
            gps.onValueChanged.AddListener(v => { _mgr.ShowGpsPath = v; _mgr.NotifyChanged(); });
        }

        // 4 Kích thước UI (Slider)
        var uiScale = GetControl<Slider>(p, 4);
        if (uiScale != null)
        {
            uiScale.minValue = 0.75f; uiScale.maxValue = 1.25f; uiScale.value = _mgr.UiScale;
            uiScale.onValueChanged.RemoveAllListeners();
            uiScale.onValueChanged.AddListener(v => { _mgr.UiScale = v; _mgr.NotifyChanged(); });
        }
    }

    private void BindAudio(RectTransform p)
    {
        // 0 Âm lượng chính (Slider)
        var master = GetControl<Slider>(p, 0);
        if (master != null)
        {
            master.minValue = 0f; master.maxValue = 1f; master.value = _mgr.MasterVolume;
            master.onValueChanged.RemoveAllListeners();
            master.onValueChanged.AddListener(v => { _mgr.MasterVolume = v; _mgr.NotifyChanged(); });
        }

        // 1 Music (Slider)
        var music = GetControl<Slider>(p, 1);
        if (music != null)
        {
            music.minValue = 0f; music.maxValue = 1f; music.value = _mgr.MusicVolume;
            music.onValueChanged.RemoveAllListeners();
            music.onValueChanged.AddListener(v => { _mgr.MusicVolume = v; _mgr.NotifyChanged(); });
        }

        // 2 SFX (Slider)
        var sfx = GetControl<Slider>(p, 2);
        if (sfx != null)
        {
            sfx.minValue = 0f; sfx.maxValue = 1f; sfx.value = _mgr.SfxVolume;
            sfx.onValueChanged.RemoveAllListeners();
            sfx.onValueChanged.AddListener(v => { _mgr.SfxVolume = v; _mgr.NotifyChanged(); });
        }

        // 3 Engine (Slider)
        var engine = GetControl<Slider>(p, 3);
        if (engine != null)
        {
            engine.minValue = 0f; engine.maxValue = 1f; engine.value = _mgr.EngineVolume;
            engine.onValueChanged.RemoveAllListeners();
            engine.onValueChanged.AddListener(v => { _mgr.EngineVolume = v; _mgr.NotifyChanged(); });
        }
    }

    private void BindEnv(RectTransform p)
    {
        // 0 Tạm dừng ngày/đêm (Toggle)
        var pause = GetControl<Toggle>(p, 0);
        if (pause != null)
        {
            pause.isOn = _mgr.PauseDayNight;
            pause.onValueChanged.RemoveAllListeners();
            pause.onValueChanged.AddListener(v => { _mgr.PauseDayNight = v; _mgr.NotifyChanged(); });
        }

        // 1 Thời lượng 1 ngày (Slider)
        var dur = GetControl<Slider>(p, 1);
        if (dur != null)
        {
            dur.minValue = 60f; dur.maxValue = 900f; dur.value = _mgr.DayDuration;
            dur.onValueChanged.RemoveAllListeners();
            dur.onValueChanged.AddListener(v => { _mgr.DayDuration = v; _mgr.NotifyChanged(); });
        }

        // 2 Giờ trong ngày (Slider)
        var time = GetControl<Slider>(p, 2);
        if (time != null)
        {
            time.minValue = 0f; time.maxValue = 24f; time.value = _mgr.TimeOfDay * 24f;
            time.onValueChanged.RemoveAllListeners();
            time.onValueChanged.AddListener(v => { _mgr.TimeOfDay = v / 24f; _mgr.NotifyChanged(); });
        }

        // 3 Đèn đường (Toggle)
        var lamps = GetControl<Toggle>(p, 3);
        if (lamps != null)
        {
            lamps.isOn = _mgr.StreetLamps;
            lamps.onValueChanged.RemoveAllListeners();
            lamps.onValueChanged.AddListener(v => { _mgr.StreetLamps = v; _mgr.NotifyChanged(); });
        }
    }

    // ─────────────────────────────────────────
    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1280, 720),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160)
    };

    private static int ResToIndex(int w, int h)
    {
        for (int i = 0; i < Resolutions.Length; i++)
            if (Resolutions[i].x == w && Resolutions[i].y == h) return i;
        return 0;
    }

    private static Vector2Int ResFromIndex(int index) =>
        Resolutions[Mathf.Clamp(index, 0, Resolutions.Length - 1)];
}
