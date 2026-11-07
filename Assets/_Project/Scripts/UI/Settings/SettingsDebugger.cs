using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Diagnostico cài đặt Settings panel: in ra toàn bộ panel, hàng, control
/// và trạng thái raycast để bạn so sánh với mong đợi.
/// Gắn vào SettingPanel root, chạy Play, xem Console.
/// </summary>
public class SettingsDebugger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("===== SETTINGS DEBUGGER =====");
        var content = transform.Find("Content/Scroll View/Viewport/Content");
        if (content == null)
        {
            Debug.LogError("Không tìm thấy Content/Scroll View/Viewport/Content");
            return;
        }

        for (int p = 0; p < content.childCount; p++)
        {
            var panel = content.GetChild(p) as RectTransform;
            Debug.LogFormat("Panel [{0}] '{1}' — {2} hàng", p, panel.name, panel.childCount);

            for (int r = 0; r < panel.childCount; r++)
            {
                var row = panel.GetChild(r) as RectTransform;
                var label = row.Find("Label");
                var control = row.Find("Control");

                string labelText = "?";
                var tmpLabel = label?.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmpLabel != null) labelText = tmpLabel.text;

                string controlInfo = "không tìm thấy Control";
                if (control != null)
                {
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var c in control.GetComponentsInChildren<Component>(true))
                    {
                        var type = c.GetType().Name;
                        if (type == "Transform" || type == "RectTransform" || type == "CanvasRenderer") continue;
                        parts.Add(type);
                    }
                    controlInfo = string.Join(", ", parts);

                    // Kiểm tra raycast
                    var graphics = control.GetComponentsInChildren<Graphic>(true);
                    bool anyRaycast = false;
                    foreach (var g in graphics)
                        if (g.raycastTarget) { anyRaycast = true; break; }
                    controlInfo += $" | raycastTarget={(anyRaycast ? "ON" : "OFF")}";
                }

                Debug.LogFormat("  Hàng {0}: Label='{1}' | Control: {2}", r, labelText, controlInfo);
            }
        }
        Debug.Log("=============================");
    }
}
