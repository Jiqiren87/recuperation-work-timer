namespace RestWorkTimer.Platform;

/// <summary>
/// Prevents idle system sleep while Rest is active. Display is allowed to turn off.
/// Must be used from the UI thread because execution state is thread-affine.
/// </summary>
public sealed class KeepAwake : IDisposable
{
    private bool _active;

    public bool IsActive => _active;

    public bool Enable()
    {
        var result = NativeMethods.SetThreadExecutionState(
            NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
        _active = result != 0;
        return _active;
    }

    public void Refresh()
    {
        if (_active)
        {
            NativeMethods.SetThreadExecutionState(
                NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
        }
    }

    public void Disable()
    {
        if (!_active)
        {
            return;
        }

        NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
        _active = false;
    }

    public void Dispose() => Disable();
}
