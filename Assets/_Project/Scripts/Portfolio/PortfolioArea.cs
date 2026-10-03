using UnityEngine;
using CodeDrive.Navigation;

namespace CodeDrive.Portfolio
{
    /// <summary>
    /// Represents a portfolio section in the world.
    /// Attach to a trigger GameObject alongside an InteractionTrigger.
    /// Self-registers with RoadNavigationManager to avoid Find calls at runtime.
    /// </summary>
    public class PortfolioArea : MonoBehaviour
    {
        [SerializeField] private PortfolioAreaType areaType;
        [SerializeField] private string areaLabel;
        [SerializeField] private Color gizmoColor = Color.cyan;

        public PortfolioAreaType AreaType => areaType;
        public string AreaLabel => string.IsNullOrEmpty(areaLabel) ? areaType.ToString() : areaLabel;

        private void OnEnable()
        {
            RoadNavigationManager.Instance?.RegisterArea(this);
            Interaction.AreaManager.Instance?.RegisterArea(this);
        }

        private void OnDisable()
        {
            RoadNavigationManager.Instance?.UnregisterArea(this);
            Interaction.AreaManager.Instance?.UnregisterArea(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, transform.localScale);

            // Label in editor
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, AreaLabel);
#endif
        }
    }
}
