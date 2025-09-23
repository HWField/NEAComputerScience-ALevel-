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

        [HttpPost("create")]
        public async Task<IActionResult> CreateSimulation([FromBody] CreateSimulationDto dto)
        {
            try
            {
                Simulation createdSimulation;

                Console.WriteLine(dto);
                if (dto.PresetId.HasValue)
                {
                    var exists = await _db.MapAreaPresets
                     .AnyAsync(p => p.Id == dto.PresetId.Value);
                    if (!exists)
                        return NotFound("Preset not found");

                    createdSimulation = await _simulationSetup.CreateSimulation(dto.SimulationName, dto.PresetId.Value, dto.AgentCount, GetUserId());
                }
                else
                {
                    // Create new preset from OSM data
                    if (string.IsNullOrEmpty(dto.PresetName) ||
                        !dto.Latitude.HasValue ||
                        !dto.Longitude.HasValue ||
                        !dto.RadiusKm.HasValue)
                    {
                        return BadRequest("Name, Latitude, Longitude, and RadiusKm are required for creating a new preset");
                    }

                    MapAreaPreset preset = await _presetService.FetchAndCreatePresetAsync(
                        dto.PresetName,
                        dto.Latitude.Value,
                        dto.Longitude.Value,
                        dto.RadiusKm.Value);

                    createdSimulation = await _simulationSetup.CreateSimulation(dto.SimulationName, preset.Id, dto.AgentCount, GetUserId());
                }

                return Ok(new
                {
                    Message = "Simulation created successfully",
                    Preset = new
                    {
                        createdSimulation
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create simulation: {ex.Message}");
            }
        }





        // [HttpPost("{id}/start")]
        // public IActionResult StartSimulation(int id)
        // {
        //     var sim = _db.Simulations
        //         .Include(s => s.Agents)
        //         .Include(s => s.Locations)
        //         .FirstOrDefault(s => s.Id == id);

        //     if (sim == null) return NotFound();

        //     var engine = new SimulationEngine.SimulationEngine(sim);
        //     _runner.EnqueueSimulation(engine);

        //     return Ok(new { Status = "Queued/Running" });
        // }


        // DELETE /api/simulations/{id}
        // [HttpDelete("{id}")]
        // public async Task<IActionResult> DeleteSimulation(int id)
        // {
        //     var userId = GetUserId();

        //     // Load simulation with related entities, async as mght take a while for large simulations and so shouldnt block thread
        //     var sim = await _db.Simulations
        //         .Include(s => s.Agents)
        //         .Include(s => s.Locations)
        //         .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        //     if (sim == null)
        //         return NotFound("Simulation not found or you do not have permission");

        //     // Remove related entities first (if cascade delete not configured)
        //     _db.Agents.RemoveRange(sim.Agents);
        //     _db.Locations.RemoveRange(sim.Locations);

        //     // Remove the simulation
        //     _db.Simulations.Remove(sim);
        //     await _db.SaveChangesAsync();

        //     return Ok(new { message = $"Simulation '{sim.Name}' deleted successfully" });
        // }

    }
}