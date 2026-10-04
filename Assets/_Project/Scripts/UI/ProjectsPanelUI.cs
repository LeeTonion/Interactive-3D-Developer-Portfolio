using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Master panel for the Projects area.
    /// Shows a scrollable grid of ProjectCardUI items.
    /// When a card is clicked, hides the list and shows ProjectDetailPanelUI.
    /// </summary>
    public class ProjectsPanelUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ProjectData[] projects;

        [Header("List View")]
        [SerializeField] private Transform       cardContentParent;  // Scroll View > Viewport > Content
        [SerializeField] private GameObject      cardPrefab;         // Prefab with ProjectCardUI
        [SerializeField] private GameObject      listView;
        [SerializeField] private TextMeshProUGUI listTitleText;

        [Header("Detail View")]
        [SerializeField] private ProjectDetailPanelUI detailPanel;
        [SerializeField] private GameObject           detailView;

        [Header("Close")]
        [SerializeField] private Button         closeButton;
        [SerializeField] private Core.UIManager uiManager;

        private readonly List<GameObject> _spawnedCards = new List<GameObject>();

        // ──────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            if (detailPanel != null)
                detailPanel.OnBack += ShowListView;
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);

            if (detailPanel != null)
                detailPanel.OnBack -= ShowListView;
        }

        private void OnEnable()
        {
            BuildCards();
            ShowListView();
        }

        // ──────────────────────────────────────────────────────────────────────────
        private void BuildCards()
        {
            ClearCards();

            if (listTitleText != null) listTitleText.text = "Các Dự Án Nổi Bật";
            if (projects == null || cardContentParent == null || cardPrefab == null) return;

            foreach (var data in projects)
            {
                if (data == null) continue;
                var go   = Instantiate(cardPrefab, cardContentParent);
                var card = go.GetComponent<ProjectCardUI>();
                if (card != null)
                {
                    card.Populate(data);
                    card.OnSelected += OpenDetail;
                }
                _spawnedCards.Add(go);
            }
        }

        private void ClearCards()
        {
            foreach (var go in _spawnedCards)
            {
                if (go == null) continue;
                var card = go.GetComponent<ProjectCardUI>();
                if (card != null) card.OnSelected -= OpenDetail;
                Destroy(go);
            }
            _spawnedCards.Clear();
        }

        // ──────────────────────────────────────────────────────────────────────────
        private void OpenDetail(ProjectData data)
        {
            if (detailPanel != null)
                detailPanel.Populate(data);

            SetActive(listView,   false);
            SetActive(detailView, true);
        }

        private void ShowListView()
        {
            SetActive(listView,   true);
            SetActive(detailView, false);
        }

        private void OnCloseClicked()
        {
            var mgr = uiManager != null ? uiManager : Core.UIManager.Instance;
            if (mgr != null)
                mgr.ClosePortfolioPanel();
            else
                gameObject.SetActive(false);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
