using System;
using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.BuilderPrioritySystem;
using Timberborn.Buildings;
using Timberborn.Coordinates;
using Timberborn.GameFactionSystem;
using Timberborn.PrioritySystem;
using Timberborn.ScienceSystem;
using UnityEngine;

namespace TimberbornAutopilot.Acting
{
    /// <summary>
    /// Places buildings and paths programmatically — the same pipeline the
    /// game's build tool uses, so construction sites, costs, and beaver
    /// builder behavior are all vanilla.
    /// </summary>
    public class BuildPlacer
    {
        private readonly BuildingService _buildingService;
        private readonly BlockObjectPlacerService _blockObjectPlacerService;
        private readonly BlockValidator _blockValidator;
        private readonly BuildingUnlockingService _buildingUnlockingService;
        private readonly ScienceService _scienceService;
        private readonly FactionService _factionService;

        public BuildPlacer(BuildingService buildingService,
                           BlockObjectPlacerService blockObjectPlacerService,
                           BlockValidator blockValidator,
                           BuildingUnlockingService buildingUnlockingService,
                           ScienceService scienceService,
                           FactionService factionService)
        {
            _buildingService = buildingService;
            _blockObjectPlacerService = blockObjectPlacerService;
            _blockValidator = blockValidator;
            _buildingUnlockingService = buildingUnlockingService;
            _scienceService = scienceService;
            _factionService = factionService;
        }

        /// <summary>
        /// Places a building by base template name ("WaterPump", "Path", ...) at the
        /// given tile. The faction suffix (".Folktails") is appended automatically
        /// when needed. Returns false with a reason instead of throwing.
        /// </summary>
        public bool TryPlace(string templateName, Vector3Int coordinates, Orientation orientation,
                             Priority builderPriority, out string error)
        {
            BuildingSpec buildingSpec = ResolveTemplate(templateName);
            if (buildingSpec == null)
            {
                error = $"Unknown building template '{templateName}'";
                return false;
            }
            if (!EnsureUnlocked(buildingSpec, out error))
            {
                return false;
            }

            BlockObjectSpec blockObjectSpec = buildingSpec.GetSpec<BlockObjectSpec>();
            var placement = new Placement(
                new Vector3Int(coordinates.x, coordinates.y, coordinates.z - blockObjectSpec.BaseZ),
                orientation, FlipMode.Unflipped);

            if (!_blockValidator.BlocksValid(blockObjectSpec, placement))
            {
                error = $"Invalid placement for '{templateName}' at {coordinates} {orientation}";
                return false;
            }

            _blockObjectPlacerService.GetMatchingPlacer(blockObjectSpec)
                .Place(blockObjectSpec, placement, placed => OnPlaced(placed, builderPriority));
            error = null;
            return true;
        }

        public bool TryPlacePath(Vector3Int coordinates, out string error)
        {
            return TryPlace("Path", coordinates, Orientation.Cw0, Priority.Normal, out error);
        }

        public bool CanPlace(string templateName, Vector3Int coordinates, Orientation orientation)
        {
            BuildingSpec buildingSpec = ResolveTemplate(templateName);
            if (buildingSpec == null)
            {
                return false;
            }
            BlockObjectSpec blockObjectSpec = buildingSpec.GetSpec<BlockObjectSpec>();
            var placement = new Placement(
                new Vector3Int(coordinates.x, coordinates.y, coordinates.z - blockObjectSpec.BaseZ),
                orientation, FlipMode.Unflipped);
            return _blockValidator.BlocksValid(blockObjectSpec, placement);
        }

        public bool TryUnlock(string templateName, out string error)
        {
            BuildingSpec buildingSpec = ResolveTemplate(templateName);
            if (buildingSpec == null)
            {
                error = $"Unknown building template '{templateName}'";
                return false;
            }
            return EnsureUnlocked(buildingSpec, out error);
        }

        public List<string> ListTemplateNames()
        {
            var names = new List<string>();
            foreach (BuildingSpec buildingSpec in _buildingService.Buildings)
            {
                names.Add(_buildingService.GetTemplateName(buildingSpec));
            }
            names.Sort();
            return names;
        }

        /// <summary>Resolves "WaterPump" -> "WaterPump.Folktails" when needed;
        /// paths and faction-neutral templates resolve as-is.</summary>
        private BuildingSpec ResolveTemplate(string templateName)
        {
            BuildingSpec spec = TryGetTemplate(templateName);
            if (spec != null)
            {
                return spec;
            }
            return TryGetTemplate(templateName + "." + _factionService.Current.Id);
        }

        private BuildingSpec TryGetTemplate(string name)
        {
            try
            {
                return _buildingService.GetBuildingTemplate(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool EnsureUnlocked(BuildingSpec buildingSpec, out string error)
        {
            if (_buildingUnlockingService.Unlocked(buildingSpec))
            {
                error = null;
                return true;
            }
            if (buildingSpec.ScienceCost > _scienceService.SciencePoints)
            {
                error = $"Locked and unaffordable: needs {buildingSpec.ScienceCost} science, " +
                        $"have {_scienceService.SciencePoints}";
                return false;
            }
            _buildingUnlockingService.Unlock(buildingSpec);
            Debug.Log($"[Autopilot] Unlocked building for {buildingSpec.ScienceCost} science.");
            error = null;
            return true;
        }

        private static void OnPlaced(BaseComponent placed, Priority builderPriority)
        {
            if (builderPriority != Priority.Normal)
            {
                var prioritizable = placed.GetComponent<BuilderPrioritizable>();
                if (prioritizable != null)
                {
                    prioritizable.SetPriority(builderPriority);
                }
            }
        }
    }
}
