using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.SimulationConfiguration;
using MySimulationAPI.Hubs;

namespace MySimulationAPI.SimulationRuntime
{
    public class SimulationEngine
    {
        private readonly SimulationContext _context;
        private readonly IHubContext<SimulationHub> _hubContext;
        private CancellationTokenSource? _cts;
        private System.Threading.Timer? _snapshotTimer;
        private long metricsIntervalTicks;
        private readonly SemaphoreSlim _snapshotSemaphore = new SemaphoreSlim(1, 1);
        private volatile bool _snapshotRequested = false;
        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public SimulationEngine(SimulationContext context, IHubContext<SimulationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public void Start(long? targetTick = null)
        {

            if (_cts != null && !_cts.IsCancellationRequested)
                return; // already running

            // Set status based on whether computing to a target or running normally
            if (targetTick.HasValue && targetTick.Value > _context.CurrentTick)
            {
                _context.Status = "Computing";
            }
            else
            {
                _context.Status = "Running";
            }

            _cts = new CancellationTokenSource();

            metricsIntervalTicks = _context.Simulation.MetricRecordingFrequency;
            // Start realtime snapshot timer if in real time mode
            if (!targetTick.HasValue)
            {
                var interval = TimeSpan.FromSeconds(_context.Simulation.SnapshotSavingFrequency);
                _snapshotTimer = new System.Threading.Timer(
                    _ => _snapshotRequested = true, // signal snapshot request
                    null,
                    interval, // first snapshot after interval
                    interval  // then every interval
                );
            }

            Task.Run(() => RunAsync(_cts.Token, targetTick));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _snapshotTimer?.Dispose();
            _snapshotTimer = null;
        }

        private async Task RunAsync(CancellationToken token, long? targetTick)
        {
            Console.WriteLine($"[SimulationEngine] Starting simulation {_context.Simulation.Id}...");
            using var db = _context.DbFactory.CreateDbContext();
            bool tick0Exists = await db.SimulationSnapshots
                .AnyAsync(s => s.SimulationId == _context.Simulation.Id && s.Tick == 0);
            if (!tick0Exists)
            {
                // Save a snapshot of the initial state
                await _context.SnapshotManager.SaveSnapshotAsync();
            }

            // Calculate 1 year limit in ticks (365 days)
            long oneYearTicks = (long)(TimeSpan.FromDays(365).TotalSeconds / _context.TickLength);

            var sw = new Stopwatch();
            while (!token.IsCancellationRequested)
            {
                sw.Restart();
                Tick(); // run simulation logic
                sw.Stop();

                // Check if real-time snapshot was requested (only in non-target mode)
                if (_snapshotRequested && !targetTick.HasValue)
                {
                    // get semaphore to ensure atomic snapshot
                    await _snapshotSemaphore.WaitAsync(token);
                    try
                    {
                        Console.WriteLine($"[SimulationEngine] Real-time snapshot at tick {_context.CurrentTick}");
                        await _context.SnapshotManager.SaveSnapshotAsync();
                        _snapshotRequested = false;
                    }
                    finally
                    {
                        _snapshotSemaphore.Release();
                    }
                }

                if (_context.CurrentTick % metricsIntervalTicks == 0)
                {
                    await _context.MetricsManager.RecordAsync();
                }

                // auto pause on 1 year limit
                if (_context.CurrentTick >= oneYearTicks)
                {
                    _context.Status = "Paused";
                    await SimulationHub.BroadcastState(_hubContext, _context.Simulation.Id, _context);
                    Stop();
                    break;
                }

                // auto pause on target tick
                if (targetTick.HasValue && _context.CurrentTick >= targetTick.Value)
                {
                    _context.Status = "Paused";
                    await SimulationHub.BroadcastState(_hubContext, _context.Simulation.Id, _context);
                    Stop();
                    break;
                }

                // Calculate target delay
                // Only apply delay if no target tick (real-time mode)
                // When target tick is set, run at full speed
                if (!targetTick.HasValue)
                {
                    double ticksPerDay = 86400.0 / _context.TickLength;
                    double ticksPerSecond = _context.SimulationSpeed;

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
            _context.CurrentTick++;

            // Track maximum computed tick
            if (_context.CurrentTick > _context.MaxComputedTick)
            {
                _context.MaxComputedTick = _context.CurrentTick;
            }
        }
    }
}