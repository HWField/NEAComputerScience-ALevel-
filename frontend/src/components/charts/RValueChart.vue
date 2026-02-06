<template>
  <div class="card bg-base-100 shadow-lg">
    <div class="card-body">
      <div class="flex items-center justify-between mb-4">
        <h3 class="card-title text-lg">
          <i-ph-chart-line-up class="w-5 h-5" />
          Effective Reproduction Number (R<sub>t</sub>)
        </h3>
        <div
          class="tooltip tooltip-left"
          data-tip="Average number of people each infected person infects. R < 1 means outbreak is declining."
        >
          <i-ph-info class="w-5 h-5 text-base-content/50" />
        </div>
      </div>

      <div v-if="hasInsufficientData" class="alert alert-warning">
        <i-ph-warning class="w-5 h-5" />
        <span
          >Insufficient data to calculate R-value. Need at least 10 days of
          infection data.</span
        >
      </div>

      <div v-else>
        <div class="relative" style="height: 400px">
          <Line :data="chartData" :options="chartOptions" />
        </div>

        <div class="mt-4 grid grid-cols-3 gap-4 text-center">
          <div>
            <p class="text-xs text-base-content/60">Current R<sub>t</sub></p>
            <p class="text-lg font-bold" :class="currentRClass">
              {{ currentR }}
            </p>
          </div>
          <div>
            <p class="text-xs text-base-content/60">Average R<sub>t</sub></p>
            <p class="text-lg font-bold">{{ averageR }}</p>
          </div>
          <div>
            <p class="text-xs text-base-content/60">Peak R<sub>t</sub></p>
            <p class="text-lg font-bold text-error">{{ peakR }}</p>
          </div>
        </div>

        <div class="mt-4 text-xs text-base-content/60 text-center">
          <p>R<sub>t</sub> calculated using 5-day rolling average method</p>
          <p>
            R<sub>t</sub> &lt; 1: Outbreak declining | R<sub>t</sub> &gt; 1:
            Outbreak growing
          </p>
          <p class="mt-1 italic">Smoothed over serial interval for accuracy</p>
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
  Filler,
);

interface Props {
  metrics: SimulationMetric[];
  tickLength: number;
}

const props = defineProps<Props>();

const getDayFromTick = (tick: number): number => {
  const totalSeconds = tick * props.tickLength;
  return Math.floor(totalSeconds / 86400);
};

// Aggregate metrics by day for accurate R calculation
const dailyAggregatedData = computed(() => {
  const dayMap = new Map<
    number,
    {
      day: number;
      totalInfections: number;
      newInfections: number;
    }
  >();

  for (let i = 0; i < props.metrics.length; i++) {
    const metric = props.metrics[i];
    if (!metric) continue;

    const day = getDayFromTick(metric.tick);
    const totalInfections = metric.infected + metric.recovered + metric.dead;

    if (!dayMap.has(day)) {
      dayMap.set(day, {
        day,
        totalInfections,
        newInfections: 0,
      });
    } else {
      const existing = dayMap.get(day)!;
      if (totalInfections > existing.totalInfections) {
        existing.totalInfections = totalInfections;
      }
    }
  }

  // Calculate new infections per day
  const sortedDays = Array.from(dayMap.values()).sort((a, b) => a.day - b.day);
  for (let i = 0; i < sortedDays.length; i++) {
    const current = sortedDays[i];
    const previous = sortedDays[i - 1];

    if (!current) continue;

    if (i === 0) {
      current.newInfections = current.totalInfections;
    } else if (previous) {
      current.newInfections = Math.max(
        0,
        current.totalInfections - previous.totalInfections,
      );
    }
  }

  return sortedDays;
});

const rValues = computed(() => {
  const data = dailyAggregatedData.value;
  const serialInterval = 5; // days (typical for many respiratory diseases)
  const result: { day: number; rValue: number }[] = [];

  // Need at least 2 * serialInterval days to calculate R with averaging
  if (data.length <= serialInterval * 2) {
    return result;
  }

  for (let i = serialInterval * 2; i < data.length; i++) {
    // Calculate average new cases in current serial interval window
    let currentWindowSum = 0;
    let currentWindowCount = 0;
    for (let j = 0; j < serialInterval; j++) {
      const dataPoint = data[i - j];
      if (dataPoint) {
        currentWindowSum += dataPoint.newInfections;
        currentWindowCount++;
      }
    }

    // Calculate average new cases in previous serial interval window
    let previousWindowSum = 0;
    let previousWindowCount = 0;
    for (let j = serialInterval; j < serialInterval * 2; j++) {
      const dataPoint = data[i - j];
      if (dataPoint) {
        previousWindowSum += dataPoint.newInfections;
        previousWindowCount++;
      }
    }

    // Calculate R_t if has valid data
    if (currentWindowCount > 0 && previousWindowCount > 0) {
      const currentAvg = currentWindowSum / currentWindowCount;
      const previousAvg = previousWindowSum / previousWindowCount;

      // Only calculate if previous average is meaningful (> 0.1 to avoid division by very small numbers)
      if (previousAvg > 0.1) {
        const rValue = currentAvg / previousAvg;

        // Store the result
        const currentDay = data[i];
        if (currentDay) {
          result.push({
            day: currentDay.day,
            rValue: rValue,
          });
        }
      }
    }
  }

  return result;
});

const hasInsufficientData = computed(() => rValues.value.length === 0);

const currentR = computed(() => {
  if (rValues.value.length === 0) return "N/A";
  const lastR = rValues.value[rValues.value.length - 1];
  return lastR ? lastR.rValue.toFixed(2) : "N/A";
});

const currentRClass = computed(() => {
  if (rValues.value.length === 0) return "";
  const lastR = rValues.value[rValues.value.length - 1];
  if (!lastR) return "";

  if (lastR.rValue < 0.9) return "text-success";
  if (lastR.rValue < 1.1) return "text-warning";
  return "text-error";
});

const averageR = computed(() => {
  if (rValues.value.length === 0) return "N/A";
  const sum = rValues.value.reduce((acc, val) => acc + val.rValue, 0);
  return (sum / rValues.value.length).toFixed(2);
});

const peakR = computed(() => {
  if (rValues.value.length === 0) return "N/A";
  const max = Math.max(...rValues.value.map((v) => v.rValue));
  return max.toFixed(2);
});

// Calculate appropriate y-axis max based on data
const yAxisMax = computed(() => {
  if (rValues.value.length === 0) return 5;
  const maxR = Math.max(...rValues.value.map((v) => v.rValue));

  const paddedMax = maxR * 1.2;

  // Round up to nearest nice number for better axis labels
  if (paddedMax <= 2) return 2;
  if (paddedMax <= 5) return 5;
  if (paddedMax <= 10) return 10;
  if (paddedMax <= 20) return 20;

  return Math.ceil(paddedMax / 10) * 10;
});

const chartData = computed(() => ({
  labels: rValues.value.map((v) => `Day ${v.day}`),
  datasets: [
    {
      label: "R_t",
      data: rValues.value.map((v) => v.rValue),
      borderColor: "rgb(99, 102, 241)", // Indigo
      backgroundColor: "rgba(99, 102, 241, 0.1)",
      fill: true,
      tension: 0.4,
      pointRadius: 3,
      pointHoverRadius: 6,
      borderWidth: 2,
    },
    {
      label: "R = 1 (Control Threshold)",
      data: rValues.value.map(() => 1),
      borderColor: "rgb(234, 179, 8)", // Yellow
      backgroundColor: "transparent",
      borderDash: [5, 5],
      borderWidth: 2,
      pointRadius: 0,
      fill: false,
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
        filter: (item) =>
          item.text !== "R = 1 (Control Threshold)" || rValues.value.length > 0,
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
          return context[0].label;
        },
        label: (context) => {
          if (context.datasetIndex === 1) return "";
          return `R_t: ${context.parsed.y?.toFixed(2) ?? "N/A"}`;
        },
        afterLabel: (context) => {
          if (context.datasetIndex === 1) return "";
          const r = context.parsed.y;
          if (r === null || r === undefined) return "";

          if (r < 1) {
            return "Outbreak is declining";
          } else if (r > 1) {
            return "Outbreak is growing";
          } else {
            return "Outbreak is stable";
          }
        },
      },
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      max: yAxisMax.value, // Dynamic scaling based on data
      title: {
        display: true,
        text: "Reproduction Number (R_t)",
      },
      grid: {
        color: (context) => {
          // Highlight R = 1 line
          if (context.tick.value === 1) {
            return "rgba(234, 179, 8, 0.3)";
          }
          return "rgba(0, 0, 0, 0.05)";
        },
        lineWidth: (context) => {
          if (context.tick.value === 1) {
            return 2;
          }
          return 1;
        },
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
