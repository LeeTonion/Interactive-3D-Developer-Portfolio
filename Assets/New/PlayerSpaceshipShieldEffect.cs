using System.Collections;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Hiệu ứng Khiên Năng Lượng 3D Lưới Lục Giác & Hiệu Ứng Biến Mất (Dissolve Effect) cho Phi Thuyền:
    /// - Quả cầu khiên 3D bao trọn thân tàu với lưới tổ ong phát sáng Fresnel HDR.
    /// - Khi bị trúng đạn: Tạo sóng xung kích phản hồi tại điểm va chạm, nháy sáng bảo vệ thân tàu.
    /// - Khi hết khiên (Shield Break): Kích hoạt hiệu ứng Dissolve phân rã bề mặt với viền phát sáng rực rỡ biến mất hoàn toàn.
    /// - Khi khiên hồi phục: Hiệu ứng Dissolve đảo ngược, quét viền năng lượng tái tạo lại khiên bảo vệ.
    /// </summary>
    public class PlayerSpaceshipShieldEffect : MonoBehaviour
    {
        [Header("Cấu Hình Vật Liệu Khiên (Hex Shield Material)")]
        public Shader shieldShader;
        public Material shieldMaterial;
        public Texture2D hexGridTexture;
        public Vector3 shieldScale = new Vector3(5.4f, 3.45f, 7.5f);
        public Vector3 shieldOffset = new Vector3(0f, 0.60f, 0.30f);

        [Header("Màu Sắc Khiên (HDR Colors)")]
        [ColorUsage(true, true)] public Color normalColor = new Color(0.15f, 0.85f, 1.0f, 1.0f);
        [ColorUsage(true, true)] public Color damagedColor = new Color(1.2f, 0.25f, 0.75f, 2.5f);
        [ColorUsage(true, true)] public Color dissolveBurnColor = new Color(0.35f, 2.2f, 4.0f, 3.0f);

        private GameObject _shieldBubble;
        private MeshRenderer _shieldRenderer;
        private Material _instancedMat;
        private Coroutine _dissolveRoutine;
        private Coroutine _hitWaveRoutine;
        private float _currentDissolveAmount = 0f;
        private bool _isDissolved = false;

        public bool IsDissolved => _isDissolved;
        public float DissolveAmount => _currentDissolveAmount;

        private void Awake()
        {
            EnsureShieldBubble();
        }

        private void Start()
        {
            EnsureShieldBubble();
        }

        /// <summary>
        /// Tự động khởi tạo quả cầu khiên 3D quanh phi thuyền nếu chưa có
        /// </summary>
        public void EnsureShieldBubble()
        {
            if (_shieldBubble != null && _shieldRenderer != null) return;

            var existing = transform.Find("Player_HexShield_Bubble");
            if (existing != null)
            {
                _shieldBubble = existing.gameObject;
                _shieldRenderer = _shieldBubble.GetComponent<MeshRenderer>();
            }
            else
            {
                _shieldBubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _shieldBubble.name = "Player_HexShield_Bubble";
                _shieldBubble.transform.SetParent(transform, false);
                _shieldBubble.transform.localPosition = shieldOffset;
                _shieldBubble.transform.localRotation = Quaternion.identity;
                _shieldBubble.transform.localScale = shieldScale;

                var col = _shieldBubble.GetComponent<Collider>();
                if (col != null) Destroy(col);

                _shieldRenderer = _shieldBubble.GetComponent<MeshRenderer>();
                _shieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _shieldRenderer.receiveShadows = false;
            }

            if (shieldShader == null)
            {
                shieldShader = Shader.Find("SpaceCyberpunk/SciFiHexShieldDissolve");
            }

            if (hexGridTexture == null)
            {
#if UNITY_EDITOR
                hexGridTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/New/Textures/Tex_SciFi_HexGrid.png");
#endif
            }

            if (_instancedMat == null && shieldShader != null)
            {
                _instancedMat = new Material(shieldShader);
                _instancedMat.name = "PlayerShield_Instanced";
                if (hexGridTexture != null) _instancedMat.SetTexture("_HexTex", hexGridTexture);
                _shieldRenderer.sharedMaterial = _instancedMat;
            }
            else if (_shieldRenderer.sharedMaterial != null)
            {
                _instancedMat = _shieldRenderer.material;
                if (hexGridTexture != null) _instancedMat.SetTexture("_HexTex", hexGridTexture);
            }

            SetDissolveInstant(0f);
        }

        /// <summary>
        /// Kích hoạt khi tàu nhận sát thương trúng khiên
        /// </summary>
        public void OnShieldHit(Vector3 worldHitPos, float shieldRatio)
        {
            EnsureShieldBubble();
            if (_shieldRenderer == null || _instancedMat == null) return;

            // Nếu khiên đang bị dissolve thì bật lại
            if (_isDissolved && shieldRatio > 0.05f)
            {
                TriggerShieldRestore();
            }

            if (_hitWaveRoutine != null) StopCoroutine(_hitWaveRoutine);
            _hitWaveRoutine = StartCoroutine(HitImpactRoutine(worldHitPos, shieldRatio));
        }

        private IEnumerator HitImpactRoutine(Vector3 worldHitPos, float shieldRatio)
        {
            Vector3 localHit = transform.InverseTransformPoint(worldHitPos);
            _instancedMat.SetVector("_HitPos", new Vector4(localHit.x, localHit.y, localHit.z, 1f));

            // Màu hồng tím Magenta rực rỡ chuẩn ảnh 1 khi bị bắn trúng
            Color currentColor = Color.Lerp(damagedColor, new Color(1.2f, 0.2f, 0.8f, 2.5f), 0.7f);
            _instancedMat.SetColor("_HitColor", currentColor);
            _instancedMat.SetColor("_RimColor", currentColor * 1.2f);
            _instancedMat.SetColor("_HexLineColor", currentColor * 1.5f);

            float duration = 0.65f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float intensity = Mathf.Lerp(1.0f, 0.0f, t);
                float radius = Mathf.Lerp(0.3f, 6.5f, t);

                _instancedMat.SetFloat("_HitIntensity", intensity);
                _instancedMat.SetFloat("_HitWaveProgress", radius);
                yield return null;
            }

            _instancedMat.SetFloat("_HitIntensity", 0f);
            _hitWaveRoutine = null;
        }

        /// <summary>
        /// Kích hoạt hiệu ứng Dissolve làm biến mất khiên khi SHD cạn kiệt (Shield Break)
        /// </summary>
        public void TriggerShieldDissolve(float duration = 0.85f)
        {
            EnsureShieldBubble();
            if (_dissolveRoutine != null) StopCoroutine(_dissolveRoutine);
            _dissolveRoutine = StartCoroutine(DissolveOutRoutine(duration));
        }

        /// <summary>
        /// Kích hoạt hiệu ứng tái tạo khiên khi SHD bắt đầu hồi phục (Shield Rebuild)
        /// </summary>
        public void TriggerShieldRestore(float duration = 0.85f)
        {
            EnsureShieldBubble();
            if (_dissolveRoutine != null) StopCoroutine(_dissolveRoutine);
            _dissolveRoutine = StartCoroutine(DissolveInRoutine(duration));
        }

        private IEnumerator DissolveOutRoutine(float duration)
        {
            _isDissolved = true;
            if (_shieldBubble != null) _shieldBubble.SetActive(true);

            float startVal = _currentDissolveAmount;
            float targetVal = 1.0f;
            float elapsed = 0f;

            // Đổi màu viền cháy sang màu cam đỏ khi khiên bị quá tải vỡ
            if (_instancedMat != null)
            {
                _instancedMat.SetColor("_DissolveEdgeColor", new Color(1.0f, 0.35f, 0.15f, 3.0f));
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                // Cubic ease-in-out curve
                float ease = progress * progress * (3f - 2f * progress);
                SetDissolveProgress(Mathf.Lerp(startVal, targetVal, ease));
                yield return null;
            }

            SetDissolveProgress(1.0f);
            if (_shieldBubble != null) _shieldBubble.SetActive(false);
            _dissolveRoutine = null;
        }

        private IEnumerator DissolveInRoutine(float duration)
        {
            if (_shieldBubble != null) _shieldBubble.SetActive(true);
            _isDissolved = false;

            float startVal = _currentDissolveAmount;
            float targetVal = 0.0f;
            float elapsed = 0f;

            // Màu viền tái tạo năng lượng xanh Cyan rực rỡ
            if (_instancedMat != null)
            {
                _instancedMat.SetColor("_DissolveEdgeColor", dissolveBurnColor);
                _instancedMat.SetColor("_RimColor", normalColor);
                _instancedMat.SetColor("_HexLineColor", normalColor * 1.5f);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float ease = progress * progress * (3f - 2f * progress);
                SetDissolveProgress(Mathf.Lerp(startVal, targetVal, ease));
                yield return null;
            }

            SetDissolveProgress(0.0f);
            _dissolveRoutine = null;
        }

        public void SetDissolveInstant(float amount)
        {
            amount = Mathf.Clamp01(amount);
            _currentDissolveAmount = amount;
            _isDissolved = amount >= 0.999f;
            if (_instancedMat != null)
            {
                _instancedMat.SetFloat("_DissolveAmount", amount);
            }
            if (_shieldBubble != null)
            {
                _shieldBubble.SetActive(amount < 0.999f);
            }
        }

        private void SetDissolveProgress(float amount)
        {
            _currentDissolveAmount = Mathf.Clamp01(amount);
            if (_instancedMat != null)
            {
                _instancedMat.SetFloat("_DissolveAmount", _currentDissolveAmount);
            }
        }
    }
}
