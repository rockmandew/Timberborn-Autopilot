namespace TimberbornAutopilot.Planning
{
    /// <summary>The campaign path the autopilot follows for the current run.</summary>
    public enum RunObjective
    {
        /// <summary>Fresh start: play Folktails, rush average wellbeing to the
        /// Iron Teeth unlock threshold, sweep compatible achievements.</summary>
        FolktailsUnlockRush,

        /// <summary>Iron Teeth available and selected: full max-optimization run
        /// (STRATEGY.md) — deterministic growth, reservoirs, bots, wonder.</summary>
        IronTeethMaxRun,

        /// <summary>Playing Folktails with Iron Teeth already unlocked:
        /// optimize this colony (wonder, structures) but recommend the
        /// Iron Teeth path for maximum long-term output.</summary>
        FolktailsMaxRun,
    }
}
