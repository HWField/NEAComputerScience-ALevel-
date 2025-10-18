using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MySimulationAPI.SimulationConfiguration
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
                sw.Restart();
                Tick(); // run simulation logic
                sw.Stop();

                // Save periodic snapshots every 500 ticks (will need to be adjusted based on tick length)
                if (_context.Simulation.CurrentTick % 500 == 0)
                    await _context.Snapshots.SaveSnapshotAsync();

                // -------------------------
                // Calculate target delay
                // -------------------------
                double tickLengthMinutes = _context.Simulation.TickLengthMinutes;
                double playbackSpeed = _context.SimulationSpeed <= 0 ? 1.0 : _context.SimulationSpeed;

                // Convert tick length into real-world seconds (roughly)
                // Example: 5-minute tick @ 1x = 5 seconds real time
                double targetDelaySeconds = tickLengthMinutes / playbackSpeed;

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

            // Disease progression and infection spread
            _context.InfectionService.UpdateInfectionTick();

            // Advance simulation time
            _context.Simulation.CurrentTick++;
        }
    }
}