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

        public SimulationController(SimDbContext db, OsmService osmService, PresetService presetService, SimulationSetup simulationSetup, SimulationManager simulationManager)
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
                .Include(s => s.Agents)
                .AsSplitQuery();


            if (!IsAdmin())
            {
                query = query.Where(s => s.UserId == GetUserId());
            }

            var sims = await query.ToListAsync();
            return Ok(sims.Select(s => new
            {
                s.Id,
                s.Name,
                s.CreatedAt,
                s.Status,
                MapArea = s.MapArea?.Name,
                Disease = s.Disease?.Name,
                AgentCount = s.Agents.Count
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
                sim.Status,
                sim.CreatedAt,

                // Disease info
                Disease = sim.Disease == null ? null : new
                {
                    sim.Disease.Id,
                    sim.Disease.Name,
                    sim.Disease.InfectionRate,
                    sim.Disease.RecoveryRate,
                    sim.Disease.MortalityRate,
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

            var simulationSnapshots = await _db.SimulationSnapshots
                .Where(s => s.SimulationId == id)
                .ToListAsync();
            var simulationMetrics = await _db.SimulationMetrics
                .Where(m => m.SimulationId == id)
                .ToListAsync();
            _db.SimulationSnapshots.RemoveRange(simulationSnapshots);
            _db.SimulationMetrics.RemoveRange(simulationMetrics);
            await _db.SaveChangesAsync();
            return Ok($"Simulation {id} reset successfully");
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
                    id, dto.PresetId, dto.Name, dto.InfectionRate,
                    dto.RecoveryRate, dto.MortalityRate, dto.SavePreset, GetUserId(), IsAdmin());

                return Ok(new { Message = "Disease configured successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to setup disease: {ex.Message}");
            }
        }


        /// <summary>
        /// Loads the simulation with given ID into memory and plays the simulation from the latest tick recorded.
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(int id)
        {
            var sim = await _db.Simulations
                .Include(s => s.MapArea)
                .Include(s => s.Agents)
                .Include(s => s.Disease)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sim == null) return NotFound();

            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();

            var context = _simulationManager.GetOrCreate(sim.Id);
            context.GenerateStates();
            context.Engine.Start();
            return Ok($"Simulation {id} started");
        }

        /// <summary>
        /// Stops the simulation with the given ID and removes it from memory.
        /// </summary>
        [HttpPost("{id}/stop")]
        public IActionResult Stop(int id)
        {
            var sim = _db.Simulations.Find(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            if (!_simulationManager.TryGet(id, out var context) || context == null)
            {
                return NotFound($"Simulation {id} is not running");
            }
            _simulationManager.Remove(id);
            return Ok($"Simulation {id} stopped and removed from memory");
        }

        /// <summary>
        /// Sets the simulation speed multiplier.
        /// </summary>
        [HttpPost("{id}/speed")]
        public IActionResult SetSpeed(int id, [FromBody] int speedMultiplier)
        {
            var sim = _db.Simulations.Find(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            if (!_simulationManager.TryGet(id, out var context) || context == null)
            {
                return NotFound($"Simulation {id} is not running");
            }
            context.SimulationSpeed = speedMultiplier;
            return Ok($"Simulation {id} speed set to {speedMultiplier}x");
        }

        /// <summary>
        /// Sets current simulation tick.
        /// </summary>
        [HttpPost("{id}/tick")]
        public async Task<IActionResult> SetTick(int id, [FromBody] int tick)
        {
            if (tick < 0) return BadRequest("Tick cannot be negative");
            var sim = _db.Simulations.Find(id);
            if (sim == null) return NotFound();
            if (sim.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            if (!_simulationManager.TryGet(id, out var context) || context == null)
            {
                return NotFound($"Simulation {id} is not running");
            }
            await context.SnapshotManager.LoadTargetTick(tick);
            return Ok($"Simulation {id} tick set to {tick}");
        }
    }
}