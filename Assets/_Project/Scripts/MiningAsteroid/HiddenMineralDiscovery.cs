using System.Collections;
using UnityEngine;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Feature F: Hidden Energy Chamber (Khám Phá Khoang Năng Lượng Cổ Đại Ẩn Giấu).
    /// - Concealed entrance tucked beneath Hidden_Grotto_Overhang with subtle clue crystals & pulsing energy seam.
    /// - Smoothly unlocks & slides open its mechanical doors when approached/activated.
    /// - Illuminates the interior chamber, orbits mini energy rings, activates the central rare energy crystal,
    ///   fires a brief controlled energy beam, and displays compact discovery notifications.
    /// </summary>
    public class HiddenMineralDiscovery : MonoBehaviour
    {
        [Header("Inspector Settings")]
        public float clueActivationRadius = 24f;
        public float playerInteractionRadius = 15f;
        public float autoOpenRadius = 9.5f;
        public float doorSlideDistance = 2.1f;
        public float doorOpenSpeed = 2.6f;

        [Header("Crystals, Clues & Seam")]
        public Renderer mineralRenderer;
        public Light mineralLight;
        public Renderer[] clueCrystalRenderers;
        public Renderer entranceSeamRenderer;
        public ParticleSystem sparkleParticles;
        public Color rareMineralColor = new Color(1f, 0.25f, 0.85f, 1f);
        public Color ancientEnergyColor = new Color(0.15f, 0.95f, 1f, 1f);

        [Header("Chamber Mechanical Door & Interior")]
        public Transform leftDoorPanel;
        public Transform rightDoorPanel;
        public Light[] chamberInteriorLights;
        public Transform[] ancientMiniRings;
        public LineRenderer ancientEnergyBeam;
        public ParticleSystem chamberEnergyParticles;

        [Header("Optional Legacy Holo")]
        public Transform hologramRoot;
        public TMP_Text discoveryHoloText;

        private bool _cluesIlluminated;
        private bool _isDiscovered;
        private bool _isDoorOpened;
        private bool _isSourceActivated;
        private float _doorOpenProgress;
        private float _beamTimer;
        private Transform _playerTransform;
        private Camera _mainCam;

        private Material _crystalMat;
        private Material _seamMat;
        private Material[] _clueMats;

        private Vector3 _leftDoorClosedPos;
        private Vector3 _rightDoorClosedPos;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool IsDiscovered => _isDiscovered;
        public bool IsDoorOpened => _isDoorOpened;
        public bool IsSourceActivated => _isSourceActivated;

        private void Start()
        {
            if (mineralRenderer != null)
            {
                var mats = mineralRenderer.sharedMaterials;
                int idx = mats.Length >= 3 ? 2 : (mats.Length - 1);
                if (idx >= 0 && idx < mats.Length && mats[idx] != null)
                {
                    _crystalMat = new Material(mats[idx]);
                    _crystalMat.EnableKeyword("_EMISSION");
                    _crystalMat.SetColor(EmissionColorId, rareMineralColor * 0.65f);
                    mats[idx] = _crystalMat;
                    mineralRenderer.sharedMaterials = mats;
                }
            }

            if (entranceSeamRenderer != null)
            {
                _seamMat = new Material(entranceSeamRenderer.sharedMaterial);
                _seamMat.EnableKeyword("_EMISSION");
                entranceSeamRenderer.sharedMaterial = _seamMat;
            }

            if (clueCrystalRenderers != null && clueCrystalRenderers.Length > 0)
            {
                _clueMats = new Material[clueCrystalRenderers.Length];
                for (int i = 0; i < clueCrystalRenderers.Length; i++)
                {
                    if (clueCrystalRenderers[i] == null) continue;
                    var mats = clueCrystalRenderers[i].sharedMaterials;
                    int idx = mats.Length >= 3 ? 2 : (mats.Length - 1);
                    if (idx >= 0 && idx < mats.Length && mats[idx] != null)
                    {
                        var inst = new Material(mats[idx]);
                        inst.EnableKeyword("_EMISSION");
                        inst.SetColor(EmissionColorId, ancientEnergyColor * 0.45f);
                        mats[idx] = inst;
                        clueCrystalRenderers[i].sharedMaterials = mats;
                        _clueMats[i] = inst;
                    }
                }
            }

            if (leftDoorPanel != null) _leftDoorClosedPos = leftDoorPanel.localPosition;
            if (rightDoorPanel != null) _rightDoorClosedPos = rightDoorPanel.localPosition;

            SetChamberInteriorActive(false);
            if (ancientEnergyBeam != null) ancientEnergyBeam.enabled = false;
            if (hologramRoot != null) hologramRoot.gameObject.SetActive(false);
            else if (discoveryHoloText != null) discoveryHoloText.gameObject.SetActive(false);

            FindPlayer();
        }

        private void FindPlayer()
        {
            _playerTransform = AsteroidInteractionHUD.GetPlayerTransform();
            _mainCam = Camera.main;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;

            if (_playerTransform == null || _mainCam == null) FindPlayer();

            // Subtle idle seam flicker before discovery; hide once doors unlock
            if (entranceSeamRenderer != null && entranceSeamRenderer.enabled != !_isDoorOpened)
            {
                entranceSeamRenderer.enabled = !_isDoorOpened;
            }
            if (_seamMat != null && !_isDoorOpened)
            {
                float seamGlow = _cluesIlluminated ? 1.5f + 0.5f * Mathf.Sin(time * 3.2f) : 0.4f + 0.25f * Mathf.Sin(time * 2f);
                _seamMat.SetColor(EmissionColorId, ancientEnergyColor * seamGlow);
            }

            if (_playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);

                if (!_cluesIlluminated && dist <= clueActivationRadius)
                {
                    IlluminateClues();
                }

                if (!_isDiscovered && dist <= playerInteractionRadius)
                {
                    AsteroidInteractionHUD.ReportCandidate(this, dist, "[F] UNLOCK HIDDEN ENERGY CHAMBER", ancientEnergyColor);
                    if (AsteroidInteractionHUD.ConsumeInteractPress(this) || dist <= autoOpenRadius)
                    {
                        TriggerDiscovery();
                    }
                }
                else if (_isDiscovered && !_isSourceActivated && dist <= playerInteractionRadius * 0.9f)
                {
                    AsteroidInteractionHUD.ReportCandidate(this, dist, "[F] ACTIVATE ANCIENT ENERGY CRYSTAL", rareMineralColor);
                    if (AsteroidInteractionHUD.ConsumeInteractPress(this))
                    {
                        ActivateAncientSource();
                    }
                }
            }

            // Animate mechanical door panels sliding open smoothly
            if (_isDoorOpened && _doorOpenProgress < 1f)
            {
                _doorOpenProgress = Mathf.MoveTowards(_doorOpenProgress, 1f, doorOpenSpeed * dt);
                float ease = Mathf.SmoothStep(0f, 1f, _doorOpenProgress);
                if (leftDoorPanel != null)
                {
                    leftDoorPanel.localPosition = _leftDoorClosedPos + Vector3.left * (doorSlideDistance * ease);
                }
                if (rightDoorPanel != null)
                {
                    rightDoorPanel.localPosition = _rightDoorClosedPos + Vector3.right * (doorSlideDistance * ease);
                }
            }

            // Orbit mini rings around the ancient rare crystal once discovered
            if (_isDiscovered && ancientMiniRings != null)
            {
                float ringMult = _beamTimer > 0f ? 2.8f : (_isSourceActivated ? 1.6f : 1.0f);
                for (int i = 0; i < ancientMiniRings.Length; i++)
                {
                    if (ancientMiniRings[i] == null) continue;
                    Vector3 axis = (i % 2 == 0) ? new Vector3(0.2f, 1f, 0.3f).normalized : new Vector3(1f, 0.25f, 0.15f).normalized;
                    ancientMiniRings[i].Rotate(axis * (42f * ringMult * dt), Space.Self);
                }
            }

            // Brief controlled energy beam timer
            if (_beamTimer > 0f)
            {
                _beamTimer -= dt;
                if (ancientEnergyBeam != null)
                {
                    ancientEnergyBeam.enabled = true;
                    float w = 0.24f + 0.06f * Mathf.Sin(time * 24f);
                    ancientEnergyBeam.startWidth = w;
                    ancientEnergyBeam.endWidth = w * 0.3f;
                }
                if (_beamTimer <= 0f && ancientEnergyBeam != null)
                {
                    ancientEnergyBeam.enabled = false;
                }
            }

            // Keep compact world label facing camera if enabled
            Transform targetHolo = hologramRoot != null ? hologramRoot : (discoveryHoloText != null ? discoveryHoloText.transform : null);
            if (_isDiscovered && targetHolo != null && targetHolo.gameObject.activeSelf && _mainCam != null)
            {
                Vector3 toCam = targetHolo.position - _mainCam.transform.position;
                if (toCam.sqrMagnitude > 0.01f)
                {
                    targetHolo.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                }
            }
        }

        private void IlluminateClues()
        {
            _cluesIlluminated = true;
            if (_clueMats != null)
            {
                for (int i = 0; i < _clueMats.Length; i++)
                {
                    if (_clueMats[i] != null)
                    {
                        _clueMats[i].SetColor(EmissionColorId, ancientEnergyColor * 2.4f);
                    }
                }
            }
        }

        private void SetChamberInteriorActive(bool active)
        {
            if (chamberInteriorLights != null)
            {
                for (int i = 0; i < chamberInteriorLights.Length; i++)
                {
                    if (chamberInteriorLights[i] == null) continue;
                    chamberInteriorLights[i].enabled = active;
                }
            }
            if (chamberEnergyParticles != null)
            {
                if (active) chamberEnergyParticles.Play();
                else chamberEnergyParticles.Stop();
            }
        }

        public void TriggerDiscovery()
        {
            if (_isDiscovered) return;
            _isDiscovered = true;
            _isDoorOpened = true;

            IlluminateClues();
            SetChamberInteriorActive(true);

            AsteroidInteractionHUD.PlayChamberDiscoverySound();
            AsteroidInteractionHUD.ShowNotification("HIDDEN ENERGY CHAMBER DISCOVERED", ancientEnergyColor, 3.0f);

            if (_crystalMat != null)
            {
                _crystalMat.SetColor(EmissionColorId, rareMineralColor * 2.8f);
            }

            if (mineralLight != null)
            {
                mineralLight.color = rareMineralColor;
                mineralLight.intensity = 4.2f;
                mineralLight.range = 18f;
            }

            if (sparkleParticles != null)
            {
                sparkleParticles.Play();
            }

            // Suppress oversized old holo text to avoid UI clutter; keep compact notification HUD
            if (hologramRoot != null) hologramRoot.gameObject.SetActive(false);

            StartCoroutine(AutoActivateSourceAfterDoorOpens());
        }

        private IEnumerator AutoActivateSourceAfterDoorOpens()
        {
            yield return new WaitForSeconds(1.1f);
            if (!_isSourceActivated)
            {
                ActivateAncientSource();
            }
        }

        public void ActivateAncientSource()
        {
            _isSourceActivated = true;
            _beamTimer = 3.2f;

            if (ancientEnergyBeam != null)
            {
                ancientEnergyBeam.enabled = true;
            }

            AsteroidInteractionHUD.PlayCoreActivationSound();
            var ship = AsteroidInteractionHUD.GetPlayerTransform()?.GetComponent<PlayerSpaceshipController>();
            if (ship != null)
            {
                ship.RechargeEnergy(0f, true);
                AsteroidInteractionHUD.ShowNotification("ANCIENT ENERGY SOURCE DETECTED — SHIP ENERGY 100%", rareMineralColor, 3.2f);
            }
            else
            {
                AsteroidInteractionHUD.ShowNotification("ANCIENT ENERGY SOURCE DETECTED", rareMineralColor, 3.2f);
            }

            if (_crystalMat != null)
            {
                _crystalMat.SetColor(EmissionColorId, rareMineralColor * 4.5f);
            }

            if (mineralLight != null)
            {
                mineralLight.color = rareMineralColor;
                mineralLight.intensity = 5.8f;
            }

            if (chamberEnergyParticles != null)
            {
                chamberEnergyParticles.Emit(24);
            }
        }
    }
}
