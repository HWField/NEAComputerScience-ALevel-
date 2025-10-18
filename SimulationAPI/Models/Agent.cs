using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using MySimulationAPI.Services;
using MySimulationAPI.Utils;

namespace MySimulationAPI.Models
{
    public abstract class Agent
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public Simulation? Simulation { get; set; }
        public string Name { get; set; }
        public int? HouseId { get; set; }
        public ResidentialBuilding? House { get; set; }
        public int? WorkplaceId { get; set; }
        public Building? Workplace { get; set; }
        public List<RoutineEvent> RoutineEvents { get; set; } = new();

        public MedicalProfile MedicalProfile { get; set; } = null!;
        //Transient simulation state (not stored in DB)
        [NotMapped]
        public string Status { get; set; } = "Healthy";

        [NotMapped]
        public double InfectionProgress { get; set; } = 0.0;
        [NotMapped]
        public string HealthState;

        [NotMapped]
        public bool IsQuarantined;

        [NotMapped]
        public double CurrentLat { get; set; }

        [NotMapped]
        public double CurrentLon { get; set; }

        [NotMapped]
        public int CurrentBuildingId { get; set; }
        public Building CurrentBuilding { get; set; }


    }

    public class StudentAgent : Agent
    {
    }

    public class OfficeWorkerAgent : Agent
    {
    }

    public class MedicalWorkerAgent : Agent
    {

    }

    public class RetailWorkerAgent : Agent
    {

    }

    public class ElderlyRetiredAgent : Agent
    {

    }

    public class ServiceWorkerAgent : Agent
    {

    }


}