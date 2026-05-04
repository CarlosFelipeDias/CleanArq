namespace CleanArq.Domain.Common;

/// <summary>
/// Represents an error/validation notification within the domain
/// </summary>
public sealed record Notification
{
    /// <summary>
    /// Property name that generated the error
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; }

    public Notification(string message)
    {
        PropertyName = string.Empty;
        Message = message;
    }

    public Notification(string propertyName, string message)
    {
        PropertyName = propertyName;
        Message = message;
    }

    public override string ToString() =>
        string.IsNullOrEmpty(PropertyName)
            ? Message
            : $"{PropertyName}: {Message}";
}
