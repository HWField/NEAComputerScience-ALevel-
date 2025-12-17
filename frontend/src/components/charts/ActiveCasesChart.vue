<template>
  <div class="card bg-base-100 shadow-lg">
    <div class="card-body">
      <div class="flex items-center justify-between mb-4">
        <h3 class="card-title text-lg">
          <i-ph-pulse class="w-5 h-5" />
          Active Cases Over Time
        </h3>
        <div
          class="tooltip"
          data-tip="Currently infected agents at each time point"
        >
          <i-ph-info class="w-5 h-5 text-base-content/50" />
        </div>
      </div>
      <div class="relative" style="height: 400px">
        <Line :data="chartData" :options="chartOptions" />
      </div>
      <div class="mt-4 grid grid-cols-3 gap-4 text-center">
        <div>
          <p class="text-xs text-base-content/60">Peak Active</p>
          <p class="text-lg font-bold text-error">{{ peakActive }}</p>
        </div>
        <div>
          <p class="text-xs text-base-content/60">Current Active</p>
          <p class="text-lg font-bold">{{ currentActive }}</p>
        </div>
        <div>
          <p class="text-xs text-base-content/60">Days to Peak</p>
          <p class="text-lg font-bold">{{ daysToPeak }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { computed } from "vue";
import { Line } from "vue-chartjs";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  type ChartOptions,
} from "chart.js";
import type { SimulationMetric } from "../../api/simulation";

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

interface Props {
  metrics: SimulationMetric[];
  tickLength: number;
}

const props = defineProps<Props>();

const getTimeLabel = (tick: number): string => {
  const totalSeconds = tick * props.tickLength;
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);

  if (days > 0) {
    return `Day ${days}`;
  }
  return `${hours}h`;
};

const getDayFromTick = (tick: number): number => {
  const totalSeconds = tick * props.tickLength;
  return Math.floor(totalSeconds / 86400);
};

// Active cases = currently infected (not recovered or dead)
const activeCasesData = computed(() => props.metrics.map((m) => m.infected));

const peakActive = computed(() => {
  if (activeCasesData.value.length === 0) return 0;
  return Math.max(...activeCasesData.value);
});

const currentActive = computed(() => {
  if (activeCasesData.value.length === 0) return 0;
  return activeCasesData.value[activeCasesData.value.length - 1];
});

const daysToPeak = computed(() => {
  if (props.metrics.length === 0) return 0;
  const peak = peakActive.value;
  const peakMetric = props.metrics.find((m) => m.infected === peak);
  if (!peakMetric) return 0;
  return getDayFromTick(peakMetric.tick);
});

const chartData = computed(() => ({
  labels: props.metrics.map((m) => getTimeLabel(m.tick)),
  datasets: [
    {
      label: "Active Cases",
      data: activeCasesData.value,
      borderColor: "rgb(239, 68, 68)", // Red
      backgroundColor: "rgba(239, 68, 68, 0.1)",
      fill: true,
      tension: 0.4,
      pointRadius: 2,
      pointHoverRadius: 5,
      borderWidth: 2,
    },
  ],
}));

const chartOptions = computed<ChartOptions<"line">>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    mode: "index",
    intersect: false,
  },
  plugins: {
    legend: {
      display: false,
    },
    tooltip: {
      backgroundColor: "rgba(0, 0, 0, 0.8)",
      padding: 12,
      titleFont: {
        size: 14,
      },
      bodyFont: {
        size: 13,
      },
      callbacks: {
        title: (context) => {
          if (!context[0]) return "";
          const metric = props.metrics[context[0].dataIndex];
          if (!metric) return "";
          return `${context[0].label} (Tick ${metric.tick})`;
        },
        label: (context) => {
          return `Active Cases: ${context.parsed.y}`;
        },
        afterLabel: (context) => {
          const metric = props.metrics[context.dataIndex];
          if (!metric || context.parsed.y === null) return "";
          const totalPopulation = metric.totalAgents;
          const prevalence = (
            (context.parsed.y / totalPopulation) *
            100
          ).toFixed(2);
          return `Prevalence: ${prevalence}% of population`;
        },
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      title: {
        display: true,
        text: "Number of Active Cases",
      },
      grid: {
        color: "rgba(0, 0, 0, 0.05)",
      },
    },
    x: {
      grid: {
        display: false,
      },
      ticks: {
        maxRotation: 45,
        minRotation: 45,
        autoSkip: true,
        maxTicksLimit: 15,
      },
    },
  },
}));
</script>
