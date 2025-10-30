<template>
  <div class="toast toast-end">
    <transition-group name="toast" tag="div">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="alert shadow-lg w-72"
        :class="{
          'alert-success': toast.type === 'success',
          'alert-error': toast.type === 'error',
          'alert-info': toast.type === 'info',
          'alert-warning': toast.type === 'warning',
        }"
      >
        <div>
          <span>{{ toast.message }}</span>
        </div>
        <button class="btn btn-sm btn-ghost ml-auto" @click="remove(toast.id)">
          ✕
        </button>
      </div>
    </transition-group>
  </div>
</template>

<script setup>
import { useToastStore } from "@/stores/useToastStore";
import { computed } from "vue";
const toastStore = useToastStore();
const toasts = computed(() => toastStore.toasts);
const remove = toastStore.remove;
</script>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}
.toast-enter-from {
  opacity: 0;
  transform: translateX(50%);
}
.toast-leave-to {
  opacity: 0;
  transform: translateX(50%);
}
</style>
