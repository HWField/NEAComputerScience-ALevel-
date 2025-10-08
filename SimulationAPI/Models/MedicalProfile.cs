using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySimulationAPI.Models
{
    public class MedicalProfile
    {
        public int Id { get; set; }

        // Add this explicit foreign key property
        public int AgentId { get; set; }
        [ForeignKey("AgentId")]
        public Agent Agent { get; set; } = null!;
        public double Immunity { get; set; } // e.g., 0-100%
        public bool HasChronicCondition { get; set; }
        public double InfectionSusceptibility { get; set; } // combined factor for disease
    }
}