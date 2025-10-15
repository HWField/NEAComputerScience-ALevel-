using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.Services;
using MySimulationAPI.DTOs;
using System.Security.Claims;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // require JWT
    public class ProfileController : ControllerBase
    {
        private readonly SimDbContext _db;
        private readonly OsmService _osmService;

        public ProfileController(SimDbContext db, OsmService osmService)
        {
            _db = db;
            _osmService = osmService;
        }

        // Helper to get logged-in user
        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // GET /api/profile
        [HttpGet]
        public IActionResult GetProfile()
        {
            var user = _db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == GetUserId());

            if (user == null)
                return NotFound("User not found");

            // Return only non-sensitive fields
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Role
            });
        }

        // PUT /api/profile
        [HttpPut]
        public IActionResult UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == GetUserId());

            if (user == null)
                return NotFound("User not found");

            // Update allowed fields only
            if (!string.IsNullOrWhiteSpace(dto.Username))
                user.Username = dto.Username;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            _db.SaveChanges();

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpDelete]
        public IActionResult DeleteProfile()
        {
            var user = _db.Users
                .Include(u => u.RefreshTokens) // remove refresh tokens
                .FirstOrDefault(u => u.Id == GetUserId());

            if (user == null)
                return NotFound("User not found");

            _db.RefreshTokens.RemoveRange(user.RefreshTokens);
            _db.Users.Remove(user);
            _db.SaveChanges();

            return Ok(new { message = "Account deleted successfully" });
        }

        [HttpGet("simulations")]
        public IActionResult GetMySimulations()
        {
            var userId = GetUserId();

            var sims = _db.Simulations
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Status,
                    s.CreatedAt
                })
                .ToList();

            return Ok(sims);
        }
    }
}