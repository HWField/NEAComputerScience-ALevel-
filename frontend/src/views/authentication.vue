<script setup>
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useUserStore } from "@/stores/useUserStore";
import axios from "../api/axios";
const route = useRoute();
const userStore = useUserStore();

// Check whether on /login or /register to change form behavior and text
const isLogin = computed(() => route.name === "login");

// Form state
const form = {
  username: "",
  password: "",
  confirmPassword: "",
};
function getDeviceInfo() {
  return JSON.stringify({
    userAgent: navigator.userAgent,
    platform: navigator.platform,
    vendor: navigator.vendor,
    language: navigator.language,
    languages: navigator.languages,
    cookieEnabled: navigator.cookieEnabled,
    hardwareConcurrency: navigator.hardwareConcurrency,
    deviceMemory: navigator.deviceMemory,
    screen: {
      width: window.screen.width,
      height: window.screen.height,
      availWidth: window.screen.availWidth,
      availHeight: window.screen.availHeight,
      colorDepth: window.screen.colorDepth,
      pixelDepth: window.screen.pixelDepth,
    },
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
  });
}

const handleSubmit = async () => {
  try {
    if (isLogin.value) {
      // Use axios to login
      const response = await axios.post("/Auth/login", {
        Username: form.username,
        Password: form.password,
        DeviceInfo: getDeviceInfo(),
      });
      await userStore.Login(response.data);
    } else {
      // Use axios to register
      if (form.password !== form.confirmPassword) {
        throw new Error("Passwords do not match");
      }
      const response = await axios.post("/Auth/register", {
        Username: form.username,
        Password: form.password,
        DeviceInfo: getDeviceInfo(),
      });
      await userStore.Login(response.data);
    }
  } catch (err) {
    console.error(err);
  }
};
</script>

<template>
  <div
    class="min-h-screen flex flex-col items-center justify-center bg-gradient-to-br from-base-200 via-base-300 to-base-200 relative overflow-hidden"
  >
    <!-- Main Card -->
    <div
      class="card w-full max-w-md bg-base-100 shadow-2xl border border-base-300 relative z-10"
    >
      <div class="card-body">
        <!-- Header with Icon -->
        <div class="text-center mb-6">
          <div
            class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-primary/10 mb-4"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="h-10 w-10 text-primary"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
              />
            </svg>
          </div>
          <h2 class="text-3xl font-bold mb-2">
            {{ isLogin ? "Welcome Back" : "Join Epidemic Sim" }}
          </h2>
          <p class="text-base-content/60 text-sm">
            {{
              isLogin
                ? "Enter your credentials to continue"
                : "Create an account to start simulating"
            }}
          </p>
        </div>

        <!-- Form -->
        <div class="space-y-4">
          <!-- Username Input -->
          <div class="form-control">
            <label class="label">
              <span class="label-text font-medium">Username</span>
            </label>
            <div class="relative">
              <span
                class="absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40"
              >
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
                    d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                  />
                </svg>
              </span>
              <input
                v-model="form.username"
                type="text"
                autocapitalize="off"
                autocorrect="off"
                spellcheck="false"
                placeholder="Enter your username"
                class="input input-bordered w-full pl-10 focus:input-primary"
              />
            </div>
          </div>

          <!-- Password Input -->
          <div class="form-control">
            <label class="label">
              <span class="label-text font-medium">Password</span>
            </label>
            <div class="relative">
              <span
                class="absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40"
              >
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
                    d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                  />
                </svg>
              </span>
              <input
                v-model="form.password"
                type="password"
                placeholder="Enter your password"
                class="input input-bordered w-full pl-10 focus:input-primary"
              />
            </div>
          </div>

          <!-- Confirm Password Input (Register only) -->
          <div v-if="!isLogin" class="form-control">
            <label class="label">
              <span class="label-text font-medium">Confirm Password</span>
            </label>
            <div class="relative">
              <span
                class="absolute inset-y-0 left-0 flex items-center pl-3 text-base-content/40"
              >
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
                    d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
              </span>
              <input
                v-model="form.confirmPassword"
                type="password"
                placeholder="Confirm your password"
                class="input input-bordered w-full pl-10 focus:input-primary"
              />
            </div>
          </div>

          <!-- Submit Button -->
          <button
            class="btn btn-primary w-full mt-6 gap-2"
            @click="handleSubmit"
          >
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
                d="M13 10V3L4 14h7v7l9-11h-7z"
              />
            </svg>
            {{ isLogin ? "Sign In" : "Create Account" }}
          </button>
        </div>

        <!-- Divider -->
        <div class="divider text-xs text-base-content/40">OR</div>

        <!-- Toggle Auth Mode -->
        <div class="text-center">
          <p class="text-sm text-base-content/70">
            {{
              isLogin ? "Don't have an account?" : "Already have an account?"
            }}
            <RouterLink
              :to="{ name: isLogin ? 'register' : 'login' }"
              class="link link-primary font-semibold ml-1"
            >
              {{ isLogin ? "Sign up now" : "Sign in" }}
            </RouterLink>
          </p>
        </div>
      </div>
    </div>

    <!-- Footer Text -->
    <p class="text-center text-sm text-base-content/50 mt-8 relative z-10">
      Epidemic Simulation © {{ new Date().getFullYear() }}
    </p>
  </div>
</template>
