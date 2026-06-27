namespace Musoq.Plugins;
/// <summary>
///     Advertises optimized aggregate execution capabilities for built-in LibraryBase window aggregates.
/// </summary>
public sealed partial class LibraryBaseWindowAggregateCapabilityProvider : IWindowAggregateCapabilityProvider
{
    private const WindowAggregateCapabilities TypedAggregateModes =
        WindowAggregateCapabilities.WholePartition |
        WindowAggregateCapabilities.Running |
        WindowAggregateCapabilities.BoundedRows |
        WindowAggregateCapabilities.TypedInput |
        WindowAggregateCapabilities.TypedResult;

    /// <summary>
    ///     Returns a built-in aggregate capability for supported numeric input/result shapes.
    /// </summary>
    /// <param name="context">Requested aggregate shape.</param>
    /// <returns>A capability descriptor, or null when fallback plugin dispatch should be used.</returns>
    public WindowAggregateCapability? GetCapability(WindowAggregateCapabilityContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var functionName = NormalizeFunctionName(context.FunctionName);
        if (functionName == "COUNT")
        {
            return context.ResultType == typeof(int)
                ? CreateCapability(
                    WindowAggregateFunction.Count,
                    context.InputType,
                    typeof(int),
                    typeof(CountWindowAccumulator<>).MakeGenericType(context.InputType))
                : null;
        }

        if (!IsSupportedInputType(context.InputType))
            return null;

        return functionName switch
        {
            "SUM" when context.ResultType == typeof(decimal) => CreateCapability(
                WindowAggregateFunction.Sum,
                context.InputType,
                typeof(decimal),
                typeof(DecimalSumWindowAccumulator<>).MakeGenericType(context.InputType)),
            "AVG" when context.ResultType == typeof(decimal) => CreateCapability(
                WindowAggregateFunction.Avg,
                context.InputType,
                typeof(decimal),
                typeof(DecimalAverageWindowAccumulator<>).MakeGenericType(context.InputType)),
            _ => TryCreateMinMaxCapability(functionName, context.InputType, context.ResultType)
        };
    }

    private static WindowAggregateCapability CreateCapability(
        WindowAggregateFunction function,
        Type inputType,
        Type resultType,
        Type accumulatorType)
    {
        return new WindowAggregateCapability(
            function,
            TypedAggregateModes,
            inputType,
            resultType,
            accumulatorType);
    }

    private static string NormalizeFunctionName(string functionName)
    {
        return functionName.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static bool IsSupportedInputType(Type type)
    {
        var inputType = Nullable.GetUnderlyingType(type) ?? type;
        return inputType == typeof(decimal) ||
               inputType == typeof(int) ||
               inputType == typeof(long) ||
               inputType == typeof(double);
    }

}
