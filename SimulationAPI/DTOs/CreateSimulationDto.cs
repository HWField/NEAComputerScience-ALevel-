namespace MySimulationAPI.DTOs
{
    public class CreateSimulationDto
    {
        public string Name { get; set; } = null!;
        public double CenterLat { get; set; }
        public double CenterLon { get; set; }
        public double RadiusKm { get; set; }
        public int NumAgents { get; set; }
    }
}