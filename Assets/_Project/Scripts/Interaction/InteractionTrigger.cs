using System;
using UnityEngine;
using CodeDrive.Portfolio;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Marks a zone as a portfolio interaction area. Fires events and notifies AreaManager & InteractionManager
    /// when the player vehicle enters/exits this trigger zone.
    /// Requires a Collider set as Trigger on this GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionTrigger : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private PortfolioAreaType areaType;
        [SerializeField] private string displayName;
        [SerializeField] private Color zoneGizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);

        private PortfolioArea _portfolioArea;
        private Collider _collider;

        public PortfolioAreaType AreaType => areaType;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? areaType.ToString() : displayName;
        public PortfolioArea AssociatedArea => _portfolioArea != null ? _portfolioArea : (_portfolioArea = GetComponent<PortfolioArea>());

        public event Action<InteractionTrigger> OnPlayerEntered;
        public event Action<InteractionTrigger> OnPlayerExited;

        private void Reset()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.isTrigger = true;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.isTrigger = true;

            _portfolioArea = GetComponent<PortfolioArea>();
        }

        private void OnEnable()
        {
            InteractionManager.Instance?.RegisterTrigger(this);
        }

        private void OnDisable()
        {
            InteractionManager.Instance?.UnregisterTrigger(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayerVehicle(other)) return;

            if (_portfolioArea == null)
                _portfolioArea = GetComponent<PortfolioArea>();

            OnPlayerEntered?.Invoke(this);
            AreaManager.Instance?.NotifyPlayerEntered(_portfolioArea, this);
            InteractionManager.Instance?.HandlePlayerEntered(this);

            var booth = GetComponentInChildren<ShowcaseBooth3D>();
            if (booth != null) booth.OnZoneEntered();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayerVehicle(other)) return;

            if (_portfolioArea == null)
                _portfolioArea = GetComponent<PortfolioArea>();

            OnPlayerExited?.Invoke(this);
            AreaManager.Instance?.NotifyPlayerExited(_portfolioArea, this);
            InteractionManager.Instance?.HandlePlayerExited(this);

            var booth = GetComponentInChildren<ShowcaseBooth3D>();
            if (booth != null) booth.OnZoneExited();
        }

        /// <summary>
        /// Robust check whether the collider belongs to the Player vehicle.
        /// Handles wheel colliders, body colliders, and root Rigidbody.
        /// </summary>
        private bool IsPlayerVehicle(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) return true;
            if (other.transform.root != null && other.transform.root.CompareTag("Player")) return true;
            return false;
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = zoneGizmoColor;
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"[Area Trigger: {DisplayName}]");
#endif
        }
    }
}
