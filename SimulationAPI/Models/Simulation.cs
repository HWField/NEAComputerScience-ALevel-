using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySimulationAPI.Models
{
    public class Simulation
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Status { get; set; } = "Created";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int TickLength { get; set; } = 300; // Number of seconds each tick lasts (default 5 minutes)
        public MapArea MapArea { get; set; } = null!;
        public List<Agent> Agents { get; set; } = new();
        public Disease Disease { get; set; } = null!;
        public List<Mitigation> Mitigations { get; set; } = new();
        public List<SimulationSnapshot> SimulationSnapshots { get; set; } = new();
        public List<SimulationMetric> SimulationMetrics { get; set; } = new();
    }
}