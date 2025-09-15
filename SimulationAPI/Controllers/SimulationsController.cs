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

        public SimulationsController(SimDbContext db, OsmService osmService, SimulationRunner SimulationRunner)
        {
            _db = db;
            _osmService = osmService;
            _runner = SimulationRunner;
        }

        // Helper to get logged-in user
        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        [HttpPost("create")]
        public async Task<IActionResult> CreateSimulation([FromBody] CreateSimulationDto dto)
        {
            var userId = GetUserId();

            // Step 1: Fetch or reuse OSM locations
            var locations = await _osmService.FetchLocationsAsync(dto.CenterLat, dto.CenterLon, dto.RadiusKm);

            if (!locations.Any())
                return BadRequest("No locations found in selected area");

            var sim = new Simulation
            {
                UserId = userId,
                Status = "Created",
                CreatedAt = DateTime.UtcNow,
                Name = dto.Name
            };

            _db.Simulations.Add(sim);
            await _db.SaveChangesAsync();

            // Step 2: Assign locations to this simulation
            foreach (var loc in locations)
            {
                loc.SimulationId = sim.Id;
                _db.Locations.Add(loc);
            }
            await _db.SaveChangesAsync();

            // Step 3: Generate agents
            var homes = locations.Where(l => l.Type == "Home").ToList();
            var works = locations.Where(l => l.Type == "Work" || l.Type == "School" || l.Type == "Restaurant").ToList();

            var rnd = new Random();
            for (int i = 0; i < dto.NumAgents; i++)
            {
                var home = homes[rnd.Next(homes.Count)];
                var work = works[rnd.Next(works.Count)];

                var agent = new Agent
                {
                    SimulationId = sim.Id,
                    Name = $"Agent{i}",
                    HomeLocationId = home.Id,
                    WorkLocationId = work.Id,
                    CurrentLat = home.Lat,
                    CurrentLon = home.Lon
                };

                _db.Agents.Add(agent);
            }

            await _db.SaveChangesAsync();

            return Ok(new { sim.Id, sim.Name, Agents = dto.NumAgents });
        }

        [HttpPost("{id}/start")]
        public IActionResult StartSimulation(int id)
        {
            var sim = _db.Simulations
                .Include(s => s.Agents)
                .Include(s => s.Locations)
                .FirstOrDefault(s => s.Id == id);

            if (sim == null) return NotFound();

            var engine = new SimulationEngine.SimulationEngine(sim);
            _runner.EnqueueSimulation(engine);

            return Ok(new { Status = "Queued/Running" });
        }


        // DELETE /api/simulations/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSimulation(int id)
        {
            var userId = GetUserId();

            // Load simulation with related entities, async as mght take a while for large simulations and so shouldnt block thread
            var sim = await _db.Simulations
                .Include(s => s.Agents)
                .Include(s => s.Locations)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (sim == null)
                return NotFound("Simulation not found or you do not have permission");

            // Remove related entities first (if cascade delete not configured)
            _db.Agents.RemoveRange(sim.Agents);
            _db.Locations.RemoveRange(sim.Locations);

            // Remove the simulation
            _db.Simulations.Remove(sim);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Simulation '{sim.Name}' deleted successfully" });
        }

    }
}