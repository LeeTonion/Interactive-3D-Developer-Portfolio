using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Autonomous Surface Mining Train controller running along the MiningRailwayCircuit.
    /// Operates existing Kenney monorail train vehicles with accurate car articulation,
    /// smooth deceleration/acceleration at mining stations, and interactive cargo transport loops.
    /// </summary>
    public class OrbitalMiningTrain : MonoBehaviour
    {
        [Header("Train Identification")]
        public string trainID = "MiningTrain_01";

        [Header("Railway Reference")]
        public MiningRailwayCircuit railwayCircuit;

        [Header("Train Composition (Existing Prefabs/Models)")]
        [Tooltip("The locomotive/engine head.")]
        public Transform locomotiveHead;
        [Tooltip("List of articulated cargo wagons following the locomotive.")]
        public List<Transform> cargoWagons = new List<Transform>();
        [Tooltip("The caboose / end car of the train.")]
        public Transform cabooseCar;

        [Header("Articulated Wagon Spacing")]
        [Tooltip("Physical distance in meters between consecutive cars.")]
        public float wagonSpacing = 1.95f;

        [Header("Movement Dynamics")]
        [Tooltip("Target cruise speed along the railway circuit (m/s).")]
        public float movementSpeed = 7.5f;
        [Tooltip("Acceleration rate when leaving a station or starting up (m/s^2).")]
        public float acceleration = 3.2f;
        [Tooltip("Deceleration rate when approaching a station dock (m/s^2).")]
        public float deceleration = 4.0f;
        [Tooltip("Starting distance offset along the closed-loop railway circuit.")]
        public float startingOffset = 0f;

        [Header("Stations & Loading Transport Loop")]
        public List<MiningStationDock> stations = new List<MiningStationDock>();
        [Tooltip("Duration in seconds the train remains docked at a station.")]
        public float stationStoppingDuration = 4.5f;
        [Tooltip("Duration of the active gemstone loading phase.")]
        public float loadingAnimationDuration = 2.8f;

        [Header("Visual Effects")]
        public Light engineHeadlight;
        public Light tailWarningLight;
        public List<Renderer> cargoContainers = new List<Renderer>();
        public Material emptyCargoMat;
        public Material loadedGemstoneMat;

        // Runtime State
        private float _currentDistance;
        private float _currentSpeed = 0f;
        private MiningStationDock _dockedStation = null;
        private float _stationTimer = 0f;
        private bool _isStopped = false;
        private int _lastVisitedStationIndex = -1;

        public float CurrentDistance => _currentDistance;
        public float CurrentSpeed => _currentSpeed;

        private void Start()
        {
            _currentDistance = startingOffset;
            _currentSpeed = movementSpeed;

            // Align initial positions immediately
            UpdateTrainPositions(0f);
        }

        public void AlignImmediate()
        {
            _currentDistance = startingOffset;
            _currentSpeed = movementSpeed;
            UpdateTrainPositions(0f);
        }

        private void Update()
        {
            if (railwayCircuit == null) return;

            float dt = Time.deltaTime;

            if (_isStopped)
            {
                HandleStationDocking(dt);
            }
            else
            {
                CheckStationProximity();
                // Smooth acceleration to cruise speed
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, movementSpeed, acceleration * dt);
                _currentDistance += _currentSpeed * dt;
            }

            UpdateTrainPositions(dt);
        }

        private void CheckStationProximity()
        {
            if (stations == null || stations.Count == 0) return;

            float loopLen = railwayCircuit.TotalLength;

            for (int i = 0; i < stations.Count; i++)
            {
                if (i == _lastVisitedStationIndex) continue;

                var st = stations[i];
                if (st == null) continue;

                float diff = (st.circuitDistance - Mathf.Repeat(_currentDistance, loopLen));
                if (diff < -loopLen * 0.5f) diff += loopLen;
                if (diff > loopLen * 0.5f) diff -= loopLen;

                // Approaching dock zone within stopping distance
                if (diff > 0f && diff < 3.5f)
                {
                    // Decelerate smoothly
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 1.2f, deceleration * Time.deltaTime);

                    if (diff < 0.45f)
                    {
                        // Dock!
                        _isStopped = true;
                        _currentSpeed = 0f;
                        _dockedStation = st;
                        _stationTimer = stationStoppingDuration;
                        _lastVisitedStationIndex = i;

                        st.SetStationState(MiningStationDock.StationState.Loading);
                        SetCargoLoadedVisuals(true);
                        break;
                    }
                }
            }
        }

        private void HandleStationDocking(float dt)
        {
            _stationTimer -= dt;

            // Loading animation timing
            if (_dockedStation != null)
            {
                if (_stationTimer <= stationStoppingDuration - loadingAnimationDuration && _stationTimer > 0.8f)
                {
                    // In the middle of gemstone loading
                    _dockedStation.SetStationState(MiningStationDock.StationState.Loading);
                }
                else if (_stationTimer <= 0.8f)
                {
                    // Preparing to depart
                    _dockedStation.SetStationState(MiningStationDock.StationState.Clearing);
                }
            }

            if (_stationTimer <= 0f)
            {
                // Resume transit
                _isStopped = false;
                if (_dockedStation != null)
                {
                    _dockedStation.SetStationState(MiningStationDock.StationState.Idle);
                    _dockedStation = null;
                }
            }
        }

        private void UpdateTrainPositions(float dt)
        {
            if (railwayCircuit == null) return;

            // 1. Locomotive Head
            if (locomotiveHead != null)
            {
                railwayCircuit.EvaluateDistance(_currentDistance, out Vector3 headPos, out Quaternion headRot);
                locomotiveHead.position = headPos;
                locomotiveHead.rotation = headRot;
            }

            // 2. Cargo Wagons articulated behind locomotive
            float accumDist = wagonSpacing;
            for (int i = 0; i < cargoWagons.Count; i++)
            {
                var wagon = cargoWagons[i];
                if (wagon == null) continue;

                float wagonDist = _currentDistance - accumDist;
                railwayCircuit.EvaluateDistance(wagonDist, out Vector3 wPos, out Quaternion wRot);
                wagon.position = wPos;
                wagon.rotation = wRot;

                accumDist += wagonSpacing;
            }

            // 3. Caboose Car
            if (cabooseCar != null)
            {
                float cabooseDist = _currentDistance - accumDist;
                railwayCircuit.EvaluateDistance(cabooseDist, out Vector3 cPos, out Quaternion cRot);
                cabooseCar.position = cPos;
                cabooseCar.rotation = cRot;
            }

            // Subtle headlight flicker / glow
            if (engineHeadlight != null)
            {
                engineHeadlight.intensity = 2.4f + Mathf.Sin(Time.time * 6f) * 0.2f;
            }
        }

        public void SetCargoLoadedVisuals(bool loaded)
        {
            Material matToApply = loaded ? loadedGemstoneMat : emptyCargoMat;
            if (matToApply == null) return;

            foreach (var r in cargoContainers)
            {
                if (r != null)
                {
                    var mats = r.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        // Target cargo submesh
                        if (mats[m] != null && mats[m].name.ToLower().Contains("red") || mats[m].name.ToLower().Contains("gemstone") || mats[m].name.ToLower().Contains("load"))
                        {
                            mats[m] = matToApply;
                        }
                    }
                    r.sharedMaterials = mats;
                }
            }
        }

        /// <summary>
        /// Collision avoidance check against another train ahead on the railway circuit.
        /// </summary>
        public void AdjustSpeedForPrecedingTrain(float otherTrainDistance, float safeDistance)
        {
            if (railwayCircuit == null) return;

            float loopLen = railwayCircuit.TotalLength;
            float diff = (otherTrainDistance - _currentDistance);
            if (diff < 0f) diff += loopLen;

            if (diff < safeDistance)
            {
                // Slow down or stop to prevent overlap
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, deceleration * 1.5f * Time.deltaTime);
            }
        }
    }
}
