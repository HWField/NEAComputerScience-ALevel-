using System.Net.Http;
using System.Text.Json;
using MySimulationAPI.Models;
using MySimulationAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace MySimulationAPI.Services
{
    public class PresetService
    {
        private readonly SimDbContext _db;
        private readonly OsmService _osmService;

        public PresetService(SimDbContext db, OsmService osmService)
        {
            _db = db;
            _osmService = osmService;
        }

        public async Task<MapAreaPreset> FetchAndCreatePresetAsync(string name, double latitude, double longitude, double radiusKm)
        {
            // Fetch both buildings and paths in one call
            var (buildings, paths) = await _osmService.FetchMapDataAsync(latitude, longitude, radiusKm);

            // Create preset from fetched data
            return await CreatePresetAsync(name, latitude, longitude, radiusKm,
                buildings,
                paths);
        }

        public async Task<MapAreaPreset> CreatePresetAsync(string name, double latitude, double longitude, double radiusKm, List<OSMBuildingPreset> buildings, List<OSMPathPreset> paths)
        {
            var preset = new MapAreaPreset
            {
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                Radius = radiusKm,
                Buildings = new List<OSMBuildingPreset>(),
                Paths = new List<OSMPathPreset>()
            };

            foreach (var building in buildings)
            {
                preset.Buildings.Add(new OSMBuildingPreset
                {
                    Name = building.Name ?? "Unknown",
                    OSMId = building.OSMId,
                    Latitude = building.Latitude,
                    Longitude = building.Longitude,
                    Type = building.Type
                });
            }

            foreach (var path in paths)
            {
                var pathPreset = new OSMPathPreset
                {
                    Type = path.Type,
                    IsOneWay = path.IsOneWay,
                    Nodes = new List<OSMPathNodePreset>()
                };

                foreach (var node in path.Nodes)
                {
                    pathPreset.Nodes.Add(new OSMPathNodePreset
                    {
                        Latitude = node.Latitude,
                        Longitude = node.Longitude
                    });
                }

                preset.Paths.Add(pathPreset);
            }

            _db.MapAreaPresets.Add(preset);
            await _db.SaveChangesAsync();
            return preset;
        }

    }
}