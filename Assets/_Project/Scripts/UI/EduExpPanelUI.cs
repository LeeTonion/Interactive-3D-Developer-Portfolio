using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Combined Education + Experience panel with two tabs.
    /// Uses a single EduExpItemUI prefab for both lists.
    /// </summary>
    public class EduExpPanelUI : MonoBehaviour
    {
        // ── Data ─────────────────────────────────────────────────────────────────
        [Header("Data")]
        [SerializeField] private EducationData  educationData;
        [SerializeField] private ExperienceData experienceData;

        // ── Tabs ─────────────────────────────────────────────────────────────────
        [Header("Tabs")]
        [SerializeField] private Button tabEducation;
        [SerializeField] private Button tabExperience;
        [SerializeField] private Color  tabActiveColor   = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color  tabInactiveColor = new Color(0.25f, 0.25f, 0.3f);

        // ── Content parents (each inside its own scroll view) ─────────────────────
        [Header("Content Parents")]
        [SerializeField] private Transform educationContent;
        [SerializeField] private Transform experienceContent;
        [SerializeField] private GameObject educationScrollView;
        [SerializeField] private GameObject experienceScrollView;

        [Header("Shared Prefab")]
        [SerializeField] private GameObject eduExpItemPrefab; // Prefab with EduExpItemUI

        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Close")]
        [SerializeField] private Button         closeButton;
        [SerializeField] private Core.UIManager uiManager;

        private readonly List<GameObject> _spawnedEdu  = new List<GameObject>();
        private readonly List<GameObject> _spawnedExp  = new List<GameObject>();
        private bool _listsBuilt;

        // ──────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (closeButton   != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (tabEducation  != null) tabEducation.onClick.AddListener(ShowEducation);
            if (tabExperience != null) tabExperience.onClick.AddListener(ShowExperience);
        }

        private void OnDestroy()
        {
            if (closeButton   != null) closeButton.onClick.RemoveListener(OnCloseClicked);
            if (tabEducation  != null) tabEducation.onClick.RemoveListener(ShowEducation);
            if (tabExperience != null) tabExperience.onClick.RemoveListener(ShowExperience);
        }

        private void OnEnable()
        {
            if (!_listsBuilt) BuildLists();
            ShowEducation(); // default tab
        }

        // ──────────────────────────────────────────────────────────────────────────
        private void BuildLists()
        {
            BuildEducation();
            BuildExperience();
            _listsBuilt = true;
        }

        private void BuildEducation()
        {
            ClearList(_spawnedEdu);
            if (educationData == null || educationContent == null || eduExpItemPrefab == null) return;

            foreach (var entry in educationData.Entries)
            {
                var go   = Instantiate(eduExpItemPrefab, educationContent);
                var item = go.GetComponent<EduExpItemUI>();
                item?.PopulateEducation(entry);
                _spawnedEdu.Add(go);
            }
        }

        private void BuildExperience()
        {
            ClearList(_spawnedExp);
            if (experienceData == null || experienceContent == null || eduExpItemPrefab == null) return;

            foreach (var entry in experienceData.Entries)
            {
                var go   = Instantiate(eduExpItemPrefab, experienceContent);
                var item = go.GetComponent<EduExpItemUI>();
                item?.PopulateExperience(entry);
                _spawnedExp.Add(go);
            }
        }

        private static void ClearList(List<GameObject> list)
        {
            foreach (var go in list) if (go != null) Destroy(go);
            list.Clear();
        }

        // ── Tab switching ─────────────────────────────────────────────────────────
        private void ShowEducation()
        {
            SetTabActive(tabEducation,  true);
            SetTabActive(tabExperience, false);
            SetActive(educationScrollView,  true);
            SetActive(experienceScrollView, false);
            if (titleText != null) titleText.text = "Học Vấn & Chứng Chỉ";
        }

        private void ShowExperience()
        {
            SetTabActive(tabEducation,  false);
            SetTabActive(tabExperience, true);
            SetActive(educationScrollView,  false);
            SetActive(experienceScrollView, true);
            if (titleText != null) titleText.text = "Kinh Nghiệm Làm Việc";
        }

        private void SetTabActive(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? tabActiveColor : tabInactiveColor;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
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
