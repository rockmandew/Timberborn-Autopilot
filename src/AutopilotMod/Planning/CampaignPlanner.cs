using Timberborn.FactionSystem;
using Timberborn.GameFactionSystem;
using Timberborn.SingletonSystem;
using Timberborn.Wellbeing;
using UnityEngine;

namespace TimberbornAutopilot.Planning
{
    /// <summary>
    /// Picks the campaign path for the current settlement (CAMPAIGN.md):
    /// fresh start -> Folktails wellbeing rush to unlock Iron Teeth;
    /// Iron Teeth start -> straight to the max-optimization run.
    /// </summary>
    public class CampaignPlanner : ILoadableSingleton
    {
        private const string FolktailsId = "Folktails";
        private const string IronTeethId = "IronTeeth";
        private const int FallbackWellbeingTarget = 15;

        private readonly FactionService _factionService;
        private readonly FactionSpecService _factionSpecService;
        private readonly FactionUnlockingService _factionUnlockingService;
        private readonly WellbeingService _wellbeingService;
        private readonly EventBus _eventBus;

        public RunObjective Objective { get; private set; }
        public string CurrentFactionId { get; private set; }
        public bool IronTeethUnlocked { get; private set; }
        public int WellbeingUnlockTarget { get; private set; } = FallbackWellbeingTarget;

        public CampaignPlanner(FactionService factionService,
                               FactionSpecService factionSpecService,
                               FactionUnlockingService factionUnlockingService,
                               WellbeingService wellbeingService,
                               EventBus eventBus)
        {
            _factionService = factionService;
            _factionSpecService = factionSpecService;
            _factionUnlockingService = factionUnlockingService;
            _wellbeingService = wellbeingService;
            _eventBus = eventBus;
        }

        public void Load()
        {
            _eventBus.Register(this);
            CurrentFactionId = _factionService.Current.Id;
            ReadIronTeethUnlockState();
            Objective = DecideObjective();
            LogDecision();
        }

        public string DescribeProgress()
        {
            if (Objective == RunObjective.FolktailsUnlockRush)
            {
                return $"unlock rush: wellbeing {_wellbeingService.AverageGlobalWellbeing}/{WellbeingUnlockTarget}";
            }
            return Objective.ToString();
        }

        [OnEvent]
        public void OnFactionUnlocked(FactionUnlockedEvent factionUnlockedEvent)
        {
            if (factionUnlockedEvent.Faction.Id == IronTeethId)
            {
                IronTeethUnlocked = true;
                if (Objective == RunObjective.FolktailsUnlockRush)
                {
                    Objective = RunObjective.FolktailsMaxRun;
                    Debug.Log("[Autopilot] *** IRON TEETH UNLOCKED — primary objective complete! " +
                              "Continuing this colony as FolktailsMaxRun; start an Iron Teeth colony " +
                              "whenever you're ready for the max run. ***");
                }
            }
        }

        private RunObjective DecideObjective()
        {
            if (CurrentFactionId == IronTeethId)
            {
                return RunObjective.IronTeethMaxRun;
            }
            return IronTeethUnlocked ? RunObjective.FolktailsMaxRun : RunObjective.FolktailsUnlockRush;
        }

        private void ReadIronTeethUnlockState()
        {
            FactionSpec ironTeeth = _factionSpecService.GetFaction(IronTeethId);
            IronTeethUnlocked = !_factionUnlockingService.IsLocked(ironTeeth);
            UnlockableFactionSpec unlockable = ironTeeth.GetSpec<UnlockableFactionSpec>();
            if (unlockable != null && unlockable.AverageWellbeingToUnlock > 0)
            {
                WellbeingUnlockTarget = unlockable.AverageWellbeingToUnlock;
            }
        }

        private void LogDecision()
        {
            string path = Objective switch
            {
                RunObjective.FolktailsUnlockRush =>
                    $"FRESH START path: Folktails, rush avg wellbeing {WellbeingUnlockTarget} to unlock Iron Teeth",
                RunObjective.IronTeethMaxRun =>
                    "IRON TEETH path: maximum-optimization run",
                _ =>
                    "Folktails colony with Iron Teeth already unlocked: optimizing here; " +
                    "Iron Teeth colony recommended for absolute max output",
            };
            Debug.Log($"[Autopilot] Campaign: faction={CurrentFactionId}, ironTeethUnlocked={IronTeethUnlocked} -> {path}");
        }
    }
}
