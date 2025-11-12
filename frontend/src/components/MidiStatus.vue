<!-- Shows current MIDI controller connection status in the UI -->

<template>
  <div v-if="midi.state.value.device" class="flex items-center gap-2">
    <!-- Interactive Badge with Tooltip -->
    <div
      v-if="showDetails"
      class="badge gap-2 transition-all tooltip tooltip-bottom cursor-pointer focus:outline-none"
      :class="statusClass"
      :data-tip="tooltipText"
      aria-label="MIDI device info"
    >
      <span class="text-xs font-medium">MIDI</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useMidiController } from "@/composables/useMidiController";

interface Props {
  showDetails?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  showDetails: true,
});

const midi = useMidiController();

const statusClass = computed(() => {
  const status = midi.state.value.status;

  switch (status) {
    case "connected":
      return "badge-success";
    case "connecting":
      return "badge-warning";
    case "error":
      return "badge-error";
    case "disconnected":
    default:
      return "badge-ghost";
  }
});

const tooltipText = computed(() => {
  const device = midi.state.value.device;
  const mode = midi.state.value.mode;
  if (!device) return "MIDI not connected";
  return `${device.name}\nMode: ${mode}\nStatus: ${midi.state.value.status}`;
});
</script>
