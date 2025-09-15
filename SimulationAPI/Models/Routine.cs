using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class Routine
    {
        public int Id { get; set; }
        public List<TimeSlot> TimeSlots { get; set; } = new();
    }

    public class TimeSlot
    {
        public int Id { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public Building? Target { get; set; }
    }

}