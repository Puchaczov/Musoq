namespace Musoq.Schema.Exceptions;

public class InjectSourceNullReferenceException : NullReferenceException
{
    public InjectSourceNullReferenceException(Type type)
        : base(CreateMessage(type))
    {
    }

    public InjectSourceNullReferenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InjectSourceNullReferenceException(string message)
        : base(message)
    {
    }

    public InjectSourceNullReferenceException()
    {
    }

    private static string CreateMessage(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return $"Inject source is null for type {type.FullName}";
    }
}
