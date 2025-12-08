namespace MySimulationAPI.DTOs
{
    public class DemoCreateSimulationDto
    {
        public required string SimulationName { get; set; }
        // For creating new preset from OSM data
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }

        public int? PresetId { get; set; }
        public string? PresetName { get; set; }
        public int AgentCount { get; set; }

    }

    public class InitialiseSimulationDto
    {
        public string SimulationName { get; set; }
    }

    public class SetupMapAreaDto
    {
        public int? PresetId { get; set; }
        // For custom map areas
        public string? PresetName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }
        public bool? SavePreset { get; set; } = false;
    }

    public class SetupPopulationDto
    {
        public int StudentCount { get; set; }
        public int OfficeWorker { get; set; }
        public int MedicalWorker { get; set; }
        public int ElderlyRetiredAgent { get; set; }
        public int RetailWorker { get; set; }
        public int ServiceWorker { get; set; }
    }

    public class SetupDiseaseDto
    {
        public int? PresetId { get; set; }
        public string? Name { get; set; } = "Default Disease";
        public double? InfectionRate { get; set; } = 0.1;
        public double? RecoveryRate { get; set; } = 0.05;
        public double? MortalityRate { get; set; } = 0.01;

        // Disease Timeline
        public double? IncubationPeriod { get; set; } = 3.0;
        public double? InfectiousPeriod { get; set; } = 10.0;

        // Symptoms & Reporting
        public double? SymptomaticRate { get; set; } = 0.7;
        public double? SevereSymptomsRate { get; set; } = 0.15;
        public double? HospitalSeekingRate { get; set; } = 0.5;

        // Natural Immunity
        public double? ImmunityDuration { get; set; } = 180;
        public double? ReinfectionResistance { get; set; } = 0.7;

        // Intervention Effectiveness
        public double? MaskEffectiveness { get; set; } = 0.5;
        public double? VaccineEffectiveness { get; set; } = 0.8;

        public bool SavePreset { get; set; } = false;
    }
}