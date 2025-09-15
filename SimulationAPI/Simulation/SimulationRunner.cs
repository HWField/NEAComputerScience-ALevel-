using Microsoft.Extensions.Hosting;
using MySimulationAPI.Models;
using System.Collections.Concurrent;

namespace MySimulationAPI.SimulationEngine
{
    public class SimulationRunner : BackgroundService
    {
        private readonly int _maxActiveSimulations = 3;
        private readonly List<SimulationEngine> _activeSimulations = new();
        private readonly ConcurrentQueue<SimulationEngine> _queue = new();

        public void EnqueueSimulation(SimulationEngine engine)
        {
            if (_activeSimulations.Count < _maxActiveSimulations)
            {
                _activeSimulations.Add(engine);
                engine.Start();
            }
            else
            {
                _queue.Enqueue(engine);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var sim in _activeSimulations.ToList())
                {
                    sim.Tick();
                    // TODO: persist snapshot asynchronously

                    // Example end condition
                    // if (!sim.Simulation.Agents.Any(a => a.HealthStatus == "Infected"))
                    // {
                    //     sim.Stop();
                    //     _activeSimulations.Remove(sim);

                    //     if (_queue.TryDequeue(out var nextSim))
                    //     {
                    //         _activeSimulations.Add(nextSim);
                    //         nextSim.Start();
                    //     }
                    // }
                }

                await Task.Delay(1000, stoppingToken); // 1 second per tick
            }
        }
    }
}