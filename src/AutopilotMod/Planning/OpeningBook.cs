using System;
using System.Collections.Generic;
using Timberborn.Coordinates;
using Timberborn.PrioritySystem;
using Timberborn.TerrainSystem;
using Timberborn.WaterSystem;
using TimberbornAutopilot.Acting;
using TimberbornAutopilot.Sensing;
using UnityEngine;

namespace TimberbornAutopilot.Planning
{
    /// <summary>
    /// Days 1-20 build order as an ordered list of goals. Every planning pass
    /// re-scans the live world, so player-built structures satisfy goals
    /// immediately. The brain only ever ADDS — it never removes or reconfigures
    /// what the player did; disagreements become suggestions. Player is final say.
    /// </summary>
    public class OpeningBook
    {
        private readonly BuildPlacer _buildPlacer;
        private readonly ZonePlanner _zonePlanner;
        private readonly WorldQuery _worldQuery;
        private readonly WorldModel _worldModel;
        private readonly BrainLog _brainLog;
        private readonly ITerrainService _terrainService;
        private readonly PathRouter _pathRouter;
        private readonly IThreadSafeWaterMap _waterMap;

        private readonly HashSet<string> _announcedGoals = new HashSet<string>();
        private readonly HashSet<string> _givenSuggestions = new HashSet<string>();
        private readonly HashSet<Vector3Int> _connectivityChecked = new HashSet<Vector3Int>();
        private int _lastRepairDay = -1;

        public bool Enabled { get; set; } = true;

        public OpeningBook(BuildPlacer buildPlacer,
                           ZonePlanner zonePlanner,
                           WorldQuery worldQuery,
                           WorldModel worldModel,
                           BrainLog brainLog,
                           ITerrainService terrainService,
                           PathRouter pathRouter,
                           IThreadSafeWaterMap waterMap)
        {
            _buildPlacer = buildPlacer;
            _zonePlanner = zonePlanner;
            _worldQuery = worldQuery;
            _worldModel = worldModel;
            _brainLog = brainLog;
            _terrainService = terrainService;
            _pathRouter = pathRouter;
            _waterMap = waterMap;
        }

        /// <summary>One planning pass: find the first unsatisfied goal and take
        /// ONE action toward it. Gentle pacing, easy to watch, easy to override.</summary>
        public void PlanningPass()
        {
            if (!Enabled)
            {
                return;
            }
            Vector3Int? districtCenter = _worldQuery.DistrictCenterCoordinates();
            Vector3Int? doorstep = _worldQuery.DistrictCenterDoorstep();
            if (!districtCenter.HasValue || !doorstep.HasValue)
            {
                return;
            }
            Vector3Int anchor = districtCenter.Value;
            Vector3Int networkRoot = doorstep.Value;
            WorldSnapshot world = _worldModel.Snapshot();

            foreach (Goal goal in BuildGoals(world, anchor))
            {
                int have = CountAny(goal.TemplateCandidates);
                if (have >= goal.TargetCount)
                {
                    continue;
                }
                if (_announcedGoals.Add(goal.Key))
                {
                    _brainLog.Announce(goal.Intent);
                }
                if (ExecuteGoal(goal, anchor, networkRoot, world))
                {
                    return;
                }
                // Goal can't act right now (science, no spot) — let later goals proceed.
            }

            // Re-sweep every day: routes that failed earlier (e.g. stairs were
            // unaffordable) succeed once science or terrain changes.
            int day = world.Cycle * 100 + world.CycleDay;
            if (day != _lastRepairDay)
            {
                _lastRepairDay = day;
                _connectivityChecked.Clear();
            }
            RepairConnectivity(networkRoot);
            CheckSuggestions(world);
        }

        /// <summary>A tile qualifies for water buildings when any neighboring column
        /// holds actual water — pumps placed inland pump nothing.</summary>
        private bool TouchesWater(Vector3Int coords)
        {
            var offsets = new[]
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
                new Vector3Int(2, 0, 0), new Vector3Int(-2, 0, 0),
                new Vector3Int(0, 2, 0), new Vector3Int(0, -2, 0),
            };
            foreach (Vector3Int offset in offsets)
            {
                Vector3Int column = coords + offset;
                for (int z = 0; z <= coords.z; z++)
                {
                    if (_waterMap.WaterDepth(new Vector3Int(column.x, column.y, z)) > 0.1f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool ExecuteGoal(Goal goal, Vector3Int anchor, Vector3Int networkRoot, WorldSnapshot world)
        {
            Vector3Int target = goal.Anchor ?? anchor;
            string lastError = null;
            foreach (string candidate in goal.TemplateCandidates)
            {
                if (TryPlaceNear(candidate, target, goal.SearchRadius, networkRoot, goal.SiteFilter,
                                 out Vector3Int placedAt, out Vector3Int? entrance, ref lastError))
                {
                    _brainLog.Note($"Placed {candidate} at ({placedAt.x},{placedAt.y},{placedAt.z}).");
                    if (_pathRouter.Connect(networkRoot, entrance ?? placedAt, out int tiles))
                    {
                        if (tiles > 0)
                        {
                            _brainLog.Note($"Routed {tiles} path tiles to the new {candidate}.");
                        }
                    }
                    else
                    {
                        _brainLog.Suggest($"No flat route to the new {candidate} at " +
                                          $"({placedAt.x},{placedAt.y}) — it may need stairs or a bridge.");
                    }
                    goal.OnPlaced?.Invoke(placedAt);
                    return true;
                }
            }
            if (lastError != null && lastError.Contains("science"))
            {
                if (_givenSuggestions.Add($"science-{goal.Key}-c{world.Cycle}d{world.CycleDay}"))
                {
                    _brainLog.Note($"{goal.TemplateCandidates[0]} is waiting on science " +
                                   $"({lastError}). Moving down the list meanwhile.");
                }
            }
            else if (_givenSuggestions.Add("cannot-place-" + goal.Key))
            {
                _brainLog.Suggest($"I couldn't find a valid spot for {goal.TemplateCandidates[0]} " +
                                  $"near ({target.x},{target.y}) — feel free to place one manually.");
            }
            return false;
        }

        /// <summary>Spiral search for a valid placement whose doorstep is reachable
        /// from the path network, trying all orientations.</summary>
        private bool TryPlaceNear(string templateName, Vector3Int anchor, int radius, Vector3Int networkRoot,
                                  Func<Vector3Int, bool> siteFilter,
                                  out Vector3Int placedAt, out Vector3Int? entrance, ref string lastError)
        {
            foreach (Vector3Int tile in Spiral(anchor, radius))
            {
                int height = _terrainService.GetTerrainHeightBelow(
                    new Vector3Int(tile.x, tile.y, _terrainService.Size.z - 1));
                var coords = new Vector3Int(tile.x, tile.y, height);
                if (siteFilter != null && !siteFilter(coords))
                {
                    continue;
                }
                foreach (Orientation orientation in Orientations)
                {
                    if (!_buildPlacer.CanPlace(templateName, coords, orientation))
                    {
                        continue;
                    }
                    Vector3Int? doorstep = _buildPlacer.PredictDoorstep(templateName, coords, orientation);
                    if (doorstep.HasValue && !_pathRouter.CanReach(networkRoot, doorstep.Value))
                    {
                        continue;
                    }
                    if (_buildPlacer.TryPlace(templateName, coords, orientation, Priority.Normal,
                                              out string error, out entrance))
                    {
                        placedAt = coords;
                        return true;
                    }
                    lastError = error;
                    if (error != null && (error.Contains("science") || error.Contains("Unknown")))
                    {
                        placedAt = default;
                        entrance = null;
                        return false;
                    }
                }
            }
            placedAt = default;
            entrance = null;
            return false;
        }

        /// <summary>One building per pass: route the path network to its doorstep.
        /// Fixes player-placed and pre-fix buildings. Routing is idempotent, so
        /// re-checking a connected doorstep costs nothing and places nothing.</summary>
        private void RepairConnectivity(Vector3Int networkRoot)
        {
            foreach (Vector3Int doorstep in _worldQuery.BuildingDoorsteps())
            {
                if (!_connectivityChecked.Add(doorstep))
                {
                    continue;
                }
                if (_pathRouter.Connect(networkRoot, doorstep, out int tiles))
                {
                    if (tiles > 0)
                    {
                        _brainLog.Note($"Connected doorstep at ({doorstep.x},{doorstep.y}) " +
                                       $"with {tiles} path tiles.");
                    }
                }
                else if (_givenSuggestions.Add($"no-route-{doorstep.x}-{doorstep.y}"))
                {
                    _brainLog.Suggest($"Building at ({doorstep.x},{doorstep.y}) has no flat route " +
                                      "to the district — it may need stairs, or consider relocating it.");
                }
                return;
            }
        }

        private List<Goal> BuildGoals(WorldSnapshot world, Vector3Int anchor)
        {
            Vector3Int? trees = _worldQuery.NearestResourceCluster(anchor, gatherable: false);
            Vector3Int? berries = _worldQuery.NearestResourceCluster(anchor, gatherable: true);

            var goals = new List<Goal>
            {
                new Goal("water-pump", new[] { "WaterPump" }, 1, 30,
                    "Placing a Water Pump by the river — drinking water is survival priority #1.")
                    { Anchor = null, SiteFilter = TouchesWater },
                new Goal("lumberjacks", new[] { "LumberjackFlag" }, 2, 12,
                    "Adding Lumberjack Flags near the forest — logs fund everything early.")
                    { Anchor = trees },
                new Goal("gatherer", new[] { "GathererFlag" }, 1, 12,
                    "Placing a Gatherer Flag on the berry patch — free food while farms grow.")
                    { Anchor = berries },
                new Goal("log-pile", new[] { "SmallPile" }, 1, 12,
                    "Adding a Small Pile — logs need a home near the lumberjacks.")
                    { Anchor = trees },
                new Goal("inventor", new[] { "Inventor" }, 1, 15,
                    "Building an Inventor — science income unlocks the whole tech ladder."),
                new Goal("farm", new[] { "EfficientFarmHouse" }, 1, 18,
                    "Building a Farmhouse — carrots are the fastest calories per tile.")
                    { OnPlaced = ZoneCarrotsAround },
                new Goal("warehouse", new[] { "SmallWarehouse" }, 1, 12,
                    "Adding a Small Warehouse — food storage before the first drought."),
                new Goal("water-tank", new[] { "SmallTank" }, 2, 15,
                    $"Building water storage — the {world.NextHazard} needs " +
                    $"{world.WaterTargetForHazard:F0} water banked."),
                new Goal("forester", new[] { "Forester" }, 1, 15,
                    "Adding a Forester — replanted trees prevent the mid-game wood crisis.")
                    { OnPlaced = ZoneTreesAround },
                new Goal("housing", new[] { "Lodge" }, world.Homeless > 0 ? 2 : 1, 15,
                    "Adding a Lodge — rested beavers work faster, and shared housing means kits."),
                new Goal("campfire", new[] { "Campfire" }, 1, 12,
                    "Placing a Campfire — first rung of the wellbeing ladder toward Iron Teeth."),
            };
            return goals;
        }

        private void ZoneCarrotsAround(Vector3Int farm)
        {
            int zoned = _zonePlanner.ZonePlanting(
                farm + new Vector3Int(-5, -5, 0), farm + new Vector3Int(5, 5, 0), "Carrot");
            _brainLog.Note($"Zoned {zoned} tiles of carrots around the farm.");
        }

        private void ZoneTreesAround(Vector3Int forester)
        {
            int zoned = _zonePlanner.ZonePlanting(
                forester + new Vector3Int(-6, -6, 0), forester + new Vector3Int(6, 6, 0), "Pine");
            _brainLog.Note($"Zoned {zoned} tiles of pines around the forester.");
        }

        private void CheckSuggestions(WorldSnapshot world)
        {
            if (world.WaterDaysLeft > 0 && world.DaysUntilHazard > 0 &&
                world.WaterStock < world.WaterTargetForHazard &&
                world.WaterDaysLeft < world.DaysUntilHazard &&
                _givenSuggestions.Add("water-warning-cycle-" + world.Cycle))
            {
                _brainLog.Suggest($"{world.NextHazard} in {world.DaysUntilHazard} days but only " +
                                  $"{world.WaterStock} water stored (target {world.WaterTargetForHazard:F0}) — " +
                                  "consider a second pump or more tanks.");
            }
            int pumps = _worldQuery.CountBuildings("WaterPump");
            if (pumps > 3 && _givenSuggestions.Add("many-pumps"))
            {
                _brainLog.Suggest($"{pumps} water pumps for {world.Adults + world.Children} beavers " +
                                  "is more than needed — workers there might serve better elsewhere. Your call.");
            }
        }

        private int CountAny(IReadOnlyList<string> candidates)
        {
            int total = 0;
            foreach (string candidate in candidates)
            {
                total += _worldQuery.CountBuildings(candidate);
            }
            return total;
        }

        private static readonly Orientation[] Orientations =
        {
            Orientation.Cw0, Orientation.Cw90, Orientation.Cw180, Orientation.Cw270,
        };

        private static IEnumerable<Vector3Int> Spiral(Vector3Int center, int radius)
        {
            yield return center;
            for (int ring = 1; ring <= radius; ring++)
            {
                for (int i = -ring; i <= ring; i++)
                {
                    yield return new Vector3Int(center.x + i, center.y - ring, 0);
                    yield return new Vector3Int(center.x + i, center.y + ring, 0);
                }
                for (int i = -ring + 1; i <= ring - 1; i++)
                {
                    yield return new Vector3Int(center.x - ring, center.y + i, 0);
                    yield return new Vector3Int(center.x + ring, center.y + i, 0);
                }
            }
        }

        private class Goal
        {
            public string Key { get; }
            public IReadOnlyList<string> TemplateCandidates { get; }
            public int TargetCount { get; }
            public int SearchRadius { get; }
            public string Intent { get; }
            public Vector3Int? Anchor { get; set; }
            public Action<Vector3Int> OnPlaced { get; set; }
            public Func<Vector3Int, bool> SiteFilter { get; set; }

            public Goal(string key, string[] templateCandidates, int targetCount,
                        int searchRadius, string intent)
            {
                Key = key;
                TemplateCandidates = templateCandidates;
                TargetCount = targetCount;
                SearchRadius = searchRadius;
                Intent = intent;
            }
        }
    }
}
