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

        [NotMapped]
        public long CurrentTick { get; set; } = 0;
        public int TickLengthMinutes { get; set; } = 5; // default 5 min per tick
        public MapArea MapArea { get; set; } = null!;
        public List<Agent> Agents { get; set; } = new();
        public Disease Disease { get; set; } = null!;
        public List<Mitigation> Mitigations { get; set; } = new();
        public List<SimulationSnapshot> SimulationSnapshots { get; set; } = new();
    }
}