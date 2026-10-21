namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Status state of a swarm agent.
/// </summary>
public enum AgentState
{
    Idle,
    Listening,
    Thinking,
    Executing,
    Error,
    Offline
}

/// <summary>
/// Model representing the live status of an AI agent or task runner.
/// Designed for future swarm integration without coupling the notch host to any LLM runtime.
/// </summary>
public interface IAgentStatus
{
    string AgentId { get; }
    string Name { get; }
    string Role { get; }
    string CurrentActivity { get; }
    AgentState State { get; }
    DateTimeOffset LastActive { get; }
}

/// <summary>
/// Event arguments delivered when agent statuses are updated.
/// </summary>
public sealed class AgentStatusChangedEventArgs : EventArgs
{
    public IAgentStatus Agent { get; }

    public AgentStatusChangedEventArgs(IAgentStatus agent)
    {
        Agent = agent;
    }
}

/// <summary>
/// Service abstraction for monitoring agent status updates from future background processes.
/// </summary>
public interface IAgentStatusService
{
    IReadOnlyList<IAgentStatus> GetAllAgents();
    IAgentStatus? GetAgent(string agentId);
    event EventHandler<AgentStatusChangedEventArgs>? AgentStatusChanged;
}

/// <summary>
/// Standard immutable record implementation of IAgentStatus.
/// </summary>
public sealed record AgentStatus(
    string AgentId,
    string Name,
    string Role,
    string CurrentActivity,
    AgentState State,
    DateTimeOffset LastActive) : IAgentStatus;
