using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Represents a mining station / platform along the railway circuit.
    /// Handles train docking, deceleration, gemstone loading animation with particle emitters, and clearance.
    /// </summary>
    public class MiningStationDock : MonoBehaviour
    {
        [Header("Station Identification")]
        public string stationName = "Mining Terminal";
        public float circuitDistance = 0f; // Distance along the railway where the train stops
        public float stopTolerance = 1.8f;

        [Header("Station Operations")]
        public float dockingDuration = 4.0f;
        public float loadingAnimationDuration = 2.5f;

        [Header("Visual Effects")]
        public ParticleSystem gemstoneLoadingParticles;
        public Light stationIndicatorLight;
        public Color idleStatusColor = new Color(0f, 0.75f, 1f, 1f);
        public Color loadingStatusColor = new Color(0.95f, 0.8f, 0.15f, 1f);
        public Color departStatusColor = new Color(0.2f, 1f, 0.4f, 1f);

        [Header("Machinery / Gantry Animations")]
        public Transform loaderArm;
        private Vector3 _initialArmRot;
        private bool _isLoadingActive = false;

        private void Start()
        {
            if (loaderArm != null)
            {
                _initialArmRot = loaderArm.localEulerAngles;
            }

            SetStationState(StationState.Idle);
        }

        public enum StationState
        {
            Idle,
            Loading,
            Clearing
        }

        public void SetStationState(StationState state)
        {
            switch (state)
            {
                case StationState.Idle:
                    _isLoadingActive = false;
                    if (stationIndicatorLight != null)
                    {
                        stationIndicatorLight.color = idleStatusColor;
                        stationIndicatorLight.intensity = 2.0f;
                    }
                    if (gemstoneLoadingParticles != null && gemstoneLoadingParticles.isPlaying)
                    {
                        gemstoneLoadingParticles.Stop();
                    }
                    break;

                case StationState.Loading:
                    _isLoadingActive = true;
                    if (stationIndicatorLight != null)
                    {
                        stationIndicatorLight.color = loadingStatusColor;
                        stationIndicatorLight.intensity = 3.5f;
                    }
                    if (gemstoneLoadingParticles != null && !gemstoneLoadingParticles.isPlaying)
                    {
                        gemstoneLoadingParticles.Play();
                    }
                    break;

                case StationState.Clearing:
                    _isLoadingActive = false;
                    if (stationIndicatorLight != null)
                    {
                        stationIndicatorLight.color = departStatusColor;
                        stationIndicatorLight.intensity = 2.8f;
                    }
                    if (gemstoneLoadingParticles != null && gemstoneLoadingParticles.isPlaying)
                    {
                        gemstoneLoadingParticles.Stop();
                    }
                    break;
            }
        }

        private void Update()
        {
            if (_isLoadingActive && loaderArm != null)
            {
                // Subtle mechanical pivot animation while loading
                float angle = Mathf.Sin(Time.time * 3.5f) * 12f;
                loaderArm.localEulerAngles = _initialArmRot + new Vector3(0f, 0f, angle);
            }
        }
    }
}

