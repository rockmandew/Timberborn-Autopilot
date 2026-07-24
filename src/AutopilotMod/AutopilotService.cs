using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using UnityEngine;

namespace TimberbornAutopilot
{
    /// <summary>
    /// The autopilot brain. Runs once per game tick; will host the
    /// sense -> plan -> act loop (economy, water engineering, city planning).
    /// </summary>
    public class AutopilotService : ILoadableSingleton, ITickableSingleton
    {
        private readonly EventBus _eventBus;
        private int _ticks;

        public AutopilotService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Load()
        {
            _eventBus.Register(this);
            Debug.Log("[Autopilot] Game context loaded, autopilot service armed.");
        }

        public void Tick()
        {
            _ticks++;
            if (_ticks % 100 == 0)
            {
                Debug.Log($"[Autopilot] Heartbeat: {_ticks} ticks.");
            }
        }
    }
}
