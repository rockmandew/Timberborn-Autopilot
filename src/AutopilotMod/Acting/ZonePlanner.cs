using System.Collections.Generic;
using Timberborn.Forestry;
using Timberborn.Planting;
using UnityEngine;

namespace TimberbornAutopilot.Acting
{
    /// <summary>Zones crops for farmhouses and trees for foresters/lumberjacks.</summary>
    public class ZonePlanner
    {
        private readonly PlantingService _plantingService;
        private readonly PlantingAreaValidator _plantingAreaValidator;
        private readonly TreeCuttingArea _treeCuttingArea;

        public ZonePlanner(PlantingService plantingService,
                           PlantingAreaValidator plantingAreaValidator,
                           TreeCuttingArea treeCuttingArea)
        {
            _plantingService = plantingService;
            _plantingAreaValidator = plantingAreaValidator;
            _treeCuttingArea = treeCuttingArea;
        }

        /// <summary>Zones a rectangle for planting ("Carrot", "Pine", ...).
        /// Skips invalid tiles; returns how many tiles were zoned.</summary>
        public int ZonePlanting(Vector3Int from, Vector3Int to, string resource)
        {
            int zoned = 0;
            foreach (Vector3Int tile in Rectangle(from, to))
            {
                if (_plantingAreaValidator.CanPlant(tile, resource))
                {
                    _plantingService.SetPlantingCoordinates(tile, resource);
                    zoned++;
                }
            }
            return zoned;
        }

        public int UnzonePlanting(Vector3Int from, Vector3Int to)
        {
            int cleared = 0;
            foreach (Vector3Int tile in Rectangle(from, to))
            {
                if (_plantingService.GetResourceAt(tile) != null)
                {
                    _plantingService.UnsetPlantingCoordinates(tile);
                    cleared++;
                }
            }
            return cleared;
        }

        /// <summary>Marks a rectangle of trees for cutting.</summary>
        public void MarkTreesForCutting(Vector3Int from, Vector3Int to)
        {
            var tiles = new List<Vector3Int>(Rectangle(from, to));
            _treeCuttingArea.AddCoordinates(tiles);
        }

        private static IEnumerable<Vector3Int> Rectangle(Vector3Int from, Vector3Int to)
        {
            int x0 = Mathf.Min(from.x, to.x), x1 = Mathf.Max(from.x, to.x);
            int y0 = Mathf.Min(from.y, to.y), y1 = Mathf.Max(from.y, to.y);
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    yield return new Vector3Int(x, y, from.z);
                }
            }
        }
    }
}
