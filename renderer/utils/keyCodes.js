/**
 * KeyCodes and ScanCodes Mapping Utility for Windows / Diablo IV
 */

// uiohook keycode constants & friendly names
export const UIOHOOK_KEY_MAP = {
  // Numbers
  2: { key: '1', name: '1', scanCode: 0x02 },
  3: { key: '2', name: '2', scanCode: 0x03 },
  4: { key: '3', name: '3', scanCode: 0x04 },
  5: { key: '4', name: '4', scanCode: 0x05 },
  6: { key: '5', name: '5', scanCode: 0x06 },
  7: { key: '6', name: '6', scanCode: 0x07 },
  8: { key: '7', name: '7', scanCode: 0x08 },
  9: { key: '8', name: '8', scanCode: 0x09 },
  10: { key: '9', name: '9', scanCode: 0x0A },
  11: { key: '0', name: '0', scanCode: 0x0B },

  // Letters
  16: { key: 'q', name: 'Q', scanCode: 0x10 },
  17: { key: 'w', name: 'W', scanCode: 0x11 },
  18: { key: 'e', name: 'E', scanCode: 0x12 },
  19: { key: 'r', name: 'R', scanCode: 0x13 },
  20: { key: 't', name: 'T', scanCode: 0x14 },
  21: { key: 'y', name: 'Y', scanCode: 0x15 },
  22: { key: 'u', name: 'U', scanCode: 0x16 },
  23: { key: 'i', name: 'I', scanCode: 0x17 },
  24: { key: 'o', name: 'O', scanCode: 0x18 },
  25: { key: 'p', name: 'P', scanCode: 0x19 },
  30: { key: 'a', name: 'A', scanCode: 0x1E },
  31: { key: 's', name: 'S', scanCode: 0x1F },
  32: { key: 'd', name: 'D', scanCode: 0x20 },
  33: { key: 'f', name: 'F', scanCode: 0x21 },
  34: { key: 'g', name: 'G', scanCode: 0x22 },
  35: { key: 'h', name: 'H', scanCode: 0x23 },
  36: { key: 'j', name: 'J', scanCode: 0x24 },
  37: { key: 'k', name: 'K', scanCode: 0x25 },
  38: { key: 'l', name: 'L', scanCode: 0x26 },
  44: { key: 'z', name: 'Z', scanCode: 0x2C },
  45: { key: 'x', name: 'X', scanCode: 0x2D },
  46: { key: 'c', name: 'C', scanCode: 0x2E },
  47: { key: 'v', name: 'V', scanCode: 0x2F },
  48: { key: 'b', name: 'B', scanCode: 0x30 },
  49: { key: 'n', name: 'N', scanCode: 0x31 },
  50: { key: 'm', name: 'M', scanCode: 0x32 },

  // Functional & Special
  1: { key: 'Escape', name: 'ESC', scanCode: 0x01 },
  28: { key: 'Enter', name: 'Enter', scanCode: 0x1C },
  57: { key: 'Space', name: 'Space', scanCode: 0x39 },
  15: { key: 'Tab', name: 'Tab', scanCode: 0x0F },
  42: { key: 'ShiftLeft', name: 'L-Shift', scanCode: 0x2A },
  54: { key: 'ShiftRight', name: 'R-Shift', scanCode: 0x36 },
  29: { key: 'ControlLeft', name: 'L-Ctrl', scanCode: 0x1D },
  56: { key: 'AltLeft', name: 'L-Alt', scanCode: 0x38 },
  59: { key: 'F1', name: 'F1', scanCode: 0x3B },
  60: { key: 'F2', name: 'F2', scanCode: 0x3C },
  61: { key: 'F3', name: 'F3', scanCode: 0x3D },
  62: { key: 'F4', name: 'F4', scanCode: 0x3E },
  63: { key: 'F5', name: 'F5', scanCode: 0x3F },
  64: { key: 'F6', name: 'F6', scanCode: 0x40 },
  65: { key: 'F7', name: 'F7', scanCode: 0x41 },
  66: { key: 'F8', name: 'F8', scanCode: 0x42 },
  67: { key: 'F9', name: 'F9', scanCode: 0x43 },
  68: { key: 'F10', name: 'F10', scanCode: 0x44 },
  87: { key: 'F11', name: 'F11', scanCode: 0x57 },
  88: { key: 'F12', name: 'F12', scanCode: 0x58 },
  3639: { key: 'PrintScreen', name: 'PrtScn', scanCode: 0x00 },
  70: { key: 'ScrollLock', name: 'ScrLk', scanCode: 0x46 },
  3653: { key: 'Pause', name: 'Pause', scanCode: 0x00 },
  3666: { key: 'Insert', name: 'Ins', scanCode: 0x52 },
  3667: { key: 'Delete', name: 'Del', scanCode: 0x53 },
  3655: { key: 'Home', name: 'Home', scanCode: 0x47 },
  3663: { key: 'End', name: 'End', scanCode: 0x4F },
  3657: { key: 'PageUp', name: 'PgUp', scanCode: 0x49 },
  3665: { key: 'PageDown', name: 'PgDn', scanCode: 0x51 },
  69: { key: 'NumLock', name: 'NumLock', scanCode: 0x45 },
  41: { key: 'Backquote', name: '`', scanCode: 0x29 },

  // Mouse Buttons (Custom synthetic codes)
  1001: { key: 'MouseLeft', name: '🖱️ 좌클릭 (L-Click)', isMouse: true, mouseButton: 1 },
  1002: { key: 'MouseRight', name: '🖱️ 우클릭 (R-Click)', isMouse: true, mouseButton: 2 },
  1003: { key: 'MouseMiddle', name: '🖱️ 휠클릭 (M-Click)', isMouse: true, mouseButton: 3 },
  1004: { key: 'MouseX1', name: '🖱️ 마우스4 (뒤로가기 X1)', isMouse: true, mouseButton: 4 },
  1005: { key: 'MouseX2', name: '🖱️ 마우스5 (앞으로가기 X2)', isMouse: true, mouseButton: 5 }
};

// DOM KeyboardEvent.code to uiohook code
export const DOM_TO_UIOHOOK = {
  'Digit1': 2, 'Digit2': 3, 'Digit3': 4, 'Digit4': 5, 'Digit5': 6,
  'Digit6': 7, 'Digit7': 8, 'Digit8': 9, 'Digit9': 10, 'Digit0': 11,
  'KeyQ': 16, 'KeyW': 17, 'KeyE': 18, 'KeyR': 19, 'KeyT': 20,
  'KeyY': 21, 'KeyU': 22, 'KeyI': 23, 'KeyO': 24, 'KeyP': 25,
  'KeyA': 30, 'KeyS': 31, 'KeyD': 32, 'KeyF': 33, 'KeyG': 34,
  'KeyH': 35, 'KeyJ': 36, 'KeyK': 37, 'KeyL': 38,
  'KeyZ': 44, 'KeyX': 45, 'KeyC': 46, 'KeyV': 47, 'KeyB': 48,
  'KeyN': 49, 'KeyM': 50,
  'Escape': 1, 'Enter': 28, 'Space': 57, 'Tab': 15,
  'ShiftLeft': 42, 'ShiftRight': 54, 'ControlLeft': 29, 'ControlRight': 3613,
  'AltLeft': 56, 'AltRight': 3640,
  'F1': 59, 'F2': 60, 'F3': 61, 'F4': 62, 'F5': 63, 'F6': 64,
  'F7': 65, 'F8': 66, 'F9': 67, 'F10': 68, 'F11': 87, 'F12': 88,
  'Backquote': 41
};

export function getKeyLabel(keyCode) {
  if (UIOHOOK_KEY_MAP[keyCode]) {
    return UIOHOOK_KEY_MAP[keyCode].name;
  }
  return `Key(${keyCode})`;
}
