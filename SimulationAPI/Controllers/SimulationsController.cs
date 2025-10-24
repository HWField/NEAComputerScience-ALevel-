using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using MySimulationAPI.DTOs;
using System.Security.Claims;
using MySimulationAPI.SimulationConfiguration;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // require JWT
    public class SimulationsController : ControllerBase
    {
        private readonly SimDbContext _db;
        private readonly OsmService _osmService;
        private readonly PresetService _presetService;
        private readonly SimulationSetup _simulationSetup;
        private readonly SimulationManager _simulationManager;

        public SimulationsController(SimDbContext db, OsmService osmService, PresetService presetService, SimulationSetup simulationSetup, SimulationManager simulationManager)
        {
            _db = db;
            _osmService = osmService;
            _presetService = presetService;
            _simulationSetup = simulationSetup;
            _simulationManager = simulationManager;
        }

        // Helper to get logged-in user
        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


        /// <summary>
        /// Initializes a new epidemic simulation.
        /// </summary>
        [HttpPost("create/step1")]
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
        [HttpPost("create/step2")]
        public async Task<IActionResult> SetupMapArea([FromBody] SetupMapAreaDto dto)
        {
            try
            {
                var (mapArea, estimate) = await _simulationSetup.SetupMapArea(
                    dto.SimulationId, dto.PresetId, dto.PresetName,
                    dto.Latitude, dto.Longitude, dto.RadiusKm, GetUserId());

                return Ok(new
                {
                    MapAreaId = mapArea.Id,
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
        [HttpPost("create/step3")]
        public async Task<IActionResult> SetupPopulation([FromBody] SetupPopulationDto dto)
        {
            try
            {
                await _simulationSetup.SetupPopulation(dto.SimulationId, dto.AgentCount, GetUserId());
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
        [HttpPost("create/step4")]
        public async Task<IActionResult> SetupDisease([FromBody] SetupDiseaseDto dto)
        {
            try
            {
                await _simulationSetup.SetupDisease(
                    dto.SimulationId, dto.PresetId, dto.Name, dto.InfectionRate,
                    dto.RecoveryRate, dto.MortalityRate, GetUserId());

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
            _simulationManager.Remove(id);
            return Ok($"Simulation {id} stopped and removed from memory");
        }
    }
}