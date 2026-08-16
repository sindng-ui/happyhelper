/**
 * Diablo IV Auto-Skill Helper - Renderer Controller
 * Standalone bundle with Gamepad (Xbox) & Keyboard & Mouse support
 */

// 1. Key Codes & Labels (Keyboard, Mouse, Xbox Gamepad)
const KEY_MAP = {
  1: 'ESC', 2: '1', 3: '2', 4: '3', 5: '4', 6: '5', 7: '6', 8: '7', 9: '8', 10: '9', 11: '0',
  16: 'Q', 17: 'W', 18: 'E', 19: 'R', 20: 'T', 21: 'Y', 22: 'U', 23: 'I', 24: 'O', 25: 'P',
  30: 'A', 31: 'S', 32: 'D', 33: 'F', 34: 'G', 35: 'H', 36: 'J', 37: 'K', 38: 'L',
  44: 'Z', 45: 'X', 46: 'C', 47: 'V', 48: 'B', 49: 'N', 50: 'M',
  28: 'Enter', 57: 'Space', 15: 'Tab',
  59: 'F1', 60: 'F2', 61: 'F3', 62: 'F4', 63: 'F5', 64: 'F6', 65: 'F7', 66: 'F8', 67: 'F9', 68: 'F10', 87: 'F11', 88: 'F12',
  1001: '좌클릭 (L-Click)', 1002: '우클릭 (R-Click)', 1003: '휠클릭 (M-Click)', 1004: '마우스4 (X1)', 1005: '마우스5 (X2)',
  // Xbox Gamepad Buttons
  2001: 'Pad A', 2002: 'Pad B', 2003: 'Pad X', 2004: 'Pad Y',
  2005: 'Pad LB', 2006: 'Pad RB', 2007: 'Pad LT', 2008: 'Pad RT',
  2009: 'Pad D-Up', 2010: 'Pad D-Down', 2011: 'Pad D-Left', 2012: 'Pad D-Right',
  2013: 'Pad L3 (LS)', 2014: 'Pad R3 (RS)', 2015: 'Pad View (Back)', 2016: 'Pad Menu (Start)'
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

// 3. Application State & Default Config
let config = {
  slots: [
    { id: 'skill1', name: '스킬 1', enabled: true, key: '1', keyCode: 2, intervalMs: 1000 },
    { id: 'skill2', name: '스킬 2', enabled: true, key: '2', keyCode: 3, intervalMs: 1000 },
    { id: 'skill3', name: '스킬 3', enabled: true, key: '3', keyCode: 4, intervalMs: 1000 },
    { id: 'skill4', name: '스킬 4', enabled: true, key: '4', keyCode: 5, intervalMs: 1000 },
    { id: 'skillLeft', name: '기본 기술', enabled: false, key: 'MouseLeft', keyCode: 1001, intervalMs: 300 },
    { id: 'skillRight', name: '핵심 기술', enabled: false, key: 'MouseRight', keyCode: 1002, intervalMs: 400 }
  ],
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

const DEFAULT_SLOTS = [
  { id: 'skill1', name: '스킬 1', enabled: true, key: '1', keyCode: 2, intervalMs: 1000 },
  { id: 'skill2', name: '스킬 2', enabled: true, key: '2', keyCode: 3, intervalMs: 1000 },
  { id: 'skill3', name: '스킬 3', enabled: true, key: '3', keyCode: 4, intervalMs: 1000 },
  { id: 'skill4', name: '스킬 4', enabled: true, key: '4', keyCode: 5, intervalMs: 1000 },
  { id: 'skillLeft', name: '기본 기술', enabled: false, key: 'MouseLeft', keyCode: 1001, intervalMs: 300 },
  { id: 'skillRight', name: '핵심 기술', enabled: false, key: 'MouseRight', keyCode: 1002, intervalMs: 400 }
];

function validateConfig(targetCfg) {
  if (!targetCfg || typeof targetCfg !== 'object') {
    targetCfg = { ...config };
  }
  if (!Array.isArray(targetCfg.slots) || targetCfg.slots.length === 0) {
    targetCfg.slots = JSON.parse(JSON.stringify(DEFAULT_SLOTS));
  } else {
    // Ensure all 6 default slot IDs exist
    DEFAULT_SLOTS.forEach(defaultSlot => {
      const exists = targetCfg.slots.some(s => s && s.id === defaultSlot.id);
      if (!exists) {
        targetCfg.slots.push({ ...defaultSlot });
      }
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

let currentBindingTarget = null;

// Global Skill Pulse Trigger (Subtle, CPU 0%, Zero-strain)
window.triggerSkillPulse = function (slotId) {
  if (!slotId) return;

  // 1. Full View: 오직 키 버튼의 테두리와 텍스트만 은은하게 톡! (140ms)
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

  // 2. Mini HUD View: 칩 테두리만 은은하게 톡! (140ms)
  if (window.MiniMode && window.MiniMode.triggerPulse) {
    window.MiniMode.triggerPulse(slotId);
  }
};


// DOM Elements
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

// Preset DOM Elements
const presetSelect = document.getElementById('presetSelect');
const btnSavePreset = document.getElementById('btnSavePreset');
const btnDeletePreset = document.getElementById('btnDeletePreset');
const presetInputOverlay = document.getElementById('presetInputOverlay');
const presetNameInput = document.getElementById('presetNameInput');
const btnConfirmSavePreset = document.getElementById('btnConfirmSavePreset');
const btnCancelSavePreset = document.getElementById('btnCancelSavePreset');

const keyBindOverlay = document.getElementById('keyBindOverlay');
const keyBindTitle = document.getElementById('keyBindTitle');
const btnCancelKeyBind = document.getElementById('btnCancelKeyBind');

const driverNoticeOverlay = document.getElementById('driverNoticeOverlay');
const btnConfirmInstallDriver = document.getElementById('btnConfirmInstallDriver');
const btnCancelInstallDriver = document.getElementById('btnCancelInstallDriver');

window.openDriverNoticeModal = function () {
  if (window.api && window.api.cancelKeyBind) window.api.cancelKeyBind();
  cancelKeyBinding();
  if (driverNoticeOverlay) driverNoticeOverlay.classList.remove('hidden');
};
window.closeDriverNoticeModal = function () {
  if (driverNoticeOverlay) driverNoticeOverlay.classList.add('hidden');
};




// Initialize
async function init() {
  // Titlebar dragging for Native Win32 Window Move
  if (titlebar) {
    titlebar.addEventListener('mousedown', (e) => {
      if (e.button === 0 && !e.target.closest('.titlebar-controls')) {
        if (window.api && window.api.dragWindow) {
          window.api.dragWindow();
        }
      }
    });
  }

  // Validate default config & initial render immediately
  config = validateConfig(config);
  renderAll();

  // 1. Titlebar Controls
  btnMinimize.addEventListener('click', () => window.api && window.api.minimizeWindow && window.api.minimizeWindow());
  btnClose.addEventListener('click', () => window.api && window.api.closeWindow && window.api.closeWindow());
  btnPinTop.addEventListener('click', async () => {
    config.alwaysOnTop = !config.alwaysOnTop;
    updatePinButton();
    if (window.api && window.api.setAlwaysOnTop) {
      await window.api.setAlwaysOnTop(config.alwaysOnTop);
    }
    syncConfig();
  });

  // 2. Start / Stop Buttons
  btnStart.addEventListener('click', async () => {
    // Force sync all DOM inputs to config object before start
    const rows = skillSlotsContainer.querySelectorAll('.skill-slot');
    config.slots.forEach((slot, idx) => {
      if (rows[idx]) {
        const input = rows[idx].querySelector('.slot-interval');
        if (input) {
          let val = parseInt(input.value, 10);
          if (!isNaN(val) && val >= 10) slot.intervalMs = val;
        }
        const chk = rows[idx].querySelector('.slot-enabled');
        if (chk) slot.enabled = chk.checked;
      }
    });
    syncConfig();

    if (window.api && window.api.startLoop) await window.api.startLoop(config);
    soundFeedback.playStart();
  });


  btnStop.addEventListener('click', async () => {
    if (window.api && window.api.stopLoop) await window.api.stopLoop();
    soundFeedback.playStop();
  });

  // 3. Hotkey Binds
  btnBindStart.addEventListener('click', () => openKeyBindModal('시작 핫키 설정', { type: 'start' }));
  btnBindStop.addEventListener('click', () => openKeyBindModal('정지 핫키 설정', { type: 'stop' }));

  // 4. Options
  chkSoundFeedback.addEventListener('change', () => {
    config.soundFeedback = chkSoundFeedback.checked;
    soundFeedback.enabled = chkSoundFeedback.checked;
    syncConfig();
  });

  // Preset Event Listeners
  if (presetSelect) {
    presetSelect.addEventListener('change', async () => {
      const selectedName = presetSelect.value;
      if (!selectedName || !window.api || !window.api.loadPreset) return;
      try {
        const loadedStr = await window.api.loadPreset(selectedName);
        if (loadedStr && loadedStr !== 'null') {
          const loadedConfig = typeof loadedStr === 'string' ? JSON.parse(loadedStr) : loadedStr;
          config = validateConfig(loadedConfig);
          config.activePreset = selectedName;
          renderAll();
          syncConfig();
        }
      } catch (e) {
        console.error('Failed to load preset:', e);
      }
    });
  }

  if (btnSavePreset) {
    btnSavePreset.addEventListener('click', () => {
      if (presetInputOverlay) {
        presetInputOverlay.classList.remove('hidden');
        if (presetNameInput) {
          presetNameInput.value = '';
          presetNameInput.focus();
        }
      }
    });
  }

  if (btnCancelSavePreset) {
    btnCancelSavePreset.addEventListener('click', () => {
      if (presetInputOverlay) presetInputOverlay.classList.add('hidden');
    });
  }

  if (btnConfirmSavePreset) {
    btnConfirmSavePreset.addEventListener('click', async () => {
      const name = presetNameInput ? presetNameInput.value.trim() : '';
      if (!name) {
        alert('프리셋 이름을 입력해 주세요.');
        return;
      }
      if (window.api && window.api.savePreset) {
        await window.api.savePreset(name, config);
        config.activePreset = name;
        if (presetInputOverlay) presetInputOverlay.classList.add('hidden');
        await loadPresetList();
        syncConfig();
      }
    });
  }

  if (btnDeletePreset) {
    btnDeletePreset.addEventListener('click', async () => {
      const targetName = config.activePreset || (presetSelect ? presetSelect.value : '');
      if (!targetName) {
        alert('삭제할 프리셋을 먼저 선택해 주세요.');
        return;
      }
      if (confirm(`'${targetName}' 프리셋을 삭제하시겠습니까?`)) {
        if (window.api && window.api.deletePreset) {
          await window.api.deletePreset(targetName);
          config.activePreset = '';
          await loadPresetList();
          syncConfig();
        }
      }
    });
  }




  btnCancelKeyBind.addEventListener('click', cancelKeyBinding);

  if (btnConfirmInstallDriver) {
    btnConfirmInstallDriver.addEventListener('click', async () => {
      window.closeDriverNoticeModal();
      if (window.api && window.api.installViGEmDriver) {
        await window.api.installViGEmDriver();
      }
    });
  }
  if (btnCancelInstallDriver) {
    btnCancelInstallDriver.addEventListener('click', window.closeDriverNoticeModal);
  }

  const btnInstallHidHide = document.getElementById('btnInstallHidHide');
  if (btnInstallHidHide) {
    btnInstallHidHide.addEventListener('click', () => {
      const url = 'https://github.com/nefarius/HidHide/releases/latest';
      if (window.api && window.api.openExternalUrl) {
        window.api.openExternalUrl(url);
      } else {
        window.open(url, '_blank');
      }
    });
  }

  const linkVigemOfficial = document.getElementById('linkVigemOfficial');
  if (linkVigemOfficial) {
    linkVigemOfficial.addEventListener('click', (e) => {
      e.preventDefault();
      if (window.api && window.api.openExternalUrl) {
        window.api.openExternalUrl('https://github.com/nefarius/ViGEmBus');
      } else {
        window.open('https://github.com/nefarius/ViGEmBus', '_blank');
      }
    });
  }

  const linkHidHideOfficial = document.getElementById('linkHidHideOfficial');
  if (linkHidHideOfficial) {
    linkHidHideOfficial.addEventListener('click', (e) => {
      e.preventDefault();
      if (window.api && window.api.openExternalUrl) {
        window.api.openExternalUrl('https://github.com/nefarius/HidHide');
      } else {
        window.open('https://github.com/nefarius/HidHide', '_blank');
      }
    });
  }



  // 5. Render UI Immediately with Default Config
  renderAll();

  // 6. Connect Backend & Sync Config
  if (window.api) {
    // ★ Register event listeners FIRST, before any await
    // This ensures pad/key/state events are captured even if getConfig hangs
    if (window.api.onStateChange) window.api.onStateChange(updateStateUI);
    if (window.api.onKeyBound) window.api.onKeyBound(handleKeyBound);
    if (window.api.onHotKeyNotice) {
      window.api.onHotKeyNotice((type) => {
        if (type === 'start') soundFeedback.playStart();
        else if (type === 'stop') soundFeedback.playStop();
        else if (type === 'disable') soundFeedback.playToggle(true);
        else if (type === 'enable') soundFeedback.playToggle(false);
      });
    }
    function updatePadStatusUI(status) {
      const padStatusEl = document.getElementById('padStatusText');
      let parsed = status;
      if (typeof status === 'string') {
        try { parsed = JSON.parse(status); } catch (e) { parsed = {}; }
      }
      if (!parsed) return;

      const isVigemActive = Boolean(parsed.vigemInstalled || parsed.viGEmInstalled || parsed.viGEmReady);
      const isHidHideActive = Boolean(parsed.hidHideInstalled);
      const isConnected = Boolean(parsed.connected || parsed.controllerConnected);

      // Update Modal Badges if open
      const badgeVigem = document.getElementById('badgeVigemStatus');
      if (badgeVigem) {
        badgeVigem.textContent = isVigemActive ? '설치됨 (정상)' : '미설치';
        badgeVigem.style.background = isVigemActive ? 'rgba(52, 211, 153, 0.2)' : 'rgba(239, 68, 68, 0.2)';
        badgeVigem.style.color = isVigemActive ? '#34d399' : '#ef4444';
      }
      const badgeHid = document.getElementById('badgeHidHideStatus');
      if (badgeHid) {
        badgeHid.textContent = isHidHideActive ? '설치됨 (정상)' : '미설치';
        badgeHid.style.background = isHidHideActive ? 'rgba(52, 211, 153, 0.2)' : 'rgba(239, 68, 68, 0.2)';
        badgeHid.style.color = isHidHideActive ? '#34d399' : '#ef4444';
      }

      if (!padStatusEl) return;

      if (!isVigemActive || !isHidHideActive) {
        const missingList = [];
        if (!isVigemActive) missingList.push('ViGEmBus');
        if (!isHidHideActive) missingList.push('HidHide');

        padStatusEl.innerHTML = `
          <div style="background: rgba(239, 68, 68, 0.15); border: 1px solid rgba(239, 68, 68, 0.4); border-radius: 6px; padding: 6px 10px; margin: 4px 0; text-align: center;">
            <div style="color: #ef4444; font-weight: 700; font-size: 11.5px; margin-bottom: 2px;">⚠️ 권장 드라이버(${missingList.join(', ')}) 미설치</div>
            <a href="#" id="linkReqDriverNotice" style="color: #38bdf8; font-weight: 600; font-size: 11px; text-decoration: underline; cursor: pointer; display: inline-block;">👉 [오픈소스 드라이버 2종 설치 안내]</a>
          </div>
        `;
        const link = document.getElementById('linkReqDriverNotice');
        if (link) {
          const handleOpenNotice = (e) => {
            if (e) {
              e.preventDefault();
              e.stopPropagation();
            }
            if (window.api && window.api.cancelKeyBind) window.api.cancelKeyBind();
            cancelKeyBinding();
            window.openDriverNoticeModal();
            return false;
          };
          link.onmousedown = handleOpenNotice;
          link.onclick = handleOpenNotice;
        }
      } else if (isConnected) {
        padStatusEl.innerHTML = '🎮 <span style="color:#34d399;font-weight:600;">Xbox 게임패드 연결됨 (ViGEm + HidHide 보호 가동 중)</span>';
      } else {
        padStatusEl.innerHTML = '🎮 <span style="color:#94a3b8;">게임패드 신호 대기 중... (패드 버튼 입력 시 자동 감지)</span>';
      }
    }
    window.updatePadStatusUI = updatePadStatusUI;

    if (window.api.onPadStatus) {
      window.api.onPadStatus(updatePadStatusUI);
    }





    if (window.api.onSkillTriggered) {
      window.api.onSkillTriggered((data) => {
        if (!data) return;
        let slotId = null;
        if (typeof data === 'string') {
          try {
            const parsed = JSON.parse(data);
            slotId = parsed.slotId;
          } catch (e) {
            slotId = data;
          }
        } else {
          slotId = data.slotId;
        }
        if (slotId && window.triggerSkillPulse) {
          window.triggerSkillPulse(slotId);
        }
      });
    }



    // Query initial pad/vigem status immediately

    if (window.api.getPadStatus) {
      try {
        const padStatus = await window.api.getPadStatus();
        if (padStatus) {
          const parsed = typeof padStatus === 'string' ? JSON.parse(padStatus) : padStatus;
          updatePadStatusUI(parsed);
        }
      } catch (e) {
        console.warn('Failed to query initial pad status:', e);
      }
    }


    // Now load remote config (with timeout protection)
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
      console.warn('Failed to load remote config (using defaults):', e);
    }
  }

  // 7. Web Gamepad Poller
  startGamepadPoller();

  // 8. Initialize Mini HUD Mode
  if (window.MiniMode) {
    window.MiniMode.init(config, { running: false, disabled: false });
  }
}



// Web Gamepad API Listener (JS fallback — works if page has focus)
function startGamepadPoller() {
  const padMap = {
    0: 2001, // A
    1: 2002, // B
    2: 2003, // X
    3: 2004, // Y
    4: 2005, // LB
    5: 2006, // RB
    6: 2007, // LT
    7: 2008, // RT
    8: 2015, // View / Back
    9: 2016, // Menu / Start
    10: 2013, // LS / L3
    11: 2014, // RS / R3
    12: 2009, // D-Up
    13: 2010, // D-Down
    14: 2011, // D-Left
    15: 2012  // D-Right
  };

  let lastPadPressed = null;

  // Detect gamepad connection via browser event
  window.addEventListener('gamepadconnected', (e) => {
    console.log('[Gamepad] Connected via Browser API:', e.gamepad.id);
    const padStatusEl = document.getElementById('padStatusText');
    if (padStatusEl) padStatusEl.textContent = '🎮 패드 감지 (브라우저): ' + (e.gamepad.id || 'Unknown');
  });

  setInterval(() => {
    if (!currentBindingTarget) {
      lastPadPressed = null;
      return;
    }

    const gamepads = navigator.getGamepads ? navigator.getGamepads() : [];
    for (let i = 0; i < gamepads.length; i++) {
      const gp = gamepads[i];
      if (!gp) continue;

      for (let btnIdx = 0; btnIdx < gp.buttons.length; btnIdx++) {
        const btn = gp.buttons[btnIdx];
        if (btn.pressed || btn.value > 0.5) {
          const mappedCode = padMap[btnIdx];
          if (mappedCode && lastPadPressed !== mappedCode) {
            lastPadPressed = mappedCode;
            handleKeyBound({ keyCode: mappedCode, isMouse: false });
            return;
          }
        }
      }
    }
  }, 40);
}




async function loadPresetList() {
  if (!presetSelect || !window.api || !window.api.listPresets) return;
  try {
    const listStr = await window.api.listPresets();
    const presets = typeof listStr === 'string' ? JSON.parse(listStr) : (listStr || []);
    
    presetSelect.innerHTML = `<option value="" disabled ${!config.activePreset ? 'selected' : ''}>저장된 프리셋 선택...</option>`;
    
    presets.forEach(pName => {
      const opt = document.createElement('option');
      opt.value = pName;
      opt.textContent = pName;
      if (config.activePreset === pName) {
        opt.selected = true;
      }
      presetSelect.appendChild(opt);
    });
  } catch (e) {
    console.warn('Failed to load preset list:', e);
  }
}

function renderAll() {
  updatePinButton();
  updateHotkeyLabels();
  chkSoundFeedback.checked = config.soundFeedback !== false;
  soundFeedback.enabled = chkSoundFeedback.checked;
  renderAllSlots();
  renderAllDisableKeys();
  loadPresetList();
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
  else btnBindStart.textContent = startKeyName;

  if (bindStopKeyText) bindStopKeyText.textContent = stopKeyName;
  else btnBindStop.textContent = stopKeyName;

  btnStart.title = `시작 (${startKeyName})`;
  btnStop.title = `정지 (${stopKeyName})`;
}



// 6 Skill Slots Rendering
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
        <button type="button" class="btn-key-bind" title="클릭하여 발동 키/패드버튼 변경">
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

    // Key bind
    const btn = row.querySelector('.btn-key-bind');
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      openKeyBindModal(`${slot.name} 발동 키 설정 (키보드/마우스/게임패드)`, { type: 'slot', slotId: slot.id });
    });

    // Single Input Inline Interval Edit logic
    const intervalInput = row.querySelector('.slot-interval-input');

    // 포커스 진입 시: '3010 ms' -> '3010'으로 뿅 변하고 전체 선택!
    intervalInput.addEventListener('focus', () => {
      intervalInput.value = slot.intervalMs;
      intervalInput.select();
    });

    // 포커스 아웃(Blur) 시: 저장 및 '3010 ms'로 뿅 복원!
    intervalInput.addEventListener('blur', () => {
      let rawVal = intervalInput.value.replace(/[^0-9]/g, '');
      let val = parseInt(rawVal, 10);
      if (isNaN(val) || val < 10) val = 10;
      slot.intervalMs = val;
      intervalInput.value = val + ' ms';
      syncConfig();
    });

    // 키보드 제어 (Enter / Escape)
    intervalInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        intervalInput.blur(); // 엔터 치면 blur 이벤트가 트리거되어 저장 및 'ms' 복원!
      } else if (e.key === 'Escape') {
        intervalInput.value = slot.intervalMs + ' ms';
        intervalInput.blur();
      }
    });

    skillSlotsContainer.appendChild(row);

  });
}


// Disable Keys Rendering
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
    openKeyBindModal('추가할 일시정지 (Disable) 키를 누르세요 (키/패드)', { type: 'addDisable' });
  });

  listWrap.appendChild(addBtn);
  disableKeysContainer.appendChild(listWrap);
}

let visualPulseTimers = [];

function startVisualPulseEngine() {
  stopVisualPulseEngine();
  if (!config || !config.slots) return;

  config.slots.forEach((slot) => {
    if (!slot.enabled) return;
    const interval = Math.max(50, slot.intervalMs || 1000);

    // 시작 시 첫 펄스 즉각 발사
    if (window.triggerSkillPulse) {
      window.triggerSkillPulse(slot.id);
    }

    // 주기별 반복 타이머 가동
    const t = setInterval(() => {
      if (window.triggerSkillPulse) {
        window.triggerSkillPulse(slot.id);
      }
    }, interval);
    visualPulseTimers.push(t);
  });
}

function stopVisualPulseEngine() {
  visualPulseTimers.forEach(t => clearInterval(t));
  visualPulseTimers = [];
}

function updateStateUI(state) {
  const startKeyName = getKeyLabel(config.startKey ? config.startKey.keyCode : 63);
  const stopKeyName = getKeyLabel(config.stopKey ? config.stopKey.keyCode : 64);

  if (!state.running) {
    stopVisualPulseEngine();
    statusBadge.className = 'status-badge stopped';
    statusText.textContent = '대기 (IDLE)';
    btnStart.title = `시작 (${startKeyName})`;
    btnStart.disabled = false;
    btnStop.disabled = true;
  } else if (state.disabled) {
    stopVisualPulseEngine();
    statusBadge.className = 'status-badge disabled';
    statusText.textContent = '일시정지';
    btnStart.title = `재개 (${startKeyName})`;
    btnStart.disabled = false;
    btnStop.disabled = false;
  } else {
    startVisualPulseEngine();
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



async function openKeyBindModal(title, target) {
  currentBindingTarget = target;
  keyBindTitle.textContent = title;
  keyBindOverlay.classList.remove('hidden');

  const padStatusEl = document.getElementById('padStatusText');
  if (padStatusEl) padStatusEl.textContent = '🔍 패드 상태 확인 중...';

  if (window.api && window.api.startKeyBind) window.api.startKeyBind();

  if (window.api && window.api.getPadStatus) {
    try {
      const padStatus = await window.api.getPadStatus();
      if (padStatus) {
        updatePadStatusUI(padStatus);
      }
    } catch (e) {
      console.warn('Failed to fetch pad status on keybind modal open:', e);
    }
  }
}

function cancelKeyBinding() {
  keyBindOverlay.classList.add('hidden');
  currentBindingTarget = null;
  if (window.api && window.api.cancelKeyBind) window.api.cancelKeyBind();
}

function handleKeyBound(keyData) {
  if (!currentBindingTarget) {
    keyBindOverlay.classList.add('hidden');
    return;
  }
  if (!keyData) {
    cancelKeyBinding();
    return;
  }

  const { keyCode } = keyData;

  // 1. ESC Key (keyCode === 1) is blocked! Cancels modal.
  if (keyCode === 1) {
    cancelKeyBinding();
    return;
  }

  // 2. Mouse Left Click (keyCode === 1001) is blocked!
  if (keyCode === 1001) {
    return;
  }

  keyBindOverlay.classList.add('hidden');
  const label = getKeyLabel(keyCode);

  if (currentBindingTarget.type === 'slot') {
    const slot = config.slots.find(s => s.id === currentBindingTarget.slotId);
    if (slot) {
      slot.keyCode = keyCode;
      slot.key = label;
      renderAllSlots();
    }
  } else if (currentBindingTarget.type === 'start') {
    config.startKey = { key: label, keyCode };
    updateHotkeyLabels();
  } else if (currentBindingTarget.type === 'stop') {
    config.stopKey = { key: label, keyCode };
    updateHotkeyLabels();
  } else if (currentBindingTarget.type === 'addDisable') {
    const exists = config.disableKeys.some(dk => dk.keyCode === keyCode);
    if (!exists) {
      config.disableKeys.push({ key: label, keyCode });
      renderAllDisableKeys();
    }
  }

  currentBindingTarget = null;
  syncConfig();
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
  if (statusBadge && statusBadge.classList.contains('running')) {
    startVisualPulseEngine();
  }
}



document.addEventListener('DOMContentLoaded', init);
