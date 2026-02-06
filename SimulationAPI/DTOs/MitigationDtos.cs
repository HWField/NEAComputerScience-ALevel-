using System.ComponentModel.DataAnnotations;

namespace MySimulationAPI.DTOs
{
    public class UpdateMitigationDto
    {
        [Required]
        public string Type { get; set; } = null!; // "SelfIsolation", "MaskWearing", "Vaccination"
        public string? Name { get; set; }
        public bool? IsActive { get; set; }

        // Self-Isolation parameters
        public double? ComplianceRate { get; set; }
        public double? SymptomThreshold { get; set; }

        // Mask Wearing parameters
        public double? AdoptionRate { get; set; }
        public double? MaskEffectivenessModifier { get; set; }

        // Vaccination parameters
        public double? VaccinationRate { get; set; }
        public double? VaccineEffectivenessModifier { get; set; }
        public int? DaysToFullEffectiveness { get; set; }
    }

    public class BatchUpdateMitigationsDto
    {
        [Required]
        public List<UpdateMitigationDto> Mitigations { get; set; } = new();
    }
}
