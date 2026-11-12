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
        private MiningStationDock _calledTargetStation = null;
        private float _stationTimer = 0f;
        private bool _isStopped = false;
        private int _lastVisitedStationIndex = -1;
        private float _overdriveSpeedMultiplier = 1f;
        private float _blockedByTrainUntil = -1f;
        private bool _isHeldByRedSignal = false;
        private float _cargoFillNormalized = 0f;
        private readonly List<Material> _cargoGlowMaterials = new List<Material>();
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public float CurrentDistance => _currentDistance;
        public float CurrentSpeed => _currentSpeed;
        public bool IsStoppedAtStation => _isStopped;
        public MiningStationDock DockedStation => _dockedStation;
        public MiningStationDock CalledTargetStation => _calledTargetStation;
        public bool IsHeldByRedSignal => _isHeldByRedSignal;

        public void SetOverdriveMultiplier(float multiplier)
        {
            _overdriveSpeedMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.2f);
        }

        public bool IsCalledToStation(MiningStationDock station)
        {
            return station != null && _calledTargetStation == station;
        }

        public void CallToStation(MiningStationDock station)
        {
            if (station == null) return;
            _calledTargetStation = station;
            if (_lastVisitedStationIndex >= 0 && stations != null && _lastVisitedStationIndex < stations.Count && stations[_lastVisitedStationIndex] == station)
            {
                _lastVisitedStationIndex = -1;
            }
        }

        public void SetSignalHold(bool hold)
        {
            _isHeldByRedSignal = hold;
        }

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
                bool blockedAhead = Time.time < _blockedByTrainUntil;
                if (_isHeldByRedSignal || blockedAhead)
                {
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, deceleration * 2.2f * dt);
                }
                else
                {
                    CheckStationProximity();
                    // Smooth acceleration to cruise speed (with Energy Core Overdrive / Call-Train boost if active)
                    float callBoost = _calledTargetStation != null ? 1.32f : 1f;
                    float targetCruise = movementSpeed * _overdriveSpeedMultiplier * callBoost;
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetCruise, acceleration * dt);
                }
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
                if (st == null || !st.CanAcceptTrain(this)) continue;

                float diff = (st.circuitDistance - Mathf.Repeat(_currentDistance, loopLen));
                if (diff < -loopLen * 0.5f) diff += loopLen;
                if (diff > loopLen * 0.5f) diff -= loopLen;

                // Approaching dock zone within stopping distance
                if (diff > 0f && diff < 3.8f)
                {
                    // Decelerate smoothly
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 1.2f, deceleration * Time.deltaTime);

                    if (diff < 0.48f)
                    {
                        Transform cargoTarget = cargoContainers.Count > 0 && cargoContainers[0] != null
                            ? cargoContainers[0].transform
                            : (cargoWagons.Count > 0 ? cargoWagons[0] : locomotiveHead);

                        if (!st.TryReserve(this, cargoTarget)) continue;

                        // Dock!
                        _isStopped = true;
                        _currentSpeed = 0f;
                        _dockedStation = st;
                        if (_calledTargetStation == st) _calledTargetStation = null;
                        _stationTimer = st.dockingDuration > 0.5f ? st.dockingDuration : stationStoppingDuration;
                        _lastVisitedStationIndex = i;
                        _cargoFillNormalized = 0f;

                        st.SetStationState(MiningStationDock.StationState.Loading);
                        SetCargoLoadedVisuals(true);
                        SetCargoGlowProgress(0.1f);
                        break;
                    }
                }
            }
        }

        private void HandleStationDocking(float dt)
        {
            float totalDur = (_dockedStation != null && _dockedStation.dockingDuration > 0.5f)
                ? _dockedStation.dockingDuration
                : stationStoppingDuration;
            _stationTimer -= dt;

            // Loading animation timing & gradual container illumination
            if (_dockedStation != null)
            {
                float clearWindow = 0.85f;
                float elapsed = Mathf.Max(0f, totalDur - _stationTimer);
                float loadDur = Mathf.Max(0.5f, totalDur - clearWindow);

                if (_stationTimer > clearWindow)
                {
                    _dockedStation.SetStationState(MiningStationDock.StationState.Loading);
                    _cargoFillNormalized = Mathf.Clamp01(elapsed / loadDur);
                    SetCargoGlowProgress(_cargoFillNormalized);
                }
                else
                {
                    // Loading complete -> Station indicator turns green, preparing to depart
                    _cargoFillNormalized = 1f;
                    SetCargoGlowProgress(1f);
                    _dockedStation.SetStationState(MiningStationDock.StationState.Clearing);
                }
            }

            if (_stationTimer <= 0f)
            {
                // Resume transit smoothly and release dock reservation
                _isStopped = false;
                if (_dockedStation != null)
                {
                    _dockedStation.SetStationState(MiningStationDock.StationState.Idle);
                    _dockedStation.ReleaseReservation(this);
                    _dockedStation = null;
                }
            }
        }

        private void SetCargoGlowProgress(float t)
        {
            EnsureCargoGlowInstances();
            Color baseCyan = new Color(0.08f, 0.92f, 1f, 1f);
            float intensity = Mathf.Lerp(0.12f, 2.35f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            for (int i = 0; i < _cargoGlowMaterials.Count; i++)
            {
                var mat = _cargoGlowMaterials[i];
                if (mat == null) continue;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(EmissionColorId, baseCyan * intensity);
            }
        }

        private void EnsureCargoGlowInstances()
        {
            if (_cargoGlowMaterials.Count > 0) return;
            foreach (var r in cargoContainers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
                    string n = mats[m].name.ToLower();
                    if (n.Contains("red") || n.Contains("gemstone") || n.Contains("load"))
                    {
                        var inst = new Material(mats[m]);
                        mats[m] = inst;
                        _cargoGlowMaterials.Add(inst);
                    }
                }
                r.sharedMaterials = mats;
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
                // Keep a short brake latch so collision avoidance holds even across script Update order
                _blockedByTrainUntil = Time.time + 0.08f;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, deceleration * 2.2f * Time.deltaTime);
            }
        }
    }
}
