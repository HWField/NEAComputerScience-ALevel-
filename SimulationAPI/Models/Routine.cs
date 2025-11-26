using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySimulationAPI.Models
{
    public class RoutineEvent
    {
        public int Id { get; set; }
        public int AgentId { get; set; }
        public Agent Agent { get; set; } = null!;
        public int DayIndex { get; set; } // 0 = Monday, 6 = Sunday
        public TimeOnly StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int BuildingId { get; set; }
        public Building Building { get; set; } = null!;
        public RoutineEventType EventType { get; set; }
        public List<(double Lat, double Lon)>? CachedPath { get; set; }
        public int CachedTravelDuration { get; set; } //stored in seconds
    }
    public enum RoutineEventType
    {
        Work,
        School,
        Leisure,
        Shopping,
        Medical,
        Home,
        Other
    }

}