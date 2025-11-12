import { onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { useMidiController } from "./useMidiController";
import type { MidiEventHandlers } from "@/types/midi";

export function useMidiNavigation() {
  const midi = useMidiController();
  const router = useRouter();

  let currentFocusIndex = 0;
  let focusableElements: HTMLElement[] = [];

  function getFocusableElements(): HTMLElement[] {
    const selector = [
      "button:not([disabled])",
      "a[href]",
      'input:not([disabled]):not([type="hidden"])',
      "select:not([disabled])",
      "textarea:not([disabled])",
      '[tabindex]:not([tabindex="-1"])',
    ].join(", ");

    return Array.from(document.querySelectorAll<HTMLElement>(selector)).filter(
      (el) => {
        // Filter out hidden elements
        const style = window.getComputedStyle(el);
        return style.display !== "none" && style.visibility !== "hidden";
      }
    );
  }

  function updateFocusableElements(): void {
    focusableElements = getFocusableElements();

    // Find current focus index
    const activeElement = document.activeElement as HTMLElement;
    const index = focusableElements.indexOf(activeElement);
    if (index !== -1) {
      currentFocusIndex = index;
    }
  }

  function focusNext(): void {
    updateFocusableElements();
    if (focusableElements.length === 0) return;

    currentFocusIndex = (currentFocusIndex + 1) % focusableElements.length;
    focusableElements[currentFocusIndex]?.focus();
  }

  function focusPrevious(): void {
    updateFocusableElements();
    if (focusableElements.length === 0) return;

    currentFocusIndex = currentFocusIndex - 1;
    if (currentFocusIndex < 0) {
      currentFocusIndex = focusableElements.length - 1;
    }
    focusableElements[currentFocusIndex]?.focus();
  }

  function clickFocused(): void {
    const activeElement = document.activeElement as HTMLElement;
    if (activeElement && activeElement !== document.body) {
      activeElement.click();
    }
  }

  function navigateBack(): void {
    router.back();
  }

  // Set midi event handlers that are called

  const handlers: MidiEventHandlers = {
    // Jog wheel: Navigate between elements
    onJog: (delta: number) => {
      if (delta > 0) {
        focusNext();
      } else if (delta < 0) {
        focusPrevious();
      }
    },

    // Encoder: Also can navigate (alternative control)
    onEncoder: (delta: number) => {
      if (delta > 0) {
        focusNext();
      } else if (delta < 0) {
        focusPrevious();
      }
    },

    // Encoder push or Select button: Trigger click (Enter)
    onEncoderPush: (pressed: boolean) => {
      if (pressed) {
        clickFocused();
      }
    },

    onButton: (button: string, pressed: boolean) => {
      if (!pressed) return;

      switch (button) {
        case "select":
          break;
        case "enter":
          // D-pad enter button activates focused element
          clickFocused();
          break;
        case "up":
          // D-pad up: Navigate to previous element
          focusPrevious();
          break;
        case "down":
          // D-pad down: Navigate to next element
          focusNext();
          break;
        case "left":
          // D-pad left: Navigate back in history
          navigateBack();
          break;
        case "right":
          // D-pad right: Navigate forward (focus next)
          focusNext();
          break;
        case "rec":
          break;
      }
    },

    // Transport controls
    onTransport: (action: string) => {
      switch (action) {
        case "stop":
          navigateBack();
          break;
        case "rewind":
          navigateBack();
          break;
        case "forward":
          focusNext();
          break;
      }
    },

    // Function buttons: Custom shortcuts
    onFunction: (number: number, pressed: boolean) => {
      if (!pressed) return;

      switch (number) {
        case 1:
          // F1: Home
          router.push("/");
          break;
        case 2:
          // F2: Settings
          router.push("/settings");
          break;
        case 3:
          // F3: Help
          router.push("/help");
          break;
        case 4:
          // F4: Admin
          router.push("/admin");
          break;
        case 5:
          // F5
          break;
        case 6:
          break;
      }
    },
  };

  function register(): void {
    midi.registerMode("navigation", handlers);
    // Light up  F buttons to show functions are availible
    midi.setButtonLed(7, "on"); // F1
    midi.setButtonLed(8, "on"); // F2
    midi.setButtonLed(9, "on"); // F3
    midi.setButtonLed(10, "on"); // F4
  }

  function unregister(): void {
    midi.unregisterMode("navigation");
  }

  // Auto-register on mount
  onMounted(() => {
    register();
  });

  onUnmounted(() => {
    unregister();
  });

  return {
    register,
    unregister,
    focusNext,
    focusPrevious,
    clickFocused,
  };
}
