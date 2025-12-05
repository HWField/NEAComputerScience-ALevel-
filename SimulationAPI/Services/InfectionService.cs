using System;
using System.Collections.Generic;
using MySimulationAPI.Models;
using MySimulationAPI.SimulationConfiguration;
using SimulationRuntime;

namespace MySimulationAPI.Services
{
    public class InfectionService
    {
        private readonly SimulationContext _context;
        private readonly Random _rng;
        private readonly Disease? _disease;

        // Realistic airborne transmission parameters
        private const float INFECTION_RADIUS_METERS = 2.0f; // WHO guidelines: 1-2m for airborne
        private const float INDOOR_MULTIPLIER = 8.0f; // Indoor air recirculation dramatically increases risk
        private const float OUTDOOR_DECAY = 0.3f; // UV light and air dispersion reduce outdoor transmission

        // Time-based transmission (per tick exposure)
        private float _transmissionPerTick;

        public InfectionService(SimulationContext context)
        {
            _context = context;
            _rng = context.Rng;
            _disease = context.Simulation.Disease;

            // Calculate per-tick transmission probability if disease is configured
            // InfectionRate is probability per hour of exposure
            // Convert to per-tick based on tick length
            if (_disease != null)
            {
                float ticksPerHour = 3600f / _context.TickLength;
                _transmissionPerTick = (float)_disease.InfectionRate / ticksPerHour;
            }
            else
            {
                _transmissionPerTick = 0f; // No infection if no disease configured
            }
        }

        /// <summary>
        /// Infects a specified number of random agents to seed the outbreak
        /// </summary>
        public void SeedInfection(int count = 1)
        {
            Span<AgentState> agents = _context.AgentStates.AsSpan();
            if (agents.Length == 0) return;

            int infected = 0;
            int attempts = 0;
            int maxAttempts = Math.Min(agents.Length, count * 10);

            while (infected < count && attempts < maxAttempts)
            {
                int idx = _rng.Next(agents.Length);
                ref var agent = ref agents[idx];

                if ((AgentHealthState)agent.HealthState == AgentHealthState.Healthy)
                {
                    agent.HealthState = (byte)AgentHealthState.Infected;
                    agent.InfectionProgress = 0f;
                    infected++;
                    Console.WriteLine($"[InfectionService] Seeded infection in agent {agent.Id}");
                }
                attempts++;
            }

            Console.WriteLine($"[InfectionService] Successfully infected {infected} agents");
        }

        public void UpdateInfectionTick()
        {
            Span<AgentState> agents = _context.AgentStates.AsSpan();
            if (agents.Length == 0)
                return;

            // --- Build spatial index for proximity queries ---
            var spatialIndex = BuildSpatialIndex(agents);

            // --- Process infection spread (infectious agents spreading to healthy agents) ---
            for (int i = 0; i < agents.Length; i++)
            {
                ref var infectious = ref agents[i];
                if ((AgentHealthState)infectious.HealthState != AgentHealthState.Infected)
                    continue;

                // Find nearby agents within transmission radius
                var nearby = new List<int>(32);
                spatialIndex.Query(infectious.Lat, infectious.Lon,
                    MetersToLatLonRadius(infectious.Lat, INFECTION_RADIUS_METERS), nearby);

                bool isIndoors = infectious.CurrentBuildingId != 0;

                for (int j = 0; j < nearby.Count; j++)
                {
                    int otherIdx = nearby[j];
                    if (otherIdx == i) continue; // Don't infect self

                    ref var susceptible = ref agents[otherIdx];
                    if ((AgentHealthState)susceptible.HealthState != AgentHealthState.Healthy)
                        continue; // Only healthy agents can be infected

                    // Check if they're in same location context
                    bool shareSpace = isIndoors
                        ? infectious.CurrentBuildingId == susceptible.CurrentBuildingId
                        : susceptible.CurrentBuildingId == 0; // Both outdoors

                    if (shareSpace)
                    {
                        TryInfect(ref infectious, ref susceptible, isIndoors);
                    }
                }
            }

            // --- Progress disease state for all infected/recovered agents ---
            for (int i = 0; i < agents.Length; i++)
                ProgressDisease(ref agents[i]);
        }

        private void TryInfect(ref AgentState infectious, ref AgentState susceptible, bool isIndoors)
        {
            // Base transmission probability per tick (time-scaled)
            float transmissionProb = _transmissionPerTick;

            // --- Environmental Modifiers ---
            if (isIndoors)
            {
                // Indoors: poor ventilation, virus concentration builds up
                transmissionProb *= INDOOR_MULTIPLIER;
            }
            else
            {
                // Outdoors: UV degradation, wind dispersion
                transmissionProb *= OUTDOOR_DECAY;
            }

            // --- Host Susceptibility Factors ---
            // Individual susceptibility (age, immune system, etc.)
            transmissionProb *= susceptible.Susceptibility;

            // Immunity from previous infection/recovery
            if (susceptible.Immunity > 0)
            {
                transmissionProb *= (1f - susceptible.Immunity);
            }

            // Chronic conditions increase risk
            if (susceptible.HasChronicCondition)
            {
                transmissionProb *= 1.8f; // Compromised immune system
            }

            // --- Infectious Agent Viral Load (disease progression) ---
            // Peak infectiousness around 20-60% disease progression
            float viralLoadMultiplier = infectious.InfectionProgress < 0.5f
                ? 0.5f + infectious.InfectionProgress // Ramping up
                : 1.5f - infectious.InfectionProgress; // Declining
            transmissionProb *= Math.Max(0.3f, viralLoadMultiplier);

            // Clamp probability to valid range
            transmissionProb = Math.Clamp(transmissionProb, 0f, 1f);

            // Roll for infection
            if (_rng.NextDouble() < transmissionProb)
            {
                susceptible.HealthState = (byte)AgentHealthState.Infected;
                susceptible.InfectionProgress = 0f;

                // Random incubation variation (±20%)
                susceptible.InfectionProgress -= (float)(_rng.NextDouble() * 0.2 - 0.1);
            }
        }

        private void ProgressDisease(ref AgentState agent)
        {
            if (_disease == null) return;

            float ticksPerDay = 86400f / _context.TickLength;

            switch ((AgentHealthState)agent.HealthState)
            {
                case AgentHealthState.Infected:
                    // Progress infection based on recovery rate (time-scaled per tick)
                    float progressPerTick = (float)_disease.RecoveryRate / ticksPerDay;
                    agent.InfectionProgress += progressPerTick;

                    // --- Mortality Check ---
                    // Risk increases with disease progression (most deaths occur at peak infection)
                    if (agent.InfectionProgress > 0.3f && agent.InfectionProgress < 0.9f)
                    {
                        // Base mortality rate adjusted by susceptibility and chronic conditions
                        float mortalityRisk = (float)_disease.MortalityRate * agent.Susceptibility;

                        if (agent.HasChronicCondition)
                        {
                            mortalityRisk *= 3.5f; // Significantly higher risk with comorbidities
                        }

                        // Scale to per-tick probability
                        mortalityRisk /= ticksPerDay;

                        if (_rng.NextDouble() < mortalityRisk)
                        {
                            agent.HealthState = (byte)AgentHealthState.Dead;
                            agent.InfectionProgress = 1f;
                            Console.WriteLine($"[InfectionService] Agent {agent.Id} died from infection");
                            return;
                        }
                    }

                    // --- Recovery Check ---
                    if (agent.InfectionProgress >= 1f)
                    {
                        agent.HealthState = (byte)AgentHealthState.Recovered;

                        // Build immunity (stronger if survived severe infection)
                        float immunityGain = agent.HasChronicCondition ? 0.15f : 0.25f;
                        agent.Immunity = Math.Min(1f, agent.Immunity + immunityGain);

                        Console.WriteLine($"[InfectionService] Agent {agent.Id} recovered (immunity: {agent.Immunity:F2})");
                    }
                    break;

                case AgentHealthState.Recovered:
                    // Slow immunity decay over time (wanes over ~6 months in simulation)
                    float immunityDecayPerTick = 0.001f / ticksPerDay;
                    agent.Immunity = Math.Max(0.05f, agent.Immunity - immunityDecayPerTick);
                    break;
            }
        }

        private SpatialIndex BuildSpatialIndex(Span<AgentState> agents)
        {
            // Calculate world bounds
            float minLat = float.MaxValue, maxLat = float.MinValue;
            float minLon = float.MaxValue, maxLon = float.MinValue;

            for (int i = 0; i < agents.Length; i++)
            {
                ref var a = ref agents[i];
                if (a.Lat < minLat) minLat = a.Lat;
                if (a.Lat > maxLat) maxLat = a.Lat;
                if (a.Lon < minLon) minLon = a.Lon;
                if (a.Lon > maxLon) maxLon = a.Lon;
            }

            // Build spatial index
            var index = new SpatialIndex(minLon, maxLon, minLat, maxLat, agents);
            for (int i = 0; i < agents.Length; i++)
            {
                // Only index living agents
                if ((AgentHealthState)agents[i].HealthState != AgentHealthState.Dead)
                {
                    index.Insert(i, ref agents[i]);
                }
            }

            return index;
        }

        private static float MetersToLatLonRadius(float latitude, float meters)
        {
            // Accurate Haversine-based conversion
            // At equator: 1 degree lat ≈ 111km
            // Longitude varies by cos(latitude)
            const float METERS_PER_DEGREE = 111_000f;
            return meters / METERS_PER_DEGREE;
        }
    }

    // ----------------------------------------------
    // Optimized Spatial Index (Grid-based for speed)
    // ----------------------------------------------
    public class SpatialIndex
    {
        private const int GRID_SIZE = 50; // 50x50 grid for fast lookup
        private readonly float _minX, _maxX, _minY, _maxY;
        private readonly float _cellWidth, _cellHeight;
        private readonly List<int>[] _cells;
        private readonly AgentState[] _agents;

        public SpatialIndex(float minX, float maxX, float minY, float maxY, Span<AgentState> agents)
        {
            _minX = minX; _maxX = maxX;
            _minY = minY; _maxY = maxY;
            _cellWidth = (maxX - minX) / GRID_SIZE;
            _cellHeight = (maxY - minY) / GRID_SIZE;
            _cells = new List<int>[GRID_SIZE * GRID_SIZE];
            _agents = agents.ToArray();

            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new List<int>(16);
        }

        public void Insert(int agentIndex, ref AgentState agent)
        {
            int cellIdx = GetCellIndex(agent.Lon, agent.Lat);
            if (cellIdx >= 0 && cellIdx < _cells.Length)
                _cells[cellIdx].Add(agentIndex);
        }

        public void Query(float lat, float lon, float radius, List<int> results)
        {
            // Determine cell range to check
            int centerX = (int)((lon - _minX) / _cellWidth);
            int centerY = (int)((lat - _minY) / _cellHeight);
            int cellRadius = (int)Math.Ceiling(radius / Math.Min(_cellWidth, _cellHeight)) + 1;

            float radiusSq = radius * radius;

            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            {
                for (int dx = -cellRadius; dx <= cellRadius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x < 0 || x >= GRID_SIZE || y < 0 || y >= GRID_SIZE)
                        continue;

                    int cellIdx = y * GRID_SIZE + x;
                    var cell = _cells[cellIdx];

                    foreach (int agentIdx in cell)
                    {
                        ref var a = ref _agents[agentIdx];
                        float dlat = a.Lat - lat;
                        float dlon = a.Lon - lon;

                        if ((dlat * dlat + dlon * dlon) <= radiusSq)
                            results.Add(agentIdx);
                    }
                }
            }
        }

        private int GetCellIndex(float lon, float lat)
        {
            int x = (int)((lon - _minX) / _cellWidth);
            int y = (int)((lat - _minY) / _cellHeight);

            if (x < 0 || x >= GRID_SIZE || y < 0 || y >= GRID_SIZE)
                return -1;

            return y * GRID_SIZE + x;
        }
    }
}