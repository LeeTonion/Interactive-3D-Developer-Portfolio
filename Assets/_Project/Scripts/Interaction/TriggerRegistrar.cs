using UnityEngine;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Placed on the _Systems/GameManager object (or any persistent object).
    /// Finds all InteractionTriggers in the scene at Start and registers them
    /// with the InteractionManager so no manual wiring is needed per area.
    /// </summary>
    public class TriggerRegistrar : MonoBehaviour
    {
        [SerializeField] private InteractionManager interactionManager;

        private void Start()
        {
            if (interactionManager == null)
            {
                Debug.LogWarning("[TriggerRegistrar] No InteractionManager assigned.");
                return;
            }

            var triggers = FindObjectsByType<InteractionTrigger>(FindObjectsSortMode.None);
            foreach (var t in triggers)
                interactionManager.RegisterTrigger(t);

            Debug.Log($"[TriggerRegistrar] Registered {triggers.Length} interaction trigger(s).");
        }
    }
}
