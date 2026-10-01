/**
 * Diablo IV Auto-Skill Helper - Renderer Controller
 * Pure Keyboard & Mouse Edition (Supports Side Mouse Buttons X1/X2)
 * Refactored & Modularized (< 400 lines)
 */

// 1. Key Codes & Friendly Labels (Keyboard & Mouse)
const KEY_MAP = {
  1: 'ESC', 2: '1', 3: '2', 4: '3', 5: '4', 6: '5', 7: '6', 8: '7', 9: '8', 10: '9', 11: '0',
  16: 'Q', 17: 'W', 18: 'E', 19: 'R', 20: 'T', 21: 'Y', 22: 'U', 23: 'I', 24: 'O', 25: 'P',
  30: 'A', 31: 'S', 32: 'D', 33: 'F', 34: 'G', 35: 'H', 36: 'J', 37: 'K', 38: 'L',
  44: 'Z', 45: 'X', 46: 'C', 47: 'V', 48: 'B', 49: 'N', 50: 'M',
  28: 'Enter', 57: 'Space', 15: 'Tab',
  59: 'F1', 60: 'F2', 61: 'F3', 62: 'F4', 63: 'F5', 64: 'F6', 65: 'F7', 66: 'F8', 67: 'F9', 68: 'F10', 87: 'F11', 88: 'F12',
  1001: '🖱️ 좌클릭 (L-Click)',
  1002: '🖱️ 우클릭 (R-Click)',
  1003: '🖱️ 휠클릭 (M-Click)',
  1004: '🖱️ 마우스4 (뒤로가기 X1)',
  1005: '🖱️ 마우스5 (앞으로가기 X2)'
};

function getKeyLabel(keyCode) {
  return KEY_MAP[keyCode] || `Key(${keyCode})`;
}
window.getKeyLabel = getKeyLabel;

// 2. Sound Feedback
const soundFeedback = {
  ctx: null,
  enabled: true,
  play(freq, type = 'sine', duration = 0.08) {
    if (!this.enabled) return;
    try {
      if (!this.ctx) {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (AudioCtx) this.ctx = new AudioCtx();
      }
      if (!this.ctx) return;
      if (this.ctx.state === 'suspended') this.ctx.resume();
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      osc.type = type;
      osc.frequency.setValueAtTime(freq, this.ctx.currentTime);
      gain.gain.setValueAtTime(0.12, this.ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, this.ctx.currentTime + duration);
      osc.connect(gain);
      gain.connect(this.ctx.destination);
      osc.start();
      osc.stop(this.ctx.currentTime + duration);
    } catch (e) { }
  },
  playStart() {
    this.play(659, 'sine', 0.08);
    setTimeout(() => this.play(880, 'sine', 0.12), 60);
  },
  playStop() {
    this.play(587, 'sine', 0.08);
    setTimeout(() => this.play(392, 'sine', 0.14), 60);
  },
  playToggle(disabled) {
    this.play(disabled ? 440 : 880, 'triangle', 0.1);
  }
};

// 3. Application State & Config Validation
const DEFAULT_SLOTS = [
  { id: 'skill1', name: '스킬 1', enabled: true, key: '1', keyCode: 2, intervalMs: 1000 },
  { id: 'skill2', name: '스킬 2', enabled: true, key: '2', keyCode: 3, intervalMs: 1000 },
  { id: 'skill3', name: '스킬 3', enabled: true, key: '3', keyCode: 4, intervalMs: 1000 },
  { id: 'skill4', name: '스킬 4', enabled: true, key: '4', keyCode: 5, intervalMs: 1000 },
  { id: 'skillLeft', name: '기본 기술', enabled: false, key: 'MouseLeft', keyCode: 1001, intervalMs: 300 },
  { id: 'skillRight', name: '핵심 기술', enabled: false, key: 'MouseRight', keyCode: 1002, intervalMs: 400 }
];

let config = {
  slots: JSON.parse(JSON.stringify(DEFAULT_SLOTS)),
  startKey: { key: 'F5', keyCode: 63 },
  stopKey: { key: 'F6', keyCode: 64 },
  disableKeys: [
    { key: 'Escape', keyCode: 1 },
    { key: 't', keyCode: 20 },
    { key: 'i', keyCode: 23 },
    { key: 'Enter', keyCode: 28 }
  ],
  randomJitter: true,
  alwaysOnTop: true,
  soundFeedback: true
};

function validateConfig(targetCfg) {
  if (!targetCfg || typeof targetCfg !== 'object') {
    targetCfg = { ...config };
  }
  if (!Array.isArray(targetCfg.slots) || targetCfg.slots.length === 0) {
    targetCfg.slots = JSON.parse(JSON.stringify(DEFAULT_SLOTS));
  } else {
    DEFAULT_SLOTS.forEach(defaultSlot => {
      const exists = targetCfg.slots.some(s => s && s.id === defaultSlot.id);
      if (!exists) targetCfg.slots.push({ ...defaultSlot });
    });
  }
  if (!Array.isArray(targetCfg.disableKeys)) {
    targetCfg.disableKeys = [
      { key: 'Escape', keyCode: 1 },
      { key: 't', keyCode: 20 },
      { key: 'i', keyCode: 23 },
      { key: 'Enter', keyCode: 28 }
    ];
  }
  if (!targetCfg.startKey || !targetCfg.startKey.keyCode) {
    targetCfg.startKey = { key: 'F5', keyCode: 63 };
  }
  if (!targetCfg.stopKey || !targetCfg.stopKey.keyCode) {
    targetCfg.stopKey = { key: 'F6', keyCode: 64 };
  }
  return targetCfg;
}

// 4. Global Skill Pulse Trigger (CPU 0%, Zero-strain)
window.triggerSkillPulse = function (slotId) {
  if (!slotId) return;
  const slotEl = document.querySelector(`[data-slot-id="${slotId}"]`);
  if (slotEl) {
    const keyBtn = slotEl.querySelector('.btn-key-bind');
    if (keyBtn) {
      keyBtn.style.transition = 'none';
      keyBtn.style.borderColor = '#34d399';
      keyBtn.style.backgroundColor = '#162b20';
      keyBtn.style.color = '#34d399';
      setTimeout(() => {
        keyBtn.style.transition = 'all 0.15s ease';
        keyBtn.style.borderColor = '';
        keyBtn.style.backgroundColor = '';
        keyBtn.style.color = '';
      }, 140);
    }
  }
  if (window.MiniMode && window.MiniMode.triggerPulse) {
    window.MiniMode.triggerPulse(slotId);
  }
};

// 5. DOM References
const titlebar = document.querySelector('.titlebar');
const btnPinTop = document.getElementById('btnPinTop');
const btnMinimize = document.getElementById('btnMinimize');
const btnClose = document.getElementById('btnClose');
const statusBadge = document.getElementById('statusBadge');
const statusText = document.getElementById('statusText');
const btnStart = document.getElementById('btnStart');
const btnStop = document.getElementById('btnStop');
const startKeyLabel = document.getElementById('startKeyLabel');
const stopKeyLabel = document.getElementById('stopKeyLabel');
const skillSlotsContainer = document.getElementById('skillSlotsContainer');
const disableKeysContainer = document.getElementById('disableKeysContainer');
const btnBindStart = document.getElementById('btnBindStart');
const btnBindStop = document.getElementById('btnBindStop');
const chkSoundFeedback = document.getElementById('chkSoundFeedback');

let presetController = null;

// 6. Application Initialization
async function init() {
  if (titlebar) {
    titlebar.addEventListener('mousedown', (e) => {
      if (e.button === 0 && !e.target.closest('.titlebar-controls')) {
        if (window.api && window.api.dragWindow) window.api.dragWindow();
      }
    });
  }

  config = validateConfig(config);

  // Initialize KeyBindModal component
  if (window.KeyBindModal) {
    window.KeyBindModal.initKeyBindModal(onKeySuccessfullyBound, () => {});
  }

  // Initialize PresetController component
  if (window.PresetController) {
    presetController = window.PresetController.initPresetController(
      () => config,
      (newCfg) => {
        config = validateConfig(newCfg);
        renderAll();
      },
      syncConfig
    );
  }

  renderAll();

  // Titlebar controls
  btnMinimize.addEventListener('click', () => window.api && window.api.minimizeWindow && window.api.minimizeWindow());
  btnClose.addEventListener('click', () => window.api && window.api.closeWindow && window.api.closeWindow());
  btnPinTop.addEventListener('click', async () => {
    config.alwaysOnTop = !config.alwaysOnTop;
    updatePinButton();
    if (window.api && window.api.setAlwaysOnTop) await window.api.setAlwaysOnTop(config.alwaysOnTop);
    syncConfig();
  });

  // Start / Stop buttons
  btnStart.addEventListener('click', async () => {
    syncInputsToConfig();
    syncConfig();
    if (window.api && window.api.startLoop) await window.api.startLoop(config);
    soundFeedback.playStart();
  });

  btnStop.addEventListener('click', async () => {
    if (window.api && window.api.stopLoop) await window.api.stopLoop();
    soundFeedback.playStop();
  });

  // Hotkey Bind buttons
  btnBindStart.addEventListener('click', () => {
    if (window.KeyBindModal) window.KeyBindModal.openKeyBindModal('시작 핫키 설정 (키보드/마우스)', { type: 'start' });
  });
  btnBindStop.addEventListener('click', () => {
    if (window.KeyBindModal) window.KeyBindModal.openKeyBindModal('정지 핫키 설정 (키보드/마우스)', { type: 'stop' });
  });

  if (chkSoundFeedback) {
    chkSoundFeedback.addEventListener('change', () => {
      config.soundFeedback = chkSoundFeedback.checked;
      soundFeedback.enabled = chkSoundFeedback.checked;
      syncConfig();
    });
  }

  // IPC Event Listeners
  if (window.api) {
    if (window.api.onStateChange) window.api.onStateChange(updateStateUI);
    if (window.api.onKeyBound) window.api.onKeyBound((keyData) => {
      if (window.KeyBindModal) window.KeyBindModal.handleKeyBound(keyData);
    });
    if (window.api.onHotKeyNotice) {
      window.api.onHotKeyNotice((type) => {
        if (type === 'start') soundFeedback.playStart();
        else if (type === 'stop') soundFeedback.playStop();
        else if (type === 'disable') soundFeedback.playToggle(true);
        else if (type === 'enable') soundFeedback.playToggle(false);
      });
    }
    if (window.api.onSkillTriggered) {
      window.api.onSkillTriggered((data) => {
        if (!data) return;
        let slotId = typeof data === 'string' ? (JSON.parse(data).slotId || data) : data.slotId;
        if (slotId && window.triggerSkillPulse) window.triggerSkillPulse(slotId);
      });
    }

    // Load initial config from backend
    try {
      const configPromise = window.api.getConfig();
      const timeout = new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), 3000));
      const remoteConfig = await Promise.race([configPromise, timeout]);
      if (remoteConfig) {
        const parsed = typeof remoteConfig === 'string' ? JSON.parse(remoteConfig) : remoteConfig;
        config = validateConfig(parsed);
        renderAll();
      }
    } catch (e) {
      console.warn('Failed to load remote config:', e);
    }
  }

  // Initialize Mini HUD Mode
  if (window.MiniMode) {
    window.MiniMode.init(config, { running: false, disabled: false });
  }
}

// 7. Key Binding Completion Handler
function onKeySuccessfullyBound(target, keyCode) {
  const label = getKeyLabel(keyCode);
  if (target.type === 'slot') {
    const slot = config.slots.find(s => s.id === target.slotId);
    if (slot) {
      slot.keyCode = keyCode;
      slot.key = label;
      renderAllSlots();
    }
  } else if (target.type === 'start') {
    config.startKey = { key: label, keyCode };
    updateHotkeyLabels();
  } else if (target.type === 'stop') {
    config.stopKey = { key: label, keyCode };
    updateHotkeyLabels();
  } else if (target.type === 'addDisable') {
    const exists = config.disableKeys.some(dk => dk.keyCode === keyCode);
    if (!exists) {
      config.disableKeys.push({ key: label, keyCode });
      renderAllDisableKeys();
    }
  }
  syncConfig();
}

// 8. Renderers & Sync
function renderAll() {
  updatePinButton();
  updateHotkeyLabels();
  if (chkSoundFeedback) {
    chkSoundFeedback.checked = config.soundFeedback !== false;
    soundFeedback.enabled = chkSoundFeedback.checked;
  }
  renderAllSlots();
  renderAllDisableKeys();
  if (presetController) presetController.loadPresetList();
}

function updatePinButton() {
  if (config.alwaysOnTop) btnPinTop.classList.add('active-pin');
  else btnPinTop.classList.remove('active-pin');
}

function updateHotkeyLabels() {
  const startKeyName = getKeyLabel(config.startKey.keyCode);
  const stopKeyName = getKeyLabel(config.stopKey.keyCode);
  if (startKeyLabel) startKeyLabel.textContent = startKeyName;
  if (stopKeyLabel) stopKeyLabel.textContent = stopKeyName;

  const bindStartKeyText = document.getElementById('bindStartKeyText');
  const bindStopKeyText = document.getElementById('bindStopKeyText');
  if (bindStartKeyText) bindStartKeyText.textContent = startKeyName;
  if (bindStopKeyText) bindStopKeyText.textContent = stopKeyName;

  btnStart.title = `시작 (${startKeyName})`;
  btnStop.title = `정지 (${stopKeyName})`;
}

function renderAllSlots() {
  skillSlotsContainer.innerHTML = '';
  config.slots.forEach((slot) => {
    const row = document.createElement('div');
    row.className = `skill-slot ${slot.enabled ? 'active' : 'disabled'}`;
    row.setAttribute('data-slot-id', slot.id);
    row.innerHTML = `
      <div class="slot-check-wrap">
        <label class="custom-checkbox">
          <input type="checkbox" class="slot-enabled" ${slot.enabled ? 'checked' : ''} />
          <span class="checkmark"></span>
        </label>
      </div>
      <div class="slot-name-wrap">
        <span class="slot-label">${slot.name}</span>
      </div>
      <div class="slot-key-wrap">
        <button type="button" class="btn-key-bind" title="클릭하여 발동 키/마우스버튼 변경">
          <span class="key-text">${getKeyLabel(slot.keyCode)}</span>
        </button>
      </div>
      <div class="slot-interval-wrap">
        <input type="text" class="slot-interval-input" value="${slot.intervalMs} ms" title="클릭하여 발동 주기(ms) 수정" />
      </div>
    `;

    // Checkbox toggle
    const chk = row.querySelector('.slot-enabled');
    chk.addEventListener('change', (e) => {
      slot.enabled = e.target.checked;
      row.className = `skill-slot ${slot.enabled ? 'active' : 'disabled'}`;
      syncConfig();
    });

    // Key bind modal open
    const btn = row.querySelector('.btn-key-bind');
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      if (window.KeyBindModal) {
        window.KeyBindModal.openKeyBindModal(`${slot.name} 발동 키 설정 (키보드 / 마우스)`, { type: 'slot', slotId: slot.id });
      }
    });

    // Inline interval input
    const intervalInput = row.querySelector('.slot-interval-input');
    intervalInput.addEventListener('focus', () => {
      intervalInput.value = slot.intervalMs;
      intervalInput.select();
    });
    intervalInput.addEventListener('blur', () => {
      let rawVal = intervalInput.value.replace(/[^0-9]/g, '');
      let val = parseInt(rawVal, 10);
      if (isNaN(val) || val < 10) val = 10;
      slot.intervalMs = val;
      intervalInput.value = val + ' ms';
      syncConfig();
    });
    intervalInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') intervalInput.blur();
      else if (e.key === 'Escape') {
        intervalInput.value = slot.intervalMs + ' ms';
        intervalInput.blur();
      }
    });

    skillSlotsContainer.appendChild(row);
  });
}

function renderAllDisableKeys() {
  disableKeysContainer.innerHTML = '';
  const listWrap = document.createElement('div');
  listWrap.className = 'disable-keys-list';

  config.disableKeys.forEach((item, index) => {
    const chip = document.createElement('div');
    chip.className = 'key-chip';
    chip.innerHTML = `
      <span class="chip-key">${getKeyLabel(item.keyCode)}</span>
      <button type="button" class="btn-remove-chip" title="삭제">&times;</button>
    `;
    chip.querySelector('.btn-remove-chip').addEventListener('click', (e) => {
      e.stopPropagation();
      config.disableKeys.splice(index, 1);
      renderAllDisableKeys();
      syncConfig();
    });
    listWrap.appendChild(chip);
  });

  const addBtn = document.createElement('button');
  addBtn.type = 'button';
  addBtn.className = 'btn-add-key';
  addBtn.innerHTML = `<span>+ 키 추가</span>`;
  addBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    if (window.KeyBindModal) {
      window.KeyBindModal.openKeyBindModal('추가할 일시정지 (Disable) 키를 누르세요 (키보드/마우스)', { type: 'addDisable' });
    }
  });

  listWrap.appendChild(addBtn);
  disableKeysContainer.appendChild(listWrap);
}

function syncInputsToConfig() {
  const rows = skillSlotsContainer.querySelectorAll('.skill-slot');
  config.slots.forEach((slot, idx) => {
    if (rows[idx]) {
      const input = rows[idx].querySelector('.slot-interval-input');
      if (input) {
        let val = parseInt(input.value.replace(/[^0-9]/g, ''), 10);
        if (!isNaN(val) && val >= 10) slot.intervalMs = val;
      }
      const chk = rows[idx].querySelector('.slot-enabled');
      if (chk) slot.enabled = chk.checked;
    }
  });
}

function syncConfig() {
  if (window.api) {
    if (window.api.updateConfig) window.api.updateConfig(config);
    if (window.api.saveConfig) window.api.saveConfig(config);
    if (config.activePreset && window.api.savePreset) {
      window.api.savePreset(config.activePreset, config);
    }
  }
  if (window.MiniMode) {
    window.MiniMode.updateConfig(config);
  }
}

function updateStateUI(state) {
  const startKeyName = getKeyLabel(config.startKey ? config.startKey.keyCode : 63);
  const stopKeyName = getKeyLabel(config.stopKey ? config.stopKey.keyCode : 64);

  if (!state.running) {
    statusBadge.className = 'status-badge stopped';
    statusText.textContent = '대기 (IDLE)';
    btnStart.title = `시작 (${startKeyName})`;
    btnStart.disabled = false;
    btnStop.disabled = true;
  } else if (state.disabled) {
    statusBadge.className = 'status-badge disabled';
    statusText.textContent = '일시정지';
    btnStart.title = `재개 (${startKeyName})`;
    btnStart.disabled = false;
    btnStop.disabled = false;
  } else {
    statusBadge.className = 'status-badge running';
    statusText.textContent = '실행중 (RUN)';
    btnStart.title = `실행중 (${startKeyName})`;
    btnStart.disabled = true;
    btnStop.disabled = false;
  }

  if (window.MiniMode) {
    window.MiniMode.updateState(state);
  }
}

document.addEventListener('DOMContentLoaded', init);
