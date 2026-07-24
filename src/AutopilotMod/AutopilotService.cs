using System.Linq;
using Timberborn.GameCycleSystem;
using Timberborn.HttpApiSystem;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using TimberbornAutopilot.Http;
using TimberbornAutopilot.Planning;
using TimberbornAutopilot.Sensing;
using UnityEngine;

namespace TimberbornAutopilot
{
    /// <summary>
    /// The autopilot brain. Runs once per game tick; hosts the
    /// sense -> plan -> act loop (economy, water engineering, city planning).
    /// v0.2: sense layer — daily world reports + live HTTP status endpoint.
    /// </summary>
    public class AutopilotService : ILoadableSingleton, ITickableSingleton, IUpdatableSingleton
    {
        private readonly EventBus _eventBus;
        private readonly WorldModel _worldModel;
        private readonly HttpApi _httpApi;
        private readonly CampaignPlanner _campaignPlanner;
        private readonly AutopilotCommandEndpoint _commandEndpoint;

        public AutopilotService(EventBus eventBus, WorldModel worldModel, HttpApi httpApi,
                                CampaignPlanner campaignPlanner, AutopilotCommandEndpoint commandEndpoint)
        {
            _eventBus = eventBus;
            _worldModel = worldModel;
            _httpApi = httpApi;
            _campaignPlanner = campaignPlanner;
            _commandEndpoint = commandEndpoint;
        }

        public void Load()
        {
            _eventBus.Register(this);
            if (!_httpApi.IsRunning)
            {
                _httpApi.Start();
            }
            Debug.Log("[Autopilot] Armed. Live status: " + _httpApi.Url + "api/autopilot/status");
        }

        public void Tick()
        {
            // Planner loop lands here in v0.5.
        }

        /// <summary>Runs every frame, even while the game is paused — remote
        /// commands work at any time.</summary>
        public void UpdateSingleton()
        {
            _commandEndpoint.ExecutePending();
        }

        [OnEvent]
        public void OnCycleDayStarted(CycleDayStartedEvent cycleDayStartedEvent)
        {
            LogDailyReport();
        }

        private void LogDailyReport()
        {
            WorldSnapshot s = _worldModel.Snapshot();
            string hazard = s.IsHazardousWeather
                ? $"{s.NextHazard} ACTIVE ({s.HazardDurationDays}d total)"
                : $"{s.NextHazard} in {s.DaysUntilHazard}d for {s.HazardDurationDays}d";
            string topStocks = string.Join(", ",
                s.Stocks.OrderByDescending(kv => kv.Value).Take(5).Select(kv => $"{kv.Key}:{kv.Value}"));

            Debug.Log(
                $"[Autopilot] C{s.Cycle}D{s.CycleDay} | pop {s.Adults}+{s.Children}k+{s.Bots}b " +
                $"wb {s.AverageWellbeing} | water {s.WaterStock} ({s.WaterDaysLeft:F1}d, need {s.WaterTargetForHazard:F0}) | " +
                $"food {s.FoodStock} ({s.FoodDaysLeft:F1}d, need {s.FoodTargetForHazard:F0}) | " +
                $"sci {s.SciencePoints} | {hazard} | {topStocks} | {_campaignPlanner.DescribeProgress()}");
        }
    }
}
