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
      <div role="tabpanel" class="tab-content p-0 pt-6">
        <!-- Account Tab -->
        <div class="max-w-3xl">
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h2 class="card-title text-2xl mb-4">Account Information</h2>
              <p class="text-base-content/70 mb-6">
                Update your account details and personal information
              </p>

              <div class="form-control w-full">
                <label class="label">
                  <span class="label-text font-medium">Username:</span>
                </label>
                <input
                  type="text"
                  v-model="username"
                  placeholder="Enter username"
                  class="input input-bordered w-full"
                />
                <label class="label">
                  <span class="label-text-alt text-base-content/60"
                    >This is your public display name</span
                  >
                </label>
              </div>

              <div class="card-actions justify-end mt-4">
                <button @click="saveAccount" class="btn btn-primary">
                  Save Changes
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <input
        type="radio"
        name="settings_tabs"
        role="tab"
        class="tab"
        aria-label="Appearance"
      />
      <div role="tabpanel" class="tab-content p-0 pt-6">
        <!-- Appearance Tab -->
        <div class="space-y-6">
          <!-- Theme Selection Card -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h2 class="card-title text-2xl mb-2">Theme Preferences</h2>
              <p class="text-base-content/70 mb-6">
                Customize the look and feel of the platform
              </p>

              <div class="space-y-6">
                <!-- Theme Selector -->
                <div>
                  <label class="label">
                    <span class="label-text font-medium text-lg"
                      >Select Theme</span
                    >
                  </label>

                  <div
                    class="flex flex-col lg:flex-row items-start lg:items-start gap-6"
                  >
                    <!-- Theme selector dropdown -->
                    <div class="w-full lg:w-80">
                      <select
                        v-model="selectedTheme"
                        class="select select-bordered w-full"
                      >
                        <option
                          v-for="theme in themes"
                          :key="theme.name"
                          :value="theme.name"
                        >
                          {{ theme.label }}
                        </option>
                      </select>
                    </div>

                    <!-- Theme preview -->
                    <div class="flex-1 w-full">
                      <div
                        class="rounded-lg p-4 border-2 border-base-300 bg-base-100"
                        :data-theme="selectedTheme"
                      >
                        <div class="text-sm font-medium mb-3 opacity-70">
                          Preview
                        </div>
                        <div class="grid grid-cols-3 gap-3">
                          <div
                            class="bg-primary text-primary-content p-3 rounded-lg text-center font-semibold text-sm"
                          >
                            Primary
                          </div>
                          <div
                            class="bg-secondary text-secondary-content p-3 rounded-lg text-center font-semibold text-sm"
                          >
                            Secondary
                          </div>
                          <div
                            class="bg-accent text-accent-content p-3 rounded-lg text-center font-semibold text-sm"
                          >
                            Accent
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Expandable grid preview -->
                <div class="divider"></div>

                <details class="collapse collapse-arrow bg-base-200">
                  <summary
                    class="collapse-title text-md font-medium cursor-pointer"
                  >
                    Browse All Available Themes
                  </summary>
                  <div class="collapse-content">
                    <div
                      class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 gap-4 pt-4"
                    >
                      <div
                        v-for="theme in themes"
                        :key="theme.name"
                        @click="selectedTheme = theme.name"
                        :class="[
                          'card cursor-pointer border-2 transition-all hover:shadow-lg',
                          selectedTheme === theme.name
                            ? 'border-primary ring-2 ring-primary ring-offset-2 ring-offset-base-100'
                            : 'border-base-300 hover:border-base-content/20',
                        ]"
                        :data-theme="theme.name"
                      >
                        <div class="card-body p-3">
                          <h3 class="text-sm font-semibold text-center mb-2">
                            {{ theme.label }}
                          </h3>
                          <div class="flex flex-col gap-1.5">
                            <div
                              class="bg-primary text-primary-content p-1.5 rounded text-center text-[10px] font-medium"
                            >
                              Primary
                            </div>
                            <div
                              class="bg-secondary text-secondary-content p-1.5 rounded text-center text-[10px] font-medium"
                            >
                              Secondary
                            </div>
                            <div
                              class="bg-accent text-accent-content p-1.5 rounded text-center text-[10px] font-medium"
                            >
                              Accent
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </details>
              </div>
            </div>
          </div>

          <!-- Additional Settings Card -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h2 class="card-title text-2xl mb-2">Display Options</h2>
              <p class="text-base-content/70 mb-6">
                Manage your privacy and display preferences
              </p>

              <!-- Streamer Mode -->
              <div class="form-control">
                <label class="label cursor-pointer justify-start gap-4">
                  <input
                    type="checkbox"
                    v-model="streamerMode"
                    class="toggle toggle-primary"
                  />
                  <div>
                    <span class="label-text font-medium text-base"
                      >Streamer Mode</span
                    >
                    <p class="text-sm text-base-content/60 mt-1">
                      Hide sensitive information when streaming or recording
                    </p>
                  </div>
                </label>
              </div>

              <div class="card-actions justify-end mt-6">
                <button @click="saveAppearance" class="btn btn-primary">
                  Save Appearance Settings
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <input
        type="radio"
        name="settings_tabs"
        role="tab"
        class="tab"
        aria-label="Security"
      />
      <div role="tabpanel" class="tab-content p-0 pt-6">
        <!-- Security Tab -->
        <div class="space-y-6">
          <!-- Change Password Card -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h2 class="card-title text-2xl mb-2">Change Password</h2>
              <p class="text-base-content/70 mb-6">
                Update your password to keep your account secure
              </p>

              <div class="space-y-4 max-w-xl">
                <div class="form-control w-full">
                  <label class="label">
                    <span class="label-text font-medium">Current Password</span>
                  </label>
                  <input
                    type="password"
                    v-model="currentPassword"
                    placeholder="Enter current password"
                    class="input input-bordered w-full"
                  />
                </div>

                <div class="form-control w-full">
                  <label class="label">
                    <span class="label-text font-medium">New Password</span>
                  </label>
                  <input
                    type="password"
                    v-model="newPassword"
                    placeholder="Enter new password"
                    class="input input-bordered w-full"
                  />
                </div>

                <div class="form-control w-full">
                  <label class="label">
                    <span class="label-text font-medium"
                      >Confirm New Password</span
                    >
                  </label>
                  <input
                    type="password"
                    v-model="confirmPassword"
                    placeholder="Confirm new password"
                    class="input input-bordered w-full"
                  />
                </div>
              </div>

              <div class="card-actions justify-end mt-6">
                <button @click="changePassword" class="btn btn-primary">
                  Update Password
                </button>
              </div>
            </div>
          </div>

          <!-- Active Sessions Card -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <div
                class="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-4 mb-6"
              >
                <div>
                  <h2 class="card-title text-2xl mb-2">Active Sessions</h2>
                  <p class="text-base-content/70">
                    Manage devices that are currently logged into your account
                  </p>
                </div>
                <button
                  class="btn btn-error btn-sm sm:btn-md"
                  @click="logoutAllSessions"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M3 3a1 1 0 011 1v12a1 1 0 11-2 0V4a1 1 0 011-1zm7.707 3.293a1 1 0 010 1.414L9.414 9H17a1 1 0 110 2H9.414l1.293 1.293a1 1 0 01-1.414 1.414l-3-3a1 1 0 010-1.414l3-3a1 1 0 011.414 0z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  Logout All Sessions
                </button>
              </div>

              <div class="overflow-x-auto">
                <table class="table table-zebra">
                  <thead>
                    <tr>
                      <th class="font-semibold">Device</th>
                      <th class="font-semibold">Platform</th>
                      <th class="font-semibold">IP Address</th>
                      <th class="font-semibold">Last Active</th>
                      <th class="font-semibold">Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="session in activeSessions" :key="session.id">
                      <td class="font-medium">{{ session.name }}</td>
                      <td>{{ session.platform }}</td>
                      <td>
                        <code class="text-xs bg-base-200 px-2 py-1 rounded">{{
                          session.ip
                        }}</code>
                      </td>
                      <td class="text-sm">{{ session.lastActive }}</td>
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

              <div v-if="activeSessions.length === 0" class="text-center py-12">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  class="h-16 w-16 mx-auto text-base-content/20 mb-4"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                  />
                </svg>
                <p class="text-base-content/60">No active sessions found</p>
              </div>
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
