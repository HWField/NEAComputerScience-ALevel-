using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // only admins can access these endpoints
    public class AdminController : ControllerBase
    {
        private readonly SimDbContext _db;

        public AdminController(SimDbContext db)
        {
            _db = db;
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
    }
}