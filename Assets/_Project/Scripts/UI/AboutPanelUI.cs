using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Displays the About Me / Profile section using a ProfileData ScriptableObject.
    /// Supports UITheme for consistent styling.
    /// </summary>
    public class AboutPanelUI : MonoBehaviour
    {
        [Header("Profile Data")]
        [SerializeField] private ProfileData profileData;
        [SerializeField] private UITheme uiTheme;

        [Header("UI References")]
        [SerializeField] private Image           avatarImage;
        [SerializeField] private TextMeshProUGUI fullNameText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bioText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private TextMeshProUGUI emailText;
        [SerializeField] private TextMeshProUGUI githubText;
        [SerializeField] private TextMeshProUGUI linkedinText;

        [Header("Close")]
        [SerializeField] private Button          closeButton;
        [SerializeField] private Core.UIManager  uiManager;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            // Apply theme
            if (uiTheme != null)
            {
                if (fullNameText != null) uiTheme.ApplyToText(fullNameText, UITheme.TextStyle.Heading);
                if (titleText != null) uiTheme.ApplyToText(titleText, UITheme.TextStyle.Subheading);
                if (bioText != null) uiTheme.ApplyToText(bioText, UITheme.TextStyle.Body);
                if (locationText != null) uiTheme.ApplyToText(locationText, UITheme.TextStyle.Caption);
                if (emailText != null) uiTheme.ApplyToText(emailText, UITheme.TextStyle.Caption);
                if (githubText != null) uiTheme.ApplyToText(githubText, UITheme.TextStyle.Caption);
                if (linkedinText != null) uiTheme.ApplyToText(linkedinText, UITheme.TextStyle.Caption);

                // Style close button
                if (closeButton != null)
                    uiTheme.ApplyToButton(closeButton, closeButton.GetComponentInChildren<TMP_Text>());
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnEnable()
        {
            Populate(profileData);
        }

        public void Populate(ProfileData data)
        {
            if (data == null) return;

            if (avatarImage != null && data.ProfilePhoto != null)
                avatarImage.sprite = data.ProfilePhoto;

            SetText(fullNameText, data.FullName);
            SetText(titleText,    data.Title);
            SetText(bioText,      data.Bio);
            SetText(locationText, string.IsNullOrEmpty(data.Location)    ? "" : $"📍 {data.Location}");
            SetText(emailText,    string.IsNullOrEmpty(data.Email)       ? "" : $"✉ {data.Email}");
            SetText(githubText,   string.IsNullOrEmpty(data.GithubUrl)   ? "" : $"GitHub: {data.GithubUrl}");
            SetText(linkedinText, string.IsNullOrEmpty(data.LinkedinUrl) ? "" : $"LinkedIn: {data.LinkedinUrl}");
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label == null) return;
            label.text = value ?? string.Empty;
        }

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
