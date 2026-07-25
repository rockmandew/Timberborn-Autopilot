using System;
using System.IO;
using Newtonsoft.Json;
using Timberborn.PlatformUtilities;
using UnityEngine;

namespace TimberbornAutopilot.Training
{
    /// <summary>
    /// Every tunable strategy number, externalized so the training loop can
    /// mutate them between episodes. Loaded from Documents\Timberborn\Autopilot\
    /// params.json; missing file or fields fall back to these defaults.
    /// </summary>
    public class AutopilotParams
    {
        public int TreeMarkRadius = 12;
        public int CarrotZoneHalf = 5;
        public int PineZoneHalf = 6;
        public int LumberjackTarget = 2;
        public int TankTarget = 2;
        public int LodgeTarget = 2;
        public float SecondPumpWaterDays = 1.5f;
        public int SecondPumpEarliestDay = 3;
        public int PlanningTickInterval = 30;
        public int PumpSearchRadius = 30;
        public int DefaultSearchRadius = 15;

        /// <summary>DI creates this with defaults; AutopilotService copies the
        /// disk values in at game load.</summary>
        public void CopyFrom(AutopilotParams other)
        {
            TreeMarkRadius = other.TreeMarkRadius;
            CarrotZoneHalf = other.CarrotZoneHalf;
            PineZoneHalf = other.PineZoneHalf;
            LumberjackTarget = other.LumberjackTarget;
            TankTarget = other.TankTarget;
            LodgeTarget = other.LodgeTarget;
            SecondPumpWaterDays = other.SecondPumpWaterDays;
            SecondPumpEarliestDay = other.SecondPumpEarliestDay;
            PlanningTickInterval = other.PlanningTickInterval;
            PumpSearchRadius = other.PumpSearchRadius;
            DefaultSearchRadius = other.DefaultSearchRadius;
        }

        public static string Directory => Path.Combine(UserDataFolder.Folder, "Autopilot");
        public static string FilePath => Path.Combine(Directory, "params.json");

        public static AutopilotParams Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonConvert.DeserializeObject<AutopilotParams>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        Debug.Log("[Autopilot] Loaded tunable params from " + FilePath);
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Autopilot] Failed to load params.json, using defaults: " + e.Message);
            }
            var defaults = new AutopilotParams();
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(defaults, Formatting.Indented));
            }
            catch (Exception)
            {
                // Read-only documents folder is survivable; defaults still apply.
            }
            return defaults;
        }
    }
}
