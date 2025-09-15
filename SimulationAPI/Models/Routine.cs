using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySimulationAPI.Models
{
    public class RoutineEvent
    {
        public int Id { get; set; }
        public int AgentId { get; set; }
        public Agent Agent { get; set; } = null!;
        /// <summary>
        /// The tick when the event should nominally start.
        /// </summary>
        public DayOfWeek DayIndex { get; set; }
        public TimeOnly StartTime { get; set; }

        /// <summary>
        /// Duration of the activity in ticks (excluding travel).
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Target building for the activity.
        /// </summary>
        public Building Building { get; set; } = null!;

        public RoutineEventType EventType { get; set; }

        // ---- Simulation-only ----
        [NotMapped]
        public List<(double Lat, double Lon)>? CachedPath { get; set; }

        [NotMapped]
        public TimeSpan CachedTravelDuration { get; set; }

        /// <summary>
        /// Returns the tick when the agent should actually depart from previous location,
        /// factoring in travel time.
        /// </summary>
        public TimeOnly GetDepartureTime()
        {
            return StartTime.Add(-CachedTravelDuration); // add the negatie of travel time which effectively sutracts the time
        }
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