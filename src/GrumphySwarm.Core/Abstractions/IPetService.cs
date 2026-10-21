namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Reserved interface for future desktop pet hosting milestone.
/// Allows the notch window to safely display pet visual slots without requiring the pet engine yet.
/// </summary>
public interface IPetService
{
    string ActivePetName { get; }
    string Mood { get; }
    bool IsVisible { get; }
}
