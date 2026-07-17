namespace Musoq.Plugins;

#pragma warning disable CS1591
public readonly record struct CorrelatedScalarSubqueryResult<T>(T Value, byte Cardinality);

public static class CorrelatedScalarSubqueryResultExtractor
{
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static T GetValue<T>(CorrelatedScalarSubqueryResult<T> result)
    {
        if (result.Cardinality > 1)
            throw new InvalidOperationException("Scalar subquery returned more than one row.");

        return result.Cardinality == 0 ? default! : result.Value;
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static T GetValue<T>(CorrelatedScalarSubqueryResult<T>? result)
    {
        return result.HasValue ? GetValue(result.GetValueOrDefault()) : default!;
    }
}
#pragma warning restore CS1591
