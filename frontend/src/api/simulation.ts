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
  initialInfectionCount: number;
  metricRecordingFrequency: number;
  snapshotSavingFrequency: number;
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
  agentBreakdown: {
    students: number;
    officeWorkers: number;
    medicalWorkers: number;
    retailWorkers: number;
    serviceWorkers: number;
    elderlyRetired: number;
  };
}

// SignalR/Hub related types
// These instances enable real-time updates from the backend to the frontend, allowing the UI to reflect changes in simulation state, tick progression, and metrics without needing to poll the server continually.
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

export interface SimulationMetric {
  tick: number;
  totalAgents: number;
  infected: number;
  recovered: number;
  dead: number;
  quarantined: number;
  healthy: number;
}

export interface MetricsResponse {
  metrics: SimulationMetric[];
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
export interface UpdateMitigationDto {
  type: "SelfIsolation" | "MaskWearing" | "Vaccination";
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

export interface UpdateDiseaseDto {
  infectionRate?: number;
  recoveryRate?: number;
  mortalityRate?: number;
  incubationPeriod?: number;
  infectiousPeriod?: number;
  symptomaticRate?: number;
  severeSymptomsRate?: number;
  hospitalSeekingRate?: number;
  immunityDuration?: number;
  reinfectionResistance?: number;
  maskEffectiveness?: number;
  vaccineEffectiveness?: number;
}

export interface UpdateInitialInfectionDto {
  count: number;
}

export interface UpdateMetricFrequencyDto {
  frequency: number;
}

export interface UpdateSnapshotFrequencyDto {
  frequency: number;
}

export const simulationApi = {
  // Making Api calls relating to the simulation, these functions abstract away the details of the HTTP requests and provide a clean interface for the rest of the frontend to interact with the backend API.
  async getAll(): Promise<Simulation[]> {
    const response = await api.get("/simulation");
    return response.data;
  },

  async getById(id: number): Promise<SimulationDetail> {
    const response = await api.get(`/simulation/${id}`);
    return response.data;
  },

  async load(id: number): Promise<void> {
    await api.post(`/simulation/${id}/load`);
  },

  async run(id: number, targetTick?: number): Promise<void> {
    await api.post(`/simulation/${id}/run`, targetTick || null);
  },

  async pause(id: number): Promise<void> {
    await api.post(`/simulation/${id}/pause`);
  },

  async unload(id: number): Promise<void> {
    await api.post(`/simulation/${id}/unload`);
  },
  async setSpeed(id: number, speedMultiplier: number): Promise<void> {
    await api.post(`/simulation/${id}/speed`, speedMultiplier);
  },

  async setTick(id: number, tick: number): Promise<void> {
    await api.post(`/simulation/${id}/tick`, tick);
  },

  async delete(id: number): Promise<void> {
    await api.delete(`/simulation/${id}`);
  },

  async reset(id: number): Promise<void> {
    await api.delete(`/simulation/${id}/reset`);
  },

  async seedInfection(id: number, count: number = 1): Promise<void> {
    await api.post(`/simulation/${id}/infect`, count);
  },

  async getMitigations(id: number): Promise<Mitigation[]> {
    const response = await api.get(`/simulation/${id}/mitigations`);
    return response.data;
  },

  async updateMitigations(
    id: number,
    mitigations: UpdateMitigationDto[],
  ): Promise<Mitigation[]> {
    const response = await api.put(`/simulation/${id}/mitigations`, {
      mitigations,
    });
    return response.data;
  },

  async updateDisease(
    id: number,
    dto: UpdateDiseaseDto,
  ): Promise<{ message: string; requiresReset: boolean }> {
    const response = await api.put(`/simulation/${id}/disease`, dto);
    return response.data;
  },

  async updateInitialInfection(
    id: number,
    dto: UpdateInitialInfectionDto,
  ): Promise<{ message: string; initialInfectionCount: number }> {
    const response = await api.put(`/simulation/${id}/initial-infection`, dto);
    return response.data;
  },

  async updateMetricFrequency(
    id: number,
    dto: UpdateMetricFrequencyDto,
  ): Promise<{ message: string; metricRecordingFrequency: number }> {
    const response = await api.put(`/simulation/${id}/metric-frequency`, dto);
    return response.data;
  },

  async updateSnapshotFrequency(
    id: number,
    dto: UpdateSnapshotFrequencyDto,
  ): Promise<{ message: string; snapshotSavingFrequency: number }> {
    const response = await api.put(`/simulation/${id}/snapshot-frequency`, dto);
    return response.data;
  },

  // Get historical metrics for analysis
  async getMetrics(
    id: number,
    startTick?: number,
    endTick?: number,
  ): Promise<MetricsResponse> {
    const params = new URLSearchParams();
    if (startTick !== undefined)
      params.append("startTick", startTick.toString());
    if (endTick !== undefined) params.append("endTick", endTick.toString());

    const response = await api.get(
      `/simulation/${id}/metrics${
        params.toString() ? `?${params.toString()}` : ""
      }`,
    );
    return response.data;
  },
};
