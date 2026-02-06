using System;
using System.Collections.Generic;
using System.Linq;
using MySimulationAPI.Models;
using MySimulationAPI.SimulationConfiguration;
using SimulationRuntime;

namespace MySimulationAPI.Services
{
    public class AgentService
    {
        private readonly SimulationContext _context;
        private Random _rng;

        // Thread local random for parallel routine generation
        [ThreadStatic]
        private static Random? _threadRng;

        public AgentService(SimulationContext context)
        {
            _context = context;
            _rng = _context.Rng;
        }

        public List<RoutineEvent> GenerateAgentRoutine(Agent agent)
        {
            var events = new List<RoutineEvent>();
            // thread-local RNG for parallel safety
            var rng = _threadRng ??= new Random(Guid.NewGuid().GetHashCode());
            var buildingService = _context.BuildingService;
            var routingService = _context.RoutingService;

            if (agent.House == null)
                throw new InvalidOperationException("Agent must have a home assigned.");

            // local location tracker used whilst calcuating routes
            (double curLat, double curLon) = (agent.House.Latitude, agent.House.Longitude);

            void AddRoutineEvent(int day, TimeOnly start, TimeSpan duration, RoutineEventType type, Building target)
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

            // STUDENT
            if (agent is StudentAgent)
            {
                for (int day = 0; day <= 4; day++) // Weekdays
                {
                    if (agent.Workplace != null)
                    {
                        var schoolStart = new TimeOnly(8, 30).AddMinutes(rng.Next(-15, 16));

                        // Morning at home before school
                        if (schoolStart.Hour > 0)
                        {
                            AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(schoolStart.Hour), RoutineEventType.Home, agent.House);
                        }

                        AddRoutineEvent(day, schoolStart, TimeSpan.FromHours(7), RoutineEventType.School, agent.Workplace);

                        // After school activity (40% chance) - leisure or retail
                        var schoolEnd = schoolStart.AddHours(7);
                        if (rng.NextDouble() < 0.4)
                        {
                            var activityStart = schoolEnd.AddMinutes(rng.Next(15, 46));
                            var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                agent.Workplace.Latitude, agent.Workplace.Longitude, k: 3);
                            var target = leisure ?? agent.House;
                            AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(1, 3)), RoutineEventType.Leisure, target);
                        }

                        // Return home for evening
                        var homeStart = schoolEnd.AddMinutes(rng.Next(30, 121));
                        AddRoutineEvent(day, homeStart, TimeSpan.FromHours(10), RoutineEventType.Home, agent.House);
                    }
                    else
                    {
                        // No school - stay home all day
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                    }
                }

                // Weekends, more social activities
                for (int day = 5; day <= 6; day++)
                {
                    var morningHome = new TimeOnly(rng.Next(8, 11), 0);

                    // Morning at home, always add to ensure agent has a starting position
                    AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(morningHome.Hour), RoutineEventType.Home, agent.House);

                    // Daytime activity (80% chance)
                    if (rng.NextDouble() < 0.8)
                    {
                        var activityStart = new TimeOnly(rng.Next(10, 15), rng.Next(0, 60));
                        Building dest;
                        RoutineEventType type;

                        var roll = rng.NextDouble();
                        if (roll < 0.6) // Leisure (sports, cinema, etc)
                        {
                            var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 5);
                            dest = leisure ?? agent.House;
                            type = RoutineEventType.Leisure;
                        }
                        else // Shopping or food
                        {
                            var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 5);
                            dest = retail ?? agent.House;
                            type = RoutineEventType.Shopping;
                        }

                        AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(2, 5)), type, dest);
                    }

                    // Evening at home
                    AddRoutineEvent(day, new TimeOnly(19, 0).AddMinutes(rng.Next(-60, 61)), TimeSpan.FromHours(10), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            // OFFICE WORKER
            if (agent is OfficeWorkerAgent)
            {
                for (int day = 0; day <= 4; day++) // Weekdays
                {
                    if (agent.Workplace != null)
                    {
                        var workStart = new TimeOnly(rng.Next(7, 10), rng.Next(0, 60));
                        var workDuration = rng.Next(8, 10);

                        // Morning at home before work
                        if (workStart.Hour > 0)
                        {
                            AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(workStart.Hour), RoutineEventType.Home, agent.House);
                        }

                        AddRoutineEvent(day, workStart, TimeSpan.FromHours(workDuration), RoutineEventType.Work, agent.Workplace);

                        // Lunch break (50% chance to leave office)
                        if (rng.NextDouble() < 0.5)
                        {
                            var lunchStart = workStart.AddHours(rng.Next(4, 5));
                            var restaurant = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.Workplace.Latitude, agent.Workplace.Longitude, k: 3);
                            if (restaurant != null)
                            {
                                AddRoutineEvent(day, lunchStart, TimeSpan.FromMinutes(rng.Next(30, 61)), RoutineEventType.Other, restaurant);
                            }
                        }

                        // After work activity (30% chance) e.g gym, drinks, shopping
                        var workEnd = workStart.AddHours(workDuration);
                        if (rng.NextDouble() < 0.3)
                        {
                            var activityStart = workEnd.AddMinutes(rng.Next(15, 46));
                            Building dest;
                            RoutineEventType type;

                            if (rng.NextDouble() < 0.6) // Leisure (gym, drinks)
                            {
                                var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                    agent.Workplace.Latitude, agent.Workplace.Longitude, k: 5);
                                dest = leisure ?? agent.House;
                                type = RoutineEventType.Leisure;
                            }
                            else // Quick shopping
                            {
                                var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                    agent.Workplace.Latitude, agent.Workplace.Longitude, k: 5);
                                dest = retail ?? agent.House;
                                type = RoutineEventType.Shopping;
                            }

                            AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(1, 3)), type, dest);
                        }

                        // Return home for evening
                        var homeStart = workEnd.AddMinutes(rng.Next(15, 121));
                        AddRoutineEvent(day, homeStart, TimeSpan.FromHours(10), RoutineEventType.Home, agent.House);
                    }
                    else
                    {
                        // No workplace, stay home all day
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                    }
                }

                // Weekends, errands and leisure
                for (int day = 5; day <= 6; day++)
                {
                    // Morning at home, always add to ensure starting position
                    var morningEnd = new TimeOnly(rng.Next(8, 12), rng.Next(0, 60));
                    AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(morningEnd.Hour), RoutineEventType.Home, agent.House);

                    // Daytime activity (70% chance)
                    if (rng.NextDouble() < 0.7)
                    {
                        var activityStart = new TimeOnly(rng.Next(10, 15), rng.Next(0, 60));
                        Building dest;
                        RoutineEventType type;

                        var roll = rng.NextDouble();
                        if (roll < 0.4) // Shopping (groceries, errands)
                        {
                            var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 5);
                            dest = retail ?? agent.House;
                            type = RoutineEventType.Shopping;
                        }
                        else // Leisure (sports, dining, entertainment)
                        {
                            var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 5);
                            dest = leisure ?? agent.House;
                            type = RoutineEventType.Leisure;
                        }

                        AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(2, 5)), type, dest);
                    }

                    // Evening activity on Saturday (40% chance, dinner, nightlife)
                    if (day == 5 && rng.NextDouble() < 0.4)
                    {
                        var eveningStart = new TimeOnly(rng.Next(18, 21), rng.Next(0, 60));
                        var restaurant = buildingService.FindNearestBuildingByType<RetailBuilding>(
                            agent.House.Latitude, agent.House.Longitude, k: 5);
                        var dest = restaurant ?? agent.House;
                        AddRoutineEvent(day, eveningStart, TimeSpan.FromHours(rng.Next(2, 4)), RoutineEventType.Leisure, dest);
                    }

                    // Evening at home
                    AddRoutineEvent(day, new TimeOnly(20, 0).AddMinutes(rng.Next(-120, 61)), TimeSpan.FromHours(8), RoutineEventType.Home, agent.House);
                }

                return events;
            }

            // MEDICAL WORKER
            if (agent is MedicalWorkerAgent)
            {
                // Medical workers have rotating shifts (day/evening/night)
                for (int day = 0; day <= 6; day++)
                {
                    if (agent.Workplace == null)
                    {
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                        continue;
                    }

                    double shiftRoll = rng.NextDouble();

                    if (shiftRoll < 0.5) // Day shift (50%)
                    {
                        var shiftStart = new TimeOnly(7, 0).AddMinutes(rng.Next(-30, 31));

                        // Morning at home before shift
                        if (shiftStart.Hour > 0)
                        {
                            AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(shiftStart.Hour), RoutineEventType.Home, agent.House);
                        }

                        AddRoutineEvent(day, shiftStart, TimeSpan.FromHours(12), RoutineEventType.Work, agent.Workplace);

                        // Quick meal break during shift (60% chance)
                        if (rng.NextDouble() < 0.6)
                        {
                            var breakTime = shiftStart.AddHours(rng.Next(5, 7));
                            var nearbyFood = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.Workplace.Latitude, agent.Workplace.Longitude, k: 2);
                            if (nearbyFood != null)
                            {
                                AddRoutineEvent(day, breakTime, TimeSpan.FromMinutes(30), RoutineEventType.Other, nearbyFood);
                            }
                        }

                        // Home after shift
                        var homeTime = shiftStart.AddHours(12).AddMinutes(rng.Next(15, 61));
                        AddRoutineEvent(day, homeTime, TimeSpan.FromHours(10), RoutineEventType.Home, agent.House);
                    }
                    else if (shiftRoll < 0.8) // Evening shift (30%)
                    {
                        // Morning at home
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(15), RoutineEventType.Home, agent.House);

                        var shiftStart = new TimeOnly(15, 0).AddMinutes(rng.Next(-30, 31));
                        AddRoutineEvent(day, shiftStart, TimeSpan.FromHours(12), RoutineEventType.Work, agent.Workplace);

                        // Late night home
                        var homeTime = shiftStart.AddHours(12).AddMinutes(rng.Next(15, 61));
                        if (homeTime.Hour < 6) // crosses midnight
                        {
                            AddRoutineEvent(day, homeTime, TimeSpan.FromHours(24 - homeTime.Hour), RoutineEventType.Home, agent.House);
                        }
                    }
                    else // Day off (20%)
                    {
                        // Errands or rest
                        if (rng.NextDouble() < 0.5)
                        {
                            var activityStart = new TimeOnly(rng.Next(10, 14), rng.Next(0, 60));
                            var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 3);
                            var dest = retail ?? agent.House;
                            AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(1, 3)), RoutineEventType.Shopping, dest);
                        }

                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                    }
                }

                return events;
            }

            // RETAIL WORKER
            if (agent is RetailWorkerAgent)
            {
                // Retail workers typically work 5 or 6 days with varied shifts
                for (int day = 0; day <= 6; day++)
                {
                    if (agent.Workplace == null)
                    {
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                        continue;
                    }

                    // 85% chance of working each day (some days off)
                    if (rng.NextDouble() < 0.85)
                    {
                        var shiftStart = new TimeOnly(rng.Next(6, 14), rng.Next(0, 60));
                        var shiftDuration = TimeSpan.FromHours(rng.Next(6, 9));

                        // Morning at home before shift
                        if (shiftStart.Hour > 0)
                        {
                            AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(shiftStart.Hour), RoutineEventType.Home, agent.House);
                        }

                        AddRoutineEvent(day, shiftStart, shiftDuration, RoutineEventType.Work, agent.Workplace);

                        // Lunch/break (40% chance to leave workplace)
                        if (rng.NextDouble() < 0.4)
                        {
                            var breakTime = shiftStart.AddHours(shiftDuration.TotalHours / 2);
                            var nearbyFood = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.Workplace.Latitude, agent.Workplace.Longitude, k: 2);
                            if (nearbyFood != null && nearbyFood.Id != agent.Workplace.Id)
                            {
                                AddRoutineEvent(day, breakTime, TimeSpan.FromMinutes(30), RoutineEventType.Other, nearbyFood);
                            }
                        }

                        // After work
                        var workEnd = shiftStart.Add(shiftDuration);

                        // Occasional after-work activity (20% chance on weekdays, 40% on weekends)
                        bool isWeekend = day >= 5;
                        double activityChance = isWeekend ? 0.4 : 0.2;

                        if (rng.NextDouble() < activityChance)
                        {
                            var activityStart = workEnd.AddMinutes(rng.Next(30, 91));
                            var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                agent.Workplace.Latitude, agent.Workplace.Longitude, k: 3);
                            var dest = leisure ?? agent.House;
                            AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(1, 3)), RoutineEventType.Leisure, dest);
                        }

                        // Home for evening
                        var homeTime = workEnd.AddMinutes(rng.Next(30, 121));
                        AddRoutineEvent(day, homeTime, TimeSpan.FromHours(10), RoutineEventType.Home, agent.House);
                    }
                    else
                    {
                        // Day off - errands or leisure
                        if (rng.NextDouble() < 0.6)
                        {
                            var activityStart = new TimeOnly(rng.Next(10, 15), rng.Next(0, 60));
                            Building dest;
                            RoutineEventType type;

                            if (rng.NextDouble() < 0.5)
                            {
                                var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                    agent.House.Latitude, agent.House.Longitude, k: 3);
                                dest = retail ?? agent.House;
                                type = RoutineEventType.Shopping;
                            }
                            else
                            {
                                var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                    agent.House.Latitude, agent.House.Longitude, k: 3);
                                dest = leisure ?? agent.House;
                                type = RoutineEventType.Leisure;
                            }

                            AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(2, 4)), type, dest);
                        }

                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                    }
                }

                return events;
            }

            // SERVICE WORKER (Hospitality, Leisure staff)
            if (agent is ServiceWorkerAgent)
            {
                // Service workers often have evening/night shifts, work weekends
                for (int day = 0; day <= 6; day++)
                {
                    if (agent.Workplace == null)
                    {
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
                        continue;
                    }

                    // 80% chance of working
                    if (rng.NextDouble() < 0.8)
                    {
                        // Varied shift times, often later in the day for hospitality
                        var shiftStart = new TimeOnly(rng.Next(10, 18), rng.Next(0, 60));
                        var shiftDuration = TimeSpan.FromHours(rng.Next(7, 10));

                        // Morning at home before shift
                        if (shiftStart.Hour > 0)
                        {
                            AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(shiftStart.Hour), RoutineEventType.Home, agent.House);
                        }

                        AddRoutineEvent(day, shiftStart, shiftDuration, RoutineEventType.Work, agent.Workplace);

                        // Break during shift (50% chance)
                        if (rng.NextDouble() < 0.5)
                        {
                            var breakTime = shiftStart.AddHours(rng.Next(3, 5));
                            var nearbyFood = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.Workplace.Latitude, agent.Workplace.Longitude, k: 2);
                            if (nearbyFood != null && nearbyFood.Id != agent.Workplace.Id)
                            {
                                AddRoutineEvent(day, breakTime, TimeSpan.FromMinutes(30), RoutineEventType.Other, nearbyFood);
                            }
                        }

                        // After work
                        var workEnd = shiftStart.Add(shiftDuration);

                        // Occasional socializing after work (25% chance)
                        if (rng.NextDouble() < 0.25 && workEnd.Hour >= 20)
                        {
                            var socialStart = workEnd.AddMinutes(rng.Next(15, 46));
                            var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                agent.Workplace.Latitude, agent.Workplace.Longitude, k: 3);
                            var dest = leisure ?? agent.House;
                            AddRoutineEvent(day, socialStart, TimeSpan.FromHours(rng.Next(1, 3)), RoutineEventType.Leisure, dest);
                        }

                        // Home
                        var homeTime = workEnd.AddMinutes(rng.Next(30, 91));
                        var hoursUntilMidnight = 24 - homeTime.Hour;
                        AddRoutineEvent(day, homeTime, TimeSpan.FromHours(hoursUntilMidnight), RoutineEventType.Home, agent.House);
                    }
                    else
                    {
                        // Day off - rest and errands
                        // Morning sleep-in - always add to ensure starting position
                        var wakeTime = new TimeOnly(rng.Next(9, 13), rng.Next(0, 60));
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(wakeTime.Hour), RoutineEventType.Home, agent.House);

                        // Afternoon activity (60% chance)
                        if (rng.NextDouble() < 0.6)
                        {
                            var activityStart = new TimeOnly(rng.Next(13, 17), rng.Next(0, 60));
                            Building dest;
                            RoutineEventType type;

                            if (rng.NextDouble() < 0.5)
                            {
                                var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                    agent.House.Latitude, agent.House.Longitude, k: 3);
                                dest = retail ?? agent.House;
                                type = RoutineEventType.Shopping;
                            }
                            else
                            {
                                var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                    agent.House.Latitude, agent.House.Longitude, k: 3);
                                dest = leisure ?? agent.House;
                                type = RoutineEventType.Leisure;
                            }

                            AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(2, 4)), type, dest);
                        }

                        // Evening at home
                        AddRoutineEvent(day, new TimeOnly(20, 0).AddMinutes(rng.Next(-60, 61)), TimeSpan.FromHours(8), RoutineEventType.Home, agent.House);
                    }
                }

                return events;
            }

            // ELDERLY / RETIRED
            if (agent is ElderlyRetiredAgent)
            {
                for (int day = 0; day <= 6; day++)
                {
                    // Morning at home (wake up later, 7-10am), always add to ensure starting position
                    var morningEnd = new TimeOnly(rng.Next(7, 11), rng.Next(0, 60));
                    AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(morningEnd.Hour), RoutineEventType.Home, agent.House);

                    // Morning activity (60% chance: shopping, errands, socializing)
                    if (rng.NextDouble() < 0.6)
                    {
                        var activityStart = new TimeOnly(rng.Next(9, 12), rng.Next(0, 60));
                        Building dest;
                        RoutineEventType type;

                        var roll = rng.NextDouble();
                        if (roll < 0.6) // Shopping (groceries, pharmacy)
                        {
                            var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 3);
                            dest = retail ?? agent.House;
                            type = RoutineEventType.Shopping;
                        }
                        else if (roll < 0.8) // Medical appointment (occasional)
                        {
                            var medical = buildingService.FindNearestBuildingByType<MedicalBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 2);
                            dest = medical ?? agent.House;
                            type = RoutineEventType.Medical;
                        }
                        else // Leisure (community center, park, cafe)
                        {
                            var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 3);
                            dest = leisure ?? agent.House;
                            type = RoutineEventType.Leisure;
                        }

                        AddRoutineEvent(day, activityStart, TimeSpan.FromHours(rng.Next(1, 3)), type, dest);
                    }

                    // Afternoon activity (40% chance, less frequent)
                    if (rng.NextDouble() < 0.4)
                    {
                        var afternoonStart = new TimeOnly(rng.Next(14, 16), rng.Next(0, 60));
                        Building dest;
                        RoutineEventType type;

                        if (rng.NextDouble() < 0.7) // Leisure (social activities)
                        {
                            var leisure = buildingService.FindNearestBuildingByType<LeisureBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 3);
                            dest = leisure ?? agent.House;
                            type = RoutineEventType.Leisure;
                        }
                        else // Quick shopping
                        {
                            var retail = buildingService.FindNearestBuildingByType<RetailBuilding>(
                                agent.House.Latitude, agent.House.Longitude, k: 2);
                            dest = retail ?? agent.House;
                            type = RoutineEventType.Shopping;
                        }

                        AddRoutineEvent(day, afternoonStart, TimeSpan.FromHours(rng.Next(1, 2)), type, dest);
                    }

                    // Evening at home (early, 6-8pm)
                    var eveningStart = new TimeOnly(rng.Next(18, 20), rng.Next(0, 60));
                    AddRoutineEvent(day, eveningStart, TimeSpan.FromHours(24 - eveningStart.Hour), RoutineEventType.Home, agent.House);
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

            // Defaults (used as catch for unspecific agent types)
            baseImmunity = 0.7;
            chronicConditionChance = 0.1;
            baseSusceptibility = 0.3;

            // Type-specific overrides
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

            // Random variation (introduce diversity) 
            double immunityVariation = 0.15;         // 
            double susceptibilityVariation = 0.15;   // 

            double finalImmunity = Math.Clamp(
                baseImmunity + (_rng.NextDouble() * 2 - 1) * immunityVariation,
                0.05, 1.0);

            bool hasChronicCondition = _rng.NextDouble() < chronicConditionChance;

            double finalSusceptibility = Math.Clamp(
                baseSusceptibility + (_rng.NextDouble() * 2 - 1) * susceptibilityVariation,
                0.05, 1.0);

            // Adjust for chronic conditions 
            if (hasChronicCondition)
            {
                finalImmunity = Math.Max(0.05, finalImmunity * 0.8); // 20% weaker immunity
                finalSusceptibility = Math.Min(1.0, finalSusceptibility * 1.3); // 30% more vulnerable
            }

            // Create profile 
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
            // core timing
            var (dayIndex, tickOfDay) = Utils.SimulationTimeHelper.GetSimulationTime(_context.CurrentTick, _context.TickLength);
            // local references to arrays for speed
            var agents = _context.AgentStates;
            var routines = _context.RoutineStates;
            var buildings = _context.BuildingStates;

            // Check for self-isolation mitigation
            var selfIsolationMitigation = _context.Mitigations.OfType<SelfIsolationMitigation>().FirstOrDefault();

            // quick map BuildingId -> index in building array
            var buildingIndexById = new Dictionary<int, int>(buildings.Length);
            for (int bi = 0; bi < buildings.Length; bi++)
                buildingIndexById[buildings[bi].Id] = bi;

            // occupancy counters
            short[] occupantCounts = new short[buildings.Length];

            // pre-filter routines to only those for today's day and build a lookup by AgentId
            var routinesToday = routines.Where(r => r.Day == dayIndex).ToArray();
            var routinesLookup = routinesToday.ToLookup(r => r.AgentId);

            // main loop over agents
            for (int i = 0; i < agents.Length; i++)
            {
                ref var agent = ref agents[i];
                int agentId = agent.Id; // Capture for lambda use

                // Check for Self-Isolation 
                // If agent is symptomatic and self-isolation policy is active, force them to stay home
                bool shouldIsolate = false;
                if (selfIsolationMitigation != null &&
                    selfIsolationMitigation.IsActive &&
                    (AgentHealthState)agent.HealthState == AgentHealthState.Infected &&
                    agent.SymptomSeverity >= selfIsolationMitigation.SymptomThreshold)
                {
                    // Check if this agent complies with self-isolation (deterministic per agent)
                    // Use agent ID as seed for consistent compliance decision
                    var agentRng = new Random(agentId + _context.Simulation.Id);
                    if (agentRng.NextDouble() < selfIsolationMitigation.ComplianceRate)
                    {
                        shouldIsolate = true;
                        agent.IsIsolating = true;
                    }
                }
                else
                {
                    agent.IsIsolating = false;
                }

                // get today's routines for this agent  
                var agentRoutines = routinesLookup[agentId].OrderBy(r => r.StartTick).ToList();
                if (!agentRoutines.Any())
                {
                    // no routines today - find a Home event from any day to get home position
                    var anyHomeEvent = routines.FirstOrDefault(r => r.AgentId == agentId && r.Type == (byte)RoutineEventType.Home);
                    if (anyHomeEvent.BuildingId != 0 && buildingIndexById.TryGetValue(anyHomeEvent.BuildingId, out var homeIdx))
                    {
                        agent.Lat = buildings[homeIdx].X;
                        agent.Lon = buildings[homeIdx].Y;
                        agent.CurrentBuildingId = anyHomeEvent.BuildingId;
                        occupantCounts[homeIdx]++;
                    }
                    continue;
                }

                // If agent is self-isolating, override their routine and keep them home
                if (shouldIsolate)
                {
                    var homeEvent = agentRoutines.FirstOrDefault(r => r.Type == (byte)RoutineEventType.Home);
                    if (homeEvent.BuildingId != 0 && buildingIndexById.TryGetValue(homeEvent.BuildingId, out var homeIdx))
                    {
                        agent.Lat = buildings[homeIdx].X;
                        agent.Lon = buildings[homeIdx].Y;
                        agent.CurrentBuildingId = homeEvent.BuildingId;
                        occupantCounts[homeIdx]++;
                    }
                    continue; // Skip normal routine processing
                }

                // Find the active event for current tickOfDay
                RoutineEventState? activeEvent = null;
                foreach (var ev in agentRoutines)
                {
                    int departTick = ev.StartTick - (int)ev.CachedTravelDuration;
                    int endTick = ev.StartTick + ev.DurationTicks;

                    // Agent is either traveling to or at this event
                    if (tickOfDay >= departTick && tickOfDay < endTick)
                    {
                        activeEvent = ev;
                        break;
                    }
                }

                if (!activeEvent.HasValue)
                {
                    // No active event - default to home position
                    // This handles gaps between routines (early morning, late night, etc.)
                    var homeEvent = agentRoutines.FirstOrDefault(r => r.Type == (byte)RoutineEventType.Home);
                    if (homeEvent.BuildingId != 0 && buildingIndexById.TryGetValue(homeEvent.BuildingId, out var homeIdx))
                    {
                        agent.Lat = buildings[homeIdx].X;
                        agent.Lon = buildings[homeIdx].Y;
                        agent.CurrentBuildingId = homeEvent.BuildingId;
                        occupantCounts[homeIdx]++;
                    }
                    continue;
                }

                var evt = activeEvent.Value;
                int travelStartTick = evt.StartTick - (int)evt.CachedTravelDuration;
                int arrivalTick = evt.StartTick;
                int eventEndTick = evt.StartTick + evt.DurationTicks;

                // PHASE 1: TRAVELING TO THE DESTINATION
                if (tickOfDay >= travelStartTick && tickOfDay < arrivalTick)
                {
                    // Agent is currently traveling
                    if (evt.Path != null && evt.Path.Length > 1 && evt.CachedTravelDuration > 0)
                    {
                        // Calculate progress through journey [0..1]
                        float progress = (float)(tickOfDay - travelStartTick) / evt.CachedTravelDuration;
                        progress = Math.Clamp(progress, 0f, 1f);

                        // Interpolate along the path segments
                        int segCount = evt.Path.Length - 1;
                        float segPos = progress * segCount;
                        int segIndex = (int)MathF.Floor(segPos);
                        float frac = segPos - segIndex;

                        // Clamp to valid range
                        segIndex = Math.Clamp(segIndex, 0, segCount - 1);

                        var p1 = evt.Path[segIndex];
                        var p2 = evt.Path[segIndex + 1];

                        // Smooth interpolation between path nodes
                        agent.Lat = p1.Lat + (p2.Lat - p1.Lat) * frac;
                        agent.Lon = p1.Lon + (p2.Lon - p1.Lon) * frac;
                        agent.CurrentBuildingId = 0; // in transit
                    }
                    else
                    {
                        // No path or single-node path - direct interpolation
                        // This handles cases where start == end or path failed
                        if (evt.BuildingId != 0 && buildingIndexById.TryGetValue(evt.BuildingId, out var bidx))
                        {
                            agent.Lat = buildings[bidx].X;
                            agent.Lon = buildings[bidx].Y;
                        }
                        agent.CurrentBuildingId = 0;
                    }
                }
                // PHASE 2: AT THE DESTINATION
                else if (tickOfDay >= arrivalTick && tickOfDay < eventEndTick)
                {
                    // Agent has arrived and is at the building
                    agent.CurrentBuildingId = evt.BuildingId;

                    // Position agent at building coordinates
                    if (evt.BuildingId != 0 && buildingIndexById.TryGetValue(evt.BuildingId, out var bidx))
                    {
                        // If has a path, place agent at the final path node (entrance)
                        // Otherwise use building center
                        if (evt.Path != null && evt.Path.Length > 0)
                        {
                            var finalNode = evt.Path[^1];
                            agent.Lat = finalNode.Lat;
                            agent.Lon = finalNode.Lon;
                        }
                        else
                        {
                            agent.Lat = buildings[bidx].X;
                            agent.Lon = buildings[bidx].Y;
                        }

                        // count occupancy
                        occupantCounts[bidx]++;
                    }
                }
            }

            // write back occupant counts
            for (int bi = 0; bi < buildings.Length; bi++)
            {
                buildings[bi].Occupants = occupantCounts[bi];
            }
        }
    }
}