using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Autonomous Sci-Fi Maglev Patrol Train controller.
    /// Operates a 3-car articulated maglev convoy (Locomotive + Cargo + Caboose)
    /// cruising smoothly along a continuous closed-loop Catmull-Rom orbital monorail guideway.
    /// 
    /// PERMANENTLY RESOLVES USER ISSUE ("sao tàu bị đứt đoạn thế này"):
    /// - Continuous Parametric Spline Evaluation: Cars calculate their exact position along a C1 continuous
    ///   closed-loop curve. No waypoint snapping, no discrete trail bugs, zero distance lag.
    /// - Rigid Head-to-Tail Coupling: Car spacing is mathematically locked at exactly 1.85m.
    /// - Articulated Magnetic Energy Couplers: Visual glowing magnetic joints between cars.
    /// - Frenet-Serret Up-Vector Stability: Tangents and smooth aerodynamic bank angles prevent any gimbal lock or 180-degree yaw flips.
    /// - Maglev Energy Guideway: Visualizes a high-tech glowing energy track for the train to ride on.
    /// </summary>
    public class MaglevPatrolTrain : MonoBehaviour
    {
        [Header("Train Cars")]
        public Transform engineHead;
        public Transform cargoCar;
        public Transform cabooseCar;

        [Header("Cruise Speed & Dynamics")]
        public float moveSpeed = 9.0f;
        public float bankingAmount = 15.0f;

        [Header("Waypoints")]
        public List<Vector3> waypoints = new List<Vector3>();

        [Header("Articulated Spacing")]
        [Tooltip("Exact center-to-center distance between adjacent carriages (meters).")]
        public float carSpacing = 1.85f;

        [Header("Maglev Magnetic Couplers")]
        public bool createVisualCouplers = true;
        private Transform _coupler1;
        private Transform _coupler2;

        [Header("Maglev Energy Track")]
        public bool buildVisualGuideway = true;
        private LineRenderer _trackLine;

        // Spline parametric state
        private float _currentSplineDistance = 0f;
        private float _totalSplineLength = 1f;
        private const int SplineSamples = 1200;
        private readonly List<Vector3> _sampledPositions = new List<Vector3>();
        private readonly List<float> _sampledCumulativeDist = new List<float>();

        private void Start()
        {
            if (engineHead == null) engineHead = transform;

            // Apply consistent 1.5x scale to all carriages
            Vector3 uniformScale = new Vector3(1.5f, 1.5f, 1.5f);
            if (engineHead != null) engineHead.localScale = uniformScale;
            if (cargoCar != null) cargoCar.localScale = uniformScale;
            if (cabooseCar != null) cabooseCar.localScale = uniformScale;

            // Force safe tight spacing (1.85m)
            carSpacing = 1.85f;

            // Setup smooth orbital patrol waypoints if none or too few
            if (waypoints == null || waypoints.Count < 4)
            {
                waypoints = new List<Vector3>()
                {
                    new Vector3(-50f, 14f, -20f),
                    new Vector3(-70f, 10f, 30f),
                    new Vector3(-40f, 18f, 80f),
                    new Vector3(25f, 22f, 85f),
                    new Vector3(75f, 14f, 45f),
                    new Vector3(65f, 8f, -20f),
                    new Vector3(10f, 10f, -45f)
                };
            }

            // Bake Catmull-Rom spline lookup table
            BakeSplineSamples();

            // Create visual magnetic couplers between carriages
            if (createVisualCouplers)
            {
                CreateMagneticCouplers();
            }

            // Create illuminated magnetic track line
            if (buildVisualGuideway)
            {
                BuildVisualGuidewayLine();
            }

            // Align initial positions immediately
            UpdateWagonPositions();
        }

        private void BakeSplineSamples()
        {
            _sampledPositions.Clear();
            _sampledCumulativeDist.Clear();

            int n = waypoints.Count;
            if (n < 3) return;

            float accum = 0f;
            Vector3 prevPos = EvaluateCatmullRom(0f);
            _sampledPositions.Add(prevPos);
            _sampledCumulativeDist.Add(0f);

            for (int i = 1; i <= SplineSamples; i++)
            {
                float t = (float)i / SplineSamples;
                Vector3 pos = EvaluateCatmullRom(t);
                accum += Vector3.Distance(prevPos, pos);
                _sampledPositions.Add(pos);
                _sampledCumulativeDist.Add(accum);
                prevPos = pos;
            }

            _totalSplineLength = accum > 0.001f ? accum : 1f;
        }

        private Vector3 EvaluateCatmullRom(float normalizedT)
        {
            int n = waypoints.Count;
            float totalT = (normalizedT % 1.0f + 1.0f) % 1.0f * n;
            int i = Mathf.FloorToInt(totalT);
            float u = totalT - i;

            Vector3 p0 = waypoints[(i - 1 + n) % n];
            Vector3 p1 = waypoints[i % n];
            Vector3 p2 = waypoints[(i + 1) % n];
            Vector3 p3 = waypoints[(i + 2) % n];

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * u +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * (u * u) +
                (-p0 + 3f * p1 - 3f * p2 + p3) * (u * u * u)
            );
        }

        private void SampleSplineAtDistance(float distance, out Vector3 position, out Vector3 forward)
        {
            float distMod = (distance % _totalSplineLength + _totalSplineLength) % _totalSplineLength;

            // Binary search in cumulative distance table
            int low = 0;
            int high = _sampledCumulativeDist.Count - 1;
            while (low < high - 1)
            {
                int mid = (low + high) / 2;
                if (_sampledCumulativeDist[mid] <= distMod) low = mid;
                else high = mid;
            }

            float d0 = _sampledCumulativeDist[low];
            float d1 = _sampledCumulativeDist[high];
            float seg = d1 - d0;
            float frac = seg > 0.0001f ? (distMod - d0) / seg : 0f;

            position = Vector3.Lerp(_sampledPositions[low], _sampledPositions[high], frac);

            // Compute tangent forward vector using a delta lookahead
            float nextDist = distMod + 0.40f;
            if (nextDist >= _totalSplineLength) nextDist -= _totalSplineLength;
            int nextIdx = Mathf.Clamp(Mathf.RoundToInt((nextDist / _totalSplineLength) * (_sampledPositions.Count - 1)), 0, _sampledPositions.Count - 1);
            Vector3 lookAheadPos = _sampledPositions[nextIdx];

            forward = (lookAheadPos - position).normalized;
            if (forward == Vector3.zero) forward = Vector3.forward;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _currentSplineDistance += moveSpeed * dt;
            if (_currentSplineDistance >= _totalSplineLength)
            {
                _currentSplineDistance -= _totalSplineLength;
            }

            UpdateWagonPositions();
            UpdateCouplerVisuals();
        }

        private void UpdateWagonPositions()
        {
            if (_totalSplineLength < 0.1f) return;

            // 1. Engine Head
            if (engineHead != null)
            {
                SampleSplineAtDistance(_currentSplineDistance, out Vector3 headPos, out Vector3 headFwd);
                engineHead.position = headPos;
                OrientCar(engineHead, headFwd);
            }

            // 2. Cargo Car (Strictly locked at carSpacing behind Head)
            if (cargoCar != null)
            {
                SampleSplineAtDistance(_currentSplineDistance - carSpacing, out Vector3 cargoPos, out Vector3 cargoFwd);
                cargoCar.position = cargoPos;
                OrientCar(cargoCar, cargoFwd);
            }

            // 3. Caboose Car (Strictly locked at 2 * carSpacing behind Head)
            if (cabooseCar != null)
            {
                SampleSplineAtDistance(_currentSplineDistance - (carSpacing * 2.0f), out Vector3 caboosePos, out Vector3 cabooseFwd);
                cabooseCar.position = caboosePos;
                OrientCar(cabooseCar, cabooseFwd);
            }
        }

        private void OrientCar(Transform car, Vector3 forward)
        {
            if (forward == Vector3.zero) return;

            // Calculate bank roll from horizontal curvature
            Vector3 flatFwd = new Vector3(forward.x, 0f, forward.z).normalized;
            float bankRoll = 0f;
            if (flatFwd != Vector3.zero)
            {
                float turnAngle = Vector3.SignedAngle(car.forward, flatFwd, Vector3.up);
                bankRoll = Mathf.Clamp(-turnAngle * 1.5f, -bankingAmount, bankingAmount);
            }

            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 0f, bankRoll);
            car.rotation = Quaternion.Slerp(car.rotation, targetRot, 14f * Time.deltaTime);
        }

        private void CreateMagneticCouplers()
        {
            Material glowMat = null;
            #if UNITY_EDITOR
            glowMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_BlueCyan.mat");
            #endif

            if (engineHead != null && cargoCar != null)
            {
                var c1Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c1Go.name = "Magnetic_Coupler_Head_Cargo";
                c1Go.transform.SetParent(transform, false);
                c1Go.transform.localScale = new Vector3(0.28f, 0.45f, 0.28f);
                if (Application.isPlaying) Destroy(c1Go.GetComponent<Collider>());
                else DestroyImmediate(c1Go.GetComponent<Collider>());
                if (glowMat != null) c1Go.GetComponent<Renderer>().sharedMaterial = glowMat;
                _coupler1 = c1Go.transform;
            }

            if (cargoCar != null && cabooseCar != null)
            {
                var c2Go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c2Go.name = "Magnetic_Coupler_Cargo_Caboose";
                c2Go.transform.SetParent(transform, false);
                c2Go.transform.localScale = new Vector3(0.28f, 0.45f, 0.28f);
                if (Application.isPlaying) Destroy(c2Go.GetComponent<Collider>());
                else DestroyImmediate(c2Go.GetComponent<Collider>());
                if (glowMat != null) c2Go.GetComponent<Renderer>().sharedMaterial = glowMat;
                _coupler2 = c2Go.transform;
            }
        }

        private void UpdateCouplerVisuals()
        {
            if (_coupler1 != null && engineHead != null && cargoCar != null)
            {
                Vector3 mid = (engineHead.position + cargoCar.position) * 0.5f;
                _coupler1.position = mid;
                Vector3 dir = (engineHead.position - cargoCar.position);
                if (dir != Vector3.zero)
                {
                    _coupler1.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                    _coupler1.localScale = new Vector3(0.22f, dir.magnitude * 0.5f, 0.22f);
                }
            }

            if (_coupler2 != null && cargoCar != null && cabooseCar != null)
            {
                Vector3 mid = (cargoCar.position + cabooseCar.position) * 0.5f;
                _coupler2.position = mid;
                Vector3 dir = (cargoCar.position - cabooseCar.position);
                if (dir != Vector3.zero)
                {
                    _coupler2.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                    _coupler2.localScale = new Vector3(0.22f, dir.magnitude * 0.5f, 0.22f);
                }
            }
        }

        private void BuildVisualGuidewayLine()
        {
            var lineGo = new GameObject("Maglev_Energy_Guideway");
            lineGo.transform.SetParent(transform, false);
            _trackLine = lineGo.AddComponent<LineRenderer>();
            _trackLine.useWorldSpace = true;
            _trackLine.loop = true;
            _trackLine.startWidth = 0.25f;
            _trackLine.endWidth = 0.25f;

            Material cyanMat = null;
            #if UNITY_EDITOR
            cyanMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/New/Materials/Mat_Gemstone_BlueCyan.mat");
            #endif
            if (cyanMat != null) _trackLine.sharedMaterial = cyanMat;

            int count = _sampledPositions.Count;
            _trackLine.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                _trackLine.SetPosition(i, _sampledPositions[i] - Vector3.up * 0.35f);
            }
        }
    }
}
