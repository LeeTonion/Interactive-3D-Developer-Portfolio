using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Animates ventilation fans continuously and cycles landing pad hydraulic pistons
    /// synchronized with steam vent bursts.
    /// </summary>
    public class AmbientMachineryAnimator : MonoBehaviour
    {
        [Header("Ventilation Fans")]
        public Transform[] fanBlades;
        public float[] fanSpeeds;

        [Header("Hydraulic Pistons & Synchronized Steam Vents")]
        public Transform[] hydraulicPistons;
        public ParticleSystem[] syncedSteamVents;
        public float pistonCycleSeconds = 6.5f;
        public float pistonStroke = 0.24f;

        private Vector3[] _pistonBasePos;
        private float[] _pistonTimers;
        private bool[] _ventFired;

        private void Start()
        {
            if (hydraulicPistons != null)
            {
                _pistonBasePos = new Vector3[hydraulicPistons.Length];
                _pistonTimers = new float[hydraulicPistons.Length];
                _ventFired = new bool[hydraulicPistons.Length];
                for (int i = 0; i < hydraulicPistons.Length; i++)
                {
                    if (hydraulicPistons[i] != null)
                        _pistonBasePos[i] = hydraulicPistons[i].localPosition;
                    _pistonTimers[i] = i * (pistonCycleSeconds / Mathf.Max(1, hydraulicPistons.Length));
                }
            }
        }

        private void Update()
        {
            float spd = HangarSimulationManager.EffectiveSpeed;
            if (spd <= 0.001f) return;
            float dt = Time.deltaTime * spd;

            // 1. Spin all ventilation fans
            if (fanBlades != null)
            {
                for (int i = 0; i < fanBlades.Length; i++)
                {
                    if (fanBlades[i] == null) continue;
                    float fSpd = (fanSpeeds != null && i < fanSpeeds.Length) ? fanSpeeds[i] : 180f;
                    fanBlades[i].Rotate(Vector3.forward, fSpd * dt, Space.Self);
                }
            }

            // 2. Cycle hydraulic pistons with synchronized steam vent burst
            if (hydraulicPistons != null && _pistonBasePos != null)
            {
                for (int i = 0; i < hydraulicPistons.Length; i++)
                {
                    if (hydraulicPistons[i] == null) continue;
                    _pistonTimers[i] = (_pistonTimers[i] + dt) % pistonCycleSeconds;
                    float t = _pistonTimers[i];

                    // Brief 1.1s pressure release stroke per cycle
                    float offset = 0f;
                    if (t < 1.1f)
                    {
                        float p = t / 1.1f;
                        offset = -Mathf.Sin(p * Mathf.PI) * pistonStroke;
                        if (!_ventFired[i] && syncedSteamVents != null && i < syncedSteamVents.Length && syncedSteamVents[i] != null)
                        {
                            syncedSteamVents[i].Emit(14);
                            _ventFired[i] = true;
                        }
                    }
                    else
                    {
                        _ventFired[i] = false;
                    }

                    hydraulicPistons[i].localPosition = _pistonBasePos[i] + Vector3.up * offset;
                }
            }
        }
    }
}
