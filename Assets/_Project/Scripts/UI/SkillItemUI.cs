using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// Represents a single skill row in the Skills panel.
    /// Assign to the SkillItem prefab that lives inside the scroll view content.
    /// </summary>
    public class SkillItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI percentText;
        [SerializeField] private Slider          progressSlider;
        [SerializeField] private Image           fillImage;

        // Optional: colour ramp low→high
        [SerializeField] private Color colorLow  = new Color(0.9f, 0.35f, 0.2f);
        [SerializeField] private Color colorHigh = new Color(0.2f, 0.85f, 0.5f);

        public void Populate(SkillEntry entry)
        {
            if (skillNameText != null) skillNameText.text = entry.skillName;
            if (categoryText  != null) categoryText.text  = entry.category;
            if (percentText   != null) percentText.text   = $"{entry.proficiencyPercent}%";

            float normalized = Mathf.Clamp01(entry.proficiencyPercent / 100f);

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value    = normalized;
            }

            if (fillImage != null)
                fillImage.color = Color.Lerp(colorLow, colorHigh, normalized);
        }
    }
}
