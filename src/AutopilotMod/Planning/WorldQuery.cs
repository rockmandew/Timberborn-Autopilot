using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.BuilderPrioritySystem;
using Timberborn.Buildings;
using Timberborn.ConstructionSites;
using Timberborn.Cutting;
using Timberborn.EntitySystem;
using Timberborn.GameDistricts;
using Timberborn.Gathering;
using Timberborn.NaturalResourcesModelSystem;
using Timberborn.PrioritySystem;
using UnityEngine;

namespace TimberbornAutopilot.Planning
{
    /// <summary>
    /// Spatial queries over the live world. Always reads fresh state, so
    /// anything the player built by hand counts immediately.
    /// </summary>
    public class WorldQuery
    {
        private readonly EntityComponentRegistry _entityComponentRegistry;
        private readonly DistrictCenterRegistry _districtCenterRegistry;

        public WorldQuery(EntityComponentRegistry entityComponentRegistry,
                          DistrictCenterRegistry districtCenterRegistry)
        {
            _entityComponentRegistry = entityComponentRegistry;
            _districtCenterRegistry = districtCenterRegistry;
        }

        /// <summary>Counts buildings (finished or under construction) whose template
        /// matches the base name, e.g. "WaterPump" matches "WaterPump.Folktails(Clone)".</summary>
        public int CountBuildings(string baseName)
        {
            int count = 0;
            foreach (Building building in _entityComponentRegistry.GetEnabled<Building>())
            {
                if (NameMatches(building.GameObject.name, baseName))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Doorstep (or origin) of every building except paths and the
        /// district center — the tiles the path network must reach.</summary>
        public List<Vector3Int> BuildingDoorsteps()
        {
            Vector3Int? districtCenter = DistrictCenterCoordinates();
            var doorsteps = new List<Vector3Int>();
            foreach (Building building in _entityComponentRegistry.GetEnabled<Building>())
            {
                if (building.GameObject.name.StartsWith("Path"))
                {
                    continue;
                }
                var blockObject = building.GetComponent<BlockObject>();
                if (blockObject == null || blockObject.Coordinates == districtCenter)
                {
                    continue;
                }
                doorsteps.Add(blockObject.HasEntrance
                    ? blockObject.PositionedEntrance.DoorstepCoordinates
                    : blockObject.Coordinates);
            }
            return doorsteps;
        }

        /// <summary>How many construction sites are still unfinished — the brain
        /// stops placing new goals past a cap so materials/builders concentrate.</summary>
        public int CountUnfinishedSites()
        {
            int count = 0;
            foreach (Building building in _entityComponentRegistry.GetEnabled<Building>())
            {
                // Paths are trivial free sites — only real buildings saturate
                // the material/builder pipeline.
                if (!building.GameObject.name.StartsWith("Path") &&
                    building.HasComponent<ConstructionSite>())
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Doorsteps of buildings the GAME says are not connected to any
        /// district (DistrictBuilding.District == null) — ground truth for the
        /// unconnected monitor, excluding paths and district centers.</summary>
        public List<Vector3Int> UnconnectedDoorsteps()
        {
            Vector3Int? districtCenter = DistrictCenterCoordinates();
            var doorsteps = new List<Vector3Int>();
            foreach (Building building in _entityComponentRegistry.GetEnabled<Building>())
            {
                if (building.GameObject.name.StartsWith("Path"))
                {
                    continue;
                }
                var districtBuilding = building.GetComponent<DistrictBuilding>();
                if (districtBuilding == null ||
                    districtBuilding.District != null || districtBuilding.ConstructionDistrict != null)
                {
                    continue;
                }
                var blockObject = building.GetComponent<BlockObject>();
                if (blockObject == null || blockObject.Coordinates == districtCenter)
                {
                    continue;
                }
                doorsteps.Add(blockObject.HasEntrance
                    ? blockObject.PositionedEntrance.DoorstepCoordinates
                    : blockObject.Coordinates);
            }
            return doorsteps;
        }

        /// <summary>Coordinates of all buildings matching the base template name.</summary>
        public List<Vector3Int> BuildingCoordinatesByName(string baseName)
        {
            var coordinates = new List<Vector3Int>();
            foreach (Building building in _entityComponentRegistry.GetEnabled<Building>())
            {
                if (!NameMatches(building.GameObject.name, baseName))
                {
                    continue;
                }
                var blockObject = building.GetComponent<BlockObject>();
                if (blockObject != null)
                {
                    coordinates.Add(blockObject.Coordinates);
                }
            }
            return coordinates;
        }

        /// <summary>Raises builder priority on every matching building still under
        /// construction. Returns how many were changed.</summary>
        public int BoostConstructionPriority(string baseName, Priority priority)
        {
            int boosted = 0;
            foreach (Building building in _entityComponentRegistry.GetEnabled<Building>())
            {
                if (!NameMatches(building.GameObject.name, baseName))
                {
                    continue;
                }
                var prioritizable = building.GetComponent<BuilderPrioritizable>();
                if (prioritizable != null && prioritizable.Priority != priority)
                {
                    prioritizable.SetPriority(priority);
                    boosted++;
                }
            }
            return boosted;
        }

        public Vector3Int? DistrictCenterCoordinates()
        {
            foreach (DistrictCenter districtCenter in _districtCenterRegistry.AllDistrictCenters)
            {
                return districtCenter.GetComponent<BlockObject>().Coordinates;
            }
            return null;
        }

        /// <summary>The district center's doorstep — the root of the path network.</summary>
        public Vector3Int? DistrictCenterDoorstep()
        {
            foreach (DistrictCenter districtCenter in _districtCenterRegistry.AllDistrictCenters)
            {
                var blockObject = districtCenter.GetComponent<BlockObject>();
                if (blockObject.HasEntrance)
                {
                    return blockObject.PositionedEntrance.DoorstepCoordinates;
                }
                return blockObject.Coordinates;
            }
            return null;
        }

        /// <summary>Centroid of the N trees (cuttable) or berry bushes (gatherable)
        /// closest to the anchor — where to aim flags.</summary>
        public Vector3Int? NearestResourceCluster(Vector3Int anchor, bool gatherable, int sampleSize = 12)
        {
            var positions = new List<Vector3Int>();
            foreach (NaturalResourceModel resource in _entityComponentRegistry.GetEnabled<NaturalResourceModel>())
            {
                bool matches = gatherable
                    ? resource.HasComponent<Gatherable>()
                    : resource.HasComponent<Cuttable>();
                if (!matches)
                {
                    continue;
                }
                var blockObject = resource.GetComponent<BlockObject>();
                if (blockObject != null)
                {
                    positions.Add(blockObject.Coordinates);
                }
            }
            if (positions.Count == 0)
            {
                return null;
            }
            positions.Sort((a, b) => Distance(a, anchor).CompareTo(Distance(b, anchor)));
            int take = Mathf.Min(sampleSize, positions.Count);
            var sum = Vector3Int.zero;
            for (int i = 0; i < take; i++)
            {
                sum += positions[i];
            }
            return new Vector3Int(sum.x / take, sum.y / take, 0);
        }

        /// <summary>Exact block coordinates of live trees (cuttable) or bushes
        /// (gatherable) within radius of the anchor — for precise area marking.</summary>
        public List<Vector3Int> ResourceCoordinatesNear(Vector3Int anchor, int radius, bool gatherable)
        {
            var coordinates = new List<Vector3Int>();
            foreach (NaturalResourceModel resource in _entityComponentRegistry.GetEnabled<NaturalResourceModel>())
            {
                bool matches = gatherable
                    ? resource.HasComponent<Gatherable>()
                    : resource.HasComponent<Cuttable>();
                if (!matches)
                {
                    continue;
                }
                var blockObject = resource.GetComponent<BlockObject>();
                if (blockObject != null && Distance(blockObject.Coordinates, anchor) <= radius)
                {
                    coordinates.Add(blockObject.Coordinates);
                }
            }
            return coordinates;
        }

        private static int Distance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static bool NameMatches(string gameObjectName, string baseName)
        {
            if (!gameObjectName.StartsWith(baseName))
            {
                return false;
            }
            if (gameObjectName.Length == baseName.Length)
            {
                return true;
            }
            char next = gameObjectName[baseName.Length];
            return next == '.' || next == '(';
        }
    }
}
