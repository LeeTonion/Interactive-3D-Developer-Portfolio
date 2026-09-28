using System;
using UnityEngine;
using CodeDrive.Portfolio;

namespace CodeDrive.Navigation
{
    /// <summary>
    /// Displays a 3D visual beacon (vertical beam, rings, floating label) at the active GPS destination.
    /// </summary>
    public class NavigationBeacon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float beamHeight = 25f;
        [SerializeField] private float beamRadius = 1.5f;
        [SerializeField] private Color beaconColor = new Color(0f, 0.9f, 1f, 0.7f);
        [SerializeField] private float bobbingSpeed = 2f;
        [SerializeField] private float bobbingAmount = 0.5f;
        [SerializeField] private float rotationSpeed = 45f;

        private GameObject _beaconVisualContainer;
        private Transform _floatingMarker;
        private Vector3 _basePos;
        private bool _isActive;

        private void Start()
        {
            CreateBeaconVisuals();
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

        private void CreateBeaconVisuals()
        {
            _beaconVisualContainer = new GameObject("GPS_BeaconVisuals");
            _beaconVisualContainer.transform.SetParent(transform, false);

            // Light beam (cylinder)
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "LightBeam";
            beam.transform.SetParent(_beaconVisualContainer.transform, false);
            beam.transform.localScale = new Vector3(beamRadius * 2f, beamHeight * 0.5f, beamRadius * 2f);
            beam.transform.localPosition = new Vector3(0f, beamHeight * 0.5f, 0f);

            DestroyImmediate(beam.GetComponent<Collider>());

            var mr = beam.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            mat.name = "GPS_Beacon_Material";
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", beaconColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", beaconColor);

            mr.material = mat;

            // Floating icon / marker at the top
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "FloatingDiamond";
            marker.transform.SetParent(_beaconVisualContainer.transform, false);
            marker.transform.localPosition = new Vector3(0f, 3f, 0f);
            marker.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            marker.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

            DestroyImmediate(marker.GetComponent<Collider>());

            var markerMr = marker.GetComponent<MeshRenderer>();
            markerMr.material = mat;
            _floatingMarker = marker.transform;
        }

        private void Update()
        {
            if (!_isActive || _beaconVisualContainer == null) return;

            // Bobbing & rotating marker animation
            float newY = _basePos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            _beaconVisualContainer.transform.position = new Vector3(_basePos.x, newY, _basePos.z);

            if (_floatingMarker != null)
            {
                _floatingMarker.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void HandleTargetChanged(PortfolioArea targetArea)
        {
            if (targetArea == null)
            {
                HideBeacon();
                return;
            }

            // Position beacon at destination point on road
            _basePos = targetArea.transform.position;
            _beaconVisualContainer.transform.position = _basePos;
            _beaconVisualContainer.SetActive(true);
            _isActive = true;
        }

        private void HandleDestinationReached(PortfolioArea area)
        {
            HideBeacon();
        }

        private void HideBeacon()
        {
            _isActive = false;
            if (_beaconVisualContainer != null)
            {
                _beaconVisualContainer.SetActive(false);
            }
        }
    }
}
