namespace Kyna.Common.Events;

public class CommunicationEventArgs(string? message, string? scope) : EventArgs
{
    public string? Message { get; } = message;
    public string? Scope { get; } = scope;
}
