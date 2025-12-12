namespace MySimulationAPI.DTOs
{
    public class UpdateDiseaseDto
    {
        public double? InfectionRate { get; set; }
        public double? RecoveryRate { get; set; }
        public double? MortalityRate { get; set; }
        public double? IncubationPeriod { get; set; }
        public double? InfectiousPeriod { get; set; }
        public double? SymptomaticRate { get; set; }
        public double? SevereSymptomsRate { get; set; }
        public double? HospitalSeekingRate { get; set; }
        public double? ImmunityDuration { get; set; }
        public double? ReinfectionResistance { get; set; }
        public double? MaskEffectiveness { get; set; }
        public double? VaccineEffectiveness { get; set; }
    }

    public class UpdateInitialInfectionDto
    {
        public int Count { get; set; }
    }

    public class UpdateMetricFrequencyDto
    {
        public int Frequency { get; set; }
    }

    public class UpdateSnapshotFrequencyDto
    {
        public int Frequency { get; set; }
    }
}
