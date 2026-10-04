using UnityEngine;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Generic timeline card used for both Education and Experience entries.
    /// Assign to a prefab placed inside a Scroll View content transform.
    /// </summary>
    public class EduExpItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI headerText;      // degree / role
        [SerializeField] private TextMeshProUGUI subHeaderText;   // institution / company
        [SerializeField] private TextMeshProUGUI periodText;      // years / dates
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI tagText;         // fieldOfStudy or "Hiện tại"

        public void PopulateEducation(EducationEntry entry)
        {
            SetText(headerText,      entry.degree);
            SetText(subHeaderText,   entry.institution);
            string period = string.IsNullOrEmpty(entry.endYear)
                ? $"{entry.startYear} – Hiện tại"
                : $"{entry.startYear} – {entry.endYear}";
            SetText(periodText,      period);
            SetText(descriptionText, entry.description);
            SetText(tagText,         entry.fieldOfStudy);
        }

        public void PopulateExperience(ExperienceEntry entry)
        {
            SetText(headerText,      entry.role);
            SetText(subHeaderText,   entry.company);
            string period = entry.isCurrent
                ? $"{entry.startDate} – Hiện tại"
                : $"{entry.startDate} – {entry.endDate}";
            SetText(periodText,      period);
            SetText(descriptionText, entry.description);
            SetText(tagText,         entry.isCurrent ? "● Hiện tại" : "");
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label == null) return;
            label.text = value ?? string.Empty;
        }
    }
}
