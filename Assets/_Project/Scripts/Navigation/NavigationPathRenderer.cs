using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodeDrive.Navigation
{
    /// <summary>
    /// Renders glowing 3D GPS navigation path lines on top of the road surface.
    /// Smooths sharp turns and animates texture scrolling along the route.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class NavigationPathRenderer : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private float lineWidth = 2.2f;
        [SerializeField] private float pathYOffset = 0.6f;
        [SerializeField] private Color startColor = new Color(0f, 0.95f, 1f, 1f); // Bright Neon Cyan
        [SerializeField] private Color endColor = new Color(1f, 0.85f, 0.1f, 1f);   // Bright Neon Gold
        [SerializeField] private float pulseSpeed = 2.5f;

        [Header("Smoothing")]
        [SerializeField] private bool enableSplineSmoothing = true;
        [SerializeField] private int pointsPerSegment = 6;

        private LineRenderer _lineRenderer;
        private Material _pathMaterial;
        private float _textureOffset;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        private void Start()
        {
            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnPathUpdated += HandlePathUpdated;
                RoadNavigationManager.Instance.OnPathCleared += HandlePathCleared;

                if (RoadNavigationManager.Instance.HasActivePath)
                {
                    HandlePathUpdated(RoadNavigationManager.Instance.CurrentPathWaypoints);
                }
            }
        }

        private void OnDestroy()
        {
            if (RoadNavigationManager.Instance != null)
            {
                RoadNavigationManager.Instance.OnPathUpdated -= HandlePathUpdated;
                RoadNavigationManager.Instance.OnPathCleared -= HandlePathCleared;
            }
        }

        private void SetupLineRenderer()
        {
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth * 0.85f;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.alignment = LineAlignment.View; // Face camera clearly
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.textureMode = LineTextureMode.Tile;
            _lineRenderer.sortingOrder = 100;
            _lineRenderer.numCornerVertices = 8; // Prevents giant miter joint spikes on sharp turns
            _lineRenderer.numCapVertices = 8;

            // Sprites/Default works 100% reliably in URP and Built-In Pipelines
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            _pathMaterial = new Material(shader);
            _pathMaterial.name = "GPS_Path_Material";
            _pathMaterial.color = Color.white;
            _pathMaterial.mainTexture = GenerateArrowTexture();
            _pathMaterial.mainTextureScale = new Vector2(1f / (lineWidth * 1.5f), 1f); // Keeps arrow proportions nice

            _lineRenderer.material = _pathMaterial;

            // Gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(endColor.a, 1f) }
            );
            _lineRenderer.colorGradient = gradient;
            _lineRenderer.enabled = false;
        }

        private Texture2D GenerateArrowTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color clear = new Color(1, 1, 1, 0);
            
            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size; // width, 0 to 1. Center is 0.5
                float distFromCenterV = Mathf.Abs(v - 0.5f);
                
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size; // length, 0 to 1.
                    
                    // Arrow pointing right (+U). Tip at U=0.8
                    float centerlineU = 0.8f - (distFromCenterV * 1.4f);
                    float thickness = 0.15f;
                    
                    if (u > centerlineU - thickness && u < centerlineU + thickness)
                    {
                        // Soft anti-aliased edge
                        float alpha = 1f - (Mathf.Abs(u - centerlineU) / thickness);
                        alpha = Mathf.SmoothStep(0, 1, alpha * 3f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            if (!_lineRenderer.enabled || _pathMaterial == null) return;

            // Texture UV scrolling effect (arrows moving forward)
            _textureOffset -= Time.deltaTime * pulseSpeed;
            _lineRenderer.material.mainTextureOffset = new Vector2(_textureOffset, 0f);
        }

        private void HandlePathUpdated(List<Vector3> rawWaypoints)
        {
            if (rawWaypoints == null || rawWaypoints.Count < 2)
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;

            List<Vector3> renderPoints = enableSplineSmoothing ? SmoothPath(rawWaypoints) : ElevatePoints(rawWaypoints);

            _lineRenderer.positionCount = renderPoints.Count;
            _lineRenderer.SetPositions(renderPoints.ToArray());
        }

        private void HandlePathCleared()
        {
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }

        private List<Vector3> ElevatePoints(List<Vector3> points)
        {
            List<Vector3> result = new List<Vector3>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                result.Add(new Vector3(points[i].x, points[i].y + pathYOffset, points[i].z));
            }
            return result;
        }

        private List<Vector3> SmoothPath(List<Vector3> waypoints)
        {
            if (waypoints.Count <= 2) return ElevatePoints(waypoints);

            List<Vector3> elevated = ElevatePoints(waypoints);
            List<Vector3> smoothed = new List<Vector3>();

            for (int i = 0; i < elevated.Count - 1; i++)
            {
                Vector3 p0 = i == 0 ? elevated[i] : elevated[i - 1];
                Vector3 p1 = elevated[i];
                Vector3 p2 = elevated[i + 1];
                Vector3 p3 = (i + 2 < elevated.Count) ? elevated[i + 2] : elevated[i + 1];

                for (int j = 0; j < pointsPerSegment; j++)
                {
                    float t = (float)j / pointsPerSegment;
                    Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                    smoothed.Add(point);
                }
            }

            smoothed.Add(elevated[elevated.Count - 1]);
            return smoothed;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
    }
}
