namespace GrumphySwarm.Core.Settings;

/// <summary>
/// Service contract for loading, persisting, and notifying changes to notch settings.
/// </summary>
public interface ISettingsService
{
    NotchSettings CurrentSettings { get; }
    NotchSettings LoadSettings();
    Task<NotchSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(NotchSettings settings, CancellationToken cancellationToken = default);
    event EventHandler<NotchSettings>? SettingsChanged;
}
