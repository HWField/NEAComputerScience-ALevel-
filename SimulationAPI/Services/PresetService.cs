using System.Net.Http;
using System.Text.Json;
using MySimulationAPI.Models;
using MySimulationAPI.Data;
using EFCore.BulkExtensions;
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

        public async Task<MapAreaPreset> FetchAndCreateMapPresetAsync(string name, double latitude, double longitude, double radiusKm)
        {
            var (buildings, paths) = await _osmService.FetchMapDataAsync(latitude, longitude, radiusKm);

            // Create preset from fetched data
            return await CreateMapPresetAsync(name, latitude, longitude, radiusKm,
                buildings,
                paths);
        }

        public async Task<MapAreaPreset> CreateMapPresetAsync(
    string name, double latitude, double longitude, double radiusKm,
    List<OSMBuildingPreset> buildings, List<OSMPathPreset> paths)
        {
            // Disable tracking completely for performance
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            // insert the parent preset
            var preset = new MapAreaPreset
            {
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                Radius = radiusKm
            };

            await _db.BulkInsertAsync(new List<MapAreaPreset> { preset }, new BulkConfig
            {
                SetOutputIdentity = true
            });

            // Bulk insert buildings
            List<OSMBuildingPreset> buildingEntities = new();
            if (buildings?.Any() == true)
            {
                buildingEntities = buildings.Select(b => new OSMBuildingPreset
                {
                    MapAreaPresetId = preset.Id,
                    Name = b.Name ?? "Unknown",
                    OSMId = b.OSMId,
                    Latitude = b.Latitude,
                    Longitude = b.Longitude,
                    Type = b.Type
                }).ToList();

                await _db.BulkInsertAsync(buildingEntities, new BulkConfig
                {
                    BatchSize = 5000
                });
            }

            // Bulk insert paths (and capture generated IDs)
            List<OSMPathPreset> pathEntities = new();
            if (paths?.Any() == true)
            {
                pathEntities = paths.Select(p => new OSMPathPreset
                {
                    MapAreaPresetId = preset.Id,
                    Type = p.Type,
                    IsOneWay = p.IsOneWay,
                    OSMId = p.OSMId
                }).ToList();

                await _db.BulkInsertAsync(pathEntities, new BulkConfig
                {
                    SetOutputIdentity = true,
                    BatchSize = 5000
                });
            }

            // Bulk insert path nodes (linked by path IDs)
            if (pathEntities.Any())
            {
                var nodeEntities = new List<OSMPathNodePreset>();

                foreach (var (pathEntity, originalPath) in pathEntities.Zip(paths))
                {
                    if (originalPath.Nodes is null || !originalPath.Nodes.Any())
                        continue;

                    nodeEntities.AddRange(originalPath.Nodes.Select(n => new OSMPathNodePreset
                    {
                        OSMPathPresetId = pathEntity.Id,
                        Latitude = n.Latitude,
                        Longitude = n.Longitude
                    }));
                }

                if (nodeEntities.Count > 0)
                {
                    await _db.BulkInsertAsync(nodeEntities, new BulkConfig
                    {
                        BatchSize = 10000
                    });
                }
            }

            // Re-enable trackings
            _db.ChangeTracker.AutoDetectChangesEnabled = true;

            // Return a detached preset with populated collections (for caller convenience)
            preset.Buildings = buildingEntities;
            preset.Paths = pathEntities;

            return preset;
        }

        public async Task SeedDefaultsAsync()
        {
            // === Seed Admin User ===
            if (!await _db.Users.AnyAsync(u => u.Username == "admin"))
            {
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Environment.GetEnvironmentVariable("ADMIN_PASSWORD")),
                    Role = "Admin"
                };
                _db.Users.Add(adminUser);
            }

            // === Seed Global Disease Presets ===
            if (!await _db.DiseasePresets.AnyAsync(d => d.Name == "Covid-19"))
            {
                _db.DiseasePresets.Add(new DiseasePreset
                {
                    Name = "Covid-19",
                    InfectionRate = 0.3,
                    RecoveryRate = 0.1,
                    MortalityRate = 0.015,
                });
            }

            if (!await _db.DiseasePresets.AnyAsync(d => d.Name == "Influenza"))
            {
                _db.DiseasePresets.Add(new DiseasePreset
                {
                    Name = "Influenza",
                    InfectionRate = 0.15,
                    RecoveryRate = 0.12,
                    MortalityRate = 0.005,
                });
            }

            // === Seed Global MapArea Presets ===
            if (!await _db.MapAreaPresets.AnyAsync(m => m.Name == "CambridgeCity"))
            {
                await FetchAndCreateMapPresetAsync("CambridgeCity", 52.1951, 0.1313, 1);
            }
            await _db.SaveChangesAsync();
        }
    }
}