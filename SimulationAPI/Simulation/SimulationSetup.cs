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
        private readonly RoutingService _routingService;
        private readonly Random _rng = new Random(); // can seed for reproducibility

        public SimulationSetup(SimDbContext db, BuildingService buildingService, RoutingService routingService)
        {
            _db = db;
            _buildingService = buildingService;
            _routingService = routingService;
        }
        public async Task<Simulation> CreateSimulation(string simulationName, int presetId, int agentCount, int userId)
        {
            // Load just the MapAreaPreset metadata
            var preset = await _db.MapAreaPresets
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == presetId);

            if (preset == null) throw new Exception("Preset not found");

            // Create a new MapArea 
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

            // Load Buildings separately
            var buildingPresets = await _db.OSMBuildingPresets
                .AsNoTracking()
                .Where(b => b.MapAreaPresetId == presetId)
                .ToListAsync();
            // Transform into 'real' Buildings
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
                building.Capacity = 4; // FOR NOW ASSUME CAPACITY OF FOUR BUT NEEDS CHANGING
                // building.Capacity = b.Capacity ?? 4;
                // Calculate number of residential buildings (housing)

                // if (b.Type == "Home" && numHousing > 0)
                // {
                //     // Distribute agents evenly across housing buildings, can be made more complex in future
                //     building.Capacity = agentCount / numHousing;
                //     building.RemainingCapacity = building.Capacity;
                // }
                // else
                // {
                //     building.RemainingCapacity = b.Capacity ?? 0;
                // }

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
            Console.WriteLine($"Loaded {newBuildings.Count} building presets for presetId {presetId}");
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

                // Create a bulk config without trying to include shadow properties
                var bulkConfig = new BulkConfig
                {
                    // Let EF Core handle the discriminator automatically
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
                MapAreaId = mapArea.Id,
                Nodes = presetPath.Nodes.Select(nodePreset => new PathNode
                {
                    Lat = nodePreset.Latitude,
                    Lon = nodePreset.Longitude,
                    ConnectedPaths = new List<Models.Path>() // can link later
                }).ToList()
            }).ToList();

            await _db.BulkInsertAsync(newPaths, options => options.IncludeGraph = true);

            // 6. Create Disease record
            var Disease = new Disease
            {
                Name = "Default Disease",
                InfectionRate = 0.1,
                RecoveryRate = 0.05,
                MortalityRate = 0.01
            };

            // 6. Create Simulation record
            var user = await _db.Users.FindAsync(userId);
            if (user == null) throw new Exception($"User with ID {userId} not found");
            var sim = new Simulation
            {
                User = user,
                Name = simulationName,
                MapArea = mapArea,
                Agents = new List<Agent>(), // will bulk insert below
                Diseases = new List<Disease> { Disease },
            };

            _db.Simulations.Add(sim);
            await _db.SaveChangesAsync();
            Console.WriteLine($"Created simulation {sim.Name} with ID {sim.Id}");


            // 6. Generate Agents
            var agents = new List<Agent>(agentCount);
            for (int i = 0; i < agentCount; i++)
            {
                agents.Add(CreateRandomAgent(sim));
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

            // Create a bulk config without trying to include shadow properties
            var agentBulkConfig = new BulkConfig
            {
                PreserveInsertOrder = true, // to keep order (no longer needed)
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

            // Fetch objects back with DB IDs
            var savedAgents = await _db.Agents
                .Where(a => a.SimulationId == sim.Id)
                .ToListAsync();
            Console.WriteLine($"Retrieved {savedAgents.Count} agents from database with IDs");

            var residentialBuildingsForAssignment = await _db.Buildings
                .OfType<ResidentialBuilding>()
                .Where(b => b.MapAreaId == mapArea.Id)
                .ToListAsync();

            var allBuildingsForWorkplace = await _db.Buildings
                .Where(b => b.MapAreaId == mapArea.Id)
                .ToListAsync();


            // Assign housing and workplaces

            BuildingService.AssignHousing(savedAgents, residentialBuildingsForAssignment, _rng);
            Console.WriteLine("Assigned housing to agents");
            BuildingService.AssignWorkplaces(savedAgents, allBuildingsForWorkplace, 2, 20, 5, _rng);
            Console.WriteLine("Assigned workplaces to agents");


            // Set the FK IDs explicitly
            foreach (var agent in savedAgents)
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

            await _db.BulkUpdateAsync(savedAgents, bulkUpdateConfig);


            Console.WriteLine("Generating agent routines...");
            // Generate routines for all agents
            foreach (var agent in savedAgents)
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
            var routineEvents = savedAgents.SelectMany(a => a.RoutineEvents).ToList();
            Console.WriteLine($"Generatedr outine events for {savedAgents.Count} agents");

            // Set up bulk config for routine events
            var routineEventConfig = new BulkConfig
            {
                PreserveInsertOrder = true,
                SetOutputIdentity = true
            };

            // Bulk insert routine events
            await _db.BulkInsertAsync(routineEvents, routineEventConfig);
            Console.WriteLine($"Saved {routineEvents.Count} routine events to database");

            // create and insert medical profiles
            var medicalProfiles = new List<MedicalProfile>(agents.Count);
            foreach (var agent in savedAgents)
            {
                var profile = agent.GenerateMedicalProfile(_rng);
                profile.AgentId = agent.Id; // Set FK explicitly
                medicalProfiles.Add(profile);
            }

            // Bulk insert medical profiles
            var profileBulkConfig = new BulkConfig { PreserveInsertOrder = true };
            try
            {
                Console.WriteLine($"Inserting {medicalProfiles.Count} medical profiles...");
                await _db.BulkInsertAsync(medicalProfiles, profileBulkConfig);
                Console.WriteLine("All medical profiles inserted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting medical profiles: {ex.Message}");
                throw;
            }


            return sim;
        }

        private Agent CreateRandomAgent(Simulation sim)
        {
            int type = _rng.Next(6); // adjust based on your agent types

            // Create agent with proper type
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

            // Generate a random name for the agent
            agent.Name = GenerateRandomName();
            agent.SimulationId = sim.Id;

            return agent;
        }
        private string GenerateRandomName()
        {
            // Sample first names and last names
            string[] firstNames = { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
                         "William", "Elizabeth", "David", "Susan", "Richard", "Jessica", "Joseph", "Sarah" };

            string[] lastNames = { "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson",
                        "Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin" };

            // Generate a random full name
            return $"{firstNames[_rng.Next(firstNames.Length)]} {lastNames[_rng.Next(lastNames.Length)]}";
        }

    }
}