using System;
using System.IO;
using Newtonsoft.Json;
using Timberborn.PlatformUtilities;
using UnityEngine;

namespace TimberbornAutopilot.Training
{
    /// <summary>
    /// Training-mode switch and episode settings, read from
    /// Documents\Timberborn\Autopilot\training.json. The watchdog script writes
    /// Enabled=true before each launch and false when training stops, so normal
    /// play is never hijacked.
    /// </summary>
    public class TrainingConfig
    {
        public bool Enabled = false;
        public string FactionId = "Folktails";
        public string MapName = "Plains";
        public string SettlementName = "TrainingRun";
        public int MaxCycles = 6;
        public float GameSpeed = 10f;

        public static string FilePath => Path.Combine(AutopilotParams.Directory, "training.json");

        public static TrainingConfig Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonConvert.DeserializeObject<TrainingConfig>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Autopilot] Failed to load training.json: " + e.Message);
            }
            return new TrainingConfig();
        }
    }
}
