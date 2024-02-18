namespace Kyna.Infrastructure.DataImport;

public sealed class ApiLimitReachedException : Exception
{
    public ApiLimitReachedException()
    {
    }

    public ApiLimitReachedException(string? message) : base(message)
    {
    }

    public ApiLimitReachedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
