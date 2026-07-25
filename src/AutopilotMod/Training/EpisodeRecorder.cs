using System;
using System.IO;
using Newtonsoft.Json;
using Timberborn.GameCycleSystem;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using TimberbornAutopilot.Acting;
using TimberbornAutopilot.Planning;
using TimberbornAutopilot.Sensing;
using UnityEngine;

namespace TimberbornAutopilot.Training
{
    /// <summary>
    /// Training-mode episode lifecycle: snapshots the success measures every
    /// game day into an episode log, detects success (wellbeing target = the
    /// Iron Teeth unlock), failure (extinction), or the cycle horizon; scores
    /// the run, writes last-result.json for the watchdog, and exits the game.
    /// Inert unless training.json says Enabled.
    /// </summary>
    public class EpisodeRecorder : ILoadableSingleton, ITickableSingleton
    {
        private bool _speedApplied;
        private readonly EventBus _eventBus;
        private readonly WorldModel _worldModel;
        private readonly SpeedController _speedController;
        private readonly BrainLog _brainLog;

        private TrainingConfig _config;
        private string _episodePath;
        private int _daysElapsed;
        private int _consecutiveDryDays;
        private bool _ended;

        public EpisodeRecorder(EventBus eventBus, WorldModel worldModel,
                               SpeedController speedController, BrainLog brainLog)
        {
            _eventBus = eventBus;
            _worldModel = worldModel;
            _speedController = speedController;
            _brainLog = brainLog;
        }

        public void Load()
        {
            _config = TrainingConfig.Load();
            if (!_config.Enabled)
            {
                return;
            }
            Directory.CreateDirectory(Path.Combine(AutopilotParams.Directory, "episodes"));
            _episodePath = Path.Combine(AutopilotParams.Directory, "episodes",
                $"episode_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl");
            _eventBus.Register(this);
            _brainLog.Note($"TRAINING EPISODE started — target: wellbeing " +
                           $"{_worldModel.Snapshot().WellbeingUnlockTarget}, horizon {_config.MaxCycles} cycles.");
        }

        /// <summary>Speed must be applied AFTER the game finishes its own load-time
        /// speed reset — first tick is the earliest safe moment.</summary>
        public void Tick()
        {
            if (!_speedApplied && _config.Enabled && !_ended)
            {
                _speedApplied = true;
                _speedController.SetSpeed(_config.GameSpeed);
            }
        }

        [OnEvent]
        public void OnCycleDayStarted(CycleDayStartedEvent cycleDayStartedEvent)
        {
            if (_ended || !_config.Enabled)
            {
                return;
            }
            _daysElapsed++;
            // Re-assert speed daily in case anything reset it.
            _speedController.SetSpeed(_config.GameSpeed);
            WorldSnapshot s = _worldModel.Snapshot();
            AppendEpisodeLine(new
            {
                day = _daysElapsed,
                cycle = s.Cycle,
                cycleDay = s.CycleDay,
                adults = s.Adults,
                children = s.Children,
                wellbeing = s.AverageWellbeing,
                water = s.WaterStock,
                waterDays = s.WaterDaysLeft,
                food = s.FoodStock,
                foodDays = s.FoodDaysLeft,
                science = s.SciencePoints,
                homeless = s.Homeless,
                contaminated = s.ContaminatedBeavers,
            });

            _consecutiveDryDays = (s.WaterStock == 0 && s.FoodStock == 0 && _daysElapsed > 3)
                ? _consecutiveDryDays + 1 : 0;

            if (s.AverageWellbeing >= s.WellbeingUnlockTarget)
            {
                EndEpisode("success", 10000 - _daysElapsed * 10, s,
                    $"Reached wellbeing {s.AverageWellbeing} in {_daysElapsed} days");
            }
            else if (s.Adults + s.Children == 0)
            {
                EndEpisode("extinct", -1000 + _daysElapsed, s, "Colony died out");
            }
            else if (_consecutiveDryDays >= 3)
            {
                // Death spiral: no food AND no water for 3 straight days —
                // end early instead of burning wall-clock on a doomed run.
                EndEpisode("starving", -500 + _daysElapsed, s,
                    $"Death spiral: dry for {_consecutiveDryDays} days at day {_daysElapsed}");
            }
            else if (s.Cycle > _config.MaxCycles)
            {
                EndEpisode("horizon", s.AverageWellbeing * 100 + (s.Adults + s.Children) * 10, s,
                    $"Horizon: wellbeing {s.AverageWellbeing} after {_daysElapsed} days");
            }
        }

        private void EndEpisode(string result, int score, WorldSnapshot s, string reason)
        {
            _ended = true;
            var summary = new
            {
                result,
                score,
                reason,
                days = _daysElapsed,
                finalWellbeing = s.AverageWellbeing,
                finalPopulation = s.Adults + s.Children,
                finalWater = s.WaterStock,
                finalFood = s.FoodStock,
                science = s.SciencePoints,
                settlement = _config.SettlementName,
                endedAt = DateTime.Now.ToString("o"),
            };
            AppendEpisodeLine(summary);
            File.WriteAllText(Path.Combine(AutopilotParams.Directory, "last-result.json"),
                JsonConvert.SerializeObject(summary, Formatting.Indented));
            Debug.Log($"[Autopilot:Training] EPISODE END — {result} (score {score}): {reason}. Exiting game.");
            Application.Quit();
        }

        private void AppendEpisodeLine(object record)
        {
            try
            {
                File.AppendAllText(_episodePath, JsonConvert.SerializeObject(record) + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Autopilot:Training] Episode log write failed: " + e.Message);
            }
        }
    }
}
