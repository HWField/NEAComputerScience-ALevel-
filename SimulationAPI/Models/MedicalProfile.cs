using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySimulationAPI.Models
{
    public class MedicalProfile
    {
        public int Id { get; set; }
        public int AgentId { get; set; }
        public Agent Agent { get; set; } = null!;
        public double Immunity { get; set; }
        public bool HasChronicCondition { get; set; }
        public double InfectionSusceptibility { get; set; }
    }
}