using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using MySimulationAPI.Hubs;
using MySimulationAPI.SimulationConfiguration;
using SimulationRuntime;

namespace MySimulationAPI.Services
{

    public class SimulationBroadcastService : IDisposable
    {
        private readonly IHubContext<SimulationHub> _hubContext;
        private readonly System.Threading.Timer _broadcastTimer;
        private const int BroadcastIntervalMs = 333; // ~3 updates per second

        // Track which agents are assigned to which clients
        // Key: connectionId, Value: (simulationId, agentIds[])
        private readonly ConcurrentDictionary<string, (int SimulationId, int[] AgentIds)> _clientAgents = new();

        // Track which simulations have active tracking
        private readonly ConcurrentDictionary<int, SimulationContext> _activeSimulations = new();

        public SimulationBroadcastService(IHubContext<SimulationHub> hubContext)
        {
            _hubContext = hubContext;

            // Start periodic broadcast timer
            _broadcastTimer = new System.Threading.Timer(
                BroadcastAllAgentPositions,
                null,
                BroadcastIntervalMs,
                BroadcastIntervalMs
            );
        }

        /// <summary>
        /// Allocates a set of agents to a specific client connection for tracking.
        /// Uses a deterministic algorithm to ensure consistent allocation.
        /// </summary>
        public int[] AllocateAgentsToClient(
            string connectionId,
            int simulationId,
            SimulationContext context,
            int requestedCount)
        {
            // Remove any existing allocation for this connection
            _clientAgents.TryRemove(connectionId, out _);

            // Register the simulation context if not already tracked
            _activeSimulations.TryAdd(simulationId, context);

            var agents = context.AgentStates;
            if (agents.Length == 0)
            {
                Console.WriteLine($"[AgentTracking] No agents available in simulation {simulationId}");
                return Array.Empty<int>();
            }

            // Cap requested count to available agents
            int allocatedCount = Math.Min(requestedCount, agents.Length);

            // Deterministic selection: evenly distribute agents which ensures each client gets a representative sample
            int[] selectedAgentIds = new int[allocatedCount];
            float step = (float)agents.Length / allocatedCount;

            for (int i = 0; i < allocatedCount; i++)
            {
                int index = (int)(i * step);
                selectedAgentIds[i] = agents[index].Id;
            }

            // Store allocation
            _clientAgents[connectionId] = (simulationId, selectedAgentIds);

            Console.WriteLine($"[AgentTracking] Allocated {allocatedCount} agents to connection {connectionId.Substring(0, 8)}...");
            return selectedAgentIds;
        }

        /// <summary>
        /// Removes agent tracking for a disconnected client.
        /// </summary>
        public void RemoveClientTracking(string connectionId)
        {
            if (_clientAgents.TryRemove(connectionId, out var allocation))
            {
                Console.WriteLine($"[AgentTracking] Removed tracking for connection {connectionId.Substring(0, 8)}...");

                // Clean up simulation if no more clients tracking it
                CleanupSimulationIfUnused(allocation.SimulationId);
            }
        }

        /// <summary>
        /// Periodic broadcast of agent positions and health metrics to tracking clients only.
        /// </summary>
        private async void BroadcastAllAgentPositions(object? state)
        {
            if (_clientAgents.IsEmpty)
            {
                return; // No clients tracking, skip
            }

            // Broadcast agent positions and metrics to clients who requested tracking
            foreach (var (connectionId, allocation) in _clientAgents)
            {
                try
                {
                    var (simulationId, agentIds) = allocation;

                    // Get the simulation context
                    if (!_activeSimulations.TryGetValue(simulationId, out var context))
                        continue;

                    // Gather current positions for tracked agents
                    var agentPositions = GatherAgentPositions(context, agentIds);

                    // Calculate metrics for this simulation
                    var metrics = CalculateHealthMetrics(context);

                    // Send both agent positions and metrics to the specific tracking client
                    var client = _hubContext.Clients.Client(connectionId);

                    if (agentPositions.Length > 0)
                    {
                        await client.SendAsync("AgentPositions", new
                        {
                            simulationId,
                            tick = context.CurrentTick,
                            agents = agentPositions,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });
                    }
                    else
                    {
                        Console.WriteLine($"[AgentTracking] Warning: No agent positions found for client {connectionId.Substring(0, 8)}... tracking {agentIds.Length} agents");
                    }

                    // Send metrics update to the same tracking client
                    await client.SendAsync("MetricsUpdate", new
                    {
                        tick = context.CurrentTick,
                        totalAgents = metrics.Total,
                        infected = metrics.Infected,
                        recovered = metrics.Recovered,
                        dead = metrics.Dead,
                        quarantined = metrics.Quarantined,
                        healthy = metrics.Healthy,
                        totalInfections = context.InfectionService.TotalInfections,
                        activeInfections = context.InfectionService.ActiveInfections,
                        symptomaticCases = context.InfectionService.SymptomaticCases,
                        reportedCases = context.InfectionService.ReportedCases,
                        totalDeaths = context.InfectionService.TotalDeaths,
                        totalRecoveries = context.InfectionService.TotalRecoveries
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AgentTracking] Error broadcasting to {connectionId.Substring(0, 8)}...: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Efficiently calculates current health metrics from agent states.
        /// </summary>
        private HealthMetrics CalculateHealthMetrics(SimulationContext context)
        {
            var agents = context.AgentStates;
            if (agents == null || agents.Length == 0)
            {
                return new HealthMetrics { Total = 0 };
            }
            int total = agents.Length;
            int infected = 0;
            int recovered = 0;
            int dead = 0;
            int quarantined = 0;

            // Single pass through all agents for efficiency
            for (int i = 0; i < agents.Length; i++)
            {
                ref var agent = ref agents[i];

                switch ((AgentHealthState)agent.HealthState)
                {
                    case AgentHealthState.Infected:
                        infected++;
                        break;
                    case AgentHealthState.Recovered:
                        recovered++;
                        break;
                    case AgentHealthState.Dead:
                        dead++;
                        break;
                }

                if (agent.IsIsolating)
                {
                    quarantined++;
                }
            }

            int healthy = total - infected - recovered - dead;

            return new HealthMetrics
            {
                Total = total,
                Healthy = healthy,
                Infected = infected,
                Recovered = recovered,
                Dead = dead,
                Quarantined = quarantined
            };
        }

        /// <summary>
        /// Efficiently gathers position data for specific agents.
        /// </summary>
        private AgentPositionDto[] GatherAgentPositions(SimulationContext context, int[] agentIds)
        {
            var agents = context.AgentStates;
            var positions = new AgentPositionDto[agentIds.Length];
            int foundCount = 0;

            // Create a HashSet for O(1) lookup
            var targetIds = new HashSet<int>(agentIds);

            // Single pass through agent array
            for (int i = 0; i < agents.Length && foundCount < agentIds.Length; i++)
            {
                ref var agent = ref agents[i];
                if (targetIds.Contains(agent.Id))
                {
                    positions[foundCount++] = new AgentPositionDto
                    {
                        Id = agent.Id,
                        Lat = agent.Lat,
                        Lon = agent.Lon,
                        HealthState = GetHealthStateString((AgentHealthState)agent.HealthState),
                        isIsolating = agent.IsIsolating,
                        CurrentBuildingId = agent.CurrentBuildingId > 0 ? agent.CurrentBuildingId : null
                    };
                }
            }

            // Return only filled positions
            if (foundCount < positions.Length)
            {
                Array.Resize(ref positions, foundCount);
            }

            return positions;
        }

        private static string GetHealthStateString(AgentHealthState state)
        {
            return state switch
            {
                AgentHealthState.Healthy => "Healthy",
                AgentHealthState.Infected => "Infected",
                AgentHealthState.Recovered => "Recovered",
                AgentHealthState.Dead => "Dead",
                _ => "Healthy"
            };
        }

        /// <summary>
        /// Removes simulation from tracking if no clients are watching it.
        /// </summary>
        private void CleanupSimulationIfUnused(int simulationId)
        {
            // Check if any clients are still tracking this simulation
            bool stillInUse = _clientAgents.Values.Any(v => v.SimulationId == simulationId);

            if (!stillInUse)
            {
                _activeSimulations.TryRemove(simulationId, out _);
                Console.WriteLine($"[AgentTracking] Removed simulation {simulationId} from tracking");
            }
        }

        public void Dispose()
        {
            _broadcastTimer?.Dispose();
            _clientAgents.Clear();
            _activeSimulations.Clear();
        }
    }
    public class AgentPositionDto
    {
        public int Id { get; set; }
        public float Lat { get; set; }
        public float Lon { get; set; }
        public string HealthState { get; set; } = "Healthy";
        public bool isIsolating { get; set; }
        public int? CurrentBuildingId { get; set; }
    }

    public class HealthMetrics
    {
        public int Total { get; set; }
        public int Healthy { get; set; }
        public int Infected { get; set; }
        public int Recovered { get; set; }
        public int Dead { get; set; }
        public int Quarantined { get; set; }
    }
}
