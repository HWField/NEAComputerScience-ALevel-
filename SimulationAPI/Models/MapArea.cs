using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class MapArea
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!; // e.g. "London"

        public int PresetRefID { get; set; } // Reference to geographicAreaPreset

        // Boundaries of map area
        public double CenterLat { get; set; }
        public double CenterLon { get; set; }
        public double RadiusKm { get; set; }

        // One-to-one relationship with Simulation
        public int SimulationId { get; set; }
        public Simulation Simulation { get; set; } = null!;

        public List<Building> Buildings { get; set; } = new();
        public List<Path> Paths { get; set; } = new();
    }
}