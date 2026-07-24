using Timberborn.ModManagerScene;
using UnityEngine;

namespace TimberbornAutopilot
{
    public class AutopilotModStarter : IModStarter
    {
        public void StartMod(IModEnvironment modEnvironment)
        {
            Debug.Log("[Autopilot] Loaded from " + modEnvironment.ModPath);
        }
    }
}
