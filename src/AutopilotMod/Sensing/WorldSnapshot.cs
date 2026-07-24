using System.Collections.Generic;

namespace TimberbornAutopilot.Sensing
{
    /// <summary>
    /// Plain-data snapshot of everything the planner needs, taken once per
    /// game day (and on demand via the HTTP endpoint).
    /// </summary>
    public class WorldSnapshot
    {
        // Time
        public int Cycle;
        public int CycleDay;
        public float HoursPassedToday;

        // Weather
        public bool IsHazardousWeather;
        public string NextHazard;            // "Drought" / "Badtide" (this cycle's hazard)
        public int HazardDurationDays;
        public int TemperateDurationDays;
        public int DaysUntilHazard;          // 0 while hazard is active

        // Population
        public int Adults;
        public int Children;
        public int Bots;
        public int FreeBeds;
        public int Homeless;
        public int UnemployedAdults;
        public int FreeWorkslots;
        public int ContaminatedBeavers;
        public int AverageWellbeing;

        // Economy
        public int SciencePoints;
        public Dictionary<string, int> Stocks = new Dictionary<string, int>();

        // Survival math (STRATEGY.md constants: 2.67 food, 2.12 water per beaver-day)
        public int WaterStock;
        public int FoodStock;
        public float WaterDaysLeft;
        public float FoodDaysLeft;
        public float WaterTargetForHazard;   // pop * 2.12 * hazardDays * 1.25
        public float FoodTargetForHazard;    // pop * 2.67 * hazardDays * 1.25
    }
}
