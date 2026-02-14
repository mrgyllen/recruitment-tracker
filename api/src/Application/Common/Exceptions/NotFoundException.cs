namespace api.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException()
        : base()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string name, object key)
        : base("The requested resource was not found.")
    {
        EntityName = name;
        EntityKey = key;
    }

    public string? EntityName { get; }
    public object? EntityKey { get; }
}
