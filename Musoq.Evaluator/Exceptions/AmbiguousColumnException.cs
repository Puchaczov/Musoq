using System;

namespace Musoq.Evaluator.Exceptions;

public class AmbiguousColumnException : Exception
{
    public AmbiguousColumnException(string column, string alias1, string alias2)
        : base($"Ambiguous column name {column} between {alias1} and {alias2} aliases.")
    {
    }
}