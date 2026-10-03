using UnityEngine;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Placed on the _Systems/GameManager object (or any persistent manager object).
    /// Finds all InteractionTriggers in the scene at Start and registers them
    /// with the InteractionManager and AreaManager so zero manual wiring is needed.
    /// </summary>
    public class TriggerRegistrar : MonoBehaviour
    {
        [SerializeField] private InteractionManager interactionManager;

        private void Start()
        {
            if (interactionManager == null)
            {
                interactionManager = InteractionManager.Instance != null 
                    ? InteractionManager.Instance 
                    : UnityEngine.Object.FindObjectOfType<InteractionManager>();
            }

            var triggers = UnityEngine.Object.FindObjectsOfType<InteractionTrigger>();
            foreach (var t in triggers)
            {
                if (interactionManager != null)
                    interactionManager.RegisterTrigger(t);

                var area = t.GetComponent<Portfolio.PortfolioArea>();
                if (area != null && AreaManager.Instance != null)
                    AreaManager.Instance.RegisterArea(area);
            }

            Debug.Log($"[TriggerRegistrar] Auto-registered {triggers.Length} interaction trigger(s).");
        }
    }
}
