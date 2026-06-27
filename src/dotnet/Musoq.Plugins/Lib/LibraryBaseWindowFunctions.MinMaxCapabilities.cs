namespace Musoq.Plugins;

/// <summary>
///     Provides minimum and maximum aggregate capability descriptors.
/// </summary>
public sealed partial class LibraryBaseWindowAggregateCapabilityProvider
{
    private static WindowAggregateCapability? TryCreateMinMaxCapability(
        string functionName,
        Type inputType,
        Type resultType)
    {
        if (!IsMinMaxResultType(inputType, resultType))
            return null;

        return functionName switch
        {
            "MIN" => CreateCapability(
                WindowAggregateFunction.Min,
                inputType,
                resultType,
                typeof(MinWindowAccumulator<,>).MakeGenericType(inputType, resultType)),
            "MAX" => CreateCapability(
                WindowAggregateFunction.Max,
                inputType,
                resultType,
                typeof(MaxWindowAccumulator<,>).MakeGenericType(inputType, resultType)),
            _ => null
        };
    }

    private static bool IsMinMaxResultType(Type inputType, Type resultType)
    {
        var expectedResultType = inputType.IsValueType && Nullable.GetUnderlyingType(inputType) == null
            ? typeof(Nullable<>).MakeGenericType(inputType)
            : inputType;

        return resultType == expectedResultType;
    }
}
