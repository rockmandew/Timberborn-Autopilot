using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.Cutting;
using Timberborn.EntitySystem;
using Timberborn.GameDistricts;
using Timberborn.Gathering;
using Timberborn.NaturalResourcesModelSystem;
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

        public Vector3Int? DistrictCenterCoordinates()
        {
            foreach (DistrictCenter districtCenter in _districtCenterRegistry.AllDistrictCenters)
            {
                return districtCenter.GetComponent<BlockObject>().Coordinates;
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
