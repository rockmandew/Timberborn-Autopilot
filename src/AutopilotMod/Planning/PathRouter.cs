using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.PrioritySystem;
using Timberborn.TerrainSystem;
using TimberbornAutopilot.Acting;
using UnityEngine;

namespace TimberbornAutopilot.Planning
{
    /// <summary>
    /// BFS path routing over walkable terrain, including one-level climbs via
    /// Stairs placed on the lower tile facing uphill. Existing paths are free
    /// to traverse; new tiles must pass placement validation. Climb tiles are
    /// entered straight-through so the stair's bottom edge lines up.
    /// </summary>
    public class PathRouter
    {
        private const int MaxSearchNodes = 8000;

        // At Cw0 stairs ascend toward Up; rotate for other headings. If live
        // testing shows them backwards, flip this to Direction2D.Down.
        private const Direction2D UphillAtCw0 = Direction2D.Up;

        private readonly IBlockService _blockService;
        private readonly ITerrainService _terrainService;
        private readonly BuildPlacer _buildPlacer;

        public PathRouter(IBlockService blockService,
                          ITerrainService terrainService,
                          BuildPlacer buildPlacer)
        {
            _blockService = blockService;
            _terrainService = terrainService;
            _buildPlacer = buildPlacer;
        }

        /// <summary>Routes from the existing path network (seeded from the district
        /// doorstep and nearby path tiles) to the target doorstep, placing paths
        /// and stairs. Returns false when no route exists.</summary>
        public bool Connect(Vector3Int networkRoot, Vector3Int target, out string report)
        {
            List<RouteStep> route = FindRoute(NetworkSeeds(networkRoot), SurfaceTile(target));
            if (route == null)
            {
                report = "no route";
                return false;
            }
            int placed = 0, existing = 0, failed = 0;
            string firstError = null;
            foreach (RouteStep step in route)
            {
                if (step.IsStairs)
                {
                    if (HasStairsAt(step.Tile))
                    {
                        existing++;
                    }
                    else if (_buildPlacer.TryPlace("Stairs", step.Tile, step.StairsOrientation,
                                                   Priority.Normal, out string stairsError))
                    {
                        placed++;
                    }
                    else
                    {
                        failed++;
                        firstError = firstError ?? stairsError;
                    }
                }
                else if (_blockService.GetPathObjectAt(step.Tile) != null)
                {
                    existing++;
                }
                else if (_buildPlacer.TryPlacePath(step.Tile, out string pathError))
                {
                    placed++;
                }
                else
                {
                    failed++;
                    firstError = firstError ?? pathError;
                }
            }
            report = $"route {route.Count} tiles: {placed} placed, {existing} existing, {failed} failed" +
                     (firstError != null ? $" (first failure: {firstError})" : "");
            return true;
        }

        /// <summary>True when a route (flat or staired) to the target exists —
        /// used as a placement precondition so no building is born unreachable.</summary>
        public bool CanReach(Vector3Int networkRoot, Vector3Int target)
        {
            return FindRoute(NetworkSeeds(networkRoot), SurfaceTile(target)) != null;
        }

        private List<Vector3Int> NetworkSeeds(Vector3Int networkRoot)
        {
            var seeds = new List<Vector3Int> { SurfaceTile(networkRoot) };
            for (int dx = -14; dx <= 14; dx++)
            {
                for (int dy = -14; dy <= 14; dy++)
                {
                    Vector3Int tile = SurfaceTile(new Vector3Int(networkRoot.x + dx, networkRoot.y + dy, 0));
                    if (_blockService.GetPathObjectAt(tile) != null)
                    {
                        seeds.Add(tile);
                    }
                }
            }
            return seeds;
        }

        private List<RouteStep> FindRoute(List<Vector3Int> seeds, Vector3Int goal)
        {
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var entryDirection = new Dictionary<Vector3Int, Vector3Int>();
            var visited = new HashSet<Vector3Int>();
            var seedSet = new HashSet<Vector3Int>(seeds);
            var queue = new Queue<Vector3Int>();
            foreach (Vector3Int seed in seeds)
            {
                if (visited.Add(seed))
                {
                    queue.Enqueue(seed);
                }
            }
            int examined = 0;

            while (queue.Count > 0 && examined++ < MaxSearchNodes)
            {
                Vector3Int current = queue.Dequeue();
                bool currentIsClimb = IsClimbTile(current, cameFrom, entryDirection);
                foreach (Vector3Int direction in Directions)
                {
                    Vector3Int next = SurfaceTile(new Vector3Int(current.x + direction.x,
                                                                 current.y + direction.y, 0));
                    if (visited.Contains(next))
                    {
                        continue;
                    }
                    int dz = next.z - current.z;
                    // A stairs tile only connects straight through.
                    if (currentIsClimb && entryDirection.TryGetValue(current, out Vector3Int entry) &&
                        entry != direction)
                    {
                        continue;
                    }
                    // Validate the edge FIRST — a route may never end on an
                    // illegal hop (e.g. straight off a cliff onto the doorstep).
                    if (dz == 0)
                    {
                        if (!IsWalkable(next))
                        {
                            continue;
                        }
                    }
                    else if (dz == 1)
                    {
                        // Ascend: current becomes the stairs tile; entry must be straight.
                        if (entryDirection.TryGetValue(current, out Vector3Int came) && came != direction)
                        {
                            continue;
                        }
                        if (!StairsUsable(current, direction) || !IsWalkable(next))
                        {
                            continue;
                        }
                    }
                    else if (dz == -1)
                    {
                        // Descend: next becomes the stairs tile facing back at us.
                        if (!StairsUsable(next, new Vector3Int(-direction.x, -direction.y, 0)))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                    if (next == goal || IsAdjacentFlat(next, goal))
                    {
                        cameFrom[next] = current;
                        entryDirection[next] = direction;
                        return Reconstruct(cameFrom, entryDirection, seedSet, next);
                    }
                    visited.Add(next);
                    cameFrom[next] = current;
                    entryDirection[next] = direction;
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        private bool IsClimbTile(Vector3Int tile, Dictionary<Vector3Int, Vector3Int> cameFrom,
                                 Dictionary<Vector3Int, Vector3Int> entryDirection)
        {
            return cameFrom.TryGetValue(tile, out Vector3Int previous) && previous.z != tile.z;
        }

        private bool StairsUsable(Vector3Int lowerTile, Vector3Int uphillDirection)
        {
            if (HasStairsAt(lowerTile))
            {
                return true;
            }
            // Only count on stairs we can actually afford right now — planning
            // around unaffordable stairs recreates the science deadlock.
            return _buildPlacer.IsAvailable("Stairs") &&
                   _buildPlacer.CanPlace("Stairs", lowerTile, OrientationFor(uphillDirection));
        }

        private bool HasStairsAt(Vector3Int tile)
        {
            foreach (BlockObject blockObject in _blockService.GetObjectsAt(tile))
            {
                if (blockObject.GameObject.name.StartsWith("Stairs"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsWalkable(Vector3Int tile)
        {
            if (_blockService.GetPathObjectAt(tile) != null)
            {
                return true;
            }
            return _buildPlacer.CanPlace("Path", tile, Orientation.Cw0);
        }

        private Vector3Int SurfaceTile(Vector3Int column)
        {
            int height = _terrainService.GetTerrainHeightBelow(
                new Vector3Int(column.x, column.y, _terrainService.Size.z - 1));
            return new Vector3Int(column.x, column.y, height);
        }

        private static bool IsAdjacentFlat(Vector3Int a, Vector3Int b)
        {
            return a.z == b.z && Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) <= 1;
        }

        private static Orientation OrientationFor(Vector3Int uphillDirection)
        {
            Direction2D wanted = ToDirection2D(uphillDirection);
            foreach (Orientation orientation in OrientationValues)
            {
                if (orientation.Transform(UphillAtCw0) == wanted)
                {
                    return orientation;
                }
            }
            return Orientation.Cw0;
        }

        private static Direction2D ToDirection2D(Vector3Int direction)
        {
            if (direction.x > 0) return Direction2D.Right;
            if (direction.x < 0) return Direction2D.Left;
            if (direction.y > 0) return Direction2D.Up;
            return Direction2D.Down;
        }

        private List<RouteStep> Reconstruct(Dictionary<Vector3Int, Vector3Int> cameFrom,
                                            Dictionary<Vector3Int, Vector3Int> entryDirection,
                                            HashSet<Vector3Int> seeds, Vector3Int end)
        {
            var tiles = new List<Vector3Int> { end };
            Vector3Int current = end;
            while (cameFrom.TryGetValue(current, out Vector3Int previous) && !seeds.Contains(previous))
            {
                tiles.Add(previous);
                current = previous;
            }
            tiles.Reverse();

            var steps = new List<RouteStep>();
            for (int i = 0; i < tiles.Count; i++)
            {
                Vector3Int tile = tiles[i];
                bool stairs = false;
                Orientation orientation = Orientation.Cw0;
                Vector3Int? higher = null;
                if (i > 0 && tiles[i - 1].z == tile.z + 1)
                {
                    higher = tiles[i - 1];
                }
                if (i < tiles.Count - 1 && tiles[i + 1].z == tile.z + 1)
                {
                    higher = tiles[i + 1];
                }
                if (higher.HasValue)
                {
                    stairs = true;
                    orientation = OrientationFor(new Vector3Int(higher.Value.x - tile.x,
                                                                higher.Value.y - tile.y, 0));
                }
                steps.Add(new RouteStep(tile, stairs, orientation));
            }
            return steps;
        }

        private static readonly Vector3Int[] Directions =
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        };

        private static readonly Orientation[] OrientationValues =
        {
            Orientation.Cw0, Orientation.Cw90, Orientation.Cw180, Orientation.Cw270,
        };

        private readonly struct RouteStep
        {
            public Vector3Int Tile { get; }
            public bool IsStairs { get; }
            public Orientation StairsOrientation { get; }

            public RouteStep(Vector3Int tile, bool isStairs, Orientation stairsOrientation)
            {
                Tile = tile;
                IsStairs = isStairs;
                StairsOrientation = stairsOrientation;
            }
        }
    }
}
