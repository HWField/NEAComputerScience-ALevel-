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

        public string Status { get; set; } = "Healthy";


        // One-to-one relationship with MedicalProfile
        [InverseProperty("Agent")]
        public virtual MedicalProfile MedicalProfile { get; set; } = null!;

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
                // Find all events for the current day
                var todaysEvents = RoutineEvents
                    .Where(e => e.DayIndex == (DayOfWeek)dayIndex)
                    .OrderBy(e => e.StartTime)
                    .ToList();

                // Find the next event after current time
                CurrentEvent = todaysEvents
                    .FirstOrDefault(e => e.StartTime >= timeOfDay);

                if (CurrentEvent != null)
                {
                    NextEventTick = currentTick +
                        SimulationTimeHelper.ConvertTimeSpanToTicks(CurrentEvent.Duration, Simulation.TickLengthMinutes);
                }
            }
        }

        public abstract List<RoutineEvent> GenerateRoutine(RoutingService routingService, BuildingService buildingService);
        public abstract MedicalProfile GenerateMedicalProfile(Random rng);

        public string GenerateRandomName(Random _rng)
        {
            string[] first = { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda" };
            string[] last = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
            return $"{first[_rng.Next(first.Length)]} {last[_rng.Next(last.Length)]}";
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
        private static readonly Random _rnd = new();

        public override List<RoutineEvent> GenerateRoutine(RoutingService routingService, BuildingService buildingService)
        {
            var events = new List<RoutineEvent>();
            int mapId = Simulation.MapArea.Id;

            // ---- Weekdays: Monday (0) - Friday (4) ----
            for (int day = 0; day <= 4; day++)
            {
                var schoolStart = new TimeOnly(8, 30).AddMinutes(_rnd.Next(-10, 11));
                var schoolDuration = TimeSpan.FromHours(7);
                var (cachedPath, travelDuration) = routingService.ComputeRoute(
                    mapId, House.Latitude, House.Longitude, Workplace.Latitude, Workplace.Longitude);

                var schoolEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = schoolStart,
                    Duration = schoolDuration,
                    EventType = RoutineEventType.School,
                    Building = Workplace,
                    CachedTravelDuration = travelDuration,
                    CachedPath = cachedPath
                };
                events.Add(schoolEvent);

                var homeStart = schoolStart.Add(schoolDuration);
                var homeDuration = TimeSpan.FromHours(8);

                var (homePathcachedPath, homeTravelDuration) = routingService.ComputeRoute(
                    mapId, Workplace.Latitude, Workplace.Longitude, House.Latitude, House.Longitude);

                var homeEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = homeStart,
                    Duration = homeDuration,
                    EventType = RoutineEventType.Home,
                    Building = House,
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
                    buildingService.GetBuildingFromTypeAndTravelTime(BuildingType.Recreation, House.Latitude, House.Longitude, 30) : House;

                var (leisurePath, leisureTravelTime) = isGoingOut ?
                    routingService.ComputeRoute(mapId, House.Latitude, House.Longitude,
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
                    Building = House,
                    CachedTravelDuration = TimeSpan.Zero
                };
                events.Add(homeEvent);
            }

            return events;
        }

        public override MedicalProfile GenerateMedicalProfile(Random rng)
        {
            // Randomize medical traits based on agent type
            double baseImmunity = 0.7; // Base immunity level (70%)
            double chronicConditionChance = 0.1; // 10% chance for chronic condition
            double baseSusceptibility = 0.3; // Base susceptibility (30%)


            baseImmunity = 0.8; // Students have higher immunity
            chronicConditionChance = 0.05; // Lower chance of chronic conditions

            // Add some randomness
            double immunityVariation = 0.2; // +/- 20% variation
            double susceptibilityVariation = 0.2; // +/- 20% variation

            // Calculate final values with randomness
            double finalImmunity = Math.Clamp(
                baseImmunity + (rng.NextDouble() * immunityVariation * 2 - immunityVariation),
                0.1, 1.0); // Between 10% and 100%

            bool hasChronicCondition = rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (rng.NextDouble() * susceptibilityVariation * 2 - susceptibilityVariation),
                0.1, 1.0); // Between 10% and 100%

            // Increase susceptibility if they have a chronic condition
            if (hasChronicCondition)
            {
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% increase, capped at 100%
            }

            // Create and return the medical profile
            return new MedicalProfile
            {
                Agent = this,
                Immunity = finalImmunity,
                HasChronicCondition = hasChronicCondition,
                InfectionSusceptibility = finalSusceptibility
            };

        }
    }

    public class OfficeWorkerAgent : Agent
    {
        private static readonly Random _rnd = new();

        public override List<RoutineEvent> GenerateRoutine(RoutingService routingService, BuildingService buildingService)
        {
            var events = new List<RoutineEvent>();
            int mapId = Simulation.MapArea.Id;

            // ---- Weekdays: Monday (0) - Friday (4) ----
            for (int day = 0; day <= 4; day++)
            {
                var workStart = new TimeOnly(8, 0).AddMinutes(_rnd.Next(-30, 61)); // 7:30-9:00 AM
                var workDuration = TimeSpan.FromHours(_rnd.Next(8, 10)); // 8-9 hour workday

                var (workPath, workTravelTime) = routingService.ComputeRoute(
                    mapId, House.Latitude, House.Longitude, Workplace.Latitude, Workplace.Longitude);

                var workEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = workStart,
                    Duration = workDuration,
                    EventType = RoutineEventType.Work,
                    Building = Workplace,
                    CachedTravelDuration = workTravelTime,
                    CachedPath = workPath
                };
                events.Add(workEvent);

                // Lunch break (30% chance)
                if (_rnd.NextDouble() < 0.3)
                {
                    var lunchStart = workStart.AddHours(4);
                    var restaurantBuilding = buildingService.GetBuildingFromTypeAndTravelTime(BuildingType.Hospitality,
                        Workplace.Latitude, Workplace.Longitude, 10);

                    if (restaurantBuilding != null)
                    {
                        var (lunchPath, lunchTravelTime) = routingService.ComputeRoute(
                            mapId, Workplace.Latitude, Workplace.Longitude,
                            restaurantBuilding.Latitude, restaurantBuilding.Longitude);

                        var lunchEvent = new RoutineEvent
                        {
                            DayIndex = (DayOfWeek)day,
                            StartTime = lunchStart,
                            Duration = TimeSpan.FromHours(1),
                            EventType = RoutineEventType.Other,
                            Building = restaurantBuilding,
                            CachedTravelDuration = lunchTravelTime,
                            CachedPath = lunchPath
                        };
                        events.Add(lunchEvent);
                    }
                }

                // Evening - return home
                var homeStart = workStart.Add(workDuration);
                var (homePath, homeTravelTime) = routingService.ComputeRoute(
                    mapId, Workplace.Latitude, Workplace.Longitude, House.Latitude, House.Longitude);

                var homeEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = homeStart,
                    Duration = TimeSpan.FromHours(12),
                    EventType = RoutineEventType.Home,
                    Building = House,
                    CachedTravelDuration = homeTravelTime,
                    CachedPath = homePath
                };
                events.Add(homeEvent);
            }

            // ---- Weekends: Shopping, leisure ----
            int[] weekendDays = { 5, 6 };
            foreach (var day in weekendDays)
            {
                // Weekend activities
                if (_rnd.NextDouble() < 0.6) // 60% chance of going out
                {
                    var activityStart = new TimeOnly(10, 0).AddMinutes(_rnd.Next(-60, 121));
                    Building? destination = null;
                    RoutineEventType eventType = RoutineEventType.Leisure;

                    // Choose activity type
                    var activityChoice = _rnd.NextDouble();
                    if (activityChoice < 0.4) // Shopping
                    {
                        destination = buildingService.GetBuildingFromTypeAndTravelTime(BuildingType.Retail, House.Latitude, House.Longitude, 60);
                        eventType = RoutineEventType.Shopping;
                    }
                    else // Leisure
                    {
                        destination = buildingService.GetBuildingFromTypeAndTravelTime(BuildingType.Recreation, House.Latitude, House.Longitude, 60);
                        eventType = RoutineEventType.Leisure;
                    }

                    if (destination != null)
                    {
                        var (activityPath, activityTravelTime) = routingService.ComputeRoute(
                            mapId, House.Latitude, House.Longitude, destination.Latitude, destination.Longitude);

                        var activityEvent = new RoutineEvent
                        {
                            DayIndex = (DayOfWeek)day,
                            StartTime = activityStart,
                            Duration = TimeSpan.FromHours(_rnd.Next(2, 5)),
                            EventType = eventType,
                            Building = destination,
                            CachedTravelDuration = activityTravelTime,
                            CachedPath = activityPath
                        };
                        events.Add(activityEvent);
                    }
                }

                // Rest of day at home
                var homeStart = new TimeOnly(18, 0);
                var homeEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = homeStart,
                    Duration = TimeSpan.FromHours(12),
                    EventType = RoutineEventType.Home,
                    Building = House,
                    CachedTravelDuration = TimeSpan.Zero
                };
                events.Add(homeEvent);
            }

            return events;
        }

        public override MedicalProfile GenerateMedicalProfile(Random rng)
        {
            // Randomize medical traits based on agent type
            double baseImmunity = 0.7; // Base immunity level (70%)
            double chronicConditionChance = 0.1; // 10% chance for chronic condition
            double baseSusceptibility = 0.3; // Base susceptibility (30%)

            // Add some randomness
            double immunityVariation = 0.2; // +/- 20% variation
            double susceptibilityVariation = 0.2; // +/- 20% variation

            // Calculate final values with randomness
            double finalImmunity = Math.Clamp(
                baseImmunity + (rng.NextDouble() * immunityVariation * 2 - immunityVariation),
                0.1, 1.0); // Between 10% and 100%

            bool hasChronicCondition = rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (rng.NextDouble() * susceptibilityVariation * 2 - susceptibilityVariation),
                0.1, 1.0); // Between 10% and 100%

            // Increase susceptibility if they have a chronic condition
            if (hasChronicCondition)
            {
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% increase, capped at 100%
            }

            // Create and return the medical profile
            return new MedicalProfile
            {
                Agent = this,
                Immunity = finalImmunity,
                HasChronicCondition = hasChronicCondition,
                InfectionSusceptibility = finalSusceptibility
            };

        }
    }

    public class MedicalWorkerAgent : Agent
    {
        private static readonly Random _rnd = new();

        public override List<RoutineEvent> GenerateRoutine(RoutingService routingService, BuildingService buildingService)
        {
            var events = new List<RoutineEvent>();
            int mapId = Simulation.MapArea.Id;

            // Medical workers often have irregular schedules, shift work
            for (int day = 0; day < 7; day++)
            {
                var shiftType = _rnd.NextDouble();

                if (shiftType < 0.6) // Day shift (60%)
                {
                    var workStart = new TimeOnly(7, 0).AddMinutes(_rnd.Next(-30, 61));
                    var workDuration = TimeSpan.FromHours(12); // 12-hour shifts common in healthcare

                    var (workPath, workTravelTime) = routingService.ComputeRoute(
                        mapId, House.Latitude, House.Longitude, Workplace.Latitude, Workplace.Longitude);

                    var workEvent = new RoutineEvent
                    {
                        DayIndex = (DayOfWeek)day,
                        StartTime = workStart,
                        Duration = workDuration,
                        EventType = RoutineEventType.Work,
                        Building = Workplace,
                        CachedTravelDuration = workTravelTime,
                        CachedPath = workPath
                    };
                    events.Add(workEvent);
                }
                else if (shiftType < 0.8) // Night shift (20%)
                {
                    var workStart = new TimeOnly(19, 0).AddMinutes(_rnd.Next(-30, 61));
                    var workDuration = TimeSpan.FromHours(12);

                    var (workPath, workTravelTime) = routingService.ComputeRoute(
                        mapId, House.Latitude, House.Longitude, Workplace.Latitude, Workplace.Longitude);

                    var workEvent = new RoutineEvent
                    {
                        DayIndex = (DayOfWeek)day,
                        StartTime = workStart,
                        Duration = workDuration,
                        EventType = RoutineEventType.Work,
                        Building = Workplace,
                        CachedTravelDuration = workTravelTime,
                        CachedPath = workPath
                    };
                    events.Add(workEvent);
                }
                // 20% chance of day off

                // Home time when not working
                var homeEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = new TimeOnly(0, 0),
                    Duration = TimeSpan.FromHours(24),
                    EventType = RoutineEventType.Home,
                    Building = House,
                    CachedTravelDuration = TimeSpan.Zero
                };
                events.Add(homeEvent);
            }

            return events;
        }

        public override MedicalProfile GenerateMedicalProfile(Random rng)
        {
            // Randomize medical traits based on agent type
            double baseImmunity = 0.7; // Base immunity level (70%)
            double chronicConditionChance = 0.1; // 10% chance for chronic condition
            double baseSusceptibility = 0.3; // Base susceptibility (30%)

            // Adjust parameters based on agent type

            baseImmunity = 0.75; // Medical workers have slightly higher immunity
            baseSusceptibility = 0.4; // But higher exposure risk


            // Add some randomness
            double immunityVariation = 0.2; // +/- 20% variation
            double susceptibilityVariation = 0.2; // +/- 20% variation

            // Calculate final values with randomness
            double finalImmunity = Math.Clamp(
                baseImmunity + (rng.NextDouble() * immunityVariation * 2 - immunityVariation),
                0.1, 1.0); // Between 10% and 100%

            bool hasChronicCondition = rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (rng.NextDouble() * susceptibilityVariation * 2 - susceptibilityVariation),
                0.1, 1.0); // Between 10% and 100%

            // Increase susceptibility if they have a chronic condition
            if (hasChronicCondition)
            {
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% increase, capped at 100%
            }

            // Create and return the medical profile
            return new MedicalProfile
            {
                Agent = this,
                Immunity = finalImmunity,
                HasChronicCondition = hasChronicCondition,
                InfectionSusceptibility = finalSusceptibility
            };

        }
    }

    public class RetailWorkerAgent : Agent
    {
        private static readonly Random _rnd = new();

        public override List<RoutineEvent> GenerateRoutine(RoutingService routingService, BuildingService buildingService)
        {
            var events = new List<RoutineEvent>();
            int mapId = Simulation.MapArea.Id;

            // Retail workers often work weekends, varied shifts
            for (int day = 0; day < 7; day++)
            {
                // 85% chance of working any given day (retail is demanding)
                if (_rnd.NextDouble() < 0.85)
                {
                    var isWeekend = day == 5 || day == 6;
                    var shiftChoice = _rnd.NextDouble();

                    TimeOnly workStart;
                    TimeSpan workDuration;

                    if (shiftChoice < 0.4) // Morning shift
                    {
                        workStart = new TimeOnly(8, 0).AddMinutes(_rnd.Next(-30, 31));
                        workDuration = TimeSpan.FromHours(8);
                    }
                    else if (shiftChoice < 0.8) // Afternoon shift
                    {
                        workStart = new TimeOnly(13, 0).AddMinutes(_rnd.Next(-30, 31));
                        workDuration = TimeSpan.FromHours(8);
                    }
                    else // Evening shift
                    {
                        workStart = new TimeOnly(17, 0).AddMinutes(_rnd.Next(-30, 31));
                        workDuration = TimeSpan.FromHours(6);
                    }

                    var (workPath, workTravelTime) = routingService.ComputeRoute(
                        mapId, House.Latitude, House.Longitude, Workplace.Latitude, Workplace.Longitude);

                    var workEvent = new RoutineEvent
                    {
                        DayIndex = (DayOfWeek)day,
                        StartTime = workStart,
                        Duration = workDuration,
                        EventType = RoutineEventType.Work,
                        Building = Workplace,
                        CachedTravelDuration = workTravelTime,
                        CachedPath = workPath
                    };
                    events.Add(workEvent);

                    // Return home after work
                    var homeStart = workStart.Add(workDuration);
                    var (homePath, homeTravelTime) = routingService.ComputeRoute(
                        mapId, Workplace.Latitude, Workplace.Longitude, House.Latitude, House.Longitude);

                    var homeEvent = new RoutineEvent
                    {
                        DayIndex = (DayOfWeek)day,
                        StartTime = homeStart,
                        Duration = TimeSpan.FromHours(12),
                        EventType = RoutineEventType.Home,
                        Building = House,
                        CachedTravelDuration = homeTravelTime,
                        CachedPath = homePath
                    };
                    events.Add(homeEvent);
                }
            }

            return events;
        }

        public override MedicalProfile GenerateMedicalProfile(Random rng)
        {
            // Randomize medical traits based on agent type
            double baseImmunity = 0.7; // Base immunity level (70%)
            double chronicConditionChance = 0.1; // 10% chance for chronic condition
            double baseSusceptibility = 0.3; // Base susceptibility (30%)

            // Add some randomness
            double immunityVariation = 0.2; // +/- 20% variation
            double susceptibilityVariation = 0.2; // +/- 20% variation

            // Calculate final values with randomness
            double finalImmunity = Math.Clamp(
                baseImmunity + (rng.NextDouble() * immunityVariation * 2 - immunityVariation),
                0.1, 1.0); // Between 10% and 100%

            bool hasChronicCondition = rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (rng.NextDouble() * susceptibilityVariation * 2 - susceptibilityVariation),
                0.1, 1.0); // Between 10% and 100%

            // Increase susceptibility if they have a chronic condition
            if (hasChronicCondition)
            {
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% increase, capped at 100%
            }

            // Create and return the medical profile
            return new MedicalProfile
            {
                Agent = this,
                Immunity = finalImmunity,
                HasChronicCondition = hasChronicCondition,
                InfectionSusceptibility = finalSusceptibility
            };

        }
    }

    public class ElderlyRetiredAgent : Agent
    {
        private static readonly Random _rnd = new();

        public override List<RoutineEvent> GenerateRoutine(RoutingService routingService, BuildingService buildingService)
        {
            var events = new List<RoutineEvent>();
            int mapId = Simulation.MapArea.Id;

            for (int day = 0; day < 7; day++)
            {
                // Elderly have more regular, slower-paced routines
                // Morning grocery shopping (3 days a week)
                if (day % 2 == 0 && _rnd.NextDouble() < 0.7)
                {
                    var shopStart = new TimeOnly(9, 0).AddMinutes(_rnd.Next(-30, 61));
                    var grocery = buildingService.GetBuildingFromTypeAndTravelTime(BuildingType.Retail,
                        House.Latitude, House.Longitude, 75);

                    if (grocery != null)
                    {
                        var (shopPath, shopTravelTime) = routingService.ComputeRoute(
                            mapId, House.Latitude, House.Longitude, grocery.Latitude, grocery.Longitude);

                        var shopEvent = new RoutineEvent
                        {
                            DayIndex = (DayOfWeek)day,
                            StartTime = shopStart,
                            Duration = TimeSpan.FromHours(1.5),
                            EventType = RoutineEventType.Shopping,
                            Building = grocery,
                            CachedTravelDuration = shopTravelTime,
                            CachedPath = shopPath
                        };
                        events.Add(shopEvent);
                    }
                }

                // Medical appointments (once per week)
                if (day == 2 && _rnd.NextDouble() < 0.4)
                {
                    var medStart = new TimeOnly(14, 0).AddMinutes(_rnd.Next(-60, 61));
                    var clinic = buildingService.GetBuildingFromTypeAndTravelTime(BuildingType.Medical,
                        House.Latitude, House.Longitude, 120);

                    if (clinic != null)
                    {
                        var (medPath, medTravelTime) = routingService.ComputeRoute(
                            mapId, House.Latitude, House.Longitude, clinic.Latitude, clinic.Longitude);

                        var medEvent = new RoutineEvent
                        {
                            DayIndex = (DayOfWeek)day,
                            StartTime = medStart,
                            Duration = TimeSpan.FromHours(2),
                            EventType = RoutineEventType.Medical,
                            Building = clinic,
                            CachedTravelDuration = medTravelTime,
                            CachedPath = medPath
                        };
                        events.Add(medEvent);
                    }
                }

                // Social activities (weekends)
                if ((day == 5 || day == 6) && _rnd.NextDouble() < 0.5)
                {
                    var socialStart = new TimeOnly(11, 0).AddMinutes(_rnd.Next(-60, 61));
                    var leisureBuilding = buildingService.GetBuildingFromTypeAndTravelTime(BuildingType.Recreation, House.Latitude, House.Longitude, 60);

                    if (leisureBuilding != null)
                    {
                        var (socialPath, socialTravelTime) = routingService.ComputeRoute(
                            mapId, House.Latitude, House.Longitude, leisureBuilding.Latitude, leisureBuilding.Longitude);

                        var socialEvent = new RoutineEvent
                        {
                            DayIndex = (DayOfWeek)day,
                            StartTime = socialStart,
                            Duration = TimeSpan.FromHours(3),
                            EventType = RoutineEventType.Leisure,
                            Building = leisureBuilding,
                            CachedTravelDuration = socialTravelTime,
                            CachedPath = socialPath
                        };
                        events.Add(socialEvent);
                    }
                }

                // Most time at home
                var homeEvent = new RoutineEvent
                {
                    DayIndex = (DayOfWeek)day,
                    StartTime = new TimeOnly(18, 0),
                    Duration = TimeSpan.FromHours(12),
                    EventType = RoutineEventType.Home,
                    Building = House,
                    CachedTravelDuration = TimeSpan.Zero
                };
                events.Add(homeEvent);
            }

            return events;
        }

        public override MedicalProfile GenerateMedicalProfile(Random rng)
        {
            // Randomize medical traits based on agent type
            double baseImmunity = 0.7; // Base immunity level (70%)
            double chronicConditionChance = 0.1; // 10% chance for chronic condition
            double baseSusceptibility = 0.3; // Base susceptibility (30%)

            baseImmunity = 0.5; // Elderly have lower immunity
            chronicConditionChance = 0.4; // Higher chance of chronic conditions
            baseSusceptibility = 0.6; // More susceptible to infections


            // Add some randomness
            double immunityVariation = 0.2; // +/- 20% variation
            double susceptibilityVariation = 0.2; // +/- 20% variation

            // Calculate final values with randomness
            double finalImmunity = Math.Clamp(
                baseImmunity + (rng.NextDouble() * immunityVariation * 2 - immunityVariation),
                0.1, 1.0); // Between 10% and 100%

            bool hasChronicCondition = rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (rng.NextDouble() * susceptibilityVariation * 2 - susceptibilityVariation),
                0.1, 1.0); // Between 10% and 100%

            // Increase susceptibility if they have a chronic condition
            if (hasChronicCondition)
            {
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% increase, capped at 100%
            }

            // Create and return the medical profile
            return new MedicalProfile
            {
                Agent = this,
                Immunity = finalImmunity,
                HasChronicCondition = hasChronicCondition,
                InfectionSusceptibility = finalSusceptibility
            };

        }
    }

    public class ServiceWorkerAgent : Agent
    {
        private static readonly Random _rnd = new();

        public override List<RoutineEvent> GenerateRoutine(RoutingService routingService, BuildingService buildingService)
        {
            var events = new List<RoutineEvent>();
            int mapId = Simulation.MapArea.Id;

            // Service workers often work irregular hours, weekends
            for (int day = 0; day < 7; day++)
            {
                if (_rnd.NextDouble() < 0.8) // 80% chance of working
                {
                    var shiftChoice = _rnd.NextDouble();
                    TimeOnly workStart;
                    TimeSpan workDuration;

                    if (shiftChoice < 0.3) // Breakfast/lunch shift
                    {
                        workStart = new TimeOnly(6, 0).AddMinutes(_rnd.Next(0, 61));
                        workDuration = TimeSpan.FromHours(8);
                    }
                    else if (shiftChoice < 0.7) // Lunch/dinner shift
                    {
                        workStart = new TimeOnly(11, 0).AddMinutes(_rnd.Next(-30, 61));
                        workDuration = TimeSpan.FromHours(9);
                    }
                    else // Evening/night shift
                    {
                        workStart = new TimeOnly(17, 0).AddMinutes(_rnd.Next(-30, 61));
                        workDuration = TimeSpan.FromHours(8);
                    }

                    var (workPath, workTravelTime) = routingService.ComputeRoute(
                        mapId, House.Latitude, House.Longitude, Workplace.Latitude, Workplace.Longitude);

                    var workEvent = new RoutineEvent
                    {
                        DayIndex = (DayOfWeek)day,
                        StartTime = workStart,
                        Duration = workDuration,
                        EventType = RoutineEventType.Work,
                        Building = Workplace,
                        CachedTravelDuration = workTravelTime,
                        CachedPath = workPath
                    };
                    events.Add(workEvent);
                }
            }

            return events;
        }

        public override MedicalProfile GenerateMedicalProfile(Random rng)
        {
            // Randomize medical traits based on agent type
            double baseImmunity = 0.7; // Base immunity level (70%)
            double chronicConditionChance = 0.1; // 10% chance for chronic condition
            double baseSusceptibility = 0.3; // Base susceptibility (30%)

            // Add some randomness
            double immunityVariation = 0.2; // +/- 20% variation
            double susceptibilityVariation = 0.2; // +/- 20% variation

            // Calculate final values with randomness
            double finalImmunity = Math.Clamp(
                baseImmunity + (rng.NextDouble() * immunityVariation * 2 - immunityVariation),
                0.1, 1.0); // Between 10% and 100%

            bool hasChronicCondition = rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (rng.NextDouble() * susceptibilityVariation * 2 - susceptibilityVariation),
                0.1, 1.0); // Between 10% and 100%

            // Increase susceptibility if they have a chronic condition
            if (hasChronicCondition)
            {
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% increase, capped at 100%
            }

            // Create and return the medical profile
            return new MedicalProfile
            {
                Agent = this,
                Immunity = finalImmunity,
                HasChronicCondition = hasChronicCondition,
                InfectionSusceptibility = finalSusceptibility
            };

        }
    }


}