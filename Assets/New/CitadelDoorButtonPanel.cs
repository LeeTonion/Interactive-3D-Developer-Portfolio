using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Nút bấm mở cửa phòng chỉ huy Cánh Đông (Command Bridge Door Button Panel).
    /// </summary>
    public class CitadelDoorButtonPanel : MonoBehaviour
    {
        public static CitadelDoorButtonPanel Instance { get; private set; }

        public Canvas buttonCanvas;
        public Image buttonImage;
        public TMP_Text labelText;
        public Light panelLight;

        private AudioSource _audioSource;
        private AudioClip _pressChime;

        private static readonly Color ColorIdle = new Color(0f, 0.75f, 1f, 0.85f);
        private static readonly Color ColorPressed = new Color(0.25f, 1f, 0.45f, 1f);

        private void Awake()
        {
            Instance = this;
            BuildAudio();
            EnsurePanelSetup();
        }

        private void BuildAudio()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0.5f;
            _audioSource.playOnAwake = false;

            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.18f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = 1f - ((float)i / samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * 840f * t) * decay * 0.3f;
            }
            _pressChime = AudioClip.Create("Door_Panel_Click", samples, 1, sampleRate, false);
            _pressChime.SetData(data, 0);
        }

        public void EnsurePanelSetup()
        {
            if (buttonCanvas == null)
            {
                var cTr = transform.Find("Panel_Canvas");
                if (cTr != null) buttonCanvas = cTr.GetComponent<Canvas>();
            }

            if (buttonCanvas == null)
            {
                // Hộp đế nút bấm trên tường
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Panel_Backplate";
                box.transform.SetParent(transform, false);
                box.transform.localPosition = Vector3.zero;
                box.transform.localRotation = Quaternion.identity;
                box.transform.localScale = new Vector3(0.35f, 0.45f, 0.08f);
                var bMr = box.GetComponent<MeshRenderer>();
                var bMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                bMat.color = new Color(0.10f, 0.12f, 0.16f);
                bMat.SetFloat("_Smoothness", 0.7f);
                bMr.material = bMat;
                if (box.GetComponent<Collider>() != null) Destroy(box.GetComponent<Collider>());

                // Canvas World-Space hướng ra hành lang
                GameObject canvasGo = new GameObject("Panel_Canvas");
                canvasGo.transform.SetParent(transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 0f, 0.042f);
                canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                canvasGo.transform.localScale = Vector3.one * 0.0018f;

                buttonCanvas = canvasGo.AddComponent<Canvas>();
                buttonCanvas.renderMode = RenderMode.WorldSpace;
                var rt = canvasGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(160f, 200f);

                // Nền nút bấm phát quang
                var btnGo = new GameObject("Button_Face");
                btnGo.transform.SetParent(canvasGo.transform, false);
                var btnRt = btnGo.AddComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(140f, 180f);
                buttonImage = btnGo.AddComponent<Image>();
                buttonImage.color = new Color(0.02f, 0.08f, 0.14f, 0.95f);

                // Viền phát sáng
                var outlineGo = new GameObject("Glow_Border");
                outlineGo.transform.SetParent(canvasGo.transform, false);
                var oRt = outlineGo.AddComponent<RectTransform>();
                oRt.sizeDelta = new Vector2(144f, 184f);
                var oImg = outlineGo.AddComponent<Image>();
                oImg.color = new Color(0f, 0.75f, 1f, 0.25f);

                // Chữ hiển thị
                var textGo = new GameObject("Label");
                textGo.transform.SetParent(canvasGo.transform, false);
                var tRt = textGo.AddComponent<RectTransform>();
                tRt.sizeDelta = new Vector2(130f, 160f);
                labelText = textGo.AddComponent<TextMeshProUGUI>();
                labelText.text = "<b>BRIDGE ACCESS</b>\n<size=14><color=#44ffbb>[ PRESS TO OPEN ]</color></size>";
                labelText.fontSize = 20;
                labelText.fontStyle = FontStyles.Bold;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = new Color(0.4f, 0.9f, 1f, 1f);
            }

            if (panelLight == null)
            {
                var lTr = transform.Find("Panel_Light");
                if (lTr != null) panelLight = lTr.GetComponent<Light>();
                else
                {
                    var lGo = new GameObject("Panel_Light");
                    lGo.transform.SetParent(transform, false);
                    lGo.transform.localPosition = new Vector3(0f, 0f, 0.15f);
                    panelLight = lGo.AddComponent<Light>();
                    panelLight.type = LightType.Point;
                    panelLight.range = 2.5f;
                    panelLight.intensity = 2.2f;
                    panelLight.color = new Color(0.2f, 0.8f, 1f);
                }
            }
        }

        public IEnumerator PerformPressRoutine(Action onComplete)
        {
            if (_audioSource != null && _pressChime != null)
            {
                _audioSource.PlayOneShot(_pressChime);
            }

            if (labelText != null)
            {
                labelText.text = "<b>BRIDGE ACCESS</b>\n<size=14><color=#44ff77>[ ACCESS GRANTED ]</color></size>";
                labelText.color = ColorPressed;
            }
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.1f, 0.35f, 0.2f, 0.95f);
            }
            if (panelLight != null)
            {
                panelLight.color = ColorPressed;
                panelLight.intensity = 3.0f;
            }

            yield return new WaitForSeconds(0.35f);

            onComplete?.Invoke();

            yield return new WaitForSeconds(0.6f);

            // Phục hồi
            if (labelText != null)
            {
                labelText.text = "BRIDGE\nACCESS\n\n[ OPEN ]";
                labelText.color = ColorIdle;
            }
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.04f, 0.16f, 0.26f, 0.95f);
            }
            if (panelLight != null)
            {
                panelLight.color = ColorIdle;
                panelLight.intensity = 1.5f;
            }
        }
    }
}

