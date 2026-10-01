/**
 * PresetController Component
 * Handles preset list loading, saving, switching, and deleting.
 */

(function () {
  function initPresetController(getConfig, onConfigLoaded, syncConfig) {
    const presetSelect = document.getElementById('presetSelect');
    const btnSavePreset = document.getElementById('btnSavePreset');
    const btnDeletePreset = document.getElementById('btnDeletePreset');
    const presetInputOverlay = document.getElementById('presetInputOverlay');
    const presetNameInput = document.getElementById('presetNameInput');
    const btnConfirmSavePreset = document.getElementById('btnConfirmSavePreset');
    const btnCancelSavePreset = document.getElementById('btnCancelSavePreset');

    async function loadPresetList() {
      if (!presetSelect || !window.api || !window.api.listPresets) return;
      try {
        const listStr = await window.api.listPresets();
        const presets = typeof listStr === 'string' ? JSON.parse(listStr) : (listStr || []);
        const config = getConfig();

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

    // 1. Preset switch
    if (presetSelect) {
      presetSelect.addEventListener('change', async () => {
        const selectedName = presetSelect.value;
        if (!selectedName || !window.api || !window.api.loadPreset) return;
        try {
          const loadedStr = await window.api.loadPreset(selectedName);
          if (loadedStr && loadedStr !== 'null') {
            const loadedConfig = typeof loadedStr === 'string' ? JSON.parse(loadedStr) : loadedStr;
            loadedConfig.activePreset = selectedName;
            onConfigLoaded(loadedConfig);
            syncConfig();
          }
        } catch (e) {
          console.error('Failed to load preset:', e);
        }
      });
    }

    // 2. Open Save Preset Modal
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

    // 3. Confirm Save Preset
    if (btnConfirmSavePreset) {
      btnConfirmSavePreset.addEventListener('click', async () => {
        const name = presetNameInput ? presetNameInput.value.trim() : '';
        if (!name) {
          alert('프리셋 이름을 입력해 주세요.');
          return;
        }
        if (window.api && window.api.savePreset) {
          const config = getConfig();
          await window.api.savePreset(name, config);
          config.activePreset = name;
          if (presetInputOverlay) presetInputOverlay.classList.add('hidden');
          await loadPresetList();
          syncConfig();
        }
      });
    }

    // 4. Delete Preset
    if (btnDeletePreset) {
      btnDeletePreset.addEventListener('click', async () => {
        const config = getConfig();
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

    return {
      loadPresetList
    };
  }

  window.PresetController = {
    initPresetController
  };
})();
