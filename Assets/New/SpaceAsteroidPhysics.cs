using System.Collections;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Vật lý thiên thạch không gian & Hệ thống Bắn Nổ (Zero-G Asteroid Physics & Destructible):
    /// - Sử dụng Rigidbody thực tế (MeshCollider Convex) để va chạm vật lý với tàu người chơi và các thiên thạch khác.
    /// - Khi bị đạn Laser bắn trúng sẽ giật lùi theo lực đạn và nổ tung thành nhiều mảnh vỡ 3D khi hết máu (tự tái tạo sau 16s).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceAsteroidPhysics : MonoBehaviour
    {
        [Header("Chuyển Động & Tự Xoay (Zero-G Drift & Tumble)")]
        [Tooltip("Tốc độ trôi tự nhiên trong không gian")]
        public float driftSpeed = 2.2f;

        [Tooltip("Tốc độ tự xoay 3 trục (độ/giây)")]
        public float tumbleSpeed = 25f;

        [Header("Giới Hạn Quỹ Đạo (Orbital Boundary Tether)")]
        [Tooltip("Bán kính vùng hoạt động quanh vị trí gốc trước khi bị lực hút kéo nhẹ về")]
        public float roamRadius = 28f;

        [Tooltip("Độ mạnh lực hút kéo thiên thạch quay lại vùng quỹ đạo")]
        public float returnForce = 1.8f;

        [Tooltip("Vận tốc tối đa khi bị tàu đâm văng")]
        public float maxVelocity = 22f;

        [Header("Độ Bền & Nổ Tung (Destructible Asteroid)")]
        public float maxHealth = 90f;
        public float respawnDelay = 16f;

        private float _currentHealth;
        private bool _isDestroyed;
        private Rigidbody _rb;
        private Collider[] _colliders;
        private Renderer[] _renderers;
        private Vector3 _anchorPosition;
        private Vector3 _initialScale;
        private Vector3 _targetDriftVel;
        private Vector3 _targetAngularVel;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _anchorPosition = transform.position;
            _initialScale = transform.localScale;
            _currentHealth = maxHealth;

            var meshCol = GetComponent<MeshCollider>();
            if (meshCol == null) meshCol = gameObject.AddComponent<MeshCollider>();
            if (meshCol.sharedMesh == null)
            {
                var mf = GetComponentInChildren<MeshFilter>();
                if (mf != null) meshCol.sharedMesh = mf.sharedMesh;
            }
            meshCol.convex = true;

            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);

            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.linearDamping = 0.25f;
            _rb.angularDamping = 0.15f;

            float avgScale = (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3f;
            _rb.mass = Mathf.Clamp(avgScale * 3.5f, 5f, 80f);
        }

        private void Start()
        {
            int seed = Mathf.RoundToInt(_anchorPosition.x * 13f + _anchorPosition.y * 7f + _anchorPosition.z * 19f);
            Random.State oldState = Random.state;
            Random.InitState(seed);

            Vector3 driftDir = Random.onUnitSphere;
            driftDir.y *= 0.35f;
            _targetDriftVel = driftDir.normalized * Random.Range(driftSpeed * 0.5f, driftSpeed * 1.3f);

            _targetAngularVel = Random.onUnitSphere * (tumbleSpeed * Mathf.Deg2Rad * Random.Range(0.6f, 1.4f));

            Random.state = oldState;

            _rb.linearVelocity = _targetDriftVel;
            _rb.angularVelocity = _targetAngularVel;
        }

        private void FixedUpdate()
        {
            if (_isDestroyed || _rb == null || _rb.isKinematic) return;

            _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, _targetAngularVel, Time.fixedDeltaTime * 0.5f);

            Vector3 offset = _anchorPosition - transform.position;
            float dist = offset.magnitude;
            if (dist > roamRadius)
            {
                float excess = dist - roamRadius;
                _rb.AddForce(offset.normalized * (excess * returnForce), ForceMode.Acceleration);
            }
            else
            {
                _rb.AddForce((_targetDriftVel - _rb.linearVelocity) * 0.35f, ForceMode.Acceleration);
            }

            if (_rb.linearVelocity.sqrMagnitude > maxVelocity * maxVelocity)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxVelocity;
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDir)
        {
            if (_isDestroyed) return;

            _currentHealth -= damage;

            if (_rb != null && !_rb.isKinematic)
            {
                _rb.AddForceAtPosition(hitDir.normalized * 18f, hitPoint, ForceMode.Impulse);
            }

            if (_currentHealth <= 0f)
            {
                ExplodeAsteroid();
            }
        }

        private void ExplodeAsteroid()
        {
            _isDestroyed = true;
            float avgScale = (_initialScale.x + _initialScale.y + _initialScale.z) / 3f;
            Material mat = (_renderers != null && _renderers.Length > 0) ? _renderers[0].sharedMaterial : null;

            SpaceExplosionVFX.SpawnExplosion(transform.position, Mathf.Clamp(avgScale * 0.65f, 2f, 7f), true, mat);

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            SetVisibleAndSolid(false);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            yield return new WaitForSeconds(respawnDelay);

            transform.position = _anchorPosition;
            transform.localScale = Vector3.zero;
            _currentHealth = maxHealth;
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = _targetDriftVel;
                _rb.angularVelocity = _targetAngularVel;
            }
            SetVisibleAndSolid(true);

            // Hiệu ứng phóng to mượt khi tái tạo thiên thạch
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(Vector3.zero, _initialScale, t / 0.6f);
                yield return null;
            }
            transform.localScale = _initialScale;
            _isDestroyed = false;
        }

        private void SetVisibleAndSolid(bool active)
        {
            if (_renderers != null)
                foreach (var r in _renderers) if (r != null) r.enabled = active;
            if (_colliders != null)
                foreach (var c in _colliders) if (c != null) c.enabled = active;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isDestroyed || _rb == null) return;
            if (collision.relativeVelocity.sqrMagnitude > 1f)
            {
                Vector3 impulseTorque = Vector3.Cross(collision.GetContact(0).normal, collision.relativeVelocity) * 0.15f;
                _rb.AddTorque(impulseTorque, ForceMode.VelocityChange);
            }
        }
    }
}
