using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.InventorySystem;
using Timberborn.PrioritySystem;
using Timberborn.StockpilePrioritySystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace TimberbornAutopilot.Acting
{
    /// <summary>Adjusts existing buildings: work priority, worker count, pause state.</summary>
    public class CrewManager
    {
        private readonly IBlockService _blockService;

        public CrewManager(IBlockService blockService)
        {
            _blockService = blockService;
        }

        public bool TrySetWorkplacePriority(Vector3Int coordinates, Priority priority)
        {
            var workplacePriority = FindComponentAt<WorkplacePriority>(coordinates);
            if (workplacePriority == null)
            {
                return false;
            }
            workplacePriority.SetPriority(priority);
            return true;
        }

        public bool TrySetDesiredWorkers(Vector3Int coordinates, int desired)
        {
            var workplace = FindComponentAt<Workplace>(coordinates);
            if (workplace == null)
            {
                return false;
            }
            while (workplace.DesiredWorkers < desired && workplace.DesiredWorkers < workplace.MaxWorkers)
            {
                workplace.IncreaseDesiredWorkers();
            }
            while (workplace.DesiredWorkers > desired && workplace.DesiredWorkers > 1)
            {
                workplace.DecreaseDesiredWorkers();
            }
            return true;
        }

        /// <summary>Assigns a good to a storage building and sets its hauling mode:
        /// "accept" (default), "obtain" (actively haul in), "supply", or "empty".</summary>
        public bool TryConfigureStorage(Vector3Int coordinates, string goodId, string mode = "accept")
        {
            var allower = FindComponentAt<SingleGoodAllower>(coordinates);
            if (allower == null)
            {
                return false;
            }
            allower.Allow(goodId);
            var stockpilePriority = FindComponentAt<StockpilePriority>(coordinates);
            if (stockpilePriority != null)
            {
                switch (mode)
                {
                    case "obtain": stockpilePriority.Obtain(); break;
                    case "supply": stockpilePriority.Supply(); break;
                    case "empty": stockpilePriority.Empty(); break;
                    default: stockpilePriority.Accept(); break;
                }
            }
            return true;
        }

        public bool TrySetPaused(Vector3Int coordinates, bool paused)
        {
            var pausable = FindComponentAt<PausableBuilding>(coordinates);
            if (pausable == null)
            {
                return false;
            }
            if (paused)
            {
                pausable.Pause();
            }
            else
            {
                pausable.Resume();
            }
            return true;
        }

        private T FindComponentAt<T>(Vector3Int coordinates) where T : class
        {
            foreach (BlockObject blockObject in _blockService.GetObjectsAt(coordinates))
            {
                var component = blockObject.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }
            return null;
        }
    }
}
