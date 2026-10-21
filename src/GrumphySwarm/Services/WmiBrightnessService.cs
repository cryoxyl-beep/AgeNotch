using System.Management;
using GrumphySwarm.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Services;

/// <summary>
/// Screen brightness monitoring and control using Windows WMI (root\wmi).
/// Employs event-driven WmiMonitorBrightnessEvent watcher for supported displays.
/// Gracefully falls back when displays do not support WMI brightness control.
/// </summary>
public sealed class WmiBrightnessService : IBrightnessService
{
    private readonly ILogger<WmiBrightnessService> _logger;
    private readonly object _lock = new();

    private ManagementEventWatcher? _eventWatcher;
    private int _currentBrightness = 50;
    private bool _isSupported;
    private string _hardwareDescription = "Checking...";
    private bool _disposed;

    public event EventHandler<BrightnessChangedEventArgs>? BrightnessChanged;

    public int CurrentBrightness
    {
        get
        {
            lock (_lock) return _currentBrightness;
        }
    }

    public bool IsSupported
    {
        get
        {
            lock (_lock) return _isSupported;
        }
    }

    public string HardwareDescription
    {
        get
        {
            lock (_lock) return _hardwareDescription;
        }
    }

    public WmiBrightnessService(ILogger<WmiBrightnessService>? logger = null)
    {
        _logger = logger ?? NullLogger<WmiBrightnessService>.Instance;
        InitializeBrightnessSubsystem();
    }

    private void InitializeBrightnessSubsystem()
    {
        try
        {
            // 1. Initial brightness query
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentBrightness, Active, InstanceName FROM WmiMonitorBrightness");
            using var collection = searcher.Get();

            bool found = false;
            foreach (ManagementObject obj in collection)
            {
                using (obj)
                {
                    if (obj["CurrentBrightness"] is byte b)
                    {
                        _currentBrightness = b;
                        _isSupported = true;
                        _hardwareDescription = obj["InstanceName"]?.ToString() ?? "WMI Built-in Monitor";
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                _isSupported = false;
                _hardwareDescription = "Hardware display does not expose WmiMonitorBrightness";
                _logger.LogInformation("WMI monitor brightness is not supported on this display interface: {Desc}", _hardwareDescription);
                return;
            }

            _logger.LogInformation("Display brightness initialized: {Brightness}%, Hardware: {Desc}", _currentBrightness, _hardwareDescription);

            // 2. Event-driven brightness watcher
            _eventWatcher = new ManagementEventWatcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessEvent");
            _eventWatcher.EventArrived += OnBrightnessEventArrived;
            _eventWatcher.Start();

            _logger.LogInformation("WmiMonitorBrightnessEvent watcher started successfully.");
        }
        catch (ManagementException mex)
        {
            _isSupported = false;
            _hardwareDescription = $"WMI Unavailable ({mex.ErrorCode})";
            _logger.LogInformation("WMI brightness unavailable on this platform: {Message}", mex.Message);
        }
        catch (Exception ex)
        {
            _isSupported = false;
            _hardwareDescription = "Brightness subsystem initialization error";
            _logger.LogWarning(ex, "Failed to initialize WMI brightness subsystem.");
        }
    }

    private void OnBrightnessEventArrived(object sender, EventArrivedEventArgs e)
    {
        if (_disposed) return;

        try
        {
            if (e.NewEvent.Properties["Brightness"]?.Value is byte b)
            {
                int newBrightness = b;
                bool changed = false;

                lock (_lock)
                {
                    if (_currentBrightness != newBrightness)
                    {
                        _currentBrightness = newBrightness;
                        changed = true;
                    }
                }

                if (changed)
                {
                    _logger.LogDebug("Hardware brightness changed event: {Brightness}%", newBrightness);
                    BrightnessChanged?.Invoke(this, new BrightnessChangedEventArgs(newBrightness));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WMI brightness event.");
        }
    }

    public Task SetBrightnessAsync(int brightnessPercent)
    {
        if (!_isSupported) return Task.CompletedTask;

        brightnessPercent = Math.Clamp(brightnessPercent, 0, 100);

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessMethods");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                using (obj)
                {
                    obj.InvokeMethod("WmiSetBrightness", new object[] { 1u, (byte)brightnessPercent });
                    lock (_lock)
                    {
                        _currentBrightness = brightnessPercent;
                    }
                    BrightnessChanged?.Invoke(this, new BrightnessChangedEventArgs(brightnessPercent));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set display brightness via WMI.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_eventWatcher != null)
        {
            try
            {
                _eventWatcher.Stop();
                _eventWatcher.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            _eventWatcher = null;
        }
    }
}
