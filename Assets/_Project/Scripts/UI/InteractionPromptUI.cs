using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CodeDrive.UI
{
    /// <summary>
    /// Shows or hides the "Press E to interact" prompt.
    /// Attach to the InteractionPrompt UI GameObject.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private string defaultPrompt = "Press [E] to interact";

        private void Awake()
        {
            if (promptText != null)
                promptText.text = defaultPrompt;

            gameObject.SetActive(false);
        }

        public void SetPromptText(string text)
        {
            if (promptText != null)
                promptText.text = text;
        }
    }
}
