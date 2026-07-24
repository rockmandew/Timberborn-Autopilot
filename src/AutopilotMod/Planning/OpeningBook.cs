using System;
using System.Collections.Generic;
using Timberborn.Coordinates;
using Timberborn.PrioritySystem;
using Timberborn.TerrainSystem;
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

        private readonly HashSet<string> _announcedGoals = new HashSet<string>();
        private readonly HashSet<string> _givenSuggestions = new HashSet<string>();

        public bool Enabled { get; set; } = true;

        public OpeningBook(BuildPlacer buildPlacer,
                           ZonePlanner zonePlanner,
                           WorldQuery worldQuery,
                           WorldModel worldModel,
                           BrainLog brainLog,
                           ITerrainService terrainService)
        {
            _buildPlacer = buildPlacer;
            _zonePlanner = zonePlanner;
            _worldQuery = worldQuery;
            _worldModel = worldModel;
            _brainLog = brainLog;
            _terrainService = terrainService;
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
            if (!districtCenter.HasValue)
            {
                return;
            }
            Vector3Int anchor = districtCenter.Value;
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
                ExecuteGoal(goal, anchor);
                return;
            }

            CheckSuggestions(world);
        }

        private void ExecuteGoal(Goal goal, Vector3Int anchor)
        {
            Vector3Int target = goal.Anchor ?? anchor;
            foreach (string candidate in goal.TemplateCandidates)
            {
                if (TryPlaceNear(candidate, target, goal.SearchRadius, out Vector3Int placedAt))
                {
                    _brainLog.Note($"Placed {candidate} at ({placedAt.x},{placedAt.y},{placedAt.z}).");
                    BuildPathToward(anchor, placedAt);
                    goal.OnPlaced?.Invoke(placedAt);
                    return;
                }
            }
            if (_givenSuggestions.Add("cannot-place-" + goal.Key))
            {
                _brainLog.Suggest($"I couldn't find a valid spot for {goal.TemplateCandidates[0]} " +
                                  $"near ({target.x},{target.y}) — feel free to place one manually.");
            }
        }

        /// <summary>Spiral search for a valid placement, trying all orientations.</summary>
        private bool TryPlaceNear(string templateName, Vector3Int anchor, int radius, out Vector3Int placedAt)
        {
            foreach (Vector3Int tile in Spiral(anchor, radius))
            {
                int height = _terrainService.GetTerrainHeightBelow(
                    new Vector3Int(tile.x, tile.y, _terrainService.Size.z - 1));
                var coords = new Vector3Int(tile.x, tile.y, height);
                foreach (Orientation orientation in Orientations)
                {
                    if (_buildPlacer.CanPlace(templateName, coords, orientation) &&
                        _buildPlacer.TryPlace(templateName, coords, orientation, Priority.Normal, out _))
                    {
                        placedAt = coords;
                        return true;
                    }
                }
            }
            placedAt = default;
            return false;
        }

        /// <summary>L-shaped path on flat terrain from the district toward a building.
        /// Tiles that fail (occupied, slope) are skipped — a path already there is fine.</summary>
        private void BuildPathToward(Vector3Int from, Vector3Int to)
        {
            foreach (Vector3Int tile in LRoute(from, to))
            {
                int height = _terrainService.GetTerrainHeightBelow(
                    new Vector3Int(tile.x, tile.y, _terrainService.Size.z - 1));
                _buildPlacer.TryPlacePath(new Vector3Int(tile.x, tile.y, height), out _);
            }
        }

        private List<Goal> BuildGoals(WorldSnapshot world, Vector3Int anchor)
        {
            Vector3Int? trees = _worldQuery.NearestResourceCluster(anchor, gatherable: false);
            Vector3Int? berries = _worldQuery.NearestResourceCluster(anchor, gatherable: true);

            var goals = new List<Goal>
            {
                new Goal("water-pump", new[] { "WaterPump" }, 1, 25,
                    "Placing a Water Pump by the river — drinking water is survival priority #1.")
                    { Anchor = null },
                new Goal("lumberjacks", new[] { "LumberjackFlag" }, 2, 12,
                    "Adding Lumberjack Flags near the forest — logs fund everything early.")
                    { Anchor = trees },
                new Goal("gatherer", new[] { "GathererFlag" }, 1, 12,
                    "Placing a Gatherer Flag on the berry patch — free food while farms grow.")
                    { Anchor = berries },
                new Goal("farm", new[] { "FarmHouse", "Farmhouse" }, 1, 18,
                    "Building a Farmhouse — carrots are the fastest calories per tile.")
                    { OnPlaced = ZoneCarrotsAround },
                new Goal("forester", new[] { "Forester", "ForesterFlag" }, 1, 15,
                    "Adding a Forester — replanted trees prevent the mid-game wood crisis.")
                    { OnPlaced = ZoneTreesAround },
                new Goal("water-tank", new[] { "SmallWaterTank", "WaterTank" }, 1, 15,
                    $"Building water storage — the {world.NextHazard} needs " +
                    $"{world.WaterTargetForHazard:F0} water banked."),
                new Goal("inventor", new[] { "Inventor" }, 1, 15,
                    "Building an Inventor — science unlocks everything on the wellbeing ladder."),
                new Goal("housing", new[] { "Lodge" }, world.Homeless > 0 ? 2 : 1, 15,
                    "Adding a Lodge — rested beavers work faster, and shared housing means kits."),
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

        private static IEnumerable<Vector3Int> LRoute(Vector3Int from, Vector3Int to)
        {
            int stepX = Math.Sign(to.x - from.x);
            for (int x = from.x; x != to.x; x += stepX == 0 ? 1 : stepX)
            {
                if (stepX == 0) break;
                yield return new Vector3Int(x, from.y, 0);
            }
            int stepY = Math.Sign(to.y - from.y);
            for (int y = from.y; y != to.y; y += stepY == 0 ? 1 : stepY)
            {
                if (stepY == 0) break;
                yield return new Vector3Int(to.x, y, 0);
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
