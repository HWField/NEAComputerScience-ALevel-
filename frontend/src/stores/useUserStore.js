import { defineStore } from "pinia";
import { ref, watch } from "vue";
import { useRouter } from "vue-router";
import axios from "../api/axios";

export const useUserStore = defineStore(
  "userStore",
  () => {
    const isAuthenticated = ref(false);
    const username = ref("");
    const role = ref("");
    const theme = ref("light");
    const streamerMode = ref(false);
    const avatar = ref("");
    const token = ref("");
    const refreshToken = ref("");

    const router = useRouter();

    function UpdateUser(data) {
      username.value = data.username;
      role.value = data.role;
    }

    async function FetchUser() {
      try {
        const response = await axios.get("/profile", {});
        isAuthenticated.value = true;
        UpdateUser(response.data);
        if (response.data.theme) {
          applyTheme(response.data.theme);
        }

        return true;
      } catch (error) {
        console.error("Error fetching user data:", error);
        isAuthenticated.value = false;
        return false;
      }
    }

    watch(
      theme,
      (newTheme) => {
        if (newTheme) {
          document.documentElement.setAttribute("data-theme", newTheme);
        }
      },
      { immediate: true }
    );

    async function Login(userData, redirect = null) {
      token.value = userData.accessToken;
      refreshToken.value = userData.refreshToken;
      isAuthenticated.value = true;
      FetchUser();

      if (redirect) {
        router.replace(decodeURIComponent(redirect));
      } else {
        router.replace("/"); // Default route after login
      }
    }

    async function Logout() {
      isAuthenticated.value = false;
      username.value = "";
      role.value = "";
      token.value = "";
      theme.value = "light";
      refreshToken.value = "";
      router.replace("/login");
    }

    return {
      isAuthenticated,
      username,
      role,
      theme,
      streamerMode,
      token,
      refreshToken,
      Login,
      Logout,
      UpdateUser,
      FetchUser,
    };
  },

  // Enable persistence
  {
    persist: {
      enabled: true,
      strategies: [
        {
          key: "userStore", // LocalStorage key
          storage: localStorage,
          paths: ["token", "refreshToken"], // Only persist the tokens, not the entire state
        },
      ],
    },
  }
);
