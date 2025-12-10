<template>
  <div class="card bg-base-100 shadow-xl">
    <div class="card-body">
      <h2 class="card-title flex items-center gap-2">
        <i-ph-shield-check class="w-6 h-6 text-primary" />
        Disease Mitigations
      </h2>

      <!-- Loading State -->
      <div v-if="loading" class="flex justify-center py-8">
        <span class="loading loading-spinner loading-lg"></span>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="alert alert-error">
        <i-ph-warning-circle class="w-5 h-5" />
        <span>{{ error }}</span>
      </div>

      <!-- Mitigations List -->
      <div v-else class="space-y-4">
        <!-- Self-Isolation -->
        <div class="bg-base-200 rounded-lg">
          <div class="flex items-center justify-between p-4">
            <div class="flex items-center gap-2">
              <i-ph-house class="w-5 h-5" />
              <span class="font-medium">Self-Isolation</span>
              <span
                v-if="hasMitigation('SelfIsolation')"
                class="badge badge-success badge-sm"
                >Active</span
              >
            </div>
            <button
              @click="toggleMitigation('SelfIsolation')"
              class="btn btn-sm btn-circle"
              :class="
                hasMitigation('SelfIsolation') ? 'btn-error' : 'btn-primary'
              "
            >
              <i-ph-x v-if="hasMitigation('SelfIsolation')" class="w-5 h-5" />
              <i-ph-plus v-else class="w-5 h-5" />
            </button>
          </div>
          <div v-if="hasMitigation('SelfIsolation')" class="px-4 pb-4 pt-0">
            <div class="space-y-4">
              <div>
                <label class="label">
                  <span class="label-text"
                    >Compliance Rate ({{
                      formatPercent(selfIsolation?.complianceRate ?? 0.7)
                    }})</span
                  >
                  <span class="label-text-alt"
                    >% of symptomatic agents who will self-isolate</span
                  >
                </label>
                <input
                  type="range"
                  min="0"
                  max="100"
                  :value="(selfIsolation?.complianceRate ?? 0.7) * 100"
                  @input="updateSelfIsolation('complianceRate', $event)"
                  class="range range-primary range-sm"
                />
              </div>

              <div>
                <label class="label">
                  <span class="label-text"
                    >Symptom Threshold ({{
                      formatPercent(selfIsolation?.symptomThreshold ?? 0.3)
                    }})</span
                  >
                  <span class="label-text-alt"
                    >Minimum symptom severity to trigger isolation</span
                  >
                </label>
                <input
                  type="range"
                  min="0"
                  max="100"
                  :value="(selfIsolation?.symptomThreshold ?? 0.3) * 100"
                  @input="updateSelfIsolation('symptomThreshold', $event)"
                  class="range range-primary range-sm"
                />
                <div class="flex justify-between text-xs px-2 mt-1">
                  <span>Mild</span>
                  <span>Severe</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Mask Wearing -->
        <div class="bg-base-200 rounded-lg">
          <div class="flex items-center justify-between p-4">
            <div class="flex items-center gap-2">
              <i-ph-mask-happy class="w-5 h-5" />
              <span class="font-medium">Mask Wearing</span>
              <span
                v-if="hasMitigation('MaskWearing')"
                class="badge badge-success badge-sm"
                >Active</span
              >
            </div>
            <button
              @click="toggleMitigation('MaskWearing')"
              class="btn btn-sm btn-circle"
              :class="
                hasMitigation('MaskWearing') ? 'btn-error' : 'btn-primary'
              "
            >
              <i-ph-x v-if="hasMitigation('MaskWearing')" class="w-5 h-5" />
              <i-ph-plus v-else class="w-5 h-5" />
            </button>
          </div>
          <div v-if="hasMitigation('MaskWearing')" class="px-4 pb-4 pt-0">
            <div class="space-y-4">
              <div>
                <label class="label">
                  <span class="label-text"
                    >Adoption Rate ({{
                      formatPercent(maskWearing?.adoptionRate ?? 0.6)
                    }})</span
                  >
                  <span class="label-text-alt"
                    >% of population that will wear masks</span
                  >
                </label>
                <input
                  type="range"
                  min="0"
                  max="100"
                  :value="(maskWearing?.adoptionRate ?? 0.6) * 100"
                  @input="updateMaskWearing('adoptionRate', $event)"
                  class="range range-primary range-sm"
                />
              </div>

              <div class="alert alert-info">
                <i-ph-info class="w-5 h-5" />
                <span class="text-sm"
                  >Mask effectiveness is determined by the disease
                  configuration.</span
                >
              </div>
            </div>
          </div>
        </div>

        <!-- Vaccination -->
        <div class="bg-base-200 rounded-lg">
          <div class="flex items-center justify-between p-4">
            <div class="flex items-center gap-2">
              <i-ph-syringe class="w-5 h-5" />
              <span class="font-medium">Vaccination</span>
              <span
                v-if="hasMitigation('Vaccination')"
                class="badge badge-success badge-sm"
                >Active</span
              >
            </div>
            <button
              @click="toggleMitigation('Vaccination')"
              class="btn btn-sm btn-circle"
              :class="
                hasMitigation('Vaccination') ? 'btn-error' : 'btn-primary'
              "
            >
              <i-ph-x v-if="hasMitigation('Vaccination')" class="w-5 h-5" />
              <i-ph-plus v-else class="w-5 h-5" />
            </button>
          </div>
          <div v-if="hasMitigation('Vaccination')" class="px-4 pb-4 pt-0">
            <div class="space-y-4">
              <div>
                <label class="label">
                  <span class="label-text"
                    >Vaccination Rate ({{
                      formatPercent(vaccination?.vaccinationRate ?? 0.7)
                    }})</span
                  >
                  <span class="label-text-alt"
                    >% of population that will get vaccinated</span
                  >
                </label>
                <input
                  type="range"
                  min="0"
                  max="100"
                  :value="(vaccination?.vaccinationRate ?? 0.7) * 100"
                  @input="updateVaccination('vaccinationRate', $event)"
                  class="range range-primary range-sm"
                />
              </div>

              <div>
                <label class="label">
                  <span class="label-text"
                    >Days to Full Effectiveness ({{
                      vaccination?.daysToFullEffectiveness ?? 14
                    }})</span
                  >
                  <span class="label-text-alt"
                    >Time for vaccine to reach full protection</span
                  >
                </label>
                <input
                  type="range"
                  min="1"
                  max="30"
                  :value="vaccination?.daysToFullEffectiveness ?? 14"
                  @input="updateVaccination('daysToFullEffectiveness', $event)"
                  class="range range-primary range-sm"
                />
              </div>

              <div class="alert alert-info">
                <i-ph-info class="w-5 h-5" />
                <span class="text-sm"
                  >Vaccine effectiveness is determined by the disease
                  configuration.</span
                >
              </div>
            </div>
          </div>
        </div>

        <!-- Warning about data invalidation -->
        <div class="alert alert-warning" v-if="mitigations.length > 0">
          <i-ph-warning class="w-5 h-5" />
          <div class="text-sm">
            <strong>Note:</strong> Changing mitigations will delete all future
            simulation snapshots and metrics as the outcomes will be different.
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue";
import {
  simulationApi,
  type Mitigation,
  type CreateMitigationDto,
  type UpdateMitigationDto,
} from "../api/simulation";
import { useToastStore } from "../stores/useToastStore";

const props = defineProps<{
  simulationId: number;
}>();

const emit = defineEmits<{
  (e: "mitigationChanged"): void;
}>();

const toast = useToastStore();
const loading = ref(false);
const error = ref<string | null>(null);
const mitigations = ref<Mitigation[]>([]);

// Computed getters for each mitigation type
const selfIsolation = computed(() =>
  mitigations.value.find((m) => m.type === "SelfIsolation")
);
const maskWearing = computed(() =>
  mitigations.value.find((m) => m.type === "MaskWearing")
);
const vaccination = computed(() =>
  mitigations.value.find((m) => m.type === "Vaccination")
);

onMounted(() => {
  loadMitigations();
});

// Reload when simulation ID changes
watch(() => props.simulationId, loadMitigations);

async function loadMitigations() {
  try {
    loading.value = true;
    error.value = null;
    mitigations.value = await simulationApi.getMitigations(props.simulationId);
  } catch (err: any) {
    error.value = err.response?.data?.message || "Failed to load mitigations";
    toast.show(error.value ?? "Failed to load mitigations", "error");
  } finally {
    loading.value = false;
  }
}

function hasMitigation(type: string): boolean {
  return mitigations.value.some((m) => m.type === type && m.isActive);
}

function formatPercent(value: number): string {
  return `${Math.round(value * 100)}%`;
}

async function toggleMitigation(
  type: "SelfIsolation" | "MaskWearing" | "Vaccination"
) {
  const existing = mitigations.value.find((m) => m.type === type);

  if (existing) {
    // Toggle off (remove)
    await removeMitigation(type);
  } else {
    // Toggle on (create)
    await createMitigation(type);
  }
}

async function createMitigation(
  type: "SelfIsolation" | "MaskWearing" | "Vaccination"
) {
  try {
    loading.value = true;
    const dto: CreateMitigationDto = { type };

    // Set default values based on type
    if (type === "SelfIsolation") {
      dto.complianceRate = 0.7;
      dto.symptomThreshold = 0.3;
    } else if (type === "MaskWearing") {
      dto.adoptionRate = 0.6;
    } else if (type === "Vaccination") {
      dto.vaccinationRate = 0.7;
      dto.daysToFullEffectiveness = 14;
    }

    const newMitigation = await simulationApi.createMitigation(
      props.simulationId,
      dto
    );
    mitigations.value.push(newMitigation);
    toast.show(`${type} mitigation added successfully`, "success");
    emit("mitigationChanged");
  } catch (err: any) {
    const errorMsg =
      err.response?.data?.message || `Failed to add ${type} mitigation`;
    toast.show(errorMsg, "error");
  } finally {
    loading.value = false;
  }
}

async function removeMitigation(type: string) {
  const mitigation = mitigations.value.find((m) => m.type === type);
  if (!mitigation) return;

  try {
    loading.value = true;
    await simulationApi.deleteMitigation(props.simulationId, mitigation.id);
    mitigations.value = mitigations.value.filter((m) => m.id !== mitigation.id);
    toast.show(`${type} mitigation removed successfully`, "success");
    emit("mitigationChanged");
  } catch (err: any) {
    const errorMsg =
      err.response?.data?.message || `Failed to remove ${type} mitigation`;
    toast.show(errorMsg, "error");
  } finally {
    loading.value = false;
  }
}

async function updateSelfIsolation(field: string, event: Event) {
  if (!selfIsolation.value) return;

  const value = parseFloat((event.target as HTMLInputElement).value) / 100;
  const dto: UpdateMitigationDto = {};

  if (field === "complianceRate") dto.complianceRate = value;
  else if (field === "symptomThreshold") dto.symptomThreshold = value;

  await updateMitigation(selfIsolation.value.id, dto);
}

async function updateMaskWearing(field: string, event: Event) {
  if (!maskWearing.value) return;

  const value = parseFloat((event.target as HTMLInputElement).value) / 100;
  const dto: UpdateMitigationDto = { adoptionRate: value };

  await updateMitigation(maskWearing.value.id, dto);
}

async function updateVaccination(field: string, event: Event) {
  if (!vaccination.value) return;

  const value =
    field === "daysToFullEffectiveness"
      ? parseInt((event.target as HTMLInputElement).value)
      : parseFloat((event.target as HTMLInputElement).value) / 100;

  const dto: UpdateMitigationDto = {};
  if (field === "vaccinationRate") dto.vaccinationRate = value;
  else if (field === "daysToFullEffectiveness")
    dto.daysToFullEffectiveness = value;

  await updateMitigation(vaccination.value.id, dto);
}

async function updateMitigation(id: number, dto: UpdateMitigationDto) {
  try {
    const updated = await simulationApi.updateMitigation(
      props.simulationId,
      id,
      dto
    );
    const index = mitigations.value.findIndex((m) => m.id === id);
    if (index !== -1) {
      mitigations.value[index] = updated;
    }
    emit("mitigationChanged");
  } catch (err: any) {
    const errorMsg =
      err.response?.data?.message || "Failed to update mitigation";
    toast.show(errorMsg, "error");
  }
}
</script>
