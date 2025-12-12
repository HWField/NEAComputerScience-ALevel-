<template>
  <div class="min-h-[60vh] flex items-center justify-center">
    <div class="text-center max-w-md">
      <div class="mb-6">
        <i-ph-chart-line-up class="w-24 h-24 mx-auto text-primary/30" />
      </div>
      <h2 class="text-3xl font-bold mb-3">Analysis Dashboard</h2>
      <p class="text-base-content/70 mb-6">
        This section will contain detailed analytics, charts, and statistical
        analysis of your simulation results.
      </p>
      <div class="flex flex-col gap-2 text-sm text-base-content/60">
        <div class="flex items-center gap-2 justify-center">
          <i-ph-check-circle class="w-4 h-4 text-success" />
          <span>Time-series infection graphs</span>
        </div>
        <div class="flex items-center gap-2 justify-center">
          <i-ph-check-circle class="w-4 h-4 text-success" />
          <span>R-value calculations</span>
        </div>
        <div class="flex items-center gap-2 justify-center">
          <i-ph-check-circle class="w-4 h-4 text-success" />
          <span>Mitigation effectiveness comparison</span>
        </div>
        <div class="flex items-center gap-2 justify-center">
          <i-ph-check-circle class="w-4 h-4 text-success" />
          <span>Heat maps and geographic analysis</span>
        </div>
        <div class="flex items-center gap-2 justify-center">
          <i-ph-check-circle class="w-4 h-4 text-success" />
          <span>Export capabilities for data analysis</span>
        </div>
      </div>
      <div class="mt-8">
        <div class="badge badge-info gap-2">
          <i-ph-hammer class="w-4 h-4" />
          Coming Soon
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import type { SimulationDetail } from "@/api/simulation";

interface Props {
  simulationData: SimulationDetail | null;
}

const props = defineProps<Props>();
</script>
