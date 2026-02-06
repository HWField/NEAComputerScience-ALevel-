namespace MySimulationAPI.DTOs
{
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class LoginDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string DeviceInfo { get; set; } = null!;
    }
    public class LogoutAllDto
    {
        public string Username { get; set; } = null!;
    }
    public class LogoutSessionDto
    {
        public int SessionId { get; set; }
    }
}