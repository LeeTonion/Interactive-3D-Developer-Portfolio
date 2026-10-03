using System;
using UnityEngine;
using CodeDrive.Portfolio;

namespace CodeDrive.Navigation
{
    /// <summary>
    /// GPS indicator at destination. Beam cylinder removed as requested by user.
    /// </summary>
    public class NavigationBeacon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showFloatingMarker = false;

        private GameObject _beaconVisualContainer;
        private bool _isActive;

        private void Start()
        {
            HideBeacon();

            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnTargetChanged += HandleTargetChanged;
                RoadNavigationManager.Instance.OnPathCleared += HideBeacon;
                RoadNavigationManager.Instance.OnDestinationReached += HandleDestinationReached;
            }
        }

        private void OnDestroy()
        {
            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnTargetChanged -= HandleTargetChanged;
                RoadNavigationManager.Instance.OnPathCleared -= HideBeacon;
                RoadNavigationManager.Instance.OnDestinationReached -= HandleDestinationReached;
            }
        }

        private void HandleTargetChanged(PortfolioArea targetArea)
        {
            if (targetArea == null)
            {
                HideBeacon();
                return;
            }

            _isActive = true;
        }

        private void HandleDestinationReached(PortfolioArea area)
        {
            HideBeacon();
        }

        public void HideBeacon()
        {
            _isActive = false;
            if (_beaconVisualContainer != null)
            {
                _beaconVisualContainer.SetActive(false);
            }
        }
    }
}
