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

            // Configure longer timeout for large datasets
            _db.Database.SetCommandTimeout(300); // 5 minutes

            Console.WriteLine($"[SaveSnapshot] Starting snapshot save for tick {_context.CurrentTick}...");

            // --- Delete existing snapshot (if any) ---
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

            // --- AGENTS ---
            var agents = _context.AgentStates;
            Console.Write($"[SaveSnapshot] Preparing {agents.Length} agent snapshots... ");
            var agentSnaps = agents.Select(a => new AgentSnapshot
            {
                SimulationSnapshotId = snapshot.Id,
                AgentId = a.Id,
                Latitude = a.Lat,
                Longitude = a.Lon,
                InfectionProgress = a.InfectionProgress,
                IsQuarantined = a.IsQuarantined,
                CurrentBuildingId = a.CurrentBuildingId > 0 ? a.CurrentBuildingId : null,
                HealthState = ConvertHealthStateToString((AgentHealthState)a.HealthState),
                SymptomSeverity = a.SymptomSeverity,
                Immunity = a.Immunity,
                WearsMask = a.WearsMask,
                IsVaccinated = a.IsVaccinated,
                InfectionCount = a.InfectionCount
            }).ToList();
            Console.WriteLine("✓");

            // --- BUILDINGS ---
            var buildings = _context.BuildingStates;
            Console.Write($"[SaveSnapshot] Preparing {buildings.Length} building snapshots... ");
            var buildingSnaps = buildings.Select(b => new BuildingSnapshot
            {
                SimulationSnapshotId = snapshot.Id,
                BuildingId = b.Id,
                CurrentOccupancy = b.Occupants
            }).ToList();
            Console.WriteLine("✓");

            // --- SAVE TO DATABASE ---
            Console.Write($"[SaveSnapshot] Saving {agentSnaps.Count} agent snapshots... ");
            var startTime = DateTime.Now;
            await _db.BulkInsertAsync(agentSnaps, new BulkConfig
            {
                BatchSize = 5000,
                BulkCopyTimeout = 300
            });
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"✓ Done in {elapsed:F1}s");

            Console.Write($"[SaveSnapshot] Saving {buildingSnaps.Count} building snapshots... ");
            startTime = DateTime.Now;
            await _db.BulkInsertAsync(buildingSnaps, new BulkConfig
            {
                BatchSize = 5000,
                BulkCopyTimeout = 120
            });
            elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"✓ Done in {elapsed:F1}s");

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

        // ------------------------------------------------------------
        // Load snapshot from DB (nearest <= targetTick)
        // ------------------------------------------------------------
        public async Task<bool> LoadTargetTick(long targetTick)
        {
            using var _db = _context.DbFactory.CreateDbContext();

            // Configure longer timeout for large datasets
            _db.Database.SetCommandTimeout(120); // 2 minutes

            _context.Engine.Stop();

            // Step 1: Find the snapshot (WITHOUT eager loading)
            var snapshot = await _db.SimulationSnapshots
                .Where(s => s.SimulationId == _context.Simulation.Id && s.Tick <= targetTick)
                .OrderByDescending(s => s.Tick)
                .FirstOrDefaultAsync();

            if (snapshot == null)
            {
                Console.WriteLine($"[LoadTargetTick] No snapshot found for tick <= {targetTick}");
                return false;
            }

            Console.WriteLine($"[LoadTargetTick] Loading snapshot {snapshot.Id} at tick {snapshot.Tick}...");

            // Step 2: Load agent snapshots in batches
            var agentSnaps = new List<AgentSnapshot>();
            var batchSize = 5000;
            var offset = 0;
            var loadedCount = 0;

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
                loadedCount += batch.Count;
                offset += batchSize;

                if (loadedCount % 10000 == 0)
                {
                    Console.WriteLine($"[LoadTargetTick] Loaded {loadedCount} agent snapshots...");
                }
            }

            Console.WriteLine($"[LoadTargetTick] Loaded {agentSnaps.Count} total agent snapshots");

            // Step 3: Load building snapshots in batches
            var buildingSnaps = new List<BuildingSnapshot>();
            offset = 0;
            loadedCount = 0;

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
                loadedCount += batch.Count;
                offset += batchSize;
            }

            Console.WriteLine($"[LoadTargetTick] Loaded {buildingSnaps.Count} building snapshots");

            // Assign to snapshot for RestoreSnapshot method
            snapshot.Agents = agentSnaps;
            snapshot.Buildings = buildingSnaps;

            Console.WriteLine($"[LoadTargetTick] Restoring snapshot state...");
            RestoreSnapshot(snapshot);

            Console.WriteLine($"[LoadTargetTick] Starting engine to compute forward to tick {targetTick}...");
            _context.Engine.Start(targetTick); //compute forward to target tick

            return true;
        }

        public async Task<bool> LoadLatestSnapshotAsync()
        {
            using var _db = _context.DbFactory.CreateDbContext();
            _db.Database.SetCommandTimeout(120);

            // Find the latest snapshot
            var snapshot = await _db.SimulationSnapshots
                .Where(s => s.SimulationId == _context.Simulation.Id)
                .OrderByDescending(s => s.Tick)
                .FirstOrDefaultAsync();

            if (snapshot == null)
            {
                return false;
            }
            // Load agent snapshots in batches
            var agentSnaps = new List<AgentSnapshot>();
            var batchSize = 5000;
            var offset = 0;

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
            }

            // Count health states in snapshot data for verification
            int snapshotInfected = agentSnaps.Count(a => a.HealthState.Equals("Infected", StringComparison.OrdinalIgnoreCase));
            int snapshotRecovered = agentSnaps.Count(a => a.HealthState.Equals("Recovered", StringComparison.OrdinalIgnoreCase));
            int snapshotDead = agentSnaps.Count(a => a.HealthState.Equals("Dead", StringComparison.OrdinalIgnoreCase));

            // Load building snapshots in batches
            var buildingSnaps = new List<BuildingSnapshot>();
            offset = 0;

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

            // Assign to snapshot
            snapshot.Agents = agentSnaps;
            snapshot.Buildings = buildingSnaps;

            // Restore without computing forward
            RestoreSnapshot(snapshot);

            return true;
        }

        // ------------------------------------------------------------
        // Restore state into context
        // ------------------------------------------------------------
        private void RestoreSnapshot(SimulationSnapshot snapshot)
        {
            // --- Set simulation tick ---
            _context.CurrentTick = snapshot.Tick;

            // --- Restore cumulative infection metrics ---
            _context.InfectionService.RestoreMetricsFromSnapshot(
                snapshot.TotalInfections,
                snapshot.TotalDeaths,
                snapshot.TotalRecoveries
            );

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
            int restoredCount = 0;
            int skippedCount = 0;
            int infectedCount = 0;
            int recoveredCount = 0;
            int deadCount = 0;

            for (int i = 0; i < agents.Length; i++)
            {
                ref var a = ref agents[i];

                if (!agentSnapLookup.TryGetValue(a.Id, out var snap))
                {
                    skippedCount++;
                    continue;
                }

                a.Lat = (float)snap.Latitude;
                a.Lon = (float)snap.Longitude;
                a.InfectionProgress = (float)snap.InfectionProgress;
                a.IsQuarantined = snap.IsQuarantined;
                a.CurrentBuildingId = snap.CurrentBuildingId ?? 0;

                // Parse health string → byte enum
                a.HealthState = (byte)ParseHealthState(snap.HealthState);

                // Restore extended infection tracking
                a.SymptomSeverity = snap.SymptomSeverity;
                a.Immunity = snap.Immunity;
                a.WearsMask = snap.WearsMask;
                a.IsVaccinated = snap.IsVaccinated;
                a.InfectionCount = snap.InfectionCount;

                restoredCount++;

                // Count health states for verification
                switch ((AgentHealthState)a.HealthState)
                {
                    case AgentHealthState.Infected:
                        infectedCount++;
                        break;
                    case AgentHealthState.Recovered:
                        recoveredCount++;
                        break;
                    case AgentHealthState.Dead:
                        deadCount++;
                        break;
                }
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