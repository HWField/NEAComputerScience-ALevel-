<template>
  <div class="relative w-full h-full">
    <div
      ref="mapContainer"
      class="w-full h-full rounded-xl overflow-hidden"
    ></div>

    <!-- Map Legend -->
    <div
      class="absolute bottom-4 right-4 bg-base-100 rounded-lg shadow-lg p-3 text-xs z-[1000]"
    >
      <div class="font-semibold mb-2">Agent Status</div>
      <div class="space-y-1">
        <div class="flex items-center gap-2">
          <div class="w-3 h-3 rounded-full bg-success"></div>
          <span>Healthy</span>
        </div>
        <div class="flex items-center gap-2">
          <div class="w-3 h-3 rounded-full bg-error"></div>
          <span>Infected</span>
        </div>
        <div class="flex items-center gap-2">
          <div class="w-3 h-3 rounded-full bg-info"></div>
          <span>Recovered</span>
        </div>
        <div class="flex items-center gap-2">
          <div class="w-3 h-3 rounded-full bg-warning"></div>
          <span>Quarantined</span>
        </div>
      </div>
      <div
        class="mt-2 pt-2 border-t border-base-300 text-xs text-base-content/60"
      >
        Showing {{ displayedAgentCount }} of {{ props.totalAgents }} agents
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from "vue";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import type { AgentPosition } from "@/api/simulation";

interface MapArea {
  centerLat: number;
  centerLon: number;
  radiusKm: number;
  name: string;
}

interface AgentLocation {
  id: number;
  lat: number;
  lon: number;
  status: "Healthy" | "Infected" | "Recovered" | "Quarantined" | "Dead";
}

const props = defineProps<{
  mapArea: MapArea;
  agents?: AgentPosition[];
  totalAgents?: number; // Total agent count from metrics
}>();

const mapContainer = ref<HTMLElement | null>(null);
let map: L.Map | null = null;
let radiusCircle: L.Circle | null = null;
let agentMarkers: Map<number, L.CircleMarker> = new Map();

const displayedAgentCount = ref(0);

// Agent status colors
const statusColors = {
  Healthy: "#36d399",
  Infected: "#f87272",
  Recovered: "#3abff8",
  Quarantined: "#fbbd23",
  Dead: "#6b7280",
};

function initializeMap() {
  if (!mapContainer.value) return;

  // map centered on the simulation area
  map = L.map(mapContainer.value, {
    attributionControl: false,
    preferCanvas: true,
  }).setView([props.mapArea.centerLat, props.mapArea.centerLon], 13);
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
  }).addTo(map);

  // circle showing simulation area
  radiusCircle = L.circle([props.mapArea.centerLat, props.mapArea.centerLon], {
    color: "#3b82f6", // blue
    fillColor: "#3b82f6",
    fillOpacity: 0.1,
    radius: props.mapArea.radiusKm * 1000,
    weight: 2,
  }).addTo(map);
  map.fitBounds(radiusCircle.getBounds(), { padding: [50, 50] });
}

function updateAgents(agents: AgentPosition[]) {
  if (!map) return;

  displayedAgentCount.value = agents.length;

  const currentAgentIds = new Set(agents.map((a) => a.id));

  agentMarkers.forEach((marker, id) => {
    if (!currentAgentIds.has(id)) {
      marker.remove();
      agentMarkers.delete(id);
    }
  });

  agents.forEach((agent) => {
    // Map status directly from agent data
    const status = agent.isIsolating ? "Quarantined" : agent.healthState;
    const color = statusColors[status] || "#6b7280";

    let marker = agentMarkers.get(agent.id);

    if (marker) {
      marker.setLatLng([agent.lat, agent.lon]);
      marker.setStyle({
        fillColor: color,
        color: color,
      });

      marker.setPopupContent(`
        <b>Agent #${agent.id}</b><br>
        Status: ${status}<br>
        Location: ${agent.lat.toFixed(6)}, ${agent.lon.toFixed(6)}
      `);
    } else {
      marker = L.circleMarker([agent.lat, agent.lon], {
        radius: 4,
        fillColor: color,
        color: color,
        weight: 1,
        opacity: 0.8,
        fillOpacity: 0.6,
      });

      if (map) {
        marker.addTo(map);
      }

      marker.bindPopup(`
        <b>Agent #${agent.id}</b><br>
        Status: ${status}<br>
        Location: ${agent.lat.toFixed(6)}, ${agent.lon.toFixed(6)}
      `);

      agentMarkers.set(agent.id, marker);
    }
  });
}

// Watch for agent updates
watch(
  () => props.agents,
  (newAgents) => {
    if (newAgents && newAgents.length > 0) {
      updateAgents(newAgents);
    } else if (newAgents && newAgents.length === 0) {
      // Clear all markers if agents array is empty
      agentMarkers.forEach((marker) => marker.remove());
      agentMarkers.clear();
      displayedAgentCount.value = 0;
    }
  },
  { deep: true, immediate: true },
);

onMounted(() => {
  initializeMap();
  // Initial agent display
  if (props.agents && props.agents.length > 0) {
    updateAgents(props.agents);
  }
});

onUnmounted(() => {
  // Clean up markers
  agentMarkers.forEach((marker) => marker.remove());
  agentMarkers.clear();

  // Clean up map
  if (map) {
    map.remove();
    map = null;
  }
});
</script>

<style scoped>
:deep(.leaflet-container) {
  width: 100%;
  height: 100%;
}
</style>
