using UnityEngine;

namespace CodeDrive.Core
{
    /// <summary>
    /// Manages UI panels and overlay visibility.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private GameObject portfolioPanel;
        [SerializeField] private GameObject interactionPrompt;

        private void Start()
        {
            HideAll();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPauseToggled += HandlePauseToggled;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPauseToggled -= HandlePauseToggled;
            }
        }

        private void HideAll()
        {
            SetActive(pauseOverlay, false);
            SetActive(portfolioPanel, false);
            SetActive(interactionPrompt, false);
        }

        private void HandlePauseToggled()
        {
            if (GameManager.Instance == null) return;
            SetActive(pauseOverlay, GameManager.Instance.IsPaused);

            // Hide portfolio panel when pausing
            if (GameManager.Instance.IsPaused)
                SetActive(portfolioPanel, false);
        }

        public void ShowInteractionPrompt(bool show)
        {
            SetActive(interactionPrompt, show);
        }

        public void OpenPortfolioPanel()
        {
            SetActive(portfolioPanel, true);
            GameManager.Instance?.SetPaused(true);
        }

        public void ClosePortfolioPanel()
        {
            SetActive(portfolioPanel, false);
            GameManager.Instance?.SetPaused(false);
        }

        public bool IsPortfolioPanelOpen()
        {
            return portfolioPanel != null && portfolioPanel.activeSelf;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
