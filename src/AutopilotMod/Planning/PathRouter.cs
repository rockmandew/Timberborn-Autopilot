using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.TerrainSystem;
using TimberbornAutopilot.Acting;
using UnityEngine;

namespace TimberbornAutopilot.Planning
{
    /// <summary>
    /// BFS path routing on flat walkable terrain. Existing paths are free to
    /// traverse; new tiles must pass path-placement validation. Height changes
    /// are avoided entirely (stairs come with the water engineer in v0.6).
    /// </summary>
    public class PathRouter
    {
        private const int MaxSearchNodes = 6000;

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

        /// <summary>Routes a path from the EXISTING path network (seeded from the
        /// district doorstep and every path tile near it) to the target doorstep.
        /// Returns false when no flat walkable route exists.</summary>
        public bool Connect(Vector3Int networkRoot, Vector3Int target, out int tilesPlaced)
        {
            tilesPlaced = 0;
            List<Vector3Int> seeds = NetworkSeeds(networkRoot);
            List<Vector3Int> route = FindRoute(seeds, SurfaceTile(target));
            if (route == null)
            {
                return false;
            }
            foreach (Vector3Int tile in route)
            {
                if (_blockService.GetPathObjectAt(tile) == null &&
                    _buildPlacer.TryPlacePath(tile, out _))
                {
                    tilesPlaced++;
                }
            }
            return true;
        }

        /// <summary>The district doorstep plus every existing path tile around it —
        /// guaranteed entry points into the real walkable network.</summary>
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

        private List<Vector3Int> FindRoute(List<Vector3Int> seeds, Vector3Int goal)
        {
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var queue = new Queue<Vector3Int>();
            var visited = new HashSet<Vector3Int>();
            var seedSet = new HashSet<Vector3Int>(seeds);
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
                foreach (Vector3Int next in Neighbors(current))
                {
                    if (visited.Contains(next))
                    {
                        continue;
                    }
                    if (IsAdjacent(next, goal) || next == goal)
                    {
                        cameFrom[next] = current;
                        return Reconstruct(cameFrom, seedSet, next);
                    }
                    if (!IsWalkable(next) || next.z != current.z)
                    {
                        continue;
                    }
                    visited.Add(next);
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        private IEnumerable<Vector3Int> Neighbors(Vector3Int tile)
        {
            yield return SurfaceTile(new Vector3Int(tile.x + 1, tile.y, 0));
            yield return SurfaceTile(new Vector3Int(tile.x - 1, tile.y, 0));
            yield return SurfaceTile(new Vector3Int(tile.x, tile.y + 1, 0));
            yield return SurfaceTile(new Vector3Int(tile.x, tile.y - 1, 0));
        }

        private bool IsWalkable(Vector3Int tile)
        {
            if (_blockService.GetPathObjectAt(tile) != null)
            {
                return true;
            }
            return _buildPlacer.CanPlace("Path", tile, Timberborn.Coordinates.Orientation.Cw0);
        }

        private Vector3Int SurfaceTile(Vector3Int column)
        {
            int height = _terrainService.GetTerrainHeightBelow(
                new Vector3Int(column.x, column.y, _terrainService.Size.z - 1));
            return new Vector3Int(column.x, column.y, height);
        }

        private static bool IsAdjacent(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) <= 1;
        }

        private static List<Vector3Int> Reconstruct(Dictionary<Vector3Int, Vector3Int> cameFrom,
                                                    HashSet<Vector3Int> seeds, Vector3Int end)
        {
            var route = new List<Vector3Int> { end };
            Vector3Int current = end;
            while (cameFrom.TryGetValue(current, out Vector3Int previous) && !seeds.Contains(previous))
            {
                route.Add(previous);
                current = previous;
            }
            route.Reverse();
            return route;
        }
    }
}
