using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Lightweight closed-loop spline circuit for smooth train navigation.
    /// Supports distance evaluation, curvature tangent, and smooth cubic Hermite / Catmull-Rom interpolation.
    /// </summary>
    public class MiningRailwayCircuit : MonoBehaviour
    {
        [Header("Circuit Waypoints (Local to this Transform)")]
        [Tooltip("Key guide waypoints defining the continuous orbital railway around the asteroid.")]
        public List<Vector3> waypoints = new List<Vector3>();

        [Header("Sampling Resolution")]
        [Range(10, 50)]
        public int samplesPerSegment = 24;

        // Baked high-resolution samples for O(1) constant-speed evaluation
        private struct SplineSample
        {
            public Vector3 position;
            public Vector3 forward;
            public Vector3 up;
            public float distance;
        }

        private SplineSample[] _samples;
        private float _totalLength;

        public float TotalLength => _totalLength > 0.001f ? _totalLength : 1f;

        private void Awake()
        {
            BakeSpline();
        }

        private void OnValidate()
        {
            if (waypoints != null && waypoints.Count >= 3)
            {
                BakeSpline();
            }
        }

        public void BakeSpline()
        {
            if (waypoints == null || waypoints.Count < 3) return;

            int n = waypoints.Count;
            int totalSamples = n * samplesPerSegment;
            _samples = new SplineSample[totalSamples + 1];

            float accum = 0f;
            Vector3 prevPos = EvaluateCatmullRom(0, 0f);

            for (int seg = 0; seg < n; seg++)
            {
                for (int s = 0; s < samplesPerSegment; s++)
                {
                    int idx = seg * samplesPerSegment + s;
                    float t = (float)s / samplesPerSegment;
                    Vector3 pos = EvaluateCatmullRom(seg, t);
                    Vector3 nextPos = EvaluateCatmullRom(seg, (float)(s + 1) / samplesPerSegment);
                    Vector3 fwd = (nextPos - pos).normalized;
                    if (fwd == Vector3.zero) fwd = Vector3.forward;

                    if (idx > 0)
                    {
                        accum += Vector3.Distance(prevPos, pos);
                    }
                    prevPos = pos;

                    _samples[idx] = new SplineSample
                    {
                        position = transform.TransformPoint(pos),
                        forward = transform.TransformDirection(fwd),
                        up = Vector3.up,
                        distance = accum
                    };
                }
            }

            // Close the loop
            Vector3 finalPos = EvaluateCatmullRom(0, 0f);
            accum += Vector3.Distance(prevPos, finalPos);
            _samples[totalSamples] = new SplineSample
            {
                position = transform.TransformPoint(finalPos),
                forward = _samples[0].forward,
                up = Vector3.up,
                distance = accum
            };

            _totalLength = accum;
        }

        private Vector3 EvaluateCatmullRom(int seg, float t)
        {
            int n = waypoints.Count;
            Vector3 p0 = waypoints[(seg - 1 + n) % n];
            Vector3 p1 = waypoints[seg];
            Vector3 p2 = waypoints[(seg + 1) % n];
            Vector3 p3 = waypoints[(seg + 2) % n];

            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        /// <summary>
        /// Query position and tangent rotation at a specific distance along the closed-loop railway circuit.
        /// </summary>
        public void EvaluateDistance(float distance, out Vector3 worldPos, out Quaternion worldRot)
        {
            if (_samples == null || _samples.Length < 2 || _totalLength <= 0.001f)
            {
                BakeSpline();
            }

            if (_samples == null || _samples.Length < 2)
            {
                worldPos = transform.position;
                worldRot = transform.rotation;
                return;
            }

            // Loop distance smoothly
            distance = Mathf.Repeat(distance, _totalLength);

            // Binary search in sorted distance samples
            int low = 0;
            int high = _samples.Length - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (_samples[mid].distance < distance) low = mid + 1;
                else high = mid - 1;
            }

            int idxA = Mathf.Clamp(high, 0, _samples.Length - 2);
            int idxB = idxA + 1;

            float distA = _samples[idxA].distance;
            float distB = _samples[idxB].distance;
            float factor = (distB - distA) > 0.0001f ? (distance - distA) / (distB - distA) : 0f;
            factor = Mathf.Clamp01(factor);

            worldPos = Vector3.Lerp(_samples[idxA].position, _samples[idxB].position, factor);
            Vector3 fwd = Vector3.Slerp(_samples[idxA].forward, _samples[idxB].forward, factor);
            if (fwd == Vector3.zero) fwd = Vector3.forward;

            worldRot = Quaternion.LookRotation(fwd, Vector3.up);
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 3) return;

            Gizmos.color = new Color(0f, 0.85f, 1f, 0.75f);
            int n = waypoints.Count;
            for (int seg = 0; seg < n; seg++)
            {
                Vector3 prev = transform.TransformPoint(EvaluateCatmullRom(seg, 0f));
                for (int s = 1; s <= 12; s++)
                {
                    Vector3 pt = transform.TransformPoint(EvaluateCatmullRom(seg, (float)s / 12f));
                    Gizmos.DrawLine(prev, pt);
                    prev = pt;
                }
            }

            Gizmos.color = Color.yellow;
            foreach (var wp in waypoints)
            {
                Gizmos.DrawSphere(transform.TransformPoint(wp), 0.45f);
            }
        }
    }
}

