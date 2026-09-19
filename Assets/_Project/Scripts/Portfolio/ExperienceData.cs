using UnityEngine;

namespace CodeDrive.Portfolio
{
    [System.Serializable]
    public struct ExperienceEntry
    {
        public string company;
        public string role;
        public string startDate;
        public string endDate;
        public bool isCurrent;
        [TextArea(2, 5)]
        public string description;
    }

    [CreateAssetMenu(fileName = "ExperienceData", menuName = "CodeDrive/Portfolio/Experience Data")]
    public class ExperienceData : ScriptableObject
    {
        [SerializeField] private ExperienceEntry[] entries;

        public ExperienceEntry[] Entries => entries;
    }
}
