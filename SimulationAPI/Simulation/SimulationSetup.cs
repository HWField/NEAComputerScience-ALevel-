using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using EFCore.BulkExtensions;
using System.Collections.Concurrent;
using SQLitePCL;
using MySimulationAPI.DTOs;

namespace MySimulationAPI.SimulationConfiguration
{
    public class SimulationSetup
    {
        private readonly IDbContextFactory<SimDbContext> _dbFactory;
        private readonly PresetService _presetService;
        private readonly SimulationManager _simulationManager;
        private readonly Random _rng = new();

        public SimulationSetup(IDbContextFactory<SimDbContext> dbFactory, PresetService presetService, SimulationManager simulationManager)
        {
            _dbFactory = dbFactory;
            _presetService = presetService;
            _simulationManager = simulationManager;
        }

        // Create initial simulation with just name (will later add input for parameters such as seed, tick rate etc)
        public async Task<int> InitialiseSimulation(string simulationName, int userId)
        {
            using var db = _dbFactory.CreateDbContext();
            var sim = new Simulation
            {
                UserId = userId,
                Name = simulationName,
            };

            db.Simulations.Add(sim);
            await db.SaveChangesAsync();

            return sim.Id;
        }

        // Initialise map area and return population estimates based on housing capacity
        public async Task<PopulationCapacityResult> SetupMapArea(int simulationId, int? presetId, string? presetName, double? latitude, double? longitude, double? radiusKm, bool? savePreset, int userId, bool IsAdmin)
        {
            using var _db = _dbFactory.CreateDbContext();
            // Verify simulation exists and belongs to user
            var simulation = await _db.Simulations
                .FirstOrDefaultAsync(s => s.Id == simulationId && s.UserId == userId)
                ?? throw new Exception("Simulation not found or access denied");

            int actualPresetId;

            // switch between using existing preset or creating new one from OSM data
            if (presetId.HasValue)
            {
                var preset = await _db.MapAreaPresets
                    .FirstOrDefaultAsync(p =>
                        p.Id == presetId.Value &&
                        (
                            p.UserId == null ||
                            p.UserId == userId ||
                            IsAdmin
                        )
                    )
                ?? throw new Exception("Preset not found or access denied");
                actualPresetId = preset.Id;
            }
            else
            {
                if (string.IsNullOrEmpty(presetName) || !latitude.HasValue || !longitude.HasValue || !radiusKm.HasValue)
                    throw new Exception("Name, Latitude, Longitude, and RadiusKm are required for creating a new preset");

                var preset = await _presetService.FetchAndCreateMapPresetAsync(
                    presetName, latitude.Value, longitude.Value, radiusKm.Value, userId);
                actualPresetId = preset.Id;

            }

            // Load preset data for map area creation
            var presetData = await _db.MapAreaPresets
                .AsNoTracking()
                .FirstAsync(p => p.Id == actualPresetId);

            var mapArea = new MapArea
            {
                Name = $"{simulation.Name} - {presetData.Name}-{Guid.NewGuid()}",
                SimulationId = simulation.Id,
                CenterLat = presetData.Latitude,
                CenterLon = presetData.Longitude,
                RadiusKm = presetData.Radius,
                PresetRefID = presetData.Id
            };

            _db.MapAreas.Add(mapArea);

            await _db.SaveChangesAsync();

            // Set the navigation property for the simulation
            simulation.MapArea = mapArea;
            await _db.SaveChangesAsync();

            var buildingPresets = await _db.OSMBuildingPresets
                .AsNoTracking()
                .Where(b => b.MapAreaPresetId == actualPresetId)
                .Select(b => new { b.Name, b.Latitude, b.Longitude, b.Type, b.Capacity })
                .ToListAsync();


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
                building.Capacity = b.Capacity ?? 0;
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
                .Where(p => p.MapAreaPresetId == actualPresetId)
                .ToListAsync();

            var newPaths = pathPresets.Select(presetPath => new Models.Path
            {
                Type = Enum.TryParse<PathType>(presetPath.Type, true, out var parsed) ? parsed : PathType.Other,
                IsOneWay = presetPath.IsOneWay,
                SpeedModifier = presetPath.SpeedModifier,
                MapAreaId = mapArea.Id,
                Nodes = presetPath.Nodes.Select((n, index) => new PathNode
                {
                    OSMNodeId = n.OSMNodeId,
                    Lat = n.Latitude,
                    Lon = n.Longitude,
                    Sequence = index
                }).ToList()
            }).ToList();

            await _db.BulkInsertAsync(newPaths, opts => opts.IncludeGraph = true);
            _db.ChangeTracker.Clear();

            // Remove the preset map area from DB if it was just created and savePreset is false
            if (!presetId.HasValue && savePreset == false)
            {
                var presetToRemove = await _db.MapAreaPresets.FirstOrDefaultAsync(p => p.Id == actualPresetId);
                if (presetToRemove != null)
                {
                    _db.MapAreaPresets.Remove(presetToRemove);
                    await _db.SaveChangesAsync();
                }
            }

            var residentialCount = newBuildings.OfType<ResidentialBuilding>().Sum(b => b.Capacity);
            var educationCapacity = newBuildings.OfType<EducationBuilding>().Sum(b => b.MaxStudents);
            var officeCapacity = newBuildings.OfType<OfficeBuilding>().Sum(b => b.Capacity);
            var medicalCapacity = newBuildings.OfType<MedicalBuilding>().Sum(b => b.Capacity);
            var retailCapacity = newBuildings.OfType<RetailBuilding>().Sum(b => b.Capacity);
            var leisureCapacity = newBuildings.OfType<LeisureBuilding>().Sum(b => b.Capacity);

            // Calculate max population (housing is the hard limit)
            var maxPopulation = residentialCount;

            // Calculate constraints for each agent type
            var maxStudents = educationCapacity;
            var maxOfficeWorkers = officeCapacity;
            var maxMedicalWorkers = medicalCapacity;
            var maxRetailWorkers = retailCapacity;
            var maxServiceWorkers = leisureCapacity;
            // Elderly don't need workplace, so limited only by housing
            var maxElderly = maxPopulation;

            var population = new PopulationCapacityResult
            {
                MaxTotalPopulation = maxPopulation,
                MaxStudents = maxStudents,
                MaxOfficeWorkers = maxOfficeWorkers,
                MaxMedicalWorkers = maxMedicalWorkers,
                MaxRetailWorkers = maxRetailWorkers,
                MaxServiceWorkers = maxServiceWorkers,
                MaxElderlyRetired = maxElderly,
            };

            return population;
        }

        public class PopulationCapacityResult
        {
            public int MaxTotalPopulation { get; set; }
            public int MaxStudents { get; set; }
            public int MaxOfficeWorkers { get; set; }
            public int MaxMedicalWorkers { get; set; }
            public int MaxRetailWorkers { get; set; }
            public int MaxServiceWorkers { get; set; }
            public int MaxElderlyRetired { get; set; }
        }

        // Generate population
        public async Task SetupPopulation(int simulationId, SetupPopulationDto agentCounts, int userId)
        {
            using var _db = _dbFactory.CreateDbContext();
            // Fetch simulation from DB
            var simulation = await _db.Simulations
                .Include(s => s.MapArea)
                .FirstOrDefaultAsync(s => s.Id == simulationId && s.UserId == userId)
                ?? throw new Exception("Simulation not found or access denied");

            // Create the SimulationContext
            var context = await _simulationManager.GetOrCreateAsync(simulationId);

            // Generate agents based on the specified counts for each type
            var agents = new List<Agent>();

            for (int i = 0; i < agentCounts.StudentCount; i++)
            {
                var agent = new StudentAgent
                {
                    Name = GenerateRandomName(_rng),
                    SimulationId = simulation.Id
                };
                agents.Add(agent);
            }
            for (int i = 0; i < agentCounts.OfficeWorker; i++)
            {
                var agent = new OfficeWorkerAgent
                {
                    Name = GenerateRandomName(_rng),
                    SimulationId = simulation.Id
                };
                agents.Add(agent);
            }
            for (int i = 0; i < agentCounts.MedicalWorker; i++)
            {
                var agent = new MedicalWorkerAgent
                {
                    Name = GenerateRandomName(_rng),
                    SimulationId = simulation.Id
                };
                agents.Add(agent);
            }
            for (int i = 0; i < agentCounts.RetailWorker; i++)
            {
                var agent = new RetailWorkerAgent
                {
                    Name = GenerateRandomName(_rng),
                    SimulationId = simulation.Id
                };
                agents.Add(agent);
            }
            for (int i = 0; i < agentCounts.ElderlyRetiredAgent; i++)
            {
                var agent = new ElderlyRetiredAgent
                {
                    Name = GenerateRandomName(_rng),
                    SimulationId = simulation.Id
                };
                agents.Add(agent);
            }
            for (int i = 0; i < agentCounts.ServiceWorker; i++)
            {
                var agent = new ServiceWorkerAgent
                {
                    Name = GenerateRandomName(_rng),
                    SimulationId = simulation.Id
                };
                agents.Add(agent);
            }

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

            // Rehydrate the context so it has all loaded entities
            context.ReloadEntities();

            // Assign housing & workplaces deterministically
            Console.Write($"[Assignment] Assigning housing for {agents.Count} agents... ");
            var assignStart = DateTime.Now;
            context.BuildingService.AssignHousing();
            var assignTime = (DateTime.Now - assignStart).TotalSeconds;
            Console.WriteLine($"✓ Done in {assignTime:F1}s");

            Console.Write($"[Assignment] Assigning workplaces... ");
            assignStart = DateTime.Now;
            context.BuildingService.AssignWorkplaces();
            assignTime = (DateTime.Now - assignStart).TotalSeconds;
            Console.WriteLine($"✓ Done in {assignTime:F1}s");

            agents = context.Agents; // reload agents with assigned buildings and Ids
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

            // Generate routines for all agents WITH PARALLEL PROCESSING
            Console.WriteLine($"[Routine Generation] Starting for {agents.Count} agents...");
            var progressLock = new object();
            var completed = 0;
            var errors = 0;
            var startTime = DateTime.Now;
            var lastProgressUpdate = DateTime.Now;

            // Use Parallel.ForEach for multi-threaded processing
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(agents, parallelOptions, agent =>
            {
                try
                {
                    // Generate routine events
                    agent.RoutineEvents = context.AgentService.GenerateAgentRoutine(agent);
                    foreach (var routineEvent in agent.RoutineEvents)
                    {
                        routineEvent.AgentId = agent.Id;
                        routineEvent.BuildingId = routineEvent.Building.Id;
                    }

                    // Update progress (thread-safe)
                    lock (progressLock)
                    {
                        completed++;
                        var now = DateTime.Now;
                        // Update progress bar every 0.5 seconds or every 100 agents
                        if ((now - lastProgressUpdate).TotalMilliseconds >= 500 || completed % 100 == 0)
                        {
                            lastProgressUpdate = now;
                            var percent = (completed * 100.0) / agents.Count;
                            var elapsed = (now - startTime).TotalSeconds;
                            var rate = completed / elapsed;
                            var eta = (agents.Count - completed) / rate;

                            // Create progress bar
                            var barWidth = 40;
                            var filledWidth = (int)(barWidth * completed / agents.Count);
                            var bar = new string('█', filledWidth) + new string('░', barWidth - filledWidth);

                            Console.Write($"\r[{bar}] {percent:F1}% ({completed}/{agents.Count}) | {rate:F0} agents/s | ETA: {eta:F0}s ");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (progressLock)
                    {
                        errors++;
                        if (errors <= 5) // Only show first 5 errors to avoid spam
                        {
                            Console.WriteLine($"\n[ERROR] Agent {agent.Id}: {ex.Message}");
                        }
                    }
                }
            });

            // Final progress update
            var totalTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"\n[Routine Generation] ✓ Completed in {totalTime:F1}s ({agents.Count / totalTime:F0} agents/s)");
            if (errors > 0)
            {
                Console.WriteLine($"[Routine Generation] ⚠ {errors} errors occurred");
            }

            // Save all routine events to the database
            Console.WriteLine($"[Database] Preparing {agents.Count} agents' routine events...");
            var routineEvents = agents.SelectMany(a => a.RoutineEvents).ToList();
            Console.WriteLine($"[Database] Total routine events: {routineEvents.Count}");

            // Debug: Check first event's path
            if (routineEvents.Count > 0 && routineEvents[0].CachedPath != null)
            {
                var firstPath = routineEvents[0].CachedPath;
                if (firstPath != null)
                {
                    Console.WriteLine($"[DEBUG] First routine event path has {firstPath.Count} nodes");
                    if (firstPath.Count > 0)
                    {
                        Console.WriteLine($"[DEBUG] First node: ({firstPath[0].Lat}, {firstPath[0].Lon})");
                    }
                }
            }

            // Set up bulk config for routine events
            var routineEventConfig = new BulkConfig
            {
                PreserveInsertOrder = false, // Don't need order for routine events
                SetOutputIdentity = false, // Don't need IDs back, saves time
                BatchSize = 5000, // Insert in batches to avoid timeouts
                BulkCopyTimeout = 300 // 5 minutes timeout
            };

            // Bulk insert routine events in batches with progress tracking
            Console.WriteLine($"[Database] Inserting {routineEvents.Count} routine events in batches...");
            var dbStart = DateTime.Now;
            var batchSize = 50000; // Process 50k events at a time
            var batches = (int)Math.Ceiling(routineEvents.Count / (double)batchSize);

            for (int i = 0; i < batches; i++)
            {
                var batch = routineEvents.Skip(i * batchSize).Take(batchSize).ToList();
                await _db.BulkInsertAsync(batch, routineEventConfig);

                var batchPercent = ((i + 1) * 100.0) / batches;
                var inserted = Math.Min((i + 1) * batchSize, routineEvents.Count);
                Console.Write($"\r[Database] Progress: {batchPercent:F0}% ({inserted:N0}/{routineEvents.Count:N0}) ");
            }

            var dbTime = (DateTime.Now - dbStart).TotalSeconds;
            Console.WriteLine($"\n[Database] ✓ Done in {dbTime:F1}s");

            // generate and insert medical profiles for each agent
            Console.Write($"[Database] Generating {agents.Count} medical profiles... ");
            var medicalProfiles = new List<MedicalProfile>(agents.Count);
            foreach (var agent in agents)
            {
                var profile = context.AgentService.GenerateMedicalProfile(agent);
                profile.AgentId = agent.Id; // Set FK explicitly
                medicalProfiles.Add(profile);
            }
            Console.WriteLine("✓ Done");

            // Bulk insert medical profiles
            Console.Write($"[Database] Inserting {medicalProfiles.Count} medical profiles... ");
            var profileBulkConfig = new BulkConfig
            {
                PreserveInsertOrder = false,
                BatchSize = 5000,
                BulkCopyTimeout = 120 // 2 minutes timeout
            };
            try
            {
                dbStart = DateTime.Now;
                await _db.BulkInsertAsync(medicalProfiles, profileBulkConfig);
                dbTime = (DateTime.Now - dbStart).TotalSeconds;
                Console.WriteLine($"✓ Done in {dbTime:F1}s");
            }
            catch
            {
                throw;
            }
            Console.WriteLine($"[Cleanup] Unloading simulation {simulationId} from memory...");
            await _simulationManager.RemoveAsync(simulationId);
            Console.WriteLine($"✓ Setup complete!");
        }


        // Setup disease
        public async Task SetupDisease(int simulationId, int? presetId, string? name, double? infectionRate, double? recoveryRate, double? mortalityRate, bool savePreset, int userId, bool IsAdmin)
        {
            using var _db = _dbFactory.CreateDbContext();
            var simulation = await _db.Simulations
                .FirstOrDefaultAsync(s => s.Id == simulationId && s.UserId == userId)
                ?? throw new Exception("Simulation not found or access denied");

            if (presetId.HasValue)
            {
                var preset = await _db.DiseasePresets
                    .FirstOrDefaultAsync(p =>
                        p.Id == presetId.Value &&
                        (
                            p.UserId == null ||
                            p.UserId == userId ||
                            IsAdmin
                        )
                    )
                    ?? throw new Exception("Preset not found or access denied");

                var disease = new Disease
                {
                    Name = preset.Name,
                    SimulationId = simulation.Id,
                    InfectionRate = preset.InfectionRate,
                    RecoveryRate = preset.RecoveryRate,
                    MortalityRate = preset.MortalityRate,
                };
                _db.Diseases.Add(disease);
                await _db.SaveChangesAsync();

            }
            else
            {
                if (string.IsNullOrEmpty(name) || !infectionRate.HasValue || !recoveryRate.HasValue || !mortalityRate.HasValue)
                    throw new Exception("Name, InfectionRate, RecoveryRate, and MortalityRate are required for creating a new disease preset");

                var disease = new Disease
                {
                    Name = name,
                    SimulationId = simulation.Id,
                    InfectionRate = infectionRate.Value,
                    RecoveryRate = recoveryRate.Value,
                    MortalityRate = mortalityRate.Value,
                };
                _db.Diseases.Add(disease);

                if (savePreset)
                {
                    var preset = new DiseasePreset
                    {
                        Name = name,
                        InfectionRate = infectionRate.Value,
                        RecoveryRate = recoveryRate.Value,
                        MortalityRate = mortalityRate.Value,
                        // The preset is scoped to the user by default
                        UserId = userId
                    };
                    _db.DiseasePresets.Add(preset);
                }
                await _db.SaveChangesAsync();

            }

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

            agent.Name = GenerateRandomName(_rng);
            agent.SimulationId = simId;
            return agent;


        }
        public string GenerateRandomName(Random _rng)
        {
            string[] first = { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda" };
            string[] last = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
            return $"{first[_rng.Next(first.Length)]} {last[_rng.Next(last.Length)]}";
        }



    }
}