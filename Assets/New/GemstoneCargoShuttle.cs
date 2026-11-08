using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Autonomous Cargo Shuttle transporting raw energy crystals between GemstoneMiningAsteroid
    /// and the Gemstone Import Terminal at the Main Island (AboutIsland).
    /// Features full vertical VTOL Landing & Takeoff animation:
    /// - Smooth spline approach to an overhead holding point.
    /// - Descent & touchdown onto landing gear with suspension compress & thruster blast down.
    /// - Cargo unloading hold period with animated mineral pod lift & energy beam.
    /// - Vertical VTOL liftoff, rotation toward transit heading, and high-speed cruise departure.
    /// </summary>
    public class GemstoneCargoShuttle : MonoBehaviour
    {
        [Header("Route Waypoints (World Space)")]
        public List<Vector3> routeWaypoints = new List<Vector3>();

        [Header("Flight Dynamics")]
        public float cruiseSpeed = 16f;
        public float turnSpeed = 4.5f;

        [Header("VTOL Landing & Takeoff Animation")]
        [Tooltip("Height above pad where vertical descent begins")]
        public float vtolHoverAltitude = 7.5f;
        public float landingDescentSpeed = 2.4f;
        public float liftoffAscentSpeed = 3.2f;
        public float padTouchdownOffset = 0.45f;

        [Header("Hold / Loading Durations")]
        public float loadingHoldTime = 4.0f;
        public float unloadingHoldTime = 4.5f;

        [Header("References")]
        public Transform cargoPodPayload;
        public Light engineGlowLight;
        public Light vtolBellyLight;
        public ParticleSystem engineTrailParticles;
        public ParticleSystem vtolDownblastParticles;

        [Header("Materials for Cargo Pod")]
        public Renderer cargoPodRenderer;
        public Material loadedCrystalMat;
        public Material emptyContainerMat;

        // Runtime states
        public enum ShuttleState
        {
            LoadingAtMiningAsteroid,
            AscendingFromAsteroid,
            FlyingToMainIsland,
            DescendingToMainPad,
            UnloadingAtMainTerminal,
            AscendingFromMainPad,
            FlyingBackToMiningAsteroid,
            DescendingToAsteroidPad
        }

        public ShuttleState currentState = ShuttleState.FlyingToMainIsland;
        private int _currentWpIndex = 0;
        private float _holdTimer = 0f;
        private Vector3 _dockPadTarget;
        private Quaternion _dockPadRotation;
        private Vector3 _cargoPodInitialLocalPos;

        public float RemainingHoldTime => _holdTimer;

        private void Start()
        {
            if (cargoPodPayload != null)
            {
                _cargoPodInitialLocalPos = cargoPodPayload.localPosition;
            }

            if (routeWaypoints == null || routeWaypoints.Count < 2)
            {
                routeWaypoints = new List<Vector3>()
                {
                    new Vector3(110f, 16.5f, -67f),  // Mining Asteroid Pad
                    new Vector3(98f, 24f, -48f),
                    new Vector3(72f, 26f, -22f),
                    new Vector3(50f, 18f, -6f),
                    new Vector3(36f, 10f, -6f),
                    new Vector3(26.50f, 0.12f, -6.00f)     // Main Island Terminal VTOL Pad
                };
            }

            _dockPadTarget = routeWaypoints[routeWaypoints.Count - 1];
            _dockPadRotation = Quaternion.Euler(0f, -90f, 0f);

            SetCargoVisualState(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            switch (currentState)
            {
                // 1. Mining Asteroid Operations
                case ShuttleState.LoadingAtMiningAsteroid:
                    HandleLoadingHold(dt);
                    break;

                case ShuttleState.AscendingFromAsteroid:
                    HandleAscent(fromAsteroid: true, dt);
                    break;

                // 2. Cruise to Main Island
                case ShuttleState.FlyingToMainIsland:
                    MoveAlongCruiseRoute(forward: true, dt);
                    break;

                // 3. Main Island Landing & Unloading
                case ShuttleState.DescendingToMainPad:
                    HandleLandingDescent(toMain: true, dt);
                    break;

                case ShuttleState.UnloadingAtMainTerminal:
                    HandleUnloadingHold(dt);
                    break;

                case ShuttleState.AscendingFromMainPad:
                    HandleAscent(fromAsteroid: false, dt);
                    break;

                // 4. Return Cruise
                case ShuttleState.FlyingBackToMiningAsteroid:
                    MoveAlongCruiseRoute(forward: false, dt);
                    break;

                case ShuttleState.DescendingToAsteroidPad:
                    HandleLandingDescent(toMain: false, dt);
                    break;
            }

            // Engine & Thruster visual updates
            UpdateThrusters(dt);
        }

        private void MoveAlongCruiseRoute(bool forward, float dt)
        {
            if (routeWaypoints.Count < 2) return;

            Vector3 targetWp = routeWaypoints[_currentWpIndex];
            Vector3 diff = targetWp - transform.position;
            float dist = diff.magnitude;

            if (dist > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(diff.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * dt);
            }

            float speed = cruiseSpeed;
            if (dist < 10f) speed = Mathf.Lerp(cruiseSpeed * 0.45f, cruiseSpeed, dist / 10f);

            transform.position = Vector3.MoveTowards(transform.position, targetWp, speed * dt);

            if (dist < 1.5f)
            {
                if (forward)
                {
                    _currentWpIndex++;
                    if (_currentWpIndex >= routeWaypoints.Count - 1)
                    {
                        // Arrived at overhead holding point -> Begin VTOL descent onto Main Island Pad
                        currentState = ShuttleState.DescendingToMainPad;
                    }
                }
                else
                {
                    _currentWpIndex--;
                    if (_currentWpIndex <= 0)
                    {
                        // Arrived at overhead holding point of Asteroid -> Begin VTOL descent onto Asteroid Pad
                        currentState = ShuttleState.DescendingToAsteroidPad;
                    }
                }
            }
        }

        private void HandleLandingDescent(bool toMain, float dt)
        {
            Vector3 padPos = toMain ? routeWaypoints[routeWaypoints.Count - 1] : routeWaypoints[0];
            Vector3 touchdownPos = padPos + Vector3.up * padTouchdownOffset;

            // Align rotation to landing pad orientation
            Quaternion targetRot = toMain ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 3.5f * dt);

            // Move downward vertically
            transform.position = Vector3.MoveTowards(transform.position, touchdownPos, landingDescentSpeed * dt);

            // Enable downblast VTOL thruster effects
            SetVTOLActive(true);

            if (Vector3.Distance(transform.position, touchdownPos) < 0.08f)
            {
                // Touchdown!
                transform.position = touchdownPos;
                SetVTOLActive(false);

                if (toMain)
                {
                    currentState = ShuttleState.UnloadingAtMainTerminal;
                    _holdTimer = unloadingHoldTime;
                }
                else
                {
                    currentState = ShuttleState.LoadingAtMiningAsteroid;
                    _holdTimer = loadingHoldTime;
                }
            }
        }

        private void HandleAscent(bool fromAsteroid, float dt)
        {
            Vector3 padPos = fromAsteroid ? routeWaypoints[0] : routeWaypoints[routeWaypoints.Count - 1];
            Vector3 hoverPos = padPos + Vector3.up * vtolHoverAltitude;

            SetVTOLActive(true);

            transform.position = Vector3.MoveTowards(transform.position, hoverPos, liftoffAscentSpeed * dt);

            if (Vector3.Distance(transform.position, hoverPos) < 0.2f)
            {
                SetVTOLActive(false);

                if (fromAsteroid)
                {
                    currentState = ShuttleState.FlyingToMainIsland;
                    _currentWpIndex = 1;
                }
                else
                {
                    currentState = ShuttleState.FlyingBackToMiningAsteroid;
                    _currentWpIndex = routeWaypoints.Count - 2;
                }
            }
        }

        private void HandleUnloadingHold(float dt)
        {
            _holdTimer -= dt;

            // Subtle cargo pod levitation / scan animation while unloading
            if (cargoPodPayload != null)
            {
                float lift = Mathf.Sin((unloadingHoldTime - _holdTimer) * 2f) * 0.25f;
                cargoPodPayload.localPosition = _cargoPodInitialLocalPos + new Vector3(0f, Mathf.Max(0f, lift), 0f);
            }

            if (_holdTimer <= 1.2f)
            {
                SetCargoVisualState(false);
            }

            if (_holdTimer <= 0f)
            {
                // Safety timing check: Ensure suction gantry is fully retracted before liftoff!
                var gantry = Object.FindFirstObjectByType<GantryCranePulleyAnimation>();
                if (gantry != null && !gantry.IsRetractedAndSafe)
                {
                    // Hold in place until gantry nozzle has safely pulled away
                    return;
                }

                if (cargoPodPayload != null) cargoPodPayload.localPosition = _cargoPodInitialLocalPos;
                currentState = ShuttleState.AscendingFromMainPad;
            }
        }

        private void HandleLoadingHold(float dt)
        {
            _holdTimer -= dt;

            if (_holdTimer <= 1.2f)
            {
                SetCargoVisualState(true);
            }

            if (_holdTimer <= 0f)
            {
                currentState = ShuttleState.AscendingFromAsteroid;
            }
        }

        private void SetVTOLActive(bool active)
        {
            if (vtolBellyLight != null) vtolBellyLight.enabled = active;
            if (vtolDownblastParticles != null)
            {
                if (active && !vtolDownblastParticles.isPlaying) vtolDownblastParticles.Play();
                else if (!active && vtolDownblastParticles.isPlaying) vtolDownblastParticles.Stop();
            }
        }

        private void UpdateThrusters(float dt)
        {
            if (engineGlowLight != null)
            {
                bool isCruising = (currentState == ShuttleState.FlyingToMainIsland || currentState == ShuttleState.FlyingBackToMiningAsteroid);
                float baseIntensity = isCruising ? 3.0f : 1.2f;
                engineGlowLight.intensity = baseIntensity + Mathf.Sin(Time.time * 9f) * 0.35f;
            }
        }

        public void SetCargoVisualState(bool loaded)
        {
            if (cargoPodPayload != null)
            {
                cargoPodPayload.gameObject.SetActive(true);
            }

            if (cargoPodRenderer != null)
            {
                Material m = loaded ? loadedCrystalMat : emptyContainerMat;
                if (m != null)
                {
                    var mats = cargoPodRenderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null && (mats[i].name.ToLower().Contains("red") || mats[i].name.ToLower().Contains("gemstone") || mats[i].name.ToLower().Contains("slate") || mats[i].name.ToLower().Contains("crystal") || mats[i].name.ToLower().Contains("metal")))
                        {
                            mats[i] = m;
                        }
                    }
                    cargoPodRenderer.sharedMaterials = mats;
                }
            }
        }
    }
}
