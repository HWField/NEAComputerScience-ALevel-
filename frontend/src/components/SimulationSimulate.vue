<template>
  <div>
    <div class="grid grid-cols-12 gap-6">
      <!-- Left Sidebar Population Overview -->
      <div class="col-span-3 space-y-6">
        <!-- Population Stats Card -->
        <div class="card bg-base-100 shadow-xl">
          <div class="card-body">
            <div
              class="flex items-center justify-between cursor-pointer"
              @click="toggleSection('population')"
            >
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-users class="w-5 h-5 text-primary" />
                Population
              </h3>
              <i-ph-caret-down
                class="w-5 h-5 transition-transform"
                :class="{ 'rotate-180': expandedSections.population }"
              />
            </div>
            <div v-if="expandedSections.population" class="space-y-4 mt-4">
              <!-- Total Agents -->
              <div class="stat bg-base-200 rounded-lg">
                <div class="stat-title text-xs">Total Agents</div>
                <div class="stat-value text-2xl">
                  {{
                    formatNumber(
                      metrics?.totalAgents || simulationData?.agentCount || 0,
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
                    formatNumber(metrics?.healthy || 0)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="text-sm flex items-center gap-2">
                    <span class="w-3 h-3 rounded-full bg-error"></span>
                    Infected
                  </span>
                  <span class="font-bold text-error">{{
                    formatNumber(metrics?.infected || 0)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="text-sm flex items-center gap-2">
                    <span class="w-3 h-3 rounded-full bg-info"></span>
                    Recovered
                  </span>
                  <span class="font-bold text-info">{{
                    formatNumber(metrics?.recovered || 0)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="text-sm flex items-center gap-2">
                    <span class="w-3 h-3 rounded-full bg-base-300"></span>
                    Deceased
                  </span>
                  <span class="font-bold text-base-content/70">{{
                    formatNumber(metrics?.dead || 0)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="text-sm flex items-center gap-2">
                    <span class="w-3 h-3 rounded-full bg-warning"></span>
                    Quarantined
                  </span>
                  <span class="font-bold text-warning">{{
                    formatNumber(metrics?.quarantined || 0)
                  }}</span>
                </div>
              </div>
            </div>
            <div v-else class="text-sm text-base-content/60 italic mt-2">
              Click to expand
            </div>
          </div>
        </div>

        <!-- Infection Metrics -->
        <div class="card bg-base-100 shadow-xl">
          <div class="card-body">
            <div
              class="flex items-center justify-between cursor-pointer"
              @click="toggleSection('infection')"
            >
              <h3 class="card-title text-lg flex items-center gap-2">
                <i-ph-trend-up class="w-5 h-5 text-error" />
                Infection Metrics
              </h3>
              <i-ph-caret-down
                class="w-5 h-5 transition-transform"
                :class="{ 'rotate-180': expandedSections.infection }"
              />
            </div>
            <div v-if="expandedSections.infection" class="space-y-4 mt-4">
              <!-- Current Infection Rates -->
              <div>
                <div class="flex justify-between mb-2">
                  <span class="text-sm text-base-content/70"
                    >Current Active</span
                  >
                  <span class="font-bold text-error"
                    >{{ currentInfectionRate }}%</span
                  >
                </div>
              </div>

              <!-- Symptomatic vs Asymptomatic -->
              <div class="space-y-2">
                <div class="flex justify-between items-center text-sm">
                  <span class="text-base-content/70">Symptomatic Cases</span>
                  <span class="font-bold text-warning">{{
                    formatNumber(metrics?.symptomaticCases || 0)
                  }}</span>
                </div>
                <div class="flex justify-between items-center text-sm">
                  <span class="text-base-content/70">Reported Cases</span>
                  <span class="font-bold text-info">{{
                    formatNumber(metrics?.reportedCases || 0)
                  }}</span>
                </div>
                <div class="text-xs text-base-content/60 mt-1">
                  Reporting rate: {{ reportingRate }}%
                </div>
              </div>

              <div class="divider my-2"></div>

              <!-- Cumulative Statistics -->
              <div class="space-y-2">
                <div class="text-xs text-base-content/70 font-semibold mb-1">
                  Cumulative Statistics
                </div>
                <div class="flex justify-between items-center text-sm">
                  <span class="text-base-content/70">Total Infections</span>
                  <span class="font-bold">{{
                    formatNumber(metrics?.totalInfections || 0)
                  }}</span>
                </div>
                <div class="flex justify-between items-center text-sm">
                  <span class="text-base-content/70">Total Recoveries</span>
                  <span class="font-bold text-success">{{
                    formatNumber(metrics?.totalRecoveries || 0)
                  }}</span>
                </div>
                <div class="flex justify-between items-center text-sm">
                  <span class="text-base-content/70">Total Deaths</span>
                  <span class="font-bold text-error">{{
                    formatNumber(metrics?.totalDeaths || 0)
                  }}</span>
                </div>
                <div class="text-xs text-base-content/60 mt-1">
                  Case Fatality Rate:
                  {{
                    metrics?.totalInfections
                      ? (
                          ((metrics?.totalDeaths || 0) /
                            metrics.totalInfections) *
                          100
                        ).toFixed(2)
                      : "0.00"
                  }}%
                </div>
              </div>
            </div>
            <div v-else class="text-sm text-base-content/60 italic mt-2">
              Click to expand
            </div>
          </div>
        </div>
      </div>

      <!-- Center Map View -->
      <div class="col-span-9 space-y-6">
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
                :agents="Array.from(agentPositions.values())"
                :totalAgents="
                  metrics?.totalAgents || simulationData?.agentCount || 0
                "
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
              <div class="text-sm text-base-content/70 uppercase tracking-wide">
                Simulation Time
              </div>
              <div
                class="text-2xl font-mono font-bold tabular-nums"
                :class="isComputing ? 'text-info' : 'text-primary'"
              >
                {{ simulationDateTime.date }} {{ simulationDateTime.time }}
              </div>
              <div class="text-xs text-base-content/60">
                <span
                  v-if="isComputing"
                  class="flex items-center justify-center gap-2"
                >
                  <span
                    class="loading loading-spinner loading-xs text-info"
                  ></span>
                  <span class="text-info font-medium"
                    >Computing to target time</span
                  >
                </span>
                <span v-else>
                  Duration: <span class="font-mono">{{ formattedTime }}</span>
                </span>
              </div>
            </div>

            <!-- Playback Controls -->
            <div class="flex gap-2">
              <button
                class="btn btn-circle btn-sm"
                :class="isRunning ? 'btn-warning' : 'btn-primary'"
                @click="togglePlayPause"
                :disabled="!isLoadedInMemory || isComputing"
              >
                <i-ph-pause class="w-5 h-5" v-if="isRunning" />
                <i-ph-play class="w-5 h-5" v-else />
              </button>
            </div>
          </div>

          <!-- Timeline Slider -->
          <div class="relative mb-6">
            <!-- Custom Multi-Track Slider -->
            <div class="relative h-3 w-full rounded-full bg-base-300">
              <!-- Pre-computed Track (Ghost/Background Layer) -->
              <div
                v-if="maxComputedTick > 0"
                class="absolute h-full rounded-full pointer-events-none"
                :class="
                  isComputing
                    ? 'bg-info/20 border-2 border-info/40'
                    : 'bg-warning/20 border-2 border-warning/40'
                "
                :style="{
                  width: `${(maxComputedTick / maxTick) * 100}%`,
                  transition: 'width 200ms ease-out',
                }"
              ></div>

              <!-- Current Progress Track (Foreground Layer) -->
              <div
                class="absolute h-full rounded-full pointer-events-none"
                :class="isComputing ? 'bg-info' : 'bg-warning'"
                :style="{
                  width: `${(currentTick / maxTick) * 100}%`,
                  transition: isRunning
                    ? 'width 100ms linear'
                    : 'width 200ms ease-out',
                }"
              ></div>

              <!-- Slider Input -->
              <input
                type="range"
                min="0"
                :max="maxTick"
                :value="currentTick"
                @change="handleTickChange"
                class="timeline-slider"
                :class="{ 'cursor-not-allowed opacity-50': isRunning }"
                :disabled="isRunning"
              />
            </div>

            <!-- Timeline Labels -->
            <div class="flex justify-between text-xs text-base-content/60 mt-3">
              <span>Start</span>
              <span class="flex flex-col items-center gap-1">
                <span
                  :class="
                    isComputing
                      ? 'text-info font-medium'
                      : 'text-warning font-medium'
                  "
                  >Current</span
                >
                <span>{{ formatTime(currentTick) }}</span>
              </span>
              <span
                v-if="maxComputedTick > 0"
                class="flex flex-col items-center gap-1"
              >
                <span
                  :class="
                    isComputing
                      ? 'text-info/60 font-medium'
                      : 'text-warning/60 font-medium'
                  "
                  >Computed</span
                >
                <span>{{ formatTime(maxComputedTick) }}</span>
              </span>
              <span class="flex flex-col items-center gap-1">
                <span>Max (1 Year)</span>
                <span>{{ formatTime(maxTick) }}</span>
              </span>
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
              min="1"
              max="100"
              :value="currentSpeed"
              @input="handleSpeedChange"
              class="range range-primary range-sm"
              step="0.5"
              :disabled="!isLoadedInMemory || isComputing"
            />
            <div class="flex justify-between text-xs text-base-content/60 mt-1">
              <span>1x</span>
              <span>50x</span>
              <span>100x</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed } from "vue";
import type { SimulationDetail, AgentPosition } from "@/api/simulation";
import SimulationMap from "@/components/SimulationMap.vue";

interface SimulationMetrics {
  totalAgents: number;
  healthy: number;
  infected: number;
  recovered: number;
  dead: number;
  quarantined: number;
  symptomaticCases: number;
  reportedCases: number;
  totalInfections: number;
  totalRecoveries: number;
  totalDeaths: number;
  activeInfections: number;
}

interface Props {
  simulationData: SimulationDetail | null;
  metrics: SimulationMetrics | null;
  currentTick: number;
  maxComputedTick: number;
  isRunning: boolean;
  isComputing: boolean;
  isLoadedInMemory: boolean;
  currentSpeed: number;
  agentPositions: Map<number, AgentPosition>;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  togglePlayPause: [];
  speedChange: [speed: number];
  tickChange: [tick: number];
}>();

const expandedSections = ref({
  population: true,
  infection: true,
});

const maxTick = computed(() => {
  if (!props.simulationData) return 100000;
  const tickLength = props.simulationData.tickLength;
  const secondsInYear = 365 * 24 * 60 * 60;
  return Math.floor(secondsInYear / tickLength);
});

const formattedTime = computed(() => {
  return formatTime(props.currentTick);
});

const simulationDateTime = computed(() => {
  const tickLength = props.simulationData?.tickLength ?? 300;
  const totalSeconds = Math.floor(props.currentTick) * tickLength;

  const startDate = new Date("2025-01-01T00:00:00");
  const totalMilliseconds = totalSeconds * 1000;
  const currentDate = new Date(startDate.getTime() + totalMilliseconds);

  const day = String(currentDate.getDate()).padStart(2, "0");
  const month = String(currentDate.getMonth() + 1).padStart(2, "0");
  const year = String(currentDate.getFullYear()).slice(-2);
  const dateStr = `${day}/${month}/${year}`;

  const hours = String(currentDate.getHours()).padStart(2, "0");
  const minutes = String(currentDate.getMinutes()).padStart(2, "0");
  const timeStr = `${hours}:${minutes}`;

  return {
    date: dateStr,
    time: timeStr,
  };
});

const currentInfectionRate = computed(() => {
  if (!props.metrics || props.metrics.totalAgents === 0) return "0.0";
  return (
    (props.metrics.activeInfections / props.metrics.totalAgents) *
    100
  ).toFixed(1);
});

const reportingRate = computed(() => {
  if (!props.metrics || props.metrics.symptomaticCases === 0) return "0.0";
  return (
    (props.metrics.reportedCases / props.metrics.symptomaticCases) *
    100
  ).toFixed(1);
});

function toggleSection(section: keyof typeof expandedSections.value) {
  expandedSections.value[section] = !expandedSections.value[section];
}

function formatNumber(num: number): string {
  return new Intl.NumberFormat().format(num);
}

function formatTime(tick: number): string {
  if (!isFinite(tick) || tick < 0) {
    return "000d 00h 00m";
  }

  const tickLength = props.simulationData?.tickLength ?? 300;
  const totalSeconds = Math.floor(tick) * tickLength;
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);

  return `${String(days).padStart(3, "0")}d ${String(hours).padStart(
    2,
    "0",
  )}h ${String(minutes).padStart(2, "0")}m`;
}

function togglePlayPause() {
  emit("togglePlayPause");
}

function handleSpeedChange(event: Event) {
  const target = event.target as HTMLInputElement;
  const speed = parseFloat(target.value);
  emit("speedChange", speed);
}

function handleTickChange(event: Event) {
  const target = event.target as HTMLInputElement;
  const tick = parseInt(target.value);
  emit("tickChange", tick);
}
</script>

<style scoped>
.tabular-nums {
  font-variant-numeric: tabular-nums;
}

/* Custom Timeline Slider */
.timeline-slider {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  cursor: pointer;
  z-index: 10;
  -webkit-appearance: none;
  appearance: none;
  background: transparent;
  margin: 0;
}

.timeline-slider::-webkit-slider-runnable-track {
  width: 100%;
  height: 100%;
  background: transparent;
  border: none;
  cursor: pointer;
}

.timeline-slider::-webkit-slider-thumb {
  -webkit-appearance: none;
  appearance: none;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: hsl(var(--wa));
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
  border: 3px solid hsl(var(--b1));
  position: relative;
  margin-top: -8.5px;
  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
}

.timeline-slider::-moz-range-track {
  width: 100%;
  height: 100%;
  background: transparent;
  border: none;
  cursor: pointer;
}

.timeline-slider::-moz-range-thumb {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: hsl(var(--wa));
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
  border: 3px solid hsl(var(--b1));
  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
}

.timeline-slider:hover::-webkit-slider-thumb {
  transform: scale(1.15);
  box-shadow: 0 3px 12px rgba(0, 0, 0, 0.4);
}

.timeline-slider:hover::-moz-range-thumb {
  transform: scale(1.15);
  box-shadow: 0 3px 12px rgba(0, 0, 0, 0.4);
}

.timeline-slider:disabled {
  cursor: not-allowed;
}

.timeline-slider:disabled::-webkit-slider-thumb {
  cursor: not-allowed;
  opacity: 0.5;
  background: hsl(var(--b3));
}

.timeline-slider:disabled::-moz-range-thumb {
  cursor: not-allowed;
  opacity: 0.5;
  background: hsl(var(--b3));
}
</style>
