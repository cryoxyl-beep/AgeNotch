using System.IO;
using System.Text.Json;
using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Services;

/// <summary>
/// JSON file-backed persistence service for local Agent profiles.
/// Persists agent configurations in %LocalAppData%\GrumphySwarm\agents.json.
/// </summary>
public sealed class JsonAgentProfileService : IAgentProfileService
{
    private readonly string _filePath;
    private readonly ILogger<JsonAgentProfileService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly List<AgentProfile> _cachedProfiles = new();
    private bool _isLoaded;

    public event EventHandler? ProfilesChanged;

    public JsonAgentProfileService(string? filePath = null, ILogger<JsonAgentProfileService>? logger = null)
    {
        _logger = logger ?? NullLogger<JsonAgentProfileService>.Instance;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "GrumphySwarm");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "agents.json");
        }
        else
        {
            _filePath = filePath;
        }
    }

    public async Task<IReadOnlyList<AgentProfile>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isLoaded)
            {
                await LoadInternalAsync(cancellationToken);
            }

            return _cachedProfiles.ToList();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveProfileAsync(AgentProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isLoaded)
            {
                await LoadInternalAsync(cancellationToken);
            }

            int index = _cachedProfiles.FindIndex(p => p.Id == profile.Id);
            if (index >= 0)
            {
                _cachedProfiles[index] = profile;
            }
            else
            {
                _cachedProfiles.Add(profile);
            }

            await SaveInternalAsync(cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }

        ProfilesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId)) return false;

        bool removed = false;
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isLoaded)
            {
                await LoadInternalAsync(cancellationToken);
            }

            int countBefore = _cachedProfiles.Count;
            _cachedProfiles.RemoveAll(p => p.Id == profileId);
            removed = _cachedProfiles.Count < countBefore;

            if (removed)
            {
                await SaveInternalAsync(cancellationToken);
            }
        }
        finally
        {
            _fileLock.Release();
        }

        if (removed)
        {
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    private async Task LoadInternalAsync(CancellationToken cancellationToken)
    {
        _cachedProfiles.Clear();

        if (File.Exists(_filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                var loaded = JsonSerializer.Deserialize<List<AgentProfile>>(json);
                if (loaded != null && loaded.Count > 0)
                {
                    _cachedProfiles.AddRange(loaded);
                    _isLoaded = true;
                    _logger.LogInformation("Loaded {Count} agent profiles from {Path}.", _cachedProfiles.Count, _filePath);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read agent profiles from {Path}. Using default starter profiles.", _filePath);
            }
        }

        // Starter template profiles
        _cachedProfiles.Add(new AgentProfile
        {
            Id = "agent_desktop_asst",
            Name = "Desktop Assistant",
            Description = "Monitors active desktop tasks, window organization, and productivity workflows.",
            TaskType = AgentTaskType.DesktopAssistant,
            StatusNote = "Standby (Execution Not Connected)"
        });

        _cachedProfiles.Add(new AgentProfile
        {
            Id = "agent_task_monitor",
            Name = "System Task Monitor",
            Description = "Tracks background system workloads, resource bottlenecks, and network anomalies.",
            TaskType = AgentTaskType.TaskMonitor,
            StatusNote = "Standby (Execution Not Connected)"
        });

        _isLoaded = true;
        await SaveInternalAsync(cancellationToken);
    }

    private async Task SaveInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_cachedProfiles, options);

            var tempFile = $"{_filePath}.tmp";
            await File.WriteAllTextAsync(tempFile, json, cancellationToken);
            File.Move(tempFile, _filePath, overwrite: true);

            _logger.LogInformation("Saved {Count} agent profiles to {Path}.", _cachedProfiles.Count, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist agent profiles to {Path}.", _filePath);
        }
    }
}
