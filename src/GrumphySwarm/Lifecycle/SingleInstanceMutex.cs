namespace GrumphySwarm.Lifecycle;

/// <summary>
/// Ensures single-instance application execution using a named operating system mutex.
/// </summary>
public sealed class SingleInstanceMutex : IDisposable
{
    private const string MutexBaseName = "GrumphySwarm_SingleInstanceMutex";
    private readonly Mutex _mutex;
    private readonly bool _hasHandle;
    private bool _disposed;

    public SingleInstanceMutex()
    {
        var mutexName = $"{MutexBaseName}_{Environment.UserName}";
        _mutex = new Mutex(true, mutexName, out _hasHandle);
    }

    public bool IsOnlyInstance => _hasHandle;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_hasHandle)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex already released
            }
        }

        _mutex.Dispose();
    }
}
