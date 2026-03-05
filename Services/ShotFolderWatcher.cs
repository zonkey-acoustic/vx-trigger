using System.IO;

namespace ShotTrigger.Services;

public class ShotFolderWatcher : IDisposable
{
    private FileSystemWatcher? _watcher;
    private bool _disposed;
    private readonly string _shotsDirectory;

    public event EventHandler? ShotDetected;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? Error;

    public bool IsRunning { get; private set; }

    public ShotFolderWatcher(string shotsDirectory)
    {
        _shotsDirectory = shotsDirectory;
    }

    public void Start()
    {
        if (IsRunning || _disposed)
            return;

        if (!Directory.Exists(_shotsDirectory))
        {
            Error?.Invoke(this, $"Shots directory not found: {_shotsDirectory}");
            return;
        }

        _watcher = new FileSystemWatcher(_shotsDirectory)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
            InternalBufferSize = 16384,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnDirectoryCreated;
        _watcher.Error += OnWatcherError;

        IsRunning = true;
        StatusChanged?.Invoke(this, $"Monitoring: {_shotsDirectory}");
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnDirectoryCreated;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }

        IsRunning = false;
        StatusChanged?.Invoke(this, "Stopped");
    }

    private void OnDirectoryCreated(object sender, FileSystemEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ShotFolderWatcher: New directory detected: {e.Name}");
        ShotDetected?.Invoke(this, EventArgs.Empty);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        System.Diagnostics.Debug.WriteLine($"ShotFolderWatcher: Error: {ex.Message}");
        Error?.Invoke(this, ex.Message);

        // Attempt to restart after a brief delay
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            if (_disposed) return;
            try
            {
                Stop();
                Start();
            }
            catch (Exception restartEx)
            {
                System.Diagnostics.Debug.WriteLine($"ShotFolderWatcher: Restart failed: {restartEx.Message}");
                Error?.Invoke(this, $"Failed to restart: {restartEx.Message}");
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _disposed = true;
    }
}
