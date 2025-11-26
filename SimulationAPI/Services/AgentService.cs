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

            if (agent.House == null)
                throw new InvalidOperationException("Agent must have a home assigned.");

            // local location tracker used whilst calcuating routes
            (double curLat, double curLon) = (agent.House.Latitude, agent.House.Longitude);

            // ------------------------------
            // Local helper: add a routine event
            // ------------------------------
            void AddRoutineEvent(int day, TimeOnly start, TimeSpan duration, RoutineEventType type, Building target)
            {
                var (path, travelTime) = routingService.ComputeRoute(
                    curLat, curLon,
                    target.Latitude, target.Longitude
                );

                // Debug: Log the first path generation
                if (events.Count == 0)
                {
                    Console.WriteLine($"[AgentService] First route computed:");
                    Console.WriteLine($"  - Path nodes: {path?.Count ?? 0}");
                    if (path != null && path.Count > 0)
                    {
                        Console.WriteLine($"  - First node: ({path[0].Lat}, {path[0].Lon})");
                        Console.WriteLine($"  - Last node: ({path[^1].Lat}, {path[^1].Lon})");
                    }
                    Console.WriteLine($"  - Travel time: {travelTime}s");
                }

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
                    AddRoutineEvent(day, schoolStart, TimeSpan.FromHours(7), RoutineEventType.School, agent.Workplace);

                    // Return home
                    var homeStart = schoolStart.AddHours(7).AddMinutes(rng.Next(0, 31));
                    AddRoutineEvent(day, homeStart, TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
                }

                // Weekends
                for (int day = 5; day <= 6; day++)
                {
                    if (rng.NextDouble() < 0.7)
                    {
                        var leisureStart = new TimeOnly(10, 0).AddMinutes(rng.Next(-60, 61));
                        var leisureDur = TimeSpan.FromHours(rng.Next(2, 5));
                        var target = PickLeisureOrHome();
                        AddRoutineEvent(day, leisureStart, leisureDur, RoutineEventType.Leisure, target);
                    }

                    AddRoutineEvent(day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
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
                    AddRoutineEvent(day, workStart, TimeSpan.FromHours(rng.Next(8, 10)), RoutineEventType.Work, agent.Workplace);

                    // Lunch break (30%)
                    if (rng.NextDouble() < 0.3)
                    {
                        var lunchStart = workStart.AddHours(4);
                        var restaurant = buildingService.GetRandomBuilding<RetailBuilding>(rng) ?? agent.Workplace;
                        AddRoutineEvent(day, lunchStart, TimeSpan.FromHours(1), RoutineEventType.Other, restaurant);
                    }

                    // Return home
                    var endTime = workStart.AddHours(rng.Next(8, 10));
                    AddRoutineEvent(day, endTime, TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
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

                        AddRoutineEvent(day, start, TimeSpan.FromHours(rng.Next(2, 5)), type, dest);
                    }

                    AddRoutineEvent(day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
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
                        AddRoutineEvent(day, new TimeOnly(7, 0), TimeSpan.FromHours(12), RoutineEventType.Work, agent.Workplace);
                    else if (roll < 0.8)
                        AddRoutineEvent(day, new TimeOnly(19, 0), TimeSpan.FromHours(12), RoutineEventType.Work, agent.Workplace);
                    else
                        AddRoutineEvent(day, new TimeOnly(0, 0), TimeSpan.FromHours(24), RoutineEventType.Home, agent.House);
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
                        AddRoutineEvent(day, new TimeOnly(startHour, 0), dur, RoutineEventType.Work, agent.Workplace);
                    }

                    AddRoutineEvent(day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
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
                        AddRoutineEvent(day, new TimeOnly(startHour, 0), TimeSpan.FromHours(8), RoutineEventType.Work, agent.Workplace);
                    }

                    AddRoutineEvent(day, new TimeOnly(23, 0), TimeSpan.FromHours(8), RoutineEventType.Home, agent.House);
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
                        AddRoutineEvent(day, start, TimeSpan.FromHours(1.5), RoutineEventType.Shopping, shop);
                    }
                    else if (rng.NextDouble() < 0.3)
                    {
                        var leisure = PickLeisureOrHome();
                        AddRoutineEvent(day, new TimeOnly(14, 0), TimeSpan.FromHours(2), RoutineEventType.Leisure, leisure);
                    }

                    AddRoutineEvent(day, new TimeOnly(18, 0), TimeSpan.FromHours(12), RoutineEventType.Home, agent.House);
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
            // core timing
            var (dayIndex, tickOfDay) = Utils.SimulationTimeHelper.GetSimulationTime(_context.CurrentTick, _context.TickLength);
            // local references to arrays for speed
            var agents = _context.AgentStates;
            var routines = _context.RoutineStates;
            var buildings = _context.BuildingStates;

            // quick map BuildingId -> index in building array (to avoid LINQ .FirstOrDefault inside loop which has O(n) cost)
            var buildingIndexById = new Dictionary<int, int>(buildings.Length);
            for (int bi = 0; bi < buildings.Length; bi++)
                buildingIndexById[buildings[bi].Id] = bi;

            // occupancy counters
            short[] occupantCounts = new short[buildings.Length];

            // pre-filter routines to only those for today's day and build a lookup by AgentId for fast retrieval
            var routinesToday = routines.Where(r => r.Day == dayIndex).ToArray();
            var routinesLookup = routinesToday.ToLookup(r => r.AgentId);

            // main loop over agents
            for (int i = 0; i < agents.Length; i++)
            {
                ref var agent = ref agents[i];

                // get todays routines for this agent  
                var agentRoutines = routinesLookup[agent.Id];
                if (!agentRoutines.Any())
                {
                    continue;
                }

                // find the first active event for current tickOfDay
                RoutineEventState? activeEvent = null;
                foreach (var ev in agentRoutines)
                {
                    int startTick = ev.StartTick;
                    int endTick = ev.StartTick + ev.DurationTicks;
                    int departTick = startTick - (int)ev.CachedTravelDuration;

                    if (tickOfDay >= departTick && tickOfDay < endTick)
                    {
                        activeEvent = ev;
                        break;
                    }
                }

                if (!activeEvent.HasValue)
                {
                    continue;
                }
                var evt = activeEvent.Value;
                int travelStart = evt.StartTick - (int)evt.CachedTravelDuration;
                int travelEnd = evt.StartTick;

                if (tickOfDay >= travelStart && tickOfDay < travelEnd && evt.Path != null && evt.Path.Length > 1 && evt.CachedTravelDuration > 0)
                {
                    // progress [0..1] through travel in ticks
                    float progress = (float)(tickOfDay - travelStart) / evt.CachedTravelDuration;
                    progress = Math.Clamp(progress, 0f, 1f);

                    int segCount = evt.Path.Length - 1;
                    float segPos = progress * segCount;
                    int segIndex = (int)MathF.Floor(segPos);
                    float frac = segPos - segIndex;

                    if (segIndex < 0) segIndex = 0;
                    if (segIndex >= segCount) segIndex = segCount - 1;

                    var p1 = evt.Path[segIndex];
                    var p2 = evt.Path[segIndex + 1];

                    agent.Lat = p1.Lat + (p2.Lat - p1.Lat) * frac;
                    agent.Lon = p1.Lon + (p2.Lon - p1.Lon) * frac;
                    agent.CurrentBuildingId = 0; // in transit
                }
                else
                {
                    // agent is at the event building location
                    agent.CurrentBuildingId = evt.BuildingId;

                    // update agent position to building coordinates (fast lookup by index)
                    if (evt.BuildingId != 0 && buildingIndexById.TryGetValue(evt.BuildingId, out var bidx))
                    {
                        agent.Lat = buildings[bidx].X;
                        agent.Lon = buildings[bidx].Y;

                        // count occupancy
                        occupantCounts[bidx]++;
                    }
                    else
                    {
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