import { fileURLToPath, URL } from "node:url";
import { visualizer } from "rollup-plugin-visualizer";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import tailwind from "@tailwindcss/vite";
import autoprefixer from "autoprefixer";
import vueDevTools from "vite-plugin-vue-devtools";
import Icons from "unplugin-icons/vite";
import IconsResolver from "unplugin-icons/resolver";
import Components from "unplugin-vue-components/vite";
import AutoImport from "unplugin-auto-import/vite";

// https://vite.dev/config/
export default defineConfig({
  server: {
    host: true, // listen on all interfaces (0.0.0.0)
    port: 5173,
    // hmr: {
    //   protocol: "ws",
    //   host: "henryfield.net",
    // },
  },
  plugins: [
    vue(),
    vueDevTools(),
    visualizer(),
    tailwind(),
    Icons({
      autoInstall: true,
      compiler: "vue3",
    }),
    Components({
      resolvers: [
        IconsResolver({
          prefix: "i",
        }),
      ],
      dts: true,
    }),
    AutoImport({
      imports: [
        "vue",
        "vue-router",
        "pinia", // Auto-import defineStore, storeToRefs, etc.
        {
          axios: [["default", "axios"]],
        },
      ],
      dts: true,
      dirs: ["./src/stores/**"],
      eslintrc: {
        enabled: true, // Generates .eslintrc-auto-import.json for ESLint
      },
    }),
  ],
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
  // preview: {
  //   allowedHosts: ["henryfield.net"],
  // },
});
