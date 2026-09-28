using System;
using System.Collections.Generic;
using UnityEngine;
using CodeDrive.Portfolio;

namespace CodeDrive.Navigation
{
    /// <summary>
    /// Manages road-based GPS navigation to PortfolioAreas using a lightweight Node Graph (A*).
    /// Zero NavMesh CPU/Memory overhead!
    /// </summary>
    public class RoadNavigationManager : MonoBehaviour
    {
        public static RoadNavigationManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform playerVehicle;
        [SerializeField] private float recalculateDistanceThreshold = 2.0f;
        [SerializeField] private float arrivalDistanceThreshold = 8f;

        // Active State
        private PortfolioArea _currentTargetArea;
        private List<Vector3> _currentPathWaypoints = new List<Vector3>();
        private float _remainingDistance;
        private bool _hasActivePath;
        private Vector3 _lastCalculatedPlayerPos;

        // Registered targets (populated via self-registration)
        private Dictionary<PortfolioAreaType, PortfolioArea> _portfolioAreasMap = new Dictionary<PortfolioAreaType, PortfolioArea>();
        private List<PortfolioArea> _allPortfolioAreas = new List<PortfolioArea>();

        // Events
        public event Action<PortfolioArea> OnTargetChanged;
        public event Action<List<Vector3>> OnPathUpdated;
        public event Action OnPathCleared;
        public event Action<PortfolioArea> OnDestinationReached;
        public event Action OnAreasListUpdated;

        public PortfolioArea CurrentTargetArea => _currentTargetArea;
        public List<Vector3> CurrentPathWaypoints => _currentPathWaypoints;
        public float RemainingDistance => _remainingDistance;
        public bool HasActivePath => _hasActivePath;
        public IReadOnlyList<PortfolioArea> AllPortfolioAreas => _allPortfolioAreas;
        public Transform PlayerVehicle => playerVehicle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindPlayerIfNeeded();
            
            // Fallback: manually find all PortfolioArea in scene to guarantee registration
            // regardless of script execution order.
            var allAreas = FindObjectsOfType<PortfolioArea>();
            foreach (var area in allAreas)
            {
                RegisterArea(area);
            }
        }

        private void FindPlayerIfNeeded()
        {
            if (playerVehicle == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerVehicle = playerObj.transform;
            }
        }

        public void SetPlayerVehicle(Transform vehicle)
        {
            playerVehicle = vehicle;
        }

        public void RegisterArea(PortfolioArea area)
        {
            if (area == null) return;

            if (!_allPortfolioAreas.Contains(area))
            {
                _allPortfolioAreas.Add(area);
                _portfolioAreasMap[area.AreaType] = area;
                OnAreasListUpdated?.Invoke();
            }
        }

        public void UnregisterArea(PortfolioArea area)
        {
            if (area == null) return;

            if (_allPortfolioAreas.Contains(area))
            {
                _allPortfolioAreas.Remove(area);
                _portfolioAreasMap.Remove(area.AreaType);
                OnAreasListUpdated?.Invoke();
            }
        }

        public void SetTargetArea(PortfolioAreaType areaType)
        {
            if (_portfolioAreasMap.TryGetValue(areaType, out var area))
            {
                SetTargetArea(area);
            }
        }

        public void SetTargetArea(PortfolioArea targetArea)
        {
            if (_currentTargetArea == targetArea && _hasActivePath) return;

            _currentTargetArea = targetArea;
            OnTargetChanged?.Invoke(_currentTargetArea);

            if (_currentTargetArea == null)
            {
                ClearNavigation();
                return;
            }

            RecalculatePath(true);
        }

        public void ClearNavigation()
        {
            _currentTargetArea = null;
            _hasActivePath = false;
            _currentPathWaypoints.Clear();
            _remainingDistance = 0f;

            OnTargetChanged?.Invoke(null);
            OnPathCleared?.Invoke();
        }

        private void Update()
        {
            FindPlayerIfNeeded();

            if (!_hasActivePath || _currentTargetArea == null || playerVehicle == null) return;

            // Check if player vehicle has moved significantly to recalculate road path
            float distToLast = Vector3.Distance(playerVehicle.position, _lastCalculatedPlayerPos);
            if (distToLast >= recalculateDistanceThreshold)
            {
                RecalculatePath(false);
            }
            else
            {
                // Update active path points relative to vehicle current position
                UpdateWaypointsProgress();
            }

            // Check arrival
            if (_hasActivePath && _remainingDistance <= arrivalDistanceThreshold)
            {
                var areaReached = _currentTargetArea;
                ClearNavigation();
                OnDestinationReached?.Invoke(areaReached);
            }
        }

        public void RecalculatePath(bool force)
        {
            FindPlayerIfNeeded();

            if (playerVehicle == null || _currentTargetArea == null) return;
            if (RoadGraph.Instance == null) return;

            _lastCalculatedPlayerPos = playerVehicle.position;

            // Ultra-fast Node Graph A* Search (< 0.01ms CPU time)
            List<Vector3> rawPath = RoadGraph.Instance.FindShortestPath(playerVehicle.position, _currentTargetArea.transform.position);

            if (rawPath != null && rawPath.Count >= 2)
            {
                _currentPathWaypoints = new List<Vector3>(rawPath);
                _hasActivePath = true;

                // Immediately clean up nodes that are behind the car to prevent backward paths
                CleanUpWaypointsBehindCar();

                CalculateTotalDistance();
                OnPathUpdated?.Invoke(_currentPathWaypoints);
            }
        }

        private void UpdateWaypointsProgress()
        {
            if (_currentPathWaypoints.Count < 2) return;

            // Update starting point to car position
            _currentPathWaypoints[0] = playerVehicle.position;

            CleanUpWaypointsBehindCar();

            CalculateTotalDistance();
            OnPathUpdated?.Invoke(_currentPathWaypoints);
        }

        private void CleanUpWaypointsBehindCar()
        {
            // Remove waypoints that the car has already passed (too close or behind)
            while (_currentPathWaypoints.Count > 2)
            {
                Vector3 toCorner = _currentPathWaypoints[1] - playerVehicle.position;
                toCorner.y = 0;
                
                if (toCorner.magnitude < 5f || Vector3.Dot(playerVehicle.forward, toCorner.normalized) < -0.1f)
                {
                    _currentPathWaypoints.RemoveAt(1);
                }
                else
                {
                    break;
                }
            }
        }

        private void CalculateTotalDistance()
        {
            _remainingDistance = 0f;
            for (int i = 0; i < _currentPathWaypoints.Count - 1; i++)
            {
                _remainingDistance += Vector3.Distance(_currentPathWaypoints[i], _currentPathWaypoints[i + 1]);
            }
        }

        /// <summary>
        /// Gets direction vector pointing towards the next road turn / waypoint.
        /// </summary>
        public Vector3 GetNextWaypointDirection()
        {
            if (!_hasActivePath || _currentPathWaypoints.Count < 2 || playerVehicle == null)
                return playerVehicle != null ? playerVehicle.forward : Vector3.forward;

            Vector3 dir = (_currentPathWaypoints[1] - playerVehicle.position);
            dir.y = 0;
            return dir.magnitude > 0.1f ? dir.normalized : playerVehicle.forward;
        }
    }
}
