import { getKeyLabel } from '../utils/keyCodes.js';

/**
 * DisableKeys Component
 * Manages multiple toggle disable keys list with add/remove UI
 */
export function renderDisableKeys(container, disableKeys, onUpdate, onRequestAddKey) {
  container.innerHTML = '';

  const listWrap = document.createElement('div');
  listWrap.className = 'disable-keys-list';

  disableKeys.forEach((item, index) => {
    const chip = document.createElement('div');
    chip.className = 'key-chip';
    chip.innerHTML = `
      <span class="chip-key">${getKeyLabel(item.keyCode)}</span>
      <button type="button" class="btn-remove-chip" title="삭제" data-index="${index}">&times;</button>
    `;

    const removeBtn = chip.querySelector('.btn-remove-chip');
    removeBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      disableKeys.splice(index, 1);
      onUpdate(disableKeys);
    });

    listWrap.appendChild(chip);
  });

  // Add Key Button
  const addBtn = document.createElement('button');
  addBtn.type = 'button';
  addBtn.className = 'btn-add-key';
  addBtn.innerHTML = `<span>+ 키 추가</span>`;
  addBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    onRequestAddKey();
  });

  listWrap.appendChild(addBtn);
  container.appendChild(listWrap);
}
