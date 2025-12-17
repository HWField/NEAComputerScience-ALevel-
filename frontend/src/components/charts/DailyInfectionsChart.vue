<template>
  <div class="card bg-base-100 shadow-lg">
    <div class="card-body">
      <h3 class="card-title text-lg mb-4">
        <i-ph-trend-up class="w-5 h-5" />
        Daily New Infections
      </h3>
      <div class="relative" style="height: 400px">
        <Bar :data="chartData" :options="chartOptions" />
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
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
  BarElement,
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

// Helper to get day number from tick
const getDayFromTick = (tick: number): number => {
  const totalSeconds = tick * props.tickLength;
  return Math.floor(totalSeconds / 86400);
};

// Aggregate metrics by day
const dailyAggregatedData = computed(() => {
  // Map to store data by day
  const dayMap = new Map<
    number,
    {
      day: number;
      totalInfections: number;
      metricsCount: number;
    }
  >();

  // Calculate total infections for each metric
  for (let i = 0; i < props.metrics.length; i++) {
    const metric = props.metrics[i];
    if (!metric) continue;

    const day = getDayFromTick(metric.tick);
    const totalInfections = metric.infected + metric.recovered + metric.dead;

    if (!dayMap.has(day)) {
      dayMap.set(day, {
        day,
        totalInfections,
        metricsCount: 1,
      });
    } else {
      // Update with the latest (highest) total for this day
      const existing = dayMap.get(day)!;
      if (totalInfections > existing.totalInfections) {
        existing.totalInfections = totalInfections;
      }
      existing.metricsCount++;
    }
  }

  // Convert to sorted array
  return Array.from(dayMap.values()).sort((a, b) => a.day - b.day);
});

// Calculate new infections per day
const dailyNewInfections = computed(() => {
  const result: number[] = [];
  const aggregated = dailyAggregatedData.value;

  for (let i = 0; i < aggregated.length; i++) {
    const current = aggregated[i];
    const previous = aggregated[i - 1];

    if (!current) continue;

    if (i === 0) {
      // First day shows the initial infections
      result.push(current.totalInfections);
    } else if (previous) {
      const newInfections = Math.max(
        0,
        current.totalInfections - previous.totalInfections
      );
      result.push(newInfections);
    }
  }

  return result;
});

const chartData = computed(() => ({
  labels: dailyAggregatedData.value.map((d) => `Day ${d.day}`),
  datasets: [
    {
      label: "New Infections",
      data: dailyNewInfections.value,
      backgroundColor: "rgba(239, 68, 68, 0.7)",
      borderColor: "rgb(239, 68, 68)",
      borderWidth: 1,
    },
  ],
}));

const chartOptions = computed<ChartOptions<"bar">>(() => ({
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
          const dayData = dailyAggregatedData.value[context[0].dataIndex];
          if (!dayData) return "";
          return `Day ${dayData.day}`;
        },
        label: (context) => {
          const value = context.parsed.y;
          return `New Infections: ${value}`;
        },
        footer: (context) => {
          if (!context[0]) return "";
          const dayData = dailyAggregatedData.value[context[0].dataIndex];
          if (!dayData) return "";
          return `Total cases so far: ${dayData.totalInfections}`;
        },
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      title: {
        display: true,
        text: "New Infections",
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
