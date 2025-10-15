using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using EFCore.BulkExtensions;
using System.Collections.Concurrent;

namespace MySimulationAPI.SimulationEngine
{
    public class SimulationSetup
    {
        private readonly SimDbContext _db;
        private readonly BuildingService _buildingService;
        private readonly RoutingService _routingService;
        private readonly Random _rng = new(); // Need to seed to make 'randomness' consistent for for rewind / recalculation functions

        public SimulationSetup(SimDbContext db, BuildingService buildingService, RoutingService routingService)
        {
            _db = db;
            _buildingService = buildingService;
            _routingService = routingService;
        }

        public async Task<Simulation> CreateSimulation(string simulationName, int presetId, int agentCount, int userId)
        {

            // Load preset data
            var preset = await _db.MapAreaPresets
                .AsNoTracking()
                .Select(p => new { p.Id, p.Name, p.Latitude, p.Longitude, p.Radius })
                .FirstOrDefaultAsync(p => p.Id == presetId)
                ?? throw new Exception("Preset not found");

            // Load OSM buildings from preset
            var buildingPresets = await _db.OSMBuildingPresets
                .AsNoTracking()
                .Where(b => b.MapAreaPresetId == presetId)
                .Select(b => new { b.Name, b.Latitude, b.Longitude, b.Type, b.Capacity })
                .ToListAsync();

            // Create simulation
            var sim = new Simulation
            {
                UserId = userId,
                Name = simulationName,
            };
            await _db.BulkInsertAsync(new List<Simulation> { sim }, new BulkConfig { SetOutputIdentity = true });

            var disease = new Disease
            {
                Name = "Default Disease",
                SimulationId = sim.Id,
                InfectionRate = 0.1,
                RecoveryRate = 0.05,
                MortalityRate = 0.01,
            };
            _db.Diseases.Add(disease);
            await _db.SaveChangesAsync();

            var mapArea = new MapArea
            {
                Name = $"{simulationName} - {preset.Name}-{Guid.NewGuid()}",
                SimulationId = sim.Id,
                CenterLat = preset.Latitude,
                CenterLon = preset.Longitude,
                RadiusKm = preset.Radius,
                PresetRefID = preset.Id
            };
            _db.MapAreas.Add(mapArea);
            await _db.SaveChangesAsync();

            // Set the navigation property for the simulation
            sim.MapArea = mapArea;

            // Transform to simulation buildings (parallelized)
            // cant easily bulk insert with TPH inheritance, so split into separate lists and insert each type separately
            // also cant use SetOutputIdentity with TPH, so need to later re-query to get the IDs
            // also need to set the shadow property 'BuildingType' manually for each type so that the discriminator is set correctly
            var newBuildings = new List<Building>(buildingPresets.Count);
            int numHousing = buildingPresets.Count(bp => bp.Type == "Home");
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
                building.Capacity = 4;
                // for now assumign capacity of 4 per building but will need changing, will use number of housing buildings to estimate (can later refine with more parameters
                building.MapAreaId = mapArea.Id;
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
                        break;
                }

                newBuildings.Add(building);
            }
            try
            {
                // Group buildings by their implemented type
                var residentialBuildings = newBuildings.OfType<ResidentialBuilding>().ToList();
                var officeBuildings = newBuildings.OfType<OfficeBuilding>().ToList();
                var educationBuildings = newBuildings.OfType<EducationBuilding>().ToList();
                var medicalBuildings = newBuildings.OfType<MedicalBuilding>().ToList();
                var retailBuildings = newBuildings.OfType<RetailBuilding>().ToList();
                var leisureBuildings = newBuildings.OfType<LeisureBuilding>().ToList();
                foreach (var building in newBuildings.OfType<ResidentialBuilding>())
                    _db.Entry(building).Property("BuildingType").CurrentValue = "Residential";
                foreach (var building in newBuildings.OfType<OfficeBuilding>())
                    _db.Entry(building).Property("BuildingType").CurrentValue = "Office";
                foreach (var building in newBuildings.OfType<EducationBuilding>())
                    _db.Entry(building).Property("BuildingType").CurrentValue = "Education";
                foreach (var building in newBuildings.OfType<MedicalBuilding>())
                    _db.Entry(building).Property("BuildingType").CurrentValue = "Medical";
                foreach (var building in newBuildings.OfType<RetailBuilding>())
                    _db.Entry(building).Property("BuildingType").CurrentValue = "Retail";
                foreach (var building in newBuildings.OfType<LeisureBuilding>())
                    _db.Entry(building).Property("BuildingType").CurrentValue = "Leisure";

                var bulkConfig = new BulkConfig
                {
                    PreserveInsertOrder = true
                };

                // Insert each building type separately
                Console.WriteLine($"Inserting {residentialBuildings.Count} residential buildings...");
                if (residentialBuildings.Any())
                {
                    await _db.BulkInsertAsync(residentialBuildings, bulkConfig);
                }

                Console.WriteLine($"Inserting {officeBuildings.Count} office buildings...");
                if (officeBuildings.Any())
                {
                    await _db.BulkInsertAsync(officeBuildings, bulkConfig);
                }

                Console.WriteLine($"Inserting {educationBuildings.Count} education buildings...");
                if (educationBuildings.Any())
                {
                    await _db.BulkInsertAsync(educationBuildings, bulkConfig);
                }

                Console.WriteLine($"Inserting {medicalBuildings.Count} medical buildings...");
                if (medicalBuildings.Any())
                {
                    await _db.BulkInsertAsync(medicalBuildings, bulkConfig);
                }

                Console.WriteLine($"Inserting {retailBuildings.Count} retail buildings...");
                if (retailBuildings.Any())
                {
                    await _db.BulkInsertAsync(retailBuildings, bulkConfig);
                }

                Console.WriteLine($"Inserting {leisureBuildings.Count} leisure buildings...");
                if (leisureBuildings.Any())
                {
                    await _db.BulkInsertAsync(leisureBuildings, bulkConfig);
                }

                Console.WriteLine("All buildings inserted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Bulk insert failed: " + ex);
            }

            _db.ChangeTracker.Clear();

            // Load OSM paths + nodes
            var pathPresets = await _db.OSMPathPresets
                .AsNoTracking()
                .Include(p => p.Nodes)
                .Where(p => p.MapAreaPresetId == presetId)
                .ToListAsync();

            var newPaths = pathPresets.Select(presetPath => new Models.Path
            {
                Type = Enum.TryParse<PathType>(presetPath.Type, true, out var parsed) ? parsed : PathType.Other,
                IsOneWay = presetPath.IsOneWay,
                SpeedModifier = presetPath.SpeedModifier,
                MapAreaId = mapArea.Id,
                Nodes = presetPath.Nodes.Select(n => new PathNode
                {
                    Lat = n.Latitude,
                    Lon = n.Longitude
                }).ToList()
            }).ToList();

            await _db.BulkInsertAsync(newPaths, opts => opts.IncludeGraph = true);
            _db.ChangeTracker.Clear();

            // Generate Agents
            var agents = new List<Agent>(agentCount);
            for (int i = 0; i < agentCount; i++)
            {
                agents.Add(CreateRandomAgent(sim.Id));
            }
            Console.WriteLine($"Generated {agents.Count} agents for simulation {sim.Id}");


            // Group agents by their concrete type
            var studentAgents = agents.OfType<StudentAgent>().ToList();
            var officeWorkerAgents = agents.OfType<OfficeWorkerAgent>().ToList();
            var medicalWorkerAgents = agents.OfType<MedicalWorkerAgent>().ToList();
            var retailWorkerAgents = agents.OfType<RetailWorkerAgent>().ToList();
            var elderlyRetiredAgents = agents.OfType<ElderlyRetiredAgent>().ToList();
            var serviceWorkerAgents = agents.OfType<ServiceWorkerAgent>().ToList();
            foreach (var agent in agents.OfType<StudentAgent>())
                _db.Entry(agent).Property("AgentType").CurrentValue = "Student";

            foreach (var agent in agents.OfType<OfficeWorkerAgent>())
                _db.Entry(agent).Property("AgentType").CurrentValue = "OfficeWorker";

            foreach (var agent in agents.OfType<MedicalWorkerAgent>())
                _db.Entry(agent).Property("AgentType").CurrentValue = "MedicalWorker";

            foreach (var agent in agents.OfType<RetailWorkerAgent>())
                _db.Entry(agent).Property("AgentType").CurrentValue = "RetailWorker";

            foreach (var agent in agents.OfType<ElderlyRetiredAgent>())
                _db.Entry(agent).Property("AgentType").CurrentValue = "ElderlyRetired";

            foreach (var agent in agents.OfType<ServiceWorkerAgent>())
                _db.Entry(agent).Property("AgentType").CurrentValue = "ServiceWorker";

            var agentBulkConfig = new BulkConfig
            {
                PreserveInsertOrder = true,
            };

            Console.WriteLine($"Inserting {studentAgents.Count} student agents...");
            if (studentAgents.Any())
            {
                await _db.BulkInsertAsync(studentAgents, agentBulkConfig);
            }
            Console.WriteLine($"Inserting {officeWorkerAgents.Count} office worker agents...");
            if (officeWorkerAgents.Any())
            {
                await _db.BulkInsertAsync(officeWorkerAgents, agentBulkConfig);
            }
            Console.WriteLine($"Inserting {medicalWorkerAgents.Count} medical worker agents...");
            if (medicalWorkerAgents.Any())
            {
                await _db.BulkInsertAsync(medicalWorkerAgents, agentBulkConfig);
            }
            Console.WriteLine($"Inserting {retailWorkerAgents.Count} retail worker agents...");
            if (retailWorkerAgents.Any())
            {
                await _db.BulkInsertAsync(retailWorkerAgents, agentBulkConfig);
            }
            Console.WriteLine($"Inserting {elderlyRetiredAgents.Count} elderly retired agents...");
            if (elderlyRetiredAgents.Any())
            {
                await _db.BulkInsertAsync(elderlyRetiredAgents, agentBulkConfig);
            }
            Console.WriteLine($"Inserting {serviceWorkerAgents.Count} service worker agents...");
            if (serviceWorkerAgents.Any())
            {
                await _db.BulkInsertAsync(serviceWorkerAgents, agentBulkConfig);
            }

            // Rehydrate objects with their assigned IDs and navigation properties
            agents = await _db.Agents
                .Where(a => a.SimulationId == sim.Id)
                .Include(a => a.House)
                .Include(a => a.Workplace)
                .Include(a => a.Simulation)
                    .ThenInclude(s => s.MapArea)
                .ToListAsync();

            Console.WriteLine($"Retrieved {agents.Count} agents from database with IDs");

            newBuildings = await _db.Buildings
                .Where(b => b.MapAreaId == mapArea.Id)
                .ToListAsync();

            var residentialBuildingsForAssignment = newBuildings
                .OfType<ResidentialBuilding>()
                .ToList();


            // Assign housing and workplaces
            BuildingService.AssignHousing(agents, residentialBuildingsForAssignment, _rng);
            Console.WriteLine("Assigned housing to agents");
            BuildingService.AssignWorkplaces(agents, newBuildings, 2, 20, 5, _rng);
            Console.WriteLine("Assigned workplaces to agents");


            // Set the FK IDs explicitly
            foreach (var agent in agents)
            {
                agent.HouseId = agent.House?.Id;
                agent.WorkplaceId = agent.Workplace?.Id;
            }

            // Update only the foreign key properties
            var bulkUpdateConfig = new BulkConfig
            {
                PropertiesToInclude = new List<string> { "HouseId", "WorkplaceId" },
                UpdateByProperties = new List<string> { "Id" }
            };
            await _db.BulkUpdateAsync(agents, bulkUpdateConfig);

            // Generate routines for all agents
            foreach (var agent in agents)
            {
                {
                    try
                    {
                        // Generate routine events
                        agent.RoutineEvents = agent.GenerateRoutine(_routingService, _buildingService);
                        foreach (var routineEvent in agent.RoutineEvents)
                        {
                            routineEvent.AgentId = agent.Id;
                            routineEvent.BuildingId = routineEvent.Building.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error generating routine for agent {agent.Id}: {ex.Message}");
                    }
                }
            }

            // Save all routine events to the database
            var routineEvents = agents.SelectMany(a => a.RoutineEvents).ToList();
            Console.WriteLine($"Generatedr outine events for {agents.Count} agents");

            // Set up bulk config for routine events
            var routineEventConfig = new BulkConfig
            {
                PreserveInsertOrder = true,
                SetOutputIdentity = true
            };

            // Bulk insert routine events
            await _db.BulkInsertAsync(routineEvents, routineEventConfig);
            Console.WriteLine($"Saved {routineEvents.Count} routine events to database");

            // generate and insert medical profiles for each agent
            var medicalProfiles = new List<MedicalProfile>(agents.Count);
            foreach (var agent in agents)
            {
                var profile = agent.GenerateMedicalProfile(_rng);
                profile.AgentId = agent.Id; // Set FK explicitly
                medicalProfiles.Add(profile);
            }

            // Bulk insert medical profiles
            var profileBulkConfig = new BulkConfig { PreserveInsertOrder = true };
            try
            {
                await _db.BulkInsertAsync(medicalProfiles, profileBulkConfig);
            }
            catch
            {
                throw;
            }

            return sim;
        }

        private Agent CreateRandomAgent(int simId)
        {
            var type = _rng.Next(6);
            Agent agent = type switch
            {
                0 => new StudentAgent(),
                1 => new OfficeWorkerAgent(),
                2 => new MedicalWorkerAgent(),
                3 => new RetailWorkerAgent(),
                4 => new ElderlyRetiredAgent(),
                5 => new ServiceWorkerAgent(),
                _ => new StudentAgent()
            };

            agent.Name = agent.GenerateRandomName(_rng);
            agent.SimulationId = simId;
            return agent;
        }

    }
}