<template>
  <div class="layout-container">
    <Navbar class="navbar-fixed" />
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import Navbar from "@/components/Navbar.vue";
</script>

<style scoped>
.layout-container {
  height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.navbar-fixed {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  width: 100%;
  z-index: 1000;
}

.main-content {
  flex: 1;
  overflow-y: auto;
  margin-top: var(--navbar-height, 65px);
}
</style>
