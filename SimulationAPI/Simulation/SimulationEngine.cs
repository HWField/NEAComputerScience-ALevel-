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
        private long snapshotIntervalTicks = 0;
        private long metricsIntervalTicks = 0;
        public SimulationEngine(SimulationContext context)
        {
            _context = context;
        }

        public void Start()
        {
            // snapshots will save every 14 in sim days be default (can later scale depending on compute times etc)
            snapshotIntervalTicks = (long)(TimeSpan.FromDays(14).TotalSeconds / _context.TickLength);
            // metrics recorded twice every sim day
            metricsIntervalTicks = (long)((TimeSpan.FromDays(1).TotalSeconds / _context.TickLength) / 2);
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

                if (_context.CurrentTick % snapshotIntervalTicks == 0)
                {
                    Console.WriteLine($"[SimulationEngine] Saving snapshot at tick {_context.CurrentTick}...");
                    await _context.Snapshots.SaveSnapshotAsync();
                    Console.WriteLine($"[SimulationEngine] Snapshot saved.");
                }
                if (_context.CurrentTick % metricsIntervalTicks == 0)
                {
                    Console.WriteLine($"[SimulationEngine] Saving metrics at tick {_context.CurrentTick}...");
                    await _context.MetricsManager.RecordAsync();
                    Console.WriteLine($"[SimulationEngine] Metrics saved.");

                }
                // -------------------------
                // Calculate target delay
                // -------------------------

                double ticksPerDay = 86400.0 / _context.TickLength;
                double ticksPerSecond = _context.SimulationSpeed * ticksPerDay;

                double targetDelaySeconds = (_context.SimulationSpeed == 0)
                    ? 0
                    : 1.0 / ticksPerSecond;

                double computeSeconds = sw.Elapsed.TotalSeconds;
                double remainingSeconds = Math.Max(0, targetDelaySeconds - computeSeconds);

                if (_context.SimulationSpeed != 0)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(remainingSeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
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