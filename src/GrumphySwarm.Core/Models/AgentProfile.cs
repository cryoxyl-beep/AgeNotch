using System.Text.Json.Serialization;

namespace GrumphySwarm.Core.Models;

/// <summary>
/// Represents a locally configured agent profile in the Agents workspace.
/// Execution status is explicitly marked as Standby/Not Connected in Milestone 1.
/// </summary>
public sealed class AgentProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("taskType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AgentTaskType TaskType { get; set; } = AgentTaskType.DesktopAssistant;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("statusNote")]
    public string StatusNote { get; set; } = "Standby (Execution Not Connected)";
}
