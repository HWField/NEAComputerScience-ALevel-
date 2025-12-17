<template>
  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
    <div class="card bg-base-100 shadow-lg border-l-4 border-error">
      <div class="card-body p-4">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-base-content/60">Peak Infections</p>
            <p class="text-3xl font-bold text-error">{{ peakInfections }}</p>
            <p class="text-xs text-base-content/50 mt-1">
              {{ peakInfectionsDay }}
            </p>
          </div>
          <i-ph-warning-circle class="w-12 h-12 text-error/20" />
        </div>
      </div>
    </div>

    <div class="card bg-base-100 shadow-lg border-l-4 border-info">
      <div class="card-body p-4">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-base-content/60">Total Recovered</p>
            <p class="text-3xl font-bold text-info">{{ totalRecovered }}</p>
            <p class="text-xs text-base-content/50 mt-1">
              {{ recoveryRate }} recovery rate
            </p>
          </div>
          <i-ph-heart class="w-12 h-12 text-info/20" />
        </div>
      </div>
    </div>

    <div class="card bg-base-100 shadow-lg border-l-4 border-base-300">
      <div class="card-body p-4">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-base-content/60">Total Deaths</p>
            <p class="text-3xl font-bold">{{ totalDeaths }}</p>
            <p class="text-xs text-base-content/50 mt-1">
              {{ mortalityRate }} mortality rate
            </p>
          </div>
          <i-ph-x-circle class="w-12 h-12 text-base-300/50" />
        </div>
      </div>
    </div>

    <div class="card bg-base-100 shadow-lg border-l-4 border-warning">
      <div class="card-body p-4">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-base-content/60">Current Quarantined</p>
            <p class="text-3xl font-bold text-warning">
              {{ currentQuarantined }}
            </p>
            <p class="text-xs text-base-content/50 mt-1">
              {{ quarantinePercentage }} of population
            </p>
          </div>
          <i-ph-house class="w-12 h-12 text-warning/20" />
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { computed } from "vue";
import type { SimulationMetric } from "../../api/simulation";

interface Props {
  metrics: SimulationMetric[];
  tickLength: number;
}

const props = defineProps<Props>();

const peakInfections = computed(() => {
  if (props.metrics.length === 0) return 0;
  return Math.max(...props.metrics.map((m) => m.infected));
});

const peakInfectionsDay = computed(() => {
  if (props.metrics.length === 0) return "N/A";
  const peak = peakInfections.value;
  const metric = props.metrics.find((m) => m.infected === peak);
  if (!metric) return "N/A";

  const totalSeconds = metric.tick * props.tickLength;
  const days = Math.floor(totalSeconds / 86400);
  return `Day ${days}`;
});

const latestMetric = computed(() => {
  if (props.metrics.length === 0) return null;
  return props.metrics[props.metrics.length - 1];
});

const totalRecovered = computed(() => {
  return latestMetric.value?.recovered ?? 0;
});

const totalDeaths = computed(() => {
  return latestMetric.value?.dead ?? 0;
});

const currentQuarantined = computed(() => {
  return latestMetric.value?.quarantined ?? 0;
});

const totalInfections = computed(() => {
  if (!latestMetric.value) return 0;
  return (
    latestMetric.value.infected +
    latestMetric.value.recovered +
    latestMetric.value.dead
  );
});

const recoveryRate = computed(() => {
  if (totalInfections.value === 0) return "0%";
  const rate = (totalRecovered.value / totalInfections.value) * 100;
  return `${rate.toFixed(1)}%`;
});

const mortalityRate = computed(() => {
  if (totalInfections.value === 0) return "0%";
  const rate = (totalDeaths.value / totalInfections.value) * 100;
  return `${rate.toFixed(1)}%`;
});

const quarantinePercentage = computed(() => {
  if (!latestMetric.value || latestMetric.value.totalAgents === 0) return "0%";
  const rate =
    (currentQuarantined.value / latestMetric.value.totalAgents) * 100;
  return `${rate.toFixed(1)}%`;
});
</script>
