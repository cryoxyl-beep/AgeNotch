using System.Runtime.InteropServices;
using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Interop.CoreAudio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Services;

/// <summary>
/// Event-driven Windows Core Audio volume service.
/// Uses native COM IAudioEndpointVolume and IAudioEndpointVolumeCallback without polling.
/// </summary>
public sealed class WindowsAudioVolumeService : IAudioVolumeService, IAudioEndpointVolumeCallback, IMMNotificationClient
{
    private static readonly Guid IAudioEndpointVolumeGuid = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private const uint CLSCTX_INPROC_SERVER = 1;
    private const uint CLSCTX_ALL = 23;

    private readonly ILogger<WindowsAudioVolumeService> _logger;
    private readonly object _lock = new();

    private IMMDeviceEnumerator? _enumerator;
    private IMMDevice? _currentDevice;
    private IAudioEndpointVolume? _endpointVolume;
    private int _currentVolume = 50;
    private bool _isMuted;
    private bool _disposed;

    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    public int CurrentVolume
    {
        get
        {
            lock (_lock) return _currentVolume;
        }
    }

    public bool IsMuted
    {
        get
        {
            lock (_lock) return _isMuted;
        }
    }

    public WindowsAudioVolumeService(ILogger<WindowsAudioVolumeService>? logger = null)
    {
        _logger = logger ?? NullLogger<WindowsAudioVolumeService>.Instance;
        InitializeCoreAudio();
    }

    private void InitializeCoreAudio()
    {
        try
        {
            _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            _enumerator.RegisterEndpointNotificationCallback(this);

            BindToDefaultDevice();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Windows Core Audio volume subsystem.");
        }
    }

    private void BindToDefaultDevice()
    {
        lock (_lock)
        {
            UnbindCurrentEndpoint();

            if (_enumerator == null) return;

            try
            {
                int hr = _enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
                if (hr != 0 || device == null)
                {
                    hr = _enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device);
                }

                if (hr != 0 || device == null)
                {
                    _logger.LogWarning("No default audio render endpoint available on system (hr: 0x{Hr:X8}).", hr);
                    return;
                }

                _currentDevice = device;
                var guid = IAudioEndpointVolumeGuid;
                hr = _currentDevice.Activate(ref guid, CLSCTX_INPROC_SERVER, IntPtr.Zero, out var volumeObj);
                if ((hr != 0 || volumeObj == null) && hr != 0)
                {
                    hr = _currentDevice.Activate(ref guid, CLSCTX_ALL, IntPtr.Zero, out volumeObj);
                }
                if (hr != 0 || volumeObj == null)
                {
                    _logger.LogWarning("Failed to activate IAudioEndpointVolume on default audio device (hr: 0x{Hr:X8}).", hr);
                    return;
                }

                _endpointVolume = (IAudioEndpointVolume)volumeObj;
                _endpointVolume.RegisterControlChangeNotify(this);

                // Read initial volume and mute state
                if (_endpointVolume.GetMasterVolumeLevelScalar(out float level) == 0)
                {
                    _currentVolume = (int)Math.Round(level * 100.0f);
                }

                if (_endpointVolume.GetMute(out bool muted) == 0)
                {
                    _isMuted = muted;
                }

                _logger.LogInformation("Core Audio volume initialized: {Volume}%, Muted: {Muted}", _currentVolume, _isMuted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception binding to default audio endpoint.");
            }
        }
    }

    private void UnbindCurrentEndpoint()
    {
        if (_endpointVolume != null)
        {
            try
            {
                _endpointVolume.UnregisterControlChangeNotify(this);
                Marshal.ReleaseComObject(_endpointVolume);
            }
            catch
            {
                // Ignore COM cleanup exceptions
            }
            _endpointVolume = null;
        }

        if (_currentDevice != null)
        {
            try
            {
                Marshal.ReleaseComObject(_currentDevice);
            }
            catch
            {
                // Ignore COM cleanup exceptions
            }
            _currentDevice = null;
        }
    }

    public int OnNotify(IntPtr pNotify)
    {
        if (pNotify == IntPtr.Zero || _disposed) return 0;

        try
        {
            var data = Marshal.PtrToStructure<AUDIO_VOLUME_NOTIFICATION_DATA>(pNotify);
            int newVolume = (int)Math.Round(data.fMasterVolume * 100.0f);
            bool newMuted = data.bMuted;

            bool changed = false;
            lock (_lock)
            {
                if (_currentVolume != newVolume || _isMuted != newMuted)
                {
                    _currentVolume = newVolume;
                    _isMuted = newMuted;
                    changed = true;
                }
            }

            if (changed)
            {
                _logger.LogDebug("Audio volume changed event: {Volume}%, Muted: {Muted}", newVolume, newMuted);
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(newVolume, newMuted));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Core Audio volume notification.");
        }

        return 0;
    }

    public Task SetVolumeAsync(int volumePercent)
    {
        volumePercent = Math.Clamp(volumePercent, 0, 100);
        float scalar = volumePercent / 100.0f;

        lock (_lock)
        {
            if (_endpointVolume != null)
            {
                try
                {
                    var ctx = Guid.Empty;
                    _endpointVolume.SetMasterVolumeLevelScalar(scalar, ref ctx);
                    _currentVolume = volumePercent;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to set master audio volume.");
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task ToggleMuteAsync()
    {
        lock (_lock)
        {
            if (_endpointVolume != null)
            {
                try
                {
                    bool newMute = !_isMuted;
                    var ctx = Guid.Empty;
                    _endpointVolume.SetMute(newMute, ref ctx);
                    _isMuted = newMute;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to toggle master audio mute.");
                }
            }
        }

        return Task.CompletedTask;
    }

    // IMMNotificationClient callbacks
    public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId)
    {
        if (flow == EDataFlow.eRender && (role == ERole.eMultimedia || role == ERole.eConsole))
        {
            _logger.LogInformation("Default audio render device changed. Re-binding endpoint.");
            BindToDefaultDevice();

            // Emit current volume of newly bound device
            VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(CurrentVolume, IsMuted));
        }
    }

    public void OnDeviceStateChanged(string pwstrDeviceId, uint dwNewState) { }
    public void OnDeviceAdded(string pwstrDeviceId) { }
    public void OnDeviceRemoved(string pwstrDeviceId) { }
    public void OnPropertyValueChanged(string pwstrDeviceId, IntPtr key) { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            UnbindCurrentEndpoint();

            if (_enumerator != null)
            {
                try
                {
                    _enumerator.UnregisterEndpointNotificationCallback(this);
                    Marshal.ReleaseComObject(_enumerator);
                }
                catch
                {
                    // Ignore COM cleanup exceptions
                }
                _enumerator = null;
            }
        }
    }
}
