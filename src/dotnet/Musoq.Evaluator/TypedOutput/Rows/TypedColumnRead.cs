using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Tables;

/// <summary>Reads a generated column with an exact type check and no erased value.</summary>
public static class TypedColumnRead
{
    /// <summary>Consumes a concrete assignment result without an erased argument or a local-name collision.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Discard<T>(T value)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T, TValue>(TValue value)
    {
        if (typeof(T) != typeof(TValue))
            throw new InvalidOperationException($"Column has type '{typeof(TValue)}', not '{typeof(T)}'.");
        return Unsafe.As<TValue, T>(ref value);
    }
}
