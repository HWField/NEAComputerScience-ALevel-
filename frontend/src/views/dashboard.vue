<script setup lang="ts">
import { ref, onMounted } from "vue";
import axios from "../api/axios";
import { useToastStore } from "@/stores/useToastStore";
import { useRouter } from "vue-router";
import SimulationDetailsModal from "@/components/SimulationDetailsModal.vue";
import CreateSimulationModal from "@/components/CreateSimulationModal.vue";
const toast = useToastStore();
const router = useRouter();

interface Simulation {
  id: number;
  name: string;
  status: "running" | "completed" | "paused" | "failed";
  agentCount: number;
  mapArea: string;
  diseaese: string;
  progress?: number;
  createdAt: string;
}

// Stats data
const totalSimulations = ref(0);
const activeSimulations = ref(0);
const completedSimulations = ref(0);
const totalAgents = ref("");
const avgDuration = ref(0);

// Simulations data
const simulations = ref<Simulation[]>([]);

const showCreateModal = ref(false);
const showIconPicker = ref(false);

const previewData = ref(null);
const showSneakModal = ref(false);

const openSimulation = (id: number) => {
  router.push({ name: "simulation", params: { id: id.toString() } });
};

onMounted(async () => {
  await fetchSimulations();
});

const fetchSimulations = async () => {
  try {
    const res = await axios.get("/Simulation");
    simulations.value = res.data
      .sort(
        (a: any, b: any) =>
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      )
      .map((sim: any) => ({
        id: sim.id,
        name: sim.name,
        status: sim.status,
        agentCount: sim.agentCount,
        mapArea: sim.mapArea,
        diseaese: sim.diseaese,
        progress: 0,
        createdAt: sim.createdAt,
      }));

    totalSimulations.value = simulations.value.length;
    activeSimulations.value = simulations.value.filter(
      (s) => s.status === "running"
    ).length;
    completedSimulations.value = simulations.value.filter(
      (s) => s.status === "completed"
    ).length;
    totalAgents.value = simulations.value
      .reduce((sum, s) => sum + s.agentCount, 0)
      .toLocaleString();
    avgDuration.value = Math.round(
      simulations.value.reduce((sum, s) => sum + (s.progress || 0), 0) /
        (simulations.value.length || 1)
    );
  } catch (error) {
    console.error("Failed to fetch simulations:", error);
    simulations.value = [];
  }
};

// simulation data preview
const showDetailsModal = ref(false);
const selectedSimulationId = ref<number | null>(null);
const sneakSimulation = (id: number) => {
  selectedSimulationId.value = id;
  showDetailsModal.value = true;
};

const createNewSimulation = () => {
  showCreateModal.value = true;
};
</script>

<template>
  <div class="container mx-auto p-6 space-y-6">
    <div class="flex justify-between items-center">
      <div>
        <h1 class="text-3xl font-bold">Dashboard</h1>
        <p class="text-base-content/70 mt-1">
          Manage and monitor your simulations
        </p>
      </div>
      <button @click="createNewSimulation" class="btn btn-primary gap-2">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          class="h-5 w-5"
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <path
            fill-rule="evenodd"
            d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z"
            clip-rule="evenodd"
          />
        </svg>
        New Simulation
      </button>
    </div>

    <!-- Stats Section -->
    <div class="stats stats-vertical lg:stats-horizontal shadow w-full">
      <div class="stat">
        <div class="stat-figure text-primary">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
            />
          </svg>
        </div>
        <div class="stat-title">Total Simulations</div>
        <div class="stat-value text-primary">{{ totalSimulations }}</div>
        <div class="stat-desc">All time</div>
      </div>

      <div class="stat">
        <div class="stat-figure text-secondary">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M13 10V3L4 14h7v7l9-11h-7z"
            />
          </svg>
        </div>
        <div class="stat-title">Active</div>
        <div class="stat-value text-secondary">{{ activeSimulations }}</div>
        <div class="stat-desc">Currently running</div>
      </div>

      <div class="stat">
        <div class="stat-figure text-success">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="stat-title">Completed</div>
        <div class="stat-value text-success">{{ completedSimulations }}</div>
        <div class="stat-desc">Finished simulations</div>
      </div>

      <div class="stat">
        <div class="stat-figure text-info">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
            />
          </svg>
        </div>
        <div class="stat-title">Total Agents</div>
        <div class="stat-value">
          {{ totalAgents }}
        </div>
        <div class="stat-desc">Across all simulations</div>
      </div>

      <div class="stat">
        <div class="stat-figure text-accent">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
        <div class="stat-title">Avg Duration</div>
        <div class="stat-value text-accent">{{ avgDuration }}</div>
        <div class="stat-desc">Days per simulation</div>
      </div>
    </div>

    <!-- Simulations Grid -->
    <div>
      <h2 class="text-2xl font-semibold mb-4">Simulations:</h2>

      <!-- Empty State -->
      <div v-if="simulations.length === 0" class="card bg-base-200">
        <div class="card-body items-center text-center py-16">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-24 w-24 text-base-content/20 mb-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
          </svg>
          <h3 class="text-xl font-bold">No simulations yet</h3>
          <p class="text-base-content/70 mt-2">
            Get started by creating your first epidemic simulation
          </p>
          <button @click="createNewSimulation" class="btn btn-primary mt-4">
            Create Your First Simulation
          </button>
        </div>
      </div>

      <!-- Simulations Grid -->
      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div
          v-for="simulation in simulations"
          :key="simulation.id"
          class="card bg-base-100 shadow-lg hover:shadow-xl transition-shadow cursor-pointer"
          @click="openSimulation(simulation.id)"
        >
          <div class="card-body">
            <!-- Status Badge -->
            <div class="flex justify-between items-start mb-2">
              <h3 class="card-title text-lg">{{ simulation.name }}</h3>
              <div
                class="badge"
                :class="{
                  'badge-success': simulation.status === 'completed',
                  'badge-info': simulation.status === 'running',
                  'badge-warning': simulation.status === 'paused',
                  'badge-error': simulation.status === 'failed',
                }"
              >
                {{ simulation.status }}
              </div>
            </div>

            <!-- Simulation Info -->
            <div class="space-y-2 text-sm">
              <div class="flex justify-between">
                <span class="text-base-content/70">Name:</span>
                <span class="font-medium capitalize">{{
                  simulation.name
                }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Agents:</span>
                <span class="font-medium">{{ simulation.agentCount }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Created:</span>
                <span class="font-medium"
                  >{{
                    new Date(simulation.createdAt).toLocaleString(undefined, {
                      day: "2-digit",
                      month: "2-digit",
                      year: "2-digit",
                      hour: "2-digit",
                      minute: "2-digit",
                      hour12: false,
                    })
                  }}
                </span>
              </div>
            </div>

            <!-- Progress Bar (for running simulations) -->
            <div v-if="simulation.status === 'running'" class="mt-4">
              <div class="flex justify-between text-xs mb-1">
                <span>Progress</span>
                <span>{{ simulation.progress }}%</span>
              </div>
              <progress
                class="progress progress-primary w-full"
                :value="simulation.progress"
                max="100"
              ></progress>
            </div>

            <!-- Card Actions -->
            <div class="card-actions justify-end mt-4">
              <button
                class="btn btn-sm btn-ghost"
                @click.stop="sneakSimulation(simulation.id)"
              >
                View Details
              </button>
              <button
                class="btn btn-sm btn-primary"
                @click.stop="openSimulation(simulation.id)"
              >
                Open
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <SimulationDetailsModal
      v-model="showDetailsModal"
      :simulation-id="selectedSimulationId"
      @deleted="fetchSimulations"
      @open="openSimulation"
    />

    <CreateSimulationModal
      v-model="showCreateModal"
      @created="fetchSimulations"
    />
  </div>
</template>

<style scoped></style>
