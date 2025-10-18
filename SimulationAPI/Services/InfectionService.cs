using System;
using System.Collections.Generic;
using System.Linq;
using MySimulationAPI.Models;
using MySimulationAPI.SimulationConfiguration;

namespace MySimulationAPI.Services
{
    public class InfectionService
    {
        private readonly SimulationContext _context;
        private readonly Random _rng;
        private readonly Disease _disease;

        // Infection radius in meters
        private const double INFECTION_RADIUS_METERS = 5.0;
        private const double INDOOR_MULTIPLIER = 3.0;

        public InfectionService(SimulationContext context)
        {
            _context = context;
            _rng = context.Rng;
            _disease = context.Simulation.Disease;
        }

        public void UpdateInfectionTick()
        {
            var agents = _context.Agents;

            if (!agents.Any())
                return;

            // Build a spatial index
            double minLat = agents.Min(a => a.CurrentLat);
            double maxLat = agents.Max(a => a.CurrentLat);
            double minLon = agents.Min(a => a.CurrentLon);
            double maxLon = agents.Max(a => a.CurrentLon);

            var quadtree = new Quadtree(minLon, maxLon, minLat, maxLat);
            foreach (var agent in agents)
                quadtree.Insert(agent);

            // Infection spread
            foreach (var agent in agents)
            {
                if (agent.Status != "Infected") continue;

                var nearby = new List<Agent>();
                quadtree.Query(agent.CurrentLat, agent.CurrentLon, MetersToLatLonRadius(agent.CurrentLat, INFECTION_RADIUS_METERS), nearby);

                foreach (var other in nearby)
                {
                    if (agent.Id == other.Id) continue;
                    if (other.Status != "Healthy") continue;

                    TryInfect(agent, other);
                }
            }

            // Progress disease (recovery / mortality)
            UpdateHealthProgression(agents);
        }

        private void TryInfect(Agent source, Agent target)
        {
            // Calculate infection chance
            double baseChance = _disease.InfectionRate * target.MedicalProfile.InfectionSusceptibility;

            // Boost indoors chances (limited ventilation etc)
            if (source.Workplace != null && source.Workplace == target.Workplace)
                baseChance *= INDOOR_MULTIPLIER;

            // Chronic conditions make infection more likely
            if (target.MedicalProfile.HasChronicCondition)
                baseChance *= 1.5;

            // Clamp to 1.0
            baseChance = Math.Min(baseChance, 1.0);

            // Determine infection event
            if (_rng.NextDouble() < baseChance)
            {
                target.Status = "Infected";
                target.InfectionProgress = 0.0;
            }
        }

        private void UpdateHealthProgression(List<Agent> agents)
        {
            foreach (var agent in agents)
            {
                switch (agent.Status)
                {
                    case "Infected":
                        agent.InfectionProgress += _disease.RecoveryRate;

                        // Death check first
                        if (_rng.NextDouble() < _disease.MortalityRate * agent.MedicalProfile.InfectionSusceptibility)
                        {
                            agent.Status = "Dead";
                            continue;
                        }

                        // Recovery check
                        if (agent.InfectionProgress >= 1.0)
                        {
                            agent.Status = "Recovered";
                            agent.MedicalProfile.Immunity = Math.Min(1.0, agent.MedicalProfile.Immunity + 0.2);
                        }
                        break;

                    case "Recovered":
                        // Gradual immunity decay
                        agent.MedicalProfile.Immunity = Math.Max(0.1, agent.MedicalProfile.Immunity - 0.001);
                        break;
                }
            }
        }
        private static double MetersToLatLonRadius(double latitude, double meters)
        {
            // Rough conversion: 1 degree lat ≈ 111km, 1 degree lon ≈ 111km * cos(lat)
            return meters / 111_000.0;
        }
    }

    // ---------------------------------------------------------
    // Supporting Spatial Structure
    // ---------------------------------------------------------
    public class Quadtree
    {
        private const int MAX_AGENTS = 10;
        private readonly double MinX, MaxX, MinY, MaxY;
        private readonly List<Agent> Agents = new();
        private Quadtree[]? Children = null;

        public Quadtree(double minX, double maxX, double minY, double maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public void Insert(Agent agent)
        {
            if (Children != null)
            {
                int quad = GetQuadrant(agent);
                Children[quad].Insert(agent);
                return;
            }

            Agents.Add(agent);

            if (Agents.Count > MAX_AGENTS)
                Subdivide();
        }

        private void Subdivide()
        {
            double midX = (MinX + MaxX) / 2;
            double midY = (MinY + MaxY) / 2;

            Children = new Quadtree[4];
            Children[0] = new Quadtree(MinX, midX, MinY, midY);
            Children[1] = new Quadtree(midX, MaxX, MinY, midY);
            Children[2] = new Quadtree(MinX, midX, midY, MaxY);
            Children[3] = new Quadtree(midX, MaxX, midY, MaxY);

            foreach (var a in Agents)
            {
                int quad = GetQuadrant(a);
                Children[quad].Insert(a);
            }
            Agents.Clear();
        }

        private int GetQuadrant(Agent agent)
        {
            double midX = (MinX + MaxX) / 2;
            double midY = (MinY + MaxY) / 2;

            bool left = agent.CurrentLon < midX;
            bool bottom = agent.CurrentLat < midY;

            if (left && bottom) return 0;
            if (!left && bottom) return 1;
            if (left && !bottom) return 2;
            return 3;
        }

        public void Query(double lat, double lon, double radius, List<Agent> result)
        {
            if (!IntersectsCircle(lat, lon, radius)) return;

            if (Children != null)
            {
                foreach (var child in Children)
                    child.Query(lat, lon, radius, result);
            }
            else
            {
                double radiusSq = radius * radius;
                foreach (var agent in Agents)
                {
                    double dx = agent.CurrentLat - lat;
                    double dy = agent.CurrentLon - lon;
                    if ((dx * dx + dy * dy) <= radiusSq)
                        result.Add(agent);
                }
            }
        }

        private bool IntersectsCircle(double lat, double lon, double radius)
        {
            double closestX = Math.Max(MinX, Math.Min(lon, MaxX));
            double closestY = Math.Max(MinY, Math.Min(lat, MaxY));
            double dx = closestX - lon;
            double dy = closestY - lat;
            return (dx * dx + dy * dy) <= radius * radius;
        }
    }
}