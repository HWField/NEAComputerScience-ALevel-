namespace MySimulationAPI.Models
{
    public class SimulationSnapshot
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public Simulation Simulation { get; set; } = null!;
        public long Tick { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<AgentSnapshot> Agents { get; set; } = new List<AgentSnapshot>();
        public ICollection<BuildingSnapshot> Buildings { get; set; } = new List<BuildingSnapshot>();
    }

    public class AgentSnapshot
    {
        public int Id { get; set; }
        public int AgentId { get; set; }
        public Agent Agent { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string HealthState { get; set; } = "Healthy";
        public double InfectionProgress { get; set; } = 0.0;
        public bool IsQuarantined { get; set; } = false;
        public int? CurrentBuildingId { get; set; }
        public Building? CurrentBuilding { get; set; }
        public int SimulationSnapshotId { get; set; }
        public SimulationSnapshot Snapshot { get; set; } = null!;
    }

    public class BuildingSnapshot
    {
        public int Id { get; set; }
        public int BuildingId { get; set; }
        public Building Building { get; set; } = null!;
        public int CurrentOccupancy { get; set; }
        public int SimulationSnapshotId { get; set; }
        public SimulationSnapshot Snapshot { get; set; } = null!;
    }
}