using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;

namespace MySimulationAPI.SimulationConfiguration
{
    public class SimulationContext
    {
        public Simulation Simulation { get; }
        public SimDbContext Db { get; }
        public Random Rng { get; set; }
        public long CurrentTick { get; set; } = 0; // runtime tick, updated via snapshot


        // In-memory object lists (Prevents multiple db calls)
        public List<Agent> Agents { get; set; }
        public List<Building> Buildings { get; set; }
        public List<Models.Path> Paths { get; set; }

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

            // Load relevant objects in one go
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
            RoutingService = new RoutingService(simulation);
            BuildingService = new BuildingService(this);
            Snapshots = new SnapshotManager(this);
            AgentService = new AgentService(this);
            Engine = new SimulationEngine(this);
            InfectionService = new InfectionService(this);
        }
        public void ReloadEntities()
        {
            Agents = Db.Agents.Where(a => a.SimulationId == Simulation.Id)
                              .Include(a => a.RoutineEvents)
                              .AsNoTracking()
                              .ToList();


            Buildings = Db.Buildings.Where(b => b.MapAreaId == Simulation.MapArea.Id).AsNoTracking().ToList();
        }

        public async Task SaveAsync()
        {
            await Db.SaveChangesAsync();
        }
    }
}