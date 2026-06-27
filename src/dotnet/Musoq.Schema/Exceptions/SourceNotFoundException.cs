namespace Musoq.Schema.Exceptions;

public class SourceNotFoundException : Exception
{
    public SourceNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SourceNotFoundException(string message)
        : base(message)
    {
    }

    public SourceNotFoundException()
    {
    }
}
