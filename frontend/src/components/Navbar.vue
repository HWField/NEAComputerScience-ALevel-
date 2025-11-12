<template>
  <div class="navbar bg-base-100 shadow-sm">
    <div class="flex-1">
      <a @click="Home" class="btn btn-ghost text-xl">Epedemic simulation</a>
    </div>
    <div class="flex gap-2 items-center">
      <MidiStatus :showDetails="true" />
      <div class="dropdown dropdown-end">
        <div tabindex="0" role="button" class="btn btn-ghost btn-circle avatar">
          <div class="w-10 rounded-full">
            <img
              alt="Tailwind CSS Navbar component"
              src="https://img.daisyui.com/images/stock/photo-1534528741775-53994a69daeb.webp"
            />
          </div>
        </div>
        <ul
          tabindex="-1"
          class="menu menu-sm dropdown-content bg-base-100 rounded-box z-1 mt-3 w-52 p-2 shadow"
        >
          <li><a @click="() => router.push('/help')">Help</a></li>
          <li v-if="userStore.role === 'Admin'">
            <a @click="() => router.push('/admin')">Admin</a>
          </li>
          <li><a @click="Settings">Settings</a></li>
          <li>
            <a @click="logout" class="text-error">Logout</a>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useUserStore } from "@/stores/useUserStore";
import axios from "../api/axios";
import { useRouter } from "vue-router";
import { useToastStore } from "@/stores/useToastStore";
import MidiStatus from "@/components/MidiStatus.vue";

const toast = useToastStore();
const router = useRouter();
const userStore = useUserStore();

function Settings() {
  router.push("/settings");
}
async function logout() {
  await axios.post("/Auth/logout", {
    refreshToken: userStore.refreshToken,
  });
  toast.show("Logged out successfully", "success");
  userStore.Logout();
}
function Home() {
  router.push("/");
}
</script>
