using UnityEngine;

namespace CodeDrive.Portfolio
{
    [System.Serializable]
    public struct EducationEntry
    {
        public string institution;
        public string degree;
        public string fieldOfStudy;
        public string startYear;
        public string endYear;
        public string description;
    }

    [CreateAssetMenu(fileName = "EducationData", menuName = "CodeDrive/Portfolio/Education Data")]
    public class EducationData : ScriptableObject
    {
        [SerializeField] private EducationEntry[] entries;

        public EducationEntry[] Entries => entries;
    }
}
