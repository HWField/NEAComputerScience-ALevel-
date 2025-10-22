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
        private readonly Disease _disease;

        private const float INFECTION_RADIUS_METERS = 5.0f;
        private const float INDOOR_MULTIPLIER = 3.0f;

        public InfectionService(SimulationContext context)
        {
            _context = context;
            _rng = context.Rng;
            _disease = context.Simulation.Disease;
        }

        public void UpdateInfectionTick()
        {
            Span<AgentState> agents = _context.AgentStates.AsSpan();
            if (agents.Length == 0)
                return;

            // --- world bounds for quadtree ---
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

            // --- Build quadtree from agents ---
            var quadtree = new QuadtreeStruct(minLon, maxLon, minLat, maxLat);
            for (int i = 0; i < agents.Length; i++)
                quadtree.Insert(ref agents[i]);

            // --- Process infection spread ---
            for (int i = 0; i < agents.Length; i++)
            {
                ref var src = ref agents[i];
                if ((AgentHealthState)src.HealthState != AgentHealthState.Infected)
                    continue;

                var nearby = new List<int>(32);
                quadtree.Query(src.Lat, src.Lon, MetersToLatLonRadius(src.Lat, INFECTION_RADIUS_METERS), nearby);

                for (int j = 0; j < nearby.Count; j++)
                {
                    int otherIdx = nearby[j];
                    if (otherIdx == i) continue;

                    ref var dst = ref agents[otherIdx];
                    if ((AgentHealthState)dst.HealthState != AgentHealthState.Healthy)
                        continue;

                    TryInfect(ref src, ref dst);
                }
            }

            // --- Progress infection state ---
            for (int i = 0; i < agents.Length; i++)
                ProgressDisease(ref agents[i]);
        }

        private void TryInfect(ref AgentState src, ref AgentState dst)
        {
            float baseChance = (float)_disease.InfectionRate * dst.Susceptibility;

            // If both in same building (indoors multiplier)
            if (src.CurrentBuildingId != 0 &&
                src.CurrentBuildingId == dst.CurrentBuildingId)
                baseChance *= INDOOR_MULTIPLIER;

            if (dst.HasChronicCondition)
                baseChance *= 1.5f;

            if (baseChance > 1f) baseChance = 1f;

            if (_rng.NextDouble() < baseChance)
            {
                dst.HealthState = (byte)AgentHealthState.Infected;
                dst.InfectionProgress = 0f;
            }
        }

        private void ProgressDisease(ref AgentState a)
        {
            switch ((AgentHealthState)a.HealthState)
            {
                case AgentHealthState.Infected:
                    a.InfectionProgress += (float)_disease.RecoveryRate;

                    // Mortality check
                    if (_rng.NextDouble() < _disease.MortalityRate * a.Susceptibility)
                    {
                        a.HealthState = (byte)AgentHealthState.Dead;
                        return;
                    }

                    // Recovery check
                    if (a.InfectionProgress >= 1f)
                    {
                        a.HealthState = (byte)AgentHealthState.Recovered;
                        a.Immunity = MathF.Min(1f, a.Immunity + 0.2f);
                    }
                    break;

                case AgentHealthState.Recovered:
                    // Slow immunity decay
                    a.Immunity = MathF.Max(0.1f, a.Immunity - 0.001f);
                    break;
            }
        }

        private static float MetersToLatLonRadius(float latitude, float meters)
        {
            // Approx conversion (1 degree lat = ~111 km)
            return (float)(meters / 111_000.0);
        }
    }

    // ----------------------------------------------
    // Struct-optimized Quadtree (index-based)
    // ----------------------------------------------
    public class QuadtreeStruct
    {
        private const int MAX_AGENTS = 32;
        private readonly float MinX, MaxX, MinY, MaxY;
        private readonly List<int> _indices = new(MAX_AGENTS);
        private QuadtreeStruct[]? _children;
        private AgentState[]? _source;

        public QuadtreeStruct(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY;
        }

        public void Insert(ref AgentState agent)
        {
            if (_children != null)
            {
                int quad = GetQuadrant(agent);
                _children[quad].Insert(ref agent);
                return;
            }

            _indices.Add(agent.Id);

            if (_indices.Count > MAX_AGENTS)
                Subdivide();
        }

        private void Subdivide()
        {
            float midX = (MinX + MaxX) / 2;
            float midY = (MinY + MaxY) / 2;

            _children = new QuadtreeStruct[4];
            _children[0] = new QuadtreeStruct(MinX, midX, MinY, midY);
            _children[1] = new QuadtreeStruct(midX, MaxX, MinY, midY);
            _children[2] = new QuadtreeStruct(MinX, midX, midY, MaxY);
            _children[3] = new QuadtreeStruct(midX, MaxX, midY, MaxY);
        }

        private int GetQuadrant(AgentState a)
        {
            float midX = (MinX + MaxX) / 2;
            float midY = (MinY + MaxY) / 2;

            bool left = a.Lon < midX;
            bool bottom = a.Lat < midY;

            if (left && bottom) return 0;
            if (!left && bottom) return 1;
            if (left && !bottom) return 2;
            return 3;
        }

        public void Query(float lat, float lon, float radius, List<int> results)
        {
            if (!IntersectsCircle(lat, lon, radius))
                return;

            if (_children != null)
            {
                foreach (var child in _children)
                    child.Query(lat, lon, radius, results);
            }
            else
            {
                float radiusSq = radius * radius;
                foreach (int idx in _indices)
                {
                    ref var a = ref _source![idx];
                    float dx = a.Lat - lat;
                    float dy = a.Lon - lon;
                    if ((dx * dx + dy * dy) <= radiusSq)
                        results.Add(idx);
                }
            }
        }

        private bool IntersectsCircle(float lat, float lon, float radius)
        {
            float closestX = Math.Clamp(lon, MinX, MaxX);
            float closestY = Math.Clamp(lat, MinY, MaxY);
            float dx = closestX - lon;
            float dy = closestY - lat;
            return (dx * dx + dy * dy) <= radius * radius;
        }
    }
}