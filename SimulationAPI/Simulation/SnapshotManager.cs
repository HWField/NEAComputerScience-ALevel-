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
    public class SnapshotManager
    {
        private readonly SimulationContext _context;
        public SnapshotManager(SimulationContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------
        // Save snapshot to DB
        // ------------------------------------------------------------
        public async Task SaveSnapshotAsync()
        {
            using var _db = _context.DbFactory.CreateDbContext();

            // --- Delete existing snapshot (if any) ---
            var existing = await _db.SimulationSnapshots
                .FirstOrDefaultAsync(s => s.SimulationId == _context.Simulation.Id && s.Tick == _context.CurrentTick);
            if (existing != null)
            {
                _db.SimulationSnapshots.Remove(existing);
                await _db.SaveChangesAsync(); // cascades to AgentSnapshots + BuildingSnapshots
            }

            var snapshot = new SimulationSnapshot
            {
                SimulationId = _context.Simulation.Id,
                Tick = _context.CurrentTick,
                CreatedAt = DateTime.UtcNow
            };

            await _db.SimulationSnapshots.AddAsync(snapshot);
            await _db.SaveChangesAsync();
            // --- AGENTS ---
            var agents = _context.AgentStates;
            var agentSnaps = agents.Select(a => new AgentSnapshot
            {
                SimulationSnapshotId = snapshot.Id,
                AgentId = a.Id,
                Latitude = a.Lat,
                Longitude = a.Lon,
                InfectionProgress = a.InfectionProgress,
                IsQuarantined = a.IsQuarantined,
                CurrentBuildingId = a.CurrentBuildingId > 0 ? a.CurrentBuildingId : null,
                HealthState = ConvertHealthStateToString((AgentHealthState)a.HealthState)
            }).ToList();

            // --- BUILDINGS ---
            var buildings = _context.BuildingStates;
            var buildingSnaps = buildings.Select(b => new BuildingSnapshot
            {
                SimulationSnapshotId = snapshot.Id,
                BuildingId = b.Id,
                CurrentOccupancy = b.Occupants
            }).ToList();

            // --- SAVE TO DATABASE ---
            await _db.BulkInsertAsync(agentSnaps, new BulkConfig { BatchSize = 5000 });
            await _db.BulkInsertAsync(buildingSnaps, new BulkConfig { BatchSize = 5000 });
        }

        /// <summary>
        /// Converts AgentHealthState enum to a human-readable string for DB.
        /// </summary>
        private static string ConvertHealthStateToString(AgentHealthState state)
        {
            return state switch
            {
                AgentHealthState.Healthy => "Healthy",
                AgentHealthState.Infected => "Infected",
                AgentHealthState.Recovered => "Recovered",
                AgentHealthState.Dead => "Dead",
                _ => "Healthy"
            };
        }

        // ------------------------------------------------------------
        // Load snapshot from DB (nearest <= targetTick)
        // ------------------------------------------------------------
        public async Task<bool> LoadTargetTick(long targetTick)
        {
            using var _db = _context.DbFactory.CreateDbContext();
            _context.Engine.Stop();
            var snapshot = await _db.SimulationSnapshots
                .Include(s => s.Agents)
                .Include(s => s.Buildings)
                .Where(s => s.SimulationId == _context.Simulation.Id && s.Tick <= targetTick)
                .OrderByDescending(s => s.Tick)
                .FirstOrDefaultAsync();

            if (snapshot == null)
            {
                Console.WriteLine($"[LoadTargetTick] No snapshot found for tick <= {targetTick}");
                return false;
            }

            RestoreSnapshot(snapshot);
            _context.Engine.Start(targetTick); //compute forward to target tick
            return true;
        }

        // ------------------------------------------------------------
        // Restore state into context
        // ------------------------------------------------------------
        private void RestoreSnapshot(SimulationSnapshot snapshot)
        {
            // --- Set simulation tick ---
            _context.CurrentTick = snapshot.Tick;

            // --- Build fast lookup dictionaries for snapshot data ---
            var agentSnaps = snapshot.Agents;
            var buildingSnaps = snapshot.Buildings;

            var agentSnapLookup = new Dictionary<int, AgentSnapshot>(agentSnaps.Count);
            foreach (var s in agentSnaps)
                agentSnapLookup[s.AgentId] = s;

            var buildingSnapLookup = new Dictionary<int, BuildingSnapshot>(buildingSnaps.Count);
            foreach (var s in buildingSnaps)
                buildingSnapLookup[s.BuildingId] = s;

            // --- Restore AgentStates ---
            var agents = _context.AgentStates;
            for (int i = 0; i < agents.Length; i++)
            {
                ref var a = ref agents[i];

                if (!agentSnapLookup.TryGetValue(a.Id, out var snap))
                    continue;

                a.Lat = (float)snap.Latitude;
                a.Lon = (float)snap.Longitude;
                a.InfectionProgress = (float)snap.InfectionProgress;
                a.IsQuarantined = snap.IsQuarantined;
                a.CurrentBuildingId = snap.CurrentBuildingId ?? 0;

                // Parse health string → byte enum
                a.HealthState = (byte)ParseHealthState(snap.HealthState);
            }

            // --- Restore BuildingStates ---
            var buildings = _context.BuildingStates;
            for (int i = 0; i < buildings.Length; i++)
            {
                ref var b = ref buildings[i];
                if (buildingSnapLookup.TryGetValue(b.Id, out var snap))
                {
                    b.Occupants = (short)snap.CurrentOccupancy;
                }
            }

            // --- Reset RNG to deterministic state for this tick ---
            _context.Rng = new Random(_context.Simulation.Id + (int)snapshot.Tick);
        }

        /// <summary>
        /// Converts string-based DB health state to AgentHealthState enum
        /// </summary>
        private static AgentHealthState ParseHealthState(string state)
        {
            return state.ToLowerInvariant() switch
            {
                "healthy" => AgentHealthState.Healthy,
                "infected" => AgentHealthState.Infected,
                "recovered" => AgentHealthState.Recovered,
                "dead" => AgentHealthState.Dead,
                _ => AgentHealthState.Healthy
            };
        }


    }
}