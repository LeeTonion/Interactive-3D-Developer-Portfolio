using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Central manager for the Orbital Surface Mining Railway System.
    /// Controls train fleet dispatching, distance separation/collision avoidance,
    /// and stations coordination on the Gemstone Mining Asteroid.
    /// </summary>
    public class MiningRailwaySystem : MonoBehaviour
    {
        [Header("Railway Circuit Reference")]
        public MiningRailwayCircuit circuit;

        [Header("Train Fleet Configuration")]
        [Tooltip("Active autonomous trains running on the railway.")]
        public List<OrbitalMiningTrain> activeTrains = new List<OrbitalMiningTrain>();

        [Header("Traffic & Safety Settings")]
        [Tooltip("Minimum safe distance in meters between consecutive trains.")]
        public float safeFollowDistance = 18.0f;

        [Header("Stations On Circuit")]
        public List<MiningStationDock> stations = new List<MiningStationDock>();

        private void Update()
        {
            if (activeTrains == null || activeTrains.Count < 2 || circuit == null) return;

            // Collision avoidance between trains on a single closed circuit
            for (int i = 0; i < activeTrains.Count; i++)
            {
                var currentTrain = activeTrains[i];
                if (currentTrain == null) continue;

                // Find the train directly ahead
                float minDist = float.MaxValue;
                OrbitalMiningTrain leadingTrain = null;

                float loopLen = circuit.TotalLength;

                for (int j = 0; j < activeTrains.Count; j++)
                {
                    if (i == j) continue;
                    var otherTrain = activeTrains[j];
                    if (otherTrain == null) continue;

                    float distAhead = otherTrain.CurrentDistance - currentTrain.CurrentDistance;
                    if (distAhead < 0f) distAhead += loopLen;

                    if (distAhead < minDist)
                    {
                        minDist = distAhead;
                        leadingTrain = otherTrain;
                    }
                }

                if (leadingTrain != null && minDist < safeFollowDistance)
                {
                    currentTrain.AdjustSpeedForPrecedingTrain(leadingTrain.CurrentDistance, safeFollowDistance);
                }
            }
        }
    }
}

