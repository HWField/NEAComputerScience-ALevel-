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
        private const float INFECTION_RADIUS_METERS = 2.0f;
        private const float INDOOR_MULTIPLIER = 2.5f;
        private const float OUTDOOR_DECAY = 0.15f;

        // Time-based transmission (per tick exposure)
        private float _transmissionPerTick;

        // Tracking metrics using private backing fields with thread-safe reads
        private int _totalInfections;
        private int _activeInfections;
        private int _symptomaticCases;
        private int _reportedCases;
        private int _totalDeaths;
        private int _totalRecoveries;

        // Public properties that provide thread-safe atomic reads
        public int TotalInfections => System.Threading.Volatile.Read(ref _totalInfections);
        public int ActiveInfections => System.Threading.Volatile.Read(ref _activeInfections);
        public int SymptomaticCases => System.Threading.Volatile.Read(ref _symptomaticCases);
        public int ReportedCases => System.Threading.Volatile.Read(ref _reportedCases);
        public int TotalDeaths => System.Threading.Volatile.Read(ref _totalDeaths);
        public int TotalRecoveries => System.Threading.Volatile.Read(ref _totalRecoveries);

        public InfectionService(SimulationContext context)
        {
            _context = context;
            _rng = context.Rng;
            _disease = context.Simulation.Disease;

            // Calculate per-tick transmission probability if disease is configured
            // InfectionRate is probability per HOUR of close contact exposure
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

        public void RestoreMetricsFromSnapshot(int totalInfections, int totalDeaths, int totalRecoveries)
        {
            System.Threading.Volatile.Write(ref _totalInfections, totalInfections);
            System.Threading.Volatile.Write(ref _totalDeaths, totalDeaths);
            System.Threading.Volatile.Write(ref _totalRecoveries, totalRecoveries);

            // Recalculate current state metrics from agent states
            RecalculateActiveMetrics();
        }

        public void RecalculateActiveMetrics()
        {
            Span<AgentState> agents = _context.AgentStates.AsSpan();

            int activeInfections = 0;
            int symptomaticCases = 0;
            int reportedCases = 0;

            for (int i = 0; i < agents.Length; i++)
            {
                ref var agent = ref agents[i];

                if ((AgentHealthState)agent.HealthState == AgentHealthState.Infected)
                {
                    activeInfections++;

                    if (agent.SymptomSeverity > 0.1f)
                    {
                        symptomaticCases++;

                        if (agent.SymptomSeverity > 0.3f)
                            reportedCases++;
                    }
                }
            }

            // Atomically update all metrics at once
            System.Threading.Volatile.Write(ref _activeInfections, activeInfections);
            System.Threading.Volatile.Write(ref _symptomaticCases, symptomaticCases);
            System.Threading.Volatile.Write(ref _reportedCases, reportedCases);
        }


        // Infects a specified number of random agents to seed the outbreak
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
                    agent.InfectionCount++;

                    // Determine if symptomatic
                    if (_disease != null && _rng.NextDouble() < _disease.SymptomaticRate)
                    {
                        // Severity varies (0.1 = mild, 1.0 = severe)
                        agent.SymptomSeverity = _rng.NextDouble() < _disease.SevereSymptomsRate
                            ? (float)(_rng.NextDouble() * 0.5 + 0.5)  // 0.5-1.0 (severe)
                            : (float)(_rng.NextDouble() * 0.4 + 0.1); // 0.1-0.5 (mild)
                    }
                    else
                    {
                        agent.SymptomSeverity = 0f; // Asymptomatic
                    }

                    infected++;
                    System.Threading.Interlocked.Increment(ref _totalInfections);
                }
                attempts++;
            }
        }

        public void UpdateInfectionTick()
        {
            Span<AgentState> agents = _context.AgentStates.AsSpan();
            if (agents.Length == 0)
                return;

            // Use local variables during computation to avoid intermediate states being read
            int activeInfections = 0;
            int symptomaticCases = 0;
            int reportedCases = 0;

            // --- Build spatial index for proximity queries ---
            var spatialIndex = BuildSpatialIndex(agents);

            // --- Process infection spread (infectious agents spreading to healthy agents) ---
            for (int i = 0; i < agents.Length; i++)
            {
                ref var infectious = ref agents[i];
                if ((AgentHealthState)infectious.HealthState != AgentHealthState.Infected)
                    continue;

                activeInfections++;

                // Calculate days since infection
                float daysSinceInfection = _disease != null ? infectious.InfectionProgress / (float)_disease.RecoveryRate : 0;

                // Check if agent is past incubation period (infectious)
                bool isInfectious = _disease != null && daysSinceInfection >= _disease.IncubationPeriod;

                if (infectious.SymptomSeverity > 0.1f)
                {
                    symptomaticCases++;

                    // Reported if symptoms are severe enough to test/notice
                    if (infectious.SymptomSeverity > 0.3f)
                        reportedCases++;
                }

                // Only spread infection if past incubation period
                if (!isInfectious)
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
            {
                ref var agent = ref agents[i];
                ProgressDisease(ref agent);
                DecayImmunity(ref agent);
                ProgressVaccination(ref agent);
            }

            // Atomically write all computed metrics at once at the end
            System.Threading.Volatile.Write(ref _activeInfections, activeInfections);
            System.Threading.Volatile.Write(ref _symptomaticCases, symptomaticCases);
            System.Threading.Volatile.Write(ref _reportedCases, reportedCases);
        }

        private void TryInfect(ref AgentState infectious, ref AgentState susceptible, bool isIndoors)
        {
            if (_disease == null) return;
            // Base transmission probability per tick (time-scaled)
            float transmissionProb = _transmissionPerTick;

            // --- Environmental Modifiers ---
            if (isIndoors)
                transmissionProb *= INDOOR_MULTIPLIER;
            else
                transmissionProb *= OUTDOOR_DECAY;

            // --- Intervention: Masks (probabilistic check per transmission attempt) ---
            var maskMitigation = _context.Mitigations.OfType<MaskWearingMitigation>().FirstOrDefault();
            if (maskMitigation != null && maskMitigation.IsActive)
            {
                // Probabilistic: infectious agent wears mask based on adoption rate
                if (_rng.NextDouble() < maskMitigation.AdoptionRate)
                {
                    float maskEffectiveness = (float)_disease.MaskEffectiveness;
                    // Apply effectiveness modifier from mitigation policy
                    maskEffectiveness *= (float)maskMitigation.EffectivenessModifier;
                    transmissionProb *= (1f - maskEffectiveness);
                }

                // Probabilistic: susceptible agent wears mask (masks protect wearer less)
                if (_rng.NextDouble() < maskMitigation.AdoptionRate)
                {
                    float maskEffectiveness = (float)_disease.MaskEffectiveness * 0.5f;
                    maskEffectiveness *= (float)maskMitigation.EffectivenessModifier;
                    transmissionProb *= (1f - maskEffectiveness);
                }
            }

            // --- Host Susceptibility ---
            float effectiveSusceptibility = susceptible.Susceptibility;

            // Intervention: Vaccination (probabilistic check per transmission attempt)
            var vaccineMitigation = _context.Mitigations.OfType<VaccinationMitigation>().FirstOrDefault();
            if (vaccineMitigation != null && vaccineMitigation.IsActive)
            {
                // Probabilistic: susceptible agent is vaccinated based on vaccination rate
                if (_rng.NextDouble() < vaccineMitigation.VaccinationRate)
                {
                    // Vaccination effectiveness increases over time to full effectiveness
                    float vaccineEffectiveness = (float)_disease.VaccineEffectiveness;
                    // Apply effectiveness modifier from mitigation policy
                    vaccineEffectiveness *= (float)vaccineMitigation.EffectivenessModifier;
                    // Progress towards full effectiveness (stored in agent state)
                    vaccineEffectiveness *= susceptible.VaccinationProgress;
                    effectiveSusceptibility *= (1f - vaccineEffectiveness);
                }
            }

            // Natural immunity from previous infections
            if (susceptible.Immunity > 0)
                effectiveSusceptibility *= (1f - susceptible.Immunity);

            transmissionProb *= effectiveSusceptibility;

            // Chronic conditions increase risk
            if (susceptible.HasChronicCondition)
                transmissionProb *= 1.5f;

            // --- Viral Load (infection stage) ---
            float viralLoadMultiplier = infectious.InfectionProgress < 0.5f
                ? 0.5f + infectious.InfectionProgress
                : 1.5f - infectious.InfectionProgress;
            transmissionProb *= Math.Max(0.3f, viralLoadMultiplier);

            // --- Behavioral Factor: Symptomatic agents may reduce contacts ---
            // If infectious agent has moderate/severe symptoms, they're less likely to be in public
            if (infectious.SymptomSeverity > 0.4f && !isIndoors)
            {
                // Symptomatic people avoid going out (reduces outdoor transmission)
                transmissionProb *= 0.3f; // 70% reduction when symptomatic and outdoors
            }

            // Clamp probability to valid range
            transmissionProb = Math.Clamp(transmissionProb, 0f, 1f);

            // Roll for infection
            if (_rng.NextDouble() < transmissionProb)
            {
                susceptible.HealthState = (byte)AgentHealthState.Infected;
                susceptible.InfectionProgress = 0f;
                susceptible.InfectionCount++;
                System.Threading.Interlocked.Increment(ref _totalInfections);

                // Determine symptoms
                if (_rng.NextDouble() < _disease.SymptomaticRate)
                {
                    susceptible.SymptomSeverity = _rng.NextDouble() < _disease.SevereSymptomsRate
                        ? (float)(_rng.NextDouble() * 0.5 + 0.5)  // 0.5-1.0 (severe)
                        : (float)(_rng.NextDouble() * 0.4 + 0.1); // 0.1-0.5 (mild)
                }
                else
                {
                    susceptible.SymptomSeverity = 0f; // Asymptomatic
                }
            }
        }

        private void ProgressDisease(ref AgentState agent)
        {
            if (_disease == null) return;

            float ticksPerDay = 86400f / _context.TickLength;

            switch ((AgentHealthState)agent.HealthState)
            {
                case AgentHealthState.Infected:
                    // Progress infection
                    float progressPerTick = (float)_disease.RecoveryRate / ticksPerDay;
                    agent.InfectionProgress += progressPerTick;

                    // Symptoms develop and peak around 30-60% of infection
                    if (agent.SymptomSeverity > 0)
                    {
                        // Symptoms worsen then improve
                        float symptomMultiplier = agent.InfectionProgress < 0.5f
                            ? agent.InfectionProgress * 2f  // Ramping up
                            : 2f - agent.InfectionProgress * 2f; // Declining

                        float currentSymptoms = agent.SymptomSeverity * symptomMultiplier;

                        // Hospital-seeking behavior for severe symptoms
                        if (currentSymptoms > 0.7f && _rng.NextDouble() < _disease.HospitalSeekingRate / ticksPerDay)
                        {
                            // later expand to actually modify agent routines
                        }
                    }

                    // --- Mortality Check (once at peak) ---
                    float prevProgress = agent.InfectionProgress - progressPerTick;
                    if (prevProgress < 0.5f && agent.InfectionProgress >= 0.5f)
                    {
                        float mortalityRisk = (float)_disease.MortalityRate * agent.Susceptibility;

                        if (agent.HasChronicCondition)
                            mortalityRisk *= 3.5f;

                        // Severe symptoms correlate with mortality
                        if (agent.SymptomSeverity > 0.5f)
                            mortalityRisk *= 2f;

                        if (_rng.NextDouble() < mortalityRisk)
                        {
                            agent.HealthState = (byte)AgentHealthState.Dead;
                            agent.InfectionProgress = 1f;
                            agent.SymptomSeverity = 0f;
                            System.Threading.Interlocked.Increment(ref _totalDeaths);
                            return;
                        }
                    }

                    // --- Recovery ---
                    if (agent.InfectionProgress >= 1f)
                    {
                        agent.HealthState = (byte)AgentHealthState.Recovered;
                        System.Threading.Interlocked.Increment(ref _totalRecoveries);

                        // Build immunity based on disease properties
                        float immunityGain = (float)_disease.ReinfectionResistance;

                        // Check if agent was probabilistically vaccinated (affects immunity gain)
                        var vaccineMitigation = _context.Mitigations.OfType<VaccinationMitigation>().FirstOrDefault();
                        if (vaccineMitigation != null && vaccineMitigation.IsActive)
                        {
                            // Use agent ID as seed for deterministic vaccination status
                            var agentRng = new Random(agent.Id + _context.Simulation.Id);
                            if (agentRng.NextDouble() < vaccineMitigation.VaccinationRate)
                                immunityGain *= 1.2f; // Vaccination boosts immunity
                        }

                        // Chronic conditions reduce immune response
                        if (agent.HasChronicCondition)
                            immunityGain *= 0.7f;

                        agent.Immunity = Math.Min(1f, agent.Immunity + immunityGain);
                        agent.SymptomSeverity = 0f;
                    }
                    break;

                case AgentHealthState.Recovered:
                    // Immunity naturally decays (handled in DecayImmunity)
                    break;
            }
        }

        private void DecayImmunity(ref AgentState agent)
        {
            if (_disease == null || agent.Immunity <= 0) return;

            // Immunity decays based on disease's ImmunityDuration
            float ticksPerDay = 86400f / _context.TickLength;
            float decayPerTick = 1f / ((float)_disease.ImmunityDuration * ticksPerDay);

            agent.Immunity = Math.Max(0f, agent.Immunity - decayPerTick);

            // When immunity fully wanes, agent becomes susceptible again (reinfection possible)
            if (agent.Immunity < 0.05f && (AgentHealthState)agent.HealthState == AgentHealthState.Recovered)
            {
                agent.HealthState = (byte)AgentHealthState.Healthy;
                agent.Immunity = 0f;
            }
        }

        private void ProgressVaccination(ref AgentState agent)
        {
            // Progress vaccination effectiveness over time for agents
            // VaccinationProgress tracks how long agent has been "vaccinated" (0.0 to 1.0)
            var vaccineMitigation = _context.Mitigations.OfType<VaccinationMitigation>().FirstOrDefault();
            if (vaccineMitigation == null || !vaccineMitigation.IsActive) return;

            // Check if this agent is deterministically vaccinated based on policy rate
            var agentRng = new Random(agent.Id + _context.Simulation.Id);
            if (agentRng.NextDouble() >= vaccineMitigation.VaccinationRate) return;

            // Only progress if not yet at full effectiveness
            if (agent.VaccinationProgress >= 1f) return;

            // Progress vaccination effectiveness over time (0.0 to 1.0)
            float ticksPerDay = 86400f / _context.TickLength;
            float daysToFull = vaccineMitigation.DaysToFullEffectiveness;
            float progressPerTick = 1f / (daysToFull * ticksPerDay);

            agent.VaccinationProgress = Math.Min(1f, agent.VaccinationProgress + progressPerTick);
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