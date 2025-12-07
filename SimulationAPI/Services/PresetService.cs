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

        public async Task<MapAreaPreset> FetchAndCreateMapPresetAsync(string name, double latitude, double longitude, double radiusKm, int? userId = null)
        {
            var (buildings, paths) = await _osmService.FetchMapDataAsync(latitude, longitude, radiusKm);

            // Create preset from fetched data
            return await CreateMapPresetAsync(name, latitude, longitude, radiusKm,
                buildings,
                paths, userId);
        }

        public async Task<MapAreaPreset> CreateMapPresetAsync(
    string name, double latitude, double longitude, double radiusKm,
    List<OSMBuildingPreset> buildings, List<OSMPathPreset> paths, int? userId = null)
        {
            // Disable tracking completely for performance
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            // insert the parent preset
            var preset = new MapAreaPreset
            {
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                Radius = radiusKm,
                UserId = userId
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
                    Type = b.Type,
                    Capacity = b.Capacity
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
                    SpeedModifier = p.SpeedModifier,
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

                foreach (var (pathEntity, originalPath) in pathEntities.Zip(paths!))  // paths! is safe here due to check above
                {
                    if (originalPath.Nodes is null || !originalPath.Nodes.Any())
                        continue;

                    nodeEntities.AddRange(originalPath.Nodes.Select(n => new OSMPathNodePreset
                    {
                        OSMPathPresetId = pathEntity.Id,
                        OSMNodeId = n.OSMNodeId,
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
                    InfectionRate = 0.02,              // 2% per hour of close contact
                    RecoveryRate = 0.07,               // ~14 days to recover
                    MortalityRate = 0.015,             // 1.5% case fatality rate
                    IncubationPeriod = 3.0,            // 3 days incubation (presymptomatic but infectious after day 2)
                    InfectiousPeriod = 10.0,           // 10 days of peak infectiousness
                    SymptomaticRate = 0.7,             // 70% develop noticeable symptoms
                    SevereSymptomsRate = 0.15,         // 15% develop severe symptoms
                    HospitalSeekingRate = 0.6,         // 60% of severe cases seek medical help
                    ImmunityDuration = 180,            // ~6 months immunity
                    ReinfectionResistance = 0.7,       // 70% protection from reinfection
                    MaskEffectiveness = 0.5,           // 50% reduction with masks
                    VaccineEffectiveness = 0.8         // 80% efficacy
                });
            }

            if (!await _db.DiseasePresets.AnyAsync(d => d.Name == "Influenza"))
            {
                _db.DiseasePresets.Add(new DiseasePreset
                {
                    Name = "Influenza",
                    InfectionRate = 0.015,             // Slightly lower than COVID
                    RecoveryRate = 0.14,               // ~7 days recovery
                    MortalityRate = 0.005,             // 0.5% CFR
                    IncubationPeriod = 1.5,            // 1.5 days incubation (faster)
                    InfectiousPeriod = 7.0,            // 7 days infectious
                    SymptomaticRate = 0.85,            // 85% develop symptoms
                    SevereSymptomsRate = 0.1,          // 10% severe
                    HospitalSeekingRate = 0.4,         // 40% of severe seek help
                    ImmunityDuration = 90,             // ~3 months (seasonal)
                    ReinfectionResistance = 0.5,       // 50% protection (strain variation)
                    MaskEffectiveness = 0.4,           // 40% reduction
                    VaccineEffectiveness = 0.6         // 60% efficacy (varies by strain)
                });
            }

            // === Seed Global MapArea Presets ===
            if (!await _db.MapAreaPresets.AnyAsync(m => m.Name == "CambridgeCity"))
            {
                await FetchAndCreateMapPresetAsync("CambridgeCity", 52.1951, 0.1313, 1);
            }
            if (!await _db.MapAreaPresets.AnyAsync(m => m.Name == "SaffronWalden"))
            {
                await FetchAndCreateMapPresetAsync("SaffronWalden", 52.0174, 0.2438, 1);
            }
            await _db.SaveChangesAsync();
        }
    }
}