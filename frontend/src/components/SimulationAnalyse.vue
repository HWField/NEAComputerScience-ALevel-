<template>
  <div class="space-y-6">
    <!-- Header with refresh button -->
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-3xl font-bold">Analysis Dashboard</h2>
        <p class="text-base-content/70 mt-1">
          Historical metrics and disease spread analysis
        </p>
      </div>
      <button
        class="btn btn-primary gap-2"
        :class="{ loading: loading }"
        @click="fetchMetrics"
        :disabled="loading"
      >
        <i-ph-arrow-clockwise v-if="!loading" class="w-5 h-5" />
        {{ loading ? "Loading..." : "Refresh Data" }}
      </button>
    </div>

    <!-- Loading state -->
    <div
      v-if="loading && !metrics.length"
      class="flex items-center justify-center py-20"
    >
      <div class="text-center">
        <span class="loading loading-spinner loading-lg text-primary"></span>
        <p class="mt-4 text-base-content/70">Loading metrics data...</p>
      </div>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="alert alert-error">
      <i-ph-warning-circle class="w-6 h-6" />
      <span>{{ error }}</span>
    </div>

    <!-- No data state -->
    <div v-else-if="!metrics.length" class="text-center py-20">
      <div class="mb-6">
        <i-ph-chart-line-up class="w-24 h-24 mx-auto text-base-content/20" />
      </div>
      <h3 class="text-2xl font-bold mb-3">No Data Available</h3>
      <p class="text-base-content/70 mb-6">
        Run your simulation to generate metrics data for analysis.
      </p>
    </div>

    <!-- Data visualization -->
    <div v-else class="space-y-6">
      <!-- Summary Statistics -->
      <MetricsSummary :metrics="metrics" :tickLength="tickLength" />

      <!-- Main Charts -->
      <div class="grid grid-cols-1 gap-6">
        <InfectionTimelineChart :metrics="metrics" :tickLength="tickLength" />

        <!-- R-Value Chart -->
        <RValueChart :metrics="metrics" :tickLength="tickLength" />

        <DailyInfectionsChart :metrics="metrics" :tickLength="tickLength" />
        <PopulationStackedChart :metrics="metrics" :tickLength="tickLength" />
      </div>

      <!-- Additional Info -->
      <div class="card bg-base-100 shadow-lg">
        <div class="card-body">
          <h3 class="card-title text-lg mb-4">
            <i-ph-info class="w-5 h-5" />
            Data Information
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
            <div>
              <span class="text-base-content/60">Total Data Points:</span>
              <span class="ml-2 font-semibold">{{ metrics.length }}</span>
            </div>
            <div>
              <span class="text-base-content/60">Time Range:</span>
              <span class="ml-2 font-semibold">{{ timeRange }}</span>
            </div>
            <div>
              <span class="text-base-content/60">Last Updated:</span>
              <span class="ml-2 font-semibold">{{ lastUpdated }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, watch } from "vue";
import { simulationApi } from "../api/simulation";
import type { SimulationDetail, SimulationMetric } from "../api/simulation";
import InfectionTimelineChart from "./charts/InfectionTimelineChart.vue";
import DailyInfectionsChart from "./charts/DailyInfectionsChart.vue";
import PopulationStackedChart from "./charts/PopulationStackedChart.vue";
import RValueChart from "./charts/RValueChart.vue";
import MetricsSummary from "./charts/MetricsSummary.vue";

interface Props {
  simulationData: SimulationDetail | null;
}

const props = defineProps<Props>();

const metrics = ref<SimulationMetric[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const lastUpdated = ref<string>("");
const tickLength = computed(() => props.simulationData?.tickLength ?? 60);
const timeRange = computed(() => {
  if (metrics.value.length === 0) return "N/A";
  const firstTick = metrics.value[0]?.tick ?? 0;
  const lastTick = metrics.value[metrics.value.length - 1]?.tick ?? 0;

  const totalSeconds = (lastTick - firstTick) * tickLength.value;
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);

  if (days > 0) {
    return `${days} day${days !== 1 ? "s" : ""}`;
  }
  return `${hours} hour${hours !== 1 ? "s" : ""}`;
});

const fetchMetrics = async () => {
  if (!props.simulationData?.id) {
    error.value = "No simulation selected";
    return;
  }

  loading.value = true;
  error.value = null;

  try {
    const response = await simulationApi.getMetrics(props.simulationData.id);
    metrics.value = response.metrics;
    lastUpdated.value = new Date().toLocaleTimeString();
  } catch (err: any) {
    error.value = err.response?.data?.message || "Failed to load metrics data";
    console.error("Error fetching metrics:", err);
  } finally {
    loading.value = false;
  }
};

// Fetch metrics when component mounts
onMounted(() => {
  fetchMetrics();
});

// Refetch when simulation changes
watch(
  () => props.simulationData?.id,
  () => {
    if (props.simulationData?.id) {
      fetchMetrics();
    }
  },
);
</script>
