using System;

namespace Musoq.Evaluator.Exceptions;

public class ConstructionNotYetSupported : Exception
{
    public ConstructionNotYetSupported(string message)
        : base(message)
    {
    }
}