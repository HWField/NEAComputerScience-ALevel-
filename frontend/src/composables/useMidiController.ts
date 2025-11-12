import { ref, computed, watch, type Ref } from "vue";
import { useRouter } from "vue-router";
import type {
  MidiControlEvent,
  MidiControllerState,
  MidiDeviceInfo,
  ConnectionStatus,
  ControlMode,
  MidiEventCallback,
  UnsubscribeFunction,
  MidiEventHandlers,
  LcdUpdate,
  FaderPositionUpdate,
  SegmentDisplayUpdate,
} from "@/types/midi";
import {
  MIDI_CC,
  MidiMessageType,
  createSegmentDisplayData,
} from "@/types/midi";

// singelleton instance so that all components share the same MIDI controller state
let instance: ReturnType<typeof createMidiController> | null = null;

export function useMidiController() {
  if (!instance) {
    instance = createMidiController();
  }
  return instance;
}

function createMidiController() {
  const router = useRouter();

  const state = ref<MidiControllerState>({
    status: "disconnected",
    device: null,
    mode: "navigation",
    error: null,
    faderValue: 0,
    jogValue: 0,
    encoderValue: 0,
  });

  const midiAccess = ref<MIDIAccess | null>(null);
  const inputDevice = ref<MIDIInput | null>(null);
  const outputDevice = ref<MIDIOutput | null>(null);
  const subscribers = ref<Set<MidiEventCallback>>(new Set());
  const modeHandlers = ref<Map<ControlMode, MidiEventHandlers>>(new Map());

  const isConnected = computed(() => state.value.status === "connected");
  const currentMode = computed(() => state.value.mode);

  // Connection Management
  async function connect(): Promise<void> {
    try {
      state.value.status = "connecting";
      state.value.error = null;

      // Request MIDI access
      if (!navigator.requestMIDIAccess) {
        throw new Error("Web MIDI API not supported in this browser");
      }

      const access = await navigator.requestMIDIAccess({ sysex: true });
      midiAccess.value = access;

      // Find X-Touch One (look for "X-TOUCH ONE" in the name) as that is the controller I have
      const inputs: MIDIInput[] = [];
      const outputs: MIDIOutput[] = [];

      access.inputs.forEach((input) => inputs.push(input));
      access.outputs.forEach((output) => outputs.push(output));

      const input = inputs.find(
        (device) =>
          device.name?.toLowerCase().includes("x-touch") ||
          device.name?.toLowerCase().includes("xtouch")
      );

      const output = outputs.find(
        (device) =>
          device.name?.toLowerCase().includes("x-touch") ||
          device.name?.toLowerCase().includes("xtouch")
      );

      if (!input || !output) {
        // Try to use any available MIDI device for development
        if (inputs.length > 0 && outputs.length > 0) {
          console.warn(
            "X-Touch One not found, using first available MIDI device"
          );
          inputDevice.value = inputs[0]!;
          outputDevice.value = outputs[0]!;
        } else {
          throw new Error(
            "No MIDI devices found. Please connect your Behringer X-Touch One."
          );
        }
      } else {
        inputDevice.value = input;
        outputDevice.value = output;
      }

      // Set up message handler
      if (inputDevice.value) {
        inputDevice.value.onmidimessage = handleMidiMessage;

        state.value.device = {
          id: inputDevice.value.id,
          name: inputDevice.value.name || "Unknown",
          manufacturer: inputDevice.value.manufacturer || "Unknown",
          state: inputDevice.value.state,
          connection: inputDevice.value.connection,
        };

        state.value.status = "connected";
        console.log("✅ MIDI Controller connected:", state.value.device.name);

        // Initialize display
        sendLcdText({ line1: "Connected", line2: "Ready" });
      }

      // Listen for device changes
      access.onstatechange = handleDeviceStateChange;
    } catch (err: any) {
      state.value.status = "error";
      state.value.error = err.message;
      console.error("❌ MIDI connection failed:", err);
      throw err;
    }
  }

  /**
   * Disconnect from MIDI device
   */
  function disconnect(): void {
    if (inputDevice.value) {
      inputDevice.value.onmidimessage = null;
    }

    inputDevice.value = null;
    outputDevice.value = null;
    state.value.status = "disconnected";
    state.value.device = null;

    console.log("🔌 MIDI Controller disconnected");
  }

  // Handle MIDI device connection state changes

  function handleDeviceStateChange(event: MIDIConnectionEvent): void {
    const port = event.port;
    if (!port) return;

    console.log(`MIDI device ${port.state}: ${port.name}`);

    if (port.state === "disconnected" && port.id === state.value.device?.id) {
      state.value.status = "disconnected";
      state.value.device = null;
      state.value.error = "Device disconnected";
    }
  }

  // Message Parsing
  function handleMidiMessage(event: MIDIMessageEvent): void {
    if (!event.data || event.data.length < 2) return;
    // console.error("MIDI Message:", event.data);
    const data = event.data;
    const status = data[0];
    const data1 = data[1];
    const data2 = data[2] ?? 0;

    if (status === undefined || data1 === undefined) return;

    const messageType = status & 0xf0;

    // Parse message into high-level event
    let controlEvent: MidiControlEvent | null = null;

    switch (messageType) {
      case MidiMessageType.CONTROL_CHANGE:
        controlEvent = parseControlChange(data1, data2);
        break;

      case MidiMessageType.NOTE_ON:
        controlEvent = parseNoteOn(data1, data2);
        break;

      case MidiMessageType.NOTE_OFF:
        controlEvent = parseNoteOff(data1, data2);
        break;
    }

    if (controlEvent) {
      // Update state
      updateStateFromEvent(controlEvent);

      // Emit to subscribers
      emitEvent(controlEvent);

      // Handle based on current mode
      handleEvent(controlEvent);
    }
  }

  /**
   * Parse Control Change messages (faders, encoders, jog wheel)
   */
  function parseControlChange(
    cc: number,
    value: number
  ): MidiControlEvent | null {
    switch (cc) {
      case MIDI_CC.FADER:
        return {
          type: "fader",
          value,
          normalized: value / 127,
        };

      case MIDI_CC.JOG_WHEEL:
        // X-Touch One jog wheel: 65 = CW (clockwise), 1 = CCW (counter-clockwise)
        const delta = value === 65 ? 1 : value === 1 ? -1 : 0;
        return {
          type: "jog",
          delta,
          value,
        };

      case MIDI_CC.ENCODER:
        // X-Touch One encoder in absolute mode: 0-127
        // In relative mode: 65 = increment, 1 = decrement
        // Assume relative mode for now
        const encoderDelta = value === 65 ? 1 : value === 1 ? -1 : 0;
        return {
          type: "encoder",
          delta: encoderDelta,
          value,
        };

      default:
        return null;
    }
  }

  // Parse Note On messages (button presses)
  function parseNoteOn(
    note: number,
    velocity: number
  ): MidiControlEvent | null {
    const pressed = velocity > 0;

    // Encoder push
    if (note === MIDI_CC.ENCODER_PUSH) {
      return { type: "encoder-push", pressed };
    }

    // Transport buttons (only trigger on press, not release)
    if (pressed) {
      if (note === MIDI_CC.BUTTON_PLAY) {
        return { type: "transport", action: "play" };
      }
      if (note === MIDI_CC.BUTTON_STOP) {
        return { type: "transport", action: "stop" };
      }
      if (note === MIDI_CC.BUTTON_RECORD) {
        return { type: "transport", action: "record" };
      }
      if (note === MIDI_CC.BUTTON_REWIND) {
        return { type: "transport", action: "rewind" };
      }
      if (note === MIDI_CC.BUTTON_FORWARD) {
        return { type: "transport", action: "forward" };
      }
    }

    // Function buttons (F1-F6)
    if (note >= MIDI_CC.BUTTON_F1 && note <= MIDI_CC.BUTTON_F6) {
      const fnNumber = note - MIDI_CC.BUTTON_F1 + 1;
      return { type: "function", number: fnNumber, pressed };
    }

    // Generic buttons
    const buttonNames: Record<number, string> = {
      [MIDI_CC.BUTTON_REC]: "rec",
      [MIDI_CC.BUTTON_SOLO]: "solo",
      [MIDI_CC.BUTTON_MUTE]: "mute",
      [MIDI_CC.BUTTON_SELECT]: "select",
      [MIDI_CC.BUTTON_UP]: "up",
      [MIDI_CC.BUTTON_DOWN]: "down",
      [MIDI_CC.BUTTON_LEFT]: "left",
      [MIDI_CC.BUTTON_RIGHT]: "right",
      [MIDI_CC.BUTTON_ENTER]: "enter",
    };

    const buttonName = buttonNames[note];
    if (buttonName) {
      return { type: "button", button: buttonName, pressed };
    }

    return null;
  }

  // Parse Note Off messages
  function parseNoteOff(
    note: number,
    velocity: number
  ): MidiControlEvent | null {
    // Treat as button release
    return parseNoteOn(note, 0);
  }

  // Update internal state from control event
  function updateStateFromEvent(event: MidiControlEvent): void {
    switch (event.type) {
      case "fader":
        state.value.faderValue = event.value;
        break;
      case "jog":
        state.value.jogValue = event.value;
        break;
      case "encoder":
        state.value.encoderValue = event.value;
        break;
    }
  }

  // Event Emission & Handling

  // Subscribe to MIDI events
  function subscribe(callback: MidiEventCallback): UnsubscribeFunction {
    subscribers.value.add(callback);
    return () => subscribers.value.delete(callback);
  }

  // Emit event to all subscribers
  function emitEvent(event: MidiControlEvent): void {
    subscribers.value.forEach((callback) => {
      try {
        callback(event);
      } catch (err) {
        console.error("Error in MIDI event subscriber:", err);
      }
    });
  }

  // Handle event based on current mode
  function handleEvent(event: MidiControlEvent): void {
    const handlers = modeHandlers.value.get(state.value.mode);

    if (!handlers) {
      console.warn(`No handlers registered for mode: ${state.value.mode}`);
      return;
    }

    try {
      switch (event.type) {
        case "jog":
          if (handlers.onJog) {
            console.log(`  → Calling onJog(${event.delta}, ${event.value})`);
            handlers.onJog(event.delta, event.value);
          }
          break;
        case "fader":
          if (handlers.onFader) {
            handlers.onFader(event.value, event.normalized);
          }
          break;
        case "encoder":
          if (handlers.onEncoder) {
            handlers.onEncoder(event.delta, event.value);
          }
          break;
        case "encoder-push":
          if (handlers.onEncoderPush) {
            handlers.onEncoderPush(event.pressed);
          }
          break;
        case "button":
          if (handlers.onButton) {
            handlers.onButton(event.button, event.pressed);
          }
          break;
        case "transport":
          if (handlers.onTransport) {
            handlers.onTransport(event.action);
          }
          break;
        case "function":
          if (handlers.onFunction) {
            handlers.onFunction(event.number, event.pressed);
          }
          break;
      }
    } catch (err) {
      console.error("Error handling MIDI event:", err);
    }
  }

  // Controller Mode Management

  function setMode(mode: ControlMode): void {
    const previousMode = state.value.mode;

    // Blank controller when switching modes to ensure clean slate
    if (previousMode !== mode) {
      blankController();
    }
    state.value.mode = mode;
  }

  function registerMode(mode: ControlMode, handlers: MidiEventHandlers): void {
    modeHandlers.value.set(mode, handlers);
  }

  function unregisterMode(mode: ControlMode): void {
    modeHandlers.value.delete(mode);
  }

  // Midi Output / User Feedback

  // Send MIDI message to controller
  function sendMidi(data: number[]): void {
    if (!outputDevice.value) {
      console.warn("Cannot send MIDI: no output device");
      return;
    }
    try {
      outputDevice.value.send(data);
    } catch (err) {
      console.error("Error sending MIDI:", err);
    }
  }

  // Update motorized fader position (0-127)
  function setFaderPosition(position: number): void {
    const value = Math.max(0, Math.min(127, Math.round(position)));
    sendMidi([MidiMessageType.CONTROL_CHANGE, MIDI_CC.FADER, value]);
  }

  /**
   * Send text to LCD display (via SysEx)
   * X-Touch One LCD SysEx format:
   * F0 00 20 32 41 4C 00 cc c1..c14 F7
   * cc: bits 0-2: backlight color, bit 4: invert upper, bit 5: invert lower
   * c1..c7: upper line characters (ASCII)
   * c8..c14: lower line characters (ASCII)
   */
  function sendLcdText(update: LcdUpdate): void {
    if (!outputDevice.value) {
      console.warn("Cannot send LCD text: no output device");
      return;
    }

    try {
      // Default: white backlight, no inversion
      let cc = 0x07; // white color (bits 0-2 = 111)
      if (update.clear) {
        cc = 0x00; // black/off
      }
      // Prepare 14 character slots (7 upper + 7 lower)
      const chars: number[] = new Array(14).fill(0x20); // Fill with spaces (ASCII 32)
      // Fill upper line (positions 0-6)
      if (update.line1) {
        const line1 = update.line1.substring(0, 7).padEnd(7, " ");
        for (let i = 0; i < 7; i++) {
          chars[i] = line1.charCodeAt(i);
        }
      }
      // Fill lower line (positions 7-13)
      if (update.line2) {
        const line2 = update.line2.substring(0, 7).padEnd(7, " ");
        for (let i = 0; i < 7; i++) {
          chars[7 + i] = line2.charCodeAt(i);
        }
      }
      // Build SysEx message
      const sysex = [
        0xf0, // SysEx start
        0x00,
        0x20,
        0x32, // Manufacturer ID (Behringer)
        0x41, // Device ID (X-Touch)
        0x4c, // LCD command
        0x00, // Channel/device number
        cc, // Color/invert flags
        ...chars, // 14 character bytes
        0xf7, // SysEx end
      ];
      sendMidi(sysex);
    } catch (err) {
      console.error("Error sending LCD text:", err);
    }
  }

  function sendSegmentDisplay(update: SegmentDisplayUpdate): void {
    if (!outputDevice.value) {
      console.warn("Cannot send segment display: no output device");
      return;
    }

    try {
      const segmentData = createSegmentDisplayData(update);

      // Dots: d1 controls dots for displays 0-7, d2 for displays 8-11
      // Layout: [] [] DDD. HH. MM. TTT
      // Positions (0-indexed): 0-1 blank, 2-4 days, 5-6 hours, 7-8 mins, 9-11 ticks
      const d1 = (1 << 4) | (1 << 6); // Dots at positions 4 and 6 (within d1 range 0-7)
      const d2 = 1 << 0; // Dot at position 8 (bit 0 of d2, since d2 covers positions 8-11)
      const sysex = [
        0xf0, // SysEx start
        0x00,
        0x20,
        0x32, // Manufacturer ID (Behringer)
        0x41, // Device ID (0x41 = X-Touch One, same as LCD)
        0x37, // Segment display command
        ...segmentData, // 12 segment bytes (s1..s12)
        d1, // Dots for displays 1-7
        d2, // Dots for displays 8-12
        0xf7, // SysEx end
      ];

      sendMidi(sysex);
    } catch (err) {
      console.error("Error sending segment display:", err);
    }
  }

  // button LED state
  function setButtonLed(note: number, state: "off" | "on" | "blink"): void {
    let velocity = 0;
    if (state === "on") velocity = 127;
    if (state === "blink") velocity = 64; // velocity 64 = blink

    sendMidi([MidiMessageType.NOTE_ON, note, velocity]);
  }

  // function to 'blank' controller when switching modes, turns off all lights and resets faders etc
  function blankController(): void {
    // Turn off all function buttons (F1-F6)
    for (let i = MIDI_CC.BUTTON_F1; i <= MIDI_CC.BUTTON_F6; i++) {
      setButtonLed(i, "off");
    }

    // Turn off transport buttons
    setButtonLed(MIDI_CC.BUTTON_PLAY, "off");
    setButtonLed(MIDI_CC.BUTTON_STOP, "off");
    setButtonLed(MIDI_CC.BUTTON_RECORD, "off");
    setButtonLed(MIDI_CC.BUTTON_REWIND, "off");
    setButtonLed(MIDI_CC.BUTTON_FORWARD, "off");

    // Turn off other buttons
    setButtonLed(MIDI_CC.BUTTON_REC, "off");
    setButtonLed(MIDI_CC.BUTTON_SOLO, "off");
    setButtonLed(MIDI_CC.BUTTON_MUTE, "off");
    setButtonLed(MIDI_CC.BUTTON_SELECT, "off");

    // Turn off D-pad buttons (if they have LEDs)
    setButtonLed(MIDI_CC.BUTTON_UP, "off");
    setButtonLed(MIDI_CC.BUTTON_DOWN, "off");
    setButtonLed(MIDI_CC.BUTTON_LEFT, "off");
    setButtonLed(MIDI_CC.BUTTON_RIGHT, "off");
    setButtonLed(MIDI_CC.BUTTON_ENTER, "off");

    // Reset fader to middle position (center = 64)
    setFaderPosition(64);

    // Clear LCD display
    sendLcdText({
      line1: "Ready",
      line2: "Mode: " + state.value.mode,
    });

    // Clear segment display (all segments blank/off)
    // Send raw SysEx with all blank segments (0x00 = no segments lit)
    const blankSegments = Array(12).fill(0x00);
    const sysex = [
      0xf0, // SysEx start
      0x00,
      0x20,
      0x32, // Manufacturer ID (Behringer)
      0x41, // Device ID (X-Touch One)
      0x37, // Segment display command
      ...blankSegments, // 12 blank segment bytes
      0x00, // No dots (d1)
      0x00, // No dots (d2)
      0xf7, // SysEx end
    ];
    sendMidi(sysex);
  }

  // Auto mode switch
  // Watch route changes and switch modes accordingly
  watch(
    () => router.currentRoute.value.name,
    (routeName) => {
      if (routeName === "simulation") {
        setMode("simulation");
      } else {
        setMode("navigation");
      }
    },
    { immediate: true }
  );

  return {
    // State
    state: computed(() => state.value),
    isConnected,
    currentMode,

    // Connection
    connect,
    disconnect,

    // Events
    subscribe,

    // Modes
    setMode,
    registerMode,
    unregisterMode,
    blankController,

    // Output
    setFaderPosition,
    sendLcdText,
    sendSegmentDisplay,
    setButtonLed,
    sendMidi,
  };
}
export type MidiController = ReturnType<typeof useMidiController>;
