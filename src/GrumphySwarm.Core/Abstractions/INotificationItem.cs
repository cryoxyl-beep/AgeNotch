namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Severity level of notch notifications.
/// </summary>
public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Critical
}

/// <summary>
/// Represents a notification presented through the notch island.
/// </summary>
public interface INotificationItem
{
    string Id { get; }
    string Title { get; }
    string Message { get; }
    NotificationSeverity Severity { get; }
    DateTimeOffset Timestamp { get; }
    TimeSpan DisplayDuration { get; }
}

public sealed record NotificationItem(
    string Id,
    string Title,
    string Message,
    NotificationSeverity Severity,
    DateTimeOffset Timestamp,
    TimeSpan DisplayDuration) : INotificationItem;

public sealed class NotificationEventArgs : EventArgs
{
    public INotificationItem Notification { get; }

    public NotificationEventArgs(INotificationItem notification)
    {
        Notification = notification;
    }
}

/// <summary>
/// Service abstraction for posting notifications to the notch without coupling to the caller.
/// </summary>
public interface INotificationService
{
    void Post(INotificationItem notification);
    event EventHandler<NotificationEventArgs>? NotificationPosted;
}
