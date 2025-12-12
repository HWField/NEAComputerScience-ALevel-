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

        <!-- Tab Navigation -->
        <div class="flex gap-2 mt-4">
          <button
            class="tab tab-lifted"
            :class="{ 'tab-active': activeTab === 'configure' }"
            @click="activeTab = 'configure'"
          >
            <i-ph-gear class="w-4 h-4 mr-2" />
            Configure
          </button>
          <button
            class="tab tab-lifted"
            :class="{ 'tab-active': activeTab === 'simulate' }"
            @click="activeTab = 'simulate'"
          >
            <i-ph-play class="w-4 h-4 mr-2" />
            Simulate
          </button>
          <button
            class="tab tab-lifted"
            :class="{ 'tab-active': activeTab === 'analyse' }"
            @click="activeTab = 'analyse'"
          >
            <i-ph-chart-line-up class="w-4 h-4 mr-2" />
            Analyse
          </button>
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

    <!-- Main Content -->
    <div v-else class="container mx-auto px-6 py-6">
      <!-- Connection Error Alert -->
      <div
        v-if="hubState.error && hubState.connectionStatus.value !== 'connected'"
        class="alert alert-warning mb-6"
      >
        <i-ph-warning class="w-6 h-6" />
        <span>{{ hubState.error }}</span>
      </div>

      <!-- Tab Content -->
      <SimulationConfigure
        v-if="activeTab === 'configure'"
        :simulation-data="simulationData"
      />

      <SimulationSimulate
        v-else-if="activeTab === 'simulate'"
        :simulation-data="simulationData"
        :metrics="metrics"
        :current-tick="currentTick"
        :max-computed-tick="maxComputedTick"
        :is-running="isRunning"
        :is-computing="isComputing"
        :is-loaded-in-memory="isLoadedInMemory"
        :current-speed="currentSpeed"
        :agent-positions="agentPositions"
        @toggle-play-pause="togglePlayPause"
        @speed-change="handleSpeedChange"
        @tick-change="handleTickChange"
        @reset="handleReset"
        @unload="handleUnload"
      />

      <SimulationAnalyse
        v-else-if="activeTab === 'analyse'"
        :simulation-data="simulationData"
      />
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useSimulationHub } from "@/composables/useSimulationHub";
import { simulationApi, type SimulationDetail } from "@/api/simulation";
import SimulationConfigure from "@/components/SimulationConfigure.vue";
import SimulationSimulate from "@/components/SimulationSimulate.vue";
import SimulationAnalyse from "@/components/SimulationAnalyse.vue";

type TabType = "configure" | "simulate" | "analyse";

const route = useRoute();
const router = useRouter();
const id = computed(() => parseInt(route.params.id as string));

// Tab state
const activeTab = ref<TabType>("simulate");

const isInitializing = ref(true);
const error = ref<string | null>(null);
const simulationData = ref<SimulationDetail | null>(null);
const isLoadedInMemory = ref(false);

const hubState = useSimulationHub(id.value);
const {
  currentState,
  latestMetrics,
  currentInterpolatedTick,
  maxComputedTick,
  agentPositions,
  isTrackingAgents,
  connect,
  disconnect,
  startAgentTracking,
  stopAgentTracking,
} = hubState;

const currentTick = computed(() => {
  // Round to avoid sub-tick visual jitter
  return Math.floor(currentInterpolatedTick.value);
});

const isRunning = computed(() => {
  const status = currentState.value?.status;
  return status === "Running";
});

const isComputing = computed(() => {
  const status = currentState.value?.status;
  return status === "Computing";
});

const currentSpeed = computed(() => currentState.value?.speed || 1);
const displayStatus = computed(() => {
  const status = currentState.value?.status || "Unknown";
  return status;
});
const metrics = computed(() => latestMetrics.value);

// Status badge styling
const statusBadgeClass = computed(() => {
  const status = displayStatus.value.toLowerCase();
  if (status === "running") return "badge-success";
  if (status === "computing") return "badge-info";
  if (status === "loaded" || status === "paused") return "badge-warning";
  if (status === "error") return "badge-error";
  return "badge-ghost";
});

const statusIndicatorClass = computed(() => {
  const status = displayStatus.value.toLowerCase();
  if (status === "running") return "bg-success animate-pulse";
  if (status === "computing") return "bg-info animate-pulse";
  if (status === "loaded" || status === "paused") return "bg-warning";
  if (status === "error") return "bg-error animate-pulse";
  return "bg-base-content/50";
});

async function loadSimulation() {
  try {
    isInitializing.value = true;
    error.value = null;

    // Connect to SignalR hub for real-time updates
    await connect();

    // Fetch core simulation details
    simulationData.value = await simulationApi.getById(id.value);

    // Load into server memory
    await simulationApi.load(id.value);
    isLoadedInMemory.value = true;

    // Start agent tracking (1000 agents at 3 Hz)
    await startAgentTracking(1000);
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
    } else {
      await simulationApi.run(id.value);
    }
  } catch (err: any) {
    console.error("Failed to toggle play/pause:", err);
  }
}

async function handleSpeedChange(speed: number) {
  try {
    await simulationApi.setSpeed(id.value, speed);
  } catch (err: any) {
    console.error("Failed to set speed:", err);
  }
}

async function handleTickChange(tick: number) {
  try {
    await simulationApi.setTick(id.value, tick);
  } catch (err: any) {
    console.error("Failed to set tick:", err);
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

onMounted(() => {
  loadSimulation();
});

onUnmounted(() => {
  disconnect();
});
</script>
