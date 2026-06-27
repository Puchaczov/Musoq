namespace Musoq.Converter;

public sealed class MultiStatementQueryException : Exception
{
    public MultiStatementQueryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MultiStatementQueryException(string message)
        : base(message)
    {
    }

    public MultiStatementQueryException()
        : base("Multi-statement queries are not supported. Submit one statement at a time.")
    {
    }
}