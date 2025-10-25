using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using MySimulationAPI.DTOs;
using System.Security.Claims;
using SimulationAPI.Migrations;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // require JWT on all routes
    public class PresetController : ControllerBase
    {
        private readonly SimDbContext _db;
        public PresetController(SimDbContext db)
        {
            _db = db;
        }

        // Helper to get logged-in user
        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        // Helper to check if user is admin
        private bool IsAdmin() => User.IsInRole("Admin");

        // MapArea routes:
        /// <summary>
        /// Gets all Map Area Presets (for logged in user or all if admin).
        /// </summary>
        [HttpGet("mapareas")]
        public async Task<IActionResult> GetMapAreas()
        {
            IQueryable<MapAreaPreset> query = _db.MapAreaPresets
             .Include(p => p.Buildings)
             .Include(p => p.Paths)
             .AsSplitQuery();

            if (!IsAdmin())
            {
                query = query.Where(p => p.UserId == null || p.UserId == GetUserId());
            }
            var sims = await query.ToListAsync();
            return Ok(sims.Select(s => new
            {
                s.Id,
                s.Name,
                s.Latitude,
                s.Longitude,
                s.Radius,
                BuildingCount = s.Buildings?.Count ?? 0,
                PathCount = s.Paths?.Count ?? 0
            }));
        }

        /// <summary>
        /// Gets a specific MapArea Presets.
        /// </summary>
        [HttpGet("mapareas/{id}")]
        public async Task<IActionResult> GetMapArea(int id)
        {
            var mapAreaPreset = await _db.MapAreaPresets
               .FirstOrDefaultAsync(p => p.Id == id);
            if (mapAreaPreset == null) return NotFound();
            if (mapAreaPreset.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            return Ok(mapAreaPreset);
        }
        // no post route as it is done via Simulation setup

        /// <summary>
        ///  Updates a MapArea Preset.
        /// </summary>
        /// <returns></returns>
        [HttpPut("mapareas/{id}")]
        public async Task<IActionResult> UpdateMapArea(int id, [FromBody] UpdateMapAreaPresetDto dto)
        {
            var mapAreaPreset = await _db.MapAreaPresets
               .FirstOrDefaultAsync(p => p.Id == id);
            if (mapAreaPreset == null) return NotFound();
            if (mapAreaPreset.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            mapAreaPreset.Name = dto.Name ?? mapAreaPreset.Name;
            mapAreaPreset.Latitude = dto.Latitude ?? mapAreaPreset.Latitude;
            mapAreaPreset.Longitude = dto.Longitude ?? mapAreaPreset.Longitude;
            mapAreaPreset.Radius = dto.Radius ?? mapAreaPreset.Radius;
            _db.MapAreaPresets.Update(mapAreaPreset);
            await _db.SaveChangesAsync();
            return Ok(mapAreaPreset);
        }

        /// <summary>
        /// Deletes a MapArea Preset.
        /// </summary>
        [HttpDelete("mapareas/{id}")]
        public async Task<IActionResult> DeleteMapArea(int id)
        {
            var mapAreaPreset = await _db.MapAreaPresets.FindAsync(id);
            if (mapAreaPreset == null) return NotFound();
            if (mapAreaPreset.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            _db.MapAreaPresets.Remove(mapAreaPreset);
            await _db.SaveChangesAsync();
            return NoContent();
        }



        // Disease routes:
        /// <summary>
        /// Gets all Disease Presets (for logged in user or all if admin).
        /// </summary>
        [HttpGet("diseases")]
        public async Task<IActionResult> GetDiseases()
        {
            IQueryable<DiseasePreset> query = _db.DiseasePresets;
            if (!IsAdmin())
            {
                query = query.Where(p => p.UserId == null || p.UserId == GetUserId());
            }
            var sims = await query.ToListAsync();
            return Ok(sims.Select(s => new
            {
                s.Id,
                s.Name,
                s.InfectionRate,
                s.RecoveryRate,
                s.MortalityRate,
            }));
        }

        /// <summary>
        /// Gets a specific Disease Presets.
        /// </summary>
        [HttpGet("diseases/{id}")]
        public async Task<IActionResult> GetDisease(int id)
        {
            var diseasePreset = await _db.DiseasePresets
               .FirstOrDefaultAsync(p => p.Id == id);
            if (diseasePreset == null) return NotFound();
            if (diseasePreset.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            return Ok(diseasePreset);
        }
        // no post route as it is done via Simulation setup

        /// <summary>
        ///  Updates a Disease Preset.
        /// </summary>
        [HttpPut("diseases/{id}")]
        public async Task<IActionResult> UpdateDisease(int id, [FromBody] UpdateDiseasePresetDto dto)
        {
            var diseasePreset = await _db.DiseasePresets
               .FirstOrDefaultAsync(p => p.Id == id);
            if (diseasePreset == null) return NotFound();
            if (diseasePreset.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            diseasePreset.Name = dto.Name ?? diseasePreset.Name;
            diseasePreset.InfectionRate = dto.InfectionRate ?? diseasePreset.InfectionRate;
            diseasePreset.RecoveryRate = dto.RecoveryRate ?? diseasePreset.RecoveryRate;
            diseasePreset.MortalityRate = dto.MortalityRate ?? diseasePreset.MortalityRate;
            _db.DiseasePresets.Update(diseasePreset);
            await _db.SaveChangesAsync();
            return Ok(diseasePreset);
        }

        /// <summary>
        /// Deletes a Disease Preset.
        /// </summary>
        [HttpDelete("diseases/{id}")]
        public async Task<IActionResult> DeleteDisease(int id)
        {
            var diseasePreset = await _db.DiseasePresets.FindAsync(id);
            if (diseasePreset == null) return NotFound();
            if (diseasePreset.UserId != GetUserId() && !IsAdmin())
                return Forbid();
            _db.DiseasePresets.Remove(diseasePreset);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}