using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Bàn phím / Thiết bị điều khiển truy cập tại Cổng Tây căn cứ (Entrance Keypad Terminal).
    /// Hiển thị màn hình khóa an ninh, hoạt họa nhập mật mã, đèn LED báo trạng thái và âm thanh cyber.
    /// </summary>
    public class CitadelKeypadTerminal : MonoBehaviour
    {
        public static CitadelKeypadTerminal Instance { get; private set; }

        [Header("Terminal References")]
        public GameObject consoleModel;
        public Canvas screenCanvas;
        public TMP_Text titleText;
        public TMP_Text codeDisplayText;
        public TMP_Text statusText;
        public Image lockStatusIcon;
        public Light terminalScreenLight;

        private AudioSource _audioSource;
        private AudioClip _beepLowClip;
        private AudioClip _beepHighClip;
        private AudioClip _accessGrantedClip;
        private AudioClip _accessDeniedClip;

        private static readonly Color ColorLocked = new Color(1f, 0.25f, 0.25f);
        private static readonly Color ColorGranted = new Color(0.2f, 1f, 0.45f);
        private static readonly Color ColorAmber = new Color(1f, 0.75f, 0.2f);
        private static readonly Color ColorCyan = new Color(0.15f, 0.9f, 1f);

        public bool IsAccessGranted { get; private set; }

        private void Awake()
        {
            Instance = this;
            BuildProceduralAudio();
            EnsureTerminalSetup();
            ResetTerminal();
        }

        private void BuildProceduralAudio()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0.5f;
            _audioSource.playOnAwake = false;

            _beepLowClip = CreateTone(720f, 0.055f, 0.25f);
            _beepHighClip = CreateTone(980f, 0.065f, 0.28f);
            _accessGrantedClip = CreateTwoToneChime(660f, 990f, 0.32f);
            _accessDeniedClip = CreateTwoToneChime(420f, 310f, 0.28f);
        }

        private static AudioClip CreateTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = 1f - ((float)i / samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * decay * volume;
            }
            var clip = AudioClip.Create("Tone_" + frequency, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateTwoToneChime(float freq1, float freq2, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            int mid = samples / 2;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float f = (i < mid) ? freq1 : freq2;
                float decay = 1f - ((float)i / samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * decay * 0.35f;
            }
            var clip = AudioClip.Create("Chime_" + freq1 + "_" + freq2, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayBeep(bool high)
        {
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(high ? _beepHighClip : _beepLowClip);
            }
        }

        public void PlayGrantedChime()
        {
            if (_audioSource != null && _accessGrantedClip != null)
            {
                _audioSource.PlayOneShot(_accessGrantedClip);
            }
        }

        public void EnsureTerminalSetup()
        {
            // Tìm hoặc đặt model console
            if (consoleModel == null)
            {
                var existing = transform.Find("Console_Stand");
                if (existing != null) consoleModel = existing.gameObject;
            }

            if (consoleModel == null)
            {
#if UNITY_EDITOR
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console.prefab");
                if (prefab != null)
                {
                    consoleModel = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
                    consoleModel.name = "Console_Stand";
                    consoleModel.transform.localPosition = Vector3.zero;
                    consoleModel.transform.localRotation = Quaternion.identity;
                }
#endif
            }

            // Tạo màn hình giao diện Canvas World-Space nghiêng theo mặt phẳng màn hình console
            if (screenCanvas == null)
            {
                var canvasTr = transform.Find("Keypad_Screen_Canvas");
                if (canvasTr != null) screenCanvas = canvasTr.GetComponent<Canvas>();
            }

            if (screenCanvas == null)
            {
                GameObject canvasGo = new GameObject("Keypad_Screen_Canvas");
                canvasGo.transform.SetParent(transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 1.18f, 0.05f);
                canvasGo.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
                canvasGo.transform.localScale = Vector3.one * 0.0016f;

                screenCanvas = canvasGo.AddComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.WorldSpace;
                var rt = canvasGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(480f, 320f);

                // Khung nền đen bóng Sci-Fi
                var bgGo = new GameObject("Screen_Background");
                bgGo.transform.SetParent(canvasGo.transform, false);
                var bgRt = bgGo.AddComponent<RectTransform>();
                bgRt.sizeDelta = new Vector2(470f, 310f);
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.color = new Color(0.02f, 0.06f, 0.12f, 0.95f);

                // Viền phát quang Neon Cyan
                var outlineGo = new GameObject("Border_Glow");
                outlineGo.transform.SetParent(canvasGo.transform, false);
                var outRt = outlineGo.AddComponent<RectTransform>();
                outRt.sizeDelta = new Vector2(474f, 314f);
                var outImg = outlineGo.AddComponent<Image>();
                outImg.color = new Color(0.1f, 0.8f, 1f, 0.4f);

                // Tiêu đề
                var titleGo = new GameObject("Title_Text");
                titleGo.transform.SetParent(canvasGo.transform, false);
                var titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchoredPosition = new Vector2(0f, 115f);
                titleRt.sizeDelta = new Vector2(440f, 40f);
                titleText = titleGo.AddComponent<TextMeshProUGUI>();
                titleText.text = "CITADEL SECURITY CLEARANCE";
                titleText.fontSize = 20;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = ColorCyan;

                // Ô hiển thị mật khẩu PIN
                var codeBoxGo = new GameObject("Code_Display_Box");
                codeBoxGo.transform.SetParent(canvasGo.transform, false);
                var codeRt = codeBoxGo.AddComponent<RectTransform>();
                codeRt.anchoredPosition = new Vector2(0f, 65f);
                codeRt.sizeDelta = new Vector2(360f, 50f);
                var codeBoxImg = codeBoxGo.AddComponent<Image>();
                codeBoxImg.color = new Color(0.04f, 0.15f, 0.25f, 0.9f);

                var codeTextGo = new GameObject("Code_Text");
                codeTextGo.transform.SetParent(codeBoxGo.transform, false);
                var ctRt = codeTextGo.AddComponent<RectTransform>();
                ctRt.sizeDelta = new Vector2(340f, 44f);
                codeDisplayText = codeTextGo.AddComponent<TextMeshProUGUI>();
                codeDisplayText.text = "[ • • • • ]";
                codeDisplayText.fontSize = 32;
                codeDisplayText.fontStyle = FontStyles.Bold;
                codeDisplayText.alignment = TextAlignmentOptions.Center;
                codeDisplayText.color = ColorAmber;

                // Ma trận bàn phím số 3x4
                var keypadGridGo = new GameObject("Keypad_Grid");
                keypadGridGo.transform.SetParent(canvasGo.transform, false);
                var gridRt = keypadGridGo.AddComponent<RectTransform>();
                gridRt.anchoredPosition = new Vector2(0f, -30f);
                gridRt.sizeDelta = new Vector2(260f, 110f);
                var gridLayout = keypadGridGo.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(74f, 24f);
                gridLayout.spacing = new Vector2(6f, 4f);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 3;

                string[] keyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" };
                foreach (var k in keyLabels)
                {
                    var btnGo = new GameObject("Key_" + k);
                    btnGo.transform.SetParent(keypadGridGo.transform, false);
                    var bImg = btnGo.AddComponent<Image>();
                    bImg.color = new Color(0.08f, 0.22f, 0.35f, 0.85f);

                    var bTxtGo = new GameObject("Text");
                    bTxtGo.transform.SetParent(btnGo.transform, false);
                    var bRt = bTxtGo.AddComponent<RectTransform>();
                    bRt.sizeDelta = new Vector2(70f, 22f);
                    var bTmp = bTxtGo.AddComponent<TextMeshProUGUI>();
                    bTmp.text = k;
                    bTmp.fontSize = 15;
                    bTmp.fontStyle = FontStyles.Bold;
                    bTmp.alignment = TextAlignmentOptions.Center;
                    bTmp.color = ColorCyan;
                }

                // Dòng trạng thái bên dưới
                var statusGo = new GameObject("Status_Text");
                statusGo.transform.SetParent(canvasGo.transform, false);
                var stRt = statusGo.AddComponent<RectTransform>();
                stRt.anchoredPosition = new Vector2(0f, -115f);
                stRt.sizeDelta = new Vector2(440f, 35f);
                statusText = statusGo.AddComponent<TextMeshProUGUI>();
                statusText.text = "LOCKED // ENTER ACCESS PIN";
                statusText.fontSize = 16;
                statusText.fontStyle = FontStyles.Bold;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = ColorLocked;
            }

            // Đèn rọi màn hình
            if (terminalScreenLight == null)
            {
                var lightGo = transform.Find("Terminal_Light");
                if (lightGo != null) terminalScreenLight = lightGo.GetComponent<Light>();
                else
                {
                    var lGo = new GameObject("Terminal_Light");
                    lGo.transform.SetParent(transform, false);
                    lGo.transform.localPosition = new Vector3(0f, 1.45f, 0.25f);
                    terminalScreenLight = lGo.AddComponent<Light>();
                    terminalScreenLight.type = LightType.Point;
                    terminalScreenLight.range = 2.4f;
                    terminalScreenLight.intensity = 1.8f;
                    terminalScreenLight.color = ColorCyan;
                }
            }
        }

        public void ResetTerminal()
        {
            IsAccessGranted = false;
            if (codeDisplayText != null)
            {
                codeDisplayText.text = "[  -  -  -  -  ]";
                codeDisplayText.color = ColorAmber;
            }
            if (statusText != null)
            {
                statusText.text = "LOCKED // ENTER SECURITY PIN";
                statusText.color = ColorLocked;
            }
            if (terminalScreenLight != null)
            {
                terminalScreenLight.color = ColorAmber;
                terminalScreenLight.intensity = 1.2f;
            }
        }

        /// <summary>
        /// Hoạt họa nhập mật khẩu tự động theo kịch bản góc nhìn thứ nhất
        /// </summary>
        public IEnumerator PerformPasscodeEntrySequence(Action onAccessGranted)
        {
            ResetTerminal();
            yield return new WaitForSeconds(0.4f);

            // Bắt đầu nhập từng ký tự mật khẩu (ví dụ: 7, 3, 9, 1)
            string[] digits = { "7", "3", "9", "1" };
            string[] pinStages = {
                "[  *  -  -  -  ]",
                "[  *  *  -  -  ]",
                "[  *  *  *  -  ]",
                "[  *  *  *  *  ]"
            };

            for (int i = 0; i < digits.Length; i++)
            {
                PlayBeep(high: (i % 2 == 1));
                if (codeDisplayText != null)
                {
                    codeDisplayText.text = pinStages[i];
                    codeDisplayText.color = ColorCyan;
                }
                HighlightKeyButton(digits[i]);
                yield return new WaitForSeconds(0.26f);
            }

            // Xác thực
            if (statusText != null)
            {
                statusText.text = "VERIFYING BIOMETRIC CLEARANCE...";
                statusText.color = ColorCyan;
            }
            yield return new WaitForSeconds(0.40f);

            // Chấp thuận truy cập
            IsAccessGranted = true;
            PlayGrantedChime();

            if (codeDisplayText != null)
            {
                codeDisplayText.text = "[ 7 3 9 1 ]";
                codeDisplayText.color = ColorGranted;
            }
            if (statusText != null)
            {
                statusText.text = "<color=#44ff77>ACCESS GRANTED // AIRLOCK CYCLE</color>";
                statusText.color = ColorGranted;
            }
            if (terminalScreenLight != null)
            {
                terminalScreenLight.color = ColorGranted;
                terminalScreenLight.intensity = 3.2f;
            }

            // Nhấp nháy xanh thành công
            for (int f = 0; f < 3; f++)
            {
                if (codeDisplayText != null) codeDisplayText.enabled = false;
                yield return new WaitForSeconds(0.08f);
                if (codeDisplayText != null) codeDisplayText.enabled = true;
                yield return new WaitForSeconds(0.08f);
            }

            yield return new WaitForSeconds(0.2f);
            onAccessGranted?.Invoke();
        }

        private void HighlightKeyButton(string key)
        {
            if (screenCanvas == null) return;
            var keyTr = screenCanvas.transform.Find("Keypad_Grid/Key_" + key);
            if (keyTr != null)
            {
                var img = keyTr.GetComponent<Image>();
                if (img != null)
                {
                    StartCoroutine(FlashButton(img));
                }
            }
        }

        private IEnumerator FlashButton(Image img)
        {
            Color orig = img.color;
            img.color = new Color(0f, 0.95f, 1f, 1f);
            yield return new WaitForSeconds(0.12f);
            if (img != null) img.color = orig;
        }
    }
}

