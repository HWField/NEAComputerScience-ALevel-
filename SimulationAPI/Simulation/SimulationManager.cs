using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;

namespace MySimulationAPI.SimulationConfiguration
{
    public class SimulationManager
    {
        private readonly ConcurrentDictionary<int, SimulationContext> _contexts = new();
        private readonly SimDbContext _db;
        public SimulationManager(SimDbContext db)
        {
            _db = db;
        }

        public SimulationContext GetOrCreate(int simulationId)
        {
            return _contexts.GetOrAdd(simulationId, id =>
            {
                var sim = _db.Simulations
                    .Include(s => s.MapArea)
                    .Include(s => s.Agents)
                    .Include(s => s.Disease)
                    .First(s => s.Id == id);

                return new SimulationContext(sim, _db);
            });
        }

        public bool TryGet(int simulationId, out SimulationContext context)
            => _contexts.TryGetValue(simulationId, out context);

        public void Remove(int simulationId)
        {
            if (_contexts.TryRemove(simulationId, out var ctx))
            {
                // Stop the simulation engine
                ctx.Engine.Stop();

                // Dispose the context to release memory and unmanaged resources
                ctx.Dispose();
            }
        }
    }
}