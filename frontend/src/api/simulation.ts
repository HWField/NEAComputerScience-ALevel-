import api from "./axios";

export interface Simulation {
  id: number;
  name: string;
  createdAt: string;
  status: "running" | "completed" | "paused" | "failed";
  progress?: number;
  mapArea: string | null;
  disease: string | null;
  agentCount: number;
}

export interface SimulationDetail {
  id: number;
  name: string;
  status: string;
  createdAt: string;
  progress: number;
  tickLength: number;
  disease: {
    id: number;
    name: string;
    infectionRate: number;
    recoveryRate: number;
    mortalityRate: number;
  } | null;
  mapArea: {
    id: number;
    name: string;
    centerLat: number;
    centerLon: number;
    radiusKm: number;
    buildingCount: number;
  } | null;
  agentCount: number;
}

// SignalR/Hub related types
export interface SimulationState {
  currentTick: number;
  status: string;
  speed: number;
  tickLength: number;
}

export interface TickUpdate {
  currentTick: number;
  maximumTick: number;
  timestamp: number;
}

export interface MetricsUpdate {
  currentTick: number;
  TotalAgents: number;
  Infected: number;
  Recovered: number;
  Dead: number;
  Quarantined: number;
  Healthy: number;
}

export interface AgentPosition {
  id: number;
  lat: number;
  lon: number;
  healthState: "Healthy" | "Infected" | "Recovered" | "Dead";
  isQuarantined: boolean;
}

export interface AgentPositionUpdate {
  simulationId: number;
  tick: number;
  agents: AgentPosition[];
  timestamp: number;
}

export type ConnectionStatus =
  | "disconnected"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "failed";

export const simulationApi = {
  // Get all simulations
  async getAll(): Promise<Simulation[]> {
    const response = await api.get("/simulation");
    return response.data;
  },

  // Get single simulation details
  async getById(id: number): Promise<SimulationDetail> {
    const response = await api.get(`/simulation/${id}`);
    return response.data;
  },

  // Load simulation into memory
  async load(id: number): Promise<void> {
    await api.post(`/simulation/${id}/load`);
  },

  // Start/resume simulation
  async run(id: number, targetTick?: number): Promise<void> {
    await api.post(`/simulation/${id}/run`, targetTick || null);
  },

  // Pause simulation
  async pause(id: number): Promise<void> {
    await api.post(`/simulation/${id}/pause`);
  },

  // Unload simulation from memory
  async unload(id: number): Promise<void> {
    await api.post(`/simulation/${id}/unload`);
  },

  // Set simulation speed
  async setSpeed(id: number, speedMultiplier: number): Promise<void> {
    await api.post(`/simulation/${id}/speed`, speedMultiplier);
  },

  // Set current tick
  async setTick(id: number, tick: number): Promise<void> {
    await api.post(`/simulation/${id}/tick`, tick);
  },

  // Delete simulation
  async delete(id: number): Promise<void> {
    await api.delete(`/simulation/${id}`);
  },

  // Reset simulation (remove computed data)
  async reset(id: number): Promise<void> {
    await api.delete(`/simulation/${id}/reset`);
  },
};
