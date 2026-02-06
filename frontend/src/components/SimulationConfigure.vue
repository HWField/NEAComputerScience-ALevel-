<template>
  <div class="grid grid-cols-12 gap-6">
    <!-- Left Column Disease Profile -->
    <div class="col-span-6 space-y-6">
      <!-- Disease Info Card -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <div
            class="flex items-center justify-between cursor-pointer"
            @click="toggleSection('disease')"
          >
            <h3 class="card-title text-lg flex items-center gap-2">
              <i-ph-virus class="w-5 h-5 text-error" />
              Disease Profile
            </h3>
            <i-ph-caret-down
              class="w-5 h-5 transition-transform"
              :class="{ 'rotate-180': expandedSections.disease }"
            />
          </div>
          <div
            v-if="expandedSections.disease && simulationData?.disease"
            class="space-y-3 mt-4"
          >
            <!-- Edit/Save buttons -->
            <div class="flex justify-end gap-2">
              <button
                v-if="!isEditingDisease"
                class="btn btn-sm btn-primary"
                @click="startEditingDisease"
              >
                <i-ph-pencil class="w-4 h-4" />
                Edit Disease
              </button>
              <template v-else>
                <button
                  class="btn btn-sm btn-ghost"
                  @click="cancelEditingDisease"
                  :disabled="isSaving"
                >
                  Cancel
                </button>
                <button
                  class="btn btn-sm btn-success"
                  @click="saveDisease"
                  :disabled="isSaving"
                >
                  <span v-if="!isSaving">Save Changes</span>
                  <span
                    v-else
                    class="loading loading-spinner loading-xs"
                  ></span>
                </button>
              </template>
            </div>

            <div>
              <div class="text-xs text-base-content/70 mb-1">Name</div>
              <div class="font-semibold">
                {{ simulationData.disease.name }}
              </div>
            </div>
            <div class="divider my-2"></div>

            <!-- Disease Properties (View/Edit Mode) -->
            <div v-if="!isEditingDisease" class="grid grid-cols-2 gap-2">
              <div>
                <div class="text-xs text-base-content/70 mb-1">
                  Infection Rate
                </div>
                <div class="text-error font-bold">
                  {{
                    (simulationData.disease.infectionRate * 100).toFixed(1)
                  }}%/hr
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">
                  Recovery Rate
                </div>
                <div class="text-success font-bold">
                  {{
                    (simulationData.disease.recoveryRate * 100).toFixed(1)
                  }}%/day
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">
                  Mortality Rate
                </div>
                <div class="text-warning font-bold">
                  {{ (simulationData.disease.mortalityRate * 100).toFixed(1) }}%
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">
                  Symptomatic Rate
                </div>
                <div class="font-bold">
                  {{
                    (simulationData.disease.symptomaticRate * 100).toFixed(1)
                  }}%
                </div>
              </div>
            </div>

            <!-- Edit Mode: Input Fields -->
            <div v-else class="grid grid-cols-2 gap-3">
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Infection Rate (%/hr)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.infectionRate"
                  step="0.001"
                  min="0"
                  max="1"
                  class="input input-sm input-bordered"
                />
              </div>
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Recovery Rate (%/day)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.recoveryRate"
                  step="0.001"
                  min="0"
                  max="1"
                  class="input input-sm input-bordered"
                />
              </div>
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Mortality Rate (%)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.mortalityRate"
                  step="0.001"
                  min="0"
                  max="1"
                  class="input input-sm input-bordered"
                />
              </div>
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Symptomatic Rate (%)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.symptomaticRate"
                  step="0.01"
                  min="0"
                  max="1"
                  class="input input-sm input-bordered"
                />
              </div>
            </div>

            <div class="divider my-2"></div>

            <div v-if="!isEditingDisease" class="space-y-2 text-sm">
              <div class="flex justify-between">
                <span class="text-base-content/70">Incubation Period</span>
                <span class="font-medium"
                  >{{ simulationData.disease.incubationPeriod }} days</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Infectious Period</span>
                <span class="font-medium"
                  >{{ simulationData.disease.infectiousPeriod }} days</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Immunity Duration</span>
                <span class="font-medium"
                  >{{ simulationData.disease.immunityDuration }} days</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Severe Symptoms</span>
                <span class="font-medium"
                  >{{
                    (simulationData.disease.severeSymptomsRate * 100).toFixed(
                      1,
                    )
                  }}%</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Hospital Seeking</span>
                <span class="font-medium"
                  >{{
                    (simulationData.disease.hospitalSeekingRate * 100).toFixed(
                      1,
                    )
                  }}%</span
                >
              </div>
            </div>

            <!-- Edit Mode: More Input Fields -->
            <div v-else class="grid grid-cols-2 gap-3">
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Incubation Period (days)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.incubationPeriod"
                  step="0.1"
                  min="0"
                  class="input input-sm input-bordered"
                />
              </div>
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Infectious Period (days)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.infectiousPeriod"
                  step="0.1"
                  min="0"
                  class="input input-sm input-bordered"
                />
              </div>
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Immunity Duration (days)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.immunityDuration"
                  step="1"
                  min="0"
                  class="input input-sm input-bordered"
                />
              </div>
              <div class="form-control">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Severe Symptoms (%)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.severeSymptomsRate"
                  step="0.01"
                  min="0"
                  max="1"
                  class="input input-sm input-bordered"
                />
              </div>
              <div class="form-control col-span-2">
                <label class="label py-1"
                  ><span class="label-text text-xs"
                    >Hospital Seeking (%)</span
                  ></label
                >
                <input
                  type="number"
                  v-model.number="diseaseEdit.hospitalSeekingRate"
                  step="0.01"
                  min="0"
                  max="1"
                  class="input input-sm input-bordered"
                />
              </div>
            </div>

            <div class="divider my-2"></div>

            <div v-if="!isEditingDisease" class="space-y-2 text-sm">
              <div class="text-xs text-base-content/70 font-semibold mb-1">
                Intervention Effectiveness
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Masks</span>
                <span class="font-medium text-info"
                  >{{
                    (simulationData.disease.maskEffectiveness * 100).toFixed(0)
                  }}% reduction</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Vaccines</span>
                <span class="font-medium text-success"
                  >{{
                    (simulationData.disease.vaccineEffectiveness * 100).toFixed(
                      0,
                    )
                  }}% reduction</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Reinfection Resistance</span>
                <span class="font-medium"
                  >{{
                    (
                      simulationData.disease.reinfectionResistance * 100
                    ).toFixed(0)
                  }}%</span
                >
              </div>
            </div>

            <!-- Edit Mode: Intervention Fields -->
            <div v-else class="space-y-3">
              <div class="text-xs text-base-content/70 font-semibold">
                Intervention Effectiveness
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div class="form-control">
                  <label class="label py-1"
                    ><span class="label-text text-xs"
                      >Mask Effectiveness (%)</span
                    ></label
                  >
                  <input
                    type="number"
                    v-model.number="diseaseEdit.maskEffectiveness"
                    step="0.01"
                    min="0"
                    max="1"
                    class="input input-sm input-bordered"
                  />
                </div>
                <div class="form-control">
                  <label class="label py-1"
                    ><span class="label-text text-xs"
                      >Vaccine Effectiveness (%)</span
                    ></label
                  >
                  <input
                    type="number"
                    v-model.number="diseaseEdit.vaccineEffectiveness"
                    step="0.01"
                    min="0"
                    max="1"
                    class="input input-sm input-bordered"
                  />
                </div>
                <div class="form-control col-span-2">
                  <label class="label py-1"
                    ><span class="label-text text-xs"
                      >Reinfection Resistance (%)</span
                    ></label
                  >
                  <input
                    type="number"
                    v-model.number="diseaseEdit.reinfectionResistance"
                    step="0.01"
                    min="0"
                    max="1"
                    class="input input-sm input-bordered"
                  />
                </div>
              </div>
            </div>
          </div>
          <div
            v-else-if="!expandedSections.disease"
            class="text-sm text-base-content/60 italic mt-2"
          >
            Click to expand
          </div>
          <div v-else class="text-sm text-base-content/60 italic mt-2">
            No disease configured
          </div>
        </div>
      </div>

      <!-- Map Area Info -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <div
            class="flex items-center justify-between cursor-pointer"
            @click="toggleSection('mapArea')"
          >
            <h3 class="card-title text-lg flex items-center gap-2">
              <i-ph-map-pin-bold class="w-5 h-5 text-accent" />
              Map Area
            </h3>
            <i-ph-caret-down
              class="w-5 h-5 transition-transform"
              :class="{ 'rotate-180': expandedSections.mapArea }"
            />
          </div>
          <div
            v-if="expandedSections.mapArea && simulationData?.mapArea"
            class="space-y-3 mt-4"
          >
            <div>
              <div class="text-xs text-base-content/70 mb-1">Location</div>
              <div class="font-semibold">
                {{ simulationData.mapArea.name }}
              </div>
            </div>
            <div class="divider my-2"></div>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <div class="text-xs text-base-content/70 mb-1">Radius</div>
                <div class="font-bold">
                  {{ simulationData.mapArea.radiusKm }} km
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">Buildings</div>
                <div class="font-bold">
                  {{ formatNumber(simulationData.mapArea.buildingCount) }}
                </div>
              </div>
              <div>
                <div class="text-xs text-base-content/70 mb-1">Area</div>
                <div class="font-bold">
                  {{
                    (
                      Math.PI * Math.pow(simulationData.mapArea.radiusKm, 2)
                    ).toFixed(1)
                  }}
                  km²
                </div>
              </div>
            </div>
            <div class="divider my-2"></div>
            <div class="grid grid-cols-2 gap-2 text-sm">
              <div class="flex justify-between">
                <span class="text-base-content/70">Latitude</span>
                <span class="font-medium font-mono">{{
                  simulationData.mapArea.centerLat.toFixed(4)
                }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Longitude</span>
                <span class="font-medium font-mono">{{
                  simulationData.mapArea.centerLon.toFixed(4)
                }}</span>
              </div>
            </div>
          </div>
          <div
            v-else-if="!expandedSections.mapArea"
            class="text-sm text-base-content/60 italic mt-2"
          >
            Click to expand
          </div>
          <div v-else class="text-sm text-base-content/60 italic mt-2">
            No map area configured
          </div>
        </div>
      </div>
    </div>

    <!-- Right Column Population & Mitigations -->
    <div class="col-span-6 space-y-6">
      <!-- Population Stats Card -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <div
            class="flex items-center justify-between cursor-pointer"
            @click="toggleSection('population')"
          >
            <h3 class="card-title text-lg flex items-center gap-2">
              <i-ph-users class="w-5 h-5 text-primary" />
              Population Configuration
            </h3>
            <i-ph-caret-down
              class="w-5 h-5 transition-transform"
              :class="{ 'rotate-180': expandedSections.population }"
            />
          </div>
          <div v-if="expandedSections.population" class="space-y-4 mt-4">
            <!-- Total Agents -->
            <div class="stat bg-base-200 rounded-lg">
              <div class="stat-title text-xs">Total Agents</div>
              <div class="stat-value text-2xl">
                {{ formatNumber(simulationData?.agentCount || 0) }}
              </div>
              <div class="stat-desc">Simulated population size</div>
            </div>

            <div class="divider my-2"></div>

            <!-- Agent Role Distribution -->
            <div
              v-if="simulationData?.agentBreakdown"
              class="text-sm text-base-content/70"
            >
              <div class="font-semibold mb-3">Agent Distribution by Role</div>
              <div class="space-y-2">
                <div class="flex justify-between items-center">
                  <span class="flex items-center gap-2">
                    <i-ph-student class="w-4 h-4 text-info" />
                    Students
                  </span>
                  <span class="font-bold">{{
                    formatNumber(simulationData.agentBreakdown.students)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="flex items-center gap-2">
                    <i-ph-briefcase class="w-4 h-4 text-primary" />
                    Office Workers
                  </span>
                  <span class="font-bold">{{
                    formatNumber(simulationData.agentBreakdown.officeWorkers)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="flex items-center gap-2">
                    <i-ph-heart-break class="w-4 h-4 text-error" />
                    Medical Workers
                  </span>
                  <span class="font-bold">{{
                    formatNumber(simulationData.agentBreakdown.medicalWorkers)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="flex items-center gap-2">
                    <i-ph-storefront class="w-4 h-4 text-warning" />
                    Retail Workers
                  </span>
                  <span class="font-bold">{{
                    formatNumber(simulationData.agentBreakdown.retailWorkers)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="flex items-center gap-2">
                    <i-ph-wrench class="w-4 h-4 text-accent" />
                    Service Workers
                  </span>
                  <span class="font-bold">{{
                    formatNumber(simulationData.agentBreakdown.serviceWorkers)
                  }}</span>
                </div>
                <div class="flex justify-between items-center">
                  <span class="flex items-center gap-2">
                    <i-ph-user class="w-4 h-4 text-base-content/50" />
                    Elderly/Retired
                  </span>
                  <span class="font-bold">{{
                    formatNumber(simulationData.agentBreakdown.elderlyRetired)
                  }}</span>
                </div>
              </div>
            </div>
          </div>
          <div v-else class="text-sm text-base-content/60 italic mt-2">
            Click to expand
          </div>
        </div>
      </div>

      <!-- General Configuration Card -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <div class="flex items-center justify-between">
            <h3 class="card-title text-lg flex items-center gap-2">
              <i-ph-gear class="w-5 h-5 text-warning" />
              General Configuration
            </h3>
            <button
              v-if="!isEditingInfection"
              class="btn btn-sm btn-primary"
              @click="isEditingInfection = true"
            >
              <i-ph-pencil class="w-4 h-4" />
              Edit
            </button>
          </div>

          <!-- View Mode -->
          <div v-if="!isEditingInfection" class="space-y-3 mt-4">
            <div class="space-y-2 text-sm">
              <div class="flex justify-between">
                <span class="text-base-content/70"
                  >Initial Infection Count</span
                >
                <span class="font-medium">{{
                  simulationData?.initialInfectionCount || 0
                }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Metric Recording</span>
                <span class="font-medium"
                  >Every
                  {{ simulationData?.metricRecordingFrequency || 10 }}
                  tick(s)</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-base-content/70">Snapshot Saving</span>
                <span class="font-medium"
                  >Every
                  {{ simulationData?.snapshotSavingFrequency || 100 }}
                  second(s)</span
                >
              </div>
            </div>
          </div>

          <!-- Edit Mode -->
          <div v-else class="space-y-3 mt-4">
            <div class="form-control">
              <label class="label py-1">
                <span class="label-text text-xs">Initial Infection Count</span>
              </label>
              <input
                type="number"
                v-model.number="initialInfectionEdit"
                min="0"
                max="1000"
                class="input input-sm input-bordered"
              />
              <label class="label py-0">
                <span class="label-text-alt"
                  >Agents infected on seed/reset</span
                >
              </label>
            </div>

            <div class="form-control">
              <label class="label py-1">
                <span class="label-text text-xs"
                  >Metric Recording Frequency</span
                >
              </label>
              <input
                type="number"
                v-model.number="metricFrequencyEdit"
                min="1"
                max="10000"
                class="input input-sm input-bordered"
              />
              <label class="label py-0">
                <span class="label-text-alt">Record metrics every N ticks</span>
              </label>
            </div>

            <div class="form-control">
              <label class="label py-1">
                <span class="label-text text-xs"
                  >Snapshot Saving Frequency</span
                >
              </label>
              <input
                type="number"
                v-model.number="snapshotFrequencyEdit"
                min="1"
                max="10000"
                class="input input-sm input-bordered"
              />
              <label class="label py-0">
                <span class="label-text-alt"
                  >Save snapshot every N seconds (real-time)</span
                >
              </label>
            </div>

            <div class="flex justify-end gap-2 mt-3">
              <button
                class="btn btn-sm btn-ghost"
                @click="cancelEditingInfection"
                :disabled="isSaving"
              >
                Cancel
              </button>
              <button
                class="btn btn-sm btn-success"
                @click="saveInfectionAndFrequencies"
                :disabled="isSaving"
              >
                <span v-if="!isSaving">Save Changes</span>
                <span v-else class="loading loading-spinner loading-xs"></span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Mitigations Configuration -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <div
            class="flex items-center justify-between cursor-pointer"
            @click="toggleSection('mitigations')"
          >
            <h3 class="card-title text-lg flex items-center gap-2">
              <i-ph-shield class="w-5 h-5 text-success" />
              Mitigation Strategies
            </h3>
            <i-ph-caret-down
              class="w-5 h-5 transition-transform"
              :class="{ 'rotate-180': expandedSections.mitigations }"
            />
          </div>

          <!-- Loading State -->
          <div
            v-if="mitigationsLoading && expandedSections.mitigations"
            class="flex justify-center py-8"
          >
            <span class="loading loading-spinner loading-lg"></span>
          </div>

          <!-- Error State -->
          <div
            v-else-if="mitigationsError && expandedSections.mitigations"
            class="alert alert-error mt-4"
          >
            <i-ph-warning-circle class="w-5 h-5" />
            <span>{{ mitigationsError }}</span>
          </div>

          <!-- Collapsed State -->
          <div
            v-else-if="!expandedSections.mitigations"
            class="text-sm text-base-content/60 italic mt-2"
          >
            Click to expand
          </div>

          <!-- Expanded Content -->
          <div v-else class="space-y-4 mt-4">
            <!-- Edit/Save buttons -->
            <div class="flex justify-end gap-2">
              <button
                v-if="!isEditingMitigations"
                class="btn btn-sm btn-primary"
                @click="isEditingMitigations = true"
              >
                <i-ph-pencil class="w-4 h-4" />
                Edit Mitigations
              </button>
              <template v-else>
                <button
                  class="btn btn-sm btn-ghost"
                  @click="cancelEditingMitigations"
                  :disabled="mitigationsLoading"
                >
                  Cancel
                </button>
                <button
                  class="btn btn-sm btn-success"
                  @click="saveEditingMitigations"
                  :disabled="mitigationsLoading"
                >
                  <span v-if="!mitigationsLoading">Done</span>
                  <span
                    v-else
                    class="loading loading-spinner loading-xs"
                  ></span>
                </button>
              </template>
            </div>

            <!-- View Mode: Summary of active mitigations -->
            <div v-if="!isEditingMitigations">
              <div v-if="mitigations.length === 0" class="text-center py-8">
                <i-ph-shield-slash
                  class="w-12 h-12 mx-auto mb-3 opacity-50 text-base-content/50"
                />
                <p class="text-base-content/60">
                  No mitigation strategies active
                </p>
                <p class="text-sm text-base-content/50 mt-1">
                  Click "Edit Mitigations" to add strategies
                </p>
              </div>

              <div v-else class="space-y-3">
                <!-- Self-Isolation Summary -->
                <div
                  v-if="selfIsolation"
                  class="bg-base-200 rounded-lg p-4 space-y-2"
                  :class="{ 'opacity-60': !selfIsolation.isActive }"
                >
                  <div class="flex items-center gap-2">
                    <i-ph-house class="w-5 h-5" />
                    <span class="font-medium">Self-Isolation</span>
                    <span
                      v-if="selfIsolation.isActive"
                      class="badge badge-success badge-sm"
                      >Active</span
                    >
                    <span v-else class="badge badge-warning badge-sm"
                      >Inactive</span
                    >
                  </div>
                  <div class="text-sm text-base-content/70 space-y-1">
                    <div class="flex justify-between">
                      <span>Compliance Rate:</span>
                      <span class="font-medium">{{
                        formatMitigationPercent(
                          selfIsolation.complianceRate ?? 0.7,
                        )
                      }}</span>
                    </div>
                    <div class="flex justify-between">
                      <span>Symptom Threshold:</span>
                      <span class="font-medium">{{
                        formatMitigationPercent(
                          selfIsolation.symptomThreshold ?? 0.3,
                        )
                      }}</span>
                    </div>
                  </div>
                </div>

                <!-- Mask Wearing Summary -->
                <div
                  v-if="maskWearing"
                  class="bg-base-200 rounded-lg p-4 space-y-2"
                  :class="{ 'opacity-60': !maskWearing.isActive }"
                >
                  <div class="flex items-center gap-2">
                    <i-ph-mask-happy class="w-5 h-5" />
                    <span class="font-medium">Mask Wearing</span>
                    <span
                      v-if="maskWearing.isActive"
                      class="badge badge-success badge-sm"
                      >Active</span
                    >
                    <span v-else class="badge badge-warning badge-sm"
                      >Inactive</span
                    >
                  </div>
                  <div class="text-sm text-base-content/70 space-y-1">
                    <div class="flex justify-between">
                      <span>Adoption Rate:</span>
                      <span class="font-medium">{{
                        formatMitigationPercent(maskWearing.adoptionRate ?? 0.6)
                      }}</span>
                    </div>
                  </div>
                </div>

                <!-- Vaccination Summary -->
                <div
                  v-if="vaccination"
                  class="bg-base-200 rounded-lg p-4 space-y-2"
                  :class="{ 'opacity-60': !vaccination.isActive }"
                >
                  <div class="flex items-center gap-2">
                    <i-ph-syringe class="w-5 h-5" />
                    <span class="font-medium">Vaccination</span>
                    <span
                      v-if="vaccination.isActive"
                      class="badge badge-success badge-sm"
                      >Active</span
                    >
                    <span v-else class="badge badge-warning badge-sm"
                      >Inactive</span
                    >
                  </div>
                  <div class="text-sm text-base-content/70 space-y-1">
                    <div class="flex justify-between">
                      <span>Vaccination Rate:</span>
                      <span class="font-medium">{{
                        formatMitigationPercent(
                          vaccination.vaccinationRate ?? 0.7,
                        )
                      }}</span>
                    </div>
                    <div class="flex justify-between">
                      <span>Days to Full Effectiveness:</span>
                      <span class="font-medium"
                        >{{
                          vaccination.daysToFullEffectiveness ?? 14
                        }}
                        days</span
                      >
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Edit Mode: Full mitigation controls -->
            <div v-else class="space-y-4">
              <!-- Self-Isolation -->
              <div
                class="bg-base-200 rounded-lg"
                :class="{
                  'opacity-60':
                    hasMitigation('SelfIsolation') &&
                    !isMitigationActive('SelfIsolation'),
                }"
              >
                <div class="flex items-center justify-between p-4">
                  <div class="flex items-center gap-2">
                    <i-ph-house class="w-5 h-5" />
                    <span class="font-medium">Self-Isolation</span>
                    <span
                      v-if="
                        hasMitigation('SelfIsolation') &&
                        isMitigationActive('SelfIsolation')
                      "
                      class="badge badge-success badge-sm"
                      >Active</span
                    >
                    <span
                      v-else-if="
                        hasMitigation('SelfIsolation') &&
                        !isMitigationActive('SelfIsolation')
                      "
                      class="badge badge-warning badge-sm"
                      >Inactive</span
                    >
                  </div>
                  <div class="flex gap-2">
                    <!-- Toggle Active/Inactive -->
                    <button
                      v-if="hasMitigation('SelfIsolation')"
                      @click="toggleMitigation('SelfIsolation')"
                      class="btn btn-sm btn-circle"
                      :class="
                        isMitigationActive('SelfIsolation')
                          ? 'btn-warning'
                          : 'btn-success'
                      "
                      :title="
                        isMitigationActive('SelfIsolation')
                          ? 'Deactivate'
                          : 'Activate'
                      "
                    >
                      <i-ph-pause
                        v-if="isMitigationActive('SelfIsolation')"
                        class="w-5 h-5"
                      />
                      <i-ph-play v-else class="w-5 h-5" />
                    </button>
                    <!-- Delete -->
                    <button
                      v-if="hasMitigation('SelfIsolation')"
                      @click="deleteMitigation('SelfIsolation')"
                      class="btn btn-sm btn-circle btn-error"
                      title="Delete"
                    >
                      <i-ph-trash class="w-5 h-5" />
                    </button>
                    <!-- Add -->
                    <button
                      v-if="!hasMitigation('SelfIsolation')"
                      @click="toggleMitigation('SelfIsolation')"
                      class="btn btn-sm btn-circle btn-primary"
                      title="Add"
                    >
                      <i-ph-plus class="w-5 h-5" />
                    </button>
                  </div>
                </div>
                <div
                  v-if="hasMitigation('SelfIsolation')"
                  class="px-4 pb-4 pt-0"
                >
                  <div class="space-y-4">
                    <div>
                      <label class="label">
                        <span class="label-text"
                          >Compliance Rate ({{
                            formatMitigationPercent(
                              selfIsolation?.complianceRate ?? 0.7,
                            )
                          }})</span
                        >
                        <span class="label-text-alt"
                          >% of symptomatic agents who will self-isolate</span
                        >
                      </label>
                      <input
                        type="range"
                        min="0"
                        max="100"
                        :value="(selfIsolation?.complianceRate ?? 0.7) * 100"
                        @input="updateSelfIsolation('complianceRate', $event)"
                        class="range range-primary range-sm"
                      />
                    </div>

                    <div>
                      <label class="label">
                        <span class="label-text"
                          >Symptom Threshold ({{
                            formatMitigationPercent(
                              selfIsolation?.symptomThreshold ?? 0.3,
                            )
                          }})</span
                        >
                        <span class="label-text-alt"
                          >Minimum symptom severity to trigger isolation</span
                        >
                      </label>
                      <input
                        type="range"
                        min="0"
                        max="100"
                        :value="(selfIsolation?.symptomThreshold ?? 0.3) * 100"
                        @input="updateSelfIsolation('symptomThreshold', $event)"
                        class="range range-primary range-sm"
                      />
                      <div class="flex justify-between text-xs px-2 mt-1">
                        <span>Mild</span>
                        <span>Severe</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Mask Wearing -->
              <div
                class="bg-base-200 rounded-lg"
                :class="{
                  'opacity-60':
                    hasMitigation('MaskWearing') &&
                    !isMitigationActive('MaskWearing'),
                }"
              >
                <div class="flex items-center justify-between p-4">
                  <div class="flex items-center gap-2">
                    <i-ph-mask-happy class="w-5 h-5" />
                    <span class="font-medium">Mask Wearing</span>
                    <span
                      v-if="
                        hasMitigation('MaskWearing') &&
                        isMitigationActive('MaskWearing')
                      "
                      class="badge badge-success badge-sm"
                      >Active</span
                    >
                    <span
                      v-else-if="
                        hasMitigation('MaskWearing') &&
                        !isMitigationActive('MaskWearing')
                      "
                      class="badge badge-warning badge-sm"
                      >Inactive</span
                    >
                  </div>
                  <div class="flex gap-2">
                    <!-- Toggle Active/Inactive -->
                    <button
                      v-if="hasMitigation('MaskWearing')"
                      @click="toggleMitigation('MaskWearing')"
                      class="btn btn-sm btn-circle"
                      :class="
                        isMitigationActive('MaskWearing')
                          ? 'btn-warning'
                          : 'btn-success'
                      "
                      :title="
                        isMitigationActive('MaskWearing')
                          ? 'Deactivate'
                          : 'Activate'
                      "
                    >
                      <i-ph-pause
                        v-if="isMitigationActive('MaskWearing')"
                        class="w-5 h-5"
                      />
                      <i-ph-play v-else class="w-5 h-5" />
                    </button>
                    <!-- Delete -->
                    <button
                      v-if="hasMitigation('MaskWearing')"
                      @click="deleteMitigation('MaskWearing')"
                      class="btn btn-sm btn-circle btn-error"
                      title="Delete"
                    >
                      <i-ph-trash class="w-5 h-5" />
                    </button>
                    <!-- Add -->
                    <button
                      v-if="!hasMitigation('MaskWearing')"
                      @click="toggleMitigation('MaskWearing')"
                      class="btn btn-sm btn-circle btn-primary"
                      title="Add"
                    >
                      <i-ph-plus class="w-5 h-5" />
                    </button>
                  </div>
                </div>
                <div v-if="hasMitigation('MaskWearing')" class="px-4 pb-4 pt-0">
                  <div class="space-y-4">
                    <div>
                      <label class="label">
                        <span class="label-text"
                          >Adoption Rate ({{
                            formatMitigationPercent(
                              maskWearing?.adoptionRate ?? 0.6,
                            )
                          }})</span
                        >
                        <span class="label-text-alt"
                          >% of population that will wear masks</span
                        >
                      </label>
                      <input
                        type="range"
                        min="0"
                        max="100"
                        :value="(maskWearing?.adoptionRate ?? 0.6) * 100"
                        @input="updateMaskWearing('adoptionRate', $event)"
                        class="range range-primary range-sm"
                      />
                    </div>

                    <div class="alert alert-info">
                      <i-ph-info class="w-5 h-5" />
                      <span class="text-sm"
                        >Mask effectiveness is determined by the disease
                        configuration.</span
                      >
                    </div>
                  </div>
                </div>
              </div>

              <!-- Vaccination -->
              <div
                class="bg-base-200 rounded-lg"
                :class="{
                  'opacity-60':
                    hasMitigation('Vaccination') &&
                    !isMitigationActive('Vaccination'),
                }"
              >
                <div class="flex items-center justify-between p-4">
                  <div class="flex items-center gap-2">
                    <i-ph-syringe class="w-5 h-5" />
                    <span class="font-medium">Vaccination</span>
                    <span
                      v-if="
                        hasMitigation('Vaccination') &&
                        isMitigationActive('Vaccination')
                      "
                      class="badge badge-success badge-sm"
                      >Active</span
                    >
                    <span
                      v-else-if="
                        hasMitigation('Vaccination') &&
                        !isMitigationActive('Vaccination')
                      "
                      class="badge badge-warning badge-sm"
                      >Inactive</span
                    >
                  </div>
                  <div class="flex gap-2">
                    <!-- Toggle Active/Inactive -->
                    <button
                      v-if="hasMitigation('Vaccination')"
                      @click="toggleMitigation('Vaccination')"
                      class="btn btn-sm btn-circle"
                      :class="
                        isMitigationActive('Vaccination')
                          ? 'btn-warning'
                          : 'btn-success'
                      "
                      :title="
                        isMitigationActive('Vaccination')
                          ? 'Deactivate'
                          : 'Activate'
                      "
                    >
                      <i-ph-pause
                        v-if="isMitigationActive('Vaccination')"
                        class="w-5 h-5"
                      />
                      <i-ph-play v-else class="w-5 h-5" />
                    </button>
                    <!-- Delete -->
                    <button
                      v-if="hasMitigation('Vaccination')"
                      @click="deleteMitigation('Vaccination')"
                      class="btn btn-sm btn-circle btn-error"
                      title="Delete"
                    >
                      <i-ph-trash class="w-5 h-5" />
                    </button>
                    <!-- Add -->
                    <button
                      v-if="!hasMitigation('Vaccination')"
                      @click="toggleMitigation('Vaccination')"
                      class="btn btn-sm btn-circle btn-primary"
                      title="Add"
                    >
                      <i-ph-plus class="w-5 h-5" />
                    </button>
                  </div>
                </div>
                <div v-if="hasMitigation('Vaccination')" class="px-4 pb-4 pt-0">
                  <div class="space-y-4">
                    <div>
                      <label class="label">
                        <span class="label-text"
                          >Vaccination Rate ({{
                            formatMitigationPercent(
                              vaccination?.vaccinationRate ?? 0.7,
                            )
                          }})</span
                        >
                        <span class="label-text-alt"
                          >% of population that will get vaccinated</span
                        >
                      </label>
                      <input
                        type="range"
                        min="0"
                        max="100"
                        :value="(vaccination?.vaccinationRate ?? 0.7) * 100"
                        @input="updateVaccination('vaccinationRate', $event)"
                        class="range range-primary range-sm"
                      />
                    </div>

                    <div>
                      <label class="label">
                        <span class="label-text"
                          >Days to Full Effectiveness ({{
                            vaccination?.daysToFullEffectiveness ?? 14
                          }})</span
                        >
                        <span class="label-text-alt"
                          >Time for vaccine to reach full protection</span
                        >
                      </label>
                      <input
                        type="range"
                        min="1"
                        max="30"
                        :value="vaccination?.daysToFullEffectiveness ?? 14"
                        @input="
                          updateVaccination('daysToFullEffectiveness', $event)
                        "
                        class="range range-primary range-sm"
                      />
                    </div>

                    <div class="alert alert-info">
                      <i-ph-info class="w-5 h-5" />
                      <span class="text-sm"
                        >Vaccine effectiveness is determined by the disease
                        configuration.</span
                      >
                    </div>
                  </div>
                </div>
              </div>

              <!-- Warning about data invalidation -->
              <div class="alert alert-warning" v-if="isEditingMitigations">
                <i-ph-warning class="w-5 h-5" />
                <div class="text-sm">
                  <strong>Note:</strong> Changing mitigations will delete all
                  future simulation snapshots and metrics as the outcomes will
                  be different.
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
          <div
            class="flex items-center justify-between cursor-pointer"
            @click="toggleSection('quickActions')"
          >
            <h3 class="card-title text-lg flex items-center gap-2">
              <i-ph-lightning-bold class="w-5 h-5 text-accent" />
              Quick Actions
            </h3>
            <i-ph-caret-down
              class="w-5 h-5 transition-transform"
              :class="{ 'rotate-180': expandedSections.quickActions }"
            />
          </div>
          <div v-if="expandedSections.quickActions" class="space-y-2 mt-4">
            <button
              class="btn btn-outline btn-sm w-full gap-2"
              @click="handleReset"
            >
              <i-ph-arrow-clockwise class="w-4 h-4" />
              Reset Simulation
            </button>
            <button
              class="btn btn-outline btn-sm w-full gap-2 btn-error"
              @click="handleUnload"
            >
              <i-ph-eject-simple class="w-4 h-4" />
              Unload from Memory
            </button>
          </div>
          <div v-else class="text-sm text-base-content/60 italic mt-2">
            Click to expand
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, watch, computed, onMounted } from "vue";
import type {
  SimulationDetail,
  UpdateDiseaseDto,
  Mitigation,
  UpdateMitigationDto,
} from "@/api/simulation";
import { simulationApi } from "@/api/simulation";
import { useToastStore } from "@/stores/useToastStore";

interface Props {
  simulationData: SimulationDetail | null;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  (e: "refresh"): void;
  (e: "reset"): void;
  (e: "unload"): void;
}>();

const toast = useToastStore();

const expandedSections = ref({
  disease: true,
  mapArea: true,
  population: true,
  quickActions: true,
  mitigations: true,
});

// Editing states
const isEditingDisease = ref(false);
const isEditingInfection = ref(false);
const isEditingFrequencies = ref(false);
const isEditingMitigations = ref(false);
const isSaving = ref(false);

// Editable disease properties
const diseaseEdit = reactive<UpdateDiseaseDto>({});

// Editable infection and frequency properties
const initialInfectionEdit = ref(0);
const metricFrequencyEdit = ref(10);
const snapshotFrequencyEdit = ref(100);

// Mitigation states
const mitigationsLoading = ref(false);
const mitigationsError = ref<string | null>(null);
const mitigations = ref<Mitigation[]>([]);

// Computed properties to extract specific mitigation types
const selfIsolation = computed(() =>
  mitigations.value.find((m) => m.type === "SelfIsolation"),
);

const maskWearing = computed(() =>
  mitigations.value.find((m) => m.type === "MaskWearing"),
);

const vaccination = computed(() =>
  mitigations.value.find((m) => m.type === "Vaccination"),
);

// Watch for simulation data changes to update edit values
watch(
  () => props.simulationData,
  (newData) => {
    if (newData?.disease) {
      Object.assign(diseaseEdit, {
        infectionRate: newData.disease.infectionRate,
        recoveryRate: newData.disease.recoveryRate,
        mortalityRate: newData.disease.mortalityRate,
        incubationPeriod: newData.disease.incubationPeriod,
        infectiousPeriod: newData.disease.infectiousPeriod,
        symptomaticRate: newData.disease.symptomaticRate,
        severeSymptomsRate: newData.disease.severeSymptomsRate,
        hospitalSeekingRate: newData.disease.hospitalSeekingRate,
        immunityDuration: newData.disease.immunityDuration,
        reinfectionResistance: newData.disease.reinfectionResistance,
        maskEffectiveness: newData.disease.maskEffectiveness,
        vaccineEffectiveness: newData.disease.vaccineEffectiveness,
      });
    }
    if (newData) {
      initialInfectionEdit.value = newData.initialInfectionCount;
      metricFrequencyEdit.value = newData.metricRecordingFrequency;
      snapshotFrequencyEdit.value = newData.snapshotSavingFrequency;
    }
  },
  { immediate: true },
);

function toggleSection(section: keyof typeof expandedSections.value) {
  expandedSections.value[section] = !expandedSections.value[section];
}

function formatNumber(num: number): string {
  return new Intl.NumberFormat().format(num);
}

// Disease editing functions
function startEditingDisease() {
  isEditingDisease.value = true;
}

function cancelEditingDisease() {
  isEditingDisease.value = false;
  // Reset values from props
  if (props.simulationData?.disease) {
    Object.assign(diseaseEdit, {
      infectionRate: props.simulationData.disease.infectionRate,
      recoveryRate: props.simulationData.disease.recoveryRate,
      mortalityRate: props.simulationData.disease.mortalityRate,
      incubationPeriod: props.simulationData.disease.incubationPeriod,
      infectiousPeriod: props.simulationData.disease.infectiousPeriod,
      symptomaticRate: props.simulationData.disease.symptomaticRate,
      severeSymptomsRate: props.simulationData.disease.severeSymptomsRate,
      hospitalSeekingRate: props.simulationData.disease.hospitalSeekingRate,
      immunityDuration: props.simulationData.disease.immunityDuration,
      reinfectionResistance: props.simulationData.disease.reinfectionResistance,
      maskEffectiveness: props.simulationData.disease.maskEffectiveness,
      vaccineEffectiveness: props.simulationData.disease.vaccineEffectiveness,
    });
  }
}

async function saveDisease() {
  if (!props.simulationData) return;

  isSaving.value = true;
  try {
    const response = await simulationApi.updateDisease(
      props.simulationData.id,
      diseaseEdit,
    );

    toast.show(
      response.message +
        " - Please reset the simulation for changes to take effect.",
      "success",
    );

    isEditingDisease.value = false;
    emit("refresh");
  } catch (error: any) {
    toast.show(
      "Failed to update disease: " + (error.response?.data || error.message),
      "error",
    );
  } finally {
    isSaving.value = false;
  }
}

// Initial infection and frequencies editing functions
function cancelEditingInfection() {
  isEditingInfection.value = false;
  // Reset values from props
  if (props.simulationData) {
    initialInfectionEdit.value = props.simulationData.initialInfectionCount;
    metricFrequencyEdit.value = props.simulationData.metricRecordingFrequency;
    snapshotFrequencyEdit.value = props.simulationData.snapshotSavingFrequency;
  }
}

async function saveInfectionAndFrequencies() {
  if (!props.simulationData) return;

  isSaving.value = true;
  try {
    // Update initial infection count
    await simulationApi.updateInitialInfection(props.simulationData.id, {
      count: initialInfectionEdit.value,
    });

    // Update metric recording frequency
    await simulationApi.updateMetricFrequency(props.simulationData.id, {
      frequency: metricFrequencyEdit.value,
    });

    // Update snapshot saving frequency
    await simulationApi.updateSnapshotFrequency(props.simulationData.id, {
      frequency: snapshotFrequencyEdit.value,
    });

    toast.show("Simulation parameters updated successfully!", "success");
    isEditingInfection.value = false;
    emit("refresh");
  } catch (error: any) {
    toast.show(
      "Failed to update parameters: " + (error.response?.data || error.message),
      "error",
    );
  } finally {
    isSaving.value = false;
  }
}

// Quick Actions
function handleReset() {
  emit("reset");
}

function handleUnload() {
  emit("unload");
}

// Mitigation functions
onMounted(() => {
  if (props.simulationData) {
    loadMitigations();
  }
});

// Reload when simulation ID changes
watch(
  () => props.simulationData?.id,
  (newId) => {
    if (newId) {
      loadMitigations();
    }
  },
);

function cancelEditingMitigations() {
  isEditingMitigations.value = false;
  // Refetch to reset edits
  loadMitigations();
}

async function saveEditingMitigations() {
  if (!props.simulationData) return;

  mitigationsLoading.value = true;
  try {
    // Convert all mitigations to DTOs for batch update
    const mitigationDtos = mitigations.value.map((mitigation) => {
      const dto: UpdateMitigationDto = {
        type: mitigation.type as
          | "SelfIsolation"
          | "MaskWearing"
          | "Vaccination",
        name: mitigation.name,
        isActive: mitigation.isActive,
      };

      if (mitigation.type === "SelfIsolation") {
        dto.complianceRate = (mitigation as any).complianceRate ?? 0.7;
        dto.symptomThreshold = (mitigation as any).symptomThreshold ?? 0.3;
      } else if (mitigation.type === "MaskWearing") {
        dto.adoptionRate = (mitigation as any).adoptionRate ?? 0.6;
      } else if (mitigation.type === "Vaccination") {
        dto.vaccinationRate = (mitigation as any).vaccinationRate ?? 0.7;
        dto.daysToFullEffectiveness =
          (mitigation as any).daysToFullEffectiveness ?? 14;
      }

      return dto;
    });

    // Send batch update - this replaces ALL mitigations
    const updated = await simulationApi.updateMitigations(
      props.simulationData.id,
      mitigationDtos,
    );

    // Update local state with response
    mitigations.value = updated;

    isEditingMitigations.value = false;

    toast.show(
      "Mitigation changes saved successfully! Note: Changes will affect future simulation outcomes.",
      "success",
    );
  } catch (error: any) {
    toast.show(
      "Failed to save mitigations: " + (error.response?.data || error.message),
      "error",
    );
  } finally {
    mitigationsLoading.value = false;
  }
}

async function loadMitigations() {
  if (!props.simulationData) return;

  try {
    mitigationsLoading.value = true;
    mitigationsError.value = null;
    mitigations.value = await simulationApi.getMitigations(
      props.simulationData.id,
    );
  } catch (err: any) {
    mitigationsError.value =
      err.response?.data?.message || "Failed to load mitigations";
    toast.show(mitigationsError.value ?? "Failed to load mitigations", "error");
  } finally {
    mitigationsLoading.value = false;
  }
}

function hasMitigation(type: string): boolean {
  const checkArray = mitigations.value;
  return checkArray.some((m) => m.type === type);
}

function isMitigationActive(type: string): boolean {
  const checkArray = mitigations.value;
  const mitigation = checkArray.find((m) => m.type === type);
  return mitigation ? mitigation.isActive : false;
}

function formatMitigationPercent(value: number): string {
  return `${Math.round(value * 100)}%`;
}

function toggleMitigation(
  type: "SelfIsolation" | "MaskWearing" | "Vaccination",
) {
  if (!isEditingMitigations.value) return;
  const existing = mitigations.value.find((m) => m.type === type);
  if (existing) {
    // Toggle active/inactive
    existing.isActive = !existing.isActive;
  } else {
    // Add new mitigation with defaults
    const newMitigation = createDefaultMitigation(type);
    mitigations.value.push(newMitigation);
  }
}

function deleteMitigation(
  type: "SelfIsolation" | "MaskWearing" | "Vaccination",
) {
  if (!isEditingMitigations.value) return;

  // Remove mitigation completely from array
  mitigations.value = mitigations.value.filter((m) => m.type !== type);
}

function createDefaultMitigation(
  type: "SelfIsolation" | "MaskWearing" | "Vaccination",
): Mitigation {
  const base = {
    id: -Date.now(), // Temporary negative ID for new items
    type,
    simulationId: props.simulationData?.id || 0,
    isActive: true,
    createdAt: new Date().toISOString(),
    modifiedAt: new Date().toISOString(),
  };

  if (type === "SelfIsolation") {
    return {
      ...base,
      name: "Self-Isolation Protocol",
      complianceRate: 0.7,
      symptomThreshold: 0.3,
    } as any;
  } else if (type === "MaskWearing") {
    return {
      ...base,
      name: "Mask Wearing Policy",
      adoptionRate: 0.6,
    } as any;
  } else {
    // Vaccination
    return {
      ...base,
      name: "Vaccination Campaign",
      vaccinationRate: 0.7,
      daysToFullEffectiveness: 14,
    } as any;
  }
}

function updateSelfIsolation(field: string, event: Event) {
  if (!isEditingMitigations.value) return;

  const value = parseFloat((event.target as HTMLInputElement).value) / 100;
  const mitigation = mitigations.value.find((m) => m.type === "SelfIsolation");

  if (mitigation) {
    if (field === "complianceRate") {
      (mitigation as any).complianceRate = value;
    } else if (field === "symptomThreshold") {
      (mitigation as any).symptomThreshold = value;
    }
  }
}

function updateMaskWearing(field: string, event: Event) {
  if (!isEditingMitigations.value) return;

  const value = parseFloat((event.target as HTMLInputElement).value) / 100;
  const mitigation = mitigations.value.find((m) => m.type === "MaskWearing");

  if (mitigation && field === "adoptionRate") {
    (mitigation as any).adoptionRate = value;
  }
}

function updateVaccination(field: string, event: Event) {
  if (!isEditingMitigations.value) return;

  const value =
    field === "daysToFullEffectiveness"
      ? parseInt((event.target as HTMLInputElement).value)
      : parseFloat((event.target as HTMLInputElement).value) / 100;

  const mitigation = mitigations.value.find((m) => m.type === "Vaccination");

  if (mitigation) {
    if (field === "vaccinationRate") {
      (mitigation as any).vaccinationRate = value;
    } else if (field === "daysToFullEffectiveness") {
      (mitigation as any).daysToFullEffectiveness = value;
    }
  }
}
</script>
