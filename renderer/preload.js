const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('api', {
  // Config & State
  getConfig: () => ipcRenderer.invoke('get-config'),
  saveConfig: (config) => ipcRenderer.invoke('save-config', config),
  updateConfig: (config) => ipcRenderer.invoke('update-config', config),

  // Control
  startLoop: (config) => ipcRenderer.invoke('start-loop', config),
  stopLoop: () => ipcRenderer.invoke('stop-loop'),
  toggleDisable: () => ipcRenderer.invoke('toggle-disable'),

  // Key Binding Mode
  startKeyBind: () => ipcRenderer.invoke('start-key-bind'),
  cancelKeyBind: () => ipcRenderer.invoke('cancel-key-bind'),

  // Presets
  listPresets: () => ipcRenderer.invoke('list-presets'),
  savePreset: (name, config) => ipcRenderer.invoke('save-preset', name, config),
  loadPreset: (name) => ipcRenderer.invoke('load-preset', name),
  exportConfig: (config) => ipcRenderer.invoke('export-config', config),
  importConfig: () => ipcRenderer.invoke('import-config'),

  // Window Controls
  setAlwaysOnTop: (alwaysOnTop) => ipcRenderer.invoke('set-always-on-top', alwaysOnTop),
  minimizeWindow: () => ipcRenderer.invoke('minimize-window'),
  closeWindow: () => ipcRenderer.invoke('close-window'),

  // Event Listeners from Main
  onStateChange: (callback) => {
    ipcRenderer.on('state-changed', (event, data) => callback(data));
  },
  onKeyBound: (callback) => {
    ipcRenderer.on('key-bound', (event, data) => callback(data));
  },
  onHotKeyNotice: (callback) => {
    ipcRenderer.on('hotkey-notice', (event, type) => callback(type));
  }
});
