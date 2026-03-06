namespace VXTrigger.Services;

public class TriggerOrchestrator : IDisposable
{
    private ShotFolderWatcher? _watcher;
    private readonly AudioTriggerService _audioTrigger;
    private readonly NetworkTriggerService _networkTrigger;
    private readonly SwingVideoService _swingVideo;
    private TriggerSettings _settings;
    private bool _disposed;

    private DateTime _lastTriggerTime = DateTime.MinValue;
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);
    private readonly object _triggerLock = new();

    public event EventHandler<string>? StatusChanged;
    public event EventHandler? ShotFired;

    public TriggerSettings Settings => _settings;
    public AudioTriggerService AudioTrigger => _audioTrigger;
    public NetworkTriggerService NetworkTrigger => _networkTrigger;
    public SwingVideoService SwingVideo => _swingVideo;
    public bool IsRunning => _watcher?.IsRunning ?? false;
    public int ShotCount { get; private set; }
    public DateTime? LastShotTime { get; private set; }

    public TriggerOrchestrator()
    {
        _settings = TriggerSettings.Load();
        _audioTrigger = new AudioTriggerService();
        _networkTrigger = new NetworkTriggerService();
        _swingVideo = new SwingVideoService();

        ApplySettings();
    }

    public void ApplySettings()
    {
        _audioTrigger.IsEnabled = _settings.AudioTriggerEnabled;
        _audioTrigger.SelectedDeviceIndex = _settings.SelectedDeviceIndex;
        _audioTrigger.SelectedDeviceName = _settings.SelectedDeviceName;
        _audioTrigger.ToneFrequencyHz = _settings.ToneFrequencyHz;
        _audioTrigger.ToneNoiseDecay = _settings.ToneNoiseDecay;
        _audioTrigger.ToneToneDecay = _settings.ToneToneDecay;
        _audioTrigger.ToneMix = _settings.ToneMix;
        _audioTrigger.ToneDurationMs = _settings.ToneDurationMs;

        _networkTrigger.IsEnabled = _settings.NetworkTriggerEnabled;
        _networkTrigger.Port = _settings.NetworkTriggerPort;
        _networkTrigger.TargetHost = _settings.NetworkTriggerHost;

        _swingVideo.IsEnabled = _settings.SwingVideoEnabled;
        _swingVideo.SourcePath = _settings.SwingVideoSourcePath;
        _swingVideo.DestinationPath = _settings.SwingVideoDestinationPath;
    }

    public void Start()
    {
        Stop();

        _watcher = new ShotFolderWatcher(_settings.ShotsDirectoryPath);
        _watcher.ShotDetected += OnShotDetected;
        _watcher.StatusChanged += (_, status) => StatusChanged?.Invoke(this, status);
        _watcher.Error += (_, error) => StatusChanged?.Invoke(this, $"Error: {error}");
        _watcher.Start();
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.ShotDetected -= OnShotDetected;
            _watcher.Dispose();
            _watcher = null;
            StatusChanged?.Invoke(this, "Stopped");
        }
    }

    public void SaveSettings()
    {
        _settings.Save();
        ApplySettings();
    }

    public void ReloadSettings()
    {
        _settings = TriggerSettings.Load();
        ApplySettings();
    }

    private void OnShotDetected(object? sender, EventArgs e)
    {
        lock (_triggerLock)
        {
            var now = DateTime.Now;
            if (now - _lastTriggerTime < _debounceInterval)
                return;

            _lastTriggerTime = now;
            ShotCount++;
            LastShotTime = now;
        }

        _audioTrigger.PlayTriggerTone();
        _networkTrigger.SendTriggerPacket();
        _swingVideo.OnShotDetected();

        ShotFired?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _audioTrigger.Dispose();
        _networkTrigger.Dispose();
        _swingVideo.Dispose();
        _disposed = true;
    }
}
