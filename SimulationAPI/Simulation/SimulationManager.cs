using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Hubs;
using MySimulationAPI.Models;

namespace MySimulationAPI.SimulationConfiguration
{
    public class SimulationManager
    {
        private readonly ConcurrentDictionary<int, SimulationContext> _contexts = new();
        private readonly IDbContextFactory<SimDbContext> _dbFactory;
        private readonly IHubContext<SimulationHub> _hubContext;

        public SimulationManager(IDbContextFactory<SimDbContext> dbFactory, IHubContext<SimulationHub> hubContext)
        {
            _dbFactory = dbFactory;
            _hubContext = hubContext;
        }

        public SimulationContext GetOrCreate(int simulationId)
        {
            return _contexts.GetOrAdd(simulationId, id =>
            {
                using var db = _dbFactory.CreateDbContext();

                var sim = db.Simulations
                    .Include(s => s.MapArea)
                    .Include(s => s.Agents)
                    .Include(s => s.Disease)
                    .First(s => s.Id == id);

                var context = new SimulationContext(sim, _dbFactory, _hubContext);
                sim.Status = "Loaded";
                db.SaveChanges();

                return context;
            });
        }

        public bool TryGet(int simulationId, out SimulationContext context)
            => _contexts.TryGetValue(simulationId, out context);

        public void Remove(int simulationId)
        {
            if (_contexts.TryRemove(simulationId, out var ctx))
            {

                ctx.Engine.Stop();

                using var db = _dbFactory.CreateDbContext();
                var sim = db.Simulations.Find(simulationId);
                if (sim != null)
                {
                    sim.Status = "Unloaded";
                    db.SaveChanges();
                }

                // Dispose the context to release memory and unmanaged resources
                ctx.Dispose();
            }
        }

        public IEnumerable<SimulationContext> GetAllLoaded()
        {
            return _contexts.Values;
        }

        public void RemoveAll()
        {
            var keys = _contexts.Keys.ToList();
            foreach (var key in keys)
            {
                Remove(key);
            }
        }
    }
}