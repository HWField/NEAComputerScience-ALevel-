using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySimulationAPI.Models
{

    public class SimulationMetric
    {
        public int Id { get; set; }
        public int SimulationId { get; set; }
        public Simulation Simulation { get; set; } = null!;
        public long Tick { get; set; }
        public int TotalAgents { get; set; }
        public int Infected { get; set; }
        public int Recovered { get; set; }
        public int Dead { get; set; }
        public int Quarantined { get; set; }
    }
}