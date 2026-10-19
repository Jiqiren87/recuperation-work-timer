namespace RestWorkTimer.Platform;

using System.Windows.Forms;

public static class FullscreenDetector
{
    private const int QunsBusy = 2;
    private const int QunsRunningD3DFullScreen = 3;
    private const int QunsPresentationMode = 4;
    private const int QunsQuietTime = 6;

    /// <summary>
    /// True when Windows reports a full-screen / busy state where a normal
    /// topmost window is often hidden behind the app or game. In that case the
    /// reminder must briefly take foreground to become visible.
    /// </summary>
    public static bool IsExclusiveFullscreen()
    {
        try
        {
            var result = NativeMethods.SHQueryUserNotificationState(out var state);
            if (result == 0)
            {
                var shellSaysBusy = state is QunsBusy
                    or QunsRunningD3DFullScreen
                    or QunsPresentationMode
                    or QunsQuietTime;
                if (!shellSaysBusy)
                {
                    return false;
                }
            }
        }
        catch
        {
            // Fall through to geometry.
        }

        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(foreground, out var rect))
        {
            return false;
        }

        var screen = Screen.FromHandle(foreground);
        var bounds = screen.Bounds;
        return rect.Left <= bounds.Left
               && rect.Top <= bounds.Top
               && rect.Right >= bounds.Right
               && rect.Bottom >= bounds.Bottom;
    }
}
