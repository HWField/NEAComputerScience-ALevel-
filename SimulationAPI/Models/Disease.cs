using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class Disease
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public Simulation Simulation { get; set; } = null!;
        public string Name { get; set; }
        public double InfectionRate { get; set; }
        public double RecoveryRate { get; set; }
        public double MortalityRate { get; set; }
    }
}