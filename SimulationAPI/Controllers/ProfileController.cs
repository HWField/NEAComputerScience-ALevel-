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
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly SimDbContext _db;

        public ProfileController(SimDbContext db, OsmService osmService)
        {
            _db = db;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        /// <summary>
        /// Get profile details of the logged-in user
        /// </summary>
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
                user.Role,
                user.Theme,
                user.StreamerMode
            });
        }

        /// <summary>
        /// Update profile details of the logged-in user
        /// </summary>
        [HttpPut]
        public IActionResult UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == GetUserId());

            if (user == null)
                return NotFound("User not found");

            // Update allowed fields only
            if (!string.IsNullOrWhiteSpace(dto.Username))
                user.Username = dto.Username;

            if (!string.IsNullOrWhiteSpace(dto.Theme))
                user.Theme = dto.Theme;

            if (dto.StreamerMode.HasValue)
                user.StreamerMode = dto.StreamerMode.Value;

            _db.SaveChanges();

            return Ok(new { message = "Profile updated successfully" });
        }

        /// <summary>
        /// Update the logged-in user's password
        /// </summary>
        [HttpPost("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == GetUserId());
            if (user == null)
                return NotFound("User not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest("Incorrect current password");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _db.SaveChanges();

            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>
        /// Delete the logged-in user's account
        /// </summary>
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

    }
}