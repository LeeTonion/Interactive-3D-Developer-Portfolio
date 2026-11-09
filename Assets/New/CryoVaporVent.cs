using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Điều khiển van xả sương lạnh định kỳ (Periodic Cryo Vapor Venting) từ các buồng ngủ đông và bình dưỡng khí.
    /// </summary>
    public class CryoVaporVent : MonoBehaviour
    {
        [Header("Tham chiếu Particle System")]
        public ParticleSystem vaporParticle;
        public Light ventGlowLight;

        [Header("Khoảng thời gian xả định kỳ (Seconds)")]
        public float minInterval = 4.5f;
        public float maxInterval = 8.0f;
        public int burstCount = 20;

        private float _timer;
        private float _lightFadeTimer;

        private void Start()
        {
            _timer = Random.Range(1.5f, 4.0f);
            if (vaporParticle == null) vaporParticle = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _timer -= dt;

            if (_timer <= 0f)
            {
                _timer = Random.Range(minInterval, maxInterval);
                TriggerVentBurst();
            }

            if (_lightFadeTimer > 0f)
            {
                _lightFadeTimer -= dt;
                if (ventGlowLight != null)
                {
                    ventGlowLight.intensity = Mathf.Lerp(ventGlowLight.intensity, 0.4f, dt * 3f);
                }
            }
        }

        public void TriggerVentBurst()
        {
            if (vaporParticle != null)
            {
                vaporParticle.Emit(burstCount);
            }
            if (ventGlowLight != null)
            {
                ventGlowLight.intensity = 2.5f;
                _lightFadeTimer = 0.8f;
            }
        }
    }
}

