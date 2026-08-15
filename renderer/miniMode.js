/**
 * Mini HUD Mode Controller Module
 * Handles mini window state, active skills chip rendering, and IPC sync.
 */
(function (global) {
  let isMiniMode = false;
  let currentConfig = null;
  let currentState = { running: false, disabled: false };

  // DOM Elements Cache
  let fullView = null;
  let miniView = null;
  let btnToggleMini = null;
  let btnMiniRestore = null;
  let btnMiniPin = null;
  let btnMiniClose = null;
  let miniStatusBadge = null;
  let miniStatusText = null;
  let btnMiniStart = null;
  let btnMiniStop = null;
  let miniSkillsRow = null;

  function init(config, state) {
    currentConfig = config;
    currentState = state || currentState;

    fullView = document.getElementById('fullView');
    miniView = document.getElementById('miniView');
    btnToggleMini = document.getElementById('btnToggleMini');
    btnMiniRestore = document.getElementById('btnMiniRestore');
    btnMiniPin = document.getElementById('btnMiniPin');
    btnMiniClose = document.getElementById('btnMiniClose');
    miniStatusBadge = document.getElementById('miniStatusBadge');
    miniStatusText = document.getElementById('miniStatusText');
    btnMiniStart = document.getElementById('btnMiniStart');
    btnMiniStop = document.getElementById('btnMiniStop');
    miniSkillsRow = document.getElementById('miniSkillsRow');

    bindEvents();
    renderMiniSkills();
    updateMiniStatus();
  }

  function bindEvents() {
    if (miniView) {
      miniView.addEventListener('mousedown', (e) => {
        if (e.button === 0 && !e.target.closest('button')) {
          if (window.api && window.api.dragWindow) {
            window.api.dragWindow();
          }
        }
      });
    }

    if (btnToggleMini) {
      btnToggleMini.addEventListener('click', () => setMode(true));
    }
    if (btnMiniRestore) {
      btnMiniRestore.addEventListener('click', () => setMode(false));
    }
    if (btnMiniClose) {
      btnMiniClose.addEventListener('click', () => {
        if (window.api && window.api.closeWindow) window.api.closeWindow();
      });
    }

    if (btnMiniPin) {
      btnMiniPin.addEventListener('click', () => {
        if (currentConfig) {
          currentConfig.alwaysOnTop = !currentConfig.alwaysOnTop;
          if (window.api && window.api.setAlwaysOnTop) {
            window.api.setAlwaysOnTop(currentConfig.alwaysOnTop);
          }
          updatePinButtons();
        }
      });
    }

    if (btnMiniStart) {
      btnMiniStart.addEventListener('click', () => {
        const fullStart = document.getElementById('btnStart');
        if (fullStart) fullStart.click();
      });
    }

    if (btnMiniStop) {
      btnMiniStop.addEventListener('click', () => {
        const fullStop = document.getElementById('btnStop');
        if (fullStop) fullStop.click();
      });
    }
  }

  function setMode(mini) {
    isMiniMode = mini;
    const titlebar = document.querySelector('.titlebar');

    if (mini) {
      if (titlebar) titlebar.classList.add('hidden');
      if (fullView) fullView.classList.add('hidden');
      if (miniView) miniView.classList.remove('hidden');
      renderMiniSkills();
      if (window.api && window.api.setWindowMode) {
        window.api.setWindowMode('mini');
      }
      // Mini HUD 모드 진입 시 100% 강제 항상 위(Topmost) 고정!
      if (window.api && window.api.setAlwaysOnTop) {
        window.api.setAlwaysOnTop(true);
      }
      updateMiniStatus();
    } else {
      if (miniView) miniView.classList.add('hidden');
      if (fullView) fullView.classList.remove('hidden');
      if (titlebar) titlebar.classList.remove('hidden');
      if (window.api && window.api.setWindowMode) {
        window.api.setWindowMode('full');
      }
      // Full 모드로 돌아오면 기존 설정값 복원
      if (window.api && window.api.setAlwaysOnTop && currentConfig) {
        window.api.setAlwaysOnTop(currentConfig.alwaysOnTop !== false);
      }
    }
  }


  function updateConfig(newConfig) {
    currentConfig = newConfig;
    renderMiniSkills();
    updatePinButtons();
  }

  function updateState(newState) {
    currentState = newState;
    updateMiniStatus();
    renderMiniSkills();
  }

  function updatePinButtons() {
    const isPinned = currentConfig && currentConfig.alwaysOnTop;
    const fullPin = document.getElementById('btnPinTop');
    if (fullPin) {
      if (isPinned) fullPin.classList.add('active-pin');
      else fullPin.classList.remove('active-pin');
    }
    if (btnMiniPin) {
      if (isPinned) btnMiniPin.classList.add('active-pin');
      else btnMiniPin.classList.remove('active-pin');
    }
  }

  function updateMiniStatus() {
    if (!miniStatusBadge || !miniStatusText || !btnMiniStart || !btnMiniStop) return;

    if (!currentState.running) {
      miniStatusBadge.className = 'mini-status-badge stopped';
      miniStatusText.textContent = '대기';
      btnMiniStart.disabled = false;
      btnMiniStop.disabled = true;
    } else if (currentState.disabled) {
      miniStatusBadge.className = 'mini-status-badge disabled';
      miniStatusText.textContent = '일시정지';
      btnMiniStart.disabled = false;
      btnMiniStop.disabled = false;
    } else {
      miniStatusBadge.className = 'mini-status-badge running';
      miniStatusText.textContent = '실행중';
      btnMiniStart.disabled = true;
      btnMiniStop.disabled = false;
    }
  }

  function renderMiniSkills() {
    if (!miniSkillsRow) return;
    miniSkillsRow.classList.add('hidden');
    miniSkillsRow.style.display = 'none';
    miniSkillsRow.innerHTML = '';
  }

  function triggerPulse(slotId) {
    if (!miniSkillsRow) return;
    const chip = miniSkillsRow.querySelector(`[data-slot-id="${slotId}"]`);
    if (chip) {
      chip.style.transition = 'none';
      chip.style.borderColor = '#6ee7b7';
      chip.style.backgroundColor = '#059669';
      chip.style.color = '#ffffff';
      chip.style.transform = 'scale(1.1)';

      const keySpan = chip.querySelector('.chip-key');
      const intSpan = chip.querySelector('.chip-interval');
      if (keySpan) keySpan.style.color = '#ffffff';
      if (intSpan) intSpan.style.color = '#ffffff';

      setTimeout(() => {
        chip.style.transition = 'all 0.15s ease';
        chip.style.borderColor = '';
        chip.style.backgroundColor = '';
        chip.style.color = '';
        chip.style.transform = '';
        if (keySpan) keySpan.style.color = '';
        if (intSpan) intSpan.style.color = '';
      }, 180);
    }
  }






  // Export to global scope
  global.MiniMode = {
    init,
    setMode,
    updateConfig,
    updateState,
    triggerPulse,
    isMini: () => isMiniMode
  };
})(window);

