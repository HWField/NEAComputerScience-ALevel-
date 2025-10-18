using System;
using System.Collections.Generic;
using System.Linq;
using MySimulationAPI.Models;
using MySimulationAPI.SimulationConfiguration;

namespace MySimulationAPI.Services
{
    public class AgentService
    {
        private readonly SimulationContext _context;
        private Random _rng;

        public AgentService(SimulationContext context)
        {
            _context = context;
            _rng = _context.Rng;
        }

        public List<RoutineEvent> GenerateAgentRoutine(Agent agent)
        {
            var events = new List<RoutineEvent>();
            var rng = _context.Rng;
            var buildingService = _context.BuildingService;
            var routingService = _context.RoutingService;
            // local location tracker used whilst calcuating routes
            (double curLat, double curLon) = (agent.House.Latitude, agent.House.Longitude);

            if (agent.House == null)
                throw new InvalidOperationException("Agent must have a home assigned.");

            // ------------------------------
            // Local helper: add a routine event
            // ------------------------------
            void AddRoutineEvent(DayOfWeek day, TimeOnly start, TimeSpan duration, RoutineEventType type, Building target)
            {
                var (path, travelTime) = routingService.ComputeRoute(
       curLat, curLon,
       target.Latitude, target.Longitude
   );

                events.Add(new RoutineEvent
                {
                    DayIndex = day,
                    StartTime = start,
                    Duration = duration,
                    EventType = type,
                    Building = target,
                    CachedTravelDuration = travelTime,
                    CachedPath = path
                });


                // Move agent location so that next route is calculated from here
                curLat = target.Latitude;
                curLon = target.Longitude;
            }

            // ------------------------------
            // Local helper: random daily leisure location
            // ------------------------------
            Building PickLeisureOrHome()
            {
                var leisure = buildingService.GetRandomBuilding<LeisureBuilding>(rng);
                return (Building)leisure ?? agent.House;
            }

            // ------------------------------
            // STUDENT
            // ------------------------------
            if (agent is StudentAgent)
            {
                for (int day = 0; day <= 4; day++)
                {
                    if (agent.Workplace == null) continue;

                    var schoolStart = new TimeOnly(8, 30).AddMinutes(rng.Next(-15, 16));
                    AddRoutineEvent((DayOfWeek)day, schoolStart, TimeSpan.FromHours(7), RoutineEventType.School, agent.Workplace);

                    // Return home
                    var homeStart = schoolStart.AddHours(7).AddMinutes(rng.Next(0, 31));
                    AddRoutineEvent((DayOfWeek)day, homeStart, TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
                }

                // Weekends
                for (int day = 5; day <= 6; day++)
                {
                    if (rng.NextDouble() < 0.7)
                    {
                        var leisureStart = new TimeOnly(10, 0).AddMinutes(rng.Next(-60, 61));
                        var leisureDur = TimeSpan.FromHours(rng.Next(2, 5));
                        var target = PickLeisureOrHome();
                        AddRoutineEvent((DayOfWeek)day, leisureStart, leisureDur, RoutineEventType.Leisure, target);
                    }

                    AddRoutineEvent((DayOfWeek)day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            // ------------------------------
            // OFFICE WORKER
            // ------------------------------
            if (agent is OfficeWorkerAgent)
            {
                for (int day = 0; day <= 4; day++)
                {
                    if (agent.Workplace == null) continue;

                    var workStart = new TimeOnly(8, 0).AddMinutes(rng.Next(-30, 61));
                    AddRoutineEvent((DayOfWeek)day, workStart, TimeSpan.FromHours(rng.Next(8, 10)), RoutineEventType.Work, agent.Workplace);

                    // Lunch break (30%)
                    if (rng.NextDouble() < 0.3)
                    {
                        var lunchStart = workStart.AddHours(4);
                        var restaurant = buildingService.GetRandomBuilding<RetailBuilding>(rng) ?? agent.Workplace;
                        AddRoutineEvent((DayOfWeek)day, lunchStart, TimeSpan.FromHours(1), RoutineEventType.Other, restaurant);
                    }

                    // Return home
                    var endTime = workStart.AddHours(rng.Next(8, 10));
                    AddRoutineEvent((DayOfWeek)day, endTime, TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
                }

                // Weekends
                for (int day = 5; day <= 6; day++)
                {
                    if (rng.NextDouble() < 0.6)
                    {
                        var start = new TimeOnly(10, 0).AddMinutes(rng.Next(-60, 121));
                        Building dest;
                        RoutineEventType type;

                        if (rng.NextDouble() < 0.4)
                        {
                            dest = (Building)buildingService.GetRandomBuilding<RetailBuilding>(rng) ?? agent.House;
                            type = RoutineEventType.Shopping;
                        }
                        else
                        {
                            dest = (Building)buildingService.GetRandomBuilding<LeisureBuilding>(rng) ?? agent.House;
                            type = RoutineEventType.Leisure;
                        }

                        AddRoutineEvent((DayOfWeek)day, start, TimeSpan.FromHours(rng.Next(2, 5)), type, dest);
                    }

                    AddRoutineEvent((DayOfWeek)day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            // ------------------------------
            // MEDICAL WORKER
            // ------------------------------
            if (agent is MedicalWorkerAgent)
            {
                for (int day = 0; day <= 6; day++)
                {
                    if (agent.Workplace == null) continue;

                    double roll = rng.NextDouble();

                    if (roll < 0.5)
                        AddRoutineEvent((DayOfWeek)day, new TimeOnly(7, 0), TimeSpan.FromHours(12), RoutineEventType.Work, agent.Workplace);
                    else if (roll < 0.8)
                        AddRoutineEvent((DayOfWeek)day, new TimeOnly(19, 0), TimeSpan.FromHours(12), RoutineEventType.Work, agent.Workplace);
                    else
                        AddRoutineEvent((DayOfWeek)day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            // ------------------------------
            // RETAIL WORKER
            // ------------------------------
            if (agent is RetailWorkerAgent)
            {
                for (int day = 0; day <= 6; day++)
                {
                    if (agent.Workplace == null) continue;

                    if (rng.NextDouble() < 0.85)
                    {
                        var startHour = rng.Next(6, 12);
                        var dur = TimeSpan.FromHours(rng.Next(6, 9));
                        AddRoutineEvent((DayOfWeek)day, new TimeOnly(startHour, 0), dur, RoutineEventType.Work, agent.Workplace);
                    }

                    AddRoutineEvent((DayOfWeek)day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            // ------------------------------
            // SERVICE WORKER (Hospitality, Leisure staff)
            // ------------------------------
            if (agent is ServiceWorkerAgent)
            {
                for (int day = 0; day <= 6; day++)
                {
                    if (agent.Workplace == null) continue;

                    if (rng.NextDouble() < 0.8)
                    {
                        var startHour = rng.Next(10, 16); // later shifts
                        AddRoutineEvent((DayOfWeek)day, new TimeOnly(startHour, 0), TimeSpan.FromHours(8), RoutineEventType.Work, agent.Workplace);
                    }

                    AddRoutineEvent((DayOfWeek)day, new TimeOnly(23, 0), TimeSpan.FromHours(8), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            // ------------------------------
            // ELDERLY / RETIRED
            // ------------------------------
            if (agent is ElderlyRetiredAgent)
            {
                for (int day = 0; day <= 6; day++)
                {
                    if (rng.NextDouble() < 0.5)
                    {
                        var start = new TimeOnly(9, 0).AddMinutes(rng.Next(-30, 61));
                        var shop = (Building)buildingService.GetRandomBuilding<RetailBuilding>(rng) ?? agent.House;
                        AddRoutineEvent((DayOfWeek)day, start, TimeSpan.FromHours(1.5), RoutineEventType.Shopping, shop);
                    }
                    else if (rng.NextDouble() < 0.3)
                    {
                        var leisure = PickLeisureOrHome();
                        AddRoutineEvent((DayOfWeek)day, new TimeOnly(14, 0), TimeSpan.FromHours(2), RoutineEventType.Leisure, leisure);
                    }

                    AddRoutineEvent((DayOfWeek)day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            return events;
        }
        public MedicalProfile GenerateMedicalProfile(Agent agent)
        {
            double baseImmunity;
            double chronicConditionChance;
            double baseSusceptibility;

            // --- Defaults (used as catch for unspecific agent types) ---
            baseImmunity = 0.7;
            chronicConditionChance = 0.1;
            baseSusceptibility = 0.3;

            // --- Type-specific overrides ---
            switch (agent)
            {
                case StudentAgent:
                    baseImmunity = 0.85;              // younger, stronger immune response
                    chronicConditionChance = 0.03;    // low chance
                    baseSusceptibility = 0.25;        // low exposure risk
                    break;

                case OfficeWorkerAgent:
                    baseImmunity = 0.7;               // average
                    chronicConditionChance = 0.1;
                    baseSusceptibility = 0.35;        // moderate risk from indoor office exposure
                    break;

                case MedicalWorkerAgent:
                    baseImmunity = 0.75;              // trained / possibly vaccinated
                    chronicConditionChance = 0.08;
                    baseSusceptibility = 0.5;         // high exposure risk to infected patients
                    break;

                case RetailWorkerAgent:
                    baseImmunity = 0.7;
                    chronicConditionChance = 0.1;
                    baseSusceptibility = 0.45;        // frequent public contact
                    break;

                case ServiceWorkerAgent:
                    baseImmunity = 0.68;
                    chronicConditionChance = 0.1;
                    baseSusceptibility = 0.4;         // medium-high exposure (restaurants, leisure)
                    break;

                case ElderlyRetiredAgent:
                    baseImmunity = 0.4;               // weaker immune systems
                    chronicConditionChance = 0.4;     // much higher rate
                    baseSusceptibility = 0.6;         // more vulnerable to infection
                    break;
            }

            // --- Random variation (introduce diversity) ---
            double immunityVariation = 0.15;         // 
            double susceptibilityVariation = 0.15;   // 

            double finalImmunity = Math.Clamp(
                baseImmunity + (_rng.NextDouble() * 2 - 1) * immunityVariation,
                0.05, 1.0);

            bool hasChronicCondition = _rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (_rng.NextDouble() * 2 - 1) * susceptibilityVariation,
                0.05, 1.0);

            // --- Adjust for chronic conditions ---
            if (hasChronicCondition)
            {
                finalImmunity = Math.Max(0.05, finalImmunity * 0.8);          // 20% weaker immunity
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% more vulnerable
            }

            // --- Create profile ---
            return new MedicalProfile
            {
                AgentId = agent.Id,
                Immunity = Math.Round(finalImmunity, 3),
                HasChronicCondition = hasChronicCondition,
                InfectionSusceptibility = Math.Round(finalSusceptibility, 3)
            };
        }


        public void UpdateAgentsForTick()
        {
            var tick = _context.Simulation.CurrentTick;
            int tickMinutes = _context.Simulation.TickLengthMinutes;

            // Get current simulated time
            var (dayIndex, timeOfDay) = Utils.SimulationTimeHelper.GetSimulationTime(tick, tickMinutes);
            var currentDay = (DayOfWeek)(((int)DayOfWeek.Monday + dayIndex) % 7);

            foreach (var agent in _context.Agents)
            {
                var routineToday = agent.RoutineEvents
                    .Where(r => r.DayIndex == currentDay)
                    .OrderBy(r => r.StartTime)
                    .ToList();

                if (routineToday.Count == 0) continue;

                RoutineEvent? currentEvent = null;

                foreach (var e in routineToday)
                {
                    var depart = e.GetDepartureTime();
                    var end = e.StartTime.Add(e.Duration);

                    bool active = timeOfDay >= depart && timeOfDay < end;
                    if (active)
                    {
                        currentEvent = e;
                        break;
                    }
                }

                if (currentEvent == null)
                {
                    agent.CurrentBuilding = agent.House;
                    agent.CurrentLat = agent.House.Latitude;
                    agent.CurrentLon = agent.House.Longitude;
                    continue;
                }

                // --- Travel interpolation ---
                var departTime = currentEvent.GetDepartureTime();
                var travelEnd = currentEvent.StartTime;
                var totalTravelSeconds = currentEvent.CachedTravelDuration.TotalSeconds;

                if (timeOfDay >= departTime && timeOfDay < travelEnd && currentEvent.CachedPath != null)
                {
                    double travelProgress = (timeOfDay.ToTimeSpan() - departTime.ToTimeSpan()).TotalSeconds / totalTravelSeconds;
                    travelProgress = Math.Clamp(travelProgress, 0, 1);

                    int segmentCount = currentEvent.CachedPath.Count - 1;
                    if (segmentCount > 0)
                    {
                        double segmentPos = travelProgress * segmentCount;
                        int i = (int)Math.Floor(segmentPos);
                        double frac = segmentPos - i;

                        if (i < segmentCount)
                        {
                            var (lat1, lon1) = currentEvent.CachedPath[i];
                            var (lat2, lon2) = currentEvent.CachedPath[i + 1];
                            agent.CurrentLat = lat1 + (lat2 - lat1) * frac;
                            agent.CurrentLon = lon1 + (lon2 - lon1) * frac;
                        }
                    }

                    agent.CurrentBuilding = null;
                }
                else
                {
                    agent.CurrentBuilding = currentEvent.Building;
                    agent.CurrentLat = currentEvent.Building.Latitude;
                    agent.CurrentLon = currentEvent.Building.Longitude;
                }
            }
        }
    }
}