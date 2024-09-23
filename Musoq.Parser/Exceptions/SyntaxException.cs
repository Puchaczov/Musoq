using System;

namespace Musoq.Parser.Exceptions;

public class SyntaxException(string message, string queryPart) : Exception(message)
{
    public string QueryPart { get; } = queryPart;
}