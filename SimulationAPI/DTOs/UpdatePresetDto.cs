
namespace MySimulationAPI.DTOs
{
    public class UpdateMapAreaPresetDto
    {
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Radius { get; set; }
    }

    public class UpdateDiseasePresetDto
    {
        public string? Name { get; set; }
        public double? InfectionRate { get; set; }
        public double? RecoveryRate { get; set; }
        public double? MortalityRate { get; set; }
    }
}