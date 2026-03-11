using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace D2RPriceChecker.Services
{
    public class HotkeyManager : IDisposable
    {
        private readonly IntPtr _handle;
        private readonly HwndSource _source;
        private readonly Dictionary<int, Action> _actions = new();
        private int _currentId = 0;

        public HotkeyManager(IntPtr handle)
        {
            _handle = handle;
            _source = HwndSource.FromHwnd(handle);
            _source.AddHook(HwndHook);
        }

        public void Register(Key key, ModifierKeys modifiers, Action action)
        {
            _currentId++;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            uint mod = (uint)modifiers;

            RegisterHotKey(_handle, _currentId, mod, vk);
            _actions[_currentId] = action;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();

                if (_actions.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (var id in _actions.Keys)
                UnregisterHotKey(_handle, id);

            _source.RemoveHook(HwndHook);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
