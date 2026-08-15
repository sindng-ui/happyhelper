import { getKeyLabel, DOM_TO_UIOHOOK } from '../utils/keyCodes.js';

/**
 * SkillSlot Component
 * Renders individual skill slot with checkbox, key binding trigger, and interval (ms) input
 */
export function createSkillSlot(slot, onUpdate, onRequestKeyBind) {
  const row = document.createElement('div');
  row.className = `skill-slot ${slot.enabled ? 'active' : 'disabled'}`;
  row.dataset.slotId = slot.id;

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
      <button type="button" class="btn-key-bind ${slot.isBinding ? 'binding' : ''}" title="클릭하여 발동 키 변경">
        <span class="key-text">${slot.isBinding ? '키 입력...' : getKeyLabel(slot.keyCode)}</span>
      </button>
    </div>

    <div class="slot-interval-wrap">
      <div class="interval-input-group">
        <input type="number" class="slot-interval" value="${slot.intervalMs}" min="10" max="60000" step="50" title="발동 주기 (ms)" />
        <span class="unit">ms</span>
      </div>
    </div>
  `;

  // 1. Checkbox change
  const checkInput = row.querySelector('.slot-enabled');
  checkInput.addEventListener('change', (e) => {
    slot.enabled = e.target.checked;
    if (slot.enabled) {
      row.classList.remove('disabled');
      row.classList.add('active');
    } else {
      row.classList.remove('active');
      row.classList.add('disabled');
    }
    onUpdate(slot);
  });

  // 2. Key Bind button click
  const bindBtn = row.querySelector('.btn-key-bind');
  bindBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    onRequestKeyBind(slot);
  });

  // 3. Interval change
  const intervalInput = row.querySelector('.slot-interval');
  intervalInput.addEventListener('change', (e) => {
    let val = parseInt(e.target.value, 10);
    if (isNaN(val) || val < 10) val = 10;
    if (val > 60000) val = 60000;
    slot.intervalMs = val;
    e.target.value = val;
    onUpdate(slot);
  });

  return row;
}
