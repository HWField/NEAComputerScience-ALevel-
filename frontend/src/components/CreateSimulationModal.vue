<script setup lang="ts">
import { ref, watch, onMounted, nextTick, onUnmounted } from "vue";
import axios from "../api/axios";
import { useToastStore } from "@/stores/useToastStore";
import L from "leaflet";
import { GeoSearchControl, OpenStreetMapProvider } from "leaflet-geosearch";
import * as signalR from "@microsoft/signalr";
import { useUserStore } from "@/stores/useUserStore";

const toast = useToastStore();
const userStore = useUserStore();
const emit = defineEmits(["created"]);

const show = defineModel<boolean>({ required: true });
const step = ref(1);
const loading = ref(false);
const loadingMessage = ref("");

// Progress tracking
const progressStep = ref("");
const progressMessage = ref("");
const progressPercentage = ref(0);
const progressCurrent = ref(0);
const progressTotal = ref(0);

// SignalR connection
let hubConnection: signalR.HubConnection | null = null;

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

// Initialize SignalR connection for progress updates
const initializeSignalR = async (simulationId: number) => {
  if (hubConnection) {
    await hubConnection.stop();
  }

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("http://127.0.0.1:5081/hubs/simulation", {
      accessTokenFactory: () => userStore.token || "",
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Listen for setup progress
  hubConnection.on(
    "SetupProgress",
    (data: {
      step: string;
      message: string;
      percentage: number;
      current: number;
      total: number;
    }) => {
      progressStep.value = data.step;
      progressMessage.value = data.message;
      progressPercentage.value = data.percentage;
      progressCurrent.value = data.current;
      progressTotal.value = data.total;
      loadingMessage.value = `${data.step}: ${data.message}`;
    }
  );

  try {
    await hubConnection.start();
    console.log("SignalR connected for simulation setup");

    // Join the simulation group to receive progress updates
    await hubConnection.invoke("JoinSimulation", simulationId);
  } catch (err) {
    console.error("Failed to connect SignalR:", err);
  }
};

// Cleanup SignalR connection
const cleanupSignalR = async () => {
  if (hubConnection) {
    try {
      await hubConnection.stop();
    } catch (err) {
      console.error("Failed to stop SignalR:", err);
    }
    hubConnection = null;
  }
};

onUnmounted(() => {
  cleanupSignalR();
});

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
      infectionRate: 0.3,
      recoveryRate: 0.1,
      mortalityRate: 0.01,
      incubationPeriod: 3.0,
      infectiousPeriod: 10.0,
      symptomaticRate: 0.7,
      severeSymptomsRate: 0.15,
      hospitalSeekingRate: 0.5,
      immunityDuration: 180,
      reinfectionResistance: 0.7,
      maskEffectiveness: 0.5,
      vaccineEffectiveness: 0.8,
      savePreset: false,
    },
    // step 5 initial infection
    initialInfectionCount: 1,
  };
  step.value = 1;
  show.value = true;
};

watch(show, (val) => {
  if (val) {
    createNewSimulation();
  } else {
    // Clean up SignalR when modal closes
    cleanupSignalR();
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

      // Initialize SignalR connection before starting population generation
      await initializeSignalR(newSimDetails.value.id);

      // Reset progress tracking
      progressStep.value = "";
      progressMessage.value = "";
      progressPercentage.value = 0;
      progressCurrent.value = 0;
      progressTotal.value = 0;

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

      // Cleanup SignalR connection after population setup completes
      await cleanupSignalR();

      // Reset progress tracking so it doesn't show on other steps
      progressStep.value = "";
      progressMessage.value = "";
      progressPercentage.value = 0;
      progressCurrent.value = 0;
      progressTotal.value = 0;

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
            incubationPeriod:
              newSimDetails.value.customDisease.incubationPeriod,
            infectiousPeriod:
              newSimDetails.value.customDisease.infectiousPeriod,
            symptomaticRate: newSimDetails.value.customDisease.symptomaticRate,
            severeSymptomsRate:
              newSimDetails.value.customDisease.severeSymptomsRate,
            hospitalSeekingRate:
              newSimDetails.value.customDisease.hospitalSeekingRate,
            immunityDuration:
              newSimDetails.value.customDisease.immunityDuration,
            reinfectionResistance:
              newSimDetails.value.customDisease.reinfectionResistance,
            maskEffectiveness:
              newSimDetails.value.customDisease.maskEffectiveness,
            vaccineEffectiveness:
              newSimDetails.value.customDisease.vaccineEffectiveness,
            SavePreset: newSimDetails.value.customDisease.savePreset,
          }
        );
      }
      loadingMessage.value = "";
    }

    if (step.value === 5) {
      // Validate infection count
      const totalAgents = Object.values(newSimDetails.value.population).reduce(
        (a, b) => a + b,
        0
      );

      if (newSimDetails.value.initialInfectionCount < 1) {
        toast.show(
          "Must infect at least 1 agent to start the outbreak.",
          "error"
        );
        return;
      }

      if (newSimDetails.value.initialInfectionCount > totalAgents) {
        toast.show(
          `Cannot infect more agents (${newSimDetails.value.initialInfectionCount}) than total population (${totalAgents})`,
          "error"
        );
        return;
      }

      if (newSimDetails.value.initialInfectionCount > 100) {
        toast.show(
          "Maximum initial infection count is 100 agents for performance reasons.",
          "error"
        );
        return;
      }

      // Load simulation and seed infection
      loadingMessage.value = "Loading simulation and seeding infection...";
      try {
        // Load simulation into memory
        await axios.post(`/Simulation/${newSimDetails.value.id}/load`);

        // Seed initial infection
        await axios.post(
          `/Simulation/${newSimDetails.value.id}/infect`,
          newSimDetails.value.initialInfectionCount
        );

        toast.show(
          `Simulation created successfully with ${newSimDetails.value.initialInfectionCount} initially infected agent(s)!`,
          "success"
        );
      } catch (err) {
        console.error("Failed to seed infection:", err);
        toast.show(
          "Failed to initialize infection. You can manually infect agents later.",
          "warning"
        );
      }
      loadingMessage.value = "";
    }

    if (step.value < 6) step.value++;

    if (step.value == 6) {
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
            v-for="n in 5"
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

          <div v-else class="space-y-4">
            <!-- Disease Name -->
            <div class="form-control">
              <label class="label">
                <span class="label-text font-semibold">Disease Name</span>
              </label>
              <input
                v-model="newSimDetails.customDisease.PresetName"
                class="input input-bordered w-full"
                placeholder="e.g., Custom Flu"
              />
            </div>

            <!-- Save as Preset -->
            <div class="form-control">
              <label class="label cursor-pointer justify-start gap-3">
                <input
                  type="checkbox"
                  v-model="newSimDetails.customDisease.savePreset"
                  class="checkbox checkbox-primary"
                />
                <span class="label-text">Save as preset for future use</span>
              </label>
            </div>

            <!-- Transmission & Outcomes -->
            <div class="divider">Transmission & Outcomes</div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <!-- Infection Rate -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Infection Rate</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Base probability of transmission per contact (0.0 - 1.0). Higher = more contagious"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="newSimDetails.customDisease.infectionRate"
                  class="input input-bordered w-full"
                  placeholder="0.3"
                />
              </div>

              <!-- Recovery Rate -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Recovery Rate</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Daily probability of recovery (0.0 - 1.0). Higher = faster recovery"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="newSimDetails.customDisease.recoveryRate"
                  class="input input-bordered w-full"
                  placeholder="0.1"
                />
              </div>

              <!-- Mortality Rate -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Mortality Rate</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Probability of death for infected individuals (0.0 - 1.0). Higher = more deadly"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="newSimDetails.customDisease.mortalityRate"
                  class="input input-bordered w-full"
                  placeholder="0.01"
                />
              </div>
            </div>

            <!-- Disease Timeline -->
            <div class="divider">Disease Timeline</div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <!-- Incubation Period -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Incubation Period (days)</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Days before infected agents become contagious. During this time they're infected but can't spread the disease"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.5"
                  min="0"
                  v-model.number="newSimDetails.customDisease.incubationPeriod"
                  class="input input-bordered w-full"
                  placeholder="3.0"
                />
              </div>

              <!-- Infectious Period -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Infectious Period (days)</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Days of peak infectiousness. After this period, agents are less likely to spread the disease"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.5"
                  min="0"
                  v-model.number="newSimDetails.customDisease.infectiousPeriod"
                  class="input input-bordered w-full"
                  placeholder="10.0"
                />
              </div>
            </div>

            <!-- Symptoms & Healthcare -->
            <div class="divider">Symptoms & Healthcare</div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <!-- Symptomatic Rate -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Symptomatic Rate</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Percentage of infected who develop noticeable symptoms (0.0 - 1.0). Rest are asymptomatic carriers"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="newSimDetails.customDisease.symptomaticRate"
                  class="input input-bordered w-full"
                  placeholder="0.7"
                />
              </div>

              <!-- Severe Symptoms Rate -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Severe Symptoms Rate</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Percentage who develop severe symptoms requiring medical attention (0.0 - 1.0)"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="
                    newSimDetails.customDisease.severeSymptomsRate
                  "
                  class="input input-bordered w-full"
                  placeholder="0.15"
                />
              </div>

              <!-- Hospital Seeking Rate -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Hospital Seeking Rate</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Percentage of severe cases that actually seek medical help (0.0 - 1.0)"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="
                    newSimDetails.customDisease.hospitalSeekingRate
                  "
                  class="input input-bordered w-full"
                  placeholder="0.5"
                />
              </div>
            </div>

            <!-- Immunity -->
            <div class="divider">Immunity & Reinfection</div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <!-- Immunity Duration -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Immunity Duration (days)</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Days before immunity from recovery starts to decrease. Default 180 days (6 months)"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="1"
                  min="0"
                  v-model.number="newSimDetails.customDisease.immunityDuration"
                  class="input input-bordered w-full"
                  placeholder="180"
                />
              </div>

              <!-- Reinfection Resistance -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Reinfection Resistance</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Reduction in susceptibility after recovery (0.0 - 1.0). 0.7 means 70% resistance to reinfection"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="
                    newSimDetails.customDisease.reinfectionResistance
                  "
                  class="input input-bordered w-full"
                  placeholder="0.7"
                />
              </div>
            </div>

            <!-- Interventions -->
            <div class="divider">Intervention Effectiveness</div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <!-- Mask Effectiveness -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Mask Effectiveness</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Reduction in transmission when masks are worn (0.0 - 1.0). 0.5 means 50% reduction"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="newSimDetails.customDisease.maskEffectiveness"
                  class="input input-bordered w-full"
                  placeholder="0.5"
                />
              </div>

              <!-- Vaccine Effectiveness -->
              <div class="form-control">
                <label class="label">
                  <span class="label-text">Vaccine Effectiveness</span>
                  <div
                    class="tooltip tooltip-left"
                    data-tip="Reduction in infection risk when vaccinated (0.0 - 1.0). 0.8 means 80% protection"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-4 w-4 opacity-70"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  v-model.number="
                    newSimDetails.customDisease.vaccineEffectiveness
                  "
                  class="input input-bordered w-full"
                  placeholder="0.8"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Step 5: Initial Infection Setup -->
        <div v-if="step === 5" class="space-y-4">
          <div class="alert alert-warning mb-4">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="stroke-current shrink-0 h-6 w-6"
              fill="none"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
              />
            </svg>
            <span>
              <strong>Outbreak Configuration:</strong> Choose how many agents to
              initially infect to start the disease spread.
            </span>
          </div>

          <div class="form-control">
            <label class="label">
              <span class="label-text font-semibold"
                >Initial Infected Agents</span
              >
              <span class="label-text-alt">
                Total Population:
                {{
                  Object.values(newSimDetails.population).reduce(
                    (a, b) => a + b,
                    0
                  )
                }}
              </span>
            </label>

            <input
              type="number"
              min="1"
              max="100"
              v-model.number="newSimDetails.initialInfectionCount"
              class="input input-bordered w-full"
              placeholder="Number of initially infected agents"
            />

            <label class="label">
              <span class="label-text-alt text-xs opacity-70">
                Recommended: 1-5 for realistic outbreak simulation
              </span>
              <span class="label-text-alt text-xs">Maximum: 100</span>
            </label>
          </div>

          <div class="bg-base-200 rounded-lg p-4 mt-4">
            <h4 class="font-semibold mb-3 flex items-center gap-2">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                class="h-5 w-5"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              How It Works
            </h4>
            <ul class="text-sm space-y-2 opacity-80">
              <li>
                • <strong>Random Selection:</strong> Agents are randomly chosen
                from the entire population
              </li>
              <li>
                • <strong>Patient Zero(s):</strong> These agents will be marked
                as "Infected" at simulation start
              </li>
              <li>
                • <strong>Spread Dynamics:</strong> The disease will spread
                based on your configured parameters (infection rate, proximity,
                indoor/outdoor contact)
              </li>
              <li>
                • <strong>Realistic Modeling:</strong> Fewer initial infections
                create more realistic epidemic curves
              </li>
            </ul>
          </div>

          <div
            class="stats stats-vertical lg:stats-horizontal shadow w-full mt-4"
          >
            <div class="stat">
              <div class="stat-title">Initial Infected</div>
              <div class="stat-value text-error">
                {{ newSimDetails.initialInfectionCount }}
              </div>
              <div class="stat-desc">
                {{
                  (
                    (newSimDetails.initialInfectionCount /
                      Object.values(newSimDetails.population).reduce(
                        (a, b) => a + b,
                        0
                      )) *
                    100
                  ).toFixed(3)
                }}% of population
              </div>
            </div>

            <div class="stat">
              <div class="stat-title">Susceptible</div>
              <div class="stat-value text-success">
                {{
                  Object.values(newSimDetails.population).reduce(
                    (a, b) => a + b,
                    0
                  ) - newSimDetails.initialInfectionCount
                }}
              </div>
              <div class="stat-desc">Healthy agents at risk</div>
            </div>
          </div>
        </div>

        <!-- Navigation Buttons -->
        <div class="modal-action flex justify-between mt-6">
          <div></div>
          <div class="flex flex-col items-end gap-2">
            <!-- Loading State -->
            <div v-if="loading" class="flex items-center gap-3">
              <span class="loading loading-spinner loading-md"></span>

              <div v-if="progressPercentage > 0" class="min-w-[350px]">
                <div class="text-sm font-semibold mb-1">
                  {{ progressStep }}
                </div>
                <div class="text-xs opacity-70 mb-2">
                  {{ progressMessage }}
                </div>

                <!-- Progress bar -->
                <div class="w-full bg-base-300 rounded-full h-2.5">
                  <div
                    class="bg-primary h-2.5 rounded-full transition-all duration-300"
                    :style="{ width: progressPercentage + '%' }"
                  ></div>
                </div>

                <!-- Progress details -->
                <div class="flex justify-between text-xs mt-1 opacity-60">
                  <span>{{ progressPercentage }}%</span>
                  <span v-if="progressTotal > 0">
                    {{ progressCurrent.toLocaleString() }} /
                    {{ progressTotal.toLocaleString() }}
                  </span>
                </div>
              </div>
              <span v-else class="text-sm">{{ loadingMessage }}</span>
            </div>

            <!-- Buttons when not loading -->
            <div v-else>
              <button
                v-if="step < 5"
                class="btn btn-primary"
                @click="handleNextStep"
              >
                Next
              </button>
              <button v-else class="btn btn-success" @click="handleNextStep">
                Finish & Start Outbreak
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
