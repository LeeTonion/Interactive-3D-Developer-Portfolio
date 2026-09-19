using UnityEngine;
using CodeDrive.Portfolio;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Marks a zone as a portfolio area. Fires events when the player enters/exits.
    /// Requires a Collider set as Trigger on this GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionTrigger : MonoBehaviour
    {
        [SerializeField] private PortfolioAreaType areaType;
        [SerializeField] private string displayName;

        public PortfolioAreaType AreaType => areaType;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? areaType.ToString() : displayName;

        public System.Action<InteractionTrigger> OnPlayerEntered;
        public System.Action<InteractionTrigger> OnPlayerExited;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            OnPlayerEntered?.Invoke(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            OnPlayerExited?.Invoke(this);
        }
    }
}
