import { ref, onUnmounted } from "vue";
import * as signalR from "@microsoft/signalr";
import { useUserStore } from "@/stores/useUserStore";
import type {
  SimulationState,
  TickUpdate,
  MetricsUpdate,
  ConnectionStatus,
  AgentPosition,
  AgentPositionUpdate,
} from "@/api/simulation";

export function useSimulationHub(simulationId: number) {
  const userStore = useUserStore();
  const connection = ref<signalR.HubConnection | null>(null);
  const connectionStatus = ref<ConnectionStatus>("disconnected");
  const reconnectAttempts = ref(0);
  const error = ref<string | null>(null);

  // State from server
  const currentState = ref<SimulationState | null>(null);
  const latestMetrics = ref<MetricsUpdate | null>(null);
  const currentInterpolatedTick = ref<number>(0);
  const maxComputedTick = ref<number>(0);

  // Agent tracking state
  const agentPositions = ref<Map<number, AgentPosition>>(new Map());
  const isTrackingAgents = ref(false);
  const trackedAgentIds = ref<number[]>([]);

  // Interpolation state
  let lastServerTick = 0;
  let lastServerTickTime = 0;
  let estimatedTicksPerSecond = 0;
  let interpolationFrameId: number | null = null;

  // Event handlers
  const onTickUpdate = ref<((data: TickUpdate) => void) | null>(null);
  const onMetricsUpdate = ref<((data: MetricsUpdate) => void) | null>(null);
  const onStateChange = ref<((data: SimulationState) => void) | null>(null);

  /**
   * Calculate expected ticks per second based on simulation speed and tick length
   * matches the server's calculation: ticksPerSecond = speed * (86400 / tickLength)
   */
  const calculateExpectedTickRate = (): number => {
    if (!currentState.value || currentState.value.speed === 0) {
      return 0;
    }

    const tickLength = currentState.value.tickLength; // seconds per tick
    const speed = currentState.value.speed; // speed multiplier

    // Match server calculation from SimulationEngine.cs:
    // ticksPerDay = 86400.0 / tickLength
    // ticksPerSecond = speed * ticksPerDay
    const ticksPerDay = 86400.0 / tickLength;
    const ticksPerSecond = speed * ticksPerDay;

    return ticksPerSecond;
  };

  /**
   * Update interpolation based on server tick update
   */
  const updateInterpolation = (serverTick: number | undefined) => {
    // Validate input
    if (serverTick === undefined || !isFinite(serverTick) || serverTick < 0) {
      console.warn(
        `[SimulationHub] Invalid server tick received: ${serverTick}`
      );
      return;
    }

    const now = performance.now() / 1000; // current time in seconds

    if (lastServerTickTime > 0 && lastServerTick > 0) {
      // Calculate actual tick rate based on server updates
      const tickDelta = serverTick - lastServerTick;
      const timeDelta = now - lastServerTickTime;

      if (timeDelta > 0 && tickDelta > 0) {
        const actualTicksPerSecond = tickDelta / timeDelta;

        // Smooth the estimation using exponential moving average
        // Higher alpha = faster response to changes, lower alpha = smoother
        const alpha = 0.4;
        if (estimatedTicksPerSecond === 0) {
          estimatedTicksPerSecond = actualTicksPerSecond;
        } else {
          estimatedTicksPerSecond =
            alpha * actualTicksPerSecond +
            (1 - alpha) * estimatedTicksPerSecond;
        }
      }
    } else {
      // First update - use theoretical rate
      estimatedTicksPerSecond = calculateExpectedTickRate();
    }

    // Update tracking variables
    lastServerTick = serverTick;
    lastServerTickTime = now;
    currentInterpolatedTick.value = serverTick;
  };

  /**
   * Animation loop for smooth tick interpolation
   */
  const interpolateLoop = () => {
    const status = currentState.value?.status;

    // Only interpolate when running normally
    if (
      !currentState.value ||
      (status !== "Running" && status !== "Computing")
    ) {
      // Not running, don't interpolate
      interpolationFrameId = requestAnimationFrame(interpolateLoop);
      return;
    }

    // Only interpolate if we have a valid baseline
    if (lastServerTickTime > 0 && estimatedTicksPerSecond > 0) {
      const now = performance.now() / 1000;
      const timeSinceLastUpdate = now - lastServerTickTime;

      // Calculate interpolated tick
      const ticksElapsed = timeSinceLastUpdate * estimatedTicksPerSecond;
      const interpolatedTick = lastServerTick + ticksElapsed;

      // Clamp interpolation to not exceed maxComputedTick
      // This prevents the progress bar from going beyond what's actually computed
      const clampedTick = Math.min(interpolatedTick, maxComputedTick.value);

      // Only extrapolate up to 150ms ahead (1.5 broadcast intervals)
      // This prevents runaway interpolation if broadcasts stop
      const maxExtrapolationTime = 0.15; // seconds
      if (timeSinceLastUpdate > maxExtrapolationTime) {
        // Hold at the last known position if haven't received updates
        currentInterpolatedTick.value = lastServerTick;
      } else if (isFinite(clampedTick) && clampedTick >= 0) {
        currentInterpolatedTick.value = clampedTick;
      }
    }

    // Continue the loop
    interpolationFrameId = requestAnimationFrame(interpolateLoop);
  };

  /**
   * Start the interpolation animation loop
   */
  const startInterpolation = () => {
    if (interpolationFrameId !== null) return; // already running
    interpolationFrameId = requestAnimationFrame(interpolateLoop);
  };

  /**
   * Stop the interpolation animation loop
   */
  const stopInterpolation = () => {
    if (interpolationFrameId !== null) {
      cancelAnimationFrame(interpolationFrameId);
      interpolationFrameId = null;
    }
  };

  const connect = async () => {
    if (connection.value || connectionStatus.value === "connecting") return;

    connectionStatus.value = "connecting";
    error.value = null;

    try {
      const hubConnection = new signalR.HubConnectionBuilder()
        .withUrl("http://127.0.0.1:5081/hubs/simulation", {
          accessTokenFactory: () => userStore.token || "",
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            // Exponential backoff: 0s, 2s, 4s, 8s, 16s, then 30s max
            if (retryContext.previousRetryCount === 0) return 0;
            const delay = Math.min(
              2000 * Math.pow(2, retryContext.previousRetryCount - 1),
              30000
            );
            return delay;
          },
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Set up event handlers
      hubConnection.on("SimulationState", (data: SimulationState) => {
        console.log("[SimulationHub] Received SimulationState:", data);

        const wasRunning = currentState.value?.status === "Running";
        const wasComputing = currentState.value?.status === "Computing";
        const isNowRunning = data.status === "Running";
        const isNowComputing = data.status === "Computing";

        // Set state first, before any calculations that depend on it
        currentState.value = data;

        // Ensure maxComputedTick tracks the furthest computed point
        if (data.currentTick > maxComputedTick.value) {
          maxComputedTick.value = data.currentTick;
        }

        // Update interpolation when state changes
        updateInterpolation(data.currentTick);

        // Handle running state transitions
        if ((isNowRunning || isNowComputing) && !(wasRunning || wasComputing)) {
          // Just started running or computing
          const newExpectedRate = calculateExpectedTickRate();
          if (newExpectedRate > 0) {
            estimatedTicksPerSecond = newExpectedRate;
            console.log(
              `[SimulationHub] Simulation ${
                data.status
              }, expected rate: ${newExpectedRate.toFixed(4)} tps`
            );
          }
          startInterpolation();
        } else if (
          (isNowRunning || isNowComputing) &&
          data.speed !== currentState.value?.speed
        ) {
          // Speed changed while running
          const newExpectedRate = calculateExpectedTickRate();
          if (newExpectedRate > 0) {
            estimatedTicksPerSecond = newExpectedRate;
            console.log(
              `[SimulationHub] Speed changed to ${
                data.speed
              }x, new rate: ${newExpectedRate.toFixed(4)} tps`
            );
          }
        } else if (!isNowRunning && !isNowComputing) {
          // Paused or stopped
          stopInterpolation();
          if (data.currentTick !== undefined) {
            currentInterpolatedTick.value = data.currentTick;
          }
        }

        if (onStateChange.value) {
          onStateChange.value(data);
        }
      });

      hubConnection.on("TickUpdate", (data: TickUpdate) => {
        // Update server tick in state
        if (currentState.value) {
          currentState.value.currentTick = data.currentTick;
        }

        // Update max computed tick from server
        // Ensure maxComputedTick is always >= currentTick
        if (
          data.maximumTick !== undefined &&
          data.maximumTick >= data.currentTick
        ) {
          maxComputedTick.value = data.maximumTick;
        } else {
          maxComputedTick.value = Math.max(
            maxComputedTick.value,
            data.currentTick
          );
        }

        // Update interpolation with new server tick
        updateInterpolation(data.currentTick);

        if (onTickUpdate.value) {
          onTickUpdate.value(data);
        }
      });

      hubConnection.on("MetricsUpdate", (data: MetricsUpdate) => {
        latestMetrics.value = data;

        if (data.tick > maxComputedTick.value) {
          maxComputedTick.value = data.tick;
        }

        // Metrics also contain tick, use it to refine interpolation
        updateInterpolation(data.tick);

        if (onMetricsUpdate.value) {
          onMetricsUpdate.value(data);
        }
      });

      // Agent position updates
      hubConnection.on("AgentPositions", (data: AgentPositionUpdate) => {
        for (const agent of data.agents) {
          agentPositions.value.set(agent.id, agent);
        }
      });

      hubConnection.on(
        "AgentTrackingStarted",
        (data: { simulationId: number; agentIds: number[]; count: number }) => {
          trackedAgentIds.value = data.agentIds;
          isTrackingAgents.value = true;
          console.log(`[SimulationHub] Started tracking ${data.count} agents`);
        }
      );

      hubConnection.on("AgentTrackingStopped", () => {
        isTrackingAgents.value = false;
        agentPositions.value.clear();
        trackedAgentIds.value = [];
        console.log("[SimulationHub] Stopped tracking agents");
      });

      // Connection state handlers
      hubConnection.onreconnecting((err) => {
        reconnectAttempts.value++;
        console.warn(
          `[SimulationHub] Connection lost, reconnecting (attempt ${reconnectAttempts.value})...`,
          err
        );

        // After 3 failed attempts, show a more severe status
        if (reconnectAttempts.value >= 3) {
          connectionStatus.value = "failed";
          error.value = `Connection unstable. Reconnecting (attempt ${reconnectAttempts.value})...`;
        } else {
          connectionStatus.value = "reconnecting";
          error.value = "Connection lost, attempting to reconnect...";
        }
      });

      hubConnection.onreconnected((connectionId) => {
        console.log("[SimulationHub] Reconnected:", connectionId);
        console.log("[SimulationHub] Setting connectionStatus to 'connected'");
        connectionStatus.value = "connected";
        reconnectAttempts.value = 0; // Reset counter on successful reconnection
        error.value = null;
        joinSimulation();
      });

      hubConnection.onclose((err) => {
        console.error("[SimulationHub] Connection closed:", err);
        console.log(
          "[SimulationHub] Setting connectionStatus to 'disconnected'"
        );
        connectionStatus.value = "disconnected";
        connection.value = null;
        if (err) {
          error.value = "Connection to server lost";
        }
      });

      await hubConnection.start();
      connection.value = hubConnection;
      connectionStatus.value = "connected";
      console.log("[SimulationHub] Connected successfully");

      // Join the simulation room
      await joinSimulation();
    } catch (err: any) {
      console.error("[SimulationHub] Connection failed:", err);
      error.value = err.message || "Failed to connect to simulation hub";
      connectionStatus.value = "disconnected";
    }
  };

  const joinSimulation = async () => {
    if (
      !connection.value ||
      connection.value.state !== signalR.HubConnectionState.Connected
    ) {
      console.warn("[SimulationHub] Cannot join - not connected");
      return;
    }

    try {
      await connection.value.invoke("JoinSimulation", simulationId);
      console.log(`[SimulationHub] Joined simulation ${simulationId}`);
    } catch (err: any) {
      console.error("[SimulationHub] Failed to join simulation:", err);
      error.value = err.message || "Failed to join simulation";
    }
  };

  const leaveSimulation = async () => {
    if (
      !connection.value ||
      connection.value.state !== signalR.HubConnectionState.Connected
    ) {
      return;
    }

    try {
      await connection.value.invoke("LeaveSimulation", simulationId);
      console.log(`[SimulationHub] Left simulation ${simulationId}`);
    } catch (err: any) {
      console.error("[SimulationHub] Failed to leave simulation:", err);
    }
  };

  const disconnect = async () => {
    if (!connection.value) return;

    try {
      stopInterpolation();
      await stopAgentTracking();
      await leaveSimulation();
      await connection.value.stop();
      connection.value = null;
      connectionStatus.value = "disconnected";
      console.log("[SimulationHub] Disconnected");
    } catch (err: any) {
      console.error("[SimulationHub] Error during disconnect:", err);
    }
  };

  const startAgentTracking = async (agentCount: number = 1000) => {
    if (
      !connection.value ||
      connection.value.state !== signalR.HubConnectionState.Connected
    ) {
      console.warn(
        "[SimulationHub] Cannot start agent tracking - not connected"
      );
      return;
    }

    try {
      await connection.value.invoke(
        "StartAgentTracking",
        simulationId,
        agentCount
      );
      console.log(
        `[SimulationHub] Requested tracking for ${agentCount} agents`
      );
    } catch (err: any) {
      console.error("[SimulationHub] Failed to start agent tracking:", err);
      error.value = err.message || "Failed to start agent tracking";
    }
  };

  const stopAgentTracking = async () => {
    if (
      !connection.value ||
      connection.value.state !== signalR.HubConnectionState.Connected
    ) {
      return;
    }

    try {
      await connection.value.invoke("StopAgentTracking");
      isTrackingAgents.value = false;
      agentPositions.value.clear();
      trackedAgentIds.value = [];
      console.log("[SimulationHub] Stopped agent tracking");
    } catch (err: any) {
      console.error("[SimulationHub] Failed to stop agent tracking:", err);
    }
  };

  onUnmounted(() => {
    disconnect();
  });

  return {
    // Connection state
    connection,
    connectionStatus,
    reconnectAttempts,
    error,

    // Data
    currentState,
    latestMetrics,
    currentInterpolatedTick,
    maxComputedTick,

    // Agent tracking
    agentPositions,
    isTrackingAgents,
    trackedAgentIds,

    // Event handlers
    onTickUpdate,
    onMetricsUpdate,
    onStateChange,

    // Methods
    connect,
    disconnect,
    joinSimulation,
    leaveSimulation,
    startAgentTracking,
    stopAgentTracking,
  };
}
