using System.Collections.Generic;
using UnityEngine;

namespace CodeDrive.Navigation
{
    /// <summary>
    /// Represents a single waypoint node in the road network graph.
    /// </summary>
    public class RoadNode : MonoBehaviour
    {
        [Header("Graph Connections")]
        public List<RoadNode> neighbors = new List<RoadNode>();

        [Header("Editor Visualization")]
        [SerializeField] private Color gizmoColor = Color.green;

        /// <summary>
        /// Returns the center of the road mesh (driving lane center) rather than transform pivot.
        /// Falls back to transform.position if no renderer is found.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    Vector3 center = renderer.bounds.center;
                    center.y = transform.position.y; // Keep ground level Y
                    return center;
                }
                return transform.position;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, 1.2f);

            Gizmos.color = Color.yellow;
            if (neighbors != null)
            {
                foreach (var neighbor in neighbors)
                {
                    if (neighbor != null)
                    {
                        Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, neighbor.transform.position + Vector3.up * 0.2f);
                    }
                }
            }
        }
    }
}
