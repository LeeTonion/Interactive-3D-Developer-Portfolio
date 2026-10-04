using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using CodeDrive.Portfolio;

namespace CodeDrive.UI
{
    /// <summary>
    /// A single project card in the Projects list panel.
    /// Displays thumbnail, name, and tech tags.
    /// Fires OnSelected when the card button is clicked.
    /// </summary>
    public class ProjectCardUI : MonoBehaviour
    {
        [SerializeField] private Image           thumbnailImage;
        [SerializeField] private TextMeshProUGUI projectNameText;
        [SerializeField] private TextMeshProUGUI techTagsText;
        [SerializeField] private Button          selectButton;

        public event Action<ProjectData> OnSelected;

        private ProjectData _data;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(HandleClick);
        }

        public void Populate(ProjectData data)
        {
            _data = data;
            if (data == null) return;

            if (thumbnailImage   != null && data.Thumbnail != null)
                thumbnailImage.sprite = data.Thumbnail;

            if (projectNameText  != null)
                projectNameText.text  = data.ProjectName;

            if (techTagsText     != null)
                techTagsText.text     = data.Technologies != null
                    ? string.Join("  •  ", data.Technologies)
                    : string.Empty;
        }

        private void HandleClick()
        {
            if (_data != null)
                OnSelected?.Invoke(_data);
        }
    }
}
