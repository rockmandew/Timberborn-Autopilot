using System;
using System.Collections.Generic;
using Timberborn.GameCycleSystem;
using Timberborn.Goods;
using Timberborn.HazardousWeatherSystem;
using Timberborn.Population;
using Timberborn.ResourceCountingSystem;
using Timberborn.ScienceSystem;
using Timberborn.TimeSystem;
using Timberborn.WeatherSystem;
using Timberborn.Wellbeing;
using TimberbornAutopilot.Planning;

namespace TimberbornAutopilot.Sensing
{
    /// <summary>
    /// Reads live game state into a WorldSnapshot. All reads go through the
    /// same singletons the game's own UI uses, so numbers match what a
    /// player sees.
    /// </summary>
    public class WorldModel
    {
        private const float FoodPerBeaverDay = 2.67f;
        private const float WaterPerBeaverDay = 2.12f;
        private const float SafetyBuffer = 1.25f;

        private readonly GameCycleService _gameCycleService;
        private readonly IDayNightCycle _dayNightCycle;
        private readonly WeatherService _weatherService;
        private readonly HazardousWeatherService _hazardousWeatherService;
        private readonly PopulationService _populationService;
        private readonly WellbeingService _wellbeingService;
        private readonly ScienceService _scienceService;
        private readonly ResourceCountingService _resourceCountingService;
        private readonly IGoodService _goodService;
        private readonly CampaignPlanner _campaignPlanner;

        private List<string> _foodGoodIds;
        private List<string> _waterGoodIds;

        public WorldModel(GameCycleService gameCycleService,
                          IDayNightCycle dayNightCycle,
                          WeatherService weatherService,
                          HazardousWeatherService hazardousWeatherService,
                          PopulationService populationService,
                          WellbeingService wellbeingService,
                          ScienceService scienceService,
                          ResourceCountingService resourceCountingService,
                          IGoodService goodService,
                          CampaignPlanner campaignPlanner)
        {
            _gameCycleService = gameCycleService;
            _dayNightCycle = dayNightCycle;
            _weatherService = weatherService;
            _hazardousWeatherService = hazardousWeatherService;
            _populationService = populationService;
            _wellbeingService = wellbeingService;
            _scienceService = scienceService;
            _resourceCountingService = resourceCountingService;
            _goodService = goodService;
            _campaignPlanner = campaignPlanner;
        }

        public WorldSnapshot Snapshot()
        {
            ClassifyGoodsOnce();
            var snapshot = new WorldSnapshot();
            ReadCampaign(snapshot);
            ReadTimeAndWeather(snapshot);
            ReadPopulation(snapshot);
            ReadEconomy(snapshot);
            ComputeSurvivalMath(snapshot);
            return snapshot;
        }

        private void ReadCampaign(WorldSnapshot s)
        {
            s.Faction = _campaignPlanner.CurrentFactionId;
            s.Objective = _campaignPlanner.Objective.ToString();
            s.IronTeethUnlocked = _campaignPlanner.IronTeethUnlocked;
            s.WellbeingUnlockTarget = _campaignPlanner.WellbeingUnlockTarget;
        }

        private void ReadTimeAndWeather(WorldSnapshot s)
        {
            s.Cycle = _gameCycleService.Cycle;
            s.CycleDay = _gameCycleService.CycleDay;
            s.HoursPassedToday = _dayNightCycle.HoursPassedToday;

            s.IsHazardousWeather = _weatherService.IsHazardousWeather;
            s.NextHazard = _hazardousWeatherService.CurrentCycleHazardousWeather?.Id ?? "Unknown";
            s.HazardDurationDays = _weatherService.HazardousWeatherDuration;
            s.TemperateDurationDays = _weatherService.TemperateWeatherDuration;
            s.DaysUntilHazard = Math.Max(0, _weatherService.HazardousWeatherStartCycleDay - s.CycleDay);
        }

        private void ReadPopulation(WorldSnapshot s)
        {
            PopulationData pop = _populationService.GlobalPopulationData;
            s.Adults = pop.NumberOfAdults;
            s.Children = pop.NumberOfChildren;
            s.Bots = pop.NumberOfBots;
            s.FreeBeds = pop.BedData.FreeBeds;
            s.Homeless = pop.BedData.Homeless;
            s.UnemployedAdults = pop.BeaverWorkplaceData.Unemployed;
            s.FreeWorkslots = pop.BeaverWorkplaceData.FreeWorkslots + pop.BotWorkplaceData.FreeWorkslots;
            s.ContaminatedBeavers = pop.ContaminationData.ContaminatedAdults + pop.ContaminationData.ContaminatedChildren;
            s.AverageWellbeing = _wellbeingService.AverageGlobalWellbeing;
        }

        private void ReadEconomy(WorldSnapshot s)
        {
            s.SciencePoints = _scienceService.SciencePoints;
            foreach (string goodId in _goodService.Goods)
            {
                int available = _resourceCountingService.GetGlobalResourceCount(goodId).AvailableStock;
                if (available > 0)
                {
                    s.Stocks[goodId] = available;
                }
            }
        }

        private void ComputeSurvivalMath(WorldSnapshot s)
        {
            s.WaterStock = SumStocks(s, _waterGoodIds);
            s.FoodStock = SumStocks(s, _foodGoodIds);

            int beavers = s.Adults + s.Children;
            if (beavers > 0)
            {
                s.WaterDaysLeft = s.WaterStock / (beavers * WaterPerBeaverDay);
                s.FoodDaysLeft = s.FoodStock / (beavers * FoodPerBeaverDay);
            }
            s.WaterTargetForHazard = beavers * WaterPerBeaverDay * s.HazardDurationDays * SafetyBuffer;
            s.FoodTargetForHazard = beavers * FoodPerBeaverDay * s.HazardDurationDays * SafetyBuffer;
        }

        private static int SumStocks(WorldSnapshot s, List<string> goodIds)
        {
            int total = 0;
            foreach (string id in goodIds)
            {
                if (s.Stocks.TryGetValue(id, out int amount))
                {
                    total += amount;
                }
            }
            return total;
        }

        /// <summary>
        /// A good counts as food/water if consuming it restores the Hunger/Thirst
        /// need — data-driven, so it holds for both factions and future patches.
        /// </summary>
        private void ClassifyGoodsOnce()
        {
            if (_foodGoodIds != null)
            {
                return;
            }
            _foodGoodIds = new List<string>();
            _waterGoodIds = new List<string>();
            foreach (string goodId in _goodService.Goods)
            {
                GoodSpec spec = _goodService.GetGood(goodId);
                foreach (var effect in spec.ConsumptionEffects)
                {
                    if (effect.Points <= 0 || string.IsNullOrEmpty(effect.NeedId))
                    {
                        continue;
                    }
                    if (effect.NeedId.IndexOf("Hunger", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _foodGoodIds.Add(goodId);
                        break;
                    }
                    if (effect.NeedId.IndexOf("Thirst", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _waterGoodIds.Add(goodId);
                        break;
                    }
                }
            }
        }
    }
}
