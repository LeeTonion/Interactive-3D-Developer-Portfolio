using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Shared AAA world-anchored floating interaction prompt (white circular key cap + crisp outlined uppercase label),
    /// projected directly onto the 3D interactable object in screen space (no clunky bottom-center GUI.Box).
    /// </summary>
    public static class WorldInteractPromptUI
    {
        private static Texture2D _circleTex;
        private static GUIStyle _keyStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _labelOutlineStyle;

        private static void EnsureAssets()
        {
            if (_circleTex == null)
            {
                const int size = 64;
                _circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                _circleTex.wrapMode = TextureWrapMode.Clamp;
                _circleTex.filterMode = FilterMode.Bilinear;

                float center = (size - 1) * 0.5f;
                float outerR = size * 0.47f;
                float innerR = size * 0.40f;

                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        if (d <= innerR)
                        {
                            pixels[y * size + x] = new Color(1f, 1f, 1f, 0.96f);
                        }
                        else if (d <= outerR)
                        {
                            float edge = Mathf.Clamp01((outerR - d) * 1.6f);
                            pixels[y * size + x] = new Color(0.06f, 0.08f, 0.12f, 0.92f * edge);
                        }
                        else
                        {
                            pixels[y * size + x] = Color.clear;
                        }
                    }
                }
                _circleTex.SetPixels(pixels);
                _circleTex.Apply();
            }

            if (_keyStyle == null)
            {
                _keyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _keyStyle.normal.textColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 19,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                _labelStyle.normal.textColor = Color.white;
            }

            if (_labelOutlineStyle == null)
            {
                _labelOutlineStyle = new GUIStyle(_labelStyle);
                _labelOutlineStyle.normal.textColor = new Color(0.04f, 0.05f, 0.08f, 0.96f);
            }
        }

        /// <summary>
        /// Draws a crisp floating "(KEY) ACTION" prompt anchored directly on the 3D world position.
        /// </summary>
        public static void DrawAtWorldPos(Vector3 worldPos, string keyLabel, string actionLabel)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0.15f) return;

            float sx = screenPos.x;
            float sy = Screen.height - screenPos.y;

            // Clamp softly inside viewport margins so prompt never clips off-screen
            sx = Mathf.Clamp(sx, 80f, Screen.width - 220f);
            sy = Mathf.Clamp(sy, 50f, Screen.height - 50f);

            DrawAtScreenPos(sx, sy, keyLabel, actionLabel);
        }

        /// <summary>
        /// Draws the same crisp "(KEY) ACTION" prompt at an explicit screen coordinate (e.g. inside cockpit).
        /// </summary>
        public static void DrawAtScreenPos(float x, float y, string keyLabel, string actionLabel)
        {
            EnsureAssets();

            const float circleSize = 36f;
            float cx = x - circleSize * 0.5f;
            float cy = y - circleSize * 0.5f;

            // 1. White circular key cap with crisp dark border
            GUI.DrawTexture(new Rect(cx, cy, circleSize, circleSize), _circleTex, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(cx, cy - 1f, circleSize, circleSize), keyLabel, _keyStyle);

            // 2. Crisp outlined uppercase action label right beside the circle
            if (!string.IsNullOrEmpty(actionLabel))
            {
                string upper = actionLabel.ToUpperInvariant();
                Rect textRect = new Rect(cx + circleSize + 8f, cy, 320f, circleSize);
                DrawOutlinedLabel(textRect, upper);
            }
        }

        private static void DrawOutlinedLabel(Rect r, string text)
        {
            const float o = 1.6f;
            GUI.Label(new Rect(r.x - o, r.y, r.width, r.height), text, _labelOutlineStyle);
            GUI.Label(new Rect(r.x + o, r.y, r.width, r.height), text, _labelOutlineStyle);
            GUI.Label(new Rect(r.x, r.y - o, r.width, r.height), text, _labelOutlineStyle);
            GUI.Label(new Rect(r.x, r.y + o, r.width, r.height), text, _labelOutlineStyle);
            GUI.Label(new Rect(r.x - o, r.y - o, r.width, r.height), text, _labelOutlineStyle);
            GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, _labelOutlineStyle);
            GUI.Label(r, text, _labelStyle);
        }
    }
}
