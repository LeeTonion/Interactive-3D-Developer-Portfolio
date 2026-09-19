using UnityEngine;

namespace CodeDrive.Portfolio
{
    [System.Serializable]
    public struct SkillEntry
    {
        public string skillName;
        [Range(0, 100)] public int proficiencyPercent;
        public string category;
    }

    [CreateAssetMenu(fileName = "SkillData", menuName = "CodeDrive/Portfolio/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [SerializeField] private SkillEntry[] skills;

        public SkillEntry[] Skills => skills;
    }
}
