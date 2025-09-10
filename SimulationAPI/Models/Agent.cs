using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Current absolute position
        public double CurrentLat { get; set; }
        public double CurrentLon { get; set; }

        // ---- Simulation-only (not stored in DB) ----
        [NotMapped]
        public List<(double Lat, double Lon)>? Path { get; set; } // polyline route

        [NotMapped]
        public int CurrentSegmentIndex { get; set; } = 0;

        [NotMapped]
        public double DistanceAlongSegment { get; set; } = 0;

        [NotMapped]
        public double SpeedMetersPerTick { get; set; } = 1.4; // ~5km/h walking default
    }
}