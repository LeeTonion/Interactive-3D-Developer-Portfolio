using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Shows full details for a single ProjectData entry.
    /// Opened by ProjectsPanelUI when a card is clicked.
    /// </summary>
    public class ProjectDetailPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image           thumbnailImage;
        [SerializeField] private TextMeshProUGUI projectNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI technologiesText;
        [SerializeField] private TextMeshProUGUI repoUrlText;
        [SerializeField] private TextMeshProUGUI liveUrlText;

        [Header("Buttons")]
        [SerializeField] private Button backButton;   // Returns to project list
        [SerializeField] private Button closeButton;  // Closes the whole portfolio panel
        [SerializeField] private Core.UIManager uiManager;

        // ──────────────────────────────────────────────────────────────────────────
        public event System.Action OnBack;

        private void Awake()
        {
            if (backButton  != null) backButton.onClick.AddListener(OnBackClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (backButton  != null) backButton.onClick.RemoveListener(OnBackClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        // ──────────────────────────────────────────────────────────────────────────
        public void Populate(ProjectData data)
        {
            if (data == null) return;

            if (thumbnailImage != null && data.Thumbnail != null)
                thumbnailImage.sprite = data.Thumbnail;

            SetText(projectNameText,  data.ProjectName);
            SetText(descriptionText,  data.Description);
            SetText(technologiesText, data.Technologies != null
                ? "🛠 " + string.Join("  •  ", data.Technologies)
                : string.Empty);
            SetText(repoUrlText,  string.IsNullOrEmpty(data.RepositoryUrl) ? "" : $"🔗 {data.RepositoryUrl}");
            SetText(liveUrlText,  string.IsNullOrEmpty(data.LiveUrl)       ? "" : $"🌐 {data.LiveUrl}");
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label == null) return;
            label.text = value ?? string.Empty;
        }

        private void OnBackClicked()  => OnBack?.Invoke();

        private void OnCloseClicked()
        {
            var mgr = uiManager != null ? uiManager : Core.UIManager.Instance;
            if (mgr != null)
                mgr.ClosePortfolioPanel();
            else
                gameObject.SetActive(false);
        }
    }
}
