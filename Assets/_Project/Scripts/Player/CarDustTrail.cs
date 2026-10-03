using UnityEngine;

/// <summary>
/// Creates stylized dust/dirt particle trails behind the car wheels.
/// Bruno Simon style: subtle, warm-toned, small soft particles that
/// kick up when driving fast or drifting.
/// </summary>
public class CarDustTrail : MonoBehaviour
{
    [Header("Dust Settings")]
    [Tooltip("Minimum speed (m/s) before dust appears")]
    [SerializeField] private float minSpeedForDust = 3f;
    [Tooltip("Speed at which dust is at full intensity")]
    [SerializeField] private float fullDustSpeed = 15f;
    [Tooltip("Extra dust multiplier when drifting/handbraking")]
    [SerializeField] private float driftDustMultiplier = 3f;

    [Header("Particle System Settings")]
    [SerializeField] private float baseEmissionRate = 25f;
    [SerializeField] private float maxEmissionRate = 80f;
    [SerializeField] private float particleLifetime = 1.2f;
    [SerializeField] private float particleSize = 0.3f;
    [SerializeField] private float particleSpeed = 1.5f;

    [Header("Colors (Bruno Simon Warm Palette)")]
    [SerializeField] private Color dustColorStart = new Color(0.85f, 0.78f, 0.65f, 0.4f); // Warm sand
    [SerializeField] private Color dustColorEnd = new Color(0.75f, 0.68f, 0.55f, 0.0f);   // Fade to nothing

    private CarControl _car;
    private Rigidbody _rb;
    private ParticleSystem[] _dustSystems;

    private void Start()
    {
        _car = GetComponent<CarControl>();
        _rb = GetComponent<Rigidbody>();

        if (_car == null || _rb == null) return;

        // Create dust emitters at rear wheel positions
        CreateDustEmitters();
    }

    private void CreateDustEmitters()
    {
        // Try to find rear wheel positions from CarControl
        Transform[] wheelMeshes = _car.wheelMeshes;
        if (wheelMeshes == null || wheelMeshes.Length < 4) return;

        // Rear wheels are index 2 and 3 (RL, RR)
        _dustSystems = new ParticleSystem[2];
        for (int i = 0; i < 2; i++)
        {
            Transform wheelTransform = wheelMeshes[i + 2]; // RL=2, RR=3
            if (wheelTransform == null) continue;

            GameObject dustObj = new GameObject($"DustTrail_{(i == 0 ? "RL" : "RR")}");
            dustObj.transform.SetParent(wheelTransform, false);
            dustObj.transform.localPosition = Vector3.down * 0.2f;

            var ps = dustObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = particleLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.5f, particleSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.5f, particleSize);
            main.startColor = new ParticleSystem.MinMaxGradient(dustColorStart, dustColorEnd);
            main.gravityModifier = 0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            // Emission
            var emission = ps.emission;
            emission.rateOverTime = 0f; // Controlled in Update

            // Shape: small cone pointing backward-upward
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-70f, 0f, 0f); // Point slightly backward and up

            // Size over lifetime: shrink then grow (puff)
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.3f);
            sizeCurve.AddKey(0.3f, 1f);
            sizeCurve.AddKey(1f, 1.5f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime: fade out alpha
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(dustColorStart, 0f),
                    new GradientColorKey(new Color(0.8f, 0.75f, 0.65f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(0.35f, 0.15f),
                    new GradientAlphaKey(0.25f, 0.5f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            // Renderer - use default particle material
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Try to set a soft particle material
            var defaultMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (defaultMat != null)
            {
                defaultMat.SetFloat("_Mode", 1); // Additive or fade
                defaultMat.color = dustColorStart;
                renderer.material = defaultMat;
            }

            ps.Stop();
            _dustSystems[i] = ps;
        }
    }

    private void Update()
    {
        if (_car == null || _rb == null || _dustSystems == null) return;

        float speed = _rb.linearVelocity.magnitude;
        bool isDrifting = _car.IsBraking;

        for (int i = 0; i < _dustSystems.Length; i++)
        {
            if (_dustSystems[i] == null) continue;

            var emission = _dustSystems[i].emission;

            if (speed < minSpeedForDust)
            {
                emission.rateOverTime = 0f;
                if (_dustSystems[i].isPlaying) _dustSystems[i].Stop();
                continue;
            }

            if (!_dustSystems[i].isPlaying) _dustSystems[i].Play();

            float speedNorm = Mathf.InverseLerp(minSpeedForDust, fullDustSpeed, speed);
            float rate = Mathf.Lerp(baseEmissionRate, maxEmissionRate, speedNorm);

            if (isDrifting)
            {
                rate *= driftDustMultiplier;
            }

            emission.rateOverTime = rate;

            // Scale particle size with speed
            var main = _dustSystems[i].main;
            float sizeScale = Mathf.Lerp(0.5f, 1.5f, speedNorm);
            main.startSize = new ParticleSystem.MinMaxCurve(particleSize * sizeScale * 0.5f, particleSize * sizeScale);
        }
    }
}
