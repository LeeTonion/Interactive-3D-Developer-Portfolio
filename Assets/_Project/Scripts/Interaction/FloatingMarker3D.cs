using UnityEngine;
using UnityEngine.EventSystems;

namespace CodeDrive.Interaction
{
    /// <summary>
    /// Creates a smooth floating/bobbing motion and ensures speech bubbles/badges
    /// always face the active camera directly (billboarding), plus supports mouse click interaction.
    /// Enhanced with glow pulse, diamond spin, and proximity scaling.
    /// </summary>
    public class FloatingMarker3D : MonoBehaviour, IPointerClickHandler
    {
        [Header("Bobbing Motion")]
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobSpeed = 2.5f;

        [Header("Billboard to Camera")]
        [Tooltip("When enabled, this marker always faces the camera directly and never spins backwards")]
        [SerializeField] private bool billboardToCamera = true;

        [Header("Diamond Spin")]
        [Tooltip("The diamond mesh child to spin independently (while the label stays billboarded)")]
        [SerializeField] private Transform diamondMesh;
        [SerializeField] private float spinSpeed = 90f;

        [Header("Glow Pulse")]
        [Tooltip("Pulsing emission intensity on the diamond marker material")]
        [SerializeField] private float glowPulseSpeed = 3f;
        [SerializeField] private float glowMinIntensity = 0.5f;
        [SerializeField] private float glowMaxIntensity = 2.5f;
        [SerializeField] private Color glowColor = new Color(0.3f, 0.9f, 1f);

        [Header("Proximity Scale")]
        [Tooltip("Scale up subtly when camera is closer")]
        [SerializeField] private bool enableProximityScale = true;
        [SerializeField] private float minScale = 0.8f;
        [SerializeField] private float maxScale = 1.3f;
        [SerializeField] private float nearDistance = 15f;
        [SerializeField] private float farDistance = 80f;

        [Header("Speech Bubble Hover Pop-up")]
        [Tooltip("The speech bubble canvas/sign that pops up when hovered")]
        [SerializeField] private Transform speechBubbleRoot;
        [SerializeField] private float bubblePopSpeed = 10f;
        [SerializeField] private float hoverScaleBonus = 1.25f;

        [Header("Parent Booth Reference")]
        [SerializeField] private ShowcaseBooth3D parentBooth;

        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;
        private Vector3 _bubbleDefaultLocalScale = new Vector3(0.02f, 0.02f, 0.02f);
        private Vector3 _diamondInitialScale = Vector3.one;
        private Camera _mainCam;
        private Renderer _diamondRenderer;
        private MaterialPropertyBlock _propBlock;
        private bool _isHovered;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool IsHovered => _isHovered;

        private void Awake()
        {
            if (parentBooth == null)
                parentBooth = GetComponentInParent<ShowcaseBooth3D>();

            // Auto-find diamond mesh if not assigned
            if (diamondMesh == null)
            {
                var t = transform.Find("DiamondMarker");
                if (t != null) diamondMesh = t;
            }

            if (diamondMesh != null)
            {
                _diamondRenderer = diamondMesh.GetComponent<Renderer>();
                _diamondInitialScale = diamondMesh.localScale;
            }

            // Auto-find speech bubble sign
            if (speechBubbleRoot == null)
            {
                var t = transform.Find("SpeechBubbleCanvas");
                if (t != null) speechBubbleRoot = t;
            }

            if (speechBubbleRoot != null)
            {
                _bubbleDefaultLocalScale = speechBubbleRoot.localScale;
                if (_bubbleDefaultLocalScale.sqrMagnitude < 0.00001f)
                    _bubbleDefaultLocalScale = new Vector3(0.02f, 0.02f, 0.02f);

                // Hidden by default: only the floating point is visible!
                speechBubbleRoot.localScale = Vector3.zero;
            }

            _propBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalScale = transform.localScale;
            _mainCam = Camera.main;

            // Ensure emission is enabled on the diamond material
            if (_diamondRenderer != null && _diamondRenderer.sharedMaterial != null)
            {
                _diamondRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }

        private void Update()
        {
            if (parentBooth != null && parentBooth.IsFocused)
            {
                _isHovered = false;
                if (speechBubbleRoot != null) speechBubbleRoot.localScale = Vector3.zero;
                return;
            }

            CheckMouseHover();
            UpdateBubbleAndHoverVisuals();
        }

        private void CheckMouseHover()
        {
            if (parentBooth != null && parentBooth.IsFocused)
            {
                _isHovered = false;
                return;
            }
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            Vector2 mousePos = Vector2.zero;
            bool hasMouse = false;

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                hasMouse = true;
            }
            if (!hasMouse)
            {
                mousePos = UnityEngine.Input.mousePosition;
                hasMouse = true;
            }

            if (hasMouse)
            {
                Ray ray = _mainCam.ScreenPointToRay(mousePos);
                var hits = Physics.RaycastAll(ray, 400f, ~0, QueryTriggerInteraction.Collide);
                _isHovered = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform.IsChildOf(transform))
                    {
                        _isHovered = true;
                        break;
                    }
                }
            }
        }

        private void UpdateBubbleAndHoverVisuals()
        {
            // 1. Smoothly pop up or shrink the speech bubble sign
            if (speechBubbleRoot != null)
            {
                Vector3 targetBubbleScale = _isHovered ? _bubbleDefaultLocalScale : Vector3.zero;
                speechBubbleRoot.localScale = Vector3.Lerp(speechBubbleRoot.localScale, targetBubbleScale, Time.deltaTime * bubblePopSpeed);
                if (!_isHovered && speechBubbleRoot.localScale.sqrMagnitude < 0.000001f)
                {
                    speechBubbleRoot.localScale = Vector3.zero;
                }
            }

            // 2. Expand diamond subtly when hovered
            if (diamondMesh != null)
            {
                Vector3 targetDiamondScale = _isHovered ? _diamondInitialScale * hoverScaleBonus : _diamondInitialScale;
                diamondMesh.localScale = Vector3.Lerp(diamondMesh.localScale, targetDiamondScale, Time.deltaTime * bubblePopSpeed);
            }
        }

        private void LateUpdate()
        {
            // --- Vertical bobbing ---
            float newY = _initialLocalPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(_initialLocalPosition.x, newY, _initialLocalPosition.z);

            // --- Billboard: entire marker root faces camera directly (no mirroring) ---
            if (billboardToCamera)
            {
                if (_mainCam == null) _mainCam = Camera.main;
                if (_mainCam != null)
                {
                    transform.rotation = _mainCam.transform.rotation;
                }
            }

            // --- Diamond Spin (independent of billboard) ---
            if (diamondMesh != null)
            {
                diamondMesh.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            }

            // --- Glow Pulse (intensifies when hovered) ---
            if (_diamondRenderer != null)
            {
                float basePulse = Mathf.Lerp(glowMinIntensity, glowMaxIntensity,
                    (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);
                float hoverBoost = _isHovered ? 1.5f : 1.0f;
                Color emissionColor = glowColor * (basePulse * hoverBoost);

                _diamondRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(EmissionColorId, emissionColor);
                _diamondRenderer.SetPropertyBlock(_propBlock);
            }

            // --- Proximity Scale ---
            if (enableProximityScale && _mainCam != null)
            {
                float dist = Vector3.Distance(_mainCam.transform.position, transform.position);
                float t = Mathf.InverseLerp(nearDistance, farDistance, dist);
                float scaleFactor = Mathf.Lerp(maxScale, minScale, t);
                transform.localScale = _initialLocalScale * scaleFactor;
            }
        }

        private void OnMouseDown()
        {
            TriggerFocus();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerFocus();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        private void TriggerFocus()
        {
            if (parentBooth != null)
            {
                parentBooth.EnterFocusView();
            }
        }
    }
}
