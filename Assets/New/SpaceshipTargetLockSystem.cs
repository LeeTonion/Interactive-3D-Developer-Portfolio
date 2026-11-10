using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Hệ Thống Khóa Mục Tiêu & Theo Dõi Tàu Địch (Sci-Fi Target Lock-On & Tracking HUD):
    /// - Quét các phi thuyền địch (SpaceTrafficShipAI) và mục tiêu trong tầm nhìn không gian.
    /// - Hiển thị khung ngắm hình thoi 4 góc (▲ ▼ ◄ ►) cùng nhãn TRK / LOCK và khoảng cách như thiết kế ảnh 2.
    /// - Điều kiện khóa mục tiêu:
    ///   + Phải nằm trong tầm nhìn 40m của tàu (dist <= 40m).
    ///   + Phải giữ tâm ngắm liên tục vào mục tiêu trong 3 giây (3.0s).
    /// - Khi đang khóa (Locking): hiển thị đồng hồ đếm ngược 3s -> 0s kèm thu hẹp 4 góc hình thoi.
    /// - Khi đã khóa cứng (LOCK): pháo Laser Plasma tự động hội tụ bắn chính xác vào tàu địch.
    /// </summary>
    public class SpaceshipTargetLockSystem : MonoBehaviour
    {
        [Header("Cấu Hình Tầm Quét (Tracking Range)")]
        public float maxTrackingDistance = 1600f;
        public float lockAngleThreshold = 18f;
        public float trackingFovAngle = 65f;

        [Header("Điều Kiện Khóa Mục Tiêu (Lock-On Requirements)")]
        [Tooltip("Khoảng cách tối đa so với tàu để có thể khóa mục tiêu (mặc định: 300m theo yêu cầu người dùng)")]
        public float maxLockDistance = 300f;

        [Tooltip("Thời gian giữ tâm ngắm liên tục vào mục tiêu để khóa cứng (mặc định: 3 giây)")]
        public float lockHoldDuration = 3.0f;

        [Header("Màu Sắc HUD Khóa Mục Tiêu")]
        public Color trackingColor = new Color(1f, 0.42f, 0.22f, 0.95f);
        public Color lockingColor = new Color(1f, 0.68f, 0.15f, 1f);
        [Tooltip("Màu khi đã khóa cứng mục tiêu (chuyển màu xanh theo yêu cầu người dùng)")]
        public Color lockedColor = new Color(0.15f, 1f, 0.45f, 1f);

        private Camera _mainCam;
        private Canvas _canvas;
        private RectTransform _canvasRt;
        private RectTransform _hudRootRt;   // RectTransform của _trackingUiRoot
        private GameObject _trackingUiRoot;
        private Transform _currentLockedTarget;
        private Transform _potentialLockCandidate;
        private float _lockHoldTimer = 0f;

        private readonly List<TrackingMarkerUI> _markers = new List<TrackingMarkerUI>();
        private const int MaxTrackedTargets = 8;

        public Transform CurrentLockedTarget => _currentLockedTarget;
        public bool HasLockedTarget => _currentLockedTarget != null;
        public float LockProgress => Mathf.Clamp01(_lockHoldTimer / lockHoldDuration);

        private class TrackingMarkerUI
        {
            public GameObject Root;
            public RectTransform RectTr;
            public TMP_Text LabelText;
            public TMP_Text DistText;
            public Image ArrowTop;
            public Image ArrowBottom;
            public Image ArrowLeft;
            public Image ArrowRight;
            public RectTransform ArrowTopRt;
            public RectTransform ArrowBottomRt;
            public RectTransform ArrowLeftRt;
            public RectTransform ArrowRightRt;
            public bool IsInUse;
        }

        private void Start()
        {
            _mainCam = Camera.main;
            EnsureTrackingUI();
        }

        private void Update()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            if (AboutInfoStation.AnyStationActive)
            {
                if (_trackingUiRoot != null && _trackingUiRoot.activeSelf)
                    _trackingUiRoot.SetActive(false);
                return;
            }

            // Chỉ gọi EnsureTrackingUI nếu markers bị mất (không gọi mỗi frame)
            if (_markers.Count == 0) EnsureTrackingUI();
            if (_markers.Count == 0) return;

            if (_trackingUiRoot != null && !_trackingUiRoot.activeSelf)
                _trackingUiRoot.SetActive(true);

            UpdateTargetLockingAndTracking();
        }

        private void EnsureTrackingUI()
        {
            if (_trackingUiRoot != null && _markers.Count > 0) return;

            // Tìm Canvas ScreenSpaceOverlay
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas targetCanvas = null;
            foreach (var c in allCanvases)
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.name == "Canvas")
                { targetCanvas = c; break; }
            if (targetCanvas == null)
                foreach (var c in allCanvases)
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    { targetCanvas = c; break; }
            if (targetCanvas == null) return;

            _canvas   = targetCanvas;
            _canvasRt = targetCanvas.GetComponent<RectTransform>();

            // Xóa TẤT CẢ HUD root cũ còn tồn tại - DestroyImmediate để không deferred
            var existing = targetCanvas.transform.Find("TargetTracking_HUD_Root");
            while (existing != null)
            {
                DestroyImmediate(existing.gameObject);
                existing = targetCanvas.transform.Find("TargetTracking_HUD_Root");
            }

            _trackingUiRoot = new GameObject("TargetTracking_HUD_Root", typeof(RectTransform));
            _trackingUiRoot.transform.SetParent(targetCanvas.transform, false);

            _hudRootRt = _trackingUiRoot.GetComponent<RectTransform>();
            _hudRootRt.anchorMin  = Vector2.zero;
            _hudRootRt.anchorMax  = Vector2.one;
            _hudRootRt.pivot      = new Vector2(0.5f, 0.5f);
            _hudRootRt.offsetMin  = Vector2.zero;
            _hudRootRt.offsetMax  = Vector2.zero;

            _markers.Clear();
            for (int i = 0; i < MaxTrackedTargets; i++)
                _markers.Add(CreateMarkerUI(_trackingUiRoot.transform, i));
        }

        private TrackingMarkerUI CreateMarkerUI(Transform parent, int index)
        {
            var go = new GameObject("Marker_" + index, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 80f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var marker = new TrackingMarkerUI
            {
                Root = go,
                RectTr = rt
            };

            // 4 mũi tên kiểu bracket ◇ như ảnh 2 - pointing inward
            float arrowOffset = 28f;
            float arrowSize = 11f;

            marker.ArrowTop    = CreateArrow("ArrowTop",    go.transform, new Vector2(0f,  arrowOffset), arrowSize, 0f,   out marker.ArrowTopRt);
            marker.ArrowBottom = CreateArrow("ArrowBottom",  go.transform, new Vector2(0f, -arrowOffset), arrowSize, 180f, out marker.ArrowBottomRt);
            marker.ArrowLeft   = CreateArrow("ArrowLeft",   go.transform, new Vector2(-arrowOffset, 0f), arrowSize, 90f,  out marker.ArrowLeftRt);
            marker.ArrowRight  = CreateArrow("ArrowRight",  go.transform, new Vector2( arrowOffset, 0f), arrowSize, 270f, out marker.ArrowRightRt);

            // Nhãn TRK / LOCK (trái + trên)
            marker.LabelText = CreateText("LabelTRK", go.transform, "TRK", 13f, trackingColor, TextAlignmentOptions.MidlineLeft);
            var lblRt = marker.LabelText.rectTransform;
            lblRt.anchoredPosition = new Vector2(-arrowOffset - 2f, arrowOffset + 6f);
            lblRt.sizeDelta = new Vector2(80f, 20f);

            // Khoảng cách bên dưới nhãn
            marker.DistText = CreateText("DistText", go.transform, "0.8km", 11.5f, trackingColor, TextAlignmentOptions.MidlineLeft);
            var distRt = marker.DistText.rectTransform;
            distRt.anchoredPosition = new Vector2(-arrowOffset - 2f, arrowOffset - 8f);
            distRt.sizeDelta = new Vector2(80f, 20f);

            go.SetActive(false);
            return marker;
        }

        private Image CreateArrow(string name, Transform parent, Vector2 pos, float size, float rotZ, out RectTransform rectTr)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = trackingColor;
            img.raycastTarget = false;

            rectTr = img.rectTransform;
            rectTr.sizeDelta = new Vector2(size, size * 0.7f);
            rectTr.anchoredPosition = pos;
            rectTr.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            return img;
        }

        private TMP_Text CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void UpdateTargetLockingAndTracking()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;
            EnsureTrackingUI();

            for (int i = 0; i < _markers.Count; i++) _markers[i].IsInUse = false;

            IReadOnlyList<SpaceTrafficShipAI> allShips = SpaceTrafficShipAI.ActiveShips;
            if (allShips == null || allShips.Count == 0)
            {
                allShips = FindObjectsByType<SpaceTrafficShipAI>(FindObjectsSortMode.None);
            }

            Vector3 camPos = _mainCam.transform.position;
            Vector3 camForward = _mainCam.transform.forward;
            Vector3 shipPos = transform.position;

            Transform bestCandidateInAim = null;
            float smallestLockAngle = float.MaxValue;

            // Thu thập các mục tiêu hợp lệ trong tầm nhìn
            var candidateTargets = new List<(Transform target, float distCam, float distShip, float angle)>();

            foreach (var ship in allShips)
            {
                if (ship == null) continue;

                Transform tr = ship.transform;
                Vector3 toTargetCam = tr.position - camPos;
                float distCam = toTargetCam.magnitude;
                if (distCam > maxTrackingDistance || distCam < 4f) continue;

                Vector3 dir = toTargetCam / distCam;
                float angle = Vector3.Angle(camForward, dir);
                if (angle > trackingFovAngle) continue;

                float distShip = Vector3.Distance(shipPos, tr.position);
                candidateTargets.Add((tr, distCam, distShip, angle));

                // Điều kiện để được đưa vào diện khóa mục tiêu:
                // 1) Phải trong tầm nhìn 300m của tàu (distShip <= 300m)
                // 2) Phải nằm trong góc ngắm (angle < lockAngleThreshold)
                if (distShip <= maxLockDistance && angle < lockAngleThreshold && angle < smallestLockAngle)
                {
                    smallestLockAngle = angle;
                    bestCandidateInAim = tr;
                }
            }

            // Xử lý đếm thời gian 3 giây liên tục để khóa mục tiêu
            if (bestCandidateInAim != null)
            {
                if (_potentialLockCandidate == bestCandidateInAim)
                {
                    _lockHoldTimer += Time.deltaTime;
                }
                else
                {
                    _potentialLockCandidate = bestCandidateInAim;
                    _lockHoldTimer = 0f;
                }

                if (_lockHoldTimer >= lockHoldDuration)
                {
                    _currentLockedTarget = _potentialLockCandidate;
                }
            }
            else
            {
                _potentialLockCandidate = null;
                _lockHoldTimer = Mathf.Max(0f, _lockHoldTimer - Time.deltaTime * 2.5f);

                // Nếu đã khóa mục tiêu từ trước, kiểm tra xem mục tiêu có bay quá 300m hoặc ra ngoài tầm ngắm không
                // Lưu ý: Tàu bị phá hủy KHÔNG mất dấu hiệu khóa (theo yêu cầu người dùng)
                if (_currentLockedTarget != null)
                {
                    float distShip = Vector3.Distance(shipPos, _currentLockedTarget.position);
                    Vector3 toTarget = _currentLockedTarget.position - camPos;
                    float angle = Vector3.Angle(camForward, toTarget.normalized);

                    // Mất khóa chỉ khi bay ra quá xa (vượt quá tầm khóa 300m + 25% trễ) hoặc quay đầu lệch góc quá 65 độ
                    if (distShip > maxLockDistance * 1.25f || angle > 65f)
                    {
                        _currentLockedTarget = null;
                    }
                }
            }

            // Sắp xếp các mục tiêu theo độ ưu tiên (gần tâm ngắm nhất lên đầu)
            candidateTargets.Sort((a, b) => a.angle.CompareTo(b.angle));

            int markerIdx = 0;
            for (int i = 0; i < candidateTargets.Count && markerIdx < _markers.Count; i++)
            {
                var candidate = candidateTargets[i];
                Vector3 screenPoint = _mainCam.WorldToScreenPoint(candidate.target.position);

                if (screenPoint.z <= 0f) continue; // Phía sau camera

                var marker = _markers[markerIdx++];
                marker.IsInUse = true;
                marker.Root.SetActive(true);

                // ======= POSITIONING =======
                // Dùng canvas scaleFactor để chuyển screen pixel → canvas local coords
                // Cách này work với bất kỳ anchor nào:
                // - Với anchor=(0.5,0.5) pivot center: anchoredPos = (screenPt - screenCenter) / scaleFactor
                // - Với anchor=(0,0) pivot bottom-left: anchoredPos = screenPt / scaleFactor
                // Ta set anchor = (0.5,0.5) + dùng RectTransformUtility để đảm bảo đúng
                if (_hudRootRt == null && _trackingUiRoot != null)
                    _hudRootRt = _trackingUiRoot.GetComponent<RectTransform>();

                if (_hudRootRt != null)
                {
                    // Force anchor = center pivot trên marker trước khi tính
                    marker.RectTr.anchorMin = new Vector2(0.5f, 0.5f);
                    marker.RectTr.anchorMax = new Vector2(0.5f, 0.5f);
                    marker.RectTr.pivot     = new Vector2(0.5f, 0.5f);

                    Camera cam2D = (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
                        ? _canvas.worldCamera : null;

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _hudRootRt, screenPoint, cam2D, out Vector2 localPt))
                    {
                        marker.RectTr.anchoredPosition = localPt;
                    }
                }

                bool isLocked = candidate.target == _currentLockedTarget;
                bool isLocking = candidate.target == _potentialLockCandidate && _lockHoldTimer > 0f && !isLocked;

                Color activeColor;
                float arrowDist = 26f;

                if (isLocked)
                {
                    activeColor = lockedColor;
                    arrowDist = 20f;
                    float pulse = 1f + 0.14f * Mathf.Sin(Time.time * 14f);
                    marker.RectTr.localScale = new Vector3(pulse, pulse, 1f);
                    marker.LabelText.text = "LOCK";
                    marker.LabelText.color = lockedColor;
                }
                else if (isLocking)
                {
                    float progress = Mathf.Clamp01(_lockHoldTimer / lockHoldDuration);
                    float timeLeft = Mathf.Max(0f, lockHoldDuration - _lockHoldTimer);
                    activeColor = Color.Lerp(lockingColor, lockedColor, progress);
                    arrowDist = Mathf.Lerp(28f, 21f, progress);

                    float fastPulse = 1f + 0.08f * Mathf.Sin(Time.time * 22f);
                    marker.RectTr.localScale = new Vector3(fastPulse, fastPulse, 1f);
                    marker.LabelText.text = $"LOCK {timeLeft:F1}s";
                    marker.LabelText.color = activeColor;
                }
                else
                {
                    activeColor = trackingColor;
                    arrowDist = 26f;
                    marker.RectTr.localScale = Vector3.one;
                    marker.LabelText.text = "TRK";
                    marker.LabelText.color = trackingColor;
                }

                // Cập nhật vị trí 4 mũi tên hình thoi co giãn theo trạng thái khóa
                marker.ArrowTopRt.anchoredPosition = new Vector2(0f, arrowDist);
                marker.ArrowBottomRt.anchoredPosition = new Vector2(0f, -arrowDist);
                marker.ArrowLeftRt.anchoredPosition = new Vector2(-arrowDist, 0f);
                marker.ArrowRightRt.anchoredPosition = new Vector2(arrowDist, 0f);

                marker.ArrowTop.color = activeColor;
                marker.ArrowBottom.color = activeColor;
                marker.ArrowLeft.color = activeColor;
                marker.ArrowRight.color = activeColor;

                // Hiển thị khoảng cách: nếu < 100m hiển thị dạng số mét (38m) cho dễ căn tầm 40m, nếu >= 100m hiển thị dạng km (0.8km)
                if (candidate.distShip < 100f)
                {
                    marker.DistText.text = $"{candidate.distShip:F0}m";
                }
                else
                {
                    float distKm = candidate.distShip / 1000f;
                    marker.DistText.text = distKm >= 1.0f ? $"{distKm:F1}km" : $"{distKm:F2}km";
                }
                marker.DistText.color = activeColor;
            }

            // Tắt các marker không dùng
            for (int i = 0; i < _markers.Count; i++)
            {
                if (!_markers[i].IsInUse && _markers[i].Root.activeSelf)
                {
                    _markers[i].Root.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Lấy tọa độ mục tiêu đang bị khóa cứng để pháo laser tự động hội tụ bắn trúng
        /// </summary>
        public bool TryGetLockAimPoint(out Vector3 aimPoint)
        {
            if (_currentLockedTarget != null)
            {
                var rb = _currentLockedTarget.GetComponent<Rigidbody>();
                Vector3 lead = Vector3.zero;
                if (rb != null)
                {
                    float dist = Vector3.Distance(transform.position, _currentLockedTarget.position);
                    float travelTime = dist / 125f;
                    lead = rb.linearVelocity * (travelTime * 0.7f);
                }
                aimPoint = _currentLockedTarget.position + lead;
                return true;
            }
            aimPoint = Vector3.zero;
            return false;
        }
    }
}
