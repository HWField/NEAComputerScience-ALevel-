<script setup lang="ts">
import { ref, watch, onMounted, nextTick } from "vue";
import axios from "../api/axios";
import { useToastStore } from "@/stores/useToastStore";
import L from "leaflet";
import { GeoSearchControl, OpenStreetMapProvider } from "leaflet-geosearch";
const toast = useToastStore();
const emit = defineEmits(["created"]);

const show = defineModel<boolean>({ required: true });
const step = ref(1);
const loading = ref(false);
const loadingMessage = ref("");

interface Preset {
  id: number;
  name: string;
}
const mapAreaPresets = ref<Preset[]>([]);
const diseasePresets = ref<Preset[]>([]);
const newSimDetails = ref({});

// Leaflet map refs
const map = ref<L.Map | null>(null);
const circle = ref<L.Circle | null>(null);
const radiusKm = ref(1.0); // Fixed radius in kilometers (real-world distance)

const createNewSimulation = async () => {
  const response = await axios.get("/Preset/mapareas");
  mapAreaPresets.value = response.data.map((item: any) => ({
    id: item.id,
    name: item.name,
  }));
  const response2 = await axios.get("/Preset/diseases");
  diseasePresets.value = response2.data.map((item: any) => ({
    id: item.id,
    name: item.name,
  }));

  newSimDetails.value = {
    id: 0,
    // step 1 setup
    name: "",
    icon: "",
    // step 2 map
    mapType: "preset",
    mapPresetId: 0,
    customMap: {
      PresetName: "",
      longitude: 0,
      latitude: 0,
      radius: 0,
      savePreset: true,
    },
    // step 3 population
    maxPopulation: {
      MaxTotalPopulation: 0,
      MaxStudents: 0,
      MaxOfficeWorkers: 0,
      MaxMedicalWorkers: 0,
      MaxRetailWorkers: 0,
      MaxServiceWorkers: 0,
      MaxElderlyRetired: 0,
    },
    population: {
      Student: 0,
      OfficeWorker: 0,
      MedicalWorker: 0,
      EdlerlyRetiredAgent: 0,
      RetailWorker: 0,
      ServiceWorker: 0,
    },
    // step 4 disease
    diseaseType: "preset",
    diseasePresetID: 0,
    customDisease: {
      PresetName: "",
      infectionRate: 0.0,
      recoveryRate: 0.0,
      mortalityRate: 0.0,
      savePreset: false,
    },
  };
  step.value = 1;
  show.value = true;
};

watch(show, (val) => {
  if (val) {
    createNewSimulation();
  }
});

// Initialize map
watch(
  () => [step.value, newSimDetails.value.mapType],
  async ([newStep, newMapType]) => {
    if (newStep === 2 && newMapType === "custom") {
      await nextTick();
      initializeMap();
    }
  }
);

// Update circle position when map moves
const updateCircle = () => {
  if (!map.value || !circle.value) return;

  const center = map.value.getCenter();

  // Update circle position (radius stays constant in km)
  circle.value.setLatLng(center);

  // Update the stored values
  newSimDetails.value.customMap.longitude = center.lng;
  newSimDetails.value.customMap.latitude = center.lat;
  newSimDetails.value.customMap.radius = radiusKm.value;
};

// Update circle radius when slider changes
const updateCircleRadius = () => {
  if (!circle.value) return;
  circle.value.setRadius(radiusKm.value * 1000); // Convert km to meters
  updateCircle();
};

const initializeMap = () => {
  // Destroy existing map if any
  if (map.value) {
    map.value.remove();
    map.value = null;
    circle.value = null;
  }

  const mapContainer = document.getElementById("simulation-map");
  if (!mapContainer) return;

  // Initialize map centered on Hills Road Sixth Form College, Cambridge
  const defaultLat = newSimDetails.value.customMap.latitude || 52.1882;
  const defaultLng = newSimDetails.value.customMap.longitude || 0.1356;
  const defaultZoom = 13;

  map.value = L.map("simulation-map", {
    attributionControl: false,
  }).setView([defaultLat, defaultLng], defaultZoom);

  // Add OpenStreetMap tiles
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
  }).addTo(map.value);

  // Add the circle with fixed kilometer radius
  circle.value = L.circle([defaultLat, defaultLng], {
    radius: radiusKm.value * 1000, // Convert km to meters for Leaflet
    color: "#3b82f6",
    fillColor: "#3b82f6",
    fillOpacity: 0.2,
    weight: 2,
  }).addTo(map.value);

  // Update circle position on map move (radius stays constant)
  map.value.on("move", updateCircle);
  map.value.on("zoom", updateCircle);

  // Add search control
  const provider = new OpenStreetMapProvider();
  const searchControl = new (GeoSearchControl as any)({
    provider: provider,
    style: "bar",
    autoComplete: true,
    autoCompleteDelay: 250,
    showMarker: false,
    retainZoomLevel: false,
  });

  map.value.addControl(searchControl);

  // Handle search result
  map.value.on("geosearch/showlocation", (result: any) => {
    const { x, y } = result.location;
    map.value?.setView([y, x], 13);
  });

  // Initialize the stored values
  updateCircle();
};

const handleNextStep = async () => {
  loading.value = true;
  loadingMessage.value = ""; // Reset message

  try {
    if (step.value === 1) {
      if (newSimDetails.value.name == "") {
        toast.show("Please enter a simulation name.", "error");
        return;
      }
      loadingMessage.value = "Creating simulation...";
      const response = await axios.post("/Simulation", {
        SimulationName: newSimDetails.value.name,
        // icon: newSimDetails.value.icon,
      });
      newSimDetails.value.id = response.data.simulationId;
      console.error("Created simulation with ID:", newSimDetails.value.id);
    }

    if (step.value === 2) {
      if (newSimDetails.value.mapType === "preset") {
        if (newSimDetails.value.mapPresetId === 0) {
          toast.show("Please select a map preset.", "error");
          return;
        }
        loadingMessage.value = "Loading map area preset...";
        const response = await axios.post(
          `/Simulation/${newSimDetails.value.id}/map-area`,
          {
            presetId: newSimDetails.value.mapPresetId,
          }
        );

        // Capture population capacity limits from the response
        if (response.data.populationEstimate) {
          const estimate = response.data.populationEstimate;
          newSimDetails.value.maxPopulation = {
            MaxTotalPopulation: estimate.maxTotalPopulation,
            MaxStudents: estimate.maxStudents,
            MaxOfficeWorkers: estimate.maxOfficeWorkers,
            MaxMedicalWorkers: estimate.maxMedicalWorkers,
            MaxRetailWorkers: estimate.maxRetailWorkers,
            MaxServiceWorkers: estimate.maxServiceWorkers,
            MaxElderlyRetired: estimate.maxElderlyRetired,
          };

          // Set initial population values
          let population = {
            Student: estimate.maxStudents,
            OfficeWorker: estimate.maxOfficeWorkers,
            MedicalWorker: estimate.maxMedicalWorkers,
            ElderlyRetiredAgent: estimate.maxElderlyRetired,
            RetailWorker: estimate.maxRetailWorkers,
            ServiceWorker: estimate.maxServiceWorkers,
          };

          // Check if sum exceeds max total population
          const totalPopulation = Object.values(population).reduce(
            (a, b) => a + b,
            0
          );

          if (totalPopulation > estimate.maxTotalPopulation) {
            // Scale down proportionally
            const scaleFactor = estimate.maxTotalPopulation / totalPopulation;
            population = {
              Student: Math.floor(estimate.maxStudents * scaleFactor),
              OfficeWorker: Math.floor(estimate.maxOfficeWorkers * scaleFactor),
              MedicalWorker: Math.floor(
                estimate.maxMedicalWorkers * scaleFactor
              ),
              ElderlyRetiredAgent: Math.floor(
                estimate.maxElderlyRetired * scaleFactor
              ),
              RetailWorker: Math.floor(estimate.maxRetailWorkers * scaleFactor),
              ServiceWorker: Math.floor(
                estimate.maxServiceWorkers * scaleFactor
              ),
            };
          }

          newSimDetails.value.population = population;
        }
      } else {
        // Custom map area - this may take a while for large areas
        if (
          !newSimDetails.value.customMap.PresetName ||
          newSimDetails.value.customMap.PresetName.trim() === ""
        ) {
          toast.show("Please enter a custom map name.", "error");
          return;
        }
        loadingMessage.value =
          "Downloading map data from OpenStreetMap... This may take several minutes for large areas.";

        const response = await axios.post(
          `/Simulation/${newSimDetails.value.id}/map-area`,
          {
            presetName: newSimDetails.value.customMap.PresetName,
            longitude: newSimDetails.value.customMap.longitude,
            latitude: newSimDetails.value.customMap.latitude,
            radiusKm: newSimDetails.value.customMap.radius,
            SavePreset: newSimDetails.value.customMap.savePreset,
          },
          {
            timeout: 600000, // 10 minutes timeout for large map downloads
          }
        );

        // Capture population capacity limits from the response
        if (response.data.populationEstimate) {
          const estimate = response.data.populationEstimate;
          newSimDetails.value.maxPopulation = {
            MaxTotalPopulation: estimate.maxTotalPopulation,
            MaxStudents: estimate.maxStudents,
            MaxOfficeWorkers: estimate.maxOfficeWorkers,
            MaxMedicalWorkers: estimate.maxMedicalWorkers,
            MaxRetailWorkers: estimate.maxRetailWorkers,
            MaxServiceWorkers: estimate.maxServiceWorkers,
            MaxElderlyRetired: estimate.maxElderlyRetired,
          };

          // Set initial population values
          let population = {
            Student: estimate.maxStudents,
            OfficeWorker: estimate.maxOfficeWorkers,
            MedicalWorker: estimate.maxMedicalWorkers,
            ElderlyRetiredAgent: estimate.maxElderlyRetired,
            RetailWorker: estimate.maxRetailWorkers,
            ServiceWorker: estimate.maxServiceWorkers,
          };

          // Check if sum exceeds max total population
          const totalPopulation = Object.values(population).reduce(
            (a, b) => a + b,
            0
          );

          if (totalPopulation > estimate.maxTotalPopulation) {
            // Scale down proportionally
            const scaleFactor = estimate.maxTotalPopulation / totalPopulation;
            population = {
              Student: Math.floor(estimate.maxStudents * scaleFactor),
              OfficeWorker: Math.floor(estimate.maxOfficeWorkers * scaleFactor),
              MedicalWorker: Math.floor(
                estimate.maxMedicalWorkers * scaleFactor
              ),
              ElderlyRetiredAgent: Math.floor(
                estimate.maxElderlyRetired * scaleFactor
              ),
              RetailWorker: Math.floor(estimate.maxRetailWorkers * scaleFactor),
              ServiceWorker: Math.floor(
                estimate.maxServiceWorkers * scaleFactor
              ),
            };
          }

          newSimDetails.value.population = population;
        }
      }
      loadingMessage.value = ""; // Clear loading message after step 2
    }

    if (step.value === 3) {
      // Validate individual population limits
      const errors = [];

      if (
        newSimDetails.value.population.Student >
        newSimDetails.value.maxPopulation.MaxStudents
      ) {
        errors.push(
          `Students cannot exceed ${newSimDetails.value.maxPopulation.MaxStudents}`
        );
      }
      if (
        newSimDetails.value.population.OfficeWorker >
        newSimDetails.value.maxPopulation.MaxOfficeWorkers
      ) {
        errors.push(
          `Office Workers cannot exceed ${newSimDetails.value.maxPopulation.MaxOfficeWorkers}`
        );
      }
      if (
        newSimDetails.value.population.MedicalWorker >
        newSimDetails.value.maxPopulation.MaxMedicalWorkers
      ) {
        errors.push(
          `Medical Workers cannot exceed ${newSimDetails.value.maxPopulation.MaxMedicalWorkers}`
        );
      }
      if (
        newSimDetails.value.population.RetailWorker >
        newSimDetails.value.maxPopulation.MaxRetailWorkers
      ) {
        errors.push(
          `Retail Workers cannot exceed ${newSimDetails.value.maxPopulation.MaxRetailWorkers}`
        );
      }
      if (
        newSimDetails.value.population.ServiceWorker >
        newSimDetails.value.maxPopulation.MaxServiceWorkers
      ) {
        errors.push(
          `Service Workers cannot exceed ${newSimDetails.value.maxPopulation.MaxServiceWorkers}`
        );
      }
      if (
        newSimDetails.value.population.ElderlyRetiredAgent >
        newSimDetails.value.maxPopulation.MaxElderlyRetired
      ) {
        errors.push(
          `Elderly/Retired cannot exceed ${newSimDetails.value.maxPopulation.MaxElderlyRetired}`
        );
      }

      if (errors.length > 0) {
        toast.show(errors.join(". "), "error");
        return;
      }

      // Validate total population
      const totalPopulation = Object.values(
        newSimDetails.value.population
      ).reduce((a, b) => a + b, 0);

      if (
        totalPopulation > newSimDetails.value.maxPopulation.MaxTotalPopulation
      ) {
        const excess =
          totalPopulation -
          newSimDetails.value.maxPopulation.MaxTotalPopulation;
        toast.show(
          `Total population cannot exceed ${newSimDetails.value.maxPopulation.MaxTotalPopulation}. Currently exceeds by ${excess}`,
          "error"
        );
        return;
      }

      // Send population configuration to API
      loadingMessage.value = "Generating population...";
      const response = await axios.post(
        `/Simulation/${newSimDetails.value.id}/population`,
        {
          studentCount: newSimDetails.value.population.Student,
          officeWorker: newSimDetails.value.population.OfficeWorker,
          medicalWorker: newSimDetails.value.population.MedicalWorker,
          elderlyRetiredAgent:
            newSimDetails.value.population.ElderlyRetiredAgent,
          retailWorker: newSimDetails.value.population.RetailWorker,
          serviceWorker: newSimDetails.value.population.ServiceWorker,
        }
      );
      loadingMessage.value = "";
    }

    if (step.value === 4) {
      loadingMessage.value = "Configuring disease parameters...";
      if (newSimDetails.value.diseaseType === "preset") {
        if (newSimDetails.value.diseasePresetID === 0) {
          toast.show("Please select a disease preset.", "error");
          return;
        }
        const response = await axios.post(
          `/Simulation/${newSimDetails.value.id}/disease`,
          {
            presetId: newSimDetails.value.diseasePresetID,
          }
        );
      } else {
        const response = await axios.post(
          `/Simulation/${newSimDetails.value.id}/disease`,
          {
            presetName: newSimDetails.value.customDisease.PresetName,
            infectionRate: newSimDetails.value.customDisease.infectionRate,
            recoveryRate: newSimDetails.value.customDisease.recoveryRate,
            mortalityRate: newSimDetails.value.customDisease.mortalityRate,
            SavePreset: newSimDetails.value.customDisease.savePreset,
          }
        );
      }
      loadingMessage.value = "";
    }

    if (step.value < 5) step.value++;

    if (step.value == 5) {
      show.value = false;
      emit("created");
    }
  } catch (err) {
    console.error(`Failed on step ${step.value}:`, err);
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div>
    <input
      type="checkbox"
      id="create-sim-modal"
      class="modal-toggle"
      v-model="show"
    />

    <div class="modal">
      <div class="modal-box max-w-2xl">
        <h3 class="font-bold text-xl mb-4">
          Create Simulation — Step {{ step }}
        </h3>

        <!-- Step Indicators -->
        <div class="flex justify-between mb-6">
          <div
            v-for="n in 4"
            :key="n"
            class="flex-1 h-2 mx-1 rounded-full"
            :class="{
              'bg-secondary': step >= n + 1,
              'bg-primary': step === n,
              'bg-base-300': step < n + 1,
            }"
          ></div>
        </div>

        <!-- Step 1: Name + Icon -->
        <div v-if="step === 1" class="space-y-4">
          <div class="form-control">
            <label class="label"
              ><span class="label-text">Simulation Name:</span></label
            >
            <input
              v-model="newSimDetails.name"
              class="input input-bordered w-full"
              placeholder="e.g. Sick City"
            />
          </div>
          <div class="form-control">
            <label class="label"
              ><span class="label-text">Select Icon:</span></label
            >
            <div class="flex space-x-2">
              <button
                v-for="icon in ['🧪', '🦠', '🏙️', '🌍', '🧬', '⚙️']"
                :key="icon"
                class="btn"
                :class="{ 'btn-primary': newSimDetails.icon === icon }"
                @click="newSimDetails.icon = icon"
              >
                {{ icon }}
              </button>
            </div>
          </div>
        </div>

        <!-- Step 2: Map Selection -->
        <div v-if="step === 2" class="space-y-4">
          <div class="tabs mb-4">
            <a
              class="tab tab-bordered"
              :class="{ 'tab-active': newSimDetails.mapType === 'preset' }"
              @click="newSimDetails.mapType = 'preset'"
            >
              Preset
            </a>
            <a
              class="tab tab-bordered"
              :class="{ 'tab-active': newSimDetails.mapType === 'custom' }"
              @click="newSimDetails.mapType = 'custom'"
            >
              Custom
            </a>
          </div>

          <div v-if="newSimDetails.mapType === 'preset'">
            <label class="label"
              ><span class="label-text">Select Map Preset:</span></label
            >
            <select
              v-model="newSimDetails.mapPresetId"
              class="select select-bordered w-full"
            >
              <option disabled value="0">Choose a preset...</option>
              <option
                v-for="preset in mapAreaPresets"
                :key="preset.id"
                :value="preset.id"
              >
                {{ preset.name }}
              </option>
            </select>
          </div>

          <div v-else>
            <!-- Map Configuration -->
            <div class="mb-6">
              <label class="label mb-4">
                <span class="label-text font-semibold text-base"
                  >Map Configuration</span
                >
              </label>

              <!-- Map Name -->
              <div class="mb-6">
                <label class="label mb-2">
                  <span class="label-text">Custom Map Name</span>
                </label>
                <input
                  v-model="newSimDetails.customMap.PresetName"
                  class="input input-bordered w-full"
                  placeholder="e.g., Hills Road Area"
                />
              </div>

              <!-- Save as Preset -->
              <div class="mb-6">
                <label class="label cursor-pointer justify-start gap-3">
                  <input
                    type="checkbox"
                    v-model="newSimDetails.customMap.savePreset"
                    class="checkbox checkbox-primary"
                  />
                  <span class="label-text">Save as preset for future use</span>
                </label>
              </div>

              <!-- Radius Slider -->
              <div class="mb-4">
                <label class="label flex justify-between items-center mb-2">
                  <span class="label-text">Simulation Radius</span>
                  <span class="label-text-alt text-primary font-bold">
                    {{ radiusKm.toFixed(2) }} km
                  </span>
                </label>
                <div class="flex items-center gap-4">
                  <input
                    type="range"
                    min="0.1"
                    max="15"
                    step="0.1"
                    v-model.number="radiusKm"
                    @input="updateCircleRadius"
                    class="range range-primary range-xs flex-1"
                  />
                </div>
                <div class="flex justify-between px-2.5 mt-2 text-xs">
                  <span>|</span>
                  <span>|</span>
                </div>
                <div class="flex justify-between px-2.5 mt-2 text-xs">
                  <span>0 km</span>
                  <span>15 km</span>
                </div>
              </div>
            </div>

            <!-- Map Selection -->
            <div class="mb-4">
              <label class="label">
                <span class="label-text font-semibold text-base"
                  >Select Map Area</span
                >
                <span class="label-text-alt text-xs opacity-70">
                  Pan and zoom to position
                </span>
              </label>

              <div
                id="simulation-map"
                class="w-full h-96 rounded-lg border-2 border-base-300 shadow-md mb-3"
              ></div>

              <!-- Map Center Info -->
              <div class="bg-base-200 rounded-lg p-3">
                <div class="flex items-center justify-between">
                  <div>
                    <h4 class="text-xs font-semibold opacity-70 mb-1">
                      Map Center
                    </h4>
                    <p class="text-sm font-mono">
                      {{
                        newSimDetails.customMap.latitude?.toFixed(4) ?? "N/A"
                      }},
                      {{
                        newSimDetails.customMap.longitude?.toFixed(4) ?? "N/A"
                      }}
                    </p>
                  </div>
                  <div class="text-right">
                    <h4 class="text-xs font-semibold opacity-70 mb-1">
                      Coverage Area
                    </h4>
                    <p class="text-sm font-bold text-primary">
                      ~{{ (Math.PI * radiusKm * radiusKm).toFixed(2) }} km²
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Step 3: Population Setup -->
        <div v-if="step === 3" class="space-y-4">
          <!-- Total Population Summary -->
          <div class="alert alert-info mb-4">
            <div>
              <span class="font-bold">Maximum Total Population:</span>
              {{ newSimDetails.maxPopulation.MaxTotalPopulation }}
            </div>
            <div>
              <span class="font-bold">Current Total:</span>
              {{
                Object.values(newSimDetails.population).reduce(
                  (a, b) => a + b,
                  0
                )
              }}
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <!-- Students -->
            <div class="flex flex-col items-start">
              <label class="mb-1 font-medium">Student</label>
              <input
                type="number"
                min="0"
                :max="newSimDetails.maxPopulation.MaxStudents"
                v-model.number="newSimDetails.population.Student"
                class="input input-bordered w-full"
                :class="{
                  'input-error':
                    newSimDetails.population.Student >
                    newSimDetails.maxPopulation.MaxStudents,
                }"
                placeholder="Enter number"
              />
              <span class="text-xs text-gray-500 mt-1">
                Max: {{ newSimDetails.maxPopulation.MaxStudents }}
              </span>
            </div>

            <!-- Office Workers -->
            <div class="flex flex-col items-start">
              <label class="mb-1 font-medium">Office Worker</label>
              <input
                type="number"
                min="0"
                :max="newSimDetails.maxPopulation.MaxOfficeWorkers"
                v-model.number="newSimDetails.population.OfficeWorker"
                class="input input-bordered w-full"
                :class="{
                  'input-error':
                    newSimDetails.population.OfficeWorker >
                    newSimDetails.maxPopulation.MaxOfficeWorkers,
                }"
                placeholder="Enter number"
              />
              <span class="text-xs text-gray-500 mt-1">
                Max: {{ newSimDetails.maxPopulation.MaxOfficeWorkers }}
              </span>
            </div>

            <!-- Medical Workers -->
            <div class="flex flex-col items-start">
              <label class="mb-1 font-medium">Medical Worker</label>
              <input
                type="number"
                min="0"
                :max="newSimDetails.maxPopulation.MaxMedicalWorkers"
                v-model.number="newSimDetails.population.MedicalWorker"
                class="input input-bordered w-full"
                :class="{
                  'input-error':
                    newSimDetails.population.MedicalWorker >
                    newSimDetails.maxPopulation.MaxMedicalWorkers,
                }"
                placeholder="Enter number"
              />
              <span class="text-xs text-gray-500 mt-1">
                Max: {{ newSimDetails.maxPopulation.MaxMedicalWorkers }}
              </span>
            </div>

            <!-- Retail Workers -->
            <div class="flex flex-col items-start">
              <label class="mb-1 font-medium">Retail Worker</label>
              <input
                type="number"
                min="0"
                :max="newSimDetails.maxPopulation.MaxRetailWorkers"
                v-model.number="newSimDetails.population.RetailWorker"
                class="input input-bordered w-full"
                :class="{
                  'input-error':
                    newSimDetails.population.RetailWorker >
                    newSimDetails.maxPopulation.MaxRetailWorkers,
                }"
                placeholder="Enter number"
              />
              <span class="text-xs text-gray-500 mt-1">
                Max: {{ newSimDetails.maxPopulation.MaxRetailWorkers }}
              </span>
            </div>

            <!-- Service Workers -->
            <div class="flex flex-col items-start">
              <label class="mb-1 font-medium">Service Worker</label>
              <input
                type="number"
                min="0"
                :max="newSimDetails.maxPopulation.MaxServiceWorkers"
                v-model.number="newSimDetails.population.ServiceWorker"
                class="input input-bordered w-full"
                :class="{
                  'input-error':
                    newSimDetails.population.ServiceWorker >
                    newSimDetails.maxPopulation.MaxServiceWorkers,
                }"
                placeholder="Enter number"
              />
              <span class="text-xs text-gray-500 mt-1">
                Max: {{ newSimDetails.maxPopulation.MaxServiceWorkers }}
              </span>
            </div>

            <!-- Elderly/Retired -->
            <div class="flex flex-col items-start">
              <label class="mb-1 font-medium">Elderly Retired Agent</label>
              <input
                type="number"
                min="0"
                :max="newSimDetails.maxPopulation.MaxElderlyRetired"
                v-model.number="newSimDetails.population.ElderlyRetiredAgent"
                class="input input-bordered w-full"
                :class="{
                  'input-error':
                    newSimDetails.population.ElderlyRetiredAgent >
                    newSimDetails.maxPopulation.MaxElderlyRetired,
                }"
                placeholder="Enter number"
              />
              <span class="text-xs text-gray-500 mt-1">
                Max: {{ newSimDetails.maxPopulation.MaxElderlyRetired }}
              </span>
            </div>
          </div>
        </div>

        <!-- Step 4: Disease Setup -->
        <div v-if="step === 4" class="space-y-4">
          <div class="tabs mb-4">
            <a
              class="tab tab-bordered"
              :class="{ 'tab-active': newSimDetails.diseaseType === 'preset' }"
              @click="newSimDetails.diseaseType = 'preset'"
            >
              Preset
            </a>
            <a
              class="tab tab-bordered"
              :class="{ 'tab-active': newSimDetails.diseaseType === 'custom' }"
              @click="newSimDetails.diseaseType = 'custom'"
            >
              Custom
            </a>
          </div>

          <div v-if="newSimDetails.diseaseType === 'preset'">
            <label class="label"
              ><span class="label-text">Select Disease Preset:</span></label
            >
            <select
              v-model="newSimDetails.diseasePresetID"
              class="select select-bordered w-full"
            >
              <option disabled value="0">Choose a preset...</option>
              <option
                v-for="preset in diseasePresets"
                :key="preset.id"
                :value="preset.id"
              >
                {{ preset.name }}
              </option>
            </select>
          </div>

          <div v-else>
            <label class="label"
              ><span class="label-text">Custom Disease Name:</span></label
            >
            <input
              v-model="newSimDetails.customDisease.PresetName"
              class="input input-bordered w-full mb-2"
              placeholder="Enter custom disease name"
            />
            <div class="flex items-center mb-2">
              <input
                type="checkbox"
                v-model="newSimDetails.customDisease.savePreset"
                class="checkbox mr-2"
                id="save-disease-preset"
              />
              <label
                for="save-disease-preset"
                class="cursor-pointer select-none"
              >
                Save as preset
              </label>
            </div>
            <div class="grid grid-cols-3 gap-2">
              <div>
                <label class="label"
                  ><span class="label-text">Infection Rate:</span></label
                >
                <input
                  type="decimal"
                  v-model.number="newSimDetails.customDisease.infectionRate"
                  class="input input-bordered w-full"
                />
              </div>
              <div>
                <label class="label"
                  ><span class="label-text">Recovery Rate:</span></label
                >
                <input
                  type="decimal"
                  v-model.number="newSimDetails.customDisease.recoveryRate"
                  class="input input-bordered w-full"
                />
              </div>
              <div>
                <label class="label"
                  ><span class="label-text">Mortality Rate:</span></label
                >
                <input
                  type="decimal"
                  v-model.number="newSimDetails.customDisease.mortalityRate"
                  class="input input-bordered w-full"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Navigation Buttons -->
        <div class="modal-action flex justify-between mt-6">
          <div></div>
          <div>
            <!-- Loading State -->
            <div v-if="loading" class="flex items-center gap-3">
              <span class="loading loading-spinner loading-md"></span>
              <span v-if="loadingMessage" class="text-sm">{{
                loadingMessage
              }}</span>
            </div>
            <!-- Buttons when not loading -->
            <div v-else>
              <button
                v-if="step < 4"
                class="btn btn-primary"
                @click="handleNextStep"
              >
                Next
              </button>
              <button v-else class="btn btn-success" @click="handleNextStep">
                Finish
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
#simulation-map {
  z-index: 0;
}

:deep(.geosearch) {
  z-index: 1000;
}

:deep(.leaflet-control-geosearch) {
  z-index: 1000;
}
</style>
