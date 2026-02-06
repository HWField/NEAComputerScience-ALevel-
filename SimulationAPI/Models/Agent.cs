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
    }

    public static class AgentTypeRegistry
    {
        private static readonly Dictionary<Type, byte> _codes = new();
        private static byte _nextCode = 1;

        public static byte GetTypeCode(Type type)
        {
            if (_codes.TryGetValue(type, out var code)) return code;
            code = _nextCode++;
            _codes[type] = code;
            return code;
        }
    }

    // blank initialisation of agent types to ensure they are registered in the AgentTypeRegistry and have consistent discriminator values in the database enabling TPH inheritance mapping in Entity Framework Core.
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