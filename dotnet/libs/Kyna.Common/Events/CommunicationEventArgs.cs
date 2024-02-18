namespace Kyna.Common.Events;

public sealed class CommunicationEventArgs(string? message, string? scope) : EventArgs
{
    public string? Message { get; } = message;
    public string? Scope { get; } = scope;
}
