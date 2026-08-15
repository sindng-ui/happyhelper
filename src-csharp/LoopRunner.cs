using System;
using System.Collections.Generic;
using System.Threading;

namespace HappyHelper
{
    public class LoopRunner
    {
        private AppConfig _config;
        private volatile bool _running = false;
        private volatile bool _disabled = false;
        private CancellationTokenSource _cts = null;
        private readonly Random _rand = new Random();
        private readonly object _lock = new object();

        public bool Running { get { return _running; } }
        public bool Disabled { get { return _disabled; } }

        public event Action<bool, bool> StateChanged; // running, disabled
        public event Action<string, int> SkillTriggered; // slotId, keyCode


        public void Start(AppConfig config)
        {
            lock (_lock)
            {
                StopInternal();
                _config = config;
                _running = true;
                _disabled = false;
                _cts = new CancellationTokenSource();

                StartWorkerThreads(_cts.Token);

                var handler = StateChanged;
                if (handler != null) handler(_running, _disabled);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                StopInternal();

                var handler = StateChanged;
                if (handler != null) handler(_running, _disabled);
            }
        }

        private void StopInternal()
        {
            _running = false;
            _disabled = false;
            if (_cts != null)
            {
                try
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
                catch { }
                _cts = null;
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                if (!_running) return;
                if (_disabled) return;

                _disabled = true;
                DebugLog.Write("[LoopRunner] Paused (Disabled = true)");

                var handler = StateChanged;
                if (handler != null) handler(_running, _disabled);
            }
        }

        public void Resume()
        {
            lock (_lock)
            {
                if (!_running) return;
                if (!_disabled) return;

                _disabled = false;
                DebugLog.Write("[LoopRunner] Resumed (Disabled = false)");

                var handler = StateChanged;
                if (handler != null) handler(_running, _disabled);
            }
        }

        public void ToggleDisable()
        {
            Pause();
        }

        public void UpdateConfig(AppConfig config)

        {
            lock (_lock)
            {
                _config = config;
                if (_running)
                {
                    if (_cts != null)
                    {
                        try { _cts.Cancel(); _cts.Dispose(); } catch { }
                    }
                    _cts = new CancellationTokenSource();
                    StartWorkerThreads(_cts.Token);
                }
            }
        }

        private void StartWorkerThreads(CancellationToken token)
        {
            if (_config == null || _config.slots == null) return;

            DebugLog.Write("[LoopRunner] StartWorkerThreads: " + _config.slots.Count + " total slots");
            foreach (var slot in _config.slots)
            {
                DebugLog.Write(string.Format("[LoopRunner] Slot id={0} enabled={1} keyCode={2} intervalMs={3} key={4}",
                    slot.id, slot.enabled, slot.keyCode, slot.intervalMs, slot.key));
                if (!slot.enabled) continue;

                DebugLog.Write("[LoopRunner] STARTING thread for id=" + slot.id + " keyCode=" + slot.keyCode + " interval=" + slot.intervalMs + "ms");
                var currentSlot = slot;
                var th = new Thread(() => RunSlotLoop(currentSlot, token));
                th.IsBackground = true;
                th.Start();
            }
        }

        private void RunSlotLoop(SkillSlotConfig slot, CancellationToken token)
        {
            if (slot == null || !slot.enabled) return;

            DebugLog.Write(string.Format("[LoopRunner] Loop Started for slot {0} with interval={1}ms", slot.id, slot.intervalMs));

            while (!token.IsCancellationRequested && _running && slot.enabled)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                if (!_disabled && slot.enabled)
                {
                    try
                    {
                        InputEngine.SendAction(slot.keyCode);
                        var triggerHandler = SkillTriggered;
                        if (triggerHandler != null) triggerHandler(slot.id, slot.keyCode);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Loop execution error: " + ex.Message);
                    }
                }


                // Calculate target delay with permanent Gaussian human jittering (Always ON)
                int targetDelay = Math.Max(10, slot.intervalMs);
                lock (_rand)
                {
                    // Generate Gaussian Jitter (Mean = 0, StdDev = 20ms)
                    double gaussian = NextGaussian(_rand, 0, 20);
                    int jitter = (int)Math.Round(gaussian);
                    if (jitter < -60) jitter = -60;
                    if (jitter > 60) jitter = 60;
                    targetDelay = Math.Max(10, targetDelay + jitter);
                }


                // Wait remaining time precisely using Stopwatch
                while (!token.IsCancellationRequested && _running && slot.enabled)
                {
                    long remainingMs = targetDelay - sw.ElapsedMilliseconds;
                    if (remainingMs <= 0) break;

                    int sleepChunk = (int)Math.Min(15, remainingMs);
                    Thread.Sleep(sleepChunk);
                }
            }
            DebugLog.Write(string.Format("[LoopRunner] Loop Stopped for slot {0}", slot.id));
        }

        private static double NextGaussian(Random rand, double mean, double stdDev)
        {
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }
}


