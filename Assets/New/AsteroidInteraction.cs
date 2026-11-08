using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Section 7 (Interaction C & D): Asteroid Interactive Discoveries.
    /// - Interaction D (Cargo Ship Observation):
    ///   Khi người chơi tiếp cận Bến Bốc Dỡ Hàng (Cargo Loading Platform):
    ///   1. Đèn tín hiệu bốc dỡ (Loading Lights) kích hoạt sáng rực theo chuỗi.
    ///   2. Các tàu chở hàng lân cận hoạt động sôi nổi hơn.
    ///   3. Hoạt cảnh bốc dỡ thùng quặng năng lượng (Cargo Transfer Animation) diễn ra mượt mà.
    /// </summary>
    public class AsteroidInteraction : MonoBehaviour
    {
        [Header("Inspector Settings (Bán Kính Tương Tác)")]
        public float playerInteractionRadius = 22f;

        [Header("Interaction D: Cargo Ship Observation")]
        public Light[] loadingPlatformLights;
        public CargoShipOrbitController[] nearbyCargoShips;
        public Transform cargoContainershuttle;
        public LineRenderer cargoTransferBeam;
        public Vector3 transferStartLocalPos = new Vector3(-2.5f, 1.2f, 0f);
        public Vector3 transferEndLocalPos = new Vector3(2.5f, 2.6f, 0f);
        public Transform transferBeamAnchor;

        public Color idleLightColor = new Color(0f, 0.75f, 1f, 1f);
        public Color activeLoadingColor = new Color(0.15f, 1f, 0.55f, 1f);

        private Transform _playerTransform;
        private bool _isObserving;
        private float _transferPhase;

        private void Start()
        {
            FindPlayer();
            SetLoadingState(false);
        }

        private void FindPlayer()
        {
            var p = GameObject.Find("PlayerSpaceship");
            if (p != null) _playerTransform = p.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;
            if (_playerTransform == null) FindPlayer();

            bool playerNear = false;
            if (_playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                playerNear = dist <= playerInteractionRadius;
            }

            if (playerNear != _isObserving)
            {
                _isObserving = playerNear;
                SetLoadingState(_isObserving);
            }

            if (_isObserving)
            {
                // 1. Đèn bốc dỡ chớp sáng theo chuỗi dẫn đường
                if (loadingPlatformLights != null)
                {
                    for (int i = 0; i < loadingPlatformLights.Length; i++)
                    {
                        if (loadingPlatformLights[i] == null) continue;
                        float wave = 0.5f + 0.5f * Mathf.Sin(time * 7f - i * 1.1f);
                        loadingPlatformLights[i].color = Color.Lerp(idleLightColor, activeLoadingColor, wave);
                        loadingPlatformLights[i].intensity = Mathf.Lerp(2.0f, 5.2f, wave);
                    }
                }

                // 2. Hoạt cảnh di chuyển thùng quặng năng lượng (Cargo Transfer Animation)
                _transferPhase += dt * 0.65f;
                float pingPong = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(_transferPhase, 1f));
                if (cargoContainershuttle != null)
                {
                    Vector3 pos = Vector3.Lerp(transferStartLocalPos, transferEndLocalPos, pingPong);
                    pos.y += Mathf.Sin(pingPong * Mathf.PI) * 1.1f;
                    cargoContainershuttle.localPosition = pos;
                    cargoContainershuttle.Rotate(Vector3.up * (35f * dt), Space.Self);

                    if (cargoTransferBeam != null && transferBeamAnchor != null)
                    {
                        cargoTransferBeam.enabled = true;
                        cargoTransferBeam.SetPosition(0, transferBeamAnchor.position);
                        cargoTransferBeam.SetPosition(1, cargoContainershuttle.position);
                    }
                }
            }
        }

        private void SetLoadingState(bool active)
        {
            if (nearbyCargoShips != null)
            {
                for (int i = 0; i < nearbyCargoShips.Length; i++)
                {
                    if (nearbyCargoShips[i] != null)
                    {
                        nearbyCargoShips[i].SetObservationBoost(active);
                    }
                }
            }

            if (!active)
            {
                if (cargoTransferBeam != null) cargoTransferBeam.enabled = false;
                if (loadingPlatformLights != null)
                {
                    for (int i = 0; i < loadingPlatformLights.Length; i++)
                    {
                        if (loadingPlatformLights[i] == null) continue;
                        loadingPlatformLights[i].color = idleLightColor;
                        loadingPlatformLights[i].intensity = 1.6f;
                    }
                }
            }
        }
    }
}

