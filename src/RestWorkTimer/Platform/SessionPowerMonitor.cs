using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RestWorkTimer.Platform;

/// <summary>
/// Hidden message window for workstation lock/unlock and power suspend/resume notifications.
/// </summary>
public sealed class SessionPowerMonitor : NativeWindow, IDisposable
{
    private bool _registered;
    private bool _disposed;

    public SessionPowerMonitor()
    {
        CreateHandle(new CreateParams
        {
            Caption = "RestWorkTimer.SessionPower",
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0
        });

        _registered = NativeMethods.WTSRegisterSessionNotification(
            Handle, NativeMethods.NOTIFY_FOR_THIS_SESSION);

        if (!_registered)
        {
            var error = new Win32Exception(Marshal.GetLastWin32Error());
            DestroyHandle();
            throw new InvalidOperationException(
                $"无法注册会话锁屏通知：{error.Message}", error);
        }
    }

    public event EventHandler? SessionLocked;
    public event EventHandler? SessionUnlocked;
    public event EventHandler? SystemSuspending;
    public event EventHandler? SystemResumed;

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case NativeMethods.WM_WTSSESSION_CHANGE:
                switch (m.WParam.ToInt32())
                {
                    case NativeMethods.WTS_SESSION_LOCK:
                        SessionLocked?.Invoke(this, EventArgs.Empty);
                        break;
                    case NativeMethods.WTS_SESSION_UNLOCK:
                        SessionUnlocked?.Invoke(this, EventArgs.Empty);
                        break;
                }
                break;

            case NativeMethods.WM_POWERBROADCAST:
                switch (m.WParam.ToInt32())
                {
                    case NativeMethods.PBT_APMSUSPEND:
                        SystemSuspending?.Invoke(this, EventArgs.Empty);
                        break;
                    case NativeMethods.PBT_APMRESUMEAUTOMATIC:
                    case NativeMethods.PBT_APMRESUMESUSPEND:
                    case NativeMethods.PBT_APMRESUMECRITICAL:
                        SystemResumed?.Invoke(this, EventArgs.Empty);
                        break;
                }
                break;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_registered)
        {
            NativeMethods.WTSUnRegisterSessionNotification(Handle);
            _registered = false;
        }

        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }

        GC.SuppressFinalize(this);
    }
}
