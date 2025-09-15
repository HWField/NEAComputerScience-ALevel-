using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class MapArea
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!; // e.g. "London"
        public bool Preset { get; set; } = false; // "Preset" or "Custom"

        // Boundaries
        public double CenterLat { get; set; }
        public double CenterLon { get; set; }
        public double RadiusKm { get; set; }

        public List<Building> Buildings { get; set; } = new();
        public List<Path> Paths { get; set; } = new();
    }
}