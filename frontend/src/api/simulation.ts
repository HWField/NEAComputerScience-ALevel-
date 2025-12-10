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
    incubationPeriod: number;
    infectiousPeriod: number;
    symptomaticRate: number;
    severeSymptomsRate: number;
    hospitalSeekingRate: number;
    immunityDuration: number;
    reinfectionResistance: number;
    maskEffectiveness: number;
    vaccineEffectiveness: number;
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
  tick: number;
  totalAgents: number;
  infected: number;
  recovered: number;
  dead: number;
  quarantined: number;
  healthy: number;
  totalInfections: number;
  activeInfections: number;
  symptomaticCases: number;
  reportedCases: number;
  totalDeaths: number;
  totalRecoveries: number;
}

export interface AgentPosition {
  id: number;
  lat: number;
  lon: number;
  healthState: "Healthy" | "Infected" | "Recovered" | "Dead";
  isIsolating: boolean;
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

// Mitigation types
export interface Mitigation {
  id: number;
  name: string;
  type: "SelfIsolation" | "MaskWearing" | "Vaccination";
  simulationId: number;
  isActive: boolean;
  createdAt: string;
  modifiedAt?: string;

  // Self-Isolation specific
  complianceRate?: number;
  symptomThreshold?: number;

  // Mask Wearing specific
  adoptionRate?: number;
  effectivenessModifier?: number;

  // Vaccination specific
  vaccinationRate?: number;
  daysToFullEffectiveness?: number;
}

export interface CreateMitigationDto {
  type: "SelfIsolation" | "MaskWearing" | "Vaccination";
  name?: string;

  // Self-Isolation parameters
  complianceRate?: number;
  symptomThreshold?: number;

  // Mask Wearing parameters
  adoptionRate?: number;
  maskEffectivenessModifier?: number;

  // Vaccination parameters
  vaccinationRate?: number;
  vaccineEffectivenessModifier?: number;
  daysToFullEffectiveness?: number;
}

export interface UpdateMitigationDto {
  name?: string;
  isActive?: boolean;

  // Self-Isolation parameters
  complianceRate?: number;
  symptomThreshold?: number;

  // Mask Wearing parameters
  adoptionRate?: number;
  maskEffectivenessModifier?: number;

  // Vaccination parameters
  vaccinationRate?: number;
  vaccineEffectivenessModifier?: number;
  daysToFullEffectiveness?: number;
}

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

  // Seed infection
  async seedInfection(id: number, count: number = 1): Promise<void> {
    await api.post(`/simulation/${id}/infect`, count);
  },

  // Get all mitigations for a simulation
  async getMitigations(id: number): Promise<Mitigation[]> {
    const response = await api.get(`/simulation/${id}/mitigations`);
    return response.data;
  },

  // Create a new mitigation
  async createMitigation(
    id: number,
    dto: CreateMitigationDto
  ): Promise<Mitigation> {
    const response = await api.post(`/simulation/${id}/mitigations`, dto);
    return response.data;
  },

  // Update an existing mitigation
  async updateMitigation(
    id: number,
    mitigationId: number,
    dto: UpdateMitigationDto
  ): Promise<Mitigation> {
    const response = await api.put(
      `/simulation/${id}/mitigations/${mitigationId}`,
      dto
    );
    return response.data;
  },

  // Delete a mitigation
  async deleteMitigation(id: number, mitigationId: number): Promise<void> {
    await api.delete(`/simulation/${id}/mitigations/${mitigationId}`);
  },
};
