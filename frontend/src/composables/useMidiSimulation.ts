import { watch, computed, onMounted, onUnmounted, type Ref } from "vue";
import { useMidiController } from "./useMidiController";
import type { MidiEventHandlers } from "@/types/midi";
import { simulationApi } from "@/api/simulation";

interface SimulationMidiOptions {
  simulationId: Ref<number>;
  currentTick: Ref<number>;
  maxTick: Ref<number>;
  currentSpeed: Ref<number>;
  tickLength: Ref<number>;
  isRunning: Ref<boolean>;
  isComputing: Ref<boolean>;
  onReset?: () => void;
  onUnload?: () => void;
}

export function useMidiSimulation(options: SimulationMidiOptions) {
  const midi = useMidiController();

  const {
    simulationId,
    currentTick,
    maxTick,
    currentSpeed,
    tickLength,
    isRunning,
    isComputing,
    onReset,
    onUnload,
  } = options;

  // Local state for smooth control with "drag-like" behavior
  let pendingTickChange: number | null = null;
  let tickChangeDebounce: ReturnType<typeof setTimeout> | null = null;
  let localTickValue = currentTick.value; // Local state for immediate UI updates

  let pendingSpeedChange: number | null = null;
  let speedChangeDebounce: ReturnType<typeof setTimeout> | null = null;
  let localSpeedValue = currentSpeed.value; // Local state for immediate UI updates

  let lastFaderValue = 0;
  let isJogActive = false;
  let isFaderActive = false;

  async function setTick(tick: number, immediate = false): Promise<void> {
    // Update local state immediately for smooth UI
    localTickValue = tick;
    pendingTickChange = tick;

    // Clear existing timeout
    if (tickChangeDebounce) {
      clearTimeout(tickChangeDebounce);
    }

    // If immediate, commit right away
    if (immediate) {
      try {
        await simulationApi.setTick(simulationId.value, tick);
        pendingTickChange = null;
        isJogActive = false;
      } catch (err) {
        console.error("Failed to set tick:", err);
      }
      return;
    }

    // Otherwise, wait for user to stop moving (500ms of inactivity)
    tickChangeDebounce = setTimeout(async () => {
      if (pendingTickChange !== null && !isRunning.value) {
        try {
          console.log(`✅ Committing tick change: ${pendingTickChange}`);
          await simulationApi.setTick(simulationId.value, pendingTickChange);
          pendingTickChange = null;
          isJogActive = false;
        } catch (err) {
          console.error("Failed to set tick:", err);
        }
      }
    }, 500); // 500ms wait after last movement
  }

  async function setSpeed(speed: number, immediate = false): Promise<void> {
    // Update local state immediately
    localSpeedValue = speed;
    pendingSpeedChange = speed;

    if (speedChangeDebounce) {
      clearTimeout(speedChangeDebounce);
    }

    // If immediate, commit right away
    if (immediate) {
      try {
        await simulationApi.setSpeed(simulationId.value, speed);
        pendingSpeedChange = null;
        isFaderActive = false;
      } catch (err) {
        console.error("Failed to set speed:", err);
      }
      return;
    }

    // Otherwise, wait for user to stop moving (300ms of inactivity)
    speedChangeDebounce = setTimeout(async () => {
      try {
        console.log(`✅ Committing speed change: ${speed}`);
        await simulationApi.setSpeed(simulationId.value, speed);
        pendingSpeedChange = null;
        isFaderActive = false;
      } catch (err) {
        console.error("Failed to set speed:", err);
      }
    }, 300); // 300ms wait after last movement
  }

  async function togglePlayPause(): Promise<void> {
    try {
      if (isRunning.value) {
        await simulationApi.pause(simulationId.value);
      } else {
        await simulationApi.run(simulationId.value);
      }
    } catch (err) {
      console.error("Failed to toggle play/pause:", err);
    }
  }

  async function pause(): Promise<void> {
    try {
      if (isRunning.value) {
        await simulationApi.pause(simulationId.value);
      }
    } catch (err) {
      console.error("Failed to pause:", err);
    }
  }

  function jumpForward(): void {
    if (isRunning.value) return;
    const newTick = Math.min(maxTick.value, currentTick.value + 1000);
    setTick(newTick, true);
  }

  function jumpBackward(): void {
    if (isRunning.value) return;
    const newTick = Math.max(0, currentTick.value - 1000);
    setTick(newTick, true);
  }

  const handlers: MidiEventHandlers = {
    // Jog wheel: Scrub timeline
    onJog: (delta: number) => {
      // Mark jog as active
      isJogActive = true;

      // Scale delta based on zoom level (more ticks = bigger jumps)
      const scaleFactor = Math.max(1, Math.floor(maxTick.value / 10000));
      const tickDelta = delta * scaleFactor * 10; // Multiply by 10 for faster scrubbing

      const newTick = Math.max(
        0,
        Math.min(maxTick.value, localTickValue + tickDelta)
      );

      // Update with drag-like behavior (commits after 500ms of inactivity)
      setTick(newTick);
    },

    // Fader: Control simulation speed (0-10x)
    onFader: (value: number, normalized: number) => {
      if (isComputing.value) return;

      // Prevent jitter: only update if change is significant
      if (Math.abs(value - lastFaderValue) < 2) return;
      lastFaderValue = value;

      // Mark fader as active
      isFaderActive = true;

      // Map 0-127 to 0.5-10x speed (normalized is 0-1)
      // 0.5 + (normalized * 9.5) gives range 0.5 to 10
      const speed = Math.round((0.5 + normalized * 9.5) * 2) / 2;

      // Update local state and commit after inactivity
      setSpeed(speed);

      // Update LCD with current speed (immediate feedback)
      midi.sendLcdText({
        line1: "Speed",
        line2: `${speed.toFixed(1)}x`,
      });
    },

    // Encoder: Fine adjustment of speed (alternative to fader)
    onEncoder: (delta: number) => {
      if (isComputing.value) return;

      const newSpeed = Math.max(0, Math.min(10, localSpeedValue + delta * 0.5));
      setSpeed(newSpeed, true); // Immediate commit for encoder
    },

    // Transport controls
    onTransport: (action: string) => {
      switch (action) {
        case "play":
          togglePlayPause();
          break;
        case "stop":
          pause();
          break;
        case "rewind":
          jumpBackward();
          break;
        case "forward":
          jumpForward();
          break;
      }
    },

    // Function buttons: Quick actions
    onFunction: (number: number, pressed: boolean) => {
      if (!pressed) return;

      switch (number) {
        case 1:
          // F1: Jump to start (0%)
          setTick(0, true); // Immediate commit
          break;
        case 2:
          // F2: Jump to middle (50%)
          setTick(Math.floor(maxTick.value * 0.5), true); // Immediate commit
          break;
        case 3:
          // F3: Jump to end (100%)
          setTick(maxTick.value, true); // Immediate commit
          break;
        case 4:
          // F4: (Reserved for future use)
          break;
        case 5:
          // F5: Reset simulation
          if (onReset) {
            onReset();
          }
          break;
        case 6:
          // F6: Unload simulation
          if (onUnload) {
            onUnload();
          }
          break;
      }
    },
  };

  function register(): void {
    midi.registerMode("simulation", handlers);
    midi.setMode("simulation");

    // Light up all F buttons to show functions are availible
    midi.setButtonLed(7, "on"); // F1
    midi.setButtonLed(8, "on"); // F2
    midi.setButtonLed(9, "on"); // F3
    midi.setButtonLed(10, "on"); // F4
    midi.setButtonLed(11, "on"); // F5
    midi.setButtonLed(12, "on"); // F6

    // Send initial feedback
    updateFeedback();
  }

  /**
   * Unregister simulation mode handlers
   */
  function unregister(): void {
    midi.unregisterMode("simulation");
    midi.setMode("navigation");

    // Turn off all F button LEDs when leaving simulation mode
    midi.setButtonLed(7, "off"); // F1
    midi.setButtonLed(8, "off"); // F2
    midi.setButtonLed(9, "off"); // F3
    midi.setButtonLed(10, "off"); // F4
    midi.setButtonLed(11, "off"); // F5
    midi.setButtonLed(12, "off"); // F6

    // Clear any pending debounces
    if (tickChangeDebounce) clearTimeout(tickChangeDebounce);
    if (speedChangeDebounce) clearTimeout(speedChangeDebounce);
  }

  /**
   * Update MIDI controller feedback (fader position, LCD)
   */
  function updateFeedback(): void {
    // Only update fader if user isn't actively moving it
    if (!isFaderActive) {
      const faderPosition = Math.round((currentSpeed.value / 10) * 127);
      midi.setFaderPosition(faderPosition);
    }

    // Calculate simulation time breakdown
    const totalSeconds = Math.floor(currentTick.value) * tickLength.value;
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const ticks = Math.floor(currentTick.value);

    // Update segment display with time info
    midi.sendSegmentDisplay({
      days,
      hours,
      minutes,
      ticks,
    });

    // Also update LCD to verify SysEx is working
    midi.sendLcdText({
      line1: `D${days} ${hours}h${minutes}m`,
      line2: `Tick ${ticks}`,
    });

    // Update transport button LEDs based on simulation state
    if (isRunning.value) {
      midi.setButtonLed(23, "on"); // Play button solid when running
      midi.setButtonLed(22, "blink"); // Stop button flashing when running
    } else {
      midi.setButtonLed(23, "blink"); // Play button flashing when stopped (ready to play)
      midi.setButtonLed(22, "off"); // Stop button off when stopped
    }
  }

  // Sync local state with actual state when not actively being controlled
  watch(currentTick, (newTick) => {
    if (!isJogActive) {
      localTickValue = newTick;
    }
  });

  watch(currentSpeed, (newSpeed) => {
    if (!isFaderActive) {
      localSpeedValue = newSpeed;
    }
  });

  // Watch for changes and update feedback
  watch([currentTick, currentSpeed, isRunning], () => {
    updateFeedback();
  });

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
    updateFeedback,
  };
}
