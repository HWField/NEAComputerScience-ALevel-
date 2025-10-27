import { createRouter, createWebHistory } from "vue-router";
import HomeView from "@/views/home.vue";
import AuthenticationView from "@/views/authentication.vue";
import Error from "@/views/error.vue";
import { useUserStore } from "@/stores/useUserStore";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "home",
      component: HomeView,
    },
    {
      path: "/dashboard",
      name: "dashboard",
      component: HomeView,
      meta: { requiresAuth: true }, // Protect this route
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
  const userStore = useUserStore(); // Access store inside navigation guard
  if (to.meta.requiresAuth && !userStore.isAuthenticated) {
    next({ name: "login", query: { redirect: to.fullPath } }); // Redirect to login if not authenticated
  } else {
    next(); // Allow navigation
  }
});
export default router;
