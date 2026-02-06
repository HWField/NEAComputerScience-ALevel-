using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.SimulationConfiguration;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // only admins can access these endpoints
    public class AdminController : ControllerBase
    {
        private readonly SimDbContext _db;
        private readonly SimulationManager _simulationManager;

        public AdminController(SimDbContext db, SimulationManager simulationManager)
        {
            _db = db;
            _simulationManager = simulationManager;
        }

        /// <summary>
        /// Delete all simulations and their related data
        /// </summary>
        [HttpDelete("simulations")]
        public async Task<IActionResult> DeleteAllSimulations()
        {
            var simulationCount = await _db.Simulations.CountAsync();

            if (simulationCount == 0)
            {
                return Ok(new { message = "No simulations to delete", deletedCount = 0 });
            }
            await _db.Simulations.ExecuteDeleteAsync();
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "All simulations and related data deleted successfully",
                deletedSimulations = simulationCount
            });
        }

        /// <summary>
        /// Delete all (non default) presets and their related data
        /// </summary>
        [HttpDelete("presets")]
        public async Task<IActionResult> DeleteAllPresets()
        {
            await _db.MapAreaPresets
                     .Where(p => p.UserId != null)
                     .ExecuteDeleteAsync();

            await _db.DiseasePresets
              .Where(p => p.UserId != null)
              .ExecuteDeleteAsync();

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "All (non default) presets and related data deleted successfully",
            });
        }

        /// <summary>
        /// Get database statistics 
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDatabaseStats()
        {
            try
            {
                var stats = new
                {
                    Simulations = await _db.Simulations.CountAsync(),
                    Snapshots = new
                    {
                        Snapshots = await _db.SimulationSnapshots.CountAsync(),
                        AgentSnapshots = await _db.AgentSnapshots.CountAsync(),
                        BuildingSnapshots = await _db.BuildingSnapshots.CountAsync()
                    },
                    Metrics = await _db.SimulationMetrics.CountAsync(),
                    Agents = await _db.Agents.CountAsync(),
                    MapAreas = await _db.MapAreas.CountAsync(),
                    Buildings = await _db.Buildings.CountAsync(),
                    Paths = await _db.Paths.CountAsync(),
                    Diseases = await _db.Diseases.CountAsync(),
                    Mitigations = await _db.Mitigations.CountAsync(),
                    MedicalProfiles = await _db.MedicalProfiles.CountAsync(),
                    Presets = new
                    {
                        MapAreaPresets = await _db.MapAreaPresets.CountAsync(),
                        OSMBuildingPresets = await _db.OSMBuildingPresets.CountAsync(),
                        OSMPathPresets = await _db.OSMPathPresets.CountAsync(),
                        OSMPathNodePresets = await _db.OSMPathNodePresets.CountAsync(),
                        DiseasePresets = await _db.DiseasePresets.CountAsync()
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving database statistics",
                    error = ex.Message
                });
            }
        }


        /// <summary>
        /// Get Users
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _db.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Role,
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving users",
                    error = ex.Message
                });
            }
        }


        /// <summary>
        /// Get Loaded simulations
        /// </summary>
        [HttpGet("loaded-simulations")]
        public async Task<IActionResult> GetLoadedSimulations()
        {
            try
            {
                var loadedContexts = _simulationManager.GetAllLoaded();

                var simulationData = new List<object>();

                foreach (var context in loadedContexts)
                {
                    var owner = await _db.Users
                        .Where(u => u.Id == context.Simulation.UserId)
                        .Select(u => u.Username)
                        .FirstOrDefaultAsync();

                    simulationData.Add(new
                    {
                        Id = context.Simulation.Id,
                        Name = context.Simulation.Name,
                        Status = context.Simulation.Status,
                        OwnerUsername = owner,
                        AgentCount = context.Agents?.Count ?? 0,
                        CurrentTick = context.CurrentTick,
                        Speed = context.SimulationSpeed,
                        TickLength = context.TickLength
                    });
                }

                return Ok(simulationData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error retrieving loaded simulations",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Unload all simulations from memory
        /// </summary>
        [HttpPost("unload-all")]
        public async Task<IActionResult> UnloadAllSimulations()
        {
            try
            {
                var count = _simulationManager.GetAllLoaded().ToList().Count;
                await _simulationManager.RemoveAllAsync();
                return Ok(new
                {
                    message = $"Successfully unloaded {count} simulation(s) from memory",
                    unloadedCount = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error unloading all simulations",
                    error = ex.Message
                });
            }
        }
    }
}