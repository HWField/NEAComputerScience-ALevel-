using System.Collections.Generic;

namespace MySimulationAPI.Models
{

    public class Agent
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public Simulation Simulation { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Type { get; set; } = "Worker";
        public int HomeLocationId { get; set; }
        public Location HomeLocation { get; set; } = null!;
        public int WorkLocationId { get; set; }
        public Location WorkLocation { get; set; } = null!;

        public string HealthStatus { get; set; } = "Healthy";
        public double CurrentLat { get; set; }
        public double CurrentLon { get; set; }
    }
}