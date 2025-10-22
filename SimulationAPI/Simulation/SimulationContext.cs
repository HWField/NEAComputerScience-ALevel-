using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using MySimulationAPI.SimulationRuntime;
using SimulationRuntime;

namespace MySimulationAPI.SimulationConfiguration
{
    public class SimulationContext : IDisposable
    {
        public Simulation Simulation { get; }
        public SimDbContext Db { get; }
        public Random Rng { get; set; }

        // Runtime properties
        public int TickLength { get; set; } = 300; // Default 5minute tick
        public double SimulationSpeed { get; set; } = 1.0; // multiplier, 1.0 = 1 tick per real second (greater than 1 is faster)
        public bool IsPaused { get; set; } = false;
        public long CurrentTick { get; set; } = 0; // runtime tick, updated via snapshot

        // In-memory object lists (Prevents multiple db calls)
        public List<Agent> Agents { get; set; }
        public List<Building> Buildings { get; set; }
        public List<Models.Path> Paths { get; set; }

        // In-memory object structs for runtime use (efficent compared to iterating over classes)
        public AgentState[] AgentStates { get; private set; } = Array.Empty<AgentState>();
        public BuildingState[] BuildingStates { get; private set; } = Array.Empty<BuildingState>();
        public RoutineEventState[] RoutineStates { get; private set; } = Array.Empty<RoutineEventState>();

        // Runtime services (one created per simulation context so that caches etc can be related)
        public BuildingService BuildingService { get; }
        public RoutingService RoutingService { get; }
        public SnapshotManager Snapshots { get; }
        public AgentService AgentService { get; }
        public InfectionService InfectionService { get; }
        public SimulationEngine Engine { get; }

        public SimulationContext(Simulation simulation, SimDbContext db)
        {
            Simulation = simulation;
            Db = db;
            Rng = new Random(simulation.Id); // deterministic seed
            TickLength = Simulation.TickLength;
            SimulationSpeed = 1.0;
            IsPaused = false;

            // Determine current tick (latest snapshot)
            CurrentTick = db.SimulationSnapshots
                .Where(s => s.SimulationId == simulation.Id)
                .OrderByDescending(s => s.Tick)
                .Select(s => (long?)s.Tick)
                .FirstOrDefault() ?? 0;

            // Load relevant db objects in one go
            Buildings = db.Buildings
                .Where(b => b.MapAreaId == simulation.MapArea.Id)
                .AsNoTracking()
                .ToList();

            Agents = db.Agents
                .Where(a => a.SimulationId == simulation.Id)
                .Include(a => a.RoutineEvents)
                .AsNoTracking()
                .ToList();

            Paths = db.Paths
                .Where(p => p.MapAreaId == simulation.MapArea.Id)
                .Include(p => p.Nodes)
                .AsNoTracking()
                .ToList();

            // Instantiate services using in-memory data
            RoutingService = new RoutingService(this);
            BuildingService = new BuildingService(this);
            Snapshots = new SnapshotManager(this);
            AgentService = new AgentService(this);
            Engine = new SimulationEngine(this);
            InfectionService = new InfectionService(this);
        }
        public void ReloadEntities()
        {
            Buildings = Db.Buildings
                .Where(b => b.MapAreaId == Simulation.MapArea.Id)
                .AsNoTracking()
                .ToList();

            Agents = Db.Agents
                .Where(a => a.SimulationId == Simulation.Id)
                .Include(a => a.RoutineEvents)
                .AsNoTracking()
                .ToList();

            Paths = Db.Paths
                .Where(p => p.MapAreaId == Simulation.MapArea.Id)
                .Include(p => p.Nodes)
                .AsNoTracking()
                .ToList();
        }

        public async Task SaveAsync()
        {
            await Db.SaveChangesAsync();
        }

        public void GenerateStates()
        // Generates a base state for agents and buildings, Done at a later stage to allow for modifications before starting the simulation
        {
            // --- Buildings ---
            BuildingStates = new BuildingState[Buildings.Count];
            for (int i = 0; i < Buildings.Count; i++)
            {
                var b = Buildings[i];
                BuildingStates[i] = new BuildingState
                {
                    Id = b.Id,
                    X = (float)b.Latitude,
                    Y = (float)b.Longitude,
                    Capacity = (short)b.Capacity,
                    Occupants = 0,
                    BuildingType = BuildingTypeRegistry.GetTypeCode(b.GetType())
                };
            }

            // --- Agents ---
            AgentStates = new AgentState[Agents.Count];

            var routineList = new List<RoutineEventState>();
            for (int i = 0; i < Agents.Count; i++)
            {
                var a = Agents[i];
                AgentStates[i] = new AgentState
                {
                    Id = a.Id,
                    AgentType = AgentTypeRegistry.GetTypeCode(a.GetType()),
                    Lat = (float)(a.House?.Latitude ?? 0),
                    Lon = (float)(a.House?.Longitude ?? 0),
                    Immunity = (float)a.MedicalProfile.Immunity,
                    Susceptibility = (float)a.MedicalProfile.InfectionSusceptibility,
                    HasChronicCondition = a.MedicalProfile.HasChronicCondition,
                    HealthState = (byte)AgentHealthState.Healthy,
                    CurrentBuildingId = a.HouseId ?? 0
                };
                // --- Routines ---
                foreach (var ev in a.RoutineEvents)
                {
                    routineList.Add(new RoutineEventState
                    {
                        AgentId = a.Id,
                        Day = ev.DayIndex,
                        StartTick = Utils.SimulationTimeHelper.ConvertTimeToTick(ev.StartTime, TickLength),
                        DurationTicks = Utils.SimulationTimeHelper.ConvertTimeSpanToTicks(ev.Duration, TickLength),
                        BuildingId = ev.BuildingId,
                        Type = (byte)ev.EventType,
                        CachedTravelDuration = ev.CachedTravelDuration / TickLength,
                        Path = ev.CachedPath?.Select(p => ((float)p.Lat, (float)p.Lon)).ToArray()
                    });
                }
            }
            RoutineStates = routineList.ToArray();
        }

        public void Dispose()
        {
            Agents = null!;
            Buildings = null!;
            Paths = null!;

            // Clear struct arrays
            AgentStates = Array.Empty<AgentState>();
            BuildingStates = Array.Empty<BuildingState>();
            RoutineStates = Array.Empty<RoutineEventState>();
        }
    }
}