<template>
  <div class="container mx-auto p-6 max-w-6xl">
    <h1 class="text-3xl font-bold mb-6">Admin Dashboard</h1>

    <!-- Tabs -->
    <div role="tablist" class="tabs tabs-border">
      <!-- Users Tab -->
      <input
        type="radio"
        name="admin_tabs"
        role="tab"
        class="tab"
        aria-label="Users"
        checked
      />
      <div role="tabpanel" class="tab-content p-0 pt-6">
        <div class="card bg-base-100 shadow-xl">
          <div class="card-body">
            <div class="flex justify-between items-start mb-6">
              <div>
                <h2 class="card-title text-2xl mb-2">User Management</h2>
                <p class="text-base-content/70">View and manage all users</p>
              </div>
              <button @click="fetchUsers" class="btn btn-sm btn-ghost">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  class="h-5 w-5"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                >
                  <path
                    fill-rule="evenodd"
                    d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z"
                    clip-rule="evenodd"
                  />
                </svg>
                Refresh
              </button>
            </div>

            <div v-if="loadingUsers" class="flex justify-center py-12">
              <span class="loading loading-spinner loading-lg"></span>
            </div>

            <div v-else-if="users.length === 0" class="text-center py-12">
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
                  d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
                />
              </svg>
              <p class="text-base-content/60">No users found</p>
            </div>

            <div v-else class="overflow-x-auto">
              <table class="table table-zebra">
                <thead>
                  <tr>
                    <th class="font-semibold">ID</th>
                    <th class="font-semibold">Username</th>
                    <th class="font-semibold">Role</th>
                    <th class="font-semibold">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="user in users" :key="user.id">
                    <td class="font-mono text-sm">{{ user.id }}</td>
                    <td class="font-medium">{{ user.username }}</td>

                    <td>
                      <span
                        class="badge"
                        :class="
                          user.role === 'Admin' ? 'badge-error' : 'badge-ghost'
                        "
                      >
                        {{ user.role }}
                      </span>
                    </td>
                    <td>
                      <button
                        class="btn btn-sm btn-ghost"
                        @click="viewUserDetails(user)"
                      >
                        View
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- Database Tab -->
      <input
        type="radio"
        name="admin_tabs"
        role="tab"
        class="tab"
        aria-label="Database"
      />
      <div role="tabpanel" class="tab-content p-0 pt-6">
        <div class="space-y-6">
          <!-- Database Statistics -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <div class="flex justify-between items-start mb-6">
                <div>
                  <h2 class="card-title text-2xl mb-2">Database Statistics</h2>
                  <p class="text-base-content/70">
                    Overview of stored data in the database
                  </p>
                </div>
                <button
                  @click="fetchDatabaseStats"
                  class="btn btn-sm btn-ghost"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  Refresh
                </button>
              </div>

              <div v-if="loadingStats" class="flex justify-center py-12">
                <span class="loading loading-spinner loading-lg"></span>
              </div>

              <div
                v-else
                class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
              >
                <!-- Simulations -->
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Simulations</div>
                  <div class="stat-value text-primary">
                    {{ dbStats.simulations || 0 }}
                  </div>
                  <div class="stat-desc">Total simulations created</div>
                </div>

                <!-- Agents -->
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Agents</div>
                  <div class="stat-value text-secondary">
                    {{ dbStats.agents || 0 }}
                  </div>
                  <div class="stat-desc">Total agents generated</div>
                </div>

                <!-- Snapshots -->
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Snapshots</div>
                  <div class="stat-value text-accent">
                    {{ (dbStats.snapshots?.snapshots || 0).toLocaleString() }}
                  </div>
                  <div class="stat-desc">Simulation snapshots</div>
                </div>

                <!-- Buildings -->
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Buildings</div>
                  <div class="stat-value">{{ dbStats.buildings || 0 }}</div>
                  <div class="stat-desc">Total buildings</div>
                </div>

                <!-- Map Areas -->
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Map Areas</div>
                  <div class="stat-value">{{ dbStats.mapAreas || 0 }}</div>
                  <div class="stat-desc">Configured map areas</div>
                </div>

                <!-- Metrics -->
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Metrics</div>
                  <div class="stat-value">{{ dbStats.metrics || 0 }}</div>
                  <div class="stat-desc">Recorded metrics</div>
                </div>
              </div>

              <!-- Presets Section -->
              <div class="divider mt-8">Presets</div>
              <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Map Area Presets</div>
                  <div class="stat-value text-sm">
                    {{
                      (dbStats.presets?.mapAreaPresets || 0).toLocaleString()
                    }}
                  </div>
                </div>
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">Disease Presets</div>
                  <div class="stat-value text-sm">
                    {{
                      (dbStats.presets?.diseasePresets || 0).toLocaleString()
                    }}
                  </div>
                </div>
                <div class="stat bg-base-200 rounded-lg">
                  <div class="stat-title">OSM Building Presets</div>
                  <div class="stat-value text-sm">
                    {{
                      (
                        dbStats.presets?.osmBuildingPresets || 0
                      ).toLocaleString()
                    }}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Database Actions -->
          <div class="card bg-base-100 shadow-xl">
            <div class="card-body">
              <h2 class="card-title text-2xl mb-2">Database Actions</h2>
              <p class="text-base-content/70 mb-6">
                Perform bulk operations on the database
              </p>

              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <!-- Delete All Simulations -->
                <div class="card bg-base-200">
                  <div class="card-body">
                    <h3 class="card-title text-lg">Delete All Simulations</h3>
                    <p class="text-sm text-base-content/70 mb-4">
                      Permanently delete all simulations and related data
                      (agents, snapshots, metrics)
                    </p>
                    <div class="card-actions justify-end">
                      <button
                        @click="deleteAllSimulations"
                        class="btn btn-error btn-sm"
                      >
                        <svg
                          xmlns="http://www.w3.org/2000/svg"
                          class="h-5 w-5"
                          viewBox="0 0 20 20"
                          fill="currentColor"
                        >
                          <path
                            fill-rule="evenodd"
                            d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z"
                            clip-rule="evenodd"
                          />
                        </svg>
                        Delete All Simulations
                      </button>
                    </div>
                  </div>
                </div>

                <!-- Delete All Presets -->
                <div class="card bg-base-200">
                  <div class="card-body">
                    <h3 class="card-title text-lg">Delete All Presets</h3>
                    <p class="text-sm text-base-content/70 mb-4">
                      Delete all user-created presets (default presets are
                      preserved)
                    </p>
                    <div class="card-actions justify-end">
                      <button
                        @click="deleteAllPresets"
                        class="btn btn-error btn-sm"
                      >
                        <svg
                          xmlns="http://www.w3.org/2000/svg"
                          class="h-5 w-5"
                          viewBox="0 0 20 20"
                          fill="currentColor"
                        >
                          <path
                            fill-rule="evenodd"
                            d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z"
                            clip-rule="evenodd"
                          />
                        </svg>
                        Delete All Presets
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Memory Tab -->
      <input
        type="radio"
        name="admin_tabs"
        role="tab"
        class="tab"
        aria-label="Memory"
      />
      <div role="tabpanel" class="tab-content p-0 pt-6">
        <div class="card bg-base-100 shadow-xl">
          <div class="card-body">
            <div class="flex justify-between items-start mb-6">
              <div>
                <h2 class="card-title text-2xl mb-2">Memory Management</h2>
                <p class="text-base-content/70">
                  View and manage simulations currently loaded in memory
                </p>
              </div>
              <div class="flex gap-2">
                <button
                  @click="fetchLoadedSimulations"
                  class="btn btn-sm btn-ghost"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  Refresh
                </button>
                <button
                  v-if="loadedSimulations.length > 0"
                  @click="unloadAllSimulations"
                  class="btn btn-sm btn-error"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  Unload All
                </button>
              </div>
            </div>

            <div v-if="loadingMemory" class="flex justify-center py-12">
              <span class="loading loading-spinner loading-lg"></span>
            </div>

            <div
              v-else-if="loadedSimulations.length === 0"
              class="text-center py-12"
            >
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
                  d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4"
                />
              </svg>
              <p class="text-base-content/60">
                No simulations loaded in memory
              </p>
            </div>

            <div v-else class="overflow-x-auto">
              <table class="table table-zebra">
                <thead>
                  <tr>
                    <th class="font-semibold">ID</th>
                    <th class="font-semibold">Name</th>
                    <th class="font-semibold">Status</th>
                    <th class="font-semibold">Owner</th>
                    <th class="font-semibold">Agents</th>
                    <th class="font-semibold">Current Tick</th>
                    <th class="font-semibold">Speed</th>
                    <th class="font-semibold">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="sim in loadedSimulations" :key="sim.id">
                    <td class="font-mono text-sm">{{ sim.id }}</td>
                    <td class="font-medium">{{ sim.name }}</td>
                    <td>
                      <span
                        class="badge"
                        :class="{
                          'badge-success': sim.status === 'Running',
                          'badge-warning': sim.status === 'Paused',
                          'badge-info': sim.status === 'Loaded',
                        }"
                      >
                        {{ sim.status }}
                      </span>
                    </td>
                    <td class="text-sm">
                      {{ sim.ownerUsername || "Unknown" }}
                    </td>
                    <td class="text-sm">
                      {{ sim.agentCount?.toLocaleString() || 0 }}
                    </td>
                    <td class="font-mono text-sm">
                      {{ sim.currentTick || 0 }}
                    </td>
                    <td class="text-sm">{{ sim.speed || 1 }}x</td>
                    <td>
                      <div class="flex gap-2">
                        <button
                          v-if="sim.status === 'Running'"
                          @click="pauseSimulation(sim.id)"
                          class="btn btn-xs btn-warning"
                          title="Pause"
                        >
                          <svg
                            xmlns="http://www.w3.org/2000/svg"
                            class="h-4 w-4"
                            viewBox="0 0 20 20"
                            fill="currentColor"
                          >
                            <path
                              fill-rule="evenodd"
                              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zM7 8a1 1 0 012 0v4a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v4a1 1 0 102 0V8a1 1 0 00-1-1z"
                              clip-rule="evenodd"
                            />
                          </svg>
                        </button>
                        <button
                          v-else
                          @click="runSimulation(sim.id)"
                          class="btn btn-xs btn-success"
                          title="Run"
                        >
                          <svg
                            xmlns="http://www.w3.org/2000/svg"
                            class="h-4 w-4"
                            viewBox="0 0 20 20"
                            fill="currentColor"
                          >
                            <path
                              fill-rule="evenodd"
                              d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z"
                              clip-rule="evenodd"
                            />
                          </svg>
                        </button>
                        <button
                          @click="unloadSimulation(sim.id)"
                          class="btn btn-xs btn-error"
                          title="Unload"
                        >
                          <svg
                            xmlns="http://www.w3.org/2000/svg"
                            class="h-4 w-4"
                            viewBox="0 0 20 20"
                            fill="currentColor"
                          >
                            <path
                              fill-rule="evenodd"
                              d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                              clip-rule="evenodd"
                            />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- User Details Modal -->
    <dialog
      id="user_details_modal"
      class="modal"
      :class="{ 'modal-open': selectedUser !== null }"
    >
      <div class="modal-box w-full max-w-md">
        <h3 class="font-bold text-lg mb-4">User Details</h3>

        <div v-if="selectedUser" class="space-y-4">
          <!-- User ID -->
          <div>
            <label class="label">
              <span class="label-text font-medium">User ID</span>
            </label>
            <input
              type="text"
              class="input input-bordered w-full"
              :value="selectedUser.id"
              disabled
            />
          </div>

          <!-- Username -->
          <div>
            <label class="label">
              <span class="label-text font-medium">Username</span>
            </label>
            <input
              type="text"
              class="input input-bordered w-full"
              :value="selectedUser.username"
              disabled
            />
          </div>

          <!-- User Role -->
          <div>
            <label class="label">
              <span class="label-text font-medium">Role</span>
            </label>
            <select
              v-model="selectedUserRole"
              class="select select-bordered w-full"
            >
              <option value="User">User</option>
              <option value="Admin">Admin</option>
            </select>
          </div>

          <!-- Status Indicator -->
          <div>
            <label class="label">
              <span class="label-text font-medium">Current Role</span>
            </label>
            <div class="flex items-center gap-2">
              <span
                class="badge"
                :class="
                  selectedUser.role === 'Admin' ? 'badge-error' : 'badge-ghost'
                "
              >
                {{ selectedUser.role }}
              </span>
              <span
                v-if="selectedUserRole !== selectedUser.role"
                class="text-sm text-warning"
              >
                Changes unsaved
              </span>
            </div>
          </div>
        </div>

        <div class="modal-action mt-6">
          <button @click="closeUserModal" class="btn btn-ghost">Cancel</button>
          <button
            v-if="selectedUserRole !== selectedUser?.role"
            @click="updateUserRole"
            class="btn btn-primary"
            :disabled="updatingRole"
          >
            <span
              v-if="updatingRole"
              class="loading loading-spinner loading-sm"
            ></span>
            <span v-else>Save Changes</span>
          </button>
        </div>
      </div>
      <form
        method="dialog"
        class="modal-backdrop"
        @submit.prevent="closeUserModal"
      >
        <button></button>
      </form>
    </dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useToastStore } from "@/stores/useToastStore";
import axios from "../api/axios";

const toast = useToastStore();

const users = ref([]);
const loadingUsers = ref(false);

const dbStats = ref({});
const loadingStats = ref(false);

const loadedSimulations = ref([]);
const loadingMemory = ref(false);

const selectedUser = ref(null);
const selectedUserRole = ref("User");
const updatingRole = ref(false);

const fetchUsers = async () => {
  loadingUsers.value = true;
  try {
    const res = await axios.get("/Admin/users");
    console.error(res.data);
    users.value = res.data;
  } catch (err) {
    toast.show("Failed to fetch users", "error");
  } finally {
    loadingUsers.value = false;
  }
};

const viewUserDetails = (user) => {
  selectedUser.value = user;
  selectedUserRole.value = user.role;
  document.getElementById("user_details_modal").showModal();
};

const closeUserModal = () => {
  selectedUser.value = null;
  selectedUserRole.value = "User";
  document.getElementById("user_details_modal").close();
};

const updateUserRole = async () => {
  if (!selectedUser.value) return;

  updatingRole.value = true;
  try {
    await axios.put(`/Admin/users/${selectedUser.value.id}/role`, {
      role: selectedUserRole.value,
    });

    // Update the user in the list
    const userIndex = users.value.findIndex(
      (u) => u.id === selectedUser.value.id,
    );
    if (userIndex !== -1) {
      users.value[userIndex].role = selectedUserRole.value;
      selectedUser.value.role = selectedUserRole.value;
    }

    toast.show(`User role updated to ${selectedUserRole.value}`, "success");
  } catch (err) {
    toast.show("Failed to update user role", "error");
    selectedUserRole.value = selectedUser.value.role;
  } finally {
    updatingRole.value = false;
  }
};

const fetchDatabaseStats = async () => {
  loadingStats.value = true;
  try {
    const res = await axios.get("/Admin/stats");
    dbStats.value = res.data;
  } catch (err) {
    toast.show("Failed to fetch database statistics", "error");
  } finally {
    loadingStats.value = false;
  }
};

const deleteAllSimulations = async () => {
  if (
    !confirm(
      "Are you sure you want to delete ALL simulations? This action cannot be undone!",
    )
  ) {
    return;
  }
  try {
    const res = await axios.delete("/Admin/simulations");
    toast.show(res.data.message, "success");
    await fetchDatabaseStats();
  } catch (err) {
    toast.show("Failed to delete simulations", "error");
  }
};

const deleteAllPresets = async () => {
  if (
    !confirm(
      "Are you sure you want to delete ALL user-created presets? Default presets will be preserved.",
    )
  ) {
    return;
  }
  try {
    const res = await axios.delete("/Admin/presets");
    toast.show(res.data.message, "success");
    await fetchDatabaseStats();
  } catch (err) {
    toast.show("Failed to delete presets", "error");
  }
};

const fetchLoadedSimulations = async () => {
  loadingMemory.value = true;
  try {
    const res = await axios.get("/Admin/loaded-simulations");
    loadedSimulations.value = res.data;
  } catch (err) {
    toast.show("Failed to fetch loaded simulations", "error");
  } finally {
    loadingMemory.value = false;
  }
};

const unloadSimulation = async (id) => {
  try {
    await axios.post(`/Simulation/${id}/unload`);
    toast.show(`Simulation ${id} unloaded`, "success");
    await fetchLoadedSimulations();
  } catch (err) {
    toast.show(`Failed to unload simulation ${id}`, "error");
  }
};

const pauseSimulation = async (id) => {
  try {
    await axios.post(`/Simulation/${id}/pause`);
    toast.show(`Simulation ${id} paused`, "success");
    await fetchLoadedSimulations();
  } catch (err) {
    toast.show(`Failed to pause simulation ${id}`, "error");
  }
};

const runSimulation = async (id) => {
  try {
    await axios.post(`/Simulation/${id}/run`);
    toast.show(`Simulation ${id} running`, "success");
    await fetchLoadedSimulations();
  } catch (err) {
    toast.show(`Failed to run simulation ${id}`, "error");
  }
};

const unloadAllSimulations = async () => {
  if (
    !confirm(
      "Are you sure you want to unload ALL simulations from memory? Running simulations will be stopped.",
    )
  ) {
    return;
  }
  try {
    await axios.post("/Admin/unload-all");
    toast.show("All simulations unloaded", "success");
    await fetchLoadedSimulations();
  } catch (err) {
    toast.show("Failed to unload all simulations", "error");
  }
};

onMounted(() => {
  fetchUsers();
  fetchDatabaseStats();
  fetchLoadedSimulations();
});
</script>
