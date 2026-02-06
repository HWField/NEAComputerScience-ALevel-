<template>
  <div class="card bg-base-100 shadow-lg">
    <div class="card-body">
      <h3 class="card-title text-lg mb-4">
        <i-ph-chart-line class="w-5 h-5" />
        Infection Timeline
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
  tickLength: number; // seconds per tick
}

const props = defineProps<Props>();

// Convert tick to time label
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
      backgroundColor: "rgba(34, 197, 94, 0.1)",
      fill: true,
      tension: 0.4,
      pointRadius: 2,
      pointHoverRadius: 5,
    },
    {
      label: "Infected",
      data: props.metrics.map((m) => m.infected),
      borderColor: "rgb(239, 68, 68)", // Red
      backgroundColor: "rgba(239, 68, 68, 0.1)",
      fill: true,
      tension: 0.4,
      pointRadius: 2,
      pointHoverRadius: 5,
    },
    {
      label: "Recovered",
      data: props.metrics.map((m) => m.recovered),
      borderColor: "rgb(59, 130, 246)", // Blue
      backgroundColor: "rgba(59, 130, 246, 0.1)",
      fill: true,
      tension: 0.4,
      pointRadius: 2,
      pointHoverRadius: 5,
    },
    {
      label: "Dead",
      data: props.metrics.map((m) => m.dead),
      borderColor: "rgb(107, 114, 128)", // Gray
      backgroundColor: "rgba(107, 114, 128, 0.1)",
      fill: true,
      tension: 0.4,
      pointRadius: 2,
      pointHoverRadius: 5,
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
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      stacked: false,
      title: {
        display: true,
        text: "Number of Agents",
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
