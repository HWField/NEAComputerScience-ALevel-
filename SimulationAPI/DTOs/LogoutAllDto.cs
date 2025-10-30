namespace MySimulationAPI.DTOs
{
    public class LogoutAllDto
    {
        public string Username { get; set; } = null!;
    }
    public class LogoutSessionDto
    {
        public int SessionId { get; set; }
    }
}