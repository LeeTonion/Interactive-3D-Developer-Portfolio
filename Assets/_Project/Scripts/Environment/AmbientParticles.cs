using UnityEngine;

namespace CodeDrive.Environment
{
    /// <summary>
    /// Creates dreamy ambient floating particles in the scene.
    /// Bruno Simon style: small glowing specks that gently drift upward,
    /// creating a magical/whimsical atmosphere like fireflies or dust motes.
    /// </summary>
    public class AmbientParticles : MonoBehaviour
    {
        [Header("Particle Settings")]
        [SerializeField] private int maxParticles = 300;
        [SerializeField] private float spawnRadius = 60f;
        [SerializeField] private float particleLifetime = 12f;
        [SerializeField] private float particleSpeed = 0.3f;
        [SerializeField] private float particleSize = 0.08f;

        [Header("Day/Night Colors")]
        [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.85f, 0.5f);      // Warm golden motes
        [SerializeField] private Color nightColor = new Color(0.4f, 0.7f, 1f, 0.6f);       // Cool blue fireflies
        [SerializeField] private Color duskColor = new Color(1f, 0.65f, 0.3f, 0.55f);      // Orange sunset embers

        [Header("Follow Camera")]
        [SerializeField] private bool followCamera = true;

        private ParticleSystem _ps;
        private Camera _mainCam;

        private void Start()
        {
            _mainCam = Camera.main;
            CreateParticleSystem();
        }

        private void CreateParticleSystem()
        {
            _ps = gameObject.AddComponent<ParticleSystem>();

            var main = _ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.7f, particleLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.3f, particleSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.4f, particleSize);
            main.startColor = dayColor;
            main.gravityModifier = -0.02f; // Gently float upward
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            // Emission
            var emission = _ps.emission;
            emission.rateOverTime = maxParticles / (particleLifetime * 0.5f);

            // Shape: large sphere around camera
            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = spawnRadius;

            // Size over lifetime: gentle pulsing
            var sizeOverLifetime = _ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.1f, 0.8f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(0.9f, 0.8f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime: fade in and out
            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.6f, 0.2f),
                    new GradientAlphaKey(0.6f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            // Noise for organic drifting movement
            var noise = _ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.1f;
            noise.damping = true;
            noise.octaveCount = 2;

            // Renderer
            var renderer = _ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            var mat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (mat != null)
            {
                mat.SetFloat("_Mode", 0); // Additive
                mat.color = dayColor;
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                renderer.material = mat;
            }
        }

        private void Update()
        {
            // Follow camera position
            if (followCamera && _mainCam != null)
            {
                transform.position = _mainCam.transform.position;
            }

            // Update color based on day/night cycle
            if (_ps != null && DayNightCycleManager.Instance != null)
            {
                float time = DayNightCycleManager.Instance.CurrentTimeOfDay;
                Color targetColor;

                if (time >= 0.25f && time < 0.7f)
                {
                    // Day time - warm golden motes
                    targetColor = dayColor;
                }
                else if (time >= 0.7f && time < 0.8f)
                {
                    // Dusk - orange sunset embers
                    float t = Mathf.InverseLerp(0.7f, 0.8f, time);
                    targetColor = Color.Lerp(dayColor, duskColor, t);
                }
                else if (time >= 0.8f || time < 0.15f)
                {
                    // Night - cool blue fireflies
                    targetColor = nightColor;
                }
                else
                {
                    // Dawn - transition from night to day
                    float t = Mathf.InverseLerp(0.15f, 0.25f, time);
                    targetColor = Color.Lerp(nightColor, dayColor, t);
                }

                var main = _ps.main;
                main.startColor = targetColor;
            }
        }
    }
}
