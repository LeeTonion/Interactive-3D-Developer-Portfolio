using UnityEngine;

namespace SpaceCyberpunk.HangarAnimation
{
    /// <summary>
    /// Controls the 5-minute (300s) continuously escalating Post-Attack Reactor Meltdown in Room02_Reactor_Room.
    /// Every visual cue (cracks, electric arcs, severed wire shorts, core color/spin, conduit overheat,
    /// smoke, strobes, and catwalk collapse) ramps up smoothly over elapsedTime [0..300s] so the player
    /// perceives constant worsening rather than abrupt step jumps.
    /// </summary>
    public class Room02_ReactorMeltdownController : MonoBehaviour
    {
        public enum MeltdownStage
        {
            Stage01_BattleDamaged = 1,
            Stage02_ContainmentStress = 2,
            Stage03_StructuralCollapse = 3,
            Stage04_CriticalDetonation = 4
        }

        [Header("Timeline (300s = 5 Minutes)")]
        public float totalDuration = 300f;
        public float stage2Time = 90f;   // 1:30
        public float stage3Time = 180f;  // 3:00
        public float stage4Time = 255f;  // 4:15
        public bool autoRun = true;
        [Range(0f, 300f)] public float elapsedTime = 0f;

        [Header("Room Bounds (Local Space)")]
        public Vector3 roomCenterLocal = new Vector3(0f, 8.25f, -76f);
        public Vector3 roomHalfExtents = new Vector3(13.5f, 7f, 10.5f);

        [Header("Core Helix, Rings & Conduits")]
        public Transform plasmaHelixPivot;
        public Transform[] magneticRings;
        public Renderer[] cyanCoreRenderers;
        public Renderer[] amberConduitRenderers;
        public LineRenderer[] electricArcLines;
        public Vector3 coreCenterLocal = new Vector3(0f, 2.25f, -76f);

        [Header("Stage Visual Groups")]
        public GameObject stage1DamageGroup;
        public GameObject stage2StressGroup;
        public GameObject stage3CollapseGroup;
        public GameObject stage4DetonationGroup;

        [Header("Progressive Cracks & Collapsible Catwalks")]
        public GameObject[] progressiveCrackObjects;
        public Transform[] collapsibleCatwalks;
        public Transform[] blownCatwalkSegments;
        public GameObject intactContainmentGlass;
        public GameObject hairlineCrackGlass;
        public GameObject heavyFractureGlass;

        [Header("Lighting & Red Strobe Beacons")]
        public Light[] mainEmergencyLights;
        public Light[] cyanCoreLights;
        public Light[] amberConduitLights;
        public Light[] redStrobeBeacons;
        public Transform[] strobeSpinners;

        [Header("VFX Particle Systems")]
        public ParticleSystem[] stage1SteamAndSparks;
        public ParticleSystem[] stage2ArcsAndSparks;
        public ParticleSystem[] stage3SmokeAndFlames;
        public ParticleSystem[] stage4ExplosionVfx;

        [Header("Severed Wire Short-Circuit VFX")]
        public Light[] severedWireLights;
        public ParticleSystem[] severedWireSparks;
        public LineRenderer[] severedWireArcs;

        private MeltdownStage _currentStage = MeltdownStage.Stage01_BattleDamaged;
        private Vector3[] _collapseBasePos;
        private Quaternion[] _collapseBaseRot;
        private Vector3[] _blownBasePos;
        private Quaternion[] _blownBaseRot;
        private float[] _wireNextZap;
        private float[] _wireZapEnd;

        private Transform _player;
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private string _toastText;
        private float _toastTimer;
        private float _arcTimer;
        private GUIStyle _barTitleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _promptStyle;
        private Texture2D _whiteTex;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _whiteTex = Texture2D.whiteTexture;
        }

        private void Start()
        {
            CacheBaseTransforms(collapsibleCatwalks, ref _collapseBasePos, ref _collapseBaseRot);
            CacheBaseTransforms(blownCatwalkSegments, ref _blownBasePos, ref _blownBaseRot);
            if (progressiveCrackObjects == null || progressiveCrackObjects.Length == 0)
                CollectProgressiveCracks();

            // Keep glass crack containers active so individual cracks can appear progressively
            if (hairlineCrackGlass != null) hairlineCrackGlass.SetActive(true);
            if (heavyFractureGlass != null) heavyFractureGlass.SetActive(true);
            if (stage2StressGroup != null) stage2StressGroup.SetActive(true);

            FindPlayer();
            ApplyStageState(GetStageForTime(elapsedTime), true);
            ApplyContinuousProgression(Mathf.Clamp01(elapsedTime / Mathf.Max(1f, totalDuration)));
        }

        private void CollectProgressiveCracks()
        {
            var list = new System.Collections.Generic.List<GameObject>();
            if (hairlineCrackGlass != null)
            {
                foreach (Transform c in hairlineCrackGlass.transform)
                    if (c.name.StartsWith("SpiderCrack_")) list.Add(c.gameObject);
            }
            if (heavyFractureGlass != null)
            {
                foreach (Transform c in heavyFractureGlass.transform)
                    if (c.name.StartsWith("HeavyCrack_")) list.Add(c.gameObject);
            }
            progressiveCrackObjects = list.ToArray();
        }

        private static void CacheBaseTransforms(Transform[] arr, ref Vector3[] pos, ref Quaternion[] rot)
        {
            if (arr == null || arr.Length == 0) return;
            pos = new Vector3[arr.Length];
            rot = new Quaternion[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != null)
                {
                    pos[i] = arr[i].localPosition;
                    rot[i] = arr[i].localRotation;
                }
            }
        }

        private void FindPlayer()
        {
            var pc = FindFirstObjectByType<PlayerCharacterController>();
            if (pc != null) _player = pc.transform;
            else if (Camera.main != null) _player = Camera.main.transform;
        }

        public MeltdownStage GetStageForTime(float t)
        {
            if (t >= stage4Time) return MeltdownStage.Stage04_CriticalDetonation;
            if (t >= stage3Time) return MeltdownStage.Stage03_StructuralCollapse;
            if (t >= stage2Time) return MeltdownStage.Stage02_ContainmentStress;
            return MeltdownStage.Stage01_BattleDamaged;
        }

        public void JumpToStage(MeltdownStage stage)
        {
            switch (stage)
            {
                case MeltdownStage.Stage01_BattleDamaged: elapsedTime = 5f; break;
                case MeltdownStage.Stage02_ContainmentStress: elapsedTime = stage2Time + 5f; break;
                case MeltdownStage.Stage03_StructuralCollapse: elapsedTime = stage3Time + 5f; break;
                case MeltdownStage.Stage04_CriticalDetonation: elapsedTime = stage4Time + 2f; break;
            }
            ApplyStageState(stage, true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;

            if (autoRun)
                elapsedTime = Mathf.Min(totalDuration, elapsedTime + dt);

            if (IsPlayerInRoom())
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) JumpToStage(MeltdownStage.Stage01_BattleDamaged);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) JumpToStage(MeltdownStage.Stage02_ContainmentStress);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) JumpToStage(MeltdownStage.Stage03_StructuralCollapse);
                else if (Input.GetKeyDown(KeyCode.Alpha4)) JumpToStage(MeltdownStage.Stage04_CriticalDetonation);
                else if (Input.GetKeyDown(KeyCode.R))
                {
                    elapsedTime = 0f;
                    ApplyStageState(MeltdownStage.Stage01_BattleDamaged, true);
                    _toastText = "TIMELINE RESET [0:00]";
                    _toastTimer = 2.5f;
                }
            }

            float progress = Mathf.Clamp01(elapsedTime / Mathf.Max(1f, totalDuration));
            MeltdownStage targetStage = GetStageForTime(elapsedTime);
            if (targetStage != _currentStage)
                ApplyStageState(targetStage, false);

            ApplyContinuousProgression(progress);
            AnimateCoreAndRings(dt, t, progress);
            AnimateElectricArcs(dt, progress);
            AnimateSeveredWireShorts(t, progress);
            AnimateConduitsAndLights(dt, t, progress);

            if (_toastTimer > 0f) _toastTimer -= dt;
        }

        private void ApplyStageState(MeltdownStage stage, bool immediate)
        {
            _currentStage = stage;
            int s = (int)stage;

            if (stage1DamageGroup != null) stage1DamageGroup.SetActive(true);
            if (stage2StressGroup != null) stage2StressGroup.SetActive(true);
            if (stage3CollapseGroup != null) stage3CollapseGroup.SetActive(s >= 3);
            if (stage4DetonationGroup != null) stage4DetonationGroup.SetActive(s >= 4);

            if (intactContainmentGlass != null) intactContainmentGlass.SetActive(s < 4);

            ToggleParticles(stage1SteamAndSparks, true);
            ToggleParticles(stage2ArcsAndSparks, true);
            ToggleParticles(stage3SmokeAndFlames, s >= 3);
            ToggleParticles(stage4ExplosionVfx, s >= 4);

            if (!immediate)
            {
                _toastText = stage switch
                {
                    MeltdownStage.Stage02_ContainmentStress => "STAGE 02: CONTAINMENT STRESS & SURGE [1:30]",
                    MeltdownStage.Stage03_StructuralCollapse => "STAGE 03: STRUCTURAL COLLAPSE [3:00]",
                    MeltdownStage.Stage04_CriticalDetonation => "STAGE 04: CRITICAL CORE DETONATION [4:15]",
                    _ => "STAGE 01: BATTLE-DAMAGED & DESTABILIZING [0:00]"
                };
                _toastTimer = 4f;
            }
        }

        private void ApplyContinuousProgression(float progress)
        {
            // 1. Progressive glass cracks: reveal one by one from progress 0.06 -> 0.84
            if (progressiveCrackObjects != null && progressiveCrackObjects.Length > 0)
            {
                int total = progressiveCrackObjects.Length;
                float norm = Mathf.InverseLerp(0.06f, 0.84f, progress);
                int visibleCount = Mathf.RoundToInt(norm * total);
                for (int i = 0; i < total; i++)
                {
                    if (progressiveCrackObjects[i] != null)
                        progressiveCrackObjects[i].SetActive(i < visibleCount && progress < 0.94f);
                }
            }

            // 2. Progressive ceiling spark shower rate (ramps smoothly from 0 -> 55/s)
            if (stage2ArcsAndSparks != null)
            {
                float sparkScale = Mathf.InverseLerp(0.08f, 0.85f, progress);
                for (int i = 0; i < stage2ArcsAndSparks.Length; i++)
                {
                    if (stage2ArcsAndSparks[i] == null) continue;
                    var em = stage2ArcsAndSparks[i].emission;
                    em.rateOverTime = Mathf.Lerp(2f, 52f, sparkScale);
                }
            }

            // 3. Progressive catwalk buckling & collapse (starts gently tilting at 0.52, fully collapsed by 0.82)
            if (collapsibleCatwalks != null && _collapseBasePos != null)
            {
                int n = collapsibleCatwalks.Length;
                for (int i = 0; i < n; i++)
                {
                    if (collapsibleCatwalks[i] == null) continue;
                    float segStart = 0.52f + (i / (float)Mathf.Max(1, n)) * 0.18f;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(segStart, segStart + 0.12f, progress));
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    collapsibleCatwalks[i].localPosition = Vector3.Lerp(_collapseBasePos[i], _collapseBasePos[i] + new Vector3(0.35f * dir, -1.6f, 0.2f), k);
                    collapsibleCatwalks[i].localRotation = Quaternion.Slerp(_collapseBaseRot[i], _collapseBaseRot[i] * Quaternion.Euler(18f * dir, -10f, -26f), k);
                }
            }

            // 4. Progressive blasted catwalk segments in Stage 04 (0.85 -> 0.98)
            if (blownCatwalkSegments != null && _blownBasePos != null)
            {
                int n = blownCatwalkSegments.Length;
                for (int i = 0; i < n; i++)
                {
                    if (blownCatwalkSegments[i] == null) continue;
                    float segStart = 0.85f + (i / (float)Mathf.Max(1, n)) * 0.08f;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(segStart, segStart + 0.06f, progress));
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    blownCatwalkSegments[i].localPosition = Vector3.Lerp(_blownBasePos[i], _blownBasePos[i] + new Vector3(dir * 1.1f, -1.2f - (i % 3) * 0.4f, dir * 0.9f), k);
                    blownCatwalkSegments[i].localRotation = Quaternion.Slerp(_blownBaseRot[i], _blownBaseRot[i] * Quaternion.Euler(24f * dir, 16f * dir, -32f * dir), k);
                }
            }
        }

        private static void ToggleParticles(ParticleSystem[] systems, bool active)
        {
            if (systems == null) return;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                if (active && !systems[i].isPlaying) systems[i].Play();
                else if (!active && systems[i].isPlaying) systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void AnimateCoreAndRings(float dt, float t, float progress)
        {
            float speedMult = Mathf.Lerp(1.0f, 6.2f, progress * progress);

            if (plasmaHelixPivot != null)
                plasmaHelixPivot.Rotate(Vector3.up, 70f * speedMult * dt, Space.Self);

            if (magneticRings != null)
            {
                for (int i = 0; i < magneticRings.Length; i++)
                {
                    if (magneticRings[i] == null) continue;
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    magneticRings[i].Rotate(Vector3.up, dir * (26f + i * 11f) * speedMult * dt, Space.Self);
                }
            }

            if (cyanCoreRenderers != null)
            {
                Color cCyan = new Color(0.22f, 0.90f, 1.0f);
                Color cSurge = new Color(0.75f, 0.98f, 1.0f);
                Color cCrit = new Color(1.0f, 0.38f, 0.16f);
                Color cBlast = new Color(1.0f, 0.82f, 0.35f);

                Color baseCol;
                if (progress < 0.45f)
                    baseCol = Color.Lerp(cCyan, cSurge, progress / 0.45f);
                else if (progress < 0.80f)
                    baseCol = Color.Lerp(cSurge, cCrit, (progress - 0.45f) / 0.35f);
                else
                    baseCol = Color.Lerp(cCrit, cBlast, Mathf.PingPong(t * 8f, 1f));

                float pulse = Mathf.Lerp(1.2f, 2.6f, progress) + Mathf.Lerp(0.2f, 0.8f, progress) * Mathf.Sin(t * Mathf.Lerp(2.5f, 14f, progress));
                ApplyColorAndEmission(cyanCoreRenderers, baseCol, baseCol * pulse);
            }
        }

        private void AnimateElectricArcs(float dt, float progress)
        {
            if (electricArcLines == null || electricArcLines.Length == 0) return;

            // Gradually activate more arcs as progress climbs from 0.10 -> 0.75
            int activeArcs = Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(0.08f, 0.72f, progress) * (electricArcLines.Length + 0.5f)), 0, electricArcLines.Length);
            bool allowArcs = progress >= 0.08f && progress < 0.96f;

            _arcTimer -= dt;
            bool refresh = _arcTimer <= 0f;
            if (refresh) _arcTimer = Mathf.Lerp(0.11f, 0.035f, progress);

            for (int i = 0; i < electricArcLines.Length; i++)
            {
                var lr = electricArcLines[i];
                if (lr == null) continue;
                bool on = allowArcs && i < activeArcs;
                lr.enabled = on;
                if (!on || !refresh) continue;

                lr.startWidth = Mathf.Lerp(0.045f, 0.11f, progress);
                int pts = lr.positionCount;
                if (pts < 3) continue;

                float yStart = coreCenterLocal.y + 1.8f + (i % 3) * 3.1f;
                float angA = (i * 67f + Time.time * 90f) * Mathf.Deg2Rad;
                float angB = angA + ((i % 2 == 0) ? 1.1f : -1.2f);
                Vector3 p0 = new Vector3(coreCenterLocal.x + Mathf.Cos(angA) * 2.4f, yStart, coreCenterLocal.z + Mathf.Sin(angA) * 2.4f);
                Vector3 p1 = new Vector3(coreCenterLocal.x + Mathf.Cos(angB) * 4.8f, yStart + Random.Range(-1.4f, 1.4f), coreCenterLocal.z + Mathf.Sin(angB) * 4.8f);

                for (int k = 0; k < pts; k++)
                {
                    float f = k / (float)(pts - 1);
                    Vector3 pos = Vector3.Lerp(p0, p1, f);
                    if (k > 0 && k < pts - 1) pos += Random.insideUnitSphere * Mathf.Lerp(0.28f, 0.58f, progress);
                    lr.SetPosition(k, pos);
                }
            }
        }

        private void AnimateSeveredWireShorts(float t, float progress)
        {
            int count = severedWireSparks != null ? severedWireSparks.Length : 0;
            if (count == 0) return;

            if (_wireNextZap == null || _wireNextZap.Length != count)
            {
                _wireNextZap = new float[count];
                _wireZapEnd = new float[count];
                for (int i = 0; i < count; i++) _wireNextZap[i] = t + Random.Range(0.2f, 1.8f);
            }

            // Smoothly increases short-circuit frequency & spark count over time!
            float intervalMult = Mathf.Lerp(1.45f, 0.18f, progress);
            int burstMin = Mathf.RoundToInt(Mathf.Lerp(6f, 18f, progress));
            int burstMax = Mathf.RoundToInt(Mathf.Lerp(12f, 32f, progress));

            for (int i = 0; i < count; i++)
            {
                if (t >= _wireNextZap[i])
                {
                    _wireZapEnd[i] = t + Random.Range(0.14f, 0.36f);
                    _wireNextZap[i] = _wireZapEnd[i] + Random.Range(0.45f, 2.0f) * intervalMult;
                    if (severedWireSparks[i] != null)
                        severedWireSparks[i].Emit(Random.Range(burstMin, burstMax));
                }

                bool zapping = t < _wireZapEnd[i];
                if (severedWireLights != null && i < severedWireLights.Length && severedWireLights[i] != null)
                {
                    severedWireLights[i].enabled = zapping;
                    if (zapping)
                        severedWireLights[i].intensity = Random.Range(3.5f, Mathf.Lerp(6.5f, 11f, progress));
                }

                if (severedWireArcs != null && i < severedWireArcs.Length && severedWireArcs[i] != null)
                {
                    var lr = severedWireArcs[i];
                    lr.enabled = zapping;
                    if (zapping && lr.positionCount >= 4)
                    {
                        Vector3 endDir = Random.onUnitSphere * Mathf.Lerp(0.38f, 0.72f, progress);
                        endDir.y = -Mathf.Abs(endDir.y) - 0.12f;
                        for (int k = 0; k < lr.positionCount; k++)
                        {
                            float f = k / (float)(lr.positionCount - 1);
                            Vector3 p = Vector3.Lerp(Vector3.zero, endDir, f);
                            if (k > 0 && k < lr.positionCount - 1) p += Random.insideUnitSphere * 0.14f;
                            lr.SetPosition(k, p);
                        }
                    }
                }
            }
        }

        private void AnimateConduitsAndLights(float dt, float t, float progress)
        {
            // Conduits smoothly overheat from cool dark metal -> warm amber -> incandescent fiery orange
            if (amberConduitRenderers != null && amberConduitRenderers.Length > 0)
            {
                float heat = Mathf.InverseLerp(0.10f, 0.80f, progress);
                Color baseCol = Color.Lerp(new Color(0.22f, 0.25f, 0.29f), new Color(1.0f, 0.42f, 0.06f), heat);
                float pulse = 0.25f + heat * (1.8f + 0.55f * Mathf.Sin(t * Mathf.Lerp(2f, 10f, heat)));
                ApplyColorAndEmission(amberConduitRenderers, baseCol, new Color(1.0f, 0.36f, 0.04f) * pulse);
            }

            // Main lights start flickering from 0.25 and die out by 0.60
            if (mainEmergencyLights != null)
            {
                for (int i = 0; i < mainEmergencyLights.Length; i++)
                {
                    if (mainEmergencyLights[i] == null) continue;
                    if (progress >= 0.62f)
                    {
                        mainEmergencyLights[i].enabled = false;
                    }
                    else
                    {
                        mainEmergencyLights[i].enabled = true;
                        float flickerChance = Mathf.InverseLerp(0.20f, 0.60f, progress);
                        bool dip = flickerChance > 0.05f && Mathf.PerlinNoise(t * (8f + flickerChance * 14f), i * 3.7f) < flickerChance * 0.65f;
                        mainEmergencyLights[i].intensity = dip ? 0.3f : Mathf.Lerp(2.8f, 1.5f, progress);
                    }
                }
            }

            // Red strobe beacons fade in smoothly from 0.55 -> 1.0
            float strobeRamp = Mathf.InverseLerp(0.55f, 0.78f, progress);
            if (redStrobeBeacons != null)
            {
                for (int i = 0; i < redStrobeBeacons.Length; i++)
                {
                    if (redStrobeBeacons[i] == null) continue;
                    redStrobeBeacons[i].enabled = strobeRamp > 0.01f;
                    if (strobeRamp > 0.01f)
                        redStrobeBeacons[i].intensity = strobeRamp * (7f + 7f * Mathf.Sin(t * 9f + i * 1.7f));
                }
            }

            if (strobeSpinners != null && strobeRamp > 0.01f)
            {
                for (int i = 0; i < strobeSpinners.Length; i++)
                    if (strobeSpinners[i] != null) strobeSpinners[i].Rotate(Vector3.up, 260f * dt, Space.Self);
            }

            if (cyanCoreLights != null)
            {
                Color cLight = Color.Lerp(new Color(0.25f, 0.9f, 1f), new Color(1f, 0.45f, 0.18f), Mathf.InverseLerp(0.45f, 0.85f, progress));
                for (int i = 0; i < cyanCoreLights.Length; i++)
                {
                    if (cyanCoreLights[i] == null) continue;
                    cyanCoreLights[i].color = cLight;
                    cyanCoreLights[i].intensity = Mathf.Lerp(6.5f, 18f, progress) + (progress > 0.75f ? 4f * Mathf.Sin(t * 14f) : 0f);
                }
            }

            if (amberConduitLights != null)
            {
                for (int i = 0; i < amberConduitLights.Length; i++)
                    if (amberConduitLights[i] != null)
                        amberConduitLights[i].intensity = Mathf.Lerp(0.6f, 12f, progress);
            }
        }

        private void ApplyColorAndEmission(Renderer[] renderers, Color baseCol, Color emission)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, baseCol);
                _mpb.SetColor(EmissionColorId, emission);
                renderers[i].SetPropertyBlock(_mpb);
            }
        }

        private bool IsPlayerInRoom()
        {
            Transform target = _player != null ? _player : (Camera.main != null ? Camera.main.transform : null);
            if (target == null) return false;
            Vector3 localP = transform.InverseTransformPoint(target.position);
            Vector3 d = localP - roomCenterLocal;
            return Mathf.Abs(d.x) <= roomHalfExtents.x &&
                   Mathf.Abs(d.y) <= roomHalfExtents.y &&
                   Mathf.Abs(d.z) <= roomHalfExtents.z;
        }

        private void OnGUI()
        {
            if (!IsPlayerInRoom()) return;

            if (_barTitleStyle == null)
            {
                _barTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.85f, 0.95f, 1f) }
                };
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.9f, 0.45f) }
                };
            }

            float w = Mathf.Min(680f, Screen.width - 40f);
            float x = (Screen.width - w) * 0.5f;
            float y = 14f;

            GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.82f);
            GUI.DrawTexture(new Rect(x - 10f, y - 6f, w + 20f, 54f), _whiteTex);
            GUI.color = Color.white;

            int mins = Mathf.FloorToInt(elapsedTime / 60f);
            int secs = Mathf.FloorToInt(elapsedTime % 60f);
            float ratio = Mathf.Clamp01(elapsedTime / totalDuration);
            string stageTitle = _currentStage switch
            {
                MeltdownStage.Stage01_BattleDamaged => "STAGE 01: BATTLE-DAMAGED & DESTABILIZING [0:00-1:30]",
                MeltdownStage.Stage02_ContainmentStress => "STAGE 02: CONTAINMENT STRESS & SURGE [1:30-3:00]",
                MeltdownStage.Stage03_StructuralCollapse => "STAGE 03: STRUCTURAL COLLAPSE [3:00-4:15]",
                _ => "STAGE 04: CRITICAL CORE DETONATION [4:15-5:00]"
            };
            GUI.Label(new Rect(x, y - 2f, w - 110f, 20f), stageTitle, _barTitleStyle);
            GUI.Label(new Rect(x + w - 110f, y - 2f, 110f, 20f), $"{mins}:{secs:00} / 5:00", _labelStyle);

            float barY = y + 22f;
            GUI.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
            GUI.DrawTexture(new Rect(x, barY, w, 10f), _whiteTex);

            Color fillCol = Color.Lerp(new Color(0.15f, 0.85f, 1f), new Color(1f, 0.12f, 0.08f), ratio);
            GUI.color = fillCol;
            GUI.DrawTexture(new Rect(x, barY, w * ratio, 10f), _whiteTex);
            GUI.color = Color.white;

            GUI.Label(new Rect(x, barY + 11f, w, 16f), $"INSTABILITY {Mathf.RoundToInt(ratio * 100f)}%   |   [1-4] Preview Stage   |   [R] Reset", _labelStyle);

            if (_toastTimer > 0f && !string.IsNullOrEmpty(_toastText))
            {
                GUI.Box(new Rect((Screen.width - 500f) * 0.5f, 80f, 500f, 32f), _toastText, _promptStyle);
            }
        }
    }
}
