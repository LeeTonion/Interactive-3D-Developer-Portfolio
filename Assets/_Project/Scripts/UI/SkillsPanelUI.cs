using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Displays a scrollable list of skills built from SkillData ScriptableObject.
    /// Instantiates SkillItemUI prefab for each SkillEntry.
    /// </summary>
    public class SkillsPanelUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SkillData skillData;

        [Header("UI References")]
        [SerializeField] private Transform       contentParent;   // Scroll View > Viewport > Content
        [SerializeField] private GameObject      skillItemPrefab; // Prefab with SkillItemUI
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Close")]
        [SerializeField] private Button         closeButton;
        [SerializeField] private Core.UIManager uiManager;

        private readonly List<GameObject> _spawnedItems = new List<GameObject>();

        // ──────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnEnable()
        {
            Populate(skillData);
        }

        // ──────────────────────────────────────────────────────────────────────────
        public void Populate(SkillData data)
        {
            ClearItems();

            if (titleText != null)
                titleText.text = "Kỹ Năng Chuyên Môn";

            if (data == null || contentParent == null || skillItemPrefab == null) return;

            foreach (var entry in data.Skills)
            {
                var go   = Instantiate(skillItemPrefab, contentParent);
                var item = go.GetComponent<SkillItemUI>();
                if (item != null)
                    item.Populate(entry);

                _spawnedItems.Add(go);
            }
        }

        private void ClearItems()
        {
            foreach (var go in _spawnedItems)
                if (go != null) Destroy(go);
            _spawnedItems.Clear();
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
