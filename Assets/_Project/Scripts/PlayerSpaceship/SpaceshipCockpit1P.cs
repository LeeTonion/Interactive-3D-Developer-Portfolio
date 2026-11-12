using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Buồng lái Phi Thuyền Góc Nhìn Thứ Nhất AAA (1st-Person Starfighter Cockpit Rig):
    /// - Khung kim loại phay xước vát cạnh nhiều tầng kèm cần ga động (Throttle), cần lái động (Flight Stick),
    ///   cụm thấu kính chiếu HUD trung tâm, hốc turbine sọc gió và nòng pháo plasma nhô nhẹ hai bên mũi.
    /// - 3 màn hình OLED MFD phân giải cao phủ vân quét scanline:
    ///   + Trái (SYS // DEFENSE): Sơ đồ khung tàu + thanh chia ô Hull/Shield + chỉ số lực G-Force.
    ///   + Giữa (TAC // RADAR): Quét sóng radar xoay kèm chấm mục tiêu (Blip) trực tiếp trong tầm 250m & khóa mục tiêu.
    ///   + Phải (ENG // PROPULSION): Đường chân trời nhân tạo (Artificial Horizon) nghiêng theo cánh + chế độ lực đẩy + Boost.
    /// - Luồng bụi sao ngoại vi tốc độ cao khi Boost & phản hồi giật nòng khi bắn laser / rung lắc khi trúng đạn.
    /// </summary>
    public class SpaceshipCockpit1P : MonoBehaviour
    {
        [Header("Liên Kết Hệ Thống")]
        public PlayerSpaceshipController spaceship;
        public SpaceshipCameraFollow cameraFollow;

        private Transform _swayPivot;
        private GameObject _cockpitRoot;
        private Transform _throttleHandlePivot;
        private Transform _flightStickPivot;
        private ParticleSystem _speedStreaksPs;

        // Materials
        private Material _armorDarkMat;
        private Material _armorTrimMat;
        private Material _armorAccentMat;
        private Material _cyanGlowMat;
        private Material _hoodTrimMat;
        private Material _muzzleGlowMat;
        private Material _cautionLedMat;
        private Material _screenGlassMat;

        private Light _cockpitFillLight;

        // Left MFD (SYS // DEFENSE)
        private Image[] _mfdHpCells;
        private Image[] _mfdShdCells;
        private TMP_Text _mfdHpValText;
        private TMP_Text _mfdShdValText;
        private TMP_Text _mfdSysStatusText;
        private TMP_Text _mfdGForceText;
        private Image _mfdShipShieldRing;
        private Image _mfdShipCoreIcon;

        // Center MFD (TAC // RADAR)
        private RectTransform _mfdRadarSweep;
        private RectTransform _mfdRadarContainer;
        private readonly List<Image> _radarBlips = new List<Image>();
        private TMP_Text _mfdTacStateText;
        private TMP_Text _mfdTacTargetText;
        private Image _mfdTacRingImg;

        // Right MFD (ENG // PROPULSION)
        private RectTransform _mfdHorizonPivot;
        private TMP_Text _mfdSpeedValText;
        private TMP_Text _mfdThrustModeText;
        private Image[] _mfdBoostCells;
        private TMP_Text _mfdAttitudeText;

        // Sway, Recoil & Impact State
        private float _smoothedRoll;
        private float _smoothedPitch;
        private float _smoothedYawShift;
        private float _smoothedZKick;
        private float _fireRecoilKick;
        private float _damageShakeTimer;
        private float _muzzleFlashTimer;
        private float _smoothedStickPitch;
        private float _smoothedStickRoll;
        private float _smoothedThrottleSlide;

        // Cached procedural textures / sprites
        private static Texture2D _metalPanelTex;
        private static Texture2D _scanlineTex;
        private static Sprite _scanlineSprite;
        private static Sprite _solidWhiteSprite;
        private static Sprite _radarCircleSprite;
        private static Sprite _radarSweepSprite;
        private static Sprite _blipDotSprite;
        private static Sprite _shipSilhouetteSprite;

        public bool IsCockpitActive => _cockpitRoot != null && _cockpitRoot.activeSelf;

        public void NotifyWeaponFire()
        {
            _fireRecoilKick = 1f;
            _muzzleFlashTimer = 0.09f;
        }

        public void NotifyDamageImpact()
        {
            _damageShakeTimer = 0.35f;
        }

        private void OnEnable()
        {
            EnsureCockpitBuilt();
        }

        private void OnDestroy()
        {
            if (_swayPivot != null) Destroy(_swayPivot.gameObject);
            if (_armorDarkMat != null) Destroy(_armorDarkMat);
            if (_armorTrimMat != null) Destroy(_armorTrimMat);
            if (_armorAccentMat != null) Destroy(_armorAccentMat);
            if (_cyanGlowMat != null) Destroy(_cyanGlowMat);
            if (_hoodTrimMat != null) Destroy(_hoodTrimMat);
            if (_muzzleGlowMat != null) Destroy(_muzzleGlowMat);
            if (_cautionLedMat != null) Destroy(_cautionLedMat);
            if (_screenGlassMat != null) Destroy(_screenGlassMat);
        }

        public void SetVisible(bool visible)
        {
            EnsureCockpitBuilt();
            if (_cockpitRoot != null && _cockpitRoot.activeSelf != visible)
            {
                _cockpitRoot.SetActive(visible);
            }
        }

        public void UpdateCockpit(float dt)
        {
            if (cameraFollow == null) cameraFollow = GetComponent<SpaceshipCameraFollow>();
            if (spaceship == null) spaceship = FindFirstObjectByType<PlayerSpaceshipController>();

            bool shouldShow = cameraFollow != null
                              && cameraFollow.enabled
                              && cameraFollow.firstPersonMode
                              && spaceship != null
                              && spaceship.gameObject.activeInHierarchy
                              && !spaceship.IsRespawning
                              && !AboutInfoStation.AnyStationActive
                              && !AboutInfoStation.AnyLandingSequenceActive
                              && !SpawnZoneAnimator.IsAnyIntroWalkActive;

            SetVisible(shouldShow);
            if (!shouldShow) return;

            UpdateSwayAndHardwareAnimation(dt);
            UpdateMfdTelemetry(dt);
            UpdateSpeedStreaks();
        }

        private void UpdateSwayAndHardwareAnimation(float dt)
        {
            if (_swayPivot == null || spaceship == null) return;

            float safeDt = Mathf.Max(0.001f, dt);

            // Quán tính G-force quanh buồng lái + độ lắc giật pháo laser / trúng đạn
            float targetRoll = Mathf.Clamp(-spaceship.CurrentBankAngle * 0.14f - spaceship.CurrentRollRate * 0.015f, -4.2f, 4.2f);
            float targetPitch = Mathf.Clamp((spaceship.IsBoosting ? 0.85f : spaceship.CurrentSpeedRatio * 0.40f) + spaceship.CurrentPitchRate * 0.014f, -2.5f, 2.5f);
            float targetYawShift = Mathf.Clamp(-spaceship.CurrentYawRate * 0.00018f, -0.014f, 0.014f);

            _fireRecoilKick = Mathf.Max(0f, _fireRecoilKick - safeDt * 10f);
            _damageShakeTimer = Mathf.Max(0f, _damageShakeTimer - safeDt);
            _muzzleFlashTimer = Mathf.Max(0f, _muzzleFlashTimer - safeDt);

            float targetZKick = (spaceship.IsBoosting ? -0.028f : -spaceship.CurrentSpeedRatio * 0.013f) - (_fireRecoilKick * 0.007f);

            float vib = spaceship.IsBoosting
                ? Mathf.Sin(Time.time * 46f) * 0.0009f
                : Mathf.Sin(Time.time * 14f) * 0.00022f;

            Vector3 hitJitter = Vector3.zero;
            if (_damageShakeTimer > 0f)
            {
                float dmgScale = (_damageShakeTimer / 0.35f) * 0.009f;
                hitJitter = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 55f, 2f) - 0.5f) * dmgScale,
                    (Mathf.PerlinNoise(5f, Time.time * 55f) - 0.5f) * dmgScale,
                    0f);
            }

            _smoothedRoll = Mathf.Lerp(_smoothedRoll, targetRoll, safeDt * 8f);
            _smoothedPitch = Mathf.Lerp(_smoothedPitch, targetPitch, safeDt * 8f);
            _smoothedYawShift = Mathf.Lerp(_smoothedYawShift, targetYawShift, safeDt * 7f);
            _smoothedZKick = Mathf.Lerp(_smoothedZKick, targetZKick, safeDt * 9f);

            _swayPivot.localPosition = new Vector3(_smoothedYawShift, vib, _smoothedZKick) + hitJitter;
            _swayPivot.localRotation = Quaternion.Euler(_smoothedPitch, 0f, _smoothedRoll);

            // Hoạt ảnh cần ga (Throttle Quadrant) trượt mượt theo lực đẩy
            if (_throttleHandlePivot != null)
            {
                float targetSlide = spaceship.IsBraking ? -0.022f : (spaceship.ThrottleRatio * 0.048f + (spaceship.IsBoosting ? 0.016f : 0f));
                _smoothedThrottleSlide = Mathf.Lerp(_smoothedThrottleSlide, targetSlide, safeDt * 10f);
                _throttleHandlePivot.localPosition = new Vector3(-0.335f, -0.396f + _smoothedThrottleSlide * 0.20f, 0.650f + _smoothedThrottleSlide);
            }

            // Hoạt ảnh cần lái máy bay (Flight Stick) nghiêng theo Pitch/Turn/Roll
            if (_flightStickPivot != null)
            {
                float stPitch = Mathf.Clamp(spaceship.PitchInput * 14f, -15f, 15f);
                float stRoll = Mathf.Clamp(-spaceship.TurnInput * 13f - spaceship.RollInput * 10f, -16f, 16f);
                _smoothedStickPitch = Mathf.Lerp(_smoothedStickPitch, stPitch, safeDt * 14f);
                _smoothedStickRoll = Mathf.Lerp(_smoothedStickRoll, stRoll, safeDt * 14f);
                _flightStickPivot.localRotation = Quaternion.Euler(_smoothedStickPitch, 0f, _smoothedStickRoll);
            }

            // Chớp sáng đầu nòng pháo plasma ở hai bên mũi khi khai hỏa
            if (_muzzleGlowMat != null)
            {
                float flash = _muzzleFlashTimer > 0f ? (_muzzleFlashTimer / 0.09f) : 0f;
                Color c = Color.Lerp(new Color(0.18f, 0.85f, 1.0f, 1f), new Color(0.75f, 1.0f, 1.0f, 1f), flash);
                _muzzleGlowMat.color = c;
                if (_muzzleGlowMat.HasProperty("_BaseColor")) _muzzleGlowMat.SetColor("_BaseColor", c);
            }
        }

        private void UpdateSpeedStreaks()
        {
            if (_speedStreaksPs == null || spaceship == null) return;
            var emission = _speedStreaksPs.emission;
            var main = _speedStreaksPs.main;
            float r = spaceship.CurrentSpeedRatio;

            if (spaceship.IsBoosting)
            {
                emission.rateOverTime = 72f;
                main.startSpeed = 26f;
                main.startColor = new Color(0.55f, 0.95f, 1f, 0.45f);
            }
            else if (r > 0.35f)
            {
                emission.rateOverTime = Mathf.Lerp(4f, 26f, (r - 0.35f) / 0.65f);
                main.startSpeed = Mathf.Lerp(10f, 18f, r);
                main.startColor = new Color(0.75f, 0.92f, 1f, 0.25f);
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }

        private void UpdateMfdTelemetry(float dt)
        {
            if (spaceship == null) return;

            // 1. Left MFD: SYS // DEFENSE (Segmented Hull & Shield, Ship Schematic, G-Force)
            float hpR = spaceship.HealthRatio;
            float shdR = spaceship.ShieldRatio;
            bool underFire = spaceship.IsUnderFire;

            Color hpFillCol = hpR < 0.3f ? new Color(1f, 0.30f, 0.30f, 0.95f) : new Color(0.86f, 0.96f, 1f, 0.95f);
            Color shdFillCol = shdR < 0.2f ? new Color(1f, 0.55f, 0.20f, 0.95f) : new Color(0.24f, 0.92f, 1f, 0.95f);
            UpdateSegmentedBar(_mfdHpCells, hpR, hpFillCol);
            UpdateSegmentedBar(_mfdShdCells, shdR, shdFillCol);

            if (_mfdHpValText != null) _mfdHpValText.text = $"{Mathf.RoundToInt(hpR * 100f)}%";
            if (_mfdShdValText != null) _mfdShdValText.text = $"{Mathf.RoundToInt(shdR * 100f)}%";

            if (_mfdGForceText != null)
            {
                float g = spaceship.CurrentGForce;
                _mfdGForceText.text = $"G-LOAD: {g:F1}G";
                _mfdGForceText.color = g > 4.2f ? new Color(1f, 0.72f, 0.24f, 0.95f) : new Color(0.65f, 0.92f, 1f, 0.90f);
            }

            if (_mfdShipShieldRing != null)
            {
                _mfdShipShieldRing.color = underFire
                    ? new Color(1f, 0.32f, 0.38f, 0.95f)
                    : (shdR > 0.05f ? new Color(0.24f, 0.92f, 1f, 0.78f) : new Color(0.5f, 0.25f, 0.25f, 0.35f));
            }
            if (_mfdShipCoreIcon != null)
            {
                _mfdShipCoreIcon.color = hpR < 0.35f ? new Color(1f, 0.35f, 0.30f, 1f) : new Color(0.82f, 0.96f, 1f, 0.92f);
            }

            if (_mfdSysStatusText != null)
            {
                if (underFire)
                {
                    _mfdSysStatusText.text = "! IMPACT ALERT !";
                    _mfdSysStatusText.color = new Color(1f, 0.32f, 0.42f, 1f);
                }
                else if (shdR <= 0.05f)
                {
                    _mfdSysStatusText.text = "SHIELD DEPLETED";
                    _mfdSysStatusText.color = new Color(1f, 0.60f, 0.20f, 1f);
                }
                else
                {
                    _mfdSysStatusText.text = "SHIELD: NOMINAL";
                    _mfdSysStatusText.color = new Color(0.28f, 0.95f, 1f, 0.92f);
                }
            }

            // Đèn trạng thái Caution LED trên táp-lô
            if (_cautionLedMat != null)
            {
                Color ledCol = underFire || hpR < 0.35f
                    ? new Color(1f, 0.24f, 0.28f, 1f)
                    : (spaceship.IsBoosting ? new Color(1f, 0.72f, 0.20f, 1f) : new Color(0.22f, 0.96f, 0.65f, 1f));
                _cautionLedMat.color = ledCol;
                if (_cautionLedMat.HasProperty("_BaseColor")) _cautionLedMat.SetColor("_BaseColor", ledCol);
            }

            // 2. Center MFD: TAC // RADAR & TARGET BLIPS
            if (_mfdRadarSweep != null)
            {
                _mfdRadarSweep.Rotate(0f, 0f, -105f * dt);
            }

            var lockSys = SpaceshipTargetLockSystem.Instance;
            bool hasLock = lockSys != null && lockSys.HasLockedTarget;
            float lockProg = lockSys != null ? lockSys.LockProgress : 0f;
            Transform lockedTr = hasLock ? lockSys.CurrentLockedTarget : null;

            if (_hoodTrimMat != null)
            {
                Color targetTrim = hasLock
                    ? new Color(1.0f, 0.62f, 0.16f, 1f)
                    : (lockProg > 0.01f ? new Color(1.0f, 0.80f, 0.22f, 1f) : new Color(0.22f, 0.92f, 1.0f, 1f));
                _hoodTrimMat.color = targetTrim;
                if (_hoodTrimMat.HasProperty("_BaseColor")) _hoodTrimMat.SetColor("_BaseColor", targetTrim);
            }

            UpdateRadarBlips(lockedTr);

            if (_mfdTacStateText != null && _mfdTacTargetText != null)
            {
                if (hasLock && lockedTr != null)
                {
                    float d = Vector3.Distance(spaceship.transform.position, lockedTr.position);
                    _mfdTacStateText.text = $"LOCKED [ {d:F0}M ]";
                    _mfdTacStateText.color = new Color(1f, 0.72f, 0.22f, 1f);

                    string nm = lockedTr.name.Replace("_Patrol", "").Replace("(Clone)", "").ToUpperInvariant();
                    if (nm.Length > 15) nm = nm.Substring(0, 15);
                    _mfdTacTargetText.text = nm;
                    _mfdTacTargetText.color = new Color(1f, 0.92f, 0.75f, 1f);
                    if (_mfdTacRingImg != null) _mfdTacRingImg.color = new Color(1f, 0.65f, 0.16f, 0.95f);
                }
                else if (lockProg > 0.01f)
                {
                    _mfdTacStateText.text = "ACQUIRING...";
                    _mfdTacStateText.color = new Color(1f, 0.85f, 0.25f, 1f);
                    _mfdTacTargetText.text = $"{Mathf.RoundToInt(lockProg * 100f)}%";
                    _mfdTacTargetText.color = new Color(1f, 0.85f, 0.25f, 1f);
                    if (_mfdTacRingImg != null) _mfdTacRingImg.color = new Color(1f, 0.82f, 0.24f, 0.9f);
                }
                else
                {
                    _mfdTacStateText.text = "SCANNING SECTOR";
                    _mfdTacStateText.color = new Color(0.28f, 0.92f, 1f, 0.92f);
                    _mfdTacTargetText.text = "200M SENSOR";
                    _mfdTacTargetText.color = new Color(0.55f, 0.88f, 1f, 0.75f);
                    if (_mfdTacRingImg != null) _mfdTacRingImg.color = new Color(0.24f, 0.86f, 1f, 0.72f);
                }
            }

            // 3. Right MFD: ENG // PROPULSION + Artificial Horizon
            int kmh = Mathf.RoundToInt(Mathf.Abs(spaceship.CurrentForwardSpeed) * 14f);
            if (_mfdSpeedValText != null)
            {
                _mfdSpeedValText.text = $"{kmh} KM/H";
                _mfdSpeedValText.color = spaceship.IsBoosting ? new Color(0.32f, 1f, 0.92f, 1f) : new Color(0.88f, 0.97f, 1f, 1f);
            }

            if (_mfdThrustModeText != null)
            {
                if (spaceship.IsBoosting)
                {
                    _mfdThrustModeText.text = "MODE: AFTERBURNER";
                    _mfdThrustModeText.color = new Color(1f, 0.65f, 0.22f, 1f);
                }
                else if (spaceship.IsBraking)
                {
                    _mfdThrustModeText.text = "MODE: RETRO BRAKE";
                    _mfdThrustModeText.color = new Color(1f, 0.45f, 0.45f, 1f);
                }
                else if (spaceship.IsHovering && Mathf.Abs(spaceship.CurrentForwardSpeed) < 3f)
                {
                    _mfdThrustModeText.text = "MODE: HOVER PAD";
                    _mfdThrustModeText.color = new Color(0.45f, 0.95f, 0.82f, 0.95f);
                }
                else if (spaceship.IsCoasting)
                {
                    _mfdThrustModeText.text = "MODE: SPACE GLIDE";
                    _mfdThrustModeText.color = new Color(0.45f, 0.88f, 1f, 0.90f);
                }
                else
                {
                    _mfdThrustModeText.text = "MODE: CRUISE THRUST";
                    _mfdThrustModeText.color = new Color(0.32f, 0.94f, 1f, 0.95f);
                }
            }

            Color boostCol = spaceship.IsBoosting ? new Color(1f, 0.56f, 0.18f, 0.96f) : new Color(0.24f, 0.92f, 1f, 0.95f);
            UpdateSegmentedBar(_mfdBoostCells, spaceship.BoostRatio, boostCol);

            Vector3 eul = spaceship.transform.eulerAngles;
            float pitchDeg = eul.x > 180f ? eul.x - 360f : eul.x;
            float rollDeg = eul.z > 180f ? eul.z - 360f : eul.z;

            if (_mfdHorizonPivot != null)
            {
                // Đường chân trời nhân tạo xoay ngược lại góc nghiêng cánh thực & tịnh tiến theo góc ngóc mũi
                float visualRoll = rollDeg + spaceship.CurrentBankAngle * 0.45f;
                _mfdHorizonPivot.localRotation = Quaternion.Euler(0f, 0f, -visualRoll);
                _mfdHorizonPivot.anchoredPosition = new Vector2(0f, Mathf.Clamp(pitchDeg * 0.65f, -24f, 24f));
            }

            if (_mfdAttitudeText != null)
            {
                _mfdAttitudeText.text = $"HDG {Mathf.RoundToInt(eul.y):000} | PT {-Mathf.RoundToInt(pitchDeg):+0;-0;0}";
            }
        }

        private void UpdateRadarBlips(Transform lockedTr)
        {
            if (_mfdRadarContainer == null || spaceship == null) return;

            var activeShips = SpaceTrafficShipAI.ActiveShips;
            int count = activeShips != null ? activeShips.Count : 0;
            float radarRange = 250f;
            float radarRadiusPx = 54f;

            int blipIdx = 0;
            for (int i = 0; i < count; i++)
            {
                var ai = activeShips[i];
                if (ai == null || !ai.gameObject.activeInHierarchy || ai.IsDestroyed) continue;

                Vector3 localPos = spaceship.transform.InverseTransformPoint(ai.transform.position);
                Vector2 flat = new Vector2(localPos.x, localPos.z);
                float dist = flat.magnitude;
                if (dist > radarRange) continue;

                Image blip = GetOrCreateRadarBlip(blipIdx++);
                if (!blip.gameObject.activeSelf) blip.gameObject.SetActive(true);

                Vector2 norm = flat / radarRange;
                blip.rectTransform.anchoredPosition = norm * radarRadiusPx;

                bool isLocked = (lockedTr != null && (ai.transform == lockedTr || ai.transform.IsChildOf(lockedTr)));
                blip.color = isLocked ? new Color(1f, 0.70f, 0.16f, 1f) : new Color(0.28f, 0.96f, 1f, 0.88f);
                float sz = isLocked ? 9.5f : 6.0f;
                blip.rectTransform.sizeDelta = new Vector2(sz, sz);

                if (blipIdx >= 10) break;
            }

            for (int i = blipIdx; i < _radarBlips.Count; i++)
            {
                if (_radarBlips[i].gameObject.activeSelf) _radarBlips[i].gameObject.SetActive(false);
            }
        }

        private Image GetOrCreateRadarBlip(int idx)
        {
            while (_radarBlips.Count <= idx)
            {
                var go = new GameObject("RadarBlip_" + _radarBlips.Count, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_mfdRadarContainer, false);
                var img = go.GetComponent<Image>();
                img.sprite = GetOrCreateBlipDotSprite();
                img.raycastTarget = false;
                img.rectTransform.sizeDelta = new Vector2(6f, 6f);
                _radarBlips.Add(img);
            }
            return _radarBlips[idx];
        }

        private static void UpdateSegmentedBar(Image[] cells, float ratio, Color activeCol)
        {
            if (cells == null || cells.Length == 0) return;
            float clamped = Mathf.Clamp01(ratio);
            int total = cells.Length;
            float activeFloat = clamped * total;
            Color dimCol = new Color(0.06f, 0.15f, 0.24f, 0.72f);

            for (int i = 0; i < total; i++)
            {
                if (cells[i] == null) continue;
                float cellFill = Mathf.Clamp01(activeFloat - i);
                cells[i].color = Color.Lerp(dimCol, activeCol, cellFill);
            }
        }

        private void EnsureCockpitBuilt()
        {
            if (_cockpitRoot != null) return;

            var existingSway = transform.Find("CockpitSwayPivot_1P");
            if (existingSway != null)
            {
                DestroyImmediate(existingSway.gameObject);
            }

            InitMaterials();

            var swayGo = new GameObject("CockpitSwayPivot_1P");
            swayGo.transform.SetParent(transform, false);
            swayGo.transform.localPosition = Vector3.zero;
            swayGo.transform.localRotation = Quaternion.identity;
            _swayPivot = swayGo.transform;

            _cockpitRoot = new GameObject("CockpitRig_1P");
            _cockpitRoot.transform.SetParent(_swayPivot, false);

            // Đèn rọi buồng lái dịu nhẹ giữ chi tiết kim loại phay xước tối màu
            var lightGo = new GameObject("CockpitInteriorLight");
            lightGo.transform.SetParent(_cockpitRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.14f, 0.44f);
            _cockpitFillLight = lightGo.AddComponent<Light>();
            _cockpitFillLight.type = LightType.Point;
            _cockpitFillLight.color = new Color(0.55f, 0.85f, 1.0f, 1f);
            _cockpitFillLight.intensity = 0.90f;
            _cockpitFillLight.range = 2.8f;
            _cockpitFillLight.shadows = LightShadows.None;

            BuildCockpitHullTub();
            BuildConsoleStructure();
            BuildCockpitHardwareDetails();
            BuildSideCowlsAndIntakes();
            BuildDashboardMFDs();
            BuildSpeedStreakParticles();
        }

        private void BuildCockpitHullTub()
        {
            // Sàn & thân vỏ khoang lái khép kín hoàn toàn (nằm gọn dưới tầm cần ga/cần lái/turbine để lộ trọn chi tiết cơ khí)
            CreatePart("Cockpit_Tub_Floor", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.568f, 0.62f), Quaternion.identity,
                new Vector3(2.90f, 0.145f, 2.00f), _armorDarkMat);

            // Sàn mũi vát thấp giữa hai họng turbine (giữ thoáng tầm nhìn bãi đỗ phía trước)
            CreatePart("Forward_Nose_Deck", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.495f, 1.32f), Quaternion.Euler(12f, 0f, 0f),
                new Vector3(0.96f, 0.085f, 0.84f), _armorDarkMat);
            CreatePart("Forward_Nose_Spine", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.452f, 1.34f), Quaternion.Euler(13f, 0f, 0f),
                new Vector3(0.24f, 0.040f, 0.68f), _armorTrimMat);

            for (int side = -1; side <= 1; side += 2)
            {
                string tag = side < 0 ? "L" : "R";

                // Bệ hông phẳng đỡ khít dưới đế Cần Ga (Throttle) & Cần Lái (Stick), bịt kín kẽ hở hai bên MFD
                CreatePart("Side_Console_Deck_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.44f, -0.480f, 0.64f), Quaternion.Euler(0f, side * -10f, 0f),
                    new Vector3(0.36f, 0.095f, 1.06f), _armorDarkMat);
                CreatePart("Side_Console_Bevel_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.425f, -0.432f, 0.44f), Quaternion.Euler(0f, side * -10f, 0f),
                    new Vector3(0.052f, 0.012f, 0.40f), _armorTrimMat);

                // Khoang đỡ thấp bịt kín dưới gầm ống hút gió turbine mà vẫn giữ nguyên khối trụ sọc kim loại nổi bật phía trên
                CreatePart("Intake_Bay_Bulkhead_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.60f, -0.515f, 0.96f), Quaternion.identity,
                    new Vector3(0.50f, 0.110f, 0.98f), _armorDarkMat);

                // Nẹp góc nhỏ giấu kín phía sau rìa ngoài MFD để bịt lỗ góc giáp mui
                CreatePart("Hood_Cheek_Seal_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.365f, -0.378f, 0.805f), Quaternion.Euler(4f, side * -18f, 0f),
                    new Vector3(0.16f, 0.115f, 0.10f), _armorDarkMat);

                // Bệ thành hông ngoài hạ thấp dưới vai giáp để chắn hoàn toàn kẽ hở mép màn hình (16:9 / 21:9 / Boost)
                CreatePart("Cockpit_Outer_Sill_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.96f, -0.506f, 0.58f), Quaternion.identity,
                    new Vector3(0.66f, 0.140f, 1.62f), _armorDarkMat);
            }
        }

        private static void SetPbrMaterial(Material mat, Color col, float metallic, float smoothness, Texture2D tex)
        {
            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                mat.mainTextureScale = new Vector2(2f, 2f);
            }
        }

        private void InitMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? lit;
            Texture2D panelTex = GetOrCreateMetalPanelTexture();

            _armorDarkMat = new Material(lit);
            SetPbrMaterial(_armorDarkMat, new Color(0.068f, 0.088f, 0.122f, 1f), 0.42f, 0.24f, panelTex);

            _armorTrimMat = new Material(lit);
            SetPbrMaterial(_armorTrimMat, new Color(0.125f, 0.168f, 0.232f, 1f), 0.52f, 0.30f, panelTex);

            _armorAccentMat = new Material(lit);
            SetPbrMaterial(_armorAccentMat, new Color(0.175f, 0.235f, 0.320f, 1f), 0.65f, 0.38f, panelTex);

            _cyanGlowMat = new Material(unlit);
            _cyanGlowMat.color = new Color(0.22f, 0.92f, 1.0f, 1f);
            if (_cyanGlowMat.HasProperty("_BaseColor")) _cyanGlowMat.SetColor("_BaseColor", new Color(0.22f, 0.92f, 1.0f, 1f));

            _hoodTrimMat = new Material(unlit);
            _hoodTrimMat.color = new Color(0.22f, 0.92f, 1.0f, 1f);
            if (_hoodTrimMat.HasProperty("_BaseColor")) _hoodTrimMat.SetColor("_BaseColor", new Color(0.22f, 0.92f, 1.0f, 1f));

            _muzzleGlowMat = new Material(unlit);
            _muzzleGlowMat.color = new Color(0.18f, 0.85f, 1.0f, 1f);
            if (_muzzleGlowMat.HasProperty("_BaseColor")) _muzzleGlowMat.SetColor("_BaseColor", new Color(0.18f, 0.85f, 1.0f, 1f));

            _cautionLedMat = new Material(unlit);
            _cautionLedMat.color = new Color(0.22f, 0.96f, 0.65f, 1f);
            if (_cautionLedMat.HasProperty("_BaseColor")) _cautionLedMat.SetColor("_BaseColor", new Color(0.22f, 0.96f, 0.65f, 1f));

            _screenGlassMat = new Material(unlit);
            _screenGlassMat.color = new Color(0.015f, 0.055f, 0.095f, 1f);
            if (_screenGlassMat.HasProperty("_BaseColor")) _screenGlassMat.SetColor("_BaseColor", new Color(0.015f, 0.055f, 0.095f, 1f));
        }

        private void BuildConsoleStructure()
        {
            // Khối đế bảng điều khiển rộng phủ kín toàn bộ phía sau 3 MFD & ăn khớp sang bệ hông hai bên
            CreatePart("Console_Core_Back", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.465f, 0.955f), Quaternion.Euler(6f, 0f, 0f),
                new Vector3(0.96f, 0.28f, 0.26f), _armorDarkMat);

            CreatePart("Console_Lower_Apron", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.484f, 0.72f), Quaternion.Euler(-6f, 0f, 0f),
                new Vector3(0.84f, 0.075f, 0.34f), _armorDarkMat);

            // Nẹp kín khe giữa MFD giữa và hai MFD cánh bên
            CreatePart("MFD_Joint_Backing_L", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(-0.112f, -0.368f, 0.752f), Quaternion.Euler(22f, -9f, 0f),
                new Vector3(0.058f, 0.148f, 0.045f), _armorDarkMat);
            CreatePart("MFD_Joint_Backing_R", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0.112f, -0.368f, 0.752f), Quaternion.Euler(22f, 9f, 0f),
                new Vector3(0.058f, 0.148f, 0.045f), _armorDarkMat);

            // Mui che nắng đa lớp vát cạnh sắc nét (Chamfered Stealth Glare Shield)
            CreatePart("Hood_Center", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.270f, 0.785f), Quaternion.Euler(6f, 0f, 0f),
                new Vector3(0.24f, 0.020f, 0.135f), _armorTrimMat);

            CreatePart("Hood_Center_TopRib", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.256f, 0.805f), Quaternion.Euler(8f, 0f, 0f),
                new Vector3(0.16f, 0.010f, 0.105f), _armorAccentMat);

            CreatePart("Hood_Left", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(-0.228f, -0.290f, 0.768f), Quaternion.Euler(6f, -20f, 11f),
                new Vector3(0.22f, 0.018f, 0.130f), _armorTrimMat);

            CreatePart("Hood_Right", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0.228f, -0.290f, 0.768f), Quaternion.Euler(6f, 20f, -11f),
                new Vector3(0.22f, 0.018f, 0.130f), _armorTrimMat);

            CreatePart("Hood_Corner_Cap_L", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(-0.320f, -0.304f, 0.748f), Quaternion.Euler(6f, -24f, 16f),
                new Vector3(0.085f, 0.020f, 0.125f), _armorTrimMat);
            CreatePart("Hood_Corner_Cap_R", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0.320f, -0.304f, 0.748f), Quaternion.Euler(6f, 24f, -16f),
                new Vector3(0.085f, 0.020f, 0.125f), _armorTrimMat);

            // Cụm thấu kính chiếu HUD quang học trên gờ mui giữa
            CreatePart("HUD_Projector_Cowl", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.258f, 0.742f), Quaternion.Euler(-14f, 0f, 0f),
                new Vector3(0.105f, 0.014f, 0.042f), _armorDarkMat);
            CreatePart("HUD_Projector_Lens", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.250f, 0.742f), Quaternion.Euler(-14f, 0f, 0f),
                new Vector3(0.082f, 0.004f, 0.022f), _cyanGlowMat);

            // Đường chỉ đèn Neon chạy dọc gờ mũi mui che nắng (chuyển hổ phách khi khóa mục tiêu)
            CreatePart("Hood_Trim_Center", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0f, -0.275f, 0.716f), Quaternion.identity,
                new Vector3(0.22f, 0.0045f, 0.006f), _hoodTrimMat);

            CreatePart("Hood_Trim_Left", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(-0.220f, -0.295f, 0.702f), Quaternion.Euler(0f, -20f, 11f),
                new Vector3(0.19f, 0.0045f, 0.006f), _hoodTrimMat);

            CreatePart("Hood_Trim_Right", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(0.220f, -0.295f, 0.702f), Quaternion.Euler(0f, 20f, -11f),
                new Vector3(0.19f, 0.0045f, 0.006f), _hoodTrimMat);
        }

        private void BuildCockpitHardwareDetails()
        {
            // 1. Cụm cần ga cơ khí (Throttle Quadrant) bên góc dưới trái
            CreatePart("Throttle_Rail_Housing", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(-0.335f, -0.415f, 0.670f), Quaternion.Euler(12f, -16f, 8f),
                new Vector3(0.048f, 0.024f, 0.145f), _armorDarkMat);
            CreatePart("Throttle_Slot_Glow", _cockpitRoot.transform, PrimitiveType.Cube,
                new Vector3(-0.335f, -0.402f, 0.670f), Quaternion.Euler(12f, -16f, 8f),
                new Vector3(0.006f, 0.003f, 0.120f), _cyanGlowMat);

            var thrHandle = new GameObject("Throttle_Grip_Pivot");
            thrHandle.transform.SetParent(_cockpitRoot.transform, false);
            thrHandle.transform.localPosition = new Vector3(-0.335f, -0.396f, 0.650f);
            thrHandle.transform.localRotation = Quaternion.Euler(0f, -16f, 8f);
            _throttleHandlePivot = thrHandle.transform;

            CreatePart("Throttle_Stem", _throttleHandlePivot, PrimitiveType.Cube,
                new Vector3(0f, -0.008f, 0f), Quaternion.identity, new Vector3(0.014f, 0.022f, 0.014f), _armorAccentMat);
            CreatePart("Throttle_Bar", _throttleHandlePivot, PrimitiveType.Cylinder,
                new Vector3(0f, 0.006f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.018f, 0.028f, 0.018f), _armorTrimMat);

            // 2. Cụm cần lái trung tâm-phải (Flight Stick) nghiêng động theo thao tác lái
            CreatePart("Stick_Boot_Collar", _cockpitRoot.transform, PrimitiveType.Cylinder,
                new Vector3(0.335f, -0.418f, 0.668f), Quaternion.Euler(8f, 16f, -8f),
                new Vector3(0.056f, 0.014f, 0.056f), _armorDarkMat);

            var stickGo = new GameObject("FlightStick_Pivot");
            stickGo.transform.SetParent(_cockpitRoot.transform, false);
            stickGo.transform.localPosition = new Vector3(0.335f, -0.410f, 0.668f);
            stickGo.transform.localRotation = Quaternion.identity;
            _flightStickPivot = stickGo.transform;

            CreatePart("Stick_Shaft", _flightStickPivot, PrimitiveType.Cylinder,
                new Vector3(0f, 0.022f, 0f), Quaternion.identity, new Vector3(0.016f, 0.024f, 0.016f), _armorAccentMat);
            CreatePart("Stick_Grip_Top", _flightStickPivot, PrimitiveType.Cube,
                new Vector3(0f, 0.044f, 0.006f), Quaternion.Euler(18f, 0f, 0f), new Vector3(0.026f, 0.028f, 0.032f), _armorTrimMat);
            CreatePart("Stick_FireTrigger_LED", _flightStickPivot, PrimitiveType.Cube,
                new Vector3(0f, 0.050f, -0.011f), Quaternion.Euler(18f, 0f, 0f), new Vector3(0.012f, 0.008f, 0.004f), _muzzleGlowMat);

            // 3. Hàng đèn LED trạng thái Caution / System dọc mép dưới mui táp-lô
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                Material m = Mathf.Abs(i) == 1 ? _cautionLedMat : _cyanGlowMat;
                CreatePart($"Status_LED_{i}", _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(i * 0.145f, -0.422f, 0.680f), Quaternion.identity,
                    new Vector3(0.014f, 0.005f, 0.006f), m);
            }
        }

        private void BuildSideCowlsAndIntakes()
        {
            // Hai cụm vỏ giáp vai & họng hút gió turbine có gờ sọc + lỗ thông nhiệt + đầu nòng pháo hai bên mũi
            for (int side = -1; side <= 1; side += 2)
            {
                string tag = side < 0 ? "L" : "R";

                // Thân vỏ giáp vai hai bên dưới
                CreatePart("Cowl_Body_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.76f, -0.44f, 1.12f), Quaternion.Euler(5f, side * -12f, side * -16f),
                    new Vector3(0.44f, 0.22f, 0.84f), _armorTrimMat);

                // Tấm ốp vát cạnh trên vai (chamfer ridge)
                CreatePart("Cowl_TopRidge_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.72f, -0.32f, 1.15f), Quaternion.Euler(4f, side * -10f, side * -28f),
                    new Vector3(0.18f, 0.022f, 0.76f), _armorAccentMat);

                // Tấm ốp mũi thuôn dài về phía trước
                CreatePart("Nose_Shoulder_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.88f, -0.40f, 1.54f), Quaternion.Euler(4f, side * 14f, side * -8f),
                    new Vector3(0.34f, 0.13f, 0.72f), _armorDarkMat);

                // Khe tản nhiệt xếp tầng trên giáp vai
                for (int v = 0; v < 4; v++)
                {
                    CreatePart($"Louver_Vent_{tag}_{v}", _cockpitRoot.transform, PrimitiveType.Cube,
                        new Vector3(side * 0.84f, -0.328f, 1.32f + v * 0.072f), Quaternion.Euler(24f, side * 12f, side * -8f),
                        new Vector3(0.12f, 0.012f, 0.036f), _armorAccentMat);
                }

                // Ống hút gió turbine kèm gờ sọc kim loại + lõi phát quang nhẹ bên trong
                CreatePart("Intake_Cylinder_" + tag, _cockpitRoot.transform, PrimitiveType.Cylinder,
                    new Vector3(side * 0.56f, -0.43f, 1.02f), Quaternion.Euler(90f, side * -10f, 0f),
                    new Vector3(0.23f, 0.28f, 0.23f), _armorDarkMat);
                CreatePart("Intake_CoreGlow_" + tag, _cockpitRoot.transform, PrimitiveType.Cylinder,
                    new Vector3(side * 0.56f, -0.43f, 1.24f), Quaternion.Euler(90f, side * -10f, 0f),
                    new Vector3(0.15f, 0.012f, 0.15f), _cyanGlowMat);

                for (int r = 0; r < 7; r++)
                {
                    CreatePart($"Intake_Rib_{tag}_{r}", _cockpitRoot.transform, PrimitiveType.Cylinder,
                        new Vector3(side * 0.56f, -0.425f, 0.84f + r * 0.050f), Quaternion.Euler(90f, side * -10f, 0f),
                        new Vector3(0.248f, 0.012f, 0.248f), _armorAccentMat);
                }

                // Khớp nối vây mũi & Nòng pháo Laser kép nhô ra ở hai bên rìa dưới mũi tàu
                CreatePart("Cannon_Mount_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.94f, -0.405f, 1.76f), Quaternion.Euler(2f, side * 4f, side * -6f),
                    new Vector3(0.085f, 0.075f, 0.36f), _armorTrimMat);
                CreatePart("Cannon_Barrel_" + tag, _cockpitRoot.transform, PrimitiveType.Cylinder,
                    new Vector3(side * 0.94f, -0.395f, 1.98f), Quaternion.Euler(90f, side * 3f, 0f),
                    new Vector3(0.044f, 0.18f, 0.044f), _armorAccentMat);
                CreatePart("Cannon_MuzzleGlow_" + tag, _cockpitRoot.transform, PrimitiveType.Cylinder,
                    new Vector3(side * 0.94f, -0.395f, 2.15f), Quaternion.Euler(90f, side * 3f, 0f),
                    new Vector3(0.036f, 0.025f, 0.036f), _muzzleGlowMat);

                // Khối bo góc thấp dưới kính buồng lái + đường nét LED dọc vai
                CreatePart("Lower_Corner_Fairing_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.78f, -0.31f, 0.68f), Quaternion.Euler(-12f, side * 18f, side * 24f),
                    new Vector3(0.18f, 0.045f, 0.34f), _armorDarkMat);
                CreatePart("Side_Glow_Strip_" + tag, _cockpitRoot.transform, PrimitiveType.Cube,
                    new Vector3(side * 0.635f, -0.325f, 1.26f), Quaternion.Euler(5f, side * 10f, side * -12f),
                    new Vector3(0.010f, 0.007f, 0.64f), _cyanGlowMat);
            }
        }

        private void BuildDashboardMFDs()
        {
            Vector3 eyeFocus = new Vector3(0f, -0.05f, 0f);

            // Màn hình Trái: SYS // DEFENSE (HP / SHIELD / Schematic / G-Load)
            Vector3 leftPos = new Vector3(-0.214f, -0.376f, 0.702f);
            Quaternion leftRot = Quaternion.LookRotation(leftPos - eyeFocus, Vector3.up);
            Transform leftPanel = CreateMfdPanel("MFD_Left", leftPos, leftRot, new Vector2(0.168f, 0.114f), out RectTransform leftRoot);
            BuildMfdBezel(leftPanel, 0.172f, 0.118f);
            PopulateLeftMfd(leftRoot);

            // Màn hình Giữa: TAC // RADAR (Radar Sweep + Live Target Blips + Lock Status)
            Vector3 centerPos = new Vector3(0f, -0.368f, 0.735f);
            Quaternion centerRot = Quaternion.LookRotation(centerPos - eyeFocus, Vector3.up);
            Transform centerPanel = CreateMfdPanel("MFD_Center", centerPos, centerRot, new Vector2(0.188f, 0.122f), out RectTransform centerRoot);
            BuildMfdBezel(centerPanel, 0.192f, 0.126f);
            PopulateCenterMfd(centerRoot);

            // Màn hình Phải: ENG // PROPULSION (Artificial Horizon / Speed / Thrust Mode / Boost)
            Vector3 rightPos = new Vector3(0.214f, -0.376f, 0.702f);
            Quaternion rightRot = Quaternion.LookRotation(rightPos - eyeFocus, Vector3.up);
            Transform rightPanel = CreateMfdPanel("MFD_Right", rightPos, rightRot, new Vector2(0.168f, 0.114f), out RectTransform rightRoot);
            BuildMfdBezel(rightPanel, 0.172f, 0.118f);
            PopulateRightMfd(rightRoot);
        }

        private Transform CreateMfdPanel(string name, Vector3 localPos, Quaternion localRot, Vector2 worldSize, out RectTransform uiRoot)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(_cockpitRoot.transform, false);
            holder.transform.localPosition = localPos;
            holder.transform.localRotation = localRot;

            // Vỏ lưng khay màn hình nằm phía sau (+Z local)
            CreatePart("HousingBack", holder.transform, PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.022f), Quaternion.identity,
                new Vector3(worldSize.x + 0.018f, worldSize.y + 0.018f, 0.036f), _armorDarkMat);

            // Nền kính tối
            CreatePart("GlassBacking", holder.transform, PrimitiveType.Quad,
                new Vector3(0f, 0f, 0.002f), Quaternion.identity,
                new Vector3(worldSize.x, worldSize.y, 1f), _screenGlassMat);

            // Canvas WorldSpace độ phân giải 440x300 crisp
            var canvasGo = new GameObject("MFD_Canvas", typeof(Canvas));
            canvasGo.transform.SetParent(holder.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.004f);
            canvasGo.transform.localRotation = Quaternion.identity;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = GetComponent<Camera>();
            canvas.sortingOrder = 10;

            var cRt = canvas.GetComponent<RectTransform>();
            float resW = 440f;
            float resH = resW * (worldSize.y / worldSize.x);
            cRt.sizeDelta = new Vector2(resW, resH);
            float scale = worldSize.x / resW;
            cRt.localScale = new Vector3(scale, scale, scale);

            var bgImgGo = new GameObject("BgTint", typeof(RectTransform), typeof(Image));
            bgImgGo.transform.SetParent(cRt, false);
            var bgRt = bgImgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgImgGo.GetComponent<Image>();
            bgImg.color = new Color(0.015f, 0.095f, 0.175f, 0.95f);
            bgImg.raycastTarget = false;

            // Lớp phủ đường quét CRT/OLED Scanlines
            var scanGo = new GameObject("ScanlineOverlay", typeof(RectTransform), typeof(Image));
            scanGo.transform.SetParent(cRt, false);
            var scanRt = scanGo.GetComponent<RectTransform>();
            scanRt.anchorMin = Vector2.zero;
            scanRt.anchorMax = Vector2.one;
            scanRt.offsetMin = Vector2.zero;
            scanRt.offsetMax = Vector2.zero;
            var scanImg = scanGo.GetComponent<Image>();
            scanImg.sprite = GetOrCreateScanlineSprite();
            scanImg.type = Image.Type.Tiled;
            scanImg.color = new Color(0.25f, 0.92f, 1f, 0.14f);
            scanImg.raycastTarget = false;

            uiRoot = cRt;
            return holder.transform;
        }

        private void BuildMfdBezel(Transform parent, float w, float h)
        {
            float t = 0.0045f;
            float d = 0.008f;
            CreatePart("FrameTop", parent, PrimitiveType.Cube, new Vector3(0f, h * 0.5f, -0.002f), Quaternion.identity, new Vector3(w, t, d), _cyanGlowMat);
            CreatePart("FrameBot", parent, PrimitiveType.Cube, new Vector3(0f, -h * 0.5f, -0.002f), Quaternion.identity, new Vector3(w, t, d), _cyanGlowMat);
            CreatePart("FrameLeft", parent, PrimitiveType.Cube, new Vector3(-w * 0.5f, 0f, -0.002f), Quaternion.identity, new Vector3(t, h, d), _cyanGlowMat);
            CreatePart("FrameRight", parent, PrimitiveType.Cube, new Vector3(w * 0.5f, 0f, -0.002f), Quaternion.identity, new Vector3(t, h, d), _cyanGlowMat);
        }

        private void PopulateLeftMfd(RectTransform root)
        {
            CreateMfdText("Hdr", root, new Vector2(0f, 114f), new Vector2(400f, 30f), "SYS // DEFENSE", 22f, new Color(0.32f, 0.94f, 1f, 0.96f), TextAlignmentOptions.Center);

            // Sơ đồ khung phi thuyền top-down + vòng khiên phát sáng bên trái màn hình
            var shipBox = new GameObject("ShipSchematic", typeof(RectTransform));
            shipBox.transform.SetParent(root, false);
            var sbRt = shipBox.GetComponent<RectTransform>();
            sbRt.anchoredPosition = new Vector2(-128f, 14f);
            sbRt.sizeDelta = new Vector2(118f, 118f);

            var ringGo = new GameObject("ShieldOutline", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(sbRt, false);
            _mfdShipShieldRing = ringGo.GetComponent<Image>();
            _mfdShipShieldRing.sprite = GetOrCreateMfdCircleSprite();
            _mfdShipShieldRing.color = new Color(0.24f, 0.92f, 1f, 0.78f);
            _mfdShipShieldRing.raycastTarget = false;
            _mfdShipShieldRing.rectTransform.sizeDelta = new Vector2(106f, 106f);

            var coreGo = new GameObject("ShipSilhouette", typeof(RectTransform), typeof(Image));
            coreGo.transform.SetParent(sbRt, false);
            _mfdShipCoreIcon = coreGo.GetComponent<Image>();
            _mfdShipCoreIcon.sprite = GetOrCreateShipSilhouetteSprite();
            _mfdShipCoreIcon.color = new Color(0.82f, 0.96f, 1f, 0.95f);
            _mfdShipCoreIcon.raycastTarget = false;
            _mfdShipCoreIcon.rectTransform.sizeDelta = new Vector2(78f, 78f);

            // Cột phải: Thanh phân đoạn HULL & SHD + G-LOAD
            CreateMfdText("HpLbl", root, new Vector2(12f, 66f), new Vector2(100f, 26f), "HULL", 20f, Color.white, TextAlignmentOptions.MidlineLeft);
            _mfdHpValText = CreateMfdText("HpVal", root, new Vector2(148f, 66f), new Vector2(100f, 26f), "100%", 20f, new Color(0.86f, 0.96f, 1f, 1f), TextAlignmentOptions.MidlineRight);
            _mfdHpCells = CreateMfdSegmentedBar("HpCells", root, new Vector2(78f, 36f), 220f, 18f, 8);

            CreateMfdText("ShdLbl", root, new Vector2(12f, -2f), new Vector2(100f, 26f), "SHD", 20f, new Color(0.28f, 0.92f, 1f, 1f), TextAlignmentOptions.MidlineLeft);
            _mfdShdValText = CreateMfdText("ShdVal", root, new Vector2(148f, -2f), new Vector2(100f, 26f), "100%", 20f, new Color(0.28f, 0.92f, 1f, 1f), TextAlignmentOptions.MidlineRight);
            _mfdShdCells = CreateMfdSegmentedBar("ShdCells", root, new Vector2(78f, -32f), 220f, 18f, 8);

            _mfdGForceText = CreateMfdText("GLoad", root, new Vector2(-128f, -72f), new Vector2(150f, 24f), "G-LOAD: 1.0G", 17f, new Color(0.65f, 0.92f, 1f, 0.90f), TextAlignmentOptions.Center);
            _mfdSysStatusText = CreateMfdText("SysStat", root, new Vector2(78f, -86f), new Vector2(230f, 28f), "SHIELD: NOMINAL", 19f, new Color(0.28f, 0.95f, 1f, 0.95f), TextAlignmentOptions.Center);
        }

        private void PopulateCenterMfd(RectTransform root)
        {
            CreateMfdText("Hdr", root, new Vector2(0f, 108f), new Vector2(400f, 28f), "TAC // RADAR", 22f, new Color(0.32f, 0.94f, 1f, 0.96f), TextAlignmentOptions.Center);

            var radarRoot = new GameObject("RadarContainer", typeof(RectTransform));
            radarRoot.transform.SetParent(root, false);
            _mfdRadarContainer = radarRoot.GetComponent<RectTransform>();
            _mfdRadarContainer.anchoredPosition = new Vector2(0f, 12f);
            _mfdRadarContainer.sizeDelta = new Vector2(128f, 128f);

            // Chữ thập trục radar
            var axisH = new GameObject("AxisH", typeof(RectTransform), typeof(Image));
            axisH.transform.SetParent(_mfdRadarContainer, false);
            var hImg = axisH.GetComponent<Image>();
            hImg.color = new Color(0.24f, 0.86f, 1f, 0.28f);
            hImg.raycastTarget = false;
            hImg.rectTransform.sizeDelta = new Vector2(120f, 1.5f);

            var axisV = new GameObject("AxisV", typeof(RectTransform), typeof(Image));
            axisV.transform.SetParent(_mfdRadarContainer, false);
            var vImg = axisV.GetComponent<Image>();
            vImg.color = new Color(0.24f, 0.86f, 1f, 0.28f);
            vImg.raycastTarget = false;
            vImg.rectTransform.sizeDelta = new Vector2(1.5f, 120f);

            var ringGo = new GameObject("TacRingOuter", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(_mfdRadarContainer, false);
            _mfdTacRingImg = ringGo.GetComponent<Image>();
            _mfdTacRingImg.sprite = GetOrCreateMfdCircleSprite();
            _mfdTacRingImg.color = new Color(0.24f, 0.86f, 1f, 0.78f);
            _mfdTacRingImg.raycastTarget = false;
            _mfdTacRingImg.rectTransform.sizeDelta = new Vector2(126f, 126f);

            var innerRingGo = new GameObject("TacRingInner", typeof(RectTransform), typeof(Image));
            innerRingGo.transform.SetParent(_mfdRadarContainer, false);
            var innerImg = innerRingGo.GetComponent<Image>();
            innerImg.sprite = GetOrCreateMfdCircleSprite();
            innerImg.color = new Color(0.24f, 0.86f, 1f, 0.42f);
            innerImg.raycastTarget = false;
            innerImg.rectTransform.sizeDelta = new Vector2(68f, 68f);

            // Quạt quét radar xoay mượt (Sector Gradient)
            var sweepGo = new GameObject("RadarSweepSector", typeof(RectTransform), typeof(Image));
            sweepGo.transform.SetParent(_mfdRadarContainer, false);
            _mfdRadarSweep = sweepGo.GetComponent<RectTransform>();
            _mfdRadarSweep.sizeDelta = new Vector2(122f, 122f);
            var sweepImg = sweepGo.GetComponent<Image>();
            sweepImg.sprite = GetOrCreateRadarSweepSprite();
            sweepImg.color = new Color(0.32f, 1f, 0.94f, 0.48f);
            sweepImg.raycastTarget = false;

            _mfdTacStateText = CreateMfdText("TacState", root, new Vector2(0f, -76f), new Vector2(400f, 28f), "SCANNING SECTOR", 21f, new Color(0.28f, 0.92f, 1f, 0.96f), TextAlignmentOptions.Center);
            _mfdTacTargetText = CreateMfdText("TacTarget", root, new Vector2(0f, -104f), new Vector2(400f, 24f), "200M SENSOR", 17f, new Color(0.55f, 0.88f, 1f, 0.82f), TextAlignmentOptions.Center);
        }

        private void PopulateRightMfd(RectTransform root)
        {
            CreateMfdText("Hdr", root, new Vector2(0f, 114f), new Vector2(400f, 30f), "ENG // PROPULSION", 22f, new Color(0.32f, 0.94f, 1f, 0.96f), TextAlignmentOptions.Center);

            // Khung Đường Chân Trời Nhân Tạo (Artificial Horizon Viewport) bên trái màn hình phải
            var ahMaskGo = new GameObject("HorizonViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            ahMaskGo.transform.SetParent(root, false);
            var ahRt = ahMaskGo.GetComponent<RectTransform>();
            ahRt.anchoredPosition = new Vector2(-128f, 14f);
            ahRt.sizeDelta = new Vector2(114f, 114f);
            var ahBg = ahMaskGo.GetComponent<Image>();
            ahBg.color = new Color(0.02f, 0.08f, 0.14f, 0.85f);
            ahBg.raycastTarget = false;

            var ahRing = new GameObject("HorizonRim", typeof(RectTransform), typeof(Image));
            ahRing.transform.SetParent(ahRt, false);
            var rimImg = ahRing.GetComponent<Image>();
            rimImg.sprite = GetOrCreateMfdCircleSprite();
            rimImg.color = new Color(0.26f, 0.88f, 1f, 0.55f);
            rimImg.raycastTarget = false;
            rimImg.rectTransform.sizeDelta = new Vector2(112f, 112f);

            var hPivotGo = new GameObject("HorizonPivot", typeof(RectTransform));
            hPivotGo.transform.SetParent(ahRt, false);
            _mfdHorizonPivot = hPivotGo.GetComponent<RectTransform>();
            _mfdHorizonPivot.sizeDelta = new Vector2(120f, 80f);

            var hLine = new GameObject("HorizonLine", typeof(RectTransform), typeof(Image));
            hLine.transform.SetParent(_mfdHorizonPivot, false);
            var hlImg = hLine.GetComponent<Image>();
            hlImg.color = new Color(0.32f, 0.96f, 1f, 0.92f);
            hlImg.raycastTarget = false;
            hlImg.rectTransform.sizeDelta = new Vector2(98f, 2.5f);

            var hUpRung = new GameObject("PitchUp", typeof(RectTransform), typeof(Image));
            hUpRung.transform.SetParent(_mfdHorizonPivot, false);
            var urImg = hUpRung.GetComponent<Image>();
            urImg.color = new Color(0.32f, 0.96f, 1f, 0.55f);
            urImg.raycastTarget = false;
            urImg.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            urImg.rectTransform.sizeDelta = new Vector2(44f, 1.8f);

            var hDnRung = new GameObject("PitchDn", typeof(RectTransform), typeof(Image));
            hDnRung.transform.SetParent(_mfdHorizonPivot, false);
            var drImg = hDnRung.GetComponent<Image>();
            drImg.color = new Color(1f, 0.65f, 0.22f, 0.55f);
            drImg.raycastTarget = false;
            drImg.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            drImg.rectTransform.sizeDelta = new Vector2(44f, 1.8f);

            var datum = new GameObject("AircraftDatum", typeof(RectTransform), typeof(Image));
            datum.transform.SetParent(ahRt, false);
            var dtImg = datum.GetComponent<Image>();
            dtImg.color = new Color(1f, 0.80f, 0.24f, 0.95f);
            dtImg.raycastTarget = false;
            dtImg.rectTransform.sizeDelta = new Vector2(22f, 3f);

            // Cột phải: Tốc độ + Chế độ + Thanh chia ô Boost + HDG/PT
            _mfdSpeedValText = CreateMfdText("SpdVal", root, new Vector2(78f, 52f), new Vector2(230f, 44f), "0 KM/H", 34f, new Color(0.92f, 0.98f, 1f, 1f), TextAlignmentOptions.Center);
            _mfdThrustModeText = CreateMfdText("ThrustMode", root, new Vector2(78f, 12f), new Vector2(230f, 24f), "MODE: HOVER PAD", 16f, new Color(0.45f, 0.95f, 0.82f, 0.95f), TextAlignmentOptions.Center);

            CreateMfdText("BstLbl", root, new Vector2(78f, -18f), new Vector2(230f, 22f), "AFTERBURNER RESERVE", 15f, new Color(0.52f, 0.90f, 1f, 0.88f), TextAlignmentOptions.Center);
            _mfdBoostCells = CreateMfdSegmentedBar("BstCells", root, new Vector2(78f, -44f), 220f, 18f, 8);

            _mfdAttitudeText = CreateMfdText("AttVal", root, new Vector2(0f, -96f), new Vector2(390f, 28f), "HDG 000 | PT 0", 19f, new Color(0.75f, 0.92f, 1f, 0.92f), TextAlignmentOptions.Center);
        }

        private void BuildSpeedStreakParticles()
        {
            var psGo = new GameObject("CockpitSpeedStreaks_PS");
            psGo.transform.SetParent(_cockpitRoot.transform, false);
            psGo.transform.localPosition = new Vector3(0f, 0f, 6.5f);
            psGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            _speedStreaksPs = psGo.AddComponent<ParticleSystem>();
            var main = _speedStreaksPs.main;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed = 16f;
            main.startSize = 0.025f;
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new Color(0.55f, 0.95f, 1f, 0.35f);

            var shape = _speedStreaksPs.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 2.4f;
            shape.donutRadius = 0.85f;

            var emission = _speedStreaksPs.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var renderer = psGo.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 5.5f;
                renderer.velocityScale = 0.14f;
                Shader unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit != null)
                {
                    var pMat = new Material(unlit);
                    pMat.color = Color.white;
                    renderer.sharedMaterial = pMat;
                }
            }
        }

        private static TMP_Text CreateMfdText(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color col, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = col;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image[] CreateMfdSegmentedBar(string name, Transform parent, Vector2 pos, float totalWidth, float height, int cellCount)
        {
            var container = new GameObject(name + "_Track", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(parent, false);
            var rt = container.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(totalWidth, height);
            var bg = container.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.08f, 0.14f, 0.88f);
            bg.raycastTarget = false;

            Image[] cells = new Image[cellCount];
            float pad = 3f;
            float gap = 3.5f;
            float usableW = totalWidth - pad * 2f - gap * (cellCount - 1);
            float cellW = usableW / cellCount;
            float startX = -totalWidth * 0.5f + pad + cellW * 0.5f;

            for (int i = 0; i < cellCount; i++)
            {
                var cGo = new GameObject("Cell_" + i, typeof(RectTransform), typeof(Image));
                cGo.transform.SetParent(rt, false);
                var cRt = cGo.GetComponent<RectTransform>();
                cRt.anchoredPosition = new Vector2(startX + i * (cellW + gap), 0f);
                cRt.sizeDelta = new Vector2(cellW, height - 4f);
                var img = cGo.GetComponent<Image>();
                img.sprite = GetOrCreateSolidWhiteSprite();
                img.color = new Color(0.24f, 0.92f, 1f, 0.95f);
                img.raycastTarget = false;
                cells[i] = img;
            }
            return cells;
        }

        private static Texture2D GetOrCreateMetalPanelTexture()
        {
            if (_metalPanelTex != null) return _metalPanelTex;
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                bool hSeam = (y == 0 || y == 1 || y == 128);
                for (int x = 0; x < size; x++)
                {
                    bool vSeam = (x == 0 || x == 1 || x == 128);
                    float grain = (Mathf.PerlinNoise(x * 0.18f, y * 1.4f) - 0.5f) * 0.10f;
                    float baseVal = 0.86f + grain;
                    if (hSeam || vSeam) baseVal *= 0.48f;
                    else if ((x == 8 || x == 120 || x == 136 || x == 248) && (y == 8 || y == 120 || y == 136 || y == 248)) baseVal *= 0.62f;
                    px[y * size + x] = new Color(baseVal, baseVal, baseVal, 1f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _metalPanelTex = tex;
            return _metalPanelTex;
        }

        private static Sprite GetOrCreateScanlineSprite()
        {
            if (_scanlineSprite != null) return _scanlineSprite;
            if (_scanlineTex == null)
            {
                _scanlineTex = new Texture2D(4, 8, TextureFormat.RGBA32, false);
                _scanlineTex.wrapMode = TextureWrapMode.Repeat;
                Color[] px = new Color[4 * 8];
                for (int y = 0; y < 8; y++)
                {
                    float a = (y % 4 == 0) ? 0.55f : 0.08f;
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(1f, 1f, 1f, a);
                }
                _scanlineTex.SetPixels(px);
                _scanlineTex.Apply();
            }
            _scanlineSprite = Sprite.Create(_scanlineTex, new Rect(0, 0, 4, 8), new Vector2(0.5f, 0.5f), 100f);
            return _scanlineSprite;
        }

        private static Sprite GetOrCreateSolidWhiteSprite()
        {
            if (_solidWhiteSprite != null) return _solidWhiteSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var cols = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(cols);
            tex.Apply();
            _solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            return _solidWhiteSprite;
        }

        private static Sprite GetOrCreateBlipDotSprite()
        {
            if (_blipDotSprite != null) return _blipDotSprite;
            int sz = 32;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = sz * 0.5f;
            float r = sz * 0.36f;
            Color[] px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float a = 1f - Mathf.Clamp01((d - r) / 2.2f);
                    px[y * sz + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _blipDotSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            return _blipDotSprite;
        }

        private static Sprite GetOrCreateShipSilhouetteSprite()
        {
            if (_shipSilhouetteSprite != null) return _shipSilhouetteSprite;
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[sz * sz];

            for (int y = 0; y < sz; y++)
            {
                float ny = (y / (float)(sz - 1)) * 2f - 1f; // -1 đuôi -> +1 mũi
                for (int x = 0; x < sz; x++)
                {
                    float nx = Mathf.Abs((x / (float)(sz - 1)) * 2f - 1f);

                    // Thân tiêm kích thuôn nhọn (nose -> tail)
                    float bodyHalfW = ny > 0.1f ? Mathf.Lerp(0.18f, 0.03f, (ny - 0.1f) / 0.82f) : 0.18f;
                    bool inFuselage = (ny >= -0.78f && ny <= 0.88f && nx <= bodyHalfW);

                    // Đôi cánh xuôi sau (swept delta wings)
                    float wingFrontY = 0.25f - nx * 0.72f;
                    float wingBackY = -0.48f - nx * 0.16f;
                    bool inWings = (nx <= 0.86f && ny <= wingFrontY && ny >= wingBackY);

                    // Đuôi ống xả phản lực đôi
                    bool inNozzle = (ny >= -0.84f && ny < -0.58f && nx >= 0.08f && nx <= 0.24f);

                    px[y * sz + x] = (inFuselage || inWings || inNozzle) ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _shipSilhouetteSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            return _shipSilhouetteSprite;
        }

        private static Sprite GetOrCreateRadarSweepSprite()
        {
            if (_radarSweepSprite != null) return _radarSweepSprite;
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            float maxR = size * 0.48f;
            Color[] px = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > maxR || d < 3f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    // Quạt quét góc 0..65 độ (mờ dần về cạnh sau)
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;
                    float wedge = (angle >= 25f && angle <= 90f) ? (angle - 25f) / 65f : 0f;
                    float radial = Mathf.Lerp(0.35f, 1f, d / maxR);
                    float edgeFade = 1f - Mathf.Clamp01((d - (maxR - 2f)) / 2f);
                    px[y * size + x] = new Color(1f, 1f, 1f, wedge * wedge * radial * edgeFade);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _radarSweepSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _radarSweepSprite;
        }

        private static Sprite GetOrCreateMfdCircleSprite()
        {
            if (_radarCircleSprite != null) return _radarCircleSprite;
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float c = size * 0.5f;
            float r = size * 0.44f;
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float err = Mathf.Abs(d - r);
                    float a = 1f - Mathf.Clamp01((err - 2.0f) / 1.8f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _radarCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _radarCircleSprite;
        }

        private static GameObject CreatePart(string name, Transform parent, PrimitiveType type, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }
            return go;
        }
    }
}
