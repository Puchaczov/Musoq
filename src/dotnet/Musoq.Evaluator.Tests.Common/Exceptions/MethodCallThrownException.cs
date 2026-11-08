using System;

namespace Musoq.Evaluator.Tests.Exceptions;

public class MethodCallThrownException : Exception
{
    public MethodCallThrownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MethodCallThrownException(string message)
        : base(message)
    {
    }

    public MethodCallThrownException()
    {
    }
}
