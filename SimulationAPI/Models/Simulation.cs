using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class Simulation
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Status { get; set; } = "Created";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Agent> Agents { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }
}