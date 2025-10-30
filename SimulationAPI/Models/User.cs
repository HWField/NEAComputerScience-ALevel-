using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Role { get; set; } = "User";
        public string Theme { get; set; } = "light";
        public bool StreamerMode { get; set; } = false;
        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}