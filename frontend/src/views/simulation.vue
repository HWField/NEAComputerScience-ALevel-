<template>
  <div class="min-h-screen bg-base-200">
    <!-- Header -->
    <div class="bg-base-100 border-b border-base-300">
      <div class="container mx-auto px-6 py-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-4">
            <button @click="$router.back()" class="btn btn-ghost btn-sm gap-2">
              <i-ph-arrow-left class="w-5 h-5" />
              Back
            </button>
            <div class="divider divider-horizontal"></div>
            <div>
              <h1 class="text-2xl font-bold">
                {{ simulationData?.name || "Loading..." }}
              </h1>
            </div>
          </div>
          <div class="flex items-center gap-3">
            <div class="badge badge-lg" :class="statusBadgeClass">
              <span
                class="w-2 h-2 rounded-full mr-2"
                :class="statusIndicatorClass"
              ></span>
              {{ displayStatus }}
            </div>
            <div
              v-if="hubState.connectionStatus.value === 'connected'"
              class="badge badge-success gap-2"
            >
              <i-ph-wifi-high class="w-4 h-4" />
              Live
            </div>
            <div
              v-else-if="hubState.connectionStatus.value === 'connecting'"
              class="badge badge-warning gap-2"
            >
              <span class="loading loading-spinner loading-xs"></span>
              Connecting
            </div>
            <div
              v-else-if="hubState.connectionStatus.value === 'reconnecting'"
              class="badge badge-warning gap-2"
            >
              <span class="loading loading-spinner loading-xs"></span>
              Reconnecting
            </div>
            <div
              v-else-if="hubState.connectionStatus.value === 'failed'"
              class="badge badge-error gap-2"
            >
              <i-ph-warning-circle class="w-4 h-4" />
              Connection Issues
            </div>
            <div
              v-else-if="hubState.connectionStatus.value === 'disconnected'"
              class="badge badge-error gap-2"
            >
              <i-ph-wifi-slash class="w-4 h-4" />
              Offline
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div
      v-if="isInitializing"
      class="flex items-center justify-center min-h-[60vh]"
    >
      <div class="text-center">
        <span class="loading loading-spinner loading-lg text-primary"></span>
        <p class="mt-4 text-base-content/70">Initialising simulation...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="container mx-auto px-6 py-12">
      <div class="alert alert-error">
        <i-ph-warning-circle class="w-6 h-6" />
        <span>{{ error }}</span>
      </div>
    </div>

    <div v-else class="container mx-auto px-6 py-6">
      <!-- Connection Error Alert -->
      <div
        v-if="hubState.error && hubState.connectionStatus.value !== 'connected'"
        class="alert alert-warning mb-6"
      >
        <i-ph-warning class="w-6 h-6" />
        <span>{{ hubState.error }}</span>
      </div>

      <div class="grid grid-cols-12 gap-6">
        <!-- Left Sidebar - Disease & Population Stats -->
        <div class="col-span-3 space-y-6">
          <!-- Disease Info Card -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-virus class="w-5 h-5 text-error" />
                Disease Profile
              </h3>
              <div v-if="simulationData?.disease" class="space-y-3 mt-4">
                <div>
                  <div class="text-xs text-base-content/70 mb-1">Name</div>
                  <div class="font-semibold">
                    {{ simulationData.disease.name }}
                  </div>
                </div>
                <div class="grid grid-cols-2 gap-2">
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Infection
                    </div>
                    <div class="text-error font-bold">
                      {{
                        (simulationData.disease.infectionRate * 100).toFixed(1)
                      }}%
                    </div>
                  </div>
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Recovery
                    </div>
                    <div class="text-success font-bold">
                      {{
                        (simulationData.disease.recoveryRate * 100).toFixed(1)
                      }}%
                    </div>
                  </div>
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Mortality
                    </div>
                    <div class="text-warning font-bold">
                      {{
                        (simulationData.disease.mortalityRate * 100).toFixed(1)
                      }}%
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="text-sm text-base-content/60 italic">
                No disease configured
              </div>
            </div>
          </div>

          <!-- Population Stats Card -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-users class="w-5 h-5 text-primary" />
                Population
              </h3>
              <div class="space-y-4 mt-4">
                <!-- Total Agents -->
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title text-xs">Total Agents</div>
                  <div class="stat-value text-2xl">
                    {{
                      formatNumber(
                        metrics?.TotalAgents || simulationData?.agentCount || 0
                      )
                    }}
                  </div>
                </div>

                <!-- Health Breakdown -->
                <div class="space-y-2">
                  <div class="flex justify-between items-center">
                    <span class="text-sm flex items-center gap-2">
                      <span class="w-3 h-3 rounded-full bg-success"></span>
                      Healthy
                    </span>
                    <span class="font-bold text-success">{{
                      formatNumber(metrics?.Healthy || 0)
                    }}</span>
                  </div>
                  <div class="flex justify-between items-center">
                    <span class="text-sm flex items-center gap-2">
                      <span class="w-3 h-3 rounded-full bg-error"></span>
                      Infected
                    </span>
                    <span class="font-bold text-error">{{
                      formatNumber(metrics?.Infected || 0)
                    }}</span>
                  </div>
                  <div class="flex justify-between items-center">
                    <span class="text-sm flex items-center gap-2">
                      <span class="w-3 h-3 rounded-full bg-info"></span>
                      Recovered
                    </span>
                    <span class="font-bold text-info">{{
                      formatNumber(metrics?.Recovered || 0)
                    }}</span>
                  </div>
                  <div class="flex justify-between items-center">
                    <span class="text-sm flex items-center gap-2">
                      <span class="w-3 h-3 rounded-full bg-base-300"></span>
                      Deceased
                    </span>
                    <span class="font-bold text-base-content/70">{{
                      formatNumber(metrics?.Dead || 0)
                    }}</span>
                  </div>
                  <div class="flex justify-between items-center">
                    <span class="text-sm flex items-center gap-2">
                      <span class="w-3 h-3 rounded-full bg-warning"></span>
                      Quarantined
                    </span>
                    <span class="font-bold text-warning">{{
                      formatNumber(metrics?.Quarantined || 0)
                    }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Map Area Info -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-map-pin-bold class="w-5 h-5 text-accent" />
                Map Area
              </h3>
              <div v-if="simulationData?.mapArea" class="space-y-3 mt-4">
                <div>
                  <div class="text-xs text-base-content/70 mb-1">Location</div>
                  <div class="font-semibold">
                    {{ simulationData.mapArea.name }}
                  </div>
                </div>
                <div class="grid grid-cols-2 gap-2">
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">Radius</div>
                    <div class="font-bold">
                      {{ simulationData.mapArea.radiusKm }} km
                    </div>
                  </div>
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Buildings
                    </div>
                    <div class="font-bold">
                      {{ formatNumber(simulationData.mapArea.buildingCount) }}
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="text-sm text-base-content/60 italic">
                No map area configured
              </div>
            </div>
          </div>
        </div>

        <!-- Center - Map View -->
        <div class="col-span-6 space-y-6">
          <!-- Map Card -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h2 class="card-title text-2xl mb-6 flex items-center gap-3">
                <i-ph-map-trifold class="w-7 h-7 text-primary" />
                Simulation Area
              </h2>

              <!-- Map Container -->
              <div
                v-if="simulationData?.mapArea"
                class="rounded-xl border border-base-300 overflow-hidden h-[500px]"
              >
                <SimulationMap
                  :mapArea="simulationData.mapArea"
                  :agents="agentLocations"
                  :maxDisplayedAgents="2000"
                />
              </div>
              <div
                v-else
                class="bg-base-200 rounded-xl p-12 border border-base-300 h-[500px] flex items-center justify-center"
              >
                <div class="text-center text-base-content/50">
                  <i-ph-map-trifold class="w-16 h-16 mx-auto mb-3 opacity-50" />
                  <p>No map area configured</p>
                </div>
              </div>

              <!-- Map Stats -->
              <div
                v-if="simulationData?.mapArea"
                class="grid grid-cols-3 gap-4 mt-4"
              >
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title text-xs">Buildings</div>
                  <div class="stat-value text-xl">
                    {{ formatNumber(simulationData.mapArea.buildingCount) }}
                  </div>
                </div>
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title text-xs">Coverage Area</div>
                  <div class="stat-value text-xl">
                    {{
                      (
                        Math.PI * Math.pow(simulationData.mapArea.radiusKm, 2)
                      ).toFixed(1)
                    }}
                    km²
                  </div>
                </div>
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title text-xs">Population</div>
                  <div class="stat-value text-xl">
                    {{ formatNumber(simulationData?.agentCount || 0) }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Sidebar - System Metrics -->
        <div class="col-span-3 space-y-6">
          <!-- Performance Metrics -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-chart-line class="w-5 h-5 text-warning" />
                Performance
              </h3>
              <div class="space-y-4 mt-4">
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title text-xs">Server Tick Rate</div>
                  <div class="stat-value text-2xl">
                    {{ serverTickRate.toFixed(1) }}
                    <span class="text-base">tps</span>
                  </div>
                </div>
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title text-xs">Avg Latency</div>
                  <div class="stat-value text-2xl">{{ avgLatency }}ms</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Infection Metrics -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-trend-up class="w-5 h-5 text-error" />
                Infection Rate
              </h3>
              <div class="space-y-4 mt-4">
                <div>
                  <div class="flex justify-between mb-2">
                    <span class="text-sm text-base-content/70">Current</span>
                    <span class="font-bold text-error"
                      >{{ infectionRate }}%</span
                    >
                  </div>
                  <!-- <progress
                    class="progress progress-error w-full"
                    :value="infectionRate"
                    max="100"
                  ></progress> -->
                </div>
                <div>
                  <div class="flex justify-between mb-2">
                    <span class="text-sm text-base-content/70">Recovery</span>
                    <span class="font-bold text-info">{{ recoveryRate }}%</span>
                  </div>
                  <!-- <progress
                    class="progress progress-info w-full"
                    :value="recoveryRate"
                    max="100"
                  ></progress> -->
                </div>
              </div>
            </div>
          </div>

          <!-- Quick Actions -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-lightning-bold class="w-5 h-5 text-accent" />
                Quick Actions
              </h3>
              <div class="space-y-2 mt-4">
                <button
                  class="btn btn-outline btn-sm w-full gap-2"
                  @click="handleReset"
                >
                  <i-ph-arrow-clockwise class="w-4 h-4" />
                  Reset Simulation
                </button>
                <button
                  class="btn btn-outline btn-sm w-full gap-2 btn-error"
                  @click="handleUnload"
                >
                  <i-ph-eject-simple class="w-4 h-4" />
                  Unload from Memory
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Bottom Timeline Control -->
      <div class="mt-6">
        <div class="card bg-base-100 shadow-xl">
          <div class="card-body">
            <div class="flex items-center justify-between mb-6">
              <h3 class="text-lg font-semibold flex items-center gap-2">
                <i-ph-clock class="w-5 h-5" />
                Timeline Control
              </h3>

              <!-- Time Display -->
              <div class="text-center">
                <div
                  class="text-sm text-base-content/70 uppercase tracking-wide"
                >
                  Simulation Time
                </div>
                <div
                  class="text-2xl font-mono font-bold text-primary tabular-nums"
                >
                  {{ formattedTime }}
                </div>
                <div class="text-xs text-base-content/60">
                  Tick:
                  <span class="font-mono">{{ formatNumber(currentTick) }}</span>
                </div>
              </div>

              <!-- Playback Controls -->
              <div class="flex gap-2">
                <button
                  class="btn btn-circle btn-sm"
                  :class="isRunning ? 'btn-warning' : 'btn-primary'"
                  @click="togglePlayPause"
                  :disabled="!isLoadedInMemory"
                >
                  <i-ph-pause class="w-5 h-5" v-if="isRunning" />
                  <i-ph-play class="w-5 h-5" v-else />
                </button>
                <button
                  class="btn btn-circle btn-sm"
                  @click="handleStepBack"
                  :disabled="!isLoadedInMemory || isRunning"
                >
                  <i-ph-skip-back-bold class="w-5 h-5" />
                </button>
                <button
                  class="btn btn-circle btn-sm"
                  @click="handleStepForward"
                  :disabled="!isLoadedInMemory || isRunning"
                >
                  <i-ph-skip-forward-bold class="w-5 h-5" />
                </button>
              </div>
            </div>

            <!-- Timeline Slider -->
            <div class="relative mb-6">
              <input
                type="range"
                min="0"
                :max="maxTick"
                :value="currentTick"
                @input="handleTickChange"
                class="range range-primary"
                :disabled="isRunning"
              />
              <div
                class="flex justify-between text-xs text-base-content/60 mt-2"
              >
                <span>Start</span>
                <span>{{ formatTime(Math.floor(maxTick / 2)) }}</span>
                <span>{{ formatTime(maxTick) }}</span>
              </div>
            </div>

            <!-- Speed Control -->
            <div class="bg-base-200 rounded-xl p-4 border border-base-300">
              <div class="flex items-center justify-between mb-3">
                <span class="text-sm font-medium flex items-center gap-2">
                  <i-ph-gauge class="w-4 h-4" />
                  Simulation Speed
                </span>
                <span class="badge badge-primary badge-sm"
                  >{{ currentSpeed }}x</span
                >
              </div>
              <input
                type="range"
                min="0"
                max="10"
                :value="currentSpeed"
                @input="handleSpeedChange"
                class="range range-primary range-sm"
                step="0.5"
                :disabled="!isLoadedInMemory"
              />
              <div
                class="flex justify-between text-xs text-base-content/60 mt-1"
              >
                <span>0x</span>
                <span>5x</span>
                <span>10x</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useSimulationHub } from "@/composables/useSimulationHub";
import { simulationApi, type SimulationDetail } from "@/api/simulation";
import SimulationMap from "@/components/SimulationMap.vue";

interface AgentLocation {
  id: number;
  lat: number;
  lon: number;
  status: "Healthy" | "Infected" | "Recovered" | "Quarantined" | "Dead";
}
const route = useRoute();
const router = useRouter();
const id = computed(() => parseInt(route.params.id as string));

const isInitializing = ref(true);
const error = ref<string | null>(null);
const simulationData = ref<SimulationDetail | null>(null);
const isLoadedInMemory = ref(false);
const agentLocations = ref<AgentLocation[]>([]);

const hubState = useSimulationHub(id.value);
const {
  currentState,
  latestMetrics,
  onTickUpdate,
  onMetricsUpdate,
  connect,
  disconnect,
} = hubState;

const currentTick = computed(() => currentState.value?.currentTick || 0);

const isRunning = computed(() => {
  const status = currentState.value?.status;
  console.log("[Simulation.vue] isRunning computed - Status:", status);
  return status === "Running";
});
const currentSpeed = computed(() => currentState.value?.speed || 1);
const displayStatus = computed(() => {
  const status = currentState.value?.status || "Unknown";
  console.log("[Simulation.vue] displayStatus computed:", status);
  return status;
});
const metrics = computed(() => latestMetrics.value);

// Watch for status changes
watch(
  currentState,
  (newState) => {
    console.log("[Simulation.vue] currentState changed:", newState);
    console.log("[Simulation.vue] New status:", newState?.status);
  },
  { deep: true }
);

// Performance tracking
const serverTickRate = ref(0);
const avgLatency = ref(0);
const tickHistory = ref<{ tick: number; timestamp: number }[]>([]);

// Max tick for timeline - need to decide how to approach this later
const maxTick = ref(100000);

// Status badge styling
const statusBadgeClass = computed(() => {
  const status = displayStatus.value.toLowerCase();
  if (status === "running") return "badge-success";
  if (status === "loaded" || status === "paused") return "badge-warning";
  if (status === "error") return "badge-error";
  return "badge-ghost";
});

const statusIndicatorClass = computed(() => {
  const status = displayStatus.value.toLowerCase();
  if (status === "running") return "bg-success animate-pulse";
  if (status === "loaded" || status === "paused") return "bg-warning";
  if (status === "error") return "bg-error animate-pulse";
  return "bg-base-content/50";
});

const formattedTime = computed(() => {
  return formatTime(currentTick.value);
});

const infectionRate = computed(() => {
  if (!metrics.value || metrics.value.TotalAgents === 0) return 0;
  return ((metrics.value.Infected / metrics.value.TotalAgents) * 100).toFixed(
    1
  );
});

const recoveryRate = computed(() => {
  if (!metrics.value || metrics.value.TotalAgents === 0) return 0;
  return ((metrics.value.Recovered / metrics.value.TotalAgents) * 100).toFixed(
    1
  );
});

function formatNumber(num: number): string {
  return new Intl.NumberFormat().format(num);
}

function formatTime(tick: number): string {
  const tickLength = simulationData.value?.status === "Loaded" ? 300 : 300; // default 5 min
  const totalSeconds = tick * tickLength;
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);

  if (days > 0) {
    return `${days}d ${hours}h ${minutes}m`;
  } else if (hours > 0) {
    return `${hours}h ${minutes}m`;
  } else {
    return `${minutes}m`;
  }
}

async function loadSimulation() {
  try {
    isInitializing.value = true;
    error.value = null;
    // Connect to SignalR
    await connect();

    // Fetch simulation details
    simulationData.value = await simulationApi.getById(id.value);
    console.error("Simulation Data:", simulationData.value);

    // Load into server memory
    await simulationApi.load(id.value);
    isLoadedInMemory.value = true;
  } catch (err: any) {
    console.error("Failed to load simulation:", err);
    error.value = err.message || "Failed to load simulation";
  } finally {
    isInitializing.value = false;
  }
}

async function togglePlayPause() {
  try {
    if (isRunning.value) {
      await simulationApi.pause(id.value);
      console.error("Simulation paused");
    } else {
      await simulationApi.run(id.value);
      console.error("Simulation running");
    }
  } catch (err: any) {
    console.error("Failed to toggle play/pause:", err);
  }
}

async function handleSpeedChange(event: Event) {
  const target = event.target as HTMLInputElement;
  const speed = parseFloat(target.value);
  try {
    await simulationApi.setSpeed(id.value, speed);
  } catch (err: any) {
    console.error("Failed to set speed:", err);
  }
}

async function handleTickChange(event: Event) {
  const target = event.target as HTMLInputElement;
  const tick = parseInt(target.value);
  try {
    await simulationApi.setTick(id.value, tick);
  } catch (err: any) {
    console.error("Failed to set tick:", err);
  }
}

async function handleStepBack() {
  const newTick = Math.max(0, currentTick.value - 100);
  try {
    await simulationApi.setTick(id.value, newTick);
  } catch (err: any) {
    console.error("Failed to step back:", err);
  }
}

async function handleStepForward() {
  const newTick = Math.min(maxTick.value, currentTick.value + 100);
  try {
    await simulationApi.setTick(id.value, newTick);
  } catch (err: any) {
    console.error("Failed to step forward:", err);
  }
}

async function handleReset() {
  if (
    !confirm(
      "Are you sure you want to reset this simulation? All computed data will be lost."
    )
  ) {
    return;
  }
  try {
    await simulationApi.reset(id.value);
    window.location.reload();
  } catch (err: any) {
    console.error("Failed to reset simulation:", err);
  }
}

async function handleUnload() {
  if (
    !confirm(
      "Unload this simulation from memory? You will be redirected to the home screen."
    )
  ) {
    return;
  }
  try {
    await simulationApi.unload(id.value);
    isLoadedInMemory.value = false;
    router.push("/");
  } catch (err: any) {
    console.error("Failed to unload simulation:", err);
  }
}
onTickUpdate.value = (data) => {
  const now = Date.now();

  // Store tick number and timestamp
  tickHistory.value.push({ tick: data.Tick, timestamp: now });

  // Keep last 10 seconds of data
  const tenSecondsAgo = now - 10000;
  tickHistory.value = tickHistory.value.filter(
    (entry) => entry.timestamp > tenSecondsAgo
  );

  // Calculate server tick rate (ticks per second)
  if (tickHistory.value.length > 1) {
    const firstEntry = tickHistory.value[0];
    const lastEntry = tickHistory.value[tickHistory.value.length - 1];

    if (firstEntry && lastEntry) {
      const tickDifference = lastEntry.tick - firstEntry.tick;
      const timeDifference =
        (lastEntry.timestamp - firstEntry.timestamp) / 1000; // in seconds

      if (timeDifference > 0) {
        serverTickRate.value = tickDifference / timeDifference;
      }
    }
  }

  // Calculate latency
  const latency = now - data.Timestamp;
  if (latency > 0 && latency < 10000) {
    avgLatency.value = Math.round(latency);
  }
};

onMounted(() => {
  loadSimulation();
});

onUnmounted(() => {
  disconnect();
});
</script>

<style scoped>
.tabular-nums {
  font-variant-numeric: tabular-nums;
}
</style>
