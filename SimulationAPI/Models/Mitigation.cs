using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MySimulationAPI.Models
{
    public abstract class Mitigation
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Type { get; set; } = null!; // "SelfIsolation", "MaskWearing", "Vaccination"

        public int SimulationId { get; set; }
        public Simulation? Simulation { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }

    public class SelfIsolationMitigation : Mitigation
    {
        /// <summary>
        /// Percentage of symptomatic agents that will comply with self-isolation (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double ComplianceRate { get; set; } = 0.7;

        /// <summary>
        /// Minimum symptom severity (0.0 to 1.0) required for agent to self-isolate
        /// </summary>
        [Range(0.0, 1.0)]
        public double SymptomThreshold { get; set; } = 0.3;

        public SelfIsolationMitigation()
        {
            Type = "SelfIsolation";
            Name = "Self-Isolation Protocol";
        }
    }

    public class MaskWearingMitigation : Mitigation
    {
        /// <summary>
        /// Percentage of population that will wear masks (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double AdoptionRate { get; set; } = 0.6;

        /// <summary>
        /// Reduction in transmission probability when mask is worn (0.0 to 1.0)
        /// Uses disease's MaskEffectiveness as baseline, modified by proper usage
        /// </summary>
        [Range(0.0, 1.0)]
        public double EffectivenessModifier { get; set; } = 1.0;

        public MaskWearingMitigation()
        {
            Type = "MaskWearing";
            Name = "Mask Wearing Policy";
        }
    }

    public class VaccinationMitigation : Mitigation
    {
        /// <summary>
        /// Percentage of population that will get vaccinated (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double VaccinationRate { get; set; } = 0.7;

        /// <summary>
        /// Boost to immunity for vaccinated agents (0.0 to 1.0)
        /// Uses disease's VaccineEffectiveness as baseline
        /// </summary>
        [Range(0.0, 1.0)]
        public double EffectivenessModifier { get; set; } = 1.0;

        /// <summary>
        /// Days required for vaccine to reach full effectiveness
        /// </summary>
        public int DaysToFullEffectiveness { get; set; } = 14;

        public VaccinationMitigation()
        {
            Type = "Vaccination";
            Name = "Vaccination Campaign";
        }
    }
}