using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class MedicalProfile
    {
        public int Id { get; set; }
        Agent Agent { get; set; } = null!;
        public double Immunity { get; set; } // e.g., 0-100%
        public bool HasChronicCondition { get; set; }
        public double InfectionSusceptibility { get; set; } // combined factor for disease
    }
}