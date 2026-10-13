using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Contact panel with clickable action buttons:
    /// - Email: opens default mail client
    /// - GitHub: opens GitHub profile
    /// - LinkedIn: opens LinkedIn profile
    /// - Download CV: downloads CV file
    /// All URLs configurable via ProfileData ScriptableObject.
    /// </summary>
    public class ContactPanelUI : MonoBehaviour
    {
        [Header("Profile Data")]
        [SerializeField] private ProfileData profileData;
        [SerializeField] private UITheme uiTheme;

        [Header("UI References - Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("UI References - Buttons")]
        [SerializeField] private Button emailButton;
        [SerializeField] private Button githubButton;
        [SerializeField] private Button linkedinButton;
        [SerializeField] private Button downloadCvButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private TextMeshProUGUI emailButtonText;
        [SerializeField] private TextMeshProUGUI githubButtonText;
        [SerializeField] private TextMeshProUGUI linkedinButtonText;
        [SerializeField] private TextMeshProUGUI downloadCvButtonText;

        [Header("Button Icons (optional)")]
        [SerializeField] private Image emailIcon;
        [SerializeField] private Image githubIcon;
        [SerializeField] private Image linkedinIcon;
        [SerializeField] private Image cvIcon;

        [Header("Feedback")]
        [SerializeField] private float buttonClickFeedbackDuration = 0.15f;

        private void Awake()
        {
            // Button listeners
            if (emailButton != null) emailButton.onClick.AddListener(OnEmailClicked);
            if (githubButton != null) githubButton.onClick.AddListener(OnGitHubClicked);
            if (linkedinButton != null) linkedinButton.onClick.AddListener(OnLinkedInClicked);
            if (downloadCvButton != null) downloadCvButton.onClick.AddListener(OnDownloadCvClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            // Apply theme
            if (uiTheme != null)
            {
                if (titleText != null) uiTheme.ApplyToText(titleText, UITheme.TextStyle.Heading);
                if (subtitleText != null) uiTheme.ApplyToText(subtitleText, UITheme.TextStyle.Subheading);

                ApplyButtonTheme(emailButton, emailButtonText);
                ApplyButtonTheme(githubButton, githubButtonText);
                ApplyButtonTheme(linkedinButton, linkedinButtonText);
                ApplyButtonTheme(downloadCvButton, downloadCvButtonText);

                if (closeButton != null)
                    uiTheme.ApplyToButton(closeButton, closeButton.GetComponentInChildren<TMP_Text>());
            }

            // Set default button texts
            SetDefaultButtonTexts();
        }

        private void OnDestroy()
        {
            if (emailButton != null) emailButton.onClick.RemoveListener(OnEmailClicked);
            if (githubButton != null) githubButton.onClick.RemoveListener(OnGitHubClicked);
            if (linkedinButton != null) linkedinButton.onClick.RemoveListener(OnLinkedInClicked);
            if (downloadCvButton != null) downloadCvButton.onClick.RemoveListener(OnDownloadCvClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnEnable()
        {
            Populate(profileData);
        }

        public void Populate(ProfileData data)
        {
            if (data == null) return;

            // Update button interactability based on whether URLs are configured
            if (emailButton != null) emailButton.interactable = !string.IsNullOrEmpty(data.Email);
            if (githubButton != null) githubButton.interactable = !string.IsNullOrEmpty(data.GithubUrl);
            if (linkedinButton != null) linkedinButton.interactable = !string.IsNullOrEmpty(data.LinkedinUrl);
            if (downloadCvButton != null) downloadCvButton.interactable = !string.IsNullOrEmpty(data.CvUrl);

            // Update button texts with actual values
            if (emailButtonText != null && !string.IsNullOrEmpty(data.Email))
                emailButtonText.text = $"✉  {data.Email}";
            if (githubButtonText != null && !string.IsNullOrEmpty(data.GithubUrl))
                githubButtonText.text = $"🐙  {data.GithubUrl}";
            if (linkedinButtonText != null && !string.IsNullOrEmpty(data.LinkedinUrl))
                linkedinButtonText.text = $"💼  {data.LinkedinUrl}";
            if (downloadCvButtonText != null && !string.IsNullOrEmpty(data.CvUrl))
                downloadCvButtonText.text = $"⬇  Download CV";

            // Disable buttons visually if no URL
            SetButtonVisualState(emailButton, !string.IsNullOrEmpty(data.Email));
            SetButtonVisualState(githubButton, !string.IsNullOrEmpty(data.GithubUrl));
            SetButtonVisualState(linkedinButton, !string.IsNullOrEmpty(data.LinkedinUrl));
            SetButtonVisualState(downloadCvButton, !string.IsNullOrEmpty(data.CvUrl));
        }

        private void SetDefaultButtonTexts()
        {
            if (emailButtonText != null) emailButtonText.text = "✉  Email";
            if (githubButtonText != null) githubButtonText.text = "🐙  GitHub";
            if (linkedinButtonText != null) linkedinButtonText.text = "💼  LinkedIn";
            if (downloadCvButtonText != null) downloadCvButtonText.text = "⬇  Download CV";
        }

        private void ApplyButtonTheme(Button btn, TextMeshProUGUI btnText)
        {
            if (btn != null && uiTheme != null)
            {
                var colors = btn.colors;
                colors.normalColor = uiTheme.Primary;
                colors.highlightedColor = uiTheme.PrimaryLight;
                colors.pressedColor = uiTheme.PrimaryDark;
                colors.selectedColor = uiTheme.Primary;
                colors.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);
                colors.fadeDuration = uiTheme.ButtonTransitionDuration;
                btn.colors = colors;
            }
            if (btnText != null && uiTheme != null)
            {
                btnText.font = uiTheme.BodyFont;
                btnText.fontSize = uiTheme.ButtonFontSize;
                btnText.color = Color.white;
                btnText.fontStyle = FontStyles.Bold;
            }
        }

        private void SetButtonVisualState(Button btn, bool enabled)
        {
            if (btn == null) return;
            btn.interactable = enabled;
            if (!enabled && uiTheme != null)
            {
                var colors = btn.colors;
                colors.normalColor = uiTheme.TextMuted;
                colors.highlightedColor = uiTheme.TextMuted;
                colors.pressedColor = uiTheme.TextMuted;
                btn.colors = colors;
            }
        }

        // ─── Button Actions ───

        private void OnEmailClicked()
        {
            if (profileData == null || string.IsNullOrEmpty(profileData.Email)) return;
            string mailto = $"mailto:{profileData.Email}?subject=Portfolio%20Contact%20from%20Website";
            Application.OpenURL(mailto);
            PlayClickFeedback(emailButton);
        }

        private void OnGitHubClicked()
        {
            if (profileData == null || string.IsNullOrEmpty(profileData.GithubUrl)) return;
            string url = EnsureHttps(profileData.GithubUrl);
            Application.OpenURL(url);
            PlayClickFeedback(githubButton);
        }

        private void OnLinkedInClicked()
        {
            if (profileData == null || string.IsNullOrEmpty(profileData.LinkedinUrl)) return;
            string url = EnsureHttps(profileData.LinkedinUrl);
            Application.OpenURL(url);
            PlayClickFeedback(linkedinButton);
        }

        private void OnDownloadCvClicked()
        {
            if (profileData == null || string.IsNullOrEmpty(profileData.CvUrl)) return;
            string url = EnsureHttps(profileData.CvUrl);
            Application.OpenURL(url); // Browser will handle download
            PlayClickFeedback(downloadCvButton);
        }

        private void OnCloseClicked()
        {
            var mgr = Core.UIManager.Instance;
            if (mgr != null)
                mgr.ClosePortfolioPanel();
            else
                gameObject.SetActive(false);
        }

        private string EnsureHttps(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                return "https://" + url;
            return url;
        }

        private void PlayClickFeedback(Button btn)
        {
            if (btn == null) return;
            StartCoroutine(ButtonClickFeedbackRoutine(btn));
        }

        private System.Collections.IEnumerator ButtonClickFeedbackRoutine(Button btn)
        {
            var colors = btn.colors;
            Color originalNormal = colors.normalColor;
            colors.normalColor = colors.pressedColor;
            btn.colors = colors;

            yield return new WaitForSecondsRealtime(buttonClickFeedbackDuration);

            colors.normalColor = originalNormal;
            btn.colors = colors;
        }
    }
}