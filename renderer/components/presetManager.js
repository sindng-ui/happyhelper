/**
 * PresetManager Component
 * Handles saving/loading presets & export/import via IPC
 */
export function initPresetManager({
  presetSelectEl,
  btnSavePresetEl,
  btnExportEl,
  btnImportEl,
  getCurrentConfig,
  onConfigLoaded
}) {
  // Refresh preset dropdown list
  async function refreshPresetList(selectedName = '') {
    try {
      const presets = await window.api.listPresets();
      presetSelectEl.innerHTML = '<option value="">-- 프리셋 선택 --</option>';
      presets.forEach((name) => {
        const opt = document.createElement('option');
        opt.value = name;
        opt.textContent = name;
        if (name === selectedName) opt.selected = true;
        presetSelectEl.appendChild(opt);
      });
    } catch (err) {
      console.error('Failed to list presets:', err);
    }
  }

  // Load selected preset
  presetSelectEl.addEventListener('change', async (e) => {
    const name = e.target.value;
    if (!name) return;
    try {
      const config = await window.api.loadPreset(name);
      if (config) {
        onConfigLoaded(config);
      }
    } catch (err) {
      alert(`프리셋 불러오기 실패: ${err.message}`);
    }
  });

  // Save new preset
  btnSavePresetEl.addEventListener('click', async () => {
    const name = prompt('저장할 프리셋 이름을 입력하세요:', '디아4_기본세팅');
    if (!name || !name.trim()) return;

    try {
      const config = getCurrentConfig();
      await window.api.savePreset(name.trim(), config);
      await refreshPresetList(name.trim());
      alert(`프리셋 '${name.trim()}' 저장이 완료되었습니다.`);
    } catch (err) {
      alert(`프리셋 저장 실패: ${err.message}`);
    }
  });

  // Export JSON file
  btnExportEl.addEventListener('click', async () => {
    try {
      const config = getCurrentConfig();
      const res = await window.api.exportConfig(config);
      if (res && res.success) {
        alert('설정 파일이 성공적으로 내보내졌습니다.');
      }
    } catch (err) {
      alert(`내보내기 실패: ${err.message}`);
    }
  });

  // Import JSON file
  btnImportEl.addEventListener('click', async () => {
    try {
      const res = await window.api.importConfig();
      if (res && res.config) {
        onConfigLoaded(res.config);
        alert('설정 파일을 성공적으로 불러왔습니다.');
      }
    } catch (err) {
      alert(`가져오기 실패: ${err.message}`);
    }
  });

  // Initial load
  refreshPresetList();

  return { refreshPresetList };
}
