import { ref, onUnmounted } from "vue";
import * as signalR from "@microsoft/signalr";
import { useUserStore } from "@/stores/useUserStore";
import type {
  SimulationState,
  TickUpdate,
  MetricsUpdate,
  ConnectionStatus,
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

  // Event handlers
  const onTickUpdate = ref<((data: TickUpdate) => void) | null>(null);
  const onMetricsUpdate = ref<((data: MetricsUpdate) => void) | null>(null);
  const onStateChange = ref<((data: SimulationState) => void) | null>(null);

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
        currentState.value = data;
        if (onStateChange.value) {
          onStateChange.value(data);
        }
      });

      hubConnection.on("TickUpdate", (data: TickUpdate) => {
        if (currentState.value) {
          currentState.value.CurrentTick = data.Tick;
        }
        if (onTickUpdate.value) {
          onTickUpdate.value(data);
        }
      });

      hubConnection.on("MetricsUpdate", (data: MetricsUpdate) => {
        latestMetrics.value = data;
        if (onMetricsUpdate.value) {
          onMetricsUpdate.value(data);
        }
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
      await leaveSimulation();
      await connection.value.stop();
      connection.value = null;
      connectionStatus.value = "disconnected";
      console.log("[SimulationHub] Disconnected");
    } catch (err: any) {
      console.error("[SimulationHub] Error during disconnect:", err);
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

    // Event handlers
    onTickUpdate,
    onMetricsUpdate,
    onStateChange,

    // Methods
    connect,
    disconnect,
    joinSimulation,
    leaveSimulation,
  };
}
