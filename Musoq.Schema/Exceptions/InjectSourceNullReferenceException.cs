using System;

namespace Musoq.Schema.Exceptions;

public class InjectSourceNullReferenceException : NullReferenceException
{
    public InjectSourceNullReferenceException(Type type)
        : base($"Inject source is null for type {type.FullName}")
    {
    }
}