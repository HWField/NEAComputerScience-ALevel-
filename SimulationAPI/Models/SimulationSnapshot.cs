using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class SimulationSnapshot
    {
        public int Tick { get; set; }
        public int HealthyCount { get; set; }
        public int InfectedCount { get; set; }
        public int RecoveredCount { get; set; }
    }
}