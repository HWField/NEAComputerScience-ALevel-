import axios from "axios";
import router from "@/router";
import { useUserStore } from "@/stores/useUserStore";

// Create an Axios instance targeting the backend API
const BASE_URL = "http://127.0.0.1:8000";

const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Function to get the base URL
export const getBaseUrl = () => BASE_URL;

// Function to refresh token
const refreshAccessToken = async () => {
  console.error("Refreshing token...");
  const userStore = useUserStore();

  try {
    // Create a separate axios instance to avoid interceptor loops
    const response = await axios.post(
      `${BASE_URL}/refresh`,
      {},
      {
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${userStore.refreshToken}`,
        },
      }
    );

    // Update token in store
    userStore.token = response.data.access_token;

    return response.data.access_token;
  } catch (error) {
    // If refresh token is invalid or expired, log the user out
    await userStore.InstantLogout();
    router.push("/login");
    return null;
  }
};
// Interceptor to add JWT token to request headers
api.interceptors.request.use(
  (config) => {
    const token = useUserStore().token; // Retrieve the token from Pinia store
    if (token) {
      config.headers["Authorization"] = `Bearer ${token}`; // Attach token to Authorization header
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If the error is 401 and haven't tried refreshing the token yet
    if (
      error.response &&
      error.response.status === 401 &&
      !originalRequest._retry
    ) {
      originalRequest._retry = true;

      const userStore = useUserStore();

      // Check if have a refresh token
      if (userStore.refreshToken) {
        try {
          // Try to get a new access token
          const newToken = await refreshAccessToken();

          if (newToken) {
            // Update the Authorization header
            originalRequest.headers["Authorization"] = `Bearer ${newToken}`;
            // Retry the original request
            return axios(originalRequest);
          }
        } catch (refreshError) {
          console.error("Error refreshing token:", refreshError);
          // Continue to logout handling
        }
      }

      console.error("Session expired, please log in again.");
      // If token refresh failed or no refresh token, proceed with logout
      await userStore.InstantLogout();

      // Save the original route in the query parameters
      const currentRoute = window.location.pathname;
      router.push({
        path: "/login",
        query: { redirect: currentRoute },
      });
    } else if (!error.response) {
      console.error("Cannot connect to the server");
      const errorMessage = encodeURIComponent("Cannot connect to the server");
      router.push({
        path: "/error",
        query: { message: errorMessage },
      });
    }
    return Promise.reject(error);
  }
);

export default api;
