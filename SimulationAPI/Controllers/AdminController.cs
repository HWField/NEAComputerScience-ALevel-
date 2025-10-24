using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly SimDbContext _context;

        public AdminController(SimDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Delete all simulations and their related data (agents, diseases, mitigations, map areas, buildings, paths)
        /// </summary>
        [HttpDelete("simulations/all")]
        public async Task<IActionResult> DeleteAllSimulations()
        {
            try
            {
                var simulationCount = await _context.Simulations.CountAsync();

                if (simulationCount == 0)
                {
                    return Ok(new { message = "No simulations to delete", deletedCount = 0 });
                }

                await _context.Simulations.ExecuteDeleteAsync();

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "All simulations and related data deleted successfully",
                    deletedSimulations = simulationCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error deleting simulations",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete all presets and their related data (buildings, paths, path nodes)
        /// </summary>
        [HttpDelete("presets/all")]
        public async Task<IActionResult> DeleteAllPresets()
        {
            try
            {
                var presetCount = await _context.MapAreaPresets.CountAsync();

                if (presetCount == 0)
                {
                    return Ok(new { message = "No presets to delete", deletedCount = 0 });
                }


                await _context.MapAreaPresets.ExecuteDeleteAsync();

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "All presets and related data deleted successfully",
                    deletedPresets = presetCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error deleting presets",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get database statistics (counts of various entities)
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDatabaseStats()
        {
            try
            {
                var stats = new
                {
                    Simulations = await _context.Simulations.CountAsync(),
                    Snapshots = new
                    {
                        Snapshots = await _context.SimulationSnapshots.CountAsync(),
                        AgentSnapshots = await _context.AgentSnapshots.CountAsync(),
                        BuildingSnapshots = await _context.BuildingSnapshots.CountAsync()
                    },
                    Agents = await _context.Agents.CountAsync(),
                    MapAreas = await _context.MapAreas.CountAsync(),
                    Buildings = await _context.Buildings.CountAsync(),
                    Paths = await _context.Paths.CountAsync(),
                    Diseases = await _context.Diseases.CountAsync(),
                    Mitigations = await _context.Mitigations.CountAsync(),
                    MedicalProfiles = await _context.MedicalProfiles.CountAsync(),
                    Presets = new
                    {
                        MapAreaPresets = await _context.MapAreaPresets.CountAsync(),
                        OSMBuildingPresets = await _context.OSMBuildingPresets.CountAsync(),
                        OSMPathPresets = await _context.OSMPathPresets.CountAsync(),
                        OSMPathNodePresets = await _context.OSMPathNodePresets.CountAsync(),
                        DiseasePresets = await _context.DiseasePresets.CountAsync()
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
        /// Delete a specific simulation by ID (and all its related data)
        /// </summary>
        [HttpDelete("simulations/{id}")]
        public async Task<IActionResult> DeleteSimulation(int id)
        {
            try
            {
                var simulation = await _context.Simulations.FindAsync(id);

                if (simulation == null)
                {
                    return NotFound(new { message = $"Simulation with ID {id} not found" });
                }

                _context.Simulations.Remove(simulation);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Simulation {id} and all related data deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Error deleting simulation {id}",
                    error = ex.Message
                });
            }
        }


        /// <summary>
        /// Delete a set of simulationSnapshots by by simulation ID (essentially resetting simulation to fresh)
        /// </summary>
        [HttpDelete("SimulationSnapshots/{id}")]
        public async Task<IActionResult> DeleteSimulationSnapshots(int id)
        {
            try
            {

                var simulationSnapshots = await _context.SimulationSnapshots
                    .Where(s => s.SimulationId == id)
                    .ToListAsync();

                if (simulationSnapshots.Count == 0)
                {
                    return NotFound(new { message = $"No SimulationSnapshots found for Simulation ID {id}" });
                }

                _context.SimulationSnapshots.RemoveRange(simulationSnapshots);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Simulation {id} Snapshots and all related data deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Error deleting Snapshots with Simulation ID: {id}",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a specific preset by ID (and all its related data)
        /// </summary>
        [HttpDelete("presets/{id}")]
        public async Task<IActionResult> DeletePreset(int id)
        {
            try
            {
                var preset = await _context.MapAreaPresets.FindAsync(id);

                if (preset == null)
                {
                    return NotFound(new { message = $"Preset with ID {id} not found" });
                }

                _context.MapAreaPresets.Remove(preset);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Preset {id} and all related data deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Error deleting preset {id}",
                    error = ex.Message
                });
            }
        }
    }
}