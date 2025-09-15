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
        public Simulation Simulation { get; set; } = null!;

        public string Name { get; set; } = null!;

        public Building Home { get; set; } = null!;

        public abstract void GenerateRoutine(RoutingService routingService, BuildingService buildingService);

        public Dictionary<DayOfWeek, List<RoutineEvent>> WeeklyRoutine { get; set; }
            = new();

        public string Status { get; set; } = "Healthy";

        public MedicalProfile MedicalProfile { get; set; } = null!;

        // Current absolute position
        public double CurrentLat { get; set; }
        public double CurrentLon { get; set; }

        // Simplified simulation state - movement handled by MovementSystem
        [NotMapped]
        public RoutineEvent? CurrentEvent { get; set; }

        [NotMapped] 
        public long NextEventTick { get; set; }

        /// <summary>
        /// Update agent routine state per tick
        /// </summary>
        public void UpdateRoutine(long currentTick)
        {
            var (dayIndex, timeOfDay) = SimulationTimeHelper.GetSimulationTime(currentTick, Simulation.TickLengthMinutes);

            // Check if we need to start a new event
            if (CurrentEvent == null || timeOfDay >= CurrentEvent.StartTime.Add(CurrentEvent.Duration))
            {
                if (WeeklyRoutine.TryGetValue((DayOfWeek)dayIndex, out var events))
                {
                    CurrentEvent = events.FirstOrDefault(e => e.StartTime >= timeOfDay);
                    
                    if (CurrentEvent != null)
                    {
                        NextEventTick = currentTick + SimulationTimeHelper.ConvertTimeSpanToTicks(CurrentEvent.Duration, Simulation.TickLengthMinutes);
                    }
                }
            }
        }

        // Get current target location for movement system
        public (double lat, double lon)? GetCurrentTarget()
        {
            return CurrentEvent?.Building != null ? 
                (CurrentEvent.Building.Latitude, CurrentEvent.Building.Longitude) : null;
        }
    }

    public class StudentAgent : Agent
    {
        public Building School { get; set; } = null!;
        private static readonly Random _rnd = new();

        public override void GenerateRoutine(RoutingService routingService, BuildingService buildingService)
        {
            var events = new List<RoutineEvent>();
            int mapId = Simulation.MapAreaId; // You'll need to add this

            // ---- Weekdays: Monday (0) - Friday (4) ----
            for (int day = 0; day <= 4; day++)
            {
                var schoolStart = new TimeOnly(8, 30).AddMinutes(_rnd.Next(-10, 11));
                var schoolDuration = TimeSpan.FromHours(7);
                
                var (cachedPath, travelDuration) = routingService.ComputeRoute(
                    mapId, Home.Latitude, Home.Longitude, School.Latitude, School.Longitude);
                
                var schoolEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = schoolStart,
                    Duration = schoolDuration,
                    EventType = RoutineEventType.School,
                    Building = School,
                    CachedTravelDuration = travelDuration,
                    CachedPath = cachedPath
                };
                events.Add(schoolEvent);

                var homeStart = schoolStart.Add(schoolDuration);
                var homeDuration = TimeSpan.FromHours(8);

                var (homePathcachedPath, homeTravelDuration) = routingService.ComputeRoute(
                    mapId, School.Latitude, School.Longitude, Home.Latitude, Home.Longitude);
                
                var homeEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = homeStart,
                    Duration = homeDuration,
                    EventType = RoutineEventType.Home,
                    Building = Home,
                    CachedTravelDuration = homeTravelDuration,
                    CachedPath = homePathcachedPath
                };
                events.Add(homeEvent);
            }

            // ---- Weekends ----
            int[] weekendDays = { 5, 6 };
            foreach (var day in weekendDays)
            {
                var leisureStart = new TimeOnly(9, 0).AddMinutes(_rnd.Next(-30, 31));
                var leisureDuration = TimeSpan.FromHours(_rnd.Next(2, 5));

                bool isGoingOut = _rnd.NextDouble() < 0.5;
                Building leisureLocation = isGoingOut ? 
                    buildingService.GetRandomLeisureBuilding(Home, 30) : Home;

                var (leisurePath, leisureTravelTime) = isGoingOut ? 
                    routingService.ComputeRoute(mapId, Home.Latitude, Home.Longitude, 
                        leisureLocation.Latitude, leisureLocation.Longitude) : 
                    (null, TimeSpan.Zero);

                var leisureEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = leisureStart,
                    Duration = leisureDuration,
                    EventType = RoutineEventType.Leisure,
                    Building = leisureLocation,
                    CachedTravelDuration = leisureTravelTime,
                    CachedPath = leisurePath
                };
                events.Add(leisureEvent);

                var homeStart = leisureStart.Add(leisureDuration);
                var homeDuration = TimeSpan.FromHours(12);

                var homeEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = homeStart,
                    Duration = homeDuration,
                    EventType = RoutineEventType.Home,
                    Building = Home,
                    CachedTravelDuration = TimeSpan.Zero
                };
                events.Add(homeEvent);
            }

            // Build WeeklyRoutine dictionary
            WeeklyRoutine = events
                .GroupBy(e => e.DayIndex)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.StartTime).ToList());
        }
    }

    public class OfficeWorkerAgent : Agent
    {
        public Building Workplace { get; set; } = null!; // need to write logic to find a nearby office
    }

    public class RoutineEvent
    {
        public int Id { get; set; }

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