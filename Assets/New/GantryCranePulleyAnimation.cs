using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Controls the mechanical telescopic energy extraction suction nozzle at the Gemstone Import Terminal.
    /// Perfectly TIMED with the landed cargo shuttle:
    /// - Idle/Standby: Suction nozzle retracted high in overhead carriage. No extraction beam.
    /// - Shuttle Touchdown: Nozzle deploys downwards and docks into shuttle's mineral tank.
    /// - Pumping/Sucking: High-intensity plasma suction beam + pulsating glow transfer energy.
    /// - Finish: Suction shuts off, nozzle retracts cleanly back up BEFORE shuttle ascends to take off.
    /// </summary>
    public class GantryCranePulleyAnimation : MonoBehaviour
    {
        [Header("Gantry Hierarchy Components")]
        [Tooltip("The trolley carriage that moves horizontally along the truss.")]
        public Transform trolleyCarriage;
        [Tooltip("The vertical telescoping suction nozzle / pipe extending downwards.")]
        public Transform suctionNozzle;
        [Tooltip("Suction head magnetic coupling bell.")]
        public Transform suctionHeadBell;

        [Header("Energy Suction Effects")]
        public Light suctionPlasmaLight;
        public LineRenderer extractionBeamLine;
        public ParticleSystem energySuctionParticles;

        [Header("Telescopic Extension Range (Local to Trolley)")]
        public float retractedLocalY = -0.5f;
        public float extendedDockedLocalY = -3.8f;
        public float extendSpeed = 3.2f;
        public float retractSpeed = 3.6f;

        [Header("Target Shuttle Reference")]
        public GemstoneCargoShuttle activeLandedShuttle;

        public enum SuctionState
        {
            StandbyRetracted,
            ExtendingToLandedShuttle,
            ActiveSuctionPumping,
            RetractingBeforeTakeoff
        }

        public SuctionState currentState = SuctionState.StandbyRetracted;
        private float _currentNozzleY = -0.5f;

        public bool IsRetractedAndSafe => currentState == SuctionState.StandbyRetracted && Mathf.Abs(_currentNozzleY - retractedLocalY) < 0.15f;

        private void Start()
        {
            _currentNozzleY = retractedLocalY;
            SetExtractionActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Check shuttle presence
            if (activeLandedShuttle == null)
            {
                // Auto-find nearby shuttle in fleet
                var fleet = GameObject.Find("Gemstone_Cargo_Shuttle_Fleet");
                if (fleet != null)
                {
                    foreach (Transform t in fleet.transform)
                    {
                        var s = t.GetComponent<GemstoneCargoShuttle>();
                        if (s != null && Vector3.Distance(s.transform.position, transform.position) < 15f)
                        {
                            activeLandedShuttle = s;
                            break;
                        }
                    }
                }
            }

            // Synchronize state strictly with shuttle's flight cycle
            bool isShuttleDockedOnPad = activeLandedShuttle != null &&
                                       activeLandedShuttle.currentState == GemstoneCargoShuttle.ShuttleState.UnloadingAtMainTerminal;

            if (isShuttleDockedOnPad)
            {
                // Shuttle is on pad!
                if (currentState == SuctionState.StandbyRetracted)
                {
                    currentState = SuctionState.ExtendingToLandedShuttle;
                }
            }
            else
            {
                // Shuttle is flying, ascending, or absent -> MUST RETRACT & SHUT OFF
                if (currentState != SuctionState.StandbyRetracted)
                {
                    currentState = SuctionState.RetractingBeforeTakeoff;
                }
            }

            // State Machine Execution
            switch (currentState)
            {
                case SuctionState.StandbyRetracted:
                    _currentNozzleY = Mathf.MoveTowards(_currentNozzleY, retractedLocalY, retractSpeed * dt);
                    SetExtractionActive(false);
                    break;

                case SuctionState.ExtendingToLandedShuttle:
                    _currentNozzleY = Mathf.MoveTowards(_currentNozzleY, extendedDockedLocalY, extendSpeed * dt);
                    SetExtractionActive(false);
                    if (Mathf.Abs(_currentNozzleY - extendedDockedLocalY) < 0.05f)
                    {
                        currentState = SuctionState.ActiveSuctionPumping;
                    }
                    break;

                case SuctionState.ActiveSuctionPumping:
                    _currentNozzleY = extendedDockedLocalY;
                    SetExtractionActive(true);

                    // Dynamic plasma light pulsation
                    if (suctionPlasmaLight != null)
                    {
                        suctionPlasmaLight.intensity = 4.2f + Mathf.Sin(Time.time * 16f) * 1.2f;
                    }

                    // If shuttle is near the end of hold time, start retracting early so it clears before liftoff!
                    if (activeLandedShuttle != null && activeLandedShuttle.RemainingHoldTime < 1.0f)
                    {
                        currentState = SuctionState.RetractingBeforeTakeoff;
                    }
                    break;

                case SuctionState.RetractingBeforeTakeoff:
                    SetExtractionActive(false);
                    _currentNozzleY = Mathf.MoveTowards(_currentNozzleY, retractedLocalY, retractSpeed * dt);
                    if (Mathf.Abs(_currentNozzleY - retractedLocalY) < 0.05f)
                    {
                        currentState = SuctionState.StandbyRetracted;
                    }
                    break;
            }

            // Apply positions
            if (suctionNozzle != null)
            {
                suctionNozzle.localPosition = new Vector3(0f, _currentNozzleY, 0f);
            }

            // Update extraction beam endpoint if active
            if (extractionBeamLine != null && extractionBeamLine.enabled && suctionHeadBell != null && activeLandedShuttle != null)
            {
                extractionBeamLine.SetPosition(0, suctionHeadBell.position);
                Vector3 payloadPos = activeLandedShuttle.cargoPodPayload != null ? activeLandedShuttle.cargoPodPayload.position : activeLandedShuttle.transform.position + Vector3.up * 0.4f;
                extractionBeamLine.SetPosition(1, payloadPos);
            }
        }

        private void SetExtractionActive(bool active)
        {
            if (suctionPlasmaLight != null) suctionPlasmaLight.enabled = active;
            if (extractionBeamLine != null) extractionBeamLine.enabled = active;
            if (energySuctionParticles != null)
            {
                if (active && !energySuctionParticles.isPlaying) energySuctionParticles.Play();
                else if (!active && energySuctionParticles.isPlaying) energySuctionParticles.Stop();
            }
        }
    }
}
