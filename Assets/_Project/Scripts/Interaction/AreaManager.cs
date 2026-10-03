using System;
using System.Collections.Generic;
using UnityEngine;
using CodeDrive.Portfolio;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Central manager for all interactive portfolio areas and trigger zones in the world.
    /// Tracks all PortfolioAreas, current active area, and broadcasts events when entering or exiting zones.
    /// </summary>
    public class AreaManager : MonoBehaviour
    {
        public static AreaManager Instance { get; private set; }

        [Header("Active State")]
        [SerializeField] private PortfolioArea currentArea;
        [SerializeField] private InteractionTrigger currentTrigger;

        private readonly List<PortfolioArea> _registeredAreas = new List<PortfolioArea>();
        private readonly Dictionary<PortfolioAreaType, PortfolioArea> _areaByTypeMap = new Dictionary<PortfolioAreaType, PortfolioArea>();

        // Events
        public event Action<PortfolioArea> OnAreaEntered;
        public event Action<PortfolioArea> OnAreaExited;
        public event Action<PortfolioArea> OnAreaInteracted;
        public event Action OnAreasListUpdated;

        public PortfolioArea CurrentArea => currentArea;
        public InteractionTrigger CurrentTrigger => currentTrigger;
        public bool IsInAnyArea => currentArea != null;
        public IReadOnlyList<PortfolioArea> AllAreas => _registeredAreas;

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
            // Auto-discover all PortfolioAreas in scene to ensure complete list
            var foundAreas = UnityEngine.Object.FindObjectsOfType<PortfolioArea>();
            foreach (var area in foundAreas)
            {
                RegisterArea(area);
            }
        }

        public void RegisterArea(PortfolioArea area)
        {
            if (area == null) return;

            if (!_registeredAreas.Contains(area))
            {
                _registeredAreas.Add(area);
                _areaByTypeMap[area.AreaType] = area;
                OnAreasListUpdated?.Invoke();
            }
        }

        public void UnregisterArea(PortfolioArea area)
        {
            if (area == null) return;

            if (_registeredAreas.Contains(area))
            {
                _registeredAreas.Remove(area);
                _areaByTypeMap.Remove(area.AreaType);
                if (currentArea == area)
                {
                    NotifyPlayerExited(area, null);
                }
                OnAreasListUpdated?.Invoke();
            }
        }

        public PortfolioArea GetAreaByType(PortfolioAreaType areaType)
        {
            _areaByTypeMap.TryGetValue(areaType, out var area);
            return area;
        }

        public void NotifyPlayerEntered(PortfolioArea area, InteractionTrigger trigger)
        {
            currentArea = area;
            currentTrigger = trigger;

            string areaName = area != null ? area.AreaLabel : (trigger != null ? trigger.DisplayName : "Unknown Area");
            Debug.Log($"<color=#2EA3FF>[AreaManager]</color> Player entered area: <b>{areaName}</b> ({(area != null ? area.AreaType.ToString() : "N/A")})");

            OnAreaEntered?.Invoke(area);
        }

        public void NotifyPlayerExited(PortfolioArea area, InteractionTrigger trigger)
        {
            if (currentArea == area || (trigger != null && currentTrigger == trigger))
            {
                string areaName = area != null ? area.AreaLabel : (trigger != null ? trigger.DisplayName : "Unknown Area");
                Debug.Log($"<color=#FF9A2E>[AreaManager]</color> Player exited area: <b>{areaName}</b>");

                currentArea = null;
                currentTrigger = null;

                OnAreaExited?.Invoke(area);
            }
        }

        public void NotifyAreaInteracted(PortfolioArea area)
        {
            if (area != null)
            {
                OnAreaInteracted?.Invoke(area);
            }
        }
    }
}
