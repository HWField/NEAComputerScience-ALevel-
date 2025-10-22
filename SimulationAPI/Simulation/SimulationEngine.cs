using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MySimulationAPI.SimulationConfiguration;

namespace MySimulationAPI.SimulationRuntime
{
    public class SimulationEngine
    {
        private readonly SimulationContext _context;
        private CancellationTokenSource? _cts;

        public SimulationEngine(SimulationContext context)
        {
            _context = context;
        }

        public void Start()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                return; // already running

            _cts = new CancellationTokenSource();
            Task.Run(() => RunAsync(_cts.Token));
        }

        public void Stop() => _cts?.Cancel();

        private async Task RunAsync(CancellationToken token)
        {
            Console.WriteLine($"[SimulationEngine] Starting simulation {_context.Simulation.Id}...");
            var sw = new Stopwatch();

            while (!token.IsCancellationRequested)
            {
                Console.WriteLine($"[SimulationEngine] Beginning tick {_context.CurrentTick}...");
                sw.Restart();
                Tick(); // run simulation logic
                Console.WriteLine($"[SimulationEngine] Finished tick {_context.CurrentTick}.");
                sw.Stop();
                Console.WriteLine($"[SimulationEngine] Tick {_context.CurrentTick} took {sw.Elapsed.TotalSeconds:F2} seconds.");

                // Save periodic snapshots every 500 ticks (will later need to be adjusted based on tick length to trade off flexability vs storage use)
                if (_context.CurrentTick % 500 == 0)
                    await _context.Snapshots.SaveSnapshotAsync();

                // -------------------------
                // Calculate target delay
                // -------------------------

                // Convert tick length into real-world seconds (roughly)
                // Example: 5-minute tick @ 1x = 5 seconds real time
                double targetDelaySeconds = _context.TickLength / _context.SimulationSpeed;
                // Adjust for actual compute time to maintain consistent pacing
                double computeSeconds = sw.Elapsed.TotalSeconds;
                double remainingSeconds = Math.Max(0, targetDelaySeconds - computeSeconds);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(remainingSeconds), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            Console.WriteLine($"[SimulationEngine] Simulation stopped.");
        }

        private void Tick()
        {
            // Advance all agents according to their routines
            _context.AgentService.UpdateAgentsForTick();
            Console.WriteLine($"[SimulationEngine] Agents updated for tick {_context.CurrentTick}.");
            // Disease progression and infection spread
            _context.InfectionService.UpdateInfectionTick();
            Console.WriteLine($"[SimulationEngine] Infection updated for tick {_context.CurrentTick}.");
            // Advance simulation time
            _context.CurrentTick++;
            Console.WriteLine($"[SimulationEngine] Tick {_context.CurrentTick} completed.");
        }
    }
}