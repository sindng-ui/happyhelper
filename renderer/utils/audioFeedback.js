/**
 * Low-latency Web Audio API Sound Feedback
 */
class SoundFeedback {
  constructor() {
    this.ctx = null;
    this.enabled = true;
  }

  init() {
    if (!this.ctx) {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      if (AudioCtx) {
        this.ctx = new AudioCtx();
      }
    }
  }

  playBeep(freq, type = 'sine', duration = 0.08) {
    if (!this.enabled) return;
    try {
      this.init();
      if (!this.ctx) return;
      if (this.ctx.state === 'suspended') {
        this.ctx.resume();
      }

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
    } catch (e) {
      console.warn('Audio feedback failed:', e);
    }
  }

  playStart() {
    this.playBeep(659, 'sine', 0.08); // E5
    setTimeout(() => this.playBeep(880, 'sine', 0.12), 60); // A5
  }

  playStop() {
    this.playBeep(587, 'sine', 0.08); // D5
    setTimeout(() => this.playBeep(392, 'sine', 0.14), 60); // G4
  }

  playToggle(disabled) {
    if (disabled) {
      this.playBeep(440, 'triangle', 0.1);
    } else {
      this.playBeep(880, 'triangle', 0.1);
    }
  }

  setEnabled(enabled) {
    this.enabled = enabled;
  }
}

export const soundFeedback = new SoundFeedback();
