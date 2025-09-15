using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class Disease
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double InfectionRate { get; set; }
        public double RecoveryRate { get; set; }
        public double MortalityRate { get; set; }
    }
}