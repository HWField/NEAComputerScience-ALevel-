using System;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Hubs;
using MySimulationAPI.Models;
using NetTopologySuite.Operation.Overlay.Snap;
using SimulationRuntime;

namespace MySimulationAPI.SimulationConfiguration
{
    public class MetricManager
    {
        private readonly SimulationContext _context;
        private readonly IHubContext<SimulationHub>? _hubContext;

        public MetricManager(SimulationContext context, IHubContext<SimulationHub>? hubContext = null)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task RecordAsync()
        {
            var agents = _context.AgentStates;
            var total = agents.Length;
            var infected = agents.Count(a => a.HealthState == (byte)AgentHealthState.Infected);
            var recovered = agents.Count(a => a.HealthState == (byte)AgentHealthState.Recovered);
            var dead = agents.Count(a => a.HealthState == (byte)AgentHealthState.Dead);
            var quarantined = agents.Count(a => a.IsQuarantined);

            using var db = _context.DbFactory.CreateDbContext();

            // ensure only one metric entry per tick
            var existing = await db.SimulationMetrics
                .FirstOrDefaultAsync(m => m.SimulationId == _context.Simulation.Id && m.Tick == _context.CurrentTick);

            if (existing != null)
            {
                existing.TotalAgents = total;
                existing.Infected = infected;
                existing.Recovered = recovered;
                existing.Dead = dead;
                existing.Quarantined = quarantined;
            }
            else
            {
                await db.AddAsync(new SimulationMetric
                {
                    SimulationId = _context.Simulation.Id,
                    Tick = _context.CurrentTick,
                    TotalAgents = total,
                    Infected = infected,
                    Recovered = recovered,
                    Dead = dead,
                    Quarantined = quarantined
                });
            }
            await db.SaveChangesAsync();

            // Broadcast metrics 
            if (_hubContext != null)
            {
                await SimulationHub.BroadcastMetrics(_hubContext, _context.Simulation.Id, new
                {
                    Tick = _context.CurrentTick,
                    TotalAgents = total,
                    Infected = infected,
                    Recovered = recovered,
                    Dead = dead,
                    Quarantined = quarantined,
                    Healthy = total - infected - recovered - dead
                });
            }
        }
    }
}