using MySimulationAPI.Models;

namespace MySimulationAPI.SimulationEngine
{
    public class SimulationEngine
    {
        public Simulation Simulation { get; private set; }
        public bool IsRunning { get; private set; } = false;
        private int _tick = 0;

        public SimulationEngine(Simulation simulation)
        {
            Simulation = simulation;
        }

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        // Advance one tick
        public void Tick()
        {
            if (!IsRunning) return;

            _tick++;

            foreach (var agent in Simulation.Agents)
            {
                MoveAgent(agent);
            }

            ComputeInteractions();
        }

        private void MoveAgent(Agent agent)
        {
            // Simple pseudo-schedule
            int hour = _tick % 24;

            if (agent.Type == "Student")
            {
                agent.CurrentLat = hour >= 8 && hour < 15
                    ? GetLocationLat("School")
                    : GetLocationLat("Home");
                agent.CurrentLon = hour >= 8 && hour < 15
                    ? GetLocationLon("School")
                    : GetLocationLon("Home");
            }
            else
            {
                agent.CurrentLat = hour >= 9 && hour < 17
                    ? GetLocationLat("Work")
                    : GetLocationLat("Home");
                agent.CurrentLon = hour >= 9 && hour < 17
                    ? GetLocationLon("Work")
                    : GetLocationLon("Home");
            }
        }

        private double GetLocationLat(string type)
        {
            var loc = Simulation.Locations.FirstOrDefault(l => l.Type == type);
            return loc?.Lat ?? 0;
        }

        private double GetLocationLon(string type)
        {
            var loc = Simulation.Locations.FirstOrDefault(l => l.Type == type);
            return loc?.Lon ?? 0;
        }

        private void ComputeInteractions()
        {
            double radius = 0.0005; // ~50 meters
            foreach (var a in Simulation.Agents.Where(a => a.HealthStatus == "Infected"))
            {
                foreach (var b in Simulation.Agents.Where(b => b.HealthStatus == "Healthy"))
                {
                    double dx = a.CurrentLat - b.CurrentLat;
                    double dy = a.CurrentLon - b.CurrentLon;
                    if (Math.Sqrt(dx * dx + dy * dy) < radius)
                    {
                        b.HealthStatus = "Infected";
                    }
                }
            }
        }

        public SimulationSnapshot GetSnapshot()
        {
            return new SimulationSnapshot
            {
                Tick = _tick,
                HealthyCount = Simulation.Agents.Count(a => a.HealthStatus == "Healthy"),
                InfectedCount = Simulation.Agents.Count(a => a.HealthStatus == "Infected"),
                RecoveredCount = Simulation.Agents.Count(a => a.HealthStatus == "Recovered")
            };
        }
    }
}