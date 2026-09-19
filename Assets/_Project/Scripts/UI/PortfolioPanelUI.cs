using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Placeholder portfolio panel. Displays the area type and a close button.
    /// Wire up to UIManager via inspector.
    /// </summary>
    public class PortfolioPanelUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button closeButton;

        [SerializeField] private Core.UIManager uiManager;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        public void Populate(PortfolioAreaType areaType, string label)
        {
            if (titleText != null)
                titleText.text = label;

            if (bodyText != null)
                bodyText.text = $"[{areaType}]\n\nContent coming soon.";
        }

        private void OnCloseClicked()
        {
            uiManager?.ClosePortfolioPanel();
        }
    }
}
