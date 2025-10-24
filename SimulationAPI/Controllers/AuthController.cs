using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.Models;
using MySimulationAPI.DTOs;

namespace MySimulationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SimDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(SimDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // REGISTER
        /// <summary>
        /// Registers a new user with a username and password.
        /// </summary>
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            if (_db.Users.Any(u => u.Username == dto.Username))
                return BadRequest("Username taken");

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "User" // default role
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return Ok(new { user.Id, user.Username });
        }

        /// <summary>
        /// Logins a user and issues JWT access and refresh tokens.
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var user = _db.Users.Include(u => u.RefreshTokens)
                                .FirstOrDefault(u => u.Username == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            // Generate access token
            // var accessToken = GenerateJwt(user, TimeSpan.FromMinutes(5));
            var accessToken = GenerateJwt(user, TimeSpan.FromHours(99999)); // TEMP: long-lived token for testing

            // Generate per-device refresh token
            var refreshToken = Guid.NewGuid().ToString("N");

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                DeviceInfo = dto.DeviceInfo ?? "Unknown",
                CreatedAt = DateTime.UtcNow
            });

            _db.SaveChanges();

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        // REFRESH JWT
        /// <summary>
        /// Refreshes the JWT access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshDto dto)
        {
            var refreshToken = _db.RefreshTokens.Include(rt => rt.User)
                                                .FirstOrDefault(rt => rt.Token == dto.RefreshToken);

            if (refreshToken == null)
                return Unauthorized("Invalid refresh token");

            var user = refreshToken.User;

            // Issue new access token (refresh token remains valid until manually revoked)
            var newAccessToken = GenerateJwt(user, TimeSpan.FromMinutes(5));

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = refreshToken.Token // same token, not rotated
            });
        }

        // LOGOUT DEVICE
        /// <summary>
        /// Revokes a specific refresh token, logging out that device.
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout([FromBody] RefreshDto dto)
        {
            var refreshToken = _db.RefreshTokens.FirstOrDefault(rt => rt.Token == dto.RefreshToken);

            if (refreshToken != null)
            {
                _db.RefreshTokens.Remove(refreshToken);
                _db.SaveChanges();
            }

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Revokes all refresh tokens for a user, logging out from all devices.
        /// </summary>
        [HttpPost("logout-all")]
        public IActionResult LogoutAll([FromBody] LogoutAllDto dto)
        {
            var user = _db.Users.Include(u => u.RefreshTokens)
                                .FirstOrDefault(u => u.Username == dto.Username);

            if (user != null)
            {
                _db.RefreshTokens.RemoveRange(user.RefreshTokens);
                _db.SaveChanges();
            }

            return Ok(new { message = "All devices logged out successfully" });
        }

        // HELPER: Generate JWT with Role Claim
        private string GenerateJwt(User user, TimeSpan expires)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role) // role claim
                }),
                Expires = DateTime.UtcNow.Add(expires),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}