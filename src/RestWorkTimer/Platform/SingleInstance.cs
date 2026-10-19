namespace RestWorkTimer.Platform;

public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\RestWorkTimer.SingleInstance";
    private Mutex? _mutex;
    private bool _owned;

    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        _owned = createdNew;
        return createdNew;
    }

    public void Dispose()
    {
        if (_mutex is null)
        {
            return;
        }

        if (_owned)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Already released; nothing to do.
            }

            _owned = false;
        }

        _mutex.Dispose();
        _mutex = null;
    }
}
