using UnityEngine;

namespace CodeDrive.Portfolio
{
    [CreateAssetMenu(fileName = "ProjectData", menuName = "CodeDrive/Portfolio/Project Data")]
    public class ProjectData : ScriptableObject
    {
        [SerializeField] private string projectName;
        [SerializeField] private string description;
        [SerializeField] private string[] technologies;
        [SerializeField] private string repositoryUrl;
        [SerializeField] private string liveUrl;
        [SerializeField] private Sprite thumbnail;

        public string   ProjectName   => projectName;
        public string   Description   => description;
        public string[] Technologies  => technologies;
        public string   RepositoryUrl => repositoryUrl;
        public string   LiveUrl       => liveUrl;
        public Sprite   Thumbnail     => thumbnail;
    }
}
