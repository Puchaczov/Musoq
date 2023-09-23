using System;

namespace Musoq.Evaluator.Exceptions;

public class ObjectDoesNotImplementIndexerException : Exception
{
    public ObjectDoesNotImplementIndexerException(string message)
        : base(message)
    {
    }
}