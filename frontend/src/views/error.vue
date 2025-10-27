<template>
  <div class="min-h-screen bg-base-200 flex items-center justify-center p-4">
    <div class="card w-full max-w-md bg-base-100 shadow-xl">
      <div class="card-body items-center text-center">
        <!-- Error Icon -->
        <div class="text-error mb-4">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-24 w-24"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            />
          </svg>
        </div>

        <!-- Title -->
        <h2 class="card-title text-3xl font-bold mb-2">Oops!</h2>

        <!-- Error Message -->
        <p class="text-base-content/70 mb-6">
          {{ message || "Something went wrong. Please try again later." }}
        </p>

        <!-- Action Buttons -->
        <div class="card-actions">
          <button @click="goBack" class="btn btn-primary">Go Back</button>
          <router-link to="/" class="btn btn-ghost"> Home Page </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { defineProps } from "vue";
import { useRouter } from "vue-router";

defineProps({
  message: {
    type: String,
    default: "",
  },
});

const router = useRouter();

const goBack = () => {
  router.back();
};
</script>
