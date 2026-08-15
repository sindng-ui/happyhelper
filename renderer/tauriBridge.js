/**
 * Universal Tauri, Electron & C# WebView2 IPC Bridge
 * Automatically detects the current environment and sets up window.api.
 */
(function() {
  // 1. C# WebView2 Environment Detect
  if (window.chrome && window.chrome.webview) {
    const bridge = window.chrome.webview;
    const listeners = {
      'state-changed': [],
      'key-bound': [],
      'hotkey-notice': [],
      'pad-status': [],
      'skill-triggered': []
    };


    bridge.addEventListener('message', (event) => {
      try {
        const msg = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
        if (msg && msg.type && listeners[msg.type]) {
          listeners[msg.type].forEach(cb => cb(msg.payload));
        }
      } catch (e) {
        console.error('Error handling WebView2 message:', e);
      }
    });

    const invoke = (method, args = {}) => {
      return new Promise((resolve) => {
        const reqId = Math.random().toString(36).substring(2, 9);
        const onceHandler = (event) => {
          try {
            const msg = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
            if (msg && msg.type === 'response' && msg.reqId === reqId) {
              bridge.removeEventListener('message', onceHandler);
              resolve(msg.payload);
            }
          } catch (e) {}
        };
        bridge.addEventListener('message', onceHandler);
        bridge.postMessage(JSON.stringify({ reqId, method, args }));
      });
    };

    window.api = {
      getConfig: () => invoke('getConfig'),
      saveConfig: (config) => invoke('saveConfig', { config }),
      updateConfig: (config) => invoke('updateConfig', { config }),
      startLoop: (config) => invoke('startLoop', { config }),
      stopLoop: () => invoke('stopLoop'),
      toggleDisable: () => invoke('toggleDisable'),
      startKeyBind: () => invoke('startKeyBind'),
      cancelKeyBind: () => invoke('cancelKeyBind'),
      listPresets: () => invoke('listPresets'),
      savePreset: (name, config) => invoke('savePreset', { name, config }),
      loadPreset: (name) => invoke('loadPreset', { name }),
      deletePreset: (name) => invoke('deletePreset', { name }),
      exportConfig: (config) => invoke('exportConfig', { config }),
      importConfig: () => invoke('importConfig'),
      setAlwaysOnTop: (alwaysOnTop) => invoke('setAlwaysOnTop', { alwaysOnTop }),
      setWindowMode: (mode) => invoke('setWindowMode', { mode }),
      dragWindow: () => invoke('dragWindow'),


      minimizeWindow: () => invoke('minimizeWindow'),
      closeWindow: () => invoke('closeWindow'),

      onStateChange: (cb) => listeners['state-changed'].push(cb),
      onKeyBound: (cb) => listeners['key-bound'].push(cb),
      onHotKeyNotice: (cb) => listeners['hotkey-notice'].push(cb),
      onPadStatus: (cb) => listeners['pad-status'].push(cb),
      onSkillTriggered: (cb) => listeners['skill-triggered'].push(cb),
      getPadStatus: () => invoke('getPadStatus'),
      installViGEmDriver: () => invoke('installViGEmDriver'),
      invoke: (method, args) => invoke(method, args)
    };
    return;
  }




  // 2. Tauri Environment Detect
  if (window.__TAURI__) {
    const { invoke } = window.__TAURI__.tauri;
    const { listen } = window.__TAURI__.event;
    const { save, open } = window.__TAURI__.dialog;
    const { writeTextFile, readTextFile } = window.__TAURI__.fs;

    window.api = {
      getConfig: () => invoke('get_config'),
      saveConfig: (config) => invoke('save_config', { config }),
      updateConfig: (config) => invoke('update_config', { config }),
      startLoop: (config) => invoke('start_loop', { config }),
      stopLoop: () => invoke('stop_loop'),
      toggleDisable: () => invoke('toggle_disable'),
      startKeyBind: () => invoke('start_key_bind'),
      cancelKeyBind: () => invoke('cancel_key_bind'),
      listPresets: () => invoke('list_presets'),
      savePreset: (name, config) => invoke('save_preset', { name, config }),
      loadPreset: (name) => invoke('load_preset', { name }),
      exportConfig: async (config) => {
        try {
          const filePath = await save({
            title: '설정 내보내기',
            defaultPath: 'diablo4_skill_config.json',
            filters: [{ name: 'JSON Files', extensions: ['json'] }]
          });
          if (filePath) {
            await writeTextFile(filePath, JSON.stringify(config, null, 2));
            return { success: true, filePath };
          }
        } catch (e) {}
        return { success: false };
      },
      importConfig: async () => {
        try {
          const filePath = await open({
            title: '설정 불러오기',
            filters: [{ name: 'JSON Files', extensions: ['json'] }],
            multiple: false
          });
          if (filePath) {
            const raw = await readTextFile(filePath);
            return { success: true, config: JSON.parse(raw) };
          }
        } catch (e) {}
        return { success: false };
      },
      setAlwaysOnTop: (alwaysOnTop) => invoke('set_always_on_top', { alwaysOnTop }),
      dragWindow: () => {},
      minimizeWindow: () => invoke('minimize_window'),
      closeWindow: () => invoke('close_window'),

      onStateChange: (cb) => listen('state-changed', (ev) => cb(ev.payload)),
      onKeyBound: (cb) => listen('key-bound', (ev) => cb(ev.payload)),
      onHotKeyNotice: (cb) => listen('hotkey-notice', (ev) => cb(ev.payload))
    };
  }
})();
