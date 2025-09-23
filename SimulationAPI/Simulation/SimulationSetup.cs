using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using EFCore.BulkExtensions;
namespace MySimulationAPI.SimulationEngine
{
    public class SimulationSetup
    {
        private readonly SimDbContext _db;
        private readonly BuildingService _buildingService;
        private readonly Random _rng = new Random();

        public SimulationSetup(SimDbContext db, BuildingService buildingService)
        {
            _db = db;
            _buildingService = buildingService;
        }
        public async Task<Simulation> CreateSimulation(string simulationName, int presetId, int agentCount, int userId)
        {
            // 1. Load just the MapAreaPreset metadata
            var preset = await _db.MapAreaPresets
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == presetId);

            if (preset == null) throw new Exception("Preset not found");

            // 2. Create a new MapArea 
            var mapArea = new MapArea
            {
                Name = $"{simulationName} - {preset.Name}-{Guid.NewGuid()}",
                CenterLat = preset.Latitude,
                CenterLon = preset.Longitude,
                RadiusKm = preset.Radius,
                PresetRefID = preset.Id
            };

            _db.MapAreas.Add(mapArea);
            await _db.SaveChangesAsync();

            // 3. Load Buildings separately
            var buildingPresets = await _db.OSMBuildingPresets
                .AsNoTracking()
                .Where(b => b.MapAreaPresetId == presetId)
                .ToListAsync();

            // Transform into real Buildings
            var newBuildings = new List<Building>(buildingPresets.Count);

            foreach (var b in buildingPresets)
            {
                Building building = b.Type switch
                {
                    "Home" => new ResidentialBuilding(),
                    "Work" => new OfficeBuilding(),
                    "School" => new EducationBuilding(),
                    "Medical" => new MedicalBuilding(),
                    "Retail" => new RetailBuilding(),
                    "Leisure" => new LeisureBuilding(),
                    _ => throw new Exception($"Unknown building type: {b.Type}")
                };

                building.Name = b.Name;
                building.Latitude = b.Latitude;
                building.Longitude = b.Longitude;
                building.Capacity = b.Capacity ?? 0;
                building.RemainingCapacity = b.Capacity ?? 0;
                building.MapAreaId = mapArea.Id; // assign by FK to avoid heavy tracking

                switch (building)
                {
                    case OfficeBuilding office:
                        office.HasCafeteria = false;
                        break;
                    case EducationBuilding edu:
                        edu.MaxStudents = b.Capacity ?? 0;
                        break;
                    case MedicalBuilding med:
                        med.NumBeds = b.Capacity ?? 0;
                        med.HasICU = false;
                        break;
                    case RetailBuilding retail:
                        retail.HasFood = false;
                        break;
                    case LeisureBuilding leisure:
                        leisure.HasFood = false;
                        break;
                }

                newBuildings.Add(building);
            }

            // Bulk insert all buildings in one go
            await _db.BulkInsertAsync(newBuildings);

            // 4. Load and transform Paths (usually fewer, so regular EF insert is fine)
            var pathPresets = await _db.OSMPathPresets
                .AsNoTracking()
                .Where(p => p.MapAreaPresetId == presetId)
                .Include(p => p.Nodes)
                .ToListAsync();

            var newPaths = pathPresets.Select(presetPath => new Models.Path
            {
                Type = Enum.TryParse<PathType>(presetPath.Type, true, out var parsedType)
                    ? parsedType
                    : PathType.Other,
                IsOneWay = presetPath.IsOneWay,
                SpeedModifier = presetPath.SpeedModifier,
                Nodes = presetPath.Nodes.Select(nodePreset => new PathNode
                {
                    Lat = nodePreset.Latitude,
                    Lon = nodePreset.Longitude,
                    ConnectedPaths = new List<Models.Path>() // can link later
                }).ToList()
            }).ToList();

            await _db.BulkInsertAsync(newPaths, options => options.IncludeGraph = true);

            // 5. Create Simulation record
            var user = await _db.Users.FindAsync(userId);
            if (user == null) throw new Exception($"User with ID {userId} not found");
            var sim = new Simulation
            {
                User = user,
                Name = simulationName,
                MapArea = mapArea,
                Agents = new List<Agent>() // will bulk insert below
            };

            _db.Simulations.Add(sim);
            await _db.SaveChangesAsync();

            // 6. Generate Agents
            var agents = new List<Agent>(agentCount);
            for (int i = 0; i < agentCount; i++)
            {
                agents.Add(CreateRandomAgent());
            }

            await _db.BulkInsertAsync(agents);

            // 7. Assign housing & jobs (in-memory assignment, then bulk update agents)
            BuildingService.AssignHousing(agents, newBuildings.OfType<ResidentialBuilding>().ToList(), _rng);
            BuildingService.AssignWorkplaces(agents, newBuildings.ToList(), 2, 20, 5, _rng);

            await _db.BulkUpdateAsync(agents);

            return sim;
        }

        private Agent CreateRandomAgent()
        {
            int type = _rng.Next(6); // adjust based on your agent types
            return type switch
            {
                0 => new StudentAgent(),
                1 => new OfficeWorkerAgent(),
                2 => new MedicalWorkerAgent(),
                3 => new RetailWorkerAgent(),
                4 => new ElderlyRetiredAgent(),
                5 => new ServiceWorkerAgent(),
                _ => new StudentAgent()
            };
        }

    }
}