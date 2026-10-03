using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Displays detailed information for each portfolio section.
    /// Supports both TextMeshPro and standard Unity UI Text components.
    /// </summary>
    public class PortfolioPanelUI : MonoBehaviour
    {
        [Header("UI Text References (TMP or standard UI Text)")]
        [SerializeField] private TextMeshProUGUI titleTmpText;
        [SerializeField] private TextMeshProUGUI bodyTmpText;
        [SerializeField] private Text titleUiText;
        [SerializeField] private Text bodyUiText;

        [Header("Controls")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Core.UIManager uiManager;

        private void Awake()
        {
            FindTextComponentsIfNeeded();

            if (closeButton == null)
                closeButton = GetComponentInChildren<Button>(true);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void FindTextComponentsIfNeeded()
        {
            if (titleTmpText == null && titleUiText == null)
            {
                var texts = GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.name.ToLower().Contains("title") || t.name.ToLower().Contains("header"))
                        titleUiText = t;
                    else if (t.name.ToLower().Contains("body") || t.name.ToLower().Contains("content"))
                        bodyUiText = t;
                }
            }
        }

        public void Populate(PortfolioAreaType areaType, string label)
        {
            FindTextComponentsIfNeeded();

            string title = string.IsNullOrEmpty(label) ? areaType.ToString() : label;
            string content = GetAreaContent(areaType, title);

            SetTitle(title);
            SetBody(content);
        }

        public void Populate(PortfolioArea area)
        {
            if (area == null) return;
            Populate(area.AreaType, area.AreaLabel);
        }

        private void SetTitle(string text)
        {
            if (titleTmpText != null) titleTmpText.text = text;
            if (titleUiText != null) titleUiText.text = text;
        }

        private void SetBody(string text)
        {
            if (bodyTmpText != null) bodyTmpText.text = text;
            if (bodyUiText != null) bodyUiText.text = text;
        }

        private string GetAreaContent(PortfolioAreaType areaType, string title)
        {
            switch (areaType)
            {
                case PortfolioAreaType.About:
                    return "Xin chào! Tôi là một Unity Game & Simulation Developer đam mê sáng tạo.\n\n" +
                           "• Chuyên môn: Lập trình Gameplay 3D, Tối ưu hiệu năng, Vật lý Xe cộ & Hệ thống Điều khiển.\n" +
                           "• Mục tiêu: Xây dựng các trải nghiệm 3D tương tác sống động, trực quan và tối ưu mượt mà trên mọi thiết bị.\n\n" +
                           "Dự án 3D Portfolio này là sự kết hợp giữa thiết kế thành phố tương tác và công nghệ GPS dẫn đường thông minh.";

                case PortfolioAreaType.Skills:
                    return "KỸ NĂNG CHUYÊN MÔN (TECHNICAL SKILLS):\n\n" +
                           "• Ngôn ngữ: C#, C++, Python, HLSL / ShaderLab\n" +
                           "• Game Engine: Unity 3D (URP/HDRP), Unreal Engine 5\n" +
                           "• Kỹ thuật Cốt lõi: Node Graph A* Pathfinding, Custom Physics (Vehicle Controller), State Machine, Object Pooling\n" +
                           "• UI & Tools: Unity UI / UI Toolkit, Cinemachine, Input System, Git, CI/CD Workflow\n" +
                           "• Tối ưu hóa: Profiler, Memory Management, Draw Calls & Batching Optimization";

                case PortfolioAreaType.Projects:
                    return "CÁC DỰ ÁN NỔI BẬT (FEATURED PROJECTS):\n\n" +
                           "1. Interactive 3D Portfolio (Dự án hiện tại)\n" +
                           "   - Thành phố 3D tương tác với hệ thống xe hơi vật lý thực tế, GPS Navigation A* Road Graph và hệ thống Trigger Zone tự động.\n\n" +
                           "2. 3D Action RPG Combat System\n" +
                           "   - Hệ thống Combo đòn đánh mượt mà, AI Boss hành vi đa dạng với Behavior Tree.\n\n" +
                           "3. Multiplayer High-Speed Racing Game\n" +
                           "   - Đua xe nhiều người chơi qua mạng với Client-side Prediction và Server Reconciliation.";

                case PortfolioAreaType.Experience:
                    return "KINH NGHIỆM LÀM VIỆC (WORK EXPERIENCE):\n\n" +
                           "• Senior Unity Developer (2023 - Hiện tại)\n" +
                           "  - Kiến trúc hệ thống Core Gameplay, phát triển module tương tác và quản lý vùng bản đồ thời gian thực.\n" +
                           "  - Nâng cao hiệu năng rendering và tối ưu hóa bộ nhớ cho các cảnh 3D quy mô lớn.\n\n" +
                           "• Game Developer (2021 - 2023)\n" +
                           "  - Lập trình cơ chế điều khiển phương tiện, tích hợp âm thanh FMOD và thiết kế giao diện UI/UX trực quan.";

                case PortfolioAreaType.Education:
                    return "HỌC VẤN & CHỨNG CHỈ (EDUCATION & CERTIFICATIONS):\n\n" +
                           "• Cử nhân Công nghệ Thông tin / Kỹ thuật Phần mềm\n" +
                           "• Unity Certified Professional: Programmer\n" +
                           "• Khóa học chuyên sâu: Advanced 3D Graphics & Game Architecture";

                case PortfolioAreaType.Contact:
                    return "THÔNG TIN LIÊN HỆ (CONTACT INFORMATION):\n\n" +
                           "• Email: leetoniom.dev@gmail.com\n" +
                           "• GitHub: github.com/LeeTonion\n" +
                           "• LinkedIn: linkedin.com/in/leetoniom\n" +
                           "• Portfolio: leetoniom.portfolio.io\n\n" +
                           "Rất vui được kết nối và hợp tác cùng bạn trong các dự án tương lai!";

                default:
                    return $"Thông tin chi tiết khu vực [{title}] đang được cập nhật.";
            }
        }

        private void OnCloseClicked()
        {
            if (uiManager != null)
                uiManager.ClosePortfolioPanel();
            else
                gameObject.SetActive(false);
        }
    }
}
