using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AviationCrosshair.Hotkeys
{
    /// <summary>
    /// Registers real, system-wide global hotkeys via the Win32 RegisterHotKey API,
    /// so they fire even when Aviation Crosshair does not have focus (needed since
    /// the overlay is click-through / has no focus at all).
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        private readonly HwndSource _source;
        private readonly Dictionary<int, Action> _handlers = new();
        private readonly Dictionary<int, (uint mods, uint vk)> _registered = new();
        private int _nextId = 1;

        public HotkeyManager(Window window)
        {
            var helper = new WindowInteropHelper(window);
            // Window must already have a handle (call after window is shown, or use EnsureHandle via a hidden helper window).
            if (helper.Handle == IntPtr.Zero)
                helper.EnsureHandle();
            _source = HwndSource.FromHwnd(helper.Handle)!;
            _source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_handlers.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Register a hotkey from a string like "F8" or "Ctrl+Shift+F1".
        /// Returns false if the combination is already claimed by another app
        /// (Windows returns failure from RegisterHotKey in that case).
        /// </summary>
        public bool TryRegister(string keyName, Action onPressed, out int id)
        {
            id = -1;
            if (!TryParse(keyName, out uint mods, out uint vk))
                return false;

            int newId = _nextId++;
            bool ok = RegisterHotKey(_source.Handle, newId, mods, vk);
            if (!ok) return false;

            _handlers[newId] = onPressed;
            _registered[newId] = (mods, vk);
            id = newId;
            return true;
        }

        public void Unregister(int id)
        {
            if (_registered.ContainsKey(id))
            {
                UnregisterHotKey(_source.Handle, id);
                _registered.Remove(id);
                _handlers.Remove(id);
            }
        }

        public void UnregisterAll()
        {
            foreach (var id in new List<int>(_registered.Keys))
                Unregister(id);
        }

        private static bool TryParse(string keyName, out uint mods, out uint vk)
        {
            mods = 0;
            vk = 0;
            if (string.IsNullOrWhiteSpace(keyName)) return false;

            var parts = keyName.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            string keyPart = parts[^1];

            foreach (var part in parts[..^1])
            {
                switch (part.ToLowerInvariant())
                {
                    case "ctrl": case "control": mods |= 0x0002; break;
                    case "alt": mods |= 0x0001; break;
                    case "shift": mods |= 0x0004; break;
                    case "win": mods |= 0x0008; break;
                }
            }

            if (!Enum.TryParse<Key>(keyPart, true, out var key))
                return false;

            vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            return vk != 0;
        }

        public void Dispose()
        {
            UnregisterAll();
            _source.RemoveHook(WndProc);
        }
    }
}
