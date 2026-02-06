<template>
  <div class="card bg-base-100 shadow-lg">
    <div class="card-body">
      <h3 class="card-title text-lg mb-4">
        <i-ph-users-three class="w-5 h-5" />
        Population Breakdown
      </h3>
      <div class="relative" style="height: 400px">
        <Line :data="chartData" :options="chartOptions" />
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
  Filler,
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

const chartData = computed(() => ({
  labels: props.metrics.map((m) => getTimeLabel(m.tick)),
  datasets: [
    {
      label: "Healthy",
      data: props.metrics.map((m) => m.healthy),
      borderColor: "rgb(34, 197, 94)", // Green
      backgroundColor: "rgba(34, 197, 94, 0.6)",
      fill: true,
      tension: 0.4,
      pointRadius: 0,
    },
    {
      label: "Infected",
      data: props.metrics.map((m) => m.infected),
      borderColor: "rgb(239, 68, 68)", // Red
      backgroundColor: "rgba(239, 68, 68, 0.6)",
      fill: true,
      tension: 0.4,
      pointRadius: 0,
    },
    {
      label: "Recovered",
      data: props.metrics.map((m) => m.recovered),
      borderColor: "rgb(59, 130, 246)", // Blue
      backgroundColor: "rgba(59, 130, 246, 0.6)",
      fill: true,
      tension: 0.4,
      pointRadius: 0,
    },
    {
      label: "Dead",
      data: props.metrics.map((m) => m.dead),
      borderColor: "rgb(107, 114, 128)", // Gray
      backgroundColor: "rgba(107, 114, 128, 0.6)",
      fill: true,
      tension: 0.4,
      pointRadius: 0,
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
      position: "top",
      labels: {
        usePointStyle: true,
        padding: 15,
        font: {
          size: 12,
        },
      },
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
          const tick = props.metrics[context[0].dataIndex]?.tick ?? 0;
          return `${context[0].label} (Tick ${tick})`;
        },
        footer: (context) => {
          if (!context[0]) return "";
          const metric = props.metrics[context[0].dataIndex];
          if (!metric) return "";
          return `Total: ${metric.totalAgents}`;
        },
      },
    },
  },
  scales: {
    y: {
      stacked: true,
      beginAtZero: true,
      title: {
        display: true,
        text: "Number of Agents",
      },
      grid: {
        color: "rgba(0, 0, 0, 0.05)",
      },
    },
    x: {
      stacked: true,
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
