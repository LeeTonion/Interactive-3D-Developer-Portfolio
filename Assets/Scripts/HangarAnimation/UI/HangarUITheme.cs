using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Centralized Sci-Fi Cockpit UI Theme, procedural glassmorphism sprites,
    /// and Vietnamese Unicode TMP font support for Unity 6 URP.
    /// </summary>
    public static class HangarUITheme
    {
        // Core Futuristic Palette (restrained, high-contrast, no excessive bloom)
        public static readonly Color BgDeepNavy      = new Color(0.020f, 0.045f, 0.085f, 0.94f);
        public static readonly Color BgGlassPanel    = new Color(0.035f, 0.085f, 0.145f, 0.78f);
        public static readonly Color BgHeaderStrip   = new Color(0.045f, 0.125f, 0.215f, 0.92f);
        public static readonly Color BgTrack         = new Color(0.040f, 0.095f, 0.165f, 0.92f);

        public static readonly Color NeonCyan        = new Color(0.16f, 0.90f, 1.00f, 1.00f);
        public static readonly Color ElectricBlue    = new Color(0.18f, 0.56f, 1.00f, 1.00f);
        public static readonly Color StatusGreen     = new Color(0.18f, 0.96f, 0.52f, 1.00f);
        public static readonly Color StatusAmber     = new Color(1.00f, 0.72f, 0.16f, 1.00f);
        public static readonly Color StatusRed       = new Color(1.00f, 0.26f, 0.24f, 1.00f);
        public static readonly Color TextPrimary     = new Color(0.92f, 0.97f, 1.00f, 1.00f);
        public static readonly Color TextMuted       = new Color(0.58f, 0.72f, 0.84f, 0.90f);

        public static readonly Color BtnNormal       = new Color(0.055f, 0.145f, 0.240f, 0.94f);
        public static readonly Color BtnHover        = new Color(0.095f, 0.245f, 0.390f, 0.98f);
        public static readonly Color BtnDisabled     = new Color(0.050f, 0.080f, 0.120f, 0.65f);
        public static readonly Color BtnGreen        = new Color(0.065f, 0.300f, 0.185f, 0.96f);
        public static readonly Color BtnGreenHover   = new Color(0.100f, 0.430f, 0.265f, 1.00f);
        public static readonly Color BtnDanger       = new Color(0.300f, 0.075f, 0.085f, 0.96f);
        public static readonly Color BtnDangerHover  = new Color(0.450f, 0.115f, 0.125f, 1.00f);
        public static readonly Color BtnSelected     = new Color(0.085f, 0.310f, 0.440f, 0.98f);

        private static Sprite _glassFrameSprite;
        private static Sprite _solidRoundedSprite;
        private static Sprite _scanlineSprite;
        private static TMP_FontAsset _unicodeFontAsset;

        public static TMP_FontAsset GetUnicodeFont()
        {
            if (_unicodeFontAsset != null) return _unicodeFontAsset;

            // Create a dynamic OS TMP font asset so all Vietnamese diacritics render crisply
            Font osFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 34);
            if (osFont != null)
            {
                _unicodeFontAsset = TMP_FontAsset.CreateFontAsset(osFont);
                if (_unicodeFontAsset != null)
                {
                    _unicodeFontAsset.name = "Hangar_Vietnamese_DynamicSDF";
                    return _unicodeFontAsset;
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        public static Sprite GetGlassFrameSprite()
        {
            if (_glassFrameSprite != null) return _glassFrameSprite;

            const int sz = 32;
            const int border = 6;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    int dx = Mathf.Min(x, sz - 1 - x);
                    int dy = Mathf.Min(y, sz - 1 - y);
                    // Chamfered sci-fi corners
                    if (dx + dy < 3)
                    {
                        px[y * sz + x] = Color.clear;
                    }
                    else if (dx <= 1 || dy <= 1 || dx + dy == 3)
                    {
                        px[y * sz + x] = Color.white;
                    }
                    else
                    {
                        px[y * sz + x] = new Color(1f, 1f, 1f, 0.92f);
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _glassFrameSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return _glassFrameSprite;
        }

        public static Sprite GetScanlineSprite()
        {
            if (_scanlineSprite != null) return _scanlineSprite;
            const int h = 64;
            Texture2D tex = new Texture2D(4, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] px = new Color[4 * h];
            for (int y = 0; y < h; y++)
            {
                float u = (float)y / (h - 1);
                float alpha = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 2.5f) * 0.16f;
                Color c = new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, alpha);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            _scanlineSprite = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f));
            return _scanlineSprite;
        }

        public static GameObject CreateGlassPanel(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor, Color borderColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image bg = go.AddComponent<Image>();
            bg.sprite = GetGlassFrameSprite();
            bg.type = Image.Type.Sliced;
            bg.color = fillColor;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1.4f, -1.4f);
            return go;
        }

        public static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, Color color, Vector2 pos, Vector2 size, TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Bold)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = GetUnicodeFont();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static HangarStatusIndicator CreateStatusDot(Transform parent, string name, Vector2 pos, float diameter = 10f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(diameter, diameter);

            Image img = go.AddComponent<Image>();
            img.sprite = GetGlassFrameSprite();
            img.type = Image.Type.Sliced;
            img.color = NeonCyan;
            img.raycastTarget = false;

            return go.AddComponent<HangarStatusIndicator>();
        }

        public static HangarAnimatedBar CreateAnimatedBar(Transform parent, string name, string label, Vector2 pos, float totalWidth, Color fillColor)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rowRt = row.AddComponent<RectTransform>();
            rowRt.anchoredPosition = pos;
            rowRt.sizeDelta = new Vector2(totalWidth, 20f);

            CreateTMP(row.transform, "Label", label, 13f, TextMuted, new Vector2(-totalWidth * 0.5f + 34f, 0f), new Vector2(68f, 20f), TextAlignmentOptions.Left);

            float trackW = totalWidth - 118f;
            GameObject track = new GameObject("Track");
            track.transform.SetParent(row.transform, false);
            RectTransform trRt = track.AddComponent<RectTransform>();
            trRt.anchoredPosition = new Vector2(10f, 0f);
            trRt.sizeDelta = new Vector2(trackW, 12f);
            Image trImg = track.AddComponent<Image>();
            trImg.sprite = GetGlassFrameSprite();
            trImg.type = Image.Type.Sliced;
            trImg.color = BgTrack;
            trImg.raycastTarget = false;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(track.transform, false);
            RectTransform fRt = fill.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(1.5f, 1.5f);
            fRt.offsetMax = new Vector2(-1.5f, -1.5f);
            Image fImg = fill.AddComponent<Image>();
            fImg.type = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.color = fillColor;
            fImg.fillAmount = 0.5f;
            fImg.raycastTarget = false;

            TextMeshProUGUI valTmp = CreateTMP(row.transform, "Value", "50%", 13f, TextPrimary, new Vector2(totalWidth * 0.5f - 24f, 0f), new Vector2(52f, 20f), TextAlignmentOptions.Right);

            HangarAnimatedBar bar = row.AddComponent<HangarAnimatedBar>();
            bar.Initialize(fImg, valTmp, fillColor);
            return bar;
        }

        public static HangarSciFiButton CreateSciFiButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color baseCol, Color hoverCol, Color borderCol, Color textCol, Action onClick, float fontSize = 15f)
        {
            GameObject go = CreateGlassPanel(parent, name, pos, size, baseCol, borderCol);
            Button btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());

            TextMeshProUGUI tmp = CreateTMP(go.transform, "Label", label, fontSize, textCol, Vector2.zero, size - new Vector2(14f, 8f), TextAlignmentOptions.Center);
            Outline outline = go.GetComponent<Outline>();
            Image bg = go.GetComponent<Image>();

            HangarSciFiButton widget = go.AddComponent<HangarSciFiButton>();
            widget.Initialize(btn, bg, outline, tmp, baseCol, hoverCol, borderCol, textCol);
            return widget;
        }
    }
}
