using System.Collections.Generic;

namespace MySimulationAPI.Models
{

    public class SimulationEngine
    {
        private readonly Simulation _sim;
        private int _tick = 0;
        public bool IsRunning { get; private set; } = false;

        public SimulationEngine(Simulation sim)
        {
            _sim = sim;
        }

        public void Start() => IsRunning = true;

        public void Tick()
        {
            if (!IsRunning) return;

            _tick++;

            foreach (var agent in _sim.Agents)
            {
                MoveAgent(agent);
            }

            SpreadInfections();
        }

        private void MoveAgent(Agent agent)
        {
            // Simple daily schedule: morning to work, lunch out, back to work, home
            int hour = _tick % 24;

            if (hour < 8 || hour >= 18) // home
            {
                agent.CurrentLat = agent.HomeLocation.Lat;
                agent.CurrentLon = agent.HomeLocation.Lon;
            }
            else if (hour == 12) // lunch (random small offset near work)
            {
                agent.CurrentLat = agent.WorkLocation.Lat + Random.Shared.NextDouble() * 0.001;
                agent.CurrentLon = agent.WorkLocation.Lon + Random.Shared.NextDouble() * 0.001;
            }
            else // work
            {
                agent.CurrentLat = agent.WorkLocation.Lat;
                agent.CurrentLon = agent.WorkLocation.Lon;
            }
        }

        private void SpreadInfections()
        {
            double infectionRadius = 0.0005; // about 50m
            foreach (var a in _sim.Agents.Where(a => a.HealthStatus == "Infected"))
            {
                foreach (var b in _sim.Agents.Where(b => b.HealthStatus == "Healthy"))
                {
                    if (Distance(a, b) < infectionRadius && Random.Shared.NextDouble() < 0.05)
                    {
                        b.HealthStatus = "Infected";
                    }
                }
            }
        }

        private double Distance(Agent a, Agent b)
        {
            double dx = a.CurrentLat - b.CurrentLat;
            double dy = a.CurrentLon - b.CurrentLon;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}