using System;

namespace Musoq.Parser.Exceptions;

public class SyntaxException : Exception
{
    public string QueryPart { get; }

    public SyntaxException(string message, string queryPart) : base(message)
    {
        QueryPart = queryPart;
    }

    public SyntaxException(string message, string queryPart, Exception innerException) : base(message, innerException)
    {
        QueryPart = queryPart;
    }
}