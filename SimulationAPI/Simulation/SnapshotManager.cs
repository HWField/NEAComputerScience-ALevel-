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

        // Save snapshot to DB
        public async Task SaveSnapshotAsync()
        {
            using var _db = _context.DbFactory.CreateDbContext();

            // Configure longer timeout for large datasets
            _db.Database.SetCommandTimeout(300); // 5 minutes

            Console.WriteLine($"[SaveSnapshot] Starting snapshot save for tick {_context.CurrentTick}...");

            // Delete existing snapshot (if any)
            var existing = await _db.SimulationSnapshots
                .FirstOrDefaultAsync(s => s.SimulationId == _context.Simulation.Id && s.Tick == _context.CurrentTick);
            if (existing != null)
            {
                Console.WriteLine($"[SaveSnapshot] Deleting existing snapshot at tick {_context.CurrentTick}...");
                _db.SimulationSnapshots.Remove(existing);
                await _db.SaveChangesAsync(); // cascades to AgentSnapshots + BuildingSnapshots
            }

            var snapshot = new SimulationSnapshot
            {
                SimulationId = _context.Simulation.Id,
                Tick = _context.CurrentTick,
                CreatedAt = DateTime.UtcNow,
                TotalInfections = _context.InfectionService.TotalInfections,
                TotalDeaths = _context.InfectionService.TotalDeaths,
                TotalRecoveries = _context.InfectionService.TotalRecoveries
            };

            await _db.SimulationSnapshots.AddAsync(snapshot);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[SaveSnapshot] Created snapshot {snapshot.Id}");

            // AGENTS
            var agents = _context.AgentStates;
            Console.Write($"[SaveSnapshot] Preparing {agents.Length} agent snapshots... ");
            var agentSnaps = agents.Select(a => new AgentSnapshot
            {
                SimulationSnapshotId = snapshot.Id,
                AgentId = a.Id,
                Latitude = a.Lat,
                Longitude = a.Lon,
                InfectionProgress = a.InfectionProgress,
                isIsolating = a.IsIsolating,
                CurrentBuildingId = a.CurrentBuildingId > 0 ? a.CurrentBuildingId : null,
                HealthState = ConvertHealthStateToString((AgentHealthState)a.HealthState),
                SymptomSeverity = a.SymptomSeverity,
                Immunity = a.Immunity,
                WearsMask = false,
                IsVaccinated = false,
                InfectionCount = a.InfectionCount
            }).ToList();


            // BUILDINGS
            var buildings = _context.BuildingStates;
            Console.Write($"[SaveSnapshot] Preparing {buildings.Length} building snapshots... ");
            var buildingSnaps = buildings.Select(b => new BuildingSnapshot
            {
                SimulationSnapshotId = snapshot.Id,
                BuildingId = b.Id,
                CurrentOccupancy = b.Occupants
            }).ToList();

            // SAVE TO DATABASE
            Console.Write($"[SaveSnapshot] Saving {agentSnaps.Count} agent snapshots... ");
            var startTime = DateTime.Now;
            await _db.BulkInsertAsync(agentSnaps, new BulkConfig
            {
                BatchSize = 5000,
                BulkCopyTimeout = 300
            });
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Done in {elapsed:F1}s");

            Console.Write($"[SaveSnapshot] Saving {buildingSnaps.Count} building snapshots... ");
            startTime = DateTime.Now;
            await _db.BulkInsertAsync(buildingSnaps, new BulkConfig
            {
                BatchSize = 5000,
                BulkCopyTimeout = 120
            });
            elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Done in {elapsed:F1}s");

            _context.MaxComputedTick = Math.Max(_context.MaxComputedTick, _context.CurrentTick);
            Console.WriteLine($"[SaveSnapshot] Snapshot save complete!");
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


        // Load snapshot from DB (nearest <= targetTick) and compute forward
        public async Task<bool> LoadTargetTick(long targetTick)
        {
            _context.Engine.Stop();

            // Find the snapshot closest to but not exceeding targetTick
            var snapshot = await FindSnapshotAsync(targetTick);
            if (snapshot == null)
            {
                Console.WriteLine($"[LoadTargetTick] No snapshot found for tick <= {targetTick}");
                return false;
            }

            Console.WriteLine($"[LoadTargetTick] Found snapshot at tick {snapshot.Tick}");

            // Load snapshot data
            await LoadSnapshotDataAsync(snapshot);

            // Restore the snapshot state
            Console.WriteLine($"[LoadTargetTick] Restoring snapshot state...");
            RestoreSnapshot(snapshot);

            // If the snapshot tick is less than target, compute forward
            if (snapshot.Tick < targetTick)
            {
                Console.WriteLine($"[LoadTargetTick] Computing forward from tick {snapshot.Tick} to {targetTick}...");
                _context.Engine.Start(targetTick);
            }
            else
            {
                Console.WriteLine($"[LoadTargetTick] Snapshot is at target tick {targetTick}");
            }

            return true;
        }

        public async Task<bool> LoadLatestSnapshotAsync()
        {
            _context.Engine.Stop();

            // Find the latest snapshot (highest tick number)
            var snapshot = await FindLatestSnapshotAsync();
            if (snapshot == null)
            {
                Console.WriteLine($"[LoadLatestSnapshot] No snapshots found");
                return false;
            }

            Console.WriteLine($"[LoadLatestSnapshot] Found latest snapshot at tick {snapshot.Tick}");

            // Load snapshot data
            await LoadSnapshotDataAsync(snapshot);

            // Restore the snapshot state
            Console.WriteLine($"[LoadLatestSnapshot] Restoring snapshot state...");
            RestoreSnapshot(snapshot);

            return true;
        }


        // Find snapshot at or before targetTick
        private async Task<SimulationSnapshot?> FindSnapshotAsync(long targetTick)
        {
            using var _db = _context.DbFactory.CreateDbContext();
            _db.Database.SetCommandTimeout(120);

            return await _db.SimulationSnapshots
                .Where(s => s.SimulationId == _context.Simulation.Id && s.Tick <= targetTick)
                .OrderByDescending(s => s.Tick)
                .FirstOrDefaultAsync();
        }

        private async Task<SimulationSnapshot?> FindLatestSnapshotAsync()
        {
            using var _db = _context.DbFactory.CreateDbContext();
            _db.Database.SetCommandTimeout(120);

            return await _db.SimulationSnapshots
                .Where(s => s.SimulationId == _context.Simulation.Id)
                .OrderByDescending(s => s.Tick)
                .FirstOrDefaultAsync();
        }


        // Load agent and building snapshots in batches
        private async Task LoadSnapshotDataAsync(SimulationSnapshot snapshot)
        {
            using var _db = _context.DbFactory.CreateDbContext();
            _db.Database.SetCommandTimeout(120);

            const int batchSize = 5000;

            // Load agent snapshots in batches
            var agentSnaps = new List<AgentSnapshot>();
            var offset = 0;

            Console.Write($"[LoadSnapshot] Loading agent snapshots... ");
            var startTime = DateTime.Now;

            while (true)
            {
                var batch = await _db.Set<AgentSnapshot>()
                    .Where(a => a.SimulationSnapshotId == snapshot.Id)
                    .OrderBy(a => a.AgentId)
                    .Skip(offset)
                    .Take(batchSize)
                    .AsNoTracking()
                    .ToListAsync();

                if (batch.Count == 0) break;

                agentSnaps.AddRange(batch);
                offset += batchSize;

                if (agentSnaps.Count % 10000 == 0)
                {
                    Console.Write($"{agentSnaps.Count}... ");
                }
            }

            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Loaded {agentSnaps.Count} agents in {elapsed:F1}s");

            // Load building snapshots in batches
            var buildingSnaps = new List<BuildingSnapshot>();
            offset = 0;

            Console.Write($"[LoadSnapshot] Loading building snapshots... ");
            startTime = DateTime.Now;

            while (true)
            {
                var batch = await _db.Set<BuildingSnapshot>()
                    .Where(b => b.SimulationSnapshotId == snapshot.Id)
                    .OrderBy(b => b.BuildingId)
                    .Skip(offset)
                    .Take(batchSize)
                    .AsNoTracking()
                    .ToListAsync();

                if (batch.Count == 0) break;

                buildingSnaps.AddRange(batch);
                offset += batchSize;
            }

            elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Loaded {buildingSnaps.Count} buildings in {elapsed:F1}s");

            // Assign to snapshot
            snapshot.Agents = agentSnaps;
            snapshot.Buildings = buildingSnaps;
        }


        // Restore state into context
        private void RestoreSnapshot(SimulationSnapshot snapshot)
        {
            // Set simulation tick
            _context.CurrentTick = snapshot.Tick;

            // Restore cumulative infection metrics
            _context.InfectionService.RestoreMetricsFromSnapshot(
                snapshot.TotalInfections,
                snapshot.TotalDeaths,
                snapshot.TotalRecoveries
            );

            // Build fast lookup dictionaries for snapshot data
            var agentSnaps = snapshot.Agents;
            var buildingSnaps = snapshot.Buildings;

            var agentSnapLookup = new Dictionary<int, AgentSnapshot>(agentSnaps.Count);
            foreach (var s in agentSnaps)
                agentSnapLookup[s.AgentId] = s;

            var buildingSnapLookup = new Dictionary<int, BuildingSnapshot>(buildingSnaps.Count);
            foreach (var s in buildingSnaps)
                buildingSnapLookup[s.BuildingId] = s;

            // Restore AgentStates
            var agents = _context.AgentStates;

            for (int i = 0; i < agents.Length; i++)
            {
                ref var a = ref agents[i];

                if (agentSnapLookup.TryGetValue(a.Id, out var snap))
                {
                    a.Lat = (float)snap.Latitude;
                    a.Lon = (float)snap.Longitude;
                    a.InfectionProgress = (float)snap.InfectionProgress;
                    a.IsIsolating = snap.isIsolating;
                    a.CurrentBuildingId = snap.CurrentBuildingId ?? 0;
                    a.HealthState = (byte)ParseHealthState(snap.HealthState);
                    a.SymptomSeverity = snap.SymptomSeverity;
                    a.Immunity = snap.Immunity;
                    a.InfectionCount = snap.InfectionCount;
                }
            }

            // Restore BuildingStates
            var buildings = _context.BuildingStates;
            for (int i = 0; i < buildings.Length; i++)
            {
                ref var b = ref buildings[i];
                if (buildingSnapLookup.TryGetValue(b.Id, out var snap))
                {
                    b.Occupants = (short)snap.CurrentOccupancy;
                }
            }

            // Reset RNG to deterministic state for this tick
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