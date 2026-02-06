import { createRouter, createWebHistory } from "vue-router";
import DashboardView from "@/views/dashboard.vue";
import AdminView from "@/views/admin.vue";
import AuthenticationView from "@/views/authentication.vue";
import SettingsView from "@/views/settings.vue";
import SimulationView from "@/views/simulation.vue";
import Error from "@/views/error.vue";
import { useUserStore } from "@/stores/useUserStore";
import { useToastStore } from "@/stores/useToastStore";
import MainLayout from "@/layouts/MainLayout.vue";
// Creates a new router instance which components can use to navigate users around the webapp.
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      component: MainLayout,
      children: [
        {
          path: "",
          name: "dashboard",
          component: DashboardView,
          meta: { requiresAuth: true },
        },
        {
          path: "admin",
          name: "admin",
          component: AdminView,
          meta: { requiresAuth: true, requiresAdmin: true },
        },
        {
          path: "settings",
          name: "settings",
          component: SettingsView,
          meta: { requiresAuth: true },
        },
        {
          path: "simulation/:id",
          name: "simulation",
          component: SimulationView,
          meta: { requiresAuth: true },
          props: true,
        },
      ],
    },
    {
      path: "/login",
      name: "login",
      component: AuthenticationView,
    },
    {
      path: "/register",
      name: "register",
      component: AuthenticationView,
    },
    {
      path: "/error",
      name: "error",
      component: Error,
      props: (route) => ({ message: route.query.message }),
    },
    {
      path: "/:catchAll(.*)",
      name: "not-found",
      component: Error,
      props: { message: "Page not found" },
    },
  ],
});

router.beforeEach((to, from, next) => {
  const userStore = useUserStore();
  const toastStore = useToastStore();
  // Check authentication
  if (to.meta.requiresAuth && !userStore.isAuthenticated) {
    next({ name: "login", query: { redirect: to.fullPath } });
    return;
  }

  // Check admin role
  if (to.meta.requiresAdmin && userStore.role !== "Admin") {
    toastStore.show("You do not have admin privileges", "error");
    next({ name: "" }); // Redirect to home
    return;
  }

  next();
});
export default router;
