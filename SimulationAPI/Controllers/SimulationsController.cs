using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using MySimulationAPI.DTOs;
using System.Security.Claims;
using MySimulationAPI.SimulationEngine;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // require JWT
    public class SimulationsController : ControllerBase
    {
        private readonly SimDbContext _db;
        private readonly OsmService _osmService;
        private readonly SimulationRunner _runner;
        private readonly PresetService _presetService;

        private readonly SimulationSetup _simulationSetup;

        public SimulationsController(SimDbContext db, OsmService osmService, SimulationRunner SimulationRunner, PresetService presetService, SimulationSetup simulationSetup)
        {
            _db = db;
            _osmService = osmService;
            _runner = SimulationRunner;
            _presetService = presetService;
            _simulationSetup = simulationSetup;
        }

        // Helper to get logged-in user
        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // routes to create simulation with single request
        [HttpPost("DemoCreate")]
        public async Task<IActionResult> CreateSimulation([FromBody] DemoCreateSimulationDto dto)
        {
            try
            {
                Simulation createdSimulation;

                if (dto.PresetId.HasValue)
                {
                    var exists = await _db.MapAreaPresets
                     .AnyAsync(p => p.Id == dto.PresetId.Value);
                    if (!exists)
                        return NotFound("Preset not found");

                    createdSimulation = await _simulationSetup.DemoCreateSimulation(dto.SimulationName, dto.PresetId.Value, dto.AgentCount, GetUserId());
                }
                else
                {
                    Console.WriteLine("Creating new preset from OSM data");
                    // Create new preset from OSM data
                    if (string.IsNullOrEmpty(dto.PresetName) ||
                        !dto.Latitude.HasValue ||
                        !dto.Longitude.HasValue ||
                        !dto.RadiusKm.HasValue)
                    {
                        return BadRequest("Name, Latitude, Longitude, and RadiusKm are required for creating a new preset");
                    }
                    Console.WriteLine("Creating new preset from OSM data2");

                    MapAreaPreset preset = await _presetService.FetchAndCreatePresetAsync(
                        dto.PresetName,
                        dto.Latitude.Value,
                        dto.Longitude.Value,
                        dto.RadiusKm.Value);

                    Console.WriteLine($"Created preset {preset.Id} - {preset.Name}");

                    createdSimulation = await _simulationSetup.DemoCreateSimulation(dto.SimulationName, preset.Id, dto.AgentCount, GetUserId());
                }

                return Ok(new
                {
                    Message = "Simulation created successfully",

                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create simulation: {ex.Message}");
            }
        }

        // routes to generate simulation in multiple steps (used by frontend wizard)
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

        [HttpPost("create/step4")]
        public async Task<IActionResult> SetupDisease([FromBody] SetupDiseaseDto dto)
        {
            try
            {
                await _simulationSetup.SetupDisease(
                    dto.SimulationId, dto.Name, dto.InfectionRate,
                    dto.RecoveryRate, dto.MortalityRate, GetUserId());

                return Ok(new { Message = "Disease configured successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to setup disease: {ex.Message}");
            }
        }
    }
}