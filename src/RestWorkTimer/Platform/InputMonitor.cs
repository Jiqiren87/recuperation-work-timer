using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RestWorkTimer.Platform;

/// <summary>
/// Global low-level keyboard/mouse monitor. Injected events (SendInput, AutoHotkey,
/// PyAutoGUI, ...) are ignored so automation does not start or extend work time.
/// </summary>
public sealed class InputMonitor : IDisposable
{
    private readonly NativeMethods.HookProc _keyboardProc;
    private readonly NativeMethods.HookProc _mouseProc;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private bool _disposed;

    public InputMonitor()
    {
        _keyboardProc = KeyboardHook;
        _mouseProc = MouseHook;
    }

    public event EventHandler? HumanInputDetected;

    public bool IsRunning => _keyboardHook != IntPtr.Zero && _mouseHook != IntPtr.Zero;

    public string? LastError { get; private set; }

    public bool Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InputMonitor));
        }

        if (IsRunning)
        {
            return true;
        }

        var moduleHandle = GetCurrentModuleHandle();
        _keyboardHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
        if (_keyboardHook == IntPtr.Zero)
        {
            LastError = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            return false;
        }

        _mouseHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
        if (_mouseHook == IntPtr.Zero)
        {
            LastError = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
            return false;
        }

        LastError = null;
        return true;
    }

    private static IntPtr GetCurrentModuleHandle()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var moduleName = process.MainModule?.ModuleName;
            var handle = NativeMethods.GetModuleHandle(moduleName);
            if (handle != IntPtr.Zero)
            {
                return handle;
            }
        }
        catch
        {
            // Fall through to the executable handle.
        }

        return NativeMethods.GetModuleHandle(null);
    }

    private IntPtr KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var data = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                if (!InputFilter.IsInjectedKeyboard(data.Flags))
                {
                    HumanInputDetected?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch
        {
            // A hook must never throw into user32.
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                if (!InputFilter.IsInjectedMouse(data.Flags))
                {
                    HumanInputDetected?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch
        {
            // A hook must never throw into user32.
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }
}
