using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class Location
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public Simulation Simulation { get; set; } = null!;

        public string OSMId { get; set; } = null!;
        public string Type { get; set; } = null!; // Home, Work, School, Restaurant, etc.
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}