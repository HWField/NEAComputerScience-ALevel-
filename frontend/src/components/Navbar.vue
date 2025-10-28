<template>
  <div class="navbar bg-base-100 shadow-sm">
    <div class="flex-1">
      <a class="btn btn-ghost text-xl">Epedemic simulation</a>
    </div>
    <div class="flex gap-2">
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
          <li>
            <a> Profile </a>
          </li>
          <li><a>Settings</a></li>
          <li>
            <a @click="logout">Logout</a>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useUserStore } from "@/stores/useUserStore";

const userStore = useUserStore();

function logout() {
  userStore.Logout();
}
</script>
