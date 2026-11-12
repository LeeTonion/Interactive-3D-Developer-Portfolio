using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpaceCyberpunk
{
    /// <summary>
    /// Trạm Cấp Năng Lượng & Sửa Chữa Phi Thuyền (Spaceship Quantum Recharge & Repair Station).
    /// - Đặt tại vị trí chiến lược giữa hành lang bay không gian.
    /// - Khi tàu người chơi bay vào vùng trạm:
    ///   * Kích hoạt 2 chùm tia Laser Plasma Năng Lượng (Energy Tether Beams) cắm vào thân tàu.
    ///   * Hồi phục Khiên Năng Lượng & Máu Thân Tàu (Shield & Hull Repair) cực nhanh.
    ///   * Hiệu ứng hạt tia điện hồi phục và âm thanh / ánh sáng năng lượng xanh neon rực rỡ.
    ///   * Bảng Hologram HUD hiển thị trạng thái sạc: [ ⚡ DOCKING RECHARGE ACTIVE // 100% MAXIMUM ].
    /// </summary>
    public class SpaceshipRechargeStation : MonoBehaviour
    {
        [Header("Thông Số Nạp Năng Lượng")]
        public float rechargeRate = 75f; // Máu/giây
        public float triggerRadius = 8.5f;

        [Header("Điểm Bắn Tia Sạc Năng Lượng")]
        public Transform leftPylonEmitter;
        public Transform rightPylonEmitter;

        [Header("LineRenderers Chùm Tia Nạp")]
        public LineRenderer leftBeam;
        public LineRenderer rightBeam;

        [Header("Hiệu Ứng Ánh Sáng Trạm Sạc")]
        public Light coreDockLight;
        public Color idleColor = new Color(0f, 0.75f, 1f, 1f);
        public Color activeRechargeColor = new Color(0.1f, 1f, 0.45f, 1f);

        [Header("Bảng Thông Báo Hologram")]
        public TMP_Text statusHoloText;

        private PlayerSpaceshipController _activePlayer;
        private bool _isRecharging = false;

        private void Start()
        {
            SetupBeams();
        }

        private void SetupBeams()
        {
            if (leftBeam != null)
            {
                leftBeam.positionCount = 2;
                leftBeam.enabled = false;
            }
            if (rightBeam != null)
            {
                rightBeam.positionCount = 2;
                rightBeam.enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerSpaceshipController>();
            if (player != null)
            {
                _activePlayer = player;
                _isRecharging = true;
                if (leftBeam != null) leftBeam.enabled = true;
                if (rightBeam != null) rightBeam.enabled = true;
                if (coreDockLight != null) coreDockLight.color = activeRechargeColor;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerSpaceshipController>();
            if (player != null && player == _activePlayer)
            {
                _activePlayer = null;
                _isRecharging = false;
                if (leftBeam != null) leftBeam.enabled = false;
                if (rightBeam != null) rightBeam.enabled = false;
                if (coreDockLight != null) coreDockLight.color = idleColor;
                if (statusHoloText != null) statusHoloText.text = "⚡ QUANTUM RECHARGE DOCK\n[ STANDBY // ENTER TO RECHARGE ]";
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;

            if (_isRecharging && _activePlayer != null)
            {
                // Nạp năng lượng & sửa chữa thân tàu
                _activePlayer.RechargeEnergy(rechargeRate);

                Vector3 targetPos = _activePlayer.transform.position;

                // Cập nhật 2 chùm tia laser năng lượng
                if (leftBeam != null && leftPylonEmitter != null)
                {
                    leftBeam.SetPosition(0, leftPylonEmitter.position);
                    leftBeam.SetPosition(1, targetPos + _activePlayer.transform.right * -0.6f);
                    float w = 0.18f + 0.06f * Mathf.Sin(time * 25f);
                    leftBeam.startWidth = w;
                    leftBeam.endWidth = w * 0.4f;
                }

                if (rightBeam != null && rightPylonEmitter != null)
                {
                    rightBeam.SetPosition(0, rightPylonEmitter.position);
                    rightBeam.SetPosition(1, targetPos + _activePlayer.transform.right * 0.6f);
                    float w = 0.18f + 0.06f * Mathf.Sin(time * 25f + 1f);
                    rightBeam.startWidth = w;
                    rightBeam.endWidth = w * 0.4f;
                }

                if (statusHoloText != null)
                {
                    int hpPercent = Mathf.CeilToInt(_activePlayer.HealthRatio * 100f);
                    statusHoloText.text = $"⚡ RECHARGING ENERGY // INTEGRITY: {hpPercent}%\n[ SHIELD & HULL REPAIR ACTIVE ]";
                    statusHoloText.color = activeRechargeColor;
                }

                if (coreDockLight != null)
                {
                    coreDockLight.intensity = 5.0f + 2.0f * Mathf.Sin(time * 15f);
                }
            }
            else
            {
                if (coreDockLight != null)
                {
                    coreDockLight.intensity = 3.5f + 0.8f * Mathf.Sin(time * 3f);
                }
            }
        }
    }
}

