using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class ReferenceComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static readonly ReferenceComparer<T> Instance = new();

    private ReferenceComparer()
    {
    }

    public bool Equals(T? left, T? right)
    {
        return ReferenceEquals(left, right);
    }

    public int GetHashCode(T obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}
