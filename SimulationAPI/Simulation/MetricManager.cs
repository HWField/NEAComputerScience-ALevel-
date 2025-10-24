using System;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using NetTopologySuite.Operation.Overlay.Snap;
using SimulationRuntime;

namespace MySimulationAPI.SimulationConfiguration
{
    public class MetricManager
    {
        private readonly SimulationContext _context;

        public MetricManager(SimulationContext context)
        {
            _context = context;
        }

        public async Task RecordAsync()
        {
            var agents = _context.AgentStates;
            var total = agents.Length;
            var infected = agents.Count(a => a.HealthState == (byte)AgentHealthState.Infected);
            var recovered = agents.Count(a => a.HealthState == (byte)AgentHealthState.Recovered);
            var dead = agents.Count(a => a.HealthState == (byte)AgentHealthState.Dead);
            var quarantined = agents.Count(a => a.IsQuarantined);

            var metric = new SimulationMetric
            {
                SimulationId = _context.Simulation.Id,
                Tick = _context.CurrentTick,
                TotalAgents = total,
                Infected = infected,
                Recovered = recovered,
                Dead = dead,
                Quarantined = quarantined
            };

            using var db = _context.DbFactory.CreateDbContext();
            await db.AddAsync(metric);
            await db.SaveChangesAsync();
        }
    }
}