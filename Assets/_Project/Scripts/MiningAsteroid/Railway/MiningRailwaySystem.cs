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

        [Header("Stations & Signal Control")]
        public List<MiningStationDock> stations = new List<MiningStationDock>();
        public TrainSignalController signalController;
        public Light[] railwayNavigationLights;

        private bool _lightsEnabled = true;

        public bool AreLightsEnabled => _lightsEnabled;

        public void SetNavigationLights(bool enabled)
        {
            _lightsEnabled = enabled;
            if (railwayNavigationLights != null)
            {
                for (int i = 0; i < railwayNavigationLights.Length; i++)
                {
                    if (railwayNavigationLights[i] == null) continue;
                    railwayNavigationLights[i].enabled = enabled;
                }
            }
        }

        private void Update()
        {
            if (activeTrains == null || circuit == null) return;

            float loopLen = circuit.TotalLength;

            // 1. Apply Red Signal Hold before controlled section
            if (signalController != null)
            {
                float stopDist = Mathf.Repeat(signalController.signalStopDistance, loopLen);
                bool isRed = signalController.IsSignalRed;

                for (int i = 0; i < activeTrains.Count; i++)
                {
                    var tr = activeTrains[i];
                    if (tr == null) continue;

                    if (isRed && !tr.IsStoppedAtStation)
                    {
                        float trainDist = Mathf.Repeat(tr.CurrentDistance, loopLen);
                        float toSignal = stopDist - trainDist;
                        if (toSignal < -loopLen * 0.5f) toSignal += loopLen;
                        if (toSignal > loopLen * 0.5f) toSignal -= loopLen;

                        bool shouldHold = toSignal >= -0.6f && toSignal <= 9.5f;
                        tr.SetSignalHold(shouldHold);
                    }
                    else
                    {
                        tr.SetSignalHold(false);
                    }
                }
            }

            if (activeTrains.Count < 2) return;

            // 2. Collision avoidance between trains on a single closed circuit
            for (int i = 0; i < activeTrains.Count; i++)
            {
                var currentTrain = activeTrains[i];
                if (currentTrain == null) continue;

                float minDist = float.MaxValue;
                OrbitalMiningTrain leadingTrain = null;

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

