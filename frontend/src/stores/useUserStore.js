import { defineStore } from "pinia";
import { ref } from "vue";
import { useRouter } from "vue-router";
import axios from "../api/axios";

export const useUserStore = defineStore(
  "userStore",
  () => {
    const isAuthenticated = ref(false);
    const username = ref("");
    const email = ref("");
    const role = ref("");
    const avatar = ref("");
    const token = ref("");
    const refreshToken = ref("");

    const router = useRouter();

    function UpdateUser(data) {
      username.value = data.username;
      email.value = data.email;
      role.value = data.userStatus;
    }

    async function saveUser(updates = {}) {
      try {
        const response = await axios.post("/update", {
          operation: "user",
          ...updates,
        });
        // Update local state with any returned values
        if (response.data.status === "success") {
          // Update local state for each field that was updated
          Object.keys(updates).forEach((key) => {
            if (key === "username" && updates.username) {
              username.value = updates.username;
            } else if (key === "email" && updates.email) {
              email.value = updates.email;
            }
          });
          return true;
        }
        return false;
      } catch (error) {
        return false;
      }
    }

    async function FetchUser() {
      try {
        const response = await axios.post("/load", {
          operation: "user",
        });
        isAuthenticated.value = true;
        console.log("user data:", response.data);
        UpdateUser(response.data);
        return true;
      } catch (error) {
        console.error("Error fetching user data:", error);
        isAuthenticated.value = false;
        return false;
      }
    }

    async function Login(userData, redirect = null) {
      token.value = userData.access_token;
      refreshToken.value = userData.refresh_token;
      isAuthenticated.value = true;

      const graphStore = useGraphStore();
      await graphStore.fetchGraph();
      FetchUser();

      if (redirect) {
        router.replace(decodeURIComponent(redirect));
      } else {
        router.replace("/dashboard"); // Default route after login
      }
    }

    async function Logout() {
      isAuthenticated.value = false;
      username.value = "";
      email.value = "";
      role.value = "";
      token.value = "";
      refreshToken.value = "";
      router.replace("/login");
    }

    return {
      isAuthenticated,
      username,
      email,
      role,
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
