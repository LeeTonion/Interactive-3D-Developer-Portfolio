using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Hiệu ứng đèn LED trạng thái chớp nháy ngẫu nhiên trên các tủ máy chủ (Server Racks).
    /// Sử dụng MaterialPropertyBlock để tối ưu hiệu năng tuyệt đối, không sinh rác bộ nhớ (Zero Allocation).
    /// </summary>
    public class ServerBlinkingLights : MonoBehaviour
    {
        [Header("Tham chiếu Renderer")]
        public Renderer[] ledRenderers;
        public Light rackActivityLight;

        [Header("Tần số chớp (Blink Rate)")]
        public float minBlinkInterval = 0.08f;
        public float maxBlinkInterval = 0.28f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;
        private float[] _timers;
        private Color[] _activeColors;

        private static readonly Color[] Palette = new Color[]
        {
            new Color(0.1f, 1.0f, 0.4f) * 2.2f, // Green (Online)
            new Color(0.1f, 0.9f, 1.0f) * 2.2f, // Cyan (Network I/O)
            new Color(1.0f, 0.6f, 0.1f) * 2.2f, // Amber (Cache Active)
            new Color(1.0f, 0.15f, 0.2f) * 1.8f, // Red (Warning/Heartbeat)
            new Color(0.02f, 0.05f, 0.08f) * 0.1f // Off/Dim
        };

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (ledRenderers == null || ledRenderers.Length == 0)
            {
                ledRenderers = GetComponentsInChildren<Renderer>();
            }

            if (ledRenderers != null && ledRenderers.Length > 0)
            {
                _timers = new float[ledRenderers.Length];
                _activeColors = new Color[ledRenderers.Length];
                for (int i = 0; i < ledRenderers.Length; i++)
                {
                    _timers[i] = Random.Range(minBlinkInterval, maxBlinkInterval);
                    _activeColors[i] = Palette[Random.Range(0, Palette.Length)];
                }
            }
        }

        private void Update()
        {
            if (ledRenderers == null || ledRenderers.Length == 0) return;

            float dt = Time.deltaTime;
            for (int i = 0; i < ledRenderers.Length; i++)
            {
                _timers[i] -= dt;
                if (_timers[i] <= 0f)
                {
                    _timers[i] = Random.Range(minBlinkInterval, maxBlinkInterval);
                    _activeColors[i] = Palette[Random.Range(0, Palette.Length)];

                    if (ledRenderers[i] != null)
                    {
                        ledRenderers[i].GetPropertyBlock(_mpb);
                        _mpb.SetColor(EmissionColorId, _activeColors[i]);
                        ledRenderers[i].SetPropertyBlock(_mpb);
                    }
                }
            }

            if (rackActivityLight != null)
            {
                rackActivityLight.intensity = 1.0f + Mathf.PingPong(Time.time * 4f, 0.8f);
            }
        }
    }
}

