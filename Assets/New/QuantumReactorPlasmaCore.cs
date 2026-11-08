using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Animates the Glowing Plasma Core inside the Quantum Plasma Converter Reactor.
    /// Features:
    /// - 3-Axis Gimbal Gyroscope Rings rotating at smooth mechanical speeds.
    /// - Pulsing high-emission plasma sphere core with dynamic point light breathing.
    /// - Orbital micro-particle sparks and plasma surge waves.
    /// </summary>
    public class QuantumReactorPlasmaCore : MonoBehaviour
    {
        [Header("Core Sphere & Lights")]
        public Transform plasmaCoreSphere;
        public Light coreLight;
        public Color plasmaColor = new Color(0f, 0.95f, 1f, 1f);
        public float baseIntensity = 3.5f;
        public float pulseRange = 1.2f;
        public float pulseFrequency = 3.2f;

        [Header("Gimbal Gyro Rings")]
        public Transform outerRing;
        public Transform middleRing;
        public Transform innerRing;
        public float outerSpeed = 24f;
        public float middleSpeed = -38f;
        public float innerSpeed = 52f;

        [Header("Plasma Particles")]
        public ParticleSystem plasmaSparksPs;

        private Material _coreMat;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            if (plasmaCoreSphere != null)
            {
                var rend = plasmaCoreSphere.GetComponent<Renderer>();
                if (rend != null) _coreMat = rend.material;
            }

            if (coreLight != null)
            {
                coreLight.color = plasmaColor;
                coreLight.range = 14f;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Rotate 3 gimbal rings on independent axes
            if (outerRing != null) outerRing.Rotate(Vector3.up, outerSpeed * dt, Space.Self);
            if (middleRing != null) middleRing.Rotate(Vector3.right, middleSpeed * dt, Space.Self);
            if (innerRing != null) innerRing.Rotate(Vector3.forward, innerSpeed * dt, Space.Self);

            // 2. Pulse plasma core intensity & emission
            float wave = Mathf.Sin(Time.time * pulseFrequency);
            float intensity = baseIntensity + wave * pulseRange;

            if (coreLight != null)
            {
                coreLight.intensity = intensity;
            }

            if (_coreMat != null)
            {
                Color emissiveCol = plasmaColor * (2.2f + wave * 0.8f);
                _coreMat.SetColor(EmissionColorId, emissiveCol);
            }

            // 3. Subtle floating core bobbing
            if (plasmaCoreSphere != null)
            {
                float bobY = Mathf.Sin(Time.time * 4.5f) * 0.06f;
                Vector3 p = plasmaCoreSphere.localPosition;
                plasmaCoreSphere.localPosition = new Vector3(p.x, bobY, p.z);
            }
        }
    }
}

