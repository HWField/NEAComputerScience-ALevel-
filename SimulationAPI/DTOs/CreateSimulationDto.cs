namespace MySimulationAPI.DTOs
{
    public class CreateSimulationDto
    {
        public required string SimulationName { get; set; }
        // For creating new preset from OSM data
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }

        public int? PresetId { get; set; }
        public string? PresetName { get; set; }
        public int AgentCount { get; set; }

    }
}