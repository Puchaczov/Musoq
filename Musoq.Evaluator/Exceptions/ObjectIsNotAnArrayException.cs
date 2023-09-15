using System;

namespace Musoq.Evaluator.Exceptions;

public class ObjectIsNotAnArrayException : Exception
{
    public ObjectIsNotAnArrayException(string message)
        : base(message)
    {
    }
}