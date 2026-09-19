using UnityEngine;

namespace CodeDrive.Core
{
    /// <summary>
    /// Manages audio playback. Extend with clip references as needed.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioClip interactSound;
        [SerializeField] private AudioClip closePanelSound;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void PlayInteract()
        {
            PlayClip(interactSound);
        }

        public void PlayClosePanel()
        {
            PlayClip(closePanelSound);
        }

        public void PlayClip(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }
    }
}
