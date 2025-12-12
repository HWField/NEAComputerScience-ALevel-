<template>
  <div class="grid grid-cols-12 gap-6">
    <!-- Left Column - Disease Profile -->
    <div class="col-span-6 space-y-6">
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
                  {{ (simulationData.disease.mortalityRate * 100).toFixed(1) }}%
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">
                  Symptomatic Rate
                </div>
                <div class="font-bold">
                  {{
                    (simulationData.disease.symptomaticRate * 100).toFixed(1)
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
                    (simulationData.disease.severeSymptomsRate * 100).toFixed(
                      1
                    )
                  }}%</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Hospital Seeking</span>
                <span class="font-medium"
                  >{{
                    (simulationData.disease.hospitalSeekingRate * 100).toFixed(
                      1
                    )
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
                    (simulationData.disease.maskEffectiveness * 100).toFixed(0)
                  }}% reduction</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Vaccines</span>
                <span class="font-medium text-success"
                  >{{
                    (simulationData.disease.vaccineEffectiveness * 100).toFixed(
                      0
                    )
                  }}% reduction</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Reinfection Resistance</span>
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
            <div class="divider my-2"></div>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <div class="text-xs text-base-content/70 mb-1">Radius</div>
                <div class="font-bold">
                  {{ simulationData.mapArea.radiusKm }} km
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">Buildings</div>
                <div class="font-bold">
                  {{ formatNumber(simulationData.mapArea.buildingCount) }}
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">Area</div>
                <div class="font-bold">
                  {{
                    (
                      Math.PI * Math.pow(simulationData.mapArea.radiusKm, 2)
                    ).toFixed(1)
                  }}
                  km²
                </div>
              </div>
            </div>
            <div class="divider my-2"></div>
            <div class="grid grid-cols-2 gap-2 text-sm">
              <div class="flex justify-between">
                <span class="text-base-content/70">Latitude</span>
                <span class="font-medium font-mono">{{
                  simulationData.mapArea.centerLat.toFixed(4)
                }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Longitude</span>
                <span class="font-medium font-mono">{{
                  simulationData.mapArea.centerLon.toFixed(4)
                }}</span>
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
    </div>

    <!-- Right Column - Population & Mitigations -->
    <div class="col-span-6 space-y-6">
      <!-- Population Stats Card -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <div
            class="flex items-center justify-between cursor-pointer"
            @click="toggleSection('population')"
          >
            <h3 class="card-title text-lg flex items-center gap-2">
              <i-ph-users class="w-5 h-5 text-primary" />
              Population Configuration
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
                {{ formatNumber(simulationData?.agentCount || 0) }}
              </div>
              <div class="stat-desc">Simulated population size</div>
            </div>

            <div class="divider my-2"></div>

            <!-- Agent Role Distribution (Placeholder for future feature) -->
            <div class="text-sm text-base-content/70">
              <div class="font-semibold mb-2">Agent Distribution</div>
              <p class="text-xs text-base-content/60 italic">
                Detailed agent role breakdown will be displayed here when
                available
              </p>
            </div>
          </div>
          <div v-else class="text-sm text-base-content/60 italic mt-2">
            Click to expand
          </div>
        </div>
      </div>

      <!-- Mitigations Configuration -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <h3 class="card-title text-lg flex items-center gap-2">
            <i-ph-shield class="w-5 h-5 text-success" />
            Mitigation Strategies
          </h3>
          <MitigationPanel
            v-if="simulationData"
            :simulation-id="simulationData.id"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref } from "vue";
import type { SimulationDetail } from "@/api/simulation";
import MitigationPanel from "@/components/MitigationPanel.vue";

interface Props {
  simulationData: SimulationDetail | null;
}

const props = defineProps<Props>();

const expandedSections = ref({
  disease: true,
  mapArea: true,
  population: true,
});

function toggleSection(section: keyof typeof expandedSections.value) {
  expandedSections.value[section] = !expandedSections.value[section];
}

function formatNumber(num: number): string {
  return new Intl.NumberFormat().format(num);
}
</script>
