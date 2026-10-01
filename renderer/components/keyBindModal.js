/**
 * KeyBindModal Component
 * Manages key binding overlay, keyboard & mouse (including side buttons X1/X2) capture,
 * and prevents browser default navigation actions (Back/Forward).
 */

(function () {
  let currentTarget = null;
  let boundCallback = null;
  let cancelCallback = null;

  function initKeyBindModal(onBound, onCancel) {
    boundCallback = onBound;
    cancelCallback = onCancel;

    const btnCancel = document.getElementById('btnCancelKeyBind');
    if (btnCancel) {
      btnCancel.addEventListener('click', (e) => {
        e.stopPropagation();
        cancelKeyBinding();
      });
    }

    // Prevent browser history navigation on mouse side buttons (X1/X2) & capture inside browser
    const handleMouseButton = (e) => {
      // Mouse button numbers: 0=Left, 1=Middle, 2=Right, 3=BrowserBack (X1), 4=BrowserForward (X2)
      if (e.button === 3 || e.button === 4) {
        e.preventDefault();
        e.stopPropagation();
      }

      if (!currentTarget) return;

      let mouseCode = 0;
      if (e.button === 1) mouseCode = 1003; // Middle click
      else if (e.button === 2) mouseCode = 1002; // Right click
      else if (e.button === 3) mouseCode = 1004; // Mouse4 (X1)
      else if (e.button === 4) mouseCode = 1005; // Mouse5 (X2)

      if (mouseCode > 0) {
        e.preventDefault();
        e.stopPropagation();
        handleKeyBound({ keyCode: mouseCode, isMouse: true });
      }
    };

    window.addEventListener('auxclick', handleMouseButton, true);
    window.addEventListener('mouseup', handleMouseButton, true);
    window.addEventListener('contextmenu', (e) => {
      if (currentTarget) {
        e.preventDefault();
      }
    }, true);

    // Keyboard Escape cancellation
    window.addEventListener('keydown', (e) => {
      if (!currentTarget) return;
      if (e.key === 'Escape') {
        e.preventDefault();
        cancelKeyBinding();
      }
    }, true);
  }

  function openKeyBindModal(title, target) {
    currentTarget = target;
    const titleEl = document.getElementById('keyBindTitle');
    const overlay = document.getElementById('keyBindOverlay');
    if (titleEl) titleEl.textContent = title;
    if (overlay) overlay.classList.remove('hidden');

    if (window.api && window.api.startKeyBind) {
      window.api.startKeyBind();
    }
  }

  function cancelKeyBinding() {
    const overlay = document.getElementById('keyBindOverlay');
    if (overlay) overlay.classList.add('hidden');
    currentTarget = null;
    if (window.api && window.api.cancelKeyBind) {
      window.api.cancelKeyBind();
    }
    if (cancelCallback) cancelCallback();
  }

  function handleKeyBound(keyData) {
    const overlay = document.getElementById('keyBindOverlay');
    if (!currentTarget) {
      if (overlay) overlay.classList.add('hidden');
      return;
    }
    if (!keyData) {
      cancelKeyBinding();
      return;
    }

    const { keyCode } = keyData;

    // 1. ESC Key (keyCode === 1) cancels
    if (keyCode === 1) {
      cancelKeyBinding();
      return;
    }

    // 2. Mouse Left Click (keyCode === 1001) is ignored for UI safety
    if (keyCode === 1001) {
      return;
    }

    if (overlay) overlay.classList.add('hidden');
    const target = currentTarget;
    currentTarget = null;

    if (boundCallback) {
      boundCallback(target, keyCode);
    }
  }

  function isBinding() {
    return currentTarget !== null;
  }

  window.KeyBindModal = {
    initKeyBindModal,
    openKeyBindModal,
    cancelKeyBinding,
    handleKeyBound,
    isBinding
  };
})();
