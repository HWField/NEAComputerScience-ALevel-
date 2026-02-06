using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class Disease
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public Simulation Simulation { get; set; } = null!;

        public string Name { get; set; } = null!;
        public double InfectionRate { get; set; }
        public double RecoveryRate { get; set; }
        public double MortalityRate { get; set; }

        // Disease Timeline
        public double IncubationPeriod { get; set; } = 3.0;     // Days before becoming infectious (presymptomatic)
        public double InfectiousPeriod { get; set; } = 10.0;    // Days of peak infectiousness

        // Symptom & Reporting
        public double SymptomaticRate { get; set; } = 0.7;      // % who develop noticeable symptoms
        public double SevereSymptomsRate { get; set; } = 0.15;  // % who develop severe symptoms
        public double HospitalSeekingRate { get; set; } = 0.5;  // % of severe cases seeking medical help

        // Natural Immunity
        public double ImmunityDuration { get; set; } = 180;     // Days before immunity decreases (default 6 months)
        public double ReinfectionResistance { get; set; } = 0.7; // Reduction in susceptibility after recovery

        // Intervention Effectiveness
        public double MaskEffectiveness { get; set; } = 0.5;
        public double VaccineEffectiveness { get; set; } = 0.8;
    }
}