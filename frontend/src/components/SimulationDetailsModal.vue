<script setup lang="ts">
import { ref, watch } from "vue";
import axios from "../api/axios";
import { useToastStore } from "@/stores/useToastStore";
import type { SimulationDetail } from "@/api/simulation";
const toast = useToastStore();

interface Props {
  simulationId: number | null;
}

const props = defineProps<Props>();
const emit = defineEmits(["update:show", "deleted", "open"]);
const show = defineModel<boolean>({ required: true });
const previewData = ref<SimulationDetail | null>(null);

watch(
  () => props.simulationId,
  async (id) => {
    if (id && show) {
      await fetchSimulationData(id);
    }
  },
  { immediate: true }
);

watch(show, async (isShown) => {
  if (isShown && props.simulationId) {
    await fetchSimulationData(props.simulationId);
  }
});

const fetchSimulationData = async (id: number) => {
  try {
    const response = await axios.get(`/Simulation/${id}`);
    previewData.value = response.data;
  } catch (error) {
    toast.show("Failed to load simulation details.", "error");
    previewData.value = null;
  }
};

const deleteSimulation = async () => {
  if (!previewData.value) return;
  if (
    !window.confirm(
      "Are you sure you want to delete this simulation? This action cannot be undone."
    )
  ) {
    return;
  }
  try {
    await axios.delete(`/Simulation/${previewData.value.id}`);
    toast.show("Simulation deleted successfully.", "success");
    show.value = false;
    emit("deleted");
  } catch (error) {
    toast.show("Failed to delete simulation.", "error");
  }
};

const openSimulation = () => {
  if (previewData.value) {
    emit("open", previewData.value.id);
    show.value = false;
  }
};
</script>

<template>
  <div>
    <input
      type="checkbox"
      id="sneak-sim-modal"
      class="modal-toggle"
      v-model="show"
    />

    <div class="modal">
      <div class="modal-box max-w-3xl">
        <h3 class="font-bold text-2xl mb-4">Simulation Details</h3>

        <div v-if="previewData" class="space-y-4">
          <!-- Basic Info -->
          <div class="card bg-base-200">
            <div class="card-body">
              <h4 class="card-title text-lg">Basic Information</h4>
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <span class="text-base-content/70">Name:</span>
                  <p class="font-semibold">{{ previewData.name }}</p>
                </div>
                <div>
                  <span class="text-base-content/70">Status:</span>
                  <div
                    class="badge"
                    :class="{
                      'badge-success': previewData.status === 'completed',
                      'badge-info': previewData.status === 'running',
                      'badge-warning': previewData.status === 'paused',
                      'badge-error': previewData.status === 'failed',
                    }"
                  >
                    {{ previewData.status }}
                  </div>
                </div>
                <div>
                  <span class="text-base-content/70">Created:</span>
                  <p class="font-semibold">
                    {{ new Date(previewData.createdAt).toLocaleDateString() }}
                  </p>
                </div>
                <div>
                  <span class="text-base-content/70">Total Agents:</span>
                  <p class="font-semibold">{{ previewData.agentCount }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Map Area -->
          <div class="card bg-base-200">
            <div class="card-body">
              <h4 class="card-title text-lg">Map Area</h4>
              <div v-if="previewData.mapArea" class="grid grid-cols-2 gap-4">
                <div>
                  <span class="text-base-content/70">Name:</span>
                  <p class="font-semibold">{{ previewData.mapArea.name }}</p>
                </div>
                <div>
                  <span class="text-base-content/70">Buildings:</span>
                  <p class="font-semibold">
                    {{ previewData.mapArea.buildingCount }}
                  </p>
                </div>
                <div>
                  <span class="text-base-content/70">Center (Lat, Lon):</span>
                  <p class="font-semibold">
                    {{ previewData.mapArea.centerLat?.toFixed(4) }},
                    {{ previewData.mapArea.centerLon?.toFixed(4) }}
                  </p>
                </div>
                <div>
                  <span class="text-base-content/70">Radius:</span>
                  <p class="font-semibold">
                    {{ previewData.mapArea.radiusKm }} km
                  </p>
                </div>
              </div>
              <p v-else class="text-base-content/70">No map area configured</p>
            </div>
          </div>

          <!-- Population -->
          <div class="card bg-base-200">
            <div class="card-body">
              <h4 class="card-title text-lg">Population</h4>
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <span class="text-base-content/70">Total Agents:</span>
                  <p class="font-semibold text-2xl">
                    {{ previewData.agentCount }}
                  </p>
                </div>
                <div v-if="previewData.mapArea">
                  <span class="text-base-content/70">Density:</span>
                  <p class="font-semibold">
                    {{
                      Math.round(
                        previewData.agentCount /
                          previewData.mapArea.buildingCount
                      )
                    }}
                    agents/building
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- Disease Info -->
          <div class="card bg-base-200">
            <div class="card-body">
              <h4 class="card-title text-lg">Disease</h4>
              <div v-if="previewData.disease" class="grid grid-cols-2 gap-4">
                <div class="col-span-2">
                  <span class="text-base-content/70">Name:</span>
                  <p class="font-semibold text-xl">
                    {{ previewData.disease.name }}
                  </p>
                </div>
                <div>
                  <span class="text-base-content/70">Infection Rate:</span>
                  <p class="font-semibold">
                    {{ (previewData.disease.infectionRate * 100).toFixed(1) }}%
                  </p>
                </div>
                <div>
                  <span class="text-base-content/70">Recovery Rate:</span>
                  <p class="font-semibold">
                    {{ (previewData.disease.recoveryRate * 100).toFixed(1) }}%
                  </p>
                </div>
                <div>
                  <span class="text-base-content/70">Mortality Rate:</span>
                  <p class="font-semibold text-error">
                    {{ (previewData.disease.mortalityRate * 100).toFixed(1) }}%
                  </p>
                </div>
              </div>
              <p v-else class="text-base-content/70">No disease configured</p>
            </div>
          </div>

          <!-- Progress (if running) -->
          <div v-if="previewData.status === 'running'" class="card bg-base-200">
            <div class="card-body">
              <h4 class="card-title text-lg">Progress</h4>
              <progress
                class="progress progress-primary w-full"
                :value="previewData.progress || 0"
                max="100"
              ></progress>
              <p class="text-sm text-center mt-2">
                {{ previewData.progress || 0 }}%
              </p>
            </div>
          </div>
        </div>
        <div v-else class="text-center py-8">
          <span class="loading loading-spinner loading-lg"></span>
          <p class="mt-2">Loading simulation data...</p>
        </div>

        <!-- Modal Actions -->
        <div class="modal-action">
          <div class="flex justify-between w-full">
            <button class="btn btn-error" @click="deleteSimulation">
              Delete
            </button>
            <div>
              <button class="btn mr-2" @click="show = false">Close</button>
              <button class="btn btn-primary" @click="openSimulation">
                Open Simulation
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
