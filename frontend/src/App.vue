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
import AppToast from "@/components/AppToast.vue";
const userStore = useUserStore();
const loading = ref(true);

onMounted(async () => {
  try {
    // if user has tokens, fetch user data (profile name etc)
    if (userStore.refreshToken || userStore.accessToken) {
      await userStore.FetchUser();
    }
  } catch (err) {
    console.error("Failed to fetch user:", err);
  } finally {
    loading.value = false;
  }
});
</script>
