using System.Collections.Generic;
using System.Text;
using Timberborn.BlockSystem;
using Timberborn.GameDistricts;
using Timberborn.SoilMoistureSystem;
using Timberborn.TerrainSystem;
using Timberborn.WaterSystem;
using UnityEngine;

namespace TimberbornAutopilot.Sensing
{
    /// <summary>
    /// Compact spatial snapshot for site selection: terrain heights, water,
    /// and soil moisture around a point (default: the starting district center).
    /// Terrain row encoding: base-36 digit per tile (height 0-35).
    /// Surface row encoding: '~' water, '+' moist land, '.' dry land.
    /// </summary>
    public class MapSurveyor
    {
        private readonly ITerrainService _terrainService;
        private readonly IThreadSafeWaterMap _waterMap;
        private readonly ISoilMoistureService _soilMoistureService;
        private readonly DistrictCenterRegistry _districtCenterRegistry;

        public MapSurveyor(ITerrainService terrainService,
                           IThreadSafeWaterMap waterMap,
                           ISoilMoistureService soilMoistureService,
                           DistrictCenterRegistry districtCenterRegistry)
        {
            _terrainService = terrainService;
            _waterMap = waterMap;
            _soilMoistureService = soilMoistureService;
            _districtCenterRegistry = districtCenterRegistry;
        }

        public object Survey(int? centerX, int? centerY, int radius)
        {
            Vector3Int mapSize = _terrainService.Size;
            var districts = new List<object>();
            Vector3Int center = new Vector3Int(mapSize.x / 2, mapSize.y / 2, 0);
            foreach (DistrictCenter districtCenter in _districtCenterRegistry.AllDistrictCenters)
            {
                Vector3Int coords = districtCenter.GetComponent<BlockObject>().Coordinates;
                districts.Add(new { x = coords.x, y = coords.y, z = coords.z });
                center = coords;
            }
            if (centerX.HasValue && centerY.HasValue)
            {
                center = new Vector3Int(centerX.Value, centerY.Value, 0);
            }

            int x0 = Mathf.Max(0, center.x - radius);
            int x1 = Mathf.Min(mapSize.x - 1, center.x + radius);
            int y0 = Mathf.Max(0, center.y - radius);
            int y1 = Mathf.Min(mapSize.y - 1, center.y + radius);

            var terrainRows = new List<string>();
            var surfaceRows = new List<string>();
            for (int y = y0; y <= y1; y++)
            {
                var terrainRow = new StringBuilder();
                var surfaceRow = new StringBuilder();
                for (int x = x0; x <= x1; x++)
                {
                    var column = new Vector3Int(x, y, 0);
                    int height = _terrainService.GetTerrainHeightBelow(
                        new Vector3Int(x, y, mapSize.z - 1));
                    terrainRow.Append(ToBase36(height));

                    var surfaceCoords = new Vector3Int(x, y, height);
                    if (_waterMap.WaterDepth(surfaceCoords) > 0.05f)
                    {
                        surfaceRow.Append('~');
                    }
                    else if (_soilMoistureService.SoilIsMoist(column))
                    {
                        surfaceRow.Append('+');
                    }
                    else
                    {
                        surfaceRow.Append('.');
                    }
                }
                terrainRows.Add(terrainRow.ToString());
                surfaceRows.Add(surfaceRow.ToString());
            }

            return new
            {
                mapSize = new { x = mapSize.x, y = mapSize.y, z = mapSize.z },
                window = new { x0, y0, x1, y1, note = "rows ordered y0->y1, chars x0->x1" },
                districts,
                terrain = terrainRows,
                surface = surfaceRows,
            };
        }

        private static char ToBase36(int value)
        {
            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            return digits[Mathf.Clamp(value, 0, 35)];
        }
    }
}
