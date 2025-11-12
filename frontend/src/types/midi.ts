export const MIDI_CC = {
  // Fader
  FADER: 70,

  // Fader Touch
  FADER_TOUCH: 110,

  // Jog Wheel (CC 88: CW=65, CCW=1)
  JOG_WHEEL: 88,

  // Encoder (top rotary)
  ENCODER: 80,
  ENCODER_PUSH: 0, // Note on message

  // Encoder Ring LEDs
  ENCODER_RING: 80,

  // Meter LEDs
  METER: 90,

  // Foot Switch
  FOOT_SWITCH: 64,

  // CHANNEL Buttons (Note On/Off messages 0-34)
  BUTTON_REC: 6,
  BUTTON_SOLO: 5,
  BUTTON_MUTE: 4,
  BUTTON_SELECT: 3,

  // DPAD BUTTONS
  BUTTON_UP: 30,
  BUTTON_DOWN: 34,
  BUTTON_LEFT: 31,
  BUTTON_RIGHT: 33,
  BUTTON_ENTER: 32,

  // Transport buttons (Note messages)
  BUTTON_REWIND: 20,
  BUTTON_FORWARD: 21,
  BUTTON_STOP: 22,
  BUTTON_PLAY: 23,
  BUTTON_RECORD: 24,

  // Function buttons (Note messages) - Only 6 buttons available
  BUTTON_F1: 7,
  BUTTON_F2: 8,
  BUTTON_F3: 9,
  BUTTON_F4: 10,
  BUTTON_F5: 11,
  BUTTON_F6: 12,
} as const;

export enum MidiMessageType {
  NOTE_ON = 0x90,
  NOTE_OFF = 0x80,
  CONTROL_CHANGE = 0xb0,
  SYSEX = 0xf0,
}

// Event Types
export type MidiControlEvent =
  | { type: "jog"; delta: number; value: number }
  | { type: "fader"; value: number; normalized: number }
  | { type: "encoder"; delta: number; value: number }
  | { type: "encoder-push"; pressed: boolean }
  | { type: "button"; button: string; pressed: boolean }
  | {
      type: "transport";
      action: "play" | "stop" | "record" | "rewind" | "forward";
    }
  | { type: "function"; number: number; pressed: boolean };

export type NavigationAction =
  | "left"
  | "right"
  | "up"
  | "down"
  | "enter"
  | "back"
  | "tab"
  | "shift-tab";

// Control Modes how MIDI input is interpreted

export type ControlMode =
  | "navigation" // Default: keyboard-like navigation
  | "simulation" // Simulation page: timeline + speed control
  | "custom"; // Per-page custom handlers

//Configuration for a control mode

export interface ModeConfig {
  mode: ControlMode;
  route?: string;
  handlers: MidiEventHandlers;
}

// Handler functions for MIDI events
export interface MidiEventHandlers {
  onJog?: (delta: number, value: number) => void;
  onFader?: (value: number, normalized: number) => void;
  onEncoder?: (delta: number, value: number) => void;
  onEncoderPush?: (pressed: boolean) => void;
  onButton?: (button: string, pressed: boolean) => void;
  onTransport?: (action: string) => void;
  onFunction?: (number: number, pressed: boolean) => void;
}

// Device State
export type ConnectionStatus =
  | "disconnected"
  | "connecting"
  | "connected"
  | "error";

export interface MidiDeviceInfo {
  id: string;
  name: string;
  manufacturer: string;
  state: MIDIPortDeviceState;
  connection: MIDIPortConnectionState;
}

export interface MidiControllerState {
  status: ConnectionStatus;
  device: MidiDeviceInfo | null;
  mode: ControlMode;
  error: string | null;
  faderValue: number;
  jogValue: number;
  encoderValue: number;
}

// LCD Display
export interface LcdUpdate {
  line1?: string;
  line2?: string;
  clear?: boolean;
}

// Segment display update
// Format: DDD HH MM TTTT (Days, Hours, Minutes, Ticks)

export interface SegmentDisplayUpdate {
  days: number; // 0-999 (3 digits)
  hours: number; // 0-99 (2 digits)
  minutes: number; // 0-99 (2 digits)
  ticks: number; // 0-9999 (4 digits)
  dots?: number[]; // Optional: which displays should show dots (0-11)
}

// 7-Segment Encoding
export const SEGMENT_PATTERNS: Record<string, number> = {
  "0": 0b00111111, // a,b,c,d,e,f
  "1": 0b00000110, // b,c
  "2": 0b01011011, // a,b,d,e,g
  "3": 0b01001111, // a,b,c,d,g
  "4": 0b01100110, // b,c,f,g
  "5": 0b01101101, // a,c,d,f,g
  "6": 0b01111101, // a,c,d,e,f,g
  "7": 0b00000111, // a,b,c
  "8": 0b01111111, // all segments
  "9": 0b01101111, // a,b,c,d,f,g
  " ": 0b00000000, // blank
  "-": 0b01000000, // g only (minus sign)
};

// Feedback Messages

// Motorized fader position (0-127)
export interface FaderPositionUpdate {
  position: number; // 0-127
}

// Button LED state
export interface ButtonLedUpdate {
  button: number;
  state: "off" | "on" | "blink";
}

// Callback for MIDI event subscriptions
export type MidiEventCallback = (event: MidiControlEvent) => void;

// Unsubscribe function returned by event subscriptions
export type UnsubscribeFunction = () => void;

// Utility Functions

// Convert a number to an array of 7-segment display bytes
// Pads with leading zeros or spaces as needed
export function numberToSegments(
  num: number,
  length: number,
  padChar: " " | "0" = "0"
): number[] {
  const str = num.toString().padStart(length, padChar);
  return str
    .split("")
    .map((char) => SEGMENT_PATTERNS[char] ?? SEGMENT_PATTERNS[" "] ?? 0);
}

// Create segment display data for simulation time
// Layout: __ DDD HH MM TTT (blank, days, hours, minutes, ticks)
// Positions: 1-2: blank, 3-5: days, 6-7: hours, 8-9: mins, 10-12: ticks
export function createSegmentDisplayData(
  update: SegmentDisplayUpdate
): number[] {
  const segments: number[] = [];

  // Positions 1-2: blank
  segments.push(SEGMENT_PATTERNS[" "] ?? 0);
  segments.push(SEGMENT_PATTERNS[" "] ?? 0);

  // Positions 3-5: Days (3 digits)
  segments.push(...numberToSegments(Math.min(999, update.days), 3));

  // Positions 6-7: Hours (2 digits)
  segments.push(...numberToSegments(Math.min(99, update.hours), 2));

  // Positions 8-9: Minutes (2 digits)
  segments.push(...numberToSegments(Math.min(99, update.minutes), 2));

  // Positions 10-12: Ticks (3 digits - showing last 3 digits only)
  const ticksDisplay = update.ticks % 1000; // Get last 3 digits
  segments.push(...numberToSegments(ticksDisplay, 3));

  return segments.slice(0, 12);
}

// Type Guards
export function isJogEvent(
  event: MidiControlEvent
): event is Extract<MidiControlEvent, { type: "jog" }> {
  return event.type === "jog";
}

export function isFaderEvent(
  event: MidiControlEvent
): event is Extract<MidiControlEvent, { type: "fader" }> {
  return event.type === "fader";
}

export function isButtonEvent(
  event: MidiControlEvent
): event is Extract<MidiControlEvent, { type: "button" }> {
  return event.type === "button";
}

export function isTransportEvent(
  event: MidiControlEvent
): event is Extract<MidiControlEvent, { type: "transport" }> {
  return event.type === "transport";
}
