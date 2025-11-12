<template>
  <div class="h-full">
    <RouterView v-if="!loading" class="h-full" />
    <div v-else class="flex justify-center items-center h-screen">
      <span class="loading loading-ring loading-xl"></span>
    </div>
    <AppToast />
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useUserStore } from "@/stores/useUserStore";
import { useMidiController } from "@/composables/useMidiController";
import { useMidiNavigation } from "@/composables/useMidiNavigation";
import AppToast from "@/components/AppToast.vue";
const userStore = useUserStore();
const loading = ref(true);

// MIDI Controller
const midi = useMidiController();
const midiNav = useMidiNavigation();

async function initializeMidi() {
  try {
    await midi.connect();
  } catch (err) {
    // Silently fail if no MIDI device - app still works without it
  }
}

// ============================================================================

onMounted(async () => {
  try {
    // if user has tokens, fetch user data (profile name etc)
    if (userStore.refreshToken || userStore.accessToken) {
      await userStore.FetchUser();
    }
    initializeMidi();
  } catch (err) {
    console.error("Failed to fetch user:", err);
  } finally {
    loading.value = false;
  }
});
</script>
