import { defineStore } from "pinia";

export const useToastStore = defineStore("toastStore", {
  state: () => ({
    toasts: [] as {
      id: number;
      message: string;
      type?: "success" | "error" | "info" | "warning";
    }[],
  }),
  actions: {
    show(
      message: string,
      type: "success" | "error" | "info" | "warning" = "info",
      duration = 3000
    ) {
      const id = Date.now();
      this.toasts.push({ id, message, type });
      setTimeout(() => this.remove(id), duration);
    },
    remove(id: number) {
      this.toasts = this.toasts.filter((t) => t.id !== id);
    },
  },
});
