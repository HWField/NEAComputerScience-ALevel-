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
              <div
                class="flex items-center justify-between cursor-pointer"
                @click="toggleSection('disease')"
              >
                <h3 class="card-title text-lg flex items-center gap-2">
                  <i-ph-virus class="w-5 h-5 text-error" />
                  Disease Profile
                </h3>
                <i-ph-caret-down
                  class="w-5 h-5 transition-transform"
                  :class="{ 'rotate-180': expandedSections.disease }"
                />
              </div>
              <div
                v-if="expandedSections.disease && simulationData?.disease"
                class="space-y-3 mt-4"
              >
                <div>
                  <div class="text-xs text-base-content/70 mb-1">Name</div>
                  <div class="font-semibold">
                    {{ simulationData.disease.name }}
                  </div>
                </div>
                <div class="divider my-2"></div>
                <div class="grid grid-cols-2 gap-2">
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Infection Rate
                    </div>
                    <div class="text-error font-bold">
                      {{
                        (simulationData.disease.infectionRate * 100).toFixed(1)
                      }}%/hr
                    </div>
                  </div>
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Recovery Rate
                    </div>
                    <div class="text-success font-bold">
                      {{
                        (simulationData.disease.recoveryRate * 100).toFixed(1)
                      }}%/day
                    </div>
                  </div>
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Mortality Rate
                    </div>
                    <div class="text-warning font-bold">
                      {{
                        (simulationData.disease.mortalityRate * 100).toFixed(1)
                      }}%
                    </div>
                  </div>
                  <div>
                    <div class="text-xs text-base-content/70 mb-1">
                      Symptomatic Rate
                    </div>
                    <div class="font-bold">
                      {{
                        (simulationData.disease.symptomaticRate * 100).toFixed(
                          1
                        )
                      }}%
                    </div>
                  </div>
                </div>
                <div class="divider my-2"></div>
                <div class="space-y-2 text-sm">
                  <div class="flex justify-between">
                    <span class="text-base-content/70">Incubation Period</span>
                    <span class="font-medium"
                      >{{ simulationData.disease.incubationPeriod }} days</span
                    >
                  </div>
                  <div class="flex justify-between">
                    <span class="text-base-content/70">Infectious Period</span>
                    <span class="font-medium"
                      >{{ simulationData.disease.infectiousPeriod }} days</span
                    >
                  </div>
                  <div class="flex justify-between">
                    <span class="text-base-content/70">Immunity Duration</span>
                    <span class="font-medium"
                      >{{ simulationData.disease.immunityDuration }} days</span
                    >
                  </div>
                  <div class="flex justify-between">
                    <span class="text-base-content/70">Severe Symptoms</span>
                    <span class="font-medium"
                      >{{
                        (
                          simulationData.disease.severeSymptomsRate * 100
                        ).toFixed(1)
                      }}%</span
                    >
                  </div>
                  <div class="flex justify-between">
                    <span class="text-base-content/70">Hospital Seeking</span>
                    <span class="font-medium"
                      >{{
                        (
                          simulationData.disease.hospitalSeekingRate * 100
                        ).toFixed(1)
                      }}%</span
                    >
                  </div>
                </div>
                <div class="divider my-2"></div>
                <div class="space-y-2 text-sm">
                  <div class="text-xs text-base-content/70 font-semibold mb-1">
                    Intervention Effectiveness
                  </div>
                  <div class="flex justify-between">
                    <span class="text-base-content/70">Masks</span>
                    <span class="font-medium text-info"
                      >{{
                        (
                          simulationData.disease.maskEffectiveness * 100
                        ).toFixed(0)
                      }}% reduction</span
                    >
                  </div>
                  <div class="flex justify-between">
                    <span class="text-base-content/70">Vaccines</span>
                    <span class="font-medium text-success"
                      >{{
                        (
                          simulationData.disease.vaccineEffectiveness * 100
                        ).toFixed(0)
                      }}% reduction</span
                    >
                  </div>
                  <div class="flex justify-between">
                    <span class="text-base-content/70"
                      >Reinfection Resistance</span
                    >
                    <span class="font-medium"
                      >{{
                        (
                          simulationData.disease.reinfectionResistance * 100
                        ).toFixed(0)
                      }}%</span
                    >
                  </div>
                </div>
              </div>
              <div
                v-else-if="!expandedSections.disease"
                class="text-sm text-base-content/60 italic mt-2"
              >
                Click to expand
              </div>
              <div v-else class="text-sm text-base-content/60 italic mt-2">
                No disease configured
              </div>
            </div>
          </div>

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
                        metrics?.totalAgents || simulationData?.agentCount || 0
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

          <!-- Mitigation Panel -->
          <MitigationPanel
            v-if="simulationData"
            :simulation-id="simulationData.id"
          />
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

        <!-- Right Sidebar - System Metrics -->
        <div class="col-span-3 space-y-6">
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
          <!-- Map Area Info -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <div
                class="flex items-center justify-between cursor-pointer"
                @click="toggleSection('mapArea')"
              >
                <h3 class="card-title text-lg flex items-center gap-2">
                  <i-ph-map-pin-bold class="w-5 h-5 text-accent" />
                  Map Area
                </h3>
                <i-ph-caret-down
                  class="w-5 h-5 transition-transform"
                  :class="{ 'rotate-180': expandedSections.mapArea }"
                />
              </div>
              <div
                v-if="expandedSections.mapArea && simulationData?.mapArea"
                class="space-y-3 mt-4"
              >
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
              <div
                v-else-if="!expandedSections.mapArea"
                class="text-sm text-base-content/60 italic mt-2"
              >
                Click to expand
              </div>
              <div v-else class="text-sm text-base-content/60 italic mt-2">
                No map area configured
              </div>
            </div>
          </div>
          <!-- Quick Actions -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <div
                class="flex items-center justify-between cursor-pointer"
                @click="toggleSection('quickActions')"
              >
                <h3 class="card-title text-lg flex items-center gap-2">
                  <i-ph-lightning-bold class="w-5 h-5 text-accent" />
                  Quick Actions
                </h3>
                <i-ph-caret-down
                  class="w-5 h-5 transition-transform"
                  :class="{ 'rotate-180': expandedSections.quickActions }"
                />
              </div>
              <div v-if="expandedSections.quickActions" class="space-y-2 mt-4">
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
              <div v-else class="text-sm text-base-content/60 italic mt-2">
                Click to expand
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
                  class="text-2xl font-mono font-bold tabular-nums"
                  :class="isComputing ? 'text-info' : 'text-primary'"
                >
                  {{ simulationDateTime.date }}
                  {{ simulationDateTime.time }}
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
                    Duration:
                    <span class="font-mono">{{ formattedTime }}</span>
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
              <div
                class="flex justify-between text-xs text-base-content/60 mt-3"
              >
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
              <div
                class="flex justify-between text-xs text-base-content/60 mt-1"
              >
                <span>1x</span>
                <span>50x</span>
                <span>100x</span>
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
import MitigationPanel from "@/components/MitigationPanel.vue";

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

// Performance tracking
const serverTickRate = ref(0);
const avgLatency = ref(0);
const tickHistory = ref<{ tick: number; timestamp: number }[]>([]);

const maxTick = computed(() => {
  if (!simulationData.value) return 100000; // default fallback
  const tickLength = simulationData.value.tickLength; // seconds per tick
  const secondsInYear = 365 * 24 * 60 * 60; // 31,536,000 seconds
  return Math.floor(secondsInYear / tickLength);
});

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

const formattedTime = computed(() => {
  return formatTime(currentTick.value);
});

const simulationDateTime = computed(() => {
  const tickLength = simulationData.value?.tickLength ?? 300; // default 5 min
  const totalSeconds = Math.floor(currentTick.value) * tickLength;

  // Calculate date starting from January 1st, 2025
  const startDate = new Date("2025-01-01T00:00:00");
  const totalMilliseconds = totalSeconds * 1000;
  const currentDate = new Date(startDate.getTime() + totalMilliseconds);

  // Format date as dd/mm/yy
  const day = String(currentDate.getDate()).padStart(2, "0");
  const month = String(currentDate.getMonth() + 1).padStart(2, "0");
  const year = String(currentDate.getFullYear()).slice(-2);
  const dateStr = `${day}/${month}/${year}`;

  // Format time as HH:MM
  const hours = String(currentDate.getHours()).padStart(2, "0");
  const minutes = String(currentDate.getMinutes()).padStart(2, "0");
  const timeStr = `${hours}:${minutes}`;

  return {
    date: dateStr,
    time: timeStr,
  };
});

const infectionRate = computed(() => {
  if (!metrics.value || metrics.value.totalAgents === 0) return 0;
  return ((metrics.value.infected / metrics.value.totalAgents) * 100).toFixed(
    1
  );
});

const recoveryRate = computed(() => {
  if (!metrics.value || metrics.value.totalAgents === 0) return 0;
  return ((metrics.value.recovered / metrics.value.totalAgents) * 100).toFixed(
    1
  );
});

const expandedSections = ref({
  disease: true,
  population: true,
  infection: true,
  mapArea: true,
  quickActions: true,
});

function toggleSection(section: keyof typeof expandedSections.value) {
  expandedSections.value[section] = !expandedSections.value[section];
}

const currentInfectionRate = computed(() => {
  if (!metrics.value || metrics.value.totalAgents === 0) return "0.0";
  return (
    (metrics.value.activeInfections / metrics.value.totalAgents) *
    100
  ).toFixed(1);
});

const reportingRate = computed(() => {
  if (!metrics.value || metrics.value.symptomaticCases === 0) return "0.0";
  return (
    (metrics.value.reportedCases / metrics.value.symptomaticCases) *
    100
  ).toFixed(1);
});

function formatNumber(num: number): string {
  return new Intl.NumberFormat().format(num);
}

function formatTime(tick: number): string {
  // Handle invalid tick values
  if (!isFinite(tick) || tick < 0) {
    return "000d 00h 00m";
  }

  const tickLength = simulationData.value?.tickLength ?? 300; // default 5 min
  const totalSeconds = Math.floor(tick) * tickLength;
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);

  // Always show all units with fixed width padding for consistent display
  return `${String(days).padStart(3, "0")}d ${String(hours).padStart(
    2,
    "0"
  )}h ${String(minutes).padStart(2, "0")}m`;
}

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

onMounted(() => {
  loadSimulation();
});

onUnmounted(() => {
  disconnect();
});

// Debug: Watch interpolated tick
watch(currentInterpolatedTick, (newVal) => {
  if (!isFinite(newVal)) {
    console.error("[Simulation] Invalid interpolated tick:", newVal);
  }
});
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

/* Chrome/Safari slider track */
.timeline-slider::-webkit-slider-runnable-track {
  width: 100%;
  height: 100%;
  background: transparent;
  border: none;
  cursor: pointer;
}

/* Chrome/Safari slider thumb */
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
  margin-top: -8.5px; /* Center the thumb vertically */
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

/* Firefox slider track */
.timeline-slider::-moz-range-track {
  width: 100%;
  height: 100%;
  background: transparent;
  border: none;
  cursor: pointer;
}

/* Firefox slider thumb */
.timeline-slider::-moz-range-thumb {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: hsl(var(--wa));
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
  border: 3px solid hsl(var(--b1));
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

/* Hover effects */
.timeline-slider:hover::-webkit-slider-thumb {
  transform: scale(1.15);
  box-shadow: 0 3px 12px rgba(0, 0, 0, 0.4);
}

.timeline-slider:hover::-moz-range-thumb {
  transform: scale(1.15);
  box-shadow: 0 3px 12px rgba(0, 0, 0, 0.4);
}

/* Disabled state */
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
