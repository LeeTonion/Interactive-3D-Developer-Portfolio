using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Skills panel - displays categorized skill list with proficiency bars.
    /// Uses SkillData ScriptableObject and SkillItemUI prefab.
    /// Supports UITheme for consistent styling.
    /// </summary>
    public class SkillsPanelUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SkillData skillData;
        [SerializeField] private UITheme uiTheme;

        [Header("UI References")]
        [SerializeField] private Transform skillContentParent;     // ScrollView > Viewport > Content
        [SerializeField] private GameObject skillItemPrefab;       // Prefab with SkillItemUI
        [SerializeField] private GameObject categoryHeaderPrefab;  // Optional: category header
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Layout")]
        [SerializeField] private float categorySpacing = 24f;
        [SerializeField] private float itemSpacing = 8f;

        [Header("Close")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Core.UIManager uiManager;

        private readonly List<GameObject> _spawnedItems = new List<GameObject>();

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            // Apply theme if assigned
            if (uiTheme != null && titleText != null)
                uiTheme.ApplyToText(titleText, UITheme.TextStyle.Heading);
            if (uiTheme != null && subtitleText != null)
                uiTheme.ApplyToText(subtitleText, UITheme.TextStyle.Subheading);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnEnable()
        {
            BuildSkillsList();
        }

        private void OnDisable()
        {
            ClearItems();
        }

        private void BuildSkillsList()
        {
            ClearItems();

            if (titleText != null) titleText.text = "KỸ NĂNG CHUYÊN MÔN";
            if (subtitleText != null) subtitleText.text = "Technical Skills & Proficiency";

            if (skillData == null || skillData.Skills == null || skillContentParent == null || skillItemPrefab == null)
                return;

            // Group skills by category
            var categories = new Dictionary<string, List<SkillEntry>>();
            foreach (var entry in skillData.Skills)
            {
                string cat = string.IsNullOrEmpty(entry.category) ? "General" : entry.category;
                if (!categories.ContainsKey(cat))
                    categories[cat] = new List<SkillEntry>();
                categories[cat].Add(entry);
            }

            // Sort categories alphabetically
            var sortedCategories = new List<string>(categories.Keys);
            sortedCategories.Sort();

            foreach (var category in sortedCategories)
            {
                // Category header
                if (categoryHeaderPrefab != null)
                {
                    var headerGO = Instantiate(categoryHeaderPrefab, skillContentParent);
                    var headerText = headerGO.GetComponentInChildren<TMP_Text>();
                    if (headerText != null)
                    {
                        headerText.text = category.ToUpper();
                        if (uiTheme != null)
                            uiTheme.ApplyToText(headerText, UITheme.TextStyle.Subheading);
                    }
                    _spawnedItems.Add(headerGO);
                }

                // Skill items
                var entries = categories[category];
                entries.Sort((a, b) => b.proficiencyPercent.CompareTo(a.proficiencyPercent)); // High to low

                foreach (var entry in entries)
                {
                    var itemGO = Instantiate(skillItemPrefab, skillContentParent);
                    var itemUI = itemGO.GetComponent<SkillItemUI>();
                    if (itemUI != null)
                        itemUI.Populate(entry);
                    _spawnedItems.Add(itemGO);
                }

                // Add spacing between categories (empty GameObject with LayoutElement)
                if (category != sortedCategories[sortedCategories.Count - 1])
                {
                    var spacer = new GameObject("CategorySpacer");
                    spacer.transform.SetParent(skillContentParent, false);
                    var le = spacer.AddComponent<UnityEngine.UI.LayoutElement>();
                    le.minHeight = categorySpacing;
                    _spawnedItems.Add(spacer);
                }
            }
        }

        private void ClearItems()
        {
            foreach (var go in _spawnedItems)
            {
                if (go != null) Destroy(go);
            }
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

        // Public method to refresh from external (e.g., after data change)
        public void Refresh()
        {
            if (gameObject.activeInHierarchy)
                BuildSkillsList();
        }
    }
}
