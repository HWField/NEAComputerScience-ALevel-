<template>
  <div>
    <p>Simulation ID: {{ id }}</p>
  </div>
</template>

<script lang="ts" setup>
import { onMounted } from "vue";

const props = defineProps<{
  id: string;
}>();

onMounted(() => {});
</script>
