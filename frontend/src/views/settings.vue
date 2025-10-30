<template>
  <div class="container mx-auto p-6 max-w-6xl">
    <h1 class="text-3xl font-bold mb-6">Settings</h1>

    <!-- Tabs -->
    <div role="tablist" class="tabs tabs-border">
      <input
        type="radio"
        name="settings_tabs"
        role="tab"
        class="tab"
        aria-label="Account"
        checked
      />
      <div role="tabpanel" class="tab-content p-6">
        <!-- Account Tab -->
        <div class="space-y-6">
          <div class="form-control">
            <label class="label">
              <span class="label-text">Username</span>
            </label>
            <input
              type="text"
              v-model="username"
              placeholder="Enter username"
              class="input input-bordered w-full max-w-md"
            />
          </div>

          <button @click="saveAccount" class="btn btn-primary">
            Save Account Settings
          </button>
        </div>
      </div>

      <input
        type="radio"
        name="settings_tabs"
        role="tab"
        class="tab"
        aria-label="Appearance"
      />
      <div role="tabpanel" class="tab-content p-6">
        <!-- Appearance Tab -->
        <div class="space-y-6">
          <!-- Theme Selection -->
          <div class="form-control">
            <label class="label">
              <span class="label-text text-lg font-semibold"
                >Theme Selection</span
              >
            </label>

            <div
              class="flex flex-col md:flex-row items-start md:items-center gap-4 mt-2"
            >
              <!-- Theme selector dropdown -->
              <select
                v-model="selectedTheme"
                class="select select-bordered w-full md:w-64"
              >
                <option
                  v-for="theme in themes"
                  :key="theme.name"
                  :value="theme.name"
                >
                  {{ theme.label }}
                </option>
              </select>

              <!-- Theme preview -->
              <div
                class="rounded-lg p-4 flex flex-col gap-2 border w-full md:w-64"
                :data-theme="selectedTheme"
              >
                <div
                  class="bg-primary text-primary-content p-2 rounded text-center font-semibold"
                >
                  Primary
                </div>
                <div
                  class="bg-secondary text-secondary-content p-2 rounded text-center font-semibold"
                >
                  Secondary
                </div>
                <div
                  class="bg-accent text-accent-content p-2 rounded text-center font-semibold"
                >
                  Accent
                </div>
              </div>
            </div>

            <!-- Expandable grid preview -->
            <details class="mt-4">
              <summary class="cursor-pointer font-medium text-lg">
                Preview All Themes
              </summary>
              <div
                class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4 mt-2"
              >
                <div
                  v-for="theme in themes"
                  :key="theme.name"
                  @click="selectedTheme = theme.name"
                  :class="[
                    'card cursor-pointer border-2 transition-all',
                    selectedTheme === theme.name
                      ? 'border-primary'
                      : 'border-base-300',
                  ]"
                  :data-theme="theme.name"
                >
                  <div class="card-body p-2">
                    <h3 class="card-title text-xs text-center">
                      {{ theme.label }}
                    </h3>
                    <div class="flex flex-col gap-1 mt-1">
                      <div
                        class="bg-primary text-primary-content p-1 rounded text-center text-[10px]"
                      >
                        Primary
                      </div>
                      <div
                        class="bg-secondary text-secondary-content p-1 rounded text-center text-[10px]"
                      >
                        Secondary
                      </div>
                      <div
                        class="bg-accent text-accent-content p-1 rounded text-center text-[10px]"
                      >
                        Accent
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </details>
          </div>

          <!-- Streamer Mode -->
          <div class="form-control">
            <label class="label cursor-pointer max-w-md">
              <span class="label-text">Streamer Mode</span>
              <input
                type="checkbox"
                v-model="streamerMode"
                class="toggle toggle-primary"
              />
            </label>
            <label class="label">
              <span class="label-text-alt"
                >Hide sensitive information when streaming</span
              >
            </label>
          </div>

          <!-- Save Button -->
          <button @click="saveAppearance" class="btn btn-primary">
            Save Appearance Settings
          </button>
        </div>
      </div>

      <input
        type="radio"
        name="settings_tabs"
        role="tab"
        class="tab"
        aria-label="Security"
      />
      <div role="tabpanel" class="tab-content p-6">
        <!-- Security Tab -->
        <div class="space-y-6">
          <div class="space-y-4">
            <h3 class="text-xl font-semibold">Change Password</h3>
            <div class="form-control">
              <label class="label">
                <span class="label-text">Current Password</span>
              </label>
              <input
                type="password"
                v-model="currentPassword"
                placeholder="Enter current password"
                class="input input-bordered w-full max-w-md"
              />
            </div>

            <div class="form-control">
              <label class="label">
                <span class="label-text">New Password</span>
              </label>
              <input
                type="password"
                v-model="newPassword"
                placeholder="Enter new password"
                class="input input-bordered w-full max-w-md"
              />
            </div>

            <div class="form-control">
              <label class="label">
                <span class="label-text">Confirm New Password</span>
              </label>
              <input
                type="password"
                v-model="confirmPassword"
                placeholder="Confirm new password"
                class="input input-bordered w-full max-w-md"
              />
            </div>

            <button @click="changePassword" class="btn btn-primary">
              Change Password
            </button>
          </div>

          <div class="divider"></div>

          <div>
            <div class="flex justify-between items-center mb-4">
              <h3 class="text-xl font-semibold">Active Sessions</h3>
              <button class="btn btn-error btn-sm" @click="logoutAllSessions">
                Logout All Sessions
              </button>
            </div>

            <div class="overflow-x-auto">
              <table class="table table-zebra">
                <thead>
                  <tr>
                    <th>Device</th>
                    <th>Platform</th>
                    <th>IP Address</th>
                    <th>Last Active</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="session in activeSessions" :key="session.id">
                    <td>{{ session.name }}</td>
                    <td>{{ session.platform }}</td>
                    <td>{{ session.ip }}</td>
                    <td>{{ session.lastActive }}</td>
                    <td>
                      <button
                        class="btn btn-error btn-xs"
                        @click="logoutSession(session.id)"
                      >
                        Logout
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useToastStore } from "@/stores/useToastStore";
import { useUserStore } from "@/stores/useUserStore";
import axios from "../api/axios";

const toast = useToastStore();
const userStore = useUserStore();

const username = ref(userStore.username);
const activeSessions = ref([]);
const selectedTheme = ref(userStore.theme);
const streamerMode = ref(false);
const themes = [
  { name: "light", label: "Light" },
  { name: "dark", label: "Dark" },
  { name: "cupcake", label: "Cupcake" },
  { name: "bumblebee", label: "Bumblebee" },
  { name: "emerald", label: "Emerald" },
  { name: "corporate", label: "Corporate" },
  { name: "synthwave", label: "Synthwave" },
  { name: "retro", label: "Retro" },
  { name: "cyberpunk", label: "Cyberpunk" },
  { name: "valentine", label: "Valentine" },
  { name: "halloween", label: "Halloween" },
  { name: "garden", label: "Garden" },
  { name: "forest", label: "Forest" },
  { name: "aqua", label: "Aqua" },
  { name: "lofi", label: "Lofi" },
  { name: "pastel", label: "Pastel" },
  { name: "fantasy", label: "Fantasy" },
  { name: "wireframe", label: "Wireframe" },
  { name: "black", label: "Black" },
  { name: "luxury", label: "Luxury" },
  { name: "dracula", label: "Dracula" },
  { name: "cmyk", label: "CMYK" },
  { name: "autumn", label: "Autumn" },
  { name: "business", label: "Business" },
  { name: "acid", label: "Acid" },
  { name: "lemonade", label: "Lemonade" },
  { name: "night", label: "Night" },
  { name: "coffee", label: "Coffee" },
  { name: "winter", label: "Winter" },
  { name: "dim", label: "Dim" },
  { name: "nord", label: "Nord" },
  { name: "sunset", label: "Sunset" },
  { name: "caramellatte", label: "Caramel Latte" },
  { name: "abyss", label: "Abyss" },
  { name: "silk", label: "Silk" },
];

// Fetch sessions
const fetchActiveSessions = async () => {
  try {
    const res = await axios.get("/Auth/sessions");
    activeSessions.value = res.data.map((d) => {
      const info = JSON.parse(d.deviceInfo || "{}");
      return {
        id: d.id,
        name: info.userAgent || "Unknown",
        platform: info.platform || "Unknown",
        ip: info.ip || "Unknown",
        lastActive: new Date(d.lastUsedAt).toLocaleString(),
      };
    });
  } catch (err) {
    toast.show("Failed to fetch sessions", "error");
  }
};

// Logout a single session
const logoutSession = async (sessionId) => {
  try {
    await axios.post("/Auth/logout-session", { sessionId });
    toast.show("Device logged out", "success");
    fetchActiveSessions();
  } catch (err) {
    toast.show("Failed to logout device", "error");
  }
};

// Logout all Sessions
const logoutAllSessions = async () => {
  try {
    await axios.post("/auth/logout-all");
    toast.show("All sessions logged out", "success");
    activeSessions.value = [];
  } catch (err) {
    toast.show("Failed to logout all sessions", "error");
  }
};

onMounted(fetchActiveSessions);

const currentPassword = ref("");
const newPassword = ref("");
const confirmPassword = ref("");

const saveAccount = async () => {
  try {
    const response = await axios.put("/Profile", {
      Username: username.value,
    });
    toast.show("Profile updated successfully!", "success");
  } catch (error) {}
};

const saveAppearance = async () => {
  try {
    const response = await axios.put("/Profile", {
      Theme: selectedTheme.value,
      streamerMode: streamerMode.value,
    });
    userStore.streamerMode = streamerMode.value;
    userStore.theme = selectedTheme.value;
    toast.show("Appearence updated successfully!", "success");
  } catch (error) {}
};

const changePassword = async () => {
  if (newPassword.value !== confirmPassword.value) {
    toast.show("New passwords do not match!", "error");
    return;
  }
  try {
    const response = await axios.post("/Profile/change-password", {
      CurrentPassword: currentPassword.value,
      NewPassword: newPassword.value,
    });
    toast.show("Password changed successfully!", "success");
    currentPassword.value = "";
    newPassword.value = "";
    confirmPassword.value = "";
  } catch (error) {}
};
</script>
