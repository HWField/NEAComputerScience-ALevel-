import { fileURLToPath, URL } from "node:url";
import { visualizer } from "rollup-plugin-visualizer";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import tailwind from "@tailwindcss/vite";
import autoprefixer from "autoprefixer";
import vueDevTools from "vite-plugin-vue-devtools";

// https://vite.dev/config/
export default defineConfig({
  server: {
    host: true, // listen on all interfaces (0.0.0.0)
    port: 5173,
    hmr: {
      protocol: "ws",
      host: "henryfield.net",
    },
  },
  plugins: [vue(), vueDevTools(), visualizer(), tailwind()],
  base: "./",
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  css: {
    postcss: {
      plugins: [autoprefixer()],
    },
  },
  preview: {
    allowedHosts: ["henryfield.net"],
  },
});
