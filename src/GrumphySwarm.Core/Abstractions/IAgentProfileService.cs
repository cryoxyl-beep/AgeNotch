using GrumphySwarm.Core.Models;

namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Service interface for local agent profile management and persistence.
/// </summary>
public interface IAgentProfileService
{
    /// <summary>
    /// Occurs when an agent profile is created, updated, or deleted.
    /// </summary>
    event EventHandler? ProfilesChanged;

    /// <summary>
    /// Retrieves all locally saved agent profiles.
    /// </summary>
    Task<IReadOnlyList<AgentProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates an agent profile.
    /// </summary>
    Task SaveProfileAsync(AgentProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent profile by ID.
    /// </summary>
    Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);
}
