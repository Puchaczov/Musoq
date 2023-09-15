using System;

namespace Musoq.Evaluator.Exceptions;

public class UnknownPropertyException : Exception
{
    public UnknownPropertyException(string message)
        : base(message)
    {
    }
}