using System;

namespace Musoq.Evaluator.Exceptions;

public class TypeNotFoundException : Exception
{
    public TypeNotFoundException(string message)
        : base(message)
    {

    }
}