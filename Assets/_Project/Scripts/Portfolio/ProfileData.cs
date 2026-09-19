using UnityEngine;

namespace CodeDrive.Portfolio
{
    [CreateAssetMenu(fileName = "ProfileData", menuName = "CodeDrive/Portfolio/Profile Data")]
    public class ProfileData : ScriptableObject
    {
        [SerializeField] private string fullName;
        [SerializeField] private string title;
        [SerializeField] private string bio;
        [SerializeField] private string location;
        [SerializeField] private string email;
        [SerializeField] private string githubUrl;
        [SerializeField] private string linkedinUrl;
        [SerializeField] private Sprite profilePhoto;

        public string FullName    => fullName;
        public string Title       => title;
        public string Bio         => bio;
        public string Location    => location;
        public string Email       => email;
        public string GithubUrl   => githubUrl;
        public string LinkedinUrl => linkedinUrl;
        public Sprite ProfilePhoto => profilePhoto;
    }
}
