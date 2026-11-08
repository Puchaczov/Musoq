using System;

namespace Musoq.Evaluator.Tests.Exceptions;

public class SchemaNotFoundException : Exception
{
    public SchemaNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SchemaNotFoundException(string message)
        : base(message)
    {
    }

    public SchemaNotFoundException()
    {
    }
}
