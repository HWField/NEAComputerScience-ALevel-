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
        public RoleType Role { get; set; }

        public Building HomeLocation { get; set; } = null!;
        public Building WorkLocation { get; set; } = null!;

        public string Status { get; set; } = "Healthy";

        public MedicalProfile MedicalProfile { get; set; } = null!;

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


    public enum RoleType
    {
        Office,
        Retail,
        Medical,
        Education,
        Retired,
        Unemployed
    }
}