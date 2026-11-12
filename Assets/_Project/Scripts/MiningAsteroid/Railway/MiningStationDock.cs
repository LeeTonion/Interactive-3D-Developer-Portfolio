using System.Collections.Generic;
using UnityEngine;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Represents a mining station / platform along the railway circuit.
    /// Handles train docking, deceleration, gemstone loading animation with particle emitters, and clearance.
    /// </summary>
    public class MiningStationDock : MonoBehaviour
    {
        [Header("Station Identification")]
        public string stationName = "Mining Terminal";
        public float circuitDistance = 0f; // Distance along the railway where the train stops
        public float stopTolerance = 1.8f;
        [Tooltip("Optional list of train IDs allowed to stop here automatically. Leave empty to allow any train.")]
        public string[] acceptedTrainIDs;

        [Header("Station Operations")]
        public float dockingDuration = 4.0f;
        public float loadingAnimationDuration = 2.5f;

        [Header("Visual Effects & Mineral Transfer")]
        public ParticleSystem gemstoneLoadingParticles;
        public Light stationIndicatorLight;
        public Color idleStatusColor = new Color(0f, 0.75f, 1f, 1f);
        public Color loadingStatusColor = new Color(0.95f, 0.8f, 0.15f, 1f);
        public Color departStatusColor = new Color(0.2f, 1f, 0.4f, 1f);
        [Range(4, 32)] public int mineralParticleCount = 16;
        [Range(1f, 12f)] public float particleTravelSpeed = 4.6f;
        public Color mineralOrbColor = new Color(0.15f, 0.96f, 1f, 1f);

        [Header("Machinery / Gantry Animations")]
        public Transform loaderArm;
        private Vector3 _initialArmRot;
        private bool _isLoadingActive = false;
        private Transform _activeTargetCargo;
        private float _spawnTimer;
        private int _spawnedThisCycle;

        public OrbitalMiningTrain ReservedByTrain { get; private set; }
        public bool IsOccupied => ReservedByTrain != null;
        public bool IsLoadingActive => _isLoadingActive;

        private struct MineralDroplet
        {
            public Transform transform;
            public Vector3 startPos;
            public float progress;
            public bool active;
        }
        private readonly List<MineralDroplet> _dropletPool = new List<MineralDroplet>(20);
        private static Material _sharedDropletMat;

        private void Start()
        {
            if (loaderArm != null)
            {
                _initialArmRot = loaderArm.localEulerAngles;
            }

            SetStationState(StationState.Idle);
        }

        public bool CanAcceptTrain(OrbitalMiningTrain train)
        {
            if (train == null) return false;
            if (IsOccupied && ReservedByTrain != train) return false;
            if (train.IsCalledToStation(this)) return true;

            if (acceptedTrainIDs == null || acceptedTrainIDs.Length == 0) return true;
            for (int i = 0; i < acceptedTrainIDs.Length; i++)
            {
                if (!string.IsNullOrEmpty(acceptedTrainIDs[i]) &&
                    string.Equals(acceptedTrainIDs[i], train.trainID, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryReserve(OrbitalMiningTrain train, Transform cargoTarget = null)
        {
            if (!CanAcceptTrain(train)) return false;
            ReservedByTrain = train;
            _activeTargetCargo = cargoTarget;
            _spawnedThisCycle = 0;
            return true;
        }

        public void ReleaseReservation(OrbitalMiningTrain train)
        {
            if (ReservedByTrain == train)
            {
                ReservedByTrain = null;
                _activeTargetCargo = null;
            }
        }

        public void SetCargoTarget(Transform target)
        {
            _activeTargetCargo = target;
        }

        public enum StationState
        {
            Idle,
            Loading,
            Clearing
        }

        public void SetStationState(StationState state)
        {
            switch (state)
            {
                case StationState.Idle:
                    _isLoadingActive = false;
                    if (stationIndicatorLight != null)
                    {
                        stationIndicatorLight.color = idleStatusColor;
                        stationIndicatorLight.intensity = 2.0f;
                    }
                    if (gemstoneLoadingParticles != null && gemstoneLoadingParticles.isPlaying)
                    {
                        gemstoneLoadingParticles.Stop();
                    }
                    break;

                case StationState.Loading:
                    _isLoadingActive = true;
                    if (stationIndicatorLight != null)
                    {
                        stationIndicatorLight.color = loadingStatusColor;
                        stationIndicatorLight.intensity = 3.5f;
                    }
                    if (gemstoneLoadingParticles != null && !gemstoneLoadingParticles.isPlaying)
                    {
                        gemstoneLoadingParticles.Play();
                    }
                    break;

                case StationState.Clearing:
                    _isLoadingActive = false;
                    if (stationIndicatorLight != null)
                    {
                        stationIndicatorLight.color = departStatusColor;
                        stationIndicatorLight.intensity = 2.8f;
                    }
                    if (gemstoneLoadingParticles != null && gemstoneLoadingParticles.isPlaying)
                    {
                        gemstoneLoadingParticles.Stop();
                    }
                    break;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_isLoadingActive && loaderArm != null)
            {
                float angle = Mathf.Sin(Time.time * 3.5f) * 14f;
                loaderArm.localEulerAngles = _initialArmRot + new Vector3(0f, 0f, angle);
            }
            else if (loaderArm != null)
            {
                loaderArm.localRotation = Quaternion.Slerp(loaderArm.localRotation, Quaternion.Euler(_initialArmRot), dt * 4f);
            }

            UpdateMineralStream(dt);
        }

        private void UpdateMineralStream(float dt)
        {
            if (_isLoadingActive && _spawnedThisCycle < mineralParticleCount)
            {
                _spawnTimer -= dt;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = Mathf.Max(0.08f, loadingAnimationDuration / Mathf.Max(4, mineralParticleCount));
                    SpawnMineralOrb();
                    _spawnedThisCycle++;
                }
            }

            Vector3 targetPos = _activeTargetCargo != null
                ? _activeTargetCargo.position + Vector3.up * 0.25f
                : (transform.position + Vector3.up * 0.4f);

            for (int i = 0; i < _dropletPool.Count; i++)
            {
                var d = _dropletPool[i];
                if (!d.active || d.transform == null) continue;

                float travelDur = Mathf.Max(0.25f, Vector3.Distance(d.startPos, targetPos) / Mathf.Max(1f, particleTravelSpeed));
                d.progress += dt / travelDur;

                if (d.progress >= 1f)
                {
                    d.active = false;
                    d.transform.gameObject.SetActive(false);
                }
                else
                {
                    Vector3 linear = Vector3.Lerp(d.startPos, targetPos, d.progress);
                    float arc = Mathf.Sin(d.progress * Mathf.PI) * 0.85f;
                    d.transform.position = linear + Vector3.up * arc;
                }
                _dropletPool[i] = d;
            }
        }

        private void SpawnMineralOrb()
        {
            MineralDroplet? freeSlot = null;
            int freeIdx = -1;
            for (int i = 0; i < _dropletPool.Count; i++)
            {
                if (!_dropletPool[i].active)
                {
                    freeSlot = _dropletPool[i];
                    freeIdx = i;
                    break;
                }
            }

            if (!freeSlot.HasValue)
            {
                if (_dropletPool.Count >= Mathf.Max(8, mineralParticleCount)) return;
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "LoadedMineralOrb";
                Destroy(orb.GetComponent<Collider>());
                orb.transform.SetParent(transform, true);
                orb.transform.localScale = Vector3.one * 0.14f;

                var mr = orb.GetComponent<MeshRenderer>();
                if (_sharedDropletMat == null)
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    _sharedDropletMat = new Material(sh);
                    _sharedDropletMat.EnableKeyword("_EMISSION");
                    _sharedDropletMat.SetColor("_BaseColor", mineralOrbColor);
                    _sharedDropletMat.SetColor("_EmissionColor", mineralOrbColor * 3.2f);
                }
                mr.sharedMaterial = _sharedDropletMat;

                var created = new MineralDroplet { transform = orb.transform, active = false };
                freeIdx = _dropletPool.Count;
                _dropletPool.Add(created);
            }

            var item = _dropletPool[freeIdx];
            Vector3 origin = loaderArm != null ? loaderArm.position : (transform.position + Vector3.up * 1.4f);
            origin += new Vector3(Random.Range(-0.22f, 0.22f), 0.1f, Random.Range(-0.22f, 0.22f));
            item.startPos = origin;
            item.progress = 0f;
            item.active = true;
            item.transform.position = origin;
            item.transform.gameObject.SetActive(true);
            _dropletPool[freeIdx] = item;
        }
    }
}

