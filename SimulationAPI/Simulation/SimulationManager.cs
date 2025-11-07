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

        public async Task<SimulationContext> GetOrCreateAsync(int simulationId)
        {
            if (_contexts.TryGetValue(simulationId, out var existing))
            {
                return existing;
            }

            using var db = _dbFactory.CreateDbContext();

            var sim = await db.Simulations
                .Include(s => s.MapArea)
                .Include(s => s.Agents)
                .Include(s => s.Disease)
                .FirstAsync(s => s.Id == simulationId);

            // Create in-memory context
            var context = new SimulationContext(sim, _dbFactory, _hubContext);
            _contexts[simulationId] = context;

            Console.WriteLine($"Broadcasting state: Status={context.Status}");
            await SimulationHub.BroadcastState(_hubContext, simulationId, context);

            return context;
        }

        public async Task StartAsync(int simulationId, long? targetTick = null)
        {
            if (!_contexts.TryGetValue(simulationId, out var context))
            {
                throw new InvalidOperationException($"Simulation {simulationId} is not loaded in memory");
            }

            context.Engine.Start(targetTick);
            context.Status = "Running";

            Console.WriteLine($"Broadcasting state: Status={context.Status}");
            await SimulationHub.BroadcastState(_hubContext, simulationId, context);
        }

        public async Task PauseAsync(int simulationId)
        {
            if (!_contexts.TryGetValue(simulationId, out var context))
            {
                throw new InvalidOperationException($"Simulation {simulationId} is not loaded in memory");
            }

            context.Engine.Stop();
            context.Status = "Paused";
            await SimulationHub.BroadcastState(_hubContext, simulationId, context);
        }

        public async Task SetSpeedAsync(int simulationId, int speedMultiplier)
        {
            if (!_contexts.TryGetValue(simulationId, out var context))
            {
                throw new InvalidOperationException($"Simulation {simulationId} is not loaded in memory");
            }

            context.SimulationSpeed = speedMultiplier;
            await SimulationHub.BroadcastState(_hubContext, simulationId, context);
        }

        public async Task SetTickAsync(int simulationId, int targetTick)
        {
            if (!_contexts.TryGetValue(simulationId, out var context))
            {
                throw new InvalidOperationException($"Simulation {simulationId} is not loaded in memory");
            }
            await context.SnapshotManager.LoadTargetTick(targetTick);

            await SimulationHub.BroadcastState(_hubContext, simulationId, context);
        }

        public bool TryGet(int simulationId, out SimulationContext context)
            => _contexts.TryGetValue(simulationId, out context);

        public async Task RemoveAsync(int simulationId)
        {
            if (_contexts.TryRemove(simulationId, out var ctx))
            {

                ctx.Engine.Stop();
                // Dispose the context to release memory and unmanaged resources
                ctx.Dispose();
            }
        }

        public IEnumerable<SimulationContext> GetAllLoaded()
        {
            return _contexts.Values;
        }

        public async Task RemoveAllAsync()
        {
            var keys = _contexts.Keys.ToList();
            foreach (var key in keys)
            {
                await RemoveAsync(key);
            }
        }
    }
}