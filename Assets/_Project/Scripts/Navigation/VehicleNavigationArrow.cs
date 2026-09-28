using System;
using UnityEngine;
using CodeDrive.Portfolio;

namespace CodeDrive.Navigation
{
    /// <summary>
    /// Spawns and rotates a floating 3D navigation arrow above the player's car,
    /// pointing towards the next road waypoint/turn.
    /// </summary>
    public class VehicleNavigationArrow : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private float heightAboveCar = 2.8f;
        [SerializeField] private float arrowScale = 1.2f;
        [SerializeField] private Color arrowColor = new Color(0f, 0.9f, 1f, 0.95f);
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float bobbingSpeed = 3f;
        [SerializeField] private float bobbingAmount = 0.15f;

        private GameObject _arrowContainer;
        private Transform _arrowTransform;
        private Transform _playerCar;
        private Material _arrowMaterial;

        private void Start()
        {
            CreateArrowVisuals();
            HideArrow();

            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnTargetChanged += HandleTargetChanged;
                RoadNavigationManager.Instance.OnPathCleared += HideArrow;
            }
        }

        private void OnDestroy()
        {
            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnTargetChanged -= HandleTargetChanged;
                RoadNavigationManager.Instance.OnPathCleared -= HideArrow;
            }
        }

        private void CreateArrowVisuals()
        {
            _arrowContainer = new GameObject("GPS_Vehicle3DArrow");
            _arrowContainer.transform.SetParent(transform, false);

            // Create 3D Pyramid/Pointer Mesh
            GameObject py = GameObject.CreatePrimitive(PrimitiveType.Cube);
            py.transform.SetParent(_arrowContainer.transform, false);
            py.transform.localScale = new Vector3(arrowScale * 0.5f, arrowScale * 0.3f, arrowScale * 1.2f);
            py.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            DestroyImmediate(py.GetComponent<Collider>());

            // Material
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            _arrowMaterial = new Material(shader);
            _arrowMaterial.name = "GPS_3DArrow_Mat";
            if (_arrowMaterial.HasProperty("_BaseColor")) _arrowMaterial.SetColor("_BaseColor", arrowColor);
            else if (_arrowMaterial.HasProperty("_Color")) _arrowMaterial.SetColor("_Color", arrowColor);

            py.GetComponent<MeshRenderer>().material = _arrowMaterial;
            _arrowTransform = py.transform;
        }

        private void Update()
        {
            if (RoadNavigationManager.Instance == null || !RoadNavigationManager.Instance.HasActivePath)
            {
                HideArrow();
                return;
            }

            if (_playerCar == null)
            {
                _playerCar = RoadNavigationManager.Instance.PlayerVehicle;
                if (_playerCar == null) return;
            }

            _arrowContainer.SetActive(true);

            // Follow car position above roof with gentle bobbing
            float yPos = _playerCar.position.y + heightAboveCar + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            _arrowContainer.transform.position = new Vector3(_playerCar.position.x, yPos, _playerCar.position.z);

            // Rotate towards next waypoint
            Vector3 targetDir = RoadNavigationManager.Instance.GetNextWaypointDirection();
            if (targetDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDir, Vector3.up);
                _arrowContainer.transform.rotation = Quaternion.Slerp(_arrowContainer.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        private void HandleTargetChanged(PortfolioArea area)
        {
            if (area == null) HideArrow();
        }

        private void HideArrow()
        {
            if (_arrowContainer != null)
            {
                _arrowContainer.SetActive(false);
            }
        }
    }
}
