using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using MySimulationAPI.DTOs;
using System.Security.Claims;
using MySimulationAPI.SimulationConfiguration;
using System.Threading.Tasks;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // require JWT on all routes
    public class SimulationController : ControllerBase
    {
        private readonly SimDbContext _db;
        private readonly OsmService _osmService;
        private readonly PresetService _presetService;
        private readonly SimulationSetup _simulationSetup;
        private readonly SimulationManager _simulationManager;

        public SimulationController(
            SimDbContext db,
            OsmService osmService,
            PresetService presetService,
            SimulationSetup simulationSetup,
            SimulationManager simulationManager)
        {
            _db = db;
            _osmService = osmService;
            _presetService = presetService;
            _simulationSetup = simulationSetup;
            _simulationManager = simulationManager;
        }

        // Helper to get logged-in user
        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        // Helper to check if user is admin
        private bool IsAdmin() => User.IsInRole("Admin");


        /// <summary>
        /// Gets all simulations (for loggged in user or all if admin).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSimulations()
        {
            IQueryable<Simulation> query = _db.Simulations
                .Include(s => s.MapArea)
                .Include(s => s.Disease)
                .AsSplitQuery();


            if (!IsAdmin())
            {
                query = query.Where(s => s.UserId == GetUserId());
            }

            var sims = await query.ToListAsync();

            // Get agent counts efficiently
            var simIds = sims.Select(s => s.Id).ToList();
            var agentCounts = await _db.Agents
                .Where(a => simIds.Contains(a.SimulationId))
                .GroupBy(a => a.SimulationId)
                .Select(g => new { SimulationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SimulationId, x => x.Count);

            return Ok(sims.Select(s => new
            {
                s.Id,
                s.Name,
                s.CreatedAt,
                s.Status,
                MapArea = s.MapArea?.Name,
                Disease = s.Disease?.Name,
                AgentCount = agentCounts.GetValueOrDefault(s.Id, 0)
            }));
        }

        /// <summary>
        /// Gets a specific simulation (user must be owner or be admin).
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSimulation(int id)
        {
            var sim = await _db.Simulations
                .Include(s => s.MapArea)
                .Include(s => s.Disease)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            var agentCount = await _db.Agents
                .Where(a => a.SimulationId == id)
                .CountAsync();

            var buildingCount = sim.MapArea != null
                ? await _db.Buildings
                    .Where(b => b.MapAreaId == sim.MapArea.Id)
                    .CountAsync()
                : 0;

            return Ok(new
            {
                // Simulation basic info
                sim.Id,
                sim.Name,
                sim.CreatedAt,
                sim.TickLength,

                // Disease info
                Disease = sim.Disease == null ? null : new
                {
                    sim.Disease.Id,
                    sim.Disease.Name,
                    sim.Disease.InfectionRate,
                    sim.Disease.RecoveryRate,
                    sim.Disease.MortalityRate,
                    sim.Disease.IncubationPeriod,
                    sim.Disease.InfectiousPeriod,
                    sim.Disease.SymptomaticRate,
                    sim.Disease.SevereSymptomsRate,
                    sim.Disease.HospitalSeekingRate,
                    sim.Disease.ImmunityDuration,
                    sim.Disease.ReinfectionResistance,
                    sim.Disease.MaskEffectiveness,
                    sim.Disease.VaccineEffectiveness,
                },

                // Map area info
                MapArea = sim.MapArea == null ? null : new
                {
                    sim.MapArea.Id,
                    sim.MapArea.Name,
                    sim.MapArea.CenterLat,
                    sim.MapArea.CenterLon,
                    sim.MapArea.RadiusKm,
                    BuildingCount = buildingCount
                },

                // Agent count
                AgentCount = agentCount
            });
        }

        /// <summary>
        /// Deletes a simulation by ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSimulation(int id)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            _db.Simulations.Remove(sim);
            await _db.SaveChangesAsync();
            return Ok($"Simulation {id} deleted successfully");
        }

        /// <summary>   
        /// Resets a simulation by ID removing all computed data and metrics.
        /// </summary>
        [HttpDelete("{id}/reset")]
        public async Task<IActionResult> ResetSimulation(int id)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            try
            {
                await _simulationManager.ResetAsync(id);

                var message = sim.InitialInfectionCount > 0
                    ? $"Simulation {id} reset successfully. {sim.InitialInfectionCount} agents re-infected."
                    : $"Simulation {id} reset successfully.";

                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to reset simulation: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes a new epidemic simulation.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> InitialiseSimulation([FromBody] InitialiseSimulationDto dto)
        {
            try
            {
                var simulationID = await _simulationSetup.InitialiseSimulation(dto.SimulationName, GetUserId());
                return Ok(new { SimulationId = simulationID, Message = "Simulation created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create simulation: {ex.Message}");
            }
        }
        /// <summary>
        /// Configure the map area for the simulation.
        /// </summary>
        /// <remarks>
        /// This endpoint allows you to associate a geographical area with a simulation.
        /// You can either use a predefined area preset (<c>PresetId</c>) or define a custom area
        /// using latitude, longitude, and radius in kilometers.
        /// </remarks>
        [HttpPost("{id}/map-area")]
        public async Task<IActionResult> SetupMapArea(int id, [FromBody] SetupMapAreaDto dto)
        {
            var sim = await _db.Simulations
          .FirstOrDefaultAsync(s => s.Id == id);

            if (sim == null) return NotFound();

            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            try
            {
                var estimate = await _simulationSetup.SetupMapArea(
                    id, dto.PresetId, dto.PresetName,
                    dto.Latitude, dto.Longitude, dto.RadiusKm, dto.SavePreset, GetUserId(), IsAdmin());

                return Ok(new
                {
                    PopulationEstimate = estimate,
                    Message = "Map area set up successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to setup map area: {ex.Message}");
            }
        }
        /// <summary>
        /// Configure the population for the simulation.
        /// </summary>
        [HttpPost("{id}/population")]
        public async Task<IActionResult> SetupPopulation(int id, [FromBody] SetupPopulationDto dto)
        {
            var sim = await _db.Simulations
          .FirstOrDefaultAsync(s => s.Id == id);

            if (sim == null) return NotFound();

            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            try
            {
                await _simulationSetup.SetupPopulation(id, dto, GetUserId());
                return Ok(new { Message = "Population generated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to setup population: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure the disease for the simulation.
        /// </summary>
        /// <remarks>
        /// You can either use a predefined Disease preset (<c>PresetId</c>) or define a custom disease
        /// </remarks>
        [HttpPost("{id}/disease")]
        public async Task<IActionResult> SetupDisease(int id, [FromBody] SetupDiseaseDto dto)
        {
            var sim = await _db.Simulations
           .FirstOrDefaultAsync(s => s.Id == id);

            if (sim == null) return NotFound();

            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            try
            {
                await _simulationSetup.SetupDisease(
                    id,
                    dto.PresetId,
                    dto.Name,
                    dto.InfectionRate,
                    dto.RecoveryRate,
                    dto.MortalityRate,
                    dto.IncubationPeriod,
                    dto.InfectiousPeriod,
                    dto.SymptomaticRate,
                    dto.SevereSymptomsRate,
                    dto.HospitalSeekingRate,
                    dto.ImmunityDuration,
                    dto.ReinfectionResistance,
                    dto.MaskEffectiveness,
                    dto.VaccineEffectiveness,
                    dto.SavePreset,
                    GetUserId(),
                    IsAdmin());

                return Ok(new { Message = "Disease configured successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to setup disease: {ex.Message}");
            }
        }


        /// <summary>
        /// Loads the simulation with given ID into memory.
        /// </summary>
        [HttpPost("{id}/load")]
        public async Task<IActionResult> Load(int id)
        {
            var sim = await _db.Simulations
                .Include(s => s.MapArea)
                .Include(s => s.Agents)
                .Include(s => s.Disease)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sim == null) return NotFound();

            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            if (_simulationManager.TryGet(id, out var existingContext) && existingContext != null)
            {
                return Ok($"Simulation {id} is already loaded in memory.");
            }

            var context = await _simulationManager.GetOrCreateAsync(id);
            context.GenerateStates();
            return Ok($"Simulation {id} loaded into memory.");
        }


        /// <summary>
        /// Runs the simulation with given ID.
        /// </summary>
        [HttpPost("{id}/run")]
        public async Task<IActionResult> Run(int id, [FromBody] long? targetTick = null)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            try
            {
                await _simulationManager.StartAsync(id, targetTick);
                return Ok($"Simulation {id} is now running.");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Pauses the simulation with given ID.
        /// </summary>
        [HttpPost("{id}/pause")]
        public async Task<IActionResult> Pause(int id)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            try
            {
                await _simulationManager.PauseAsync(id);
                return Ok($"Simulation {id} paused.");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }


        /// <summary>
        /// Unloads the simulation with given ID from memory.
        /// </summary>
        [HttpPost("{id}/unload")]
        public async Task<IActionResult> Unload(int id)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            if (!_simulationManager.TryGet(id, out var context) || context == null)
            {
                return NotFound($"Simulation {id} is not loaded in memory");
            }
            await _simulationManager.RemoveAsync(id);
            return Ok($"Simulation {id} unloaded from memory.");
        }

        /// <summary>
        /// Sets the simulation speed multiplier.
        /// </summary>
        [HttpPost("{id}/speed")]
        public async Task<IActionResult> SetSpeed(int id, [FromBody] double speedMultiplier)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            try
            {
                await _simulationManager.SetSpeedAsync(id, speedMultiplier);
                return Ok($"Simulation {id} speed set to {speedMultiplier}x");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Sets current simulation tick.
        /// </summary>
        [HttpPost("{id}/tick")]
        public async Task<IActionResult> SetTick(int id, [FromBody] int tick)
        {
            if (tick < 0) return BadRequest("Tick cannot be negative");
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            try
            {
                await _simulationManager.SetTickAsync(id, tick);
                return Ok($"Simulation {id} tick set to {tick}");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Seeds initial infection in the simulation by infecting random agents.
        /// </summary>
        /// <param name="id">Simulation ID</param>
        /// <param name="count">Number of agents to initially infect (default: 1)</param>
        [HttpPost("{id}/infect")]
        public async Task<IActionResult> SeedInfection(int id, [FromBody] int? count)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            if (!_simulationManager.TryGet(id, out var context) || context == null)
            {
                return NotFound($"Simulation {id} is not loaded in memory. Load it first.");
            }

            int infectCount = count ?? 1;
            if (infectCount < 1 || infectCount > 100)
            {
                return BadRequest("Infection count must be between 1 and 100");
            }

            try
            {
                context.InfectionService.SeedInfection(infectCount);

                // Store the initial infection count for future resets
                sim.InitialInfectionCount = infectCount;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"Successfully seeded {infectCount} initial infection(s)",
                    SimulationId = id,
                    InfectedCount = infectCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to seed infection: {ex.Message}");
            }
        }

        // ==========================================
        // MITIGATION ENDPOINTS
        // ==========================================

        [HttpGet("{id}/mitigations")]
        public async Task<IActionResult> GetMitigations(int id)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            var mitigations = await _db.Mitigations
                .Where(m => m.SimulationId == id)
                .ToListAsync();

            var result = mitigations.Select(m => new
            {
                m.Id,
                m.Name,
                m.Type,
                m.SimulationId,
                m.IsActive,
                m.CreatedAt,
                m.ModifiedAt,
                ComplianceRate = m is SelfIsolationMitigation si ? (double?)si.ComplianceRate : null,
                SymptomThreshold = m is SelfIsolationMitigation si2 ? (double?)si2.SymptomThreshold : null,
                AdoptionRate = m is MaskWearingMitigation mw ? (double?)mw.AdoptionRate : null,
                EffectivenessModifier = m is MaskWearingMitigation mw2 ? (double?)mw2.EffectivenessModifier :
                                       m is VaccinationMitigation v ? (double?)v.EffectivenessModifier : null,
                VaccinationRate = m is VaccinationMitigation v2 ? (double?)v2.VaccinationRate : null,
                DaysToFullEffectiveness = m is VaccinationMitigation v3 ? (int?)v3.DaysToFullEffectiveness : null
            });

            return Ok(result);
        }

        /// <summary>
        /// Creates a new mitigation for a simulation.
        /// Deletes all future snapshots and metrics as mitigation will change outcomes.
        /// </summary>
        [HttpPost("{id}/mitigations")]
        public async Task<IActionResult> CreateMitigation(int id, [FromBody] CreateMitigationDto dto)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            bool wasRunning = false;
            SimulationContext? context = null;

            if (_simulationManager.TryGet(id, out context) && context != null)
            {
                wasRunning = context.Engine.IsRunning;
                if (wasRunning)
                {
                    await _simulationManager.PauseAsync(id);
                }
            }

            Mitigation mitigation;

            switch (dto.Type)
            {
                case "SelfIsolation":
                    mitigation = new SelfIsolationMitigation
                    {
                        Name = dto.Name ?? "Self-Isolation Protocol",
                        ComplianceRate = dto.ComplianceRate ?? 0.7,
                        SymptomThreshold = dto.SymptomThreshold ?? 0.3,
                        SimulationId = id
                    };
                    break;

                case "MaskWearing":
                    mitigation = new MaskWearingMitigation
                    {
                        Name = dto.Name ?? "Mask Wearing Policy",
                        AdoptionRate = dto.AdoptionRate ?? 0.6,
                        EffectivenessModifier = dto.MaskEffectivenessModifier ?? 1.0,
                        SimulationId = id
                    };
                    break;

                case "Vaccination":
                    mitigation = new VaccinationMitigation
                    {
                        Name = dto.Name ?? "Vaccination Campaign",
                        VaccinationRate = dto.VaccinationRate ?? 0.7,
                        EffectivenessModifier = dto.VaccineEffectivenessModifier ?? 1.0,
                        DaysToFullEffectiveness = dto.DaysToFullEffectiveness ?? 14,
                        SimulationId = id
                    };
                    break;

                default:
                    return BadRequest($"Unknown mitigation type: {dto.Type}");
            }

            _db.Mitigations.Add(mitigation);

            // Delete future snapshots and metrics since mitigation changes outcomes
            await DeleteFutureData(id);

            await _db.SaveChangesAsync();

            // Resume simulation if it was running before
            if (wasRunning && context != null)
            {
                await _simulationManager.StartAsync(id);
            }

            // Return DTO to avoid circular reference
            var result = new
            {
                mitigation.Id,
                mitigation.Name,
                mitigation.Type,
                mitigation.SimulationId,
                mitigation.IsActive,
                mitigation.CreatedAt,
                mitigation.ModifiedAt,
                ComplianceRate = mitigation is SelfIsolationMitigation si ? (double?)si.ComplianceRate : null,
                SymptomThreshold = mitigation is SelfIsolationMitigation si2 ? (double?)si2.SymptomThreshold : null,
                AdoptionRate = mitigation is MaskWearingMitigation mw ? (double?)mw.AdoptionRate : null,
                EffectivenessModifier = mitigation is MaskWearingMitigation mw2 ? (double?)mw2.EffectivenessModifier :
                                       mitigation is VaccinationMitigation v ? (double?)v.EffectivenessModifier : null,
                VaccinationRate = mitigation is VaccinationMitigation v2 ? (double?)v2.VaccinationRate : null,
                DaysToFullEffectiveness = mitigation is VaccinationMitigation v3 ? (int?)v3.DaysToFullEffectiveness : null
            };

            return Ok(result);
        }

        /// <summary>
        /// Updates an existing mitigation.
        /// Deletes all future snapshots and metrics as mitigation changes will affect outcomes.
        /// </summary>
        [HttpPut("{id}/mitigations/{mitigationId}")]
        public async Task<IActionResult> UpdateMitigation(int id, int mitigationId, [FromBody] UpdateMitigationDto dto)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            var mitigation = await _db.Mitigations.FindAsync(mitigationId);
            if (mitigation == null || mitigation.SimulationId != id)
                return NotFound();

            bool wasRunning = false;
            SimulationContext? context = null;

            if (_simulationManager.TryGet(id, out context) && context != null)
            {
                wasRunning = context.Engine.IsRunning;
                if (wasRunning)
                {
                    await _simulationManager.PauseAsync(id);
                }
            }

            if (dto.Name != null) mitigation.Name = dto.Name;
            if (dto.IsActive.HasValue) mitigation.IsActive = dto.IsActive.Value;
            mitigation.ModifiedAt = DateTime.UtcNow;

            switch (mitigation)
            {
                case SelfIsolationMitigation selfIsolation:
                    if (dto.ComplianceRate.HasValue) selfIsolation.ComplianceRate = dto.ComplianceRate.Value;
                    if (dto.SymptomThreshold.HasValue) selfIsolation.SymptomThreshold = dto.SymptomThreshold.Value;
                    break;

                case MaskWearingMitigation maskWearing:
                    if (dto.AdoptionRate.HasValue) maskWearing.AdoptionRate = dto.AdoptionRate.Value;
                    if (dto.MaskEffectivenessModifier.HasValue) maskWearing.EffectivenessModifier = dto.MaskEffectivenessModifier.Value;
                    break;

                case VaccinationMitigation vaccination:
                    if (dto.VaccinationRate.HasValue) vaccination.VaccinationRate = dto.VaccinationRate.Value;
                    if (dto.VaccineEffectivenessModifier.HasValue) vaccination.EffectivenessModifier = dto.VaccineEffectivenessModifier.Value;
                    if (dto.DaysToFullEffectiveness.HasValue) vaccination.DaysToFullEffectiveness = dto.DaysToFullEffectiveness.Value;
                    break;
            }

            // Delete future snapshots and metrics since mitigation changes will affect outcomes
            await DeleteFutureData(id);

            await _db.SaveChangesAsync();

            // Resume simulation if it was running before
            if (wasRunning && context != null)
            {
                await _simulationManager.StartAsync(id);
            }

            var result = new
            {
                mitigation.Id,
                mitigation.Name,
                mitigation.Type,
                mitigation.SimulationId,
                mitigation.IsActive,
                mitigation.CreatedAt,
                mitigation.ModifiedAt,
                ComplianceRate = mitigation is SelfIsolationMitigation si ? (double?)si.ComplianceRate : null,
                SymptomThreshold = mitigation is SelfIsolationMitigation si2 ? (double?)si2.SymptomThreshold : null,
                AdoptionRate = mitigation is MaskWearingMitigation mw ? (double?)mw.AdoptionRate : null,
                EffectivenessModifier = mitigation is MaskWearingMitigation mw2 ? (double?)mw2.EffectivenessModifier :
                                       mitigation is VaccinationMitigation v ? (double?)v.EffectivenessModifier : null,
                VaccinationRate = mitigation is VaccinationMitigation v2 ? (double?)v2.VaccinationRate : null,
                DaysToFullEffectiveness = mitigation is VaccinationMitigation v3 ? (int?)v3.DaysToFullEffectiveness : null
            };

            return Ok(result);
        }

        /// <summary>
        /// Deletes a mitigation.
        /// Deletes all future snapshots and metrics as removal will change outcomes.
        /// </summary>
        [HttpDelete("{id}/mitigations/{mitigationId}")]
        public async Task<IActionResult> DeleteMitigation(int id, int mitigationId)
        {
            var sim = await _db.Simulations.FindAsync(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            var mitigation = await _db.Mitigations.FindAsync(mitigationId);
            if (mitigation == null || mitigation.SimulationId != id)
                return NotFound();

            bool wasRunning = false;
            SimulationContext? context = null;

            if (_simulationManager.TryGet(id, out context) && context != null)
            {
                wasRunning = context.Engine.IsRunning;
                if (wasRunning)
                {
                    await _simulationManager.PauseAsync(id);
                }
            }

            _db.Mitigations.Remove(mitigation);

            // Delete future snapshots and metrics since mitigation removal changes outcomes
            await DeleteFutureData(id);

            await _db.SaveChangesAsync();

            // Resume simulation if it was running before
            if (wasRunning && context != null)
            {
                await _simulationManager.StartAsync(id);
            }

            return Ok($"Mitigation {mitigationId} deleted");
        }

        /// <summary>
        /// Helper method to delete all snapshots and metrics with tick > currentTick.
        /// Called when mitigations are modified to invalidate future computed data.
        /// </summary>
        private async Task DeleteFutureData(int simulationId)
        {
            long currentTick = 0;

            // Get current tick from memory if simulation is loaded
            if (_simulationManager.TryGet(simulationId, out var context) && context != null)
            {
                currentTick = context.CurrentTick;
            }
            else
            {
                // Otherwise get latest snapshot tick from database
                currentTick = await _db.SimulationSnapshots
                    .Where(s => s.SimulationId == simulationId)
                    .OrderByDescending(s => s.Tick)
                    .Select(s => (long?)s.Tick)
                    .FirstOrDefaultAsync() ?? 0;
            }

            // Delete all snapshots with tick > currentTick
            var futureSnapshots = await _db.SimulationSnapshots
                .Where(s => s.SimulationId == simulationId && s.Tick > currentTick)
                .ToListAsync();
            _db.SimulationSnapshots.RemoveRange(futureSnapshots);

            // Delete all metrics with tick > currentTick
            var futureMetrics = await _db.SimulationMetrics
                .Where(m => m.SimulationId == simulationId && m.Tick > currentTick)
                .ToListAsync();
            _db.SimulationMetrics.RemoveRange(futureMetrics);

            Console.WriteLine($"[Mitigation Change] Deleted {futureSnapshots.Count} future snapshots and {futureMetrics.Count} future metrics for simulation {simulationId}");
        }
    }
}