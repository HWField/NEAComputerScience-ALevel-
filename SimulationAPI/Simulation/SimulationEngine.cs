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

            // ComputeInteractions
        }

        private void MoveAgent(Agent agent)
        {

        }

    }
}