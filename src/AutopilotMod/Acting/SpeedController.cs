using Timberborn.TimeSystem;
using UnityEngine;

namespace TimberbornAutopilot.Acting
{
    /// <summary>Game speed control beyond the UI's 3x cap.</summary>
    public class SpeedController
    {
        private const float MaxSpeed = 30f;

        private readonly SpeedManager _speedManager;

        public SpeedController(SpeedManager speedManager)
        {
            _speedManager = speedManager;
        }

        public float CurrentSpeed => _speedManager.CurrentSpeed;

        public void SetSpeed(float speed)
        {
            float clamped = Mathf.Clamp(speed, 0f, MaxSpeed);
            // The user's accessibility "speed scale" setting silently lerps
            // any speed >1x back toward 1x — force full scale first.
            _speedManager.ChangeSpeedScale(1f);
            _speedManager.ChangeSpeed(clamped);
            Debug.Log($"[Autopilot] Game speed -> {clamped}x (scale forced to 1)");
        }
    }
}
