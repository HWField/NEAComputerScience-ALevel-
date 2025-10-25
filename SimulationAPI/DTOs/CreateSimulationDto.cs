namespace MySimulationAPI.DTOs
{
    public class DemoCreateSimulationDto
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

    public class InitialiseSimulationDto
    {
        public string SimulationName { get; set; }
    }

    public class SetupMapAreaDto
    {
        public int? PresetId { get; set; }
        // For custom map areas
        public string? PresetName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }
    }

    public class SetupPopulationDto
    {
        public int AgentCount { get; set; }
    }

    public class SetupDiseaseDto
    {
        public int? PresetId { get; set; }
        public string? Name { get; set; } = "Default Disease";
        public double? InfectionRate { get; set; } = 0.1;
        public double? RecoveryRate { get; set; } = 0.05;
        public double? MortalityRate { get; set; } = 0.01;
        public bool SavePreset { get; set; } = false;
    }
}