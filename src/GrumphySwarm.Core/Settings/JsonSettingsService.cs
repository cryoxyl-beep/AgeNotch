using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Core.Settings;

/// <summary>
/// JSON file-based settings persistence implementation.
/// Reads and writes settings in the user's LocalApplicationData folder.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsFilePath;
    private readonly ILogger<JsonSettingsService> _logger;
    private readonly object _syncRoot = new();
    private NotchSettings _currentSettings;

    public JsonSettingsService(string? customPath = null, ILogger<JsonSettingsService>? logger = null)
    {
        _logger = logger ?? NullLogger<JsonSettingsService>.Instance;

        if (string.IsNullOrWhiteSpace(customPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "GrumphySwarm");
            _settingsFilePath = Path.Combine(appFolder, "settings.json");
        }
        else
        {
            _settingsFilePath = customPath;
        }

        _currentSettings = new NotchSettings();
    }

    public NotchSettings CurrentSettings
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentSettings.Clone();
            }
        }
    }

    public event EventHandler<NotchSettings>? SettingsChanged;

    public NotchSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Settings file not found at {Path}. Using default settings.", _settingsFilePath);
                return CurrentSettings;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize<NotchSettings>(json, SerializerOptions);

            if (loaded != null)
            {
                lock (_syncRoot)
                {
                    _currentSettings = loaded;
                }
                _logger.LogInformation("Loaded settings successfully from {Path}.", _settingsFilePath);
                SettingsChanged?.Invoke(this, CurrentSettings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}. Retaining current defaults.", _settingsFilePath);
        }

        return CurrentSettings;
    }

    public async Task<NotchSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Settings file not found at {Path}. Using default settings.", _settingsFilePath);
                return CurrentSettings;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken).ConfigureAwait(false);
            var loaded = JsonSerializer.Deserialize<NotchSettings>(json, SerializerOptions);

            if (loaded != null)
            {
                lock (_syncRoot)
                {
                    _currentSettings = loaded;
                }
                _logger.LogInformation("Loaded settings successfully from {Path}.", _settingsFilePath);
                SettingsChanged?.Invoke(this, CurrentSettings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}. Retaining current defaults.", _settingsFilePath);
        }

        return CurrentSettings;
    }

    public async Task SaveSettingsAsync(NotchSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken).ConfigureAwait(false);

            lock (_syncRoot)
            {
                _currentSettings = settings.Clone();
            }

            _logger.LogInformation("Saved settings successfully to {Path}.", _settingsFilePath);
            SettingsChanged?.Invoke(this, CurrentSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}.", _settingsFilePath);
            throw;
        }
    }
}
