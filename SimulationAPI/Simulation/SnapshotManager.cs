using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;

namespace MySimulationAPI.SimulationConfiguration
{
    public class SnapshotManager
    {
        private readonly SimulationContext _context;
        private readonly SimDbContext _db;

        public SnapshotManager(SimulationContext context)
        {
            _context = context;
            _db = context.Db;
        }

        // ------------------------------------------------------------
        // Save snapshot to DB
        // ------------------------------------------------------------
        public async Task SaveSnapshotAsync()
        {
            var snapshot = new SimulationSnapshot
            {
                SimulationId = _context.Simulation.Id,
                Tick = _context.CurrentTick,
                CreatedAt = DateTime.UtcNow,
            };

            snapshot.Agents = _context.Agents.Select(a => new AgentSnapshot
            {
                AgentId = a.Id,
                Latitude = a.CurrentLat,
                Longitude = a.CurrentLon,
                HealthState = a.MedicalProfile.ToString()
            }).ToList();

            snapshot.Buildings = _context.Buildings.Select(b => new BuildingSnapshot
            {
                BuildingId = b.Id,
                CurrentOccupancy = b.CurrentOccupancy
            }).ToList();

            await _db.SimulationSnapshots.AddAsync(snapshot);
            await _db.SaveChangesAsync();
        }

        // ------------------------------------------------------------
        // Load snapshot from DB (nearest <= targetTick)
        // ------------------------------------------------------------
        public async Task<bool> TryLoadSnapshotAsync(long targetTick)
        {
            var snapshot = await _db.SimulationSnapshots
                .Include(s => s.Agents)
                .Include(s => s.Buildings)
                .Where(s => s.SimulationId == _context.Simulation.Id && s.Tick <= targetTick)
                .OrderByDescending(s => s.Tick)
                .FirstOrDefaultAsync();

            if (snapshot == null)
                return false;

            RestoreSnapshot(snapshot);
            return true;
        }

        // ------------------------------------------------------------
        // Restore state into context
        // ------------------------------------------------------------
        private void RestoreSnapshot(SimulationSnapshot snapshot)
        {
            _context.CurrentTick = snapshot.Tick;

            // Restore agents
            foreach (var agent in _context.Agents)
            {
                var snap = snapshot.Agents.FirstOrDefault(s => s.AgentId == agent.Id);
                if (snap == null) continue;

                agent.CurrentLat = snap.Latitude;
                agent.CurrentLon = snap.Longitude;
                agent.HealthState = snap.HealthState;
                agent.InfectionProgress = snap.InfectionProgress;
                agent.IsQuarantined = snap.IsQuarantined;

                //restore building reference
                if (snap.CurrentBuildingId.HasValue)
                {
                    agent.CurrentBuilding = _context.Buildings
                        .FirstOrDefault(b => b.Id == snap.CurrentBuildingId.Value);
                }
                else
                {
                    agent.CurrentBuilding = null;
                }
            }

            // Restore building occupancy
            foreach (var building in _context.Buildings)
            {
                var snap = snapshot.Buildings.FirstOrDefault(s => s.BuildingId == building.Id);
                if (snap == null) continue;
                building.CurrentOccupancy = snap.CurrentOccupancy;
            }

            // Restore RNG state
            _context.Rng = new Random(_context.Simulation.Id + (int)snapshot.Tick);
        }


    }
}