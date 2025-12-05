using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySimulationAPI.Data;
using MySimulationAPI.SimulationConfiguration;
using MySimulationAPI.Services;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace MySimulationAPI.Hubs
{
    [Authorize]
    public class SimulationHub : Hub
    {
        private readonly SimulationManager _simulationManager;
        private readonly IDbContextFactory<SimDbContext> _dbFactory;
        private readonly SimulationBroadcastService _simulationBroadcastService;
        private static readonly ConcurrentDictionary<string, int> _connectionSimulations = new();

        public SimulationHub(SimulationManager simulationManager, IDbContextFactory<SimDbContext> dbFactory, SimulationBroadcastService simulationBroadcastService)
        {
            _simulationManager = simulationManager;
            _dbFactory = dbFactory;
            _simulationBroadcastService = simulationBroadcastService;
        }

        private int GetUserId() => int.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        private bool IsAdmin() => Context.User?.IsInRole("Admin") ?? false;

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove from simulation group when disconnected
            if (_connectionSimulations.TryGetValue(Context.ConnectionId, out var simId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Simulation_{simId}");
                _connectionSimulations.TryRemove(Context.ConnectionId, out _);
            }

            // Remove agent tracking for this client
            _simulationBroadcastService.RemoveClientTracking(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinSimulation(int simulationId)
        {

            var userId = GetUserId();

            using var db = _dbFactory.CreateDbContext();
            var simulation = await db.Simulations
                .Where(s => s.Id == simulationId)
                .Select(s => new { s.Id, s.UserId })
                .FirstOrDefaultAsync();

            if (simulation == null)
            {
                throw new HubException("Simulation not found.");
            }

            if (simulation.UserId != userId && !IsAdmin())
            {
                throw new HubException("Not authorized to join this simulation.");
            }

            // Store which simulation this connection is watching
            _connectionSimulations[Context.ConnectionId] = simulationId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Simulation_{simulationId}");

            // Send initial state
            if (_simulationManager.TryGet(simulationId, out var context))
            {
                await Clients.Caller.SendAsync("SimulationState", new
                {
                    CurrentTick = context.CurrentTick,
                    Status = context.Status,
                    Speed = context.SimulationSpeed,
                    TickLength = context.TickLength
                });
            }
        }


        public async Task LeaveSimulation(int simulationId)
        {
            _connectionSimulations.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Simulation_{simulationId}");
            _simulationBroadcastService.RemoveClientTracking(Context.ConnectionId);
        }


        public async Task StartAgentTracking(int simulationId, int agentCount = 1000)
        {
            var userId = GetUserId();

            using var db = _dbFactory.CreateDbContext();
            var simulation = await db.Simulations
                .Where(s => s.Id == simulationId)
                .Select(s => new { s.Id, s.UserId })
                .FirstOrDefaultAsync();

            if (simulation == null)
            {
                throw new HubException("Simulation not found.");
            }

            if (simulation.UserId != userId && !IsAdmin())
            {
                throw new HubException("Not authorized to track agents in this simulation.");
            }

            if (!_simulationManager.TryGet(simulationId, out var context))
            {
                throw new HubException("Simulation is not currently loaded.");
            }

            // Allocate agents to the client
            var allocatedAgentIds = _simulationBroadcastService.AllocateAgentsToClient(
                Context.ConnectionId,
                simulationId,
                context,
                agentCount
            );

            // Send confirmation with allocated agent IDs
            await Clients.Caller.SendAsync("AgentTrackingStarted", new
            {
                simulationId,
                agentIds = allocatedAgentIds,
                count = allocatedAgentIds.Length,
                updateFrequency = 3 // Hz
            });
        }

        public async Task StopAgentTracking()
        {
            _simulationBroadcastService.RemoveClientTracking(Context.ConnectionId);
            await Clients.Caller.SendAsync("AgentTrackingStopped");
        }


        public static async Task BroadcastTickUpdate(IHubContext<SimulationHub> hubContext, int simulationId, long currentTick, long maximumTick)
        {
            await hubContext.Clients.Group($"Simulation_{simulationId}").SendAsync("TickUpdate", new
            {
                currentTick,
                maximumTick,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        public static async Task BroadcastMetrics(IHubContext<SimulationHub> hubContext, int simulationId, object metrics)
        {
            await hubContext.Clients.Group($"Simulation_{simulationId}").SendAsync("MetricsUpdate", metrics);
        }

        public static async Task BroadcastState(IHubContext<SimulationHub> hubContext, int simulationId, SimulationContext context)
        {
            await hubContext.Clients.Group($"Simulation_{simulationId}").SendAsync("SimulationState", new
            {
                CurrentTick = context.CurrentTick,
                Status = context.Status,
                Speed = context.SimulationSpeed,
                TickLength = context.TickLength
            });
        }
    }
}
